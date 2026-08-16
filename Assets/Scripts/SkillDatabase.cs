using System.Collections.Generic;
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

    private Dictionary<string, SkillDefinition> lookup;

    public string IdFor(SkillDefinition skill) => skill != null ? skill.name : null;

    public SkillDefinition Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (lookup == null) BuildLookup();
        return lookup.TryGetValue(id, out var skill) ? skill : null;
    }

    private void BuildLookup()
    {
        lookup = new Dictionary<string, SkillDefinition>(skills.Length);
        foreach (var skill in skills)
            if (skill != null) lookup[skill.name] = skill;
    }

#if UNITY_EDITOR
    // See ItemDatabase.EditorSetItems — same deterministic-sort reasoning.
    public void EditorSetSkills(SkillDefinition[] value)
    {
        System.Array.Sort(value, (a, b) =>
            string.CompareOrdinal(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty));
        skills = value;
        lookup = null;
    }
#endif
}
