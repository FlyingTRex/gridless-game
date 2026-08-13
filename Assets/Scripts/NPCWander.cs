using UnityEngine;

// First step toward Hireable autonomous NPCs (2026-08-10, Ben's call: place
// the SD Macross Factory Worker model with idle wander, no interaction yet)
// — deliberately minimal, since nothing about NPC AI/hiring is designed
// beyond the name. Same flat-ground Vector3.MoveTowards approach as
// HostileCreature (no NavMesh in the project), picking a random point
// within wanderRadius of spawn, walking to it, pausing, repeating.
public class NPCWander : MonoBehaviour
{
    [SerializeField] private float wanderRadius = 6f;
    [SerializeField] private float moveSpeed = 1.2f;
    [SerializeField] private float pauseDurationMin = 2f;
    [SerializeField] private float pauseDurationMax = 5f;

    // NPCFactoryWorker.glb's authored forward axis doesn't line up with
    // Unity's LookRotation convention (local +Z) -- confirmed live
    // (2026-08-10, Ben: "our npc is moving the right" i.e. crab-walking
    // sideways instead of facing its travel direction). +90 corrects it;
    // if a future model on this same component walks backwards instead,
    // try -90 or 180 here rather than touching FaceToward's math.
    [SerializeField] private float modelForwardOffsetY = 90f;

    private Vector3 spawnPosition;
    private Vector3 target;
    private bool isWalking;
    private float pauseTimer;
    private bool isPaused;

    private void Awake()
    {
        spawnPosition = transform.position;
        pauseTimer = Random.Range(pauseDurationMin, pauseDurationMax);
    }

    // Freezes the whole state machine in place (walk progress, pause
    // countdown) rather than resetting anything, so wandering picks back
    // up exactly where it left off once unpaused. Driven by NPCDialogue
    // (2026-08-10, Ben's call: "engaging the dialog should stop movement
    // until the dialog is complete") — kept generic (not dialogue-specific
    // naming) since anything could reasonably want to pause an NPC later.
    public void SetPaused(bool paused) => isPaused = paused;

    private void Update()
    {
        if (isPaused) return;

        if (isWalking)
        {
            Vector3 flatTarget = new Vector3(target.x, transform.position.y, target.z);
            Vector3 newPos = Vector3.MoveTowards(transform.position, flatTarget, moveSpeed * Time.deltaTime);
            newPos.y = GroundHeight.Sample(newPos, transform.position.y);
            transform.position = newPos;
            FaceToward(flatTarget);

            if (Vector3.Distance(transform.position, flatTarget) < 0.05f)
            {
                isWalking = false;
                pauseTimer = Random.Range(pauseDurationMin, pauseDurationMax);
            }
        }
        else
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
            {
                PickNewTarget();
                isWalking = true;
            }
        }
    }

    private void PickNewTarget()
    {
        Vector2 offset = Random.insideUnitCircle * wanderRadius;
        target = spawnPosition + new Vector3(offset.x, 0f, offset.y);
    }

    // Public so NPCGathering (Chunk 4) can reuse the exact same
    // modelForwardOffsetY correction instead of duplicating it -- any
    // component moving this NPC around should face it the same way.
    public void FaceToward(Vector3 worldTarget)
    {
        Vector3 direction = worldTarget - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, modelForwardOffsetY, 0f);
    }
}
