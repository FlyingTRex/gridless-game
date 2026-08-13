using UnityEngine;

// Mirrors EdibleItem.cs/MedicineItem.cs's exact shape — a small registered
// item type rather than a field bolted onto ItemDefinition. Registers an
// item as valid Furnace fuel and which FuelTier (burn duration) it burns
// at. Data layer only for now — the Furnace itself has no fuel-burning
// logic yet (no lit/unlit state, no fuel inventory, no timer); see
// WOOD_AND_FUEL_PLANNING.md's build order for what's still to come.
[CreateAssetMenu(menuName = "Gridless/Fuel Item", fileName = "NewFuel")]
public class FuelItem : ScriptableObject
{
    public ItemDefinition item;
    public FuelTier fuelTier;
}
