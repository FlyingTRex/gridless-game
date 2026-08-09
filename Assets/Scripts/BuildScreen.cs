using UnityEngine;

// Build tab inside PlayerMenuScreen (Tab key). Same select/active shape as
// MagicScreen — lists pieces and lets the player arm one, but placement
// itself happens in the world (see PlayerBuilding), not a button here.
// Unlike Magic, Building gets full UI support on purpose (design-brief.md's
// Building System section) — this tab, the ghost preview, and the
// not-enough-materials message all show deliberately, not hidden.
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerSkills))]
[RequireComponent(typeof(PlayerBuilding))]
public class BuildScreen : MonoBehaviour
{
    private PlayerSkills skills;
    private PlayerBuilding building;

    private void Awake()
    {
        skills = GetComponent<PlayerSkills>();
        building = GetComponent<PlayerBuilding>();
    }

    // Called by PlayerMenuScreen while its Build tab is active.
    public void DrawContent()
    {
        GUILayout.Label("Building", DebugGUI.Header);
        GUILayout.Label("Left Mouse Button to place, Scroll Wheel to rotate.", DebugGUI.Label);
        GUILayout.Space(10);

        bool any = false;
        foreach (var piece in building.AllPieces)
        {
            if (piece == null) continue;
            any = true;

            bool unlocked = building.CanPlace(piece);
            bool armed = building.ArmedPiece == piece;

            string needs = "";
            if (piece.ingredients != null)
            {
                foreach (var ingredient in piece.ingredients)
                {
                    if (ingredient == null || ingredient.item == null) continue;
                    if (needs.Length > 0) needs += ", ";
                    needs += $"{ingredient.count}x {ingredient.item.itemName}";
                }
            }

            string label = $"{piece.pieceName}  (needs {needs})";
            int required = piece.trainedSkill != null ? CraftTierScale.SkillRequirement(piece.unlockTier) : 0;
            if (!unlocked)
                label += $"  — requires {piece.trainedSkill.skillName} {required}";

            GUILayout.BeginHorizontal();
            GUILayout.Label(label, unlocked ? DebugGUI.Label : DebugGUI.Warning);

            GUI.enabled = unlocked;
            if (GUILayout.Button(armed ? "Armed (click to cancel)" : "Arm", GUILayout.Width(140)))
                building.ArmPiece(armed ? null : piece);
            GUI.enabled = true;

            GUILayout.EndHorizontal();
        }

        if (!any)
            GUILayout.Label("No pieces available.", DebugGUI.Label);
    }
}
