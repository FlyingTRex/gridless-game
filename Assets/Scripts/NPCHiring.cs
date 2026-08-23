using Mirror;
using UnityEngine;

// Multiplayer, 2026-08-23 -- found during the "audit NPC-initiated
// behavior" pass: Update()'s work/unpaid timer is real autonomous state
// advancing every frame (same category as PlayerVitals' passive drain),
// missed the first "NPCs move server-side" pass since it isn't movement.
// Converted to NetworkBehaviour, isServer guard added below. TryHire/
// Fire/TryPay themselves needed no change -- already only ever called
// from NPCHiringScreen's Commands (built in Phase 3 sub-phase 5),
// already server-side regardless of this component's own network status.
//
// Chunk 1 of the Hireable NPCs build (see BUGS_AND_ENHANCEMENTS.md,
// 2026-08-10): the Hire/Fire/Pay state machine and currency spend, no job
// logic yet. E now opens NPCHiringScreen's popup menu instead of going
// straight to dialogue -- "Talk" is one of that menu's buttons.
//
// Chunk 6: the work timer that actually sets isWaitingForPayment. A
// 5-minute real-world stand-in for the design brief's original "5 real
// days" -- this project has no persistence (no DateTime/save-load
// anywhere), so a genuine multi-day real-world timer can't be built or
// even tested without a save system that survives closing the Editor.
// Ben's call: ship the 5-minute version now, revisit the real duration
// once actual game persistence exists (a separate prerequisite piece).
[RequireComponent(typeof(NPCDialogue))]
[RequireComponent(typeof(NPCJob))]
[RequireComponent(typeof(NPCSkills))]
[RequireComponent(typeof(NPCEncumbrance))]
[RequireComponent(typeof(NPCCargo))]
[RequireComponent(typeof(SaveId))]
public class NPCHiring : NetworkBehaviour, IInteractable
{
    [SerializeField] private CoinType hireCoinType = CoinType.Copper;
    [SerializeField] private int hireCoinAmount = 10;

    // Only counts real-world time spent actually working (NPCJob.IsReady)
    // -- an NPC sitting unassigned or unequipped isn't "working," so its
    // clock shouldn't run down either. Matches Ben's own framing ("the
    // npc works for 5 real days, and then waits for payment"). Was 300f
    // (5 real minutes); lengthened to 60 real minutes (2026-08-17, Ben's
    // call) now that Village-Flag-spawned NPCs are the only source and
    // testing/playing across a longer session made 5 minutes too short
    // a leash.
    [SerializeField] private float workDurationSeconds = 3600f;

    private NPCDialogue dialogue;
    private NPCJob job;
    private NPCSkills skills;
    private NPCEncumbrance encumbrance;
    private NPCCargo cargo;
    private NPCTraining training;
    private PlayerFame playerFame;
    private bool isHired;
    private bool isWaitingForPayment;
    private float workTimer;

    // -0.5 Fame every full workDurationSeconds spent unpaid, not a
    // one-time hit — a chronically-neglected NPC keeps costing Fame (see
    // FAME_PLANNING.md, 2026-08-14). Separate from workTimer, which stops
    // advancing once isWaitingForPayment is true.
    private float unpaidTimer;

    public bool IsHired => isHired;
    public bool IsWaitingForPayment => isWaitingForPayment;
    public CoinType HireCoinType => hireCoinType;
    public int HireCoinAmount => hireCoinAmount;
    public string DisplayName => dialogue.DisplayName;
    public NPCJob Job => job;
    public NPCSkills Skills => skills;
    public NPCEncumbrance Encumbrance => encumbrance;
    public NPCCargo Cargo => cargo;

    // Read by NPCHiringScreen to show a countdown while actively working
    // -- there was previously no visibility into how close an NPC was to
    // needing payment.
    public float WorkTimeRemaining => Mathf.Max(0f, workDurationSeconds - workTimer);

    // Read/written by SaveManager.
    public float WorkTimer => workTimer;

