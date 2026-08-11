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
    private PlayerBackpack backpackCarrier;
    private PlayerBelt beltCarrier;
    private PlayerBoot bootCarrier;
    private PlayerCanteen canteenCarrier;
    private PlayerNavComputer navComputerCarrier;
    private PlayerHealthMonitor healthMonitorCarrier;
    private PlayerSunglasses sunglassesCarrier;
    private PlayerMiningFaceShield miningShieldCarrier;
    private PlayerCurrency currency;
    private PlayerCoinDrop coinDropper;
    private PlayerVitals vitals;
    private Vector2 scrollPos;

    // Recomputed once per DrawContent() call (see FindNearbyStorageBoxes) —
    // every StorageBox within storageRange, nearest first. Read by the
    // inventory section (for the "To Storage" button), the storage section
    // itself (shows the nearest one's contents), and the move popup's
    // storage picker (lets the player choose by name when more than one is
    // in range).
    private readonly List<StorageBox> nearbyStorages = new List<StorageBox>();

    // Set when the player clicks any item box in the Equipment section —
    // whether inside a container's contents grid, or a plain item sitting
    // directly in an equip slot (e.g. a hand). Rather than acting
    // immediately, opens a popup asking where it should go
    // (Drop / a hand / the main inventory).
    private ItemDefinition pendingMoveItem;
    private Inventory pendingMoveSource;
    // The specific equipment instance behind pendingMoveItem, when the
    // clicked slot held one (a Canteen, Backpack, etc.) -- null for a plain
    // stackable item. Needed for actions that operate on the physical
    // instance rather than the item type/count (Drink/Fill a container-held
    // Canteen, same idea as TryEatFrom but Drink/Fill mutate the instance
    // directly instead of consuming a stack). Must be kept in sync with
    // pendingMoveItem/pendingMoveSource at every assignment site, including
    // explicitly clearing it to null where the source is a plain item, or a
    // stale equipment reference could leak into an unrelated popup.
    private IEquippable pendingMoveEquipment;

    // True while the move popup is showing the storage picker (a named
    // list of nearbyStorages) instead of its normal destination list —
    // entered by clicking "To Storage".
    private bool choosingStorage;

    // Set when the player clicks a coin box in the currency row — opens a
    // popup to pick how many of that type to drop.
    private CoinType? pendingDropCoinType;
    private int pendingDropAmount;

    // Set when the player clicks Drop on a plain stackable item (main
    // inventory list, or the move popup) — opens a quantity picker instead
    // of always dropping the entire stack. Real gap found in playtesting
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
        backpackCarrier = GetComponent<PlayerBackpack>();
        beltCarrier = GetComponent<PlayerBelt>();
        bootCarrier = GetComponent<PlayerBoot>();
        canteenCarrier = GetComponent<PlayerCanteen>();
        navComputerCarrier = GetComponent<PlayerNavComputer>();
        healthMonitorCarrier = GetComponent<PlayerHealthMonitor>();
        sunglassesCarrier = GetComponent<PlayerSunglasses>();
        miningShieldCarrier = GetComponent<PlayerMiningFaceShield>();
        currency = GetComponent<PlayerCurrency>();
        coinDropper = GetComponent<PlayerCoinDrop>();
        vitals = GetComponent<PlayerVitals>();
    }

    // Called by PlayerMenuScreen while its Inventory tab is active.
    public void DrawContent()
    {
        StorageBox.FindNearby(transform.position, storageRange, nearbyStorages);

        DrawCurrencySection();
        GUILayout.Space(10);

        float scrollHeight = Mathf.Min(Screen.height - ChromeReserve - CurrencySectionHeight, 640f);
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(scrollHeight));

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
                    DrawContainerContents(wornContainers[j].Inventory, wornContainers[j].Caption);
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

        if (nearbyStorages.Count > 0)
        {
            var nearest = nearbyStorages[0];
            GUILayout.Space(10);
            GUILayout.Label($"{nearest.DisplayName} (nearby)", DebugGUI.Header);
            DrawContainerContents(nearest.Inventory, "click an item for options");
        }

        GUILayout.EndScrollView();
    }

    // Called by PlayerMenuScreen right after ending its own full-screen
    // BeginArea, only while the Inventory tab is active — these are
    // absolutely-positioned popups (screen-centered) that need to sit on
    // top of, not nested inside, the tab content area.
    public void DrawPopups()
    {
        DrawPendingMovePopup();
        DrawPendingEquipPopup();
        DrawCoinDropPopup();
        DrawItemDropPopup();
        DrawTooltip();
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
    // still-open "where should this go?" or coin-drop popup doesn't stay
    // stuck open the next time the menu is reopened.
    public void ResetPopups()
    {
        pendingMoveItem = null;
        pendingMoveSource = null;
        pendingMoveEquipment = null;
        choosingStorage = false;
        pendingEquipDestinations = null;
        pendingEquipChoose = null;
        pendingEquipLabel = null;
        pendingDropCoinType = null;
        pendingDropItem = null;
        pendingDropItemSource = null;
    }

    // Small "where should this go?" dialog shown after clicking an item
    // inside a container's contents grid. Drawn last so it sits on top.
    // "To Storage" switches it into a second mode (choosingStorage) listing
    // each nearby box by name instead of moving immediately, since more
    // than one can be in range at once.
    private void DrawPendingMovePopup()
    {
        if (pendingMoveItem == null || pendingMoveSource == null) return;

        const float width = 220f;
        // Was 300f, then 360f -- bumped again for the Canteen Drink/Fill
        // buttons (DrawMoveDestinations), up to 2 more on top of the Boot's
        // per-slot buttons if a Canteen happens to be selected while
        // Military Boots (2 slots) are worn -- both sets can show at once.
        float height = choosingStorage
            ? 70f + Mathf.Max(nearbyStorages.Count, 1) * 26f
            : 420f;
        var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);

        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Label(ItemContent(pendingMoveItem, pendingMoveItem.itemName), DebugGUI.Header);

        bool resolved = choosingStorage ? DrawStoragePicker() : DrawMoveDestinations();

        GUILayout.EndArea();

        if (resolved)
        {
            pendingMoveItem = null;
            pendingMoveSource = null;
            pendingMoveEquipment = null;
            choosingStorage = false;
        }
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
    private void TryEquipWithChoice(Canteen canteen)
    {
        var destinations = canteenCarrier.AvailableDestinations(canteen);
        if (destinations.Count <= 1)
        {
            if (destinations.Count == 1) canteenCarrier.EquipTo(canteen, destinations[0]);
            return;
        }

        pendingEquipDestinations = destinations;
        pendingEquipLabel = canteen.DisplayName;
        pendingEquipChoose = destination => canteenCarrier.EquipTo(canteen, destination);
    }

    private void TryEquipWithChoice(NavigationComputer navComputer)
    {
        var destinations = navComputerCarrier.AvailableDestinations(navComputer);
        if (destinations.Count <= 1)
        {
            if (destinations.Count == 1) navComputerCarrier.EquipTo(navComputer, destinations[0]);
            return;
        }

        pendingEquipDestinations = destinations;
        pendingEquipLabel = navComputer.DisplayName;
        pendingEquipChoose = destination => navComputerCarrier.EquipTo(navComputer, destination);
    }

    private void TryEquipWithChoice(PersonalHealthMonitor monitor)
    {
        var destinations = healthMonitorCarrier.AvailableDestinations(monitor);
        if (destinations.Count <= 1)
        {
            if (destinations.Count == 1) healthMonitorCarrier.EquipTo(monitor, destinations[0]);
            return;
        }

        pendingEquipDestinations = destinations;
        pendingEquipLabel = monitor.DisplayName;
        pendingEquipChoose = destination => healthMonitorCarrier.EquipTo(monitor, destination);
    }

    // Normal destination list. Returns true once the popup should close.
    private bool DrawMoveDestinations()
    {
        // Real gap found in playtesting (2026-08-09): Eat only ever showed
        // in the main inventory list (DrawInventorySection) — an item
        // sitting in a hand slot, backpack, or storage box (this popup)
        // had no way to eat it at all without first moving it back to the
        // main inventory.
        var edible = eating != null ? eating.FindEdible(pendingMoveItem) : null;
        if (edible != null && GUILayout.Button(edible.verb))
        {
            eating.TryEatFrom(pendingMoveSource, pendingMoveItem);
            return true;
        }

        // Same gap, same fix, for Medicine (2026-08-10) — Apply only
        // showing in the main inventory list would leave a Healing Paste
        // sitting in a hand/backpack/container with no way to use it.
        var medicineItem = medicine != null ? medicine.FindMedicine(pendingMoveItem) : null;
        if (medicineItem != null && GUILayout.Button(medicineItem.verb))
        {
            medicine.TryApplyFrom(pendingMoveSource, pendingMoveItem);
            return true;
        }

        // Same gap, same fix, for a Canteen (2026-08-11) — Drink/Fill only
        // ever showed for a Canteen sitting directly in an equip slot
        // (DrawEquipmentSection's canteenHere branch); one sitting in a
        // backpack/storage box had no way to drink or refill it in place.
        // Unlike Eat/Apply, this acts on the physical Canteen instance
        // directly (pendingMoveEquipment) rather than consuming an item
        // count from pendingMoveSource — Drink/Fill don't remove the
        // canteen from its slot, they just change what's inside it.
        if (pendingMoveEquipment is Canteen canteen)
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

        if (GUILayout.Button("Drop"))
        {
            pendingDropItem = pendingMoveItem;
            pendingDropItemSource = pendingMoveSource;
            pendingDropItemAmount = pendingMoveSource.GetCount(pendingMoveItem);
            return true;
        }

        // MoveAsManyAsFit, not Move — a destination with less room than
        // the source's full count (e.g. two non-stacking Hammers, each
        // their own slot since maxStack is 1, into an empty
        // single-capacity hand) used to fail outright instead of moving
        // what actually fits (real bug found in playtesting, 2026-08-09).
        var leftHand = equipment.GetSlot("Left Hand");
        if (leftHand != null && leftHand != pendingMoveSource && GUILayout.Button("To Left Hand"))
        {
            InventoryTransfer.MoveAsManyAsFit(pendingMoveSource, leftHand, pendingMoveItem);
            return true;
        }

        var rightHand = equipment.GetSlot("Right Hand");
        if (rightHand != null && rightHand != pendingMoveSource && GUILayout.Button("To Right Hand"))
        {
            InventoryTransfer.MoveAsManyAsFit(pendingMoveSource, rightHand, pendingMoveItem);
            return true;
        }

        var equippedBackpack = backpackCarrier != null ? backpackCarrier.Equipped : null;
        if (equippedBackpack != null && equippedBackpack.Inventory != pendingMoveSource && GUILayout.Button("To Backpack"))
        {
            InventoryTransfer.MoveAsManyAsFit(pendingMoveSource, equippedBackpack.Inventory, pendingMoveItem);
            return true;
        }

        // A Boot's own named slots (Knife Sheath, Pistol Holster) are each
        // their own restricted Inventory, not one general cargo pool like
        // Backpack — so this offers one button per configured slot rather
        // than a single "To Boot". Restriction itself is enforced inside
        // MoveAsManyAsFit/AddItem (Inventory.restrictedTo), not here —
        // the button always shows if a slot exists so its presence doesn't
        // leak which items are allowed; trying to move a disallowed item
        // just silently moves nothing.
        var equippedBoot = bootCarrier != null ? bootCarrier.Equipped : null;
        if (equippedBoot != null)
        {
            foreach (var label in equippedBoot.SlotNames)
            {
                var bootSlot = equippedBoot.GetSlot(label);
                if (bootSlot == pendingMoveSource) continue;

                if (GUILayout.Button($"To {label}"))
                {
                    InventoryTransfer.MoveAsManyAsFit(pendingMoveSource, bootSlot, pendingMoveItem);
                    return true;
                }
            }
        }

        if (playerInventory.Inventory != pendingMoveSource && GUILayout.Button("To Inventory"))
        {
            InventoryTransfer.MoveAsManyAsFit(pendingMoveSource, playerInventory.Inventory, pendingMoveItem);
            return true;
        }

        if (nearbyStorages.Exists(box => box.Inventory != pendingMoveSource) && GUILayout.Button("To Storage"))
        {
            choosingStorage = true;
            return false;
        }

        return GUILayout.Button("Cancel");
    }

    // Named list of every StorageBox currently in range. Returns true once
    // the popup should close (a box was picked, or Cancel).
    private bool DrawStoragePicker()
    {
        foreach (var box in nearbyStorages)
        {
            if (box.Inventory == pendingMoveSource) continue;

            if (GUILayout.Button(box.DisplayName))
            {
                InventoryTransfer.MoveAsManyAsFit(pendingMoveSource, box.Inventory, pendingMoveItem);
                return true;
            }
        }

        if (GUILayout.Button("Back"))
        {
            choosingStorage = false;
            return false;
        }

        return GUILayout.Button("Cancel");
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

    // Ported from the old always-on PlayerInventory panel.
    private void DrawInventorySection()
    {
        ItemDefinition dropClicked = null;
        ItemDefinition packClicked = null;
        ItemDefinition eatClicked = null;
        ItemDefinition applyClicked = null;
        ItemDefinition leftHandClicked = null;
        ItemDefinition rightHandClicked = null;
        Backpack equipClicked = null;
        Backpack backpackDropClicked = null;
        Belt beltEquipClicked = null;
        Belt beltDropClicked = null;
        Boot bootEquipClicked = null;
        Boot bootDropClicked = null;
        Canteen canteenEquipClicked = null;
        Canteen canteenDropClicked = null;
        NavigationComputer navComputerEquipClicked = null;
        NavigationComputer navComputerDropClicked = null;
        PersonalHealthMonitor healthMonitorEquipClicked = null;
        PersonalHealthMonitor healthMonitorDropClicked = null;
        Sunglasses sunglassesEquipClicked = null;
        Sunglasses sunglassesDropClicked = null;
        MiningFaceShield miningShieldEquipClicked = null;
        MiningFaceShield miningShieldDropClicked = null;
        var equippedBackpack = backpackCarrier != null ? backpackCarrier.Equipped : null;

        var inv = playerInventory.Inventory;
        var slots = inv.Slots;
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            string label = $"{slot.item.itemName} x{slot.count}";
            var content = ItemContent(slot.item, label);

            GUILayout.BeginHorizontal();

            if (slot.equipment is Backpack backpack)
            {
                GUILayout.Label(content, DebugGUI.Label);
                if (SafeButton("Equip", GUILayout.Width(55)))
                    equipClicked = backpack;
                if (SafeButton("Drop", GUILayout.Width(50)))
                    backpackDropClicked = backpack;
            }
            else if (slot.equipment is Belt belt)
            {
                GUILayout.Label(content, DebugGUI.Label);
                if (SafeButton("Equip", GUILayout.Width(55)))
                    beltEquipClicked = belt;
                if (SafeButton("Drop", GUILayout.Width(50)))
                    beltDropClicked = belt;
            }
            else if (slot.equipment is Boot boot)
            {
                GUILayout.Label(content, DebugGUI.Label);
                if (SafeButton("Equip", GUILayout.Width(55)))
                    bootEquipClicked = boot;
                if (SafeButton("Drop", GUILayout.Width(50)))
                    bootDropClicked = boot;
            }
            else if (slot.equipment is Canteen canteen)
            {
                GUILayout.Label(content, DebugGUI.Label);
                if (SafeButton("Equip", GUILayout.Width(55)))
                    canteenEquipClicked = canteen;
                if (SafeButton("Drop", GUILayout.Width(50)))
                    canteenDropClicked = canteen;
            }
            else if (slot.equipment is NavigationComputer navComputer)
            {
                GUILayout.Label(content, DebugGUI.Label);
                if (SafeButton("Equip", GUILayout.Width(55)))
                    navComputerEquipClicked = navComputer;
                if (SafeButton("Drop", GUILayout.Width(50)))
                    navComputerDropClicked = navComputer;
            }
            else if (slot.equipment is PersonalHealthMonitor healthMonitor)
            {
                GUILayout.Label(content, DebugGUI.Label);
                if (SafeButton("Equip", GUILayout.Width(55)))
                    healthMonitorEquipClicked = healthMonitor;
                if (SafeButton("Drop", GUILayout.Width(50)))
                    healthMonitorDropClicked = healthMonitor;
            }
            else if (slot.equipment is Sunglasses sunglasses)
            {
                GUILayout.Label(content, DebugGUI.Label);
                if (SafeButton("Equip", GUILayout.Width(55)))
                    sunglassesEquipClicked = sunglasses;
                if (SafeButton("Drop", GUILayout.Width(50)))
                    sunglassesDropClicked = sunglasses;
            }
            else if (slot.equipment is MiningFaceShield miningShield)
            {
                GUILayout.Label(content, DebugGUI.Label);
                if (SafeButton("Equip", GUILayout.Width(55)))
                    miningShieldEquipClicked = miningShield;
                if (SafeButton("Drop", GUILayout.Width(50)))
                    miningShieldDropClicked = miningShield;
            }
            else
            {
                GUILayout.Label(content, DebugGUI.Label);

                var edible = eating != null ? eating.FindEdible(slot.item) : null;
                if (edible != null && GUILayout.Button(edible.verb, GUILayout.Width(50)))
                    eatClicked = slot.item;

                var medicineItem = medicine != null ? medicine.FindMedicine(slot.item) : null;
                if (medicineItem != null && GUILayout.Button(medicineItem.verb, GUILayout.Width(50)))
                    applyClicked = slot.item;

                if (dropping != null && SafeButton("Drop", GUILayout.Width(50)))
                    dropClicked = slot.item;

                // Real gap found in playtesting (2026-08-09): a plain item
                // sitting directly in the main inventory (e.g. a freshly
                // crafted Pickaxe — PlayerCrafting.AddCraftedOutput sends
                // plain output straight here, not to a backpack) had no way
                // to reach a hand at all. Backpack/Storage contents already
                // had this via the click-to-open move popup
                // (DrawContainerContents); the main list never did.
                var leftHandSlot = equipment.GetSlot("Left Hand");
                if (leftHandSlot != null && GUILayout.Button("To L Hand", GUILayout.Width(70)))
                    leftHandClicked = slot.item;

                var rightHandSlot = equipment.GetSlot("Right Hand");
                if (rightHandSlot != null && GUILayout.Button("To R Hand", GUILayout.Width(70)))
                    rightHandClicked = slot.item;

                if (equippedBackpack != null && GUILayout.Button("To Pack", GUILayout.Width(60)))
                    packClicked = slot.item;

                if (nearbyStorages.Count > 0 && GUILayout.Button("To Storage", GUILayout.Width(70)))
                {
                    pendingMoveItem = slot.item;
                    pendingMoveSource = inv;
                    pendingMoveEquipment = null;
                    choosingStorage = true;
                }
            }

            GUILayout.EndHorizontal();
        }

        if (eatClicked != null)
            eating.TryEat(eatClicked);
        if (applyClicked != null)
            medicine.TryApply(applyClicked);
        if (dropClicked != null)
        {
            pendingDropItem = dropClicked;
            pendingDropItemSource = inv;
            pendingDropItemAmount = inv.GetCount(dropClicked);
        }
        if (leftHandClicked != null)
            InventoryTransfer.MoveAsManyAsFit(inv, equipment.GetSlot("Left Hand"), leftHandClicked);
        if (rightHandClicked != null)
            InventoryTransfer.MoveAsManyAsFit(inv, equipment.GetSlot("Right Hand"), rightHandClicked);
        if (packClicked != null)
            InventoryTransfer.MoveAsManyAsFit(inv, equippedBackpack.Inventory, packClicked);
        if (equipClicked != null)
            backpackCarrier.Equip(equipClicked);
        if (backpackDropClicked != null)
            backpackCarrier.Drop(backpackDropClicked);
        if (beltEquipClicked != null)
            beltCarrier.Equip(beltEquipClicked);
        if (beltDropClicked != null)
            beltCarrier.Drop(beltDropClicked);
        if (bootEquipClicked != null)
            bootCarrier.Equip(bootEquipClicked);
        if (bootDropClicked != null)
            bootCarrier.Drop(bootDropClicked);
        if (canteenEquipClicked != null)
            TryEquipWithChoice(canteenEquipClicked);
        if (canteenDropClicked != null)
            canteenCarrier.Drop(canteenDropClicked);
        if (navComputerEquipClicked != null)
            TryEquipWithChoice(navComputerEquipClicked);
        if (navComputerDropClicked != null)
            navComputerCarrier.Drop(navComputerDropClicked);
        if (healthMonitorEquipClicked != null)
            TryEquipWithChoice(healthMonitorEquipClicked);
        if (healthMonitorDropClicked != null)
            healthMonitorCarrier.Drop(healthMonitorDropClicked);
        if (sunglassesEquipClicked != null)
            sunglassesCarrier.Equip(sunglassesEquipClicked);
        if (sunglassesDropClicked != null)
            sunglassesCarrier.Drop(sunglassesDropClicked);
        if (miningShieldEquipClicked != null)
            miningShieldCarrier.Equip(miningShieldEquipClicked);
        if (miningShieldDropClicked != null)
            miningShieldCarrier.Drop(miningShieldDropClicked);
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
    }

    // Non-drawing lookup of every worn container's contents — needed
    // before the slot list draws, to know how many contents panels to lay
    // out beside it. Backpack/Belt (Back/Waist) each contribute at most
    // one row, keyed off IInventoryHolder.Inventory — unchanged logic from
    // before Boot existed. Boot (Feet) is different on purpose: unlike
    // Backpack/Belt's single homogenous cargo pool, a Boot can have
    // multiple independently-typed named slots (a Knife Sheath AND a
    // Pistol Holster), so it deliberately doesn't implement
    // IInventoryHolder — it contributes one row per configured slot
    // instead, enumerated directly off the equipped Boot.
    private List<WornContentsRow> GetWornContainers()
    {
        var result = new List<WornContentsRow>();

        foreach (var slotName in new[] { "Back", "Waist" })
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
                        Caption = $"{holder.DisplayName} contents (click an item for options)",
                        Inventory = holder.Inventory,
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
                    Caption = $"{boot.DisplayName} — {label} (click an item for options)",
                    Inventory = boot.GetSlot(label),
                });
            }
        }

        return result;
    }

    // Draws the equipment slot list (Head/Face/.../Back/...). The worn
    // container's own contents used to render via this method's return
    // value — see GetWornContainer() above for why that moved.
    private void DrawEquipmentSection()
    {
        Backpack backpackEquipClicked = null;
        Backpack backpackUnequipClicked = null;
        Backpack backpackDropClicked = null;
        Belt beltEquipClicked = null;
        Belt beltUnequipClicked = null;
        Belt beltDropClicked = null;
        Boot bootEquipClicked = null;
        Boot bootUnequipClicked = null;
        Boot bootDropClicked = null;
        Canteen canteenUnequipClicked = null;
        Canteen canteenDropClicked = null;
        NavigationComputer navComputerEquipClicked = null;
        NavigationComputer navComputerUnequipClicked = null;
        NavigationComputer navComputerDropClicked = null;
        PersonalHealthMonitor healthMonitorEquipClicked = null;
        PersonalHealthMonitor healthMonitorUnequipClicked = null;
        PersonalHealthMonitor healthMonitorDropClicked = null;
        Sunglasses sunglassesEquipClicked = null;
        Sunglasses sunglassesUnequipClicked = null;
        Sunglasses sunglassesDropClicked = null;
        MiningFaceShield miningShieldEquipClicked = null;
        MiningFaceShield miningShieldUnequipClicked = null;
        MiningFaceShield miningShieldDropClicked = null;

        foreach (var slotName in SlotOrder)
        {
            var slotInventory = equipment.GetSlot(slotName);
            if (slotInventory == null) continue;

            GUILayout.BeginHorizontal();
            GUILayout.Label(slotName, DebugGUI.Label, GUILayout.Width(LabelWidth));

            var occupied = slotInventory.Slots;
            Backpack backpackHere = null;
            Belt beltHere = null;
            Boot bootHere = null;
            Canteen canteenHere = null;
            NavigationComputer navComputerHere = null;
            PersonalHealthMonitor healthMonitorHere = null;
            Sunglasses sunglassesHere = null;
            MiningFaceShield miningShieldHere = null;

            for (int i = 0; i < slotInventory.Capacity; i++)
            {
                if (i < occupied.Count)
                {
                    var entry = occupied[i];

                    // A worn container's own contents render in a side
                    // An item with an icon shows icon-only in this section,
                    // no text — a hand slot shouldn't say "Backpack" next
                    // to its own picture, and a worn container on Back/
                    // Waist shouldn't say "Equipped" either. Items without
                    // an icon keep the old text (name/count normally,
                    // "Equipped" specifically for a worn container — its
                    // own contents render in the side column, see
                    // DrawContent()).
                    bool isWornContainer = (entry.equipment is IInventoryHolder && (slotName == "Back" || slotName == "Waist"))
                        || (entry.equipment is Boot && slotName == "Feet");
                    string label = entry.item.icon != null
                        ? ""
                        : (isWornContainer ? "Equipped" : entry.item.itemName + (entry.count > 1 ? $" x{entry.count}" : ""));
                    var content = ItemContent(entry.item, label);

                    if (entry.equipment == null)
                    {
                        // A plain stackable item sitting directly in an
                        // equip slot (e.g. something picked up into a
                        // hand) — click it to open the same "where should
                        // this go?" popup as backpack contents.
                        if (GUILayout.Button(content, GUILayout.Width(BoxWidth), GUILayout.Height(BoxHeight)))
                        {
                            pendingMoveItem = entry.item;
                            pendingMoveSource = slotInventory;
                            pendingMoveEquipment = null;
                        }
                    }
                    else
                    {
                        GUILayout.Box(content, GUILayout.Width(BoxWidth), GUILayout.Height(BoxHeight));
                    }

                    if (entry.equipment is Backpack bp) backpackHere = bp;
                    if (entry.equipment is Belt bt) beltHere = bt;
                    if (entry.equipment is Boot bo) bootHere = bo;
                    if (entry.equipment is Canteen ct) canteenHere = ct;
                    if (entry.equipment is NavigationComputer nc) navComputerHere = nc;
                    if (entry.equipment is PersonalHealthMonitor phm) healthMonitorHere = phm;
                    if (entry.equipment is Sunglasses sg) sunglassesHere = sg;
                    if (entry.equipment is MiningFaceShield mfs) miningShieldHere = mfs;
                }
                else
                {
                    GUILayout.Box("Empty", GUILayout.Width(BoxWidth), GUILayout.Height(BoxHeight));
                }
            }

            if (backpackHere != null)
            {
                if (slotName == "Back")
                {
                    if (SafeButton("Unequip", GUILayout.Width(70))) backpackUnequipClicked = backpackHere;
                }
                else
                {
                    if (SafeButton("Equip", GUILayout.Width(55))) backpackEquipClicked = backpackHere;
                }

                if (SafeButton("Drop", GUILayout.Width(50))) backpackDropClicked = backpackHere;
            }
            else if (beltHere != null)
            {
                if (slotName == "Waist")
                {
                    if (SafeButton("Unequip", GUILayout.Width(70))) beltUnequipClicked = beltHere;
                }
                else
                {
                    if (SafeButton("Equip", GUILayout.Width(55))) beltEquipClicked = beltHere;
                }

                if (SafeButton("Drop", GUILayout.Width(50))) beltDropClicked = beltHere;
            }
            else if (bootHere != null)
            {
                if (slotName == "Feet")
                {
                    if (SafeButton("Unequip", GUILayout.Width(70))) bootUnequipClicked = bootHere;
                }
                else
                {
                    if (SafeButton("Equip", GUILayout.Width(55))) bootEquipClicked = bootHere;
                }

                if (SafeButton("Drop", GUILayout.Width(50))) bootDropClicked = bootHere;
            }
            else if (canteenHere != null)
            {
                string liquidLabel = canteenHere.IsEmpty
                    ? "Empty"
                    : $"{canteenHere.Liquid} {canteenHere.Amount:F0}/{canteenHere.Capacity:F0}";
                GUILayout.Label(liquidLabel, DebugGUI.Label, GUILayout.Width(90));
                if (GUILayout.Button("Drink", GUILayout.Width(50))) canteenHere.Drink(vitals);
                if (GUILayout.Button("Fill", GUILayout.Width(45))) canteenHere.Fill(LiquidType.Water);
                if (SafeButton("Unequip", GUILayout.Width(65))) canteenUnequipClicked = canteenHere;
                if (SafeButton("Drop", GUILayout.Width(50))) canteenDropClicked = canteenHere;
            }
            else if (navComputerHere != null)
            {
                bool isWorn = slotName == "Left Wrist" || slotName == "Right Wrist";
                if (isWorn)
                {
                    if (SafeButton("Unequip", GUILayout.Width(70))) navComputerUnequipClicked = navComputerHere;
                }
                else
                {
                    if (SafeButton("Equip", GUILayout.Width(55))) navComputerEquipClicked = navComputerHere;
                }

                if (SafeButton("Drop", GUILayout.Width(50))) navComputerDropClicked = navComputerHere;
            }
            else if (healthMonitorHere != null)
            {
                bool isWorn = slotName == "Left Wrist" || slotName == "Right Wrist";
                if (isWorn)
                {
                    if (SafeButton("Unequip", GUILayout.Width(70))) healthMonitorUnequipClicked = healthMonitorHere;
                }
                else
                {
                    if (SafeButton("Equip", GUILayout.Width(55))) healthMonitorEquipClicked = healthMonitorHere;
                }

                if (SafeButton("Drop", GUILayout.Width(50))) healthMonitorDropClicked = healthMonitorHere;
            }
            else if (sunglassesHere != null)
            {
                if (slotName == "Face")
                {
                    if (SafeButton("Unequip", GUILayout.Width(70))) sunglassesUnequipClicked = sunglassesHere;
                }
                else
                {
                    if (SafeButton("Equip", GUILayout.Width(55))) sunglassesEquipClicked = sunglassesHere;
                }

                if (SafeButton("Drop", GUILayout.Width(50))) sunglassesDropClicked = sunglassesHere;
            }
            else if (miningShieldHere != null)
            {
                if (slotName == "Face")
                {
                    if (SafeButton("Unequip", GUILayout.Width(70))) miningShieldUnequipClicked = miningShieldHere;
                }
                else
                {
                    if (SafeButton("Equip", GUILayout.Width(55))) miningShieldEquipClicked = miningShieldHere;
                }

                if (SafeButton("Drop", GUILayout.Width(50))) miningShieldDropClicked = miningShieldHere;
            }

            GUILayout.EndHorizontal();
        }

        if (backpackEquipClicked != null) backpackCarrier.Equip(backpackEquipClicked);
        if (backpackUnequipClicked != null) backpackCarrier.Unequip(backpackUnequipClicked);
        if (backpackDropClicked != null) backpackCarrier.Drop(backpackDropClicked);
        if (beltEquipClicked != null) beltCarrier.Equip(beltEquipClicked);
        if (beltUnequipClicked != null) beltCarrier.Unequip(beltUnequipClicked);
        if (beltDropClicked != null) beltCarrier.Drop(beltDropClicked);
        if (bootEquipClicked != null) bootCarrier.Equip(bootEquipClicked);
        if (bootUnequipClicked != null) bootCarrier.Unequip(bootUnequipClicked);
        if (bootDropClicked != null) bootCarrier.Drop(bootDropClicked);
        if (canteenUnequipClicked != null) canteenCarrier.Unequip(canteenUnequipClicked);
        if (canteenDropClicked != null) canteenCarrier.Drop(canteenDropClicked);
        if (navComputerEquipClicked != null) TryEquipWithChoice(navComputerEquipClicked);
        if (navComputerUnequipClicked != null) navComputerCarrier.Unequip(navComputerUnequipClicked);
        if (navComputerDropClicked != null) navComputerCarrier.Drop(navComputerDropClicked);
        if (healthMonitorEquipClicked != null) TryEquipWithChoice(healthMonitorEquipClicked);
        if (healthMonitorUnequipClicked != null) healthMonitorCarrier.Unequip(healthMonitorUnequipClicked);
        if (healthMonitorDropClicked != null) healthMonitorCarrier.Drop(healthMonitorDropClicked);
        if (sunglassesEquipClicked != null) sunglassesCarrier.Equip(sunglassesEquipClicked);
        if (sunglassesUnequipClicked != null) sunglassesCarrier.Unequip(sunglassesUnequipClicked);
        if (sunglassesDropClicked != null) sunglassesCarrier.Drop(sunglassesDropClicked);
        if (miningShieldEquipClicked != null) miningShieldCarrier.Equip(miningShieldEquipClicked);
        if (miningShieldUnequipClicked != null) miningShieldCarrier.Unequip(miningShieldUnequipClicked);
        if (miningShieldDropClicked != null) miningShieldCarrier.Drop(miningShieldDropClicked);
    }

    // Draws an inventory's own capacity as a wrapped grid of boxes. Occupied
    // boxes are buttons — clicking one opens the "where should this go?"
    // popup (DrawPendingMovePopup) instead of moving it anywhere directly.
    // Shared by a worn backpack's contents and a nearby StorageBox's.
    private void DrawContainerContents(Inventory inventory, string caption)
    {
        var contents = inventory.Slots;
        int capacity = inventory.Capacity;

        GUILayout.Label($"    {caption}:", DebugGUI.Label);

        int drawn = 0;
        while (drawn < capacity)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            for (int col = 0; col < SubBoxesPerRow && drawn < capacity; col++, drawn++)
            {
                GUILayout.BeginVertical(GUILayout.Width(SubBoxWidth));

                if (drawn < contents.Count)
                {
                    var entry = contents[drawn];
                    bool clicked;

                    if (entry.item.icon != null)
                    {
                        // GUIContent's icon+text combo (still used below for
                        // items with no icon) breaks down at this box's
                        // size — the icon didn't render at all and the text
                        // just truncated ("ill Rock x9"). Drawing the icon
                        // as a separate overlay on top of a plain box,
                        // rather than through GUIContent, sidesteps that
                        // entirely — same technique the Back preview box
                        // already uses successfully. DebugGUI.Slot (an
                        // explicit solid-color background, not
                        // GUI.skin.box's default runtime look) keeps this
                        // visibly readable against the panel behind it.
                        // Tooltip (item name) makes up for this slot no
                        // longer showing any text of its own now that it's
                        // icon-only — Ben's request.
                        var iconContent = new GUIContent(string.Empty, entry.item.itemName);
                        clicked = GUILayout.Button(iconContent, DebugGUI.Slot, GUILayout.Width(SubBoxWidth), GUILayout.Height(SubBoxHeight));
                        var slotRect = GUILayoutUtility.GetLastRect();
                        const float iconPadding = 6f;
                        var iconRect = new Rect(
                            slotRect.x + iconPadding, slotRect.y + iconPadding,
                            slotRect.width - iconPadding * 2f, slotRect.height - iconPadding * 2f);
                        GUI.DrawTexture(iconRect, entry.item.icon.texture, ScaleMode.ScaleToFit);
                    }
                    else
                    {
                        string label = entry.item.itemName + (entry.count > 1 ? $" x{entry.count}" : "");
                        clicked = GUILayout.Button(label, DebugGUI.Slot, GUILayout.Width(SubBoxWidth), GUILayout.Height(SubBoxHeight));
                    }

                    if (clicked)
                    {
                        pendingMoveItem = entry.item;
                        pendingMoveSource = inventory;
                        pendingMoveEquipment = entry.equipment;
                    }

                    // A Canteen shows its fill status here instead of a QTY
                    // count (same format as its Equipment-row label) — Ben's
                    // request, so a Canteen clipped to a Belt point reads the
                    // same way as one sitting directly in an equip slot.
                    // Otherwise blank for a non-stackable item (maxStack <= 1,
                    // e.g. a Backpack) rather than always showing "QTY: 1" —
                    // Ben's call. Still drawn (as an empty label) either way
                    // so every column reserves the same row height.
                    string qtyLabel = entry.equipment is Canteen canteenEntry
                        ? (canteenEntry.IsEmpty ? "Empty" : $"{canteenEntry.Liquid} {canteenEntry.Amount:F0}/{canteenEntry.Capacity:F0}")
                        : entry.item.maxStack > 1 ? $"QTY: {entry.count}" : "";
                    GUILayout.Label(qtyLabel, DebugGUI.Label, GUILayout.Width(SubBoxWidth));
                }
                else
                {
                    // Plain gray box, no "Empty" text — Ben's call,
                    // scoped to this grid specifically (the equipment
                    // slot list's own "Empty" labels are unchanged).
                    // DebugGUI.Slot (an explicit solid-color background)
                    // instead of GUI.skin.box's default, which turned out
                    // to have too little contrast to be visible at all
                    // without any text/content inside it — Ben's report:
                    // capacity became impossible to see once "Empty" was
                    // removed and nothing replaced its visibility.
                    GUILayout.Box(GUIContent.none, DebugGUI.Slot, GUILayout.Width(SubBoxWidth), GUILayout.Height(SubBoxHeight));
                    GUILayout.Label("", DebugGUI.Label, GUILayout.Width(SubBoxWidth));
                }

                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }
    }

    // GUILayout.Button responds to any mouse button by default (a Unity
    // IMGUI quirk) — right-click is also used elsewhere (PlayerRenaming),
    // and a right-click aimed at a nearby item box that overlaps this
    // button's rect (see the Back-row/contents-grid layout note above) can
    // otherwise trigger it by accident. Restricting Equip/Unequip/Drop to
    // left-click only makes them immune to that regardless of exact pixel
    // alignment. Confirmed as the fix for two reports (2026-08-03) where a
    // right-click meant for an item inside a backpack instead
    // dropped/unequipped the backpack itself.
    private static bool SafeButton(string label, params GUILayoutOption[] options)
    {
        bool clicked = GUILayout.Button(label, options);
        return clicked && Event.current.button == 0;
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
