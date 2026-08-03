using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private int capacity = 4;

    private Inventory inventory;

    public Inventory Inventory => inventory;

    private void Awake()
    {
        inventory = new Inventory(capacity);
    }

    // Returns the amount that did NOT fit (0 means everything was added).
    public int AddItem(ItemDefinition item, int quantity) => inventory.AddItem(item, quantity);

    public bool RemoveItem(ItemDefinition item, int quantity) => inventory.RemoveItem(item, quantity);

    public int GetCount(ItemDefinition item) => inventory.GetCount(item);
}
