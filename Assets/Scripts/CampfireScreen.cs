using UnityEngine;

// Opened by interacting (E) with a specific Campfire — small focused
// popup (same family as LockboxScreen) showing that Campfire's fuel and
// cooking slots plus a Light button, replacing the old buried "Campfire
// (nearby)" section that used to sit at the bottom of the Inventory tab's
// scroll view (2026-08-13 UI redesign, see CAMPFIRE_PLANNING.md — a real
// live-testing discoverability failure: Ben's report was "there's no
// mechanism to transfer fuel" even though the mechanism was technically
// present and functional, just an unlabeled row on an already-busy
// screen).
//
// Deliberately simple/button-based (Add 1 / Take, no drag-and-drop) —
// only pulls from the player's own main Inventory (PlayerInventory), not
// backpack/worn-container contents, mirroring LockboxScreen/BankScreen's
// equally narrow scope (wallet only, no backpack currency). A future
// pass could widen this if that turns out to matter in practice.
[RequireComponent(typeof(PlayerInventory))]
public class CampfireScreen : MonoBehaviour
{
    private const float PanelWidth = 420f;
    private const float PanelHeight = 420f;

    private PlayerInventory playerInventory;
    private Campfire current;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
    }

    // Called by Campfire.Complete. Only opens from normal gameplay — same
    // rule every other screen follows, so it can't stack on top of one
    // that already has the cursor unlocked.
    public void Open(Campfire campfire)
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        current = campfire;
        SetOpen(true);
    }

    // Called by FirstPersonController when Escape re-locks the cursor, so
    // the two toggles can't drift out of sync with each other.
    public void Close() => SetOpen(false);

    private void SetOpen(bool value)
    {
        isOpen = value;
        if (!value) current = null;
        Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = value;
    }

    private void OnGUI()
    {
        if (!isOpen || current == null) return;

        var rect = new Rect((Screen.width - PanelWidth) / 2f, (Screen.height - PanelHeight) / 2f, PanelWidth, PanelHeight);
        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);

        GUILayout.Label(current.DisplayName, DebugGUI.Header);
        GUILayout.Label(current.IsLit
            ? $"Lit — {Mathf.CeilToInt(current.FuelSecondsRemaining)}s of fuel left"
            : "Unlit", DebugGUI.Label);

        GUILayout.Space(10);
        DrawFuelSection();

        GUILayout.Space(10);
        DrawCookingSection();

        GUILayout.FlexibleSpace();

        GUI.enabled = !current.IsLit && current.HasFuel;
        if (GUILayout.Button("Light"))
            current.TryLightFromScreen();
        GUI.enabled = true;

        GUILayout.Space(5);
        if (GUILayout.Button("Close"))
            SetOpen(false);

        GUILayout.EndArea();
    }

    private void DrawFuelSection()
    {
        GUILayout.Label("Fuel", DebugGUI.Header);

        var slots = current.FuelInventory.Slots;
        if (slots.Count > 0)
        {
            var slot = slots[0];
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{slot.item.itemName} x{slot.count}", DebugGUI.Label);
            if (GUILayout.Button("Take", GUILayout.Width(70)))
                TakeAll(current.FuelInventory, slot.item);
            GUILayout.EndHorizontal();
            return;
        }

        var fuelItems = current.FuelItems;
        if (fuelItems == null) return;

        foreach (var fuel in fuelItems)
        {
            if (fuel == null || fuel.item == null) continue;
            int have = playerInventory.GetCount(fuel.item);
            if (have <= 0) continue;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{fuel.item.itemName} (have {have})", DebugGUI.Label);
            if (GUILayout.Button("Add 1", GUILayout.Width(70)))
                AddOne(current.FuelInventory, fuel.item);
            GUILayout.EndHorizontal();
        }
    }

    private void DrawCookingSection()
    {
        GUILayout.Label("Cooking", DebugGUI.Header);

        var slots = current.CookingInventory.Slots;
        if (slots.Count > 0)
        {
            var slot = slots[0];
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{slot.item.itemName} x{slot.count}", DebugGUI.Label);
            if (GUILayout.Button("Take", GUILayout.Width(70)))
                TakeAll(current.CookingInventory, slot.item);
            GUILayout.EndHorizontal();
            return;
        }

        var cookables = current.CookableItems;
        if (cookables == null) return;

        foreach (var recipe in cookables)
        {
            if (recipe == null || recipe.rawItem == null) continue;
            int have = playerInventory.GetCount(recipe.rawItem);
            if (have <= 0) continue;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{recipe.rawItem.itemName} (have {have})", DebugGUI.Label);
            if (GUILayout.Button("Add 1", GUILayout.Width(70)))
                AddOne(current.CookingInventory, recipe.rawItem);
            GUILayout.EndHorizontal();
        }
    }

    private void AddOne(Inventory destination, ItemDefinition item)
    {
        if (destination.AddItem(item, 1) != 0) return; // didn't fit, nothing to undo
        playerInventory.RemoveItem(item, 1);
    }

    private void TakeAll(Inventory source, ItemDefinition item)
    {
        int count = source.GetCount(item);
        if (count <= 0) return;

        int leftover = playerInventory.AddItem(item, count);
        int actuallyMoved = count - leftover;
        if (actuallyMoved > 0)
            source.RemoveItem(item, actuallyMoved);
    }
}
