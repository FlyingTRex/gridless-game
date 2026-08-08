using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Renders an inventory icon straight from a model's own prefab, instead of
// hand-drawing one — instantiates it in a throwaway scene, frames it with
// an orthographic camera sized to its measured bounds, renders to a
// transparent PNG, imports it as a Sprite, and wires it onto an
// ItemDefinition. Consolidates everything learned baking the first few
// icons (Backpack, Small Rock) into one reusable tool instead of a new
// bespoke throwaway script per item.
//
// Usage (batch mode, from the project root):
//
//   "<UnityEditor.exe>" -batchmode -quit -projectPath . -executeMethod IconBaker.Bake ^
//     -modelPath "Assets/Prefabs/SomeModel.prefab" ^
//     -itemAssetPath "Assets/Data/SomeItem.asset"
//
// Optional args:
//   -resolution <int>         small inline icon size, default 32
//   -previewResolution <int>  also bakes a second, bigger image and wires
//                             it to ItemDefinition.previewIcon (for big
//                             preview UI like InventoryScreen's Back
//                             preview box) — only used if this is > 0.
//                             Default 0 (skip).
//   -outputName <name>        base filename for the small icon (no
//                             extension); defaults to the item asset's own
//                             filename + "Icon". The preview image (if
//                             any) reuses this name + "Preview".
//
// CRITICAL: do NOT pass -nographics. It disables the graphics device
// entirely, which RenderTexture needs — the render silently produces
// nothing and the whole bake fails quietly. Batch mode still shows no
// window without it; it just also initializes the GPU device this time.
// This script checks for and aborts loudly on that condition rather than
// silently writing a blank icon.
//
// Also note: a fresh PNG's importer defaults spriteImportMode to Multiple,
// which needs hand-sliced sub-sprites before Unity will produce an actual
// Sprite object at all (AssetDatabase.LoadAssetAtPath<Sprite> silently
// returns null otherwise) — this script sets it to Single explicitly,
// already the fix for that trap.
public static class IconBaker
{
    private const string IconDir = "Assets/Textures/Icons";

    public static void Bake()
    {
        string modelPath = GetArg("-modelPath");
        string itemAssetPath = GetArg("-itemAssetPath");
        int resolution = int.Parse(GetArg("-resolution", "32"));
        int previewResolution = int.Parse(GetArg("-previewResolution", "0"));
        string outputName = GetArg("-outputName");

        if (string.IsNullOrEmpty(modelPath) || string.IsNullOrEmpty(itemAssetPath))
        {
            Debug.LogError("IconBaker: -modelPath and -itemAssetPath are both required.");
            return;
        }

        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            Debug.LogError("IconBaker: graphics device is Null — this run was launched with " +
                "-nographics, which disables RenderTexture. Re-run without that flag.");
            return;
        }

        var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (modelAsset == null)
        {
            Debug.LogError($"IconBaker: no GameObject found at {modelPath}");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<ItemDefinition>(itemAssetPath) == null)
        {
            Debug.LogError($"IconBaker: no ItemDefinition found at {itemAssetPath}");
            return;
        }

        if (string.IsNullOrEmpty(outputName))
            outputName = Path.GetFileNameWithoutExtension(itemAssetPath) + "Icon";

        // Bake everything first, wire it up last. BakeOne()'s
        // AssetDatabase.ImportAsset/SaveAndReimport calls can invalidate
        // an ItemDefinition reference held from before they ran (turns
        // into a stale/destroyed Unity Object) — reloading it fresh right
        // before use, after all baking is done, sidesteps that.
        var sprite = BakeOne(modelAsset, resolution, outputName);
        if (sprite == null) return;

        Sprite previewSprite = null;
        if (previewResolution > 0)
            previewSprite = BakeOne(modelAsset, previewResolution, outputName + "Preview");

        var itemAsset = AssetDatabase.LoadAssetAtPath<ItemDefinition>(itemAssetPath);
        if (itemAsset == null)
        {
            Debug.LogError($"IconBaker: ItemDefinition at {itemAssetPath} became unavailable after baking — aborting wire-up.");
            return;
        }

        var so = new SerializedObject(itemAsset);
        so.FindProperty("icon").objectReferenceValue = sprite;
        if (previewSprite != null)
            so.FindProperty("previewIcon").objectReferenceValue = previewSprite;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(itemAsset);
        AssetDatabase.SaveAssets();

        Debug.Log($"IconBaker: DONE — {itemAssetPath}.icon = {sprite.name}" +
            (previewResolution > 0 ? $", .previewIcon baked at {previewResolution}x{previewResolution}" : ""));
    }

    // Renders one image at the given resolution and returns the imported
    // Sprite, or null if anything failed (already logged).
    private static Sprite BakeOne(GameObject modelAsset, int resolution, string outputName)
    {
        var tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, tempScene);
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;

        var renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogError($"IconBaker: {modelAsset.name} has no Renderer anywhere in its hierarchy — nothing to render.");
            return null;
        }

        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

        var cameraGO = new GameObject("IconCamera");
        SceneManager.MoveGameObjectToScene(cameraGO, tempScene);
        var cam = cameraGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.orthographic = true;

        float maxDim = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (maxDim <= 0f) maxDim = 1f; // degenerate/zero-size mesh guard
        cam.orthographicSize = maxDim * 0.65f; // padding around the model
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = maxDim * 10f;

        // Fixed 3/4-from-above angle — matches the framing every icon
        // baked with this tool so far has used, for a consistent look
        // across the whole icon set.
        Vector3 dir = new Vector3(1f, 0.8f, -1f).normalized;
        cameraGO.transform.position = bounds.center + dir * maxDim * 3f;
        cameraGO.transform.LookAt(bounds.center);

        var keyLightGO = new GameObject("IconKeyLight");
        SceneManager.MoveGameObjectToScene(keyLightGO, tempScene);
        var keyLight = keyLightGO.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.intensity = 1.2f;
        keyLightGO.transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);

        var fillLightGO = new GameObject("IconFillLight");
        SceneManager.MoveGameObjectToScene(fillLightGO, tempScene);
        var fillLight = fillLightGO.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.intensity = 0.4f;
        fillLightGO.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        var rt = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();

        var prevActive = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        tex.Apply();
        RenderTexture.active = prevActive;

        cam.targetTexture = null;
        rt.Release();

        if (!Directory.Exists(IconDir)) Directory.CreateDirectory(IconDir);
        string iconPath = $"{IconDir}/{outputName}.png";
        File.WriteAllBytes(iconPath, tex.EncodeToPNG());

        AssetDatabase.ImportAsset(iconPath, ImportAssetOptions.ForceSynchronousImport);

        var importer = (TextureImporter)AssetImporter.GetAtPath(iconPath);
        importer.textureType = TextureImporterType.Sprite;
        // Default is Multiple, which needs hand-sliced sub-sprites before
        // any Sprite object actually exists — LoadAssetAtPath<Sprite>
        // silently returns null otherwise. This is the fix for that.
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        if (sprite == null)
            Debug.LogError($"IconBaker: sprite failed to load after import at {iconPath}");
        else
            Debug.Log($"IconBaker: baked {iconPath} ({resolution}x{resolution}) from bounds center={bounds.center} size={bounds.size}");

        return sprite;
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
