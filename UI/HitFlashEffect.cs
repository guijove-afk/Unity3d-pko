using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class HitFlashEffect : MonoBehaviour
{
    [SerializeField] private Color flashColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private float flashDuration = 0.12f;
    [SerializeField] private float flashIntensity = 0.85f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private Renderer cachedRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Color originalColor;
    private bool hasOriginalColor;
    private Coroutine flashRoutine;

    private void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();

        if (cachedRenderer != null && cachedRenderer.sharedMaterial != null)
        {
            if (cachedRenderer.sharedMaterial.HasProperty(BaseColorId))
            {
                originalColor = cachedRenderer.sharedMaterial.GetColor(BaseColorId);
                hasOriginalColor = true;
            }
            else if (cachedRenderer.sharedMaterial.HasProperty(ColorId))
            {
                originalColor = cachedRenderer.sharedMaterial.GetColor(ColorId);
                hasOriginalColor = true;
            }
        }
    }

    public void PlayFlash(float intensity = 1f)
    {
        if (cachedRenderer == null || !hasOriginalColor)
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine(Mathf.Clamp01(intensity)));
    }

    private IEnumerator FlashRoutine(float intensity)
    {
        SetColor(Color.Lerp(originalColor, flashColor, flashIntensity * intensity));
        yield return new WaitForSeconds(flashDuration);
        SetColor(originalColor);
        flashRoutine = null;
    }

    private void SetColor(Color color)
    {
        cachedRenderer.GetPropertyBlock(propertyBlock);

        if (cachedRenderer.sharedMaterial.HasProperty(BaseColorId))
            propertyBlock.SetColor(BaseColorId, color);
        else if (cachedRenderer.sharedMaterial.HasProperty(ColorId))
            propertyBlock.SetColor(ColorId, color);

        cachedRenderer.SetPropertyBlock(propertyBlock);
    }
}
