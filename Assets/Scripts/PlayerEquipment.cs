using System.Collections.Generic;
using UnityEngine;

// Named body-equipment slots for the character. Each slot is its own small
// Inventory (capacity usually 1; Face is 2), so equipping into a slot is
// exactly the same AddEquipmentItem/AddItem/RemoveItem flow used everywhere
// else in the inventory system.
public class PlayerEquipment : MonoBehaviour
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
}
