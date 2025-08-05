using UnityEngine;
using System.Collections.Generic;
using HurricaneVR.Framework.Core.Player;
using HurricaneVR.Framework.ControllerInput;

public class HMD_UIManager : MonoBehaviour
{
    [Header("XR Rig Reference")]
    public Transform xrRigTransform;
    public Transform cameraTransform;

    [Header("Hurricane VR References")]
    public HVRPlayerController playerController;
    public HVRPlayerInputs playerInputs;

    [Header("UI References")]
    public Canvas openingUICanvas;
    public Canvas dialogueUICanvas;
    public Canvas letterEventUICanvas;

    [Header("UI Positioning Offsets")]
    public Vector3 openingUIOffset = Vector3.forward * 2f; // 2 units in front
    public Vector3 dialogueUIOffset = new Vector3(0f, -0.5f, 2f); // Bottom of FOV
    public Vector3 eventUIOffset = new Vector3(0f, -0.5f, 2f); // Close reading distance

    [Header("Follow Settings")]
    public bool smoothFollow = true;
    public float followSpeed = 5f;

    [Header("Settings")]
    public float openingUIDisplayTime = 3f;

    // Current active UI tracking
    private Canvas currentActiveUI;
    private UIType currentUIType = UIType.None;

    // Letter UI specific tracking
    private bool isLetterUIActive = false;
    private Vector3 fixedLetterPosition;
    private Quaternion fixedLetterRotation;

    // Player state backup for pause functionality
    private float originalTimeScale;
    private bool wasPlayerMovementEnabled;

    public enum UIType
    {
        None,
        Opening,
        Dialogue,
        LetterEvent
    }

    #region Singleton Setup
    public static HMD_UIManager Instance { get; private set; }

    private void Awake()
    {
        // Simple singleton pattern
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

        // Initialize - hide all UIs at start
        HideAllUIs();

        // Validate references
        ValidateReferences();

        // Store original time scale
        originalTimeScale = Time.timeScale;
    }

    private void Update()
    {
        // Update UI positioning if there is an active UI
        if (currentActiveUI != null && cameraTransform != null)
        {
            // Only update position if it's not the letter UI (which should stay fixed)
            if (currentUIType != UIType.LetterEvent)
            {
                UpdateUIPosition(currentActiveUI, currentUIType);
            }
        }

        // Testing keys
        HandleTestingInput();
    }
    #endregion

    #region Public Methods
    public void ShowOpeningUI()
    {
        ShowUI(UIType.Opening);
        // Auto-hide after specified time
        Invoke(nameof(HideOpeningUI), openingUIDisplayTime);
    }

    public void ShowDialogueUI(string dialogueText = "")
    {
        ShowUI(UIType.Dialogue);
        // TODO: Set dialogue text and UI duration when we implement the dialogue component
    }

    public void ShowLetterEventUI()
    {
        ShowUI(UIType.LetterEvent);
        PausePlayerMovement();
    }

    public void HideCurrentUI()
    {
        if (currentActiveUI != null)
        {
            // If we're hiding the letter UI, resume player movement
            if (currentUIType == UIType.LetterEvent)
            {
                ResumePlayerMovement();
            }

            currentActiveUI.gameObject.SetActive(false);
            currentActiveUI = null;
            currentUIType = UIType.None;
            isLetterUIActive = false;
        }
    }

    public bool IsUIActive()
    {
        return currentUIType != UIType.None;
    }

    public UIType GetCurrentUIType()
    {
        return currentUIType;
    }

    // Public method for UI buttons to call when exiting letter
    public void OnLetterUIExitButtonClicked()
    {
        if (currentUIType == UIType.LetterEvent)
        {
            HideCurrentUI();
        }
    }
    #endregion

    #region Private Methods
    private void ShowUI(UIType uiType)
    {
        // Hide current UI first
        HideCurrentUI();

        // Show the requested UI
        Canvas uiToShow = GetUICanvas(uiType);
        if (uiToShow != null)
        {
            uiToShow.gameObject.SetActive(true);
            currentActiveUI = uiToShow;
            currentUIType = uiType;

            // Handle special positioning for letter UI
            if (uiType == UIType.LetterEvent)
            {
                SetupLetterUIPosition(uiToShow);
                isLetterUIActive = true;
            }
            else
            {
                // Position the UI immediately for non-letter UIs
                UpdateUIPosition(uiToShow, uiType);
            }

            //Debug.Log($"Showing UI: {uiType}");
        }
        else
        {
            Debug.LogWarning($"UI Canvas for {uiType} is not assigned!");
        }
    }

