using UnityEngine;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;

public class PlayerSkills : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxSkillBarSlots = 10;
    [SerializeField] private float globalCooldown = 0.5f;

    public SyncList<LearnedSkill> learnedSkills = new SyncList<LearnedSkill>();

    [SyncVar]
    private int availableSkillPoints;

    private SkillData[] skillBar;
    private Dictionary<string, float> cooldowns = new Dictionary<string, float>();
    private Dictionary<string, int> skillLevels = new Dictionary<string, int>();

    private PlayerStats stats;
    private PlayerMovement movement;
    private PlayerCombat combat;
    private PlayerAnimation anim;

    private float lastGlobalCooldown;
    private bool isCasting;
    private SkillData currentCastingSkill;
    private Coroutine castingCoroutine;

    public event Action<SkillData> OnSkillLearned;
    public event Action<SkillData> OnSkillUsed;
    public event Action<SkillData, float> OnSkillCastStarted;
    public event Action OnSkillCastFinished;
    public event Action<SkillData> OnSkillExecuted;
    public event Action<int> OnSkillPointsChanged;
    public event Action<string> OnCooldownStarted;
    public event Action<string> OnCooldownFinished;

    public bool IsCasting => isCasting;
    public int AvailableSkillPoints => availableSkillPoints;
    public IReadOnlyList<LearnedSkill> LearnedSkills => learnedSkills;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        movement = GetComponent<PlayerMovement>();
        combat = GetComponent<PlayerCombat>();
        anim = GetComponent<PlayerAnimation>();

        skillBar = new SkillData[maxSkillBarSlots];
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
        if (!isLocalPlayer) return;

        HandleInput();
        UpdateCooldowns();
    }

    private void HandleInput()
    {
        for (int i = 0; i < maxSkillBarSlots && i < 10; i++)
        {
            if (Input.GetKeyDown(KeyCode.F1 + i))
            {
                TryUseSkill(skillBar[i]);
            }
        }

        for (int i = 0; i < maxSkillBarSlots && i < 10; i++)
        {
            KeyCode key = i == 9 ? KeyCode.Alpha0 : KeyCode.Alpha1 + i;
            if (Input.GetKeyDown(key))
            {
                TryUseSkill(skillBar[i]);
            }
        }
    }

    public bool TryUseSkill(SkillData skill)
    {
        if (skill == null) return false;
        if (isCasting) return false;
        if (!CanUseSkill(skill)) return false;

        if (skill.targetType != SkillTargetType.Self)
        {
            if (combat.CurrentTarget == null)
            {
                return false;
            }
        }

        if (skill.range > 0 && combat.CurrentTarget != null && skill.targetType != SkillTargetType.Self)
        {
            float distance = Vector3.Distance(transform.position, combat.CurrentTarget.position);
            if (distance > skill.range)
            {
                movement.FollowTarget(combat.CurrentTarget, skill.range * 0.9f,
                    () => ExecuteSkill(skill));
                return true;
            }
        }

        ExecuteSkill(skill);
        return true;
    }

    private void ExecuteSkill(SkillData skill)
    {
        if (skill.castTime > 0)
        {
            StartCasting(skill);
            return;
        }

        UseSkillImmediate(skill);
    }

    private void StartCasting(SkillData skill)
    {
        if (castingCoroutine != null)
            StopCoroutine(castingCoroutine);

        isCasting = true;
        currentCastingSkill = skill;

        if (!skill.canMoveWhileCasting)
            movement.SetMovementEnabled(false);

        OnSkillCastStarted?.Invoke(skill, skill.castTime);
        castingCoroutine = StartCoroutine(CastingSequence(skill));
    }

    private IEnumerator CastingSequence(SkillData skill)
    {
        float elapsed = 0;

        if (skill.castEffect != null)
        {
            ParticleSystem castVFX = Instantiate(skill.castEffect, transform.position + Vector3.up, Quaternion.identity);
            castVFX.transform.SetParent(transform);
            Destroy(castVFX.gameObject, skill.castTime);
        }

        if (skill.castSound != null && TryGetComponent(out AudioSource audio))
            audio.PlayOneShot(skill.castSound);

        while (elapsed < skill.castTime)
        {
            elapsed += Time.deltaTime;

            if (skill.canBeInterrupted && stats.IsDead)
            {
                InterruptCast();
                yield break;
            }

            yield return null;
        }

        UseSkillImmediate(skill);
    }

    public void InterruptCast()
    {
        if (!isCasting) return;

        if (castingCoroutine != null)
            StopCoroutine(castingCoroutine);

        isCasting = false;
        currentCastingSkill = null;
        movement.SetMovementEnabled(true);

        OnSkillCastFinished?.Invoke();

        DamagePopupManager.Instance?.ShowText(transform.position + Vector3.up * 2f, "Cast Interrompido!", Color.red);
    }

    private void UseSkillImmediate(SkillData skill)
    {
        isCasting = false;
        currentCastingSkill = null;
        movement.SetMovementEnabled(true);
        OnSkillCastFinished?.Invoke();

        CmdUseSkill(skill.skillId, combat.CurrentTarget?.GetComponent<NetworkIdentity>());
    }

    [Command]
    private void CmdUseSkill(string skillId, NetworkIdentity target)
    {
        SkillData skill = SkillDatabase.Instance?.GetSkill(skillId);
        if (skill == null) return;

        if (!CanUseSkill(skill)) return;

        int skillLevel = GetSkillLevel(skillId);

        int mpCost = Mathf.RoundToInt(skill.mpCost * Mathf.Pow(skill.mpCostPerLevel, skillLevel - 1));
        int spCost = skill.spCost;
        int hpCost = skill.hpCost;

        if (stats.Mana < mpCost || stats.Stamina < spCost || stats.Health <= hpCost)
            return;

        stats.RestoreMana(-mpCost);
        stats.RestoreStamina(-spCost);
        if (hpCost > 0)
            stats.TakeTrueDamage(hpCost, netId);

        ApplySkillEffect(skill, target?.GetComponent<ICharacterStats>(), skillLevel);

        float cooldown = skill.cooldown * Mathf.Pow(skill.cooldownPerLevel, skillLevel - 1);
        cooldowns[skillId] = Time.time + cooldown;

        lastGlobalCooldown = Time.time + globalCooldown;

        RpcSkillUsed(skillId, target?.netId ?? 0);
    }

    [Server]
    private void ApplySkillEffect(SkillData skill, ICharacterStats target, int skillLevel)
    {
        int damage = Mathf.RoundToInt(skill.baseDamage * Mathf.Pow(skill.damagePerLevel, skillLevel - 1));

        switch (skill.targetType)
        {
            case SkillTargetType.Self:
                ApplySkillToTarget(skill, stats, damage);
                break;

            case SkillTargetType.SingleEnemy:
            case SkillTargetType.SingleAlly:
                if (target != null)
                    ApplySkillToTarget(skill, target, damage);
                break;

            case SkillTargetType.AreaEnemy:
            case SkillTargetType.AreaAlly:
            case SkillTargetType.AreaAll:
                ApplyAreaEffect(skill, damage);
                break;

            case SkillTargetType.Line:
                ApplyLineEffect(skill, damage);
                break;

            case SkillTargetType.Cone:
                ApplyConeEffect(skill, damage);
                break;
        }
    }

    [Server]
    private void ApplySkillToTarget(SkillData skill, ICharacterStats target, int damage)
    {
        if (target == null) return;

        if (damage > 0)
        {
            DamageType dmgType = skill.element != SkillElement.None ?
                (DamageType)Enum.Parse(typeof(DamageType), skill.element.ToString()) : DamageType.Physical;

            target.TakeDamage(damage, netId, dmgType);
        }

        if (skill.healAmount > 0)
        {
            int heal = Mathf.RoundToInt(skill.healAmount * skill.healMultiplier);
            if (target is PlayerStats playerStats)
                playerStats.Heal(heal);
        }
    }

    [Server]
    private void ApplyAreaEffect(SkillData skill, int damage)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, skill.areaRadius);
        int targetsHit = 0;

        foreach (var hit in hits)
        {
            if (targetsHit >= skill.maxTargets) break;

            ICharacterStats charStats = hit.GetComponent<ICharacterStats>();
            if (charStats == null || charStats == this.stats) continue;

            bool isEnemy = hit.GetComponent<NetworkIdentity>()?.netId != netId;

            if (skill.targetType == SkillTargetType.AreaEnemy && !isEnemy) continue;
            if (skill.targetType == SkillTargetType.AreaAlly && isEnemy) continue;

            ApplySkillToTarget(skill, charStats, damage);
            targetsHit++;
        }
    }

    [Server]
    private void ApplyLineEffect(SkillData skill, int damage)
    {
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, skill.range))
        {
            ICharacterStats charStats = hit.collider.GetComponent<ICharacterStats>();
            if (charStats != null)
                ApplySkillToTarget(skill, charStats, damage);
        }
    }

    [Server]
    private void ApplyConeEffect(SkillData skill, int damage)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, skill.range);
        foreach (var hit in hits)
        {
            ICharacterStats charStats = hit.GetComponent<ICharacterStats>();
            if (charStats == null || charStats == this.stats) continue;

            Vector3 dirToTarget = (hit.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToTarget);

            if (angle <= skill.coneAngle / 2f)
            {
                ApplySkillToTarget(skill, charStats, damage);
            }
        }
    }

    [Server]
    public bool LearnSkill(string skillId)
    {
        SkillData skill = SkillDatabase.Instance?.GetSkill(skillId);
        if (skill == null) return false;
        if (HasLearnedSkill(skillId)) return false;
        if (stats.Level < skill.requiredLevel) return false;
        if (skill.requiredClass != CharacterClass.None && skill.requiredClass != stats.CharacterClass) return false;
        if (availableSkillPoints < skill.requiredSkillPoints) return false;

        foreach (string prereq in skill.prerequisiteSkills)
        {
            if (!HasLearnedSkill(prereq)) return false;
        }

        availableSkillPoints -= skill.requiredSkillPoints;

        learnedSkills.Add(new LearnedSkill
        {
            skillId = skillId,
            level = 1,
            experience = 0
        });

        skillLevels[skillId] = 1;

        OnSkillLearned?.Invoke(skill);
        OnSkillPointsChanged?.Invoke(availableSkillPoints);

        return true;
    }

    [Server]
    public bool UpgradeSkill(string skillId)
    {
        SkillData skill = SkillDatabase.Instance?.GetSkill(skillId);
        if (skill == null) return false;

        int currentLevel = GetSkillLevel(skillId);
        if (currentLevel >= skill.maxLevel) return false;
        if (availableSkillPoints < 1) return false;

        availableSkillPoints--;

        for (int i = 0; i < learnedSkills.Count; i++)
        {
            if (learnedSkills[i].skillId == skillId)
            {
                var learned = learnedSkills[i];
                learned.level++;
                learnedSkills[i] = learned;
                skillLevels[skillId] = learned.level;
                break;
            }
        }

        OnSkillPointsChanged?.Invoke(availableSkillPoints);
        return true;
    }

    private void UpdateCooldowns()
    {
        List<string> finished = new List<string>();

        foreach (var kvp in cooldowns)
        {
            if (Time.time >= kvp.Value)
            {
                finished.Add(kvp.Key);
            }
        }

        foreach (var skillId in finished)
        {
            cooldowns.Remove(skillId);
            OnCooldownFinished?.Invoke(skillId);
        }
    }

    public float GetCooldownRemaining(string skillId)
    {
        if (cooldowns.TryGetValue(skillId, out float endTime))
            return Mathf.Max(0, endTime - Time.time);
        return 0;
    }

    public bool IsOnCooldown(string skillId)
    {
        return GetCooldownRemaining(skillId) > 0;
    }

    public bool IsOnGlobalCooldown()
    {
        return Time.time < lastGlobalCooldown;
    }

    public bool CanUseSkill(SkillData skill)
    {
        if (skill == null) return false;
        if (stats.IsDead) return false;
        if (stats.Level < skill.requiredLevel) return false;
        if (!HasLearnedSkill(skill.skillId) && skill.skillType != SkillType.Passive) return false;
        if (IsOnCooldown(skill.skillId)) return false;
        if (IsOnGlobalCooldown()) return false;
        if (isCasting) return false;

        int mpCost = Mathf.RoundToInt(skill.mpCost * Mathf.Pow(skill.mpCostPerLevel, GetSkillLevel(skill.skillId) - 1));
        if (stats.Mana < mpCost) return false;
        if (stats.Stamina < skill.spCost) return false;
        if (stats.Health <= skill.hpCost) return false;

        return true;
    }

    public bool HasLearnedSkill(string skillId)
    {
        foreach (var learned in learnedSkills)
        {
            if (learned.skillId == skillId) return true;
        }
        return false;
    }

    public int GetSkillLevel(string skillId)
    {
        if (skillLevels.TryGetValue(skillId, out int level))
            return level;
        return 0;
    }

    public void AssignSkillToBar(SkillData skill, int slot)
    {
        if (slot < 0 || slot >= maxSkillBarSlots) return;
        skillBar[slot] = skill;
    }

    public SkillData GetSkillFromBar(int slot)
    {
        if (slot < 0 || slot >= maxSkillBarSlots) return null;
        return skillBar[slot];
    }

    [ClientRpc]
    private void RpcSkillUsed(string skillId, uint targetNetId)
    {
        SkillData skill = SkillDatabase.Instance?.GetSkill(skillId);
        if (skill == null) return;

        OnSkillUsed?.Invoke(skill);
        OnSkillExecuted?.Invoke(skill);

        if (skill.executeEffect != null)
        {
            Vector3 spawnPos = transform.position + transform.forward + Vector3.up;
            Instantiate(skill.executeEffect, spawnPos, transform.rotation);
        }

        if (skill.executeSound != null && TryGetComponent(out AudioSource audio))
            audio.PlayOneShot(skill.executeSound);

        if (skill.isProjectile && skill.projectilePrefab != null)
        {
            SpawnProjectile(skill, targetNetId);
        }
    }

    private void SpawnProjectile(SkillData skill, uint targetNetId)
    {
        GameObject projectile = Instantiate(skill.projectilePrefab,
            transform.position + Vector3.up + transform.forward, transform.rotation);

        if (projectile.TryGetComponent(out SkillProjectile proj))
        {
            proj.Initialize(skill, netId, targetNetId, skill.projectileSpeed, skill.projectileHoming);
        }
    }
}

[System.Serializable]
public struct LearnedSkill : Mirror.NetworkMessage
{
    public string skillId;
    public int level;
    public int experience;
}