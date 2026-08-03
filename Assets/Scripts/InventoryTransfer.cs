public static class InventoryTransfer
{
    // Moves quantity of item from one inventory-capable object to another.
    // Fails without side effects if the source doesn't have enough, or the
    // destination doesn't have room.
    public static bool Move(Inventory from, Inventory to, ItemDefinition item, int quantity)
    {
        if (from == null || to == null || item == null || quantity <= 0) return false;
        if (from.GetCount(item) < quantity) return false;
        if (!to.HasSpaceFor(item, quantity)) return false;

        from.RemoveItem(item, quantity);
        to.AddItem(item, quantity);
        return true;
    }
}
