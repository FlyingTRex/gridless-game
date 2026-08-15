using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Dev-facing "prefab buildings" tool (MVP2 item 10) -- drops a finished,
// hand-assembled composite structure into the currently open scene fast,
// for populating TestScene.unity during development rather than placing
// pieces one at a time via the player-facing Build system. Permanent tool,
// same category as IconBaker.cs -- not one-off setup code, see CLAUDE.md's
// Assets/Editor/ convention. See MVP2_PLANNING.md item 10 and
// BUGS_AND_ENHANCEMENTS.md for the known Rectangular House gable-end roof
// gap.
public static class PrefabBuildingPlacer
{
    private const string BuildingDir = "Assets/Prefabs/Buildings";

    [MenuItem("Gridless/Place Prefab Building/Small Hut - Twig")]
    public static void PlaceSmallHutTwig() => Place("SmallHutTwig");

    [MenuItem("Gridless/Place Prefab Building/Small Hut - Plank")]
    public static void PlaceSmallHutPlank() => Place("SmallHutPlank");

    [MenuItem("Gridless/Place Prefab Building/Rectangular House - Twig")]
    public static void PlaceRectangularHouseTwig() => Place("RectangularHouseTwig");

    [MenuItem("Gridless/Place Prefab Building/Rectangular House - Plank")]
    public static void PlaceRectangularHousePlank() => Place("RectangularHousePlank");

    private static void Place(string buildingName)
    {
        string path = $"{BuildingDir}/{buildingName}.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError($"PrefabBuildingPlacer: no prefab at {path}");
            return;
        }

        // Wherever the dev is currently looking in the Scene view -- the
        // Editor-mode equivalent of Admin Spawn's "in front of the player."
        // Falls back to the world origin if no Scene view is open/focused.
        Vector3 pivot = SceneView.lastActiveSceneView != null
            ? SceneView.lastActiveSceneView.pivot
            : Vector3.zero;

        // The prefab's own root is already at the correct ground offset
        // from authoring (Foundation's own convention -- "mostly buried" by
        // design, same as every other placed piece), so this only needs to
        // find the right Y for the terrain at this XZ, not a full imported-
        // model bounds-grounding pass.
        float groundY = GroundHeight.Sample(pivot, pivot.y);

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = new Vector3(pivot.x, groundY, pivot.z);

        Undo.RegisterCreatedObjectUndo(instance, "Place Prefab Building");
        Selection.activeGameObject = instance;
        EditorSceneManager.MarkSceneDirty(instance.scene);
    }
}
