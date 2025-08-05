using UnityEngine;
using HurricaneVR.Framework.Core.Player;

[RequireComponent(typeof(Canvas))]
public class EventUIBehavior : MonoBehaviour, IUIBehavior
{
    [Header("Positioning")]
    public float distanceFromCamera = 3f;
    public Vector3 additionalOffset = Vector3.zero;

    [Header("Player Control")]
    public HVRPlayerController playerController;

    [Header("Visual Effects (optional)")]
    public bool enableDarkenEffect = false;
    // TODO: Add darken effect components here later

    [Header("References")]
    public Transform cameraTransform;

    private Canvas canvas;
    private bool isActive = false;
    private bool wasPlayerEnabled = false;

    public bool IsActive => isActive;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();

        // Set canvas to always render on top
        SetupUILayer();

        // Auto-find references if not assigned
        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                cameraTransform = mainCamera.transform;
        }

        if (playerController == null)
        {
            playerController = FindObjectOfType<HVRPlayerController>();
        }
    }

    private void SetupUILayer()
    {
        // Put this GameObject on UI layer
        gameObject.layer = LayerMask.NameToLayer("UI");

        // Set all child objects to UI layer too
        Transform[] children = GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            child.gameObject.layer = LayerMask.NameToLayer("UI");
        }

        // Ensure canvas stays in World Space but renders on top
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 1000;
    }

    public void OnUIShown()
    {
        isActive = true;

        // Position the UI immediately
        if (cameraTransform != null)
        {
            SetFixedPosition();
        }

        // Pause player movement
        PausePlayer();

        // TODO: Enable visual effects if configured
        if (enableDarkenEffect)
        {
            // Implement darken effect later
        }
    }

    public void OnUIHidden()
    {
        isActive = false;

        // Resume player movement
        ResumePlayer();

        // TODO: Disable visual effects
        if (enableDarkenEffect)
        {
            // Disable darken effect later
        }
    }

    public void UpdateBehavior()
    {

    }

    private void SetFixedPosition()
    {
        // Get camera's horizontal forward direction (project onto X-Z plane)
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 horizontalForward = new Vector3(cameraForward.x, 0f, cameraForward.z).normalized;

        // Position: camera position + horizontal offset + additional offset, keep same Y as camera
        Vector3 targetPosition = new Vector3(
            cameraTransform.position.x + horizontalForward.x * distanceFromCamera,
            cameraTransform.position.y, // Same Y as camera
            cameraTransform.position.z + horizontalForward.z * distanceFromCamera
        );

        // Apply additional offset
        targetPosition += additionalOffset;

        // Create rotation that only follows Y-axis
        Quaternion targetRotation = Quaternion.LookRotation(horizontalForward, Vector3.up);

        // Set the fixed position immediately
        transform.position = targetPosition;
        transform.rotation = targetRotation;
    }

    private void PausePlayer()
    {
        if (playerController != null)
        {
            wasPlayerEnabled = playerController.enabled;
            playerController.enabled = false;
            Debug.Log("Player movement paused");
        }
        else
        {
            Debug.LogWarning("PlayerController not assigned - cannot pause movement");
        }
    }

    private void ResumePlayer()
    {
        if (playerController != null)
        {
            playerController.enabled = wasPlayerEnabled;
            Debug.Log("Player movement resumed");
        }
    }

    // Public method for UI buttons to call
    public void OnExitButtonClicked()
    {
        // Hide this UI (which will trigger OnUIHidden)
        if (CentralUIManager.Instance != null)
        {
            CentralUIManager.Instance.HideUI(canvas);
        }
    }
}