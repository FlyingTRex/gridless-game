using System.Collections.Generic;
using UnityEngine;

// Full inventory + equipment management content, drawn as the Inventory tab
// inside PlayerMenuScreen (Tab key). Used to be its own screen toggled with
// I; folded in 2026-08-04 so Inventory/Skills/Crafting all live under one
// key instead of three. Combines what used to be separate always-on panels
// (Inventory, Backpack, Canteen) into one place, scrollable so it can't
// overflow the window.
[RequireComponent(typeof(PlayerEquipment))]
[RequireComponent(typeof(PlayerInventory))]
public class InventoryScreen : MonoBehaviour
{
    private static readonly string[] SlotOrder =
    {
        "Head", "Face", "Neck", "Chest", "Back",
        "Left Arm", "Right Arm", "Left Wrist", "Right Wrist",
        "Left Hand", "Right Hand", "Waist", "Leg", "Feet",
    };

    private const float BoxWidth = 130f;
    private const float BoxHeight = 40f;
    private const float LabelWidth = 110f;

    private const float SubBoxWidth = 70f;
    // Was 30f — smaller than the 32x32 icon it now needs to hold, before
    // even accounting for padding. Bumped up so an icon actually fits.
    private const float SubBoxHeight = 44f;
    private const int SubBoxesPerRow = 6;

    // A bigger standalone preview of whatever's worn on Back/Waist, drawn
    // once per worn container right under its own "Inventory" header —
    // Ben's request, distinct from the small icon shown next to that
    // slot's row itself further down. Was Back-only until v0.1.124-dev;
    // renamed (from BackPreview*) when it became per-container.
    private const float ContainerPreviewSize = 96f;
    private const float ContainerPreviewPadding = 8f;

    private const float PanelWidth = LabelWidth + BoxWidth * 2f + 220f;

    private static readonly (string Label, CoinType Type)[] CoinDisplayOrder =
    {
        ("Copper", CoinType.Copper),
        ("Iron", CoinType.Iron),
        ("Silver", CoinType.Silver),
        ("Gold", CoinType.Gold),
        ("Platinum", CoinType.Platinum),
    };

    private const float CoinBoxHeight = 40f;
    private const float CoinRowWidthFraction = 0.9f;
    private const float CoinGap = 10f;
    // Rough vertical space DrawCurrencySection() + its trailing Space(10)
    // takes up — reserved out of the scroll view's height so the fixed
    // currency header never gets clipped by it.
    private const float CurrencySectionHeight = 80f;

    // Rough vertical space PlayerMenuScreen's tab bar + Close button + their
    // margins take up around this content — reserved the same way, so the
    // scroll view never runs off the bottom of the full-screen menu.
    private const float ChromeReserve = 180f;

    [SerializeField] private float storageRange = 10f;

    private PlayerEquipment equipment;
    private PlayerInventory playerInventory;
    private PlayerDropping dropping;
    private PlayerEating eating;
    private PlayerMedicine medicine;
    private PlayerReading reading;
    private PlayerBackpack backpackCarrier;
    private PlayerBelt beltCarrier;
    private PlayerBoot bootCarrier;
    private PlayerCanteen canteenCarrier;
    private PlayerNavComputer navComputerCarrier;
    private PlayerHealthMonitor healthMonitorCarrier;
    private PlayerSunglasses sunglassesCarrier;
    private PlayerMiningFaceShield miningShieldCarrier;
    private PlayerTool toolCarrier;
    private PlayerShirt shirtCarrier;
    private PlayerJeans jeansCarrier;
    private PlayerCurrency currency;
    private PlayerCoinDrop coinDropper;
    private PlayerVitals vitals;
    private Vector2 scrollPos;

    // Screen-space rect of the scroll view opened in DrawContent(), used by
    // HandleAutoScroll for edge-of-view hit-testing during a drag. Captured
    // from the PREVIOUS frame's GUILayout.EndScrollView() — calling
    // GUILayoutUtility.GetLastRect() immediately after BeginScrollView()
    // throws ("cannot call GetLast immediately after beginning a group"),
    // since the scroll view's own inner group has no entries yet at that
    // point. One frame of lag is imperceptible for this purpose.
    private Rect lastScrollViewRect;

    // Recomputed once per DrawContent() call (see StorageBox.FindNearby) —
    // every StorageBox within storageRange, nearest first. The nearest
    // one's contents render directly in the scroll view (DrawContent) as
    // just another draggable grid — with more than one in range, only the
    // nearest is reachable from this screen; walking closer to a different
    // box switches which one shows.
    private readonly List<StorageBox> nearbyStorages = new List<StorageBox>();

    // Set when the player clicks (not drags) an occupied slot box anywhere
    // on this screen — the main grid, an equipment slot, or a container's
    // contents grid. Opens a small action menu (Drop / Eat / Apply /
    // Drink / Fill / Equip / Unequip, whichever apply) instead of acting
    // immediately. Replaces the old "where should this go?" destination
    // popup (2026-08-12) — moving an item is now done by dragging it,
    // so this menu is action-only.
    private ItemDefinition pendingActionItem;
    private Inventory pendingActionSource;
    // The specific equipment instance behind pendingActionItem, when the
    // clicked slot held one (a Canteen, Backpack, etc.) -- null for a plain
    // stackable item. Needed for actions that operate on the physical
    // instance rather than the item type/count (Drink/Fill a container-held
    // Canteen), and to decide whether to offer Equip vs Unequip.
    private IEquippable pendingActionEquipment;

    // --- Drag and drop (2026-08-12) ---
    // Press-and-hold an occupied slot box and drag it to where it should
    // go; release over an invalid target (or empty space) and nothing
    // happens, since the underlying data is never touched until a drop
    // actually resolves. A plain click (no movement past DragThreshold)
    // opens the action menu above instead. See the plan this was built
    // from for the full design (worn-item bugs this fixes, why each
    // equippable carrier needed a source-aware Equip overload first).
    // Was 6f — too twitchy in practice (2026-08-12 live feedback): an
    // ordinary left click naturally moves the mouse a few pixels between
    // press and release, which was enough to cross 6f and start a drag
    // instead of opening the action menu. Right-click (see HandleSlotEvents)
    // is now the reliable way to open the menu regardless of this value;
    // this just makes a plain left click more forgiving too.
    private const float DragThreshold = 12f;

    // True from MouseDown on an occupied box until the matching MouseUp,
    // regardless of whether it turns into an actual drag.
    private bool dragCandidate;
    // Promoted from dragCandidate once the mouse has moved past
    // DragThreshold since MouseDown — only then does releasing attempt a
    // drop instead of opening the action menu.
    private bool isDragging;
    private Vector2 dragStartMousePos;
    private Inventory dragSource;
    private ItemDefinition dragItem;
    private IEquippable dragEquipment;
    // Whole slot by default; Shift = half (rounded down, min 1), Ctrl = 1 —
    // decided once at MouseDown (ComputeDragQuantity), not re-evaluated
    // mid-drag. Always 1 for an equipment instance (never stacks).
    private int dragQuantity;

