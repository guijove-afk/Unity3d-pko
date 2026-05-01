using UnityEngine;

[CreateAssetMenu(fileName = "NewEquipment", menuName = "TOP/Equipment")]
public class EquipmentData : ItemData
{
    [Header("Equipamento")]
    public EquipmentSlot slot;
    public WeaponType weaponType;
    public ArmorType armorType;

    [Header("Atributos")]
    public int durability = 100;
    public int maxDurability = 100;

    [Header("Stats Bônus")]
    public int bonusSTR;
    public int bonusAGI;
    public int bonusCON;
    public int bonusSPR;
    public int bonusACC;
    public int bonusLUCK;

    public int bonusHP;
    public int bonusMP;
    public int bonusSP;

    public int bonusAttack;
    public int bonusDefense;
    public int bonusMagicAttack;
    public int bonusMagicDefense;

    public float bonusAttackSpeed;
    public float bonusMoveSpeed;

    [Header("Elemento")]
    public ElementType elementType = ElementType.None;
    public int elementLevel = 0;

    [Header("Slots de Gemas")]
    public int gemSlots = 0;

    [Header("Visual")]
    public GameObject maleModelPrefab;
    public GameObject femaleModelPrefab;
    public Vector3 modelOffset;
    public Vector3 modelRotation;
    public Vector3 modelScale = Vector3.one;

    [Header("Efeitos")]
    public ParticleSystem equipEffect;
    public ParticleSystem attackEffect;
    public AudioClip equipSound;
}