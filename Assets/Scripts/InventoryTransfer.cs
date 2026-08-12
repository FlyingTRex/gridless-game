using UnityEngine;

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

    // Moves as much of item as actually fits in `to`, capped by how much
    // `from` has — instead of failing outright the way a fixed "move
    // everything" quantity would when the destination has less room than
    // the source's full count (e.g. two non-stacking Hammers, maxStack 1,
    // into an empty single-capacity hand slot: only one fits, this moves
    // that one instead of moving nothing). Returns the amount actually
    // moved (0 if nothing could move).
    public static int MoveAsManyAsFit(Inventory from, Inventory to, ItemDefinition item) =>
        MoveAsManyAsFit(from, to, item, int.MaxValue);

    // Same as above, but additionally capped by quantityCap — used for a
    // partial-stack drag (2026-08-12), where the player asked to move only
    // part of a stack (Shift = half, Ctrl = one) rather than everything
    // that fits.
    public static int MoveAsManyAsFit(Inventory from, Inventory to, ItemDefinition item, int quantityCap)
    {
        if (from == null || to == null || item == null) return 0;

        int quantity = Mathf.Min(Mathf.Min(from.GetCount(item), to.SpaceFor(item)), quantityCap);
        if (quantity <= 0) return 0;

        return Move(from, to, item, quantity) ? quantity : 0;
    }
}
