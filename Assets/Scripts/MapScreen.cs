using UnityEngine;
using UnityEngine.InputSystem;

// Player Map UI (PLAYER_MAP_PLANNING.md, 2026-08-16) — M toggles it, same
// open/close/cursor-lock shape GameMenuScreen's backquote toggle already
// established. Reads PlayerMapExploration's revealed-cell grid and draws
// it as a texture (fog color for unrevealed cells, ground color for
// revealed ones), plus the player's own position as a marker. Also draws
// a named marker for every placed Village Flag (Ben's follow-up ask,
// 2026-08-16, once Flags became nameable via IRenameable) — shown
// unconditionally, not gated by fog reveal, same reasoning the player's
// own marker already gets.
[RequireComponent(typeof(PlayerMapExploration))]
public class MapScreen : MonoBehaviour
{
    private static readonly Color FogColor = new Color(0.05f, 0.05f, 0.05f, 1f);
    private static readonly Color RevealedColor = new Color(0.36f, 0.42f, 0.3f, 1f);
    private static readonly Color PlayerMarkerColor = new Color(0.95f, 0.85f, 0.2f, 1f);
    private static readonly Color FlagMarkerColor = new Color(0.85f, 0.25f, 0.2f, 1f);
    private static readonly Color NpcMarkerColor = new Color(0.3f, 0.65f, 0.9f, 1f);

    private const float MapFraction = 0.8f; // square map area, 80% of the shorter screen dimension
    private const float MarkerSize = 10f;
    private const float FlagMarkerSize = 8f;
    private const float NpcMarkerSize = 7f;

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
        DrawFlagMarkers(mapRect);
        DrawNpcMarkers(mapRect);
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
        Vector2 center = MapPointFor(transform.position, mapRect);
        var markerRect = new Rect(center.x - MarkerSize / 2f, center.y - MarkerSize / 2f, MarkerSize, MarkerSize);

        var prevColor = GUI.color;
        GUI.color = PlayerMarkerColor;
        GUI.DrawTexture(markerRect, Texture2D.whiteTexture);
        GUI.color = prevColor;
    }

    // Named Flag markers (Ben's follow-up ask, 2026-08-16) -- shown
    // unconditionally, not gated by fog reveal, same "you obviously know
    // where your own Flag is" reasoning the player's own marker already
    // gets. Drawn before DrawPlayerMarker so the player's marker renders
    // on top if the two ever overlap.
    private void DrawFlagMarkers(Rect mapRect)
    {
        foreach (var flag in FindObjectsByType<VillageFlag>(FindObjectsSortMode.None))
        {
            Vector2 center = MapPointFor(flag.transform.position, mapRect);
            var markerRect = new Rect(center.x - FlagMarkerSize / 2f, center.y - FlagMarkerSize / 2f, FlagMarkerSize, FlagMarkerSize);

            var prevColor = GUI.color;
            GUI.color = FlagMarkerColor;
            GUI.DrawTexture(markerRect, Texture2D.whiteTexture);
            GUI.color = prevColor;

            var content = new GUIContent(flag.DisplayName);
            var labelSize = FlagLabelStyle.CalcSize(content);
            var labelRect = new Rect(center.x - labelSize.x / 2f, markerRect.y - labelSize.y - 2f, labelSize.x, labelSize.y);
            GUI.Label(labelRect, content, FlagLabelStyle);
        }
    }

    private static GUIStyle flagLabelStyle;
    private static GUIStyle FlagLabelStyle => flagLabelStyle ??= new GUIStyle(GUI.skin.label)
    {
        fontSize = 12,
        alignment = TextAnchor.MiddleCenter,
        normal = { textColor = FlagMarkerColor },
    };

    // NPC markers (2026-08-17, BUGS_AND_ENHANCEMENTS.md "NPC
    // identification") -- exact same live-scan pattern as DrawFlagMarkers,
    // just a different source type and color. A fresh FindObjectsByType
    // scan every OnGUI frame means these track each NPC's actual live
    // position for free, same as the Flag markers already do.
    private void DrawNpcMarkers(Rect mapRect)
    {
        foreach (var npc in FindObjectsByType<NPCHiring>(FindObjectsSortMode.None))
        {
            Vector2 center = MapPointFor(npc.transform.position, mapRect);
            var markerRect = new Rect(center.x - NpcMarkerSize / 2f, center.y - NpcMarkerSize / 2f, NpcMarkerSize, NpcMarkerSize);

            var prevColor = GUI.color;
            GUI.color = NpcMarkerColor;
            GUI.DrawTexture(markerRect, Texture2D.whiteTexture);
            GUI.color = prevColor;

            var dialogue = npc.GetComponent<NPCDialogue>();
            var content = new GUIContent(dialogue != null ? dialogue.DisplayName : "NPC");
            var labelSize = NpcLabelStyle.CalcSize(content);
            var labelRect = new Rect(center.x - labelSize.x / 2f, markerRect.y - labelSize.y - 2f, labelSize.x, labelSize.y);
            GUI.Label(labelRect, content, NpcLabelStyle);
        }
    }

    private static GUIStyle npcLabelStyle;
    private static GUIStyle NpcLabelStyle => npcLabelStyle ??= new GUIStyle(GUI.skin.label)
    {
        fontSize = 12,
        alignment = TextAnchor.MiddleCenter,
        normal = { textColor = NpcMarkerColor },
    };

    // Shared world-to-map-pixel conversion -- v is measured bottom-up
    // (matches the texture's own row-0-is-bottom convention in
    // EnsureTexture above); GUI Rects are measured top-down, so Y needs
    // flipping relative to the map rect.
    private Vector2 MapPointFor(Vector3 worldPos, Rect mapRect)
    {
        exploration.WorldToCell(worldPos, out int cellX, out int cellZ);
        float u = (float)cellX / exploration.GridWidth;
        float v = (float)cellZ / exploration.GridHeight;
        return new Vector2(mapRect.x + u * mapRect.width, mapRect.y + (1f - v) * mapRect.height);
    }
}
