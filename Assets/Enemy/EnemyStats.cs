using UnityEngine;
using Mirror;
using System;

// REMOVIDO: [RequireComponent(typeof(Animator))] — o Animator fica no filho Mob1, não na raiz.
[RequireComponent(typeof(CharacterController))]
public class EnemyStats : NetworkBehaviour, ICharacterStats
{
    #region SyncVars
    [SyncVar(hook = nameof(OnHealthChanged))] private int _health;
    [SyncVar] private int _maxHealth;
    [SyncVar] private int _mana;
    [SyncVar] private int _maxMana;

    [SyncVar] private int _level = 1;
    [SyncVar] private string _enemyName = "Monster";
    [SyncVar] private bool _isDead = false;

    [SyncVar] private int _attack;
    [SyncVar] private int _defense;
    [SyncVar] private int _magicAttack;
    [SyncVar] private int _magicDefense;
    [SyncVar] private float _attackSpeed = 1f;
    [SyncVar] private float _moveSpeed = 3f;
    [SyncVar] private int _attackRange = 2;

    [SyncVar] private uint _currentAggroTarget;
    #endregion

    #region Properties
    public int Health => _health;
    public int MaxHealth => _maxHealth;
    public int Mana => _mana;
    public int MaxMana => _maxMana;
    public int Level => _level;
    public string EnemyName => _enemyName;
    public bool IsDead => _isDead;

    public int Attack => _attack;
    public int Defense => _defense;
    public int MagicAttack => _magicAttack;
    public int MagicDefense => _magicDefense;
    public float AttackSpeed => _attackSpeed;
    public float MoveSpeed => _moveSpeed;
    public int AttackRange => _attackRange;

    public uint CurrentAggroTarget => _currentAggroTarget;
    public bool HasAggro => _currentAggroTarget != 0;

    public float WanderRadius => wanderRadius;
    public float WanderInterval => wanderInterval;

    public EnemyData EnemyData => enemyData;
    #endregion

    #region Events
    public event Action OnHealthUpdated;
    public event Action OnDeath;
    public event Action OnAggroChanged;
    #endregion

    [Header("Configurações")]
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private float aggroRange = 8f;
    [SerializeField] private float loseAggroRange = 15f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float wanderRadius = 5f;
    [SerializeField] private float wanderInterval = 5f;

    [Header("Death Settings")]
    [SerializeField] private float deathDespawnDelay = 3f; // ✅ Tempo antes de sumir do mapa

    private Animator anim;
    private float lastAttackTime;
    private EnemyAI enemyAI;

    public Animator CharacterAnimator => anim;

    void Awake()
    {
        anim = ResolveCharacterAnimator();
        enemyAI = GetComponent<EnemyAI>();

        if (anim != null)
            Debug.Log($"[EnemyStats] Animator resolvido: {anim.name} (controller={(anim.runtimeAnimatorController ? anim.runtimeAnimatorController.name : "NULL")})", this);
        else
            Debug.LogError($"[EnemyStats] NENHUM Animator com controller encontrado em {gameObject.name} ou seus filhos!", this);
    }

    private Animator ResolveCharacterAnimator()
    {
        var a = GetComponent<Animator>();
        if (a != null && a.runtimeAnimatorController != null)
            return a;

        foreach (var child in GetComponentsInChildren<Animator>(true))
        {
            if (child != null && child.runtimeAnimatorController != null)
                return child;
        }

        return a;
    }

    void Start()
    {
        if (isServer)
        {
            _currentAggroTarget = 0;
            InitializeFromData();
        }
    }

    [Server]
    private void InitializeFromData()
    {
        if (enemyData == null)
        {
            Debug.LogWarning($"[{nameof(EnemyStats)}] enemyData não atribuído!", this);
            return;
        }

        _enemyName = enemyData.enemyName;
        _level = enemyData.level;
        _maxHealth = enemyData.baseHealth;
        _health = _maxHealth;
        _maxMana = enemyData.baseMana;
        _mana = _maxMana;
        _attack = enemyData.baseAttack;
        _defense = enemyData.baseDefense;
        _magicAttack = enemyData.baseMagicAttack;
        _magicDefense = enemyData.baseMagicDefense;
        _attackSpeed = enemyData.attackSpeed;
        _moveSpeed = enemyData.moveSpeed;
        _attackRange = enemyData.attackRange;
    }

    #region Damage & Death
    [Server]
    public void TakeDamage(int damage, uint attackerId, DamageType damageType = DamageType.Physical)
    {
        if (_isDead) return;

        int finalDamage = damage;

        if (damageType == DamageType.Physical)
            finalDamage = Mathf.Max(1, damage - Defense);
        else if (damageType == DamageType.Magical)
            finalDamage = Mathf.Max(1, damage - MagicDefense);

        _health = Mathf.Max(0, _health - finalDamage);

        if (!HasAggro)
        {
            SetAggroTarget(attackerId);
        }

        RpcOnDamageTaken(finalDamage);

        if (_health <= 0)
        {
            Die(attackerId);
        }
    }

    [Server]
    public void TakeTrueDamage(int damage, uint attackerId)
    {
        if (_isDead) return;
        _health = Mathf.Max(0, _health - damage);
        RpcOnDamageTaken(damage);
        if (_health <= 0) Die(attackerId);
    }

    [Server]
    public void Heal(int amount)
    {
        if (_isDead) return;
        _health = Mathf.Min(_maxHealth, _health + amount);
        OnHealthUpdated?.Invoke();
    }

