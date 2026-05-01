using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "TOP/Enemy")]
public class EnemyData : ScriptableObject
{
    [Header("Informações")]
    public string enemyName = "Monster";
    public int level = 1;
    public EnemyType enemyType = EnemyType.Normal;
    
    [Header("Stats Base")]
    public int baseHealth = 100;
    public int baseMana = 50;
    public int baseAttack = 10;
    public int baseDefense = 5;
    public int baseMagicAttack = 0;
    public int baseMagicDefense = 5;
    
    [Header("Combate")]
    public float attackSpeed = 1f;
    public float moveSpeed = 3f;
    public int attackRange = 2;
    public float aggroRange = 8f;
    
    [Header("Recompensas")]
    public int expReward = 10;
    public int goldReward = 5;
    
    [Header("Drops")]
    public EnemyDrop[] possibleDrops;
    
    [Header("Respawn")]
    public float respawnTime = 10f;
    
    [Header("Visual")]
    public RuntimeAnimatorController animatorController;
    public GameObject modelPrefab;
    
    [Header("AI")]
    public bool canWander = true;
    public bool canPatrol = false;
    public Vector3[] patrolPoints;
}

public enum EnemyType
{
    Normal,
    Elite,
    Boss,
    MiniBoss
}

[System.Serializable]
public class EnemyDrop
{
    public string itemId;
    [Range(0f, 100f)] public float dropChance = 10f;
    public int minQuantity = 1;
    public int maxQuantity = 1;
}