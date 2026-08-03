using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInventory : MonoBehaviour, IInventoryHolder
{
    [SerializeField] private int capacity = 4;

    private Inventory inventory;
    private PlayerCrafting crafting;
    private PlayerDropping dropping;
    private PlayerBackpack backpackCarrier;

    public Inventory Inventory => inventory;
    public string DisplayName => "Inventory";

    private void Awake()
    {
        inventory = new Inventory(capacity);
        crafting = GetComponent<PlayerCrafting>();
        dropping = GetComponent<PlayerDropping>();
        backpackCarrier = GetComponent<PlayerBackpack>();
    }

    // Returns the amount that did NOT fit (0 means everything was added).
    public int AddItem(ItemDefinition item, int quantity) => inventory.AddItem(item, quantity);

    public bool RemoveItem(ItemDefinition item, int quantity) => inventory.RemoveItem(item, quantity);

    public int GetCount(ItemDefinition item) => inventory.GetCount(item);

    private void OnGUI()
    {
        var rect = new Rect(10, 10, 300, 340);
        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Label("Inventory", DebugGUI.Header);

        ItemDefinition craftClicked = null;
        ItemDefinition dropClicked = null;
        ItemDefinition packClicked = null;
        Backpack equipClicked = null;
        Backpack backpackDropClicked = null;
        var equippedBackpack = backpackCarrier != null ? backpackCarrier.Equipped : null;

        var slots = inventory.Slots;
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

                if (dropping != null && GUILayout.Button("Drop", GUILayout.Width(50)))
                    dropClicked = slot.item;

                if (equippedBackpack != null && GUILayout.Button("To Pack", GUILayout.Width(60)))
                    packClicked = slot.item;
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.EndArea();

        if (craftClicked != null)
            crafting.TryCraft(craftClicked);
        if (dropClicked != null)
            dropping.Drop(dropClicked);
        if (packClicked != null)
            InventoryTransfer.Move(inventory, equippedBackpack.Inventory, packClicked, inventory.GetCount(packClicked));
        if (equipClicked != null)
            backpackCarrier.Equip(equipClicked);
        if (backpackDropClicked != null)
            backpackCarrier.Drop(backpackDropClicked);
    }
}
