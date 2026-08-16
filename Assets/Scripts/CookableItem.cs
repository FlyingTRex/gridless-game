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

    // Mirrors CraftingRecipe.requiresCanteenWater/canteenWaterAmount
    // (2026-08-15, Herbal Tea) — false/0 (default) means no such
    // requirement, same "most recipes don't need this" convention. True
    // means an equipped Canteen holding Water must have at least
    // canteenWaterAmount, consumed on StartCooking the same way
    // PlayerCrafting.StartCraft consumes it. See Campfire.HasCanteenWater.
    public bool requiresCanteenWater;
    public float canteenWaterAmount = 20f;

    // Cooking skill/quality-tier system (2026-08-15, COOKING_SKILL_PLANNING.md)
    // — null trainedSkill skips the outcome roll entirely and always
    // succeeds, same "opt-in" convention PlayerCrafting's skill-less
    // gadget recipes already use (keeps RawMeatToCookedMeatCookable, the
    // original baseline recipe, exactly as free/risk-free as it always
    // was). requiredSkillLevel is a flat int, deliberately NOT routed
    // through CraftTierScale.SkillRequirement(outputItem.tier) — food
    // items don't use the CraftTier ladder for this, and a scale tuned
    // for crafting-quality tiers doesn't transfer to cooking difficulty
    // (see CLAUDE.md's tier-scaling gotcha). See Campfire.TickCooking's
    // outcome roll and HasRequiredCookingSkill.
    public SkillDefinition trainedSkill;
    public float skillGain = 1f;
    public int requiredSkillLevel;
}
