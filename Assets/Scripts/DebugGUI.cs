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

    private static Texture2D panelBackground;
    private static Texture2D tabSelectedBackground;
    private static Texture2D tabUnselectedBackground;
    private static Texture2D tabUnselectedHoverBackground;
    private static GUIStyle labelStyle;
    private static GUIStyle headerStyle;
    private static GUIStyle warningStyle;
    private static GUIStyle tabSelectedStyle;
    private static GUIStyle tabUnselectedStyle;

    public static void DrawPanel(Rect rect)
    {
        if (panelBackground == null)
            panelBackground = SolidTexture(PanelColor);

        GUI.DrawTexture(rect, panelBackground);
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
