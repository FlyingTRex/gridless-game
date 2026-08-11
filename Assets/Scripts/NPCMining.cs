using System.Collections.Generic;
using UnityEngine;

// The actual autonomous mining loop (2026-08-10, Chunks 4-6 of the
// Hireable NPCs build -- see BUGS_AND_ENHANCEMENTS.md). Once assigned Mine
// Ore and fully equipped, finds the nearest available ResourceNode within
// searchRadius it can both use (has a matching tool) and carry the output
// of (NPCEncumbrance.CanPickUp), walks to it, mines it, repeats -- until it
// can't find anything else it can carry, at which point (Chunk 5) it walks
// back to its assigned NPCJob.DepositContainer, deposits everything, and
// resumes. If no deposit point has been set yet, falls back to Chunk 4's
// original behavior (just stops) rather than assuming one exists. Also
// stops entirely once NPCHiring.IsWaitingForPayment (Chunk 6) -- the
// 5-minute work timer running out -- until Pay clears it.
//
// Targets real ResourceNode objects in the world (Copper/Iron/Silver/Gold/
// Platinum Ore Node, Rock Node, Boulder -- every ResourceNode in this
// scene is mining-flavored; trees/bushes are separate component types),
// via ResourceNode.TryMineForNPC/PeekYield rather than Complete(), since
// Complete() is hard-wired to PlayerEquipment/PlayerSkills.
//
// Deliberately trains the job's own family skill (Mining) rather than the
// node's trainedSkill field (which still points at the older, more generic
// Gathering skill on every ore node in the scene today) -- the same
// physical action training a different skill depending on who's doing it
// is a real quirk, but retroactively repointing every existing ore node's
// trainedSkill (and changing what the player has been training by mining
// them) wasn't something to decide silently mid-chunk. Flagged in
// BUGS_AND_ENHANCEMENTS.md for Ben to weigh in on.
[RequireComponent(typeof(NPCWander))]
[RequireComponent(typeof(NPCJob))]
[RequireComponent(typeof(NPCSkills))]
[RequireComponent(typeof(NPCEncumbrance))]
[RequireComponent(typeof(NPCCargo))]
public class NPCMining : MonoBehaviour
{
    [SerializeField] private float searchRadius = 50f;
    [SerializeField] private float mineRange = 2f;
    [SerializeField] private float mineDuration = 3f;
    [SerializeField] private float moveSpeed = 1.5f;

    // How far ahead to check for something blocking the direct path, and
    // how far around it to deflect -- Ben's call, 2026-08-10: "build a
    // collision idea to allow the npc to hit something and change
    // direction," since there's no NavMesh in this project (same
    // constraint HostileCreature/NPCWander already live with). Not real
    // pathfinding -- just enough to slide along an obstacle's edge instead
    // of pushing straight through it or getting stuck.
    [SerializeField] private float obstacleCheckDistance = 1.5f;

    private NPCWander wander;
    private NPCJob job;
    private NPCSkills skills;
    private NPCEncumbrance encumbrance;
    private NPCCargo cargo;

    private NPCHiring hiring;

    private ResourceNode currentTarget;
    private float mineTimer;
    private bool isPaused;
    private bool isReturning;

    // Driven by NPCDialogue the same way it already pauses NPCWander --
    // talking to the NPC should freeze it completely, not just whichever
    // component happens to be moving it at that moment.
    public void SetPaused(bool paused) => isPaused = paused;

    private void Awake()
    {
        wander = GetComponent<NPCWander>();
        job = GetComponent<NPCJob>();
        skills = GetComponent<NPCSkills>();
        encumbrance = GetComponent<NPCEncumbrance>();
        cargo = GetComponent<NPCCargo>();
        // Optional -- not a RequireComponent, so a hypothetical future
        // non-hireable NPC with a job loop wouldn't need one. Chunk 6:
        // stops working while NPCHiring.IsWaitingForPayment, same as
        // being unassigned/unequipped -- an unpaid NPC just holds still
        // (mid-walk cargo isn't lost, just paused) until Pay clears it.
        hiring = GetComponent<NPCHiring>();
    }

