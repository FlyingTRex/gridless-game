using Newtonsoft.Json.Linq;

// Shared JSON capture/restore for any Inventory — the player's own,
// PlayerEquipment's named body slots, NPCCargo, StorageBox, and (via
// EquipmentSaveUtility) a worn Backpack/Boot/Belt's own nested Inventory.
// One shared implementation instead of one per container type, since
// Inventory is already the same shared class everywhere in this codebase.
public static class InventorySaveUtility
{
    public static JArray Capture(Inventory inventory)
    {
        var array = new JArray();
        if (inventory == null) return array;

        foreach (var slot in inventory.Slots)
        {
            var slotObj = new JObject
            {
                ["item"] = ItemDatabase.Instance != null ? ItemDatabase.Instance.IdFor(slot.item) : null,
                ["count"] = slot.count,
            };

            if (slot.equipment != null)
                slotObj["equipment"] = EquipmentSaveUtility.Capture(slot.equipment);

            array.Add(slotObj);
        }

        return array;
    }

    public static void Restore(Inventory inventory, JArray data)
    {
        if (inventory == null || data == null) return;

        foreach (var token in data)
        {
            if (!(token is JObject slotObj)) continue;

            var item = ItemDatabase.Instance != null ? ItemDatabase.Instance.Find((string)slotObj["item"]) : null;
            if (item == null) continue;

            if (slotObj["equipment"] is JObject equipmentData)
            {
                var equippable = EquipmentSaveUtility.Restore(item, equipmentData);
                if (equippable != null)
                    inventory.AddEquipmentItem(item, equippable);
            }
            else
            {
                int count = (int)(slotObj["count"] ?? 0);
                if (count > 0) inventory.AddItem(item, count);
            }
        }
    }
}
