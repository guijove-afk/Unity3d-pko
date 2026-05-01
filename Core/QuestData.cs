using UnityEngine;

[CreateAssetMenu(fileName = "NewQuest", menuName = "TOP/Quest")]
public class QuestData : ScriptableObject
{
    [Header("Informações")]
    public string questId;
    public string questName;
    public string description;
    public QuestType questType;
    public int requiredLevel;
    public string prerequisiteQuest;

    [Header("Objetivos")]
    public QuestObjective[] objectives;

    [Header("Recompensas")]
    public int expReward;
    public int goldReward;
    public int reputationReward;
    public ItemData[] itemRewards;
    public SkillData skillReward;

    [Header("NPCs")]
    public string startNPC;
    public string completeNPC;

    [Header("Diálogos")]
    [TextArea(3, 5)] public string startDialogue;
    [TextArea(3, 5)] public string inProgressDialogue;
    [TextArea(3, 5)] public string completeDialogue;

    [Header("Tempo")]
    public bool hasTimeLimit;
    public float timeLimitMinutes;
}

[System.Serializable]
public class QuestObjective
{
    public QuestObjectiveType type;
    public string targetId;
    public int requiredAmount;
    public string description;
    public Vector3 targetLocation;
}