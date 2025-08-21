using UnityEngine;

/// <summary>
/// Simple setup component for the Caption System prefab
/// This ensures the GlobalCaptionManager is properly configured when the prefab is instantiated
/// </summary>
public class CaptionSystemSetup : MonoBehaviour
{
    [Header("Required Configuration")]
    [SerializeField] private CaptionDatabase defaultDatabase;

    [Header("Setup Options")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool createDatabaseIfMissing = true;

    [Header("Validation")]
    [SerializeField] private bool validateSetupOnStart = true;

    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupCaptionSystem();
        }

        if (validateSetupOnStart)
        {
            ValidateSetup();
        }
    }

    /// <summary>
    /// Setup the caption system with the assigned database
    /// </summary>
    public void SetupCaptionSystem()
    {
        var globalManager = GetComponent<GlobalCaptionManager>();
        if (globalManager == null)
        {
            Debug.LogError("CaptionSystemSetup: No GlobalCaptionManager found on this GameObject!");
            return;
        }

        // Create database if missing and option is enabled
        if (defaultDatabase == null && createDatabaseIfMissing)
        {
            defaultDatabase = CreateDefaultDatabase();
        }

        // Assign database to global manager
        if (defaultDatabase != null)
        {
            globalManager.SetCaptionDatabase(defaultDatabase);
            Debug.Log($"Caption system setup complete with database: {defaultDatabase.name}");
        }
        else
        {
            Debug.LogWarning("CaptionSystemSetup: No caption database assigned! Please assign one in the inspector or enable 'Create Database If Missing'.");
        }
    }

    /// <summary>
    /// Validate the current setup and log any issues
    /// </summary>
    public void ValidateSetup()
    {
        var globalManager = GetComponent<GlobalCaptionManager>();

        if (globalManager == null)
        {
            Debug.LogError("CaptionSystemSetup: GlobalCaptionManager component is missing!");
            return;
        }

        if (defaultDatabase == null)
        {
            Debug.LogWarning("CaptionSystemSetup: No caption database assigned!");
            return;
        }

        // Validate database first
        defaultDatabase.ValidateDatabase();

        // Get statistics
        int validEntries = 0;
        int totalEntries = 0;

        try
        {
            var allClips = defaultDatabase.GetAllAudioClips();
            totalEntries = allClips.Count;

            foreach (var clip in allClips)
            {
                if (defaultDatabase.GetSRTForClip(clip) != null)
                {
                    validEntries++;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error validating database: {e.Message}");
            return;
        }

        Debug.Log($"Caption System Validation:\n" +
                 $"- Database: {defaultDatabase.name}\n" +
                 $"- Total Entries: {totalEntries}\n" +
                 $"- Valid Entries: {validEntries}\n" +
                 $"- Default Prefab: {(defaultDatabase.defaultCaptionPrefab != null ? defaultDatabase.defaultCaptionPrefab.name : "MISSING!")}\n" +
                 $"- System Status: {(validEntries > 0 ? "Ready" : "Needs Configuration")}");

        if (defaultDatabase.defaultCaptionPrefab == null)
        {
            Debug.LogError("CaptionSystemSetup: Default caption prefab is missing! Please assign one in the database.");
        }
    }

    /// <summary>
    /// Create a default empty database
    /// </summary>
    private CaptionDatabase CreateDefaultDatabase()
    {
#if UNITY_EDITOR
        // Only create database in editor
        var database = ScriptableObject.CreateInstance<CaptionDatabase>();

        // Try to save it as an asset
        string path = "Assets/CaptionDatabase_Generated.asset";
        UnityEditor.AssetDatabase.CreateAsset(database, path);
        UnityEditor.AssetDatabase.SaveAssets();

        Debug.Log($"Created default caption database at: {path}");
        return database;
#else
        // In build, create runtime instance (won't persist)
        Debug.LogWarning("Creating runtime caption database - entries will not persist!");
        return ScriptableObject.CreateInstance<CaptionDatabase>();
#endif
    }

    /// <summary>
    /// Set the caption database at runtime
    /// </summary>
    public void SetCaptionDatabase(CaptionDatabase database)
    {
        defaultDatabase = database;

        var globalManager = GetComponent<GlobalCaptionManager>();
        if (globalManager != null)
        {
            globalManager.SetCaptionDatabase(database);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Setup Caption System")]
    private void EditorSetupCaptionSystem()
    {
        SetupCaptionSystem();
    }

    [ContextMenu("Validate Setup")]
    private void EditorValidateSetup()
    {
        ValidateSetup();
    }

    [ContextMenu("Create Default Database")]
    private void EditorCreateDefaultDatabase()
    {
        if (defaultDatabase == null)
        {
            defaultDatabase = CreateDefaultDatabase();
        }
        else
        {
            Debug.Log("Database already exists!");
        }
    }
#endif
}