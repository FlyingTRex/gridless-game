using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerEquipment))]
[RequireComponent(typeof(PlayerVitals))]
public class PlayerHealthMonitor : MonoBehaviour
{
    // Tried in order when equipping.
    private static readonly string[] WristSlots = { "Left Wrist", "Right Wrist" };
    // Where PlayerLoot might have placed a picked-up monitor that hasn't
    // been (or can't be) worn — checked by Unequip/Drop/FindSlot so they
    // find it regardless of which of these it landed in.
    private static readonly string[] HandSlots = PlayerEquipSlots.Hands;

    [SerializeField] private ItemDefinition monitorItem;
    // Fallback only, used when PlayerBodyModel/the wrist bone isn't
    // available for some reason.
    [SerializeField] private Transform carrySlot;
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;
    // Matches Pickup.DespawnDelay — see Despawn.cs for why
    // PersonalHealthMonitor.Stash()/SetCarried(true, ...) cancel this on
    // pickup, not this script.
    [SerializeField] private float despawnDelay = 120f;

    // Root-relative worn offset (2026-08-13, same EquipmentAttach math as
    // Tool/Backpack) — shared by both wrists.
    [SerializeField] private Vector3 wornPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 wornEulerOffset = Vector3.zero;

    private PlayerInventory playerInventory;
    private PlayerEquipment equipment;
    private PlayerVitals vitals;
    private PlayerLoot loot;
    private PlayerBodyModel bodyModel;

