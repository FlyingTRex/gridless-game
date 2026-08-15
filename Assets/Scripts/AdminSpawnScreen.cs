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
[RequireComponent(typeof(PlayerGuilds))]
public class AdminSpawnScreen : MonoBehaviour
{
#if UNITY_EDITOR
    private PlayerDropping dropping;
    private PlayerGuilds playerGuilds;
    private ItemDefinition[] allItems = Array.Empty<ItemDefinition>();
    private BuildPiece[] allPieces = Array.Empty<BuildPiece>();
    private GuildDefinition[] allGuilds = Array.Empty<GuildDefinition>();
    private Vector2 scrollPos;

    // Search box (2026-08-11, traskmi's ask — the item list was getting
    // long). Same pattern as CraftingScreen.DrawSearchBar/searchQuery —
    // filters the Item section only, by itemName substring match.
    private string searchQuery = "";

    // Quantity field (2026-08-15, Ben's ask — testing a stackable item
    // like a 10-seed packet meant clicking Spawn 10 times). Shared across
    // every item in the list below, same as the search box. Parsed on
    // each Spawn click rather than kept as an int field, so a partially-
    // typed value (or garbage) never blocks typing — falls back to 1.
    private string spawnQuantityText = "1";

    private void Awake()
    {
        dropping = GetComponent<PlayerDropping>();
        playerGuilds = GetComponent<PlayerGuilds>();
        RefreshItemList();
        RefreshPieceList();
        RefreshGuildList();
        // GrantStartingTestMaterials() call disabled 2026-08-10, Ben's
        // call — its 56 lbs of free Stick/Rope/Plank (every item still at
        // the untuned default 1 lb weight) put a fresh character straight
        // into Encumbrance's >95% overload tier before they'd picked
        // anything up, making it impossible to test the lower tiers or a
        // genuinely empty-inventory baseline. Method kept intact (not
        // deleted) — re-enable this call whenever testing the Twig->Plank
        // build-upgrade path again, which is what it was built for.
    }

