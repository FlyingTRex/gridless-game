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
        // FIXED (2026-08-28): itemId alone was only ever enough to know
        // WHAT'S worn, never enough to actually resolve the live physical
        // object -- every real equip slot occupant is an IEquippable
        // (Backpack, Canteen, Tool, ...), a real spawned GameObject, not
        // just data. netId is that object's own NetworkIdentity.netId (0
        // = no live object), resolved client-side via
        // NetworkClient.spawned so a real remote client's own
        // slotInventories can register the SAME live instance everyone
        // else sees, not a stand-in.
        public uint netId;
        // FIXED (2026-08-30, found live: "picked up a Stick, it doesn't
        // show up in inventory"). A plain stackable item (no IEquippable --
        // netId stays 0) can land in a hand slot via PlayerLoot.Receive
        // just like an equipment item can, but until now nothing here
        // carried or applied its *count* client-side -- only the
        // equipment/netId branch below was ever reconciled. Needed
        // alongside itemId specifically for the plain-item case (an
        // equipment slot's own count is always 1 by construction).
        public int count;
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

        syncedSlots.Callback += OnSyncedSlotsChanged;
    }

    private void OnDestroy()
    {
        syncedSlots.Callback -= OnSyncedSlotsChanged;
    }

    private void Update()
    {
        if (isServer)
        {
            string signature = ComputeSignature();
            if (signature == lastSyncedSignature) return;

            lastSyncedSignature = signature;
            RefreshSyncedSlots();
            return;
        }

        // Client-side retry, not just the Callback below -- the equipped
        // object's own NetworkIdentity spawn message can arrive AFTER
        // this component's own syncedSlots update (most likely right
        // after connecting while another player is already wearing
        // gear), so a resolution attempted only once, at Callback time,
        // could permanently miss it. Cheap enough to just re-run every
        // tick (14 slots, capacity 1-2 each) -- once every slot already
        // matches, ApplyAllSyncedSlotsToLocal's own per-slot check makes
        // this a no-op scan, not real work.
        ApplyAllSyncedSlotsToLocal();
    }

    // FIXED (2026-08-28, same shape as PlayerInventory/PlayerSkills'
    // own fixes): syncedSlots was built (2026-08-23) to broadcast which
    // item occupies each slot, but nothing ever read it back into
    // slotInventories client-side -- a real remote client's own copy of
    // e.g. another player's worn Backpack never reflected reality.
    // Harder than a plain stackable count: an equip slot's occupant is a
    // live IEquippable GameObject, not just data, so this resolves the
    // real spawned instance via NetworkClient.spawned (see NetIdFor
    // above) rather than fabricating a stand-in. Uses Inventory's
    // existing RemoveEquipmentItem/AddEquipmentItem -- both non-
    // destructive (unlike Inventory.Clear(), which destroys the
    // equipment's GameObject; that's correct for a real save/restore
    // discarding stale data, but would be catastrophic here, destroying
    // a live networked object client-side out from under the server).
    private void OnSyncedSlotsChanged(SyncList<SyncedEquipmentSlot>.Operation op, int index, SyncedEquipmentSlot oldItem, SyncedEquipmentSlot newItem)
    {
        if (isServer) return;
        ApplyAllSyncedSlotsToLocal();
    }

    private void ApplyAllSyncedSlotsToLocal()
    {
        foreach (var entry in syncedSlots)
            ApplySyncedSlotToLocal(entry);
    }

    private void ApplySyncedSlotToLocal(SyncedEquipmentSlot entry)
    {
        var localSlot = GetSlot(entry.slotName);
        if (localSlot == null) return;

        IEquippable targetEquipment = null;
        if (entry.netId != 0 && NetworkClient.spawned.TryGetValue(entry.netId, out var identity))
            targetEquipment = identity.GetComponent(typeof(IEquippable)) as IEquippable;

        bool alreadyCorrect = false;
        // Snapshot first -- RemoveEquipmentItem mutates the same list
        // Slots exposes, can't safely foreach-and-remove in one pass.
        var currentEquipmentEntries = new List<(ItemDefinition item, IEquippable equipment)>();
        foreach (var s in localSlot.Slots)
            if (s.equipment != null) currentEquipmentEntries.Add((s.item, s.equipment));

        foreach (var (item, equipment) in currentEquipmentEntries)
        {
            if (ReferenceEquals(equipment, targetEquipment)) { alreadyCorrect = true; continue; }
            localSlot.RemoveEquipmentItem(item);
        }

        if (targetEquipment != null && !alreadyCorrect)
        {
            var targetItem = ItemDatabase.Instance != null ? ItemDatabase.Instance.Find(entry.itemId) : null;
            if (targetItem != null) localSlot.AddEquipmentItem(targetItem, targetEquipment);
            return;
        }

        // Plain stackable reconciliation (no live networked object) --
        // mirrors the equipment branch above, but by item+count instead of
        // by live-instance identity, same shape PlayerInventory.syncedSlots
        // already uses for the main inventory.
        if (targetEquipment == null)
        {
            Inventory.Slot currentPlain = null;
            foreach (var s in localSlot.Slots)
                if (s.equipment == null && s.item != null) { currentPlain = s; break; }

            var targetItem = !string.IsNullOrEmpty(entry.itemId) && ItemDatabase.Instance != null
                ? ItemDatabase.Instance.Find(entry.itemId) : null;

            bool matches = currentPlain != null && currentPlain.item == targetItem && currentPlain.count == entry.count;
            if (matches) return;

            if (currentPlain != null)
                localSlot.RemoveItem(currentPlain.item, currentPlain.count);
            if (targetItem != null && entry.count > 0)
                localSlot.AddItem(targetItem, entry.count);
        }
    }

    private string ComputeSignature()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var slot in slots)
        {
            var item = FirstItemIn(slot.slotName);
            int count = FirstItemCountIn(slot.slotName);
            uint netId = NetIdFor(GetEquipped(slot.slotName));
            sb.Append(slot.slotName).Append(':').Append(item != null ? item.name : "").Append(':').Append(count).Append(':').Append(netId).Append('|');
        }
        return sb.ToString();
    }

    private void RefreshSyncedSlots()
    {
        syncedSlots.Clear();
        foreach (var slot in slots)
        {
            var item = FirstItemIn(slot.slotName);
            int count = FirstItemCountIn(slot.slotName);
            string id = item != null ? ItemDatabase.Instance.IdFor(item) : "";
            uint netId = NetIdFor(GetEquipped(slot.slotName));
            syncedSlots.Add(new SyncedEquipmentSlot { slotName = slot.slotName, itemId = id ?? "", netId = netId, count = count });
        }
    }

    private static uint NetIdFor(IEquippable equipment)
    {
        if (equipment is Component component && component != null && component.TryGetComponent(out NetworkIdentity identity))
            return identity.netId;
        return 0;
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

    // Companion to FirstItemIn -- the same first-matching slot's stack
    // count. Always 1 for an equipment-carrying slot (by construction);
    // real for a plain stackable one (e.g. a Stick sitting in a hand).
    private int FirstItemCountIn(string slotName)
    {
        var inv = GetSlot(slotName);
        if (inv == null) return 0;
        foreach (var s in inv.Slots)
            if (s.item != null) return s.count;
        return 0;
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
