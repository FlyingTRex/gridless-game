using System.Collections.Generic;
using UnityEngine;

// The actual autonomous gathering loop (2026-08-10, Chunks 4-6 of the
// Hireable NPCs build -- see BUGS_AND_ENHANCEMENTS.md). Renamed from
// NPCMining 2026-08-13 (see NPC_JOB_GENERALIZATION_PLANNING.md) once it
// stopped being mining-only -- the loop itself was already generic
// (targets any assigned job's family/tools), the class name just hadn't
// caught up. Now spans three candidate pools instead of one:
//
// - INPCHarvestable (ResourceNode: ore/rock/Log nodes; ChoppableTree:
//   standing Trees) -- walk to it, harvest, item lands in cargo
//   immediately.
// - INPCSearchable (BerryBush/HerbBush's F-search half only -- their
//   other actions, if any, stay player-only) -- walk to it, trigger the
//   search, nothing lands in cargo yet; the search just seeds the world
//   with loose Pickups.
// - Loose Pickup objects already sitting in the world -- walk to it,
//   collect, item lands in cargo. This is what closes the loop after an
//   INPCSearchable trigger: on a later pass, the NPC finds the Pickups
//   its own search produced (or any other loose Pickup nearby) and
//   collects them, via the same "nearest thing I can currently do
//   something useful with" comparison as the other two pools -- no
//   separate state machine needed.
//
// Once assigned a job and fully equipped, finds the nearest available/
// carriable/tool-satisfied target across all three pools within
// searchRadius, walks to it, acts on it, repeats -- until nothing useful
// is left in range, at which point (Chunk 5) it walks back to its
// assigned NPCJob.DepositContainer, deposits everything, and resumes. If
// no deposit point has been set yet, falls back to Chunk 4's original
// behavior (just stops) rather than assuming one exists. Also stops
// entirely once NPCHiring.IsWaitingForPayment (Chunk 6) -- the 5-minute
// work timer running out -- until Pay clears it.
//
// Deliberately trains the job's own family skill (assignedJob.family)
// rather than the target's own SkillGain-bearing field's original
// trainedSkill -- the same physical action training a different skill
// depending on who's doing it is a real quirk, flagged in
// BUGS_AND_ENHANCEMENTS.md for Mining originally, same reasoning applies
// to every target kind added since.
[RequireComponent(typeof(NPCWander))]
[RequireComponent(typeof(NPCJob))]
[RequireComponent(typeof(NPCSkills))]
[RequireComponent(typeof(NPCEncumbrance))]
[RequireComponent(typeof(NPCCargo))]
public class NPCGathering : MonoBehaviour
{
    private enum TargetKind { None, Harvest, Search, Pickup }

    [SerializeField] private float searchRadius = 50f;
    // Bumped 2 -> 3 (2026-08-18) -- Ben's fix for the still-unexplained
    // position-oscillation bug (BUGS_AND_ENHANCEMENTS.md): the observed
    // drift is only ~0.1m each transition, so a full extra meter of
    // margin absorbs it regardless of root cause. Doesn't explain the
    // mystery, just makes the symptom stop mattering in practice.
    [SerializeField] private float harvestRange = 3f;
    [SerializeField] private float harvestDuration = 3f;
    [SerializeField] private float moveSpeed = 1.5f;

    // Leash anchored to the NPC's own DepositContainer, not its current
    // position (2026-08-17, "NPC management" -- Ben's ask, prompted by a
    // Miner observed live wandering far from base and getting stuck).
    // searchRadius above re-centers on wherever the NPC currently stands,
    // which lets it drift outward indefinitely across successive hops --
    // each hop only ever needs to be within searchRadius of wherever it
    // ALREADY wandered to, not of home. This is a second, independent
    // check anchored to a fixed point (the deposit box), so it can't
    // drift no matter how many hops it's taken. No effect at all if no
    // deposit container is assigned yet (same "nothing to check against
    // yet" fallback every other DepositContainer-dependent path in this
    // file already uses). Configurable per NPC via NPCHiringScreen.
    [SerializeField] private float maxRangeFromDeposit = 50f;
    public float MaxRangeFromDeposit
    {
        get => maxRangeFromDeposit;
        set => maxRangeFromDeposit = Mathf.Max(1f, value);
    }

    // How far ahead to check for something blocking the direct path, and
    // how far around it to deflect -- Ben's call, 2026-08-10: "build a
    // collision idea to allow the npc to hit something and change
    // direction," since there's no NavMesh in this project (same
    // constraint HostileCreature/NPCWander already live with). Not real
    // pathfinding -- just enough to slide along an obstacle's edge instead
    // of pushing straight through it or getting stuck.
    [SerializeField] private float obstacleCheckDistance = 1.5f;

