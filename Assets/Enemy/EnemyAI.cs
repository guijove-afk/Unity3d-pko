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
    private bool isDead = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stats = GetComponent<EnemyStats>();
    }

    void Start()
    {
        if (anim == null && stats != null)
            anim = stats.CharacterAnimator;

        // ✅ DEBUG: confirma qual Animator o AI está usando
        if (anim != null)
            Debug.Log($"[EnemyAI] Animator vinculado: {anim.name} | controller={(anim.runtimeAnimatorController ? anim.runtimeAnimatorController.name : "NULL")}", this);
        else
            Debug.LogError($"[EnemyAI] Animator NULO em {gameObject.name}! Verifique EnemyStats.CharacterAnimator.", this);
    }

    public override void OnStartServer()
    {
        spawnPosition = transform.position;
        agent.speed = stats.MoveSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.angularSpeed = 360f;
        agent.autoBraking = true;
        agent.updateRotation = true; // ✅ Garante que o NavMeshAgent rotaciona o root

        Debug.Log($"[EnemyAI] OnStartServer: {gameObject.name} | spawn={spawnPosition} | speed={agent.speed} | stoppingDist={stoppingDistance}");
    }

    void Update()
    {
        if (!isServer) return;

        // ✅ Se morto, para TODA a lógica de AI
        if (stats.IsDead || isDead)
        {
            if (agent.hasPath)
                agent.ResetPath();
            return;
        }

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
        // ✅ DEBUG: entrou em UpdateAggroBehavior
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            if (NetworkServer.spawned.TryGetValue(stats.CurrentAggroTarget, out NetworkIdentity identity))
            {
                currentTarget = identity.transform;
                Debug.Log($"[EnemyAI] Alvo resolvido via NetworkServer: {currentTarget.name} netId={identity.netId}");
            }
            else
            {
                Debug.Log($"[EnemyAI] Alvo {stats.CurrentAggroTarget} não encontrado em NetworkServer.spawned, perdendo aggro");
                LoseAggro();
                return;
            }
        }

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

        float loseRange = stats.EnemyData != null ? stats.EnemyData.aggroRange * 2f : 15f;

        if (distanceToTarget > loseRange)
        {
            Debug.Log($"[EnemyAI] Distância {distanceToTarget:F2} > loseRange {loseRange:F2}, perdendo aggro");
            LoseAggro();
            return;
        }

        ICharacterStats charStats = currentTarget.GetComponent<ICharacterStats>();

        if (charStats == null)
        {
            charStats = currentTarget.GetComponentInParent<ICharacterStats>();
        }

        if (charStats == null || charStats.IsDead)
        {
            Debug.Log($"[EnemyAI] charStats nulo={(charStats==null)} ou morto={(charStats != null && charStats.IsDead)}, perdendo aggro");
            LoseAggro();
            return;
        }

        float attackRange = stats.AttackRange + 0.5f;

        if (distanceToTarget <= attackRange)
        {
            Debug.Log($"[EnemyAI] Dentro da range de ataque ({distanceToTarget:F2} <= {attackRange:F2}) | CanAttack={stats.CanAttack()}");
            agent.ResetPath();
            FaceTarget(currentTarget.position);

            if (stats.CanAttack())
            {
                Debug.Log($"[EnemyAI] Chamando stats.PerformAttack({((Component)charStats).gameObject.name})");
                stats.PerformAttack(charStats);
            }
        }
        else
        {
            // ✅ PERSEGUIÇÃO: olha para o alvo ENQUANTO anda
            Debug.Log($"[EnemyAI] Fora da range ({distanceToTarget:F2} > {attackRange:F2}), perseguindo {currentTarget.name}");
            ChaseTarget(currentTarget.position);
        }
    }

    [Server]
    private void UpdateIdleBehavior()
    {
        float aggroRange = stats.EnemyData != null ? stats.EnemyData.aggroRange : 8f;
        Collider[] hits = Physics.OverlapSphere(transform.position, aggroRange);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out PlayerStats playerStats) && !playerStats.IsDead)
            {
                Debug.Log($"[EnemyAI] Player encontrado: {hit.name} netId={playerStats.netId}, adquirindo aggro");
                stats.SetAggroTarget(playerStats.netId);
                return;
            }
        }

        if (stats.EnemyData != null && stats.EnemyData.canWander &&
            Time.time >= lastWanderTime + Mathf.Max(0.5f, stats.WanderInterval))
        {
            lastWanderTime = Time.time;

            float radial = stats.EnemyData.canPatrol ? 2f : stats.WanderRadius;
            Vector3 randomDirection = Random.insideUnitSphere * radial;
            randomDirection += spawnPosition;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                currentWanderTarget = hit.position;
                agent.SetDestination(currentWanderTarget);
                Debug.Log($"[EnemyAI] Wander para {currentWanderTarget}");
            }
        }
    }

    [Server]
    private void LoseAggro()
    {
        Debug.Log($"[EnemyAI] LoseAggro chamado em {gameObject.name}");
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

            // ✅ ROTACIONA enquanto persegue (o NavMeshAgent move, mas olhar para frente é feito aqui)
            if (agent.hasPath && agent.pathPending == false)
            {
                Vector3 nextCorner = agent.path.corners.Length > 1 ? agent.path.corners[1] : targetPosition;
                FaceTarget(nextCorner);
            }
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

    // ✅ Chamado pelo EnemyStats quando morre — garante que AI pare
    [Server]
    public void OnDeath()
    {
        Debug.Log($"[EnemyAI] OnDeath chamado em {gameObject.name}");
        isDead = true;
        if (agent.hasPath)
            agent.ResetPath();
        agent.enabled = false; // Desativa completamente o agent
        currentTarget = null;
    }

    // ✅ Chamado pelo EnemyStats quando respawna — reativa o agent
    [Server]
    public void OnRespawn()
    {
        Debug.Log($"[EnemyAI] OnRespawn chamado em {gameObject.name}");
        isDead = false;
        agent.enabled = true;
        agent.Warp(spawnPosition); // Teleporta para spawn sem interpolar
        transform.position = spawnPosition;
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