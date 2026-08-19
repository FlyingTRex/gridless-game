using UnityEngine;

// The Desk/Bookshelf training ritual (2026-08-16, NPC_TRAINING_PLANNING.md
// -- Chunk 2 of the Settlement Growth Loop). Unlike NPCGathering/
// NPCCrafting, this isn't a continuous background job -- it's a one-shot
// interrupt triggered by the player (mirrors NPCDialogue's Begin/End
// shape, just with a real walk-to-Desk step and a 2-minute wait instead of
// an instant timer), after which the NPC resumes whatever job it was
// already doing.
[RequireComponent(typeof(NPCWander))]
[RequireComponent(typeof(NPCJob))]
public class NPCTraining : MonoBehaviour
{
    private const float TrainingDurationSeconds = 120f;

    // Matches PlayerCrafting/NPCCrafting's own "within 2m" surface-range
    // convention.
    private const float DeskRange = 2f;

    [SerializeField] private float searchRadius = 50f;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float obstacleCheckDistance = 1.5f;

    private NPCWander wander;
    private NPCJob job;
    private NPCGathering gathering;
    private NPCCrafting crafting;
    private PlayerFame playerFame;

    // Shared with NPCGathering/NPCCrafting/NPCGuarding/NPCSeekFlag's own
    // MoveToward via NPCMovement.cs (2026-08-19) -- see that file's header.
    private readonly NPCMovement.StuckTracker stuckTracker = new();

    private bool isTraining;
    private DeskSurface targetDesk;
    private float trainTimer;
    private CraftingRecipe pendingRecipe;
    private SkillDefinition pendingLineage;

    public bool IsTraining => isTraining;
    public float TrainSecondsElapsed => trainTimer;
    public float TrainDurationSeconds => TrainingDurationSeconds;

    private void Awake()
    {
        wander = GetComponent<NPCWander>();
        job = GetComponent<NPCJob>();
        // Optional -- not every NPC has a job loop assigned yet, and
        // training shouldn't hard-require one just to pause it.
        gathering = GetComponent<NPCGathering>();
        crafting = GetComponent<NPCCrafting>();
        playerFame = FindFirstObjectByType<PlayerFame>();
    }

    // True if this specific book would actually grant something new --
    // section 4's "book already granted" edge case, checked upfront so
    // NPCTrainingScreen never even offers a redundant book (wasting a
    // real, consumed book on a no-op grant reads as a footgun worth
    // preventing, not silently absorbing).
    public bool CanTrainWith(SkillBook book)
    {
        if (book == null || isTraining) return false;
        if (book.TargetRecipe != null) return !job.HasGrantedRecipe(book.TargetRecipe);
        if (book.TargetWish != null) return !job.HasLineage(book.TargetWish.lineage);
        return false;
    }

    // Deliberately checks Desk availability BEFORE consuming the book --
    // simpler and safer than leaving "no Desk in range" as a runtime state
    // to bail out of mid-walk with an already-consumed book and nowhere to
    // go. source is whichever Inventory the book is actually sitting in
    // (Bookshelf or the player's own main inventory), same "don't assume
    // main inventory" discipline PlayerReading.TryRead already follows.
    public bool TryBeginTraining(SkillBook book, Inventory source, out string failReason)
    {
        failReason = null;
        if (!CanTrainWith(book)) { failReason = "Already known."; return false; }
        if (source == null) { failReason = "Book source missing."; return false; }

        var desk = FindNearestWithinRadius<DeskSurface>();
        if (desk == null) { failReason = "No Desk in range."; return false; }

        // Consumed immediately, not on completion -- matches every other
        // "consume upfront" convention in this project (PlayerCrafting.
        // StartCraft, Campfire.StartCooking, NPCCrafting's own loop).
        pendingRecipe = book.TargetRecipe;
        pendingLineage = book.TargetRecipe != null ? null : book.TargetWish?.lineage;
        source.RemoveEquipmentItem(book.ItemDefinition);
        Destroy(book.gameObject);

        targetDesk = desk;
        trainTimer = 0f;
        isTraining = true;

        wander.SetPaused(true);
        gathering?.SetPaused(true);
        crafting?.SetPaused(true);
        return true;
    }

    // Called by NPCHiring.Fire (or any future reassignment) when training
    // is interrupted mid-flight -- bails out cleanly rather than
    // softlocking, same pattern NPCGathering.UpdateReturning already uses
    // when its own deposit box goes null mid-walk. The already-consumed
    // book is lost, not refunded (leaning-lost per section 4, matching the
    // consume-upfront-not-refunded convention everywhere else here).
    public void CancelTraining() => EndTrainingState();

    private void Update()
    {
        if (!isTraining) return;

        if (targetDesk == null)
        {
            CancelTraining();
            return;
        }

        float distance = Vector3.Distance(transform.position, targetDesk.transform.position);
        if (distance > DeskRange)
        {
            MoveToward(targetDesk.transform.position, targetDesk.gameObject);
            return;
        }

        wander.FaceToward(targetDesk.transform.position);
        trainTimer += Time.deltaTime;
        if (trainTimer < TrainingDurationSeconds) return;

        FinishTraining();
    }

    private void FinishTraining()
    {
        if (pendingRecipe != null) job.GrantRecipe(pendingRecipe);
        else if (pendingLineage != null) job.LearnLineage(pendingLineage);

        playerFame?.GrantNpcTraining();
        EndTrainingState();
    }

    private void EndTrainingState()
    {
        isTraining = false;
        targetDesk = null;
        trainTimer = 0f;
        pendingRecipe = null;
        pendingLineage = null;

        wander.SetPaused(false);
        gathering?.SetPaused(false);
        crafting?.SetPaused(false);
    }

    private T FindNearestWithinRadius<T>() where T : Component
    {
        T best = null;
        float bestDist = searchRadius;

        foreach (var candidate in FindObjectsByType<T>(FindObjectsSortMode.None))
        {
            float dist = Vector3.Distance(transform.position, candidate.transform.position);
            if (dist >= bestDist) continue;
            bestDist = dist;
            best = candidate;
        }

        return best;
    }

    // Same straight-line movement as NPCGathering/NPCCrafting's own
    // MoveToward -- obstacle deflection/stuck-recovery is shared via
    // NPCMovement.cs (2026-08-19).
    private void MoveToward(Vector3 targetPos, GameObject ignoreTarget)
    {
        Vector3 toTarget = targetPos - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        Vector3 desired = toTarget.normalized;
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 moveDir = NPCMovement.FindClearDirection(origin, desired, obstacleCheckDistance, ignoreTarget, stuckTracker, Time.deltaTime);

        Vector3 flatTarget = transform.position + moveDir * moveSpeed * Time.deltaTime;
        Vector3 newPos = new Vector3(flatTarget.x, transform.position.y, flatTarget.z);
        newPos.y = GroundHeight.Sample(newPos, transform.position.y);
        transform.position = newPos;
        wander.FaceToward(transform.position + moveDir);
    }
}
