using UnityEngine;

// A category of tool an NPC job needs equipped before it can run — e.g.
// Mining's "Pickaxe" slot. acceptableItems is an OR set within this one
// category (any tier satisfies it), same convention ResourceNode/
// CraftingRecipe's requiredTools already use; a job needing several
// distinct tool categories at once (Pickaxe AND Shield AND Backpack) lists
// each as its own ToolRequirement in NPCJobDefinition.toolRequirements.
[System.Serializable]
public class ToolRequirement
{
    public string label;
    public ItemDefinition[] acceptableItems;
}

// One assignable NPC job (2026-08-10, Chunk 2 of the Hireable NPCs build
// -- see BUGS_AND_ENHANCEMENTS.md). Mirrors CraftingRecipe's role as pure
// data, read by NPCJobScreen the same way CraftingScreen reads
// CraftingRecipe. family is a real discipline SkillDefinition (Mining),
// not an NPC-only concept, so an NPC's job skill is a real trainable skill
// -- tier isn't enforced against anything yet (Chunk 3 adds the NPC's own
// skill level to gate it; today every job is tier 1 and always shown).
[CreateAssetMenu(menuName = "Gridless/NPC Job Definition", fileName = "NewNPCJob")]
public class NPCJobDefinition : ScriptableObject
{
    // Which Kevin Iglesias Work/ animation loop NPCAnimatorDriver plays while
    // this job's harvestTimer is counting (NPCGathering.IsActingOnTarget) --
    // one enum entry per Work subfolder actually mapped to a shipped job
    // family so far, not every subfolder the pack ships.
    public enum WorkAnimationType { None, Mining, Chopping, Gathering }

    public string jobName = "New Job";
    public SkillDefinition family;
    public int tier = 1;
    public ToolRequirement[] toolRequirements;
    public WorkAnimationType workAnimation = WorkAnimationType.None;
}
