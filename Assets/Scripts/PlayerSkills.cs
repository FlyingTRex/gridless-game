using System;
using System.Collections.Generic;
using System.Text;
using Mirror;
using UnityEngine;

// FIXED (2026-08-28, found live -- real remote client's own Skills tab
// showed stale/missing levels for anything trained via a Command-routed
// action, e.g. Pickup.ServerComplete's skill-gain call). Same root cause
// as PlayerInventory before its own 2026-08-28 fix: `levels` was a plain
// local Dictionary, never networked, so a gain rolled server-side (inside
// a Command) only ever updated the server's own copy of this component --
// a genuine remote client's own copy (what SkillsScreen/MagicScreen read)
// never heard about it. Converted to NetworkBehaviour and added a
// syncedLevels SyncList, same server-Update-poll + client-Callback-
// reconciliation shape PlayerInventory's own fix already established.
[DisallowMultipleComponent]
public class PlayerSkills : NetworkBehaviour
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

    [System.Serializable]
    public class StartingLevel
    {
        public SkillDefinition skill;
        public float level;
    }

    // Core stats start above the .25 floor (2.0 displayed, per Ben's call
    // 2026-08-10) rather than untrained-from-zero like crafting skills —
    // everyone has SOME baseline strength/dexterity/etc. Level 20 here
    // maps to GetAttributeValue's displayed 2.0 (level/10). Crafting
    // skills need no entry here; they're meant to start at true zero.
    [SerializeField] private StartingLevel[] startingLevels;

    // Small global XP multiplier, added 2026-08-14
    // (DEXTERITY_CONSTITUTION_PLANNING.md) — smarter characters learn
    // everything else a little faster. Deliberately small (+5% at
    // Intelligence 100, i.e. intLevel/2000) — an earlier, much bigger
    // version of this idea (intLevel/200, +50% at cap) lived in
    // BUGS_AND_ENHANCEMENTS.md and was explicitly too big for "very
    // small" (Ben, 2026-08-14). Never applies to Intelligence's own
    // gains — checked inline in GainExperience below, not via a call-site
    // parameter, so none of this method's many existing callers need to
    // change.
    [SerializeField] private SkillDefinition intelligenceSkill;
    private const float IntelligenceXpBonusDivisor = 2000f;

    private readonly Dictionary<SkillDefinition, float> levels = new Dictionary<SkillDefinition, float>();

    [System.Serializable]
    public struct SyncedSkillLevel
    {
        public string skillId;
        public float level;
    }

    // Server-owned, broadcast to every observer -- same by-string-ID/
    // poll-a-signature-in-Update shape PlayerInventory.syncedSlots
    // already established, since `levels` (a plain Dictionary keyed by
    // ScriptableObject reference) can't be synced directly either.
    public readonly SyncList<SyncedSkillLevel> syncedLevels = new SyncList<SyncedSkillLevel>();
    private string lastSyncedLevelsSignature = string.Empty;

    private string message;
    private float messageExpireTime;

    private void Awake()
    {
        syncedLevels.Callback += OnSyncedLevelsChanged;

        if (startingLevels == null) return;
        foreach (var entry in startingLevels)
            if (entry.skill != null)
                levels[entry.skill] = entry.level;
    }

    private void OnDestroy()
    {
        syncedLevels.Callback -= OnSyncedLevelsChanged;
    }

    private void Update()
    {
        if (!isServer) return;

        string signature = ComputeLevelsSignature();
        if (signature == lastSyncedLevelsSignature) return;

        lastSyncedLevelsSignature = signature;
        RefreshSyncedLevels();
    }

    private string ComputeLevelsSignature()
    {
        var sb = new StringBuilder();
        foreach (var kvp in levels)
            sb.Append(kvp.Key != null ? kvp.Key.name : "null").Append(':').Append(kvp.Value.ToString("F3")).Append('|');
        return sb.ToString();
    }

    private void RefreshSyncedLevels()
    {
        syncedLevels.Clear();
        foreach (var kvp in levels)
        {
            string id = SkillDatabase.Instance != null ? SkillDatabase.Instance.IdFor(kvp.Key) : null;
            if (id == null) continue;
            syncedLevels.Add(new SyncedSkillLevel { skillId = id, level = kvp.Value });
        }
    }

    // Client-side reconciliation. Upsert-only (never removes a locally-
    // known skill) rather than a full clear-and-rebuild -- `levels` only
    // ever grows (no "unlearn a skill" mechanic exists), so there's no
    // real entry to prune, and upserting avoids a startup race where
    // Awake()'s own startingLevels seed could otherwise flash-clear
    // before the first real syncedLevels payload arrives. Also re-shows
    // the "skill increased" banner for a level bump that arrived this
    // way (e.g. a Command-routed gain, Pickup.ServerComplete and
    // similar) -- deliberately does NOT re-fire TierUnlocked, since
    // PlayerFame already reacted to that exact milestone once, server-
    // side, when GainExperience originally rolled it; firing it again
    // here would be a second, client-only trigger for an un-networked
    // (PlayerFame isn't synced either -- see BUGS_AND_ENHANCEMENTS.md)
    // system that was never meant to be asked twice.
    private void OnSyncedLevelsChanged(SyncList<SyncedSkillLevel>.Operation op, int index, SyncedSkillLevel oldItem, SyncedSkillLevel newItem)
    {
        if (isServer) return;

        foreach (var entry in syncedLevels)
        {
            var skill = SkillDatabase.Instance != null ? SkillDatabase.Instance.Find(entry.skillId) : null;
            if (skill == null) continue;

            float previous = GetLevel(skill);
            levels[skill] = entry.level;
            if (entry.level > previous) ShowGainMessage(skill, previous, entry.level);
        }
    }

    // Read by SkillsScreen (the Skills tab of PlayerMenuScreen, Tab key) to
    // render the level list — this component no longer draws its own UI.
    public IReadOnlyDictionary<SkillDefinition, float> Levels => levels;

    public float GetLevel(SkillDefinition skill) =>
        skill != null && levels.TryGetValue(skill, out var level) ? level : 0f;

    // Core stats (Strength today; Dexterity/Constitution/Intelligence
    // later, see BUGS_AND_ENHANCEMENTS.md) grow on this same 0-100
    // GainExperience track as every other skill, just remapped to a
    // .25-10 display range on the Player tab per Ben's call (2026-08-10)
    // — a .25 floor so an untrained stat never reads as literal zero, and
    // 1.0 lands at skill level 10 (a modest early gain).
    public float GetAttributeValue(SkillDefinition skill) =>
        Mathf.Max(0.25f, GetLevel(skill) / 10f);

    // How long a hold-based action gated on this skill takes right now —
    // read by ResourceNode/ChoppableTree (and, later, timed Crafting) via
    // IInteractable.GetHoldDuration. Null skill (a gadget with no defining
    // discipline, see design-brief.md's discipline-sort rule) has no tier
    // to climb, so it stays at the Crude/base duration forever.
    public float GetHoldDuration(SkillDefinition skill) =>
        CraftTierScale.HoldDuration(CraftTierScale.TierForSkillLevel(GetLevel(skill)));

    // Written by SaveManager on load — sets a level directly from save
    // data, bypassing GainExperience's diminishing-returns curve and
    // "skill increased" banner (neither applies to restoring a level that
    // was already reached in a previous session).
    public void RestoreLevel(SkillDefinition skill, float level)
    {
        if (skill != null) levels[skill] = level;
    }

    // Fired whenever GainExperience crosses a skill into a new CraftTier —
    // any skill, any category (Gathering/CraftingDiscipline/Combat/Magic/
    // Attribute, including core stats). PlayerFame subscribes to this for
    // its tier-unlock input (FAME_PLANNING.md, 2026-08-14) — added here
    // rather than duplicating TierJustUnlocked's detection logic elsewhere.
    public event Action<CraftTier> TierUnlocked;

    public void GainExperience(SkillDefinition skill, float amount)
    {
        if (skill == null || amount <= 0f) return;

        if (intelligenceSkill != null && skill != intelligenceSkill)
            amount *= 1f + GetLevel(intelligenceSkill) / IntelligenceXpBonusDivisor;

        float current = GetLevel(skill);
        float diminish = 1f - current / MaxLevel;
        float newLevel = Mathf.Clamp(current + amount * diminish, 0f, MaxLevel);
        levels[skill] = newLevel;

        // Already at MaxLevel (diminish is 0) produces newLevel == current
        // — correctly suppresses the message rather than claiming a gain
        // that didn't actually happen.
        if (newLevel > current)
        {
            bool tierUnlocked = TierJustUnlocked(current, newLevel, out var unlockedTier);
            if (tierUnlocked) TierUnlocked?.Invoke(unlockedTier);
            ShowGainMessage(skill, current, newLevel);

            // Every skill/stat gain in the game flows through this one
            // method — logging here (2026-08-16) covers all of them with a
            // single line, written to Player.log in a standalone build
            // (same folder as save.json), so a live-test session can be
            // reviewed after the fact without needing the Editor open or
            // manual save-diffing.
            Debug.Log($"[Skill] {skill.skillName} +{(newLevel - current):F3} -> {newLevel:F3}"
                + (tierUnlocked ? $" (TIER UNLOCKED: {unlockedTier})" : ""));
        }
    }

    // The on-screen banner half of a gain, split out from GainExperience
    // so OnSyncedLevelsChanged (a real client catching up on a gain that
    // was rolled server-side, e.g. inside a Command) can show the same
    // banner for the player who actually earned it, without re-rolling
    // GainExperience's own diminishing-returns math or re-firing
    // TierUnlocked (see that call site's own comment for why not).
    private void ShowGainMessage(SkillDefinition skill, float current, float newLevel)
    {
        bool tierUnlocked = TierJustUnlocked(current, newLevel, out var unlockedTier);
        string[] pool = tierUnlocked ? TierUnlockTemplates[unlockedTier] : MessageTemplates;
        string template = pool[UnityEngine.Random.Range(0, pool.Length)];
        message = string.Format(template, skill.skillName, newLevel.ToString("F1"));
        messageExpireTime = Time.time + MessageDuration;
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
    // FIXED (2026-08-28, found live -- "why did traskmi see MY skill
    // improvements"): Mirror spawns a copy of every connected player's
    // object on every other client too, so they can see each other, and
    // OnGUI has no built-in ownership concept -- it used to fire
    // unconditionally on whichever machine was running it, so the HOST
    // (server + their own client in one process) saw this banner pop up
    // for every connected player's gain, not just their own. Gated the
    // same way every other per-player OnGUI screen in this project
    // already does (GameMenuScreen, TeamScreen, ...) -- isLocalPlayer is
    // inherited for free now that this is a NetworkBehaviour.
    private void OnGUI()
    {
        if (!isLocalPlayer) return;
        if (message == null || Time.time >= messageExpireTime) return;

        const float width = 340f;
        const float height = 30f;
        var rect = new Rect((Screen.width - width) / 2f, 70f, width, height);

        DebugGUI.DrawPanel(rect);
        GUI.Label(rect, message, DebugGUI.Header);
    }
}
