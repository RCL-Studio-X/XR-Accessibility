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
    [SerializeField] private float scrollSpeed = 10f; // kept in case you want smoothing later

    [Header("Content Panel Sizing")]
    [SerializeField] private bool adjustPanelSize = true;
    [SerializeField] private float topPadding = 100f;
    [SerializeField] private float bottomPadding = 100f;
    [SerializeField] private float minContentHeight = 200f;

    [Header("Optional Features")]
    [SerializeField] private bool allowClickToSeek = false;

    [Header("Auto-Detection")]
    [SerializeField] private bool autoDetectPlayingAudio = true;
    [SerializeField] private AudioSource manualAudioSource;
    [SerializeField] private float detectionCheckInterval = 0.5f;

    private Canvas canvas;
    private bool isActive = false;

    private List<CaptionEntry> captions = new List<CaptionEntry>();
    private List<TranscriptEntryUI> entryUIElements = new List<TranscriptEntryUI>();
    private int currentHighlightedIndex = -1;

    private AudioSource trackedAudioSource;
    private AudioClip lastTrackedClip;
    private bool isTrackingAudio = false;
    private float nextDetectionCheckTime = 0f;

    public bool IsActive => isActive;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();

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
        if (canvas.gameObject.activeInHierarchy)
        {
            OnUIShown();
        }
    }

    private void Update()
    {
        if (isActive)
        {
            UpdateBehavior();
        }
    }

    public void OnUIShown()
    {
        isActive = true;

        if (manualAudioSource != null)
        {
            LoadTranscriptForAudioSource(manualAudioSource);
        }
        else if (autoDetectPlayingAudio)
        {
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

        if (autoDetectPlayingAudio && Time.time >= nextDetectionCheckTime)
        {
            CheckForAudioChanges();
            nextDetectionCheckTime = Time.time + detectionCheckInterval;
        }

        if (isTrackingAudio && trackedAudioSource != null && trackedAudioSource.isPlaying)
        {
            UpdateHighlighting();
        }
    }

    #region Auto-Detection

    private void CheckForAudioChanges()
    {
        if (manualAudioSource != null)
        {
            if (trackedAudioSource != manualAudioSource || lastTrackedClip != manualAudioSource.clip)
            {
                LoadTranscriptForAudioSource(manualAudioSource);
            }
            return;
        }

        if (trackedAudioSource != null)
        {
            if (!trackedAudioSource.isPlaying || trackedAudioSource.clip != lastTrackedClip)
            {
                TryAutoDetectAudioSource();
            }
        }
        else
        {
            TryAutoDetectAudioSource();
        }
    }

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

        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();

        foreach (var audioSource in allAudioSources)
        {
            if (audioSource.isPlaying && audioSource.clip != null)
            {
                if (database.HasCaptionForClip(audioSource.clip))
                {
                    LoadTranscriptForAudioSource(audioSource);
                    Debug.Log($"TranscriptSheet: Auto-detected audio: {audioSource.name} with clip: {audioSource.clip.name}");
                    return;
                }
            }
        }

        if (trackedAudioSource != null)
        {
            Debug.Log("TranscriptSheet: No playing audio detected, waiting...");
        }
    }

    #endregion

    #region Transcript Loading

    public void LoadTranscriptForAudioSource(AudioSource audioSource)
    {
        if (audioSource == null)
        {
            Debug.LogWarning("TranscriptSheet: Cannot load transcript for null audio source");
            return;
        }

        if (trackedAudioSource == audioSource && lastTrackedClip == audioSource.clip && isTrackingAudio)
        {
            return;
        }

        trackedAudioSource = audioSource;
        lastTrackedClip = audioSource.clip;

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

    public void LoadTranscript(TextAsset srtFile)
    {
        if (srtFile == null)
        {
            Debug.LogError("TranscriptSheet: SRT file is null");
            return;
        }

        captions = SRTParser.ParseSRT(srtFile.text);

        ClearTranscriptUI();
        CreateTranscriptUI();
        Debug.Log($"TranscriptSheet: Loaded {captions.Count} captions");
    }

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

            GameObject entryObj = Instantiate(transcriptEntryPrefab, contentContainer);
            TranscriptEntryUI entryUI = entryObj.GetComponent<TranscriptEntryUI>();

            if (entryUI == null)
            {
                entryUI = entryObj.AddComponent<TranscriptEntryUI>();
            }

            entryUI.Setup(caption, i, normalTextColor, normalBackgroundColor);

            if (allowClickToSeek)
            {
                int captionIndex = i;
                entryUI.SetClickHandler(() => OnTranscriptEntryClicked(captionIndex));
            }

            entryUIElements.Add(entryUI);
        }

        if (adjustPanelSize)
        {
            StartCoroutine(AdjustContentPanelSizeDelayed());
        }

        Debug.Log($"TranscriptSheet: Created {entryUIElements.Count} UI elements");
    }

    private System.Collections.IEnumerator AdjustContentPanelSizeDelayed()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentContainer);

        yield return null;

        AdjustContentPanelSize();
    }

    private void AdjustContentPanelSize()
    {
        if (contentContainer == null || scrollRect == null) return;

        ContentSizeFitter fitter = contentContainer.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentContainer);

        float naturalHeight = 0f;
        VerticalLayoutGroup layoutGroup = contentContainer.GetComponent<VerticalLayoutGroup>();

        if (layoutGroup != null)
        {
            float totalEntryHeight = 0f;
            foreach (var entry in entryUIElements)
            {
                if (entry != null)
                {
                    RectTransform rect = entry.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
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
            naturalHeight = LayoutUtility.GetPreferredHeight(contentContainer);
        }

        float viewportHeight = scrollRect.viewport.rect.height;
        float desiredHeight = naturalHeight + topPadding + bottomPadding;

        desiredHeight = Mathf.Max(desiredHeight, minContentHeight, viewportHeight);

        Vector2 sizeDelta = contentContainer.sizeDelta;
        sizeDelta.y = desiredHeight;
        contentContainer.sizeDelta = sizeDelta;

        if (layoutGroup != null)
        {
            layoutGroup.padding.top = (int)topPadding;
            layoutGroup.padding.bottom = (int)bottomPadding;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentContainer);

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

        int newHighlightIndex = -1;
        for (int i = 0; i < captions.Count; i++)
        {
            if (captions[i].IsActiveAtTime(currentTime))
            {
                newHighlightIndex = i;
                break;
            }
        }

        if (newHighlightIndex != currentHighlightedIndex)
        {
            if (currentHighlightedIndex >= 0 && currentHighlightedIndex < entryUIElements.Count)
            {
                entryUIElements[currentHighlightedIndex].SetHighlight(false, normalTextColor, normalBackgroundColor);
            }

            if (newHighlightIndex >= 0 && newHighlightIndex < entryUIElements.Count)
            {
                entryUIElements[newHighlightIndex].SetHighlight(true, highlightedTextColor, highlightedBackgroundColor);

                if (autoScrollToCurrentCaption)
                {
                    ScrollToEntry(newHighlightIndex);
                }
            }

            currentHighlightedIndex = newHighlightIndex;
        }
    }

    /// <summary>
    /// Scrolls step-by-step: keep the initial starting offset, and for entry N
    /// move up by the sum of heights of entries 0..N-1 plus spacing.
    /// Entry 0 does NOT cause a scroll (so no "first jump").
    /// </summary>
    private void ScrollToEntry(int index)
    {
        if (scrollRect == null || index < 0 || index >= entryUIElements.Count)
            return;

        // keep initial view for the first caption
        if (index == 0)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentContainer);

        float viewportHeight = scrollRect.viewport.rect.height;
        float contentHeight = contentContainer.rect.height;

        if (contentHeight <= viewportHeight)
            return;

        VerticalLayoutGroup layoutGroup = contentContainer.GetComponent<VerticalLayoutGroup>();
        float spacing = layoutGroup != null ? layoutGroup.spacing : 0f;

        // start from EXACTLY where we were on load (top of content = whatever padding you set)
        float targetY = 0f;

        // add heights of all previous entries + spacing
        for (int i = 0; i < index; i++)
        {
            var prevRect = entryUIElements[i].GetComponent<RectTransform>();
            if (prevRect != null)
            {
                targetY += prevRect.rect.height;
            }
            targetY += spacing;
        }

        float scrollableHeight = contentHeight - viewportHeight;
        float normalizedPosition = 1f - Mathf.Clamp01(targetY / scrollableHeight);

        scrollRect.verticalNormalizedPosition = normalizedPosition;
    }

    #endregion

    #region Click Handling

    private void OnTranscriptEntryClicked(int index)
    {
        if (!allowClickToSeek || trackedAudioSource == null)
            return;

        if (index >= 0 && index < captions.Count)
        {
            float seekTime = captions[index].startTime;
            trackedAudioSource.time = seekTime;
            Debug.Log($"TranscriptSheet: Seeked to caption {index} at time {seekTime}s");
        }
    }

    #endregion

    #region Public API

    public void SetManualAudioSource(AudioSource audioSource)
    {
        manualAudioSource = audioSource;

        if (isActive)
        {
            LoadTranscriptForAudioSource(audioSource);
        }
    }

    public void ClearManualAudioSource()
    {
        manualAudioSource = null;
        TryAutoDetectAudioSource();
    }

    public void ClearTranscript()
    {
        ClearTranscriptUI();
        captions.Clear();
        isTrackingAudio = false;
        trackedAudioSource = null;
        lastTrackedClip = null;
    }

    public void SetAutoScroll(bool enabled)
    {
        autoScrollToCurrentCaption = enabled;
    }

    public void SetContentPadding(float top, float bottom)
    {
        topPadding = top;
        bottomPadding = bottom;

        if (entryUIElements.Count > 0)
        {
            AdjustContentPanelSize();
        }
    }

    public void RefreshPanelSize()
    {
        if (entryUIElements.Count > 0)
        {
            AdjustContentPanelSize();
        }
    }

    public void SetClickToSeek(bool enabled)
    {
        allowClickToSeek = enabled;
    }

    public void SetAutoDetection(bool enabled)
    {
        autoDetectPlayingAudio = enabled;

        if (enabled && isActive)
        {
            TryAutoDetectAudioSource();
        }
    }

    public int GetCaptionCount()
    {
        return captions.Count;
    }

    public bool IsTrackingAudio()
    {
        return isTrackingAudio && trackedAudioSource != null;
    }

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
