using UnityEngine;

// Magic tab inside PlayerMenuScreen (Tab key) — read-only reference, unlike
// Crafting's clickable list, since wishes fire from in-world E-hold prompts
// (see Campfire.cs) rather than a menu button. See design-brief.md's Magic
// System "UI impact" note (2026-08-08).
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerSkills))]
[RequireComponent(typeof(PlayerVitals))]
[RequireComponent(typeof(PlayerMagic))]
public class MagicScreen : MonoBehaviour
{
    [SerializeField] private WishRecipe[] allWishes;

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
        if (allWishes != null)
        {
            foreach (var wish in allWishes)
            {
                if (wish == null || wish.lineage == null || !magic.IsLineageKnown(wish.lineage)) continue;

                any = true;
                int required = CraftTierScale.SkillRequirement(wish.unlockTier);
                bool unlocked = skills.GetLevel(wish.lineage) >= required;

                string label = $"{wish.wishName} ({wish.lineage.skillName}, {wish.willCost:F0} Will)";
                if (!unlocked)
                    label += $"  — requires {wish.lineage.skillName} {required}";

                GUILayout.Label(label, unlocked ? DebugGUI.Label : DebugGUI.Warning);
            }
        }

        if (!any)
            GUILayout.Label("No wishes known yet — your lineage hasn't unlocked one.", DebugGUI.Label);
    }
}
