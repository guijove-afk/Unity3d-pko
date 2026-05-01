using UnityEngine;
using System.Collections.Generic;

public class SkillDatabase : MonoBehaviour
{
    public static SkillDatabase Instance { get; private set; }

    [SerializeField] private List<SkillData> allSkills = new List<SkillData>();
    private Dictionary<string, SkillData> skillDictionary = new Dictionary<string, SkillData>();

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
        skillDictionary.Clear();
        foreach (var skill in allSkills)
        {
            if (skill != null && !string.IsNullOrEmpty(skill.skillId))
                skillDictionary[skill.skillId] = skill;
        }
    }

    public SkillData GetSkill(string skillId)
    {
        return skillDictionary.TryGetValue(skillId, out var skill) ? skill : null;
    }

    public List<SkillData> GetSkillsByClass(CharacterClass characterClass)
    {
        List<SkillData> result = new List<SkillData>();
        foreach (var skill in allSkills)
        {
            if (skill != null && (skill.requiredClass == CharacterClass.None || skill.requiredClass == characterClass))
                result.Add(skill);
        }
        return result;
    }

    public List<SkillData> GetSkillsByLevel(int level)
    {
        List<SkillData> result = new List<SkillData>();
        foreach (var skill in allSkills)
        {
            if (skill != null && skill.requiredLevel <= level)
                result.Add(skill);
        }
        return result;
    }

#if UNITY_EDITOR
    [ContextMenu("Auto-Register All Skills")]
    private void AutoRegister()
    {
        allSkills.Clear();
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:SkillData");
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            SkillData skill = UnityEditor.AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (skill != null)
                allSkills.Add(skill);
        }
        UnityEditor.EditorUtility.SetDirty(this);
        BuildDictionary();
        Debug.Log($"[SkillDatabase] Registradas: {allSkills.Count} skills");
    }
#endif
}