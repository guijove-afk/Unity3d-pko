using UnityEngine;
using Mirror;

public class EnemyStats : NetworkBehaviour, ICharacterStats
{
    [Header("Basic Stats")]
    [SyncVar] public string enemyName = "Monster";
    [SyncVar] public int level = 1;
    
    [Header("Health")]
    [SyncVar(hook = nameof(OnHealthChanged))] 
    private int _health;
    
    [SyncVar] 
    private int _maxHealth = 100;
    
    [Header("Combat")]
    [SyncVar] public int attack = 10;
    [SyncVar] public int defense = 5;
    [SyncVar] public float attackSpeed = 1f;
    [SyncVar] public float moveSpeed = 3f;
    [SyncVar] public float attackRange = 2f;
    
    [Header("Rewards")]
    public int expReward = 50;
    public int goldReward = 20;
    
    [Header("Visual")]
    public GameObject selectionIndicator;
    public Transform hitEffectSpawnPoint;
    
    // Interface ICharacterStats
    public int Health => _health;
    public int MaxHealth => _maxHealth;
    public bool IsDead => _health <= 0;
    
    // Events
    public event System.Action OnDeath;
    public event System.Action<int, int> OnHealthUpdated;
    
    void Start()
    {
        if (isServer)
        {
            _health = _maxHealth;
        }
        
        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);
    }
    
    [Server]
    public void Initialize(int enemyLevel)
    {
        level = enemyLevel;
        _maxHealth = 100 + (level * 20);
        attack = 10 + (level * 3);
        defense = 5 + (level * 2);
        _health = _maxHealth;
    }
    
    [Server]
    public void TakeDamage(int damage, uint attackerId, DamageType damageType = DamageType.Physical)
    {
        if (IsDead) return;
        
        int finalDamage = Mathf.Max(1, damage - defense);
        _health = Mathf.Max(0, _health - finalDamage);
        
        RpcOnDamageTaken(finalDamage);
        
        if (_health <= 0)
        {
            Die(attackerId);
        }
    }
    
    [Server]
    public void TakeTrueDamage(int damage, uint attackerId)
    {
        if (IsDead) return;
        _health = Mathf.Max(0, _health - damage);
        RpcOnDamageTaken(damage);
        if (_health <= 0) Die(attackerId);
    }
    
    [Server]
    private void Die(uint killerId)
    {
        OnDeath?.Invoke();
        RpcOnDeath();
        
        GiveRewards(killerId);
        StartCoroutine(DestroyAfterDelay(3f));
    }
    
    [Server]
    private void GiveRewards(uint killerId)
    {
        // Mirror v96+ - usa NetworkServer.connections com NetworkConnectionToClient
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn == null || conn.identity == null) continue;
            
            if (conn.identity.netId == killerId)
            {
                PlayerStats playerStats = conn.identity.GetComponent<PlayerStats>();
                if (playerStats != null)
                {
                    playerStats.AddExperience(expReward);
                    playerStats.AddGold(goldReward);
                }
                break;
            }
        }
    }
    
    private System.Collections.IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        NetworkServer.Destroy(gameObject);
    }
    
    [ClientRpc]
    private void RpcOnDamageTaken(int damage)
    {
        OnHealthUpdated?.Invoke(_health, _maxHealth);
        
        if (DamagePopupManager.Instance != null)
            DamagePopupManager.Instance.ShowDamage(transform.position + Vector3.up * 2f, damage, false);
    }
    
    [ClientRpc]
    private void RpcOnDeath()
    {
        OnHealthUpdated?.Invoke(0, _maxHealth);
        
        if (DamagePopupManager.Instance != null)
            DamagePopupManager.Instance.ShowText(transform.position + Vector3.up * 2f, "MORTO!", Color.red);
        
        if (TryGetComponent(out Collider col)) col.enabled = false;
        if (TryGetComponent(out UnityEngine.AI.NavMeshAgent agent)) agent.enabled = false;
        
        if (TryGetComponent(out Animator anim))
        {
            anim.SetTrigger("Die");
            anim.SetBool("IsDead", true);
        }
    }
    
    private void OnHealthChanged(int oldVal, int newVal)
    {
        OnHealthUpdated?.Invoke(newVal, _maxHealth);
    }
    
    public void SetSelected(bool selected)
    {
        if (selectionIndicator != null)
            selectionIndicator.SetActive(selected);
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}