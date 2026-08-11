using UnityEngine;

// Which sub-tab of SkillsScreen a skill's level shows under — Gathering
// (raw extraction: Gathering, Mining), CraftingDiscipline (assembling
// materials into finished items: Woodworking, Stonework, Metalworking,
// Forging, Minting, Sewing), Combat (weapon usage — Archery, Spear, Sword,
// Gun, Bare-handed — none exist yet, no combat system to train them).
public enum SkillCategory
{
    Gathering,
    CraftingDiscipline,
    Combat,
    // Added 2026-08-08 for the four magic lineages (Elemental/Illusion/
    // Kinetic/Restoration) — see design-brief.md's Magic System section.
    Magic,
    // Added 2026-08-10 for Strength (and, later, Dexterity/Constitution/
    // Intelligence — see BUGS_AND_ENHANCEMENTS.md) — core stats grown via
    // the same skill-via-use model as every other skill, per
    // design-brief.md's explicit "no point-buy attributes" decision. Shown
    // on PlayerMenuScreen's Player tab, not the Skills tab, so they read as
    // a distinct broad-base layer rather than one more crafting discipline.
    Attribute,
}

[CreateAssetMenu(menuName = "Gridless/Skill Definition", fileName = "NewSkill")]
public class SkillDefinition : ScriptableObject
{
    public string skillName = "New Skill";
    public SkillCategory category = SkillCategory.CraftingDiscipline;
}
