using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private float lifetime = 1.2f;
    [SerializeField] private float moveSpeed = 52f;
    [SerializeField] private float scalePunch = 0.18f;

    private TextMeshProUGUI damageText;
    private RectTransform rect;
    private float timer;
    private Vector3 baseScale;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        damageText = GetComponentInChildren<TextMeshProUGUI>();
        baseScale = transform.localScale;
    }

    public void Setup(int amount, Transform target, CombatFeedbackType feedbackType = CombatFeedbackType.Normal)
    {
        if (rect == null)
            rect = GetComponent<RectTransform>();

        if (damageText == null)
            damageText = GetComponentInChildren<TextMeshProUGUI>();

        if (damageText == null)
            return;

        timer = 0f;
        transform.localScale = baseScale;

        switch (feedbackType)
        {
            case CombatFeedbackType.Critical:
                damageText.text = "-" + amount + "!";
                damageText.color = new Color(1f, 0.25f, 0.25f, 1f);
                transform.localScale = baseScale * 1.3f;
                break;
            case CombatFeedbackType.Block:
                damageText.text = "BLOCK";
                damageText.color = new Color(0.55f, 0.8f, 1f, 1f);
                transform.localScale = baseScale * 1.05f;
                break;
            case CombatFeedbackType.Miss:
                damageText.text = "MISS";
                damageText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
                break;
            default:
                damageText.text = "-" + amount;
                damageText.color = new Color(1f, 0.35f, 0.35f, 1f);
                break;
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        transform.localPosition += Vector3.up * moveSpeed * Time.deltaTime;

        float normalized = Mathf.Clamp01(timer / lifetime);
        float alpha = 1f - normalized;
        float punch = 1f + Mathf.Sin(normalized * Mathf.PI) * scalePunch;

        transform.localScale = baseScale * punch;

        if (damageText != null)
        {
            Color color = damageText.color;
            color.a = alpha;
            damageText.color = color;
        }

        if (timer >= lifetime)
            Destroy(gameObject);
    }
}
