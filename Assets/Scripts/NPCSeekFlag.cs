using UnityEngine;

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
public class NPCSeekFlag : MonoBehaviour
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

    private Transform targetFlag;
    private float stickAroundSecondsRemaining;
    private bool hasArrived;

    private void Awake()
    {
        wander = GetComponent<NPCWander>();
        hiring = GetComponent<NPCHiring>();
    }

    // Called once, immediately after Instantiate, by VillageFlagSpawner.
    public void BeginSeeking(Transform flag, float stickAroundSeconds)
    {
        targetFlag = flag;
        stickAroundSecondsRemaining = stickAroundSeconds;
        hasArrived = false;
        wander.SetPaused(true);
    }

    private void Update()
    {
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
                MoveToward(targetFlag.position);
                return;
            }

            // Arrived, not yet hired -- behaves exactly like any other
            // pre-placed hireable NPC standing in the world (Ben's own
            // framing), so ordinary wandering resumes instead of freezing
            // in place at the Flag.
            hasArrived = true;
            wander.SetPaused(false);
            Debug.Log($"[VillageFlag] NPC arrived at {targetFlag.name}, waiting to be hired.");
        }

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

    // Same straight-line-plus-deflection movement as NPCGathering/
    // NPCCrafting/NPCTraining's own MoveToward -- duplicated rather than
    // shared, same reasoning as those.
    private void MoveToward(Vector3 targetPos)
    {
        Vector3 toTarget = targetPos - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        Vector3 desired = toTarget.normalized;
        Vector3 moveDir = FindClearDirection(desired);

        Vector3 flatTarget = transform.position + moveDir * moveSpeed * Time.deltaTime;
        Vector3 newPos = new Vector3(flatTarget.x, transform.position.y, flatTarget.z);
        newPos.y = GroundHeight.Sample(newPos, transform.position.y);
        transform.position = newPos;
        wander.FaceToward(transform.position + moveDir);
    }

    // Steers around an obstacle by widening the search angle left/right of
    // the desired direction until a clear ray is found -- replaces the old
    // single normal-based deflection (2026-08-16), which could point
    // straight into a second obstacle at a corner and stall the NPC there
    // permanently (observed live: spawned near the timber-frame building,
    // never closed the distance to the Flag).
    private Vector3 FindClearDirection(Vector3 desired)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (!Physics.Raycast(origin, desired, obstacleCheckDistance, ~0, QueryTriggerInteraction.Ignore))
            return desired;

        for (float angle = 15f; angle <= 90f; angle += 15f)
        {
            Vector3 right = Quaternion.Euler(0f, angle, 0f) * desired;
            if (!Physics.Raycast(origin, right, obstacleCheckDistance, ~0, QueryTriggerInteraction.Ignore))
                return right;

            Vector3 left = Quaternion.Euler(0f, -angle, 0f) * desired;
            if (!Physics.Raycast(origin, left, obstacleCheckDistance, ~0, QueryTriggerInteraction.Ignore))
                return left;
        }

        // Every tested direction blocked (fully surrounded) -- reverse
        // rather than stand still forever.
        return -desired;
    }
}
