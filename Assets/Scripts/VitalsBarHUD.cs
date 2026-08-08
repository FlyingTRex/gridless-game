using UnityEngine;

// Always-on Health/Stamina/Hunger/Thirst bar HUD, bottom-center of the
// screen, 2x2 grid (Health/Stamina top row, Hunger/Thirst bottom row).
// Independent of PlayerHealthMonitor's detailed text panel — this is a
// baseline glanceable readout, not gated behind wearing anything.
//
// Each bar's full width represents 150% of a stat's normal max (100), not
// just 100% — so under ordinary values (0-100) the top third of every bar
// stays unfilled/transparent by design, reserved headroom rather than a
// visual bug, in case some future buff ever pushes a stat past 100.
[RequireComponent(typeof(PlayerVitals))]
public class VitalsBarHUD : MonoBehaviour
{
    private const float ScaleMax = 100f * 1.5f;

    private const float BarWidth = 160f;
    private const float BarHeight = 20f;
    private const float ColumnGap = 10f;
    private const float RowGap = 6f;
    private const float BottomMargin = 20f;
    private const float Padding = 10f;

    private PlayerVitals vitals;

    private Texture2D backgroundTex;
    private Texture2D healthTex;
    private Texture2D staminaTex;
    private Texture2D hungerTex;
    private Texture2D thirstTex;
    private Texture2D willTex;
    private GUIStyle labelStyle;

    private void Awake()
    {
        vitals = GetComponent<PlayerVitals>();
    }

    private static Texture2D MakeTex(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }

    private void EnsureStyles()
    {
        if (backgroundTex != null) return;

        backgroundTex = MakeTex(new Color(0f, 0f, 0f, 0.5f));
        healthTex = MakeTex(new Color(0.8f, 0.15f, 0.15f));
        staminaTex = MakeTex(new Color(0.85f, 0.75f, 0.15f));
        hungerTex = MakeTex(new Color(0.85f, 0.5f, 0.15f));
        thirstTex = MakeTex(new Color(0.2f, 0.5f, 0.85f));
        willTex = MakeTex(new Color(0.6f, 0.3f, 0.85f));

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
        };
        labelStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        EnsureStyles();

        float gridWidth = BarWidth * 2f + ColumnGap;
        // Third row for Will (added 2026-08-08, Magic System) — a single
        // full-width bar, not a third column, so Body Temperature (still
        // not shown anywhere, a pre-existing gap unrelated to this) isn't
        // implied to have a matching slot it doesn't actually have.
        float gridHeight = BarHeight * 3f + RowGap * 2f;
        float originX = (Screen.width - gridWidth) / 2f;
        float originY = Screen.height - BottomMargin - gridHeight;

        var panelRect = new Rect(originX - Padding, originY - Padding, gridWidth + Padding * 2f, gridHeight + Padding * 2f);
        DebugGUI.DrawPanel(panelRect);

        DrawBar(new Rect(originX, originY, BarWidth, BarHeight), "Health", vitals.Health, ScaleMax, healthTex);
        DrawBar(new Rect(originX + BarWidth + ColumnGap, originY, BarWidth, BarHeight), "Stamina", vitals.Stamina, ScaleMax, staminaTex);
        DrawBar(new Rect(originX, originY + BarHeight + RowGap, BarWidth, BarHeight), "Hunger", vitals.Hunger, ScaleMax, hungerTex);
        DrawBar(new Rect(originX + BarWidth + ColumnGap, originY + BarHeight + RowGap, BarWidth, BarHeight), "Thirst", vitals.Thirst, ScaleMax, thirstTex);
        // Scaled against its own current max, not the fixed ScaleMax the
        // other four use — Will's ceiling grows over play, so "percent of
        //150" would read as permanently-nearly-full once maxWill climbs
        // past that fixed number.
        DrawBar(new Rect(originX, originY + (BarHeight + RowGap) * 2f, gridWidth, BarHeight), "Will", vitals.Will, vitals.MaxWill, willTex);
    }

    private void DrawBar(Rect rect, string label, float value, float scaleMax, Texture2D fillTex)
    {
        GUI.DrawTexture(rect, backgroundTex);

        float fraction = Mathf.Clamp01(value / scaleMax);
        if (fraction > 0f)
        {
            var fillRect = new Rect(rect.x, rect.y, rect.width * fraction, rect.height);
            GUI.DrawTexture(fillRect, fillTex);
        }

        GUI.Label(rect, $"{label} {value:F0}", labelStyle);
    }
}
