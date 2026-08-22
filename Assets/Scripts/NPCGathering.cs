using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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

    // NavMesh Phase 0 spike (2026-08-21, NPC_NAVIGATION_PLANNING.md) --
    // optional (not a RequireComponent yet, only the pilot prefab has one
    // wired up so far). Used purely as a pathfinding *oracle*: updatePosition/
    // updateRotation are both left off in Awake below, so the agent never
    // writes to transform itself -- MoveToward stays the sole owner of
    // transform.position exactly as before, meaning harvest-lock/
    // GroundHeight sampling/wander.FaceToward/NPCWander's own idle movement
    // all keep working completely unchanged. Null (no NavMeshAgent on this
    // prefab, or off the baked mesh) falls back to the old NPCMovement
    // straight-line-plus-deflection system, same behavior as before this
    // spike -- this is an additive, backward-compatible change.
    private NavMeshAgent agent;

    // Shared with NPCCrafting/NPCTraining/NPCGuarding/NPCSeekFlag's own
    // MoveToward via NPCMovement.cs (2026-08-19) -- see that file's header.
    private readonly NPCMovement.StuckTracker stuckTracker = new();

    private Component currentTarget;
    private TargetKind targetKind;
    private float harvestTimer;
    private bool isPaused;
    private bool isReturning;
    private Vector3 harvestLockPosition;

    // Give-up/retarget watchdog (2026-08-21) -- the physics safety net in
    // MoveToward correctly stops an NPC from clipping through a wall, but
    // on its own that just traded "clips through" for "stands frozen at
    // the wall forever" whenever the real destination turns out to be on
    // the unreachable side. Found live: most of a 4-NPC roster stuck at
    // once, each blocked by something specific to its own target/route.
    private const float BlockedGiveUpSeconds = 5f;
    private const float AvoidTargetSeconds = 30f;
    private const float ReturnRetryCooldownSeconds = 10f;
    // Real distance shrinking, not "did this exact frame's step succeed" --
    // found live (2026-08-21): an NPC pinned in a wall corner can still
    // jitter a few centimeters per frame in essentially random directions
    // (the corner-pinch symptom from earlier), which kept individually
    // succeeding just often enough to reset a per-step blocked counter
    // before it ever reached the give-up threshold, even though she made
    // zero real progress toward the target for over a minute. Tracking
    // actual distance-to-target progress instead catches both a hard
    // block and a stuck-jittering-in-place case the same way.
    private const float ProgressEpsilon = 0.05f;
    private float lastProgressDistance = float.MaxValue;
    private float noProgressSeconds;
    private float returnRetryCooldownUntil;
    private readonly Dictionary<Component, float> avoidUntilTime = new();

    // Reset wherever a fresh pursuit begins (a new harvest/search target,
    // or a new return-to-deposit leg) so a stale distance from whatever
    // was being pursued before doesn't immediately misfire the watchdog.
    private void ResetProgressTracking()
    {
        lastProgressDistance = float.MaxValue;
        noProgressSeconds = 0f;
    }

    private bool IsAvoided(Component comp) =>
        avoidUntilTime.TryGetValue(comp, out var until) && Time.time < until;

    // Called once MoveToward has been physically blocked continuously for
    // BlockedGiveUpSeconds. Two different recoveries depending on what was
    // being pursued -- a harvest/search target can be swapped for a
    // different one (FindTarget will naturally pick something else once
    // this one is excluded), but there's no alternative deposit box to
    // fall back to, so that case just backs off and retries later instead.
    private void HandleMovementBlocked()
    {
        ResetProgressTracking();

        if (isReturning)
        {
            isReturning = false;
            returnRetryCooldownUntil = Time.time + ReturnRetryCooldownSeconds;
            return;
        }

        if (currentTarget != null)
            avoidUntilTime[currentTarget] = Time.time + AvoidTargetSeconds;

        currentTarget = null;
        targetKind = TargetKind.None;
        harvestTimer = 0f;
    }

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

    // Debug readout (2026-08-21, Ben's ask, prompted by an unexplained
    // live "Iris is oscillating" report — same "add visibility instead of
    // guessing" approach that already worked for the Furnace's stalled-
    // queue mystery). Read by NPCDebugScreen; doesn't affect behavior.
    public string DebugStatus
    {
        get
        {
            if (isPaused) return "paused (NPCDialogue talking)";
            if (job.AssignedJob == null || job.AssignedJob.kind != NPCJobDefinition.JobKind.Gathering)
                return "not a Gathering-kind job";
            if (!job.IsReady) return "not ready (missing tools / unassigned / awaiting payment)";

            if (isReturning)
            {
                var box = job.DepositContainer;
                if (box == null) return "returning to deposit — no deposit box set";

                float boxDist = Vector3.Distance(transform.position, box.transform.position);
                // Distinct from the general "returning" text once actually
                // in range with cargo still on hand -- this is the stuck-
                // waiting state (2026-08-21 fix below), not mid-walk.
                if (boxDist <= harvestRange && cargo.Inventory.Slots.Count > 0)
                    return $"waiting — deposit box '{box.DisplayName}' is full, holding cargo{AgentSuffix()}";

                return $"returning to deposit — deposit box '{box.DisplayName}' dist={boxDist:F1}{AgentSuffix()}";
            }

            if (currentTarget == null)
                return $"no target — idle/wandering (NPCWander, no NavMesh) pos=({transform.position.x:F1},{transform.position.z:F1})";

            float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
            string action = IsActingOnTarget ? $"acting (harvestTimer={harvestTimer:F1}s)" : "moving toward";
            return $"{targetKind} '{currentTarget.name}' dist={dist:F1} — {action}{AgentSuffix()}";
        }
    }

    // pos=(x,z) added 2026-08-21 -- distance-to-target alone couldn't
    // distinguish "routed around the wall" from "walked straight through
    // it" after a live sighting of the latter; raw position lets a route
    // be plotted against known wall placements after the fact.
    private string AgentSuffix()
    {
        string pos = $" pos=({transform.position.x:F1},{transform.position.z:F1})";
        if (agent == null) return $" | agent: none (fallback movement){pos}";
        if (!agent.isOnNavMesh) return $" | agent: OFF NAVMESH (fallback movement){pos}";
        return $" | agent: hasPath={agent.hasPath} status={agent.pathStatus} remaining={agent.remainingDistance:F1} desiredVel={agent.desiredVelocity.magnitude:F1}{pos}";
    }

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

        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.speed = moveSpeed;
        }
    }

    private const float DebugLogInterval = 1f;
    private float nextDebugLogTime;

    private void Update()
    {
        if (isPaused) return;

        if (job.DebugEnabled && Time.time >= nextDebugLogTime)
        {
            nextDebugLogTime = Time.time + DebugLogInterval;
            var dialogue = GetComponent<NPCDialogue>();
            DebugLog.Write(dialogue != null ? dialogue.DisplayName : name, DebugStatus);
        }

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
            ResetProgressTracking();
        }

        if (currentTarget == null)
        {
            // Nothing reachable/carriable/useful right now. Head back and
            // deposit whatever's already been gathered if there's
            // somewhere to put it; otherwise (no deposit point set yet --
            // Chunk 5's own precondition) fall back to Chunk 4's original
            // behavior and just wander instead of standing frozen.
            if (job.DepositContainer != null && cargo.Inventory.Slots.Count > 0
                && Time.time >= returnRetryCooldownUntil)
            {
                isReturning = true;
                ResetProgressTracking();
                wander.SetPaused(true);
                return;
            }

            wander.SetPaused(false);
            return;
        }

        wander.SetPaused(true);

        Vector3 targetPos = currentTarget.transform.position;
        float distance = Vector3.Distance(transform.position, targetPos);

        // A ChoppableTree's transform pivot sits well beyond where its
        // collider actually blocks approach -- confirmed live, 2026-08-19,
        // via [TreeStuckDiagnostic] (now removed): pivotDistance=3.99,
        // harvestRange=3.00, colliderSurfaceDistance=0.00, identical across
        // every tree instance tested, meaning an NPC could be physically
        // touching the tree and still never satisfy the pivot-distance
        // check below. Measuring against the collider surface instead for
        // this one target type fixes the mismatch without loosening
        // harvestRange for every other target (ResourceNode/StorageBox
        // don't have this offset).
        if (currentTarget is ChoppableTree)
        {
            var treeCollider = currentTarget.GetComponent<Collider>();
            if (treeCollider != null)
                distance = Vector3.Distance(transform.position, treeCollider.ClosestPoint(transform.position));
        }

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

        // Only leave the returning state once cargo is actually empty. A
        // full deposit box leaves leftover behind (DepositCargo above
        // only removes what fit) -- clearing isReturning unconditionally
        // here caused a real live bug (2026-08-21): next tick's
        // FindTarget() finds nothing, sees the leftover cargo, and
        // immediately re-enters isReturning, which re-attempts the same
        // already-failed deposit -- an infinite idle/returning flicker
        // with the NPC frozen in place, visible in the debug log as a
        // dead position that never moves. Staying in the returning state
        // here instead means the NPC visibly waits at the box (status
        // above reports it explicitly) until space actually opens up,
        // then resumes automatically -- no thrash, no separate cooldown
        // timer needed.
        if (cargo.Inventory.Slots.Count == 0)
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
        if (IsAvoided(comp)) return;
        if (!WithinLeash(comp.transform.position)) return;

        bool hasToolReq = target.RequiredTools != null && target.RequiredTools.Length > 0;
        if (hasToolReq && !job.HasAnyTool(target.RequiredTools)) return;

        // Toolless targets (the plain Rock/Small Rock node -- ore Boulders
        // all declare a real RequiredTools, so they're unaffected) skip the
        // check above entirely, which used to mean any Gathering-kind job
        // could harvest one purely on distance. Gated per-job the same way
        // as searchesBushes/collectLoosePickups -- see
        // NPCJobDefinition.harvestsToollessRock's own comment.
        if (!hasToolReq && (job.AssignedJob == null || !job.AssignedJob.harvestsToollessRock)) return;

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
        if (IsAvoided(comp)) return;
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
        if (IsAvoided(pickup)) return;
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
    // NavMesh Phase 2 (2026-08-21, NPC_NAVIGATION_PLANNING.md) -- a
    // NavMeshObstacle carving on the Door leaf handles the *pathing* side
    // (a closed door blocks, an open one doesn't) automatically once one
    // exists near enough, but nothing makes an NPC *want* to open a
    // closed door in its way -- that's what this checks for, on a
    // cheap timer so it isn't a raycast every single frame.
    private const float DoorCheckDistance = 3f;
    private const float DoorCheckInterval = 0.5f;
    private float nextDoorCheckTime;

    private void CheckForBlockingDoor()
    {
        if (Time.time < nextDoorCheckTime) return;
        nextDoorCheckTime = Time.time + DoorCheckInterval;

        Vector3 dir = transform.forward;
        if (agent != null && agent.hasPath)
        {
            Vector3 toSteer = agent.steeringTarget - transform.position;
            toSteer.y = 0f;
            if (toSteer.sqrMagnitude > 0.01f) dir = toSteer.normalized;
        }

        Vector3 origin = transform.position + Vector3.up * 0.9f;
        if (Physics.Raycast(origin, dir, out var hit, DoorCheckDistance))
        {
            var door = hit.collider.GetComponentInParent<Door>();
            if (door != null && !door.IsOpen)
                door.OpenForNPC(transform.position);
        }
    }

    private void MoveToward(Vector3 targetPos, GameObject ignoreTarget)
    {
        CheckForBlockingDoor();

        Vector3 toTarget = targetPos - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        Vector3 moveDir;

        if (agent != null && agent.isOnNavMesh)
        {
            // NavMesh Phase 0 spike -- real routing around obstacles
            // (walls) instead of the old short-range widening-angle
            // search, which is structurally incapable of finding a route
            // through a doorway (see NPC_NAVIGATION_PLANNING.md). The
            // agent only ever supplies a desired *direction* here --
            // updatePosition/updateRotation are off (set in Awake), so
            // transform.position stays fully owned by this method below,
            // same as the fallback branch. desiredVelocity can be
            // momentarily zero for a frame or two right after
            // SetDestination while the path is still being computed;
            // falling back to the straight-line direction for that one
            // frame is safer than standing frozen.
            agent.SetDestination(targetPos);
            Vector3 desiredVel = agent.desiredVelocity;
            desiredVel.y = 0f;
            moveDir = desiredVel.sqrMagnitude > 0.0001f ? desiredVel.normalized : toTarget.normalized;
        }
        else
        {
            Vector3 desired = toTarget.normalized;
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            moveDir = NPCMovement.FindClearDirection(origin, desired, obstacleCheckDistance, ignoreTarget, stuckTracker, Time.deltaTime, transform);
        }

        Vector3 flatTarget = transform.position + moveDir * moveSpeed * Time.deltaTime;
        Vector3 newPos = new Vector3(flatTarget.x, transform.position.y, flatTarget.z);
        newPos.y = GroundHeight.Sample(newPos, transform.position.y);

        // Physics safety net (2026-08-21) -- this method fully owns
        // transform.position (updatePosition=false on the agent) and, until
        // now, applied every step unconditionally, trusting the navmesh/
        // agent steering as the *only* source of truth for "can I go here."
        // Live-confirmed (screenshot) an NPC clipping straight through a
        // wall corner even after wall pieces got real NavMeshObstacles --
        // most likely two adjoining obstacles' carve regions pinching the
        // walkable corridor tighter than the agent's radius at that exact
        // seam, which the navmesh may still report as a technically valid
        // route. A sweep test against the real colliders closes that gap
        // regardless of any remaining navmesh/obstacle-carving imprecision,
        // the same way a CharacterController would for the player -- NPCs
        // move via a raw transform set, so nothing else here ever checked.
        if (!StepIsBlocked(transform.position, newPos, ignoreTarget))
        {
            transform.position = newPos;
        }

        // Give-up watchdog -- see HandleMovementBlocked's own comment.
        // Driven by real distance-to-target progress, not by whether this
        // exact frame's step was rejected -- a hard block and a stuck-
        // jittering-in-place case (the corner-pinch symptom, individual
        // steps occasionally succeeding by a few centimeters with zero net
        // progress) both need to trip this the same way, and only the
        // former would ever show up in StepIsBlocked's per-frame result.
        float distToTarget = toTarget.magnitude;
        if (distToTarget < lastProgressDistance - ProgressEpsilon)
        {
            lastProgressDistance = distToTarget;
            noProgressSeconds = 0f;
        }
        else
        {
            noProgressSeconds += Time.deltaTime;
            if (noProgressSeconds >= BlockedGiveUpSeconds)
            {
                // Physically escape first, then abandon the target -- a
                // trapped *position* (not just a bad target) needs an
                // actual relocation, or the next target FindTarget picks
                // will just fail from the exact same spot too. Escapes
                // away from whatever direction we were failing to reach.
                Vector3 origin = transform.position + Vector3.up * 0.5f;
                NPCMovement.EscapeBump(origin, -toTarget.normalized, ignoreTarget, transform);
                HandleMovementBlocked();
                return;
            }
        }
        // Keep the agent's own internal path-following state in sync with
        // the real transform, since updatePosition=false means Unity
        // doesn't do this automatically.
        if (agent != null && agent.isOnNavMesh) agent.nextPosition = transform.position;
        wander.FaceToward(transform.position + moveDir);
    }

    // Chest-height sweep matching the NavMeshAgent's own radius (0.3) so it
    // rejects the same step width the pathfinder is supposed to be
    // planning around. Ignores this NPC's own colliders and whatever
    // world object the move is actually headed toward (a harvest target or
    // deposit box), same exemption MoveToward's ignoreTarget already
    // carries for the old fallback movement path.
    private const float StepCheckRadius = 0.3f;
    // Excludes Ground -- this sweep is purely horizontal at chest height to
    // catch vertical obstacles (walls); vertical positioning is already
    // handled separately by GroundHeight.Sample, and including Ground here
    // risks a false "blocked" on sloped terrain where the floor collider
    // can intersect a chest-height sweep.
    // NOT a static field initializer -- LayerMask.GetMask() throws
    // ("NameToLayer is not allowed to be called from a MonoBehaviour
    // constructor or instance field initializer") if called that way, and
    // because this was `static readonly`, that single throw poisoned the
    // *entire* NPCGathering type for the rest of the session (a .NET
    // TypeInitializationException cascade) -- every other NPC's
    // NPCGathering then failed to initialize too. Found live 2026-08-21:
    // 3 of 4 NPCs never spawned correctly after a reload, all traced back
    // to this one exception. Computed lazily on first real use instead,
    // safely inside Awake()/Update() territory.
    private static int stepCheckMask = -1;

    // Non-alloc (2026-08-21) -- SphereCastAll allocates a fresh hit array
    // every call, and this runs every frame for every moving Gathering NPC.
    // Normally cheap, but a dense cluster of colliders (a log-pile box
    // explosion, live-confirmed) returns a large array on every one of
    // those per-frame calls -- repeated large allocations like that are a
    // classic Unity GC-stall trigger, and are the most likely cause of a
    // real live multi-second freeze affecting every NPC at once, not just
    // the one standing in the clutter. A shared static buffer avoids the
    // allocation regardless of how many colliders are nearby. 16 is well
    // above what a single chest-height sweep should ever realistically
    // need to inspect; SphereCastNonAlloc silently caps at the buffer size
    // rather than erroring if a scene somehow exceeds it.
    private static readonly RaycastHit[] stepCheckBuffer = new RaycastHit[16];

    private bool StepIsBlocked(Vector3 from, Vector3 to, GameObject ignoreTarget)
    {
        if (stepCheckMask == -1) stepCheckMask = ~LayerMask.GetMask("Ground");

        Vector3 delta = to - from;
        delta.y = 0f;
        float dist = delta.magnitude;
        if (dist < 0.0001f) return false;

        Vector3 origin = from + Vector3.up * 0.9f;
        int hitCount = Physics.SphereCastNonAlloc(origin, StepCheckRadius, delta.normalized, stepCheckBuffer, dist, stepCheckMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitCount; i++)
        {
            var hit = stepCheckBuffer[i];
            if (hit.collider.transform.IsChildOf(transform)) continue;
            if (ignoreTarget != null && hit.collider.transform.IsChildOf(ignoreTarget.transform)) continue;
            return true;
        }
        return false;
    }
}
