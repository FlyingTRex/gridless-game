using UnityEngine;
using UnityEngine.InputSystem;

// Full inventory + equipment management screen, toggled with I. Combines
// what used to be separate always-on panels (Inventory, Backpack, Canteen)
// into one screen, so the normal HUD stays clean and all inventory state
// lives in one place, scrollable so it can't overflow the window.
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
    private const float SubBoxHeight = 30f;
    private const int SubBoxesPerRow = 6;

    private const float PanelWidth = LabelWidth + BoxWidth * 2f + 220f;

    private PlayerEquipment equipment;
    private PlayerInventory playerInventory;
    private PlayerCrafting crafting;
    private PlayerDropping dropping;
    private PlayerEating eating;
    private PlayerBackpack backpackCarrier;
    private PlayerCanteen canteenCarrier;
    private PlayerVitals vitals;
    private bool isOpen;
    private Vector2 scrollPos;

    // Set when the player clicks an item inside a container's contents
    // grid — rather than acting immediately, opens a popup asking where it
    // should go (Drop / a hand / the main inventory).
    private ItemDefinition pendingMoveItem;
    private Inventory pendingMoveSource;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        equipment = GetComponent<PlayerEquipment>();
        playerInventory = GetComponent<PlayerInventory>();
        crafting = GetComponent<PlayerCrafting>();
        dropping = GetComponent<PlayerDropping>();
        eating = GetComponent<PlayerEating>();
        backpackCarrier = GetComponent<PlayerBackpack>();
        canteenCarrier = GetComponent<PlayerCanteen>();
        vitals = GetComponent<PlayerVitals>();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
            SetOpen(!isOpen);
    }

    // Called by FirstPersonController when Escape re-locks the cursor, so
    // the two toggles can't drift out of sync with each other.
    public void Close() => SetOpen(false);

    private void SetOpen(bool value)
    {
        isOpen = value;
        if (!value)
        {
            pendingMoveItem = null;
            pendingMoveSource = null;
        }
        Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = value;
    }

    private void OnGUI()
    {
        if (!isOpen) return;

        float height = Mathf.Min(Screen.height - 40f, 700f);
        var rect = new Rect((Screen.width - PanelWidth) / 2f, (Screen.height - height) / 2f, PanelWidth, height);

        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);

        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(height - 60f));

        GUILayout.Label("Inventory", DebugGUI.Header);
        DrawInventorySection();

        GUILayout.Space(10);
        GUILayout.Label("Equipment", DebugGUI.Header);
        DrawEquipmentSection();

        GUILayout.EndScrollView();

        if (GUILayout.Button("Close", GUILayout.Width(100)))
            SetOpen(false);

        GUILayout.EndArea();

        DrawPendingMovePopup();
    }

    // Small "where should this go?" dialog shown after clicking an item
    // inside a container's contents grid. Drawn last so it sits on top.
    private void DrawPendingMovePopup()
    {
        if (pendingMoveItem == null || pendingMoveSource == null) return;

        const float width = 220f;
        const float height = 210f;
        var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);

        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Label(pendingMoveItem.itemName, DebugGUI.Header);

        bool resolved = false;

        if (GUILayout.Button("Drop"))
        {
            dropping?.DropFrom(pendingMoveSource, pendingMoveItem);
            resolved = true;
        }

        var leftHand = equipment.GetSlot("Left Hand");
        if (leftHand != null && leftHand != pendingMoveSource && GUILayout.Button("To Left Hand"))
        {
            InventoryTransfer.Move(pendingMoveSource, leftHand, pendingMoveItem, pendingMoveSource.GetCount(pendingMoveItem));
            resolved = true;
        }

        var rightHand = equipment.GetSlot("Right Hand");
        if (rightHand != null && rightHand != pendingMoveSource && GUILayout.Button("To Right Hand"))
        {
            InventoryTransfer.Move(pendingMoveSource, rightHand, pendingMoveItem, pendingMoveSource.GetCount(pendingMoveItem));
            resolved = true;
        }

        if (playerInventory.Inventory != pendingMoveSource && GUILayout.Button("To Inventory"))
        {
            InventoryTransfer.Move(pendingMoveSource, playerInventory.Inventory, pendingMoveItem, pendingMoveSource.GetCount(pendingMoveItem));
            resolved = true;
        }

        if (GUILayout.Button("Cancel"))
            resolved = true;

        GUILayout.EndArea();

        if (resolved)
        {
            pendingMoveItem = null;
            pendingMoveSource = null;
        }
    }

    // Ported from the old always-on PlayerInventory panel.
    private void DrawInventorySection()
    {
        ItemDefinition craftClicked = null;
        ItemDefinition dropClicked = null;
        ItemDefinition packClicked = null;
        ItemDefinition eatClicked = null;
        Backpack equipClicked = null;
        Backpack backpackDropClicked = null;
        Canteen canteenEquipClicked = null;
        Canteen canteenDropClicked = null;
        var equippedBackpack = backpackCarrier != null ? backpackCarrier.Equipped : null;

        var inv = playerInventory.Inventory;
        var slots = inv.Slots;
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            string label = $"{slot.item.itemName} x{slot.count}";

            GUILayout.BeginHorizontal();

            if (slot.equipment is Backpack backpack)
            {
                GUILayout.Label(label, DebugGUI.Label);
                if (GUILayout.Button("Equip", GUILayout.Width(55)))
                    equipClicked = backpack;
                if (GUILayout.Button("Drop", GUILayout.Width(50)))
                    backpackDropClicked = backpack;
            }
            else if (slot.equipment is Canteen canteen)
            {
                GUILayout.Label(label, DebugGUI.Label);
                if (GUILayout.Button("Equip", GUILayout.Width(55)))
                    canteenEquipClicked = canteen;
                if (GUILayout.Button("Drop", GUILayout.Width(50)))
                    canteenDropClicked = canteen;
            }
            else
            {
                var recipe = crafting != null ? crafting.FindRecipe(slot.item) : null;
                if (recipe != null)
                {
                    if (GUILayout.Button($"{label}  (craft {recipe.outputItem.itemName})"))
                        craftClicked = slot.item;
                }
                else
                {
                    GUILayout.Label(label, DebugGUI.Label);
                }

                var edible = eating != null ? eating.FindEdible(slot.item) : null;
                if (edible != null && GUILayout.Button(edible.verb, GUILayout.Width(50)))
                    eatClicked = slot.item;

                if (dropping != null && GUILayout.Button("Drop", GUILayout.Width(50)))
                    dropClicked = slot.item;

                if (equippedBackpack != null && GUILayout.Button("To Pack", GUILayout.Width(60)))
                    packClicked = slot.item;
            }

            GUILayout.EndHorizontal();
        }

        if (craftClicked != null)
            crafting.TryCraft(craftClicked);
        if (eatClicked != null)
            eating.TryEat(eatClicked);
        if (dropClicked != null)
            dropping.Drop(dropClicked);
        if (packClicked != null)
            InventoryTransfer.Move(inv, equippedBackpack.Inventory, packClicked, inv.GetCount(packClicked));
        if (equipClicked != null)
            backpackCarrier.Equip(equipClicked);
        if (backpackDropClicked != null)
            backpackCarrier.Drop(backpackDropClicked);
        if (canteenEquipClicked != null)
            canteenCarrier.Equip(canteenEquipClicked);
        if (canteenDropClicked != null)
            canteenCarrier.Drop(canteenDropClicked);
    }

    private void DrawEquipmentSection()
    {
        Backpack backpackEquipClicked = null;
        Backpack backpackUnequipClicked = null;
        Backpack backpackDropClicked = null;
        Canteen canteenUnequipClicked = null;
        Canteen canteenDropClicked = null;
        ItemDefinition plainItemMoveClicked = null;
        Inventory plainItemMoveSource = null;

        foreach (var slotName in SlotOrder)
        {
            var slotInventory = equipment.GetSlot(slotName);
            if (slotInventory == null) continue;

            GUILayout.BeginHorizontal();
            GUILayout.Label(slotName, DebugGUI.Label, GUILayout.Width(LabelWidth));

            var occupied = slotInventory.Slots;
            IInventoryHolder nestedHolder = null;
            Backpack backpackHere = null;
            Canteen canteenHere = null;

            for (int i = 0; i < slotInventory.Capacity; i++)
            {
                if (i < occupied.Count)
                {
                    var entry = occupied[i];
                    string label = entry.item.itemName + (entry.count > 1 ? $" x{entry.count}" : "");

                    if (entry.equipment == null)
                    {
                        // A plain stackable item sitting directly in an
                        // equip slot (e.g. something picked up into a
                        // hand) — click it to move it back to inventory.
                        if (GUILayout.Button(label, GUILayout.Width(BoxWidth), GUILayout.Height(BoxHeight)))
                        {
                            plainItemMoveClicked = entry.item;
                            plainItemMoveSource = slotInventory;
                        }
                    }
                    else
                    {
                        GUILayout.Box(label, GUILayout.Width(BoxWidth), GUILayout.Height(BoxHeight));
                    }

                    // A backpack only exposes its own storage (and can
                    // only be Unequipped, as opposed to Equipped) once
                    // it's actually worn on Back — holding one in a hand
                    // doesn't make it usable storage yet.
                    if (entry.equipment is IInventoryHolder holder && slotName == "Back") nestedHolder = holder;
                    if (entry.equipment is Backpack bp) backpackHere = bp;
                    if (entry.equipment is Canteen ct) canteenHere = ct;
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
                    if (GUILayout.Button("Unequip", GUILayout.Width(70))) backpackUnequipClicked = backpackHere;
                }
                else
                {
                    if (GUILayout.Button("Equip", GUILayout.Width(55))) backpackEquipClicked = backpackHere;
                }

                if (GUILayout.Button("Drop", GUILayout.Width(50))) backpackDropClicked = backpackHere;
            }
            else if (canteenHere != null)
            {
                string liquidLabel = canteenHere.IsEmpty
                    ? "Empty"
                    : $"{canteenHere.Liquid} {canteenHere.Amount:F0}/{canteenHere.Capacity:F0}";
                GUILayout.Label(liquidLabel, DebugGUI.Label, GUILayout.Width(90));
                if (GUILayout.Button("Drink", GUILayout.Width(50))) canteenHere.Drink(vitals);
                if (GUILayout.Button("Fill", GUILayout.Width(45))) canteenHere.Fill(LiquidType.Water);
                if (GUILayout.Button("Unequip", GUILayout.Width(65))) canteenUnequipClicked = canteenHere;
                if (GUILayout.Button("Drop", GUILayout.Width(50))) canteenDropClicked = canteenHere;
            }

            GUILayout.EndHorizontal();

            if (nestedHolder != null)
                DrawContainerContents(nestedHolder);
        }

        if (backpackEquipClicked != null) backpackCarrier.Equip(backpackEquipClicked);
        if (backpackUnequipClicked != null) backpackCarrier.Unequip(backpackUnequipClicked);
        if (backpackDropClicked != null) backpackCarrier.Drop(backpackDropClicked);
        if (canteenUnequipClicked != null) canteenCarrier.Unequip(canteenUnequipClicked);
        if (canteenDropClicked != null) canteenCarrier.Drop(canteenDropClicked);
        if (plainItemMoveClicked != null && plainItemMoveSource != null)
            InventoryTransfer.Move(plainItemMoveSource, playerInventory.Inventory,
                plainItemMoveClicked, plainItemMoveSource.GetCount(plainItemMoveClicked));
    }

    // Draws a container's own capacity as a wrapped grid of boxes. Occupied
    // boxes are buttons — clicking one opens the "where should this go?"
    // popup (DrawPendingMovePopup) instead of moving it anywhere directly.
    private void DrawContainerContents(IInventoryHolder holder)
    {
        var contents = holder.Inventory.Slots;
        int capacity = holder.Inventory.Capacity;

        GUILayout.Label($"    {holder.DisplayName} contents (click an item for options):", DebugGUI.Label);

        int drawn = 0;
        while (drawn < capacity)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            for (int col = 0; col < SubBoxesPerRow && drawn < capacity; col++, drawn++)
            {
                if (drawn < contents.Count)
                {
                    var entry = contents[drawn];
                    string label = entry.item.itemName + (entry.count > 1 ? $" x{entry.count}" : "");
                    if (GUILayout.Button(label, GUILayout.Width(SubBoxWidth), GUILayout.Height(SubBoxHeight)))
                    {
                        pendingMoveItem = entry.item;
                        pendingMoveSource = holder.Inventory;
                    }
                }
                else
                {
                    GUILayout.Box("Empty", GUILayout.Width(SubBoxWidth), GUILayout.Height(SubBoxHeight));
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}
