using System.Collections.Generic;
using UnityEngine;

// Opened by interacting (E) with a Furnace — same popup family as
// CampfireScreen/LockboxScreen. Shows the Furnace's on-board Fuel/
// Materials/Output slots, a Backpack/Hands transfer section (drag-and-drop,
// same self-contained implementation CampfireScreen uses — see that file's
// header comment for why this isn't extracted into shared code), the
// up-to-4 recipe queue picker, an Auto-Run toggle, and three nearby-
// StorageBox pickers (Fuel Source / Materials Source / Output) that assign
// Furnace's unattended-automation links.
[RequireComponent(typeof(PlayerEquipment))]
public class FurnaceScreen : MonoBehaviour
{
    private const float MaxPanelWidth = 520f;
    private const float MaxPanelHeight = 640f;
    private const float ChromeReserve = 170f;
    private static float PanelWidth => Mathf.Min(MaxPanelWidth, Screen.width * 0.92f);
    private static float PanelHeight => Mathf.Min(MaxPanelHeight, Screen.height * 0.92f);
    private static float ScrollHeight => Mathf.Max(120f, PanelHeight - ChromeReserve);
    private const float BoxSize = 56f;
    private const float BoxGap = 8f;
    private const float DragThreshold = 12f;
    private const int GridColumns = 4;

    private PlayerEquipment equipment;
    private Furnace current;
    private bool isOpen;
    private Vector2 scrollPos;
    private readonly List<StorageBox> nearbyBoxes = new List<StorageBox>();

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

    public void Open(Furnace furnace)
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        current = furnace;
        SetOpen(true);
    }

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

        GUILayout.Label("Output", DebugGUI.Header);
        DrawGrid(current.OutputInventory, GridColumns, dragSourceOnly: true);

        GUILayout.Space(10);
        DrawQueueSection();

        GUILayout.Space(10);
        DrawTransferSection();

        GUILayout.Space(10);
        GUILayout.Label("Materials", DebugGUI.Header);
        DrawGrid(current.MaterialsInventory, GridColumns, dragSourceOnly: false);

        GUILayout.Space(10);
        GUILayout.Label("Fuel (wood)", DebugGUI.Header);
        DrawGrid(current.FuelInventory, GridColumns, dragSourceOnly: false);

        GUILayout.Space(10);
        DrawAutomationSection();

        GUILayout.EndScrollView();

        GUILayout.Space(5);
        if (GUILayout.Button("Close"))
            SetOpen(false);

        GUILayout.EndArea();

        HandleGlobalDragRelease();
        DrawDropZoneHighlight();
        DrawDragGhost();
    }

    // Every SmeltableItem registered on this Furnace, each with a toggle
    // button showing whether it's currently in the up-to-4 queue. Queued
    // order is shown below as a simple numbered list — the order
    // StartNextQueuedRecipe round-robins through.
    private void DrawQueueSection()
    {
        GUILayout.Label($"Smelting Queue ({current.RecipeQueue.Count}/{Furnace.MaxQueueSize})", DebugGUI.Header);

        if (current.ActiveRecipe != null)
        {
            float progress = Mathf.Clamp01(current.SmeltSecondsElapsed / current.ActiveRecipe.smeltDurationSeconds);
            GUILayout.Label($"Smelting {current.ActiveRecipe.outputItem.itemName} — {Mathf.RoundToInt(progress * 100f)}%", DebugGUI.Label);
        }

        var recipes = current.SmeltableItems;
        if (recipes == null || recipes.Length == 0)
        {
            GUILayout.Label("No smeltable recipes registered.", DebugGUI.Label);
            return;
        }

        foreach (var recipe in recipes)
        {
            if (recipe == null || recipe.outputItem == null) continue;

            bool queued = current.IsQueued(recipe);
            bool full = !queued && current.RecipeQueue.Count >= Furnace.MaxQueueSize;

            GUI.enabled = !full;
            string label = $"{(queued ? "[Queued] " : "")}{recipe.outputItem.itemName} x{recipe.outputCount} ({Mathf.RoundToInt(recipe.smeltDurationSeconds)}s)";
            if (GUILayout.Button(label))
                current.ToggleQueue(recipe);
            GUI.enabled = true;
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

    // Auto-Run toggle plus three storage-box pickers (Fuel Source /
    // Materials Source / Output) — each lists every StorageBox currently
    // within the Furnace's own storageLinkRange (not the player's) since
    // that's the exact range AutoRefill/AutoDrain check, so nothing shown
    // here is a box that wouldn't actually work once assigned.
    private void DrawAutomationSection()
    {
        GUILayout.Label("Automation", DebugGUI.Header);

        bool autoRun = current.AutoRunEnabled;
        bool newAutoRun = GUILayout.Toggle(autoRun, autoRun ? "Auto-Run: ON" : "Auto-Run: OFF");
        if (newAutoRun != autoRun)
            current.SetAutoRun(newAutoRun);

        GUILayout.Space(6);
        current.FindNearbyStorageBoxes(nearbyBoxes);

        DrawStoragePicker("Fuel Source", current.FuelSourceBox, current.SetFuelSourceBox);
        DrawStoragePicker("Materials Source", current.MaterialsSourceBox, current.SetMaterialsSourceBox);
        DrawStoragePicker("Output Box", current.OutputBox, current.SetOutputBox);
    }

    private void DrawStoragePicker(string label, StorageBox assigned, System.Action<StorageBox> assign)
    {
        GUILayout.Label($"{label}: {(assigned != null ? assigned.DisplayName : "(none)")}", DebugGUI.Label);

        GUILayout.BeginHorizontal();
        if (assigned != null && GUILayout.Button("Clear"))
            assign(null);

        foreach (var box in nearbyBoxes)
        {
            if (box == assigned) continue;
            if (GUILayout.Button(box.DisplayName))
            {
                assign(box);
                break;
            }
        }
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

        // QTY label below the box -- separate from the box's own GUIContent
        // text so it still shows for icon-bearing items, same fix
        // CampfireScreen.DrawBox already got (that text is unconditionally
        // blanked out whenever slot.item.icon != null, dropping the count
        // entirely for any item with a baked icon -- found live, 2026-08-18).
        string qtyLabel = isDragSource || slot.item.maxStack <= 1 ? "" : $"QTY: {slot.count}";
        GUILayout.Label(qtyLabel, DebugGUI.Label, GUILayout.Width(BoxSize));
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
        dragQuantity = slot.count;
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
