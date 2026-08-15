using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds a *Pickup.prefab from an imported model — instantiate, measure and
// ground its bounds, add a bounds-fit BoxCollider + Rigidbody + Pickup, wire
// the ItemDefinition, save as a nested prefab. Every one of the 83
// *Pickup.prefab files in this project so far was built by a bespoke
// throwaway Assets/Editor/*.cs script re-deriving this exact same
// bounds/grounding/collider math from scratch (see EFFICIENCY_AUDIT.md,
// 2026-08-15) — this consolidates it into one reusable tool, the same
// reason IconBaker.cs exists for icon baking.
//
// Usage (batch mode, from the project root):
//
//   "<UnityEditor.exe>" -batchmode -nographics -quit -projectPath . -executeMethod PickupPrefabBuilder.Build ^
//     -modelPath "Assets/Models/SomeModel.glb" ^
//     -itemAssetPath "Assets/Data/SomeItem.asset" ^
//     -outputPath "Assets/Prefabs/SomeItemPickup.prefab"
//
// Optional args:
//   -wireItem <0|1>   also sets ItemDefinition.worldPickupPrefab to the new
//                     prefab. Default 1.
//
// -nographics is fine here (unlike IconBaker) — nothing in this tool renders
// or needs a graphics device, it only measures mesh bounds and writes YAML.
public static class PickupPrefabBuilder
{
    public static void Build()
    {
        string modelPath = GetArg("-modelPath");
        string itemAssetPath = GetArg("-itemAssetPath");
        string outputPath = GetArg("-outputPath");
        bool wireItem = GetArg("-wireItem", "1") != "0";

        if (string.IsNullOrEmpty(modelPath) || string.IsNullOrEmpty(itemAssetPath) || string.IsNullOrEmpty(outputPath))
        {
            Debug.LogError("PickupPrefabBuilder: -modelPath, -itemAssetPath, and -outputPath are all required.");
            return;
        }

        var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (modelAsset == null)
        {
            Debug.LogError($"PickupPrefabBuilder: no GameObject found at {modelPath}");
            return;
        }

        var itemAsset = AssetDatabase.LoadAssetAtPath<ItemDefinition>(itemAssetPath);
        if (itemAsset == null)
        {
            Debug.LogError($"PickupPrefabBuilder: no ItemDefinition found at {itemAssetPath}");
            return;
        }

        BuildAndWire(modelAsset, itemAsset, outputPath, wireItem);
    }

    // Reusable entry point — Build() above is a thin command-line-args
    // wrapper around this for the normal single-item case, same split as
    // IconBaker.Bake()/BakeAndWire().
    public static GameObject BuildAndWire(GameObject modelAsset, ItemDefinition itemAsset, string outputPath, bool wireItem = true)
    {
        var tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, tempScene);
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;

        var renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogError($"PickupPrefabBuilder: {modelAsset.name} has no Renderer anywhere in its hierarchy — nothing to measure.");
            UnityEngine.Object.DestroyImmediate(instance);
            return null;
        }

        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

        // Ground the model — an imported model's pivot isn't reliably at its
        // base (see CLAUDE.md's pivot-grounding gotcha), so measure the
        // actual lowest point and shift up to sit it on y=0 rather than
        // trusting the import's own origin.
        float groundOffset = -bounds.min.y;
        instance.transform.position += new Vector3(0f, groundOffset, 0f);
        bounds.center += new Vector3(0f, groundOffset, 0f);

        var box = instance.AddComponent<BoxCollider>();
        box.center = instance.transform.InverseTransformPoint(bounds.center);
        box.size = bounds.size;

        var rb = instance.AddComponent<Rigidbody>();
        rb.mass = 1f;

        var pickup = instance.AddComponent<Pickup>();
        var pickupSO = new SerializedObject(pickup);
        pickupSO.FindProperty("item").objectReferenceValue = itemAsset;
        pickupSO.FindProperty("quantity").intValue = 1;
        pickupSO.ApplyModifiedPropertiesWithoutUndo();

        var savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, outputPath, out bool success);
        UnityEngine.Object.DestroyImmediate(instance);

        if (!success)
        {
            Debug.LogError($"PickupPrefabBuilder: failed to save prefab at {outputPath}");
            return null;
        }

        if (wireItem)
        {
            // Re-fetch rather than trusting itemAsset across the
            // SaveAsPrefabAsset boundary — see CLAUDE.md's stale-reference
            // gotcha (asset references can go stale across a
            // LoadPrefabContents/SaveAsPrefabAsset-style operation).
            var itemPath = AssetDatabase.GetAssetPath(itemAsset);
            var freshItem = AssetDatabase.LoadAssetAtPath<ItemDefinition>(itemPath);
            var itemSO = new SerializedObject(freshItem);
            itemSO.FindProperty("worldPickupPrefab").objectReferenceValue = savedPrefab;
            itemSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(freshItem);
        }

        AssetDatabase.SaveAssets();

        Debug.Log($"PickupPrefabBuilder: DONE — {outputPath} (bounds center={bounds.center} size={bounds.size})" +
            (wireItem ? $", wired to {AssetDatabase.GetAssetPath(itemAsset)}.worldPickupPrefab" : ""));

        return savedPrefab;
    }

    private static string GetArg(string name, string defaultValue = null)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name) return args[i + 1];
        }
        return defaultValue;
    }
}
