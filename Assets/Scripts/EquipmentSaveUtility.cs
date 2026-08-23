using Newtonsoft.Json.Linq;
using UnityEngine;

// The recursive nested-equipment capture SAVE_LOAD_PLANNING.md section 4
// calls the hardest piece of the whole system — a worn equippable isn't
// just an ItemDefinition ID, it can carry its own extra state (a Canteen's
// liquid/amount) and, for anything implementing IInventoryHolder (Backpack/
// Boot/Belt), its own nested Inventory, which can itself hold more
// equipped items (a Canteen clipped into a Backpack or onto a worn Belt —
// a real, already-shipped mechanic). Genuinely recursive: InventorySaveUtility
// calls back into this for each equipped slot it finds, and this calls
// back into InventorySaveUtility for a holder's nested contents.
public static class EquipmentSaveUtility
{
    public static JObject Capture(IEquippable equipment)
    {
        var obj = new JObject();

        if (equipment is Canteen canteen && canteen.Liquid.HasValue)
        {
            obj["liquid"] = canteen.Liquid.Value.ToString();
            obj["amount"] = canteen.Amount;
        }

        if (equipment is IInventoryHolder holder && holder.Inventory != null)
            obj["nested"] = InventorySaveUtility.Capture(holder.Inventory);

        return obj;
    }

    // Instantiates a fresh instance of item's real physical prefab,
    // restores its extra state, and returns it ready for the caller to
    // AddEquipmentItem into a slot. Left Stash()ed (hidden, unparented) —
    // SaveManager's post-restore PlayerBodyModel.RefreshAllAnchors() sweep
    // is what makes a worn one visible/bone-attached again, the same sweep
    // a gender toggle already triggers for exactly this "populated slot,
    // still needs a real anchor" situation.
    public static IEquippable Restore(ItemDefinition item, JObject data)
    {
        if (item == null || item.worldPickupPrefab == null) return null;

        var instance = Object.Instantiate(item.worldPickupPrefab);
        NetworkSpawnHelper.SpawnIfNetworked(instance);
        if (!instance.TryGetComponent(out IEquippable equippable))
        {
            Object.Destroy(instance);
            return null;
        }

        if (equippable is Canteen canteen && data?["liquid"] != null)
        {
            var liquid = (LiquidType)System.Enum.Parse(typeof(LiquidType), (string)data["liquid"]);
            canteen.RestoreLiquid(liquid, (float)(data["amount"] ?? 0f));
        }

        if (equippable is IInventoryHolder holder && data?["nested"] is JArray nested)
            InventorySaveUtility.Restore(holder.Inventory, nested);

        equippable.Stash();
        return equippable;
    }
}
