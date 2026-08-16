using UnityEngine;

// First Basic Combat target (2026-08-10) — a placeholder hostile, not a
// real Animal & Hunting System entry (that's explicitly a separate, later
// Phase 2 item per design-brief.md: "tame, hunt, harvest, skin"). Idle
// until the player wanders close, then chases and bites; dies at 0 health
// and becomes skinnable with a Knife for Wolf Pelt + Raw Meat, same
// tool-gated hold-to-break shape ResourceNode already uses. No wandering
// AI in this first pass — stands still until detection triggers, simpler
// than real patrol pathing and good enough for testing combat itself. No
// NavMesh in this project — movement is a plain flat-ground MoveTowards,
// matching the terrain's own flatness rather than adding pathfinding no
// existing system uses yet.
//
// Death/skin/respawn lifecycle lives in SkinnableCreature (shared with
// PreyCreature, 2026-08-15 efficiency pass) — this class owns only the
// AI state machine and its own Pelt+Meat loot table.
public class HostileCreature : SkinnableCreature
{
    private enum AIState { Idle, Chasing, Attacking }

    [SerializeField] private float detectionRadius = 10f;
    // Beyond this, give up the chase and return to Idle — without it a
    // provoked wolf would chase across the entire map.
    [SerializeField] private float giveUpRadius = 20f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float attackDamage = 8f;
    [SerializeField] private float attackCooldown = 1.5f;

    [SerializeField] private ItemDefinition peltItem;
    [SerializeField] private ItemDefinition meatItem;
    [SerializeField] private int peltCount = 1;
    // Pelt is a flat chance, not guaranteed — Ben's call, 2026-08-10.
    [SerializeField, Range(0f, 1f)] private float peltDropChance = 0.5f;
    // Meat always drops, but the amount varies — Random.Range's upper
    // bound is exclusive, so meatMaxCount itself is a real possible roll.
    [SerializeField] private int meatMinCount = 1;
    [SerializeField] private int meatMaxCount = 2;

    private AIState aiState = AIState.Idle;
    private float attackTimer;
    private Transform target;

    private void Start()
    {
        // Looked up once rather than per-frame — same reasoning as
        // ResourceNode's shieldWearer lookup: this object isn't parented
        // under Player, so there's no cheap direct reference. Defaults to
        // the player; a Guard that lands a hit redirects aggro onto itself
        // via RedirectAggro below (2026-08-16, GUARDING_PLANNING.md).
        var vitals = FindFirstObjectByType<PlayerVitals>();
        target = vitals != null ? vitals.transform : null;
    }

    // Called by NPCGuarding after it damages this creature — without this,
    // a Guard could hit a Wolf forever and never get hit back, since this
    // AI only ever tracked the player. Re-provoking mid-fight (repeated
    // calls while already Chasing/Attacking the same target) is a
    // harmless no-op.
    public void RedirectAggro(Transform newTarget)
    {
        if (isDead || newTarget == null) return;

        // Logged only on a genuine retarget, not every repeated hit while
        // already fighting the same thing -- proves the Guard-vs-Wolf
        // retaliation mechanic actually fired, without spamming once per
        // swing.
        if (target != newTarget)
            Debug.Log($"[HostileCreature] {name} aggro redirected onto {newTarget.name}.");

        target = newTarget;
        if (aiState == AIState.Idle)
            aiState = AIState.Chasing;
    }

    private void Update()
    {
        if (isDead || target == null) return;

        attackTimer -= Time.deltaTime;
        float distance = Vector3.Distance(transform.position, target.position);

        switch (aiState)
        {
            case AIState.Idle:
                if (distance <= detectionRadius)
                    aiState = AIState.Chasing;
                break;

            case AIState.Chasing:
                if (distance > giveUpRadius)
                {
                    aiState = AIState.Idle;
                    break;
                }
                if (distance <= attackRange)
                {
                    aiState = AIState.Attacking;
                    break;
                }
                MoveToward(target.position);
                break;

            case AIState.Attacking:
                if (distance > attackRange)
                {
                    aiState = AIState.Chasing;
                    break;
                }
                FaceToward(target.position);
                if (attackTimer <= 0f)
                {
                    DealDamageToTarget();
                    attackTimer = attackCooldown;
                }
                break;
        }
    }

    // PlayerVitals isn't IDamageable (it exposes Damage() directly, not
    // TakeDamage()) — checked first so the player-facing path is
    // byte-for-byte unchanged from before this generalization. Anything
    // else (an NPCVitals-bearing Guard) goes through the normal
    // IDamageable interface every other attack in this project already
    // uses.
    private void DealDamageToTarget()
    {
        var playerVitals = target.GetComponent<PlayerVitals>();
        if (playerVitals != null)
        {
            playerVitals.Damage(attackDamage);
            return;
        }

        target.GetComponentInParent<IDamageable>()?.TakeDamage(attackDamage);
    }

    private void MoveToward(Vector3 target)
    {
        Vector3 flatTarget = new Vector3(target.x, transform.position.y, target.z);
        Vector3 newPos = Vector3.MoveTowards(transform.position, flatTarget, moveSpeed * Time.deltaTime);
        newPos.y = GroundHeight.Sample(newPos, transform.position.y);
        transform.position = newPos;
        FaceToward(target);
    }

    private void FaceToward(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    protected override void DropLoot(PlayerDropping dropping)
    {
        // Spawns via the acting player's own PlayerDropping (same
        // Configure()-calling path AdminSpawnScreen uses) rather than a
        // hardcoded chunk prefab — sidesteps the ResourceNode.SpawnChunk/
        // Pickup.Configure() gap documented in BUGS_AND_ENHANCEMENTS.md.
        // Lands near the player, who has to be standing at the corpse to
        // trigger this at all, so it reads as "at the kill" in practice.
        if (Random.value < peltDropChance)
            dropping?.SpawnPickup(peltItem, peltCount);
        dropping?.SpawnPickup(meatItem, Random.Range(meatMinCount, meatMaxCount + 1));
    }

    protected override void Respawn()
    {
        aiState = AIState.Idle;
        attackTimer = 0f;
        base.Respawn();
    }
}
