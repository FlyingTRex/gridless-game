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
    private static readonly string[] HandSlots = { "Left Hand", "Right Hand" };

    [SerializeField] private ItemDefinition monitorItem;
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;

    private PlayerInventory playerInventory;
    private PlayerEquipment equipment;
    private PlayerVitals vitals;
    private PlayerLoot loot;

    public PersonalHealthMonitor Equipped =>
        (equipment.GetEquipped("Left Wrist") as PersonalHealthMonitor)
        ?? (equipment.GetEquipped("Right Wrist") as PersonalHealthMonitor);

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
        equipment = GetComponent<PlayerEquipment>();
        vitals = GetComponent<PlayerVitals>();
        loot = GetComponent<PlayerLoot>();
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

    // Moves the monitor onto a wrist slot from wherever it currently is (a
    // regular inventory slot, or a hand if PlayerLoot put it there).
    public bool Equip(PersonalHealthMonitor monitor)
    {
        if (monitor == null) return false;

        string currentSlot = FindSlot(monitor);

        foreach (var wristSlot in WristSlots)
        {
            var slot = equipment.GetSlot(wristSlot);
            if (slot == null || !slot.AddEquipmentItem(monitorItem, monitor)) continue;

            if (currentSlot != null)
                equipment.GetSlot(currentSlot)?.RemoveEquipmentItem(monitorItem);
            else
                playerInventory.Inventory.RemoveEquipmentItem(monitorItem);

            monitor.SetCarried(true, transform);
            return true;
        }

        return false;
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
