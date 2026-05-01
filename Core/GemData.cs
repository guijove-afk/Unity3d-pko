using UnityEngine;

[CreateAssetMenu(fileName = "NewGem", menuName = "TOP/Gem")]
public class GemData : ItemData
{
    [Header("Propriedades da Gem")]
    public StatModifier statModifier;
    public int gemLevel;
    public GemType gemType;

    [Header("Combinação")]
    public bool canCombine;
    public GemData combinesTo;
}