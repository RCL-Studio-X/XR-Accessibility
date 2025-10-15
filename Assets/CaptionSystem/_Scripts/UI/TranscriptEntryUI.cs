using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// UI component for a single transcript entry
/// Handles display, highlighting, and click interactions
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TranscriptEntryUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timestampText;
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI captionText;
    [SerializeField] private Image backgroundImage;

    [Header("Auto-Find Settings")]
    [SerializeField] private bool autoFindComponents = true;

    private CaptionEntry captionData;
    private int entryIndex;
    private System.Action clickHandler;
    private bool isHighlighted = false;

    private void Awake()
    {
        if (autoFindComponents)
        {
            FindComponents();
        }
    }

    /// <summary>
    /// Auto-find text components by name
    /// </summary>
    private void FindComponents()
    {
        // Find all TextMeshPro components
        TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>();

        foreach (var text in allTexts)
        {
            string name = text.gameObject.name.ToLower();

            if (name.Contains("timestamp") || name.Contains("time"))
            {
                timestampText = text;
            }
            else if (name.Contains("speaker"))
            {
                speakerText = text;
            }
            else if (name.Contains("caption") || name.Contains("text") || name.Contains("dialogue"))
            {
                captionText = text;
            }
        }

        // Find background image
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        // Validate
        if (captionText == null)
        {
            Debug.LogWarning("TranscriptEntryUI: Could not find caption text component!");
        }
    }

    /// <summary>
    /// Setup the entry with caption data
    /// </summary>
    public void Setup(CaptionEntry caption, int index, Color textColor, Color backgroundColor)
    {
        captionData = caption;
        entryIndex = index;

        // Set timestamp
        if (timestampText != null)
        {
            timestampText.text = FormatTime(caption.startTime);
            timestampText.color = textColor;
        }

        // Set speaker
        if (speakerText != null)
        {
            speakerText.text = caption.speaker;
            speakerText.color = textColor;
        }

        // Set caption text
        if (captionText != null)
        {
            captionText.text = caption.text;
            captionText.color = textColor;
        }

        // Set background
        if (backgroundImage != null)
        {
            backgroundImage.color = backgroundColor;
        }
    }

    /// <summary>
    /// Set highlight state
    /// </summary>
    public void SetHighlight(bool highlighted, Color textColor, Color backgroundColor)
    {
        isHighlighted = highlighted;

        // Update text colors
        if (timestampText != null)
        {
            timestampText.color = textColor;
        }

        if (speakerText != null)
        {
            speakerText.color = textColor;
        }

        if (captionText != null)
        {
            captionText.color = textColor;
        }

        // Update background
        if (backgroundImage != null)
        {
            backgroundImage.color = backgroundColor;
        }

        // Optional: Add scale effect for highlighted entry
        if (highlighted)
        {
            transform.localScale = Vector3.one * 1.05f;
        }
        else
        {
            transform.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// Set click handler callback
    /// </summary>
    public void SetClickHandler(System.Action handler)
    {
        clickHandler = handler;
    }

    /// <summary>
    /// Handle pointer click event
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        clickHandler?.Invoke();
    }

    /// <summary>
    /// Format time in seconds to MM:SS format
    /// </summary>
    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    /// <summary>
    /// Get the caption data for this entry
    /// </summary>
    public CaptionEntry GetCaptionData()
    {
        return captionData;
    }

    /// <summary>
    /// Get the index of this entry in the transcript
    /// </summary>
    public int GetIndex()
    {
        return entryIndex;
    }

    /// <summary>
    /// Check if this entry is currently highlighted
    /// </summary>
    public bool IsHighlighted()
    {
        return isHighlighted;
    }

    /// <summary>
    /// Manually set text components if auto-find doesn't work
    /// </summary>
    public void SetTextComponents(TextMeshProUGUI timestamp, TextMeshProUGUI speaker, TextMeshProUGUI caption)
    {
        timestampText = timestamp;
        speakerText = speaker;
        captionText = caption;
    }

    /// <summary>
    /// Set background image component
    /// </summary>
    public void SetBackgroundImage(Image background)
    {
        backgroundImage = background;
    }
}