using Mirror;
using UnityEngine;

// Opened by interacting (E) with a hireable NPC -- same shape as
// LockboxScreen (Open(target)/Close()/IsOpen, called from the world
// object's IInteractable.Complete). Chunk 1 of the Hireable NPCs build
// (see BUGS_AND_ENHANCEMENTS.md, 2026-08-10): Hire/Fire/Pay + Talk.
// "Assign Job" (Chunk 2) hands off to NPCJobScreen -- this screen closes
// itself first, same as Talk does, rather than trying to show two modal
// panels at once.
//
// Multiplayer Phase 3 sub-phase 5, 2026-08-23: converted to
// NetworkBehaviour, plus RequestHire/RequestFire/RequestPay Commands.
// NPCHiring itself stays a plain MonoBehaviour -- NPCs moving
// server-side is a whole later phase (MULTIPLAYER_PLANNING.md), out of
// scope here -- but a Command declared on this Player-side
// NetworkBehaviour can still call TryHire/Fire/TryPay on it directly:
// the Command body always executes server-side regardless of which
// object it touches, so this gets real server authority over the
// currency spend + hire state change today, without needing NPCHiring
// converted. The target NPC travels as a NetworkIdentity (NPCFactoryWorker
// already has one from the sub-phase 4 creature/NPC sweep) rather than a
// live component reference.
[RequireComponent(typeof(PlayerCurrency))]
[RequireComponent(typeof(PlayerInventory))]
public class NPCHiringScreen : NetworkBehaviour
{
    private const float PanelWidth = 340f;
    private const float PanelHeight = 460f;

    // Fixed viewport for the Stats/Carrying section (2026-08-10, Chunk 4
    // follow-up -- Ben: "this window may need a scroll bar", confirmed
    // live once a working NPC actually accumulated several ore types at
    // once and the panel ran out of room). Everything above stays fixed
    // (Talk/Hire/Fire buttons); only the part that grows without bound as
    // the NPC mines more item types scrolls.
    private const float StatsViewHeight = 230f;

    private PlayerCurrency wallet;
    private PlayerInventory playerInventory;
    private NPCJobScreen jobScreen;
    private NPCTrainingScreen trainingScreen;
    private PlayerFame fame;
    private NPCHiring current;
    private bool isOpen;
    private Vector2 statsScroll;
    private string rangeText = "";
    private string patrolRangeText = "";

    public bool IsOpen => isOpen;

    private void Awake()
    {
        wallet = GetComponent<PlayerCurrency>();
        playerInventory = GetComponent<PlayerInventory>();
        jobScreen = GetComponent<NPCJobScreen>();
        trainingScreen = GetComponent<NPCTrainingScreen>();
        fame = GetComponent<PlayerFame>();
    }

