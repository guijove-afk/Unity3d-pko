using UnityEngine;

public class EnemySelection : MonoBehaviour
{
    [Header("Seleção Visual")]
    [SerializeField] private GameObject selectionCirclePrefab;
    [SerializeField] private float circleOffset = 0.05f;
    
    private GameObject selectionCircle;
    private bool isSelected;

    void Awake()
    {
        if (selectionCirclePrefab != null)
        {
            selectionCircle = Instantiate(selectionCirclePrefab, transform);
            selectionCircle.transform.localPosition = new Vector3(0, circleOffset, 0);
            selectionCircle.SetActive(false);
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selectionCircle != null)
        {
            selectionCircle.SetActive(selected);
        }
    }

    public bool IsSelected => isSelected;

    void OnDestroy()
    {
        if (selectionCircle != null)
            Destroy(selectionCircle);
    }
}