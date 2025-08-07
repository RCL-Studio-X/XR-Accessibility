using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class ResizeCaptionUI : MonoBehaviour
{
    [Header("Resize Settings")]
    [SerializeField] private bool autoDetectTextComponent = true;
    [SerializeField] private TextMeshProUGUI textComponent;

    [Header("Size Constraints")]
    [SerializeField] private float minHeight = 50f;
    [SerializeField] private float maxHeight = 300f;
    [SerializeField] private float padding = 20f; // Extra space around text

    [Header("Animation (Optional)")]
    [SerializeField] private bool animateResize = false;
    [SerializeField] private float animationSpeed = 10f;

    private RectTransform rectTransform;
    private string lastTextContent = "";
    private float targetHeight;
    private bool isInitialized = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (autoDetectTextComponent && textComponent == null)
        {
            // Try to find TextMeshPro component in this object or children
            textComponent = GetComponentInChildren<TextMeshProUGUI>();

            if (textComponent == null)
            {
                Debug.LogWarning($"ContentResponsivePanel on {gameObject.name}: No TextMeshProUGUI component found!");
            }
        }
    }

    private void Start()
    {
        // Initialize with current size if no text content yet
        if (textComponent != null && string.IsNullOrEmpty(textComponent.text))
        {
            targetHeight = rectTransform.sizeDelta.y;
        }

        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized || textComponent == null) return;

        // Check if text content has changed
        if (textComponent.text != lastTextContent)
        {
            UpdatePanelSize();
            lastTextContent = textComponent.text;
        }

        // Handle animated resizing
        if (animateResize && Mathf.Abs(rectTransform.sizeDelta.y - targetHeight) > 0.1f)
        {
            Vector2 currentSize = rectTransform.sizeDelta;
            currentSize.y = Mathf.Lerp(currentSize.y, targetHeight, animationSpeed * Time.deltaTime);
            rectTransform.sizeDelta = currentSize;
        }
    }

    private void UpdatePanelSize()
    {
        if (textComponent == null) return;

        // Force text to update its layout
        Canvas.ForceUpdateCanvases();
        textComponent.ForceMeshUpdate();

        // Get preferred height of the text
        float preferredHeight = textComponent.preferredHeight;

        // Add padding and apply constraints
        float newHeight = Mathf.Clamp(preferredHeight + padding, minHeight, maxHeight);

        if (animateResize)
        {
            targetHeight = newHeight;
        }
        else
        {
            // Resize immediately
            Vector2 currentSize = rectTransform.sizeDelta;
            currentSize.y = newHeight;
            rectTransform.sizeDelta = currentSize;
        }
    }

    /// <summary>
    /// Manually trigger a resize update (useful when text is changed externally)
    /// </summary>
    public void RefreshSize()
    {
        if (textComponent != null)
        {
            lastTextContent = ""; // Force update on next frame
        }
    }

    /// <summary>
    /// Set the text component to monitor (if not using auto-detection)
    /// </summary>
    public void SetTextComponent(TextMeshProUGUI textComp)
    {
        textComponent = textComp;
        autoDetectTextComponent = false;
        RefreshSize();
    }

    /// <summary>
    /// Update size constraints at runtime
    /// </summary>
    public void SetSizeConstraints(float minH, float maxH, float paddingAmount)
    {
        minHeight = minH;
        maxHeight = maxH;
        padding = paddingAmount;
        RefreshSize();
    }

    /// <summary>
    /// Get the current text component being monitored
    /// </summary>
    public TextMeshProUGUI GetTextComponent()
    {
        return textComponent;
    }

    /// <summary>
    /// Check if the panel is currently resizing (for animated mode)
    /// </summary>
    public bool IsResizing()
    {
        return animateResize && Mathf.Abs(rectTransform.sizeDelta.y - targetHeight) > 0.1f;
    }
}