using UnityEngine;
using Mirror;

/// <summary>
/// Deve ficar no mesmo GameObject do <see cref="Animator"/> que toca Attack1 (ex.: filho Mob1).
/// Stats / NetworkIdentity ficam na raiz Enemy_* — use sempre GetComponentInParent.
/// </summary>
public class EnemyAnimationEvents : MonoBehaviour
{
    private EnemyStats stats;
    private EnemyAI enemyAI;
    private Animator anim;
    private NetworkIdentity networkIdentity;

    void Awake()
    {
        stats = GetComponentInParent<EnemyStats>();
        enemyAI = GetComponentInParent<EnemyAI>();
        anim = GetComponent<Animator>();
        networkIdentity = GetComponentInParent<NetworkIdentity>();
    }

    // Chamado pelo Animation Event no frame do golpe
    public void AnimEvent_AttackHit()
    {
        if (networkIdentity == null || !networkIdentity.isServer) return;
        if (stats == null || stats.IsDead) return;
        if (!stats.HasAggro) return;

        if (NetworkServer.spawned.TryGetValue(stats.CurrentAggroTarget, out NetworkIdentity targetIdentity))
        {
            if (targetIdentity.TryGetComponent(out ICharacterStats targetStats))
            {
                if (!targetStats.IsDead)
                {
                    int damage = stats.CalculateDamage();
                    targetStats.TakeDamage(damage, networkIdentity.netId, DamageType.Physical);
                }
            }
        }
    }

    // Chamado no final da animação de ataque
    public void AnimEvent_AttackEnd()
    {
        anim?.SetBool("IsAttacking", false);
    }
}