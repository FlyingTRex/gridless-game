using System.Collections.Generic;
using Mirror;
using UnityEngine;

// Named body-equipment slots for the character. Each slot is its own small
// Inventory (capacity usually 1; Face is 2), so equipping into a slot is
// exactly the same AddEquipmentItem/AddItem/RemoveItem flow used everywhere
// else in the inventory system.
//
// Multiplayer Phase 3 sub-phase 2 (MULTIPLAYER_PLANNING.md), same slice as
// PlayerInventory.cs (2026-08-22): converted from MonoBehaviour to
// NetworkBehaviour, base-class change only, no new synced state yet.
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

    private readonly Dictionary<string, Inventory> slotInventories = new Dictionary<string, Inventory>();

    private void Awake()
    {
        foreach (var slot in slots)
            slotInventories[slot.slotName] = new Inventory(slot.capacity);
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
