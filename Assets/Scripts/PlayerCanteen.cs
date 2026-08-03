using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerEquipment))]
public class PlayerCanteen : MonoBehaviour
{
    private static readonly string[] CanteenSlots = { "Hand", "Belt" };

    [SerializeField] private ItemDefinition canteenItem;
    [SerializeField] private Transform handSlotAnchor;
    [SerializeField] private Transform beltSlotAnchor;
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;

    private PlayerInventory playerInventory;
    private PlayerEquipment equipment;
    private PlayerVitals vitals;

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
        equipment = GetComponent<PlayerEquipment>();
        vitals = GetComponent<PlayerVitals>();
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

    // Moves the canteen from a regular inventory slot onto Hand or Belt.
    public bool Equip(Canteen canteen, string slotName)
    {
        if (canteen == null) return false;
        if (!equipment.Equip(slotName, canteen)) return false;

        playerInventory.Inventory.RemoveEquipmentItem(canteenItem);
        var anchor = slotName == "Hand" ? handSlotAnchor : beltSlotAnchor;
        canteen.SetCarried(true, anchor != null ? anchor : transform);
        return true;
    }

    // Moves the canteen from Hand/Belt back into a regular inventory slot.
    // Fails (leaving it equipped) if the regular inventory is full.
    public bool Unequip(Canteen canteen, string slotName)
    {
        if (canteen == null || (equipment.GetEquipped(slotName) as Canteen) != canteen) return false;
        if (!playerInventory.Inventory.AddEquipmentItem(canteenItem, canteen)) return false;

        equipment.Unequip(slotName);
        canteen.Stash();
        return true;
    }

    // Drops the canteen into the world in front of the player, whether it
    // was equipped (slotName given) or just sitting in the regular
    // inventory (slotName null).
    public void Drop(Canteen canteen, string slotName)
    {
        if (canteen == null) return;

        if (slotName != null && (equipment.GetEquipped(slotName) as Canteen) == canteen)
            equipment.Unequip(slotName);
        else
            playerInventory.Inventory.RemoveEquipmentItem(canteenItem);

        canteen.SetCarried(false, null);
        canteen.transform.position = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight;
    }

    private void OnGUI()
    {
        float top = 10f;
        foreach (var slotName in CanteenSlots)
        {
            if (DrawSlot(slotName, top))
                top += 120f;
        }
    }

    private bool DrawSlot(string slotName, float top)
    {
        var canteen = equipment.GetEquipped(slotName) as Canteen;
        if (canteen == null) return false;

        GUILayout.BeginArea(new Rect(590, top, 240, 110));
        GUILayout.Label($"{slotName}: {canteen.DisplayName}", GUI.skin.box);

        string liquidLabel = canteen.IsEmpty ? "Empty" : $"{canteen.Liquid} {canteen.Amount:F0}/{canteen.Capacity:F0}";
        GUILayout.Label(liquidLabel);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Drink")) canteen.Drink(vitals);
        if (GUILayout.Button("Fill")) canteen.Fill(LiquidType.Water);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Unequip")) Unequip(canteen, slotName);
        if (GUILayout.Button("Drop")) Drop(canteen, slotName);
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
        return true;
    }
}
