/*using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CaptionEnabledAudioSource : MonoBehaviour
{
    [Header("Caption Configuration")]
    [SerializeField] private CaptionDatabase overrideDatabase;
    [SerializeField] private GameObject overridePrefab;
    [SerializeField] private CaptionUIType uiTypeOverride = CaptionUIType.PlayerFollow;

    [Header("Manual Caption Assignment")]
    [SerializeField] private bool useManualAssignment = false;
    [SerializeField] private AudioClip manualAudioClip;
    [SerializeField] private TextAsset manualSRTFile;

    [Header("Target Settings")]
    [SerializeField] private Transform customTarget; // For static objects or character dialogue
    [SerializeField] private bool useThisTransformAsTarget = false; // For static object captions

    [Header("Advanced Settings")]
    [SerializeField] private bool ignoreGlobalDatabase = false;
    [SerializeField] private bool autoRegisterOnStart = true;

    private AudioSource audioSource;
    private bool isRegistered = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
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
    /// Register this audio source with the global caption manager
    /// </summary>
    public void Register()
    {
        if (isRegistered) return;

        if (GlobalCaptionManager.Instance != null)
        {
            GlobalCaptionManager.Instance.RegisterAudioSource(this);
            isRegistered = true;
        }
        else
        {
            Debug.LogWarning($"CaptionEnabledAudioSource on {gameObject.name}: GlobalCaptionManager not found!");
        }
    }

    /// <summary>
    /// Unregister this audio source from the global caption manager
    /// </summary>
    public void Unregister()
    {
        if (!isRegistered) return;

        if (GlobalCaptionManager.Instance != null)
        {
            GlobalCaptionManager.Instance.UnregisterAudioSource(this);
        }

        isRegistered = false;
    }

    /// <summary>
    /// Get the caption configuration for the currently assigned audio clip
    /// </summary>
    public CaptionDatabase.CaptionEntry GetCaptionEntry()
    {
        AudioClip clipToCheck = useManualAssignment ? manualAudioClip : audioSource.clip;

        if (clipToCheck == null) return null;

        // Use manual assignment if configured
        if (useManualAssignment && manualSRTFile != null)
        {
            return new CaptionDatabase.CaptionEntry
            {
                audioClip = manualAudioClip,
                srtFile = manualSRTFile,
                captionPrefab = overridePrefab,
                uiType = uiTypeOverride,
                customTarget = GetTargetTransform()
            };
        }

        // Check override database first
        if (overrideDatabase != null)
        {
            var entry = overrideDatabase.GetEntryForClip(clipToCheck);
            if (entry != null)
            {
                // Apply local overrides to the database entry
                return ApplyLocalOverrides(entry);
            }
        }

        // Check global database if not ignoring it
        if (!ignoreGlobalDatabase && GlobalCaptionManager.Instance != null)
        {
            var globalEntry = GlobalCaptionManager.Instance.GetCaptionDatabase()?.GetEntryForClip(clipToCheck);
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
            audioClip = originalEntry.audioClip,
            srtFile = originalEntry.srtFile,
            captionPrefab = overridePrefab ?? originalEntry.captionPrefab,
            uiType = uiTypeOverride != CaptionUIType.PlayerFollow ? uiTypeOverride : originalEntry.uiType,
            customTarget = GetTargetTransform() ?? originalEntry.customTarget,
            overrideGlobalSettings = originalEntry.overrideGlobalSettings,
            customShowCaptions = originalEntry.customShowCaptions,
            customDestroyOnStop = originalEntry.customDestroyOnStop
        };

        return modifiedEntry;
    }

    /// <summary>
    /// Get the appropriate target transform based on configuration
    /// </summary>
    public Transform GetTargetTransform()
    {
        if (customTarget != null) return customTarget;
        if (useThisTransformAsTarget) return transform;
        return null;
    }

    /// <summary>
    /// Manually play audio with captions
    /// </summary>
    public void PlayWithCaptions(AudioClip clip = null, TextAsset srtFile = null)
    {
        AudioClip clipToPlay = clip ?? audioSource.clip;

        if (clipToPlay == null)
        {
            Debug.LogError($"CaptionEnabledAudioSource on {gameObject.name}: No audio clip to play!");
            return;
        }

        // Set the clip if provided
        if (clip != null)
        {
            audioSource.clip = clip;
        }

        // Start audio
        audioSource.Play();

        // Setup captions manually if SRT provided
        if (srtFile != null && GlobalCaptionManager.Instance != null)
        {
            GlobalCaptionManager.Instance.StartCaptionsManually(
                audioSource,
                clipToPlay,
                srtFile,
                overridePrefab
            );
        }
    }

    /// <summary>
    /// Stop audio and captions
    /// </summary>
    public void StopWithCaptions()
    {
        audioSource.Stop();

        if (GlobalCaptionManager.Instance != null)
        {
            GlobalCaptionManager.Instance.StopCaptionsManually(audioSource);
        }
    }

    /// <summary>
    /// Check if this audio source currently has active captions
    /// </summary>
    public bool HasActiveCaptions()
    {
        if (GlobalCaptionManager.Instance == null) return false;
        return GlobalCaptionManager.Instance.HasActiveCaptions(audioSource);
    }

    /// <summary>
    /// Get the active caption canvas for this audio source
    /// </summary>
    public Canvas GetActiveCaptionCanvas()
    {
        if (GlobalCaptionManager.Instance == null) return null;
        return GlobalCaptionManager.Instance.GetCaptionCanvas(audioSource);
    }

    /// <summary>
    /// Set manual caption assignment
    /// </summary>
    public void SetManualCaptions(AudioClip clip, TextAsset srt, GameObject prefab = null)
    {
        useManualAssignment = true;
        manualAudioClip = clip;
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
        manualAudioClip = null;
        manualSRTFile = null;
    }

#if UNITY_EDITOR
    [ContextMenu("Test Caption Configuration")]
    private void TestCaptionConfiguration()
    {
        var entry = GetCaptionEntry();
        if (entry != null)
        {
            Debug.Log($"Caption configuration found for {gameObject.name}:\n" +
                     $"Audio Clip: {entry.audioClip?.name}\n" +
                     $"SRT File: {entry.srtFile?.name}\n" +
                     $"UI Type: {entry.uiType}\n" +
                     $"Prefab: {entry.captionPrefab?.name}\n" +
                     $"Target: {entry.customTarget?.name}");
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

}*/