    [Server]
    private void Die(uint killerId)
    {
        _isDead = true;
        _health = 0;
        OnDeath?.Invoke();

        // ✅ NOTIFICA O AI para parar completamente
        enemyAI?.OnDeath();

        RpcOnDeath();

        DropLoot(killerId);

        // ✅ Começa a coroutine de respawn no servidor
        StartCoroutine(RespawnCoroutine());
    }

    [Server]
    private System.Collections.IEnumerator RespawnCoroutine()
    {
        // Aguarda o tempo de respawn configurado no EnemyData
        float waitTime = enemyData?.respawnTime ?? 10f;
        yield return new WaitForSeconds(waitTime);

        _health = _maxHealth;
        _mana = _maxMana;
        _isDead = false;
        _currentAggroTarget = 0;

        // ✅ NOTIFICA O AI para reativar
        enemyAI?.OnRespawn();

        RpcOnRespawn();
    }

    [Server]
    public void SetAggroTarget(uint targetNetId)
    {
        _currentAggroTarget = targetNetId;
        OnAggroChanged?.Invoke();
    }

    [Server]
    public void ClearAggro()
    {
        _currentAggroTarget = 0;
        OnAggroChanged?.Invoke();
    }
    #endregion

    #region Attack
    [Server]
    public bool CanAttack()
    {
        return !_isDead && Time.time >= lastAttackTime + (attackCooldown / _attackSpeed);
    }

    [Server]
    public void PerformAttack(ICharacterStats target)
    {
        if (!CanAttack() || target == null || target.IsDead) return;

        lastAttackTime = Time.time;
        RpcPlayAttackAnimation();
    }

    [Server]
    public int CalculateDamage()
    {
        int baseDmg = _attack;
        float variation = UnityEngine.Random.Range(0.9f, 1.1f);
        return Mathf.Max(1, Mathf.FloorToInt(baseDmg * variation));
    }
    #endregion

    #region RPCs
    [ClientRpc]
    private void RpcOnDamageTaken(int damage)
    {
        OnHealthUpdated?.Invoke();

        if (TryGetComponent(out HitFlashEffect flash))
            flash.PlayFlash();

        DamagePopupManager.Instance?.ShowDamage(
            transform.position + Vector3.up * 2f,
            damage,
            false
        );

        if (anim != null && anim.runtimeAnimatorController != null)
        {
            foreach (var p in anim.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == "Hit")
                {
                    anim.SetTrigger("Hit");
                    break;
                }
            }
        }
    }

    [ClientRpc]
    private void RpcOnDeath()
    {
        // ✅ Animação de morte: trigger + bool para segurar no estado Dead
        anim?.SetTrigger("Die");
        anim?.SetBool("IsDead", true);

        // ✅ Desativa colliders para não bloquear pathfinding
        foreach (var col in GetComponentsInChildren<Collider>(true))
        {
            if (col != null) col.enabled = false;
        }

        // ✅ Começa coroutine no cliente para sumir do mapa após delay
        StartCoroutine(DespawnVisualCoroutine());
    }

    private System.Collections.IEnumerator DespawnVisualCoroutine()
    {
        yield return new WaitForSeconds(deathDespawnDelay);

        // Esconde o mesh (não destrói — o servidor controla o NetworkIdentity)
        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer != null) renderer.enabled = false;
        }
    }

    [ClientRpc]
    private void RpcOnRespawn()
    {
        // ✅ Reativa visual
        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer != null) renderer.enabled = true;
        }

        foreach (var col in GetComponentsInChildren<Collider>(true))
        {
            if (col != null) col.enabled = true;
        }

        // Sai do estado Dead e volta ao Idle
        anim?.SetBool("IsDead", false);
        anim?.Play("Idle", 0, 0f);
    }

    [ClientRpc]
    private void RpcPlayAttackAnimation()
    {
        anim?.SetTrigger("Attack");
        anim?.SetBool("IsAttacking", true);
    }
    #endregion

    #region Hooks
    private void OnHealthChanged(int oldVal, int newVal) => OnHealthUpdated?.Invoke();
    #endregion

    #region Loot
    [Server]
    private void DropLoot(uint killerId)
    {
        if (enemyData == null || enemyData.possibleDrops == null) return;

        foreach (var drop in enemyData.possibleDrops)
        {
            float roll = UnityEngine.Random.Range(0f, 100f);
            if (roll <= drop.dropChance)
            {
                int quantity = UnityEngine.Random.Range(drop.minQuantity, drop.maxQuantity + 1);
                SpawnWorldDrop(drop.itemId, quantity);
            }
        }

        if (NetworkServer.spawned.TryGetValue(killerId, out NetworkIdentity killerIdentity))
        {
            if (killerIdentity.TryGetComponent(out PlayerStats playerStats))
            {
                playerStats.AddExperience(enemyData.expReward);
            }
        }
    }

    [Server]
    private void SpawnWorldDrop(string itemId, int quantity)
    {
        Vector3 dropPos = transform.position + new Vector3(
            UnityEngine.Random.Range(-1f, 1f), 
            0.5f, 
            UnityEngine.Random.Range(-1f, 1f)
        );

        GameObject dropObj = new GameObject($"Drop_{itemId}");
        dropObj.transform.position = dropPos;

        WorldItem worldItem = dropObj.AddComponent<WorldItem>();
        ItemData item = ItemDatabase.Instance?.GetItem(itemId);
        if (item != null)
        {
            worldItem.Initialize(item, quantity);
            NetworkServer.Spawn(dropObj);
        }
    }
    #endregion

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, loseAggroRange);
    }
}