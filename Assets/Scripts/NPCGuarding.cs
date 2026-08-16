using UnityEngine;

// The Guarding job loop (2026-08-16, GUARDING_PLANNING.md) -- sibling to
// NPCGathering/NPCCrafting, same always-present-on-the-prefab, bail-early-
// if-wrong-kind convention. Patrols a circle around the nearest placed
// Village Flag at that Flag's own Player Map reveal radius (Ben's own
// framing -- "circles the village based on the visible range of the
// village flag"), breaking off to fight any HostileCreature that wanders
// within its detection ring, then returning to patrol once the threat is
// gone. Reuses HostileCreature's own Idle/Chasing/Attacking state shape,
// just retargeted.
[RequireComponent(typeof(NPCWander))]
[RequireComponent(typeof(NPCJob))]
[RequireComponent(typeof(NPCSkills))]
[RequireComponent(typeof(NPCVitals))]
public class NPCGuarding : MonoBehaviour
{
    private enum State { Patrolling, Chasing, Attacking }

    [SerializeField] private float detectionRadius = 15f;
    [SerializeField] private float giveUpRadius = 25f;
    [SerializeField] private float meleeAttackRange = 2.5f;
    [SerializeField] private float rangedAttackRange = 20f;
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField] private float obstacleCheckDistance = 1.5f;

    private const float MeleeBaseDamage = 9f;
    private const float MeleeCooldown = 0.7f;
    private const float RangedBaseDamageMin = 2f;
    private const float RangedBaseDamageMax = 4f;
    private const float RangedCooldown = 1.2f;
    private const float GuardSkillGain = 1f;

    private NPCWander wander;
    private NPCJob job;
    private NPCSkills skills;
    private NPCHiring hiring;

    private State state = State.Patrolling;
    private bool isPaused;
    private float attackTimer;

    private VillageFlag patrolFlag;
    private float patrolAngle;

    private HostileCreature currentThreat;

    public void SetPaused(bool paused) => isPaused = paused;

    // Read by NPCVitals so regen pauses during a fight, same "recovers
    // when safe" reasoning PlayerVitals' own stamina already follows.
    public bool IsFighting => state == State.Chasing || state == State.Attacking;

    private void Awake()
    {
        wander = GetComponent<NPCWander>();
        job = GetComponent<NPCJob>();
        skills = GetComponent<NPCSkills>();
        hiring = GetComponent<NPCHiring>();
    }

    private void Update()
    {
        if (isPaused) return;

        bool ready = job.IsReady
            && job.AssignedJob.kind == NPCJobDefinition.JobKind.Guarding
            && (hiring == null || !hiring.IsWaitingForPayment);
        if (!ready)
        {
            state = State.Patrolling;
            currentThreat = null;
            wander.SetPaused(false);
            return;
        }

        if (currentThreat == null || !ThreatStillValid())
        {
            currentThreat = FindNearestThreat();
            if (currentThreat != null) state = State.Chasing;
        }

        if (currentThreat != null)
        {
            UpdateCombat();
            return;
        }

        state = State.Patrolling;
        UpdatePatrol();
    }

    private bool ThreatStillValid()
    {
        if (currentThreat == null) return false;
        float distance = Vector3.Distance(transform.position, currentThreat.transform.position);
        return distance <= giveUpRadius;
    }

    private HostileCreature FindNearestThreat()
    {
        HostileCreature best = null;
        float bestDist = detectionRadius;

        foreach (var hostile in FindObjectsByType<HostileCreature>(FindObjectsSortMode.None))
        {
            float dist = Vector3.Distance(transform.position, hostile.transform.position);
            if (dist >= bestDist) continue;
            bestDist = dist;
            best = hostile;
        }

        return best;
    }

    private void UpdateCombat()
    {
        wander.SetPaused(true);

        float attackRange = IsRangedEquipped() ? rangedAttackRange : meleeAttackRange;
        float distance = Vector3.Distance(transform.position, currentThreat.transform.position);

        attackTimer -= Time.deltaTime;

        if (distance > attackRange)
        {
            state = State.Chasing;
            MoveToward(currentThreat.transform.position, currentThreat.gameObject);
            return;
        }

        state = State.Attacking;
        wander.FaceToward(currentThreat.transform.position);
        if (attackTimer > 0f) return;

        ResolveAttack();
        attackTimer = IsRangedEquipped() ? RangedCooldown : MeleeCooldown;
    }

    private void ResolveAttack()
    {
        var weapon = job.GetEquipped("Weapon");
        float damage;

        if (weapon != null && weapon.isRangedWeapon)
        {
            var arrow = job.GetEquipped("Arrow");
            float bonus = arrow != null ? CraftTierScale.ArrowDamageBonus(arrow.tier) : 0f;
            bonus += CraftTierScale.BowDamageBonus(weapon.tier);
            damage = Random.Range(RangedBaseDamageMin, RangedBaseDamageMax) + bonus;
        }
        else
        {
            float bonus = weapon != null ? CraftTierScale.WeaponDamageBonus(weapon.tier) : 0f;
            damage = MeleeBaseDamage + bonus;
        }

        currentThreat.TakeDamage(damage);
        // Without this, the Wolf never notices it's being hit and keeps
        // ignoring this Guard entirely (HostileCreature only ever tracked
        // the player before this feature) -- see HostileCreature.
        // RedirectAggro's own header comment.
        currentThreat.RedirectAggro(transform);

        skills.GainExperience(job.AssignedJob.family, GuardSkillGain);
    }

    private bool IsRangedEquipped()
    {
        var weapon = job.GetEquipped("Weapon");
        return weapon != null && weapon.isRangedWeapon;
    }

    // Circles the anchor Flag at its own Player Map reveal radius (Ben's
    // framing: "would simulate patrolling around the village") -- a slowly
    // advancing target point on the circle, walked toward with the same
    // straight-line MoveToward every other NPC movement script uses, not
    // real path-following. No Flag placed -- falls back to standing at
    // wherever it currently is (detection ring still active), same
    // "nothing to do, don't crash" fallback NPCCrafting/NPCTraining
    // already use for their own unmet prerequisites.
    private void UpdatePatrol()
    {
        if (patrolFlag == null)
            patrolFlag = FindNearestFlag();

        if (patrolFlag == null)
        {
            wander.SetPaused(false);
            return;
        }

        wander.SetPaused(true);

        float radius = CraftTierScale.VillageFlagRevealRadius(patrolFlag.Tier);
        float angularSpeed = radius > 0.01f ? moveSpeed / radius : 0f;
        patrolAngle += angularSpeed * Time.deltaTime;

        Vector3 center = patrolFlag.transform.position;
        Vector3 offset = new Vector3(Mathf.Cos(patrolAngle), 0f, Mathf.Sin(patrolAngle)) * radius;
        Vector3 target = center + offset;
        target.y = GroundHeight.Sample(target, center.y);

        MoveToward(target, patrolFlag.gameObject);
    }

    private VillageFlag FindNearestFlag()
    {
        VillageFlag best = null;
        float bestDist = float.MaxValue;

        foreach (var flag in FindObjectsByType<VillageFlag>(FindObjectsSortMode.None))
        {
            float dist = Vector3.Distance(transform.position, flag.transform.position);
            if (dist >= bestDist) continue;
            bestDist = dist;
            best = flag;
        }

        return best;
    }

    // Same straight-line-plus-deflection movement as NPCGathering/
    // NPCCrafting/NPCTraining/NPCSeekFlag's own MoveToward -- duplicated
    // rather than shared, same reasoning as those.
    private void MoveToward(Vector3 targetPos, GameObject ignoreTarget)
    {
        Vector3 toTarget = targetPos - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        Vector3 desired = toTarget.normalized;
        Vector3 moveDir = desired;

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, desired, out var hit, obstacleCheckDistance, ~0, QueryTriggerInteraction.Ignore)
            && hit.collider.gameObject != ignoreTarget)
        {
            Vector3 deflected = Vector3.Cross(Vector3.up, hit.normal).normalized;
            if (Vector3.Dot(deflected, desired) < 0f) deflected = -deflected;
            moveDir = deflected;
        }

        Vector3 flatTarget = transform.position + moveDir * moveSpeed * Time.deltaTime;
        Vector3 newPos = new Vector3(flatTarget.x, transform.position.y, flatTarget.z);
        newPos.y = GroundHeight.Sample(newPos, transform.position.y);
        transform.position = newPos;
        wander.FaceToward(transform.position + moveDir);
    }
}
