using UnityEngine;

// Carrier component, structured identically to PlayerSunglasses.cs. The one
// difference: no screen-tint OnGUI overlay — this equippable's effect is
// read externally via IsWorn (ResourceNode checks it to decide whether a
// hidden ore node is revealed and whether mining it yields real ore or just
// Small Rock), not drawn by this component itself.
[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerEquipment))]
public class PlayerMiningFaceShield : MonoBehaviour
{
    private const string FaceSlot = "Face";
    private static readonly string[] HandSlots = { "Left Hand", "Right Hand" };

    [SerializeField] private ItemDefinition shieldItem;
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;
    // Matches Pickup.DespawnDelay — see Despawn.cs for why
    // MiningFaceShield.Stash()/SetCarried(true, ...) cancel this on
    // pickup, not this script.
    [SerializeField] private float despawnDelay = 120f;

    private PlayerInventory playerInventory;
    private PlayerEquipment equipment;
    private PlayerLoot loot;

    // Face has capacity 2 — search the slot's own entries for this specific
    // instance rather than trusting PlayerEquipment.GetEquipped's "first
    // equipped item", same reasoning as PlayerSunglasses.
    public MiningFaceShield Equipped
    {
        get
        {
            var slot = equipment.GetSlot(FaceSlot);
            if (slot == null) return null;

            foreach (var s in slot.Slots)
                if (s.equipment is MiningFaceShield shield) return shield;

            return null;
        }
    }

    public bool IsWorn => Equipped != null;

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
        equipment = GetComponent<PlayerEquipment>();
        loot = GetComponent<PlayerLoot>();
    }

    public bool PickUp(MiningFaceShield shield)
    {
        if (shield == null) return false;

        if (loot != null && loot.ReceiveEquipment(shieldItem, shield))
            return true;

        if (!playerInventory.Inventory.AddEquipmentItem(shieldItem, shield)) return false;

        shield.Stash();
        return true;
    }

    public bool Equip(MiningFaceShield shield)
    {
        if (shield == null) return false;

        string currentSlot = FindSlot(shield);
        var slot = equipment.GetSlot(FaceSlot);
        if (slot == null || !slot.AddEquipmentItem(shieldItem, shield)) return false;

        if (currentSlot != null)
            equipment.GetSlot(currentSlot)?.RemoveEquipmentItem(shieldItem);
        else
            playerInventory.Inventory.RemoveEquipmentItem(shieldItem);

        shield.SetCarried(true, transform);
        return true;
    }

    public bool Unequip(MiningFaceShield shield)
    {
        string slotName = FindSlot(shield);
        if (shield == null || slotName == null) return false;

        if (playerInventory.Inventory.AddEquipmentItem(shieldItem, shield))
        {
            equipment.GetSlot(slotName)?.RemoveEquipmentItem(shieldItem);
            shield.Stash();
            return true;
        }

        foreach (var handSlotName in HandSlots)
        {
            var hand = equipment.GetSlot(handSlotName);
            if (hand == null || handSlotName == slotName) continue;

            if (hand.AddEquipmentItem(shieldItem, shield))
            {
                equipment.GetSlot(slotName)?.RemoveEquipmentItem(shieldItem);
                shield.SetCarried(true, transform);
                return true;
            }
        }

        Drop(shield);
        return true;
    }

    public void Drop(MiningFaceShield shield)
    {
        if (shield == null) return;

        string slotName = FindSlot(shield);
        if (slotName != null)
            equipment.GetSlot(slotName)?.RemoveEquipmentItem(shieldItem);
        else
            playerInventory.Inventory.RemoveEquipmentItem(shieldItem);

        shield.SetCarried(false, null);
        shield.transform.position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
        var despawn = shield.gameObject.AddComponent<Despawn>();
        despawn.delay = despawnDelay;
    }

    private string FindSlot(MiningFaceShield shield)
    {
        if (Equipped == shield) return FaceSlot;

        foreach (var slotName in HandSlots)
            if ((equipment.GetEquipped(slotName) as MiningFaceShield) == shield)
                return slotName;

        return null;
    }
}
