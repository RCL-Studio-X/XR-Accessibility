/*using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "CaptionDatabase", menuName = "Caption Toolkit/Caption Database")]
public class CaptionDatabase : ScriptableObject
{
    [System.Serializable]
    public class CaptionEntry
    {
        [Header("Audio Configuration")]
        public AudioClip audioClip;
        public TextAsset srtFile;

        [Header("UI Configuration")]
        public GameObject captionPrefab;
        public CaptionUIType uiType = CaptionUIType.PlayerFollow;

        [Header("Advanced Settings")]
        public Transform customTarget; // For static objects or custom positioning
        public bool overrideGlobalSettings = false;

        [Header("Override Settings (when overrideGlobalSettings = true)")]
        public bool customShowCaptions = true;
        public bool customDestroyOnStop = true;

        /// <summary>
        /// Validate this entry for common issues
        /// </summary>
        public bool IsValid(out string errorMessage)
        {
            if (audioClip == null)
            {
                errorMessage = "Audio clip is missing";
                return false;
            }

            if (srtFile == null)
            {
                errorMessage = "SRT file is missing";
                return false;
            }

            errorMessage = "";
            return true;
        }
    }

    [Header("Database Entries")]
    [SerializeField] private List<CaptionEntry> captionEntries = new List<CaptionEntry>();

    [Header("Default Settings")]
    public GameObject defaultCaptionPrefab;
    public CaptionUIType defaultUIType = CaptionUIType.PlayerFollow;

    // Dictionary for fast lookup (built at runtime)
    private Dictionary<AudioClip, CaptionEntry> clipToEntryMap;
    private bool isInitialized = false;

    /// <summary>
    /// Initialize the database for fast lookups
    /// </summary>
    private void InitializeDatabase()
    {

        if (isInitialized) return;


        clipToEntryMap = new Dictionary<AudioClip, CaptionEntry>();

        Debug.Log($"[DEBUG] Initializing database. captionEntries count: {captionEntries?.Count ?? -1}");


        if (captionEntries != null)
        {
            foreach (var entry in captionEntries)
            {
                if (entry.audioClip != null)
                {
                    if (clipToEntryMap.ContainsKey(entry.audioClip))
                    {
                        Debug.LogWarning($"CaptionDatabase: Duplicate audio clip entry found for '{entry.audioClip.name}'. Using first entry.");
                    }
                    else
                    {
                        clipToEntryMap[entry.audioClip] = entry;

                        Debug.Log($"[DEBUG] Added clip to map: {entry.audioClip.name}");

                    }
                }
            }
        }

        isInitialized = true;
        Debug.Log($"CaptionDatabase initialized with {clipToEntryMap.Count} entries");
    }

    /// <summary>
    /// Get caption entry for a specific audio clip
    /// </summary>
    public CaptionEntry GetEntryForClip(AudioClip audioClip)
    {
        if (audioClip == null) return null;

        InitializeDatabase();

        if (clipToEntryMap == null) return null;

        clipToEntryMap.TryGetValue(audioClip, out CaptionEntry entry);
        return entry;
    }

    /// <summary>
    /// Get SRT file for a specific audio clip
    /// </summary>
    public TextAsset GetSRTForClip(AudioClip audioClip)
    {
        var entry = GetEntryForClip(audioClip);
        return entry?.srtFile;
    }

    /// <summary>
    /// Get caption prefab for a specific audio clip (with fallback to default)
    /// </summary>
    public GameObject GetPrefabForClip(AudioClip audioClip)
    {
        var entry = GetEntryForClip(audioClip);
        return entry?.captionPrefab ?? defaultCaptionPrefab;
    }

    /// <summary>
    /// Check if an audio clip has caption data
    /// </summary>
    public bool HasCaptionForClip(AudioClip audioClip)
    {
        return GetEntryForClip(audioClip) != null;
    }

    /// <summary>
    /// Get all registered audio clips
    /// </summary>
    public List<AudioClip> GetAllAudioClips()
    {

        if (clipToEntryMap == null) isInitialized = false;

        Debug.Log($"[DEBUG] GetAllAudioClips called. isInitialized: {isInitialized}");
        InitializeDatabase();
        Debug.Log($"[DEBUG] After InitializeDatabase. clipToEntryMap null: {clipToEntryMap == null}");

        return clipToEntryMap != null ? new List<AudioClip>(clipToEntryMap.Keys) : new List<AudioClip>();
    }

    /// <summary>
    /// Add a new caption entry at runtime
    /// </summary>
    public void AddEntry(AudioClip audioClip, TextAsset srtFile, GameObject prefab = null, CaptionUIType uiType = CaptionUIType.PlayerFollow)
    {
        if (audioClip == null || srtFile == null)
        {
            Debug.LogError("Cannot add caption entry with null audio clip or SRT file");
            return;
        }

        var newEntry = new CaptionEntry
        {
            audioClip = audioClip,
            srtFile = srtFile,
            captionPrefab = prefab ?? defaultCaptionPrefab,
            uiType = uiType
        };

        if (captionEntries == null)
        {
            captionEntries = new List<CaptionEntry>();
        }

        captionEntries.Add(newEntry);

        // Update runtime dictionary
        InitializeDatabase(); // This will recreate the dictionary if needed
        if (clipToEntryMap != null)
        {
            clipToEntryMap[audioClip] = newEntry;
        }

        Debug.Log($"Added caption entry for audio clip: {audioClip.name}");
    }

    /// <summary>
    /// Remove caption entry for an audio clip
    /// </summary>
    public bool RemoveEntry(AudioClip audioClip)
    {
        if (audioClip == null) return false;

        if (captionEntries == null) return false;

        var entryToRemove = captionEntries.FirstOrDefault(e => e.audioClip == audioClip);
        if (entryToRemove != null)
        {
            captionEntries.Remove(entryToRemove);

            // Update runtime dictionary
            InitializeDatabase(); // Ensure dictionary exists
            if (clipToEntryMap != null && clipToEntryMap.ContainsKey(audioClip))
            {
                clipToEntryMap.Remove(audioClip);
            }

            Debug.Log($"Removed caption entry for audio clip: {audioClip.name}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Validate all entries in the database
    /// </summary>
    public void ValidateDatabase()
    {
        int validEntries = 0;
        int invalidEntries = 0;

        if (captionEntries == null)
        {
            Debug.LogWarning("Caption database has no entries list initialized");
            return;
        }

        foreach (var entry in captionEntries)
        {
            if (entry != null && entry.IsValid(out string errorMessage))
            {
                validEntries++;
            }
            else
            {
                invalidEntries++;
                Debug.LogWarning($"Invalid caption entry: (Audio: {entry?.audioClip?.name ?? "null"})");
            }
        }

        Debug.Log($"Caption Database Validation: {validEntries} valid entries, {invalidEntries} invalid entries");
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only method to refresh the database
    /// </summary>
    [ContextMenu("Refresh Database")]
    public void RefreshDatabase()
    {
        isInitialized = false;
        InitializeDatabase();
        ValidateDatabase();
    }

    /// <summary>
    /// Editor-only method to clear all entries
    /// </summary>
    [ContextMenu("Clear All Entries")]
    public void ClearAllEntries()
    {
        captionEntries.Clear();
        clipToEntryMap?.Clear();
        isInitialized = false;
        Debug.Log("Caption database cleared");
    }
#endif
}
*/



