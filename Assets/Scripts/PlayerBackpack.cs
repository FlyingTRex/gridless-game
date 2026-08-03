using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerEquipment))]
public class PlayerBackpack : MonoBehaviour
{
    private const string BackSlot = "Back";

    [SerializeField] private ItemDefinition backpackItem;
    [SerializeField] private Transform carrySlot;
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;

    private PlayerInventory playerInventory;
    private PlayerEquipment equipment;

    public Backpack Equipped => equipment.GetEquipped(BackSlot) as Backpack;

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
        equipment = GetComponent<PlayerEquipment>();
    }

    // Called when the player interacts with a backpack lying in the world —
    // stashes it as a regular (hidden) inventory item, not worn yet.
    public bool PickUp(Backpack backpack)
    {
        if (backpack == null) return false;
        if (!playerInventory.Inventory.AddEquipmentItem(backpackItem, backpack)) return false;

        backpack.Stash();
        return true;
    }

    // Moves the backpack from a regular inventory slot onto the Back slot.
    public bool Equip(Backpack backpack)
    {
        if (backpack == null) return false;

        var slot = equipment.GetSlot(BackSlot);
        if (slot == null || !slot.AddEquipmentItem(backpackItem, backpack)) return false;

        playerInventory.Inventory.RemoveEquipmentItem(backpackItem);
        backpack.SetCarried(true, carrySlot != null ? carrySlot : transform);
        return true;
    }

    // Moves the backpack from the Back slot back into a regular inventory
    // slot. Fails (leaving it equipped) if the regular inventory is full.
    public bool Unequip(Backpack backpack)
    {
        if (backpack == null || Equipped != backpack) return false;
        if (!playerInventory.Inventory.AddEquipmentItem(backpackItem, backpack)) return false;

        equipment.GetSlot(BackSlot)?.RemoveEquipmentItem(backpackItem);
        backpack.Stash();
        return true;
    }

    // Drops the backpack into the world in front of the player, whether it
    // was equipped or just sitting in the regular inventory.
    public void Drop(Backpack backpack)
    {
        if (backpack == null) return;

        if (Equipped == backpack)
            equipment.GetSlot(BackSlot)?.RemoveEquipmentItem(backpackItem);
        else
            playerInventory.Inventory.RemoveEquipmentItem(backpackItem);

        backpack.SetCarried(false, null);
        backpack.transform.position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
    }
}
