using UnityEngine;

// A placeable building piece — sibling to CraftingRecipe/WishRecipe, same
// "decided in shape" status as the rest of the Building System
// (design-brief.md, 2026-08-08). Reuses CraftingRecipe.Ingredient directly
// rather than duplicating that class.
[CreateAssetMenu(menuName = "Gridless/Build Piece", fileName = "NewBuildPiece")]
public class BuildPiece : ScriptableObject
{
    public string pieceName = "New Piece";
    public GameObject prefab;
    public CraftingRecipe.Ingredient[] ingredients;
    public SkillDefinition trainedSkill;
    public CraftTier unlockTier = CraftTier.Crude;
    public float skillGain = 1f;

    // How far down (from wherever the player aims) this piece can reach to
    // meet the ground before placement just fails — Foundation's own
    // terrain-leveling tolerance. Not used by every future piece type
    // (Wall/Door won't reach for ground at all, they attach to a socket),
    // so this stays 0 (unused) for anything that isn't ground-reaching.
    public float groundReach = 5f;

    // The next tier up this piece's material ladder (Twig->Plank->Rock->
    // Metal) — null if already at the top, or if no upgrade path exists
    // yet. Used by PlayerPieceUpgrade's click-to-upgrade action, added
    // 2026-08-08 — see design-brief.md's Building System section.
    public BuildPiece nextTier;

    // Optional — same "bake from the prefab, blank spacer if unset"
    // convention as ItemDefinition.icon/previewIcon (2026-08-09, Build
    // tab's tile-grid redesign). Null (the default) means BuildScreen
    // draws a blank spacer for this tile, not a placeholder glyph.
    public Sprite icon;
    public Sprite previewIcon;
}
