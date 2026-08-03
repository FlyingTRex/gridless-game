using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSkills : MonoBehaviour
{
    private const float MaxLevel = 100f;

    private readonly Dictionary<SkillDefinition, float> levels = new Dictionary<SkillDefinition, float>();

    public float GetLevel(SkillDefinition skill) =>
        skill != null && levels.TryGetValue(skill, out var level) ? level : 0f;

    public void GainExperience(SkillDefinition skill, float amount)
    {
        if (skill == null || amount <= 0f) return;

        float current = GetLevel(skill);
        float diminish = 1f - current / MaxLevel;
        levels[skill] = Mathf.Clamp(current + amount * diminish, 0f, MaxLevel);
    }

    private void OnGUI()
    {
        var rect = new Rect(10, 360, 220, 200);
        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Label("Skills", DebugGUI.Header);
        foreach (var pair in levels)
            GUILayout.Label($"{pair.Key.skillName}: {pair.Value:F1}", DebugGUI.Label);
        GUILayout.EndArea();
    }
}
