using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerEquipment))]
[RequireComponent(typeof(CharacterController))]
public class PlayerNavComputer : MonoBehaviour
{
    // Tried in order when equipping.
    private static readonly string[] WristSlots = { "Left Wrist", "Right Wrist" };
    // Where PlayerLoot might have placed a picked-up computer that hasn't
    // been (or can't be) worn — checked by Unequip/Drop/FindSlot so they
    // find it regardless of which of these it landed in.
    private static readonly string[] HandSlots = { "Left Hand", "Right Hand" };

    [SerializeField] private ItemDefinition navComputerItem;
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;
    // Matches Pickup.DespawnDelay — see Despawn.cs for why NavigationComputer.
    // Stash()/SetCarried(true, ...) cancel this on pickup, not this script.
    [SerializeField] private float despawnDelay = 120f;

    private PlayerInventory playerInventory;
    private PlayerEquipment equipment;
    private PlayerLoot loot;
    private CharacterController controller;

    public NavigationComputer Equipped =>
        (equipment.GetEquipped("Left Wrist") as NavigationComputer)
        ?? (equipment.GetEquipped("Right Wrist") as NavigationComputer);

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
        equipment = GetComponent<PlayerEquipment>();
        loot = GetComponent<PlayerLoot>();
        controller = GetComponent<CharacterController>();
    }

    // Called when the player interacts with a computer lying in the world.
    // Same priority as Backpack/Canteen pickups: an equipped backpack's own
    // storage, then a free hand, then stashed straight into the main
    // inventory as a last resort.
    public bool PickUp(NavigationComputer navComputer)
    {
        if (navComputer == null) return false;

        if (loot != null && loot.ReceiveEquipment(navComputerItem, navComputer))
            return true;

        if (!playerInventory.Inventory.AddEquipmentItem(navComputerItem, navComputer)) return false;

        navComputer.Stash();
        return true;
    }

    // Every wrist slot currently free — read by InventoryScreen to decide
    // whether Equip can commit immediately (0 or 1 option) or needs to ask
    // the player which wrist they want (both free).
    public System.Collections.Generic.List<string> AvailableDestinations(NavigationComputer navComputer)
    {
        var result = new System.Collections.Generic.List<string>();
        foreach (var wristSlot in WristSlots)
        {
            var slot = equipment.GetSlot(wristSlot);
            if (slot != null && slot.Slots.Count < slot.Capacity) result.Add(wristSlot);
        }
        return result;
    }

    // Moves the computer onto a wrist slot from wherever it currently is
    // (see AvailableDestinations for the order).
    public bool Equip(NavigationComputer navComputer)
    {
        var destinations = AvailableDestinations(navComputer);
        return destinations.Count > 0 && EquipTo(navComputer, destinations[0]);
    }

    // Moves the computer onto a specific wrist the player chose (see
    // InventoryScreen's Equip destination popup) rather than picking one
    // automatically. FindSlot doesn't know about a computer sitting inside
    // a backpack's nested Inventory, so it'd wrongly fall back to removing
    // from the main inventory in that case — same bug class fixed on
    // PlayerCanteen (2026-08-12). Use the source-aware overload below when
    // the caller already knows exactly where the computer is.
    public bool EquipTo(NavigationComputer navComputer, string destination) =>
        EquipTo(navComputer, destination, playerInventory.Inventory);

    public bool EquipTo(NavigationComputer navComputer, string destination, Inventory source)
    {
        if (navComputer == null || destination == null || source == null) return false;

        string currentSlot = FindSlot(navComputer);
        var slot = equipment.GetSlot(destination);
        if (slot == null || !slot.AddEquipmentItem(navComputerItem, navComputer)) return false;

        if (currentSlot != null)
            equipment.GetSlot(currentSlot)?.RemoveEquipmentItem(navComputerItem);
        else
            source.RemoveEquipmentItem(navComputerItem);

        navComputer.SetCarried(true, transform);
        return true;
    }

    // Moves the computer from a wrist back into a regular inventory slot.
    // Prefers the main inventory; if that's full, tries a hand instead; if
    // hands are full too, drops it into the world rather than Unequip
    // silently doing nothing.
    public bool Unequip(NavigationComputer navComputer)
    {
        string slotName = FindSlot(navComputer);
        if (navComputer == null || slotName == null) return false;

        if (playerInventory.Inventory.AddEquipmentItem(navComputerItem, navComputer))
        {
            equipment.GetSlot(slotName)?.RemoveEquipmentItem(navComputerItem);
            navComputer.Stash();
            return true;
        }

        foreach (var handSlotName in HandSlots)
        {
            var hand = equipment.GetSlot(handSlotName);
            if (hand == null || handSlotName == slotName) continue;

            if (hand.AddEquipmentItem(navComputerItem, navComputer))
            {
                equipment.GetSlot(slotName)?.RemoveEquipmentItem(navComputerItem);
                navComputer.SetCarried(true, transform);
                return true;
            }
        }

        Drop(navComputer);
        return true;
    }

    // Drops the computer into the world in front of the player, wherever
    // it currently is (a wrist, a hand, or the regular inventory).
    public void Drop(NavigationComputer navComputer)
    {
        if (navComputer == null) return;

        string slotName = FindSlot(navComputer);
        if (slotName != null)
            equipment.GetSlot(slotName)?.RemoveEquipmentItem(navComputerItem);
        else
            playerInventory.Inventory.RemoveEquipmentItem(navComputerItem);

        navComputer.SetCarried(false, null);
        navComputer.transform.position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
        var despawn = navComputer.gameObject.AddComponent<Despawn>();
        despawn.delay = despawnDelay;
    }

    // Searches the wrists, then the hands, for the given computer instance.
    private string FindSlot(NavigationComputer navComputer)
    {
        foreach (var slotName in WristSlots)
            if ((equipment.GetEquipped(slotName) as NavigationComputer) == navComputer)
                return slotName;

        foreach (var slotName in HandSlots)
            if ((equipment.GetEquipped(slotName) as NavigationComputer) == navComputer)
                return slotName;

        return null;
    }

    // HUD: a compass across the top-center of the screen that turns with
    // the player's facing, plus current horizontal travel speed beneath
    // it. Only drawn while a computer is actually worn on a wrist — picked
    // up but unequipped (e.g. sitting in a hand) doesn't activate it.
    private void OnGUI()
    {
        if (Equipped == null) return;

        const float width = 320f;
        const float compassHeight = 26f;
        const float speedHeight = 22f;
        const float spacing = 4f;
        var rect = new Rect((Screen.width - width) / 2f, 10f, width, compassHeight + spacing + speedHeight);

        DebugGUI.DrawPanel(rect);
        DrawCompass(new Rect(rect.x, rect.y, width, compassHeight));

        var speedRect = new Rect(rect.x, rect.y + compassHeight + spacing, width, speedHeight);
        GUI.Label(speedRect, $"{HorizontalSpeed():F1} m/s", DebugGUI.Header);
    }

    private static readonly string[] CompassLabels = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

    // Classic FPS-style scrolling compass strip: each cardinal label's
    // horizontal position is its angular offset from the player's current
    // heading, mapped across a 180-degree field of view, so it slides
    // past as the player turns and the ones behind them simply don't draw.
    private void DrawCompass(Rect rect)
    {
        GUI.BeginGroup(rect);

        float heading = transform.eulerAngles.y;
        float pixelsPerDegree = rect.width / 180f;

        for (int i = 0; i < CompassLabels.Length; i++)
        {
            float diff = Mathf.DeltaAngle(heading, i * 45f);
            if (Mathf.Abs(diff) > 90f) continue;

            float x = rect.width / 2f + diff * pixelsPerDegree;
            GUI.Label(new Rect(x - 20f, 0f, 40f, rect.height), CompassLabels[i], DebugGUI.Header);
        }

        GUI.DrawTexture(new Rect(rect.width / 2f - 1f, 0f, 2f, rect.height), Texture2D.whiteTexture);
        GUI.EndGroup();
    }

    private float HorizontalSpeed()
    {
        Vector3 v = controller.velocity;
        v.y = 0f;
        return v.magnitude;
    }
}
