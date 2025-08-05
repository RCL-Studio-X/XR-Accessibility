using UnityEngine;
using System.Collections.Generic;
using HurricaneVR.Framework.Core.Player;
using TMPro;

public class CentralUIManager : MonoBehaviour
{
    [Header("Settings")]
    public float openingUIDisplayTime = 3f;

    [Header("UIs")]
    public Canvas openingUI;
    //public Canvas dialogueUI;
    public Canvas letterUI;
    public Canvas endingUI;
    public Canvas objectiveUI;
    public TextMeshProUGUI objectiveText;

    // Track active UIs and their behaviors
    private List<Canvas> activeUIs = new List<Canvas>();
    private List<IUIBehavior> activeBehaviors = new List<IUIBehavior>();

    #region Singleton Setup
    public static CentralUIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Start the UI sequence after a short delay to ensure everything is set up
            Invoke(nameof(StartUISequence), 0.5f);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void StartUISequence()
    {
        // Show opening UI first
        if (openingUI != null)
        {
            ShowUI(openingUI);
            // Schedule it to hide after the display time, then show objective UI
            Invoke(nameof(SwitchToObjectiveUI), openingUIDisplayTime);
        }
        else
        {
            // If no opening UI, just show objective UI directly
            ShowObjectiveUI();
        }
    }

    private void SwitchToObjectiveUI()
    {
        if (openingUI != null && IsUIActive(openingUI))
        {
            HideUI(openingUI);
        }

        ShowObjectiveUI();
    }

    private void ShowObjectiveUI()
    {
        if (objectiveUI != null)
        {
            ShowUI(objectiveUI);
        }
    }

    private void Update()
    {
        // Update all active UI behaviors
        for (int i = 0; i < activeBehaviors.Count; i++)
        {
            if (activeBehaviors[i] != null)
            {
                activeBehaviors[i].UpdateBehavior();
            }
        }

        // Testing input - remove later
        HandleTestingInput();
    }
    #endregion

    #region Public Methods
    public void ShowUI(Canvas uiCanvas)
    {
        if (uiCanvas == null) return;

        // Get the behavior component
        IUIBehavior behavior = uiCanvas.GetComponent<IUIBehavior>();

        if (behavior == null)
        {
            Debug.LogWarning($"Canvas {uiCanvas.name} doesn't have a UI behavior component!");
            return;
        }

        uiCanvas.gameObject.SetActive(true);

        // Track it
        if (!activeUIs.Contains(uiCanvas))
        {
            activeUIs.Add(uiCanvas);
            activeBehaviors.Add(behavior);
        }

        // Initialize the behavior
        behavior.OnUIShown();

        Debug.Log($"Showing UI: {uiCanvas.name}");
    }

    public void HideUI(Canvas uiCanvas)
    {
        if (uiCanvas == null) return;

        // Get the behavior component
        IUIBehavior behavior = uiCanvas.GetComponent<IUIBehavior>();

        if (behavior != null)
        {
            behavior.OnUIHidden();
        }

        // Hide the UI
        uiCanvas.gameObject.SetActive(false);

        // Remove from tracking
        int index = activeUIs.IndexOf(uiCanvas);
        if (index >= 0)
        {
            activeUIs.RemoveAt(index);
            activeBehaviors.RemoveAt(index);
        }

        Debug.Log($"Hiding UI: {uiCanvas.name}");
    }

    public void HideAllUIs()
    {
        // Create a copy to avoid modification during iteration
        var uisToHide = new List<Canvas>(activeUIs);

        foreach (Canvas ui in uisToHide)
        {
            // Don't hide persistent UIs like objective UI
            if (ui != objectiveUI)
            {
                HideUI(ui);
            }
        }
    }

    public bool IsAnyUIActive()
    {
        return activeUIs.Count > 0;
    }

    public bool IsUIActive(Canvas uiCanvas)
    {
        return activeUIs.Contains(uiCanvas);
    }

    // Check if a UI is persistent (shouldn't be hidden by HideAllUIs)
    public bool IsPersistentUI(Canvas uiCanvas)
    {
        return uiCanvas == objectiveUI;
    }

    // Method to get objective UI for external scripts to modify content later
    public Canvas GetObjectiveUI()
    {
        return objectiveUI;
    }

    //update objective text during the  game
    public void UpdateObjectiveText(string newObjective)
    {
        if (objectiveText != null)
        {
            objectiveText.text = newObjective;
        }
    }

    // Convenience methods for specific UI types
    public void ShowUIWithAutoHide(Canvas uiCanvas, float hideAfterSeconds)
    {
        ShowUI(uiCanvas);
        StartCoroutine(HideUIAfterDelay(uiCanvas, hideAfterSeconds));
    }

    private System.Collections.IEnumerator HideUIAfterDelay(Canvas uiCanvas, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (uiCanvas != null && IsUIActive(uiCanvas))
        {
            HideUI(uiCanvas);
        }
    }
    #endregion

    #region Testing - Remove in production
    [Header("Testing Keys")]
    public KeyCode testOpeningKey = KeyCode.F1;
    public KeyCode testLetterKey = KeyCode.F2;
    public KeyCode testEndingKey = KeyCode.F3;
    public KeyCode testHideAllKey = KeyCode.Escape;
    public KeyCode testRestartSequence = KeyCode.R;

    private void HandleTestingInput()
    {
        if (Input.GetKeyDown(testOpeningKey) && openingUI != null)
            ShowUIWithAutoHide(openingUI, openingUIDisplayTime);

        if (Input.GetKeyDown(testLetterKey) && letterUI != null)
            ShowUI(letterUI);

        if (Input.GetKeyDown(testEndingKey) && endingUI != null)
            ShowUI(endingUI);

        if (Input.GetKeyDown(testHideAllKey))
            HideAllUIs();

        if (Input.GetKeyDown(testRestartSequence))
            StartUISequence();
    }
    #endregion
}