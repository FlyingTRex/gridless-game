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
    private Vector2 scrollPos;

    private void Awake()
    {
        dropping = GetComponent<PlayerDropping>();
        RefreshItemList();
    }

    private void RefreshItemList()
    {
        var guids = AssetDatabase.FindAssets("t:ItemDefinition");
        allItems = new ItemDefinition[guids.Length];
        for (int i = 0; i < guids.Length; i++)
            allItems[i] = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guids[i]));

        Array.Sort(allItems, (a, b) => string.Compare(a?.itemName, b?.itemName, StringComparison.OrdinalIgnoreCase));
    }

    // Called by GameMenuScreen while its Admin tab is active.
    public void DrawContent()
    {
        GUILayout.Label("Admin — Spawn Item", DebugGUI.Header);
        GUILayout.Label("Spawns one of the item in front of you, same as a manual Drop. Editor/testing only — won't appear in a build.", DebugGUI.Label);

        if (GUILayout.Button("Refresh List", GUILayout.Width(120)))
            RefreshItemList();
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

        GUILayout.EndScrollView();
    }
#else
    public void DrawContent()
    {
        GUILayout.Label("Admin", DebugGUI.Header);
        GUILayout.Label("Editor-only tool — not available in a build.", DebugGUI.Label);
    }
#endif
}
