//using UnityEngine;
//using System.Collections.Generic;
//using System.Collections;
//using TMPro;

//public class GlobalCaptionManager : MonoBehaviour
//{
//    [Header("Database Configuration")]
//    [SerializeField] private CaptionDatabase captionDatabase;

//    [Header("Auto-Discovery Settings")]
//    [SerializeField] private bool autoDiscoverAudioSources = true;
//    [SerializeField] private float discoveryUpdateRate = 1.5f;
//    [SerializeField] private bool monitorNewAudioSources = true;

//    [Header("Caption Control")]
//    [SerializeField] private bool captionsEnabled = true;

//    [Header("Debug Settings")]
//    [SerializeField] private bool enableDebugLogs = true;


//    // Active caption sessions - now managed directly
//    private Dictionary<AudioSource, CaptionSession> activeSessions = new Dictionary<AudioSource, CaptionSession>();
//    private Dictionary<AudioSource, AudioClip> lastKnownClips = new Dictionary<AudioSource, AudioClip>();

//    // Caption session data structure
//    private class CaptionSession
//    {
//        public AudioSource audioSource;
//        public CaptionDatabase.CaptionEntry captionEntry;
//        public Canvas captionCanvas;
//        public TextMeshProUGUI speakerText;
//        public TextMeshProUGUI captionText;
//        public List<CaptionEntry> captions;
//        public int currentCaptionIndex = -1;
//        public bool wasPlayingLastFrame = false;
//        public bool isActive = false;
//    }

//    // Registered audio sources (for manual control)
//    private HashSet<CaptionEnabledAudioSource> registeredSources = new HashSet<CaptionEnabledAudioSource>();

//    // Discovery coroutine
//    private Coroutine discoveryCoroutine;

//    #region Singleton Setup
//    public static GlobalCaptionManager Instance { get; private set; }

//    private void Awake()
//    {
//        if (Instance == null)
//        {
//            Instance = this;
//            DontDestroyOnLoad(gameObject);
//        }
//        else
//        {
//            Destroy(gameObject);
//            return;
//        }
//    }
//    #endregion

//    private void Start()
//    {
//        if (captionDatabase == null)
//        {
//            Debug.LogError("GlobalCaptionManager: No caption database assigned!");
//            return;
//        }

//        Debug.Log($"[DEBUG] Database assigned: {captionDatabase.name} with {captionDatabase.GetAllAudioClips().Count} clips");

//        if (autoDiscoverAudioSources)
//        {
//            StartAutoDiscovery();
//        }

//        LogDebug("GlobalCaptionManager started successfully");
//    }

//    private void Update()
//    {
//        if (autoDiscoverAudioSources && discoveryCoroutine == null)
//        {
//            StartAutoDiscovery();
//        }

//        // Update active caption sessions
//        UpdateActiveCaptionSessions();
//    }

//    private void UpdateActiveCaptionSessions()
//    {
//        var sessionsToUpdate = new List<CaptionSession>(activeSessions.Values);

//        foreach (var session in sessionsToUpdate)
//        {
//            if (session.audioSource == null || session.captionCanvas == null)
//            {
//                // Clean up invalid session
//                CleanupSession(session);
//                continue;
//            }

//            UpdateCaptionSession(session);
//        }
//    }

//    private void UpdateCaptionSession(CaptionSession session)
//    {
//        bool isPlayingNow = session.audioSource.isPlaying;
//        bool wasPlayingLastFrame = session.wasPlayingLastFrame;

//        // Audio just started playing
//        if (isPlayingNow && !wasPlayingLastFrame)
//        {
//            session.isActive = true;
//            session.currentCaptionIndex = -1;
//        }
//        // Audio just stopped playing
//        else if (!isPlayingNow && wasPlayingLastFrame && session.isActive)
//        {
//            session.isActive = false;
//            HideCurrentCaption(session);
//        }

//        session.wasPlayingLastFrame = isPlayingNow;

//        // Update captions if actively playing
//        if (session.isActive && isPlayingNow)
//        {
//            UpdateSessionCaptions(session);
//        }

//        // Update UI behavior
//        if (session.isActive && session.captionCanvas.gameObject.activeInHierarchy)
//        {
//            IUIBehavior behavior = session.captionCanvas.GetComponent<IUIBehavior>();
//            if (behavior != null)
//            {
//                behavior.UpdateBehavior();
//            }
//        }
//    }

//    private void UpdateSessionCaptions(CaptionSession session)
//    {
//        if (session.captions.Count == 0) return;

//        float currentTime = session.audioSource.time;

//        // Find the caption that should be active now
//        CaptionEntry activeCaption = null;
//        int activeCaptionIndex = -1;

//        for (int i = 0; i < session.captions.Count; i++)
//        {
//            if (session.captions[i].IsActiveAtTime(currentTime))
//            {
//                activeCaption = session.captions[i];
//                activeCaptionIndex = i;
//                break;
//            }
//        }

//        // Update UI if caption changed
//        if (activeCaptionIndex != session.currentCaptionIndex)
//        {
//            session.currentCaptionIndex = activeCaptionIndex;

//            if (activeCaption != null)
//            {
//                ShowCaption(session, activeCaption);
//            }
//            else
//            {
//                HideCurrentCaption(session);
//            }
//        }
//    }

//    private void ShowCaption(CaptionSession session, CaptionEntry caption)
//    {
//        if (!captionsEnabled) return;

//        LogDebug($"Showing caption: [{caption.speaker}] {caption.text}");

//        // Show canvas when there's an active caption
//        if (!session.captionCanvas.gameObject.activeInHierarchy)
//        {
//            session.captionCanvas.gameObject.SetActive(true);

//            // Notify the UI behavior that the UI is now active
//            IUIBehavior behavior = session.captionCanvas.GetComponent<IUIBehavior>();
//            if (behavior != null)
//            {
//                behavior.OnUIShown();
//            }
//        }

//        // Update text components
//        if (session.speakerText != null)
//        {
//            session.speakerText.text = caption.speaker;
//        }

//        if (session.captionText != null)
//        {
//            session.captionText.text = caption.text;
//        }
//    }

