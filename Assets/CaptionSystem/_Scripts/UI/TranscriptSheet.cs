using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Displays a scrollable transcript of all captions with auto-highlighting of current caption
/// Automatically detects and displays transcripts for any playing audio with captions in the database
/// </summary>
[RequireComponent(typeof(Canvas))]
public class TranscriptSheet : MonoBehaviour, IUIBehavior
{
    [Header("UI References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform contentContainer;
    [SerializeField] private GameObject transcriptEntryPrefab;

    [Header("Highlighting")]
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color highlightedTextColor = Color.yellow;
    [SerializeField] private Color normalBackgroundColor = new Color(0, 0, 0, 0.3f);
    [SerializeField] private Color highlightedBackgroundColor = new Color(1, 1, 0, 0.5f);

    [Header("Auto-Scroll")]
    [SerializeField] private bool autoScrollToCurrentCaption = true;
    [SerializeField] private float scrollSpeed = 5f;
    [SerializeField] private bool centerCurrentCaption = true;

    [Header("Content Panel Sizing")]
    [SerializeField] private bool adjustPanelSize = true;
    [SerializeField] private float topPadding = 100f; // Space above first entry
    [SerializeField] private float bottomPadding = 100f; // Space below last entry
    [SerializeField] private float minContentHeight = 200f; // Minimum panel height

    [Header("Optional Features")]
    [SerializeField] private bool allowClickToSeek = false; // Click caption to jump to that time

    [Header("Auto-Detection")]
    [SerializeField] private bool autoDetectPlayingAudio = true;
    [SerializeField] private AudioSource manualAudioSource; // Optional: assign specific audio source
    [SerializeField] private float detectionCheckInterval = 0.5f;

    private Canvas canvas;
    private bool isActive = false;

    // Transcript data
    private List<CaptionEntry> captions = new List<CaptionEntry>();
    private List<TranscriptEntryUI> entryUIElements = new List<TranscriptEntryUI>();
    private int currentHighlightedIndex = -1;

    // Audio source tracking
    private AudioSource trackedAudioSource;
    private AudioClip lastTrackedClip;
    private bool isTrackingAudio = false;
    private float nextDetectionCheckTime = 0f;

    public bool IsActive => isActive;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();

        // Validate references
        if (scrollRect == null)
        {
            scrollRect = GetComponentsInChildren<ScrollRect>(true)[0];
        }

        if (contentContainer == null && scrollRect != null)
        {
            contentContainer = scrollRect.content;
        }

        if (transcriptEntryPrefab == null)
        {
            Debug.LogError("TranscriptSheet: No transcript entry prefab assigned!");
        }
    }

    private void Start()
    {
        // If transcript sheet is active in scene, start it
        if (canvas.gameObject.activeInHierarchy)
        {
            OnUIShown();
        }
    }

    private void Update()
    {
        // Ensure UpdateBehavior is called
        if (isActive)
        {
            UpdateBehavior();
        }
    }

    public void OnUIShown()
    {
        isActive = true;

        // If manual audio source is assigned, use it
        if (manualAudioSource != null)
        {
            LoadTranscriptForAudioSource(manualAudioSource);
        }
        else if (autoDetectPlayingAudio)
        {
            // Try to auto-detect playing audio
            TryAutoDetectAudioSource();
        }
    }

    public void OnUIHidden()
    {
        isActive = false;
        isTrackingAudio = false;
    }

    public void UpdateBehavior()
    {
        if (!isActive) return;

        // Continuously check for new or changed audio if auto-detect is enabled
        if (autoDetectPlayingAudio && Time.time >= nextDetectionCheckTime)
        {
            CheckForAudioChanges();
            nextDetectionCheckTime = Time.time + detectionCheckInterval;
        }

        // Update highlighting for current audio
        if (isTrackingAudio && trackedAudioSource != null && trackedAudioSource.isPlaying)
        {
            UpdateHighlighting();
        }
    }

    #region Auto-Detection

    /// <summary>
    /// Check if the currently tracked audio has changed or stopped, and detect new audio
    /// </summary>
    private void CheckForAudioChanges()
    {
        // If we have a manual audio source assigned, always prefer it
        if (manualAudioSource != null)
        {
            if (trackedAudioSource != manualAudioSource || lastTrackedClip != manualAudioSource.clip)
            {
                LoadTranscriptForAudioSource(manualAudioSource);
            }
            return;
        }

        // Check if tracked audio source is still valid and playing
        if (trackedAudioSource != null)
        {
            // Audio stopped or clip changed
            if (!trackedAudioSource.isPlaying || trackedAudioSource.clip != lastTrackedClip)
            {
                // Try to find new playing audio
                TryAutoDetectAudioSource();
            }
        }
        else
        {
            // No audio is tracked, try to find one
            TryAutoDetectAudioSource();
        }
    }

    /// <summary>
    /// Automatically detect any playing audio source with captions in the database
    /// </summary>
    private void TryAutoDetectAudioSource()
    {
        if (GlobalCaptionManager.Instance == null)
        {
            Debug.LogWarning("TranscriptSheet: GlobalCaptionManager not found");
            return;
        }

        var database = GlobalCaptionManager.Instance.GetCaptionDatabase();
        if (database == null)
        {
            Debug.LogWarning("TranscriptSheet: No caption database found");
            return;
        }

        // Find all audio sources in the scene
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();

        // Look for any playing audio source with captions
        foreach (var audioSource in allAudioSources)
        {
            if (audioSource.isPlaying && audioSource.clip != null)
            {
                // Check if this audio has captions in the database
                if (database.HasCaptionForClip(audioSource.clip))
                {
                    // Found a playing audio with captions!
                    LoadTranscriptForAudioSource(audioSource);
                    Debug.Log($"TranscriptSheet: Auto-detected audio: {audioSource.name} with clip: {audioSource.clip.name}");
                    return;
                }
            }
        }

        // No playing audio found - check if GlobalCaptionManager has active sessions
        if (GlobalCaptionManager.Instance.GetActiveSessionCount() > 0)
        {
            // Try to find audio sources with active caption sessions
            foreach (var audioSource in allAudioSources)
            {
                if (GlobalCaptionManager.Instance.HasActiveCaptions(audioSource))
                {
                    LoadTranscriptForAudioSource(audioSource);
                    Debug.Log($"TranscriptSheet: Auto-detected audio from active caption session: {audioSource.name}");
                    return;
                }
            }
        }

        // If we previously had audio and now can't find any, clear the transcript
        if (trackedAudioSource != null)
        {
            Debug.Log("TranscriptSheet: No playing audio detected, waiting...");
            // Don't clear yet, maybe audio is paused
        }
    }

    #endregion

    #region Transcript Loading

    /// <summary>
    /// Load transcript for a specific audio source
    /// </summary>
    public void LoadTranscriptForAudioSource(AudioSource audioSource)
    {
        if (audioSource == null)
        {
            Debug.LogWarning("TranscriptSheet: Cannot load transcript for null audio source");
            return;
        }

        // Check if this is the same audio we're already tracking
        if (trackedAudioSource == audioSource && lastTrackedClip == audioSource.clip && isTrackingAudio)
        {
            return; // Already tracking this audio
        }

        trackedAudioSource = audioSource;
        lastTrackedClip = audioSource.clip;

        // Get caption data from GlobalCaptionManager's database
        if (GlobalCaptionManager.Instance != null)
        {
            var database = GlobalCaptionManager.Instance.GetCaptionDatabase();
            if (database != null)
            {
                var entry = database.GetEntryForClip(audioSource.clip);
                if (entry != null && entry.srtFile != null)
                {
                    LoadTranscript(entry.srtFile);
                    isTrackingAudio = true;
                    Debug.Log($"TranscriptSheet: Loaded transcript for {audioSource.name} - {audioSource.clip.name}");
                    return;
                }
            }
        }

        Debug.LogWarning($"TranscriptSheet: No caption data found for audio source {audioSource.name}");
        isTrackingAudio = false;
    }

    /// <summary>
    /// Load transcript from SRT file
    /// </summary>
    public void LoadTranscript(TextAsset srtFile)
    {
        if (srtFile == null)
        {
            Debug.LogError("TranscriptSheet: SRT file is null");
            return;
        }

        // Parse SRT file
        captions = SRTParser.ParseSRT(srtFile.text);

        // Clear existing UI
        ClearTranscriptUI();

        // Create UI for each caption
        CreateTranscriptUI();

        Debug.Log($"TranscriptSheet: Loaded {captions.Count} captions");
    }

    /// <summary>
    /// Manually load transcript from caption entries (for advanced use)
    /// </summary>
    public void LoadTranscript(List<CaptionEntry> captionEntries)
    {
        captions = new List<CaptionEntry>(captionEntries);
        ClearTranscriptUI();
        CreateTranscriptUI();
    }

    #endregion

    #region UI Creation

    private void CreateTranscriptUI()
    {
        if (contentContainer == null || transcriptEntryPrefab == null)
        {
            Debug.LogError("TranscriptSheet: Missing required references for UI creation");
            return;
        }

        for (int i = 0; i < captions.Count; i++)
        {
            CaptionEntry caption = captions[i];

            // Instantiate entry
            GameObject entryObj = Instantiate(transcriptEntryPrefab, contentContainer);
            TranscriptEntryUI entryUI = entryObj.GetComponent<TranscriptEntryUI>();

            if (entryUI == null)
            {
                entryUI = entryObj.AddComponent<TranscriptEntryUI>();
            }

            // Setup entry
            entryUI.Setup(caption, i, normalTextColor, normalBackgroundColor);

            // Optional: Setup click handler for seeking
            if (allowClickToSeek)
            {
                int captionIndex = i; // Capture for closure
                entryUI.SetClickHandler(() => OnTranscriptEntryClicked(captionIndex));
            }

            entryUIElements.Add(entryUI);
        }

        // Adjust content panel size after all entries are created
        if (adjustPanelSize)
        {
            StartCoroutine(AdjustContentPanelSizeDelayed());
        }

        Debug.Log($"TranscriptSheet: Created {entryUIElements.Count} UI elements");
    }

    /// <summary>
    /// Adjust the content panel size to fit all transcript entries with proper padding
    /// This allows better centering of captions during scrolling
    /// </summary>
    private System.Collections.IEnumerator AdjustContentPanelSizeDelayed()
    {
        // Wait for layout to calculate
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return null;

        AdjustContentPanelSize();
    }

    private void AdjustContentPanelSize()
    {
        if (contentContainer == null || scrollRect == null) return;

        // Force layout update
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentContainer);

        // Get the natural height of all content
        float naturalHeight = 0f;
        VerticalLayoutGroup layoutGroup = contentContainer.GetComponent<VerticalLayoutGroup>();

        if (layoutGroup != null)
        {
            // Calculate total height from entries and spacing
            float totalEntryHeight = 0f;
            foreach (var entry in entryUIElements)
            {
                if (entry != null)
                {
                    RectTransform rect = entry.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        totalEntryHeight += rect.rect.height;
                    }
                }
            }

            float spacing = layoutGroup.spacing * Mathf.Max(0, entryUIElements.Count - 1);
            float padding = layoutGroup.padding.top + layoutGroup.padding.bottom;
            naturalHeight = totalEntryHeight + spacing + padding;
        }
        else
        {
            // Fallback: use content's preferred height
            naturalHeight = LayoutUtility.GetPreferredHeight(contentContainer);
        }

