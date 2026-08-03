using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
public class PlayerCrafting : MonoBehaviour
{
    [SerializeField] private CraftingRecipe[] recipes;

    private PlayerInventory inventory;
    private PlayerSkills skills;

    // Read by CraftingScreen (toggled with O) to render the recipe list.
    public IReadOnlyList<CraftingRecipe> Recipes => recipes;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        skills = GetComponent<PlayerSkills>();
    }

    public CraftingRecipe FindRecipe(ItemDefinition item)
    {
        if (item == null || recipes == null) return null;

        foreach (var recipe in recipes)
        {
            if (recipe != null && recipe.inputItem == item)
                return recipe;
        }

        return null;
    }

    public bool TryCraft(ItemDefinition item)
    {
        var recipe = FindRecipe(item);
        if (recipe == null) return false;

        // Checked before removing the input so a full inventory can't consume
        // the input without being able to hold the output.
        if (!inventory.Inventory.HasSpaceFor(recipe.outputItem, recipe.outputCount)) return false;
        if (!inventory.RemoveItem(recipe.inputItem, recipe.inputCount)) return false;

        inventory.AddItem(recipe.outputItem, recipe.outputCount);
        skills?.GainExperience(recipe.trainedSkill, recipe.skillGain);
        return true;
    }
}
