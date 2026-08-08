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
}

[CreateAssetMenu(menuName = "Gridless/Skill Definition", fileName = "NewSkill")]
public class SkillDefinition : ScriptableObject
{
    public string skillName = "New Skill";
    public SkillCategory category = SkillCategory.CraftingDiscipline;
}
