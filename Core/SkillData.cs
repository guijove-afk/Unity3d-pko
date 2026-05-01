using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "TOP/Skill")]
public class SkillData : ScriptableObject
{
    [Header("Informações")]
    public string skillId;
    public string skillName;
    public string description;
    public Sprite icon;
    public SkillType skillType = SkillType.Active;
    public SkillTargetType targetType = SkillTargetType.SingleEnemy;

    [Header("Requisitos")]
    public CharacterClass requiredClass = CharacterClass.None;
    public int requiredLevel = 1;
    public int requiredSkillPoints = 1;
    public string[] prerequisiteSkills;

    [Header("Custo")]
    public int mpCost;
    public int spCost;
    public int hpCost;
    public int itemCost;

    [Header("Dano/Efeito")]
    public int baseDamage;
    public float damageMultiplier = 1.0f;
    public SkillElement element = SkillElement.None;
    public float elementMultiplier = 1.0f;
    public int healAmount;
    public float healMultiplier = 1.0f;

    [Header("Área de Efeito")]
    public float range = 2f;
    public float areaRadius;
    public float coneAngle;
    public int maxTargets = 1;

    [Header("Casting")]
    public float castTime;
    public float cooldown;
    public bool canMoveWhileCasting;
    public bool canBeInterrupted = true;

    [Header("Animação")]
    public string animationTrigger = "Skill";
    public int animationVariant;
    public float animationSpeed = 1f;

    [Header("VFX")]
    public ParticleSystem castEffect;
    public ParticleSystem executeEffect;
    public ParticleSystem hitEffect;
    public AudioClip castSound;
    public AudioClip executeSound;
    public AudioClip hitSound;

    [Header("Projétil")]
    public bool isProjectile;
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;
    public bool projectileHoming;

    [Header("Buffs/Debuffs Aplicados")]
    public BuffData[] appliedBuffs;
    public BuffData[] appliedDebuffs;

    [Header("Evolução")]
    public int maxLevel = 10;
    public float damagePerLevel = 1.1f;
    public float mpCostPerLevel = 1.05f;
    public float cooldownPerLevel = 0.95f;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(skillId))
        {
            skillId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
        }
    }
}