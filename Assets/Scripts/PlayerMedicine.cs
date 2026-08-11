using UnityEngine;

// Mirrors PlayerEating.cs's exact shape (2026-08-10) — see MedicineItem.cs
// for why this is a parallel system rather than extending PlayerEating
// directly (Medicine isn't food; heals over time via StartHealOverTime,
// not Restore).
[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerVitals))]
public class PlayerMedicine : MonoBehaviour
{
    [SerializeField] private MedicineItem[] medicines;

    private PlayerInventory inventory;
    private PlayerVitals vitals;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        vitals = GetComponent<PlayerVitals>();
    }

    public MedicineItem FindMedicine(ItemDefinition item)
    {
        if (item == null || medicines == null) return null;

        foreach (var medicine in medicines)
        {
            if (medicine != null && medicine.item == item)
                return medicine;
        }

        return null;
    }

    // Applies from the main inventory specifically — same split as
    // PlayerEating.TryEat/TryEatFrom.
    public bool TryApply(ItemDefinition item) => TryApplyFrom(inventory.Inventory, item);

    public bool TryApplyFrom(Inventory source, ItemDefinition item)
    {
        var medicine = FindMedicine(item);
        if (medicine == null || source == null) return false;
        if (!source.RemoveItem(medicine.item, medicine.consumeCount)) return false;

        vitals.StartHealOverTime(medicine.healAmount, medicine.healDuration);
        return true;
    }
}
