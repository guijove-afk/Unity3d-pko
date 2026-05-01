using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Vector3 offset = new Vector3(0, 2.2f, 0);
    
    private EnemyStats stats;
    private Camera mainCamera;
    
    void Awake()
    {
        stats = GetComponentInParent<EnemyStats>();
        mainCamera = Camera.main;
        
        if (canvas != null)
            canvas.worldCamera = mainCamera;
    }
    
    void OnEnable()
    {
        if (stats != null)
            stats.OnHealthUpdated += UpdateHealthBar;
    }
    
    void OnDisable()
    {
        if (stats != null)
            stats.OnHealthUpdated -= UpdateHealthBar;
    }
    
    void LateUpdate()
    {
        if (stats == null) return;
        
        // Posicionar acima do inimigo
        transform.position = stats.transform.position + offset;
        
        // Billboard - sempre olhar para a camera
        if (mainCamera != null)
            transform.rotation = mainCamera.transform.rotation;
    }
    
    private void UpdateHealthBar(int current, int max)
    {
        if (fillImage != null)
        {
            float percent = max > 0 ? (float)current / max : 0;
            fillImage.fillAmount = percent;
        }
        
        // Esconder se estiver morto
        if (current <= 0 && canvas != null)
            canvas.enabled = false;
    }
}