using UnityEngine;
using Mirror;

public class SkillProjectile : NetworkBehaviour
{
    [SyncVar] private uint ownerNetId;
    [SyncVar] private uint targetNetId;
    [SyncVar] private string skillId;
    [SyncVar] private float speed;
    [SyncVar] private bool homing;

    private SkillData skillData;
    private Transform target;
    private Vector3 lastTargetPosition;
    private float lifetime;
    private const float MAX_LIFETIME = 10f;

    [Server]
    public void Initialize(SkillData skill, uint ownerId, uint targetId, float projectileSpeed, bool isHoming)
    {
        skillId = skill.skillId;
        ownerNetId = ownerId;
        targetNetId = targetId;
        speed = projectileSpeed;
        homing = isHoming;

        skillData = skill;

        if (NetworkServer.spawned.TryGetValue(targetId, out NetworkIdentity targetIdentity))
        {
            target = targetIdentity.transform;
            lastTargetPosition = target.position;
        }

        if (target != null)
        {
            transform.LookAt(target);
        }
    }

    void Start()
    {
        if (!isServer)
        {
            skillData = SkillDatabase.Instance?.GetSkill(skillId);
        }
    }

    void Update()
    {
        if (!isServer) return;

        lifetime += Time.deltaTime;
        if (lifetime > MAX_LIFETIME)
        {
            NetworkServer.Destroy(gameObject);
            return;
        }

        if (target != null)
        {
            lastTargetPosition = target.position;
        }

        Vector3 direction;
        if (homing && target != null)
        {
            direction = (target.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(direction);
        }
        else
        {
            direction = transform.forward;
        }

        transform.position += direction * speed * Time.deltaTime;

        CheckCollision();
    }

    [Server]
    private void CheckCollision()
    {
        float checkRadius = 0.5f;
        Collider[] hits = Physics.OverlapSphere(transform.position, checkRadius);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out NetworkIdentity identity))
            {
                if (identity.netId == ownerNetId) continue;

                if (targetNetId != 0 && identity.netId != targetNetId) continue;

                ICharacterStats charStats = hit.GetComponent<ICharacterStats>();
                if (charStats != null && !charStats.IsDead)
                {
                    int damage = skillData != null ? skillData.baseDamage : 10;
                    charStats.TakeDamage(damage, ownerNetId, DamageType.Magical);

                    RpcHitEffect(transform.position);

                    NetworkServer.Destroy(gameObject);
                    return;
                }
            }
        }

        if (!homing && Vector3.Distance(transform.position, lastTargetPosition) < 1f)
        {
            RpcHitEffect(transform.position);
            NetworkServer.Destroy(gameObject);
        }
    }

    [ClientRpc]
    private void RpcHitEffect(Vector3 position)
    {
        if (skillData != null && skillData.hitEffect != null)
        {
            Instantiate(skillData.hitEffect, position, Quaternion.identity);
        }

        if (skillData != null && skillData.hitSound != null)
        {
            AudioSource.PlayClipAtPoint(skillData.hitSound, position);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}