using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CaptionEnabledAudioSource : MonoBehaviour
{
    [Header("Caption Configuration")]
    [SerializeField] private CaptionDatabase overrideDatabase;
    [SerializeField] private GameObject overridePrefab;

    [Header("Manual Caption Assignment")]
    [SerializeField] private bool useManualAssignment = false;
    [SerializeField] private AudioClip manualAudioClip;
    [SerializeField] private TextAsset manualSRTFile;

    [Header("Advanced Settings")]
    [SerializeField] private bool ignoreGlobalDatabase = false;
    [SerializeField] private bool autoRegisterOnStart = true;

    private AudioSource audioSource;
    private bool isRegistered = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
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
    /// Register this audio source with the global caption manager
    /// </summary>
    public void Register()
    {
        if (isRegistered) return;

        if (GlobalCaptionManager.Instance != null)
        {
            GlobalCaptionManager.Instance.RegisterAudioSource(this);
            isRegistered = true;
        }
        else
        {
            Debug.LogWarning($"CaptionEnabledAudioSource on {gameObject.name}: GlobalCaptionManager not found!");
        }
    }

    /// <summary>
    /// Unregister this audio source from the global caption manager
    /// </summary>
    public void Unregister()
    {
        if (!isRegistered) return;

        if (GlobalCaptionManager.Instance != null)
        {
            GlobalCaptionManager.Instance.UnregisterAudioSource(this);
        }

        isRegistered = false;
    }

    /// <summary>
    /// Get the caption configuration for the currently assigned audio clip
    /// </summary>
    public CaptionDatabase.CaptionEntry GetCaptionEntry()
    {
        AudioClip clipToCheck = useManualAssignment ? manualAudioClip : audioSource.clip;

        if (clipToCheck == null) return null;

        // Use manual assignment if configured
        if (useManualAssignment && manualSRTFile != null)
        {
            return new CaptionDatabase.CaptionEntry
            {
                audioClip = manualAudioClip,
                srtFile = manualSRTFile,
                captionPrefab = overridePrefab
            };
        }

        // Check override database first
        if (overrideDatabase != null)
        {
            var entry = overrideDatabase.GetEntryForClip(clipToCheck);
            if (entry != null)
            {
                // Apply local overrides to the database entry
                return ApplyLocalOverrides(entry);
            }
        }

        // Check global database if not ignoring it
        if (!ignoreGlobalDatabase && GlobalCaptionManager.Instance != null)
        {
            var globalEntry = GlobalCaptionManager.Instance.GetCaptionDatabase()?.GetEntryForClip(clipToCheck);
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
            audioClip = originalEntry.audioClip,
            srtFile = originalEntry.srtFile,
            captionPrefab = overridePrefab ?? originalEntry.captionPrefab
        };

        return modifiedEntry;
    }

    /// <summary>
    /// Manually play audio with captions
    /// </summary>
    public void PlayWithCaptions(AudioClip clip = null, TextAsset srtFile = null)
    {
        AudioClip clipToPlay = clip ?? audioSource.clip;

        if (clipToPlay == null)
        {
            Debug.LogError($"CaptionEnabledAudioSource on {gameObject.name}: No audio clip to play!");
            return;
        }

        // Set the clip if provided
        if (clip != null)
        {
            audioSource.clip = clip;
        }

        // Start audio
        audioSource.Play();

        // Setup captions manually if SRT provided
        if (srtFile != null && GlobalCaptionManager.Instance != null)
        {
            GlobalCaptionManager.Instance.StartCaptionsManually(
                audioSource,
                clipToPlay,
                srtFile,
                overridePrefab
            );
        }
    }

    /// <summary>
    /// Stop audio and captions
    /// </summary>
    public void StopWithCaptions()
    {
        audioSource.Stop();

        if (GlobalCaptionManager.Instance != null)
        {
            GlobalCaptionManager.Instance.StopCaptionsManually(audioSource);
        }
    }

    /// <summary>
    /// Check if this audio source currently has active captions
    /// </summary>
    public bool HasActiveCaptions()
    {
        if (GlobalCaptionManager.Instance == null) return false;
        return GlobalCaptionManager.Instance.HasActiveCaptions(audioSource);
    }

    /// <summary>
    /// Get the active caption canvas for this audio source
    /// </summary>
    public Canvas GetActiveCaptionCanvas()
    {
        if (GlobalCaptionManager.Instance == null) return null;
        return GlobalCaptionManager.Instance.GetCaptionCanvas(audioSource);
    }

    /// <summary>
    /// Set manual caption assignment
    /// </summary>
    public void SetManualCaptions(AudioClip clip, TextAsset srt, GameObject prefab = null)
    {
        useManualAssignment = true;
        manualAudioClip = clip;
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
        manualAudioClip = null;
        manualSRTFile = null;
    }

#if UNITY_EDITOR
    [ContextMenu("Test Caption Configuration")]
    private void TestCaptionConfiguration()
    {
        var entry = GetCaptionEntry();
        if (entry != null)
        {
            Debug.Log($"Caption configuration found for {gameObject.name}:\n" +
                     $"Audio Clip: {entry.audioClip?.name}\n" +
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