using UnityEngine;

// Mirrors CookableItem.cs's exact shape — a small registered recipe type for
// the Furnace's own automated smelting queue (2026-08-13), deliberately
// separate from CraftingRecipe even though IronIngotRecipe (a CraftingRecipe)
// already smelts Iron Ore into an Iron Ingot near a Furnace. That existing
// recipe is player-driven: skill-gated, tool-gated, subject to the
// chance-of-creation roll, crafted from the Crafting tab. This type is for
// the Furnace's unattended production queue — deterministic, no skill
// involved, no risk of failure — same reasoning CookableItem stayed separate
// from CraftingRecipe for Campfire cooking. Both can reference the same
// ItemDefinitions; nothing about one affects the other.
[CreateAssetMenu(menuName = "Gridless/Smeltable Item", fileName = "NewSmeltable")]
public class SmeltableItem : ScriptableObject
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
    public float smeltDurationSeconds = 60f;
}
