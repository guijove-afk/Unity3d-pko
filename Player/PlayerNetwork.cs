using Mirror;
using UnityEngine;
using UnityEngine.AI;

public class PlayerNetwork : NetworkBehaviour
{
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetMouseButtonDown(0))
        {
            MoveToMouse();
        }
    }

    void MoveToMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Debug.Log("Destino: " + hit.point);

            // move local (resposta instantânea)
            agent.SetDestination(hit.point);

            // envia pro servidor (multiplayer)
            CmdMove(hit.point);
        }
    }

    [Command]
    void CmdMove(Vector3 target)
    {
        if (agent != null)
            agent.SetDestination(target);
    }

    public override void OnStartLocalPlayer()
    {
        Camera.main.GetComponent<CameraFollow>().target = transform;
    }
}