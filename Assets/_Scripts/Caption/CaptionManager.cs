using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class CaptionManager : MonoBehaviour
{
    [Header("Caption Prefab References")]
    [SerializeField] private GameObject captionCanvasPrefab;
    [SerializeField] private Transform parentTransform; // Optional: where to instantiate the canvas

    [Header("Settings")]
    public bool showCaptions = true;
    [SerializeField] private bool destroyOnAudioStop = true; // Whether to destroy or just deactivate

    [Header("Audio References")]
    public AudioSource audioSource;
    public TextAsset SRTFile;

    // Runtime references to instantiated UI
    private Canvas captionCanvas;
    private TextMeshProUGUI speakerText;
    private TextMeshProUGUI captionText;
    private Transform targetTransformOverride; // For UI behavior setup

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
        // Setup captions if audio source and SRT are assigned
        if (audioSource != null && SRTFile != null)
        {
            SetupCaptions(audioSource, SRTFile);
        }
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

        // Update UI behavior only when canvas exists and is active
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

        // Instantiate caption canvas if it doesn't exist
        if (captionCanvas == null)
        {
            InstantiateCaptionCanvas();
        }

        captionSystemActive = true;
        currentCaptionIndex = -1;

        // Canvas will be shown in ShowCaption() when there's actual content
        if (captionCanvas != null)
        {
            captionCanvas.gameObject.SetActive(false);
        }

        OnCaptionSystemStarted?.Invoke();
        Debug.Log("Caption system started automatically with audio");
    }

    private void StopCaptionSystem()
    {
        captionSystemActive = false;
        currentCaptionIndex = -1;

        if (captionCanvas != null)
        {
            // Notify the UI behavior before hiding/destroying
            IUIBehavior behavior = captionCanvas.GetComponent<IUIBehavior>();
            if (behavior != null)
            {
                behavior.OnUIHidden();
            }

            if (destroyOnAudioStop)
            {
                DestroyImmediate(captionCanvas.gameObject);
                captionCanvas = null;
                speakerText = null;
                captionText = null;
            }
            else
            {
                captionCanvas.gameObject.SetActive(false);
            }
        }

        OnCaptionSystemStopped?.Invoke();
        Debug.Log("Caption system stopped automatically with audio");
    }

    private void InstantiateCaptionCanvas()
    {
        if (captionCanvasPrefab == null)
        {
            Debug.LogError("Caption Canvas Prefab is not assigned!");
            return;
        }

        // For world-space canvases with behaviors like SmoothFollow, don't parent to camera
        // Instead, instantiate in world space and let the behavior handle positioning
        Transform parent = parentTransform;

        // If parentTransform is explicitly set, use it, otherwise instantiate without parent
        GameObject canvasObject = parent != null ? Instantiate(captionCanvasPrefab, parent) : Instantiate(captionCanvasPrefab);
        captionCanvas = canvasObject.GetComponent<Canvas>();

        if (captionCanvas == null)
        {
            Debug.LogError("Instantiated prefab doesn't have a Canvas component!");
            DestroyImmediate(canvasObject);
            return;
        }

        // Setup UI behavior components (like SmoothFollowBehavior)
        SetupUIBehavior();

        // Find UI components in the instantiated canvas
        FindUIComponents();

        // Initialize as inactive
        captionCanvas.gameObject.SetActive(false);

        Debug.Log($"Caption canvas instantiated: {captionCanvas.name}");
    }

    private void SetupUIBehavior()
    {
        if (captionCanvas == null) return;

        IUIBehavior behavior = captionCanvas.GetComponent<IUIBehavior>();
        if (behavior != null)
        {
            // If it's a SmoothFollowBehavior, ensure it has proper camera reference
            if (behavior is SmoothFollowBehavior smoothFollow)
            {
                // Use override if provided, otherwise try to find main camera
                Transform targetTransform = targetTransformOverride;

                if (targetTransform == null && smoothFollow.targetTransform == null)
                {
                    // Auto-assign main camera if not set
                    Camera mainCamera = Camera.main;
                    if (mainCamera != null)
                    {
                        targetTransform = mainCamera.transform;
                        Debug.Log($"Auto-assigned main camera to SmoothFollowBehavior: {mainCamera.name}");
                    }
                    else
                    {
                        Debug.LogWarning("No main camera found for SmoothFollowBehavior!");
                    }
                }

                if (targetTransform != null)
                {
                    smoothFollow.targetTransform = targetTransform;
                }
            }
        }
    }

    private void FindUIComponents()
    {
        if (captionCanvas == null) return;

        // Try to find TextMeshPro components in the canvas
        TextMeshProUGUI[] textComponents = captionCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);

        // Look for components by name or tag, or use order-based assignment
        foreach (var textComp in textComponents)
        {
            string objName = textComp.name.ToLower();

            if (objName.Contains("speaker") && speakerText == null)
            {
                speakerText = textComp;
            }
            else if ((objName.Contains("caption") || objName.Contains("text")) && captionText == null)
            {
                captionText = textComp;
            }
        }

        // Fallback: if we have exactly 2 text components, assume first is speaker, second is caption
        if (speakerText == null && captionText == null && textComponents.Length >= 2)
        {
            speakerText = textComponents[0];
            captionText = textComponents[1];
        }
        else if (speakerText == null && captionText == null && textComponents.Length == 1)
        {
            // Only one text component, use it for captions
            captionText = textComponents[0];
        }

        if (captionText == null)
        {
            Debug.LogWarning("Could not find caption text component in instantiated canvas!");
        }

        Debug.Log($"UI Components found - Speaker: {speakerText?.name}, Caption: {captionText?.name}");
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
        if (!showCaptions || captionCanvas == null) return;

        // Show canvas when there's an active caption
        if (!captionCanvas.gameObject.activeInHierarchy)
        {
            captionCanvas.gameObject.SetActive(true);

            // Notify the UI behavior that the UI is now active
            IUIBehavior behavior = captionCanvas.GetComponent<IUIBehavior>();
            if (behavior != null)
            {
                behavior.OnUIShown();
            }
        }

        // Update text components
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
        if (captionCanvas == null) return;

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
        if (captionCanvas.gameObject.activeInHierarchy)
        {
            IUIBehavior behavior = captionCanvas.GetComponent<IUIBehavior>();
            if (behavior != null)
            {
                behavior.OnUIHidden();
            }
            captionCanvas.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Clean up instantiated canvas when manager is destroyed
        if (captionCanvas != null)
        {
            DestroyImmediate(captionCanvas.gameObject);
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
    /// Setup captions with custom prefab and target transform for UI behavior
    /// </summary>
    public void SetupCaptions(AudioSource audioSource, TextAsset srtFile, GameObject customPrefab, Transform targetTransform = null)
    {
        if (customPrefab != null)
        {
            captionCanvasPrefab = customPrefab;
        }

        // Store target transform for UI behavior setup
        if (targetTransform != null)
        {
            this.targetTransformOverride = targetTransform;
        }

        SetupCaptions(audioSource, srtFile);
    }

    /// <summary>
    /// Set the target transform for UI behaviors (like SmoothFollow camera reference)
    /// </summary>
    public void SetTargetTransform(Transform target)
    {
        targetTransformOverride = target;

        // If canvas is already instantiated, update its behavior
        if (captionCanvas != null)
        {
            IUIBehavior behavior = captionCanvas.GetComponent<IUIBehavior>();
            if (behavior is SmoothFollowBehavior smoothFollow)
            {
                smoothFollow.targetTransform = target;
            }
        }
    }

    /// <summary>
    /// Clear current caption setup and destroy instantiated UI
    /// </summary>
    public void ClearCaptions()
    {
        if (captionSystemActive)
        {
            StopCaptionSystem();
        }

        // Force cleanup of instantiated canvas
        if (captionCanvas != null)
        {
            DestroyImmediate(captionCanvas.gameObject);
            captionCanvas = null;
            speakerText = null;
            captionText = null;
        }

        currentAudioSource = null;
        currentCaptions.Clear();
        currentCaptionIndex = -1;
        wasPlayingLastFrame = false;

        Debug.Log("Caption system cleared and UI destroyed");
    }

    /// <summary>
    /// Toggle caption visibility on/off
    /// </summary>
    public void ToggleCaptions()
    {
        showCaptions = !showCaptions;

        if (!showCaptions && captionSystemActive && captionCanvas != null)
        {
            HideCurrentCaption();
        }

        Debug.Log($"Captions {(showCaptions ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// Set the caption canvas prefab to use
    /// </summary>
    public void SetCaptionPrefab(GameObject prefab)
    {
        captionCanvasPrefab = prefab;
    }

    /// <summary>
    /// Set the parent transform for instantiated UI
    /// </summary>
    public void SetParentTransform(Transform parent)
    {
        parentTransform = parent;
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

    /// <summary>
    /// Get the currently instantiated canvas (if any)
    /// </summary>
    public Canvas GetCaptionCanvas()
    {
        return captionCanvas;
    }

    #endregion
}