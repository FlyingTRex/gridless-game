using System.Collections.Generic;
using UnityEngine;

// Crafting recipe grid — drawn as the Crafting tab inside PlayerMenuScreen
// (Tab key). Used to be its own screen toggled with O; folded in
// 2026-08-04 so Inventory/Skills/Crafting all live under one key instead of
// three.
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
//
// Rewritten from a flat text list to a tile grid (2026-08-09, Ben's call):
// each tile shows a big icon, the batch-quantity stepper, a Max button, and
// (while its own batch is running) a progress bar instead of the stepper —
// see PlayerCrafting.StartCraft/CancelCraft for the queue itself.
[RequireComponent(typeof(PlayerCrafting))]
[RequireComponent(typeof(PlayerInventory))]
public class CraftingScreen : MonoBehaviour
{
    private const float TabWidth = 130f;
    private const float TabHeight = 28f;
    private const int OtherTabIndex = -1;

    private const float TileWidth = 200f;
    private const int TilesPerRow = 4;
    private const float TileSpacing = 12f;
    private const float IconSize = 72f;
    private const float IconPadding = 6f;
    private const float ProgressBarHeight = 8f;

    [SerializeField] private SkillDefinition[] disciplines;

    private PlayerCrafting crafting;
    private PlayerInventory playerInventory;
    private int currentDisciplineIndex;

    // Player-chosen batch size per recipe — session-local UI state, not
    // saved, defaults to 1 the first time a recipe's tile draws.
    private readonly Dictionary<CraftingRecipe, int> desiredQuantity = new Dictionary<CraftingRecipe, int>();

    // Case-insensitive substring match against outputItem.itemName. While
    // non-empty, search replaces the discipline-tab filter entirely
    // (searches every recipe regardless of tab) rather than narrowing
    // within the current tab — the whole point is not having to remember
    // which tab something lives in.
    private string searchQuery = "";

    // Tier filter/sort — session-local UI state, not saved (CRAFT_TIER_COLORS_PLANNING.md
    // Decision 3). null filter = show every tier. Sort is now the default
    // browsing order (replacing the old implicit family-grouped order),
    // ascending (Crude first) unless toggled.
    private CraftTier? tierFilter;
    private bool sortAscending = true;

    private static readonly CraftTier[] AllTiers = (CraftTier[])System.Enum.GetValues(typeof(CraftTier));

    private Texture2D barBackgroundTex;
    private Texture2D barFillTex;

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
        GUILayout.Space(6);
        DrawSearchBar();
        GUILayout.Space(6);
        DrawTierFilterRow();
        GUILayout.Space(10);

        bool searching = !string.IsNullOrWhiteSpace(searchQuery);
        SkillDefinition wantSkill = !searching && currentDisciplineIndex != OtherTabIndex && disciplines != null
            && currentDisciplineIndex >= 0 && currentDisciplineIndex < disciplines.Length
            ? disciplines[currentDisciplineIndex]
            : null;
        string queryLower = searching ? searchQuery.Trim().ToLowerInvariant() : null;

        var recipes = crafting.Recipes;
        var visible = new List<CraftingRecipe>();

        if (recipes != null)
        {
            foreach (var recipe in recipes)
            {
                if (recipe == null || recipe.outputItem == null || recipe.ingredients == null || recipe.ingredients.Length == 0)
                    continue;

                if (searching)
                {
                    if (!recipe.outputItem.itemName.ToLowerInvariant().Contains(queryLower)) continue;
                }
                else if (recipe.trainedSkill != wantSkill)
                {
                    continue;
                }

                if (tierFilter.HasValue && recipe.outputItem.tier != tierFilter.Value) continue;

                visible.Add(recipe);
            }
        }

        // Tier-ascending is the default browsing order (Ben's call —
        // "finding recipes would be easier" than the old implicit
        // family-grouped array order). Sign flips for the descending toggle.
        int direction = sortAscending ? 1 : -1;
        visible.Sort((a, b) => direction * a.outputItem.tier.CompareTo(b.outputItem.tier));

        int column = 0;
        foreach (var recipe in visible)
        {
            if (column == 0) GUILayout.BeginHorizontal();

            DrawTile(recipe);

            column++;
            if (column >= TilesPerRow)
            {
                GUILayout.EndHorizontal();
                GUILayout.Space(TileSpacing);
                column = 0;
            }
            else
            {
                GUILayout.Space(TileSpacing);
            }
        }

