using UnityEngine;

// Central place for "does this item count as that ingredient" logic, shared
// by PlayerCrafting and PlayerBuilding so a recipe/build piece that asks for
// a raw material also accepts a refined variant of it (any Trimmed Stick
// tier for Stick, Woven Grass Cloth for Fiber) — see ItemDefinition.baseItem.
public static class IngredientMatching
{
    // Walks baseItem up to 5 hops to guard against an accidental cycle in
    // the data; no real item chain is expected to go anywhere near that
    // deep (Stick -> Trimmed Stick is one hop).
    private const int MaxChainDepth = 5;

    public static bool Satisfies(ItemDefinition candidate, ItemDefinition required)
    {
        var current = candidate;
        for (int i = 0; i < MaxChainDepth && current != null; i++)
        {
            if (current == required) return true;
            current = current.baseItem;
        }
        return false;
    }

    // Sums every slot in inv whose item Satisfies required — exact matches
    // and refined substitutes both count.
    public static int GetCount(Inventory inv, ItemDefinition required)
    {
        int total = 0;
        foreach (var slot in inv.Slots)
            if (Satisfies(slot.item, required)) total += slot.count;
        return total;
    }

    // Removes up to amount from inv, preferring exact matches of required
    // before falling back to substitutes, so a player's refined materials
    // are only spent once their raw stock of the exact item runs out.
    // Caller should only invoke this after confirming enough exists in
    // total (same contract as Inventory.RemoveItem). Returns the amount
    // actually removed.
    public static int Remove(Inventory inv, ItemDefinition required, int amount)
    {
        int removed = 0;

        int exact = inv.GetCount(required);
        if (exact > 0)
        {
            int take = Mathf.Min(exact, amount);
            inv.RemoveItem(required, take);
            removed += take;
            amount -= take;
        }

        while (amount > 0)
        {
            ItemDefinition substitute = null;
            foreach (var slot in inv.Slots)
            {
                if (slot.item != required && Satisfies(slot.item, required))
                {
                    substitute = slot.item;
                    break;
                }
            }
            if (substitute == null) break;

            int have = inv.GetCount(substitute);
            int take = Mathf.Min(have, amount);
            inv.RemoveItem(substitute, take);
            removed += take;
            amount -= take;
        }

        return removed;
    }
}
