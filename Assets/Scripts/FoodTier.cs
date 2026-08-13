// Five food-substantiality tiers — how much Hunger a consumable restores,
// separate from CraftTier (a crafting-quality axis; reusing it here would
// be exactly the mistake CLAUDE.md's tier-scaling gotcha warns about, a
// ratio/scale tuned for one quantity doesn't transfer to an unrelated one,
// and food nutrition isn't a crafting-skill concept at all). Every
// EdibleItem restores Hunger via this tier unconditionally — see
// PlayerEating.TryEatFrom.
public enum FoodTier
{
    Snack,
    LightMeal,
    Meal,
    HeartyMeal,
    Feast,
}

public static class FoodTierScale
{
    public static float HungerRestoreAmount(FoodTier tier) => tier switch
    {
        FoodTier.Snack => 15f,
        FoodTier.LightMeal => 25f,
        FoodTier.Meal => 40f,
        FoodTier.HeartyMeal => 60f,
        FoodTier.Feast => 90f,
        _ => 15f,
    };
}