//    private void HideCurrentCaption(CaptionSession session)
//    {
//        // Clear text
//        if (session.speakerText != null)
//        {
//            session.speakerText.text = "";
//        }

//        if (session.captionText != null)
//        {
//            session.captionText.text = "";
//        }

//        // Hide canvas during gaps between captions
//        if (session.captionCanvas.gameObject.activeInHierarchy)
//        {
//            IUIBehavior behavior = session.captionCanvas.GetComponent<IUIBehavior>();
//            if (behavior != null)
//            {
//                behavior.OnUIHidden();
//            }
//            session.captionCanvas.gameObject.SetActive(false);
//        }
//    }

//    private void FindUIComponents(CaptionSession session)
//    {
//        if (session.captionCanvas == null) return;

//        // Try to find TextMeshPro components in the canvas
//        TextMeshProUGUI[] textComponents = session.captionCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);

//        // Look for components by name or tag, or use order-based assignment
//        foreach (var textComp in textComponents)
//        {
//            string objName = textComp.name.ToLower();

//            if (objName.Contains("speaker") && session.speakerText == null)
//            {
//                session.speakerText = textComp;
//            }
//            else if ((objName.Contains("caption") || objName.Contains("text")) && session.captionText == null)
//            {
//                session.captionText = textComp;
//            }
//        }

//        // Fallback: if we have exactly 2 text components, assume first is speaker, second is caption
//        if (session.speakerText == null && session.captionText == null && textComponents.Length >= 2)
//        {
//            session.speakerText = textComponents[0];
//            session.captionText = textComponents[1];
//        }
//        else if (session.speakerText == null && session.captionText == null && textComponents.Length == 1)
//        {
//            // Only one text component, use it for captions
//            session.captionText = textComponents[0];
//        }

//        if (session.captionText == null)
//        {
//            Debug.LogWarning("Could not find caption text component in instantiated canvas!");
//        }

//        LogDebug($"UI Components found - Speaker: {session.speakerText?.name}, Caption: {session.captionText?.name}");
//    }

//    private void OnDestroy()
//    {
//        StopAutoDiscovery();
//        ClearAllSessions();
//    }

//    #region Auto-Discovery System

//    private void StartAutoDiscovery()
//    {
//        StopAutoDiscovery();
//        discoveryCoroutine = StartCoroutine(AudioDiscoveryLoop());
//        LogDebug("Auto-discovery started");
//    }

//    private void StopAutoDiscovery()
//    {
//        if (discoveryCoroutine != null)
//        {
//            StopCoroutine(discoveryCoroutine);
//            discoveryCoroutine = null;
//        }
//    }

//    private IEnumerator AudioDiscoveryLoop()
//    {
//        while (true)
//        {
//            yield return new WaitForSeconds(discoveryUpdateRate);

//            if (monitorNewAudioSources)
//            {
//                DiscoverNewAudioSources();
//            }

//            UpdateActiveAudioSources();
//        }
//    }

//    private void DiscoverNewAudioSources()
//    {
//        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();

//        foreach (AudioSource audioSource in allAudioSources)
//        {
//            if (audioSource != null && !lastKnownClips.ContainsKey(audioSource))
//            {
//                lastKnownClips[audioSource] = null;
//                LogDebug($"Discovered new audio source: {audioSource.name}");
//            }
//        }

//        // Clean up destroyed audio sources
//        var sourcesToRemove = new List<AudioSource>();
//        foreach (var audioSource in lastKnownClips.Keys)
//        {
//            if (audioSource == null)
//            {
//                sourcesToRemove.Add(audioSource);
//            }
//        }

//        foreach (var source in sourcesToRemove)
//        {
//            lastKnownClips.Remove(source);
//            if (activeSessions.ContainsKey(source))
//            {
//                StopCaptionSession(source);
//            }
//        }
//    }

//    private void UpdateActiveAudioSources()
//    {
//        var currentSources = new List<AudioSource>(lastKnownClips.Keys);

//        foreach (AudioSource audioSource in currentSources)
//        {
//            if (audioSource == null) continue;

//            AudioClip currentClip = audioSource.clip;
//            AudioClip lastClip = lastKnownClips[audioSource];

//            // Check if clip changed
//            if (currentClip != lastClip)
//            {
//                lastKnownClips[audioSource] = currentClip;
//                OnAudioClipChanged(audioSource, currentClip, lastClip);
//            }

//            // Check if audio stopped
//            if (!audioSource.isPlaying && activeSessions.ContainsKey(audioSource))
//            {
//                var session = activeSessions[audioSource];
//                if (session != null && !session.isActive)
//                {
//                    // Audio stopped, but caption session might not have detected it yet
//                    // Let the caption session update handle this naturally
//                }
//            }
//        }
//    }

//    private void OnAudioClipChanged(AudioSource audioSource, AudioClip newClip, AudioClip oldClip)
//    {
//        LogDebug($"Audio clip changed on {audioSource.name}: {oldClip?.name} -> {newClip?.name}");

//        // Stop previous caption session if exists
//        if (activeSessions.ContainsKey(audioSource))
//        {
//            StopCaptionSession(audioSource);
//        }

//        // Start new caption session if new clip has captions
//        if (newClip != null)
//        {
//            bool hasCaption = captionDatabase.HasCaptionForClip(newClip);
//            LogDebug($"Clip {newClip.name} has captions: {hasCaption}");

//            if (hasCaption)
//            {
//                StartCaptionSession(audioSource, newClip);
//            }
//        }
//    }

//    #endregion

//    #region Caption Session Management

//    private void StartCaptionSession(AudioSource audioSource, AudioClip audioClip)
//    {
//        var captionEntry = captionDatabase.GetEntryForClip(audioClip);
//        if (captionEntry == null)
//        {
//            LogDebug($"No caption entry found for clip: {audioClip.name}");
//            return;
//        }

//        LogDebug($"Starting caption session for {audioSource.name} with clip {audioClip.name}");

