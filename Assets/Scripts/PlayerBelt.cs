using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerEquipment))]
public class PlayerBelt : MonoBehaviour
{
    private const string WaistSlot = "Waist";
    // Where PlayerLoot might have placed a picked-up belt that hasn't been
    // (or can't be) worn — checked by Unequip/Drop so they find it
    // regardless of which of these it landed in.
    private static readonly string[] HandSlots = { "Left Hand", "Right Hand" };

    [SerializeField] private Transform carrySlot;
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;
    // Matches Pickup.DespawnDelay — see Despawn.cs for why Belt.Stash()/
    // SetCarried(true, ...) are what cancel this on pickup, not this script.
    [SerializeField] private float despawnDelay = 120f;

    private PlayerInventory playerInventory;
    private PlayerEquipment equipment;
    private PlayerLoot loot;

    public Belt Equipped => equipment.GetEquipped(WaistSlot) as Belt;

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
        equipment = GetComponent<PlayerEquipment>();
        loot = GetComponent<PlayerLoot>();
    }

    // Called when the player interacts with a belt lying in the world.
    // Routes through PlayerLoot first (equipped backpack's own contents,
    // then a free hand) — falls back to stashing as a regular (hidden)
    // inventory item only if PlayerLoot found nowhere else for it.
    public bool PickUp(Belt belt)
    {
        if (belt == null) return false;

        if (loot != null && loot.ReceiveEquipment(belt.ItemDefinition, belt))
            return true;

        if (!playerInventory.Inventory.AddEquipmentItem(belt.ItemDefinition, belt)) return false;

        belt.Stash();
        return true;
    }

    // Moves the belt onto the Waist slot from wherever it currently is (a
    // regular inventory slot, or a hand if PlayerLoot put it there on
    // pickup). See PlayerBackpack.Equip's source-aware overload comment
    // (2026-08-12) — FindSlot doesn't know about a belt sitting inside a
    // backpack's nested Inventory, so use the overload below when the
    // caller already knows exactly where the belt is.
    public bool Equip(Belt belt) => Equip(belt, playerInventory.Inventory);

    public bool Equip(Belt belt, Inventory source)
    {
        if (belt == null || source == null) return false;

        string currentSlot = FindSlot(belt);
        var slot = equipment.GetSlot(WaistSlot);
        if (slot == null || !slot.AddEquipmentItem(belt.ItemDefinition, belt)) return false;

        if (currentSlot != null)
            equipment.GetSlot(currentSlot)?.RemoveEquipmentItem(belt.ItemDefinition);
        else
            source.RemoveEquipmentItem(belt.ItemDefinition);

        belt.SetCarried(true, carrySlot != null ? carrySlot : transform);
        return true;
    }

    // Moves the belt from the Waist slot (or a hand, if PlayerLoot put it
    // there) back into a regular inventory slot. Prefers the main
    // inventory; if that's full, tries a hand instead; if hands are full
    // too, drops it into the world rather than Unequip silently doing
    // nothing.
    public bool Unequip(Belt belt)
    {
        string slotName = FindSlot(belt);
        if (belt == null || slotName == null) return false;

        if (playerInventory.Inventory.AddEquipmentItem(belt.ItemDefinition, belt))
        {
            equipment.GetSlot(slotName)?.RemoveEquipmentItem(belt.ItemDefinition);
            belt.Stash();
            return true;
        }

        foreach (var handSlotName in HandSlots)
        {
            var hand = equipment.GetSlot(handSlotName);
            if (hand == null || handSlotName == slotName) continue;

            if (hand.AddEquipmentItem(belt.ItemDefinition, belt))
            {
                equipment.GetSlot(slotName)?.RemoveEquipmentItem(belt.ItemDefinition);
                belt.SetCarried(true, transform);
                return true;
            }
        }

        Drop(belt);
        return true;
    }

    // Drops the belt into the world in front of the player, wherever it
    // currently is (Waist, a hand, or the regular inventory).
    public void Drop(Belt belt)
    {
        if (belt == null) return;

        string slotName = FindSlot(belt);
        if (slotName != null)
            equipment.GetSlot(slotName)?.RemoveEquipmentItem(belt.ItemDefinition);
        else
            playerInventory.Inventory.RemoveEquipmentItem(belt.ItemDefinition);

        belt.SetCarried(false, null);
        belt.transform.position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
        var despawn = belt.gameObject.AddComponent<Despawn>();
        despawn.delay = despawnDelay;
    }

    // Searches Waist, then the hands, for the given belt instance.
    private string FindSlot(Belt belt)
    {
        if (Equipped == belt) return WaistSlot;

        foreach (var slotName in HandSlots)
            if ((equipment.GetEquipped(slotName) as Belt) == belt)
                return slotName;

        return null;
    }
}
