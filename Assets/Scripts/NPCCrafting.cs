using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

// The autonomous bench-crafting loop (2026-08-16, NPC_JOB_GENERALIZATION_
// PLANNING.md section 7 -- the "Chunk 1" of the Settlement Growth Loop
// artifact). Sibling to NPCGathering, not an extension of it -- crafting
// has no "target in the world to walk up to and hit repeatedly" the way
// gathering does; the action is "run a CraftingRecipe," not "harvest a
// node." It does share gathering's "walk to a required surface first"
// shape for any recipe with requiresAnvilSurface/requiresFurnace set.
//
// Recipe selection is a player-curated queue, mirroring Furnace.
// recipeQueue/ToggleQueue member-for-member (Ben's call, AskUserQuestion,
// 2026-08-16) -- the NPC never auto-crafts anything it merely qualifies
// for, only what's explicitly queued. Crafting itself is deterministic --
// no CraftOutcomeRoll, matching SmeltableItem/CookableItem's existing
// unattended-automation precedent. Materials/output flow through two
// player-assigned StorageBoxes, not the NPC's own cargo -- there's no
// physical "walk it back and deposit" step the way gathering has, since
// nothing is ever carried.
[RequireComponent(typeof(NPCWander))]
[RequireComponent(typeof(NPCJob))]
[RequireComponent(typeof(NPCSkills))]
// Multiplayer Phase 3 item 4 ("NPCs move server-side"), 2026-08-23:
// converted to NetworkBehaviour, plus an isServer guard on Update() --
// see NPCGathering.cs's own header comment for the full reasoning,
// identical here.
public class NPCCrafting : NetworkBehaviour
{
    // Mirrors Furnace.MaxQueueSize -- no number was specified for this
    // queue specifically, but the design explicitly models it on Furnace's
    // own queue shape, so its depth cap carries over too rather than
    // inventing an unrelated number.
    public const int MaxQueueSize = 4;

    [SerializeField] private float craftDuration = 3f;
    [SerializeField] private float searchRadius = 50f;
    [SerializeField] private float moveSpeed = 1.5f;

    // Same short forward-raycast deflection NPCGathering uses -- there's no
    // NavMesh in this project, so this is "don't push straight through an
    // obstacle," not real pathfinding.
    [SerializeField] private float obstacleCheckDistance = 1.5f;

    // Matches PlayerCrafting.AnvilSurfaceRange/FurnaceSurfaceRange -- same
    // "within 2m" the player's own requiresAnvilSurface/requiresFurnace
    // recipes already use.
    private const float SurfaceRange = 2f;

    private NPCWander wander;
    private NPCJob job;
    private NPCSkills skills;
    private NPCHiring hiring;

    // Shared with NPCGathering/NPCTraining/NPCGuarding/NPCSeekFlag's own
    // MoveToward via NPCMovement.cs (2026-08-19) -- see that file's header.
    private readonly NPCMovement.StuckTracker stuckTracker = new();

    // NavMesh + physics safety net (2026-08-21) -- named in
    // NPC_NAVIGATION_PLANNING.md as one of the 5 movers needing this, never
    // actually converted until now (only NPCGathering had been). Same
    // shape as NPCGathering/NPCSeekFlag: NavMeshAgent supplies routing
    // direction only, this method still owns transform.position, and a
    // physics sweep rejects any step crossing real collider geometry
    // regardless of what the navmesh says.
    private NavMeshAgent agent;

    // Progress watchdog + escape bump (2026-08-21) -- see NPCGathering's
    // own comment for the full reasoning. No retarget bookkeeping needed
    // here -- physically escaping and letting this component's own
    // per-tick logic re-evaluate the walk-to-surface target next Update()
    // is the whole fix.
    private const float ProgressEpsilon = 0.05f;
    private const float BlockedGiveUpSeconds = 5f;
    private float lastProgressDistance = float.MaxValue;
    private float noProgressSeconds;

    private readonly List<CraftingRecipe> recipeQueue = new List<CraftingRecipe>();
    private StorageBox materialsSourceBox;
    private StorageBox outputBox;

    private CraftingRecipe activeRecipe;
    private float craftTimer;
    private bool isPaused;
    private Component targetSurface;

    // See NPCGathering.wasActive's comment for the full story -- this
    // component's own `!ready` branch used to call wander.SetPaused(false)
    // unconditionally every idle frame (i.e. every frame for any NPC whose
    // job isn't Crafting), racing against whichever job component actually
    // is active for real ownership of the pause state. Only release on a
    // genuine active-to-inactive transition.
    private bool wasActive;

