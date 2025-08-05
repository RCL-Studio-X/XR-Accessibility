// TransitionManager.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class TransitionManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Image gameIcon;
    public TextMeshProUGUI tipsText;
    public GameObject loadingIndicator; // Optional spinning icon

    [Header("Game Tips")]
    public string[] gameTips = {
        "Use teleportation to move around efficiently",
        "Look around to find hidden items",
        "Grab objects with your controllers",
        "Check your inventory regularly",
        "Save your progress frequently"
    };

    [Header("Settings")]
    public float tipDisplayDuration = 3f;
    public string targetSceneName;

    private AsyncOperation asyncLoad;
    private int currentTipIndex = 0;

    void Start()
    {
        // Get target scene from static variable or PlayerPrefs
        targetSceneName = TransitionHelper.TargetScene;

        StartCoroutine(LoadSceneAsync());
        StartCoroutine(ShowTipsRoutine());
    }

    IEnumerator LoadSceneAsync()
    {
        // Start loading the target scene
        asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        asyncLoad.allowSceneActivation = false; // Don't activate immediately

        // Wait until scene is almost loaded (90%)
        while (!asyncLoad.isDone)
        {
            // Scene loading progress (0-0.9)
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            // Optional: Update loading bar here
            // loadingBar.fillAmount = progress;

            // If loading is complete, wait for user input or minimum time
            if (asyncLoad.progress >= 0.9f)
            {
                // Wait a minimum time or until user is ready
                yield return new WaitForSeconds(1f);

                // Activate the scene
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    IEnumerator ShowTipsRoutine()
    {
        while (asyncLoad == null || !asyncLoad.isDone)
        {
            // Show current tip
            tipsText.text = gameTips[currentTipIndex];

            // Fade in animation (optional)
            yield return StartCoroutine(FadeInText());

            // Wait for display duration
            yield return new WaitForSeconds(tipDisplayDuration);

            // Fade out animation (optional)
            yield return StartCoroutine(FadeOutText());

            // Move to next tip
            currentTipIndex = (currentTipIndex + 1) % gameTips.Length;
        }
    }

    IEnumerator FadeInText()
    {
        Color color = tipsText.color;
        for (float t = 0; t < 1; t += Time.deltaTime * 2f)
        {
            color.a = t;
            tipsText.color = color;
            yield return null;
        }
        color.a = 1;
        tipsText.color = color;
    }

    IEnumerator FadeOutText()
    {
        Color color = tipsText.color;
        for (float t = 1; t > 0; t -= Time.deltaTime * 2f)
        {
            color.a = t;
            tipsText.color = color;
            yield return null;
        }
        color.a = 0;
        tipsText.color = color;
    }
}