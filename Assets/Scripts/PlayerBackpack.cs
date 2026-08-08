using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerEquipment))]
public class PlayerBackpack : MonoBehaviour
{
    private const string BackSlot = "Back";
    // Where PlayerLoot might have placed a picked-up backpack that hasn't
    // been (or can't be) worn — checked by Unequip/Drop so they find it
    // regardless of which of these it landed in.
    private static readonly string[] HandSlots = { "Left Hand", "Right Hand" };

    [SerializeField] private Transform carrySlot;
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;
    // Matches Pickup.DespawnDelay — see Despawn.cs for why the Backpack
    // itself (Stash()) is what cancels this on pickup, not this script.
    [SerializeField] private float despawnDelay = 120f;

    private PlayerInventory playerInventory;
    private PlayerEquipment equipment;
    private PlayerLoot loot;

    public Backpack Equipped => equipment.GetEquipped(BackSlot) as Backpack;

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
        equipment = GetComponent<PlayerEquipment>();
        loot = GetComponent<PlayerLoot>();
    }

    // Called when the player interacts with a backpack lying in the world.
    // Routes through PlayerLoot first (equipped backpack's own contents,
    // then a free hand) — falls back to stashing as a regular (hidden)
    // inventory item only if PlayerLoot found nowhere else for it.
    public bool PickUp(Backpack backpack)
    {
        if (backpack == null) return false;

        if (loot != null && loot.ReceiveEquipment(backpack.ItemDefinition, backpack))
            return true;

        if (!playerInventory.Inventory.AddEquipmentItem(backpack.ItemDefinition, backpack)) return false;

        backpack.Stash();
        return true;
    }

    // Moves the backpack onto the Back slot from wherever it currently is
    // (a regular inventory slot, or a hand if PlayerLoot put it there on
    // pickup).
    public bool Equip(Backpack backpack)
    {
        if (backpack == null) return false;

        string currentSlot = FindSlot(backpack);
        var slot = equipment.GetSlot(BackSlot);
        if (slot == null || !slot.AddEquipmentItem(backpack.ItemDefinition, backpack)) return false;

        if (currentSlot != null)
            equipment.GetSlot(currentSlot)?.RemoveEquipmentItem(backpack.ItemDefinition);
        else
            playerInventory.Inventory.RemoveEquipmentItem(backpack.ItemDefinition);

        backpack.SetCarried(true, carrySlot != null ? carrySlot : transform);
        return true;
    }

    // Moves the backpack from the Back slot (or a hand, if PlayerLoot put
    // it there) back into a regular inventory slot. Prefers the main
    // inventory; if that's full, tries a hand instead; if hands are full
    // too, drops it into the world rather than Unequip silently doing
    // nothing.
    public bool Unequip(Backpack backpack)
    {
        string slotName = FindSlot(backpack);
        if (backpack == null || slotName == null) return false;

        if (playerInventory.Inventory.AddEquipmentItem(backpack.ItemDefinition, backpack))
        {
            equipment.GetSlot(slotName)?.RemoveEquipmentItem(backpack.ItemDefinition);
            backpack.Stash();
            return true;
        }

        foreach (var handSlotName in HandSlots)
        {
            var hand = equipment.GetSlot(handSlotName);
            if (hand == null || handSlotName == slotName) continue;

            if (hand.AddEquipmentItem(backpack.ItemDefinition, backpack))
            {
                equipment.GetSlot(slotName)?.RemoveEquipmentItem(backpack.ItemDefinition);
                backpack.SetCarried(true, transform);
                return true;
            }
        }

        Drop(backpack);
        return true;
    }

    // Drops the backpack into the world in front of the player, wherever
    // it currently is (Back, a hand, or the regular inventory).
    public void Drop(Backpack backpack)
    {
        if (backpack == null) return;

        string slotName = FindSlot(backpack);
        if (slotName != null)
            equipment.GetSlot(slotName)?.RemoveEquipmentItem(backpack.ItemDefinition);
        else
            playerInventory.Inventory.RemoveEquipmentItem(backpack.ItemDefinition);

        backpack.SetCarried(false, null);
        backpack.transform.position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
        var despawn = backpack.gameObject.AddComponent<Despawn>();
        despawn.delay = despawnDelay;
    }

    // Searches Back, then the hands, for the given backpack instance.
    private string FindSlot(Backpack backpack)
    {
        if (Equipped == backpack) return BackSlot;

        foreach (var slotName in HandSlots)
            if ((equipment.GetEquipped(slotName) as Backpack) == backpack)
                return slotName;

        return null;
    }
}
