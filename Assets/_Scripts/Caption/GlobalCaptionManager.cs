using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class GlobalCaptionManager : MonoBehaviour
{
    [Header("Database Configuration")]
    [SerializeField] private CaptionDatabase captionDatabase;

    [Header("Auto-Discovery Settings")]
    [SerializeField] private bool autoDiscoverAudioSources = true;
    [SerializeField] private float discoveryUpdateRate = 0.5f;
    [SerializeField] private bool monitorNewAudioSources = true;

    [Header("Default Prefab Overrides")]
    [SerializeField] private GameObject playerFollowPrefab;
    [SerializeField] private GameObject staticObjectPrefab;
    [SerializeField] private GameObject characterDialoguePrefab;
    [SerializeField] private GameObject screenSpacePrefab;

    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;

    // Active caption sessions - now managed directly
    private Dictionary<AudioSource, CaptionSession> activeSessions = new Dictionary<AudioSource, CaptionSession>();
    private Dictionary<AudioSource, AudioClip> lastKnownClips = new Dictionary<AudioSource, AudioClip>();

    // Caption session data structure
    private class CaptionSession
    {
        public AudioSource audioSource;
        public CaptionDatabase.CaptionEntry captionEntry;
        public Canvas captionCanvas;
        public TextMeshProUGUI speakerText;
        public TextMeshProUGUI captionText;
        public List<CaptionEntry> captions;
        public int currentCaptionIndex = -1;
        public bool wasPlayingLastFrame = false;
        public bool isActive = false;
    }

    // Registered audio sources (for manual control)
    private HashSet<CaptionEnabledAudioSource> registeredSources = new HashSet<CaptionEnabledAudioSource>();

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

        Debug.Log($"[DEBUG] Database assigned: {captionDatabase.name} with {captionDatabase.GetAllAudioClips().Count} clips");

        if (autoDiscoverAudioSources)
        {
            StartAutoDiscovery();
        }

        LogDebug("GlobalCaptionManager started successfully");
    }

    private void Update()
    {
        if (autoDiscoverAudioSources && discoveryCoroutine == null)
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
            if (session.audioSource == null || session.captionCanvas == null)
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
        bool isPlayingNow = session.audioSource.isPlaying;
        bool wasPlayingLastFrame = session.wasPlayingLastFrame;

        // Audio just started playing
        if (isPlayingNow && !wasPlayingLastFrame)
        {
            session.isActive = true;
            session.currentCaptionIndex = -1;
        }
        // Audio just stopped playing
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

        float currentTime = session.audioSource.time;

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
        Debug.Log("ShowCaption");
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

        LogDebug($"Caption: [{caption.speaker}] {caption.text}");
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
        discoveryCoroutine = StartCoroutine(AudioDiscoveryLoop());
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

    private IEnumerator AudioDiscoveryLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(discoveryUpdateRate);

            if (monitorNewAudioSources)
            {
                DiscoverNewAudioSources();
            }

            UpdateActiveAudioSources();
        }
    }

    private void DiscoverNewAudioSources()
    {
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();

        foreach (AudioSource audioSource in allAudioSources)
        {
            if (audioSource != null && !lastKnownClips.ContainsKey(audioSource))
            {
                lastKnownClips[audioSource] = null;
                LogDebug($"Discovered new audio source: {audioSource.name}");
            }
        }

        // Clean up destroyed audio sources
        var sourcesToRemove = new List<AudioSource>();
        foreach (var audioSource in lastKnownClips.Keys)
        {
            if (audioSource == null)
            {
                sourcesToRemove.Add(audioSource);
            }
        }

        foreach (var source in sourcesToRemove)
        {
            lastKnownClips.Remove(source);
            if (activeSessions.ContainsKey(source))
            {
                StopCaptionSession(source);
            }
        }
    }

    private void UpdateActiveAudioSources()
    {
        var currentSources = new List<AudioSource>(lastKnownClips.Keys);

        foreach (AudioSource audioSource in currentSources)
        {
            if (audioSource == null) continue;

            AudioClip currentClip = audioSource.clip;
            AudioClip lastClip = lastKnownClips[audioSource];

            // Check if clip changed
            if (currentClip != lastClip)
            {
                lastKnownClips[audioSource] = currentClip;
                OnAudioClipChanged(audioSource, currentClip, lastClip);
            }

            // Check if audio stopped
            if (!audioSource.isPlaying && activeSessions.ContainsKey(audioSource))
            {
                var session = activeSessions[audioSource];
                if (session != null && !session.isActive)
                {
                    // Audio stopped, but caption manager might not have detected it yet
                    // Let the caption manager handle this naturally
                }
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
            Debug.Log($"[DEBUG] No caption found for clip: {newClip.name}. Database has {captionDatabase.GetAllAudioClips().Count} clips");

            StartCaptionSession(audioSource, newClip);
        }
    }

    #endregion

    #region Caption Session Management

    private void StartCaptionSession(AudioSource audioSource, AudioClip audioClip)
    {
        var captionEntry = captionDatabase.GetEntryForClip(audioClip);
        if (captionEntry == null) return;

        // Create a new caption session directly managed by this class
        var session = new CaptionSession();
        session.audioSource = audioSource;
        session.captionEntry = captionEntry;
        session.captions = SRTParser.ParseSRT(captionEntry.srtFile.text);
        session.currentCaptionIndex = -1;
        session.wasPlayingLastFrame = false;

        // Instantiate caption canvas
        session.captionCanvas = InstantiateCaptionCanvas(captionEntry, audioSource);

        if (session.captionCanvas != null)
        {
            // Find UI components
            FindUIComponents(session);
            session.captionCanvas.gameObject.SetActive(false);
        }

        // Store session
        activeSessions[audioSource] = session;

        LogDebug($"Started caption session for {audioSource.name} with clip {audioClip.name}");
    }

    private Canvas InstantiateCaptionCanvas(CaptionDatabase.CaptionEntry entry, AudioSource audioSource)
    {
        GameObject prefab = GetPrefabForUIType(entry.uiType, entry.captionPrefab);
        if (prefab == null)
        {
            Debug.LogError("No caption prefab available for UI type: " + entry.uiType);
            return null;
        }

        // Instantiate canvas
        GameObject canvasObject = Instantiate(prefab);
        canvasObject.name = $"CaptionCanvas_{audioSource.name}";
        canvasObject.transform.SetParent(transform);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Instantiated prefab doesn't have a Canvas component!");
            DestroyImmediate(canvasObject);
            return null;
        }

        // Setup UI behavior
        SetupUIBehavior(canvas, entry, audioSource);

        return canvas;
    }

    private void SetupUIBehavior(Canvas canvas, CaptionDatabase.CaptionEntry entry, AudioSource audioSource)
    {
        IUIBehavior behavior = canvas.GetComponent<IUIBehavior>();
        if (behavior != null)
        {
            // Configure based on UI type and entry settings
            if (behavior is SmoothFollowBehavior smoothFollow)
            {
                Transform targetTransform = entry.customTarget;

                if (targetTransform == null)
                {
                    if (entry.uiType == CaptionUIType.StaticObject)
                    {
                        targetTransform = audioSource.transform;
                    }
                    else
                    {
                        // Use main camera as default
                        Camera mainCamera = Camera.main;
                        if (mainCamera != null)
                        {
                            targetTransform = mainCamera.transform;
                        }
                    }
                }

                if (targetTransform != null)
                {
                    smoothFollow.targetTransform = targetTransform;
                }
            }
        }
    }

    private GameObject GetPrefabForUIType(CaptionUIType uiType, GameObject overridePrefab)
    {
        if (overridePrefab != null) return overridePrefab;

        return uiType switch
        {
            CaptionUIType.PlayerFollow => playerFollowPrefab ?? captionDatabase.defaultCaptionPrefab,
            CaptionUIType.StaticObject => staticObjectPrefab ?? captionDatabase.defaultCaptionPrefab,
            CaptionUIType.CharacterDialogue => characterDialoguePrefab ?? captionDatabase.defaultCaptionPrefab,
            CaptionUIType.ScreenSpace => screenSpacePrefab ?? captionDatabase.defaultCaptionPrefab,
            _ => captionDatabase.defaultCaptionPrefab
        };
    }

    private void StopCaptionSession(AudioSource audioSource)
    {
        if (activeSessions.TryGetValue(audioSource, out CaptionSession session))
        {
            activeSessions.Remove(audioSource);
            CleanupSession(session);
            LogDebug($"Stopped caption session for {audioSource?.name}");
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
        if (session.audioSource != null && activeSessions.ContainsKey(session.audioSource))
        {
            activeSessions.Remove(session.audioSource);
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
        lastKnownClips.Clear();
    }

    #endregion

    #region Public API

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
            captionPrefab = prefab ?? captionDatabase.defaultCaptionPrefab,
            uiType = CaptionUIType.Custom
        };

        // Create manual session
        var session = new CaptionSession();
        session.audioSource = audioSource;
        session.captionEntry = tempEntry;
        session.captions = SRTParser.ParseSRT(srtFile.text);
        session.currentCaptionIndex = -1;
        session.wasPlayingLastFrame = false;

        // Instantiate caption canvas
        session.captionCanvas = InstantiateCaptionCanvas(tempEntry, audioSource);

        if (session.captionCanvas != null)
        {
            FindUIComponents(session);
            session.captionCanvas.gameObject.SetActive(false);
        }

        // Store session
        activeSessions[audioSource] = session;

        LogDebug($"Started manual caption session for {audioSource.name}");
    }

    /// <summary>
    /// Stop captions for a specific audio source
    /// </summary>
    public void StopCaptionsManually(AudioSource audioSource)
    {
        StopCaptionSession(audioSource);
    }

    /// <summary>
    /// Check if an audio source has active captions
    /// </summary>
    public bool HasActiveCaptions(AudioSource audioSource)
    {
        return activeSessions.ContainsKey(audioSource) && activeSessions[audioSource].captionCanvas != null;
    }

    /// <summary>
    /// Get the caption session for a specific audio source
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
    /// Register an audio source for explicit monitoring
    /// </summary>
    public void RegisterAudioSource(CaptionEnabledAudioSource captionSource)
    {
        registeredSources.Add(captionSource);
        LogDebug($"Registered audio source: {captionSource.name}");
    }

    /// <summary>
    /// Unregister an audio source
    /// </summary>
    public void UnregisterAudioSource(CaptionEnabledAudioSource captionSource)
    {
        registeredSources.Remove(captionSource);

        // Stop any active session for this source
        var audioSource = captionSource.GetComponent<AudioSource>();
        if (audioSource != null)
        {
            StopCaptionSession(audioSource);
        }
    }

    /// <summary>
    /// Enable or disable auto-discovery at runtime
    /// </summary>
    public void SetAutoDiscovery(bool enabled)
    {
        autoDiscoverAudioSources = enabled;

        if (enabled)
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
    [ContextMenu("Force Refresh Audio Sources")]
    private void ForceRefreshAudioSources()
    {
        lastKnownClips.Clear();
        DiscoverNewAudioSources();
    }

    [ContextMenu("Clear All Sessions")]
    private void EditorClearAllSessions()
    {
        ClearAllSessions();
    }
#endif
}