    // One entry per slot box drawn this frame (occupied or empty — empty
    // boxes are valid drop targets too), rebuilt at the top of every
    // DrawContent() call. EquipSlotName is null for an inert-storage
    // target (main inventory, a Backpack/Boot/StorageBox's contents) and
    // the real PlayerEquipment slot name — or the "Belt" sentinel, for the
    // worn Belt's own attachment points — for a carried target.
    private struct DropZone
    {
        public Rect Rect;
        public Inventory Inventory;
        public string EquipSlotName;
    }
    private readonly List<DropZone> dropZones = new List<DropZone>();

    // Set when the player clicks a coin box in the currency row — opens a
    // popup to pick how many of that type to drop.
    private CoinType? pendingDropCoinType;
    private int pendingDropAmount;

    // Set when the player clicks Drop in the action menu on a plain
    // stackable item — opens a quantity picker instead of always dropping
    // the entire stack. Real gap found in playtesting
    // (2026-08-09): dropping "one" of a non-stacking item (any Hammer
    // tier, maxStack 1) dropped every one you had, since the old one-click
    // Drop always removed the item's full count with no way to choose
    // less. Mirrors DrawCoinDropPopup's shape exactly.
    private ItemDefinition pendingDropItem;
    private Inventory pendingDropItemSource;
    private int pendingDropItemAmount;

    // Set when an Equip click has more than one valid destination (e.g. a
    // Canteen with both hands free and a Belt worn) — opens a popup
    // listing them instead of committing to whichever one the carrier
    // would have picked automatically. Left null (and Equip commits
    // immediately) when there's only 0 or 1 option, since there's nothing
    // to choose between.
    private System.Collections.Generic.List<string> pendingEquipDestinations;
    private System.Action<string> pendingEquipChoose;
    private string pendingEquipLabel;

    private void Awake()
    {
        equipment = GetComponent<PlayerEquipment>();
        playerInventory = GetComponent<PlayerInventory>();
        dropping = GetComponent<PlayerDropping>();
        eating = GetComponent<PlayerEating>();
        medicine = GetComponent<PlayerMedicine>();
        reading = GetComponent<PlayerReading>();
        backpackCarrier = GetComponent<PlayerBackpack>();
        beltCarrier = GetComponent<PlayerBelt>();
        bootCarrier = GetComponent<PlayerBoot>();
        canteenCarrier = GetComponent<PlayerCanteen>();
        navComputerCarrier = GetComponent<PlayerNavComputer>();
        healthMonitorCarrier = GetComponent<PlayerHealthMonitor>();
        sunglassesCarrier = GetComponent<PlayerSunglasses>();
        miningShieldCarrier = GetComponent<PlayerMiningFaceShield>();
        toolCarrier = GetComponent<PlayerTool>();
        shirtCarrier = GetComponent<PlayerShirt>();
        jeansCarrier = GetComponent<PlayerJeans>();
        currency = GetComponent<PlayerCurrency>();
        coinDropper = GetComponent<PlayerCoinDrop>();
        vitals = GetComponent<PlayerVitals>();
    }

    // Called by PlayerMenuScreen while its Inventory tab is active.
    public void DrawContent()
    {
        // Every drop zone gets re-registered as the screen redraws this
        // frame — see DropZone above for why a stale list from a previous
        // frame (e.g. an item that just moved) would misreport hit-tests.
        dropZones.Clear();

        StorageBox.FindNearby(transform.position, storageRange, nearbyStorages);

        DrawCurrencySection();
        GUILayout.Space(10);

        float scrollHeight = Mathf.Min(Screen.height - ChromeReserve - CurrencySectionHeight, 640f);
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(scrollHeight));

        // A valid target (e.g. a hand slot, ~10 rows down the 14-row
        // Equipment list) can easily be scrolled out of view while an item
        // being dragged sits above the fold — real gap found live
        // (2026-08-12): dragging a Knife toward "Left Hand" with no way to
        // reach it, since there was no way to scroll while a drag was in
        // progress (grabbing the actual scrollbar thumb mid-drag isn't
        // practical with the mouse button already committed to the item).
        // Nudges scrollPos directly while the cursor sits in a margin near
        // the top/bottom edge of the scroll view — doesn't require the
        // scrollbar itself to be interactable.
        if (isDragging)
            HandleAutoScroll(lastScrollViewRect);

        DrawInventorySection();

        GUILayout.Space(10);

        // Worn containers need to be known before the slot list draws —
        // used to decide whether the single "Inventory" panel appears at
        // all, and how many preview+contents rows it stacks inside itself.
        // See GetWornContainers() for why this can't just be
        // DrawEquipmentSection()'s old return value.
        var wornContainers = GetWornContainers();

        GUILayout.BeginHorizontal();

        // Slot list panel — always present. "Equipment" now labels this
        // panel specifically, not the row as a whole (see the "Inventory"
        // label on the panel to its right, moved down from where it used
        // to sit above DrawInventorySection() — Ben's call).
        GUILayout.BeginVertical(DebugGUI.Panel);
        GUILayout.Label("Equipment", DebugGUI.Header);
        DrawEquipmentSection();
        GUILayout.EndVertical();

        // Single "Inventory" panel beside the slot list, holding one
        // preview+contents row per worn container (Backpack on Back,
        // Belt on Waist) stacked vertically inside it — not a separate
        // bordered panel per container (tried that first, v0.1.124-dev;
        // Ben's call to merge them into one panel instead once he saw two
        // side by side). Still 0 rows (no panel at all) when nothing's
        // worn on Back or Waist.
        if (wornContainers.Count > 0)
        {
            GUILayout.Space(20);
            GUILayout.BeginVertical(DebugGUI.Panel);
            GUILayout.Label("Inventory", DebugGUI.Header);

            // Rows sharing the same PreviewSlotName (e.g. a Military
            // Boot's Knife Sheath + Pistol Holster, both "Feet") come from
            // the same worn item — grouped onto one horizontal line with a
            // single shared preview icon, rather than each getting its own
            // full-width row, to save vertical space (Ben's report,
            // 2026-08-11: two boot slots stacked wasted a lot of screen).
            // Back/Waist still get one row each same as before, since
            // nothing else currently shares their slot names.
            int i = 0;
            while (i < wornContainers.Count)
            {
                string groupSlotName = wornContainers[i].PreviewSlotName;
                int groupEnd = i;
                while (groupEnd < wornContainers.Count && wornContainers[groupEnd].PreviewSlotName == groupSlotName)
                    groupEnd++;

                GUILayout.BeginHorizontal();
                DrawContainerPreview(GetSlotPreviewIcon(groupSlotName));
                GUILayout.Space(20);

                for (int j = i; j < groupEnd; j++)
                {
                    GUILayout.BeginVertical();
                    DrawContainerContents(wornContainers[j].Inventory, wornContainers[j].Caption, wornContainers[j].EquipSlotName);
                    GUILayout.EndVertical();

                    if (j < groupEnd - 1)
                        GUILayout.Space(20);
                }

                GUILayout.EndHorizontal();

                i = groupEnd;
                if (i < wornContainers.Count)
                    GUILayout.Space(10);
            }

            GUILayout.EndVertical();
        }

