using System;
using UnityEngine;

// Skill levels — drawn as the Skills tab inside PlayerMenuScreen (Tab key).
// Used to be its own screen toggled with U; folded in 2026-08-04 so
// Inventory/Skills/Crafting all live under one key instead of three.
// Sub-tabbed by SkillCategory (2026-08-05) — a flat list stopped scaling
// once the skill roster grew past a handful, same reasoning as Crafting's
// discipline sub-tabs.
[RequireComponent(typeof(PlayerSkills))]
public class SkillsScreen : MonoBehaviour
{
    private const float TabWidth = 170f;
    private const float TabHeight = 28f;

    private PlayerSkills skills;
    private SkillCategory currentCategory = SkillCategory.Gathering;

    private void Awake()
    {
        skills = GetComponent<PlayerSkills>();
    }

    // Called by PlayerMenuScreen while its Skills tab is active.
    public void DrawContent()
    {
        GUILayout.Label("Skills", DebugGUI.Header);
        DrawCategoryTabs();
        GUILayout.Space(10);

        bool any = false;
        foreach (var pair in skills.Levels)
        {
            if (pair.Key == null || pair.Key.category != currentCategory) continue;
            GUILayout.Label($"{pair.Key.skillName}: {pair.Value:F1}", DebugGUI.Label);
            any = true;
        }

        if (!any)
            GUILayout.Label(EmptyMessage(currentCategory), DebugGUI.Label);
    }

    private void DrawCategoryTabs()
    {
        GUILayout.BeginHorizontal();
        foreach (SkillCategory category in Enum.GetValues(typeof(SkillCategory)))
        {
            var style = category == currentCategory ? DebugGUI.TabSelected : DebugGUI.TabUnselected;
            if (GUILayout.Button(CategoryLabel(category), style, GUILayout.Width(TabWidth), GUILayout.Height(TabHeight)))
                currentCategory = category;
        }
        GUILayout.EndHorizontal();
    }

    private static string CategoryLabel(SkillCategory category) => category switch
    {
        SkillCategory.CraftingDiscipline => "Crafting Disciplines",
        _ => category.ToString(),
    };

    // Combat gets an honest placeholder, same treatment as GameMenuScreen's
    // Audio/Graphics tabs — no combat/hunting system exists yet, so nothing
    // could ever train a Combat-category skill today.
    private static string EmptyMessage(SkillCategory category) => category switch
    {
        SkillCategory.Combat => "No skills yet — combat/hunting isn't built.",
        _ => "No skills trained yet.",
    };
}
