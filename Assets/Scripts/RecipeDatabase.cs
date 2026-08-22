using System.Collections.Generic;
using UnityEngine;

// Reverse lookup (output item -> the recipe that produces it), needed by
// ItemValueCalculator (COMMERCE_PLANNING.md, MVP2B_PLANNING.md item 2) to
// walk a craftable item's ingredient chain. Nothing in the project builds
// this today -- CraftingRecipe/SmeltableItem are only ever looked up
// forward (by whoever already holds a reference to one), never "given this
// item, what recipe makes it." Same shape and same reason as
// ItemDatabase/SkillDatabase/NPCJobDatabase/BuildPieceDatabase: a
// hand-maintained registration array would silently go stale the moment
// someone adds a new recipe and forgets to register it, so this is
// populated by DatabaseRepopulator's existing AssetDatabase-scan pattern
// instead, same "run before any commit that adds a new recipe" habit.
[CreateAssetMenu(menuName = "Gridless/Recipe Database", fileName = "RecipeDatabase")]
public class RecipeDatabase : ScriptableObject
{
    [SerializeField] private CraftingRecipe[] craftingRecipes = System.Array.Empty<CraftingRecipe>();
    [SerializeField] private SmeltableItem[] smeltableItems = System.Array.Empty<SmeltableItem>();

    private static RecipeDatabase instance;
    public static RecipeDatabase Instance =>
        instance != null ? instance : instance = Resources.Load<RecipeDatabase>("RecipeDatabase");

    private Dictionary<ItemDefinition, CraftingRecipe> craftingByOutput;
    private Dictionary<ItemDefinition, SmeltableItem> smeltableByOutput;

    // A recipe whose output has no CraftingRecipe/SmeltableItem at all
    // (raw/gathered materials) returns null from both -- ItemValueCalculator
    // treats that as "needs a hand-seeded root baseValue," not an error.
    public CraftingRecipe FindCraftingRecipe(ItemDefinition output)
    {
        if (output == null) return null;
        if (craftingByOutput == null) BuildLookups();
        return craftingByOutput.TryGetValue(output, out var recipe) ? recipe : null;
    }

    public SmeltableItem FindSmeltableItem(ItemDefinition output)
    {
        if (output == null) return null;
        if (smeltableByOutput == null) BuildLookups();
        return smeltableByOutput.TryGetValue(output, out var smeltable) ? smeltable : null;
    }

    private void BuildLookups()
    {
        craftingByOutput = new Dictionary<ItemDefinition, CraftingRecipe>(craftingRecipes.Length);
        foreach (var recipe in craftingRecipes)
            if (recipe != null && recipe.outputItem != null)
                craftingByOutput[recipe.outputItem] = recipe;

        smeltableByOutput = new Dictionary<ItemDefinition, SmeltableItem>(smeltableItems.Length);
        foreach (var smeltable in smeltableItems)
            if (smeltable != null && smeltable.outputItem != null)
                smeltableByOutput[smeltable.outputItem] = smeltable;
    }

#if UNITY_EDITOR
    // Sorted by asset name before assigning, same "two independent
    // regenerations of the same set must produce byte-identical output"
    // discipline ItemDatabase.EditorSetItems already established --
    // AssetDatabase.FindAssets' enumeration order isn't stable across
    // machines/runs, and an unsorted array would reshuffle (and merge-
    // conflict) on every regen even when the actual recipe set didn't
    // change.
    public void EditorSetRecipes(CraftingRecipe[] crafting, SmeltableItem[] smeltable)
    {
        System.Array.Sort(crafting, (a, b) =>
            string.CompareOrdinal(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty));
        System.Array.Sort(smeltable, (a, b) =>
            string.CompareOrdinal(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty));
        craftingRecipes = crafting;
        smeltableItems = smeltable;
        craftingByOutput = null;
        smeltableByOutput = null;
    }
#endif
}
