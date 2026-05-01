using UnityEngine;
using System.Collections.Generic;

public class QuestDatabase : MonoBehaviour
{
    public static QuestDatabase Instance { get; private set; }

    [SerializeField] private List<QuestData> allQuests = new List<QuestData>();
    private Dictionary<string, QuestData> questDictionary = new Dictionary<string, QuestData>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildDictionary();
    }

    private void BuildDictionary()
    {
        questDictionary.Clear();
        foreach (var quest in allQuests)
        {
            if (quest != null && !string.IsNullOrEmpty(quest.questId))
                questDictionary[quest.questId] = quest;
        }
    }

    public QuestData GetQuest(string questId)
    {
        return questDictionary.TryGetValue(questId, out var quest) ? quest : null;
    }

    public List<QuestData> GetAvailableQuests(int playerLevel, string completedQuests)
    {
        List<QuestData> result = new List<QuestData>();
        foreach (var quest in allQuests)
        {
            if (quest != null && quest.requiredLevel <= playerLevel)
            {
                if (string.IsNullOrEmpty(quest.prerequisiteQuest) || completedQuests.Contains(quest.prerequisiteQuest))
                    result.Add(quest);
            }
        }
        return result;
    }

#if UNITY_EDITOR
    [ContextMenu("Auto-Register All Quests")]
    private void AutoRegister()
    {
        allQuests.Clear();
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:QuestData");
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            QuestData quest = UnityEditor.AssetDatabase.LoadAssetAtPath<QuestData>(path);
            if (quest != null)
                allQuests.Add(quest);
        }
        UnityEditor.EditorUtility.SetDirty(this);
        BuildDictionary();
        Debug.Log($"[QuestDatabase] Registradas: {allQuests.Count} quests");
    }
#endif
}