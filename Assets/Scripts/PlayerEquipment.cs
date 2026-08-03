using System.Collections.Generic;
using UnityEngine;

// Generic named equipment slots for the character (e.g. "Back"). Holds a
// reference to whatever inventory-capable object currently occupies each
// slot, if any.
public class PlayerEquipment : MonoBehaviour
{
    [SerializeField] private string[] slotNames = { "Back" };

    private readonly Dictionary<string, IInventoryHolder> equipped = new Dictionary<string, IInventoryHolder>();

    private void Awake()
    {
        foreach (var slotName in slotNames)
            equipped[slotName] = null;
    }

    public IInventoryHolder GetEquipped(string slotName) =>
        equipped.TryGetValue(slotName, out var holder) ? holder : null;

    public bool CanEquip(string slotName) =>
        equipped.TryGetValue(slotName, out var holder) && holder == null;

    public bool Equip(string slotName, IInventoryHolder holder)
    {
        if (holder == null || !CanEquip(slotName)) return false;
        equipped[slotName] = holder;
        return true;
    }

    public void Unequip(string slotName)
    {
        if (equipped.ContainsKey(slotName))
            equipped[slotName] = null;
    }
}
