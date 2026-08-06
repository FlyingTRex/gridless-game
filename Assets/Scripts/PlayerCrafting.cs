using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
public class PlayerCrafting : MonoBehaviour
{
    [SerializeField] private CraftingRecipe[] recipes;
    [SerializeField] private float storageRange = 10f;

    private PlayerInventory inventory;
    private PlayerSkills skills;
    private PlayerBackpack backpackCarrier;
    private PlayerEquipment equipment;
    private readonly List<StorageBox> nearbyStorages = new List<StorageBox>();

    // Read by CraftingScreen (the Crafting tab of PlayerMenuScreen, Tab key)
    // to render the recipe list.
    public IReadOnlyList<CraftingRecipe> Recipes => recipes;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        skills = GetComponent<PlayerSkills>();
        backpackCarrier = GetComponent<PlayerBackpack>();
        equipment = GetComponent<PlayerEquipment>();
    }

    // Every Inventory a recipe is allowed to draw materials from: the main
    // inventory, an equipped backpack's contents, and any StorageBox
    // within storageRange — not just what's directly in your hands/main
    // slots. Crafted output still only ever goes to the main inventory.
    private IEnumerable<Inventory> ReachableInventories()
    {
        yield return inventory.Inventory;

        var backpack = backpackCarrier != null ? backpackCarrier.Equipped : null;
        if (backpack != null)
            yield return backpack.Inventory;

        StorageBox.FindNearby(transform.position, storageRange, nearbyStorages);
        foreach (var box in nearbyStorages)
            yield return box.Inventory;
    }

    // Read by CraftingScreen's DrawContent() to show how much of an ingredient you
    // actually have access to, matching what HasIngredients/TryCraft use —
    // not just what's in the main inventory.
    public int GetAvailableCount(ItemDefinition item)
    {
        int total = 0;
        foreach (var inv in ReachableInventories())
            total += inv.GetCount(item);
        return total;
    }

    // True if every ingredient's required count is currently reachable.
    public bool HasIngredients(CraftingRecipe recipe)
    {
        if (recipe?.ingredients == null) return false;

        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient == null || ingredient.item == null) continue;
            if (GetAvailableCount(ingredient.item) < ingredient.count) return false;
        }

        return true;
    }

    // True if recipe has no tool requirement, or any one of its
    // requiredTools is currently held in a hand (not consumed — same "any
    // tier counts" check ResourceNode uses for Pickaxe/Axe gating).
    public bool HasRequiredTool(CraftingRecipe recipe)
    {
        if (recipe?.requiredTools == null || recipe.requiredTools.Length == 0) return true;
        if (equipment == null) return false;

        foreach (var tool in recipe.requiredTools)
        {
            if (tool != null && equipment.HasInHand(tool)) return true;
        }

        return false;
    }

    public bool TryCraft(CraftingRecipe recipe)
    {
        if (recipe == null || recipe.outputItem == null || recipe.ingredients == null) return false;
        if (!HasRequiredTool(recipe)) return false;

        // Checked before removing any ingredient so a full inventory can't
        // consume materials without being able to hold the output.
        if (!inventory.Inventory.HasSpaceFor(recipe.outputItem, recipe.outputCount)) return false;
        if (!HasIngredients(recipe)) return false;

        foreach (var ingredient in recipe.ingredients)
            RemoveAcrossReachable(ingredient.item, ingredient.count);

        inventory.AddItem(recipe.outputItem, recipe.outputCount);
        skills?.GainExperience(recipe.trainedSkill, recipe.skillGain);
        return true;
    }

    // Takes from the main inventory first, then the backpack, then each
    // nearby box in distance order, until amount is fully removed. Safe to
    // call only after HasIngredients confirmed enough exists in total.
    private void RemoveAcrossReachable(ItemDefinition item, int amount)
    {
        foreach (var inv in ReachableInventories())
        {
            if (amount <= 0) return;

            int have = inv.GetCount(item);
            if (have <= 0) continue;

            int take = Mathf.Min(have, amount);
            inv.RemoveItem(item, take);
            amount -= take;
        }
    }
}
