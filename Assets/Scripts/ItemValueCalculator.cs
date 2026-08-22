using System.Collections.Generic;
using UnityEngine;

// Shared, reusable base-value system (COMMERCE_PLANNING.md,
// MVP2B_PLANNING.md item 2) -- built for Village Vendor pricing, but
// deliberately not scoped to it; the Traveling Trader's own pricing
// formula ("base value x Fame-band multiplier") needs the exact same
// thing later, per FAME_PLANNING.md.
//
// Computed recursively off RecipeDatabase, not hand-authored per item --
// a craftable item's value is the sum of its ingredients' own values,
// with the requesting item's own CraftTierScale.Modifier applied exactly
// ONCE at the top. Found and fixed in a "be mean" pass before this was
// ever built: an earlier version of this formula applied the tier
// multiplier at every recursive step, which compounds exponentially
// through a multi-step chain (raw Ore -> Masterwork Ingot -> Masterwork
// Sword would apply the 5x Masterwork multiplier once computing the
// Ingot's value, then AGAIN on top of that already-inflated number
// computing the Sword's -- 5x becomes 25x, a 4-step chain hits 125x).
// The fix: ingredient costs sum using each ingredient's own already-final
// value, and tier scaling is applied once, to the item actually being
// priced -- a deep chain still costs more (each real processing step
// legitimately adds value) but doesn't compound the same tier multiplier
// redundantly at every link.
public static class ItemValueCalculator
{
    private static readonly Dictionary<ItemDefinition, float> cache = new();

    // Cycle guard -- crafting data isn't expected to ever actually cycle,
    // but an unguarded recursive walk is one bad future recipe away from
    // a stack overflow with no warning. Returns 0 for the item that would
    // close the cycle rather than recursing forever.
    private static readonly HashSet<ItemDefinition> inProgress = new();

    public static float GetBaseValue(ItemDefinition item)
    {
        if (item == null) return 0f;
        if (cache.TryGetValue(item, out var cached)) return cached;

        if (!inProgress.Add(item))
        {
            Debug.LogWarning($"ItemValueCalculator: cycle detected computing value for "
                + $"'{item.name}' -- returning 0 to break it. Check its recipe chain.");
            return 0f;
        }

        float value;
        try
        {
            value = ComputeValue(item);
        }
        finally
        {
            inProgress.Remove(item);
        }

        cache[item] = value;
        return value;
    }

    // Invalidate after any runtime change to a recipe or an item's own
    // baseValue (not expected during normal play -- recipes/items are
    // static data -- but cheap insurance for Editor tooling that might
    // recompute after an asset edit without a full domain reload).
    public static void ClearCache() => cache.Clear();

    private static float ComputeValue(ItemDefinition item)
    {
        var database = RecipeDatabase.Instance;
        var craftingRecipe = database != null ? database.FindCraftingRecipe(item) : null;
        if (craftingRecipe != null)
        {
            float rawCost = SumCraftingIngredients(craftingRecipe.ingredients);
            return rawCost * CraftTierScale.Modifier(item.tier);
        }

        var smeltable = database != null ? database.FindSmeltableItem(item) : null;
        if (smeltable != null)
        {
            float rawCost = SumSmeltableIngredients(smeltable.ingredients);
            return rawCost * CraftTierScale.Modifier(item.tier);
        }

        // Raw/gathered material with no recipe at all -- hand-seeded root
        // value, see ItemDefinition.baseValue's own comment.
        return item.baseValue;
    }

    private static float SumCraftingIngredients(CraftingRecipe.Ingredient[] ingredients)
    {
        if (ingredients == null) return 0f;
        float total = 0f;
        foreach (var ingredient in ingredients)
            if (ingredient != null && ingredient.item != null)
                total += GetBaseValue(ingredient.item) * ingredient.count;
        return total;
    }

    private static float SumSmeltableIngredients(SmeltableItem.Ingredient[] ingredients)
    {
        if (ingredients == null) return 0f;
        float total = 0f;
        foreach (var ingredient in ingredients)
            if (ingredient != null && ingredient.item != null)
                total += GetBaseValue(ingredient.item) * ingredient.count;
        return total;
    }
}
