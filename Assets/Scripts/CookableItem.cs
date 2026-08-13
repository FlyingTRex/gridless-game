using UnityEngine;

// Registers a Campfire cooking recipe. Mirrors CraftingRecipe's own
// ingredients[]/outputItem shape (2026-08-13 rework — was a single
// rawItem->cookedItem pair, upgraded to support multi-ingredient recipes
// once the Campfire gained a real recipe-matching UI). requiredAccessory
// (nullable) gates the recipe behind a specific accessory item present in
// one of the Campfire's 4 accessory slots (Grill/Cooking Pot/Kettle/
// Frying Pan) — null means cookable over the open flame with no
// accessory, same as Raw Meat -> Cooked Meat's case. See
// CAMPFIRE_PLANNING.md.
[CreateAssetMenu(menuName = "Gridless/Cookable Item", fileName = "NewCookable")]
public class CookableItem : ScriptableObject
{
    [System.Serializable]
    public class Ingredient
    {
        public ItemDefinition item;
        public int count = 1;
    }

    public Ingredient[] ingredients;
    public ItemDefinition outputItem;
    public int outputCount = 1;
    public float cookDurationSeconds = 30f;
    public ItemDefinition requiredAccessory;
}
