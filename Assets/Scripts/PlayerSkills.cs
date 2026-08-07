using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSkills : MonoBehaviour
{
    private const float MaxLevel = 100f;

    // How long the "skill increased" message stays on screen after each
    // gain. No queue — a gain while one is already showing just replaces
    // it and resets the timer, rather than stacking multiple messages.
    private const float MessageDuration = 3f;

    // Randomly picked for an ordinary gain (one that doesn't cross a tier
    // threshold — see TierUnlockTemplates below) so repeated crafting
    // doesn't show the exact same line over and over. {0} = skill name,
    // {1} = new level.
    private static readonly string[] MessageTemplates =
    {
        "Congratulations! You have increased your {0} skill to {1}!",
        "Great work! Your {0} skill has grown to {1}!",
        "Nicely done! {0} skill increased to {1}!",
        "Excellent! Your {0} expertise is now {1}!",
        "Well done! You've honed your {0} skill to {1}!",
        "Impressive! {0} skill is now {1}!",
    };

    // Shown instead of MessageTemplates when a gain pushes the skill's
    // level past a CraftTierScale.SkillRequirement threshold for the
    // first time — a bigger, more celebratory moment than an ordinary
    // gain. Crude has no entry: its threshold is 0 (see
    // CraftTierScale.SkillRequirement), so there's never a real "just
    // unlocked Crude" crossing to celebrate — everyone starts able to
    // craft it.
    private static readonly Dictionary<CraftTier, string[]> TierUnlockTemplates = new Dictionary<CraftTier, string[]>
    {
        [CraftTier.Rudimentary] = new[]
        {
            "Congratulations! Your {0} skill reached {1} — Rudimentary tier unlocked!",
            "Progress! {0} has grown to {1} — Rudimentary crafting is now within reach!",
        },
        [CraftTier.Normal] = new[]
        {
            "Well done! Your {0} skill reached {1} — Normal tier unlocked!",
            "Solid work! {0} is now {1} — Normal-quality crafting is yours!",
        },
        [CraftTier.Fine] = new[]
        {
            "Excellent! Your {0} skill reached {1} — Fine tier unlocked!",
            "Impressive! {0} has climbed to {1} — Fine craftsmanship awaits!",
        },
        [CraftTier.Masterwork] = new[]
        {
            "Incredible! Your {0} skill reached {1} — Masterwork tier unlocked!",
            "A true master! {0} has reached {1} — Masterwork creations are now within reach!",
        },
    };

    private readonly Dictionary<SkillDefinition, float> levels = new Dictionary<SkillDefinition, float>();

    private string message;
    private float messageExpireTime;

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
        float newLevel = Mathf.Clamp(current + amount * diminish, 0f, MaxLevel);
        levels[skill] = newLevel;

        // Already at MaxLevel (diminish is 0) produces newLevel == current
        // — correctly suppresses the message rather than claiming a gain
        // that didn't actually happen.
        if (newLevel > current)
        {
            string[] pool = TierJustUnlocked(current, newLevel, out var unlockedTier)
                ? TierUnlockTemplates[unlockedTier]
                : MessageTemplates;

            string template = pool[Random.Range(0, pool.Length)];
            message = string.Format(template, skill.skillName, newLevel.ToString("F1"));
            messageExpireTime = Time.time + MessageDuration;
        }
    }

    // True if this gain (current -> newLevel) crossed at least one tier's
    // skill threshold for the first time. If it somehow crossed more than
    // one at once (not possible with today's skillGain values, but not
    // assumed away either), reports the highest tier reached.
    private static bool TierJustUnlocked(float current, float newLevel, out CraftTier unlockedTier)
    {
        unlockedTier = default;
        bool found = false;

        foreach (var tier in TierUnlockTemplates.Keys)
        {
            int required = CraftTierScale.SkillRequirement(tier);
            if (current >= required || newLevel < required) continue;

            if (!found || required > CraftTierScale.SkillRequirement(unlockedTier))
            {
                unlockedTier = tier;
                found = true;
            }
        }

        return found;
    }

    // Top-center, just below where PlayerNavComputer's compass sits
    // (y=10 to y=62 when worn) so the two never overlap regardless of
    // whether a Navigation Computer happens to be equipped.
    private void OnGUI()
    {
        if (message == null || Time.time >= messageExpireTime) return;

        const float width = 340f;
        const float height = 30f;
        var rect = new Rect((Screen.width - width) / 2f, 70f, width, height);

        DebugGUI.DrawPanel(rect);
        GUI.Label(rect, message, DebugGUI.Header);
    }
}