    private NPCWander wander;
    private NPCJob job;
    private NPCSkills skills;
    private NPCEncumbrance encumbrance;
    private NPCCargo cargo;

    private NPCHiring hiring;

    // Shared with NPCCrafting/NPCTraining/NPCGuarding/NPCSeekFlag's own
    // MoveToward via NPCMovement.cs (2026-08-19) -- see that file's header.
    private readonly NPCMovement.StuckTracker stuckTracker = new();

    private Component currentTarget;
    private TargetKind targetKind;
    private float harvestTimer;
    private bool isPaused;
    private bool isReturning;
    private Vector3 harvestLockPosition;

    // Found live by Ben (2026-08-18) after an extensive elimination
    // process for the still-mysterious Miner position-drift bug -- see
    // BUGS_AND_ENHANCEMENTS.md. NPCGathering/NPCCrafting/NPCGuarding all
    // live permanently on every NPC prefab and all run their own Update()
    // every frame regardless of which job is actually assigned (the
    // established "bail early if wrong kind" convention). The `!ready`
    // branch used to call wander.SetPaused(false) unconditionally on
    // *every* idle frame, not just when actually releasing a pause this
    // component itself was holding -- so for e.g. a Mining-job NPC,
    // NPCCrafting's and NPCGuarding's own `!ready` branches were each
    // independently calling SetPaused(false) every single frame, racing
    // against NPCGathering's own SetPaused(true) with no defined winner
    // (Unity doesn't guarantee Update() order between sibling components).
    // On whichever frames the "wrong kind" component happened to run
    // after the active one, NPCWander's own independent wander target-
    // seeking would silently take over movement for a frame before the
    // active job component reclaimed control next frame -- a very
    // plausible match for the observed small, semi-consistent drift.
    // wasActive tracks whether *this* component was the one actually
    // holding the pause, so it only ever releases it on a genuine
    // active-to-inactive transition, never on every idle frame.
    private bool wasActive;

    // Driven by NPCDialogue the same way it already pauses NPCWander --
    // talking to the NPC should freeze it completely, not just whichever
    // component happens to be moving it at that moment.
    public void SetPaused(bool paused) => isPaused = paused;

    // Read by NPCAnimatorDriver to pick/hold the right Work state. True
    // across the exact same window Update() counts harvestTimer -- i.e.
    // once adjacent to a target and not mid-return -- so it lines up with
    // movement having already stopped for that target.
    public bool IsActingOnTarget => currentTarget != null && !isReturning && harvestTimer > 0f;

    public NPCJobDefinition.WorkAnimationType CurrentWorkAnimation =>
        job.AssignedJob != null ? job.AssignedJob.workAnimation : NPCJobDefinition.WorkAnimationType.None;

    private void Awake()
    {
        wander = GetComponent<NPCWander>();
        job = GetComponent<NPCJob>();
        skills = GetComponent<NPCSkills>();
        encumbrance = GetComponent<NPCEncumbrance>();
        cargo = GetComponent<NPCCargo>();
        // Optional -- not a RequireComponent, so a hypothetical future
        // non-hireable NPC with a job loop wouldn't need one. Chunk 6:
        // stops working while NPCHiring.IsWaitingForPayment, same as
        // being unassigned/unequipped -- an unpaid NPC just holds still
        // (mid-walk cargo isn't lost, just paused) until Pay clears it.
        hiring = GetComponent<NPCHiring>();
    }

    private void Update()
    {
        if (isPaused) return;

        // Bench-crafting sibling check (2026-08-16, section 7) -- job.IsReady
        // already requires AssignedJob != null, so this short-circuits safely
        // before ever dereferencing it.
        bool ready = job.IsReady
            && job.AssignedJob.kind == NPCJobDefinition.JobKind.Gathering
            && (hiring == null || !hiring.IsWaitingForPayment);
        if (!ready)
        {
            currentTarget = null;
            targetKind = TargetKind.None;
            isReturning = false;
            if (wasActive) wander.SetPaused(false);
            wasActive = false;
            return;
        }
        wasActive = true;

        if (isReturning)
        {
            UpdateReturning();
            return;
        }

        if (currentTarget == null || !CurrentTargetAvailable())
        {
            FindTarget();
            harvestTimer = 0f;
        }

        if (currentTarget == null)
        {
            // Nothing reachable/carriable/useful right now. Head back and
            // deposit whatever's already been gathered if there's
            // somewhere to put it; otherwise (no deposit point set yet --
            // Chunk 5's own precondition) fall back to Chunk 4's original
            // behavior and just wander instead of standing frozen.
            if (job.DepositContainer != null && cargo.Inventory.Slots.Count > 0)
            {
                isReturning = true;
                wander.SetPaused(true);
                return;
            }

            wander.SetPaused(false);
            return;
        }

        wander.SetPaused(true);

        Vector3 targetPos = currentTarget.transform.position;
        float distance = Vector3.Distance(transform.position, targetPos);

        bool isMoving = distance > harvestRange;

        if (isMoving)
        {
            MoveToward(targetPos, currentTarget.gameObject);
            harvestTimer = 0f;
            return;
        }

        // Position lock (2026-08-18, Ben's idea) -- belt-and-suspenders on
        // top of the wasActive fix above: whatever the true cause of the
        // drift turns out to be, forcibly re-asserting the exact position
        // captured the instant this NPC settled into range guarantees zero
        // visible movement while harvesting, regardless of what (if
        // anything) still touches this transform between frames.
        if (harvestTimer <= 0f) harvestLockPosition = transform.position;
        transform.position = harvestLockPosition;

        wander.FaceToward(targetPos);
        harvestTimer += Time.deltaTime;
        if (harvestTimer < harvestDuration) return;

        HarvestCurrentTarget();
        harvestTimer = 0f;
    }

