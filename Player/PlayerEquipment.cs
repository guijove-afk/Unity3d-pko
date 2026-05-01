using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;

public class PlayerEquipment : NetworkBehaviour
{
    [Header("Bone References")]
    [SerializeField] private Transform headBone;
    [SerializeField] private Transform bodyBone;
    [SerializeField] private Transform rightHandBone;
    [SerializeField] private Transform leftHandBone;
    [SerializeField] private Transform backBone;
    [SerializeField] private Transform feetBone;

    public SyncList<EquippedItem> equippedItems = new SyncList<EquippedItem>();

    private Dictionary<EquipmentSlot, GameObject> currentModels = new Dictionary<EquipmentSlot, GameObject>();
    private Dictionary<EquipmentSlot, EquipmentData> equippedData = new Dictionary<EquipmentSlot, EquipmentData>();
    private PlayerStats stats;
    private PlayerAnimation playerAnim;

    public event Action<EquipmentSlot, EquipmentData> OnItemEquipped;
    public event Action<EquipmentSlot> OnItemUnequipped;
    public event Action OnEquipmentChanged;

    public IReadOnlyDictionary<EquipmentSlot, EquipmentData> EquippedItems => equippedData;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        playerAnim = GetComponent<PlayerAnimation>();

        equippedItems.Callback += OnEquippedItemsChanged;
    }

    [Server]
    public bool EquipItem(string itemId)
    {
        EquipmentData item = ItemDatabase.Instance?.GetEquipment(itemId);
        if (item == null) return false;
        if (!CanEquip(item)) return false;

        UnequipItem(item.slot);

        EquippedItem slot = new EquippedItem
        {
            itemId = itemId,
            durability = item.durability,
            gemSlots = new string[item.gemSlots]
        };

        int slotIndex = (int)item.slot;
        while (equippedItems.Count <= slotIndex)
            equippedItems.Add(new EquippedItem());

        equippedItems[slotIndex] = slot;
        equippedData[item.slot] = item;

        ApplyEquipmentStats(item);

        OnItemEquipped?.Invoke(item.slot, item);
        OnEquipmentChanged?.Invoke();

        return true;
    }

    [Server]
    public void UnequipItem(EquipmentSlot slot)
    {
        int slotIndex = (int)slot;
        if (slotIndex >= equippedItems.Count) return;

        var equipped = equippedItems[slotIndex];
        if (string.IsNullOrEmpty(equipped.itemId)) return;

        EquipmentData item = ItemDatabase.Instance?.GetEquipment(equipped.itemId);
        if (item != null)
        {
            RemoveEquipmentStats(item);
        }

        equippedItems[slotIndex] = new EquippedItem();
        equippedData.Remove(slot);

        OnItemUnequipped?.Invoke(slot);
        OnEquipmentChanged?.Invoke();
    }

    [Server]
    public bool CanEquip(EquipmentData item)
    {
        if (item == null) return false;
        if (stats.Level < item.requiredLevel) return false;
        if (item.requiredClass != CharacterClass.None && item.requiredClass != stats.CharacterClass) return false;

        if (item.slot == EquipmentSlot.Weapon)
        {
            CharacterClassData classData = Resources.Load<CharacterClassData>($"Classes/{stats.CharacterClass}");
            if (classData != null && classData.allowedWeapons != null)
            {
                bool allowed = false;
                foreach (var weaponType in classData.allowedWeapons)
                {
                    if (weaponType == item.weaponType)
                    {
                        allowed = true;
                        break;
                    }
                }
                if (!allowed) return false;
            }
        }

        if (item.slot == EquipmentSlot.Armor)
        {
            CharacterClassData classData = Resources.Load<CharacterClassData>($"Classes/{stats.CharacterClass}");
            if (classData != null && classData.allowedArmors != null)
            {
                bool allowed = false;
                foreach (var armorType in classData.allowedArmors)
                {
                    if (armorType == item.armorType)
                    {
                        allowed = true;
                        break;
                    }
                }
                if (!allowed) return false;
            }
        }

        return true;
    }

    [Server]
    public void RepairItem(EquipmentSlot slot, int amount)
    {
        int slotIndex = (int)slot;
        if (slotIndex >= equippedItems.Count) return;

        var equipped = equippedItems[slotIndex];
        if (string.IsNullOrEmpty(equipped.itemId)) return;

        EquipmentData item = ItemDatabase.Instance?.GetEquipment(equipped.itemId);
        if (item == null) return;

        equipped.durability = Mathf.Min(item.maxDurability, equipped.durability + amount);
        equippedItems[slotIndex] = equipped;
    }

    [Server]
    public void DamageItem(EquipmentSlot slot, int amount)
    {
        int slotIndex = (int)slot;
        if (slotIndex >= equippedItems.Count) return;

        var equipped = equippedItems[slotIndex];
        if (string.IsNullOrEmpty(equipped.itemId)) return;

        equipped.durability = Mathf.Max(0, equipped.durability - amount);
        equippedItems[slotIndex] = equipped;

        if (equipped.durability <= 0)
        {
            RpcItemBroke(slot);
        }
    }

    [Server]
    private void ApplyEquipmentStats(EquipmentData item)
    {
        stats.AddModifier(new StatModifier { statType = StatType.STR, value = item.bonusSTR, sourceId = item.itemId });
        stats.AddModifier(new StatModifier { statType = StatType.AGI, value = item.bonusAGI, sourceId = item.itemId });
        stats.AddModifier(new StatModifier { statType = StatType.CON, value = item.bonusCON, sourceId = item.itemId });
        stats.AddModifier(new StatModifier { statType = StatType.SPR, value = item.bonusSPR, sourceId = item.itemId });
        stats.AddModifier(new StatModifier { statType = StatType.ACC, value = item.bonusACC, sourceId = item.itemId });
        stats.AddModifier(new StatModifier { statType = StatType.LUCK, value = item.bonusLUCK, sourceId = item.itemId });
        stats.AddModifier(new StatModifier { statType = StatType.HP, value = item.bonusHP, sourceId = item.itemId });
        stats.AddModifier(new StatModifier { statType = StatType.MP, value = item.bonusMP, sourceId = item.itemId });
        stats.AddModifier(new StatModifier { statType = StatType.SP, value = item.bonusSP, sourceId = item.itemId });
        stats.AddModifier(new StatModifier { statType = StatType.Attack, value = item.bonusAttack, sourceId = item.itemId });
        stats.AddModifier(new StatModifier { statType = StatType.Defense, value = item.bonusDefense, sourceId = item.itemId });
        stats.AddModifier(new StatModifier { statType = StatType.MagicAttack, value = item.bonusMagicAttack, sourceId = item.itemId });
        stats.AddModifier(new StatModifier { statType = StatType.MagicDefense, value = item.bonusMagicDefense, sourceId = item.itemId });
        stats.AddModifier(new StatModifier { statType = StatType.AttackSpeed, value = Mathf.RoundToInt(item.bonusAttackSpeed * 100), sourceId = item.itemId });
        stats.AddModifier(new StatModifier { statType = StatType.MoveSpeed, value = Mathf.RoundToInt(item.bonusMoveSpeed * 100), sourceId = item.itemId });
    }

    [Server]
    private void RemoveEquipmentStats(EquipmentData item)
    {
        stats.RemoveModifier(new StatModifier { statType = StatType.STR, value = item.bonusSTR, sourceId = item.itemId });
        stats.RemoveModifier(new StatModifier { statType = StatType.AGI, value = item.bonusAGI, sourceId = item.itemId });
        stats.RemoveModifier(new StatModifier { statType = StatType.CON, value = item.bonusCON, sourceId = item.itemId });
        stats.RemoveModifier(new StatModifier { statType = StatType.SPR, value = item.bonusSPR, sourceId = item.itemId });
        stats.RemoveModifier(new StatModifier { statType = StatType.ACC, value = item.bonusACC, sourceId = item.itemId });
        stats.RemoveModifier(new StatModifier { statType = StatType.LUCK, value = item.bonusLUCK, sourceId = item.itemId });
        stats.RemoveModifier(new StatModifier { statType = StatType.HP, value = item.bonusHP, sourceId = item.itemId });
        stats.RemoveModifier(new StatModifier { statType = StatType.MP, value = item.bonusMP, sourceId = item.itemId });
        stats.RemoveModifier(new StatModifier { statType = StatType.SP, value = item.bonusSP, sourceId = item.itemId });
        stats.RemoveModifier(new StatModifier { statType = StatType.Attack, value = item.bonusAttack, sourceId = item.itemId });
        stats.RemoveModifier(new StatModifier { statType = StatType.Defense, value = item.bonusDefense, sourceId = item.itemId });
        stats.RemoveModifier(new StatModifier { statType = StatType.MagicAttack, value = item.bonusMagicAttack, sourceId = item.itemId });
        stats.RemoveModifier(new StatModifier { statType = StatType.MagicDefense, value = item.bonusMagicDefense, sourceId = item.itemId });
        stats.RemoveModifier(new StatModifier { statType = StatType.AttackSpeed, value = Mathf.RoundToInt(item.bonusAttackSpeed * 100), sourceId = item.itemId });
        stats.RemoveModifier(new StatModifier { statType = StatType.MoveSpeed, value = Mathf.RoundToInt(item.bonusMoveSpeed * 100), sourceId = item.itemId });
    }

    private void OnEquippedItemsChanged(SyncList<EquippedItem>.Operation op, int index,
        EquippedItem oldItem, EquippedItem newItem)
    {
        switch (op)
        {
            case SyncList<EquippedItem>.Operation.OP_ADD:
            case SyncList<EquippedItem>.Operation.OP_INSERT:
            case SyncList<EquippedItem>.Operation.OP_SET:
                if (!string.IsNullOrEmpty(newItem.itemId))
                {
                    EquipmentData item = ItemDatabase.Instance?.GetEquipment(newItem.itemId);
                    if (item != null)
                        EquipVisual((EquipmentSlot)index, item);
                }
                else
                {
                    UnequipVisual((EquipmentSlot)index);
                }
                break;

            case SyncList<EquippedItem>.Operation.OP_REMOVEAT:
            case SyncList<EquippedItem>.Operation.OP_CLEAR:
                UnequipVisual((EquipmentSlot)index);
                break;
        }
    }

    private void EquipVisual(EquipmentSlot slot, EquipmentData item)
    {
        UnequipVisual(slot);

        GameObject prefab = stats.IsMale ? item.maleModelPrefab : item.femaleModelPrefab;
        if (prefab == null) prefab = item.maleModelPrefab;
        if (prefab == null) return;

        Transform bone = GetBoneForSlot(slot);
        if (bone == null) return;

        GameObject model = Instantiate(prefab, bone);
        model.transform.localPosition = item.modelOffset;
        model.transform.localRotation = Quaternion.Euler(item.modelRotation);
        model.transform.localScale = item.modelScale;

        currentModels[slot] = model;

        if (item.equipEffect != null)
        {
            ParticleSystem effect = Instantiate(item.equipEffect, model.transform.position, Quaternion.identity);
            Destroy(effect.gameObject, 2f);
        }

        if (item.equipSound != null && TryGetComponent(out AudioSource audio))
            audio.PlayOneShot(item.equipSound);
    }

    private void UnequipVisual(EquipmentSlot slot)
    {
        if (currentModels.ContainsKey(slot))
        {
            Destroy(currentModels[slot]);
            currentModels.Remove(slot);
        }
    }

    private Transform GetBoneForSlot(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Helmet => headBone,
            EquipmentSlot.Armor => bodyBone,
            EquipmentSlot.Weapon => rightHandBone,
            EquipmentSlot.Shield => leftHandBone,
            EquipmentSlot.Gloves => rightHandBone,
            EquipmentSlot.Boots => feetBone,
            EquipmentSlot.Cape => backBone,
            EquipmentSlot.Costume => bodyBone,
            _ => null
        };
    }

    [ClientRpc]
    private void RpcItemBroke(EquipmentSlot slot)
    {
        DamagePopupManager.Instance?.ShowText(transform.position + Vector3.up * 2f,
            $"{slot} Quebrou!", Color.red);
    }

    public EquipmentData GetEquippedItem(EquipmentSlot slot)
    {
        return equippedData.TryGetValue(slot, out var item) ? item : null;
    }

    public int GetTotalDefense()
    {
        int total = 0;
        foreach (var item in equippedData.Values)
        {
            total += item.bonusDefense;
        }
        return total;
    }

    public int GetTotalAttack()
    {
        int total = 0;
        foreach (var item in equippedData.Values)
        {
            total += item.bonusAttack;
        }
        return total;
    }

    public bool HasFullSet(string setName)
    {
        return false;
    }
}

[System.Serializable]
public struct EquippedItem : Mirror.NetworkMessage
{
    public string itemId;
    public int durability;
    public string[] gemSlots;
    public int refineLevel;
}