    public void SetPaused(bool paused) => isPaused = paused;

    public IReadOnlyList<CraftingRecipe> RecipeQueue => recipeQueue;
    public CraftingRecipe ActiveRecipe => activeRecipe;
    public float CraftSecondsElapsed => craftTimer;
    public float CraftDurationSeconds => craftDuration;
    public StorageBox MaterialsSourceBox => materialsSourceBox;
    public StorageBox OutputBox => outputBox;

    // Read by future NPCAnimatorDriver wiring (not built this pass) --
    // true across the same window craftTimer counts, i.e. once in position
    // and not still walking to a surface, same shape NPCGathering.
    // IsActingOnTarget already establishes.
    public bool IsActingOnTarget => activeRecipe != null && targetSurface == null && craftTimer > 0f;

    private void Awake()
    {
        wander = GetComponent<NPCWander>();
        job = GetComponent<NPCJob>();
        skills = GetComponent<NPCSkills>();
        // Optional, same convention NPCGathering keeps for it -- an
        // unpaid NPC just holds still until Pay clears it.
        hiring = GetComponent<NPCHiring>();

        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.speed = moveSpeed;
        }
    }

    public bool IsQueued(CraftingRecipe recipe) => recipe != null && recipeQueue.Contains(recipe);

    public bool ToggleQueue(CraftingRecipe recipe)
    {
        if (recipe == null) return false;

        int idx = recipeQueue.IndexOf(recipe);
        if (idx >= 0)
        {
            recipeQueue.RemoveAt(idx);
            if (activeRecipe == recipe)
            {
                // Let an already-in-progress craft finish -- its
                // ingredients haven't been touched yet either way (they're
                // only consumed on completion, see FinishActiveRecipe), so
                // there's nothing to refund; it just won't be reselected.
            }
            return false;
        }

        if (recipeQueue.Count >= MaxQueueSize) return false;
        recipeQueue.Add(recipe);
        return true;
    }

    public void SetMaterialsSourceBox(StorageBox box) => materialsSourceBox = box;
    public void SetOutputBox(StorageBox box) => outputBox = box;

    // Gates both queue-walking (FindActiveRecipe below) and the UI's
    // per-recipe satisfiability indicator -- same four-way check as
    // NPC_JOB_GENERALIZATION_PLANNING.md section 7.4.
    public bool IsSatisfiable(CraftingRecipe recipe)
    {
        if (recipe == null || recipe.outputItem == null) return false;
        if (materialsSourceBox == null || outputBox == null) return false;
        if (recipe.requiresCanteenWater) return false; // NPCs have no Canteen concept.

        if (recipe.ingredients != null)
        {
            foreach (var ingredient in recipe.ingredients)
            {
                if (ingredient == null || ingredient.item == null) continue;
                if (materialsSourceBox.Inventory.GetCount(ingredient.item) < ingredient.count) return false;
            }
        }

        if (recipe.requiredTools != null && recipe.requiredTools.Length > 0 && !job.HasAnyTool(recipe.requiredTools))
            return false;

        if (recipe.trainedSkill != null)
        {
            int required = CraftTierScale.SkillRequirement(recipe.outputItem.tier);
            // || job.HasGrantedRecipe mirrors PlayerCrafting.HasRequiredSkill's
            // own bookGrantedRecipes exception -- a trained-via-Desk NPC can
            // queue-execute a recipe it hasn't actually leveled into yet
            // (NPC_TRAINING_PLANNING.md section 5).
            if (skills.GetLevel(recipe.trainedSkill) < required && !job.HasGrantedRecipe(recipe)) return false;
        }

        if (!outputBox.Inventory.HasSpaceFor(recipe.outputItem, recipe.outputCount)) return false;
        if (recipe.bonusItem != null && !outputBox.Inventory.HasSpaceFor(recipe.bonusItem, recipe.bonusCount)) return false;

        return true;
    }

    private void Update()
    {
        if (!isServer) return;
        if (isPaused) return;

        bool ready = job.IsReady
            && job.AssignedJob.kind == NPCJobDefinition.JobKind.Crafting
            && (hiring == null || !hiring.IsWaitingForPayment);
        if (!ready)
        {
            activeRecipe = null;
            targetSurface = null;
            craftTimer = 0f;
            if (wasActive) wander.SetPaused(false);
            wasActive = false;
            return;
        }
        wasActive = true;

        if (activeRecipe == null || !IsSatisfiable(activeRecipe))
        {
            activeRecipe = FindActiveRecipe();
            craftTimer = 0f;
            targetSurface = null;
        }

        if (activeRecipe == null)
        {
            wander.SetPaused(false);
            return;
        }

        bool needsSurface = activeRecipe.requiresFurnace || activeRecipe.requiresAnvilSurface;
        if (needsSurface)
        {
            if (targetSurface == null || !WithinSurfaceRange(targetSurface))
                targetSurface = FindNearestSurface(activeRecipe);

            if (targetSurface == null)
            {
                // Nothing satisfiable to walk to right now -- try a
                // different queued recipe next tick rather than stalling
                // on this one forever.
                activeRecipe = null;
                wander.SetPaused(false);
                return;
            }

            if (!WithinSurfaceRange(targetSurface))
            {
                wander.SetPaused(true);
                MoveToward(targetSurface.transform.position, targetSurface.gameObject);
                craftTimer = 0f;
                return;
            }
        }

        wander.SetPaused(true);
        Vector3 facePos = needsSurface ? targetSurface.transform.position : transform.position + transform.forward;
        wander.FaceToward(facePos);

        craftTimer += Time.deltaTime;
        if (craftTimer < craftDuration) return;

        FinishActiveRecipe();
        craftTimer = 0f;
        targetSurface = null;
    }

    // Picks the first queued recipe (in queue order -- no round-robin,
    // unlike Furnace; nothing in the design calls for fairness here) that's
    // currently satisfiable.
    private CraftingRecipe FindActiveRecipe()
    {
        foreach (var recipe in recipeQueue)
            if (IsSatisfiable(recipe)) return recipe;
        return null;
    }

    private bool WithinSurfaceRange(Component surface) =>
        Vector3.Distance(transform.position, surface.transform.position) <= SurfaceRange;

    private Component FindNearestSurface(CraftingRecipe recipe)
    {
        if (recipe.requiresFurnace) return FindNearestWithinRadius<FurnaceSurface>();
        if (recipe.requiresAnvilSurface) return FindNearestWithinRadius<AnvilSurface>();
        return null;
    }

    private T FindNearestWithinRadius<T>() where T : Component
    {
        T best = null;
        float bestDist = searchRadius;

        foreach (var candidate in FindObjectsByType<T>(FindObjectsSortMode.None))
        {
            float dist = Vector3.Distance(transform.position, candidate.transform.position);
            if (dist >= bestDist) continue;
            bestDist = dist;
            best = candidate;
        }

        return best;
    }

    // Re-verifies satisfiability right before committing -- state can
    // change out from under a queued recipe while walking to a surface
    // (another system draining the materials box, the output box filling
    // up). A recipe that's no longer satisfiable simply doesn't fire this
    // tick, no partial consumption -- same determinism guarantee as the
    // rest of this loop.
    private void FinishActiveRecipe()
    {
        var recipe = activeRecipe;
        if (!IsSatisfiable(recipe)) return;

        if (recipe.ingredients != null)
            foreach (var ingredient in recipe.ingredients)
                if (ingredient != null && ingredient.item != null)
                    materialsSourceBox.Inventory.RemoveItem(ingredient.item, ingredient.count);

        outputBox.Inventory.AddItem(recipe.outputItem, recipe.outputCount);
        if (recipe.bonusItem != null)
            outputBox.Inventory.AddItem(recipe.bonusItem, recipe.bonusCount);

        skills.GainExperience(job.AssignedJob.family, recipe.skillGain);
        activeRecipe = null;
    }

    // Same straight-line movement as NPCGathering's own MoveToward --
    // obstacle deflection/stuck-recovery is shared via NPCMovement.cs
    // (2026-08-19).
    private void MoveToward(Vector3 targetPos, GameObject ignoreTarget)
    {
        Vector3 toTarget = targetPos - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        Vector3 moveDir;

        if (agent != null && agent.isOnNavMesh)
        {
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

        if (!StepIsBlocked(transform.position, newPos, ignoreTarget))
            transform.position = newPos;

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
                Vector3 escapeOrigin = transform.position + Vector3.up * 0.5f;
                NPCMovement.EscapeBump(escapeOrigin, -toTarget.normalized, ignoreTarget, transform);
                lastProgressDistance = float.MaxValue;
                noProgressSeconds = 0f;
                return;
            }
        }

        if (agent != null && agent.isOnNavMesh) agent.nextPosition = transform.position;
        wander.FaceToward(transform.position + moveDir);
    }

    // Same shape as NPCGathering.StepIsBlocked -- see that method's own
    // comment for the full reasoning.
    private const float StepCheckRadius = 0.3f;
    private static int stepCheckMask = -1;
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
