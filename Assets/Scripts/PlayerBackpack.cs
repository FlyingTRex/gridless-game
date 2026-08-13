using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerEquipment))]
public class PlayerBackpack : MonoBehaviour
{
    private const string BackSlot = "Back";
    // Where PlayerLoot might have placed a picked-up backpack that hasn't
    // been (or can't be) worn — checked by Unequip/Drop so they find it
    // regardless of which of these it landed in.
    private static readonly string[] HandSlots = { "Left Hand", "Right Hand" };

    // Fallback only, used when PlayerBodyModel/the Chest bone isn't
    // available for some reason — the scene's pre-existing fixed
    // BackpackAnchor child (not bone-parented, doesn't follow animation).
    [SerializeField] private Transform carrySlot;
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;
    // Matches Pickup.DespawnDelay — see Despawn.cs for why the Backpack
    // itself (Stash()) is what cancels this on pickup, not this script.
    [SerializeField] private float despawnDelay = 120f;

    // Root-relative worn offset (2026-08-13, same EquipmentAttach math
    // NPCEquipmentVisual uses). Live-feedback round 2 (Ben, screenshot):
    // sat far too high — near the neck/shoulders, not the back — with
    // the original zero Y offset. Root cause: HumanBodyBones.Chest sits
    // quite high on a Humanoid rig (near the collarbone), not mid-back,
    // so a pure backward push with no downward correction left it
    // hovering above the shoulders. Added a real downward push (-0.3) to
    // bring it down onto the actual back. Still an unconfirmed guess —
    // same numbers mirrored onto NPCJobDefinition's Backpack requirements
    // for consistency between player and NPC.
    //
    // Live-feedback round 3 (Ben, two screenshots): the Y correction
    // above worked for the bag body (now correctly sitting on the back),
    // but the same rigid model has a rolled bedroll-style extension at
    // its top that now juts up past the head. Theory: worldPickupPrefab
    // is authored to look correct *lying flat on the ground* (a dropped
    // pickup) — the 180 deg yaw only corrects facing direction, not the
    // pitch needed to stand a ground-lying prop upright against a back.
    // Trying a -90 deg X (pitch) correction on top of the existing yaw —
    // first attempt at this axis, could easily need +90 instead if this
    // over/under-rotates it the wrong way.
    //
    // Reverted (2026-08-13, live feedback: "not aligned at all," Ben
    // supplied a reference photo). Root cause of the round-3 theory above:
    // it was wrong. The floating shape misdiagnosed as "part of the
    // Backpack model" turned out to be the Jeans (fixed separately in
    // PlayerJeans.cs), not anything belonging to this object. This -90 X
    // pitch was bolted onto an already-correct Backpack based on that
    // wrong diagnosis and never undone once the real culprit was found —
    // reverting it back to the last confirmed-working rotation (yaw only).
    //
    // Round 8 (Ben, direct instruction): "backpack should be rotated on
    // the vertical axis 90 degrees" — adding that 90° yaw on top of the
    // existing 180°.
    [SerializeField] private Vector3 wornPositionOffset = new Vector3(0f, -0.3f, -0.2f);
    [SerializeField] private Vector3 wornEulerOffset = new Vector3(0f, -90f, 0f);

    private PlayerInventory playerInventory;
    private PlayerEquipment equipment;
    private PlayerLoot loot;
    private PlayerBodyModel bodyModel;

