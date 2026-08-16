using UnityEngine;

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
// Per-tier color ramp, decided in CRAFT_TIER_COLORS_PLANNING.md — used for a
// slot/tile border and the item-name text color wherever an item renders
// (InventoryScreen, CraftingScreen), so quality reads at a glance. Every
// tier gets a color, including Normal (Ben's call — consistency over
// preserving CraftTierNames' no-prefix "unremarkable baseline" asymmetry).
public static class CraftTierColors
{
    public static Color Get(CraftTier tier) => tier switch
    {
        CraftTier.Crude => new Color(0.604f, 0.604f, 0.620f),       // #9a9a9e gray
        CraftTier.Rudimentary => new Color(0.957f, 0.961f, 0.969f), // #f4f5f7 white
        CraftTier.Normal => new Color(0.435f, 0.749f, 0.416f),      // #6fbf6a green
        CraftTier.Fine => new Color(0.357f, 0.624f, 0.878f),        // #5b9fe0 blue
        CraftTier.Masterwork => new Color(0.890f, 0.647f, 0.243f),  // #e3a53e gold
        _ => Color.white,
    };
}

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

    // Weight scaling for tiered items whose better-made version should
    // also be lighter (Backpack shipped first, 2026-08-10) — deliberately
    // its own table, NOT the inverse of Modifier() above. An early attempt
    // reused 1/Modifier(tier) directly and produced an absurd 5x spread
    // (a 25 lb Crude Backpack, a hypothetical 5 lb Crude Knife) — the
    // capacity/price ratio that table was tuned for doesn't transfer to
    // weight; "5x more valuable" and "5x heavier" are very different
    // claims. This curve is deliberately narrow: Crude is only 50%
    // heavier than Normal, Masterwork only 40% lighter. See CLAUDE.md's
    // "reusing a tier-scaling table across unrelated quantities" gotcha.
    public static float WeightModifier(CraftTier tier) => tier switch
    {
        CraftTier.Crude => 1.5f,
        CraftTier.Rudimentary => 1.2f,
        CraftTier.Normal => 1f,
        CraftTier.Fine => 0.8f,
        CraftTier.Masterwork => 0.6f,
        _ => 1f,
    };

    // Fame granted when any skill (any category, including core stats)
    // crosses into this tier for the first time — see FAME_PLANNING.md
    // (2026-08-14). Crude isn't reachable as a "just unlocked" tier (every
    // skill starts there), so it has no entry; Rudimentary is the first
    // real milestone.
    public static float FameOnTierUnlock(CraftTier tier) => tier switch
    {
        CraftTier.Rudimentary => 1f,
        CraftTier.Normal => 2f,
        CraftTier.Fine => 3f,
        CraftTier.Masterwork => 5f,
        _ => 0f,
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

    // Melee weapon tier -> flat damage bonus added on top of the base
    // bare-handed punch (PlayerCombat.punchDamage) — deliberately its own
    // small table, not reused from Modifier/WeightModifier above (same
    // "a ratio tuned for one quantity doesn't transfer to another" trap
    // WeightModifier's own comment already documents). Ben's call
    // (2026-08-14), first applied to the Knife: Crude/Rudimentary add
    // nothing (not yet a real edge), Normal +1, Fine +1.5, Masterwork +2 —
    // a deliberately modest boost (~22% over a 9-damage punch at the high
    // end), meant as a reusable framework for every future melee weapon,
    // not a Knife-specific number.
    public static float WeaponDamageBonus(CraftTier tier) => tier switch
    {
        CraftTier.Crude => 0f,
        CraftTier.Rudimentary => 0f,
        CraftTier.Normal => 1f,
        CraftTier.Fine => 1.5f,
        CraftTier.Masterwork => 2f,
        _ => 0f,
    };

    // Arrow tier -> flat damage bonus, on top of a much smaller base roll
    // (Random 2-4) than melee's punch (9) — deliberately its own table,
    // not WeaponDamageBonus, per the same "a ratio tuned for one quantity
    // doesn't transfer to another" reasoning as above: reusing melee's
    // scale here would nearly triple a Crude arrow's damage at Masterwork
    // instead of the intended ~3x-top-to-bottom progression. Hunting
    // Expansion design, 2026-08-15 — the primary damage driver, Bow's own
    // bonus below stays deliberately secondary.
    public static float ArrowDamageBonus(CraftTier tier) => tier switch
    {
        CraftTier.Crude => 0f,
        CraftTier.Rudimentary => 1f,
        CraftTier.Normal => 2f,
        CraftTier.Fine => 4f,
        CraftTier.Masterwork => 6f,
        _ => 0f,
    };

    // Bow tier -> flat damage bonus, stacking with ArrowDamageBonus above.
    // Ceiling deliberately kept well below Arrow's (+1.5 vs +6) — a great
    // arrow should matter more than a great bow.
    public static float BowDamageBonus(CraftTier tier) => tier switch
    {
        CraftTier.Crude => 0f,
        CraftTier.Rudimentary => 0f,
        CraftTier.Normal => 0.5f,
        CraftTier.Fine => 1f,
        CraftTier.Masterwork => 1.5f,
        _ => 0f,
    };

    // Arrow tier -> accuracy spread cone, in degrees, applied as a random
    // deviation to the fire direction before the shot raycasts — not a
    // flat miss-chance roll. Ties accuracy to range naturally (the same
    // cone misses by more at 25m than at 10m). Dexterity shrinks this
    // further at fire time (PlayerRangedCombat), not baked in here.
    public static float ArrowAccuracySpreadDegrees(CraftTier tier) => tier switch
    {
        CraftTier.Crude => 8f,
        CraftTier.Rudimentary => 5f,
        CraftTier.Normal => 3f,
        CraftTier.Fine => 1.5f,
        CraftTier.Masterwork => 0.3f,
        _ => 8f,
    };

    // Village Flag tier -> spawn-interval multiplier (VILLAGE_FLAG_
    // PLANNING.md section 4, 2026-08-16) -- deliberately its own small
    // table, not a reuse of Modifier/WeightModifier above (same "a ratio
    // tuned for one quantity doesn't transfer to another" trap
    // WeightModifier's own comment documents). Multiplies directly into
    // the interval (lower = shorter interval = more frequent spawns),
    // restrained shape matching the design doc's own proposed range
    // ("Crude 1.0x down to Masterwork ~0.6x") rather than a 25x-style
    // swing -- exact numbers are first-pass, tune-by-playtesting like
    // everything else in this table.
    public static float VillageFlagIntervalMultiplier(CraftTier tier) => tier switch
    {
        CraftTier.Crude => 1.0f,
        CraftTier.Rudimentary => 0.9f,
        CraftTier.Normal => 0.8f,
        CraftTier.Fine => 0.7f,
        CraftTier.Masterwork => 0.6f,
        _ => 1f,
    };

    // Village Flag tier -> Player Map reveal radius, total (not additive
    // on top of the 25m on-foot base) -- PLAYER_MAP_PLANNING.md section 1,
    // locked in 2026-08-16, wired the same day the Flag's own placement
    // hook (PlayerBuilding.Confirm) went in. City Statue's own flat 125m
    // reveal (a one-time milestone, not a tier ladder) lives as a plain
    // const on PlayerBuilding rather than here, since it isn't tier-keyed.
    public static float VillageFlagRevealRadius(CraftTier tier) => tier switch
    {
        CraftTier.Crude => 35f,
        CraftTier.Rudimentary => 45f,
        CraftTier.Normal => 55f,
        CraftTier.Fine => 65f,
        CraftTier.Masterwork => 75f,
        _ => 35f,
    };

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
