using UnityEngine;

// Build tab inside PlayerMenuScreen (Tab key). Lists pieces and lets the
// player arm one, but placement itself happens in the world (see
// PlayerBuilding), not a button here. Unlike Magic, Building gets full UI
// support on purpose (design-brief.md's Building System section) — this
// tab, the ghost preview, and the not-enough-materials message all show
// deliberately, not hidden.
//
// Rewritten from a text list to a tile grid + search bar (2026-08-09,
// Ben's call — "let's do the same thing with the build tab" following
// Crafting's own redesign), reusing that exact pattern: big icon, live
// materials, a search bar that ignores nothing since there's no
// discipline-tab split here. Unlike Crafting, there's no batch/quantity/
// timer — placement is still one deliberate walk-and-aim act per piece,
// nothing here queues or produces instantly, so Arm/Armed stays exactly
// as it worked before.
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerSkills))]
[RequireComponent(typeof(PlayerBuilding))]
public class BuildScreen : MonoBehaviour
{
    private const float TileWidth = 200f;
    private const int TilesPerRow = 4;
    private const float TileSpacing = 12f;
    private const float IconSize = 72f;
    private const float IconPadding = 6f;

    private PlayerSkills skills;
    private PlayerBuilding building;

    // Case-insensitive substring match against pieceName — same shape as
    // CraftingScreen's search, just with no discipline-tab filter to
    // override since Build has never had sub-tabs.
    private string searchQuery = "";

    private void Awake()
    {
        skills = GetComponent<PlayerSkills>();
        building = GetComponent<PlayerBuilding>();
    }

    // Called by PlayerMenuScreen while its Build tab is active.
    public void DrawContent()
    {
        GUILayout.Label("Building", DebugGUI.Header);
        GUILayout.Label("Left Mouse Button to place, Scroll Wheel to rotate, Right Mouse Button to cancel.", DebugGUI.Label);
        GUILayout.Space(6);
        DrawSearchBar();
        GUILayout.Space(10);

        bool searching = !string.IsNullOrWhiteSpace(searchQuery);
        string queryLower = searching ? searchQuery.Trim().ToLowerInvariant() : null;

        bool any = false;
        int column = 0;

        foreach (var piece in building.AllPieces)
        {
            if (piece == null) continue;
            if (searching && !piece.pieceName.ToLowerInvariant().Contains(queryLower)) continue;

            if (column == 0) GUILayout.BeginHorizontal();
            any = true;

            DrawTile(piece);

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

        if (!any)
            GUILayout.Label(searching ? $"No pieces match \"{searchQuery}\"." : "No pieces available.", DebugGUI.Label);
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

    private void DrawTile(BuildPiece piece)
    {
        bool unlocked = building.CanPlace(piece);
        bool armed = building.ArmedPiece == piece;

        GUILayout.BeginVertical(DebugGUI.Panel, GUILayout.Width(TileWidth));

        DrawIcon(piece);

        GUILayout.Label(piece.pieceName, unlocked ? DebugGUI.Header : DebugGUI.Warning);

        if (piece.ingredients != null)
        {
            foreach (var ingredient in piece.ingredients)
            {
                if (ingredient == null || ingredient.item == null) continue;
                int have = building.GetAvailableCount(ingredient.item);
                var style = have < ingredient.count ? DebugGUI.Warning : DebugGUI.Label;
                GUILayout.Label($"{ingredient.item.itemName}: {ingredient.count} (have {have})", style);
            }
        }

        if (!unlocked)
        {
            int required = piece.trainedSkill != null ? CraftTierScale.SkillRequirement(piece.unlockTier) : 0;
            GUILayout.Label($"— requires {piece.trainedSkill.skillName} {required}", DebugGUI.Warning);
        }

        GUI.enabled = unlocked;
        if (GUILayout.Button(armed ? "Armed (click to cancel)" : "Arm", GUILayout.Width(TileWidth - 20f)))
            building.ArmPiece(armed ? null : piece);
        GUI.enabled = true;

        GUILayout.EndVertical();
    }

    // previewIcon preferred over icon (same convention as ItemDefinition/
    // CraftingRecipe's tiles) — blank spacer, not a placeholder glyph,
    // for a piece with neither baked yet.
    private void DrawIcon(BuildPiece piece)
    {
        var sprite = piece.previewIcon != null ? piece.previewIcon : piece.icon;

        GUILayout.Box(GUIContent.none, GUILayout.Width(IconSize), GUILayout.Height(IconSize));
        if (sprite == null) return;

        var rect = GUILayoutUtility.GetLastRect();
        var iconRect = new Rect(
            rect.x + IconPadding, rect.y + IconPadding,
            rect.width - IconPadding * 2f, rect.height - IconPadding * 2f);
        GUI.DrawTexture(iconRect, sprite.texture, ScaleMode.ScaleToFit);
    }
}
