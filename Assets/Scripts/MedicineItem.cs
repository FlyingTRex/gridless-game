using UnityEngine;

// Mirrors EdibleItem.cs's exact shape (2026-08-10, Ben's call: "an apply
// medicine function that mimics eating — it consumes one of the
// resource") — a data-driven registered item instead of a hardcoded
// per-type UI branch (like Canteen's Drink/Fill), since Medicine, like
// Berry, is a plain stackable inventory item. Always heals over time via
// PlayerVitals.StartHealOverTime rather than EdibleItem's instant
// Restore(vital, amount) — first aid is about a real vital (Health)
// recovering gradually, not an instant top-up.
[CreateAssetMenu(menuName = "Gridless/Medicine Item", fileName = "NewMedicine")]
public class MedicineItem : ScriptableObject
{
    public ItemDefinition item;
    public int consumeCount = 1;
    public float healAmount = 10f;
    public float healDuration = 10f;
    public string verb = "Apply";
}
