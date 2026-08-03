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
        if (!equipment.Equip(BackSlot, backpack)) return false;

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

        equipment.Unequip(BackSlot);
        backpack.Stash();
        return true;
    }

    // Drops the backpack into the world in front of the player, whether it
    // was equipped or just sitting in the regular inventory.
    public void Drop(Backpack backpack)
    {
        if (backpack == null) return;

        if (Equipped == backpack)
            equipment.Unequip(BackSlot);
        else
            playerInventory.Inventory.RemoveEquipmentItem(backpackItem);

        backpack.SetCarried(false, null);
        backpack.transform.position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
    }

    private void OnGUI()
    {
        var backpack = Equipped;
        if (backpack == null) return;

        GUILayout.BeginArea(new Rect(320, 10, 280, 320));
        GUILayout.Label($"Back: {backpack.DisplayName}", GUI.skin.box);

        bool unequipClicked = GUILayout.Button("Unequip");
        bool dropClicked = GUILayout.Button("Drop Backpack");

        ItemDefinition moveClicked = null;
        if (!unequipClicked && !dropClicked)
        {
            var slots = backpack.Inventory.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{slot.item.itemName} x{slot.count}");
                if (GUILayout.Button("To Inventory", GUILayout.Width(90)))
                    moveClicked = slot.item;
                GUILayout.EndHorizontal();
            }
        }

        GUILayout.EndArea();

        if (unequipClicked)
            Unequip(backpack);
        else if (dropClicked)
            Drop(backpack);
        else if (moveClicked != null)
            InventoryTransfer.Move(backpack.Inventory, playerInventory.Inventory, moveClicked, backpack.Inventory.GetCount(moveClicked));
    }
}
