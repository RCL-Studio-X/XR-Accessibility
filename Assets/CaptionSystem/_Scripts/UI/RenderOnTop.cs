using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class RenderOnTop : MonoBehaviour
{
    [Header("Render Settings")]
    [SerializeField] private string uiLayerName = "UI";
    [SerializeField] private bool autoSetupOnAwake = true;

    private Canvas canvas;
    private Camera uiCamera;
    private bool isSetup = false;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();

        if (autoSetupOnAwake)
        {
            SetupAlwaysOnTop();
        }
    }

    private void OnDestroy()
    {
        // Clean up UI camera when canvas is destroyed
        if (uiCamera != null)
        {
            Destroy(uiCamera.gameObject);
        }
    }

    public void SetupAlwaysOnTop()
    {
        if (isSetup) return;

        // Step 1: Set this canvas and all children to UI layer
        SetUILayer();

        // Step 2: Create dedicated UI camera
        CreateUICamera();

        // Step 3: Configure canvas to use UI camera
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = uiCamera;

        isSetup = true;
    }

    private void SetUILayer()
    {
        int uiLayer = LayerMask.NameToLayer(uiLayerName);

        if (uiLayer == -1)
        {
            Debug.LogWarning($"Layer '{uiLayerName}' doesn't exist. Canvas will use default layer.");
            return;
        }

        // Set this GameObject and all children to UI layer
        gameObject.layer = uiLayer;
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            child.gameObject.layer = uiLayer;
        }
    }

    private void CreateUICamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found! AlwaysOnTopBehavior requires a main camera.");
            return;
        }

        // Create UI camera as child of main camera
        GameObject uiCameraObj = new GameObject($"UICamera_{gameObject.name}");
        uiCameraObj.transform.SetParent(mainCamera.transform);
        uiCameraObj.transform.localPosition = Vector3.zero;
        uiCameraObj.transform.localRotation = Quaternion.identity;

        uiCamera = uiCameraObj.AddComponent<Camera>();

        // Copy main camera settings
        uiCamera.CopyFrom(mainCamera);

        // Configure for UI only
        uiCamera.clearFlags = CameraClearFlags.Depth; // Don't clear, just depth
        uiCamera.cullingMask = 1 << LayerMask.NameToLayer(uiLayerName); // Only render UI layer
        uiCamera.depth = mainCamera.depth + 1; // Render after main camera

        // Disable audio listener (main camera has it)
        AudioListener listener = uiCamera.GetComponent<AudioListener>();
        if (listener != null)
        {
            Destroy(listener);
        }
    }

    /// <summary>
    /// Get the UI camera created by this behavior
    /// </summary>
    public Camera GetUICamera()
    {
        return uiCamera;
    }

#if UNITY_EDITOR
    [ContextMenu("Force Setup")]
    private void ForceSetup()
    {
        if (Application.isPlaying)
        {
            isSetup = false;
            SetupAlwaysOnTop();
        }
        else
        {
            Debug.Log("Force Setup only works in Play mode");
        }
    }
#endif
}