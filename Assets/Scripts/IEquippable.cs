using UnityEngine;

public interface IEquippable
{
    string DisplayName { get; }

    // Fully hides the object (used while it's stored as inert data — in
    // the main inventory, or inside another container's Inventory).
    void Stash();

    // Worn/held (visible, non-collidable, follows anchor) when anchor is
    // set, or released into the world as a normal physical object when
    // anchor is null.
    void SetCarried(bool value, Transform anchor);

    // True if this specific equippable is allowed to be worn/carried in the
    // named "carried" destination — a PlayerEquipment body slot name (see
    // PlayerEquipment.SlotConfig), or the "Belt" sentinel for the belt-clip
    // case (see PlayerCanteen). Added for drag-and-drop (2026-08-12): the
    // data layer (Inventory.AddEquipmentItem) doesn't itself restrict which
    // equippable type can occupy which PlayerEquipment slot — that
    // restriction previously only existed implicitly, in each *Carrier
    // script only ever calling AddEquipmentItem on its own one hardcoded
    // slot name. Dragging introduces the new possibility of dropping any
    // equippable onto any body-slot rect, so this is the explicit gate that
    // replaces "no code path does it" with "no code path is allowed to."
    bool CanEquipToSlot(string slotName);
}
