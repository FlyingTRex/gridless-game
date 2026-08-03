public static class InventoryTransfer
{
    // Moves quantity of item from one inventory-capable object to another.
    // Fails without side effects if the source doesn't have enough, or the
    // destination doesn't have room. Never moves equipment-type items
    // (Backpack, Canteen, etc.) through this generic path — they require
    // type-specific handlers to properly detach the physical object.
    // Stripping the equipment reference (a known gotcha per CLAUDE.md) would
    // orphan the real object while spawning a fake, non-functional placeholder.
    public static bool Move(Inventory from, Inventory to, ItemDefinition item, int quantity)
    {
        if (from == null || to == null || item == null || quantity <= 0) return false;

        // Guard against moving equipment-type items through this generic path.
        // Check if any slot holding this item also has an equipment reference.
        foreach (var slot in from.Slots)
        {
            if (slot.item == item && slot.equipment != null)
                return false;
        }

        if (from.GetCount(item) < quantity) return false;
        if (!to.HasSpaceFor(item, quantity)) return false;

        from.RemoveItem(item, quantity);
        to.AddItem(item, quantity);
        return true;
    }
}
