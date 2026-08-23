using Mirror;
using UnityEngine;

// Multiplayer Phase 3 sub-phase 5, 2026-08-23: converted to
// NetworkBehaviour, plus RequestDrink/CmdDrink and RequestFill/CmdFill.
// A genuinely different shape from Eating/Medicine's container-key
// Commands — Drink/Fill act on the physical Canteen instance itself
// (whichever one is currently carried), not a container removal, so no
// item id or container key needs to travel over the wire at all. The
// Command just calls this.Equipped server-side (already the real,
// server-authoritative carried instance, same "read it fresh off real
// component state" pattern as PlayerCombat's ResolveAttack) and invokes
// Drink/Fill on it directly.
[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerEquipment))]
public class PlayerCanteen : NetworkBehaviour
{
    // Tried in order when equipping. "Belt" isn't a PlayerEquipment slot
    // name — it's a sentinel meaning "the currently-equipped Belt's
    // attachment points" (see BeltSlot handling below). A worn Belt
    // occupies the body's actual Waist slot, so a bare Canteen without one
    // only ever has the two hands to fall back to.
    private const string BeltSlot = "Belt";
    private static readonly string[] HandSlots = PlayerEquipSlots.Hands;

    [SerializeField] private ItemDefinition canteenItem;
    // Fallback only, used when PlayerBodyModel/the relevant bone isn't
    // available for some reason — the scene's pre-existing fixed anchors
    // (not bone-parented, don't follow animation).
    [SerializeField] private Transform leftHandSlotAnchor;
    [SerializeField] private Transform rightHandSlotAnchor;
    [SerializeField] private Transform beltSlotAnchor;
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;
    // Matches Pickup.DespawnDelay — see Despawn.cs for why Canteen.Stash()/
    // SetCarried(true, ...) are what cancel this on pickup, not this script.
    [SerializeField] private float despawnDelay = 120f;

    // Root-relative offsets (2026-08-13, same EquipmentAttach math as
    // Tool/Backpack) — hand offset shared by both hands (mirrors Tool's
    // "one grip offset regardless of which hand" convention), belt offset
    // pushed to the hip's side so it doesn't overlap a worn Belt itself.
    [SerializeField] private Vector3 handPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 handEulerOffset = Vector3.zero;
    [SerializeField] private Vector3 beltPositionOffset = new Vector3(0.15f, 0f, 0.05f);
    [SerializeField] private Vector3 beltEulerOffset = Vector3.zero;

    // The player starts the game with a Canteen already clipped to the
    // Settler's Belt specifically — same single-purpose starting-gear
    // mechanism PlayerShirt/PlayerJeans/PlayerBelt already established
    // (2026-08-12), just attaching into the belt's own Inventory instead
    // of a PlayerEquipment body slot. Needs PlayerBelt's Start() (which
    // equips the belt itself) to have already run — see
    // PlayerBelt's [DefaultExecutionOrder(-10)].
    [SerializeField] private GameObject startingCanteenPrefab;

    private PlayerInventory playerInventory;
    private PlayerEquipment equipment;
    private PlayerLoot loot;
    private PlayerBelt beltCarrier;
    private PlayerBodyModel bodyModel;
    private PlayerVitals vitals;

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
        bodyModel = GetComponent<PlayerBodyModel>();
        vitals = GetComponent<PlayerVitals>();
    }

    // Bone + fallback + offset for a given carry destination — the single
    // place that knows how each of the three spots maps onto the rig.
    private void Carry(Canteen canteen, string slotName)
    {
        switch (slotName)
        {
            case "Left Hand":
                EquipmentAttach.Carry(canteen, canteen.transform, bodyModel, HumanBodyBones.LeftHand, leftHandSlotAnchor, transform, handPositionOffset, handEulerOffset);
                break;
            case "Right Hand":
                EquipmentAttach.Carry(canteen, canteen.transform, bodyModel, HumanBodyBones.RightHand, rightHandSlotAnchor, transform, handPositionOffset, handEulerOffset);
                break;
            case BeltSlot:
                EquipmentAttach.Carry(canteen, canteen.transform, bodyModel, HumanBodyBones.Hips, beltSlotAnchor, transform, beltPositionOffset, beltEulerOffset);
                break;
        }
    }

    // Called by PlayerLoot.ReceiveEquipment (2026-08-13) so a canteen
    // picked up directly off the ground into a free hand gets bone-attached
    // the same way an inventory-screen equip already does. Assumes the
    // caller already placed it in the given hand's PlayerEquipment slot.
    public void CarryPickedUp(Canteen canteen, string handSlotName) => Carry(canteen, handSlotName);

    // Re-anchors wherever the canteen currently is onto the current
    // gender's bones — called by PlayerBodyModel after a gender switch.
    public void RefreshAnchor()
    {
        var current = Equipped;
        if (current == null) return;

        string slotName = FindSlot(current);
        if (slotName != null) Carry(current, slotName);
    }

    // Start (not Awake) so PlayerBelt's own Start (equipping the Settler's
    // Belt) has already run. Equipped != null guards against ever
    // attaching a second starting canteen; belt == null means no belt got
    // equipped this run (e.g. startingBeltPrefab unset), in which case
    // there's nothing to clip a canteen to.
    private void Start()
    {
        if (startingCanteenPrefab == null || Equipped != null) return;

        var belt = beltCarrier != null ? beltCarrier.Equipped : null;
        if (belt == null) return;

        var instance = Instantiate(startingCanteenPrefab);
        NetworkSpawnHelper.SpawnIfNetworked(instance);
        var canteen = instance.GetComponent<Canteen>();

        if (canteen != null && belt.Inventory.AddEquipmentItem(canteenItem, canteen))
            Carry(canteen, BeltSlot);
        else
            Destroy(instance);
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
    // one automatically. Removes from the main inventory — only correct
    // when the canteen is actually sitting there; a canteen picked up into
    // a Backpack (PlayerLoot.ReceiveEquipment) needs the source-aware
    // overload below instead.
    public bool EquipTo(Canteen canteen, string destination) =>
        EquipTo(canteen, destination, playerInventory.Inventory);

    // Same as above, but removes from whichever inventory the canteen is
    // actually sitting in (e.g. a Backpack's nested Inventory reached via
    // InventoryScreen's move popup) instead of assuming it's always the
    // main inventory. Real bug (2026-08-12): equipping a canteen found
    // inside a worn Backpack used to always try to remove it from the main
    // inventory, which silently did nothing there — leaving a stale entry
    // behind in the Backpack while also adding a second copy to the belt.
    public bool EquipTo(Canteen canteen, string destination, Inventory source)
    {
        if (canteen == null || destination == null || source == null) return false;

        if (destination == BeltSlot)
        {
            var belt = beltCarrier != null ? beltCarrier.Equipped : null;
            if (belt == null || !belt.Inventory.AddEquipmentItem(canteenItem, canteen)) return false;

            source.RemoveEquipmentItem(canteenItem);
            Carry(canteen, BeltSlot);
            return true;
        }

        var slot = equipment.GetSlot(destination);
        if (slot == null || !slot.AddEquipmentItem(canteenItem, canteen)) return false;

        source.RemoveEquipmentItem(canteenItem);
        Carry(canteen, destination);
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
        var despawn = canteen.gameObject.AddComponent<Despawn>();
        despawn.delay = despawnDelay;
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

    public void RequestDrink() => CmdDrink();

    [Command]
    private void CmdDrink()
    {
        Equipped?.Drink(vitals);
    }

    public void RequestFill() => CmdFill();

    [Command]
    private void CmdFill()
    {
        Equipped?.Fill(LiquidType.Water);
    }
}
