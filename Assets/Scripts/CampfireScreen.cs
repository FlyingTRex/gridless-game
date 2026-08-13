using System.Collections.Generic;
using UnityEngine;

// Opened by interacting (E) with a specific Campfire — focused popup
// (same family as LockboxScreen) showing that Campfire's fuel, cooking
// utensil, ingredient, and output slots, a recipe picker, and a Light
// button. Replaced the old buried "Campfire (nearby)" Inventory-tab
// section (2026-08-13, see CAMPFIRE_PLANNING.md), then upgraded the same
// day from simple Add-1/Take buttons to real drag-and-drop once the
// cooking system grew utensil slots + multi-ingredient recipes.
//
// Drag-and-drop here is a self-contained implementation, not a literal
// extraction from InventoryScreen.cs — mirrors that screen's exact
// interaction model (press-hold-release with a distance threshold, a
// cursor-following ghost, a highlighted drop zone) but doesn't touch
// InventoryScreen's own battle-tested code, which is deeply intertwined
// with 11 different IEquippable carrier types this screen has no need
// for (every box here is a plain, unequippable Inventory — Fuel/
// Utensils/Ingredients/Output are the Campfire's own restricted slots,
// and Backpack/Hands are the only transfer sources, per Ben's explicit
// scope: "shows only the backpack and stuff in hands").
[RequireComponent(typeof(PlayerEquipment))]
public class CampfireScreen : MonoBehaviour
{
    // Responsive to the actual window size (2026-08-13, Ben's report: a
    // fixed 640-tall panel didn't fit his screen, and he had no way to
    // scroll it — a touchpad with no working scroll gesture in this
    // window). Caps at a sane max on a big monitor, shrinks to fit
    // (almost) any smaller one instead of relying on internal scrolling
    // to cover the gap. Same pattern PlayerMenuScreen.DrawScrollable
    // already uses (Screen.height minus a fixed chrome reserve).
    private const float MaxPanelWidth = 520f;
    private const float MaxPanelHeight = 640f;
    private const float ChromeReserve = 170f; // header + status + Light/Close buttons + spacing
    private static float PanelWidth => Mathf.Min(MaxPanelWidth, Screen.width * 0.92f);
    private static float PanelHeight => Mathf.Min(MaxPanelHeight, Screen.height * 0.92f);
    private static float ScrollHeight => Mathf.Max(120f, PanelHeight - ChromeReserve);
    private const float BoxSize = 56f;
    private const float BoxGap = 8f;
    private const float DragThreshold = 12f;
    private const int GridColumns = 4;

    private PlayerEquipment equipment;
    private Campfire current;
    private bool isOpen;
    private Vector2 scrollPos;

    private bool dragCandidate;
    private bool isDragging;
    private Vector2 dragStartMousePos;
    private Inventory dragSource;
    private ItemDefinition dragItem;
    private int dragQuantity;

    private struct DropZone
    {
        public Rect Rect;
        public Inventory Inventory;
    }
    private readonly List<DropZone> dropZones = new List<DropZone>();

    public bool IsOpen => isOpen;

    private void Awake()
    {
        equipment = GetComponent<PlayerEquipment>();
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
        if (!value)
        {
            current = null;
            dragCandidate = false;
            isDragging = false;
        }
        Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = value;
    }

    private void OnGUI()
    {
        if (!isOpen || current == null) return;

        dropZones.Clear();

        var rect = new Rect((Screen.width - PanelWidth) / 2f, (Screen.height - PanelHeight) / 2f, PanelWidth, PanelHeight);
        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);

