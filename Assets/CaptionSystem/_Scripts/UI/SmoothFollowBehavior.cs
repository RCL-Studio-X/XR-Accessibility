using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class SmoothFollowBehavior : MonoBehaviour, IUIBehavior
{
    [Header("Follow Settings")]
    public bool smoothFollow = true;
    public float followSpeed = 5f;

    [Header("Positioning")]
    public Vector3 offset = Vector3.forward * 2f;

    [Header("References")]
    public Transform targetTransform; // Usually the camera

    private Canvas canvas;
    private bool isActive = false;

    public bool IsActive => isActive;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();

        // Set canvas to always render on top
        SetupUILayer();

        // Auto-find camera if not assigned
        if (targetTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                targetTransform = mainCamera.transform;
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
        if (targetTransform != null)
        {
            // Position immediately when shown
            UpdatePosition();
        }
    }

    public void OnUIHidden()
    {
        isActive = false;
    }

    public void UpdateBehavior()
    {
        if (isActive && targetTransform != null)
        {
            UpdatePosition();
        }
    }

    private void UpdatePosition()
    {
        // Calculate target position based on camera position and rotation + offset
        Vector3 targetPosition = targetTransform.position + targetTransform.TransformDirection(offset);

        // Calculate target rotation (face the camera)
        Quaternion targetRotation = Quaternion.LookRotation(targetTransform.forward, targetTransform.up);

        if (smoothFollow)
        {
            // Smooth follow
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, followSpeed * Time.deltaTime);
        }
        else
        {
            // Direct positioning
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }
    }
}