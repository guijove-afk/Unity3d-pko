using UnityEngine;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(Animator))]
public class PlayerCombat : NetworkBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float baseAttackRange = 2.5f;
    [SerializeField] private float baseAttackCooldown = 0.8f;
    [SerializeField] private float attackCastTime = 0.3f;
    [SerializeField] private int baseDamage = 10;

    [Header("Layers")]
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private LayerMask attackableLayers;

    [Header("Animation")]
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string attackTypeParam = "AttackType";
    [SerializeField] private string isAttackingParam = "IsAttacking";

    [Header("VFX")]
    [SerializeField] private ParticleSystem attackVFX;
    [SerializeField] private AudioClip attackSFX;
    [SerializeField] private AudioClip hitSFX;
    [SerializeField] private AudioClip missSFX;

    private PlayerMovement movement;
    private PlayerStats stats;
    private Animator anim;
    private AudioSource audioSource;

    private float lastAttackTime;
    private bool isAttacking;
    private Transform currentTarget;
    private bool combatEnabled = true;
    private bool autoAttack;

    public event Action OnAttackLanded;
    public event Action OnAttackStarted;
    public event Action OnAttackFinished;
    public event Action<Transform> OnTargetChanged;

    public bool CanAttack => !isAttacking && combatEnabled && !stats.IsDead &&
        Time.time >= lastAttackTime + GetAttackCooldown();
    public bool IsAttacking => isAttacking;
    public Transform CurrentTarget => currentTarget;
    public float AttackRange => baseAttackRange + (stats?.Agility * 0.01f ?? 0);

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        stats = GetComponent<PlayerStats>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        if (!isLocalPlayer)
        {
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (!isLocalPlayer || !combatEnabled) return;

        HandleInput();
        UpdateTarget();

        if (autoAttack && currentTarget != null && CanAttack)
        {
            TryAttackTarget(currentTarget);
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (currentTarget != null)
                TryAttackTarget(currentTarget);
            else
                TryAttackNearest();
        }

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            autoAttack = !autoAttack;
            if (autoAttack && currentTarget != null)
                TryAttackTarget(currentTarget);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClearTarget();
        }
    }

    public void SetTarget(Transform target)
    {
        if (target == currentTarget) return;

        currentTarget = target;
        OnTargetChanged?.Invoke(target);

        if (target != null && autoAttack)
            TryAttackTarget(target);
    }

    public void ClearTarget()
    {
        currentTarget = null;
        autoAttack = false;
        OnTargetChanged?.Invoke(null);
    }

    private void UpdateTarget()
    {
        if (currentTarget == null) return;

        float distance = Vector3.Distance(transform.position, currentTarget.position);
        ICharacterStats targetStats = currentTarget.GetComponent<ICharacterStats>();

        if (distance > AttackRange * 3f || targetStats == null || targetStats.IsDead)
        {
            ClearTarget();
        }
    }

    public void TryAttackNearest()
    {
        if (!CanAttack) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, AttackRange, attackableLayers);
        Transform nearest = GetBestTarget(hits);

        if (nearest != null)
            TryAttackTarget(nearest);
    }

    public void TryAttackTarget(Transform target)
    {
        if (!CanAttack || target == null) return;

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > AttackRange)
        {
            movement.FollowTarget(target, AttackRange * 0.8f, () => TryAttackTarget(target));
            return;
        }

        if (stats.Stamina < 5)
            return;

        Vector3 lookPos = new Vector3(target.position.x, transform.position.y, target.position.z);
        transform.LookAt(lookPos);

        StartCoroutine(AttackSequence(target));
    }

    private IEnumerator AttackSequence(Transform target)
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        stats.RestoreStamina(-5);
        movement.SetMovementEnabled(false);

        anim.SetTrigger(attackTrigger);
        anim.SetBool(isAttackingParam, true);
        OnAttackStarted?.Invoke();

        if (attackSFX != null)
            audioSource.PlayOneShot(attackSFX);

        if (attackVFX != null)
            attackVFX.Play();

        yield return new WaitForSeconds(attackCastTime);

        if (target != null && Vector3.Distance(transform.position, target.position) <= AttackRange)
        {
            NetworkIdentity targetIdentity = target.GetComponent<NetworkIdentity>();
            if (targetIdentity != null)
            {
                int damage = CalculateDamage();
                CmdPerformAttack(targetIdentity, damage);
            }
        }

        float remainingCooldown = GetAttackCooldown() - attackCastTime;
        if (remainingCooldown > 0)
            yield return new WaitForSeconds(remainingCooldown);

        isAttacking = false;
        anim.SetBool(isAttackingParam, false);
        movement.SetMovementEnabled(true);
        OnAttackFinished?.Invoke();
    }

    [Command]
    private void CmdPerformAttack(NetworkIdentity targetIdentity, int damage)
    {
        if (targetIdentity == null) return;

        ICharacterStats targetStats = targetIdentity.GetComponent<ICharacterStats>();
        if (targetStats == null || targetStats.IsDead) return;

        float distance = Vector3.Distance(transform.position, targetIdentity.transform.position);
        if (distance > AttackRange * 1.5f) return;

        targetStats.TakeDamage(damage, netId, DamageType.Physical);
        TargetAttackResult(connectionToClient, true, damage);
    }

    [TargetRpc]
    private void TargetAttackResult(NetworkConnectionToClient target, bool hit, int damage)
    {
        if (hit)
        {
            if (hitSFX != null)
                audioSource.PlayOneShot(hitSFX);

            ICharacterStats targetStats = currentTarget?.GetComponent<ICharacterStats>();
            if (targetStats != null)
                OnAttackLanded?.Invoke();
        }
        else
        {
            if (missSFX != null)
                audioSource.PlayOneShot(missSFX);
        }
    }

    private int CalculateDamage()
    {
        int baseDmg = baseDamage + stats.Attack;
        int strBonus = Mathf.FloorToInt(stats.Strength * 0.5f);

        int critRoll = UnityEngine.Random.Range(0, 100);
        bool isCritical = critRoll < stats.CriticalRate;
        float critMultiplier = isCritical ? stats.CriticalDamage : 1f;

        float variation = UnityEngine.Random.Range(0.9f, 1.1f);

        int finalDamage = Mathf.FloorToInt((baseDmg + strBonus) * critMultiplier * variation);
        return Mathf.Max(1, finalDamage);
    }

    private float GetAttackCooldown()
    {
        return baseAttackCooldown / stats.AttackSpeed;
    }

    private Transform GetBestTarget(Collider[] hits)
    {
        Transform best = null;
        float bestScore = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.transform == transform) continue;

            ICharacterStats charStats = hit.GetComponent<ICharacterStats>();
            if (charStats == null || charStats.IsDead) continue;

            float distance = Vector3.Distance(transform.position, hit.transform.position);
            float angle = Vector3.Angle(transform.forward, hit.transform.position - transform.position);

            float score = distance + angle * 0.05f;

            if (score < bestScore)
            {
                bestScore = score;
                best = hit.transform;
            }
        }

        return best;
    }

    public void SetCombatEnabled(bool enabled)
    {
        combatEnabled = enabled;
        if (!enabled)
        {
            isAttacking = false;
            ClearTarget();
        }
    }

    public void UpdateAttackSpeed(float newSpeed)
    {
        // Atualizado pelo PlayerStats
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, AttackRange);

        if (currentTarget != null)
        {
            Gizmos.DrawLine(transform.position, currentTarget.position);
            Gizmos.DrawWireSphere(currentTarget.position, 0.5f);
        }
    }
public void OnAttackHit()
{
    if (!isLocalPlayer) return;

    if (currentTarget == null) return;

    float distance = Vector3.Distance(transform.position, currentTarget.position);

    if (distance > AttackRange) return;

    NetworkIdentity targetIdentity = currentTarget.GetComponent<NetworkIdentity>();

    if (targetIdentity != null)
    {
        int damage = CalculateDamage();
        CmdPerformAttack(targetIdentity, damage);
    }
}
}