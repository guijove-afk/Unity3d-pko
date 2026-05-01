using UnityEngine;
using System.Collections.Generic;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    [SerializeField] private List<ItemData> allItems = new List<ItemData>();
    [SerializeField] private List<EquipmentData> allEquipment = new List<EquipmentData>();
    [SerializeField] private List<ConsumableData> allConsumables = new List<ConsumableData>();

    private Dictionary<string, ItemData> itemDictionary = new Dictionary<string, ItemData>();
    private Dictionary<string, EquipmentData> equipmentDictionary = new Dictionary<string, EquipmentData>();
    private Dictionary<string, ConsumableData> consumableDictionary = new Dictionary<string, ConsumableData>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildDictionaries();
    }

    private void BuildDictionaries()
    {
        itemDictionary.Clear();
        equipmentDictionary.Clear();
        consumableDictionary.Clear();

        foreach (var item in allItems)
        {
            if (item != null && !string.IsNullOrEmpty(item.itemId))
                itemDictionary[item.itemId] = item;
        }

        foreach (var equip in allEquipment)
        {
            if (equip != null && !string.IsNullOrEmpty(equip.itemId))
            {
                equipmentDictionary[equip.itemId] = equip;
                itemDictionary[equip.itemId] = equip;
            }
        }

        foreach (var consumable in allConsumables)
        {
            if (consumable != null && !string.IsNullOrEmpty(consumable.itemId))
            {
                consumableDictionary[consumable.itemId] = consumable;
                itemDictionary[consumable.itemId] = consumable;
            }
        }
    }

    public ItemData GetItem(string itemId)
    {
        return itemDictionary.TryGetValue(itemId, out var item) ? item : null;
    }

    public EquipmentData GetEquipment(string itemId)
    {
        return equipmentDictionary.TryGetValue(itemId, out var equip) ? equip : null;
    }

    public ConsumableData GetConsumable(string itemId)
    {
        return consumableDictionary.TryGetValue(itemId, out var consumable) ? consumable : null;
    }

    public List<EquipmentData> GetEquipmentBySlot(EquipmentSlot slot)
    {
        List<EquipmentData> result = new List<EquipmentData>();
        foreach (var equip in allEquipment)
        {
            if (equip != null && equip.slot == slot)
                result.Add(equip);
        }
        return result;
    }

    public List<EquipmentData> GetEquipmentByClass(CharacterClass characterClass)
    {
        List<EquipmentData> result = new List<EquipmentData>();
        foreach (var equip in allEquipment)
        {
            if (equip != null && (equip.requiredClass == CharacterClass.None || equip.requiredClass == characterClass))
                result.Add(equip);
        }
        return result;
    }

    public List<ItemData> GetItemsByLevelRange(int minLevel, int maxLevel)
    {
        List<ItemData> result = new List<ItemData>();
        foreach (var item in allItems)
        {
            if (item != null && item.requiredLevel >= minLevel && item.requiredLevel <= maxLevel)
                result.Add(item);
        }
        return result;
    }

#if UNITY_EDITOR
    [ContextMenu("Auto-Register All Items")]
    private void AutoRegister()
    {
        allItems.Clear();
        allEquipment.Clear();
        allConsumables.Clear();

        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemData");
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null)
            {
                allItems.Add(item);
                if (item is EquipmentData equip)
                    allEquipment.Add(equip);
                else if (item is ConsumableData consumable)
                    allConsumables.Add(consumable);
            }
        }

        UnityEditor.EditorUtility.SetDirty(this);
        BuildDictionaries();
        Debug.Log($"[ItemDatabase] Registrados: {allItems.Count} itens, {allEquipment.Count} equipamentos, {allConsumables.Count} consumíveis");
    }
#endif
}