using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "CaptionDatabase", menuName = "Caption Toolkit/Caption Database")]
public class CaptionDatabase : ScriptableObject
{
    [System.Serializable]
    public class CaptionEntry
    {
        [Header("Audio Configuration")]
        public AudioClip audioClip;
        public TextAsset srtFile;

        [Header("UI Configuration")]
        public GameObject captionPrefab; // Optional override

        /// <summary>
        /// Validate this entry for common issues
        /// </summary>
        public bool IsValid(out string errorMessage)
        {
            if (audioClip == null)
            {
                errorMessage = "Audio clip is missing";
                return false;
            }

            if (srtFile == null)
            {
                errorMessage = "SRT file is missing";
                return false;
            }

            errorMessage = "";
            return true;
        }
    }

    [Header("Database Entries")]
    [SerializeField] private List<CaptionEntry> captionEntries = new List<CaptionEntry>();

    [Header("Default Settings")]
    public GameObject defaultCaptionPrefab;

    // Dictionary for fast lookup (built at runtime)
    private Dictionary<AudioClip, CaptionEntry> clipToEntryMap;
    private bool isInitialized = false;

    /// <summary>
    /// Initialize the database for fast lookups
    /// </summary>
    private void InitializeDatabase()
    {
        if (isInitialized) return;

        clipToEntryMap = new Dictionary<AudioClip, CaptionEntry>();

        Debug.Log($"[DEBUG] Initializing database. captionEntries count: {captionEntries?.Count ?? -1}");

        if (captionEntries != null)
        {
            foreach (var entry in captionEntries)
            {
                if (entry.audioClip != null)
                {
                    if (clipToEntryMap.ContainsKey(entry.audioClip))
                    {
                        Debug.LogWarning($"CaptionDatabase: Duplicate audio clip entry found for '{entry.audioClip.name}'. Using first entry.");
                    }
                    else
                    {
                        clipToEntryMap[entry.audioClip] = entry;
                        Debug.Log($"[DEBUG] Added clip to map: {entry.audioClip.name}");
                    }
                }
            }
        }

        isInitialized = true;
        Debug.Log($"CaptionDatabase initialized with {clipToEntryMap.Count} entries");
    }

