using UnityEngine;
using Mirror;
using System;

public class PlayerHotbar : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxSlots = 10;

    public SyncList<HotbarSlot> hotbarSlots = new SyncList<HotbarSlot>();

    private PlayerSkills skills;
    private PlayerInventory inventory;

    public event Action<int, HotbarSlot> OnSlotChanged;
    public event Action<int> OnSlotActivated;

    void Awake()
    {
        skills = GetComponent<PlayerSkills>();
        inventory = GetComponent<PlayerInventory>();
    }

    void Start()
    {
        if (isServer)
        {
            while (hotbarSlots.Count < maxSlots)
            {
                hotbarSlots.Add(new HotbarSlot { type = HotbarSlotType.Empty });
            }
        }

        hotbarSlots.Callback += OnHotbarChanged;
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        HandleInput();
    }

    private void HandleInput()
    {
        for (int i = 0; i < maxSlots && i < 10; i++)
        {
            if (Input.GetKeyDown(KeyCode.F1 + i))
            {
                ActivateSlot(i);
            }
        }

        for (int i = 0; i < maxSlots && i < 10; i++)
        {
            KeyCode key = i == 9 ? KeyCode.Alpha0 : KeyCode.Alpha1 + i;
            if (Input.GetKeyDown(key))
            {
                ActivateSlot(i);
            }
        }
    }

    private void ActivateSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= hotbarSlots.Count) return;

        var slot = hotbarSlots[slotIndex];

        switch (slot.type)
        {
            case HotbarSlotType.Skill:
                SkillData skill = SkillDatabase.Instance?.GetSkill(slot.id);
                if (skill != null)
                    skills?.TryUseSkill(skill);
                break;

            case HotbarSlotType.Item:
                int invSlot = inventory?.FindItemSlot(slot.id) ?? -1;
                if (invSlot >= 0)
                    inventory?.CmdUseItem(invSlot);
                break;

            case HotbarSlotType.Emote:
                if (TryGetComponent(out PlayerAnimation anim))
                    anim.PlayEmote(slot.emoteId);
                break;
        }

        OnSlotActivated?.Invoke(slotIndex);
    }

    [Command]
    public void CmdSetSlot(int slotIndex, HotbarSlot slot)
    {
        if (slotIndex < 0 || slotIndex >= hotbarSlots.Count) return;
        hotbarSlots[slotIndex] = slot;
    }

    [Command]
    public void CmdClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= hotbarSlots.Count) return;
        hotbarSlots[slotIndex] = new HotbarSlot { type = HotbarSlotType.Empty };
    }

    private void OnHotbarChanged(SyncList<HotbarSlot>.Operation op, int index,
        HotbarSlot oldSlot, HotbarSlot newSlot)
    {
        OnSlotChanged?.Invoke(index, newSlot);
    }

    public HotbarSlot GetSlot(int index)
    {
        if (index < 0 || index >= hotbarSlots.Count)
            return new HotbarSlot { type = HotbarSlotType.Empty };
        return hotbarSlots[index];
    }
}

[System.Serializable]
public struct HotbarSlot : Mirror.NetworkMessage
{
    public HotbarSlotType type;
    public string id;
    public int emoteId;
}