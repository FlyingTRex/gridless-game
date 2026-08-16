using UnityEditor;
using UnityEngine;

// Without a custom editor overriding RenderStaticPreview, Unity shows every
// ScriptableObject-derived asset with the same generic placeholder icon in
// the Project window and any object picker (found live 2026-08-16 in the
// VMS admin browser's "Select Item Definition" picker -- every item looked
// identical). ItemDefinition and BuildPiece both already carry a public
// `icon` Sprite field (see IconBaker.cs's own header comment noting both
// types share that field name) -- render it as the asset's own thumbnail
// instead.
internal static class IconPreviewUtility
{
    // GPU blit + ReadPixels rather than Sprite.texture.GetPixels(): works
    // regardless of the source texture's Read/Write Enabled import setting
    // (the baked icon PNGs aren't marked readable), same reasoning IconBaker
    // itself uses a RenderTexture for its bake rather than reading pixels
    // directly off an arbitrary source.
    public static Texture2D RenderFromSprite(Sprite sprite, int width, int height)
    {
        if (sprite == null || sprite.texture == null) return null;

        var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        var prevActive = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.clear);
        Graphics.Blit(sprite.texture, rt);

        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        RenderTexture.active = prevActive;
        RenderTexture.ReleaseTemporary(rt);
        return tex;
    }
}

[CustomEditor(typeof(ItemDefinition))]
public class ItemDefinitionEditor : Editor
{
    public override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height) =>
        IconPreviewUtility.RenderFromSprite((target as ItemDefinition)?.icon, width, height);
}

[CustomEditor(typeof(BuildPiece))]
public class BuildPieceEditor : Editor
{
    public override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height) =>
        IconPreviewUtility.RenderFromSprite((target as BuildPiece)?.icon, width, height);
}
