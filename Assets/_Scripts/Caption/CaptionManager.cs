using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class CaptionManager : MonoBehaviour
{
    [Header("Caption UI References")]
    public Canvas captionCanvas;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI captionText;

    [Header("Settings")]
    public bool showCaptions = true;

    [Header("Audio References")]
    public AudioSource audioSource;
    public TextAsset SRTFile;

    // Current audio and caption data
    private AudioSource currentAudioSource;
    private List<CaptionEntry> currentCaptions = new List<CaptionEntry>();
    private int currentCaptionIndex = -1;
    private bool wasPlayingLastFrame = false;
    private bool captionSystemActive = false;

    // Events for external scripts
    public System.Action<CaptionEntry> OnCaptionChanged;
    public System.Action OnCaptionSystemStarted;
    public System.Action OnCaptionSystemStopped;

    #region Singleton Setup
    public static CaptionManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    #endregion

    private void Start()
    {
        // Initialize with captions hidden
        HideCaptions();

        SetupCaptions(audioSource, SRTFile);
    }

    private void Update()
    {
        if (currentAudioSource != null)
        {
            DetectAudioStateChanges();

            if (captionSystemActive && currentAudioSource.isPlaying)
            {
                UpdateCaptions();
            }
        }

        // Update SmoothFollowBehavior only when canvas is actually visible
        if (captionSystemActive && captionCanvas != null && captionCanvas.gameObject.activeInHierarchy)
        {
            IUIBehavior behavior = captionCanvas.GetComponent<IUIBehavior>();
            if (behavior != null)
            {
                behavior.UpdateBehavior();
            }
        }
    }

    private void DetectAudioStateChanges()
    {
        bool isPlayingNow = currentAudioSource.isPlaying;

        // Audio just started playing
        if (isPlayingNow && !wasPlayingLastFrame)
        {
            StartCaptionSystem();
        }
        // Audio just stopped playing
        else if (!isPlayingNow && wasPlayingLastFrame && captionSystemActive)
        {
            StopCaptionSystem();
        }

        wasPlayingLastFrame = isPlayingNow;
    }

    private void StartCaptionSystem()
    {
        if (currentCaptions.Count == 0)
        {
            Debug.LogWarning("No captions loaded for current audio!");
            return;
        }

        captionSystemActive = true;
        currentCaptionIndex = -1;

        // Don't show canvas immediately - wait for first caption to appear
        // Canvas will be shown in ShowCaption() when there's actual content

        OnCaptionSystemStarted?.Invoke();
        Debug.Log("Caption system started automatically with audio");
    }

    private void StopCaptionSystem()
    {
        captionSystemActive = false;
        currentCaptionIndex = -1;

        // Hide caption canvas directly
        if (captionCanvas != null)
        {
            // Notify the SmoothFollowBehavior that the UI is being hidden
            IUIBehavior behavior = captionCanvas.GetComponent<IUIBehavior>();
            if (behavior != null)
            {
                behavior.OnUIHidden();
            }

            captionCanvas.gameObject.SetActive(false);
        }

        OnCaptionSystemStopped?.Invoke();
        Debug.Log("Caption system stopped automatically with audio");
    }

    private void UpdateCaptions()
    {
        if (currentCaptions.Count == 0) return;

        float currentTime = currentAudioSource.time;

        // Find the caption that should be active now
        CaptionEntry activeCaption = null;
        int activeCaptionIndex = -1;

        for (int i = 0; i < currentCaptions.Count; i++)
        {
            if (currentCaptions[i].IsActiveAtTime(currentTime))
            {
                activeCaption = currentCaptions[i];
                activeCaptionIndex = i;
                break;
            }
        }

        // Update UI if caption changed
        if (activeCaptionIndex != currentCaptionIndex)
        {
            currentCaptionIndex = activeCaptionIndex;

            if (activeCaption != null)
            {
                ShowCaption(activeCaption);
            }
            else
            {
                HideCurrentCaption();
            }
        }
    }

    private void ShowCaption(CaptionEntry caption)
    {
        if (!showCaptions) return;

        // Show canvas when there's an active caption
        if (captionCanvas != null && !captionCanvas.gameObject.activeInHierarchy)
        {
            captionCanvas.gameObject.SetActive(true);

            // Notify the SmoothFollowBehavior that the UI is now active
            IUIBehavior behavior = captionCanvas.GetComponent<IUIBehavior>();
            if (behavior != null)
            {
                behavior.OnUIShown();
            }
        }

        if (speakerText != null)
        {
            speakerText.text = caption.speaker;
        }

        if (captionText != null)
        {
            captionText.text = caption.text;
        }

        // Notify external scripts
        OnCaptionChanged?.Invoke(caption);

        Debug.Log($"Caption: [{caption.speaker}] {caption.text}");
    }

    private void HideCurrentCaption()
    {
        // Clear text
        if (speakerText != null)
        {
            speakerText.text = "";
        }

        if (captionText != null)
        {
            captionText.text = "";
        }

        // Hide canvas during gaps between captions
        if (captionCanvas != null && captionCanvas.gameObject.activeInHierarchy)
        {
            IUIBehavior behavior = captionCanvas.GetComponent<IUIBehavior>();
            if (behavior != null)
            {
                behavior.OnUIHidden();
            }
            captionCanvas.gameObject.SetActive(false);
        }
    }

    private void HideCaptions()
    {
        HideCurrentCaption();

        // Hide canvas directly
        if (captionCanvas != null)
        {
            IUIBehavior behavior = captionCanvas.GetComponent<IUIBehavior>();
            if (behavior != null)
            {
                behavior.OnUIHidden();
            }
            captionCanvas.gameObject.SetActive(false);
        }
    }

    #region Public Methods - For External Game Managers

    /// <summary>
    /// Setup captions for a specific audio source with SRT content
    /// Call this before playing the audio
    /// </summary>
    public void SetupCaptions(AudioSource audioSource, string srtContent)
    {
        currentAudioSource = audioSource;
        currentCaptions = SRTParser.ParseSRT(srtContent);
        wasPlayingLastFrame = false;

        Debug.Log($"Caption system setup for audio source: {audioSource.name} with {currentCaptions.Count} captions");
    }

    /// <summary>
    /// Setup captions for a specific audio source with SRT file
    /// Call this before playing the audio
    /// </summary>
    public void SetupCaptions(AudioSource audioSource, TextAsset srtFile)
    {
        if (srtFile != null)
        {
            SetupCaptions(audioSource, srtFile.text);
        }
        else
        {
            Debug.LogError("SRT file is null!");
        }
    }

    /// <summary>
    /// Clear current caption setup
    /// </summary>
    public void ClearCaptions()
    {
        if (captionSystemActive)
        {
            StopCaptionSystem();
        }

        currentAudioSource = null;
        currentCaptions.Clear();
        currentCaptionIndex = -1;
        wasPlayingLastFrame = false;

        Debug.Log("Caption system cleared");
    }

    /// <summary>
    /// Toggle caption visibility on/off
    /// </summary>
    public void ToggleCaptions()
    {
        showCaptions = !showCaptions;

        if (!showCaptions && captionSystemActive)
        {
            HideCurrentCaption();
        }

        Debug.Log($"Captions {(showCaptions ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Check if caption system is currently active
    /// </summary>
    public bool IsActive()
    {
        return captionSystemActive;
    }

    /// <summary>
    /// Get current audio source being monitored
    /// </summary>
    public AudioSource GetCurrentAudioSource()
    {
        return currentAudioSource;
    }

    #endregion
}