using UnityEngine;

public class MaterialAlphaFader : MonoBehaviour
{
    [Range(0f, 1f)]
    private float alpha = 0f; // Current alpha value (editable in Inspector)

    public Material transparentMaterial; // A preconfigured transparent URP material

    [Header("Fade Settings")]
    public float fadeDuration = 2f;

    private Material runtimeMaterial;
    private float targetAlpha;
    private bool isFading = false;

    void Start()
    {
        if (transparentMaterial == null) return;

        // Clone the transparent material to avoid changing the original
        runtimeMaterial = new Material(transparentMaterial);
        GetComponent<Renderer>().material = runtimeMaterial;

        // Initialize alpha
        SetAlpha(alpha);
    }

    void Update()
    {
        if (isFading)
        {
            float step = Time.deltaTime / Mathf.Max(fadeDuration, 0.01f);
            alpha = Mathf.MoveTowards(alpha, targetAlpha, step);

            SetAlpha(alpha);

            if (Mathf.Approximately(alpha, targetAlpha))
            {
                isFading = false;
            }
        }
    }

    public void FadeTo(float newAlpha)
    {
        targetAlpha = Mathf.Clamp01(newAlpha);
        isFading = true;
    }

    public void ToggleFade()
    {
        FadeTo(alpha < 0.5f ? 1f : 0f);
    }

    private void SetAlpha(float a)
    {
        if (runtimeMaterial == null) return;

        Color c = runtimeMaterial.color;
        c.a = Mathf.Clamp01(a);
        runtimeMaterial.color = c;
    }
}
