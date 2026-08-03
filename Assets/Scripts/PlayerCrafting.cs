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

    // True if every ingredient's required count is currently held.
    public bool HasIngredients(CraftingRecipe recipe)
    {
        if (recipe?.ingredients == null) return false;

        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient == null || ingredient.item == null) continue;
            if (inventory.GetCount(ingredient.item) < ingredient.count) return false;
        }

        return true;
    }

    public bool TryCraft(CraftingRecipe recipe)
    {
        if (recipe == null || recipe.outputItem == null || recipe.ingredients == null) return false;

        // Checked before removing any ingredient so a full inventory can't
        // consume materials without being able to hold the output.
        if (!inventory.Inventory.HasSpaceFor(recipe.outputItem, recipe.outputCount)) return false;
        if (!HasIngredients(recipe)) return false;

        foreach (var ingredient in recipe.ingredients)
            inventory.RemoveItem(ingredient.item, ingredient.count);

        inventory.AddItem(recipe.outputItem, recipe.outputCount);
        skills?.GainExperience(recipe.trainedSkill, recipe.skillGain);
        return true;
    }
}
