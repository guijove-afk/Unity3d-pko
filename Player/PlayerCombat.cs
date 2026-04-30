using Mirror;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{
    public float attackRange = 2f;
    public int damage = 10;

    void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            CmdAttack();
        }
    }

    [Command]
    void CmdAttack()
    {
        RaycastHit hit;

        if (Physics.Raycast(transform.position, transform.forward, out hit, attackRange))
        {
            Health target = hit.collider.GetComponent<Health>();

            if (target != null)
            {
                target.TakeDamage(damage);
            }
        }
    }
}