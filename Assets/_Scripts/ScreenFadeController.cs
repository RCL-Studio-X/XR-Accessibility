using UnityEngine;
using HurricaneVR.Framework.Core.Player;

public class ScreenFadeController : MonoBehaviour
{
    [Header("HVR Fade Settings")]
    public HVRCanvasFade canvasFade;       // Assign in Inspector
    [Range(0.00f, 1.00f)]
    public float targetFadeLevel = 0f;     // 0 = transparent, 1 = black
    public float fadeDuration = 1f;        // Seconds

    private float currentFadeLevel;

    void Update()
    {
        // Monitor and apply fade if the value has changed (Editor adjustment or runtime change)
        if (!Mathf.Approximately(currentFadeLevel, targetFadeLevel))
        {
            FadeTo(targetFadeLevel, fadeDuration);
            currentFadeLevel = targetFadeLevel;
        }
    }

    /// <summary>
    /// Call this to fade to a specific alpha (0f = clear, 1f = black) over a given duration.
    /// </summary>
    public void FadeTo(float alpha, float duration)
    {
        if (canvasFade == null) return;

        canvasFade.Speed = Mathf.Max(0.01f, 1f / Mathf.Max(duration, 0.01f));
        canvasFade.Fade(Mathf.Clamp01(alpha), canvasFade.Speed);
    }

    /// <summary>
    /// Toggle between fully clear and fully black.
    /// </summary>
    public void ToggleFade()
    {
        float newTarget = Mathf.Approximately(targetFadeLevel, 0f) ? 1f : 0f;
        FadeTo(newTarget, fadeDuration);
        targetFadeLevel = newTarget;
    }
}