//        // Create a new caption session directly managed by this class
//        var session = new CaptionSession();
//        session.audioSource = audioSource;
//        session.captionEntry = captionEntry;
//        session.captions = SRTParser.ParseSRT(captionEntry.srtFile.text);
//        session.currentCaptionIndex = -1;
//        session.wasPlayingLastFrame = false;

//        LogDebug($"Parsed {session.captions.Count} captions from SRT file");

//        // Instantiate caption canvas
//        session.captionCanvas = InstantiateCaptionCanvas(captionEntry, audioSource);

//        if (session.captionCanvas != null)
//        {
//            // Find UI components
//            FindUIComponents(session);
//            session.captionCanvas.gameObject.SetActive(false);
//            LogDebug($"Caption canvas created and configured for {audioSource.name}");
//        }
//        else
//        {
//            LogDebug($"Failed to create caption canvas for {audioSource.name}");
//            return;
//        }

//        // Store session
//        activeSessions[audioSource] = session;

//        LogDebug($"Caption session successfully started for {audioSource.name}");
//    }

//    private Canvas InstantiateCaptionCanvas(CaptionDatabase.CaptionEntry entry, AudioSource audioSource)
//    {
//        GameObject prefab = entry.captionPrefab ?? captionDatabase.defaultCaptionPrefab;
//        if (prefab == null)
//        {
//            Debug.LogError("No caption prefab available!");
//            return null;
//        }

//        // Instantiate canvas
//        GameObject canvasObject = Instantiate(prefab);
//        canvasObject.name = $"CaptionCanvas_{audioSource.name}";

//        Canvas canvas = canvasObject.GetComponent<Canvas>();
//        if (canvas == null)
//        {
//            Debug.LogError("Instantiated prefab doesn't have a Canvas component!");
//            DestroyImmediate(canvasObject);
//            return null;
//        }

//        CharCaptionBehavior charCaption = canvas.GetComponent<CharCaptionBehavior>();
//        if (charCaption != null)
//        {
//            charCaption.SetCharacterTransform(audioSource.transform);
//        }

//        return canvas;
//    }

//    private void StopCaptionSession(AudioSource audioSource)
//    {
//        if (activeSessions.TryGetValue(audioSource, out CaptionSession session))
//        {
//            activeSessions.Remove(audioSource);
//            CleanupSession(session);
//            LogDebug($"Stopped caption session for {audioSource?.name}");
//        }
//    }

//    private void CleanupSession(CaptionSession session)
//    {
//        if (session.captionCanvas != null)
//        {
//            // Notify the UI behavior before destroying
//            IUIBehavior behavior = session.captionCanvas.GetComponent<IUIBehavior>();
//            if (behavior != null)
//            {
//                behavior.OnUIHidden();
//            }

//            DestroyImmediate(session.captionCanvas.gameObject);
//        }

//        // Remove from active sessions if still there
//        if (session.audioSource != null && activeSessions.ContainsKey(session.audioSource))
//        {
//            activeSessions.Remove(session.audioSource);
//        }
//    }

//    private void ClearAllSessions()
//    {
//        var sessionsToStop = new List<CaptionSession>(activeSessions.Values);
//        foreach (var session in sessionsToStop)
//        {
//            CleanupSession(session);
//        }

//        activeSessions.Clear();
//        lastKnownClips.Clear();
//    }

//    #endregion

//    #region Public API

//    /// <summary>
//    /// Enable or disable all captions
//    /// </summary>
//    public void SetCaptionsEnabled(bool enabled)
//    {
//        captionsEnabled = enabled;

//        // Hide all active captions when disabled
//        if (!enabled)
//        {
//            foreach (var session in activeSessions.Values)
//            {
//                if (session.captionCanvas != null && session.captionCanvas.gameObject.activeInHierarchy)
//                {
//                    session.captionCanvas.gameObject.SetActive(false);
//                }
//            }
//        }
//    }

//    /// <summary>
//    /// Toggle captions on/off
//    /// </summary>
//    public void ToggleCaptions()
//    {
//        SetCaptionsEnabled(!captionsEnabled);
//    }

//    /// <summary>
//    /// Set the caption database to use
//    /// </summary>
//    public void SetCaptionDatabase(CaptionDatabase database)
//    {
//        if (database == null)
//        {
//            Debug.LogError("Cannot set null caption database");
//            return;
//        }

//        captionDatabase = database;
//        LogDebug($"Caption database updated: {database.name}");
//    }

//    /// <summary>
//    /// Manually start captions for an audio source (overrides auto-discovery)
//    /// </summary>
//    public void StartCaptionsManually(AudioSource audioSource, AudioClip audioClip, TextAsset srtFile, GameObject prefab = null)
//    {
//        if (audioSource == null || audioClip == null || srtFile == null)
//        {
//            Debug.LogError("Cannot start captions with null parameters");
//            return;
//        }

//        // Stop any existing session
//        if (activeSessions.ContainsKey(audioSource))
//        {
//            StopCaptionSession(audioSource);
//        }

//        // Create temporary entry for manual setup
//        var tempEntry = new CaptionDatabase.CaptionEntry
//        {
//            audioClip = audioClip,
//            srtFile = srtFile,
//            captionPrefab = prefab ?? captionDatabase.defaultCaptionPrefab
//        };

//        // Create manual session
//        var session = new CaptionSession();
//        session.audioSource = audioSource;
//        session.captionEntry = tempEntry;
//        session.captions = SRTParser.ParseSRT(srtFile.text);
//        session.currentCaptionIndex = -1;
//        session.wasPlayingLastFrame = false;

//        // Instantiate caption canvas
//        session.captionCanvas = InstantiateCaptionCanvas(tempEntry, audioSource);

//        if (session.captionCanvas != null)
//        {
//            FindUIComponents(session);
//            session.captionCanvas.gameObject.SetActive(false);
//        }

//        // Store session
//        activeSessions[audioSource] = session;

//        LogDebug($"Started manual caption session for {audioSource.name}");
//    }

//    /// <summary>
//    /// Stop captions for a specific audio source
//    /// </summary>
//    public void StopCaptionsManually(AudioSource audioSource)
//    {
//        StopCaptionSession(audioSource);
//    }

