using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;

public class BuffManager : NetworkBehaviour
{
    public SyncList<ActiveBuff> activeBuffs = new SyncList<ActiveBuff>();

    private PlayerStats stats;
    private Dictionary<string, GameObject> buffEffects = new Dictionary<string, GameObject>();

    public event Action<ActiveBuff> OnBuffAdded;
    public event Action<ActiveBuff> OnBuffRemoved;
    public event Action<ActiveBuff> OnBuffRefreshed;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (!isServer) return;

        UpdateBuffs();
    }

    [Server]
    private void UpdateBuffs()
    {
        List<int> expiredIndices = new List<int>();

        for (int i = 0; i < activeBuffs.Count; i++)
        {
            var buff = activeBuffs[i];
            buff.remainingTime -= Time.deltaTime;

            buff.tickTimer -= Time.deltaTime;
            if (buff.tickTimer <= 0 && buff.tickInterval > 0)
            {
                buff.tickTimer = buff.tickInterval;
                ApplyTickEffect(buff);
            }

            if (buff.remainingTime <= 0)
            {
                expiredIndices.Add(i);
            }
            else
            {
                activeBuffs[i] = buff;
            }
        }

        for (int i = expiredIndices.Count - 1; i >= 0; i--)
        {
            int index = expiredIndices[i];
            var buff = activeBuffs[index];
            RemoveBuff(index);
        }
    }

    [Server]
    public void AddBuff(string buffId, string sourceId, float duration, StatType statType, int value, bool isPercent = false)
    {
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            if (activeBuffs[i].buffId == buffId)
            {
                var buff = activeBuffs[i];
                buff.remainingTime = duration;
                activeBuffs[i] = buff;
                OnBuffRefreshed?.Invoke(buff);
                return;
            }
        }

        ActiveBuff newBuff = new ActiveBuff
        {
            buffId = buffId,
            sourceId = sourceId,
            remainingTime = duration,
            totalDuration = duration,
            statType = statType,
            value = value,
            isPercent = isPercent,
            tickInterval = 0,
            tickTimer = 0
        };

        activeBuffs.Add(newBuff);
        ApplyBuffEffect(newBuff);
        OnBuffAdded?.Invoke(newBuff);
    }

    [Server]
    public void AddDotHot(string buffId, string sourceId, float duration, float tickInterval, int tickValue, bool isHeal)
    {
        ActiveBuff newBuff = new ActiveBuff
        {
            buffId = buffId,
            sourceId = sourceId,
            remainingTime = duration,
            totalDuration = duration,
            statType = isHeal ? StatType.HP : StatType.HP,
            value = tickValue,
            isPercent = false,
            tickInterval = tickInterval,
            tickTimer = tickInterval,
            isDot = !isHeal,
            isHot = isHeal
        };

        activeBuffs.Add(newBuff);
        OnBuffAdded?.Invoke(newBuff);
    }

    [Server]
    private void ApplyBuffEffect(ActiveBuff buff)
    {
        StatModifier modifier = new StatModifier
        {
            statType = buff.statType,
            value = buff.value,
            isPercent = buff.isPercent,
            sourceId = buff.buffId,
            duration = buff.totalDuration
        };

        stats.AddModifier(modifier);
    }

    [Server]
    private void ApplyTickEffect(ActiveBuff buff)
    {
        if (buff.isHot)
        {
            stats.Heal(buff.value);
        }
        else if (buff.isDot)
        {
            stats.TakeTrueDamage(buff.value, 0);
        }
    }

    [Server]
    private void RemoveBuff(int index)
    {
        if (index < 0 || index >= activeBuffs.Count) return;

        var buff = activeBuffs[index];

        StatModifier modifier = new StatModifier
        {
            statType = buff.statType,
            value = buff.value,
            isPercent = buff.isPercent,
            sourceId = buff.buffId
        };
        stats.RemoveModifier(modifier);

        if (buffEffects.ContainsKey(buff.buffId))
        {
            Destroy(buffEffects[buff.buffId]);
            buffEffects.Remove(buff.buffId);
        }

        OnBuffRemoved?.Invoke(buff);
        activeBuffs.RemoveAt(index);
    }

    [Server]
    public void RemoveBuffsBySource(string sourceId)
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            if (activeBuffs[i].sourceId == sourceId)
            {
                RemoveBuff(i);
            }
        }
    }

    [Server]
    public void RemoveAllBuffs()
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            RemoveBuff(i);
        }
    }

    public bool HasBuff(string buffId)
    {
        foreach (var buff in activeBuffs)
        {
            if (buff.buffId == buffId) return true;
        }
        return false;
    }

    public float GetBuffRemainingTime(string buffId)
    {
        foreach (var buff in activeBuffs)
        {
            if (buff.buffId == buffId) return buff.remainingTime;
        }
        return 0;
    }

    public IReadOnlyList<ActiveBuff> GetActiveBuffs()
    {
        return activeBuffs;
    }
}

[System.Serializable]
public struct ActiveBuff : Mirror.NetworkMessage
{
    public string buffId;
    public string sourceId;
    public float remainingTime;
    public float totalDuration;
    public StatType statType;
    public int value;
    public bool isPercent;
    public float tickInterval;
    public float tickTimer;
    public bool isDot;
    public bool isHot;
}