    private void UpdateReturning()
    {
        var box = job.DepositContainer;
        if (box == null)
        {
            // Cleared mid-return (Fire, or reassigned) -- bail out cleanly
            // rather than walking toward a null forever.
            isReturning = false;
            return;
        }

        wander.SetPaused(true);

        Vector3 boxPos = box.transform.position;
        float distance = Vector3.Distance(transform.position, boxPos);

        if (distance > harvestRange)
        {
            MoveToward(boxPos, box.gameObject);
            return;
        }

        wander.FaceToward(boxPos);
        DepositCargo(box);
        isReturning = false;
    }

    // Snapshots cargo's slots before moving anything -- Inventory.RemoveItem
    // mutates its own backing list in place (can remove a slot outright),
    // so removing while iterating Slots directly isn't safe. Leftover that
    // doesn't fit in the box stays in cargo rather than being lost, same
    // "leftover" convention every other transfer in this game uses.
    private void DepositCargo(StorageBox box)
    {
        var toMove = new List<(ItemDefinition item, int count)>();
        foreach (var slot in cargo.Inventory.Slots)
            if (slot.item != null && slot.count > 0)
                toMove.Add((slot.item, slot.count));

        foreach (var (item, count) in toMove)
        {
            int leftover = box.Inventory.AddItem(item, count);
            int moved = count - leftover;
            if (moved > 0)
                cargo.Inventory.RemoveItem(item, moved);
        }
    }

    private bool CurrentTargetAvailable()
    {
        if (currentTarget == null) return false;
        return targetKind switch
        {
            TargetKind.Harvest => ((INPCHarvestable)currentTarget).IsAvailable,
            TargetKind.Search => ((INPCSearchable)currentTarget).IsAvailable,
            TargetKind.Pickup => true,
            _ => false,
        };
    }

