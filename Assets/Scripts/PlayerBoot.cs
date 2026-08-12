using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerEquipment))]
public class PlayerBoot : MonoBehaviour
{
    private const string FeetSlot = "Feet";
    private static readonly string[] HandSlots = { "Left Hand", "Right Hand" };

    [SerializeField] private Transform carrySlot;
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;
    // Matches Pickup.DespawnDelay — see Despawn.cs for why the Boot itself
    // (Stash()) is what cancels this on pickup, not this script.
    [SerializeField] private float despawnDelay = 120f;

    private PlayerInventory playerInventory;
    private PlayerEquipment equipment;
    private PlayerLoot loot;

    public Boot Equipped => equipment.GetEquipped(FeetSlot) as Boot;

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
        equipment = GetComponent<PlayerEquipment>();
        loot = GetComponent<PlayerLoot>();
    }

    public bool PickUp(Boot boot)
    {
        if (boot == null) return false;

        if (loot != null && loot.ReceiveEquipment(boot.ItemDefinition, boot))
            return true;

        if (!playerInventory.Inventory.AddEquipmentItem(boot.ItemDefinition, boot)) return false;

        boot.Stash();
        return true;
    }

    // See PlayerBackpack.Equip's source-aware overload comment (2026-08-12)
    // — FindSlot doesn't know about a boot sitting inside a backpack's
    // nested Inventory, so use the overload below when the caller already
    // knows exactly where the boot is.
    public bool Equip(Boot boot) => Equip(boot, playerInventory.Inventory);

    public bool Equip(Boot boot, Inventory source)
    {
        if (boot == null || source == null) return false;

        string currentSlot = FindSlot(boot);
        var slot = equipment.GetSlot(FeetSlot);
        if (slot == null || !slot.AddEquipmentItem(boot.ItemDefinition, boot)) return false;

        if (currentSlot != null)
            equipment.GetSlot(currentSlot)?.RemoveEquipmentItem(boot.ItemDefinition);
        else
            source.RemoveEquipmentItem(boot.ItemDefinition);

        boot.SetCarried(true, carrySlot != null ? carrySlot : transform);
        return true;
    }

    public bool Unequip(Boot boot)
    {
        string slotName = FindSlot(boot);
        if (boot == null || slotName == null) return false;

        if (playerInventory.Inventory.AddEquipmentItem(boot.ItemDefinition, boot))
        {
            equipment.GetSlot(slotName)?.RemoveEquipmentItem(boot.ItemDefinition);
            boot.Stash();
            return true;
        }

        foreach (var handSlotName in HandSlots)
        {
            var hand = equipment.GetSlot(handSlotName);
            if (hand == null || handSlotName == slotName) continue;

            if (hand.AddEquipmentItem(boot.ItemDefinition, boot))
            {
                equipment.GetSlot(slotName)?.RemoveEquipmentItem(boot.ItemDefinition);
                boot.SetCarried(true, transform);
                return true;
            }
        }

        Drop(boot);
        return true;
    }

    public void Drop(Boot boot)
    {
        if (boot == null) return;

        string slotName = FindSlot(boot);
        if (slotName != null)
            equipment.GetSlot(slotName)?.RemoveEquipmentItem(boot.ItemDefinition);
        else
            playerInventory.Inventory.RemoveEquipmentItem(boot.ItemDefinition);

        boot.SetCarried(false, null);
        boot.transform.position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
        var despawn = boot.gameObject.AddComponent<Despawn>();
        despawn.delay = despawnDelay;
    }

    private string FindSlot(Boot boot)
    {
        if (Equipped == boot) return FeetSlot;

        foreach (var slotName in HandSlots)
            if ((equipment.GetEquipped(slotName) as Boot) == boot)
                return slotName;

        return null;
    }
}
