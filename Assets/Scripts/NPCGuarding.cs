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

    // Player-set patrol radius (2026-08-18) -- replaces the original
    // CraftTierScale.VillageFlagRevealRadius(patrolFlag.Tier) reuse, found
    // live by Ben (2026-08-17): a Masterwork Flag gave every Guard a 75m
    // patrol radius, since that scale was tuned for the Player Map's fog
    // reveal, not for how far a Guard should roam from its post -- the
    // exact "a tier-scaling ratio tuned for one quantity doesn't transfer
    // to another" gotcha CLAUDE.md already documents. Ben's own fix
    // (2026-08-18): make it the same kind of per-NPC configurable leash
    // NPCGathering.MaxRangeFromDeposit already is, rather than inventing a
    // second tier table -- same UI pattern in NPCHiringScreen, just
    // anchored to the patrol Flag instead of a deposit box.
    [SerializeField] private float patrolRadius = 15f;
    public float PatrolRadius
    {
        get => patrolRadius;
        set => patrolRadius = Mathf.Max(1f, value);
    }

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

    // Slack around patrolRadius before switching from "approach" to
    // "orbit" mode below -- without it, a Guard sitting exactly on the
    // boundary could flicker between the two modes every frame.
    private const float ApproachTolerance = 0.5f;

    private VillageFlag patrolFlag;

    private HostileCreature currentThreat;

    // See NPCGathering.wasActive's comment for the full story -- this
    // component's own `!ready` branch used to call wander.SetPaused(false)
    // unconditionally every idle frame (i.e. every frame for any NPC whose
    // job isn't Guarding), racing against whichever job component actually
    // is active for real ownership of the pause state. Only release on a
    // genuine active-to-inactive transition.
    private bool wasActive;

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
            if (wasActive) wander.SetPaused(false);
            wasActive = false;
            return;
        }
        wasActive = true;

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

    // Found live by Ben (2026-08-18): a Guard stayed locked in Attacking
    // forever after actually killing its Wolf. Root cause -- a killed
    // creature's GameObject is never destroyed (SkinnableCreature.Complete
    // just SetVisible(false)s it and schedules a much-later Respawn()),
    // so currentThreat never goes null and this check never caught it,
    // since it only ever looked at distance. Checking IsDead here fixes
    // both the pre-skin corpse (still sitting there, collider active
    // until skinned) and the post-skin invisible-but-not-destroyed case
    // the live report actually hit.
    private bool ThreatStillValid()
    {
        if (currentThreat == null || currentThreat.IsDead) return false;
        float distance = Vector3.Distance(transform.position, currentThreat.transform.position);
        return distance <= giveUpRadius;
    }

    private HostileCreature FindNearestThreat()
    {
        HostileCreature best = null;
        float bestDist = detectionRadius;

        foreach (var hostile in FindObjectsByType<HostileCreature>(FindObjectsSortMode.None))
        {
            if (hostile.IsDead) continue;

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
            float bonus = arrow != null ? arrow.EffectiveArrowDamageBonus : 0f;
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

    // Circles the anchor Flag at this Guard's own configurable patrolRadius
    // (Ben's framing: "would simulate patrolling around the village"),
    // walked toward with the same straight-line MoveToward every other NPC
    // movement script uses, not real path-following. No Flag placed --
    // falls back to standing at wherever it currently is (detection ring
    // still active), same "nothing to do, don't crash" fallback
    // NPCCrafting/NPCTraining already use for their own unmet
    // prerequisites.
    //
    // Two modes, found live by Ben (2026-08-18) setting a 2m leash and the
    // Guard never getting any closer to the Flag: the orbiting target
    // point's tangential speed works out to exactly `radius * (moveSpeed /
    // radius) = moveSpeed` -- the Guard's own top speed, REGARDLESS of
    // radius. At the original 35-75m radii this was invisible (the target
    // crawled around slowly enough to always be reachable), but at a small
    // radius the target now circles as fast as the Guard can walk, so a
    // Guard starting outside the circle was chasing a point that never
    // gets any closer -- most of its speed budget goes into keeping up
    // angularly, none into closing the gap. Fixed by splitting into an
    // approach phase (walk straight toward the nearest point on the
    // circle, a fixed target the Guard can actually catch) and only
    // orbiting once already within patrolRadius (+ tolerance).
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

        Vector3 center = patrolFlag.transform.position;
        float radius = patrolRadius;

        Vector3 toGuard = transform.position - center;
        toGuard.y = 0f;
        float distanceFromCenter = toGuard.magnitude;

        Vector3 target;
        if (distanceFromCenter > radius + ApproachTolerance)
        {
            // Outside the circle -- head for the nearest point on it along
            // the current bearing, a static target rather than a fast-
            // orbiting one.
            Vector3 dir = distanceFromCenter > 0.01f ? toGuard.normalized : Vector3.forward;
            target = center + dir * radius;
        }
        else
        {
            // Within the circle -- orbit smoothly. Recomputed from the
            // Guard's own current bearing each frame (not a persistently
            // incrementing angle) so there's no jump on first entering the
            // circle and nothing to drift out of sync with its real
            // position.
            float currentAngle = distanceFromCenter > 0.01f ? Mathf.Atan2(toGuard.z, toGuard.x) : 0f;
            float angularStep = radius > 0.01f ? (moveSpeed / radius) * Time.deltaTime : 0f;
            float nextAngle = currentAngle + angularStep;
            target = center + new Vector3(Mathf.Cos(nextAngle), 0f, Mathf.Sin(nextAngle)) * radius;
        }

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
