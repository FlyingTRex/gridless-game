using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerVitals))]
public class PlayerEating : MonoBehaviour
{
    [SerializeField] private EdibleItem[] edibles;

    private PlayerInventory inventory;
    private PlayerVitals vitals;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        vitals = GetComponent<PlayerVitals>();
    }

    public EdibleItem FindEdible(ItemDefinition item)
    {
        if (item == null || edibles == null) return null;

        foreach (var edible in edibles)
        {
            if (edible != null && edible.item == item)
                return edible;
        }

        return null;
    }

    public bool TryEat(ItemDefinition item)
    {
        var edible = FindEdible(item);
        if (edible == null) return false;
        if (!inventory.RemoveItem(edible.item, edible.consumeCount)) return false;

        vitals.Restore(edible.vital, edible.restoreAmount);
        if (edible.returnItem != null)
            inventory.AddItem(edible.returnItem, edible.consumeCount);

        return true;
    }
}
