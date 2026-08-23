using Mirror;
using UnityEngine;

// Mirrors PlayerEating.cs's exact shape (2026-08-10) — see MedicineItem.cs
// for why this is a parallel system rather than extending PlayerEating
// directly (Medicine isn't food; heals over time via StartHealOverTime,
// not Restore).
//
// Multiplayer Phase 3 sub-phase 5, 2026-08-23: converted to
// NetworkBehaviour, same RequestXFrom/CmdXFrom Command shape as
// PlayerEating's own conversion — same container-key scheme, same
// PlayerInventory.ResolveContainerByKey reuse.
[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerVitals))]
public class PlayerMedicine : NetworkBehaviour
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

    public void RequestApplyFrom(string containerKey, ItemDefinition item)
    {
        string id = ItemDatabase.Instance.IdFor(item);
        if (id == null) return;
        CmdApplyFrom(containerKey, id);
    }

    [Command]
    private void CmdApplyFrom(string containerKey, string itemId)
    {
        var item = ItemDatabase.Instance.Find(itemId);
        if (item == null) return;

        var source = inventory.ResolveContainerByKey(containerKey);
        if (source == null) return;

        TryApplyFrom(source, item);
    }
}
