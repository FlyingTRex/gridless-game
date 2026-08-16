using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Admin browser ("VMS", discussed 2026-08-16) for the 6 core data types
// (see VmsTypeInfo.All) -- list-on-left / edit-on-right, backed directly by
// each asset's own SerializedObject via Editor.CreateEditor, not a new
// inlined data store (see CLAUDE.md's ItemDatabase/SkillDatabase/
// NPCJobDatabase regeneration gotcha for why that distinction matters).
// List population is a fresh AssetDatabase scan per tab (VmsTypeInfo.
// LoadAll), same as DatabaseRepopulator's own scan -- uniform across all 6
// tabs even though only 3 of the 6 types have a database indexing them.
public class VmsWindow : EditorWindow
{
    [MenuItem("Gridless/VMS Admin Browser")]
    private static void Open()
    {
        var window = GetWindow<VmsWindow>("VMS");
        window.minSize = new Vector2(700f, 400f);
    }

    private const float ListWidth = 260f;

    private int tabIndex;
    private string search = "";
    private UnityEngine.Object[] currentAssets = Array.Empty<UnityEngine.Object>();
    private UnityEngine.Object selected;
    private Editor activeEditor;
    private Vector2 listScroll;
    private Vector2 detailScroll;

    private void OnEnable() => RefreshList();

    private void OnDisable() => DestroyActiveEditor();

    private void OnGUI()
    {
        DrawToolbar();
        DrawSearchField();

        EditorGUILayout.BeginHorizontal();
        DrawList();
        DrawDetail();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        var labels = VmsTypeInfo.All.Select(t => t.TabLabel).ToArray();
        int newTabIndex = GUILayout.Toolbar(tabIndex, labels, EditorStyles.toolbarButton,
            GUILayout.Width(labels.Length * 100f));
        if (newTabIndex != tabIndex)
            SwitchTab(newTabIndex);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60f)))
            RefreshList();

        if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(50f)))
            CreateNew();

        bool dirty = selected != null && EditorUtility.IsDirty(selected);
        using (new EditorGUI.DisabledScope(!dirty))
        {
            if (GUILayout.Button(dirty ? "Save*" : "Save", EditorStyles.toolbarButton, GUILayout.Width(50f)))
                AssetDatabase.SaveAssets();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSearchField()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Search", GUILayout.Width(50f));
        search = EditorGUILayout.TextField(search);
        if (GUILayout.Button("Clear", GUILayout.Width(50f)))
            search = "";
        EditorGUILayout.EndHorizontal();
    }

    private void DrawList()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(ListWidth));
        listScroll = EditorGUILayout.BeginScrollView(listScroll);

        foreach (var asset in currentAssets)
        {
            if (asset == null) continue;
            if (!string.IsNullOrEmpty(search) &&
                asset.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            var prevColor = GUI.backgroundColor;
            if (asset == selected) GUI.backgroundColor = new Color(0.5f, 0.7f, 1f);
            if (GUILayout.Button(asset.name))
                Select(asset);
            GUI.backgroundColor = prevColor;
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawDetail()
    {
        EditorGUILayout.BeginVertical();

        if (VmsTypeInfo.All[tabIndex].HasDatabaseReminder)
            EditorGUILayout.HelpBox(
                "Run Gridless > Repopulate Databases after adding new assets.", MessageType.Info);

        if (selected == null)
        {
            EditorGUILayout.LabelField("Select an asset from the list.");
        }
        else
        {
            detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
            if (activeEditor != null)
                activeEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.EndVertical();
    }

    private void SwitchTab(int newTabIndex)
    {
        AutoSaveIfDirty();
        tabIndex = newTabIndex;
        search = "";
        Select(null);
        RefreshList();
    }

    private void RefreshList() => currentAssets = VmsTypeInfo.LoadAll(VmsTypeInfo.All[tabIndex].Type);

    private void Select(UnityEngine.Object asset)
    {
        if (asset == selected) return;
        AutoSaveIfDirty();
        selected = asset;
        DestroyActiveEditor();
        if (selected != null)
            activeEditor = Editor.CreateEditor(selected);
    }

    private void DestroyActiveEditor()
    {
        if (activeEditor == null) return;
        DestroyImmediate(activeEditor);
        activeEditor = null;
    }

    private void AutoSaveIfDirty()
    {
        if (selected != null && EditorUtility.IsDirty(selected))
            AssetDatabase.SaveAssets();
    }

    private void CreateNew()
    {
        var type = VmsTypeInfo.All[tabIndex].Type;
        string path = EditorUtility.SaveFilePanelInProject(
            $"New {type.Name}", $"New{type.Name}.asset", "asset",
            "Choose where to save the new asset.", "Assets/Data");
        if (string.IsNullOrEmpty(path)) return;

        var instance = CreateInstance(type);
        AssetDatabase.CreateAsset(instance, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        RefreshList();
        Select(AssetDatabase.LoadAssetAtPath(path, type));
    }
}
