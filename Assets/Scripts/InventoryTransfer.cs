public static class InventoryTransfer
{
    // Moves quantity of item from one inventory-capable object to another.
    // Fails without side effects if the source doesn't have enough, or the
    // destination doesn't have room.
    //
    // If the item being moved is a single equipped instance (e.g. a
    // Sunglasses/Canteen picked up into a backpack), the plain
    // RemoveItem/AddItem path below would move only the item+count and
    // silently drop the equipment reference — orphaning the real object
    // and leaving a dead, unequippable entry behind. Detect that case and
    // carry the equipment reference across instead.
    public static bool Move(Inventory from, Inventory to, ItemDefinition item, int quantity)
    {
        if (from == null || to == null || item == null || quantity <= 0) return false;
        if (from.GetCount(item) < quantity) return false;
        if (!to.HasSpaceFor(item, quantity)) return false;

        IEquippable equipment = null;
        foreach (var slot in from.Slots)
        {
            if (slot.item == item && slot.equipment != null)
            {
                equipment = slot.equipment;
                break;
            }
        }

        if (equipment != null)
        {
            // Equipment slots are always a single instance — can't split.
            if (quantity != 1) return false;
            if (!to.AddEquipmentItem(item, equipment)) return false;
            from.RemoveEquipmentItem(item);
            return true;
        }

        from.RemoveItem(item, quantity);
        to.AddItem(item, quantity);
        return true;
    }
}
