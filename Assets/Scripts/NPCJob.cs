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

    // Training grants (NPC_TRAINING_PLANNING.md, 2026-08-16) -- parallel to
    // PlayerCrafting.bookGrantedRecipes/PlayerMagic.knownLineages, just
    // living on the NPC's own job/tool-state component rather than a
    // dedicated NPCCrafting/NPCMagic split, since NPCs have no spellcasting
    // system to justify a whole NPCMagic component yet. Never touched by
    // Assign()'s tool-wipe-on-reassignment rule or ClearJob() -- a trained
    // recipe/lineage is a standing exception on the NPC itself, not a
    // per-job tool loadout.
    private readonly HashSet<CraftingRecipe> grantedRecipes = new HashSet<CraftingRecipe>();
    private readonly HashSet<SkillDefinition> knownLineages = new HashSet<SkillDefinition>();

    // Per-NPC opt-in file logging (2026-08-21, Ben's ask, prompted by an
    // unexplained live "Iris is oscillating" report where nobody could
    // tell what she was targeting) -- toggled via a checkbox on
    // NPCHiringScreen's Manage tab, same runtime-only convention as
    // isHired/isWaitingForPayment. Lives here (not on NPCGathering
    // directly) since NPCHiringScreen manages any NPC regardless of which
    // job-kind component is actually active, and a future NPCCrafting/
    // NPCGuarding debug readout would want to read the same flag.
    public bool DebugEnabled { get; set; }

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

    // Pulls the first acceptable item the player is carrying for this
    // requirement, removes exactly one, and equips it. Checks the main
    // inventory first, then every worn container (Backpack, etc.) via
    // PlayerCarriedItems -- found live 2026-08-18: the original main-
    // inventory-only scope silently rejected tools the player genuinely
    // had, just stored in a worn Backpack rather than the main 4-slot
    // inventory (the normal way to carry more than a handful of items).
    public bool TryGiveTool(ToolRequirement requirement, PlayerInventory playerInventory, PlayerEquipment equipment)
    {
        if (requirement?.acceptableItems == null || playerInventory == null) return false;

        foreach (var item in requirement.acceptableItems)
        {
            if (item == null) continue;
            if (!PlayerCarriedItems.RemoveOne(playerInventory, equipment, item)) continue;

            equippedTools[requirement.label] = item;
            return true;
        }

        return false;
    }

    // Swaps whichever item currently fills this requirement's slot for a
    // different one the player explicitly picked (2026-08-17, "NPC
    // management" -- Ben's ask: "the ability to swap them for improved
    // versions"). Unlike Fire()'s tool-wipe, the replaced tool is returned
    // to the player's inventory, not lost -- this is a deliberate upgrade
    // action, not abandoning the NPC. No-ops if newItem isn't actually one
    // of this requirement's acceptableItems, or if the player doesn't
    // currently have one on hand (main inventory or a worn container,
    // 2026-08-18 -- see TryGiveTool's own comment for why).
    public bool SwapTool(ToolRequirement requirement, ItemDefinition newItem, PlayerInventory playerInventory, PlayerEquipment equipment)
    {
        if (requirement?.acceptableItems == null || newItem == null || playerInventory == null) return false;
        if (System.Array.IndexOf(requirement.acceptableItems, newItem) < 0) return false;
        if (!PlayerCarriedItems.RemoveOne(playerInventory, equipment, newItem)) return false;

        var previous = GetEquipped(requirement.label);
        equippedTools[requirement.label] = newItem;
        if (previous != null) playerInventory.AddItem(previous, 1);

        return true;
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

    // Mirrors PlayerCrafting.GrantRecipe/HasRequiredSkill's bookGrantedRecipes
    // set exactly, just on the NPC's side -- granting twice is a no-op
    // (HashSet.Add), and NPCTrainingScreen checks HasGrantedRecipe upfront so
    // a book already-known isn't even offered (see NPC_TRAINING_PLANNING.md
    // section 4's "book already granted" edge case).
    public bool HasGrantedRecipe(CraftingRecipe recipe) => recipe != null && grantedRecipes.Contains(recipe);
    public void GrantRecipe(CraftingRecipe recipe)
    {
        if (recipe != null) grantedRecipes.Add(recipe);
    }

    // Banked inertly (Ben's call, 2026-08-16) -- NPCs have no spellcasting
    // system at all today, so nothing currently reads this beyond the
    // already-known check below. Forward compatibility for a future NPC
    // magic-ability system, not a stub to apologize for. No bonus-level
    // tracking (unlike PlayerMagic.LearnLineage) since there's no consumer
    // that would ever read a magnitude yet -- presence is all that matters.
    public bool HasLineage(SkillDefinition lineage) => lineage != null && knownLineages.Contains(lineage);
    public void LearnLineage(SkillDefinition lineage)
    {
        if (lineage != null) knownLineages.Add(lineage);
    }
}