        GUILayout.EndHorizontal();

        // Draws every StorageBox in range, not just the nearest one --
        // found live, 2026-08-19: two boxes placed next to each other made
        // the second one completely inaccessible, since this only ever
        // indexed nearbyStorages[0] despite the list already containing
        // every box in range (sorted nearest-first by StorageBox.FindNearby).
        foreach (var box in nearbyStorages)
        {
            GUILayout.Space(10);
            GUILayout.Label($"{box.DisplayName} (nearby)", DebugGUI.Header);
            DrawContainerContents(box.Inventory, "drag to move, click for actions", null);
        }

        GUILayout.EndScrollView();
        lastScrollViewRect = GUILayoutUtility.GetLastRect();
    }

    // Called by PlayerMenuScreen right after ending its own full-screen
    // BeginArea, only while the Inventory tab is active — these are
    // absolutely-positioned popups (screen-centered) that need to sit on
    // top of, not nested inside, the tab content area.
    public void DrawPopups()
    {
        // Must run before the popups below draw — it's what turns a
        // release-without-dragging into pendingActionItem (so the action
        // menu below has something to show this same frame) and what
        // resolves an actual drag against this frame's drop-zone registry.
        HandleGlobalDragRelease();

        DrawPendingActionMenu();
        DrawPendingEquipPopup();
        DrawCoinDropPopup();
        DrawItemDropPopup();
        DrawDropZoneHighlight();
        DrawDragGhost();
        DrawTooltip();
    }

    // Outlines whichever drop zone the cursor is currently over while
    // dragging — real gap found live (2026-08-12): small, closely-packed
    // targets (a Boot's Knife Sheath sitting right next to its Pistol
    // Holster, which accepts nothing) gave no feedback at all about which
    // one a drop would actually land on, so a drop that missed by a few
    // pixels and landed on the wrong box looked identical to the item
    // just not moving. Drawn before the drag ghost so the ghost stays on
    // top of the highlight outline it's hovering over.
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

    // Unity's runtime IMGUI (unlike the Editor's) never draws GUI.tooltip
    // on its own — setting a GUIContent's tooltip just makes the string
    // available, nothing renders it without this. Drawn from DrawPopups()
    // rather than inside the scroll view in DrawContent(), same reasoning
    // as the other popups there: needs to sit on top of everything,
    // unclipped by the scroll rect, positioned in real screen space.
    private void DrawTooltip()
    {
        if (string.IsNullOrEmpty(GUI.tooltip)) return;

        var content = new GUIContent(GUI.tooltip);
        var size = DebugGUI.Label.CalcSize(content);
        var mousePos = Event.current.mousePosition;
        const float padding = 5f;
        var rect = new Rect(mousePos.x + 12f, mousePos.y + 12f, size.x + padding * 2f, size.y + padding * 2f);

        DebugGUI.DrawPanel(rect);
        GUI.Label(rect, content, DebugGUI.Label);
    }

    // Called by PlayerMenuScreen when the whole Tab menu closes, so a
    // still-open action menu, equip-choice popup, coin-drop popup, or an
    // in-progress drag doesn't stay stuck the next time the menu reopens.
    public void ResetPopups()
    {
        pendingActionItem = null;
        pendingActionSource = null;
        pendingActionEquipment = null;
        pendingEquipDestinations = null;
        pendingEquipChoose = null;
        pendingEquipLabel = null;
        pendingDropCoinType = null;
        pendingDropItem = null;
        pendingDropItemSource = null;
        dragCandidate = false;
        isDragging = false;
    }

    // Small action menu shown after clicking (not dragging) an occupied
    // slot box anywhere on this screen. Drawn last so it sits on top.
    // Replaces the old "where should this go?" destination popup
    // (2026-08-12) — moving an item is now done by dragging it (see
    // HandleGlobalDragRelease/TryDrop), so this is action-only.
    private void DrawPendingActionMenu()
    {
        if (pendingActionItem == null || pendingActionSource == null) return;

        const float width = 220f;
        const float height = 260f;
        var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);

        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Label(ItemContent(pendingActionItem, pendingActionItem.itemName), DebugGUI.Header);

        bool resolved = DrawPendingActions();

        GUILayout.EndArea();

        if (resolved)
        {
            pendingActionItem = null;
            pendingActionSource = null;
            pendingActionEquipment = null;
        }
    }

    // The action list itself. Returns true once the menu should close.
    private bool DrawPendingActions()
    {
        var edible = eating != null ? eating.FindEdible(pendingActionItem) : null;
        if (edible != null && GUILayout.Button(edible.verb))
        {
            eating.TryEatFrom(pendingActionSource, pendingActionItem);
            return true;
        }

        var medicineItem = medicine != null ? medicine.FindMedicine(pendingActionItem) : null;
        if (medicineItem != null && GUILayout.Button(medicineItem.verb))
        {
            medicine.TryApplyFrom(pendingActionSource, pendingActionItem);
            return true;
        }

        // Drink/Fill act on the physical Canteen instance directly
        // (pendingActionEquipment) rather than consuming an item count from
        // pendingActionSource — they don't remove the canteen from its
        // slot, they just change what's inside it.
        if (pendingActionEquipment is Canteen canteen)
        {
            if (!canteen.IsEmpty && GUILayout.Button("Drink"))
            {
                canteen.Drink(vitals);
                return true;
            }

            if (!canteen.IsFull && GUILayout.Button("Fill"))
            {
                canteen.Fill(LiquidType.Water);
                return true;
            }
        }

        // A SkillBook is never worn (CanEquipToSlot always false), so it
        // skips the generic Equip/Unequip block entirely below — Read is
        // its only real action besides Drop, consuming it permanently
        // (SKILL_BOOKS_PLANNING.md's Phase 3).
        if (pendingActionEquipment is SkillBook book)
        {
            if (reading != null && GUILayout.Button("Read"))
            {
                reading.TryRead(pendingActionSource, book);
                return true;
            }
        }
        else if (pendingActionEquipment != null)
        {
            if (IsCurrentlyWorn(pendingActionEquipment))
            {
                if (GUILayout.Button("Unequip"))
                {
                    UnequipDispatch(pendingActionEquipment);
                    return true;
                }
            }
            else if (GUILayout.Button("Equip"))
            {
                EquipWithChoice(pendingActionEquipment, pendingActionSource);
                return true;
            }
        }
        // Bow/Arrow aren't IEquippable (no worn slot, no carrier) — same
        // plain-ItemDefinition-held-in-a-hand category as Pickaxe/Axe, so
        // this popup never offered Equip for them (found live by Ben,
        // 2026-08-17). Drag-to-hand already worked via TryDrop's own
        // plain-item branch; this just exposes the same operation as a
        // click, same reasoning every other action here mirrors its drag
        // equivalent.
        else if (pendingActionItem != null && (pendingActionItem.isRangedWeapon || pendingActionItem.isArrow)
            && GUILayout.Button("Equip"))
        {
            TryEquipToHand(pendingActionItem, pendingActionSource);
            return true;
        }

        if (GUILayout.Button("Drop"))
        {
            pendingDropItem = pendingActionItem;
            pendingDropItemSource = pendingActionSource;
            pendingDropItemAmount = pendingActionSource.GetCount(pendingActionItem);
            return true;
        }

        return GUILayout.Button("Cancel");
    }

    // Equip destination picker — only shown when an Equip click found more
    // than one valid place to put the item (see the TryEquipWithChoice
    // overloads below); 0 or 1 options never reach here, since there's
    // nothing to choose between.
    private void DrawPendingEquipPopup()
    {
        if (pendingEquipDestinations == null || pendingEquipChoose == null) return;

        const float width = 220f;
        float height = 70f + pendingEquipDestinations.Count * 30f;
        var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);

        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Label($"Equip {pendingEquipLabel} to:", DebugGUI.Header);

        string chosen = null;
        foreach (var destination in pendingEquipDestinations)
        {
            if (GUILayout.Button(destination))
                chosen = destination;
        }

        bool cancel = GUILayout.Button("Cancel");
        GUILayout.EndArea();

        if (chosen != null)
            pendingEquipChoose(chosen);

        if (chosen != null || cancel)
        {
            pendingEquipDestinations = null;
            pendingEquipChoose = null;
            pendingEquipLabel = null;
        }
    }

    // Equips immediately if there's 0 or 1 valid destination (nothing to
    // choose); opens the Equip destination popup instead if there are 2+.
    // source is where the canteen is actually sitting right now (main
    // inventory grid, or a Backpack's nested Inventory via the move popup)
    // — EquipTo needs it to remove the canteen from the right place.
    private void TryEquipWithChoice(Canteen canteen, Inventory source)
    {
        var destinations = canteenCarrier.AvailableDestinations(canteen);
        if (destinations.Count <= 1)
        {
            if (destinations.Count == 1) canteenCarrier.EquipTo(canteen, destinations[0], source);
            return;
        }

        pendingEquipDestinations = destinations;
        pendingEquipLabel = canteen.DisplayName;
        pendingEquipChoose = destination => canteenCarrier.EquipTo(canteen, destination, source);
    }

    private void TryEquipWithChoice(NavigationComputer navComputer, Inventory source)
    {
        var destinations = navComputerCarrier.AvailableDestinations(navComputer);
        if (destinations.Count <= 1)
        {
            if (destinations.Count == 1) navComputerCarrier.EquipTo(navComputer, destinations[0], source);
            return;
        }

        pendingEquipDestinations = destinations;
        pendingEquipLabel = navComputer.DisplayName;
        pendingEquipChoose = destination => navComputerCarrier.EquipTo(navComputer, destination, source);
    }

    private void TryEquipWithChoice(PersonalHealthMonitor monitor, Inventory source)
    {
        var destinations = healthMonitorCarrier.AvailableDestinations(monitor);
        if (destinations.Count <= 1)
        {
            if (destinations.Count == 1) healthMonitorCarrier.EquipTo(monitor, destinations[0], source);
            return;
        }

        pendingEquipDestinations = destinations;
        pendingEquipLabel = monitor.DisplayName;
        pendingEquipChoose = destination => healthMonitorCarrier.EquipTo(monitor, destination, source);
    }

    private void TryEquipWithChoice(Tool tool, Inventory source)
    {
        var destinations = toolCarrier.AvailableDestinations(tool);
        if (destinations.Count <= 1)
        {
            if (destinations.Count == 1) toolCarrier.EquipTo(tool, destinations[0], source);
            return;
        }

        pendingEquipDestinations = destinations;
        pendingEquipLabel = tool.DisplayName;
        pendingEquipChoose = destination => toolCarrier.EquipTo(tool, destination, source);
    }

    // Single dispatch point for "equip this instance from this source",
    // used by both the click-menu's Equip button and drag's carried-target
    // drop path (EquipToSlotDispatch below). Each of the 8 equippable
    // types still owns its actual equip mechanics (including anchor
    // selection) via its own carrier — this just routes to the right one
    // by runtime type instead of the 8-branch if/else chains this file used
    // to have three separate copies of (DrawInventorySection,
    // DrawEquipmentSection, and this dispatch itself, pre-2026-08-12).
    private void EquipWithChoice(IEquippable equipment, Inventory source)
    {
        switch (equipment)
        {
            case Backpack backpack: backpackCarrier.Equip(backpack, source); break;
            case Belt belt: beltCarrier.Equip(belt, source); break;
            case Boot boot: bootCarrier.Equip(boot, source); break;
            case Sunglasses sunglasses: sunglassesCarrier.Equip(sunglasses, source); break;
            case MiningFaceShield shield: miningShieldCarrier.Equip(shield, source); break;
            case Canteen canteen: TryEquipWithChoice(canteen, source); break;
            case NavigationComputer navComputer: TryEquipWithChoice(navComputer, source); break;
            case PersonalHealthMonitor monitor: TryEquipWithChoice(monitor, source); break;
            case Tool tool: TryEquipWithChoice(tool, source); break;
            case Shirt shirt: shirtCarrier.Equip(shirt, source); break;
            case Jeans jeans: jeansCarrier.Equip(jeans, source); break;
        }
    }

    // Same idea as EquipWithChoice, but for a drag drop onto a specific,
    // already-known slot (no ambiguity to resolve, unlike a click) — the
    // caller (TryDrop) has already confirmed equipment.CanEquipToSlot(slotName).
    private bool EquipToSlotDispatch(IEquippable equipment, string slotName, Inventory source)
    {
        switch (equipment)
        {
            case Backpack backpack: return backpackCarrier.Equip(backpack, source);
            case Belt belt: return beltCarrier.Equip(belt, source);
            case Boot boot: return bootCarrier.Equip(boot, source);
            case Sunglasses sunglasses: return sunglassesCarrier.Equip(sunglasses, source);
            case MiningFaceShield shield: return miningShieldCarrier.Equip(shield, source);
            case Canteen canteen: return canteenCarrier.EquipTo(canteen, slotName, source);
            case NavigationComputer navComputer: return navComputerCarrier.EquipTo(navComputer, slotName, source);
            case PersonalHealthMonitor monitor: return healthMonitorCarrier.EquipTo(monitor, slotName, source);
            case Tool tool: return toolCarrier.EquipTo(tool, slotName, source);
            case Shirt shirt: return shirtCarrier.Equip(shirt, source);
            case Jeans jeans: return jeansCarrier.Equip(jeans, source);
            default: return false;
        }
    }

    // Bow/Arrow's "Equip" — moves the whole available stack (mirrors a
    // plain click having no drag-modifier concept, same amount a drag
    // would carry by default) into the first free hand, Left then Right.
    // MoveAsManyAsFit already no-ops cleanly if a hand can't take it (wrong
    // type occupying it, etc.), same as the drag path this mirrors.
    private void TryEquipToHand(ItemDefinition item, Inventory source)
    {
        int quantity = source.GetCount(item);
        if (quantity <= 0) return;

        foreach (var handName in new[] { "Left Hand", "Right Hand" })
        {
            var hand = equipment.GetSlot(handName);
            if (hand != null && InventoryTransfer.MoveAsManyAsFit(source, hand, item, quantity) > 0)
                return;
        }
    }

    private void UnequipDispatch(IEquippable equipment)
    {
        switch (equipment)
        {
            case Backpack backpack: backpackCarrier.Unequip(backpack); break;
            case Belt belt: beltCarrier.Unequip(belt); break;
            case Boot boot: bootCarrier.Unequip(boot); break;
            case Canteen canteen: canteenCarrier.Unequip(canteen); break;
            case NavigationComputer navComputer: navComputerCarrier.Unequip(navComputer); break;
            case PersonalHealthMonitor monitor: healthMonitorCarrier.Unequip(monitor); break;
            case Sunglasses sunglasses: sunglassesCarrier.Unequip(sunglasses); break;
            case MiningFaceShield shield: miningShieldCarrier.Unequip(shield); break;
            case Tool tool: toolCarrier.Unequip(tool); break;
            case Shirt shirt: shirtCarrier.Unequip(shirt); break;
            case Jeans jeans: jeansCarrier.Unequip(jeans); break;
        }
    }

    // True if this exact instance is the one currently worn/held in its
    // type's own carried location (each carrier's Equipped already checks
    // its full set of valid locations — both hands and the belt for a
    // Canteen, either wrist for NavComputer/HealthMonitor, etc.).
    private bool IsCurrentlyWorn(IEquippable equipment)
    {
        return ReferenceEquals(backpackCarrier.Equipped, equipment)
            || ReferenceEquals(beltCarrier.Equipped, equipment)
            || ReferenceEquals(bootCarrier.Equipped, equipment)
            || ReferenceEquals(canteenCarrier.Equipped, equipment)
            || ReferenceEquals(navComputerCarrier.Equipped, equipment)
            || ReferenceEquals(healthMonitorCarrier.Equipped, equipment)
            || ReferenceEquals(sunglassesCarrier.Equipped, equipment)
            || ReferenceEquals(miningShieldCarrier.Equipped, equipment)
            || ReferenceEquals(toolCarrier.Equipped, equipment)
            || ReferenceEquals(shirtCarrier.Equipped, equipment)
            || ReferenceEquals(jeansCarrier.Equipped, equipment);
    }

    // Registers one slot box's screen rect as a drop target for this frame.
    // Called for every box drawn (occupied or empty) — an empty box is
    // just as valid a drop target as an equipped one.
    //
    // Real bug found live (2026-08-12, Ben's screenshot of the drop-zone
    // highlight landing on a caption label instead of the actual box):
    // every box lives inside DrawContent()'s GUILayout.BeginScrollView,
    // which shifts child rects into a coordinate space local to the
    // scrolled content (offset by -scrollPos, clipped to the viewport) —
    // GUILayoutUtility.GetLastRect() reports rects in THAT local space.
    // But drop resolution (HandleGlobalDragRelease) and the hover
    // highlight (DrawDropZoneHighlight) both run later from DrawPopups(),
    // which sits entirely outside the scroll view/BeginArea nesting, in
    // true absolute screen space. Comparing an unconverted local rect
    // against Event.current.mousePosition in that outer context is
    // comparing two different coordinate systems — this wasn't just a
    // cosmetic highlight-position bug, it's almost certainly the root
    // cause of the original "drag onto the Knife Sheath does nothing"
    // report, since the hit-test in HandleGlobalDragRelease has the exact
    // same mismatch. GUIUtility.GUIToScreenRect converts through every
    // active BeginGroup/BeginScrollView/BeginArea clip transform back to
    // real screen space — must be called here, synchronously while still
    // inside all of that nesting, not deferred to whenever the zone is
    // later read.
    private void RegisterDropZone(Rect rect, Inventory inventory, string equipSlotName)
    {
        var screenRect = GUIUtility.GUIToScreenRect(rect);
        dropZones.Add(new DropZone { Rect = screenRect, Inventory = inventory, EquipSlotName = equipSlotName });
    }

    // Shift = half the stack (rounded down, min 1), Ctrl = exactly 1,
    // neither = the whole stack. Equipment instances always drag as 1 —
    // they don't stack, so there's nothing to split.
    private static int ComputeDragQuantity(Inventory.Slot slot, Event e)
    {
        if (slot.equipment != null || slot.item.maxStack <= 1) return slot.count;
        if (e.control) return 1;
        if (e.shift) return Mathf.Max(1, slot.count / 2);
        return slot.count;
    }

    // Left MouseDown on an occupied box starts a drag *candidate* —
    // promoted to an actual drag once the mouse moves past DragThreshold
    // (HandleGlobalDragRelease checks that every frame, since a MouseUp
    // release can land on a completely different box's rect than this one,
    // or on no box at all). Right MouseDown opens the action menu directly
    // instead — no candidate/threshold involved, since right-click is never
    // used to drag, there's nothing to disambiguate (real gap found live,
    // 2026-08-12: a left click that moved even a couple pixels — completely
    // normal for an ordinary mouse click, not a deliberate drag — was
    // crossing DragThreshold and picking the item up instead of opening the
    // menu; right-click sidesteps that ambiguity entirely rather than
    // trying to tune the threshold around it).
    private void HandleSlotEvents(Rect rect, Inventory inventory, Inventory.Slot slot)
    {
        // Found live by Ben (2026-08-18): a click on an action-popup button
        // that visually overlapped a grid slot underneath it registered on
        // the slot instead. DrawPendingActionMenu is drawn last so it sits
        // on top *visually*, but this handler runs earlier in the same
        // OnGUI pass while laying out the grid and unconditionally consumes
        // MouseDown via e.Use() -- with plain GUILayout controls (no real
        // GUI.Window modal layering), draw order doesn't grant input
        // priority, code order does. Skip entirely while the action popup
        // is open so the grid stops competing for clicks meant for it.
        if (pendingActionItem != null) return;

        var e = Event.current;
        if (e.type != EventType.MouseDown || !rect.Contains(e.mousePosition)) return;

        if (e.button == 1)
        {
            pendingActionItem = slot.item;
            pendingActionSource = inventory;
            pendingActionEquipment = slot.equipment;
            e.Use();
            return;
        }

        if (e.button != 0) return;

        dragCandidate = true;
        isDragging = false;
        dragStartMousePos = e.mousePosition;
        dragSource = inventory;
        dragItem = slot.item;
        dragEquipment = slot.equipment;
        dragQuantity = ComputeDragQuantity(slot, e);
        e.Use();
    }

    // Draws one slot box — occupied (icon or text, drag source + drop
    // target) or empty (drop target only). Shared by the main inventory
    // grid, the equipment slot list, and every container's contents grid
    // (backpack, boot slots, storage boxes) — one rendering path instead of
    // the three near-duplicate ones this screen had before drag-and-drop
    // (2026-08-12).
    private void DrawSlotBox(Inventory inventory, Inventory.Slot slot, string equipSlotName, float width, float height, bool showEmptyLabel = false)
    {
        if (slot == null)
        {
            // The equipment slot list (Head/Face/.../Feet) keeps its old
            // "Empty" text — it's the one place on this screen where each
            // box has a distinct, named meaning (this IS the Head slot,
            // not just an anonymous cargo space), so a blank box reads as
            // ambiguous rather than "nothing here." Contents grids
            // (backpack/boot/storage) stay text-free, same as before
            // drag-and-drop — Ben's original call there.
            GUILayout.Box(new GUIContent(showEmptyLabel ? "Empty" : ""), DebugGUI.Slot, GUILayout.Width(width), GUILayout.Height(height));
            RegisterDropZone(GUILayoutUtility.GetLastRect(), inventory, equipSlotName);
            GUILayout.Label("", DebugGUI.Label, GUILayout.Width(width));
            return;
        }

        // Hide the box's own contents while it's the source of an active
        // drag — the data hasn't moved yet, but showing the item in both
        // its source box and following the cursor at once reads as a
        // duplicate, not a drag.
        bool isDragSource = isDragging && dragSource == inventory && dragItem == slot.item && dragEquipment == slot.equipment;

        var content = slot.item.icon != null
            ? new GUIContent(string.Empty, slot.item.itemName)
            : new GUIContent(isDragSource ? "" : slot.item.itemName + (slot.count > 1 ? $" x{slot.count}" : ""));

        // Tier-colored text on the box itself (no icon case) — icon case
        // keeps its name in the tooltip only, colored via the border below
        // instead since GUI.tooltip is plain text and can't carry a style.
        var boxStyle = slot.item.icon != null ? DebugGUI.Slot : DebugGUI.SlotForTier(slot.item.tier);
        GUILayout.Box(content, boxStyle, GUILayout.Width(width), GUILayout.Height(height));
        var rect = GUILayoutUtility.GetLastRect();

        if (slot.item.icon != null && !isDragSource)
        {
            const float iconPadding = 6f;
            var iconRect = new Rect(rect.x + iconPadding, rect.y + iconPadding, rect.width - iconPadding * 2f, rect.height - iconPadding * 2f);
            GUI.DrawTexture(iconRect, slot.item.icon.texture, ScaleMode.ScaleToFit);
        }

        if (!isDragSource)
            GUI.DrawTexture(rect, DebugGUI.TierBorder(slot.item.tier));

        HandleSlotEvents(rect, inventory, slot);
        RegisterDropZone(rect, inventory, equipSlotName);

        // A Canteen shows its fill status here instead of a QTY count (same
        // format as its old Equipment-row label) — Ben's request, so a
        // Canteen clipped to a Belt point reads the same way as one sitting
        // directly in an equip slot. Otherwise blank for a non-stackable
        // item (maxStack <= 1) rather than always showing "QTY: 1".
        string qtyLabel = isDragSource ? "" : slot.equipment is Canteen canteenEntry
            ? (canteenEntry.IsEmpty ? "Empty" : $"{canteenEntry.Liquid} {canteenEntry.Amount:F0}/{canteenEntry.Capacity:F0}")
            : slot.item.maxStack > 1 ? $"QTY: {slot.count}" : "";
        GUILayout.Label(qtyLabel, DebugGUI.Label, GUILayout.Width(width));
    }

    // Draws inventory's full capacity as a wrapped grid of boxes (occupied
    // and empty). Shared by the main inventory, a worn container's
    // contents, and a nearby StorageBox's.
    private void DrawInventoryGrid(Inventory inventory, string equipSlotName, float boxWidth, float boxHeight)
    {
        var contents = inventory.Slots;
        int capacity = inventory.Capacity;

        int drawn = 0;
        while (drawn < capacity)
        {
            GUILayout.BeginHorizontal();
            for (int col = 0; col < SubBoxesPerRow && drawn < capacity; col++, drawn++)
            {
                GUILayout.BeginVertical(GUILayout.Width(boxWidth));
                DrawSlotBox(inventory, drawn < contents.Count ? contents[drawn] : null, equipSlotName, boxWidth, boxHeight);
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }
    }

    // Runs once per OnGUI dispatch, after DrawContent has rebuilt this
    // frame's dropZones. Promotes a drag candidate to an actual drag once
    // it's moved past DragThreshold, and on MouseUp either resolves a drop
    // (dragging) or opens the action menu (a plain click that never moved).
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
        else
        {
            pendingActionItem = dragItem;
            pendingActionSource = dragSource;
            pendingActionEquipment = dragEquipment;
        }

        dragCandidate = false;
        isDragging = false;
        e.Use();
    }

    // Resolves an actual drop against the zone the mouse was released
    // over. Nothing is ever mutated until this succeeds, so an invalid
    // drop (wrong item type for a restricted slot, wrong body slot for
    // this equipment type, target full) needs no rollback — the source box
    // simply renders normally again next frame, i.e. "snaps back" for
    // free.
    private void TryDrop(DropZone zone)
    {
        if (dragItem == null || dragSource == null || zone.Inventory == null) return;
        if (zone.Inventory == dragSource) return;

        if (zone.EquipSlotName == null)
        {
            bool wasEquipment = dragEquipment != null;
            int moved = InventoryTransfer.MoveAsManyAsFit(dragSource, zone.Inventory, dragItem, dragQuantity);
            // InventoryTransfer.Move only ever moves data — it doesn't know
            // about "carried" visual state, so an equipped item landing in
            // inert storage (e.g. a worn Boot dragged back into the
            // backpack) needs its physical object explicitly hidden here.
            if (moved > 0 && wasEquipment)
                dragEquipment.Stash();
            return;
        }

        if (dragEquipment == null)
        {
            // Hands are the one "carried" zone a plain item can land in —
            // they're just capacity-1 Inventories like any other, unlike
            // the true worn slots (Head, Face, ...) which only make sense
            // for an equippable instance.
            if (zone.EquipSlotName == "Left Hand" || zone.EquipSlotName == "Right Hand")
                InventoryTransfer.MoveAsManyAsFit(dragSource, zone.Inventory, dragItem, dragQuantity);
            return;
        }

        if (!dragEquipment.CanEquipToSlot(zone.EquipSlotName)) return;

        EquipToSlotDispatch(dragEquipment, zone.EquipSlotName, dragSource);
    }

    private const float AutoScrollMargin = 40f;
    private const float AutoScrollSpeed = 500f;

    // Scrolls the main scroll view while the cursor sits within
    // AutoScrollMargin of its top/bottom edge during an active drag. Only
    // called while isDragging (see DrawContent) — no effect otherwise.
    private void HandleAutoScroll(Rect scrollViewRect)
    {
        if (scrollViewRect.height <= 0f) return;

        float mouseY = Event.current.mousePosition.y;
        float delta = AutoScrollSpeed * Time.unscaledDeltaTime;

        if (mouseY >= scrollViewRect.y && mouseY < scrollViewRect.y + AutoScrollMargin)
            scrollPos.y -= delta;
        else if (mouseY <= scrollViewRect.yMax && mouseY > scrollViewRect.yMax - AutoScrollMargin)
            scrollPos.y += delta;

        scrollPos.y = Mathf.Max(0f, scrollPos.y);
    }

    // Floating icon (or text chip, for an item with no icon set) following
    // the cursor while isDragging is true. Drawn from DrawPopups(), same
    // reasoning as DrawTooltip() below — absolute screen position, on top
    // of everything, unclipped by the scroll view.
    private void DrawDragGhost()
    {
        if (!isDragging || dragItem == null) return;

        var mousePos = Event.current.mousePosition;

        if (dragItem.icon != null)
        {
            var rect = new Rect(mousePos.x - SubBoxWidth / 2f, mousePos.y - SubBoxHeight / 2f, SubBoxWidth, SubBoxHeight);
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

    // Fixed header row (outside the scroll view) — 5 equal-width coin
    // boxes spanning 90% of the panel's width, centered, with a label
    // above each. Clicking a box opens the drop-quantity popup for that
    // coin type (see DrawCoinDropPopup).
    private void DrawCurrencySection()
    {
        if (currency == null) return;

        float totalWidth = PanelWidth * CoinRowWidthFraction;
        float boxWidth = (totalWidth - CoinGap * (CoinDisplayOrder.Length - 1)) / CoinDisplayOrder.Length;
        float sideMargin = (PanelWidth - totalWidth) / 2f;

        GUILayout.BeginHorizontal();
        GUILayout.Space(sideMargin);

        for (int i = 0; i < CoinDisplayOrder.Length; i++)
        {
            var (label, type) = CoinDisplayOrder[i];

            GUILayout.BeginVertical(GUILayout.Width(boxWidth));
            GUILayout.Label(label, DebugGUI.Header, GUILayout.Width(boxWidth));
            if (GUILayout.Button(currency.GetBalance(type).ToString(), GUILayout.Width(boxWidth), GUILayout.Height(CoinBoxHeight)))
            {
                pendingDropCoinType = type;
                pendingDropAmount = 0;
            }
            GUILayout.EndVertical();

            if (i < CoinDisplayOrder.Length - 1)
                GUILayout.Space(CoinGap);
        }

        GUILayout.Space(sideMargin);
        GUILayout.EndHorizontal();
    }

    // Quantity picker for dropping coins, opened by clicking a coin box.
    // Stepper buttons (+/- 1 and 10) plus "All" rather than a slider —
    // matches this screen's existing button-only popups, and gives exact
    // control that a slider wouldn't at a 250-coin scale.
    private void DrawCoinDropPopup()
    {
        if (pendingDropCoinType == null) return;

        if (currency == null || coinDropper == null)
        {
            pendingDropCoinType = null;
            return;
        }

        CoinType type = pendingDropCoinType.Value;
        int balance = currency.GetBalance(type);
        pendingDropAmount = Mathf.Clamp(pendingDropAmount, 0, balance);

        const float width = 260f;
        const float height = 190f;
        var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);

        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Label($"Drop {type} Coins", DebugGUI.Header);
        GUILayout.Label($"Balance: {balance}", DebugGUI.Label);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-10", GUILayout.Width(45))) pendingDropAmount -= 10;
        if (GUILayout.Button("-1", GUILayout.Width(35))) pendingDropAmount -= 1;
        GUILayout.Label(pendingDropAmount.ToString(), DebugGUI.Header, GUILayout.Width(50));
        if (GUILayout.Button("+1", GUILayout.Width(35))) pendingDropAmount += 1;
        if (GUILayout.Button("+10", GUILayout.Width(45))) pendingDropAmount += 10;
        GUILayout.EndHorizontal();

        if (GUILayout.Button("All"))
            pendingDropAmount = balance;

        pendingDropAmount = Mathf.Clamp(pendingDropAmount, 0, balance);

        bool resolved = false;

        GUILayout.BeginHorizontal();
        GUI.enabled = pendingDropAmount > 0;
        if (GUILayout.Button("Drop"))
        {
            coinDropper.DropCoins(type, pendingDropAmount);
            resolved = true;
        }
        GUI.enabled = true;
        if (GUILayout.Button("Cancel"))
            resolved = true;
        GUILayout.EndHorizontal();

        GUILayout.EndArea();

        if (resolved)
        {
            pendingDropCoinType = null;
            pendingDropAmount = 0;
        }
    }

    // Quantity picker for dropping a plain stackable item, opened by
    // clicking Drop (main inventory list, or the move popup) instead of
    // dropping the whole stack immediately. Same stepper shape as
    // DrawCoinDropPopup, but defaults to the full count already held —
    // unlike coins, "drop everything" is the common case for items, so
    // starting full and decrementing (rather than starting at 0 and
    // building up) keeps that a one-extra-click flow instead of a
    // regression, while still making "drop just one" straightforward.
    private void DrawItemDropPopup()
    {
        if (pendingDropItem == null || pendingDropItemSource == null) return;

        int available = pendingDropItemSource.GetCount(pendingDropItem);
        if (available <= 0)
        {
            pendingDropItem = null;
            pendingDropItemSource = null;
            return;
        }
        pendingDropItemAmount = Mathf.Clamp(pendingDropItemAmount, 0, available);

        const float width = 260f;
        const float height = 190f;
        var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);

        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Label($"Drop {pendingDropItem.itemName}", DebugGUI.Header);
        GUILayout.Label($"Have: {available}", DebugGUI.Label);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-10", GUILayout.Width(45))) pendingDropItemAmount -= 10;
        if (GUILayout.Button("-1", GUILayout.Width(35))) pendingDropItemAmount -= 1;
        GUILayout.Label(pendingDropItemAmount.ToString(), DebugGUI.Header, GUILayout.Width(50));
        if (GUILayout.Button("+1", GUILayout.Width(35))) pendingDropItemAmount += 1;
        if (GUILayout.Button("+10", GUILayout.Width(45))) pendingDropItemAmount += 10;
        GUILayout.EndHorizontal();

        if (GUILayout.Button("All"))
            pendingDropItemAmount = available;

        pendingDropItemAmount = Mathf.Clamp(pendingDropItemAmount, 0, available);

        bool itemResolved = false;

        GUILayout.BeginHorizontal();
        GUI.enabled = pendingDropItemAmount > 0;
        if (GUILayout.Button("Drop"))
        {
            dropping?.DropFrom(pendingDropItemSource, pendingDropItem, pendingDropItemAmount);
            itemResolved = true;
        }
        GUI.enabled = true;
        if (GUILayout.Button("Cancel"))
            itemResolved = true;
        GUILayout.EndHorizontal();

        GUILayout.EndArea();

        if (itemResolved)
        {
            pendingDropItem = null;
            pendingDropItemSource = null;
            pendingDropItemAmount = 0;
        }
    }

    // Main inventory grid — drag an item out to move it, click it for the
    // action menu. Used to be a list of rows with an Equip/Drop button pair
    // duplicated per equipment type (2026-08-12) — that's now handled
    // uniformly by DrawSlotBox/TryDrop/DrawPendingActions for every grid on
    // this screen, main inventory included.
    private void DrawInventorySection()
    {
        GUILayout.Label("Inventory (drag to move, click for actions)", DebugGUI.Header);
        DrawInventoryGrid(playerInventory.Inventory, null, SubBoxWidth, SubBoxHeight);
    }

    // Fixed-size framed box showing a bigger icon of whatever's worn in a
    // given container slot (Back or Waist), centered directly under that
    // container's own "Inventory" header — drawn as its own row, nothing
    // at all (no row, no space) when there's no icon to show, rather than
    // an empty frame. Uses the default GUILayout.Box skin (same
    // visibly-bordered look as every other slot box on this screen, e.g.
    // the "Empty" boxes) rather than DebugGUI's near-black full-screen
    // panel overlay — that one only reads clearly sitting on top of an
    // already-dark panel; standing alone against the 3D game view behind
    // it, it was nearly invisible.
    private void DrawContainerPreview(Sprite icon)
    {
        if (icon == null) return;

        // No self-centering here — DrawContent() already wraps this call
        // (plus DrawContainerContents beside it) in one FlexibleSpace-
        // centered row, so centering it again internally would fight that.
        GUILayout.Box(GUIContent.none, GUILayout.Width(ContainerPreviewSize), GUILayout.Height(ContainerPreviewSize));
        var rect = GUILayoutUtility.GetLastRect();

        var iconRect = new Rect(
            rect.x + ContainerPreviewPadding, rect.y + ContainerPreviewPadding,
            rect.width - ContainerPreviewPadding * 2f, rect.height - ContainerPreviewPadding * 2f);
        GUI.DrawTexture(iconRect, icon.texture, ScaleMode.ScaleToFit);
    }

    // previewIcon (a separately-baked, higher-resolution image) is
    // preferred over icon here — icon is only baked ~32x32 for inline-row
    // use, and stretching that up to fill this much bigger box looks
    // visibly blurry.
    private Sprite GetSlotPreviewIcon(string slotName)
    {
        var slot = equipment.GetSlot(slotName);
        if (slot == null || slot.Slots.Count == 0) return null;
        var item = slot.Slots[0].item;
        return item.previewIcon != null ? item.previewIcon : item.icon;
    }

    // One row of the "Inventory" side panel — a preview icon (keyed by
    // equip slot name, for GetSlotPreviewIcon) plus a caption and the
    // actual Inventory to render as a contents grid.
    private struct WornContentsRow
    {
        public string PreviewSlotName;
        public string Caption;
        public Inventory Inventory;
        // Null for inert storage (a Backpack's or Boot slot's contents);
        // the "Belt" sentinel for the worn Belt's own attachment points,
        // where dropping an item is actually equipping it, not just
        // stashing it (2026-08-12) — see DropZone/TryDrop.
        public string EquipSlotName;
    }

    // Non-drawing lookup of every worn container's contents — needed
    // before the slot list draws, to know how many contents panels to lay
    // out beside it. Backpack/Belt (Back/Waist) each contribute at most
    // one row, keyed off IInventoryHolder.Inventory — unchanged logic from
    // before Boot existed. Chest (Shirt, 2026-08-12) is the same shape as
    // Back/Waist — just add the slot name here, everything else about
    // rendering a worn IInventoryHolder's contents already generalizes.
    // Boot (Feet) is different on purpose: unlike Backpack/Belt/Shirt's
    // single homogenous cargo pool, a Boot can have multiple
    // independently-typed named slots (a Knife Sheath AND a Pistol
    // Holster), so it deliberately doesn't implement IInventoryHolder — it
    // contributes one row per configured slot instead, enumerated directly
    // off the equipped Boot.
    private List<WornContentsRow> GetWornContainers()
    {
        var result = new List<WornContentsRow>();

        foreach (var slotName in new[] { "Back", "Waist", "Chest", "Leg" })
        {
            var slotInventory = equipment.GetSlot(slotName);
            if (slotInventory == null) continue;

            foreach (var entry in slotInventory.Slots)
            {
                if (entry.equipment is IInventoryHolder holder)
                {
                    result.Add(new WornContentsRow
                    {
                        PreviewSlotName = slotName,
                        Caption = $"{holder.DisplayName} contents (drag to move, click for actions)",
                        Inventory = holder.Inventory,
                        EquipSlotName = slotName == "Waist" ? "Belt" : null,
                    });
                    break;
                }
            }
        }

        var boot = bootCarrier != null ? bootCarrier.Equipped : null;
        if (boot != null)
        {
            foreach (var label in boot.SlotNames)
            {
                result.Add(new WornContentsRow
                {
                    PreviewSlotName = "Feet",
                    Caption = $"{boot.DisplayName} — {label} (drag to move, click for actions)",
                    Inventory = boot.GetSlot(label),
                    EquipSlotName = null,
                });
            }
        }

        return result;
    }

    // Draws the equipment slot list (Head/Face/.../Back/...). Each box is
    // drag source + drop target (EquipSlotName = the real PlayerEquipment
    // slot name, e.g. "Head", "Left Hand") like every other grid on this
    // screen — used to be ~230 lines of duplicated per-type Equip/Unequip/
    // Drop button branches (2026-08-12); DrawSlotBox/TryDrop/
    // DrawPendingActions now handle all 8 equipment types uniformly.
    private void DrawEquipmentSection()
    {
        foreach (var slotName in SlotOrder)
        {
            var slotInventory = equipment.GetSlot(slotName);
            if (slotInventory == null) continue;

            GUILayout.BeginHorizontal();
            GUILayout.Label(slotName, DebugGUI.Label, GUILayout.Width(LabelWidth));

            var occupied = slotInventory.Slots;
            for (int i = 0; i < slotInventory.Capacity; i++)
            {
                GUILayout.BeginVertical(GUILayout.Width(BoxWidth));
                DrawSlotBox(slotInventory, i < occupied.Count ? occupied[i] : null, slotName, BoxWidth, BoxHeight, showEmptyLabel: true);
                GUILayout.EndVertical();
            }

            GUILayout.EndHorizontal();
        }
    }

    // Header caption plus a wrapped grid of boxes (DrawInventoryGrid) for
    // inventory's own capacity. Shared by a worn container's contents and a
    // nearby StorageBox's. equipSlotName is null for ordinary inert
    // storage, or the "Belt" sentinel for the worn Belt's own attachment
    // points (see WornContentsRow).
    private void DrawContainerContents(Inventory inventory, string caption, string equipSlotName)
    {
        GUILayout.Label($"    {caption}:", DebugGUI.Label);
        DrawInventoryGrid(inventory, equipSlotName, SubBoxWidth, SubBoxHeight);
    }

    // Wraps a label with an item's icon (ItemDefinition.icon) when it has
    // one, so every place an item renders — inventory rows, equipment
    // slots, container grids, the move popup — picks it up automatically
    // instead of needing its own icon-drawing logic. Most items still have
    // no icon set, so this is a no-op text-only GUIContent for them, same
    // as before the field existed.
    private static GUIContent ItemContent(ItemDefinition item, string text)
    {
        return item.icon != null ? new GUIContent(text, item.icon.texture) : new GUIContent(text);
    }
}
