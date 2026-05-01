using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "TOP/Item")]
public class ItemData : ScriptableObject
{
    [Header("Informações Básicas")]
    public string itemId;
    public string itemName;
    public string description;
    public Sprite icon;
    public ItemType itemType = ItemType.Other;
    public ItemRarity rarity = ItemRarity.Normal;

    [Header("Restrições")]
    public int requiredLevel = 1;
    public CharacterClass requiredClass = CharacterClass.None;
    public bool isTradable = true;
    public bool isDroppable = true;
    public int maxStack = 1;

    [Header("Economia")]
    public int buyPrice;
    public int sellPrice;
    public int weight = 1;

    [Header("Visual")]
    public GameObject worldModelPrefab;
    public Vector3 dropRotation = new Vector3(0, 0, 0);
    public float dropScale = 1f;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(itemId))
        {
            itemId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
        }
    }
}