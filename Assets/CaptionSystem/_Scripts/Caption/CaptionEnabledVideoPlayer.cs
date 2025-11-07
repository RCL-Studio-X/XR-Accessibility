using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Makes a VideoPlayer component caption-aware by registering with GlobalCaptionManager
/// Similar to CaptionEnabledAudioSource but for video content
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
public class CaptionEnabledVideoPlayer : MonoBehaviour
{
    [Header("Caption Configuration")]
    [SerializeField] private CaptionDatabase overrideDatabase;
    [SerializeField] private GameObject overridePrefab;

    [Header("Manual Caption Assignment")]
    [SerializeField] private bool useManualAssignment = false;
    [SerializeField] private VideoClip manualVideoClip;
    [SerializeField] private TextAsset manualSRTFile;

    [Header("Advanced Settings")]
    [SerializeField] private bool ignoreGlobalDatabase = false;
    [SerializeField] private bool autoRegisterOnStart = true;

    private VideoPlayer videoPlayer;
    private bool isRegistered = false;

    private void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
    }

    private void Start()
    {
        if (autoRegisterOnStart)
        {
            Register();
        }
    }

    private void OnDestroy()
    {
        Unregister();
    }

    /// <summary>
    /// Register this video player with the global caption manager
    /// </summary>
    public void Register()
    {
        if (isRegistered) return;

        if (GlobalCaptionManager.Instance != null)
        {
            GlobalCaptionManager.Instance.RegisterVideoPlayer(this);
            isRegistered = true;
        }
        else
        {
            Debug.LogWarning($"CaptionEnabledVideoPlayer on {gameObject.name}: GlobalCaptionManager not found!");
        }
    }

    /// <summary>
    /// Unregister this video player from the global caption manager
    /// </summary>
    public void Unregister()
    {
        if (!isRegistered) return;

        if (GlobalCaptionManager.Instance != null)
        {
            GlobalCaptionManager.Instance.UnregisterVideoPlayer(this);
        }

        isRegistered = false;
    }

    /// <summary>
    /// Get the caption configuration for the currently assigned video clip
    /// </summary>
    public CaptionDatabase.CaptionEntry GetCaptionEntry()
    {
        VideoClip clipToCheck = useManualAssignment ? manualVideoClip : videoPlayer.clip;

        if (clipToCheck == null) return null;

        // Use manual assignment if configured
        if (useManualAssignment && manualSRTFile != null)
        {
            return new CaptionDatabase.CaptionEntry
            {
                videoClip = manualVideoClip,
                srtFile = manualSRTFile,
                captionPrefab = overridePrefab
            };
        }

        // Check override database first
        if (overrideDatabase != null)
        {
            var entry = overrideDatabase.GetEntryForVideo(clipToCheck);
            if (entry != null)
            {
                // Apply local overrides to the database entry
                return ApplyLocalOverrides(entry);
            }
        }

        // Check global database if not ignoring it
        if (!ignoreGlobalDatabase && GlobalCaptionManager.Instance != null)
        {
            var globalEntry = GlobalCaptionManager.Instance.GetCaptionDatabase()?.GetEntryForVideo(clipToCheck);
            if (globalEntry != null)
            {
                return ApplyLocalOverrides(globalEntry);
            }
        }

        return null;
    }

    /// <summary>
    /// Apply local overrides to a database entry
    /// </summary>
    private CaptionDatabase.CaptionEntry ApplyLocalOverrides(CaptionDatabase.CaptionEntry originalEntry)
    {
        // Create a copy to avoid modifying the original
        var modifiedEntry = new CaptionDatabase.CaptionEntry
        {
            videoClip = originalEntry.videoClip,
            srtFile = originalEntry.srtFile,
            captionPrefab = overridePrefab ?? originalEntry.captionPrefab
        };

        return modifiedEntry;
    }

    /// <summary>
    /// Manually play video with captions
    /// </summary>
    public void PlayWithCaptions(VideoClip clip = null, TextAsset srtFile = null)
    {
        VideoClip clipToPlay = clip ?? videoPlayer.clip;

        if (clipToPlay == null)
        {
            Debug.LogError($"CaptionEnabledVideoPlayer on {gameObject.name}: No video clip to play!");
            return;
        }

        // Set the clip if provided
        if (clip != null)
        {
            videoPlayer.clip = clip;
        }

        // Start video
        videoPlayer.Play();

        // Setup captions manually if SRT provided
        if (srtFile != null && GlobalCaptionManager.Instance != null)
        {
            GlobalCaptionManager.Instance.StartCaptionsManually(
                videoPlayer,
                clipToPlay,
                srtFile,
                overridePrefab
            );
        }
    }

    /// <summary>
    /// Stop video and captions
    /// </summary>
    public void StopWithCaptions()
    {
        videoPlayer.Stop();

        if (GlobalCaptionManager.Instance != null)
        {
            GlobalCaptionManager.Instance.StopCaptionsManually(videoPlayer);
        }
    }

    /// <summary>
    /// Check if this video player currently has active captions
    /// </summary>
    public bool HasActiveCaptions()
    {
        if (GlobalCaptionManager.Instance == null) return false;
        return GlobalCaptionManager.Instance.HasActiveCaptions(videoPlayer);
    }

    /// <summary>
    /// Get the active caption canvas for this video player
    /// </summary>
    public Canvas GetActiveCaptionCanvas()
    {
        if (GlobalCaptionManager.Instance == null) return null;
        return GlobalCaptionManager.Instance.GetCaptionCanvas(videoPlayer);
    }

    /// <summary>
    /// Set manual caption assignment
    /// </summary>
    public void SetManualCaptions(VideoClip clip, TextAsset srt, GameObject prefab = null)
    {
        useManualAssignment = true;
        manualVideoClip = clip;
        manualSRTFile = srt;

        if (prefab != null)
        {
            overridePrefab = prefab;
        }
    }

    /// <summary>
    /// Clear manual assignment and use database lookup
    /// </summary>
    public void ClearManualAssignment()
    {
        useManualAssignment = false;
        manualVideoClip = null;
        manualSRTFile = null;
    }

    /// <summary>
    /// Get the VideoPlayer component
    /// </summary>
    public VideoPlayer GetVideoPlayer()
    {
        return videoPlayer;
    }

#if UNITY_EDITOR
    [ContextMenu("Test Caption Configuration")]
    private void TestCaptionConfiguration()
    {
        var entry = GetCaptionEntry();
        if (entry != null)
        {
            Debug.Log($"Caption configuration found for {gameObject.name}:\n" +
                     $"Video Clip: {entry.videoClip?.name}\n" +
                     $"SRT File: {entry.srtFile?.name}\n" +
                     $"Prefab: {entry.captionPrefab?.name}");
        }
        else
        {
            Debug.LogWarning($"No caption configuration found for {gameObject.name}");
        }
    }

    [ContextMenu("Force Register")]
    private void ForceRegister()
    {
        if (isRegistered) Unregister();
        Register();
    }
#endif
}
