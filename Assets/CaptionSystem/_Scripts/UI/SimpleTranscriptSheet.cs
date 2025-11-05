//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using System.Collections.Generic;

///// <summary>
///// Simple transcript display - no auto-scroll, no highlighting, just shows all captions
///// </summary>
//[RequireComponent(typeof(Canvas))]
//public class SimpleTranscriptSheet : MonoBehaviour
//{
//    [Header("UI References")]
//    [SerializeField] private ScrollRect scrollRect;
//    [SerializeField] private RectTransform contentContainer;
//    [SerializeField] private GameObject transcriptEntryPrefab;

//    [Header("Display Settings")]
//    [SerializeField] private Color textColor = Color.white;
//    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.3f);

//    [Header("Auto-Detection")]
//    [SerializeField] private bool autoDetectPlayingAudio = true;
//    [SerializeField] private AudioSource manualAudioSource;
//    [SerializeField] private float detectionCheckInterval = 0.5f;

//    private Canvas canvas;
//    private List<CaptionEntry> captions = new List<CaptionEntry>();
//    private List<TranscriptEntryUI> entryUIElements = new List<TranscriptEntryUI>();

//    private AudioSource trackedAudioSource;
//    private AudioClip lastTrackedClip;
//    private float nextDetectionCheckTime = 0f;

//    private void Awake()
//    {
//        canvas = GetComponent<Canvas>();

//        if (scrollRect == null)
//        {
//            scrollRect = GetComponentInChildren<ScrollRect>();
//        }

//        if (contentContainer == null && scrollRect != null)
//        {
//            contentContainer = scrollRect.content;
//        }
//    }

//    private void Start()
//    {
//        if (canvas.gameObject.activeInHierarchy)
//        {
//            if (manualAudioSource != null)
//            {
//                LoadTranscriptForAudioSource(manualAudioSource);
//            }
//            else if (autoDetectPlayingAudio)
//            {
//                TryAutoDetectAudioSource();
//            }
//        }
//    }

//    private void Update()
//    {
//        if (autoDetectPlayingAudio && Time.time >= nextDetectionCheckTime)
//        {
//            CheckForAudioChanges();
//            nextDetectionCheckTime = Time.time + detectionCheckInterval;
//        }
//    }

//    #region Auto-Detection

//    private void CheckForAudioChanges()
//    {
//        if (manualAudioSource != null)
//        {
//            if (trackedAudioSource != manualAudioSource || lastTrackedClip != manualAudioSource.clip)
//            {
//                LoadTranscriptForAudioSource(manualAudioSource);
//            }
//            return;
//        }

//        if (trackedAudioSource != null)
//        {
//            if (!trackedAudioSource.isPlaying || trackedAudioSource.clip != lastTrackedClip)
//            {
//                TryAutoDetectAudioSource();
//            }
//        }
//        else
//        {
//            TryAutoDetectAudioSource();
//        }
//    }

//    private void TryAutoDetectAudioSource()
//    {
//        if (GlobalCaptionManager.Instance == null) return;

//        var database = GlobalCaptionManager.Instance.GetCaptionDatabase();
//        if (database == null) return;

//        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();

//        foreach (var audioSource in allAudioSources)
//        {
//            if (audioSource.isPlaying && audioSource.clip != null)
//            {
//                if (database.HasCaptionForClip(audioSource.clip))
//                {
//                    LoadTranscriptForAudioSource(audioSource);
//                    return;
//                }
//            }
//        }
//    }

//    #endregion

//    #region Transcript Loading

//    public void LoadTranscriptForAudioSource(AudioSource audioSource)
//    {
//        if (audioSource == null) return;

//        if (trackedAudioSource == audioSource && lastTrackedClip == audioSource.clip)
//        {
//            return;
//        }

//        trackedAudioSource = audioSource;
//        lastTrackedClip = audioSource.clip;

//        if (GlobalCaptionManager.Instance != null)
//        {
//            var database = GlobalCaptionManager.Instance.GetCaptionDatabase();
//            if (database != null)
//            {
//                var entry = database.GetEntryForClip(audioSource.clip);
//                if (entry != null && entry.srtFile != null)
//                {
//                    LoadTranscript(entry.srtFile);
//                    return;
//                }
//            }
//        }
//    }

//    public void LoadTranscript(TextAsset srtFile)
//    {
//        if (srtFile == null) return;

//        captions = SRTParser.ParseSRT(srtFile.text);
//        ClearTranscriptUI();
//        CreateTranscriptUI();
//    }

//    #endregion

//    #region UI Creation

//    private void CreateTranscriptUI()
//    {
//        if (contentContainer == null || transcriptEntryPrefab == null) return;

//        for (int i = 0; i < captions.Count; i++)
//        {
//            GameObject entryObj = Instantiate(transcriptEntryPrefab, contentContainer);
//            TranscriptEntryUI entryUI = entryObj.GetComponent<TranscriptEntryUI>();

//            if (entryUI == null)
//            {
//                entryUI = entryObj.AddComponent<TranscriptEntryUI>();
//            }

