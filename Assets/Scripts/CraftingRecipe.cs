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
}
