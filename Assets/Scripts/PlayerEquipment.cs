using System.Collections.Generic;
using UnityEngine;

// Generic named equipment slots for the character (Back, Hand, Belt, ...).
// Holds a reference to whatever equippable object currently occupies each
// slot, if any.
public class PlayerEquipment : MonoBehaviour
{
    [SerializeField] private string[] slotNames = { "Back", "Hand", "Belt" };

    private readonly Dictionary<string, IEquippable> equipped = new Dictionary<string, IEquippable>();

    private void Awake()
    {
        foreach (var slotName in slotNames)
            equipped[slotName] = null;
    }

    public IEquippable GetEquipped(string slotName) =>
        equipped.TryGetValue(slotName, out var holder) ? holder : null;

    public bool CanEquip(string slotName) =>
        equipped.TryGetValue(slotName, out var holder) && holder == null;

    public bool Equip(string slotName, IEquippable holder)
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
