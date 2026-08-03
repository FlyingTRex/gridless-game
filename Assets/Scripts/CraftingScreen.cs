using UnityEngine;
using UnityEngine.InputSystem;

// Crafting recipe list, toggled with O. Was previously an inline "(craft
// X)" button next to a matching item in InventoryScreen's main list;
// pulled out into its own screen, listing every known recipe (not just
// ones you currently happen to be holding the input for) alongside how
// many of the input you have on hand.
[RequireComponent(typeof(PlayerCrafting))]
[RequireComponent(typeof(PlayerInventory))]
public class CraftingScreen : MonoBehaviour
{
    private const float PanelWidth = 460f;
    private const float PanelHeight = 320f;

    private PlayerCrafting crafting;
    private PlayerInventory playerInventory;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        crafting = GetComponent<PlayerCrafting>();
        playerInventory = GetComponent<PlayerInventory>();
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.oKey.wasPressedThisFrame) return;

        // Always allow closing. Only allow opening from normal gameplay —
        // not while some other screen already has the cursor unlocked,
        // which would stack this on top of it.
        if (isOpen || Cursor.lockState == CursorLockMode.Locked)
            SetOpen(!isOpen);
    }

    // Called by FirstPersonController when Escape re-locks the cursor, so
    // the two toggles can't drift out of sync with each other.
    public void Close() => SetOpen(false);

    private void SetOpen(bool value)
    {
        isOpen = value;
        Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = value;
    }

    private void OnGUI()
    {
        if (!isOpen) return;

        var rect = new Rect((Screen.width - PanelWidth) / 2f, (Screen.height - PanelHeight) / 2f, PanelWidth, PanelHeight);
        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Label("Crafting", DebugGUI.Header);

        CraftingRecipe craftClicked = null;
        var recipes = crafting.Recipes;
        if (recipes != null)
        {
            foreach (var recipe in recipes)
            {
                if (recipe == null || recipe.outputItem == null || recipe.ingredients == null || recipe.ingredients.Length == 0)
                    continue;

                bool hasEnough = crafting.HasIngredients(recipe);
                bool hasSpace = playerInventory.Inventory.HasSpaceFor(recipe.outputItem, recipe.outputCount);

                string needs = "";
                foreach (var ingredient in recipe.ingredients)
                {
                    if (ingredient == null || ingredient.item == null) continue;
                    if (needs.Length > 0) needs += ", ";
                    needs += $"{ingredient.count}x {ingredient.item.itemName} (have {crafting.GetAvailableCount(ingredient.item)})";
                }

                string label = $"{recipe.outputItem.itemName}  (needs {needs})";
                if (hasEnough && !hasSpace)
                    label += "  — inventory full";

                GUILayout.BeginHorizontal();
                GUILayout.Label(label, DebugGUI.Label);

                // Greyed out and unclickable rather than a button that
                // silently does nothing when the recipe can't be made —
                // the missing feedback that made a failed craft look like
                // nothing happened at all.
                GUI.enabled = hasEnough && hasSpace;
                if (GUILayout.Button("Craft", GUILayout.Width(60)))
                    craftClicked = recipe;
                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }
        }

        if (craftClicked != null)
            crafting.TryCraft(craftClicked);

        if (GUILayout.Button("Close", GUILayout.Width(100)))
            SetOpen(false);

        GUILayout.EndArea();
    }
}
