using UnityEngine;

[ExecuteAlways]
public class FogTransitionController : MonoBehaviour
{
    [Range(0f, 1f)]
    public float fogAmount = 0f;

    [Header("Fog Dome Materials")]
    public Material fogDomeMaterial;
    public Material clearPresetMaterial;
    public Material foggyPresetMaterial;

    [Header("Lighting Settings")]
    public Color clearShadowColor = new Color32(0x4A, 0x8C, 0x9F, 0xFF);
    public Color foggyShadowColor = new Color32(0x6B, 0x7B, 0x7A, 0xFF);

    public Color clearFogColor = new Color32(0x4A, 0x8E, 0xA1, 0xFF);
    public Color foggyFogColor = new Color32(0x67, 0x76, 0x75, 0xFF);

    [Header("Transition")]
    public float transitionDuration = 5f;
    private bool isTransitioning = false;
    private float targetFogAmount = 0f;

    void Update()
    {
        if (isTransitioning)
        {
            float delta = Time.deltaTime / Mathf.Max(transitionDuration, 0.01f);
            fogAmount = Mathf.MoveTowards(fogAmount, targetFogAmount, delta);

            if (Mathf.Approximately(fogAmount, targetFogAmount))
            {
                isTransitioning = false;
            }
        }

        UpdateFogDome();
        UpdateLightingSettings();
    }


    public void ToggleFog()
    {
        targetFogAmount = (fogAmount < 0.5f) ? 1f : 0f;
        isTransitioning = true;
    }

    public void SetFogClear()
    {
        fogAmount = 0f;
    }

    public void SetFogFoggy()
    {
        fogAmount = 1f;
    }

    void UpdateFogDome()
    {
        if (!fogDomeMaterial || !clearPresetMaterial || !foggyPresetMaterial)
            return;

        fogDomeMaterial.SetColor("_Color", Color.Lerp(
            clearPresetMaterial.GetColor("_Color"),
            foggyPresetMaterial.GetColor("_Color"),
            fogAmount));

        fogDomeMaterial.SetFloat("_FadeStart", Mathf.Lerp(
            clearPresetMaterial.GetFloat("_FadeStart"),
            foggyPresetMaterial.GetFloat("_FadeStart"),
            fogAmount));

        fogDomeMaterial.SetFloat("_FadeEnd", Mathf.Lerp(
            clearPresetMaterial.GetFloat("_FadeEnd"),
            foggyPresetMaterial.GetFloat("_FadeEnd"),
            fogAmount));

        fogDomeMaterial.SetFloat("_VerticalFadeStart", Mathf.Lerp(
            clearPresetMaterial.GetFloat("_VerticalFadeStart"),
            foggyPresetMaterial.GetFloat("_VerticalFadeStart"),
            fogAmount));

        fogDomeMaterial.SetFloat("_VerticalFadeEnd", Mathf.Lerp(
            clearPresetMaterial.GetFloat("_VerticalFadeEnd"),
            foggyPresetMaterial.GetFloat("_VerticalFadeEnd"),
            fogAmount));
    }

    void UpdateLightingSettings()
    {
        RenderSettings.fogColor = Color.Lerp(clearFogColor, foggyFogColor, fogAmount);

        if (RenderSettings.sun != null)
        {
            RenderSettings.sun.color = Color.Lerp(clearShadowColor, foggyShadowColor, fogAmount);
            RenderSettings.sun.shadowStrength = 1f;
        }
    }
}
