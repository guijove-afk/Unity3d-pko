using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator anim;
    
    [SerializeField] private LayerMask groundLayer; // Defina como "Ground" no Inspetor

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
    }

 void Update()
{
    // Clique para mover
    if (Input.GetMouseButtonDown(0))
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            agent.SetDestination(hit.point);
        }
    }

    // Lógica da Animação (O segredo está aqui)
    // Se a velocidade for maior que 0.1, ele está se movendo
    if (agent.velocity.magnitude > 0.1f) 
    {
        anim.SetBool("isRunning", true);
    }
    else 
    {
        anim.SetBool("isRunning", false);
    }
}

    void UpdateAnimation()
    {
        // Verificamos se o agente ainda tem um caminho e se está se movendo
        // A 'remainingDistance' é a distância até o ponto clicado
        if (agent.pathPending) return;

        if (agent.remainingDistance > agent.stoppingDistance)
        {
            anim.SetBool("isRunning", true);
        }
        else
        {
            anim.SetBool("isRunning", false);
        }
    }
}