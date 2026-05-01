using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private EnemyStats enemyStats;
    [SerializeField] private Transform healthBarFill;  // Barra verde/vermelha (Scale X)
    [SerializeField] private TextMesh healthText;       // Opcional: "HP / MaxHP"

    private void Start()
    {
        if (enemyStats == null)
            enemyStats = GetComponentInParent<EnemyStats>();

        if (enemyStats != null)
        {
            // ✅ CORRETO: Action sem parâmetros
            enemyStats.OnHealthUpdated += UpdateHealthBar;
            enemyStats.OnDeath += OnEnemyDeath;
        }

        UpdateHealthBar();  // Inicializa
    }

    private void OnDestroy()
    {
        if (enemyStats != null)
        {
            enemyStats.OnHealthUpdated -= UpdateHealthBar;
            enemyStats.OnDeath -= OnEnemyDeath;
        }
    }

    /// <summary>
    /// ✅ SEM PARÂMETROS — compatível com Action
    /// </summary>
    private void UpdateHealthBar()
    {
            // 1. Verifica se as referências existem e se MaxHealth é válido para evitar divisão por zero
    if (enemyStats == null || enemyStats.MaxHealth <= 0) 
    {
        return; 
    }
        if (enemyStats == null) return;

        float healthPercent = (float)enemyStats.Health / enemyStats.MaxHealth;
        healthPercent = Mathf.Clamp01(healthPercent);

        // Atualiza escala da barra
        if (healthBarFill != null)
        {
            Vector3 scale = healthBarFill.localScale;
            scale.x = healthPercent;
            healthBarFill.localScale = scale;
        }

        // Atualiza texto
        if (healthText != null)
        {
            healthText.text = $"{enemyStats.Health} / {enemyStats.MaxHealth}";
        }

        // Cor da barra (verde → amarelo → vermelho)
        if (healthBarFill != null)
        {
            Renderer rend = healthBarFill.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = Color.Lerp(Color.red, Color.green, healthPercent);
            }
        }
    }

    private void OnEnemyDeath()
    {
        // Esconde a barra quando morre
        gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        // Billboard — sempre olha para a câmera
        if (Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up);
        }
    }
}