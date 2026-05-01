using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;

public class PlayerConsumables : NetworkBehaviour
{
    private PlayerStats stats;
    private PlayerInventory inventory;
    private PlayerEquipment equipment;

    [Header("Potion Settings")]
    [SerializeField] private float potionCooldown = 1f;

    private float lastPotionTime;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        inventory = GetComponent<PlayerInventory>();
        equipment = GetComponent<PlayerEquipment>();
    }

    public bool CanUsePotion()
    {
        return Time.time >= lastPotionTime + potionCooldown;
    }

    [Server]
    public bool UsePotion(ConsumableData potion)
    {
        if (!CanUsePotion()) return false;
        if (stats.IsDead) return false;

        lastPotionTime = Time.time;

        if (potion.restoreHP > 0)
            stats.Heal(potion.restoreHP);
        if (potion.restoreHPPercent > 0)
            stats.Heal(Mathf.RoundToInt(stats.MaxHealth * potion.restoreHPPercent / 100f));

        if (potion.restoreMP > 0)
            stats.RestoreMana(potion.restoreMP);
        if (potion.restoreMPPercent > 0)
            stats.RestoreMana(Mathf.RoundToInt(stats.MaxMana * potion.restoreMPPercent / 100f));

        if (potion.restoreSP > 0)
            stats.RestoreStamina(potion.restoreSP);

        foreach (var buff in potion.buffs)
        {
            ApplyBuff(buff);
        }

        return true;
    }

    [Server]
    private void ApplyBuff(BuffData buff)
    {
        if (TryGetComponent(out BuffManager buffManager))
        {
            buffManager.AddBuff(buff.buffName, "potion", buff.duration, buff.affectedStat, buff.value, buff.isPercent);
        }
    }
}