    // Shared by NPCHiringScreen/NPCJobScreen (2026-08-18, Ben's live report
    // -- "walked up, talked, and the npc still moved" while the Assign Job
    // menu was open) so managing an NPC via either screen holds it still
    // the same way Talk already does, instead of leaving it free to wander
    // off mid-interaction. Mirrors NPCDialogue.BeginDialogue/EndDialogue's
    // exact same four-component pause pattern -- not reused directly from
    // there since NPCDialogue's own isTalking state is a separate concern,
    // and not routed through NPCFreeze either, since that toggle represents
    // a deliberate player choice ("stay frozen") that a temporary UI-open
    // pause must not silently clear on close.
    public void SetMovementPaused(bool paused)
    {
        GetComponent<NPCWander>()?.SetPaused(paused);
        GetComponent<NPCGathering>()?.SetPaused(paused);
        GetComponent<NPCCrafting>()?.SetPaused(paused);
        GetComponent<NPCGuarding>()?.SetPaused(paused);
    }

    public void RestoreHiringState(bool hired, bool waitingForPayment, float timer)
    {
        isHired = hired;
        isWaitingForPayment = waitingForPayment;
        workTimer = timer;
    }

    public string Prompt => $"Talk to {DisplayName}";
    public bool IsInstant => true;
    public float GetHoldDuration(GameObject player) => 0f;

    private void Awake()
    {
        dialogue = GetComponent<NPCDialogue>();
        job = GetComponent<NPCJob>();
        skills = GetComponent<NPCSkills>();
        encumbrance = GetComponent<NPCEncumbrance>();
        cargo = GetComponent<NPCCargo>();
        // Optional -- not every hireable NPC setup necessarily has one.
        training = GetComponent<NPCTraining>();
        playerFame = FindFirstObjectByType<PlayerFame>();
    }

    private void Update()
    {
        if (!isServer) return;

        if (isHired && isWaitingForPayment)
        {
            unpaidTimer += Time.deltaTime;
            if (unpaidTimer >= workDurationSeconds)
            {
                unpaidTimer -= workDurationSeconds;
                playerFame?.GrantUnpaidCycle();
            }
        }

        if (!isHired || isWaitingForPayment || !job.IsReady) return;

        workTimer += Time.deltaTime;
        if (workTimer < workDurationSeconds) return;

        isWaitingForPayment = true;
        workTimer = 0f;
        OnPaymentDue?.Invoke(this);
    }

    // Fired exactly once at the moment an NPC's work cycle completes and
    // it starts waiting for payment (2026-08-17, "NPC management" -- Ben's
    // ask for awareness of payment coming due without having to babysit
    // the Roster). A static event rather than a direct player reference --
    // NPCHiring has no reason to know about the player otherwise, and this
    // keeps the notification concern entirely on the listener's side
    // (PlayerNPCPaymentToast).
    public static event System.Action<NPCHiring> OnPaymentDue;

    public void Complete(GameObject player)
    {
        player.GetComponent<NPCHiringScreen>()?.Open(this);
    }

    public bool TryHire(PlayerCurrency wallet)
    {
        if (isHired || wallet == null) return false;
        if (!wallet.Spend(hireCoinType, hireCoinAmount)) return false;

        isHired = true;
        return true;
    }

    // Tools (and the job assignment itself) are lost for good on Fire, not
    // returned to the player -- Ben's explicit call for simplicity
    // (2026-08-10).
    public void Fire()
    {
        isHired = false;
        isWaitingForPayment = false;
        workTimer = 0f;
        unpaidTimer = 0f;
        job.ClearJob();
        // Bail out cleanly rather than leaving a fired NPC stuck mid-walk
        // to a Desk with SetPaused(true) never lifted -- see NPCTraining.
        // CancelTraining's own header comment.
        training?.CancelTraining();
    }

    public bool TryPay(PlayerCurrency wallet)
    {
        if (!isHired || !isWaitingForPayment || wallet == null) return false;
        if (!wallet.Spend(hireCoinType, hireCoinAmount)) return false;

        isWaitingForPayment = false;
        workTimer = 0f;
        unpaidTimer = 0f;
        return true;
    }

    public void Talk() => dialogue.BeginDialogue();
}
