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
    public SkillDefinition trainedSkill;
    public float skillGain = 1f;

    // Null/empty (default) means no tool needed. Populate to require a tool
    // held in a hand (not consumed, unlike ingredients) — any one of these
    // satisfies it, same "any tier counts" convention as
    // ResourceNode.requiredTools (e.g. any of the 5 Knife tiers can carve a
    // Stick). requiredToolLabel is the display name shown in the Crafting
    // tab, independent of which exact tier is actually held.
    public ItemDefinition[] requiredTools;
    public string requiredToolLabel;
}
