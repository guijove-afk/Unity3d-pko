using UnityEngine;
using Mirror;

public class EnemyAnimationEvents : NetworkBehaviour
{
    private EnemyStats stats;
    private EnemyAI enemyAI;
    private Animator anim;  // ✅ ADICIONADO

    void Awake()
    {
        stats = GetComponent<EnemyStats>();
        enemyAI = GetComponent<EnemyAI>();
        anim = GetComponent<Animator>();  // ✅ INICIALIZADO
    }

    // Chamado pelo Animation Event no frame do golpe
    public void OnAttackHit()
    {
        if (!isServer) return;
        if (stats.IsDead) return;
        if (!stats.HasAggro) return;

        if (NetworkServer.spawned.TryGetValue(stats.CurrentAggroTarget, out NetworkIdentity targetIdentity))
        {
            if (targetIdentity.TryGetComponent(out ICharacterStats targetStats))
            {
                if (!targetStats.IsDead)
                {
                    int damage = stats.CalculateDamage();
                    targetStats.TakeDamage(damage, netId, DamageType.Physical);
                    RpcPlayHitEffect(targetIdentity.transform.position);
                }
            }
        }
    }

    // Chamado no final da animação de ataque
    public void OnAttackEnd()
    {
        if (anim != null)  // ✅ USANDO anim
        {
            anim.SetBool("IsAttacking", false);
        }
    }

    [ClientRpc]
    private void RpcPlayHitEffect(Vector3 position)
    {
        // Som de hit
        // Partículas
    }
}