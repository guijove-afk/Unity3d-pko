using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewSet", menuName = "TOP/Set")]
public class SetData : ScriptableObject
{
    [Header("Informações")]
    public string setName;
    public string setDescription;
    public Sprite setIcon;

    [Header("Bônus")]
    public List<SetBonus> bonuses = new List<SetBonus>();

    [Header("Itens do Set")]
    public List<EquipmentData> setItems = new List<EquipmentData>();
}

[System.Serializable]
public class SetBonus
{
    public int requiredPieces;
    public StatType statType;
    public int value;
    public bool isPercent;
}