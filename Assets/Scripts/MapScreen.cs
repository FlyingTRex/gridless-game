using UnityEngine;
using UnityEngine.InputSystem;

// Player Map UI (PLAYER_MAP_PLANNING.md, 2026-08-16) — M toggles it, same
// open/close/cursor-lock shape GameMenuScreen's backquote toggle already
// established. Reads PlayerMapExploration's revealed-cell grid and draws
// it as a texture (fog color for unrevealed cells, ground color for
// revealed ones), plus the player's own position as a marker. Doesn't
// draw Flag/Statue markers yet — those components don't exist in the
// game yet (Village Flag is being built separately, per WORKING_ON.md);
// this screen only needs PlayerMapExploration's grid to already reflect
// their reveals once RevealCircle gets called from there, no changes
// needed here when that lands.
[RequireComponent(typeof(PlayerMapExploration))]
public class MapScreen : MonoBehaviour
{
    private static readonly Color FogColor = new Color(0.05f, 0.05f, 0.05f, 1f);
    private static readonly Color RevealedColor = new Color(0.36f, 0.42f, 0.3f, 1f);
    private static readonly Color PlayerMarkerColor = new Color(0.95f, 0.85f, 0.2f, 1f);

    private const float MapFraction = 0.8f; // square map area, 80% of the shorter screen dimension
    private const float MarkerSize = 10f;

    private PlayerMapExploration exploration;
    private bool isOpen;

    private Texture2D mapTexture;
    private int cachedRevealVersion = -1;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        exploration = GetComponent<PlayerMapExploration>();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.mKey.wasPressedThisFrame) return;

        // Same "only open from normal gameplay, always allow closing"
        // guard GameMenuScreen's own backquote toggle already uses — a
        // locked cursor means nothing else has the screen right now.
        if (isOpen || Cursor.lockState == CursorLockMode.Locked)
            SetOpen(!isOpen);
    }

    // Called by FirstPersonController when Escape re-locks the cursor,
    // same as every other screen it tracks.
    public void Close() => SetOpen(false);

    private void SetOpen(bool value)
    {
        isOpen = value;
        Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = value;
    }

    private void OnGUI()
    {
        if (!isOpen) return;

        var screenRect = new Rect(0, 0, Screen.width, Screen.height);
        DebugGUI.DrawPanel(screenRect);
        GUILayout.BeginArea(screenRect);

        GUILayout.Space(20);
        GUILayout.Label("Map", DebugGUI.Header);

        EnsureTexture();

        float mapSize = Mathf.Min(Screen.width, Screen.height) * MapFraction;
        var mapRect = new Rect((Screen.width - mapSize) / 2f, 70f, mapSize, mapSize);
        GUI.DrawTexture(mapRect, mapTexture);
        DrawPlayerMarker(mapRect);

        GUILayout.EndArea();

        // Close button drawn last, absolute-positioned under the map
        // rather than via GUILayout — the texture draw above already
        // breaks out of the layout flow (GUI.DrawTexture, not
        // GUILayout.Label), so a flowed button here would overlap it.
        var closeRect = new Rect((Screen.width - 100f) / 2f, mapRect.yMax + 15f, 100f, 30f);
        if (GUI.Button(closeRect, "Close"))
            SetOpen(false);
    }

    // Rebuilds the texture only when PlayerMapExploration has actually
    // revealed something new since the last draw (RevealVersion), not
    // every OnGUI frame — same "only redo the work when the underlying
    // data changed" discipline GardenPlot4x4.UpdateVisualStage already
    // applies to its own per-frame check.
    private void EnsureTexture()
    {
        if (mapTexture != null && cachedRevealVersion == exploration.RevealVersion) return;

        int w = exploration.GridWidth;
        int h = exploration.GridHeight;
        if (mapTexture == null || mapTexture.width != w || mapTexture.height != h)
        {
            mapTexture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            mapTexture.filterMode = FilterMode.Point;
            mapTexture.wrapMode = TextureWrapMode.Clamp;
        }

        var pixels = new Color[w * h];
        for (int x = 0; x < w; x++)
        {
            for (int z = 0; z < h; z++)
            {
                // Texture row 0 is the bottom in Unity's convention, which
                // already matches world -Z-to-+Z running bottom-to-top on
                // screen the way a top-down map should read.
                pixels[z * w + x] = exploration.IsRevealed(x, z) ? RevealedColor : FogColor;
            }
        }
        mapTexture.SetPixels(pixels);
        mapTexture.Apply();

        cachedRevealVersion = exploration.RevealVersion;
    }

    private void DrawPlayerMarker(Rect mapRect)
    {
        exploration.WorldToCell(transform.position, out int cellX, out int cellZ);
        float u = (float)cellX / exploration.GridWidth;
        float v = (float)cellZ / exploration.GridHeight;

        // v is measured bottom-up (matches the texture's own row-0-is-
        // bottom convention above); GUI Rects are measured top-down, so
        // the marker's Y needs flipping relative to the map rect.
        float markerX = mapRect.x + u * mapRect.width - MarkerSize / 2f;
        float markerY = mapRect.y + (1f - v) * mapRect.height - MarkerSize / 2f;

        var prevColor = GUI.color;
        GUI.color = PlayerMarkerColor;
        GUI.DrawTexture(new Rect(markerX, markerY, MarkerSize, MarkerSize), Texture2D.whiteTexture);
        GUI.color = prevColor;
    }
}
