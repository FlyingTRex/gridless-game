using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerEquipment))]
public class PlayerBoot : MonoBehaviour
{
    private const string FeetSlot = "Feet";
    private static readonly string[] HandSlots = PlayerEquipSlots.Hands;

    // Fallback only, used when PlayerBodyModel/the Hips bone isn't
    // available for some reason — the scene's pre-existing fixed anchor
    // (not bone-parented, doesn't follow animation).
    [SerializeField] private Transform carrySlot;
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;
    // Matches Pickup.DespawnDelay — see Despawn.cs for why the Boot itself
    // (Stash()) is what cancels this on pickup, not this script.
    [SerializeField] private float despawnDelay = 120f;

    // Root-relative worn offset (2026-08-13, same EquipmentAttach math as
    // Tool/Backpack). No per-foot bone — Boot is a single combined-pair
    // mesh, not two separate meshes, so it attaches to Hips (roughly
    // central, doesn't swing as wildly as a single foot bone during a
    // walk cycle) with a downward push toward ankle height. First-pass
    // guess, same as everything else in this pass — a static single
    // anchor can't perfectly track two independently animating feet
    // either way; splitting into two per-foot meshes would be a bigger,
    // separate change if this doesn't look acceptable live.
    // Live-feedback round 3 (Ben): position confirmed correct (sits right
    // at the feet), but asked to try rotating too — the model may be
    // front-to-back reversed relative to the character's own facing.
    // Trying a 180 deg yaw flip as the first attempt.
    //
    // Reverted (2026-08-13, live feedback screenshot): the 180 yaw flip
    // made it look worse — jumbled/overlapping instead of clean.
    //
    // Corrected properly (2026-08-13, Ben: "shoes should be parallel with
    // the feet... not perpendicular", with a reference photo): this was
    // never a yaw (front-to-back) problem — wrong axis entirely. It's the
    // same ground-lying-vs-mounted pitch mismatch already found and fixed
    // on the Backpack and Jeans. worldPickupPrefab is authored to lie
    // flat on the ground for display; with zero rotation correction the
    // shoe stands on its end (toe pointing up, perpendicular to the
    // ground) instead of lying flat with the toe pointing forward
    // (parallel). Same -90 X pitch correction as Backpack/Jeans.
    //
    // Live-tweaked by Ben directly in the Play-mode Inspector (2026-08-13)
    // rather than another guess-and-screenshot round — the blind-pitch
    // theory above turned out not to match what actually looked right.
    // Trusting the live-tested result over the theory: yaw (Y), not pitch
    // (X), is what actually corrected it. "This looks closer," not yet
    // confirmed final.
    [SerializeField] private Vector3 wornPositionOffset = new Vector3(0f, -0.93f, 0.35f);
    [SerializeField] private Vector3 wornEulerOffset = new Vector3(0f, 90f, 0f);

    // The player starts the game already wearing Settler's Sneakers
    // specifically — same single-purpose starting-gear mechanism
    // PlayerShirt/PlayerJeans/PlayerBelt already established (2026-08-12).
    // Real gap found live: Boots was the one starting-gear slot that never
    // got this when Sneakers was added as a plain (non-auto-equipping)
    // Boots variant — fixed by giving it a dedicated "Settler's Sneakers"
    // item/prefab, same split Jeans already has (auto-equip variant +
    // separate plain variant, not a rename of the existing item).
    [SerializeField] private GameObject startingBootPrefab;

    private PlayerInventory playerInventory;
    private PlayerEquipment equipment;
    private PlayerLoot loot;
    private PlayerBodyModel bodyModel;

    public Boot Equipped => equipment.GetEquipped(FeetSlot) as Boot;

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
        equipment = GetComponent<PlayerEquipment>();
        loot = GetComponent<PlayerLoot>();
        bodyModel = GetComponent<PlayerBodyModel>();
    }

    // Re-anchors the worn boots onto the current Hips bone — called by
    // PlayerBodyModel after a gender switch.
    public void RefreshAnchor()
    {
        if (Equipped == null) return;
        EquipmentAttach.Carry(Equipped, Equipped.transform, bodyModel, HumanBodyBones.Hips, carrySlot, transform, wornPositionOffset, wornEulerOffset);
    }

    // Start (not Awake) so every other component's Awake — including
    // PlayerEquipment building its slot dictionary — has already run.
    // Equipped != null guards against ever equipping a second pair.
    private void Start()
    {
        if (startingBootPrefab == null || Equipped != null) return;

        var instance = Instantiate(startingBootPrefab);
        NetworkSpawnHelper.SpawnIfNetworked(instance);
        var boot = instance.GetComponent<Boot>();
        var slot = equipment.GetSlot(FeetSlot);

        if (boot != null && slot != null && slot.AddEquipmentItem(boot.ItemDefinition, boot))
            EquipmentAttach.Carry(boot, boot.transform, bodyModel, HumanBodyBones.Hips, carrySlot, transform, wornPositionOffset, wornEulerOffset);
        else
            Destroy(instance);
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

        EquipmentAttach.Carry(boot, boot.transform, bodyModel, HumanBodyBones.Hips, carrySlot, transform, wornPositionOffset, wornEulerOffset);
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
