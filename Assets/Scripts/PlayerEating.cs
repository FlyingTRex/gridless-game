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

    // Eats from the main inventory specifically — used by DrawInventorySection,
    // the only place that only ever shows a plain item from there.
    public bool TryEat(ItemDefinition item) => TryEatFrom(inventory.Inventory, item);

    // Eats from wherever the item actually is (a hand slot, a Backpack, a
    // Storage Box) — previously TryEat always removed from the main
    // inventory regardless of source, so eating a Berry sitting in a hand
    // or a container silently did nothing (FindEdible still found it, so
    // the Eat button showed, but RemoveItem on the wrong inventory found
    // zero and failed quietly).
    public bool TryEatFrom(Inventory source, ItemDefinition item)
    {
        var edible = FindEdible(item);
        if (edible == null || source == null) return false;
        if (!source.RemoveItem(edible.item, edible.consumeCount)) return false;

        vitals.Restore(VitalType.Hunger, FoodTierScale.HungerRestoreAmount(edible.foodTier));
        vitals.Restore(edible.vital, edible.restoreAmount);
        if (edible.healOverTimeAmount > 0f)
            vitals.StartHealOverTime(edible.healOverTimeAmount, edible.healOverTimeDuration);
        if (edible.returnItem != null)
            source.AddItem(edible.returnItem, edible.consumeCount);

        return true;
    }
}
