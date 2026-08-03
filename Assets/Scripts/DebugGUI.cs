using UnityEngine;

public static class DebugGUI
{
    private static Texture2D panelBackground;
    private static GUIStyle labelStyle;
    private static GUIStyle headerStyle;
    private static GUIStyle warningStyle;

    public static void DrawPanel(Rect rect)
    {
        if (panelBackground == null)
        {
            panelBackground = new Texture2D(1, 1);
            panelBackground.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.65f));
            panelBackground.Apply();
        }

        GUI.DrawTexture(rect, panelBackground);
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
}