        if (column > 0)
            GUILayout.EndHorizontal();

        if (visible.Count == 0)
            GUILayout.Label(searching ? $"No recipes match \"{searchQuery}\"." : "No recipes yet.", DebugGUI.Label);
    }

    // Tier filter chips (All + one per tier, colored per CraftTierColors)
    // ANDed with the discipline-tab/search filter above, plus a sort-
    // direction toggle. ANDed, not exclusive with search/tabs — narrows
    // whatever's already showing rather than replacing it.
    private void DrawTierFilterRow()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Tier:", DebugGUI.Label, GUILayout.Width(55));

        var allStyle = !tierFilter.HasValue ? DebugGUI.TabSelected : DebugGUI.TabUnselected;
        if (GUILayout.Button("All", allStyle, GUILayout.Width(50), GUILayout.Height(TabHeight)))
            tierFilter = null;

        foreach (var tier in AllTiers)
        {
            bool selected = tierFilter.HasValue && tierFilter.Value == tier;
            var style = selected ? DebugGUI.TabSelected : DebugGUI.TabUnselected;
            GUI.contentColor = selected ? Color.white : CraftTierColors.Get(tier);
            if (GUILayout.Button(tier.ToString(), style, GUILayout.Width(95), GUILayout.Height(TabHeight)))
                tierFilter = tier;
            GUI.contentColor = Color.white;
        }

        GUILayout.FlexibleSpace();
        if (GUILayout.Button(sortAscending ? "Tier 1 -> 5" : "Tier 5 -> 1", GUILayout.Width(100), GUILayout.Height(TabHeight)))
            sortAscending = !sortAscending;

        GUILayout.EndHorizontal();
    }

    private void DrawSearchBar()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Search:", DebugGUI.Label, GUILayout.Width(55));
        searchQuery = GUILayout.TextField(searchQuery, GUILayout.Width(220));
        if (!string.IsNullOrEmpty(searchQuery) && GUILayout.Button("Clear", GUILayout.Width(50)))
            searchQuery = "";
        GUILayout.EndHorizontal();
    }

    private void DrawTile(CraftingRecipe recipe)
    {
        GUILayout.BeginVertical(DebugGUI.Panel, GUILayout.Width(TileWidth));

        DrawIcon(recipe.outputItem);

        GUILayout.Label(recipe.outputItem.itemName, DebugGUI.TierNameCentered(recipe.outputItem.tier));
        if (recipe.bonusItem != null)
            GUILayout.Label($"+ {recipe.bonusCount}x {recipe.bonusItem.itemName}", DebugGUI.Label);

        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient == null || ingredient.item == null) continue;
            int have = crafting.GetAvailableCount(ingredient.item);
            var style = have < ingredient.count ? DebugGUI.Warning : DebugGUI.Label;
            GUILayout.Label($"{ingredient.item.itemName}: {ingredient.count} (have {have})", style);
        }

        bool hasTool = crafting.HasRequiredTool(recipe);
        bool hasSkill = crafting.HasRequiredSkill(recipe);
        bool hasAnvilSurface = crafting.HasNearbyAnvilSurface(recipe);
        bool hasFurnace = crafting.HasNearbyFurnace(recipe);
        int requiredSkill = CraftTierScale.SkillRequirement(recipe.outputItem.tier);

        if (recipe.requiredTools != null && recipe.requiredTools.Length > 0)
            GUILayout.Label(hasTool ? $"[{recipe.requiredToolLabel} in hand]" : $"— requires {recipe.requiredToolLabel} in hand",
                hasTool ? DebugGUI.Label : DebugGUI.Warning);
        if (requiredSkill > 0 && !hasSkill)
            GUILayout.Label($"— requires {recipe.trainedSkill.skillName} {requiredSkill}", DebugGUI.Warning);
        if (recipe.requiresAnvilSurface && !hasAnvilSurface)
            GUILayout.Label("— requires a Boulder or Anvil nearby", DebugGUI.Warning);
        if (recipe.requiresFurnace && !hasFurnace)
            GUILayout.Label("— requires a Furnace nearby", DebugGUI.Warning);

        bool isActiveHere = crafting.IsCrafting && crafting.ActiveRecipe == recipe;

        if (isActiveHere)
            DrawProgress();
        else
            DrawQuantityAndCraft(recipe, hasTool, hasSkill, hasAnvilSurface, hasFurnace);

        GUILayout.EndVertical();
    }

    // previewIcon (a separately-baked, higher-resolution image) is
    // preferred over icon — icon is only baked ~32x32 for inline-row use
    // and looks blurry stretched up to this size. Ben's call
    // (2026-08-09): an item with neither baked yet gets a blank spacer
    // here, not a placeholder glyph — several recipe outputs (the Trimmed
    // Stick tiers, a few gadgets) don't have one yet.
    private void DrawIcon(ItemDefinition item)
    {
        var sprite = item.previewIcon != null ? item.previewIcon : item.icon;

        GUILayout.Box(GUIContent.none, GUILayout.Width(IconSize), GUILayout.Height(IconSize));
        var rect = GUILayoutUtility.GetLastRect();
        GUI.DrawTexture(rect, DebugGUI.TierBorder(item.tier));
        if (sprite == null) return;

        var iconRect = new Rect(
            rect.x + IconPadding, rect.y + IconPadding,
            rect.width - IconPadding * 2f, rect.height - IconPadding * 2f);
        GUI.DrawTexture(iconRect, sprite.texture, ScaleMode.ScaleToFit);
    }

    private void DrawQuantityAndCraft(CraftingRecipe recipe, bool hasTool, bool hasSkill, bool hasAnvilSurface, bool hasFurnace)
    {
        bool blockedByOtherBatch = crafting.IsCrafting;
        int maxCraftable = crafting.MaxCraftable(recipe);

        int quantity = desiredQuantity.TryGetValue(recipe, out var q) ? q : 1;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-", GUILayout.Width(24))) quantity -= 1;
        GUILayout.Label(quantity.ToString(), DebugGUI.Header, GUILayout.Width(40));
        if (GUILayout.Button("+", GUILayout.Width(24))) quantity += 1;
        GUILayout.EndHorizontal();

        quantity = Mathf.Clamp(quantity, 1, Mathf.Max(1, maxCraftable));
        desiredQuantity[recipe] = quantity;

        bool hasSpace = playerInventory.Inventory.HasSpaceFor(recipe.outputItem, recipe.outputCount * quantity)
            && (recipe.bonusItem == null || playerInventory.Inventory.HasSpaceFor(recipe.bonusItem, recipe.bonusCount * quantity));
        bool gatesOk = !blockedByOtherBatch && hasTool && hasSkill && hasAnvilSurface && hasFurnace;

        GUILayout.BeginHorizontal();
        GUI.enabled = gatesOk && hasSpace && maxCraftable >= quantity;
        if (GUILayout.Button("Craft", GUILayout.Width(90)))
            crafting.StartCraft(recipe, quantity);
        GUI.enabled = true;

        GUI.enabled = gatesOk && maxCraftable > 0;
        if (GUILayout.Button("Max", GUILayout.Width(60)))
            desiredQuantity[recipe] = maxCraftable;
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        if (blockedByOtherBatch)
            GUILayout.Label("Crafting queue busy", DebugGUI.Warning);
        else if (gatesOk && !hasSpace && maxCraftable >= quantity)
            GUILayout.Label("— inventory full", DebugGUI.Warning);
    }

    // Shown instead of the quantity/Craft row while this tile's own batch
    // is the one currently running. The timer keeps advancing even if the
    // player closes this tab entirely (see PlayerCrafting.Update) — this
    // is just a view onto that state, not what drives it.
    private void DrawProgress()
    {
        float duration = crafting.ActiveItemDuration;
        float elapsed = crafting.ActiveElapsed;
        float fraction = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
        float remaining = Mathf.Max(0f, duration - elapsed);

        GUILayout.Label($"Crafting {crafting.ActiveCompleted + 1} / {crafting.ActiveTotal}  ({remaining:F1}s)", DebugGUI.Label);

        if (barBackgroundTex == null) barBackgroundTex = SolidTexture(new Color(0f, 0f, 0f, 0.6f));
        if (barFillTex == null) barFillTex = SolidTexture(new Color(0.25f, 0.85f, 0.25f));

        var rect = GUILayoutUtility.GetRect(TileWidth - 24f, ProgressBarHeight);
        GUI.DrawTexture(rect, barBackgroundTex);
        var fillRect = new Rect(rect.x, rect.y, rect.width * fraction, rect.height);
        GUI.DrawTexture(fillRect, barFillTex);

        GUILayout.Space(6);
        if (GUILayout.Button("Cancel", GUILayout.Width(90)))
            crafting.CancelCraft();
    }

    private static Texture2D SolidTexture(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
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
