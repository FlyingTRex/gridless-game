using UnityEngine;

public static class DebugGUI
{
    // Matches DrawPanel's own background exactly, so the selected tab reads
    // as physically connected to the panel below it — the core of the
    // file-folder illusion (2026-08-05): the active tab shares the panel's
    // color and sits flush against it, inactive tabs are visibly a
    // different, receded surface behind it.
    private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.92f);
    private static readonly Color TabUnselectedColor = new Color(0.12f, 0.12f, 0.13f, 0.85f);
    private static readonly Color TabUnselectedHoverColor = new Color(0.2f, 0.2f, 0.22f, 0.9f);
    // Meaningfully lighter than PanelColor so a slot box actually reads as
    // a distinct box against a Panel background — GUI.skin.box's default
    // runtime look turned out to have too little contrast there to be
    // visible at all (confirmed by Ben: empty contents-grid slots
    // rendered as nothing once their "Empty" text was removed).
    private static readonly Color SlotColor = new Color(0.3f, 0.3f, 0.32f, 1f);

    private static Texture2D panelBackground;
    private static Texture2D tabSelectedBackground;
    private static Texture2D tabUnselectedBackground;
    private static Texture2D tabUnselectedHoverBackground;
    private static Texture2D slotBackground;
    private static GUIStyle labelStyle;
    private static GUIStyle headerStyle;
    private static GUIStyle warningStyle;
    private static GUIStyle tabSelectedStyle;
    private static GUIStyle tabUnselectedStyle;
    private static GUIStyle panelStyle;
    private static GUIStyle slotStyle;

    public static void DrawPanel(Rect rect)
    {
        if (panelBackground == null)
            panelBackground = SolidTexture(PanelColor);

        GUI.DrawTexture(rect, panelBackground);
    }

    // Same background as DrawPanel, but as a GUIStyle for
    // GUILayout.BeginVertical/BeginHorizontal — the group auto-sizes to
    // fit whatever's drawn inside it and the background follows, instead
    // of needing the caller to pre-compute a Rect by hand. Use this when a
    // section of content (e.g. InventoryScreen's equipment slot list, or
    // its Back-preview + contents pair) needs to read as its own distinct
    // panel rather than floating directly on the 3D game view behind it.
    public static GUIStyle Panel
    {
        get
        {
            if (panelStyle == null)
            {
                if (panelBackground == null) panelBackground = SolidTexture(PanelColor);
                panelStyle = new GUIStyle();
                panelStyle.normal.background = panelBackground;
                panelStyle.padding = new RectOffset(15, 15, 15, 15);
                // A bare `new GUIStyle()` defaults to stretching to fill
                // whatever width/height GUILayout offers rather than
                // shrink-wrapping its content — without this, the panel
                // expands to the full row/screen instead of just framing
                // what's actually drawn inside it.
                panelStyle.stretchWidth = false;
                panelStyle.stretchHeight = false;
            }
            return panelStyle;
        }
    }

    // Explicit solid-color background for an individual item/empty slot
    // (e.g. InventoryScreen's contents grid boxes) — an actual texture
    // background rather than relying on GUI.skin.box's default runtime
    // appearance, which proved too low-contrast to read against a dark
    // panel. Use with GUILayout.Box/Button so capacity stays visible even
    // when a slot has nothing in it.
    public static GUIStyle Slot
    {
        get
        {
            if (slotStyle == null)
            {
                if (slotBackground == null) slotBackground = SolidTexture(SlotColor);
                slotStyle = new GUIStyle();
                slotStyle.normal.background = slotBackground;
                slotStyle.hover.background = slotBackground;
                slotStyle.active.background = slotBackground;
                slotStyle.stretchWidth = false;
                slotStyle.stretchHeight = false;
            }
            return slotStyle;
        }
    }

    private static Texture2D SolidTexture(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }

    public static GUIStyle Label
    {
        get
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.normal.textColor = Color.white;
            }
            return labelStyle;
        }
    }

    public static GUIStyle Header
    {
        get
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(GUI.skin.label);
                headerStyle.normal.textColor = Color.white;
                headerStyle.fontStyle = FontStyle.Bold;
                headerStyle.alignment = TextAnchor.MiddleCenter;
            }
            return headerStyle;
        }
    }

    public static GUIStyle Warning
    {
        get
        {
            if (warningStyle == null)
            {
                warningStyle = new GUIStyle(GUI.skin.label);
                warningStyle.normal.textColor = new Color(1f, 0.35f, 0.3f);
                warningStyle.fontStyle = FontStyle.Bold;
            }
            return warningStyle;
        }
    }

    // File-folder tab look (2026-08-05) — used by every tab bar in the game
    // (GameMenuScreen, PlayerMenuScreen, and the Crafting/Skills sub-tab
    // bars) so switching screens reads consistently. The active tab shares
    // DrawPanel's exact color (see PanelColor above) and a taller top
    // margin, so it visually pokes up and connects into the panel it's
    // attached to; inactive tabs use a darker, visibly separate surface.
    public static GUIStyle TabSelected
    {
        get
        {
            if (tabSelectedStyle == null)
            {
                if (tabSelectedBackground == null) tabSelectedBackground = SolidTexture(PanelColor);
                tabSelectedStyle = new GUIStyle(GUI.skin.button);
                tabSelectedStyle.normal.background = tabSelectedBackground;
                tabSelectedStyle.hover.background = tabSelectedBackground;
                tabSelectedStyle.active.background = tabSelectedBackground;
                tabSelectedStyle.normal.textColor = Color.white;
                tabSelectedStyle.hover.textColor = Color.white;
                tabSelectedStyle.active.textColor = Color.white;
                tabSelectedStyle.fontStyle = FontStyle.Bold;
                tabSelectedStyle.alignment = TextAnchor.MiddleCenter;
                tabSelectedStyle.margin = new RectOffset(2, 0, 0, 0);
            }
            return tabSelectedStyle;
        }
    }

    public static GUIStyle TabUnselected
    {
        get
        {
            if (tabUnselectedStyle == null)
            {
                if (tabUnselectedBackground == null) tabUnselectedBackground = SolidTexture(TabUnselectedColor);
                if (tabUnselectedHoverBackground == null) tabUnselectedHoverBackground = SolidTexture(TabUnselectedHoverColor);
                tabUnselectedStyle = new GUIStyle(GUI.skin.button);
                tabUnselectedStyle.normal.background = tabUnselectedBackground;
                tabUnselectedStyle.hover.background = tabUnselectedHoverBackground;
                tabUnselectedStyle.active.background = tabUnselectedHoverBackground;
                tabUnselectedStyle.normal.textColor = new Color(0.75f, 0.75f, 0.75f);
                tabUnselectedStyle.hover.textColor = Color.white;
                tabUnselectedStyle.active.textColor = Color.white;
                tabUnselectedStyle.fontStyle = FontStyle.Normal;
                tabUnselectedStyle.alignment = TextAnchor.MiddleCenter;
                // A few px taller top margin than the selected tab (which
                // has none) — the selected tab pokes up above the row,
                // the classic folder-divider cue.
                tabUnselectedStyle.margin = new RectOffset(2, 0, 4, 0);
            }
            return tabUnselectedStyle;
        }
    }
}
