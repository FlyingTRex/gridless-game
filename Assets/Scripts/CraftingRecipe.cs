using UnityEngine;

[CreateAssetMenu(menuName = "Gridless/Crafting Recipe", fileName = "NewRecipe")]
public class CraftingRecipe : ScriptableObject
{
    public ItemDefinition inputItem;
    public int inputCount = 1;
    public ItemDefinition outputItem;
    public int outputCount = 1;
    public SkillDefinition trainedSkill;
    public float skillGain = 1f;
}
