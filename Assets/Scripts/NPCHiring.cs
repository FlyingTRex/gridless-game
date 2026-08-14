using UnityEngine;

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
public class NPCHiring : MonoBehaviour, IInteractable
{
    [SerializeField] private CoinType hireCoinType = CoinType.Copper;
    [SerializeField] private int hireCoinAmount = 10;

    // Only counts real-world time spent actually working (NPCJob.IsReady)
    // -- an NPC sitting unassigned or unequipped isn't "working," so its
    // clock shouldn't run down either. Matches Ben's own framing ("the
    // npc works for 5 real days, and then waits for payment").
    [SerializeField] private float workDurationSeconds = 300f;

    private NPCDialogue dialogue;
    private NPCJob job;
    private NPCSkills skills;
    private NPCEncumbrance encumbrance;
    private NPCCargo cargo;
    private bool isHired;
    private bool isWaitingForPayment;
    private float workTimer;

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
    }

    private void Update()
    {
        if (!isHired || isWaitingForPayment || !job.IsReady) return;

        workTimer += Time.deltaTime;
        if (workTimer < workDurationSeconds) return;

        isWaitingForPayment = true;
        workTimer = 0f;
    }

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
        job.ClearJob();
    }

    public bool TryPay(PlayerCurrency wallet)
    {
        if (!isHired || !isWaitingForPayment || wallet == null) return false;
        if (!wallet.Spend(hireCoinType, hireCoinAmount)) return false;

        isWaitingForPayment = false;
        workTimer = 0f;
        return true;
    }

    public void Talk() => dialogue.BeginDialogue();
}
