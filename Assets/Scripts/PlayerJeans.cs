using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerEquipment))]
public class PlayerJeans : MonoBehaviour
{
    private const string LegSlot = "Leg";
    // Where PlayerLoot might have placed a picked-up pair that hasn't
    // been (or can't be) worn — checked by Unequip/Drop so they find it
    // regardless of which of these it landed in.
    private static readonly string[] HandSlots = { "Left Hand", "Right Hand" };

    // Fallback only, used when PlayerBodyModel/the Hips bone isn't
    // available for some reason.
    [SerializeField] private Transform carrySlot;
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;
    // Matches Pickup.DespawnDelay — see Despawn.cs for why the Jeans
    // themselves (Stash()) cancel this on pickup, not this script.
    [SerializeField] private float despawnDelay = 120f;

    // Root-relative worn offset (2026-08-13, same EquipmentAttach math as
    // Tool/Backpack).
    //
    // Live-feedback round 4 (Ben, screenshots): a blue/black rolled shape
    // kept appearing above the head across two rounds of Backpack-only
    // tuning (Y position, then X pitch) with zero visible change — meaning
    // it was never the Backpack. Re-diagnosed: the color matches denim,
    // and Jeans was the one type that never got a rotation correction at
    // all (still identity). Same theory as the Backpack fix: Jeans'
    // worldPickupPrefab is authored lying flat on the ground like every
    // other dropped pickup, so with zero rotation correction its legs
    // point in whatever direction was "up" while lying flat — which,
    // parented to an upright Hips bone with no correction, is consistent
    // with legs pointing straight up past the head instead of down.
    //
    // Round 5 (Ben, follow-up screenshot): the -90 X attempt was real
    // progress — moved from "straight up past the head" to "sideways,
    // hanging near the hand/arm" — confirming X-pitch is the right axis,
    // just not enough of it. -90 got roughly a quarter-turn from vertical;
    // doubling to -180 should swing it the rest of the way from
    // horizontal to pointing down.
    [SerializeField] private Vector3 wornPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 wornEulerOffset = new Vector3(-180f, 0f, 0f);

    // The player starts the game already wearing the Settler's Jeans
    // variant specifically — same single-purpose starting-gear mechanism
    // PlayerShirt already established (2026-08-12), not a generalized
    // system since this is still only the second caller.
    [SerializeField] private GameObject startingJeansPrefab;

    private PlayerInventory playerInventory;
    private PlayerEquipment equipment;
    private PlayerLoot loot;
    private PlayerBodyModel bodyModel;

    public Jeans Equipped => equipment.GetEquipped(LegSlot) as Jeans;

    // Re-anchors the worn jeans onto the current Hips bone — called by
    // PlayerBodyModel after a gender switch.
    public void RefreshAnchor()
    {
        if (Equipped == null) return;
        EquipmentAttach.Carry(Equipped, Equipped.transform, bodyModel, HumanBodyBones.Hips, carrySlot, transform, wornPositionOffset, wornEulerOffset);
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
    // Equipped != null guards against ever equipping a second pair.
    private void Start()
    {
        if (startingJeansPrefab == null || Equipped != null) return;

        var instance = Instantiate(startingJeansPrefab);
        var jeans = instance.GetComponent<Jeans>();
        var slot = equipment.GetSlot(LegSlot);

        if (jeans != null && slot != null && slot.AddEquipmentItem(jeans.ItemDefinition, jeans))
            EquipmentAttach.Carry(jeans, jeans.transform, bodyModel, HumanBodyBones.Hips, carrySlot, transform, wornPositionOffset, wornEulerOffset);
        else
            Destroy(instance);
    }

    // Called when the player interacts with a pair of jeans lying in the
    // world. Routes through PlayerLoot first (equipped backpack's own
    // storage, then a free hand) — falls back to stashing as a regular
    // (hidden) inventory item only if PlayerLoot found nowhere else for it.
    public bool PickUp(Jeans jeans)
    {
        if (jeans == null) return false;

        if (loot != null && loot.ReceiveEquipment(jeans.ItemDefinition, jeans))
            return true;

        if (!playerInventory.Inventory.AddEquipmentItem(jeans.ItemDefinition, jeans)) return false;

        jeans.Stash();
        return true;
    }

    // Moves the jeans onto the Leg slot from wherever they currently are —
    // source-aware from the start, same pattern PlayerShirt/PlayerCanteen
    // established this session.
    public bool Equip(Jeans jeans) => Equip(jeans, playerInventory.Inventory);

    public bool Equip(Jeans jeans, Inventory source)
    {
        if (jeans == null || source == null) return false;

        string currentSlot = FindSlot(jeans);
        var slot = equipment.GetSlot(LegSlot);
        if (slot == null || !slot.AddEquipmentItem(jeans.ItemDefinition, jeans)) return false;

        if (currentSlot != null)
            equipment.GetSlot(currentSlot)?.RemoveEquipmentItem(jeans.ItemDefinition);
        else
            source.RemoveEquipmentItem(jeans.ItemDefinition);

        EquipmentAttach.Carry(jeans, jeans.transform, bodyModel, HumanBodyBones.Hips, carrySlot, transform, wornPositionOffset, wornEulerOffset);
        return true;
    }

    // Moves the jeans from the Leg slot (or a hand, if PlayerLoot put them
    // there) back into a regular inventory slot. Prefers the main
    // inventory; if that's full, tries a hand instead; if hands are full
    // too, drops them into the world rather than Unequip silently doing
    // nothing.
    public bool Unequip(Jeans jeans)
    {
        string slotName = FindSlot(jeans);
        if (jeans == null || slotName == null) return false;

        if (playerInventory.Inventory.AddEquipmentItem(jeans.ItemDefinition, jeans))
        {
            equipment.GetSlot(slotName)?.RemoveEquipmentItem(jeans.ItemDefinition);
            jeans.Stash();
            return true;
        }

        foreach (var handSlotName in HandSlots)
        {
            var hand = equipment.GetSlot(handSlotName);
            if (hand == null || handSlotName == slotName) continue;

            if (hand.AddEquipmentItem(jeans.ItemDefinition, jeans))
            {
                equipment.GetSlot(slotName)?.RemoveEquipmentItem(jeans.ItemDefinition);
                jeans.SetCarried(true, transform);
                return true;
            }
        }

        Drop(jeans);
        return true;
    }

    // Drops the jeans into the world in front of the player, wherever they
    // currently are (Leg, a hand, or the regular inventory).
    public void Drop(Jeans jeans)
    {
        if (jeans == null) return;

        string slotName = FindSlot(jeans);
        if (slotName != null)
            equipment.GetSlot(slotName)?.RemoveEquipmentItem(jeans.ItemDefinition);
        else
            playerInventory.Inventory.RemoveEquipmentItem(jeans.ItemDefinition);

        jeans.SetCarried(false, null);
        jeans.transform.position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
        var despawn = jeans.gameObject.AddComponent<Despawn>();
        despawn.delay = despawnDelay;
    }

    // Searches Leg, then the hands, for the given jeans instance.
    private string FindSlot(Jeans jeans)
    {
        if (Equipped == jeans) return LegSlot;

        foreach (var slotName in HandSlots)
            if ((equipment.GetEquipped(slotName) as Jeans) == jeans)
                return slotName;

        return null;
    }
}
