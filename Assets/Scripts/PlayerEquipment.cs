using System.Collections.Generic;
using Mirror;
using UnityEngine;

// Named body-equipment slots for the character. Each slot is its own small
// Inventory (capacity usually 1; Face is 2), so equipping into a slot is
// exactly the same AddEquipmentItem/AddItem/RemoveItem flow used everywhere
// else in the inventory system.
//
// Multiplayer Phase 3 sub-phase 2 (MULTIPLAYER_PLANNING.md). First slice
// (2026-08-22): converted from MonoBehaviour to NetworkBehaviour, base-
// class change only. Second slice (2026-08-23): a real SyncList, same
// by-string-ID pattern PlayerInventory.cs already proved -- but shaped
// differently, since unlike Inventory (mostly plain stackable slots, a
// few equipment-carrying ones excluded from sync) an equip slot is
// virtually ALWAYS equipment-carrying by definition -- excluding those
// the same way would sync nothing at all. Instead this syncs *which item*
// occupies each named slot (what's visibly worn where), while still not
// syncing the equipped object's own deep state (a worn Backpack's nested
// contents, a Canteen's fill level, ...) -- the same complexity boundary
// SAVE_LOAD_PLANNING.md drew for persistence v1, just applied to this
// class's own slot-shaped data instead of Inventory's mostly-stackable
// shape.
public class PlayerEquipment : NetworkBehaviour
{
    [System.Serializable]
    public class SlotConfig
    {
        public string slotName;
        public int capacity = 1;
    }

    [SerializeField]
    private SlotConfig[] slots =
    {
        new SlotConfig { slotName = "Head", capacity = 1 },
        new SlotConfig { slotName = "Face", capacity = 2 },
        new SlotConfig { slotName = "Neck", capacity = 1 },
        new SlotConfig { slotName = "Chest", capacity = 1 },
        new SlotConfig { slotName = "Back", capacity = 1 },
        new SlotConfig { slotName = "Left Arm", capacity = 1 },
        new SlotConfig { slotName = "Right Arm", capacity = 1 },
        new SlotConfig { slotName = "Left Wrist", capacity = 1 },
        new SlotConfig { slotName = "Right Wrist", capacity = 1 },
        new SlotConfig { slotName = "Left Hand", capacity = 1 },
        new SlotConfig { slotName = "Right Hand", capacity = 1 },
        new SlotConfig { slotName = "Waist", capacity = 1 },
        new SlotConfig { slotName = "Leg", capacity = 1 },
        new SlotConfig { slotName = "Feet", capacity = 1 },
    };

    [System.Serializable]
    public struct SyncedEquipmentSlot
    {
        public string slotName;
        // Empty string means the slot is empty -- kept as one entry per
        // configured slot (not omitted when empty) so observers can tell
        // "no data yet" apart from "confirmed empty."
        public string itemId;
    }

    // Server-owned, broadcast to every observer -- what item (if any)
    // occupies each named slot. Polled from Update() via a signature
    // comparison, same reasoning as PlayerInventory's own syncedSlots:
    // dozens of scripts mutate a slot's Inventory directly through
    // GetSlot(...), not through a single hookable method.
    public readonly SyncList<SyncedEquipmentSlot> syncedSlots = new SyncList<SyncedEquipmentSlot>();

    private string lastSyncedSignature = string.Empty;

    private readonly Dictionary<string, Inventory> slotInventories = new Dictionary<string, Inventory>();

    private void Awake()
    {
        foreach (var slot in slots)
            slotInventories[slot.slotName] = new Inventory(slot.capacity);
    }

    private void Update()
    {
        if (!isServer) return;

        string signature = ComputeSignature();
        if (signature == lastSyncedSignature) return;

        lastSyncedSignature = signature;
        RefreshSyncedSlots();
    }

    private string ComputeSignature()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var slot in slots)
        {
            var item = FirstItemIn(slot.slotName);
            sb.Append(slot.slotName).Append(':').Append(item != null ? item.name : "").Append('|');
        }
        return sb.ToString();
    }

    private void RefreshSyncedSlots()
    {
        syncedSlots.Clear();
        foreach (var slot in slots)
        {
            var item = FirstItemIn(slot.slotName);
            string id = item != null ? ItemDatabase.Instance.IdFor(item) : "";
            syncedSlots.Add(new SyncedEquipmentSlot { slotName = slot.slotName, itemId = id ?? "" });
        }
    }

    // First item in a slot regardless of whether it's equipment-carrying
    // or a plain stackable -- unlike GetEquipped below (which only ever
    // returns an IEquippable), this is what the sync summary actually
    // wants to broadcast: what's visibly worn, full stop.
    private ItemDefinition FirstItemIn(string slotName)
    {
        var inv = GetSlot(slotName);
        if (inv == null) return null;
        foreach (var s in inv.Slots)
            if (s.item != null) return s.item;
        return null;
    }

    public IReadOnlyCollection<string> SlotNames => slotInventories.Keys;

    public Inventory GetSlot(string slotName) =>
        slotInventories.TryGetValue(slotName, out var inv) ? inv : null;

    // Convenience for the common case: a slot holding a single unique
    // equippable instance. Returns the first equipped item in the slot
    // (only meaningful to call this way on capacity-1 slots).
    public IEquippable GetEquipped(string slotName)
    {
        var slot = GetSlot(slotName);
        if (slot == null) return null;

        foreach (var s in slot.Slots)
            if (s.equipment != null) return s.equipment;

        return null;
    }

    // Convenience for tool-gated actions (e.g. ResourceNode's requiredTool)
    // — true only if the item is actually held in a hand right now, not
    // just carried somewhere in the main inventory or a backpack.
    public bool HasInHand(ItemDefinition item)
    {
        if (item == null) return false;
        var left = GetSlot("Left Hand");
        var right = GetSlot("Right Hand");
        return (left != null && left.GetCount(item) > 0) || (right != null && right.GetCount(item) > 0);
    }

    // Every distinct item currently held in either hand slot — unlike
    // HasInHand above (checks one specific known item), this is for
    // callers that need to find out *what* is held without knowing its
    // ItemDefinition ahead of time (PlayerCombat scanning for an equipped
    // melee weapon, 2026-08-14).
    public IEnumerable<ItemDefinition> GetHandItems()
    {
        foreach (var slotName in new[] { "Left Hand", "Right Hand" })
        {
            var slot = GetSlot(slotName);
            if (slot == null) continue;
            foreach (var s in slot.Slots)
                if (s.item != null) yield return s.item;
        }
    }
}
