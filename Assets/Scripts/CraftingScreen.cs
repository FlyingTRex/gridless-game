using UnityEngine;

// Crafting recipe list — drawn as the Crafting tab inside PlayerMenuScreen
// (Tab key). Used to be its own screen toggled with O; folded in
// 2026-08-04 so Inventory/Skills/Crafting all live under one key instead of
// three. Lists every known recipe (not just ones you currently happen to be
// holding the input for) alongside how many of the input you have on hand.
//
// Sub-tabbed by discipline (2026-08-05) — a flat list stopped scaling once
// the recipe count jumped from ~9 to 25 with the tool tiers. `disciplines`
// is an explicit, hand-maintained list (like GameMenuScreen.ControlsList or
// PlayerCrafting.recipes) rather than discovered dynamically, so a
// discipline with zero recipes still shows its own tab with an honest
// placeholder instead of silently not existing. Recipes with no
// `trainedSkill` (gadgets that don't have a clean defining material — see
// docs/design-brief.md's 2026-08-05 discipline-sort update) land in the
// fixed "Other" tab.
[RequireComponent(typeof(PlayerCrafting))]
[RequireComponent(typeof(PlayerInventory))]
public class CraftingScreen : MonoBehaviour
{
    private const float TabWidth = 130f;
    private const float TabHeight = 28f;
    private const int OtherTabIndex = -1;

    [SerializeField] private SkillDefinition[] disciplines;

    private PlayerCrafting crafting;
    private PlayerInventory playerInventory;
    private int currentDisciplineIndex;

    private void Awake()
    {
        crafting = GetComponent<PlayerCrafting>();
        playerInventory = GetComponent<PlayerInventory>();
    }

    // Called by PlayerMenuScreen while its Crafting tab is active.
    public void DrawContent()
    {
        GUILayout.Label("Crafting", DebugGUI.Header);
        DrawDisciplineTabs();
        GUILayout.Space(10);

        SkillDefinition wantSkill = currentDisciplineIndex != OtherTabIndex && disciplines != null
            && currentDisciplineIndex >= 0 && currentDisciplineIndex < disciplines.Length
            ? disciplines[currentDisciplineIndex]
            : null;

        CraftingRecipe craftClicked = null;
        var recipes = crafting.Recipes;
        bool any = false;
        if (recipes != null)
        {
            foreach (var recipe in recipes)
            {
                if (recipe == null || recipe.outputItem == null || recipe.ingredients == null || recipe.ingredients.Length == 0)
                    continue;
                if (recipe.trainedSkill != wantSkill) continue;

                any = true;

                bool hasEnough = crafting.HasIngredients(recipe);
                bool hasSpace = playerInventory.Inventory.HasSpaceFor(recipe.outputItem, recipe.outputCount)
                    && (recipe.bonusItem == null || playerInventory.Inventory.HasSpaceFor(recipe.bonusItem, recipe.bonusCount));
                bool hasTool = crafting.HasRequiredTool(recipe);
                bool hasSkill = crafting.HasRequiredSkill(recipe);
                int requiredSkill = recipe.outputItem != null ? CraftTierScale.SkillRequirement(recipe.outputItem.tier) : 0;

                string needs = "";
                foreach (var ingredient in recipe.ingredients)
                {
                    if (ingredient == null || ingredient.item == null) continue;
                    if (needs.Length > 0) needs += ", ";
                    needs += $"{ingredient.count}x {ingredient.item.itemName} (have {crafting.GetAvailableCount(ingredient.item)})";
                }

                string label = $"{recipe.outputItem.itemName}";
                if (recipe.bonusItem != null)
                    label += $" + {recipe.bonusCount}x {recipe.bonusItem.itemName}";
                label += $"  (needs {needs})";
                if (recipe.requiredTools != null && recipe.requiredTools.Length > 0)
                    label += hasTool ? $"  [{recipe.requiredToolLabel} in hand]" : $"  — requires {recipe.requiredToolLabel} in hand";
                if (requiredSkill > 0 && !hasSkill)
                    label += $"  — requires {recipe.trainedSkill.skillName} {requiredSkill}";
                if (hasEnough && !hasSpace)
                    label += "  — inventory full";

                GUILayout.BeginHorizontal();
                GUILayout.Label(label, DebugGUI.Label);

                // Greyed out and unclickable rather than a button that
                // silently does nothing when the recipe can't be made —
                // the missing feedback that made a failed craft look like
                // nothing happened at all.
                GUI.enabled = hasEnough && hasSpace && hasTool && hasSkill;
                if (GUILayout.Button("Craft", GUILayout.Width(60)))
                    craftClicked = recipe;
                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }
        }

        if (!any)
            GUILayout.Label("No recipes yet.", DebugGUI.Label);

        if (craftClicked != null)
            crafting.TryCraft(craftClicked);
    }

    private void DrawDisciplineTabs()
    {
        GUILayout.BeginHorizontal();

        if (disciplines != null)
        {
            for (int i = 0; i < disciplines.Length; i++)
            {
                if (disciplines[i] == null) continue;
                var style = currentDisciplineIndex == i ? DebugGUI.TabSelected : DebugGUI.TabUnselected;
                if (GUILayout.Button(disciplines[i].skillName, style, GUILayout.Width(TabWidth), GUILayout.Height(TabHeight)))
                    currentDisciplineIndex = i;
            }
        }

        var otherStyle = currentDisciplineIndex == OtherTabIndex ? DebugGUI.TabSelected : DebugGUI.TabUnselected;
        if (GUILayout.Button("Other", otherStyle, GUILayout.Width(TabWidth), GUILayout.Height(TabHeight)))
            currentDisciplineIndex = OtherTabIndex;

        GUILayout.EndHorizontal();
    }
}
