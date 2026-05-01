using UnityEngine;
using UnityEngine.AI;
using Mirror;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyStats))]
public class EnemyAI : NetworkBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private float stoppingDistance = 1.5f;
    [SerializeField] private float rotationSpeed = 10f;
    
    private NavMeshAgent agent;
    private Animator anim;
    private EnemyStats stats;
    
    private Vector3 spawnPosition;
    private Vector3 currentWanderTarget;
    private float lastWanderTime;
    
    private Transform currentTarget;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        stats = GetComponent<EnemyStats>();
    }

    public override void OnStartServer()
    {
        spawnPosition = transform.position;
        agent.speed = stats.MoveSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.angularSpeed = 360f;
        agent.autoBraking = true;
    }

    void Update()
    {
        if (!isServer || stats.IsDead) return;
        
        UpdateAI();
        UpdateAnimation();
    }

    [Server]
    private void UpdateAI()
    {
        if (stats.HasAggro)
            UpdateAggroBehavior();
        else
            UpdateIdleBehavior();
    }

    [Server]
    private void UpdateAggroBehavior()
    {
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            if (NetworkServer.spawned.TryGetValue(stats.CurrentAggroTarget, out NetworkIdentity identity))
            {
                currentTarget = identity.transform;
            }
            else
            {
                LoseAggro();
                return;
            }
        }

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        
        // ✅ USA A PROPRIEDADE PÚBLICA EnemyData em vez de enemyData
        float loseRange = stats.EnemyData != null ? stats.EnemyData.aggroRange * 2f : 15f;
        
        if (distanceToTarget > loseRange)
        {
            LoseAggro();
            return;
        }

        ICharacterStats charStats = currentTarget.GetComponent<ICharacterStats>();

        if (charStats == null || charStats.IsDead)
        {
            LoseAggro();
            return;
        }

        if (distanceToTarget <= stats.AttackRange + 0.5f)
        {
            agent.ResetPath();
            FaceTarget(currentTarget.position);
            
            if (stats.CanAttack())
            {
                stats.PerformAttack(charStats);
            }
        }
        else
        {
            ChaseTarget(currentTarget.position);
        }
    }

    [Server]
    private void UpdateIdleBehavior()
    {
        // ✅ USA A PROPRIEDADE PÚBLICA EnemyData
        float aggroRange = stats.EnemyData != null ? stats.EnemyData.aggroRange : 8f;
        Collider[] hits = Physics.OverlapSphere(transform.position, aggroRange);
        
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out PlayerStats playerStats) && !playerStats.IsDead)
            {
                stats.SetAggroTarget(playerStats.netId);
                return;
            }
        }

        // ✅ USA A PROPRIEDADE EnemyData e o wanderRadius DE LÁ
        if (stats.EnemyData != null && stats.EnemyData.canWander && 
            Time.time >= lastWanderTime + 5f)
        {
            lastWanderTime = Time.time;
            
            // ✅ USA wanderRadius DO EnemyData, não de EnemyStats
            float wanderRadius = stats.EnemyData.canPatrol ? 2f : 5f; // ou adicione wanderRadius no EnemyData
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += spawnPosition;
            
            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                currentWanderTarget = hit.position;
                agent.SetDestination(currentWanderTarget);
            }
        }
    }

    [Server]
    private void LoseAggro()
    {
        stats.ClearAggro();
        currentTarget = null;
        agent.ResetPath();
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 lookPos = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
        transform.rotation = Quaternion.Slerp(transform.rotation, 
            Quaternion.LookRotation(lookPos - transform.position), 
            Time.deltaTime * rotationSpeed);
    }

    private void ChaseTarget(Vector3 targetPosition)
    {
        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private void UpdateAnimation()
    {
        if (anim == null) return;
        
        bool isMoving = agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.1f;
        float velocity = agent.velocity.magnitude / Mathf.Max(agent.speed, 0.01f);
        
        anim.SetBool("IsMoving", isMoving);
        anim.SetFloat("MoveSpeed", velocity);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        if (agent != null && agent.hasPath)
        {
            Vector3[] corners = agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }
    }
}