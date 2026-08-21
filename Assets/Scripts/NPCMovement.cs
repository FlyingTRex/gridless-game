using UnityEngine;

// Shared obstacle-avoidance/stuck-recovery helper for every NPC script that
// does its own straight-line-plus-deflection movement (2026-08-19,
// consolidating a weak single-deflection pattern that was duplicated,
// unfixed, across NPCGathering/NPCCrafting/NPCTraining/NPCGuarding -- see
// BUGS_AND_ENHANCEMENTS.md, confirmed live 3x+ as a Boulder/obstacle full-
// freeze). Plain static class, same "static class, read by whoever needs
// it" shape as GroundHeight.cs -- each caller keeps its own MoveToward
// (ground-sampling/facing/moveSpeed all stay per-script) and just calls
// FindClearDirection instead of its own deflection block.
public static class NPCMovement
{
    // How often to snapshot position for stuck-detection, how far counts as
    // "made progress," and how many consecutive slow snapshots before
    // declaring stuck. ~6 seconds of near-zero net movement is long enough
    // to not misfire on legitimately careful/slow movement (e.g. a Guard
    // orbiting a small patrol radius) but short enough to matter.
    private const float StuckCheckInterval = 2f;
    private const float StuckMinProgress = 0.3f;
    private const int StuckIntervalsBeforeFlag = 3;

    // Widens the search angle left/right of the desired direction until a
    // clear ray is found, instead of a single normal-based deflection that
    // can point straight into a second obstacle at a corner and stall the
    // NPC there permanently (the original NPCSeekFlag fix, 2026-08-16, now
    // shared by every straight-line mover). ignoreTarget lets a mover's own
    // destination object (a ResourceNode it's walking up to, a threat it's
    // chasing) not count as "blocked" once close enough to register a hit.
    public static Vector3 FindClearDirection(Vector3 origin, Vector3 desired, float checkDistance, GameObject ignoreTarget)
    {
        if (!Blocked(origin, desired, checkDistance, ignoreTarget))
            return desired;

        for (float angle = 15f; angle <= 90f; angle += 15f)
        {
            Vector3 right = Quaternion.Euler(0f, angle, 0f) * desired;
            if (!Blocked(origin, right, checkDistance, ignoreTarget))
                return right;

            Vector3 left = Quaternion.Euler(0f, -angle, 0f) * desired;
            if (!Blocked(origin, left, checkDistance, ignoreTarget))
                return left;
        }

        // Every tested direction blocked (fully surrounded) -- reverse
        // rather than stand still forever.
        return -desired;
    }

    private static bool Blocked(Vector3 origin, Vector3 dir, float dist, GameObject ignoreTarget)
    {
        if (!Physics.Raycast(origin, dir, out var hit, dist, ~0, QueryTriggerInteraction.Ignore))
            return false;
        return ignoreTarget == null || hit.collider.gameObject != ignoreTarget;
    }

    // Per-mover stuck-recovery state -- plain class (not a MonoBehaviour),
    // one instance held as a private field on each calling script. Feeds it
    // the mover's position every MoveToward call; once it's made
    // near-zero net progress for StuckIntervalsBeforeFlag consecutive
    // checks, the next FindClearDirection-via-tracker call returns a hard
    // reverse instead of the normal widening search -- a deliberate shove
    // out of whatever pocket it's wedged into -- then resets so normal
    // search resumes. This is a physical escape hatch, not a per-caller
    // "abandon current target and re-plan" policy -- each of the 5 job
    // components already re-evaluates its target/task on its own cadence,
    // so freeing the NPC physically is enough for that to happen naturally.
    public class StuckTracker
    {
        private Vector3 lastCheckPos;
        private float timer;
        private int slowIntervals;
        private bool initialized;

        // Call once per MoveToward, before deciding the deflection
        // direction. Returns true if the mover should be shoved free this
        // call instead of doing a normal obstacle probe.
        public bool Tick(Vector3 currentPos, float deltaTime)
        {
            if (!initialized)
            {
                lastCheckPos = currentPos;
                initialized = true;
                return false;
            }

            timer += deltaTime;
            if (timer < StuckCheckInterval)
                return slowIntervals >= StuckIntervalsBeforeFlag;

            float moved = Vector3.Distance(currentPos, lastCheckPos);
            slowIntervals = moved < StuckMinProgress ? slowIntervals + 1 : 0;
            lastCheckPos = currentPos;
            timer = 0f;

            bool stuck = slowIntervals >= StuckIntervalsBeforeFlag;
            if (stuck)
                slowIntervals = 0; // one shove, then start re-counting fresh
            return stuck;
        }
    }

