using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System;
#endif

// Dev/QA tool: lists every ItemDefinition asset in the project and spawns
// one physical copy in front of the player when clicked (via
// PlayerDropping.SpawnPickup, the same instantiate-and-Configure path a
// manual Drop uses) — the Admin tab of GameMenuScreen (` key). A test aid
// so new items (e.g. a fresh batch of craft-tier tools) can be spawned on
// demand to check without crafting each one from scratch first.
//
// Editor-only: the item list is discovered via AssetDatabase, which isn't
// available in a standalone build. That trade-off is deliberate — this is
// purely a testing aid never meant to ship, and auto-discovery means a
// newly-created ItemDefinition just shows up here with no list to remember
// to update, unlike e.g. GameMenuScreen.ControlsList or PlayerCrafting's
// recipes array.
[RequireComponent(typeof(PlayerDropping))]
public class AdminSpawnScreen : MonoBehaviour
{
#if UNITY_EDITOR
    private PlayerDropping dropping;
    private ItemDefinition[] allItems = Array.Empty<ItemDefinition>();
    private BuildPiece[] allPieces = Array.Empty<BuildPiece>();
    private Vector2 scrollPos;

    private void Awake()
    {
        dropping = GetComponent<PlayerDropping>();
        RefreshItemList();
        RefreshPieceList();
    }

    private void RefreshItemList()
    {
        var guids = AssetDatabase.FindAssets("t:ItemDefinition");
        allItems = new ItemDefinition[guids.Length];
        for (int i = 0; i < guids.Length; i++)
            allItems[i] = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guids[i]));

        Array.Sort(allItems, (a, b) => string.Compare(a?.itemName, b?.itemName, StringComparison.OrdinalIgnoreCase));
    }

    private void RefreshPieceList()
    {
        var guids = AssetDatabase.FindAssets("t:BuildPiece");
        allPieces = new BuildPiece[guids.Length];
        for (int i = 0; i < guids.Length; i++)
            allPieces[i] = AssetDatabase.LoadAssetAtPath<BuildPiece>(AssetDatabase.GUIDToAssetPath(guids[i]));

        Array.Sort(allPieces, (a, b) => string.Compare(a?.pieceName, b?.pieceName, StringComparison.OrdinalIgnoreCase));
    }

    // Called by GameMenuScreen while its Admin tab is active.
    public void DrawContent()
    {
        GUILayout.Label("Admin — Spawn Item", DebugGUI.Header);
        GUILayout.Label("Spawns one of the item in front of you, same as a manual Drop. Editor/testing only — won't appear in a build.", DebugGUI.Label);

        if (GUILayout.Button("Refresh List", GUILayout.Width(120)))
        {
            RefreshItemList();
            RefreshPieceList();
        }
        GUILayout.Space(6);

        float scrollHeight = Mathf.Min(Screen.height - 260f, 500f);
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(scrollHeight));

        foreach (var item in allItems)
        {
            if (item == null) continue;

            GUILayout.BeginHorizontal();
            GUILayout.Label(item.itemName, DebugGUI.Label, GUILayout.Width(240));
            if (GUILayout.Button("Spawn", GUILayout.Width(70)))
                dropping.SpawnPickup(item);
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(14);
        GUILayout.Label("Admin — Spawn Build Piece", DebugGUI.Header);
        GUILayout.Label("Places the piece directly on the ground under you — free (no materials, no skill gate), tagged as a real PlacedPiece so upgrade/destroy still work on it.", DebugGUI.Label);
        GUILayout.Space(6);

        foreach (var piece in allPieces)
        {
            if (piece == null) continue;

            GUILayout.BeginHorizontal();
            GUILayout.Label(piece.pieceName, DebugGUI.Label, GUILayout.Width(240));
            if (GUILayout.Button("Spawn", GUILayout.Width(70)))
                SpawnPiece(piece);
            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
    }

    // Bug fixed 2026-08-09: spawning right under the player's own feet
    // (the raycast originates from the player's position) put a solid
    // collider around them — Foundation's box extends 0.8m below ground
    // and only 0.2m above, so CharacterController's own depenetration
    // resolved downward instead of up, pushing the player underground.
    // Rather than rely on physics to sort out an overlap it doesn't
    // expect, explicitly stand the player on the piece's own measured
    // top surface afterward — generalizes to any piece shape, not just
    // Foundation's specific dimensions.
    //
    // Second bug fixed same day: the ground raycast hit the player's own
    // CharacterController capsule (top at Center.y + Height/2 = 1.8) —
    // Physics.Raycast doesn't exclude the caster's own collider — before
    // it ever reached the real ground, so the piece spawned floating at
    // roughly head height with its legs dangling in open air. Confirmed
    // live via screenshot. Disabling the controller for the raycast (and
    // the position set right after) avoids it self-hitting.
    private void SpawnPiece(BuildPiece piece)
    {
        if (piece == null || piece.prefab == null) return;

        var controller = GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        Vector3 position = transform.position;
        if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out var hit, 10f))
            position = hit.point;

        var instance = Instantiate(piece.prefab, position, Quaternion.identity);
        instance.AddComponent<PlacedPiece>().Piece = piece;

        var pieceCollider = instance.GetComponentInChildren<Collider>();
        if (pieceCollider == null)
        {
            if (controller != null) controller.enabled = true;
            return;
        }

        float topY = pieceCollider.bounds.max.y;
        transform.position = new Vector3(transform.position.x, topY + 0.1f, transform.position.z);
        if (controller != null) controller.enabled = true;
    }
#else
    public void DrawContent()
    {
        GUILayout.Label("Admin", DebugGUI.Header);
        GUILayout.Label("Editor-only tool — not available in a build.", DebugGUI.Label);
    }
#endif
}
