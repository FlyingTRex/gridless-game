using UnityEngine;

// Opened by interacting (E) with a hireable NPC -- same shape as
// LockboxScreen (Open(target)/Close()/IsOpen, called from the world
// object's IInteractable.Complete). Chunk 1 of the Hireable NPCs build
// (see BUGS_AND_ENHANCEMENTS.md, 2026-08-10): Hire/Fire/Pay + Talk.
// "Assign Job" (Chunk 2) hands off to NPCJobScreen -- this screen closes
// itself first, same as Talk does, rather than trying to show two modal
// panels at once.
[RequireComponent(typeof(PlayerCurrency))]
public class NPCHiringScreen : MonoBehaviour
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
    private NPCJobScreen jobScreen;
    private NPCHiring current;
    private bool isOpen;
    private Vector2 statsScroll;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        wallet = GetComponent<PlayerCurrency>();
        jobScreen = GetComponent<NPCJobScreen>();
    }

    // Same "only opens from normal gameplay" rule every other screen
    // follows, so it can't stack on top of one that already has the
    // cursor unlocked.
    public void Open(NPCHiring npc)
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        current = npc;
        SetOpen(true);
    }

    // Called by FirstPersonController when Escape re-locks the cursor.
    public void Close() => SetOpen(false);

    private void SetOpen(bool value)
    {
        isOpen = value;
        if (!value) current = null;
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
                current.TryHire(wallet);
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

            if (current.IsWaitingForPayment)
            {
                GUILayout.Label($"Waiting for payment: {current.HireCoinAmount} {current.HireCoinType}"
                    + $"  (you have {wallet.GetBalance(current.HireCoinType)})", DebugGUI.Label);

                GUI.enabled = wallet.GetBalance(current.HireCoinType) >= current.HireCoinAmount;
                if (GUILayout.Button("Pay"))
                    current.TryPay(wallet);
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
                current.Fire();
                SetOpen(false);
                GUILayout.EndArea();
                return;
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
    private void DrawCargo()
    {
        var slots = current.Cargo.Inventory.Slots;
        bool any = false;
        foreach (var slot in slots)
        {
            if (slot.item == null) continue;
            if (!any)
            {
                GUILayout.Space(6);
                GUILayout.Label("Carrying", DebugGUI.Header);
                any = true;
            }
            GUILayout.Label($"{slot.item.itemName} x{slot.count}", DebugGUI.Label);
        }
    }
}
