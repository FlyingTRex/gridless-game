using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerEquipment))]
public class PlayerSunglasses : MonoBehaviour
{
    private const string FaceSlot = "Face";
    // Where PlayerLoot might have placed a picked-up pair that hasn't been
    // (or can't be) worn — checked by Unequip/Drop/FindSlot so they find
    // it regardless of which of these it landed in.
    private static readonly string[] HandSlots = { "Left Hand", "Right Hand" };

    [SerializeField] private ItemDefinition sunglassesItem;
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;
    // Matches Pickup.DespawnDelay — see Despawn.cs for why Sunglasses.
    // Stash()/SetCarried(true, ...) cancel this on pickup, not this script.
    [SerializeField] private float despawnDelay = 120f;

    private PlayerInventory playerInventory;
    private PlayerEquipment equipment;
    private PlayerLoot loot;

    private static Texture2D silverOverlay;

    // Face has capacity 2 (room for another accessory alongside these), so
    // — unlike a capacity-1 slot — PlayerEquipment.GetEquipped's "first
    // equipped item" isn't reliable here; search the slot's own entries
    // for this specific instance instead.
    public Sunglasses Equipped
    {
        get
        {
            var slot = equipment.GetSlot(FaceSlot);
            if (slot == null) return null;

            foreach (var s in slot.Slots)
                if (s.equipment is Sunglasses sunglasses) return sunglasses;

            return null;
        }
    }

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
        equipment = GetComponent<PlayerEquipment>();
        loot = GetComponent<PlayerLoot>();
    }

    // Called when the player interacts with sunglasses lying in the world.
    // Same priority as the other equippables: an equipped backpack's own
    // storage, then a free hand, then stashed straight into the main
    // inventory as a last resort.
    public bool PickUp(Sunglasses sunglasses)
    {
        if (sunglasses == null) return false;

        if (loot != null && loot.ReceiveEquipment(sunglassesItem, sunglasses))
            return true;

        if (!playerInventory.Inventory.AddEquipmentItem(sunglassesItem, sunglasses)) return false;

        sunglasses.Stash();
        return true;
    }

    // Moves the sunglasses onto the Face slot from wherever they currently
    // are (a regular inventory slot, or a hand if PlayerLoot put them
    // there).
    public bool Equip(Sunglasses sunglasses)
    {
        if (sunglasses == null) return false;

        string currentSlot = FindSlot(sunglasses);
        var slot = equipment.GetSlot(FaceSlot);
        if (slot == null || !slot.AddEquipmentItem(sunglassesItem, sunglasses)) return false;

        if (currentSlot != null)
            equipment.GetSlot(currentSlot)?.RemoveEquipmentItem(sunglassesItem);
        else
            playerInventory.Inventory.RemoveEquipmentItem(sunglassesItem);

        sunglasses.SetCarried(true, transform);
        return true;
    }

    // Moves the sunglasses from the Face slot (or a hand, if PlayerLoot
    // put them there) back into a regular inventory slot. Prefers the main
    // inventory; if that's full, tries a hand instead; if hands are full
    // too, drops them into the world rather than Unequip silently doing
    // nothing.
    public bool Unequip(Sunglasses sunglasses)
    {
        string slotName = FindSlot(sunglasses);
        if (sunglasses == null || slotName == null) return false;

        if (playerInventory.Inventory.AddEquipmentItem(sunglassesItem, sunglasses))
        {
            equipment.GetSlot(slotName)?.RemoveEquipmentItem(sunglassesItem);
            sunglasses.Stash();
            return true;
        }

        foreach (var handSlotName in HandSlots)
        {
            var hand = equipment.GetSlot(handSlotName);
            if (hand == null || handSlotName == slotName) continue;

            if (hand.AddEquipmentItem(sunglassesItem, sunglasses))
            {
                equipment.GetSlot(slotName)?.RemoveEquipmentItem(sunglassesItem);
                sunglasses.SetCarried(true, transform);
                return true;
            }
        }

        Drop(sunglasses);
        return true;
    }

    // Drops the sunglasses into the world in front of the player, wherever
    // they currently are (Face, a hand, or the regular inventory).
    public void Drop(Sunglasses sunglasses)
    {
        if (sunglasses == null) return;

        string slotName = FindSlot(sunglasses);
        if (slotName != null)
            equipment.GetSlot(slotName)?.RemoveEquipmentItem(sunglassesItem);
        else
            playerInventory.Inventory.RemoveEquipmentItem(sunglassesItem);

        sunglasses.SetCarried(false, null);
        sunglasses.transform.position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
        var despawn = sunglasses.gameObject.AddComponent<Despawn>();
        despawn.delay = despawnDelay;
    }

    // Searches Face, then the hands, for the given sunglasses instance.
    private string FindSlot(Sunglasses sunglasses)
    {
        if (Equipped == sunglasses) return FaceSlot;

        foreach (var slotName in HandSlots)
            if ((equipment.GetEquipped(slotName) as Sunglasses) == sunglasses)
                return slotName;

        return null;
    }

    // Light silver transparent filter across the whole screen while worn —
    // pure visual, no gameplay effect. Removed the instant Unequip runs
    // (Equipped goes null), since this is the only thing checking it.
    private void OnGUI()
    {
        if (Equipped == null) return;

        if (silverOverlay == null)
        {
            silverOverlay = new Texture2D(1, 1);
            silverOverlay.SetPixel(0, 0, new Color(0.75f, 0.78f, 0.82f, 0.25f));
            silverOverlay.Apply();
        }

        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), silverOverlay);
    }
}
