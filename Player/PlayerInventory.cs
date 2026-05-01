using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;

public class PlayerInventory : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int inventoryWidth = 6;
    [SerializeField] private int inventoryHeight = 8;
    [SerializeField] private int maxGold = 999999999;

    public SyncList<InventorySlot> inventorySlots = new SyncList<InventorySlot>();

    private PlayerStats stats;
    private PlayerEquipment equipment;

    public event Action<int, InventorySlot> OnSlotChanged;
    public event Action<ItemData, int> OnItemAdded;
    public event Action<ItemData, int> OnItemRemoved;
    public event Action OnGoldChanged;

    public int InventoryWidth => inventoryWidth;
    public int InventoryHeight => inventoryHeight;
    public int TotalSlots => inventoryWidth * inventoryHeight;
    public int UsedSlots { get; private set; }
    public int FreeSlots => TotalSlots - UsedSlots;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        equipment = GetComponent<PlayerEquipment>();

        inventorySlots.Callback += OnInventoryChanged;
    }

    void Start()
    {
        if (isServer)
        {
            while (inventorySlots.Count < TotalSlots)
            {
                inventorySlots.Add(new InventorySlot());
            }
        }
    }

    [Server]
    public bool AddItem(string itemId, int quantity = 1)
    {
        ItemData item = ItemDatabase.Instance?.GetItem(itemId);
        if (item == null) return false;
        if (quantity <= 0) return false;

        int remaining = quantity;

        if (item.maxStack > 1)
        {
            for (int i = 0; i < inventorySlots.Count && remaining > 0; i++)
            {
                if (inventorySlots[i].itemId == itemId && inventorySlots[i].quantity < item.maxStack)
                {
                    int canAdd = Mathf.Min(remaining, item.maxStack - inventorySlots[i].quantity);
                    var slot = inventorySlots[i];
                    slot.quantity += canAdd;
                    inventorySlots[i] = slot;
                    remaining -= canAdd;
                }
            }
        }

        for (int i = 0; i < inventorySlots.Count && remaining > 0; i++)
        {
            if (string.IsNullOrEmpty(inventorySlots[i].itemId))
            {
                int canAdd = Mathf.Min(remaining, item.maxStack);
                inventorySlots[i] = new InventorySlot
                {
                    itemId = itemId,
                    quantity = canAdd,
                    durability = item is EquipmentData equip ? equip.durability : 100
                };
                remaining -= canAdd;
            }
        }

        if (remaining < quantity)
        {
            OnItemAdded?.Invoke(item, quantity - remaining);
            UpdateUsedSlots();
            return remaining == 0;
        }

        return false;
    }

    [Server]
    public bool RemoveItem(string itemId, int quantity = 1)
    {
        if (quantity <= 0) return false;

        int remaining = quantity;

        for (int i = inventorySlots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            if (inventorySlots[i].itemId == itemId)
            {
                int canRemove = Mathf.Min(remaining, inventorySlots[i].quantity);
                var slot = inventorySlots[i];
                slot.quantity -= canRemove;
                remaining -= canRemove;

                if (slot.quantity <= 0)
                {
                    slot = new InventorySlot();
                }

                inventorySlots[i] = slot;
            }
        }

        if (remaining < quantity)
        {
            ItemData item = ItemDatabase.Instance?.GetItem(itemId);
            if (item != null)
                OnItemRemoved?.Invoke(item, quantity - remaining);

            UpdateUsedSlots();
            return remaining == 0;
        }

        return false;
    }

    [Server]
    public bool RemoveItemAt(int slotIndex, int quantity = 1)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots.Count) return false;
        if (string.IsNullOrEmpty(inventorySlots[slotIndex].itemId)) return false;

        var slot = inventorySlots[slotIndex];
        string itemId = slot.itemId;

        if (slot.quantity <= quantity)
        {
            inventorySlots[slotIndex] = new InventorySlot();
        }
        else
        {
            slot.quantity -= quantity;
            inventorySlots[slotIndex] = slot;
        }

        ItemData item = ItemDatabase.Instance?.GetItem(itemId);
        if (item != null)
            OnItemRemoved?.Invoke(item, quantity);

        UpdateUsedSlots();
        return true;
    }

    [Command]
    public void CmdMoveItem(int fromSlot, int toSlot)
    {
        if (fromSlot < 0 || fromSlot >= inventorySlots.Count) return;
        if (toSlot < 0 || toSlot >= inventorySlots.Count) return;
        if (fromSlot == toSlot) return;

        var from = inventorySlots[fromSlot];
        var to = inventorySlots[toSlot];

        if (from.itemId == to.itemId && !string.IsNullOrEmpty(from.itemId))
        {
            ItemData item = ItemDatabase.Instance?.GetItem(from.itemId);
            if (item != null && item.maxStack > 1)
            {
                int canStack = Mathf.Min(from.quantity, item.maxStack - to.quantity);
                if (canStack > 0)
                {
                    to.quantity += canStack;
                    from.quantity -= canStack;

                    if (from.quantity <= 0)
                        from = new InventorySlot();

                    inventorySlots[fromSlot] = from;
                    inventorySlots[toSlot] = to;
                    return;
                }
            }
        }

        inventorySlots[fromSlot] = to;
        inventorySlots[toSlot] = from;
    }

    [Command]
    public void CmdSplitItem(int slotIndex, int amount)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots.Count) return;

        var slot = inventorySlots[slotIndex];
        if (string.IsNullOrEmpty(slot.itemId)) return;
        if (slot.quantity <= amount) return;

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (string.IsNullOrEmpty(inventorySlots[i].itemId))
            {
                inventorySlots[i] = new InventorySlot
                {
                    itemId = slot.itemId,
                    quantity = amount,
                    durability = slot.durability
                };

                slot.quantity -= amount;
                inventorySlots[slotIndex] = slot;
                return;
            }
        }
    }

    [Command]
    public void CmdUseItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots.Count) return;

        var slot = inventorySlots[slotIndex];
        if (string.IsNullOrEmpty(slot.itemId)) return;

        ItemData item = ItemDatabase.Instance?.GetItem(slot.itemId);
        if (item == null) return;

        if (item is ConsumableData consumable)
        {
            UseConsumable(consumable, slotIndex);
        }
        else if (item is EquipmentData equipmentData)
        {
            EquipItem(equipmentData, slotIndex);
        }
    }

    [Server]
    private void UseConsumable(ConsumableData consumable, int slotIndex)
    {
        if (stats.Level < consumable.requiredLevel) return;

        if (consumable.restoreHP > 0)
            stats.Heal(consumable.restoreHP);
        if (consumable.restoreHPPercent > 0)
            stats.Heal(Mathf.RoundToInt(stats.MaxHealth * consumable.restoreHPPercent / 100f));

        if (consumable.restoreMP > 0)
            stats.RestoreMana(consumable.restoreMP);
        if (consumable.restoreMPPercent > 0)
            stats.RestoreMana(Mathf.RoundToInt(stats.MaxMana * consumable.restoreMPPercent / 100f));

        if (consumable.restoreSP > 0)
            stats.RestoreStamina(consumable.restoreSP);

        RemoveItemAt(slotIndex, 1);

        RpcItemUsed(consumable.itemId);
    }

    [Server]
    private void EquipItem(EquipmentData equipmentData, int slotIndex)
    {
        if (equipment == null) return;

        if (equipment.EquipItem(equipmentData.itemId))
        {
            RemoveItemAt(slotIndex, 1);
        }
    }

    [Command]
    public void CmdUnequipItem(EquipmentSlot slot)
    {
        if (equipment == null) return;

        EquipmentData item = equipment.GetEquippedItem(slot);
        if (item == null) return;

        if (AddItem(item.itemId))
        {
            equipment.UnequipItem(slot);
        }
    }

    [Command]
    public void CmdDropItem(int slotIndex, int quantity)
    {
        if (slotIndex < 0 || slotIndex >= inventorySlots.Count) return;

        var slot = inventorySlots[slotIndex];
        if (string.IsNullOrEmpty(slot.itemId)) return;

        ItemData item = ItemDatabase.Instance?.GetItem(slot.itemId);
        if (item == null || !item.isDroppable) return;

        quantity = Mathf.Min(quantity, slot.quantity);

        SpawnWorldItem(item, quantity, transform.position + transform.forward);

        RemoveItemAt(slotIndex, quantity);
    }

    [Server]
    private void SpawnWorldItem(ItemData item, int quantity, Vector3 position)
    {
        GameObject worldItemObj = new GameObject($"WorldItem_{item.itemName}");
        worldItemObj.transform.position = position;

        WorldItem worldItem = worldItemObj.AddComponent<WorldItem>();
        worldItem.Initialize(item, quantity);
    }

    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= inventorySlots.Count)
            return new InventorySlot();
        return inventorySlots[index];
    }

    public int GetItemQuantity(string itemId)
    {
        int total = 0;
        foreach (var slot in inventorySlots)
        {
            if (slot.itemId == itemId)
                total += slot.quantity;
        }
        return total;
    }

    public bool HasItem(string itemId, int quantity = 1)
    {
        return GetItemQuantity(itemId) >= quantity;
    }

    public int FindItemSlot(string itemId)
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i].itemId == itemId)
                return i;
        }
        return -1;
    }

    public int FindEmptySlot()
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (string.IsNullOrEmpty(inventorySlots[i].itemId))
                return i;
        }
        return -1;
    }

    private void OnInventoryChanged(SyncList<InventorySlot>.Operation op, int index,
        InventorySlot oldItem, InventorySlot newItem)
    {
        OnSlotChanged?.Invoke(index, newItem);
    }

    private void UpdateUsedSlots()
    {
        UsedSlots = 0;
        foreach (var slot in inventorySlots)
        {
            if (!string.IsNullOrEmpty(slot.itemId))
                UsedSlots++;
        }
    }

    [ClientRpc]
    private void RpcItemUsed(string itemId)
    {
        ItemData item = ItemDatabase.Instance?.GetItem(itemId);
        if (item is ConsumableData consumable)
        {
            if (consumable.useEffect != null)
            {
                Instantiate(consumable.useEffect, transform.position + Vector3.up, Quaternion.identity);
            }
            if (consumable.useSound != null && TryGetComponent(out AudioSource audio))
                audio.PlayOneShot(consumable.useSound);
        }
    }
}

[System.Serializable]
public struct InventorySlot : Mirror.NetworkMessage
{
    public string itemId;
    public int quantity;
    public int durability;
    public int refineLevel;
    public string[] gemSlots;
}