using UnityEngine;

[CreateAssetMenu(menuName = "Gridless/Crafting Recipe", fileName = "NewRecipe")]
public class CraftingRecipe : ScriptableObject
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

    // Optional secondary output alongside outputItem — e.g. trimming a
    // Stick with a Knife also recovers some Fiber. Null (default) means no
    // bonus output, same "most recipes don't need this" convention as
    // ItemDefinition.worldPickupPrefab. Only awarded when the craft
    // actually produces an item (see PlayerCrafting's chance-of-creation
    // roll, v0.1.82-dev) — not on a failure that produces nothing.
    public ItemDefinition bonusItem;
    public int bonusCount = 1;

    // The sibling recipe's output one CraftTier below/above this one, for
    // the same base item (e.g. Normal Knife's lowerTierItem is
    // Rudimentary Knife, higherTierItem is Fine Knife). Both null for a
    // single-tier item (Rope, Cloth, every gadget) or at the very bottom/
    // top of a ladder (Crude has no lowerTierItem, Masterwork has no
    // higherTierItem) — PlayerCrafting.TryCraft's chance-of-creation roll
    // falls back to a normal success when it calls for a tier shift with
    // nowhere to go.
    public ItemDefinition lowerTierItem;
    public ItemDefinition higherTierItem;

    public SkillDefinition trainedSkill;
    public float skillGain = 1f;

    // Null/empty (default) means no tool needed. Populate to require a tool
    // held in a hand (not consumed by a normal success — but see
    // PlayerCrafting's chance-of-creation roll, which can still break it
    // on a spectacular failure), same "any tier counts" convention as
    // ResourceNode.requiredTools (e.g. any of the 5 Knife tiers can carve a
    // Stick). requiredToolLabel is the display name shown in the Crafting
    // tab, independent of which exact tier is actually held.
    public ItemDefinition[] requiredTools;
    public string requiredToolLabel;

    // False (default) means no such requirement, same "most recipes don't
    // need this" convention as requiredTools. True means an AnvilSurface
    // (Boulder, Anvil, ...) must be within range — see
    // PlayerCrafting.HasNearbyAnvilSurface.
    public bool requiresAnvilSurface;

    // False (default) means no such requirement. True means an equipped
    // Canteen holding Water must have at least canteenWaterAmount —
    // consumed on craft the same way Canteen.Drink consumes it, just
    // feeding this recipe instead of Thirst (2026-08-10, Ben's call for
    // Healing Paste: "combine the herbs with water" — water is a real
    // tracked resource on the Canteen, not a stackable inventory item, so
    // it can't be a normal Ingredient). See
    // PlayerCrafting.HasCanteenWater/ConsumeCanteenWater.
    public bool requiresCanteenWater;
    public float canteenWaterAmount = 20f;

    // False (default) means no such requirement, same "most recipes don't
    // need this" convention as requiresAnvilSurface. True means a
    // FurnaceSurface (a placed Furnace) must be within range — see
    // PlayerCrafting.HasNearbyFurnace. Added for IronIngotRecipe
    // (2026-08-11): smelting Iron into an Ingot needs real heat, not just
    // a hard hammering surface.
    public bool requiresFurnace;
}
