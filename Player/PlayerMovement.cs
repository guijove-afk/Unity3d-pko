using UnityEngine;
using UnityEngine.AI;
using Mirror;
using System;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float baseMoveSpeed = 5f;
    [SerializeField] private float runSpeedMultiplier = 1.5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float stoppingDistance = 0.1f;

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer = 1;
    [SerializeField] private LayerMask clickableLayers = ~0;
    [SerializeField] private LayerMask interactableLayers = ~0;

    [Header("Animation")]
    [SerializeField] private string moveSpeedParam = "MoveSpeed";
    [SerializeField] private string isMovingParam = "IsMoving";
    [SerializeField] private string isRunningParam = "IsRunning";

    private NavMeshAgent agent;
    private Animator anim;
    private Camera mainCamera;
    private PlayerStats stats;
    private PlayerCombat combat;

    private bool isMovementEnabled = true;
    private bool isRunning;
    private Transform followTarget;
    private float followStopDistance;
    private Action onReachTarget;

    public event Action OnMovementStarted;
    public event Action OnMovementStopped;
    public event Action<Vector3> OnDestinationSet;
    public event Action<IInteractable> OnInteractableFound;

    public bool IsMoving => agent != null && agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.1f;
    public Vector3 CurrentDestination => agent.hasPath ? agent.destination : transform.position;
    public float CurrentSpeed => agent.velocity.magnitude;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        stats = GetComponent<PlayerStats>();
        combat = GetComponent<PlayerCombat>();
        mainCamera = Camera.main;

        if (agent == null)
        {
            Debug.LogError("[PlayerMovement] NavMeshAgent não encontrado!", this);
            enabled = false;
            return;
        }

        agent.speed = baseMoveSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.acceleration = baseMoveSpeed * 3f;
        agent.angularSpeed = 360f;
        agent.autoBraking = true;
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
        if (!isLocalPlayer || !isMovementEnabled) return;

        HandleInput();
        UpdateFollowTarget();
        UpdateAnimation();
    }

    private void HandleInput()
    {
// If the camera is null, try to find it one last time
    if (mainCamera == null) mainCamera = Camera.main;
    
    // If it's still null, exit to prevent the exception
    if (mainCamera == null) return;

    if (UnityEngine.EventSystems.EventSystem.current != null &&
        UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        return;

        isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        agent.speed = isRunning ? baseMoveSpeed * runSpeedMultiplier : baseMoveSpeed;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, clickableLayers))
            {
                if (hit.collider.TryGetComponent(out ICharacterStats characterStats) &&
                    characterStats != this.stats && !characterStats.IsDead)
                {
                    combat?.SetTarget(hit.transform);
                    return;
                }

                if (hit.collider.TryGetComponent(out IInteractable interactable))
                {
                    float distance = Vector3.Distance(transform.position, hit.point);
                    if (distance <= interactable.GetInteractionRange())
                    {
                        interactable.OnInteract(this);
                    }
                    else
                    {
                        MoveToPoint(hit.point, interactable.GetInteractionRange(),
                            () => interactable.OnInteract(this));
                    }
                    return;
                }

                if (hit.collider.TryGetComponent(out WorldItem worldItem))
                {
                    float distance = Vector3.Distance(transform.position, hit.point);
                    if (distance <= 1.5f)
                    {
                        worldItem.Pickup(GetComponent<PlayerInventory>());
                    }
                    else
                    {
                        MoveToPoint(hit.point, 1.5f, () => worldItem.Pickup(GetComponent<PlayerInventory>()));
                    }
                    return;
                }

                MoveToPoint(hit.point);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, clickableLayers))
            {
                if (hit.collider.TryGetComponent(out ICharacterStats targetStats) &&
                    targetStats != this.stats && !targetStats.IsDead)
                {
                    combat?.SetTarget(hit.transform);
                }
            }
        }
    }

    public void MoveToPoint(Vector3 destination, float stopDistance = 0.1f, Action onArrive = null)
    {
        if (!NavMesh.SamplePosition(destination, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
        {
            Debug.LogWarning("[PlayerMovement] Destino fora do NavMesh!");
            return;
        }

        followTarget = null;
        followStopDistance = 0;
        onReachTarget = onArrive;

        agent.stoppingDistance = stopDistance;
        agent.SetDestination(navHit.position);

        OnDestinationSet?.Invoke(navHit.position);
        CmdSetDestination(navHit.position, stopDistance);
    }

    public void FollowTarget(Transform target, float stopDistance, Action onArrive = null)
    {
        followTarget = target;
        followStopDistance = stopDistance;
        onReachTarget = onArrive;
        agent.stoppingDistance = stopDistance;
    }

    public void StopMovement()
    {
        followTarget = null;
        onReachTarget = null;
        agent.ResetPath();
        agent.velocity = Vector3.zero;

        if (anim != null)
        {
            anim.SetFloat(moveSpeedParam, 0f);
            anim.SetBool(isMovingParam, false);
            anim.SetBool(isRunningParam, false);
        }
    }

    public void SetMovementEnabled(bool enabled)
    {
        isMovementEnabled = enabled;
        if (!enabled) StopMovement();
    }

    [Command]
    private void CmdSetDestination(Vector3 destination, float stopDistance)
    {
        if (NavMesh.SamplePosition(destination, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
        {
            agent.stoppingDistance = stopDistance;
            agent.SetDestination(navHit.position);
        }
    }

    public void UpdateMoveSpeed(float newSpeed)
    {
        baseMoveSpeed = newSpeed;
        if (agent != null)
            agent.speed = isRunning ? baseMoveSpeed * runSpeedMultiplier : baseMoveSpeed;
    }

    private void UpdateFollowTarget()
    {
        if (followTarget != null)
        {
            if (followTarget == null)
            {
                followTarget = null;
                return;
            }

            float distance = Vector3.Distance(transform.position, followTarget.position);
            if (distance > followStopDistance + 0.5f)
            {
                agent.SetDestination(followTarget.position);
            }
            else
            {
                agent.ResetPath();
                onReachTarget?.Invoke();
                onReachTarget = null;
                followTarget = null;
            }
        }
    }

    private void UpdateAnimation()
    {
        bool wasMoving = anim.GetBool(isMovingParam);
        bool isMovingNow = IsMoving;

        float velocity = agent.velocity.magnitude / agent.speed;
        anim.SetFloat(moveSpeedParam, velocity);
        anim.SetBool(isMovingParam, isMovingNow);
        anim.SetBool(isRunningParam, isRunning && isMovingNow);

        if (!wasMoving && isMovingNow)
            OnMovementStarted?.Invoke();
        else if (wasMoving && !isMovingNow)
        {
            OnMovementStopped?.Invoke();
            onReachTarget?.Invoke();
            onReachTarget = null;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.cyan;
            Vector3[] corners = agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
            Gizmos.DrawWireSphere(agent.destination, 0.3f);
        }
    }
}

public interface IInteractable
{
    void OnInteract(PlayerMovement player);
    string GetInteractionName();
    float GetInteractionRange();
    bool CanInteract(PlayerMovement player);
}