//            entryUI.Setup(captions[i], i, textColor, backgroundColor);
//            entryUIElements.Add(entryUI);
//        }
//    }

//    private void ClearTranscriptUI()
//    {
//        foreach (var entry in entryUIElements)
//        {
//            if (entry != null)
//            {
//                Destroy(entry.gameObject);
//            }
//        }

//        entryUIElements.Clear();
//    }

//    #endregion

//    #region Public API

//    public void SetManualAudioSource(AudioSource audioSource)
//    {
//        manualAudioSource = audioSource;
//        LoadTranscriptForAudioSource(audioSource);
//    }

//    public void ClearTranscript()
//    {
//        ClearTranscriptUI();
//        captions.Clear();
//        trackedAudioSource = null;
//        lastTrackedClip = null;
//    }

//    #endregion
//}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Simple transcript display with support for both Audio and Video
/// </summary>
[RequireComponent(typeof(Canvas))]
public class SimpleTranscriptSheet : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform contentContainer;
    [SerializeField] private GameObject transcriptEntryPrefab;

    [Header("Display Settings")]
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.3f);

    [Header("Auto-Detection")]
    [SerializeField] private bool autoDetectPlayingMedia = true;
    [SerializeField] private CaptionDatabase captionDatabase; // Optional: assign directly
    [SerializeField] private AudioSource manualAudioSource;
    [SerializeField] private VideoPlayer manualVideoPlayer; // NEW: Manual video assignment
    [SerializeField] private float detectionCheckInterval = 0.5f;

    private Canvas canvas;
    private List<CaptionEntry> captions = new List<CaptionEntry>();
    private List<TranscriptEntryUI> entryUIElements = new List<TranscriptEntryUI>();

    // Audio tracking
    private AudioSource trackedAudioSource;
    private AudioClip lastTrackedClip;

    // Video tracking (NEW)
    private VideoPlayer trackedVideoPlayer;
    private VideoClip lastTrackedVideo;

    private float nextDetectionCheckTime = 0f;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();

        if (scrollRect == null)
        {
            scrollRect = GetComponentInChildren<ScrollRect>();
        }

        if (contentContainer == null && scrollRect != null)
        {
            contentContainer = scrollRect.content;
        }
    }

    private void Start()
    {
        if (canvas.gameObject.activeInHierarchy)
        {
            if (manualVideoPlayer != null)
            {
                LoadTranscriptForVideo(manualVideoPlayer);
            }
            else if (manualAudioSource != null)
            {
                LoadTranscriptForAudioSource(manualAudioSource);
            }
            else if (autoDetectPlayingMedia)
            {
                TryAutoDetectMedia();
            }
        }
    }

    private void Update()
    {
        if (autoDetectPlayingMedia && Time.time >= nextDetectionCheckTime)
        {
            CheckForMediaChanges();
            nextDetectionCheckTime = Time.time + detectionCheckInterval;
        }
    }

    #region Auto-Detection

    private void CheckForMediaChanges()
    {
        // Manual assignments take priority
        if (manualVideoPlayer != null)
        {
            if (trackedVideoPlayer != manualVideoPlayer || lastTrackedVideo != manualVideoPlayer.clip)
            {
                LoadTranscriptForVideo(manualVideoPlayer);
            }
            return;
        }

        if (manualAudioSource != null)
        {
            if (trackedAudioSource != manualAudioSource || lastTrackedClip != manualAudioSource.clip)
            {
                LoadTranscriptForAudioSource(manualAudioSource);
            }
            return;
        }

        // Check tracked video
        if (trackedVideoPlayer != null)
        {
            if (!trackedVideoPlayer.isPlaying || trackedVideoPlayer.clip != lastTrackedVideo)
            {
                TryAutoDetectMedia();
            }
        }
        // Check tracked audio
        else if (trackedAudioSource != null)
        {
            if (!trackedAudioSource.isPlaying || trackedAudioSource.clip != lastTrackedClip)
            {
                TryAutoDetectMedia();
            }
        }
        else
        {
            TryAutoDetectMedia();
        }
    }

    private void TryAutoDetectMedia()
    {
        CaptionDatabase database = captionDatabase; // Use assigned database first

        // If not assigned, try to get from GlobalCaptionManager
        if (database == null && GlobalCaptionManager.Instance != null)
        {
            database = GlobalCaptionManager.Instance.GetCaptionDatabase();
        }

        // If still null, try to find it directly in project
        if (database == null)
        {
            var databases = Resources.FindObjectsOfTypeAll<CaptionDatabase>();
            if (databases.Length > 0)
            {
                database = databases[0];
                Debug.Log($"SimpleTranscriptSheet: Found CaptionDatabase: {database.name}");
            }
        }

        if (database == null)
        {
            Debug.LogWarning("SimpleTranscriptSheet: No CaptionDatabase found!");
            return;
        }

        // First, try to detect playing videos
        VideoPlayer[] allVideoPlayers = FindObjectsOfType<VideoPlayer>();
        foreach (var videoPlayer in allVideoPlayers)
        {
            if (videoPlayer.isPlaying && videoPlayer.clip != null)
            {
                Debug.Log($"SimpleTranscriptSheet: Checking video '{videoPlayer.clip.name}'");
                if (database.HasCaptionForVideo(videoPlayer.clip))
                {
                    LoadTranscriptForVideo(videoPlayer, database);
                    Debug.Log($"SimpleTranscriptSheet: Auto-detected video: {videoPlayer.clip.name}");
                    return;
                }
                else
                {
                    Debug.Log($"SimpleTranscriptSheet: Video '{videoPlayer.clip.name}' not in database");
                }
            }
        }

        // If no video found, try audio sources
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (var audioSource in allAudioSources)
        {
            if (audioSource.isPlaying && audioSource.clip != null)
            {
                if (database.HasCaptionForClip(audioSource.clip))
                {
                    LoadTranscriptForAudioSource(audioSource, database);
                    Debug.Log($"SimpleTranscriptSheet: Auto-detected audio: {audioSource.clip.name}");
                    return;
                }
            }
        }
    }

    #endregion

    #region Transcript Loading

    public void LoadTranscriptForAudioSource(AudioSource audioSource, CaptionDatabase database = null)
    {
        if (audioSource == null) return;

        if (trackedAudioSource == audioSource && lastTrackedClip == audioSource.clip)
        {
            return;
        }

        // Clear video tracking
        trackedVideoPlayer = null;
        lastTrackedVideo = null;

        trackedAudioSource = audioSource;
        lastTrackedClip = audioSource.clip;

        // Get database if not provided
        if (database == null)
        {
            if (GlobalCaptionManager.Instance != null)
            {
                database = GlobalCaptionManager.Instance.GetCaptionDatabase();
            }
            else
            {
                database = Resources.FindObjectsOfTypeAll<CaptionDatabase>()[0];
            }
        }

        if (database != null)
        {
            var entry = database.GetEntryForClip(audioSource.clip);
            if (entry != null && entry.srtFile != null)
            {
                LoadTranscript(entry.srtFile);
                return;
            }
        }
    }

    public void LoadTranscriptForVideo(VideoPlayer videoPlayer, CaptionDatabase database = null)
    {
        if (videoPlayer == null || videoPlayer.clip == null) return;

        if (trackedVideoPlayer == videoPlayer && lastTrackedVideo == videoPlayer.clip)
        {
            return;
        }

        // Clear audio tracking
        trackedAudioSource = null;
        lastTrackedClip = null;

        trackedVideoPlayer = videoPlayer;
        lastTrackedVideo = videoPlayer.clip;

        // Get database if not provided
        if (database == null)
        {
            if (GlobalCaptionManager.Instance != null)
            {
                database = GlobalCaptionManager.Instance.GetCaptionDatabase();
            }
            else
            {
                database = Resources.FindObjectsOfTypeAll<CaptionDatabase>()[0];
            }
        }

        if (database != null)
        {
            var entry = database.GetEntryForVideo(videoPlayer.clip);
            if (entry != null && entry.srtFile != null)
            {
                LoadTranscript(entry.srtFile);
                Debug.Log($"SimpleTranscriptSheet: Loaded transcript for video: {videoPlayer.clip.name}");
                return;
            }
        }

        Debug.LogWarning($"SimpleTranscriptSheet: No caption found for video: {videoPlayer.clip.name}");
    }

    public void LoadTranscript(TextAsset srtFile)
    {
        if (srtFile == null) return;

        captions = SRTParser.ParseSRT(srtFile.text);
        ClearTranscriptUI();
        CreateTranscriptUI();
    }

    #endregion

    #region UI Creation

    private void CreateTranscriptUI()
    {
        if (contentContainer == null || transcriptEntryPrefab == null) return;

        for (int i = 0; i < captions.Count; i++)
        {
            GameObject entryObj = Instantiate(transcriptEntryPrefab, contentContainer);
            TranscriptEntryUI entryUI = entryObj.GetComponent<TranscriptEntryUI>();

            if (entryUI == null)
            {
                entryUI = entryObj.AddComponent<TranscriptEntryUI>();
            }

            entryUI.Setup(captions[i], i, textColor, backgroundColor);
            entryUIElements.Add(entryUI);
        }
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
    }

    #endregion

    #region Public API

    public void SetManualAudioSource(AudioSource audioSource)
    {
        manualAudioSource = audioSource;
        manualVideoPlayer = null; // Clear video
        LoadTranscriptForAudioSource(audioSource);
    }

    public void SetManualVideoPlayer(VideoPlayer videoPlayer)
    {
        manualVideoPlayer = videoPlayer;
        manualAudioSource = null; // Clear audio
        LoadTranscriptForVideo(videoPlayer);
    }

    public void ClearTranscript()
    {
        ClearTranscriptUI();
        captions.Clear();
        trackedAudioSource = null;
        lastTrackedClip = null;
        trackedVideoPlayer = null;
        lastTrackedVideo = null;
    }

    #endregion
}