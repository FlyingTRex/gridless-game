using UnityEngine;

// Opened from NPCHiringScreen's "Assign Job" button once an NPC is hired.
// Chunk 2 of the Hireable NPCs build (see BUGS_AND_ENHANCEMENTS.md,
// 2026-08-10): family tabs -> job tiles, same two-step shape as
// CraftingScreen's discipline tabs -> recipe tiles, since Ben's own
// design explicitly modeled this on the Crafting menu ("first you pick
// the family... once you click the family, it offers up the tiers").
// `families`/`jobs` are manually-wired arrays, same convention as
// CraftingScreen.disciplines/PlayerCrafting.recipes -- only one family
// (Mining) and one job (Mine Ore) exist today.
[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerNPCDeposit))]
public class NPCJobScreen : MonoBehaviour
{
    private const float PanelWidth = 480f;
    private const float PanelHeight = 420f;
    private const float TabWidth = 130f;
    private const float TabHeight = 28f;

    [SerializeField] private SkillDefinition[] families;
    [SerializeField] private NPCJobDefinition[] jobs;

    private PlayerInventory playerInventory;
    private PlayerNPCDeposit deposit;
    private NPCHiring current;
    private bool isOpen;
    private int currentFamilyIndex;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
        deposit = GetComponent<PlayerNPCDeposit>();
    }

    public void Open(NPCHiring npc)
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        current = npc;
        SetOpen(true);
    }

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
        GUILayout.Label($"Assign Job — {current.DisplayName}", DebugGUI.Header);

        DrawFamilyTabs();
        GUILayout.Space(6);

        var job = current.Job;
        SkillDefinition wantFamily = families != null && currentFamilyIndex >= 0 && currentFamilyIndex < families.Length
            ? families[currentFamilyIndex]
            : null;

        bool any = false;
        bool anyLocked = false;
        if (jobs != null)
        {
            foreach (var def in jobs)
            {
                if (def == null || def.family != wantFamily) continue;

                bool isAssigned = job.AssignedJob == def;
                if (!isAssigned && !MeetsSkillRequirement(def))
                {
                    anyLocked = true;
                    continue;
                }

                any = true;
                DrawJobRow(def, job);
            }
        }

        if (!any)
            GUILayout.Label(anyLocked ? "No jobs unlocked at this NPC's current skill yet." : "No jobs in this family yet.", DebugGUI.Label);

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Close", GUILayout.Width(100)))
            SetOpen(false);

        GUILayout.EndArea();
    }

    private void DrawFamilyTabs()
    {
        GUILayout.BeginHorizontal();
        if (families != null)
        {
            for (int i = 0; i < families.Length; i++)
            {
                if (families[i] == null) continue;
                var style = currentFamilyIndex == i ? DebugGUI.TabSelected : DebugGUI.TabUnselected;
                if (GUILayout.Button(families[i].skillName, style, GUILayout.Width(TabWidth), GUILayout.Height(TabHeight)))
                    currentFamilyIndex = i;
            }
        }
        GUILayout.EndHorizontal();
    }

    private void DrawJobRow(NPCJobDefinition def, NPCJob job)
    {
        bool isAssigned = job.AssignedJob == def;

        GUILayout.BeginVertical(DebugGUI.Panel);
        GUILayout.Label(isAssigned ? $"{def.jobName} (assigned)" : def.jobName, DebugGUI.Header);

        if (!isAssigned)
        {
            string warning = job.AssignedJob != null
                ? "Assigning this will lose all tools currently equipped for its other job."
                : null;
            if (warning != null)
                GUILayout.Label(warning, DebugGUI.Warning);

            if (GUILayout.Button("Assign", GUILayout.Width(90)))
                job.Assign(def);
        }
        else
        {
            DrawToolRequirements(def, job);
            DrawDepositContainer(job);
        }

        GUILayout.EndVertical();
        GUILayout.Space(8);
    }

    // Chunk 5 (2026-08-10): where NPCGathering walks the mined ore back to
    // once full. Closes this whole screen and hands off to
    // PlayerNPCDeposit's point-and-confirm targeting, same one-modal-at-
    // a-time handoff NPCHiringScreen's own "Assign Job" button already
    // uses -- targeting needs the cursor locked to aim, which a
    // GUILayout-driven menu can't do at the same time.
    private void DrawDepositContainer(NPCJob job)
    {
        GUILayout.Space(6);
        var box = job.DepositContainer;
        GUILayout.Label(box != null ? $"Deposit point: {box.DisplayName}" : "Deposit point: not set", DebugGUI.Label);

        if (GUILayout.Button("Set Deposit Container", GUILayout.Width(180)))
        {
            SetOpen(false);
            deposit.BeginTargeting(job);
        }
    }

    private void DrawToolRequirements(NPCJobDefinition def, NPCJob job)
    {
        if (def.toolRequirements == null) return;

        foreach (var req in def.toolRequirements)
        {
            if (req == null) continue;
            var equipped = job.GetEquipped(req.label);

            GUILayout.BeginHorizontal();
            GUILayout.Label(equipped != null ? $"{req.label}: {equipped.itemName}" : $"{req.label}: —",
                equipped != null ? DebugGUI.Label : DebugGUI.Warning, GUILayout.Width(260));

            if (equipped == null)
            {
                bool canGive = HasAny(req);
                GUI.enabled = canGive;
                if (GUILayout.Button("Give", GUILayout.Width(80)))
                    job.TryGiveTool(req, playerInventory);
                GUI.enabled = true;
                if (!canGive)
                    GUILayout.Label("(none in inventory)", DebugGUI.Warning);
            }
            GUILayout.EndHorizontal();
        }
    }

    // Chunk 3 (2026-08-10): job tiers actually gate on the NPC's own skill
    // now, instead of always showing regardless. Reuses CraftTierScale's
    // existing tier->required-level curve (0/10/25/50/100) rather than
    // inventing a second one -- job tier 1 maps to CraftTier.Crude (0),
    // tier 2 to Rudimentary (10), and so on, so "tier" paces the same way
    // crafting quality already does.
    private bool MeetsSkillRequirement(NPCJobDefinition def)
    {
        var craftTier = (CraftTier)Mathf.Clamp(def.tier - 1, 0, 4);
        float required = CraftTierScale.SkillRequirement(craftTier);
        return current.Skills.GetLevel(def.family) >= required;
    }

    private bool HasAny(ToolRequirement req)
    {
        if (req.acceptableItems == null) return false;
        foreach (var item in req.acceptableItems)
            if (item != null && playerInventory.GetCount(item) > 0) return true;
        return false;
    }
}
