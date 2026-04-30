using UnityEngine;
using UnityEngine.UI;

public class HPBarWorld : MonoBehaviour
{
    public Slider slider;
    public Transform target;
    public Vector3 offset = new Vector3(0, 2, 0);

    [Header("Visual")]
    public float smoothFrontSpeed = 10f;
    public float smoothBackSpeed = 2.5f;
    public float visibleDuration = 2.5f;
    public float fadeSpeed = 4f;
    public float fadeStartDistance = 12f;
    public float hideDistance = 22f;

    private Health health;
    private CanvasGroup canvasGroup;
    private Image frontFillImage;
    private Image delayedFillImage;
    private RectTransform fillAreaRect;
    private float targetRatio = 1f;
    private float frontRatio = 1f;
    private float backRatio = 1f;
    private float visibleUntil;

    void LateUpdate()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = target.position + offset;

        if (Camera.main != null)
            transform.forward = Camera.main.transform.forward;

        UpdateRatios();
        UpdateVisibility();
    }

    public void Bind(Health healthTarget, Transform followTarget)
    {
        health = healthTarget;
        target = followTarget;
        EnsureReferences();

        if (slider == null || health == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = health.MaxHp;
        targetRatio = health.MaxHp > 0 ? (float)health.Hp / health.MaxHp : 0f;
        frontRatio = targetRatio;
        backRatio = targetRatio;
        ApplyImmediateVisuals();
        SetAlpha(0f);
    }

    public void SetHealth(int hp, int maxHp)
    {
        EnsureReferences();

        if (slider == null || maxHp <= 0)
            return;

        slider.minValue = 0f;
        slider.maxValue = maxHp;
        targetRatio = Mathf.Clamp01((float)hp / maxHp);

        if (frontRatio < targetRatio)
            frontRatio = targetRatio;

        if (backRatio < targetRatio)
            backRatio = targetRatio;

        ApplyColor(targetRatio);
    }

    public void ShowForSeconds()
    {
        visibleUntil = Time.time + visibleDuration;
    }

    private void EnsureReferences()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        if (slider == null)
            slider = GetComponentInChildren<Slider>();

        if (slider == null)
            return;

        if (slider.handleRect != null)
            slider.handleRect.gameObject.SetActive(false);

        if (frontFillImage == null && slider.fillRect != null)
            frontFillImage = slider.fillRect.GetComponent<Image>();

        if (fillAreaRect == null && slider.fillRect != null)
            fillAreaRect = slider.fillRect.parent as RectTransform;

        if (delayedFillImage != null || fillAreaRect == null || frontFillImage == null)
            return;

        GameObject delayedFillObject = new GameObject("DelayedFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        delayedFillObject.transform.SetParent(fillAreaRect, false);
        delayedFillObject.transform.SetSiblingIndex(0);

        RectTransform delayedRect = delayedFillObject.GetComponent<RectTransform>();
        delayedRect.anchorMin = new Vector2(0f, 0f);
        delayedRect.anchorMax = new Vector2(0f, 1f);
        delayedRect.pivot = new Vector2(0f, 0.5f);
        delayedRect.anchoredPosition = Vector2.zero;
        delayedRect.sizeDelta = Vector2.zero;

        delayedFillImage = delayedFillObject.GetComponent<Image>();
        delayedFillImage.sprite = frontFillImage.sprite;
        delayedFillImage.type = Image.Type.Sliced;
        delayedFillImage.color = new Color(1f, 0.8f, 0.35f, 0.85f);
        delayedFillImage.raycastTarget = false;
    }

    private void UpdateRatios()
    {
        if (slider == null)
            return;

        frontRatio = Mathf.MoveTowards(frontRatio, targetRatio, smoothFrontSpeed * Time.deltaTime);
        backRatio = Mathf.MoveTowards(backRatio, targetRatio, smoothBackSpeed * Time.deltaTime);

        slider.value = slider.maxValue * frontRatio;
        ApplyDelayedFill();
        ApplyColor(frontRatio);
    }

    private void ApplyImmediateVisuals()
    {
        if (slider == null)
            return;

        slider.value = slider.maxValue * frontRatio;
        ApplyDelayedFill();
        ApplyColor(frontRatio);
    }

    private void ApplyDelayedFill()
    {
        if (delayedFillImage == null || fillAreaRect == null)
            return;

        RectTransform delayedRect = delayedFillImage.rectTransform;
        delayedRect.sizeDelta = new Vector2(fillAreaRect.rect.width * backRatio, 0f);
    }

    private void ApplyColor(float ratio)
    {
        if (frontFillImage == null)
            return;

        Color color;
        if (ratio > 0.6f)
            color = Color.Lerp(new Color(0.9f, 0.9f, 0.2f), new Color(0.15f, 0.85f, 0.35f), Mathf.InverseLerp(0.6f, 1f, ratio));
        else if (ratio > 0.3f)
            color = Color.Lerp(new Color(0.95f, 0.3f, 0.2f), new Color(0.95f, 0.85f, 0.2f), Mathf.InverseLerp(0.3f, 0.6f, ratio));
        else
            color = Color.Lerp(new Color(0.55f, 0f, 0f), new Color(0.95f, 0.2f, 0.2f), Mathf.InverseLerp(0f, 0.3f, ratio));

        frontFillImage.color = color;
    }

    private void UpdateVisibility()
    {
        if (canvasGroup == null)
            return;

        float targetAlpha = Time.time <= visibleUntil ? 1f : 0f;

        if (Camera.main != null)
        {
            float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
            float distanceAlpha = 1f - Mathf.InverseLerp(fadeStartDistance, hideDistance, distance);
            targetAlpha *= Mathf.Clamp01(distanceAlpha);
        }

        float nextAlpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        SetAlpha(nextAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = alpha;
        canvasGroup.blocksRaycasts = alpha > 0.01f;
        canvasGroup.interactable = false;
    }
}
