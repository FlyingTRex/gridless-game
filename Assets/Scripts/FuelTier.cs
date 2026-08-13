// Five fuel-efficiency tiers for the Furnace's (not yet built) burn system
// — how long one unit of a fuel item keeps it lit. A deliberately separate
// axis from CraftTier (crafting quality) and FoodTier (food substantiality)
// — reusing either would repeat the exact mistake CLAUDE.md's tier-scaling
// gotcha warns about. Tier is efficiency (burn duration) only — it never
// gates which recipes can be smelted (Ben's explicit call, 2026-08-12): a
// Tier1 fuel can smelt anything a Tier5 fuel can, it just burns out faster.
// See WOOD_AND_FUEL_PLANNING.md for the full design.
public enum FuelTier
{
    Tier1,
    Tier2,
    Tier3,
    Tier4,
    Tier5,
}

public static class FuelTierScale
{
    // Minutes of burn time per single fuel item consumed.
    public static float BurnMinutes(FuelTier tier) => tier switch
    {
        FuelTier.Tier1 => 5f,
        FuelTier.Tier2 => 10f,
        // Tiers 3-5 are placeholders (simple doubling) reserved for future
        // fuel types (Coal/Gas/Electricity) that don't exist yet — not a
        // decided balance, just a value to extrapolate from until those
        // items get designed. See WOOD_AND_FUEL_PLANNING.md.
        FuelTier.Tier3 => 20f,
        FuelTier.Tier4 => 40f,
        FuelTier.Tier5 => 80f,
        _ => 5f,
    };
}
