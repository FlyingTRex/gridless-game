using UnityEngine;

// First real PreyCreature movement (2026-08-16) -- idle/wander until the
// player gets close, then flee, closing the long-flagged "PreyCreature's
// movement half unbuilt" gap (Chicken/Deer have stood still since they
// shipped). Combines NPCWander's flat-ground idle/wander shape and
// NPCFlee's away-from-player shape into one state machine, since a single
// creature needs both halves together here (unlike those two, which live
// on different object types and never need to coexist). Built generic
// rather than Rabbit-specific -- any future PreyCreature (Pig included)
// can add this same component once it has Idle/Run clips to drive.
//
// Sibling to SkinnableCreature, not a subclass -- reads SkinnableCreature.
// IsDead to stop driving movement/animation once dead, same as
// HostileCreature's own internal isDead check.
[RequireComponent(typeof(SkinnableCreature))]
public class PreyWander : MonoBehaviour
{
    private enum State { Idle, Wandering, Fleeing }

    [SerializeField] private float wanderRadius = 8f;
    [SerializeField] private float moveSpeed = 1.2f;
    [SerializeField] private float fleeSpeed = 4f;
    [SerializeField] private float pauseDurationMin = 2f;
    [SerializeField] private float pauseDurationMax = 5f;
    [SerializeField] private float fleeDetectionRadius = 8f;
    [SerializeField] private float fleeDistance = 10f;

    // Same "model's authored forward axis doesn't match Unity's
    // LookRotation convention" correction NPCWander already needed for a
    // different imported model -- tune per-model if a future PreyWander
    // user walks backwards/sideways instead of forward.
    [SerializeField] private float modelForwardOffsetY = 0f;

    // Drives an Idle<->Run blend on whatever Animator is found on this
    // object or a child (e.g. the Rabbit's own rig) -- optional, a
    // PreyWander with no Animator still moves correctly, just without
    // animated legs.
    private static readonly int SpeedParam = Animator.StringToHash("Speed");

    private SkinnableCreature creature;
    private Animator animator;
    private Transform player;

    private Vector3 spawnPosition;
    private Vector3 target;
    private State state = State.Idle;
    private float pauseTimer;

    private void Awake()
    {
        creature = GetComponent<SkinnableCreature>();
        animator = GetComponentInChildren<Animator>();
        spawnPosition = transform.position;
        pauseTimer = Random.Range(pauseDurationMin, pauseDurationMax);

        ResolvePlayerTarget();
    }

    // Retried lazily, not just once in Awake() -- Player now carries a
    // NetworkIdentity (Multiplayer Bootstrap, 2026-08-22) and can still be
    // deactivated (Mirror hides unspawned scene NetworkIdentity objects)
    // at the moment this object's own Awake() runs, since Awake() order
    // across different GameObjects isn't guaranteed either. A one-shot
    // lookup here could permanently miss the player. Cheap no-op once
    // player is already set.
    private void ResolvePlayerTarget()
    {
        if (player != null) return;
        var vitals = FindFirstObjectByType<PlayerVitals>();
        player = vitals != null ? vitals.transform : null;
    }

    private void Update()
    {
        if (creature.IsDead)
        {
            // SkinnableCreature owns the death pose from here -- stop
            // driving movement/animation entirely rather than fighting it.
            enabled = false;
            return;
        }

        if (player == null) ResolvePlayerTarget();

        bool shouldFlee = player != null
            && Vector3.Distance(transform.position, player.position) < fleeDetectionRadius;

        if (shouldFlee)
        {
            state = State.Fleeing;
            Vector3 away = transform.position - player.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f) away = transform.forward;
            away.Normalize();
            MoveToward(transform.position + away * fleeDistance, fleeSpeed);
            return;
        }

        // Just stopped fleeing -- resume the ordinary idle/wander cycle
        // fresh rather than continuing whatever wander target was picked
        // before the flee interrupted it.
        if (state == State.Fleeing)
        {
            state = State.Idle;
            pauseTimer = Random.Range(pauseDurationMin, pauseDurationMax);
        }

        if (state == State.Wandering)
        {
            Vector3 flatTarget = new Vector3(target.x, transform.position.y, target.z);
            MoveToward(flatTarget, moveSpeed);

            if (Vector3.Distance(transform.position, flatTarget) < 0.1f)
            {
                state = State.Idle;
                pauseTimer = Random.Range(pauseDurationMin, pauseDurationMax);
                SetSpeed(0f);
            }
        }
        else
        {
            SetSpeed(0f);
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
            {
                PickNewTarget();
                state = State.Wandering;
            }
        }
    }

    private void MoveToward(Vector3 flatTarget, float speed)
    {
        Vector3 newPos = Vector3.MoveTowards(transform.position, flatTarget, speed * Time.deltaTime);
        newPos.y = GroundHeight.Sample(newPos, transform.position.y);
        transform.position = newPos;
        FaceToward(flatTarget);
        SetSpeed(speed);
    }

    private void PickNewTarget()
    {
        Vector2 offset = Random.insideUnitCircle * wanderRadius;
        target = spawnPosition + new Vector3(offset.x, 0f, offset.y);
    }

    private void FaceToward(Vector3 worldTarget)
    {
        Vector3 direction = worldTarget - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, modelForwardOffsetY, 0f);
    }

    private void SetSpeed(float speed)
    {
        if (animator != null) animator.SetFloat(SpeedParam, speed);
    }
}
