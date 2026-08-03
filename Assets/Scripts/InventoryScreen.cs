using UnityEngine;
using UnityEngine.InputSystem;

// Full character equipment screen, toggled with I. Lists every body slot
// and, for each one, a row of boxes — one per unit of that slot's
// Inventory capacity — showing what's equipped in it, if anything.
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
        float height = 60f + SlotOrder.Length * (BoxHeight + 6f) + 50f;
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
            for (int i = 0; i < slotInventory.Capacity; i++)
            {
                string boxLabel = i < occupied.Count
                    ? occupied[i].item.itemName + (occupied[i].count > 1 ? $" x{occupied[i].count}" : "")
                    : "Empty";
                GUILayout.Box(boxLabel, GUILayout.Width(BoxWidth), GUILayout.Height(BoxHeight));
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Close", GUILayout.Width(100)))
            SetOpen(false);

        GUILayout.EndArea();
    }
}
