using UnityEngine;

// Shared by any world object an NPCGathering job can walk up to and harvest
// directly into cargo — ResourceNode (ore/rock/Log nodes) and ChoppableTree
// (standing Trees), added 2026-08-13 during the Mining->Woodworking
// generalization (see NPC_JOB_GENERALIZATION_PLANNING.md). Both already had
// this exact shape individually (ResourceNode.TryMineForNPC/PeekYield
// predates this interface, added 2026-08-10) — this just names the common
// contract so NPCGathering can search one list of targets instead of one
// per concrete type.
//
// Deliberately distinct from INPCSearchable (see that file) — this
// interface's contract is "yields an item directly into cargo on success."
// A target that doesn't do that (BerryBush/HerbBush's search action) isn't
// this, and forcing it to be would make TryHarvestForNPC lie about what it
// does.
public interface INPCHarvestable
{
    bool IsAvailable { get; }
    ItemDefinition[] RequiredTools { get; }
    float SkillGain { get; }
    Transform transform { get; }

    // Read-only — doesn't consume the target. Lets NPCGathering check
    // "could I even carry this" before committing to a target.
    bool PeekYield(out ItemDefinition item, out int count);

    // Consumes the target (breaks the node / fells the tree) and returns
    // what it yielded. No tool check inside — the caller already verified
    // RequiredTools against its own equipped tools before ever calling
    // this, same as ResourceNode.TryMineForNPC's existing convention.
    bool TryHarvestForNPC(out ItemDefinition item, out int count);
}
