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
    // Fallback only, used when PlayerBodyModel/the Head bone isn't
    // available for some reason.
    [SerializeField] private Transform carrySlot;
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;
    // Matches Pickup.DespawnDelay — see Despawn.cs for why
    // MiningFaceShield.Stash()/SetCarried(true, ...) cancel this on
    // pickup, not this script.
    [SerializeField] private float despawnDelay = 120f;

    // Root-relative worn offset (2026-08-13, same EquipmentAttach math as
    // Tool/Backpack), same starting numbers as Sunglasses (also a Face
    // item, same slot).
    [SerializeField] private Vector3 wornPositionOffset = new Vector3(0f, 0.05f, 0.08f);
    [SerializeField] private Vector3 wornEulerOffset = Vector3.zero;

    private PlayerInventory playerInventory;
    private PlayerEquipment equipment;
    private PlayerLoot loot;
    private PlayerBodyModel bodyModel;

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
        bodyModel = GetComponent<PlayerBodyModel>();
    }

    // Re-anchors the worn shield onto the current Head bone — called by
    // PlayerBodyModel after a gender switch.
    public void RefreshAnchor()
    {
        if (Equipped == null) return;
        EquipmentAttach.Carry(Equipped, Equipped.transform, bodyModel, HumanBodyBones.Head, carrySlot, transform, wornPositionOffset, wornEulerOffset);
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

    // See PlayerBackpack.Equip's source-aware overload comment (2026-08-12)
    // — FindSlot doesn't know about a shield sitting inside a backpack's
    // nested Inventory, so use the overload below when the caller already
    // knows exactly where it is.
    public bool Equip(MiningFaceShield shield) => Equip(shield, playerInventory.Inventory);

    public bool Equip(MiningFaceShield shield, Inventory source)
    {
        if (shield == null || source == null) return false;

        string currentSlot = FindSlot(shield);
        var slot = equipment.GetSlot(FaceSlot);
        if (slot == null || !slot.AddEquipmentItem(shieldItem, shield)) return false;

        if (currentSlot != null)
            equipment.GetSlot(currentSlot)?.RemoveEquipmentItem(shieldItem);
        else
            source.RemoveEquipmentItem(shieldItem);

        EquipmentAttach.Carry(shield, shield.transform, bodyModel, HumanBodyBones.Head, carrySlot, transform, wornPositionOffset, wornEulerOffset);
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
