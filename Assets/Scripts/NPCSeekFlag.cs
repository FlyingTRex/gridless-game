using Mirror;
using UnityEngine;
using UnityEngine.AI;

// Walk-toward-a-fixed-point behavior for an NPC freshly spawned by
// VillageFlagSpawner (2026-08-16, VILLAGE_FLAG_PLANNING.md section 3) --
// reuses NPCWander's move/ground-sample/face plumbing via a small local
// copy, same "aimed at a fixed destination instead of away from a threat"
// reuse NPCFlee.cs already did for its own move-away behavior.
//
// Lives permanently on the hireable NPC prefab (same RequireComponent-
// chain-always-present convention NPCCrafting/NPCTraining already use) --
// every pre-placed hire already in the world also carries this component,
// but Update() no-ops immediately for them since BeginSeeking is only ever
// called by VillageFlagSpawner right after a fresh Instantiate.
[RequireComponent(typeof(NPCWander))]
[RequireComponent(typeof(NPCHiring))]
// Multiplayer Phase 3 item 4 ("NPCs move server-side"), 2026-08-23:
// converted to NetworkBehaviour, plus an isServer guard on Update() --
// see NPCGathering.cs's own header comment for the full reasoning,
// identical here.
public class NPCSeekFlag : NetworkBehaviour
{
    // Wider than the usual 2m "close enough to interact" range
    // (AnvilSurface/FurnaceSurface/DeskSurface) -- Ben's call, 2026-08-16:
    // this NPC is just idling near the Flag waiting to be hired, not
    // interacting with it, so it doesn't need to walk all the way in.
    private const float ArriveRange = 5f;

    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float obstacleCheckDistance = 1.5f;

    private NPCWander wander;
    private NPCHiring hiring;

    // Shared with NPCGathering/NPCCrafting/NPCTraining/NPCGuarding's own
    // MoveToward via NPCMovement.cs (2026-08-19) -- this component's own
    // widening-search deflection (2026-08-16) is now the shared
    // implementation those 4 were missing, and this pulls the stuck-shove
    // recovery the other 4 gained back onto this component too, for one
    // consistent behavior everywhere.
    private readonly NPCMovement.StuckTracker stuckTracker = new();

    // NavMesh + physics safety net (2026-08-21) -- this component was
    // named in NPC_NAVIGATION_PLANNING.md as one of the 5 movers needing
    // conversion, but never actually got it (confirmed live: "Cora is
    // stuck" -- a freshly-spawned NPC frozen mid-walk-in). Same treatment
    // NPCGathering already has: NavMeshAgent supplies routing direction
    // only (updatePosition/updateRotation off, this method still owns
    // transform.position), a physics sweep rejects any step that would
    // cross real collider geometry regardless of what the navmesh says,
    // and the old NPCMovement fallback (with its own stuck-bump escape)
    // only kicks in if there's no navmesh to route on at all.
    private NavMeshAgent agent;

    // Progress watchdog + escape bump (2026-08-21) -- see NPCGathering's
    // own comment for the full reasoning (found live: a give-up-on-target
    // response alone isn't enough when the mover's own *position* is
    // trapped, not the target). No retarget/avoid-list needed here --
    // there's only ever one destination (the Flag), so escaping the
    // position and letting the normal per-tick MoveToward call resume is
    // the whole fix, same "physical escape hatch, the caller re-evaluates
    // on its own cadence" shape NPCMovement.cs's own header describes.
    private const float ProgressEpsilon = 0.05f;
    private const float BlockedGiveUpSeconds = 5f;
    private float lastProgressDistance = float.MaxValue;
    private float noProgressSeconds;

    private Transform targetFlag;
    private float stickAroundSecondsRemaining;
    private bool hasArrived;

