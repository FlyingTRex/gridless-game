using System.Collections.Generic;
using UnityEngine;

// Stable-ID lookup for BuildPiece assets (SAVE_LOAD_PLANNING.md section 11)
// -- same shape as ItemDatabase/SkillDatabase/NPCJobDatabase, built for the
// same reason: a ScriptableObject reference can't serialize into save JSON
// directly, and SaveManager needs to resolve "which BuildPiece was this"
// back from a stable string on load. Populated via DatabaseRepopulator
// (Editor-only, full regen from an AssetDatabase scan). Lives at
// Assets/Resources/BuildPieceDatabase.asset so Resources.Load works in a
// build, unlike an AssetDatabase scan.
[CreateAssetMenu(menuName = "Gridless/Build Piece Database", fileName = "BuildPieceDatabase")]
public class BuildPieceDatabase : ScriptableObject
{
    [SerializeField] private BuildPiece[] pieces = System.Array.Empty<BuildPiece>();

    private static BuildPieceDatabase instance;
    public static BuildPieceDatabase Instance =>
        instance != null ? instance : instance = Resources.Load<BuildPieceDatabase>("BuildPieceDatabase");

    private Dictionary<string, BuildPiece> lookup;

    public string IdFor(BuildPiece piece) => piece != null ? piece.name : null;

    public BuildPiece Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (lookup == null) BuildLookup();
        return lookup.TryGetValue(id, out var piece) ? piece : null;
    }

    private void BuildLookup()
    {
        lookup = new Dictionary<string, BuildPiece>(pieces.Length);
        foreach (var piece in pieces)
            if (piece != null) lookup[piece.name] = piece;
    }

#if UNITY_EDITOR
    // Sorted by stable ID (asset name) before assigning -- see
    // ItemDatabase.EditorSetItems for why (CHANGELOG 2026-08-16,
    // DatabaseRepopulator determinism fix).
    public void EditorSetPieces(BuildPiece[] value)
    {
        System.Array.Sort(value, (a, b) =>
            string.CompareOrdinal(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty));
        pieces = value;
        lookup = null;
    }
#endif
}