    /// <summary>
    /// Get caption entry for a specific audio clip
    /// </summary>
    public CaptionEntry GetEntryForClip(AudioClip audioClip)
    {
        if (audioClip == null) return null;

        InitializeDatabase();

        if (clipToEntryMap == null) return null;

        clipToEntryMap.TryGetValue(audioClip, out CaptionEntry entry);
        return entry;
    }

    /// <summary>
    /// Get SRT file for a specific audio clip
    /// </summary>
    public TextAsset GetSRTForClip(AudioClip audioClip)
    {
        var entry = GetEntryForClip(audioClip);
        return entry?.srtFile;
    }

    /// <summary>
    /// Get caption prefab for a specific audio clip (with fallback to default)
    /// </summary>
    public GameObject GetPrefabForClip(AudioClip audioClip)
    {
        var entry = GetEntryForClip(audioClip);
        return entry?.captionPrefab ?? defaultCaptionPrefab;
    }

    /// <summary>
    /// Check if an audio clip has caption data
    /// </summary>
    public bool HasCaptionForClip(AudioClip audioClip)
    {
        return GetEntryForClip(audioClip) != null;
    }

    /// <summary>
    /// Get all registered audio clips
    /// </summary>
    public List<AudioClip> GetAllAudioClips()
    {
        if (clipToEntryMap == null) isInitialized = false;

        InitializeDatabase();
        return clipToEntryMap != null ? new List<AudioClip>(clipToEntryMap.Keys) : new List<AudioClip>();
    }

    /// <summary>
    /// Add a new caption entry at runtime
    /// </summary>
    public void AddEntry(AudioClip audioClip, TextAsset srtFile, GameObject prefab = null)
    {
        if (audioClip == null || srtFile == null)
        {
            Debug.LogError("Cannot add caption entry with null audio clip or SRT file");
            return;
        }

        var newEntry = new CaptionEntry
        {
            audioClip = audioClip,
            srtFile = srtFile,
            captionPrefab = prefab ?? defaultCaptionPrefab
        };

        if (captionEntries == null)
        {
            captionEntries = new List<CaptionEntry>();
        }

        captionEntries.Add(newEntry);

        // Update runtime dictionary
        InitializeDatabase(); // This will recreate the dictionary if needed
        if (clipToEntryMap != null)
        {
            clipToEntryMap[audioClip] = newEntry;
        }

        Debug.Log($"Added caption entry for audio clip: {audioClip.name}");
    }

    /// <summary>
    /// Remove caption entry for an audio clip
    /// </summary>
    public bool RemoveEntry(AudioClip audioClip)
    {
        if (audioClip == null) return false;

        if (captionEntries == null) return false;

        var entryToRemove = captionEntries.FirstOrDefault(e => e.audioClip == audioClip);
        if (entryToRemove != null)
        {
            captionEntries.Remove(entryToRemove);

            // Update runtime dictionary
            InitializeDatabase(); // Ensure dictionary exists
            if (clipToEntryMap != null && clipToEntryMap.ContainsKey(audioClip))
            {
                clipToEntryMap.Remove(audioClip);
            }

            Debug.Log($"Removed caption entry for audio clip: {audioClip.name}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Validate all entries in the database
    /// </summary>
    public void ValidateDatabase()
    {
        int validEntries = 0;
        int invalidEntries = 0;

        if (captionEntries == null)
        {
            Debug.LogWarning("Caption database has no entries list initialized");
            return;
        }

        foreach (var entry in captionEntries)
        {
            if (entry != null && entry.IsValid(out string errorMessage))
            {
                validEntries++;
            }
            else
            {
                invalidEntries++;
                Debug.LogWarning($"Invalid caption entry: (Audio: {entry?.audioClip?.name ?? "null"})");
            }
        }

        Debug.Log($"Caption Database Validation: {validEntries} valid entries, {invalidEntries} invalid entries");
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only method to refresh the database
    /// </summary>
    [ContextMenu("Refresh Database")]
    public void RefreshDatabase()
    {
        isInitialized = false;
        InitializeDatabase();
        ValidateDatabase();
    }

    /// <summary>
    /// Editor-only method to clear all entries
    /// </summary>
    [ContextMenu("Clear All Entries")]
    public void ClearAllEntries()
    {
        captionEntries.Clear();
        clipToEntryMap?.Clear();
        isInitialized = false;
        Debug.Log("Caption database cleared");
    }
#endif
}