    public PersonalHealthMonitor Equipped =>
        (equipment.GetEquipped("Left Wrist") as PersonalHealthMonitor)
        ?? (equipment.GetEquipped("Right Wrist") as PersonalHealthMonitor);

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
        equipment = GetComponent<PlayerEquipment>();
        vitals = GetComponent<PlayerVitals>();
        loot = GetComponent<PlayerLoot>();
        bodyModel = GetComponent<PlayerBodyModel>();
    }

    private static HumanBodyBones WristBone(string slotName) =>
        slotName == "Left Wrist" ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm;

    // Re-anchors the worn monitor onto the current wrist bone — called by
    // PlayerBodyModel after a gender switch.
    public void RefreshAnchor()
    {
        var current = Equipped;
        if (current == null) return;

        string slotName = FindSlot(current);
        if (slotName == null) return;

        EquipmentAttach.Carry(current, current.transform, bodyModel, WristBone(slotName), carrySlot, transform, wornPositionOffset, wornEulerOffset);
    }

    // Called when the player interacts with a monitor lying in the world.
    // Same priority as Backpack/Canteen/NavComputer pickups: an equipped
    // backpack's own storage, then a free hand, then stashed straight into
    // the main inventory as a last resort.
    public bool PickUp(PersonalHealthMonitor monitor)
    {
        if (monitor == null) return false;

        if (loot != null && loot.ReceiveEquipment(monitorItem, monitor))
            return true;

        if (!playerInventory.Inventory.AddEquipmentItem(monitorItem, monitor)) return false;

        monitor.Stash();
        return true;
    }

    // Every wrist slot currently free — read by InventoryScreen to decide
    // whether Equip can commit immediately (0 or 1 option) or needs to ask
    // the player which wrist they want (both free).
    public System.Collections.Generic.List<string> AvailableDestinations(PersonalHealthMonitor monitor)
    {
        var result = new System.Collections.Generic.List<string>();
        foreach (var wristSlot in WristSlots)
        {
            var slot = equipment.GetSlot(wristSlot);
            if (slot != null && slot.Slots.Count < slot.Capacity) result.Add(wristSlot);
        }
        return result;
    }

    // Moves the monitor onto a wrist slot from wherever it currently is
    // (see AvailableDestinations for the order).
    public bool Equip(PersonalHealthMonitor monitor)
    {
        var destinations = AvailableDestinations(monitor);
        return destinations.Count > 0 && EquipTo(monitor, destinations[0]);
    }

    // Moves the monitor onto a specific wrist the player chose (see
    // InventoryScreen's Equip destination popup) rather than picking one
    // automatically. FindSlot doesn't know about a monitor sitting inside a
    // backpack's nested Inventory, so it'd wrongly fall back to removing
    // from the main inventory in that case — same bug class fixed on
    // PlayerCanteen (2026-08-12). Use the source-aware overload below when
    // the caller already knows exactly where the monitor is.
    public bool EquipTo(PersonalHealthMonitor monitor, string destination) =>
        EquipTo(monitor, destination, playerInventory.Inventory);

    public bool EquipTo(PersonalHealthMonitor monitor, string destination, Inventory source)
    {
        if (monitor == null || destination == null || source == null) return false;

        string currentSlot = FindSlot(monitor);
        var slot = equipment.GetSlot(destination);
        if (slot == null || !slot.AddEquipmentItem(monitorItem, monitor)) return false;

        if (currentSlot != null)
            equipment.GetSlot(currentSlot)?.RemoveEquipmentItem(monitorItem);
        else
            source.RemoveEquipmentItem(monitorItem);

        EquipmentAttach.Carry(monitor, monitor.transform, bodyModel, WristBone(destination), carrySlot, transform, wornPositionOffset, wornEulerOffset);
        return true;
    }

    // Moves the monitor from a wrist back into a regular inventory slot.
    // Prefers the main inventory; if that's full, tries a hand instead; if
    // hands are full too, drops it into the world rather than Unequip
    // silently doing nothing.
    public bool Unequip(PersonalHealthMonitor monitor)
    {
        string slotName = FindSlot(monitor);
        if (monitor == null || slotName == null) return false;

        if (playerInventory.Inventory.AddEquipmentItem(monitorItem, monitor))
        {
            equipment.GetSlot(slotName)?.RemoveEquipmentItem(monitorItem);
            monitor.Stash();
            return true;
        }

        foreach (var handSlotName in HandSlots)
        {
            var hand = equipment.GetSlot(handSlotName);
            if (hand == null || handSlotName == slotName) continue;

            if (hand.AddEquipmentItem(monitorItem, monitor))
            {
                equipment.GetSlot(slotName)?.RemoveEquipmentItem(monitorItem);
                monitor.SetCarried(true, transform);
                return true;
            }
        }

        Drop(monitor);
        return true;
    }

    // Drops the monitor into the world in front of the player, wherever it
    // currently is (a wrist, a hand, or the regular inventory).
    public void Drop(PersonalHealthMonitor monitor)
    {
        if (monitor == null) return;

        string slotName = FindSlot(monitor);
        if (slotName != null)
            equipment.GetSlot(slotName)?.RemoveEquipmentItem(monitorItem);
        else
            playerInventory.Inventory.RemoveEquipmentItem(monitorItem);

        monitor.SetCarried(false, null);
        monitor.transform.position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
        var despawn = monitor.gameObject.AddComponent<Despawn>();
        despawn.delay = despawnDelay;
    }

    // Searches the wrists, then the hands, for the given monitor instance.
    private string FindSlot(PersonalHealthMonitor monitor)
    {
        foreach (var slotName in WristSlots)
            if ((equipment.GetEquipped(slotName) as PersonalHealthMonitor) == monitor)
                return slotName;

        foreach (var slotName in HandSlots)
            if ((equipment.GetEquipped(slotName) as PersonalHealthMonitor) == monitor)
                return slotName;

        return null;
    }

    // HUD: the same vitals readout PlayerVitals used to always draw in the
    // top-right corner, now gated behind actually wearing a monitor on a
    // wrist — picked up but unequipped (e.g. sitting in a hand) doesn't
    // activate it.
    private void OnGUI()
    {
        if (Equipped == null) return;

        var rect = new Rect(Screen.width - 230, 10, 220, 150);
        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Label("Vitals", DebugGUI.Header);
        GUILayout.Label($"Health: {vitals.Health:F0}", DebugGUI.Label);
        GUILayout.Label($"Hunger: {vitals.Hunger:F0}", DebugGUI.Label);
        GUILayout.Label($"Thirst: {vitals.Thirst:F0}", DebugGUI.Label);
        GUILayout.Label($"Stamina: {vitals.Stamina:F0}", DebugGUI.Label);
        GUILayout.Label($"Body Temp: {vitals.BodyTemperature:F0}", DebugGUI.Label);
        if (vitals.IsOverdrunkSick)
            GUILayout.Label("SICK: Overdrank water!", DebugGUI.Warning);
        GUILayout.EndArea();
    }
}
