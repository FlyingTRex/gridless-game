using UnityEngine;

// Carrier for Tool (Knife/Pickaxe/Hammer/Axe, any tier). Structured like
// PlayerCanteen.cs's hand-only portion, minus the Belt case — a tool has no
// body/belt slot, it only ever makes sense held in a hand. Unlike Canteen
// (which needs leftHandSlotAnchor/rightHandSlotAnchor as two distinct
// fields), a single handAnchor is enough here — the scene's existing
// HandAnchor object is already the one Canteen itself uses for both hands,
// so there's no real second anchor point to distinguish.
[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerEquipment))]
public class PlayerTool : MonoBehaviour
{
    private static readonly string[] HandSlots = PlayerEquipSlots.Hands;

    // Fallback only, used when PlayerBodyModel/the RightHand bone isn't
    // available for some reason — the scene's pre-existing fixed
    // HandAnchor child (not bone-parented, doesn't follow animation).
    [SerializeField] private Transform handAnchor;
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;
    // Matches Pickup.DespawnDelay — see Despawn.cs for why Tool.Stash()/
    // SetCarried(true, ...) are what cancel this on pickup, not this script.
    [SerializeField] private float despawnDelay = 120f;

    // Root-relative hold offset (2026-08-13, same EquipmentAttach math
    // NPCEquipmentVisual uses) — default identity, same first-pass guess
    // shipped for NPCs.
    [SerializeField] private Vector3 holdPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 holdEulerOffset = Vector3.zero;

    private PlayerInventory playerInventory;
    private PlayerEquipment equipment;
    private PlayerLoot loot;
    private PlayerBodyModel bodyModel;

    public Tool Equipped
    {
        get
        {
            foreach (var slotName in HandSlots)
                if (equipment.GetEquipped(slotName) is Tool t) return t;
            return null;
        }
    }

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
        equipment = GetComponent<PlayerEquipment>();
        loot = GetComponent<PlayerLoot>();
        bodyModel = GetComponent<PlayerBodyModel>();
    }

    // Re-anchors whatever's currently held onto the current hand bone —
    // called by PlayerBodyModel after a gender switch (the tool was
    // parented under the *previous* gender's now-inactive model).
    public void RefreshAnchor()
    {
        var current = Equipped;
        if (current == null) return;

        EquipmentAttach.Carry(current, current.transform, bodyModel, HumanBodyBones.RightHand, handAnchor, transform, holdPositionOffset, holdEulerOffset);
    }

    // Called when the player interacts with a tool lying in the world.
    // Routes through PlayerLoot first (equipped backpack's own contents,
    // then a free hand) — falls back to stashing as a regular (hidden)
    // inventory item only if PlayerLoot found nowhere else for it.
    public bool PickUp(Tool tool)
    {
        if (tool == null) return false;

        if (loot != null && loot.ReceiveEquipment(tool.ItemDefinition, tool))
            return true;

        if (!playerInventory.Inventory.AddEquipmentItem(tool.ItemDefinition, tool)) return false;

        tool.Stash();
        return true;
    }

    // Every hand slot currently free — read by InventoryScreen to decide
    // whether Equip can commit immediately (0 or 1 option) or needs to ask
    // the player which hand they want (both free).
    public System.Collections.Generic.List<string> AvailableDestinations(Tool tool)
    {
        var result = new System.Collections.Generic.List<string>();
        foreach (var slotName in HandSlots)
        {
            var slot = equipment.GetSlot(slotName);
            if (slot != null && slot.Slots.Count < slot.Capacity) result.Add(slotName);
        }
        return result;
    }

    // Moves the tool onto the first available hand from wherever it
    // currently is.
    public bool Equip(Tool tool)
    {
        var destinations = AvailableDestinations(tool);
        return destinations.Count > 0 && EquipTo(tool, destinations[0], playerInventory.Inventory);
    }

    // Moves the tool onto a specific hand the player chose (see
    // InventoryScreen's Equip destination popup) rather than picking one
    // automatically. Removes from the main inventory — only correct when
    // the tool is actually sitting there; use the source-aware overload
    // below when it might be in a Backpack instead.
    public bool EquipTo(Tool tool, string destination) =>
        EquipTo(tool, destination, playerInventory.Inventory);

    // Removes from whichever Inventory the tool is actually sitting in
    // (main inventory, or a worn Backpack's nested Inventory), instead of
    // assuming it's always the main inventory — same pattern established
    // across every equippable carrier during the drag-and-drop rework
    // (2026-08-12).
    public bool EquipTo(Tool tool, string destination, Inventory source)
    {
        if (tool == null || destination == null || source == null) return false;

        var slot = equipment.GetSlot(destination);
        if (slot == null || !slot.AddEquipmentItem(tool.ItemDefinition, tool)) return false;

        source.RemoveEquipmentItem(tool.ItemDefinition);
        EquipmentAttach.Carry(tool, tool.transform, bodyModel, HumanBodyBones.RightHand, handAnchor, transform, holdPositionOffset, holdEulerOffset);
        return true;
    }

    // Called by PlayerLoot.ReceiveEquipment (2026-08-13) so a tool picked
    // up directly off the ground into a free hand gets bone-attached the
    // same way an inventory-screen equip already does, instead of landing
    // at the player root. Assumes the caller already placed the item in a
    // hand PlayerEquipment slot.
    public void CarryPickedUp(Tool tool) =>
        EquipmentAttach.Carry(tool, tool.transform, bodyModel, HumanBodyBones.RightHand, handAnchor, transform, holdPositionOffset, holdEulerOffset);

    // Moves the tool from a hand back into a regular inventory slot. Fails
    // (leaving it equipped) if the inventory is full.
    public bool Unequip(Tool tool)
    {
        string slotName = FindSlot(tool);
        if (slotName == null) return false;
        if (!playerInventory.Inventory.AddEquipmentItem(tool.ItemDefinition, tool)) return false;

        equipment.GetSlot(slotName)?.RemoveEquipmentItem(tool.ItemDefinition);
        tool.Stash();
        return true;
    }

    // Drops the tool into the world in front of the player, whether it was
    // held or just sitting in the regular inventory.
    public void Drop(Tool tool)
    {
        if (tool == null) return;

        string slotName = FindSlot(tool);
        if (slotName != null)
            equipment.GetSlot(slotName)?.RemoveEquipmentItem(tool.ItemDefinition);
        else
            playerInventory.Inventory.RemoveEquipmentItem(tool.ItemDefinition);

        tool.SetCarried(false, null);
        tool.transform.position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
        var despawn = tool.gameObject.AddComponent<Despawn>();
        despawn.delay = despawnDelay;
    }

    public string FindSlot(Tool tool)
    {
        foreach (var slotName in HandSlots)
            if ((equipment.GetEquipped(slotName) as Tool) == tool)
                return slotName;
        return null;
    }
}
