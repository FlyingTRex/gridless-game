using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSkills : MonoBehaviour
{
    private const float MaxLevel = 100f;

    private readonly Dictionary<SkillDefinition, float> levels = new Dictionary<SkillDefinition, float>();

    // Read by SkillsScreen (the Skills tab of PlayerMenuScreen, Tab key) to
    // render the level list — this component no longer draws its own UI.
    public IReadOnlyDictionary<SkillDefinition, float> Levels => levels;

    public float GetLevel(SkillDefinition skill) =>
        skill != null && levels.TryGetValue(skill, out var level) ? level : 0f;

    public void GainExperience(SkillDefinition skill, float amount)
    {
        if (skill == null || amount <= 0f) return;

        float current = GetLevel(skill);
        float diminish = 1f - current / MaxLevel;
        levels[skill] = Mathf.Clamp(current + amount * diminish, 0f, MaxLevel);
    }
}