    private Canvas GetUICanvas(UIType uiType)
    {
        switch (uiType)
        {
            case UIType.Opening: return openingUICanvas;
            case UIType.Dialogue: return dialogueUICanvas;
            case UIType.LetterEvent: return letterEventUICanvas;
            default: return null;
        }
    }

    private Vector3 GetUIOffset(UIType uiType)
    {
        switch (uiType)
        {
            case UIType.Opening: return openingUIOffset;
            case UIType.Dialogue: return dialogueUIOffset;
            case UIType.LetterEvent: return eventUIOffset;
            default: return Vector3.zero;
        }
    }

    private void UpdateUIPosition(Canvas uiCanvas, UIType uiType)
    {
        if (cameraTransform == null || uiCanvas == null) return;

        // Calculate target position based on camera position and rotation + offset
        Vector3 offset = GetUIOffset(uiType);
        Vector3 targetPosition = cameraTransform.position + cameraTransform.TransformDirection(offset);

        // Calculate target rotation (face the camera)
        Quaternion targetRotation = Quaternion.LookRotation(cameraTransform.forward, cameraTransform.up);

        if (smoothFollow)
        {
            // Smooth follow
            uiCanvas.transform.position = Vector3.Lerp(uiCanvas.transform.position, targetPosition, followSpeed * Time.deltaTime);
            uiCanvas.transform.rotation = Quaternion.Slerp(uiCanvas.transform.rotation, targetRotation, followSpeed * Time.deltaTime);
        }
        else
        {
            // Direct positioning
            uiCanvas.transform.position = targetPosition;
            uiCanvas.transform.rotation = targetRotation;
        }
    }

    private void SetupLetterUIPosition(Canvas letterCanvas)
    {
        if (cameraTransform == null || letterCanvas == null) return;

        // Position the letter UI in the center of the field of view at a comfortable reading distance
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 horizontalForward = new Vector3(cameraForward.x, 0f, cameraForward.z).normalized;

        // Position: camera position + horizontal offset, keep same Y as camera
        fixedLetterPosition = new Vector3(
            cameraTransform.position.x + horizontalForward.x * 3,
            cameraTransform.position.y, // Same Y as camera
            cameraTransform.position.z + horizontalForward.z * 3
        );

        fixedLetterRotation = Quaternion.LookRotation(horizontalForward, Vector3.up);

        // Set the fixed position immediately
        letterCanvas.transform.position = fixedLetterPosition;
        letterCanvas.transform.rotation = fixedLetterRotation;
    }

    private void PausePlayerMovement()
    {
        if (playerController != null)
        {
            // Backup current state
            wasPlayerMovementEnabled = playerController.enabled;

            // Disable player controller to prevent movement and turning
            playerController.enabled = false;

            Debug.Log("Player movement paused for Letter UI");
        }
        else
        {
            Debug.LogWarning("PlayerController reference not assigned - cannot pause movement");
        }
    }

    private void ResumePlayerMovement()
    {
        if (playerController != null)
        {
            // Restore player controller state
            playerController.enabled = wasPlayerMovementEnabled;

            Debug.Log("Player movement resumed after Letter UI");
        }
    }

    private void HideAllUIs()
    {
        if (openingUICanvas != null) openingUICanvas.gameObject.SetActive(false);
        if (dialogueUICanvas != null) dialogueUICanvas.gameObject.SetActive(false);
        if (letterEventUICanvas != null) letterEventUICanvas.gameObject.SetActive(false);
    }

    private void HideOpeningUI()
    {
        if (currentUIType == UIType.Opening)
        {
            HideCurrentUI();
        }
    }

    private void ValidateReferences()
    {
        if (xrRigTransform == null || cameraTransform == null)
        {
            Debug.LogError("XR Rig or Camera Transform not assigned to HMD_UI_Manager!");
        }

        if (playerController == null)
        {
            Debug.LogWarning("PlayerController not assigned - Letter UI pause functionality won't work");
        }
    }
    #endregion

    #region Testing - Remove in production
    [Header("Testing (Remove Later)")]
    public KeyCode testOpeningKey = KeyCode.Alpha1;
    public KeyCode testDialogueKey = KeyCode.Alpha2;
    public KeyCode testEventKey = KeyCode.Alpha3;
    public KeyCode testHideKey = KeyCode.Escape;

    private void HandleTestingInput()
    {
        // Simple keyboard testing - remove when you have proper triggers
        if (Input.GetKeyDown(testOpeningKey)) ShowOpeningUI();
        if (Input.GetKeyDown(testDialogueKey)) ShowDialogueUI();
        if (Input.GetKeyDown(testEventKey)) ShowLetterEventUI();
        if (Input.GetKeyDown(testHideKey)) HideCurrentUI();
    }
    #endregion
}