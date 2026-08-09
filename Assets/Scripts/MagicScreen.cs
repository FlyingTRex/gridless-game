using UnityEngine;

// Magic tab inside PlayerMenuScreen (Tab key). Reads PlayerMagic.KnownWishes
// (the single source of truth as of 2026-08-08) and lets the player pick
// their "default skill" — R attempts whichever wish is selected here,
// against whatever target that wish's own WishTargeting mode calls for
// (see PlayerInteraction). Was purely read-only before this; now has one
// real action (Select).
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerSkills))]
[RequireComponent(typeof(PlayerVitals))]
[RequireComponent(typeof(PlayerMagic))]
public class MagicScreen : MonoBehaviour
{
    private PlayerSkills skills;
    private PlayerVitals vitals;
    private PlayerMagic magic;

    private void Awake()
    {
        skills = GetComponent<PlayerSkills>();
        vitals = GetComponent<PlayerVitals>();
        magic = GetComponent<PlayerMagic>();
    }

    // Called by PlayerMenuScreen while its Magic tab is active.
    public void DrawContent()
    {
        GUILayout.Label("Magic", DebugGUI.Header);

        string lineageName = magic.StartingLineage != null ? magic.StartingLineage.skillName : "None";
        GUILayout.Label($"Lineage: {lineageName}", DebugGUI.Label);
        GUILayout.Label($"Will: {vitals.Will:F0} / {vitals.MaxWill:F0}", DebugGUI.Label);
        GUILayout.Space(10);

        bool any = false;
        foreach (var wish in magic.KnownWishes)
        {
            any = true;
            int required = CraftTierScale.SkillRequirement(wish.unlockTier);
            bool unlocked = skills.GetLevel(wish.lineage) >= required;
            bool selected = magic.SelectedWish == wish;

            string label = $"{wish.wishName} ({wish.lineage.skillName}, "
                + $"{wish.successWillCost:F0} Will on success / {wish.failureWillCost:F0} on failure)";
            if (!unlocked)
                label += $"  — requires {wish.lineage.skillName} {required}";

            GUILayout.BeginHorizontal();
            GUILayout.Label(label, unlocked ? DebugGUI.Label : DebugGUI.Warning);

            // Selecting an unlocked-later wish is allowed — CanAttempt still
            // gates the actual attempt on skill tier, this just picks which
            // wish R goes for once you do qualify.
            GUI.enabled = !selected;
            if (GUILayout.Button(selected ? "Active" : "Select", GUILayout.Width(70)))
                magic.SelectWish(wish);
            GUI.enabled = true;

            GUILayout.EndHorizontal();
        }

        if (!any)
            GUILayout.Label("No wishes known yet — your lineage hasn't unlocked one.", DebugGUI.Label);
    }
}