    // Scans all three candidate pools and keeps the nearest one this NPC
    // can currently do something useful with, within searchRadius.
    private void FindTarget()
    {
        currentTarget = null;
        targetKind = TargetKind.None;
        float bestDistance = searchRadius;

        foreach (var node in FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
            ConsiderHarvestable(node, node, ref bestDistance);

        foreach (var tree in FindObjectsByType<ChoppableTree>(FindObjectsSortMode.None))
            ConsiderHarvestable(tree, tree, ref bestDistance);

        // Gated per-job (NPCJobDefinition.searchesBushes) -- see that
        // field's own comment for the live bug report (a Mining NPC
        // walking past ore to reach the nearest bush, then trying to
        // "mine" it) that caused this. BerryBush/HerbBush have no
        // RequiredTools of their own, so unlike the Harvestable pool
        // above, nothing else was gating this by job relevance.
        if (job.AssignedJob != null && job.AssignedJob.searchesBushes)
        {
            foreach (var berry in FindObjectsByType<BerryBush>(FindObjectsSortMode.None))
                ConsiderSearchable(berry, berry, ref bestDistance);

            foreach (var herb in FindObjectsByType<HerbBush>(FindObjectsSortMode.None))
                ConsiderSearchable(herb, herb, ref bestDistance);
        }

        // Gated per-job (NPCJobDefinition.collectLoosePickups) -- only
        // Forage needs this pool (it's what closes the loop after an
        // INPCSearchable trigger). Scanning it unconditionally let a
        // Mining/Woodworking NPC get distracted by whatever loose item was
        // nearest regardless of relevance to its actual job -- see that
        // field's own comment for the live bug report that caused this.
        if (job.AssignedJob != null && job.AssignedJob.collectLoosePickups)
        {
            foreach (var pickup in FindObjectsByType<Pickup>(FindObjectsSortMode.None))
                ConsiderPickup(pickup, ref bestDistance);
        }
    }

    // True if pos is within maxRangeFromDeposit of the assigned deposit
    // box, or if there's no deposit box assigned yet to check against
    // (same "nothing to check against yet" fallback every other
    // DepositContainer-dependent path in this file already uses).
    private bool WithinLeash(Vector3 pos)
    {
        var box = job.DepositContainer;
        if (box == null) return true;
        return Vector3.Distance(box.transform.position, pos) <= maxRangeFromDeposit;
    }

    private void ConsiderHarvestable(Component comp, INPCHarvestable target, ref float bestDistance)
    {
        if (!target.IsAvailable) return;
        if (!WithinLeash(comp.transform.position)) return;

        bool hasToolReq = target.RequiredTools != null && target.RequiredTools.Length > 0;
        if (hasToolReq && !job.HasAnyTool(target.RequiredTools)) return;

        if (!target.PeekYield(out var item, out var count)) return;
        if (item == null || !encumbrance.CanPickUp(item.weight * count)) return;

        float dist = Vector3.Distance(transform.position, comp.transform.position);
        if (dist >= bestDistance) return;

        bestDistance = dist;
        currentTarget = comp;
        targetKind = TargetKind.Harvest;
    }

    private void ConsiderSearchable(Component comp, INPCSearchable target, ref float bestDistance)
    {
        if (!target.IsAvailable) return;
        if (!WithinLeash(comp.transform.position)) return;

        float dist = Vector3.Distance(transform.position, comp.transform.position);
        if (dist >= bestDistance) return;

        bestDistance = dist;
        currentTarget = comp;
        targetKind = TargetKind.Search;
    }

    // No tool/family gate at all -- a loose Pickup needs neither, same as
    // the player's own Pickup.Complete. Side effect, noted in
    // NPC_JOB_GENERALIZATION_PLANNING.md section 3a: this also means a
    // foraging NPC will collect any nearby dropped item, not just ones its
    // own bush search produced.
    private void ConsiderPickup(Pickup pickup, ref float bestDistance)
    {
        if (!WithinLeash(pickup.transform.position)) return;

        var item = pickup.Item;
        if (item == null || !encumbrance.CanPickUp(item.weight * pickup.Quantity)) return;

        float dist = Vector3.Distance(transform.position, pickup.transform.position);
        if (dist >= bestDistance) return;

        bestDistance = dist;
        currentTarget = pickup;
        targetKind = TargetKind.Pickup;
    }

    private void HarvestCurrentTarget()
    {
        switch (targetKind)
        {
            case TargetKind.Harvest:
                var harvestable = (INPCHarvestable)currentTarget;
                bool succeeded = harvestable.TryHarvestForNPC(out var item, out var count);
                if (succeeded)
                {
                    cargo.Inventory.AddItem(item, count);
                    skills.GainExperience(job.AssignedJob.family, harvestable.SkillGain);
                }
                break;

            case TargetKind.Search:
                ((INPCSearchable)currentTarget).TriggerSearchForNPC();
                break;

            case TargetKind.Pickup:
                var pickup = (Pickup)currentTarget;
                if (pickup.TryPickupForNPC(out var pickedItem, out var pickedCount))
                {
                    cargo.Inventory.AddItem(pickedItem, pickedCount);
                    skills.GainExperience(job.AssignedJob.family, pickup.SkillGain);
                }
                break;
        }

        currentTarget = null;
        targetKind = TargetKind.None;
    }

    // Straight-line movement toward the target, same shape HostileCreature/
    // NPCWander already use -- obstacle deflection/stuck-recovery is shared
    // via NPCMovement.cs (2026-08-19, replacing the old single-normal-
    // deflection block that could stall against a corner). ignoreTarget is
    // whatever world object this move is actually headed toward (a
    // gathering target or a StorageBox) -- its own collider shouldn't count
    // as "blocked" once close enough to register a hit on it.
    private void MoveToward(Vector3 targetPos, GameObject ignoreTarget)
    {
        Vector3 toTarget = targetPos - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        Vector3 desired = toTarget.normalized;
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 moveDir = NPCMovement.FindClearDirection(origin, desired, obstacleCheckDistance, ignoreTarget, stuckTracker, Time.deltaTime);

        Vector3 flatTarget = transform.position + moveDir * moveSpeed * Time.deltaTime;
        Vector3 newPos = new Vector3(flatTarget.x, transform.position.y, flatTarget.z);
        newPos.y = GroundHeight.Sample(newPos, transform.position.y);
        transform.position = newPos;
        wander.FaceToward(transform.position + moveDir);
    }
}