    // Same "only opens from normal gameplay" rule every other screen
    // follows, so it can't stack on top of one that already has the
    // cursor unlocked.
    public void Open(NPCHiring npc)
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        current = npc;
        // Re-synced from the live component every time this screen opens
        // (rather than kept live-bound), same "parsed on click, not kept
        // as a live-bound number" convention AdminSpawnScreen's own
        // quantity field already uses -- a partially-typed value never
        // blocks typing.
        var gathering = npc.GetComponent<NPCGathering>();
        rangeText = gathering != null ? gathering.MaxRangeFromDeposit.ToString("F0") : "";
        var guarding = npc.GetComponent<NPCGuarding>();
        patrolRangeText = guarding != null ? guarding.PatrolRadius.ToString("F0") : "";
        SetOpen(true);
    }

    // Called by FirstPersonController when Escape re-locks the cursor.
    public void Close() => SetOpen(false);

    private void SetOpen(bool value)
    {
        if (!value) current?.SetMovementPaused(false);

        isOpen = value;
        if (!value) current = null;
        else current.SetMovementPaused(true);

        Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = value;
    }

    private void OnGUI()
    {
        if (!isOpen || current == null) return;

        var rect = new Rect((Screen.width - PanelWidth) / 2f, (Screen.height - PanelHeight) / 2f, PanelWidth, PanelHeight);
        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Label(current.DisplayName, DebugGUI.Header);

        if (GUILayout.Button("Talk"))
        {
            current.Talk();
            SetOpen(false);
            GUILayout.EndArea();
            return;
        }

        if (!current.IsHired)
        {
            GUILayout.Label($"Hire cost: {current.HireCoinAmount} {current.HireCoinType}"
                + $"  (you have {wallet.GetBalance(current.HireCoinType)})", DebugGUI.Label);

            GUI.enabled = wallet.GetBalance(current.HireCoinType) >= current.HireCoinAmount;
            if (GUILayout.Button("Hire"))
            {
                if (isClient && current.TryGetComponent(out NetworkIdentity hireIdentity))
                    RequestHire(hireIdentity);
                else if (current.TryHire(wallet))
                    fame?.GrantHire();
            }
            GUI.enabled = true;
        }
        else
        {
            var assignedJob = current.Job.AssignedJob;
            GUILayout.Label(assignedJob != null ? $"Hired — job: {assignedJob.jobName}" : "Hired — no job assigned", DebugGUI.Label);

            if (GUILayout.Button("Assign Job"))
            {
                var npc = current;
                SetOpen(false);
                jobScreen?.Open(npc);
                GUILayout.EndArea();
                return;
            }

            // Independent of whatever job is assigned (or unassigned) --
            // training is a general NPC action, not job management, so it
            // gets its own top-level button rather than living inside
            // NPCJobScreen (NPC_TRAINING_PLANNING.md section 3.1).
            if (GUILayout.Button("Train"))
            {
                var npc = current;
                SetOpen(false);
                trainingScreen?.Open(npc);
                GUILayout.EndArea();
                return;
            }

            if (current.IsWaitingForPayment)
            {
                GUILayout.Label($"Waiting for payment: {current.HireCoinAmount} {current.HireCoinType}"
                    + $"  (you have {wallet.GetBalance(current.HireCoinType)})", DebugGUI.Label);

                GUI.enabled = wallet.GetBalance(current.HireCoinType) >= current.HireCoinAmount;
                if (GUILayout.Button("Pay"))
                {
                    if (isClient && current.TryGetComponent(out NetworkIdentity payIdentity))
                        RequestPay(payIdentity);
                    else
                        current.TryPay(wallet);
                }
                GUI.enabled = true;
            }
            else if (assignedJob != null && current.Job.IsReady)
            {
                // Chunk 6 (2026-08-10) -- there was previously no way to
                // see how close an NPC was to needing payment.
                GUILayout.Label($"Working — payment due in {current.WorkTimeRemaining:F0}s", DebugGUI.Label);
            }

            if (GUILayout.Button("Fire"))
            {
                if (isClient && current.TryGetComponent(out NetworkIdentity fireIdentity))
                {
                    RequestFire(fireIdentity);
                }
                else
                {
                    current.Fire();
                    fame?.GrantFire();
                }
                SetOpen(false);
                GUILayout.EndArea();
                return;
            }

            // Freeze toggle (2026-08-17, "NPC management") -- a checkbox
            // reads its current state at a glance, same convention
            // FurnaceScreen's own Auto-Run toggle already established,
            // rather than a button whose label you have to read to know
            // whether it's on or off. NPCFreeze is optional (GetComponent,
            // no RequireComponent chain forcing it onto every NPC prefab)
            // so this quietly no-ops for any NPC that doesn't have one yet.
            var freeze = current.GetComponent<NPCFreeze>();
            if (freeze != null)
                freeze.SetFrozen(GUILayout.Toggle(freeze.IsFrozen, "Frozen (stay in place)"));

            // Debug logging toggle (2026-08-21, Ben's ask) -- writes this
            // NPC's target/movement state to DebugLog.FilePath once per
            // second while checked, same shape as the Frozen toggle above.
            // Any number of NPCs can have this on at once ("turn on
            // debugging for all"), all writing to the same shared file.
            var debugJob = current.GetComponent<NPCJob>();
            if (debugJob != null)
                debugJob.DebugEnabled = GUILayout.Toggle(debugJob.DebugEnabled, "Debug logging (writes to debug_log.txt)");

            // Work-range leash (2026-08-17, "NPC management") -- only
            // meaningful for a Gathering NPC (Crafting walks to a fixed
            // bench). Anchored to the NPC's own DepositContainer, not the
            // Flag -- see NPCGathering.MaxRangeFromDeposit's own comment
            // for why.
            //
            // Gated on the NPC's actual ASSIGNED job kind, not just
            // "has an NPCGathering component" -- every NPC prefab carries
            // all three job components at once (they each bail out early
            // if the assigned job isn't their own kind), so a component-
            // presence check alone showed this field for a Guard too,
            // which genuinely misled Ben live (2026-08-17): setting it did
            // nothing, since NPCGathering.Update() itself bails out
            // immediately for a non-Gathering job.
            var gathering = current.GetComponent<NPCGathering>();
            if (gathering != null && assignedJob != null && assignedJob.kind == NPCJobDefinition.JobKind.Gathering)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Work range (from deposit box):", DebugGUI.Label, GUILayout.Width(220));
                rangeText = GUILayout.TextField(rangeText, GUILayout.Width(50));
                if (GUILayout.Button("Set", GUILayout.Width(50))
                    && float.TryParse(rangeText, out var parsed))
                    gathering.MaxRangeFromDeposit = parsed;
                GUILayout.EndHorizontal();
            }

            // Patrol radius leash (2026-08-18) -- same shape as the
            // Gathering leash above, replacing NPCGuarding's original
            // CraftTierScale.VillageFlagRevealRadius(patrolFlag.Tier) reuse
            // (found live: a Masterwork Flag gave every Guard a 75m patrol
            // circle, since that scale was tuned for the Player Map's fog
            // reveal, not a Guard's patrol size). Anchored to the nearest
            // placed Village Flag, same as NPCGuarding.UpdatePatrol already
            // targets.
            var guarding = current.GetComponent<NPCGuarding>();
            if (guarding != null && assignedJob != null && assignedJob.kind == NPCJobDefinition.JobKind.Guarding)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Patrol radius (around Flag):", DebugGUI.Label, GUILayout.Width(220));
                patrolRangeText = GUILayout.TextField(patrolRangeText, GUILayout.Width(50));
                if (GUILayout.Button("Set", GUILayout.Width(50))
                    && float.TryParse(patrolRangeText, out var parsedPatrol))
                    guarding.PatrolRadius = parsedPatrol;
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            DrawStats();
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Close"))
            SetOpen(false);

        GUILayout.EndArea();
    }

    // Chunk 3 (2026-08-10) -- there was previously no way to see an NPC's
    // stats at all. Attribute-category skills (Strength/Dexterity/
    // Constitution/Intelligence) show on the same .25-10 displayed scale
    // the player's own Player tab uses; everything else (Mining) shows its
    // raw 0-100 level, same convention SkillsScreen uses for the player's
    // non-attribute skills. Encumbrance rides alongside Strength, same
    // pairing PlayerMenuScreen's Strength tile already uses.
    private void DrawStats()
    {
        GUILayout.Label("Stats", DebugGUI.Header);

        statsScroll = GUILayout.BeginScrollView(statsScroll, GUILayout.Height(StatsViewHeight));

        foreach (var pair in current.Skills.Levels)
        {
            if (pair.Key == null) continue;

            string display = pair.Key.category == SkillCategory.Attribute
                ? current.Skills.GetAttributeValue(pair.Key).ToString("F2")
                : pair.Value.ToString("F1");
            GUILayout.Label($"{pair.Key.skillName}: {display}", DebugGUI.Label);
        }

        var encumbrance = current.Encumbrance;
        GUILayout.Label($"Encumbrance: {encumbrance.CarriedWeight:F0}/{encumbrance.Capacity:F0} lbs", DebugGUI.Label);

        DrawCargo();

        GUILayout.EndScrollView();
    }

    // Chunk 4 (2026-08-10) -- the NPC's cargo (NPCCargo, what the mining
    // loop has collected but not yet deposited) is the other half of
    // "what is this NPC actually doing," alongside its stats above.
    //
    // "Take" buttons added 2026-08-17 ("NPC management") -- this used to
    // be read-only, which meant an unpaid/fired NPC's cargo was
    // effectively stuck (never actually lost -- Fire()/ClearJob() don't
    // touch NPCCargo -- just unreachable, no player-facing way to get it
    // back). Reuses InventoryTransfer.MoveAsManyAsFit, same utility every
    // other inventory-to-inventory transfer in this project already uses.
    // Since NPCHiringScreen.Open has no proximity check, this works
    // remotely from the Roster too, no need to physically walk to the NPC.
    private void DrawCargo()
    {
        var slots = current.Cargo.Inventory.Slots;
        bool any = false;
        foreach (var slot in new System.Collections.Generic.List<Inventory.Slot>(slots))
        {
            if (slot.item == null) continue;
            if (!any)
            {
                GUILayout.Space(6);
                GUILayout.Label("Carrying", DebugGUI.Header);
                any = true;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{slot.item.itemName} x{slot.count}", DebugGUI.Label, GUILayout.Width(200));
            if (GUILayout.Button("Take", GUILayout.Width(70)))
                InventoryTransfer.MoveAsManyAsFit(current.Cargo.Inventory, playerInventory.Inventory, slot.item);
            GUILayout.EndHorizontal();
        }

        if (any && GUILayout.Button("Take All", GUILayout.Width(100)))
        {
            foreach (var slot in new System.Collections.Generic.List<Inventory.Slot>(current.Cargo.Inventory.Slots))
                if (slot.item != null)
                    InventoryTransfer.MoveAsManyAsFit(current.Cargo.Inventory, playerInventory.Inventory, slot.item);
        }
    }

    public void RequestHire(NetworkIdentity npcIdentity) => CmdHire(npcIdentity);

    [Command]
    private void CmdHire(NetworkIdentity npcIdentity)
    {
        var npc = npcIdentity != null ? npcIdentity.GetComponent<NPCHiring>() : null;
        if (npc != null && npc.TryHire(wallet))
            fame?.GrantHire();
    }

    public void RequestFire(NetworkIdentity npcIdentity) => CmdFire(npcIdentity);

    [Command]
    private void CmdFire(NetworkIdentity npcIdentity)
    {
        var npc = npcIdentity != null ? npcIdentity.GetComponent<NPCHiring>() : null;
        if (npc == null) return;

        npc.Fire();
        fame?.GrantFire();
    }

    public void RequestPay(NetworkIdentity npcIdentity) => CmdPay(npcIdentity);

    [Command]
    private void CmdPay(NetworkIdentity npcIdentity)
    {
        var npc = npcIdentity != null ? npcIdentity.GetComponent<NPCHiring>() : null;
        npc?.TryPay(wallet);
    }
}
