using UnityEngine;

// Small shared helper (2026-08-18) for anything that needs to know "does
// the player actually have one of these" across more than just the main
// inventory -- found live by Ben: giving an NPC a tool always read "0 in
// inventory" for a Pickaxe/Backpack that was genuinely being carried, just
// inside a worn Backpack rather than the main 4-slot inventory (the normal
// way to carry more than a handful of items). NPCJob.TryGiveTool/SwapTool
// and NPCJobScreen's own "have N" display both only ever checked
// PlayerInventory directly.
//
// Mirrors InventoryScreen.GetWornContainers()' exact same slot list/
// IInventoryHolder lookup, just reading counts/removing instead of
// rendering rows -- kept as its own small static class rather than
// reworked into that screen's method, since this needs to run from
// NPCJob.cs too, which has no reason to depend on InventoryScreen.
public static class PlayerCarriedItems
{
    private static readonly string[] ContainerSlots = { "Back", "Waist", "Chest", "Leg" };

    // Total count across the main inventory plus every worn container's
    // nested contents.
    public static int GetTotalCount(PlayerInventory playerInventory, PlayerEquipment equipment, ItemDefinition item)
    {
        int count = playerInventory != null ? playerInventory.GetCount(item) : 0;
        if (equipment == null || item == null) return count;

        foreach (var slotName in ContainerSlots)
        {
            var holder = FindHolder(equipment, slotName);
            if (holder != null) count += holder.Inventory.GetCount(item);
        }

        return count;
    }

    // Removes exactly one of item from wherever it actually is -- the main
    // inventory first, then each worn container in turn. False (no-op) if
    // it isn't found anywhere.
    public static bool RemoveOne(PlayerInventory playerInventory, PlayerEquipment equipment, ItemDefinition item)
    {
        if (item == null) return false;

        if (playerInventory != null && playerInventory.GetCount(item) > 0)
            return playerInventory.RemoveItem(item, 1);

        if (equipment == null) return false;

        foreach (var slotName in ContainerSlots)
        {
            var holder = FindHolder(equipment, slotName);
            if (holder != null && holder.Inventory.GetCount(item) > 0)
                return holder.Inventory.RemoveItem(item, 1);
        }

        return false;
    }

    private static IInventoryHolder FindHolder(PlayerEquipment equipment, string slotName)
    {
        var slotInventory = equipment.GetSlot(slotName);
        if (slotInventory == null) return null;

        foreach (var entry in slotInventory.Slots)
            if (entry.equipment is IInventoryHolder holder)
                return holder;

        return null;
    }
}
