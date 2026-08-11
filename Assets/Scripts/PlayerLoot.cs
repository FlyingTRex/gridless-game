using UnityEngine;

// Decides where a newly picked-up item goes: into the equipped Backpack if
// there is one, otherwise into a free hand — evicting (physically dropping)
// whatever's in a hand if both are already occupied by something the new
// item can't stack into.
[RequireComponent(typeof(PlayerEquipment))]
[RequireComponent(typeof(PlayerEncumbrance))]
public class PlayerLoot : MonoBehaviour
{
    // Tried in order; Left Hand is evicted first if both are occupied.
    private static readonly string[] HandSlots = { "Left Hand", "Right Hand" };

    private PlayerEquipment equipment;
    private PlayerBackpack backpackCarrier;
    private PlayerDropping dropping;
    private PlayerEncumbrance encumbrance;

    private void Awake()
    {
        equipment = GetComponent<PlayerEquipment>();
        backpackCarrier = GetComponent<PlayerBackpack>();
        dropping = GetComponent<PlayerDropping>();
        encumbrance = GetComponent<PlayerEncumbrance>();
    }

    // At or over max capacity, refuse every pickup outright (2026-08-10,
    // Ben's call: "whatever you try to pick up, you can't") — a hard gate
    // on current load, not a per-item "would this specific pickup push
    // you over" check. Existing callers (Pickup.Complete) already treat a
    // full return value as "nothing fit, leave it on the ground," so no
    // caller-side change was needed to make this land correctly.
    private bool IsAtOrOverCapacity => encumbrance != null && encumbrance.LoadRatio >= 1f;

    // Returns the amount that did NOT fit anywhere (0 means fully picked up).
    public int Receive(ItemDefinition item, int quantity)
    {
        if (item == null || quantity <= 0) return quantity;
        if (IsAtOrOverCapacity) return quantity;

        var backpack = backpackCarrier != null ? backpackCarrier.Equipped : null;
        if (backpack != null)
            return backpack.Inventory.AddItem(item, quantity);

        foreach (var slotName in HandSlots)
        {
            var hand = equipment.GetSlot(slotName);
            if (hand == null) continue;

            int leftover = hand.AddItem(item, quantity);
            if (leftover < quantity) return leftover;
        }

        // Both hands refused it outright (occupied by something this item
        // can't stack with) — evict whatever's in the first hand to make
        // room, then place the new item there. Only evicts a plain item —
        // an equipment occupant (e.g. a held Canteen) would need the
        // generic drop path below, which doesn't know how to detach a
        // physical IEquippable correctly (no worldPickupPrefab for it, and
        // dropping this way orphans the real object). Same conservative
        // rule ReceiveEquipment() already applies in the mirror case.
        var evictHand = equipment.GetSlot(HandSlots[0]);
        if (evictHand == null) return quantity;

        if (evictHand.Slots.Count > 0 && evictHand.Slots[0].equipment == null)
            dropping?.DropFrom(evictHand, evictHand.Slots[0].item);

        return evictHand.AddItem(item, quantity);
    }

    // Same priority as Receive(), for equipment-type pickups (Backpack,
    // Canteen) instead of plain stackable items. Sets the item's own
    // visual state to match where it landed (hidden if packed inside a
    // container, carried/visible if it ended up in a hand). Returns false
    // if nothing had room — the caller (PlayerBackpack/PlayerCanteen)
    // falls back to stashing straight into the main inventory in that
    // case. Deliberately won't evict another equipment item from a hand
    // to make room (unlike Receive(), which can evict a plain item) —
    // swapping out someone's held Canteen for a picked-up Backpack is a
    // rarer, riskier edge case than the plain-item case and not worth the
    // complexity here.
    public bool ReceiveEquipment(ItemDefinition item, IEquippable equippable)
    {
        if (item == null || equippable == null) return false;
        if (IsAtOrOverCapacity) return false;

        var backpack = backpackCarrier != null ? backpackCarrier.Equipped : null;
        if (backpack != null && !ReferenceEquals(backpack, equippable) && backpack.Inventory.AddEquipmentItem(item, equippable))
        {
            equippable.Stash();
            return true;
        }

        foreach (var slotName in HandSlots)
        {
            var hand = equipment.GetSlot(slotName);
            if (hand != null && hand.AddEquipmentItem(item, equippable))
            {
                equippable.SetCarried(true, transform);
                return true;
            }
        }

        var evictHand = equipment.GetSlot(HandSlots[0]);
        if (evictHand != null && evictHand.Slots.Count > 0)
        {
            var occupant = evictHand.Slots[0];
            if (occupant.equipment == null)
            {
                dropping?.DropFrom(evictHand, occupant.item);
                if (evictHand.AddEquipmentItem(item, equippable))
                {
                    equippable.SetCarried(true, transform);
                    return true;
                }
            }
        }

        return false;
    }
}
