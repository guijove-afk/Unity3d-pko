using UnityEngine;

[CreateAssetMenu(fileName = "NewConsumable", menuName = "TOP/Consumable")]
public class ConsumableData : ItemData
{
    [Header("Consumível")]
    public ConsumableType consumableType;

    [Header("Efeitos")]
    public int restoreHP;
    public int restoreMP;
    public int restoreSP;
    public int restoreHPPercent;
    public int restoreMPPercent;

    [Header("Buffs")]
    public BuffData[] buffs;

    [Header("Uso")]
    public float castTime = 0.5f;
    public float cooldown = 1.0f;
    public bool canUseInCombat = true;
    public bool canUseWhileMoving = false;

    [Header("Efeitos Visuais")]
    public ParticleSystem useEffect;
    public AudioClip useSound;
}

[System.Serializable]
public class BuffData
{
    public string buffName;
    public StatType affectedStat;
    public int value;
    public float duration;
    public bool isPercent;
    public ParticleSystem buffEffect;
}