//    /// <summary>
//    /// Check if an audio source has active captions
//    /// </summary>
//    public bool HasActiveCaptions(AudioSource audioSource)
//    {
//        return activeSessions.ContainsKey(audioSource) && activeSessions[audioSource].captionCanvas != null;
//    }

//    /// <summary>
//    /// Get the caption canvas for a specific audio source
//    /// </summary>
//    public Canvas GetCaptionCanvas(AudioSource audioSource)
//    {
//        if (activeSessions.TryGetValue(audioSource, out CaptionSession session))
//        {
//            return session.captionCanvas;
//        }
//        return null;
//    }

//    /// <summary>
//    /// Register an audio source for explicit monitoring
//    /// </summary>
//    public void RegisterAudioSource(CaptionEnabledAudioSource captionSource)
//    {
//        registeredSources.Add(captionSource);
//        LogDebug($"Registered audio source: {captionSource.name}");
//    }

//    /// <summary>
//    /// Unregister an audio source
//    /// </summary>
//    public void UnregisterAudioSource(CaptionEnabledAudioSource captionSource)
//    {
//        registeredSources.Remove(captionSource);

//        // Stop any active session for this source
//        var audioSource = captionSource.GetComponent<AudioSource>();
//        if (audioSource != null)
//        {
//            StopCaptionSession(audioSource);
//        }
//    }

//    /// <summary>
//    /// Enable or disable auto-discovery at runtime
//    /// </summary>
//    public void SetAutoDiscovery(bool enabled)
//    {
//        autoDiscoverAudioSources = enabled;

//        if (enabled)
//        {
//            StartAutoDiscovery();
//        }
//        else
//        {
//            StopAutoDiscovery();
//        }
//    }

//    /// <summary>
//    /// Get the current caption database
//    /// </summary>
//    public CaptionDatabase GetCaptionDatabase()
//    {
//        return captionDatabase;
//    }

//    /// <summary>
//    /// Get count of active caption sessions
//    /// </summary>
//    public int GetActiveSessionCount()
//    {
//        return activeSessions.Count;
//    }

//    #endregion

//    #region Utility Methods

//    private void LogDebug(string message)
//    {
//        if (enableDebugLogs)
//        {
//            Debug.Log($"[GlobalCaptionManager] {message}");
//        }
//    }

//    #endregion

//#if UNITY_EDITOR
//    [ContextMenu("Force Refresh Audio Sources")]
//    private void ForceRefreshAudioSources()
//    {
//        lastKnownClips.Clear();
//        DiscoverNewAudioSources();
//    }

//    [ContextMenu("Clear All Sessions")]
//    private void EditorClearAllSessions()
//    {
//        ClearAllSessions();
//    }
//#endif
//}

using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;
using System.Collections;
using TMPro;

/// <summary>
/// Enhanced GlobalCaptionManager that supports both AudioSource and VideoPlayer captions
/// </summary>
public class GlobalCaptionManager : MonoBehaviour
{
    [Header("Database Configuration")]
    [SerializeField] private CaptionDatabase captionDatabase;

    [Header("Auto-Discovery Settings")]
    [SerializeField] private bool autoDiscoverAudioSources = true;
    [SerializeField] private bool autoDiscoverVideoPlayers = true;
    [SerializeField] private float discoveryUpdateRate = 1.5f;
    [SerializeField] private bool monitorNewSources = true;

    [Header("Caption Control")]
    [SerializeField] private bool captionsEnabled = true;

    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;

    // Active caption sessions - unified for both audio and video
    private Dictionary<object, CaptionSession> activeSessions = new Dictionary<object, CaptionSession>();
    private Dictionary<AudioSource, AudioClip> lastKnownAudioClips = new Dictionary<AudioSource, AudioClip>();
    private Dictionary<VideoPlayer, VideoClip> lastKnownVideoClips = new Dictionary<VideoPlayer, VideoClip>();

    // Caption session data structure
    private class CaptionSession
    {
        public object mediaSource; // AudioSource or VideoPlayer
        public MediaSourceType sourceType;
        public CaptionDatabase.CaptionEntry captionEntry;
        public Canvas captionCanvas;
        public TextMeshProUGUI speakerText;
        public TextMeshProUGUI captionText;
        public List<CaptionEntry> captions;
        public int currentCaptionIndex = -1;
        public bool wasPlayingLastFrame = false;
        public bool isActive = false;
    }

    private enum MediaSourceType
    {
        Audio,
        Video
    }

    // Registered sources (for manual control)
    private HashSet<CaptionEnabledAudioSource> registeredAudioSources = new HashSet<CaptionEnabledAudioSource>();
    private HashSet<CaptionEnabledVideoPlayer> registeredVideoPlayers = new HashSet<CaptionEnabledVideoPlayer>();

    // Discovery coroutine
    private Coroutine discoveryCoroutine;

    #region Singleton Setup
    public static GlobalCaptionManager Instance { get; private set; }

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
        if (captionDatabase == null)
        {
            Debug.LogError("GlobalCaptionManager: No caption database assigned!");
            return;
        }

        LogDebug($"Database assigned: {captionDatabase.name} with {captionDatabase.GetAllAudioClips().Count} audio clips and {captionDatabase.GetAllVideoClips().Count} video clips");

        if (autoDiscoverAudioSources || autoDiscoverVideoPlayers)
        {
            StartAutoDiscovery();
        }

