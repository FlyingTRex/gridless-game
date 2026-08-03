using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerEquipment))]
public class PlayerCanteen : MonoBehaviour
{
    // Tried in order when equipping — first free slot wins.
    private static readonly string[] CanteenSlots = { "Left Hand", "Right Hand", "Waist" };

    [SerializeField] private ItemDefinition canteenItem;
    [SerializeField] private Transform leftHandSlotAnchor;
    [SerializeField] private Transform rightHandSlotAnchor;
    [SerializeField] private Transform waistSlotAnchor;
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;

    private PlayerInventory playerInventory;
    private PlayerEquipment equipment;
    private PlayerVitals vitals;
    private InventoryScreen inventoryScreen;

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
        equipment = GetComponent<PlayerEquipment>();
        vitals = GetComponent<PlayerVitals>();
        inventoryScreen = GetComponent<InventoryScreen>();
    }

    // Called when the player interacts with a canteen lying in the world —
    // stashes it as a regular (hidden) inventory item, not carried yet.
    public bool PickUp(Canteen canteen)
    {
        if (canteen == null) return false;
        if (!playerInventory.Inventory.AddEquipmentItem(canteenItem, canteen)) return false;

        canteen.Stash();
        return true;
    }

    // Moves the canteen from a regular inventory slot onto the first
    // available slot in CanteenSlots (Left Hand, then Right Hand, then Waist).
    public bool Equip(Canteen canteen)
    {
        if (canteen == null) return false;

        foreach (var slotName in CanteenSlots)
        {
            var slot = equipment.GetSlot(slotName);
            if (slot == null || !slot.AddEquipmentItem(canteenItem, canteen)) continue;

            playerInventory.Inventory.RemoveEquipmentItem(canteenItem);
            canteen.SetCarried(true, AnchorFor(slotName));
            return true;
        }

        return false;
    }

    // Moves the canteen from wherever it's equipped back into a regular
    // inventory slot. Fails (leaving it equipped) if the inventory is full.
    public bool Unequip(Canteen canteen)
    {
        string slotName = FindSlot(canteen);
        if (slotName == null) return false;
        if (!playerInventory.Inventory.AddEquipmentItem(canteenItem, canteen)) return false;

        equipment.GetSlot(slotName).RemoveEquipmentItem(canteenItem);
        canteen.Stash();
        return true;
    }

    // Drops the canteen into the world in front of the player, whether it
    // was equipped or just sitting in the regular inventory.
    public void Drop(Canteen canteen)
    {
        if (canteen == null) return;

        string slotName = FindSlot(canteen);
        if (slotName != null)
            equipment.GetSlot(slotName).RemoveEquipmentItem(canteenItem);
        else
            playerInventory.Inventory.RemoveEquipmentItem(canteenItem);

        canteen.SetCarried(false, null);
        canteen.transform.position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
    }

    private string FindSlot(Canteen canteen)
    {
        foreach (var slotName in CanteenSlots)
            if ((equipment.GetEquipped(slotName) as Canteen) == canteen)
                return slotName;
        return null;
    }

    private Transform AnchorFor(string slotName)
    {
        Transform anchor = slotName switch
        {
            "Left Hand" => leftHandSlotAnchor,
            "Right Hand" => rightHandSlotAnchor,
            "Waist" => waistSlotAnchor,
            _ => null,
        };
        return anchor != null ? anchor : transform;
    }

    private void OnGUI()
    {
        // The full Equipment screen (I) already shows where the canteen is
        // equipped — avoid drawing this panel on top of it.
        if (inventoryScreen != null && inventoryScreen.IsOpen) return;

        string slotName = null;
        Canteen canteen = null;
        foreach (var candidate in CanteenSlots)
        {
            var c = equipment.GetEquipped(candidate) as Canteen;
            if (c == null) continue;
            slotName = candidate;
            canteen = c;
            break;
        }

        if (canteen == null) return;

        var rect = new Rect(610, 10, 240, 110);
        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Label($"{slotName}: {canteen.DisplayName}", DebugGUI.Header);

        string liquidLabel = canteen.IsEmpty ? "Empty" : $"{canteen.Liquid} {canteen.Amount:F0}/{canteen.Capacity:F0}";
        GUILayout.Label(liquidLabel, DebugGUI.Label);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Drink")) canteen.Drink(vitals);
        if (GUILayout.Button("Fill")) canteen.Fill(LiquidType.Water);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Unequip")) Unequip(canteen);
        if (GUILayout.Button("Drop")) Drop(canteen);
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }
}
