using UnityEngine;
using UnityEngine.AI;
using Mirror;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyAI : NetworkBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float chaseRange = 20f;
    
    [Header("Combat")]
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float rotationSpeed = 5f;
    
    private NavMeshAgent agent;
    private Animator anim;
    private EnemyStats stats;
    private Transform target;
    private float lastAttackTime;
    private bool isAttacking;
    
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        stats = GetComponent<EnemyStats>();
    }
    
    void Start()
    {
        if (!isServer) 
        {
            enabled = false;
            return;
        }
        
        agent.speed = stats.moveSpeed;
        agent.stoppingDistance = stats.attackRange * 0.8f;
    }
    
    void Update()
    {
        if (!isServer || stats.IsDead) return;
        
        FindTarget();
        
        if (target == null)
        {
            Idle();
            return;
        }
        
        float distance = Vector3.Distance(transform.position, target.position);
        
        if (distance > chaseRange)
        {
            target = null;
            return;
        }
        
        if (distance <= stats.attackRange && CanAttack())
        {
            Attack();
        }
        else if (distance > stats.attackRange)
        {
            Chase();
        }
    }
    
    [Server]
    private void FindTarget()
    {
        if (target != null) return; // Já tem alvo
        
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange);
        float closestDistance = float.MaxValue;
        Transform closest = null;
        
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out PlayerStats playerStats) && !playerStats.IsDead)
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closest = hit.transform;
                }
            }
        }
        
        target = closest;
    }
    
    [Server]
    private void Chase()
    {
        if (target == null) return;
        
        agent.isStopped = false;
        agent.SetDestination(target.position);
        
        anim.SetBool("IsMoving", true);
        anim.SetFloat("MoveSpeed", 1f);
    }
    
    [Server]
    private void Attack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        agent.isStopped = true;
        
        // Olhar para o alvo
        Vector3 lookPos = new Vector3(target.position.x, transform.position.y, target.position.z);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookPos - transform.position), Time.deltaTime * rotationSpeed);
        
        // Animacao
        anim.SetTrigger("Attack");
        anim.SetBool("IsAttacking", true);
        
        // Dano com delay (simulando o golpe)
        Invoke(nameof(DealDamage), 0.3f);
    }
    
    [Server]
    private void DealDamage()
    {
        if (target == null || stats.IsDead) return;
        
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > stats.attackRange * 1.2f) return;
        
        if (target.TryGetComponent(out PlayerStats playerStats))
        {
            int damage = Mathf.FloorToInt(stats.attack * Random.Range(0.9f, 1.1f));
            playerStats.TakeDamage(damage, netId, DamageType.Physical);
        }
        
        isAttacking = false;
        anim.SetBool("IsAttacking", false);
    }
    
    [Server]
    private void Idle()
    {
        agent.isStopped = true;
        agent.ResetPath();
        anim.SetBool("IsMoving", false);
        anim.SetFloat("MoveSpeed", 0f);
    }
    
    private bool CanAttack()
    {
        return !isAttacking && Time.time >= lastAttackTime + (attackCooldown / stats.attackSpeed);
    }
}