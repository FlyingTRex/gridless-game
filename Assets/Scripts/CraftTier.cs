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
}
