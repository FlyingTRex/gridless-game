using UnityEngine;
using UnityEngine.InputSystem;

// Full character equipment screen, toggled with I. Lists every body slot
// and, for each one, a row of boxes — one per unit of that slot's
// Inventory capacity — showing what's equipped in it, if anything. If the
// equipped item is itself a container (e.g. a Backpack), its own capacity
// and contents are drawn as a nested row underneath.
[RequireComponent(typeof(PlayerEquipment))]
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

    private PlayerEquipment equipment;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        equipment = GetComponent<PlayerEquipment>();
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
        Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = value;
    }

    private void OnGUI()
    {
        if (!isOpen) return;

        float width = LabelWidth + BoxWidth * 2f + 40f;
        float height = 60f + SlotOrder.Length * (BoxHeight + 6f) + NestedContainerHeight() + 50f;
        var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);

        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Label("Equipment", DebugGUI.Header);
        GUILayout.Space(6);

        foreach (var slotName in SlotOrder)
        {
            var slotInventory = equipment.GetSlot(slotName);
            if (slotInventory == null) continue;

            GUILayout.BeginHorizontal();
            GUILayout.Label(slotName, DebugGUI.Label, GUILayout.Width(LabelWidth));

            var occupied = slotInventory.Slots;
            IInventoryHolder nestedHolder = null;

            for (int i = 0; i < slotInventory.Capacity; i++)
            {
                if (i < occupied.Count)
                {
                    var entry = occupied[i];
                    string label = entry.item.itemName + (entry.count > 1 ? $" x{entry.count}" : "");
                    GUILayout.Box(label, GUILayout.Width(BoxWidth), GUILayout.Height(BoxHeight));
                    if (entry.equipment is IInventoryHolder holder)
                        nestedHolder = holder;
                }
                else
                {
                    GUILayout.Box("Empty", GUILayout.Width(BoxWidth), GUILayout.Height(BoxHeight));
                }
            }

            GUILayout.EndHorizontal();

            if (nestedHolder != null)
                DrawContainerContents(nestedHolder);
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Close", GUILayout.Width(100)))
            SetOpen(false);

        GUILayout.EndArea();
    }

    private void DrawContainerContents(IInventoryHolder holder)
    {
        var contents = holder.Inventory.Slots;
        int capacity = holder.Inventory.Capacity;

        GUILayout.Label($"    {holder.DisplayName} contents:", DebugGUI.Label);

        int drawn = 0;
        while (drawn < capacity)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            for (int col = 0; col < SubBoxesPerRow && drawn < capacity; col++, drawn++)
            {
                string label = drawn < contents.Count
                    ? contents[drawn].item.itemName + (contents[drawn].count > 1 ? $" x{contents[drawn].count}" : "")
                    : "Empty";
                GUILayout.Box(label, GUILayout.Width(SubBoxWidth), GUILayout.Height(SubBoxHeight));
            }
            GUILayout.EndHorizontal();
        }
    }

    // Extra panel height to reserve this frame for a nested container's
    // contents, if anything currently equipped is one. Only accounts for a
    // single nested block — fine today since only one slot (Back) can ever
    // hold a container-type equippable, but would need to sum per-slot if
    // that changes.
    private float NestedContainerHeight()
    {
        int maxCapacity = 0;
        foreach (var slotName in SlotOrder)
        {
            var slotInventory = equipment.GetSlot(slotName);
            if (slotInventory == null) continue;

            foreach (var entry in slotInventory.Slots)
                if (entry.equipment is IInventoryHolder holder)
                    maxCapacity = Mathf.Max(maxCapacity, holder.Inventory.Capacity);
        }

        if (maxCapacity <= 0) return 0f;

        int rows = Mathf.CeilToInt(maxCapacity / (float)SubBoxesPerRow);
        return rows * (SubBoxHeight + 4f) + 24f;
    }
}
