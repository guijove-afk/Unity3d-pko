using Mirror;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public class MobAI : NetworkBehaviour
{
    [Header("Detecção")]
    public float detectionRadius = 10f;
    public float attackRange = 2f;

    [Header("Ataque")]
    public int damage = 10;
    public float attackCooldown = 2f;

    [Header("Movimento")]
    public float stoppingDistance = 1f;

    private NavMeshAgent agent;
    private float nextAttackTime;
    private PlayerNetwork currentTarget;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = stoppingDistance;
    }

    [ServerCallback]
    void Update()
    {
        if (!NetworkServer.active)
            return;

        UpdateTarget();
        UpdateMovementAndAttack();
    }

    [Server]
    void UpdateTarget()
    {
        PlayerNetwork[] players = Object.FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None);
        float bestDistance = detectionRadius;
        PlayerNetwork bestPlayer = null;

        foreach (PlayerNetwork player in players)
        {
            if (player == null || player.connectionToClient == null)
                continue;

            Health playerHealth = player.GetComponent<Health>();
            if (playerHealth == null || playerHealth.Hp <= 0)
                continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                bestPlayer = player;
            }
        }

        currentTarget = bestPlayer;
    }

    [Server]
    void UpdateMovementAndAttack()
    {
        if (currentTarget == null)
        {
            if (agent.hasPath)
                agent.ResetPath();
            return;
        }

        Vector3 targetPosition = currentTarget.transform.position;
        float distance = Vector3.Distance(transform.position, targetPosition);

        if (distance > attackRange)
        {
            if (agent.enabled)
                agent.SetDestination(targetPosition);
        }
        else
        {
            if (agent.enabled)
                agent.ResetPath();

            if (Time.time >= nextAttackTime)
            {
                AttackTarget();
                nextAttackTime = Time.time + attackCooldown;
            }
        }
    }

    [Server]
    void AttackTarget()
    {
        if (currentTarget == null)
            return;

        Health targetHealth = currentTarget.GetComponent<Health>();
        if (targetHealth == null)
            return;

        targetHealth.TakeDamage(damage);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