        // Add top and bottom padding for better scrolling/centering
        float viewportHeight = scrollRect.viewport.rect.height;
        float desiredHeight = naturalHeight + topPadding + bottomPadding;

        // Ensure minimum height
        desiredHeight = Mathf.Max(desiredHeight, minContentHeight, viewportHeight);

        // Set the content container size
        Vector2 sizeDelta = contentContainer.sizeDelta;
        sizeDelta.y = desiredHeight;
        contentContainer.sizeDelta = sizeDelta;

        // Adjust padding in the layout group to center content
        if (layoutGroup != null)
        {
            layoutGroup.padding.top = (int)topPadding;
            layoutGroup.padding.bottom = (int)bottomPadding;
        }

        Debug.Log($"TranscriptSheet: Adjusted content panel size to {desiredHeight} (natural: {naturalHeight}, viewport: {viewportHeight})");
    }

    private void ClearTranscriptUI()
    {
        foreach (var entry in entryUIElements)
        {
            if (entry != null)
            {
                Destroy(entry.gameObject);
            }
        }

        entryUIElements.Clear();
        currentHighlightedIndex = -1;
    }

    #endregion

    #region Highlighting

    private void UpdateHighlighting()
    {
        if (trackedAudioSource == null || !trackedAudioSource.isPlaying)
        {
            return;
        }

        float currentTime = trackedAudioSource.time;

        // Find which caption should be highlighted
        int newHighlightIndex = -1;
        for (int i = 0; i < captions.Count; i++)
        {
            if (captions[i].IsActiveAtTime(currentTime))
            {
                newHighlightIndex = i;
                break;
            }
        }

        // Update highlighting if changed
        if (newHighlightIndex != currentHighlightedIndex)
        {
            // Unhighlight previous
            if (currentHighlightedIndex >= 0 && currentHighlightedIndex < entryUIElements.Count)
            {
                entryUIElements[currentHighlightedIndex].SetHighlight(false, normalTextColor, normalBackgroundColor);
            }

            // Highlight new
            if (newHighlightIndex >= 0 && newHighlightIndex < entryUIElements.Count)
            {
                entryUIElements[newHighlightIndex].SetHighlight(true, highlightedTextColor, highlightedBackgroundColor);

                // Auto-scroll to highlighted entry
                if (autoScrollToCurrentCaption)
                {
                    ScrollToEntry(newHighlightIndex);
                }
            }

            currentHighlightedIndex = newHighlightIndex;
        }
    }

    private void ScrollToEntry(int index)
    {
        if (scrollRect == null || index < 0 || index >= entryUIElements.Count)
        {
            return;
        }

        RectTransform entryRect = entryUIElements[index].GetComponent<RectTransform>();
        if (entryRect == null) return;

        Canvas.ForceUpdateCanvases();

        // Get viewport and content dimensions
        float viewportHeight = scrollRect.viewport.rect.height;
        float contentHeight = contentContainer.rect.height;

        // Calculate the position of the entry relative to content container
        float entryY = -entryRect.anchoredPosition.y;
        float entryHeight = entryRect.rect.height;

        float targetY;

        if (centerCurrentCaption)
        {
            // Center the entry in the viewport
            targetY = entryY - (viewportHeight / 2f) + (entryHeight / 2f);
        }
        else
        {
            // Just ensure entry is visible (scroll to top of entry)
            targetY = entryY;
        }

        // Convert to normalized position (0 = bottom, 1 = top)
        float normalizedPosition = 1f;
        if (contentHeight > viewportHeight)
        {
            normalizedPosition = Mathf.Clamp01(targetY / (contentHeight - viewportHeight));
            normalizedPosition = 1f - normalizedPosition; // Invert for Unity's scroll system
        }

        // Smooth scroll to target position
        scrollRect.verticalNormalizedPosition = Mathf.Lerp(
            scrollRect.verticalNormalizedPosition,
            normalizedPosition,
            scrollSpeed * Time.deltaTime
        );
    }

    #endregion

    #region Click Handling

    private void OnTranscriptEntryClicked(int index)
    {
        if (!allowClickToSeek || trackedAudioSource == null)
        {
            return;
        }

        if (index >= 0 && index < captions.Count)
        {
            // Seek to the start time of this caption
            float seekTime = captions[index].startTime;
            trackedAudioSource.time = seekTime;

            Debug.Log($"TranscriptSheet: Seeked to caption {index} at time {seekTime}s");
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Set the audio source to track (disables auto-detection)
    /// </summary>
    public void SetManualAudioSource(AudioSource audioSource)
    {
        manualAudioSource = audioSource;

        if (isActive)
        {
            LoadTranscriptForAudioSource(audioSource);
        }
    }

    /// <summary>
    /// Clear manual audio source and re-enable auto-detection
    /// </summary>
    public void ClearManualAudioSource()
    {
        manualAudioSource = null;
        TryAutoDetectAudioSource();
    }

    /// <summary>
    /// Clear the transcript sheet
    /// </summary>
    public void ClearTranscript()
    {
        ClearTranscriptUI();
        captions.Clear();
        isTrackingAudio = false;
        trackedAudioSource = null;
        lastTrackedClip = null;
    }

    /// <summary>
    /// Toggle auto-scroll feature
    /// </summary>
    public void SetAutoScroll(bool enabled)
    {
        autoScrollToCurrentCaption = enabled;
    }

    /// <summary>
    /// Toggle centering of current caption
    /// </summary>
    public void SetCenterCurrentCaption(bool enabled)
    {
        centerCurrentCaption = enabled;
    }

    /// <summary>
    /// Set padding values for content panel
    /// </summary>
    public void SetContentPadding(float top, float bottom)
    {
        topPadding = top;
        bottomPadding = bottom;

        if (entryUIElements.Count > 0)
        {
            AdjustContentPanelSize();
        }
    }

    /// <summary>
    /// Manually trigger content panel size adjustment
    /// </summary>
    public void RefreshPanelSize()
    {
        if (entryUIElements.Count > 0)
        {
            AdjustContentPanelSize();
        }
    }

    /// <summary>
    /// Toggle click-to-seek feature
    /// </summary>
    public void SetClickToSeek(bool enabled)
    {
        allowClickToSeek = enabled;
    }

    /// <summary>
    /// Enable or disable auto-detection
    /// </summary>
    public void SetAutoDetection(bool enabled)
    {
        autoDetectPlayingAudio = enabled;

        if (enabled && isActive)
        {
            TryAutoDetectAudioSource();
        }
    }

    /// <summary>
    /// Get the number of captions in the transcript
    /// </summary>
    public int GetCaptionCount()
    {
        return captions.Count;
    }

    /// <summary>
    /// Check if transcript is currently tracking an audio source
    /// </summary>
    public bool IsTrackingAudio()
    {
        return isTrackingAudio && trackedAudioSource != null;
    }

    /// <summary>
    /// Get the currently tracked audio source
    /// </summary>
    public AudioSource GetTrackedAudioSource()
    {
        return trackedAudioSource;
    }

    #endregion

#if UNITY_EDITOR
    [ContextMenu("Force Detect Audio")]
    private void EditorForceDetect()
    {
        TryAutoDetectAudioSource();
    }

    [ContextMenu("Refresh Transcript")]
    private void EditorRefreshTranscript()
    {
        if (trackedAudioSource != null)
        {
            LoadTranscriptForAudioSource(trackedAudioSource);
        }
    }

    [ContextMenu("Clear Transcript")]
    private void EditorClearTranscript()
    {
        ClearTranscript();
    }
#endif
}