        LogDebug("GlobalCaptionManager started successfully");
    }

    private void Update()
    {
        if ((autoDiscoverAudioSources || autoDiscoverVideoPlayers) && discoveryCoroutine == null)
        {
            StartAutoDiscovery();
        }

        // Update active caption sessions
        UpdateActiveCaptionSessions();
    }

    private void UpdateActiveCaptionSessions()
    {
        var sessionsToUpdate = new List<CaptionSession>(activeSessions.Values);

        foreach (var session in sessionsToUpdate)
        {
            if (session.mediaSource == null || session.captionCanvas == null)
            {
                // Clean up invalid session
                CleanupSession(session);
                continue;
            }

            UpdateCaptionSession(session);
        }
    }

    private void UpdateCaptionSession(CaptionSession session)
    {
        bool isPlayingNow = IsMediaPlaying(session.mediaSource, session.sourceType);
        bool wasPlayingLastFrame = session.wasPlayingLastFrame;

        // Media just started playing
        if (isPlayingNow && !wasPlayingLastFrame)
        {
            session.isActive = true;
            session.currentCaptionIndex = -1;
        }
        // Media just stopped playing
        else if (!isPlayingNow && wasPlayingLastFrame && session.isActive)
        {
            session.isActive = false;
            HideCurrentCaption(session);
        }

        session.wasPlayingLastFrame = isPlayingNow;

        // Update captions if actively playing
        if (session.isActive && isPlayingNow)
        {
            UpdateSessionCaptions(session);
        }

        // Update UI behavior
        if (session.isActive && session.captionCanvas.gameObject.activeInHierarchy)
        {
            IUIBehavior behavior = session.captionCanvas.GetComponent<IUIBehavior>();
            if (behavior != null)
            {
                behavior.UpdateBehavior();
            }
        }
    }

    private void UpdateSessionCaptions(CaptionSession session)
    {
        if (session.captions.Count == 0) return;

        float currentTime = GetMediaTime(session.mediaSource, session.sourceType);

        // Find the caption that should be active now
        CaptionEntry activeCaption = null;
        int activeCaptionIndex = -1;

        for (int i = 0; i < session.captions.Count; i++)
        {
            if (session.captions[i].IsActiveAtTime(currentTime))
            {
                activeCaption = session.captions[i];
                activeCaptionIndex = i;
                break;
            }
        }

        // Update UI if caption changed
        if (activeCaptionIndex != session.currentCaptionIndex)
        {
            session.currentCaptionIndex = activeCaptionIndex;

            if (activeCaption != null)
            {
                ShowCaption(session, activeCaption);
            }
            else
            {
                HideCurrentCaption(session);
            }
        }
    }

    private void ShowCaption(CaptionSession session, CaptionEntry caption)
    {
        if (!captionsEnabled) return;

        LogDebug($"Showing caption: [{caption.speaker}] {caption.text}");

        // Show canvas when there's an active caption
        if (!session.captionCanvas.gameObject.activeInHierarchy)
        {
            session.captionCanvas.gameObject.SetActive(true);

            // Notify the UI behavior that the UI is now active
            IUIBehavior behavior = session.captionCanvas.GetComponent<IUIBehavior>();
            if (behavior != null)
            {
                behavior.OnUIShown();
            }
        }

        // Update text components
        if (session.speakerText != null)
        {
            session.speakerText.text = caption.speaker;
        }

        if (session.captionText != null)
        {
            session.captionText.text = caption.text;
        }
    }

    private void HideCurrentCaption(CaptionSession session)
    {
        // Clear text
        if (session.speakerText != null)
        {
            session.speakerText.text = "";
        }

        if (session.captionText != null)
        {
            session.captionText.text = "";
        }

        // Hide canvas during gaps between captions
        if (session.captionCanvas.gameObject.activeInHierarchy)
        {
            IUIBehavior behavior = session.captionCanvas.GetComponent<IUIBehavior>();
            if (behavior != null)
            {
                behavior.OnUIHidden();
            }
            session.captionCanvas.gameObject.SetActive(false);
        }
    }

    private void FindUIComponents(CaptionSession session)
    {
        if (session.captionCanvas == null) return;

        // Try to find TextMeshPro components in the canvas
        TextMeshProUGUI[] textComponents = session.captionCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);

        // Look for components by name or tag, or use order-based assignment
        foreach (var textComp in textComponents)
        {
            string objName = textComp.name.ToLower();

            if (objName.Contains("speaker") && session.speakerText == null)
            {
                session.speakerText = textComp;
            }
            else if ((objName.Contains("caption") || objName.Contains("text")) && session.captionText == null)
            {
                session.captionText = textComp;
            }
        }

        // Fallback: if we have exactly 2 text components, assume first is speaker, second is caption
        if (session.speakerText == null && session.captionText == null && textComponents.Length >= 2)
        {
            session.speakerText = textComponents[0];
            session.captionText = textComponents[1];
        }
        else if (session.speakerText == null && session.captionText == null && textComponents.Length == 1)
        {
            // Only one text component, use it for captions
            session.captionText = textComponents[0];
        }

        if (session.captionText == null)
        {
            Debug.LogWarning("Could not find caption text component in instantiated canvas!");
        }

        LogDebug($"UI Components found - Speaker: {session.speakerText?.name}, Caption: {session.captionText?.name}");
    }

    private void OnDestroy()
    {
        StopAutoDiscovery();
        ClearAllSessions();
    }

    #region Auto-Discovery System

    private void StartAutoDiscovery()
    {
        StopAutoDiscovery();
        discoveryCoroutine = StartCoroutine(MediaDiscoveryLoop());
        LogDebug("Auto-discovery started");
    }

    private void StopAutoDiscovery()
    {
        if (discoveryCoroutine != null)
        {
            StopCoroutine(discoveryCoroutine);
            discoveryCoroutine = null;
        }
    }

    private IEnumerator MediaDiscoveryLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(discoveryUpdateRate);

            if (monitorNewSources)
            {
                if (autoDiscoverAudioSources)
                {
                    DiscoverNewAudioSources();
                }

                if (autoDiscoverVideoPlayers)
                {
                    DiscoverNewVideoPlayers();
                }
            }

            UpdateActiveMediaSources();
        }
    }

    private void DiscoverNewAudioSources()
    {
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();

        foreach (AudioSource audioSource in allAudioSources)
        {
            if (audioSource != null && !lastKnownAudioClips.ContainsKey(audioSource))
            {
                lastKnownAudioClips[audioSource] = null;
                LogDebug($"Discovered new audio source: {audioSource.name}");
            }
        }

        // Clean up destroyed audio sources
        CleanupDestroyedSources(lastKnownAudioClips);
    }

    private void DiscoverNewVideoPlayers()
    {
        VideoPlayer[] allVideoPlayers = FindObjectsOfType<VideoPlayer>();

        foreach (VideoPlayer videoPlayer in allVideoPlayers)
        {
            if (videoPlayer != null && !lastKnownVideoClips.ContainsKey(videoPlayer))
            {
                lastKnownVideoClips[videoPlayer] = null;
                LogDebug($"Discovered new video player: {videoPlayer.name}");
            }
        }

        // Clean up destroyed video players
        CleanupDestroyedSources(lastKnownVideoClips);
    }

    private void CleanupDestroyedSources<TSource, TClip>(Dictionary<TSource, TClip> sourceDict)
        where TSource : UnityEngine.Object
        where TClip : UnityEngine.Object
    {
        var sourcesToRemove = new List<TSource>();
        foreach (var source in sourceDict.Keys)
        {
            if (source == null)
            {
                sourcesToRemove.Add(source);
            }
        }

        foreach (var source in sourcesToRemove)
        {
            sourceDict.Remove(source);
            if (activeSessions.ContainsKey(source))
            {
                StopCaptionSession(source);
            }
        }
    }

    private void UpdateActiveMediaSources()
    {
        // Update audio sources
        UpdateAudioSources();

        // Update video players
        UpdateVideoPlayers();
    }

    private void UpdateAudioSources()
    {
        var currentSources = new List<AudioSource>(lastKnownAudioClips.Keys);

        foreach (AudioSource audioSource in currentSources)
        {
            if (audioSource == null) continue;

            AudioClip currentClip = audioSource.clip;
            AudioClip lastClip = lastKnownAudioClips[audioSource];

            // Check if clip changed
            if (currentClip != lastClip)
            {
                lastKnownAudioClips[audioSource] = currentClip;
                OnAudioClipChanged(audioSource, currentClip, lastClip);
            }
        }
    }

    private void UpdateVideoPlayers()
    {
        var currentPlayers = new List<VideoPlayer>(lastKnownVideoClips.Keys);

        foreach (VideoPlayer videoPlayer in currentPlayers)
        {
            if (videoPlayer == null) continue;

            VideoClip currentClip = videoPlayer.clip;
            VideoClip lastClip = lastKnownVideoClips[videoPlayer];

            // Check if clip changed
            if (currentClip != lastClip)
            {
                lastKnownVideoClips[videoPlayer] = currentClip;
                OnVideoClipChanged(videoPlayer, currentClip, lastClip);
            }
        }
    }

    private void OnAudioClipChanged(AudioSource audioSource, AudioClip newClip, AudioClip oldClip)
    {
        LogDebug($"Audio clip changed on {audioSource.name}: {oldClip?.name} -> {newClip?.name}");

        // Stop previous caption session if exists
        if (activeSessions.ContainsKey(audioSource))
        {
            StopCaptionSession(audioSource);
        }

        // Start new caption session if new clip has captions
        if (newClip != null && captionDatabase.HasCaptionForClip(newClip))
        {
            StartAudioCaptionSession(audioSource, newClip);
        }
    }

    private void OnVideoClipChanged(VideoPlayer videoPlayer, VideoClip newClip, VideoClip oldClip)
    {
        LogDebug($"Video clip changed on {videoPlayer.name}: {oldClip?.name} -> {newClip?.name}");

        // Stop previous caption session if exists
        if (activeSessions.ContainsKey(videoPlayer))
        {
            StopCaptionSession(videoPlayer);
        }

        // Start new caption session if new clip has captions
        if (newClip != null && captionDatabase.HasCaptionForVideo(newClip))
        {
            StartVideoCaptionSession(videoPlayer, newClip);
        }
    }

    #endregion

    #region Caption Session Management

    private void StartAudioCaptionSession(AudioSource audioSource, AudioClip audioClip)
    {
        var captionEntry = captionDatabase.GetEntryForClip(audioClip);
        if (captionEntry == null)
        {
            LogDebug($"No caption entry found for audio clip: {audioClip.name}");
            return;
        }

        LogDebug($"Starting audio caption session for {audioSource.name} with clip {audioClip.name}");

        // Create a new caption session
        var session = new CaptionSession();
        session.mediaSource = audioSource;
        session.sourceType = MediaSourceType.Audio;
        session.captionEntry = captionEntry;
        session.captions = SRTParser.ParseSRT(captionEntry.srtFile.text);
        session.currentCaptionIndex = -1;
        session.wasPlayingLastFrame = false;

        LogDebug($"Parsed {session.captions.Count} captions from SRT file");

        // Instantiate caption canvas
        session.captionCanvas = InstantiateCaptionCanvas(captionEntry, audioSource.transform, audioSource.name);

        if (session.captionCanvas != null)
        {
            FindUIComponents(session);
            session.captionCanvas.gameObject.SetActive(false);
            LogDebug($"Caption canvas created and configured for audio source {audioSource.name}");
        }
        else
        {
            LogDebug($"Failed to create caption canvas for audio source {audioSource.name}");
            return;
        }

        // Store session
        activeSessions[audioSource] = session;

        LogDebug($"Audio caption session successfully started for {audioSource.name}");
    }

    private void StartVideoCaptionSession(VideoPlayer videoPlayer, VideoClip videoClip)
    {
        var captionEntry = captionDatabase.GetEntryForVideo(videoClip);
        if (captionEntry == null)
        {
            LogDebug($"No caption entry found for video clip: {videoClip.name}");
            return;
        }

        LogDebug($"Starting video caption session for {videoPlayer.name} with clip {videoClip.name}");

        // Create a new caption session
        var session = new CaptionSession();
        session.mediaSource = videoPlayer;
        session.sourceType = MediaSourceType.Video;
        session.captionEntry = captionEntry;
        session.captions = SRTParser.ParseSRT(captionEntry.srtFile.text);
        session.currentCaptionIndex = -1;
        session.wasPlayingLastFrame = false;

        LogDebug($"Parsed {session.captions.Count} captions from SRT file");

        // Instantiate caption canvas
        session.captionCanvas = InstantiateCaptionCanvas(captionEntry, videoPlayer.transform, videoPlayer.name);

        if (session.captionCanvas != null)
        {
            FindUIComponents(session);
            session.captionCanvas.gameObject.SetActive(false);
            LogDebug($"Caption canvas created and configured for video player {videoPlayer.name}");
        }
        else
        {
            LogDebug($"Failed to create caption canvas for video player {videoPlayer.name}");
            return;
        }

        // Store session
        activeSessions[videoPlayer] = session;

        LogDebug($"Video caption session successfully started for {videoPlayer.name}");
    }

    private Canvas InstantiateCaptionCanvas(CaptionDatabase.CaptionEntry entry, Transform sourceTransform, string sourceName)
    {
        GameObject prefab = entry.captionPrefab ?? captionDatabase.defaultCaptionPrefab;
        if (prefab == null)
        {
            Debug.LogError("No caption prefab available!");
            return null;
        }

        // Instantiate canvas
        GameObject canvasObject = Instantiate(prefab);
        canvasObject.name = $"CaptionCanvas_{sourceName}";

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Instantiated prefab doesn't have a Canvas component!");
            DestroyImmediate(canvasObject);
            return null;
        }

        // Setup character caption behavior if present
        AnchoredCaptionBehavior charCaption = canvas.GetComponent<AnchoredCaptionBehavior>();
        if (charCaption != null)
        {
            charCaption.SetCharacterTransform(sourceTransform);
        }

        return canvas;
    }

    private void StopCaptionSession(object mediaSource)
    {
        if (activeSessions.TryGetValue(mediaSource, out CaptionSession session))
        {
            activeSessions.Remove(mediaSource);
            CleanupSession(session);
            LogDebug($"Stopped caption session for {GetMediaSourceName(mediaSource)}");
        }
    }

    private void CleanupSession(CaptionSession session)
    {
        if (session.captionCanvas != null)
        {
            // Notify the UI behavior before destroying
            IUIBehavior behavior = session.captionCanvas.GetComponent<IUIBehavior>();
            if (behavior != null)
            {
                behavior.OnUIHidden();
            }

            DestroyImmediate(session.captionCanvas.gameObject);
        }

        // Remove from active sessions if still there
        if (session.mediaSource != null && activeSessions.ContainsKey(session.mediaSource))
        {
            activeSessions.Remove(session.mediaSource);
        }
    }

    private void ClearAllSessions()
    {
        var sessionsToStop = new List<CaptionSession>(activeSessions.Values);
        foreach (var session in sessionsToStop)
        {
            CleanupSession(session);
        }

        activeSessions.Clear();
        lastKnownAudioClips.Clear();
        lastKnownVideoClips.Clear();
    }

    #endregion

    #region Media Helper Methods

    private bool IsMediaPlaying(object mediaSource, MediaSourceType sourceType)
    {
        switch (sourceType)
        {
            case MediaSourceType.Audio:
                return (mediaSource as AudioSource)?.isPlaying ?? false;
            case MediaSourceType.Video:
                return (mediaSource as VideoPlayer)?.isPlaying ?? false;
            default:
                return false;
        }
    }

    private float GetMediaTime(object mediaSource, MediaSourceType sourceType)
    {
        switch (sourceType)
        {
            case MediaSourceType.Audio:
                return (mediaSource as AudioSource)?.time ?? 0f;
            case MediaSourceType.Video:
                return (float)((mediaSource as VideoPlayer)?.time ?? 0.0);
            default:
                return 0f;
        }
    }

    private string GetMediaSourceName(object mediaSource)
    {
        if (mediaSource is AudioSource audioSource)
            return audioSource.name;
        if (mediaSource is VideoPlayer videoPlayer)
            return videoPlayer.name;
        return "Unknown";
    }

    #endregion

    #region Public API

    /// <summary>
    /// Enable or disable all captions
    /// </summary>
    public void SetCaptionsEnabled(bool enabled)
    {
        captionsEnabled = enabled;

        // Hide all active captions when disabled
        if (!enabled)
        {
            foreach (var session in activeSessions.Values)
            {
                if (session.captionCanvas != null && session.captionCanvas.gameObject.activeInHierarchy)
                {
                    session.captionCanvas.gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Toggle captions on/off
    /// </summary>
    public void ToggleCaptions()
    {
        SetCaptionsEnabled(!captionsEnabled);
    }

    /// <summary>
    /// Set the caption database to use
    /// </summary>
    public void SetCaptionDatabase(CaptionDatabase database)
    {
        if (database == null)
        {
            Debug.LogError("Cannot set null caption database");
            return;
        }

        captionDatabase = database;
        LogDebug($"Caption database updated: {database.name}");
    }

    /// <summary>
    /// Manually start captions for an audio source (overrides auto-discovery)
    /// </summary>
    public void StartCaptionsManually(AudioSource audioSource, AudioClip audioClip, TextAsset srtFile, GameObject prefab = null)
    {
        if (audioSource == null || audioClip == null || srtFile == null)
        {
            Debug.LogError("Cannot start captions with null parameters");
            return;
        }

        // Stop any existing session
        if (activeSessions.ContainsKey(audioSource))
        {
            StopCaptionSession(audioSource);
        }

        // Create temporary entry for manual setup
        var tempEntry = new CaptionDatabase.CaptionEntry
        {
            audioClip = audioClip,
            srtFile = srtFile,
            captionPrefab = prefab ?? captionDatabase.defaultCaptionPrefab
        };

        // Create manual session
        var session = new CaptionSession();
        session.mediaSource = audioSource;
        session.sourceType = MediaSourceType.Audio;
        session.captionEntry = tempEntry;
        session.captions = SRTParser.ParseSRT(srtFile.text);
        session.currentCaptionIndex = -1;
        session.wasPlayingLastFrame = false;

        // Instantiate caption canvas
        session.captionCanvas = InstantiateCaptionCanvas(tempEntry, audioSource.transform, audioSource.name);

        if (session.captionCanvas != null)
        {
            FindUIComponents(session);
            session.captionCanvas.gameObject.SetActive(false);
        }

        // Store session
        activeSessions[audioSource] = session;

        LogDebug($"Started manual caption session for audio {audioSource.name}");
    }

    /// <summary>
    /// Manually start captions for a video player (overrides auto-discovery)
    /// </summary>
    public void StartCaptionsManually(VideoPlayer videoPlayer, VideoClip videoClip, TextAsset srtFile, GameObject prefab = null)
    {
        if (videoPlayer == null || videoClip == null || srtFile == null)
        {
            Debug.LogError("Cannot start captions with null parameters");
            return;
        }

        // Stop any existing session
        if (activeSessions.ContainsKey(videoPlayer))
        {
            StopCaptionSession(videoPlayer);
        }

        // Create temporary entry for manual setup
        var tempEntry = new CaptionDatabase.CaptionEntry
        {
            videoClip = videoClip,
            srtFile = srtFile,
            captionPrefab = prefab ?? captionDatabase.defaultCaptionPrefab
        };

        // Create manual session
        var session = new CaptionSession();
        session.mediaSource = videoPlayer;
        session.sourceType = MediaSourceType.Video;
        session.captionEntry = tempEntry;
        session.captions = SRTParser.ParseSRT(srtFile.text);
        session.currentCaptionIndex = -1;
        session.wasPlayingLastFrame = false;

        // Instantiate caption canvas
        session.captionCanvas = InstantiateCaptionCanvas(tempEntry, videoPlayer.transform, videoPlayer.name);

        if (session.captionCanvas != null)
        {
            FindUIComponents(session);
            session.captionCanvas.gameObject.SetActive(false);
        }

        // Store session
        activeSessions[videoPlayer] = session;

        LogDebug($"Started manual caption session for video {videoPlayer.name}");
    }

    /// <summary>
    /// Stop captions for a specific audio source
    /// </summary>
    public void StopCaptionsManually(AudioSource audioSource)
    {
        StopCaptionSession(audioSource);
    }

    /// <summary>
    /// Stop captions for a specific video player
    /// </summary>
    public void StopCaptionsManually(VideoPlayer videoPlayer)
    {
        StopCaptionSession(videoPlayer);
    }

    /// <summary>
    /// Check if an audio source has active captions
    /// </summary>
    public bool HasActiveCaptions(AudioSource audioSource)
    {
        return activeSessions.ContainsKey(audioSource) && activeSessions[audioSource].captionCanvas != null;
    }

    /// <summary>
    /// Check if a video player has active captions
    /// </summary>
    public bool HasActiveCaptions(VideoPlayer videoPlayer)
    {
        return activeSessions.ContainsKey(videoPlayer) && activeSessions[videoPlayer].captionCanvas != null;
    }

    /// <summary>
    /// Get the caption canvas for a specific audio source
    /// </summary>
    public Canvas GetCaptionCanvas(AudioSource audioSource)
    {
        if (activeSessions.TryGetValue(audioSource, out CaptionSession session))
        {
            return session.captionCanvas;
        }
        return null;
    }

    /// <summary>
    /// Get the caption canvas for a specific video player
    /// </summary>
    public Canvas GetCaptionCanvas(VideoPlayer videoPlayer)
    {
        if (activeSessions.TryGetValue(videoPlayer, out CaptionSession session))
        {
            return session.captionCanvas;
        }
        return null;
    }

    /// <summary>
    /// Register an audio source for explicit monitoring
    /// </summary>
    public void RegisterAudioSource(CaptionEnabledAudioSource captionSource)
    {
        registeredAudioSources.Add(captionSource);
        LogDebug($"Registered audio source: {captionSource.name}");
    }

    /// <summary>
    /// Unregister an audio source
    /// </summary>
    public void UnregisterAudioSource(CaptionEnabledAudioSource captionSource)
    {
        registeredAudioSources.Remove(captionSource);

        // Stop any active session for this source
        var audioSource = captionSource.GetComponent<AudioSource>();
        if (audioSource != null)
        {
            StopCaptionSession(audioSource);
        }
    }

    /// <summary>
    /// Register a video player for explicit monitoring
    /// </summary>
    public void RegisterVideoPlayer(CaptionEnabledVideoPlayer captionSource)
    {
        registeredVideoPlayers.Add(captionSource);
        LogDebug($"Registered video player: {captionSource.name}");
    }

    /// <summary>
    /// Unregister a video player
    /// </summary>
    public void UnregisterVideoPlayer(CaptionEnabledVideoPlayer captionSource)
    {
        registeredVideoPlayers.Remove(captionSource);

        // Stop any active session for this source
        var videoPlayer = captionSource.GetComponent<VideoPlayer>();
        if (videoPlayer != null)
        {
            StopCaptionSession(videoPlayer);
        }
    }

    /// <summary>
    /// Enable or disable auto-discovery at runtime
    /// </summary>
    public void SetAutoDiscovery(bool audioEnabled, bool videoEnabled)
    {
        autoDiscoverAudioSources = audioEnabled;
        autoDiscoverVideoPlayers = videoEnabled;

        if (audioEnabled || videoEnabled)
        {
            StartAutoDiscovery();
        }
        else
        {
            StopAutoDiscovery();
        }
    }

    /// <summary>
    /// Get the current caption database
    /// </summary>
    public CaptionDatabase GetCaptionDatabase()
    {
        return captionDatabase;
    }

    /// <summary>
    /// Get count of active caption sessions
    /// </summary>
    public int GetActiveSessionCount()
    {
        return activeSessions.Count;
    }

    #endregion

    #region Utility Methods

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[GlobalCaptionManager] {message}");
        }
    }

    #endregion

#if UNITY_EDITOR
    [ContextMenu("Force Refresh Media Sources")]
    private void ForceRefreshMediaSources()
    {
        lastKnownAudioClips.Clear();
        lastKnownVideoClips.Clear();
        DiscoverNewAudioSources();
        DiscoverNewVideoPlayers();
    }

    [ContextMenu("Clear All Sessions")]
    private void EditorClearAllSessions()
    {
        ClearAllSessions();
    }
#endif
}