    private void Awake()
    {
        wander = GetComponent<NPCWander>();
        hiring = GetComponent<NPCHiring>();

        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.speed = moveSpeed;
        }
    }

    // Called once, immediately after Instantiate, by VillageFlagSpawner.
    public void BeginSeeking(Transform flag, float stickAroundSeconds)
    {
        targetFlag = flag;
        stickAroundSecondsRemaining = stickAroundSeconds;
        hasArrived = false;
        wander.SetPaused(true);
    }

    // Logged unconditionally, not gated behind a DebugEnabled checkbox
    // like the other movers -- an unhired NPC has no way to reach that
    // checkbox from NPCHiringScreen (found live, 2026-08-21, Ben: "can't
    // debug the ones I didn't hire"), and this state is short-lived by
    // design (a walk-in that should finish within minutes, normally only
    // 1-2 active at once), so the volume risk that motivated the
    // checkbox elsewhere doesn't really apply here.
    private const float DebugLogInterval = 1f;
    private float nextDebugLogTime;

    // Save/restore (2026-08-21) -- this component's state was pure
    // runtime-only until now, never captured by SaveManager. Found live
    // ("Gideon still around" well past his own window, confirmed via
    // saveId diff it wasn't a database issue): a save/reload recreates an
    // unhired NPC via a fresh Instantiate (RestoreNpcs), but BeginSeeking
    // is only ever called once, by VillageFlagSpawner at the moment of an
    // original spawn -- the recreated instance's targetFlag stays null
    // forever, and Update()'s very first line (`if (targetFlag == null)
    // return`) silently no-ops the whole component permanently. No
    // despawn, no seeking, just an inert NPC indistinguishable from a
    // healthy one until you check the numbers. Retroactively explains
    // Cora/Odette's identical zero-telemetry earlier the same session.
    public bool IsActivelySeeking => targetFlag != null;
    public Transform TargetFlag => targetFlag;
    public bool HasArrived => hasArrived;
    public float StickAroundSecondsRemaining => stickAroundSecondsRemaining;

    // Restores state directly rather than going through BeginSeeking --
    // that method unconditionally resets hasArrived to false and pauses
    // wander, which would wrongly re-freeze an NPC that had already
    // arrived and was standing around normally before the reload.
    public void RestoreSeekState(Transform flag, bool arrived, float stickAroundRemaining)
    {
        targetFlag = flag;
        hasArrived = arrived;
        stickAroundSecondsRemaining = stickAroundRemaining;
        wander.SetPaused(!arrived);
    }

    private void Update()
    {
        if (!isServer) return;
        if (targetFlag == null) return;

        if (hiring.IsHired)
        {
            // Made it and got hired -- ordinary hireable-NPC behavior owns
            // this GameObject permanently from here on, same as any other
            // pre-placed hire. Disabling (rather than clearing state) means
            // this component never runs its own logic again, so a later
            // Fire doesn't somehow resurrect the despawn countdown.
            enabled = false;
            return;
        }

        if (!hasArrived)
        {
            float distance = Vector3.Distance(transform.position, targetFlag.position);
            if (distance > ArriveRange)
            {
                if (Time.time >= nextDebugLogTime)
                {
                    nextDebugLogTime = Time.time + DebugLogInterval;
                    string agentInfo = agent == null ? "none (fallback movement)"
                        : !agent.isOnNavMesh ? "OFF NAVMESH (fallback movement)"
                        : $"hasPath={agent.hasPath} status={agent.pathStatus} remaining={agent.remainingDistance:F1} desiredVel={agent.desiredVelocity.magnitude:F1}";
                    var dialogue = GetComponent<NPCDialogue>();
                    DebugLog.Write(dialogue != null ? dialogue.DisplayName : name,
                        $"seeking {targetFlag.name} dist={distance:F1} noProgress={noProgressSeconds:F1}s | agent: {agentInfo} pos=({transform.position.x:F1},{transform.position.z:F1})");
                }
                MoveToward(targetFlag.position, targetFlag.gameObject);
            }
            else
            {
                // Arrived, not yet hired -- behaves exactly like any other
                // pre-placed hireable NPC standing in the world (Ben's own
                // framing), so ordinary wandering resumes instead of
                // freezing in place at the Flag.
                hasArrived = true;
                wander.SetPaused(false);
                Debug.Log($"[VillageFlag] NPC arrived at {targetFlag.name}, waiting to be hired.");
            }
        }

        // Always ticks regardless of arrival status (2026-08-21, found
        // live -- "Cora is stuck" and never despawned despite the window
        // long since closing). This used to be gated behind the walking
        // branch's own early `return` above, so a spawn that got stuck
        // before ever reaching the Flag never started this countdown at
        // all and would wait forever instead of timing out on schedule.
        //
        // Not decided in the design doc whether an unhired NPC despawns or
        // rejoins the general world population once the window closes --
        // despawn is the simpler of the two and needs no new system, so
        // it's the pick here; worth revisiting if that gap ever bites.
        stickAroundSecondsRemaining -= Time.deltaTime;
        if (stickAroundSecondsRemaining <= 0f)
        {
            Debug.Log($"[VillageFlag] NPC at {targetFlag.name} timed out unhired, despawning.");
            Destroy(gameObject);
        }
    }

    // Same straight-line movement as NPCGathering/NPCCrafting/NPCTraining/
    // NPCGuarding's own MoveToward -- obstacle deflection/stuck-recovery is
    // shared via NPCMovement.cs (2026-08-19); this component was the
    // original source of that widening-search algorithm before it got
    // pulled out into the shared helper.
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
    // comment for the full reasoning. Non-alloc from the start here since
    // that GC-allocation lesson was already learned by the time this was
    // written. ignoreTarget added (2026-08-21) -- unlike every other
    // mover, this one never exempted its own destination's collider from
    // the sweep. Not confirmed as the actual cause of a live "stops ~10m
    // out, no visible obstacle" report (the Flag's own colliders are
    // small enough that this shouldn't matter in practice), but it's a
    // real inconsistency with the other 4 movers worth closing regardless.
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
