using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class CharCaptionBehavior : MonoBehaviour, IUIBehavior
{
    [Header("Character Following")]
    public Transform characterTransform; // Will be auto-assigned to audio source transform
    public Vector3 offset = Vector3.up * 0.5f; // Offset above character
    public bool followCharacterMovement = true;

    [Header("Player Facing")]
    public bool facePlayer = true;
    public bool constrainToHorizontalRotation = true; // Only rotate on Y-axis

    [Header("Smooth Movement")]
    public bool smoothMovement = true;
    public float followSpeed = 5f;
    public float rotationSpeed = 8f;

    private Canvas canvas;
    private bool isActive = false;
    private Transform playerCamera;

    public bool IsActive => isActive;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();

        // Setup canvas for world space
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100; // Ensure it renders on top

        // Auto-find player camera
        FindPlayerCamera();
    }

    private void Start()
    {
        // If no character transform is assigned, try to get it from the caption system
        if (characterTransform == null)
        {
            TryAutoAssignCharacterTransform();
        }
    }

    private void FindPlayerCamera()
    {
        // Try multiple common camera setups
        if (playerCamera == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                playerCamera = mainCamera.transform;
            }
        }

        if (playerCamera == null)
        {
            Camera[] cameras = FindObjectsOfType<Camera>();
            foreach (var cam in cameras)
            {
                if (cam.tag == "MainCamera" || cam.name.ToLower().Contains("camera"))
                {
                    playerCamera = cam.transform;
                    break;
                }
            }
        }

        if (playerCamera == null)
        {
            Debug.LogWarning("SpeechBalloonBehavior: Could not find player camera automatically");
        }
    }

    private void TryAutoAssignCharacterTransform()
    {
        // Try to get the character transform from the parent caption system
        // This would be set by the GlobalCaptionManager when it creates the canvas
        var parentTransform = transform.parent;
        if (parentTransform != null && parentTransform.name.Contains("CaptionCanvas_"))
        {
            // The character name might be in the canvas name
            string characterName = parentTransform.name.Replace("CaptionCanvas_", "");
            GameObject characterObject = GameObject.Find(characterName);
            if (characterObject != null)
            {
                characterTransform = characterObject.transform;
                Debug.Log($"Auto-assigned character transform: {characterName}");
            }
        }
    }

    public void OnUIShown()
    {
        isActive = true;

        if (characterTransform != null)
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
        if (!isActive || characterTransform == null) return;

        UpdatePosition();

        if (facePlayer && playerCamera != null)
        {
            UpdateRotation();
        }
    }

    private void UpdatePosition()
    {
        if (!followCharacterMovement) return;

        // Calculate target position based on character position + offset
        Vector3 targetPosition = characterTransform.position + offset;

        // Apply distance constraints if player camera is available
        if (playerCamera != null)
        {
            Vector3 directionToPlayer = (playerCamera.position - targetPosition).normalized;
            float distanceToPlayer = Vector3.Distance(targetPosition, playerCamera.position);

        }

        // Apply position
        if (smoothMovement)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = targetPosition;
        }
    }

    private void UpdateRotation()
    {
        Vector3 directionToPlayer = (transform.position - playerCamera.position).normalized;

        Quaternion targetRotation;
        if (constrainToHorizontalRotation)
        {
            // Only rotate on Y-axis (horizontal rotation)
            directionToPlayer.y = 0;
            targetRotation = Quaternion.LookRotation(directionToPlayer);
        }
        else
        {
            // Full rotation toward player
            targetRotation = Quaternion.LookRotation(directionToPlayer);
        }

        if (smoothMovement)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            transform.rotation = targetRotation;
        }
    }

    /// <summary>
    /// Set the character transform that this speech balloon should follow
    /// This will typically be called by the GlobalCaptionManager
    /// </summary>
    public void SetCharacterTransform(Transform character)
    {
        characterTransform = character;

        if (isActive)
        {
            UpdatePosition();
        }
    }

    /// <summary>
    /// Set the player camera for facing behavior
    /// </summary>
    public void SetPlayerCamera(Transform camera)
    {
        playerCamera = camera;
    }

    /// <summary>
    /// Enable or disable player facing at runtime
    /// </summary>
    public void SetFacePlayer(bool shouldFace)
    {
        facePlayer = shouldFace;
    }

    /// <summary>
    /// Enable or disable character following at runtime
    /// </summary>
    public void SetFollowCharacter(bool shouldFollow)
    {
        followCharacterMovement = shouldFollow;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (characterTransform != null)
        {
            // Draw line from character to speech balloon
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(characterTransform.position, transform.position);

            // Draw offset visualization
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(characterTransform.position + offset, 0.1f);
        }

        if (playerCamera != null)
        {
            // Draw line to player
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, playerCamera.position);
        }
    }
#endif
}