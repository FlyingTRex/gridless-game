using UnityEngine;

// Mirrors EdibleItem.cs/MedicineItem.cs/FuelItem.cs's exact established
// pattern. Registers a raw->cooked conversion the Campfire's cooking slot
// can perform. requiredAccessory (nullable) gates the conversion behind a
// specific accessory equipped in one of the Campfire's 4 accessory slots
// (not built yet, 2026-08-12) -- null means cookable over the open flame
// with no accessory, same as Raw Meat -> Cooked Meat's case. See
// CAMPFIRE_PLANNING.md.
[CreateAssetMenu(menuName = "Gridless/Cookable Item", fileName = "NewCookable")]
public class CookableItem : ScriptableObject
{
    public ItemDefinition rawItem;
    public ItemDefinition cookedItem;
    public float cookDurationSeconds = 30f;
    public ItemDefinition requiredAccessory;
}
