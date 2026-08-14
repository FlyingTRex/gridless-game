using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerEquipment))]
public class PlayerShirt : MonoBehaviour
{
    private const string ChestSlot = "Chest";
    // Where PlayerLoot might have placed a picked-up shirt that hasn't
    // been (or can't be) worn — checked by Unequip/Drop so they find it
    // regardless of which of these it landed in.
    private static readonly string[] HandSlots = { "Left Hand", "Right Hand" };

    // Fallback only, used when PlayerBodyModel/the Chest bone isn't
    // available for some reason.
    [SerializeField] private Transform carrySlot;
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;
    // Matches Pickup.DespawnDelay — see Despawn.cs for why the Shirt itself
    // (Stash()) is what cancels this on pickup, not this script.
    [SerializeField] private float despawnDelay = 120f;

    // Root-relative worn offset (2026-08-13, same EquipmentAttach math as
    // Tool/Backpack) — started at Vector3.zero (a body-conforming garment,
    // no offset expected as a first guess), then live-tweaked by Ben
    // directly in the Play-mode Inspector to these values, same workflow
    // established for Boot/Backpack.
    [SerializeField] private Vector3 wornPositionOffset = new Vector3(0f, -0.33f, 0f);
    [SerializeField] private Vector3 wornEulerOffset = new Vector3(0f, 89f, 0f);

    // The player starts the game already wearing one — no generic
    // "starting gear" system exists anywhere else in the project (checked;
    // this is the first equippable that needs one), so this is a small,
    // single-purpose mechanism scoped to just this type rather than a
    // speculative generalized version for a single caller (2026-08-12).
    [SerializeField] private GameObject startingShirtPrefab;

    // "A small cache of survival rations" (docs/game-overview.md) — dropped
    // straight into the starting shirt's own pocket storage rather than a
    // second standalone starting-gear mechanism, since the shirt is already
    // guaranteed to exist and be equipped by the time this runs below.
    [SerializeField] private ItemDefinition startingRationItem;
    [SerializeField] private int startingRationCount = 2;

    private PlayerInventory playerInventory;
    private PlayerEquipment equipment;
    private PlayerLoot loot;
    private PlayerBodyModel bodyModel;

    public Shirt Equipped => equipment.GetEquipped(ChestSlot) as Shirt;

    // Re-anchors the worn shirt onto the current Chest bone — called by
    // PlayerBodyModel after a gender switch.
    public void RefreshAnchor()
    {
        if (Equipped == null) return;
        EquipmentAttach.Carry(Equipped, Equipped.transform, bodyModel, HumanBodyBones.Chest, carrySlot, transform, wornPositionOffset, wornEulerOffset);
    }

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
        equipment = GetComponent<PlayerEquipment>();
        loot = GetComponent<PlayerLoot>();
        bodyModel = GetComponent<PlayerBodyModel>();
    }

    // Start (not Awake) so every other component's Awake — including
    // PlayerEquipment building its slot dictionary — has already run.
    // Equipped != null guards against ever equipping a second one (e.g. if
    // one was somehow already worn before this fires).
    private void Start()
    {
        if (startingShirtPrefab == null || Equipped != null) return;

        var instance = Instantiate(startingShirtPrefab);
        var shirt = instance.GetComponent<Shirt>();
        var slot = equipment.GetSlot(ChestSlot);

        if (shirt != null && slot != null && slot.AddEquipmentItem(shirt.ItemDefinition, shirt))
        {
            EquipmentAttach.Carry(shirt, shirt.transform, bodyModel, HumanBodyBones.Chest, carrySlot, transform, wornPositionOffset, wornEulerOffset);
            if (startingRationItem != null && startingRationCount > 0)
                shirt.Inventory.AddItem(startingRationItem, startingRationCount);
        }
        else
        {
            Destroy(instance);
        }
    }

    // Called when the player interacts with a shirt lying in the world.
    // Routes through PlayerLoot first (equipped backpack's own contents,
    // then a free hand) — falls back to stashing as a regular (hidden)
    // inventory item only if PlayerLoot found nowhere else for it.
    public bool PickUp(Shirt shirt)
    {
        if (shirt == null) return false;

        if (loot != null && loot.ReceiveEquipment(shirt.ItemDefinition, shirt))
            return true;

        if (!playerInventory.Inventory.AddEquipmentItem(shirt.ItemDefinition, shirt)) return false;

        shirt.Stash();
        return true;
    }

    // Moves the shirt onto the Chest slot from wherever it currently is —
    // source-aware from the start (built after the drag-and-drop rework
    // established this pattern), not the "guess via FindSlot, fall back to
    // main inventory" shape earlier carriers had to be retrofitted with.
    public bool Equip(Shirt shirt) => Equip(shirt, playerInventory.Inventory);

    public bool Equip(Shirt shirt, Inventory source)
    {
        if (shirt == null || source == null) return false;

        string currentSlot = FindSlot(shirt);
        var slot = equipment.GetSlot(ChestSlot);
        if (slot == null || !slot.AddEquipmentItem(shirt.ItemDefinition, shirt)) return false;

        if (currentSlot != null)
            equipment.GetSlot(currentSlot)?.RemoveEquipmentItem(shirt.ItemDefinition);
        else
            source.RemoveEquipmentItem(shirt.ItemDefinition);

        EquipmentAttach.Carry(shirt, shirt.transform, bodyModel, HumanBodyBones.Chest, carrySlot, transform, wornPositionOffset, wornEulerOffset);
        return true;
    }

    // Moves the shirt from the Chest slot (or a hand, if PlayerLoot put it
    // there) back into a regular inventory slot. Prefers the main
    // inventory; if that's full, tries a hand instead; if hands are full
    // too, drops it into the world rather than Unequip silently doing
    // nothing.
    public bool Unequip(Shirt shirt)
    {
        string slotName = FindSlot(shirt);
        if (shirt == null || slotName == null) return false;

        if (playerInventory.Inventory.AddEquipmentItem(shirt.ItemDefinition, shirt))
        {
            equipment.GetSlot(slotName)?.RemoveEquipmentItem(shirt.ItemDefinition);
            shirt.Stash();
            return true;
        }

        foreach (var handSlotName in HandSlots)
        {
            var hand = equipment.GetSlot(handSlotName);
            if (hand == null || handSlotName == slotName) continue;

            if (hand.AddEquipmentItem(shirt.ItemDefinition, shirt))
            {
                equipment.GetSlot(slotName)?.RemoveEquipmentItem(shirt.ItemDefinition);
                shirt.SetCarried(true, transform);
                return true;
            }
        }

        Drop(shirt);
        return true;
    }

    // Drops the shirt into the world in front of the player, wherever it
    // currently is (Chest, a hand, or the regular inventory).
    public void Drop(Shirt shirt)
    {
        if (shirt == null) return;

        string slotName = FindSlot(shirt);
        if (slotName != null)
            equipment.GetSlot(slotName)?.RemoveEquipmentItem(shirt.ItemDefinition);
        else
            playerInventory.Inventory.RemoveEquipmentItem(shirt.ItemDefinition);

        shirt.SetCarried(false, null);
        shirt.transform.position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
        var despawn = shirt.gameObject.AddComponent<Despawn>();
        despawn.delay = despawnDelay;
    }

    // Searches Chest, then the hands, for the given shirt instance.
    private string FindSlot(Shirt shirt)
    {
        if (Equipped == shirt) return ChestSlot;

        foreach (var slotName in HandSlots)
            if ((equipment.GetEquipped(slotName) as Shirt) == shirt)
                return slotName;

        return null;
    }
}
