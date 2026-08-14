using System.Collections.Generic;
using UnityEngine;

// NPC-side skill/attribute tracking (2026-08-10, Chunk 3 of the Hireable
// NPCs build -- see BUGS_AND_ENHANCEMENTS.md). Deliberately a separate,
// smaller component rather than reusing PlayerSkills directly: PlayerSkills
// draws its own "skill increased" banner in OnGUI, which would fire on the
// PLAYER's screen every time an NPC's skill ticked up if attached to the
// NPC GameObject (OnGUI runs per-instance, not per-player) -- confusing,
// and not something Ben asked for. Same diminishing-returns growth curve
// as PlayerSkills.GainExperience, just silent.
public class NPCSkills : MonoBehaviour
{
    private const float MaxLevel = 100f;

    [System.Serializable]
    public class StartingLevel
    {
        public SkillDefinition skill;
        public float level;
    }

    // Core stats start at displayed 3.0 (raw level 30) per Ben's call
    // (2026-08-10) -- above a fresh player's own starting 2.00, since a
    // hired worker isn't a total novice. Mining (and any future job
    // skill) needs no entry here -- it starts at true zero, same
    // convention PlayerSkills' own starting-level comment documents for
    // crafting/gathering skills.
    [SerializeField] private StartingLevel[] startingLevels;

    private readonly Dictionary<SkillDefinition, float> levels = new Dictionary<SkillDefinition, float>();

    private void Awake()
    {
        if (startingLevels == null) return;
        foreach (var entry in startingLevels)
            if (entry.skill != null)
                levels[entry.skill] = entry.level;
    }

    // Read by NPCHiringScreen to render a stats readout the same way
    // SkillsScreen reads PlayerSkills.Levels.
    public IReadOnlyDictionary<SkillDefinition, float> Levels => levels;

    public float GetLevel(SkillDefinition skill) =>
        skill != null && levels.TryGetValue(skill, out var level) ? level : 0f;

    // Same .25 floor / level-10 remap as PlayerSkills.GetAttributeValue --
    // used for the 4 core stats (Strength/Dexterity/Constitution/
    // Intelligence), not for job skills like Mining.
    public float GetAttributeValue(SkillDefinition skill) =>
        Mathf.Max(0.25f, GetLevel(skill) / 10f);

    // Written by SaveManager on load — same direct-set semantics as
    // PlayerSkills.RestoreLevel.
    public void RestoreLevel(SkillDefinition skill, float level)
    {
        if (skill != null) levels[skill] = level;
    }

    public void GainExperience(SkillDefinition skill, float amount)
    {
        if (skill == null || amount <= 0f) return;

        float current = GetLevel(skill);
        float diminish = 1f - current / MaxLevel;
        levels[skill] = Mathf.Clamp(current + amount * diminish, 0f, MaxLevel);
    }
}
