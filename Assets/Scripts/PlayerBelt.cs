using UnityEngine;

// Runs before default-order (0) scripts on the same GameObject -- Unity
// doesn't otherwise guarantee any particular Start() order between sibling
// components. PlayerCanteen's starting-canteen attachment (2026-08-12)
// needs the Settler's Belt to already be worn (Equipped != null) by the
// time its own Start() runs, since it attaches into the belt's own
// Inventory rather than a PlayerEquipment body slot.
[DefaultExecutionOrder(-10)]
[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerEquipment))]
public class PlayerBelt : MonoBehaviour
{
    private const string WaistSlot = "Waist";
    // Where PlayerLoot might have placed a picked-up belt that hasn't been
    // (or can't be) worn — checked by Unequip/Drop so they find it
    // regardless of which of these it landed in.
    private static readonly string[] HandSlots = PlayerEquipSlots.Hands;

    // Fallback only, used when PlayerBodyModel/the Hips bone isn't
    // available for some reason — the scene's pre-existing fixed anchor
    // (not bone-parented, doesn't follow animation).
    [SerializeField] private Transform carrySlot;
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;
    // Matches Pickup.DespawnDelay — see Despawn.cs for why Belt.Stash()/
    // SetCarried(true, ...) are what cancel this on pickup, not this script.
    [SerializeField] private float despawnDelay = 120f;

    // Root-relative worn offset (2026-08-13, same EquipmentAttach math as
    // Tool/Backpack) — Belt sits right at the hips, no offset needed as a
    // first guess.
    [SerializeField] private Vector3 wornPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 wornEulerOffset = Vector3.zero;

    // The player starts the game already wearing the Settler's Belt
    // variant specifically — same single-purpose starting-gear mechanism
    // PlayerShirt/PlayerJeans already established (2026-08-12), third
    // caller now, still not worth generalizing into a shared system.
    [SerializeField] private GameObject startingBeltPrefab;

    private PlayerInventory playerInventory;
    private PlayerEquipment equipment;
    private PlayerLoot loot;
    private PlayerBodyModel bodyModel;

    public Belt Equipped => equipment.GetEquipped(WaistSlot) as Belt;

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
        equipment = GetComponent<PlayerEquipment>();
        loot = GetComponent<PlayerLoot>();
        bodyModel = GetComponent<PlayerBodyModel>();
    }

    // Re-anchors the worn belt onto the current Hips bone — called by
    // PlayerBodyModel after a gender switch.
    public void RefreshAnchor()
    {
        if (Equipped == null) return;
        EquipmentAttach.Carry(Equipped, Equipped.transform, bodyModel, HumanBodyBones.Hips, carrySlot, transform, wornPositionOffset, wornEulerOffset);
    }

    // Start (not Awake) so every other component's Awake — including
    // PlayerEquipment building its slot dictionary — has already run.
    // Equipped != null guards against ever equipping a second belt.
    private void Start()
    {
        if (startingBeltPrefab == null || Equipped != null) return;

        var instance = Instantiate(startingBeltPrefab);
        var belt = instance.GetComponent<Belt>();
        var slot = equipment.GetSlot(WaistSlot);

        if (belt != null && slot != null && slot.AddEquipmentItem(belt.ItemDefinition, belt))
            EquipmentAttach.Carry(belt, belt.transform, bodyModel, HumanBodyBones.Hips, carrySlot, transform, wornPositionOffset, wornEulerOffset);
        else
            Destroy(instance);
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

        EquipmentAttach.Carry(belt, belt.transform, bodyModel, HumanBodyBones.Hips, carrySlot, transform, wornPositionOffset, wornEulerOffset);
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

        DropClippedEquipment(belt);

        belt.SetCarried(false, null);
        belt.transform.position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
        var despawn = belt.gameObject.AddComponent<Despawn>();
        despawn.delay = despawnDelay;
    }

    // Real bug found live (2026-08-13): a Canteen clipped to a worn Belt
    // is a pure data relationship (registered in belt.Inventory), not a
    // Transform-hierarchy one — its physical object is bone-attached
    // directly (EquipmentAttach.Carry), not parented under the Belt
    // GameObject. Dropping the Belt alone left the Canteen still visibly
    // "worn," floating in place, with no owner. Detaches and drops every
    // physical equipment item still clipped to the belt at the same time,
    // scattered slightly so they don't all land exactly on top of the
    // belt itself.
    private void DropClippedEquipment(Belt belt)
    {
        var clipped = new System.Collections.Generic.List<(ItemDefinition item, IEquippable equipment)>();
        foreach (var slot in belt.Inventory.Slots)
            if (slot.equipment != null)
                clipped.Add((slot.item, slot.equipment));

        foreach (var (item, clippedEquipment) in clipped)
        {
            belt.Inventory.RemoveEquipmentItem(item);

            clippedEquipment.SetCarried(false, null);
            var clippedTransform = ((Component)clippedEquipment).transform;
            Vector3 scatter = Random.insideUnitSphere * 0.3f;
            clippedTransform.position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight + scatter;

            var despawn = ((Component)clippedEquipment).gameObject.AddComponent<Despawn>();
            despawn.delay = despawnDelay;
        }
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
