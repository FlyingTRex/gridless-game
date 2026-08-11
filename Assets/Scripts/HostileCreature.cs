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
[RequireComponent(typeof(Collider))]
public class HostileCreature : MonoBehaviour, IDamageable, IInteractable
{
    private enum State { Idle, Chasing, Attacking, Dead }

    [SerializeField] private float maxHealth = 60f;
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
    // Null/empty means no tool needed, same convention as
    // ResourceNode.requiredTools — but skinning is always Knife-gated in
    // practice (wired in the prefab, not left empty).
    [SerializeField] private ItemDefinition[] requiredTools;
    [SerializeField] private string requiredToolLabel = "Knife";
    [SerializeField] private SkillDefinition skinningSkill;
    [SerializeField] private float skinningSkillGain = 0.5f;
    [SerializeField] private float skinHoldDuration = 2f;

    // <=0 disables respawn entirely, same "0 disables it" convention as
    // ResourceNode.respawnDelay/ChoppableTree.regrowDelay.
    [SerializeField] private float respawnDelay = 180f;

    private float health;
    private State state = State.Idle;
    private float attackTimer;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private Transform player;
    private Collider col;

    public string Prompt => state == State.Dead
        ? (HasToolRequirement ? $"Hold to skin (requires {requiredToolLabel})" : "Hold to skin")
        : "";

    public bool IsInstant => false;

    public float GetHoldDuration(GameObject playerObj) => skinHoldDuration;

    private bool HasToolRequirement => requiredTools != null && requiredTools.Length > 0;

    private void Awake()
    {
        col = GetComponent<Collider>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        health = maxHealth;
    }

    private void Start()
    {
        // Looked up once rather than per-frame — same reasoning as
        // ResourceNode's shieldWearer lookup: this object isn't parented
        // under Player, so there's no cheap direct reference.
        var vitals = FindFirstObjectByType<PlayerVitals>();
        player = vitals != null ? vitals.transform : null;
    }

    private void Update()
    {
        if (state == State.Dead || player == null) return;

        attackTimer -= Time.deltaTime;
        float distance = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case State.Idle:
                if (distance <= detectionRadius)
                    state = State.Chasing;
                break;

            case State.Chasing:
                if (distance > giveUpRadius)
                {
                    state = State.Idle;
                    break;
                }
                if (distance <= attackRange)
                {
                    state = State.Attacking;
                    break;
                }
                MoveToward(player.position);
                break;

            case State.Attacking:
                if (distance > attackRange)
                {
                    state = State.Chasing;
                    break;
                }
                FaceToward(player.position);
                if (attackTimer <= 0f)
                {
                    player.GetComponent<PlayerVitals>()?.Damage(attackDamage);
                    attackTimer = attackCooldown;
                }
                break;
        }
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

    public void TakeDamage(float amount)
    {
        if (state == State.Dead) return;

        health -= amount;
        if (health <= 0f)
            Die();
    }

    private void Die()
    {
        state = State.Dead;
        // Crude "fallen over" visual — no animation system exists yet, so
        // tipping onto its side is enough to read as dead rather than
        // just standing still and no longer reacting.
        transform.rotation = spawnRotation * Quaternion.Euler(0f, 0f, 90f);
    }

    // Called by PlayerInteraction once the skin hold completes — only
    // meaningful while Dead (Prompt/GetHoldDuration already gate the UI
    // side, but Complete() itself also no-ops defensively).
    public void Complete(GameObject playerObj)
    {
        if (state != State.Dead) return;

        if (HasToolRequirement)
        {
            var equipment = playerObj.GetComponent<PlayerEquipment>();
            if (equipment == null || !HasAnyRequiredToolInHand(equipment)) return;
        }

        playerObj.GetComponent<PlayerSkills>()?.GainExperience(skinningSkill, skinningSkillGain);

        // Spawns via the acting player's own PlayerDropping (same
        // Configure()-calling path AdminSpawnScreen uses) rather than a
        // hardcoded chunk prefab — sidesteps the ResourceNode.SpawnChunk/
        // Pickup.Configure() gap documented in BUGS_AND_ENHANCEMENTS.md.
        // Lands near the player, who has to be standing at the corpse to
        // trigger this at all, so it reads as "at the kill" in practice.
        var dropping = playerObj.GetComponent<PlayerDropping>();
        if (Random.value < peltDropChance)
            dropping?.SpawnPickup(peltItem, peltCount);
        dropping?.SpawnPickup(meatItem, Random.Range(meatMinCount, meatMaxCount + 1));

        if (respawnDelay <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        SetVisible(false);
        Invoke(nameof(Respawn), respawnDelay);
    }

    private bool HasAnyRequiredToolInHand(PlayerEquipment equipment)
    {
        foreach (var tool in requiredTools)
        {
            if (tool != null && equipment.HasInHand(tool)) return true;
        }
        return false;
    }

    private void Respawn()
    {
        health = maxHealth;
        state = State.Idle;
        attackTimer = 0f;
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        if (col != null) col.enabled = visible;
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = visible;
    }
}