    // How far to physically teleport a stuck mover, once StuckTracker
    // declares it wedged -- Ben's fix, 2026-08-20, found live: a Miner
    // stuck at a wall/floor corner never actually escaped. The original
    // "shove" below just reversed the desired *direction* for that one
    // frame, which the caller then walked along at its own normal
    // moveSpeed -- at ~1.5-2.2 units/sec, that's a fraction of a meter
    // per frame, too weak to clear a real corner wedge where both the
    // forward and reverse directions can be blocked by different
    // colliders. A flat instant displacement is strong enough to clear
    // that regardless of what's immediately adjacent in either direction.
    private const float StuckBumpDistance = 4f;

    // Convenience wrapper: FindClearDirection, but hard-teleported free
    // instead of probed if the tracker says this mover has stalled.
    // Needs the actual Transform (not just its position) because the
    // escape is a real position mutation, not just a returned direction
    // for the caller to walk along -- mover.position is updated in place,
    // then re-sampled against the ground the same way every caller's own
    // MoveToward already does, so the teleport doesn't leave the mover
    // floating or embedded at the new spot.
    //
    // Validates the bump path before committing (2026-08-20, found live:
    // an NPC walked through a wall, possibly this exact fix -- the first
    // version teleported blind, with no raycast check that the 4m escape
    // path was actually clear, which a true instant displacement can
    // exploit in a way the old per-frame reverse-nudge never could since
    // ordinary collision still stopped that). Reuses the same widening-
    // angle search FindClearDirection already does for normal deflection,
    // just checked against the full StuckBumpDistance instead of the
    // caller's short obstacleCheckDistance, and clamps to whatever
    // distance actually is clear rather than assuming the full 4m is
    // safe.
    public static Vector3 FindClearDirection(Vector3 origin, Vector3 desired, float checkDistance, GameObject ignoreTarget, StuckTracker tracker, float deltaTime, Transform mover)
    {
        if (tracker != null && tracker.Tick(origin, deltaTime))
        {
            Vector3 escapeDir = FindClearBumpDirection(origin, -desired, ignoreTarget);
            float clearDist = ClearDistance(origin, escapeDir, StuckBumpDistance, ignoreTarget);

            Vector3 bumpPos = mover.position + escapeDir * clearDist;
            bumpPos.y = GroundHeight.Sample(bumpPos, mover.position.y);
            mover.position = bumpPos;
            return escapeDir;
        }

        return FindClearDirection(origin, desired, checkDistance, ignoreTarget);
    }

    // Widening-angle search (same shape as the public FindClearDirection
    // above), but checked against the full StuckBumpDistance rather than
    // a short obstacle-probe distance -- a direction that's merely clear
    // for 1.5m isn't good enough to trust for a 4m teleport.
    private static Vector3 FindClearBumpDirection(Vector3 origin, Vector3 desired, GameObject ignoreTarget)
    {
        if (!Blocked(origin, desired, StuckBumpDistance, ignoreTarget))
            return desired;

        for (float angle = 15f; angle <= 90f; angle += 15f)
        {
            Vector3 right = Quaternion.Euler(0f, angle, 0f) * desired;
            if (!Blocked(origin, right, StuckBumpDistance, ignoreTarget))
                return right;

            Vector3 left = Quaternion.Euler(0f, -angle, 0f) * desired;
            if (!Blocked(origin, left, StuckBumpDistance, ignoreTarget))
                return left;
        }

        // Every direction blocked within StuckBumpDistance -- fall through
        // to ClearDistance's own clamping on the original desired
        // direction rather than teleporting blind.
        return desired;
    }

    // How far along dir is actually clear before hitting something, up to
    // maxDist -- a small safety margin keeps the mover from landing flush
    // against whatever it hit instead of visibly clipping into it.
    private const float ClearDistanceMargin = 0.3f;

    private static float ClearDistance(Vector3 origin, Vector3 dir, float maxDist, GameObject ignoreTarget)
    {
        if (!Physics.Raycast(origin, dir, out var hit, maxDist, ~0, QueryTriggerInteraction.Ignore))
            return maxDist;
        if (ignoreTarget != null && hit.collider.gameObject == ignoreTarget)
            return maxDist;

        return Mathf.Max(0f, hit.distance - ClearDistanceMargin);
    }
}
