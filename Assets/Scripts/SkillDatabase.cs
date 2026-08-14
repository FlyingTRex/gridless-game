using UnityEngine;

// Stable-ID lookup for SkillDefinition assets — same shape/reasoning as
// ItemDatabase, just for the other ScriptableObject reference type saved
// data needs to resolve (PlayerSkills/NPCSkills' level dictionaries).
[CreateAssetMenu(menuName = "Gridless/Skill Database", fileName = "SkillDatabase")]
public class SkillDatabase : ScriptableObject
{
    [SerializeField] private SkillDefinition[] skills = System.Array.Empty<SkillDefinition>();

    private static SkillDatabase instance;
    public static SkillDatabase Instance =>
        instance != null ? instance : instance = Resources.Load<SkillDatabase>("SkillDatabase");

    public string IdFor(SkillDefinition skill) => skill != null ? skill.name : null;

    public SkillDefinition Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var skill in skills)
            if (skill != null && skill.name == id) return skill;
        return null;
    }

#if UNITY_EDITOR
    public void EditorSetSkills(SkillDefinition[] value) => skills = value;
#endif
}
