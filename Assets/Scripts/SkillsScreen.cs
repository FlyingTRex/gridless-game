using UnityEngine;
using UnityEngine.InputSystem;

// Skill levels, toggled with U. Was previously an always-on bottom-left
// panel drawn directly by PlayerSkills; pulled out into its own screen so
// it follows the same open/close convention as Inventory (I) and Crafting
// (O) instead of always taking up HUD space.
[RequireComponent(typeof(PlayerSkills))]
public class SkillsScreen : MonoBehaviour
{
    private const float PanelWidth = 260f;
    private const float PanelHeight = 260f;

    private PlayerSkills skills;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        skills = GetComponent<PlayerSkills>();
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.uKey.wasPressedThisFrame) return;

        // Always allow closing. Only allow opening from normal gameplay —
        // not while some other screen already has the cursor unlocked,
        // which would stack this on top of it.
        if (isOpen || Cursor.lockState == CursorLockMode.Locked)
            SetOpen(!isOpen);
    }

    // Called by FirstPersonController when Escape re-locks the cursor, so
    // the two toggles can't drift out of sync with each other.
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

        var rect = new Rect((Screen.width - PanelWidth) / 2f, (Screen.height - PanelHeight) / 2f, PanelWidth, PanelHeight);
        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Label("Skills", DebugGUI.Header);

        foreach (var pair in skills.Levels)
            GUILayout.Label($"{pair.Key.skillName}: {pair.Value:F1}", DebugGUI.Label);

        if (GUILayout.Button("Close", GUILayout.Width(100)))
            SetOpen(false);

        GUILayout.EndArea();
    }
}
