using Mirror;
using UnityEngine;

// Multiplayer, 2026-08-23 -- found during the same NPCWander-adjacent
// audit: real movement, missed the first pass since it wasn't part of
// the named "5". Converted to NetworkBehaviour, isServer guard on
// Update() below.
//
// Fame's negative-Fame output effect — see FAME_PLANNING.md (2026-08-14):
// "if you have negative fame, npc's will run away from you. you are a
// potential threat." Applies to every NPC, hired or not — a hired NPC's
// job (NPCGathering, if present) pauses for the duration, same as
// NPCWander's own dialogue-pause mechanism, and resumes once the player
// leaves range. Doesn't touch hire state at all.
//
// Reuses NPCWander's move/ground-sample/face plumbing via a small local
// copy rather than modifying NPCWander itself — fleeing needs a
// materially different target-picking rule (away from the player, not
// random), not just a parameter tweak.
[RequireComponent(typeof(NPCWander))]
public class NPCFlee : NetworkBehaviour
{
    private const float DetectionRange = 10f;
    private const float FleeDistance = 8f;
    private const float SpeedMultiplier = 2f;

    [SerializeField] private float baseMoveSpeed = 1.2f;

    private NPCWander wander;
    private NPCGathering gathering;
    private PlayerFame playerFame;
    private bool isFleeing;
    private Vector3 fleeTarget;

    private void Awake()
    {
        wander = GetComponent<NPCWander>();
        gathering = GetComponent<NPCGathering>();
        playerFame = FindFirstObjectByType<PlayerFame>();
    }

    private void Update()
    {
        if (!isServer) return;
        if (playerFame == null) return;

        bool shouldFlee = playerFame.Fame < 0f &&
            Vector3.Distance(transform.position, playerFame.transform.position) < DetectionRange;

        if (shouldFlee && !isFleeing)
        {
            isFleeing = true;
            wander.SetPaused(true);
            gathering?.SetPaused(true);
        }
        else if (!shouldFlee && isFleeing)
        {
            isFleeing = false;
            wander.SetPaused(false);
            gathering?.SetPaused(false);
            return;
        }

        if (!isFleeing) return;

        Vector3 away = transform.position - playerFame.transform.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.001f) away = transform.forward;
        away.Normalize();
        fleeTarget = transform.position + away * FleeDistance;

        Vector3 flatTarget = new Vector3(fleeTarget.x, transform.position.y, fleeTarget.z);
        Vector3 newPos = Vector3.MoveTowards(transform.position, flatTarget, baseMoveSpeed * SpeedMultiplier * Time.deltaTime);
        newPos.y = GroundHeight.Sample(newPos, transform.position.y);
        transform.position = newPos;
        wander.FaceToward(flatTarget);
    }
}
