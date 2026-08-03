using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerEquipment))]
public class PlayerCanteen : MonoBehaviour
{
    // Tried in order when equipping — first free slot wins.
    private static readonly string[] CanteenSlots = { "Left Hand", "Right Hand", "Waist" };

    [SerializeField] private ItemDefinition canteenItem;
    [SerializeField] private Transform leftHandSlotAnchor;
    [SerializeField] private Transform rightHandSlotAnchor;
    [SerializeField] private Transform waistSlotAnchor;
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;

    private PlayerInventory playerInventory;
    private PlayerEquipment equipment;
    private PlayerLoot loot;

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
        equipment = GetComponent<PlayerEquipment>();
        loot = GetComponent<PlayerLoot>();
    }

    // Called when the player interacts with a canteen lying in the world.
    // Routes through PlayerLoot first (equipped backpack's own contents,
    // then a free hand) — falls back to stashing as a regular (hidden)
    // inventory item only if PlayerLoot found nowhere else for it.
    public bool PickUp(Canteen canteen)
    {
        if (canteen == null) return false;

        if (loot != null && loot.ReceiveEquipment(canteenItem, canteen))
            return true;

        if (!playerInventory.Inventory.AddEquipmentItem(canteenItem, canteen)) return false;

        canteen.Stash();
        return true;
    }

    // Moves the canteen from a regular inventory slot onto the first
    // available slot in CanteenSlots (Left Hand, then Right Hand, then Waist).
    public bool Equip(Canteen canteen)
    {
        if (canteen == null) return false;

        foreach (var slotName in CanteenSlots)
        {
            var slot = equipment.GetSlot(slotName);
            if (slot == null || !slot.AddEquipmentItem(canteenItem, canteen)) continue;

            playerInventory.Inventory.RemoveEquipmentItem(canteenItem);
            canteen.SetCarried(true, AnchorFor(slotName));
            return true;
        }

        return false;
    }

    // Moves the canteen from wherever it's equipped back into a regular
    // inventory slot. Fails (leaving it equipped) if the inventory is full.
    public bool Unequip(Canteen canteen)
    {
        string slotName = FindSlot(canteen);
        if (slotName == null) return false;
        if (!playerInventory.Inventory.AddEquipmentItem(canteenItem, canteen)) return false;

        equipment.GetSlot(slotName).RemoveEquipmentItem(canteenItem);
        canteen.Stash();
        return true;
    }

    // Drops the canteen into the world in front of the player, whether it
    // was equipped or just sitting in the regular inventory.
    public void Drop(Canteen canteen)
    {
        if (canteen == null) return;

        string slotName = FindSlot(canteen);
        if (slotName != null)
            equipment.GetSlot(slotName).RemoveEquipmentItem(canteenItem);
        else
            playerInventory.Inventory.RemoveEquipmentItem(canteenItem);

        canteen.SetCarried(false, null);
        canteen.transform.position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
    }

    public string FindSlot(Canteen canteen)
    {
        foreach (var slotName in CanteenSlots)
            if ((equipment.GetEquipped(slotName) as Canteen) == canteen)
                return slotName;
        return null;
    }

    private Transform AnchorFor(string slotName)
    {
        Transform anchor = slotName switch
        {
            "Left Hand" => leftHandSlotAnchor,
            "Right Hand" => rightHandSlotAnchor,
            "Waist" => waistSlotAnchor,
            _ => null,
        };
        return anchor != null ? anchor : transform;
    }
}