    // Testing convenience (2026-08-09, Ben's ask): enough Stick + Rope on
    // game start to build 3 Twig Walls (8 Stick + 4 Rope each) without
    // gathering/chopping first. Same Editor-only scoping as the rest of
    // this tool — never meant to ship; real players gather their own
    // materials. Not the Admin item-spawn list above (that's a manual
    // per-click tool) — this runs automatically once, at scene start.
    //
    // Plank grant added 2026-08-10 after Ben hit exactly the gap this
    // comment already predicted — Admin Spawn's item list only grants 1
    // per click (same as every item), and the new Plank-tier pieces cost
    // 5-12 Plank each (69 total to build one of everything), so testing
    // the Twig->Plank upgrade path meant either dozens of individual
    // Admin Spawn clicks or grinding a tree for logs. 80 comfortably
    // covers one of each Plank piece with slack to spare.
    //
    // Disabled (not called) as of 2026-08-10 — see the Awake() comment.
    private void GrantStartingTestMaterials()
    {
        var inventory = GetComponent<PlayerInventory>();
        if (inventory == null) return;

        var stick = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Data/Stick.asset");
        var rope = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Data/Rope.asset");
        var plank = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/Data/Plank.asset");
        if (stick != null) inventory.AddItem(stick, 24);
        if (rope != null) inventory.AddItem(rope, 12);
        if (plank != null) inventory.AddItem(plank, 80);
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

    private void RefreshGuildList()
    {
        var guids = AssetDatabase.FindAssets("t:GuildDefinition");
        allGuilds = new GuildDefinition[guids.Length];
        for (int i = 0; i < guids.Length; i++)
            allGuilds[i] = AssetDatabase.LoadAssetAtPath<GuildDefinition>(AssetDatabase.GUIDToAssetPath(guids[i]));

        Array.Sort(allGuilds, (a, b) => string.Compare(a?.guildName, b?.guildName, StringComparison.OrdinalIgnoreCase));
    }

    // Garbage/empty/zero/negative input all fall back to 1 rather than
    // blocking the Spawn button or spawning nothing.
    private int ParsedSpawnQuantity() =>
        int.TryParse(spawnQuantityText, out int qty) && qty > 0 ? qty : 1;

    // Called by GameMenuScreen while its Admin tab is active.
    public void DrawContent()
    {
        GUILayout.Label("Admin — Spawn Item", DebugGUI.Header);
        GUILayout.Label("Spawns one of the item in front of you, same as a manual Drop. Editor/testing only — won't appear in a build.", DebugGUI.Label);

        if (GUILayout.Button("Refresh List", GUILayout.Width(120)))
        {
            RefreshItemList();
            RefreshPieceList();
            RefreshGuildList();
        }
        GUILayout.Space(6);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Search:", DebugGUI.Label, GUILayout.Width(55));
        searchQuery = GUILayout.TextField(searchQuery, GUILayout.Width(220));
        if (!string.IsNullOrEmpty(searchQuery) && GUILayout.Button("Clear", GUILayout.Width(50)))
            searchQuery = "";
        GUILayout.Space(12);
        GUILayout.Label("Quantity:", DebugGUI.Label, GUILayout.Width(65));
        spawnQuantityText = GUILayout.TextField(spawnQuantityText, GUILayout.Width(50));
        GUILayout.EndHorizontal();
        GUILayout.Space(6);

        bool searching = !string.IsNullOrWhiteSpace(searchQuery);
        string queryLower = searching ? searchQuery.Trim().ToLowerInvariant() : null;

        float scrollHeight = Mathf.Min(Screen.height - 260f, 500f);
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(scrollHeight));

        bool anyItemShown = false;
        foreach (var item in allItems)
        {
            if (item == null) continue;
            if (searching && !item.itemName.ToLowerInvariant().Contains(queryLower)) continue;
            anyItemShown = true;

            GUILayout.BeginHorizontal();
            GUILayout.Label(item.itemName, DebugGUI.Label, GUILayout.Width(240));
            if (GUILayout.Button("Spawn", GUILayout.Width(70)))
                dropping.SpawnPickup(item, ParsedSpawnQuantity());
            GUILayout.EndHorizontal();
        }
        if (!anyItemShown)
            GUILayout.Label(searching ? $"No items match \"{searchQuery}\"." : "No items found.", DebugGUI.Label);

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

        GUILayout.Space(14);
        GUILayout.Label("Admin — Guilds", DebugGUI.Header);
        GUILayout.Label($"Join/leave for testing — max {PlayerGuilds.MaxGuilds} at once, one Player-tab tile per joined guild. No in-world way to join yet.", DebugGUI.Label);
        GUILayout.Space(6);

        foreach (var guild in allGuilds)
        {
            if (guild == null) continue;

            bool isMember = playerGuilds.IsMember(guild);
            GUILayout.BeginHorizontal();
            GUILayout.Label(guild.guildName, DebugGUI.Label, GUILayout.Width(240));

            if (isMember)
            {
                if (GUILayout.Button("Leave", GUILayout.Width(70)))
                    playerGuilds.Leave(guild);
            }
            else
            {
                GUI.enabled = playerGuilds.Joined.Count < PlayerGuilds.MaxGuilds;
                if (GUILayout.Button("Join", GUILayout.Width(70)))
                    playerGuilds.Join(guild);
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
    }

    // Bug fixed 2026-08-09: spawning right under the player's own feet
    // (the raycast originated from the player's position) put a solid
    // collider around them — Foundation's box extends 0.8m below ground
    // and only 0.2m above, so CharacterController's own depenetration
    // resolved downward instead of up, pushing the player underground.
    // First fix stood the player on the piece's own measured top surface
    // afterward instead — worked, but had a real side effect found the
    // same day testing the Twig Wall: standing directly on top of a
    // large, flat, ground-level piece (Foundation) while looking at the
    // horizon doesn't visually read as "a piece exists" at all — it
    // blends into the grass around it. Reported as "clicking Spawn does
    // nothing"/"it's invisible", even though the Hierarchy proved it had
    // spawned correctly every time.
    //
    // Root-cause fix instead of another vantage-point workaround: spawn
    // a few meters in front of the player rather than at their own
    // position, so the piece is never underfoot to begin with. That
    // removes the original burial risk at its source — no overlap ever
    // happens — so the "teleport the player onto it afterward" rescue
    // and the CharacterController-disable dance it needed are both gone
    // too, not just papered over.
    private const float SpawnForwardDistance = 4f;

    private void SpawnPiece(BuildPiece piece)
    {
        if (piece == null || piece.prefab == null) return;

        Vector3 position = transform.position + transform.forward * SpawnForwardDistance;
        if (Physics.Raycast(position + Vector3.up * 2f, Vector3.down, out var hit, 10f))
            position = hit.point;

        var instance = Instantiate(piece.prefab, position, Quaternion.identity);
        instance.AddComponent<PlacedPiece>().Piece = piece;
    }
#else
    public void DrawContent()
    {
        GUILayout.Label("Admin", DebugGUI.Header);
        GUILayout.Label("Editor-only tool — not available in a build.", DebugGUI.Label);
    }
#endif
}