        GUILayout.Label(current.DisplayName, DebugGUI.Header);
        GUILayout.Label(current.IsLit
            ? $"Lit — {Mathf.CeilToInt(current.FuelSecondsRemaining)}s of fuel left"
            : "Unlit", DebugGUI.Label);

        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(ScrollHeight));

        GUILayout.Label("Cooking Utensils", DebugGUI.Header);
        GUILayout.BeginHorizontal();
        DrawLabeledSingleBox("Grill", current.GrillSlot);
        GUILayout.Space(BoxGap);
        DrawLabeledSingleBox("Cooking Pot", current.CookingPotSlot);
        GUILayout.Space(BoxGap);
        DrawLabeledSingleBox("Kettle", current.KettleSlot);
        GUILayout.Space(BoxGap);
        DrawLabeledSingleBox("Frying Pan", current.FryingPanSlot);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("Cooked Items", DebugGUI.Header);
        DrawGrid(current.OutputInventory, GridColumns, dragSourceOnly: true);

        GUILayout.Space(10);
        DrawRecipeSection();

        GUILayout.Space(10);
        DrawTransferSection();

        GUILayout.Space(10);
        GUILayout.Label("Ingredients", DebugGUI.Header);
        DrawGrid(current.InputInventory, GridColumns, dragSourceOnly: false);

        GUILayout.Space(10);
        GUILayout.Label("Fuel (wood)", DebugGUI.Header);
        DrawSingleBox(current.FuelInventory);

        GUILayout.EndScrollView();

        GUILayout.Space(5);
        GUI.enabled = !current.IsLit && current.HasFuel;
        if (GUILayout.Button("Light"))
            current.TryLightFromScreen();
        GUI.enabled = true;

        GUILayout.Space(5);
        if (GUILayout.Button("Close"))
            SetOpen(false);

        GUILayout.EndArea();

        // Absolute screen space, drawn after EndArea — same reasoning as
        // InventoryScreen.DrawPopups().
        HandleGlobalDragRelease();
        DrawDropZoneHighlight();
        DrawDragGhost();
    }

    // Only the recipes currently satisfiable (accessory seated + every
    // ingredient present) show up as buttons — clicking one commits
    // immediately (Ben's call: manual trigger, not the old auto-cook).
    private void DrawRecipeSection()
    {
        GUILayout.Label("Recipe", DebugGUI.Header);

        if (current.ActiveRecipe != null)
        {
            float progress = Mathf.Clamp01(current.CookSecondsElapsed / current.ActiveRecipe.cookDurationSeconds);
            GUILayout.Label($"Cooking {current.ActiveRecipe.outputItem.itemName} — {Mathf.RoundToInt(progress * 100f)}%", DebugGUI.Label);
            return;
        }

        var available = current.GetAvailableRecipes();
        if (available.Count == 0)
        {
            GUILayout.Label("No recipes available with the current utensils/ingredients.", DebugGUI.Label);
            return;
        }

        foreach (var recipe in available)
        {
            if (GUILayout.Button($"Cook {recipe.outputItem.itemName} x{recipe.outputCount}"))
                current.StartCooking(recipe);
        }
    }

    private void DrawTransferSection()
    {
        GUILayout.Label("Transfer (Backpack / Hands)", DebugGUI.Header);

        var backpackInventory = GetBackpackInventory();
        if (backpackInventory != null)
            DrawGrid(backpackInventory, GridColumns, dragSourceOnly: false);
        else
            GUILayout.Label("No backpack worn.", DebugGUI.Label);

        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        DrawLabeledSingleBox("Left Hand", equipment.GetSlot("Left Hand"));
        GUILayout.Space(BoxGap);
        DrawLabeledSingleBox("Right Hand", equipment.GetSlot("Right Hand"));
        GUILayout.EndHorizontal();
    }

    private Inventory GetBackpackInventory()
    {
        var slot = equipment.GetSlot("Back");
        if (slot == null || slot.Slots.Count == 0) return null;
        return (slot.Slots[0].equipment as IInventoryHolder)?.Inventory;
    }

    private void DrawGrid(Inventory inventory, int columns, bool dragSourceOnly)
    {
        if (inventory == null) return;

        var slots = inventory.Slots;
        int capacity = inventory.Capacity;
        int drawn = 0;

        while (drawn < capacity)
        {
            GUILayout.BeginHorizontal();
            for (int col = 0; col < columns && drawn < capacity; col++, drawn++)
            {
                if (col > 0) GUILayout.Space(BoxGap);
                DrawBox(inventory, drawn < slots.Count ? slots[drawn] : null, dragSourceOnly);
            }
            GUILayout.EndHorizontal();

            if (drawn < capacity) GUILayout.Space(BoxGap);
        }
    }

    // A single capacity-1 box with no label row above it (Fuel).
    private void DrawSingleBox(Inventory inventory)
    {
        if (inventory == null) return;
        var slots = inventory.Slots;
        DrawBox(inventory, slots.Count > 0 ? slots[0] : null, dragSourceOnly: false);
    }

    // A single capacity-1 box with a name label above it (each Utensil,
    // Left/Right Hand).
    private void DrawLabeledSingleBox(string label, Inventory inventory)
    {
        GUILayout.BeginVertical(GUILayout.Width(BoxSize));
        GUILayout.Label(label, DebugGUI.Label, GUILayout.Width(BoxSize));
        if (inventory == null)
            GUILayout.Box(GUIContent.none, DebugGUI.Slot, GUILayout.Width(BoxSize), GUILayout.Height(BoxSize));
        else
            DrawBox(inventory, inventory.Slots.Count > 0 ? inventory.Slots[0] : null, dragSourceOnly: false);
        GUILayout.EndVertical();
    }

    // dragSourceOnly = true means the box can be picked up from (Output)
    // but never registers as a drop target — the cook mechanic is the
    // only thing allowed to populate it.
    private void DrawBox(Inventory inventory, Inventory.Slot slot, bool dragSourceOnly)
    {
        if (slot == null)
        {
            GUILayout.Box(GUIContent.none, DebugGUI.Slot, GUILayout.Width(BoxSize), GUILayout.Height(BoxSize));
            if (!dragSourceOnly)
                RegisterDropZone(GUILayoutUtility.GetLastRect(), inventory);
            return;
        }

        bool isDragSource = isDragging && dragSource == inventory && dragItem == slot.item;
        var content = slot.item.icon != null
            ? new GUIContent(string.Empty, slot.item.itemName)
            : new GUIContent(isDragSource ? "" : slot.item.itemName + (slot.count > 1 ? $" x{slot.count}" : ""));

        GUILayout.Box(content, DebugGUI.Slot, GUILayout.Width(BoxSize), GUILayout.Height(BoxSize));
        var rect = GUILayoutUtility.GetLastRect();

        if (slot.item.icon != null && !isDragSource)
        {
            const float padding = 6f;
            var iconRect = new Rect(rect.x + padding, rect.y + padding, rect.width - padding * 2f, rect.height - padding * 2f);
            GUI.DrawTexture(iconRect, slot.item.icon.texture, ScaleMode.ScaleToFit);
        }

        HandleSlotMouseDown(rect, inventory, slot);
        if (!dragSourceOnly)
            RegisterDropZone(rect, inventory);
    }

    private void RegisterDropZone(Rect rect, Inventory inventory)
    {
        var screenRect = GUIUtility.GUIToScreenRect(rect);
        dropZones.Add(new DropZone { Rect = screenRect, Inventory = inventory });
    }

    private void HandleSlotMouseDown(Rect rect, Inventory inventory, Inventory.Slot slot)
    {
        var e = Event.current;
        if (e.type != EventType.MouseDown || e.button != 0 || !rect.Contains(e.mousePosition)) return;

        dragCandidate = true;
        isDragging = false;
        dragStartMousePos = e.mousePosition;
        dragSource = inventory;
        dragItem = slot.item;
        dragQuantity = slot.count; // always the whole stack — no partial-drag split needed here
        e.Use();
    }

    private void HandleGlobalDragRelease()
    {
        if (!dragCandidate) return;

        var e = Event.current;

        if (!isDragging && Vector2.Distance(e.mousePosition, dragStartMousePos) > DragThreshold)
            isDragging = true;

        if (e.type != EventType.MouseUp || e.button != 0) return;

        if (isDragging)
        {
            foreach (var zone in dropZones)
            {
                if (!zone.Rect.Contains(e.mousePosition)) continue;
                TryDrop(zone);
                break;
            }
        }

        dragCandidate = false;
        isDragging = false;
        e.Use();
    }

    private void TryDrop(DropZone zone)
    {
        if (dragItem == null || dragSource == null || zone.Inventory == null) return;
        if (zone.Inventory == dragSource) return;

        InventoryTransfer.MoveAsManyAsFit(dragSource, zone.Inventory, dragItem, dragQuantity);
    }

    private void DrawDropZoneHighlight()
    {
        if (!isDragging) return;

        var mousePos = Event.current.mousePosition;
        foreach (var zone in dropZones)
        {
            if (!zone.Rect.Contains(mousePos)) continue;

            const float thickness = 3f;
            var r = zone.Rect;
            Color highlight = Color.yellow;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, thickness), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, highlight, 0, 0);
            GUI.DrawTexture(new Rect(r.x, r.yMax - thickness, r.width, thickness), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, highlight, 0, 0);
            GUI.DrawTexture(new Rect(r.x, r.y, thickness, r.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, highlight, 0, 0);
            GUI.DrawTexture(new Rect(r.xMax - thickness, r.y, thickness, r.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, highlight, 0, 0);
            break;
        }
    }

    private void DrawDragGhost()
    {
        if (!isDragging || dragItem == null) return;

        var mousePos = Event.current.mousePosition;

        if (dragItem.icon != null)
        {
            var rect = new Rect(mousePos.x - BoxSize / 2f, mousePos.y - BoxSize / 2f, BoxSize, BoxSize);
            DebugGUI.DrawPanel(rect);
            const float padding = 6f;
            var iconRect = new Rect(rect.x + padding, rect.y + padding, rect.width - padding * 2f, rect.height - padding * 2f);
            GUI.DrawTexture(iconRect, dragItem.icon.texture, ScaleMode.ScaleToFit);
        }
        else
        {
            var content = new GUIContent(dragQuantity > 1 ? $"{dragItem.itemName} x{dragQuantity}" : dragItem.itemName);
            var size = DebugGUI.Label.CalcSize(content);
            var rect = new Rect(mousePos.x + 10f, mousePos.y + 10f, size.x + 10f, size.y + 6f);
            DebugGUI.DrawPanel(rect);
            GUI.Label(rect, content, DebugGUI.Label);
        }
    }
}
