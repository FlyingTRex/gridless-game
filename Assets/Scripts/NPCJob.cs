using System.Collections.Generic;
using UnityEngine;

// NPC-side job/tool state (2026-08-10, Chunk 2 of the Hireable NPCs build
// -- see BUGS_AND_ENHANCEMENTS.md). No actual job execution yet (Chunk 4
// is the autonomous mining loop) -- this is just "what job is assigned"
// and "which required tools has the player handed over," gating whether
// NPCJobScreen's Assign button is usable.
//
// equippedTools is runtime-only, not [SerializeField] -- same convention
// NPCHiring's isHired/isWaitingForPayment already use for state that only
// ever changes through code, never needs an Inspector default.
public class NPCJob : MonoBehaviour
{
    private NPCJobDefinition assignedJob;
    private readonly Dictionary<string, ItemDefinition> equippedTools = new Dictionary<string, ItemDefinition>();
    private StorageBox depositContainer;

    public NPCJobDefinition AssignedJob => assignedJob;

    // Read by NPCGathering (its own readiness gate) and NPCHiring (Chunk 6 --
    // the work timer only ticks while actually working) so both agree on
    // what "working" means instead of each re-deriving it separately.
    public bool IsReady => assignedJob != null && HasAllTools(assignedJob);

    // Where NPCGathering (Chunk 5) walks back to once full and deposits
    // cargo into, then resumes mining. Set via PlayerNPCDeposit's
    // point-and-confirm flow from NPCJobScreen. Deliberately NOT cleared
    // by Assign()'s reassignment wipe -- a Storage Box is a physical spot
    // in the world, not a consumable tool, so changing jobs doesn't
    // invalidate "where should mined stuff go." Fire() does clear it,
    // same full-reset treatment as everything else.
    public StorageBox DepositContainer => depositContainer;
    public void SetDepositContainer(StorageBox box) => depositContainer = box;

    public ItemDefinition GetEquipped(string label) =>
        equippedTools.TryGetValue(label, out var item) ? item : null;

    // Read by NPCEquipmentVisual (2026-08-13) to enumerate every currently
    // -equipped label/item pair and keep visual attachments in sync,
    // without needing to know each job's specific label set in advance.
    public IReadOnlyDictionary<string, ItemDefinition> EquippedTools => equippedTools;

    public bool HasAllTools(NPCJobDefinition job)
    {
        if (job?.toolRequirements == null) return true;
        foreach (var req in job.toolRequirements)
            if (GetEquipped(req.label) == null) return false;
        return true;
    }

    // Read by NPCGathering (Chunk 4) to check a ResourceNode's RequiredTools
    // against whatever the NPC actually has equipped, regardless of which
    // labeled slot it's in -- a node just needs "a Pickaxe," not one
    // specifically labeled "Pickaxe" in this job's own requirements.
    public bool HasAnyTool(ItemDefinition[] items)
    {
        if (items == null) return false;
        foreach (var equipped in equippedTools.Values)
            foreach (var item in items)
                if (equipped == item) return true;
        return false;
    }

    // Pulls the first acceptable item the player's main inventory has for
    // this requirement, removes exactly one, and equips it. Only the main
    // inventory is checked (not hands/backpack) -- simplest first pass,
    // same scope-cut every other tool-gated system this session started
    // with before anyone asked for more.
    public bool TryGiveTool(ToolRequirement requirement, PlayerInventory playerInventory)
    {
        if (requirement?.acceptableItems == null || playerInventory == null) return false;

        foreach (var item in requirement.acceptableItems)
        {
            if (item == null) continue;
            if (playerInventory.GetCount(item) <= 0) continue;

            playerInventory.RemoveItem(item, 1);
            equippedTools[requirement.label] = item;
            return true;
        }

        return false;
    }

    // Switching to a genuinely different job (or being fired) loses every
    // equipped tool for good -- Ben's explicit call, no return-to-player-
    // inventory step. Re-assigning the SAME job (e.g. re-confirming after
    // giving more tools) is a no-op on the equipped set, not a wipe.
    public void Assign(NPCJobDefinition job)
    {
        if (job != assignedJob)
            equippedTools.Clear();
        assignedJob = job;
    }

    // Written by SaveManager on load — sets the full job/tools/deposit
    // state directly from save data, bypassing Assign()'s tool-wipe-on-
    // reassignment rule (a player-action safeguard, not relevant to
    // restoring exactly the state that was saved).
    public void RestoreState(NPCJobDefinition job, Dictionary<string, ItemDefinition> tools, StorageBox deposit)
    {
        assignedJob = job;
        equippedTools.Clear();
        if (tools != null)
            foreach (var kv in tools)
                equippedTools[kv.Key] = kv.Value;
        depositContainer = deposit;
    }

    public void ClearJob()
    {
        equippedTools.Clear();
        assignedJob = null;
        depositContainer = null;
    }
}
