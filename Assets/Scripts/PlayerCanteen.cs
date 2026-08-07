using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerEquipment))]
public class PlayerCanteen : MonoBehaviour
{
    // Tried in order when equipping. "Belt" isn't a PlayerEquipment slot
    // name — it's a sentinel meaning "the currently-equipped Belt's
    // attachment points" (see BeltSlot handling below). A worn Belt
    // occupies the body's actual Waist slot, so a bare Canteen without one
    // only ever has the two hands to fall back to.
    private const string BeltSlot = "Belt";
    private static readonly string[] HandSlots = { "Left Hand", "Right Hand" };

    [SerializeField] private ItemDefinition canteenItem;
    [SerializeField] private Transform leftHandSlotAnchor;
    [SerializeField] private Transform rightHandSlotAnchor;
    [SerializeField] private Transform beltSlotAnchor;
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;

    private PlayerInventory playerInventory;
    private PlayerEquipment equipment;
    private PlayerLoot loot;
    private PlayerBelt beltCarrier;

    // Unlike Sunglasses/PersonalHealthMonitor, a canteen has no dedicated
    // "worn" slot — holding it in a hand or clipped to a worn Belt's
    // attachment points is what carrying/equipped means for it.
    public Canteen Equipped
    {
        get
        {
            foreach (var slotName in HandSlots)
                if (equipment.GetEquipped(slotName) is Canteen c) return c;

            var belt = beltCarrier != null ? beltCarrier.Equipped : null;
            if (belt != null)
                foreach (var slot in belt.Inventory.Slots)
                    if (slot.equipment is Canteen c) return c;

            return null;
        }
    }

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
        equipment = GetComponent<PlayerEquipment>();
        loot = GetComponent<PlayerLoot>();
        beltCarrier = GetComponent<PlayerBelt>();
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

    // Every carry location that's currently free and would actually accept
    // this canteen right now — read by InventoryScreen to decide whether
    // Equip can commit immediately (0 or 1 option) or needs to ask the
    // player which one they want (2+ options, e.g. both hands free and a
    // Belt worn).
    public System.Collections.Generic.List<string> AvailableDestinations(Canteen canteen)
    {
        var result = new System.Collections.Generic.List<string>();

        foreach (var slotName in HandSlots)
        {
            var slot = equipment.GetSlot(slotName);
            if (slot != null && slot.Slots.Count < slot.Capacity) result.Add(slotName);
        }

        var belt = beltCarrier != null ? beltCarrier.Equipped : null;
        if (belt != null && belt.Inventory.Slots.Count < belt.Inventory.Capacity)
            result.Add(BeltSlot);

        return result;
    }

    // Moves the canteen from a regular inventory slot onto the first
    // available carry location (see AvailableDestinations for the order).
    public bool Equip(Canteen canteen)
    {
        var destinations = AvailableDestinations(canteen);
        return destinations.Count > 0 && EquipTo(canteen, destinations[0]);
    }

    // Moves the canteen onto a specific carry location the player chose
    // (see InventoryScreen's Equip destination popup) rather than picking
    // one automatically.
    public bool EquipTo(Canteen canteen, string destination)
    {
        if (canteen == null || destination == null) return false;

        if (destination == BeltSlot)
        {
            var belt = beltCarrier != null ? beltCarrier.Equipped : null;
            if (belt == null || !belt.Inventory.AddEquipmentItem(canteenItem, canteen)) return false;

            playerInventory.Inventory.RemoveEquipmentItem(canteenItem);
            canteen.SetCarried(true, AnchorFor(BeltSlot));
            return true;
        }

        var slot = equipment.GetSlot(destination);
        if (slot == null || !slot.AddEquipmentItem(canteenItem, canteen)) return false;

        playerInventory.Inventory.RemoveEquipmentItem(canteenItem);
        canteen.SetCarried(true, AnchorFor(destination));
        return true;
    }

    // Moves the canteen from wherever it's equipped back into a regular
    // inventory slot. Fails (leaving it equipped) if the inventory is full.
    public bool Unequip(Canteen canteen)
    {
        string slotName = FindSlot(canteen);
        if (slotName == null) return false;
        if (!playerInventory.Inventory.AddEquipmentItem(canteenItem, canteen)) return false;

        RemoveFromSlot(slotName, canteen);
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
            RemoveFromSlot(slotName, canteen);
        else
            playerInventory.Inventory.RemoveEquipmentItem(canteenItem);

        canteen.SetCarried(false, null);
        canteen.transform.position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
    }

    public string FindSlot(Canteen canteen)
    {
        foreach (var slotName in HandSlots)
            if ((equipment.GetEquipped(slotName) as Canteen) == canteen)
                return slotName;

        var belt = beltCarrier != null ? beltCarrier.Equipped : null;
        if (belt != null)
            foreach (var slot in belt.Inventory.Slots)
                if ((slot.equipment as Canteen) == canteen)
                    return BeltSlot;

        return null;
    }

    private void RemoveFromSlot(string slotName, Canteen canteen)
    {
        if (slotName == BeltSlot)
        {
            var belt = beltCarrier != null ? beltCarrier.Equipped : null;
            belt?.Inventory.RemoveEquipmentItem(canteenItem);
        }
        else
        {
            equipment.GetSlot(slotName)?.RemoveEquipmentItem(canteenItem);
        }
    }

    private Transform AnchorFor(string slotName)
    {
        Transform anchor = slotName switch
        {
            "Left Hand" => leftHandSlotAnchor,
            "Right Hand" => rightHandSlotAnchor,
            BeltSlot => beltSlotAnchor,
            _ => null,
        };
        return anchor != null ? anchor : transform;
    }
}
