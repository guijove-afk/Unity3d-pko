using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance;

    public GameObject popupPrefab;
    public Canvas worldCanvas;

    private void Awake()
    {
        Instance = this;
    }

    public void ShowDamage(int damage, Transform target, CombatFeedbackType feedbackType = CombatFeedbackType.Normal)
    {
        if (popupPrefab == null || worldCanvas == null || Camera.main == null || target == null)
            return;

        GameObject popup = Instantiate(popupPrefab, worldCanvas.transform);
        RectTransform rect = popup.GetComponent<RectTransform>();

        Vector3 worldPos = target.position + Vector3.up * 2f;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        RectTransform canvasRect = worldCanvas.transform as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            Camera.main,
            out Vector2 localPos
        );

        rect.localPosition = localPos;

        DamagePopup popupComponent = popup.GetComponent<DamagePopup>();
        if (popupComponent != null)
            popupComponent.Setup(damage, target, feedbackType);
    }
}