    private void Update()
    {
        if (isPaused) return;

        bool ready = job.IsReady && (hiring == null || !hiring.IsWaitingForPayment);
        if (!ready)
        {
            currentTarget = null;
            isReturning = false;
            wander.SetPaused(false);
            return;
        }

        if (isReturning)
        {
            UpdateReturning();
            return;
        }

        if (currentTarget == null || !currentTarget.IsAvailable)
        {
            currentTarget = FindTarget();
            mineTimer = 0f;
        }

        if (currentTarget == null)
        {
            // Nothing reachable/carriable right now. Head back and
            // deposit whatever's already been mined if there's somewhere
            // to put it; otherwise (no deposit point set yet -- Chunk 5's
            // own precondition) fall back to Chunk 4's original behavior
            // and just wander instead of standing frozen.
            if (job.DepositContainer != null && cargo.Inventory.Slots.Count > 0)
            {
                isReturning = true;
                wander.SetPaused(true);
                return;
            }

            wander.SetPaused(false);
            return;
        }

        wander.SetPaused(true);

        Vector3 targetPos = currentTarget.transform.position;
        float distance = Vector3.Distance(transform.position, targetPos);

        if (distance > mineRange)
        {
            MoveToward(targetPos, currentTarget.gameObject);
            mineTimer = 0f;
            return;
        }

        wander.FaceToward(targetPos);
        mineTimer += Time.deltaTime;
        if (mineTimer < mineDuration) return;

        MineCurrentTarget(job.AssignedJob);
        currentTarget = null;
        mineTimer = 0f;
    }

    private void UpdateReturning()
    {
        var box = job.DepositContainer;
        if (box == null)
        {
            // Cleared mid-return (Fire, or reassigned) -- bail out cleanly
            // rather than walking toward a null forever.
            isReturning = false;
            return;
        }

        wander.SetPaused(true);

        Vector3 boxPos = box.transform.position;
        float distance = Vector3.Distance(transform.position, boxPos);

        if (distance > mineRange)
        {
            MoveToward(boxPos, box.gameObject);
            return;
        }

        wander.FaceToward(boxPos);
        DepositCargo(box);
        isReturning = false;
    }

    // Snapshots cargo's slots before moving anything -- Inventory.RemoveItem
    // mutates its own backing list in place (can remove a slot outright),
    // so removing while iterating Slots directly isn't safe. Leftover that
    // doesn't fit in the box stays in cargo rather than being lost, same
    // "leftover" convention every other transfer in this game uses.
    private void DepositCargo(StorageBox box)
    {
        var toMove = new List<(ItemDefinition item, int count)>();
        foreach (var slot in cargo.Inventory.Slots)
            if (slot.item != null && slot.count > 0)
                toMove.Add((slot.item, slot.count));

        foreach (var (item, count) in toMove)
        {
            int leftover = box.Inventory.AddItem(item, count);
            int moved = count - leftover;
            if (moved > 0)
                cargo.Inventory.RemoveItem(item, moved);
        }
    }

    private ResourceNode FindTarget()
    {
        ResourceNode best = null;
        float bestDistance = searchRadius;

        foreach (var node in FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
        {
            if (!node.IsAvailable) continue;

            bool hasToolReq = node.RequiredTools != null && node.RequiredTools.Length > 0;
            if (hasToolReq && !job.HasAnyTool(node.RequiredTools)) continue;

            if (!node.PeekYield(out var item, out var count)) continue;
            if (item == null || !encumbrance.CanPickUp(item.weight * count)) continue;

            float dist = Vector3.Distance(transform.position, node.transform.position);
            if (dist >= bestDistance) continue;

            bestDistance = dist;
            best = node;
        }

        return best;
    }

    private void MineCurrentTarget(NPCJobDefinition assignedJob)
    {
        if (!currentTarget.TryMineForNPC(out var item, out var count)) return;

        cargo.Inventory.AddItem(item, count);
        skills.GainExperience(assignedJob.family, currentTarget.SkillGain);
    }

    // Straight-line movement toward the target, same shape HostileCreature/
    // NPCWander already use -- except a short forward raycast first: if
    // something's in the way, slide along its surface (the tangent of the
    // hit normal) instead of walking straight into it, rather than true
    // pathfinding around it. ignoreTarget is whatever world object this
    // move is actually headed toward (a ResourceNode or a StorageBox) --
    // its own collider shouldn't count as "blocked" once close enough to
    // register a hit on it.
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
