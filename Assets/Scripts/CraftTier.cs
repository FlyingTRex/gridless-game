// Five crafting-quality tiers, decided per docs/design-brief.md. Normal is
// the baseline — deliberately no adjective prefix (see CraftTierNames), so
// a plain item name always reads as "the standard version."
public enum CraftTier
{
    Crude,
    Rudimentary,
    Normal,
    Fine,
    Masterwork,
}

// Display-name helper: every tier except Normal prefixes the base item
// name with its tier — "Crude Lockbox", "Lockbox", "Fine Lockbox".
public static class CraftTierNames
{
    public static string Prefix(CraftTier tier) => tier == CraftTier.Normal ? "" : tier + " ";

    public static string WithPrefix(CraftTier tier, string baseName) => Prefix(tier) + baseName;
}

// Suggested capacity/price scaling relative to CraftTier.Normal's baseline
// (a 2500-coin-per-type Lockbox costing 10 Gold) — chosen so every
// resulting capacity and price comes out a clean whole number off those
// two baselines, not because of any deeper simulation.
public static class CraftTierScale
{
    public static float Modifier(CraftTier tier) => tier switch
    {
        CraftTier.Crude => 0.2f,
        CraftTier.Rudimentary => 0.5f,
        CraftTier.Normal => 1f,
        CraftTier.Fine => 2f,
        CraftTier.Masterwork => 5f,
        _ => 1f,
    };

    // Minimum level (0-100, see PlayerSkills.MaxLevel) in a recipe's
    // trainedSkill required to craft that tier. Crude is deliberately 0 —
    // not 1 — since skill starts at 0 and today's only way to gain most
    // disciplines (Stonework/Woodworking/Sewing) is crafting the exact
    // items this gate would restrict; requiring >=1 at Crude would make a
    // fresh character unable to ever craft a first item in that discipline
    // at all. Ben's call, 2026-08-07.
    public static int SkillRequirement(CraftTier tier) => tier switch
    {
        CraftTier.Crude => 0,
        CraftTier.Rudimentary => 10,
        CraftTier.Normal => 25,
        CraftTier.Fine => 50,
        CraftTier.Masterwork => 100,
        _ => 0,
    };

    // Highest tier a given skill level currently qualifies for — the
    // inverse of SkillRequirement, used to turn a live skill level into a
    // tier for hold-duration lookups (see HoldDuration below).
    public static CraftTier TierForSkillLevel(float level)
    {
        CraftTier best = CraftTier.Crude;
        foreach (CraftTier tier in (CraftTier[])System.Enum.GetValues(typeof(CraftTier)))
        {
            if (level >= SkillRequirement(tier)) best = tier;
        }
        return best;
    }

    // Seconds a skill-gated hold interaction (gathering, chopping, and
    // eventually crafting) takes at each tier — replaces the old
    // punch-N-times/hitsToBreak model. Low tier takes longest (still
    // learning), Masterwork is fastest. Placeholder numbers, same as every
    // other value in this table — meant to be tuned by playtesting, not a
    // simulated result.
    public static float HoldDuration(CraftTier tier) => tier switch
    {
        CraftTier.Crude => 3f,
        CraftTier.Rudimentary => 2.25f,
        CraftTier.Normal => 1.5f,
        CraftTier.Fine => 1f,
        CraftTier.Masterwork => 0.5f,
        _ => 1.5f,
    };
}