    public Backpack Equipped => equipment.GetEquipped(BackSlot) as Backpack;

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
        equipment = GetComponent<PlayerEquipment>();
        loot = GetComponent<PlayerLoot>();
        bodyModel = GetComponent<PlayerBodyModel>();
    }

    // Re-anchors the worn backpack onto the current Chest bone — called by
    // PlayerBodyModel after a gender switch (it was parented under the
    // *previous* gender's now-inactive model).
    public void RefreshAnchor()
    {
        if (Equipped == null) return;

        EquipmentAttach.Carry(Equipped, Equipped.transform, bodyModel, HumanBodyBones.Chest, carrySlot, transform, wornPositionOffset, wornEulerOffset);
    }

    // Called when the player interacts with a backpack lying in the world.
    // Routes through PlayerLoot first (equipped backpack's own contents,
    // then a free hand) — falls back to stashing as a regular (hidden)
    // inventory item only if PlayerLoot found nowhere else for it.
    public bool PickUp(Backpack backpack)
    {
        if (backpack == null) return false;

        if (loot != null && loot.ReceiveEquipment(backpack.ItemDefinition, backpack))
            return true;

        if (!playerInventory.Inventory.AddEquipmentItem(backpack.ItemDefinition, backpack)) return false;

        backpack.Stash();
        return true;
    }

    // Moves the backpack onto the Back slot from wherever it currently is
    // (a regular inventory slot, or a hand if PlayerLoot put it there on
    // pickup). FindSlot only ever checks the main inventory / body slots /
    // hands — it doesn't know about a backpack sitting inside some other
    // container's nested Inventory (e.g. one worn backpack holding another
    // in its cargo), so it silently falls back to removing from the main
    // inventory in that case, the same bug class fixed on PlayerCanteen
    // (2026-08-12). Use the source-aware overload below when the caller
    // already knows exactly where the backpack is.
    public bool Equip(Backpack backpack) => Equip(backpack, playerInventory.Inventory);

    // Same as above, but removes from whichever Inventory the backpack is
    // actually sitting in, rather than guessing via FindSlot/main inventory.
    public bool Equip(Backpack backpack, Inventory source)
    {
        if (backpack == null || source == null) return false;

        string currentSlot = FindSlot(backpack);
        var slot = equipment.GetSlot(BackSlot);
        if (slot == null || !slot.AddEquipmentItem(backpack.ItemDefinition, backpack)) return false;

        if (currentSlot != null)
            equipment.GetSlot(currentSlot)?.RemoveEquipmentItem(backpack.ItemDefinition);
        else
            source.RemoveEquipmentItem(backpack.ItemDefinition);

        EquipmentAttach.Carry(backpack, backpack.transform, bodyModel, HumanBodyBones.Chest, carrySlot, transform, wornPositionOffset, wornEulerOffset);
        return true;
    }

    // Moves the backpack from the Back slot (or a hand, if PlayerLoot put
    // it there) back into a regular inventory slot. Prefers the main
    // inventory; if that's full, tries a hand instead; if hands are full
    // too, drops it into the world rather than Unequip silently doing
    // nothing.
    public bool Unequip(Backpack backpack)
    {
        string slotName = FindSlot(backpack);
        if (backpack == null || slotName == null) return false;

        if (playerInventory.Inventory.AddEquipmentItem(backpack.ItemDefinition, backpack))
        {
            equipment.GetSlot(slotName)?.RemoveEquipmentItem(backpack.ItemDefinition);
            backpack.Stash();
            return true;
        }

        foreach (var handSlotName in HandSlots)
        {
            var hand = equipment.GetSlot(handSlotName);
            if (hand == null || handSlotName == slotName) continue;

            if (hand.AddEquipmentItem(backpack.ItemDefinition, backpack))
            {
                equipment.GetSlot(slotName)?.RemoveEquipmentItem(backpack.ItemDefinition);
                backpack.SetCarried(true, transform);
                return true;
            }
        }

        Drop(backpack);
        return true;
    }

    // Drops the backpack into the world in front of the player, wherever
    // it currently is (Back, a hand, or the regular inventory).
    public void Drop(Backpack backpack)
    {
        if (backpack == null) return;

        string slotName = FindSlot(backpack);
        if (slotName != null)
            equipment.GetSlot(slotName)?.RemoveEquipmentItem(backpack.ItemDefinition);
        else
            playerInventory.Inventory.RemoveEquipmentItem(backpack.ItemDefinition);

        backpack.SetCarried(false, null);
        backpack.transform.position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
        var despawn = backpack.gameObject.AddComponent<Despawn>();
        despawn.delay = despawnDelay;
    }

    // Searches Back, then the hands, for the given backpack instance.
    private string FindSlot(Backpack backpack)
    {
        if (Equipped == backpack) return BackSlot;

        foreach (var slotName in HandSlots)
            if ((equipment.GetEquipped(slotName) as Backpack) == backpack)
                return slotName;

        return null;
    }
}
