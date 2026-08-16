using UnityEngine;

// Opened from NPCJobScreen's "Manage Crafting Queue" button once a
// Crafting-kind job is assigned (2026-08-16, NPC_JOB_GENERALIZATION_
// PLANNING.md section 7.5) -- same "Campfire got its own popup instead of
// overloading Inventory" precedent as every other structure-specific
// screen in this project. Recipe list is family-scoped to the assigned
// job's own family (the same grouping CraftingScreen/NPCJobScreen already
// use), read directly off PlayerCrafting.Recipes rather than a new
// per-job recipe-list asset -- discovery is family-scoped, selection is
// the queue itself.
[RequireComponent(typeof(PlayerCrafting))]
[RequireComponent(typeof(PlayerNPCDeposit))]
public class NPCCraftingScreen : MonoBehaviour
{
    private const float PanelWidth = 480f;
    private const float PanelHeight = 460f;

    private PlayerCrafting playerCrafting;
    private PlayerNPCDeposit deposit;
    private NPCHiring current;
    private NPCCrafting crafting;
    private bool isOpen;
    private Vector2 scrollPos;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        playerCrafting = GetComponent<PlayerCrafting>();
        deposit = GetComponent<PlayerNPCDeposit>();
    }

    public void Open(NPCHiring npc)
    {
        if (Cursor.lockState != CursorLockMode.Locked || npc == null) return;

        crafting = npc.GetComponent<NPCCrafting>();
        if (crafting == null) return;

        current = npc;
        SetOpen(true);
    }

    public void Close() => SetOpen(false);

    private void SetOpen(bool value)
    {
        isOpen = value;
        if (!value)
        {
            current = null;
            crafting = null;
        }
        Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = value;
    }

    private void OnGUI()
    {
        if (!isOpen || current == null || crafting == null) return;

        var rect = new Rect((Screen.width - PanelWidth) / 2f, (Screen.height - PanelHeight) / 2f, PanelWidth, PanelHeight);
        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);

        GUILayout.Label($"Crafting Queue — {current.DisplayName}", DebugGUI.Header);

        if (crafting.ActiveRecipe != null)
        {
            float progress = Mathf.Clamp01(crafting.CraftSecondsElapsed / crafting.CraftDurationSeconds);
            GUILayout.Label($"Crafting {crafting.ActiveRecipe.outputItem.itemName} — {Mathf.RoundToInt(progress * 100f)}%", DebugGUI.Label);
        }
        else
        {
            GUILayout.Label("Idle — nothing queued is currently craftable.", DebugGUI.Label);
        }

        GUILayout.Space(6);
        DrawBoxAssignment();

        GUILayout.Space(6);
        scrollPos = GUILayout.BeginScrollView(scrollPos);
        DrawRecipeList();
        GUILayout.EndScrollView();

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Close", GUILayout.Width(100)))
            SetOpen(false);

        GUILayout.EndArea();
    }

    private void DrawBoxAssignment()
    {
        DrawBoxRow("Materials Source", crafting.MaterialsSourceBox, crafting.SetMaterialsSourceBox);
        DrawBoxRow("Output Box", crafting.OutputBox, crafting.SetOutputBox);
    }

    private void DrawBoxRow(string label, StorageBox assigned, System.Action<StorageBox> assign)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: {(assigned != null ? assigned.DisplayName : "not set")}", DebugGUI.Label, GUILayout.Width(260));

        if (GUILayout.Button("Set", GUILayout.Width(60)))
        {
            SetOpen(false);
            deposit.BeginTargeting(assign);
        }
        if (assigned != null && GUILayout.Button("Clear", GUILayout.Width(60)))
            assign(null);

        GUILayout.EndHorizontal();
    }

    // Family-scoped: only recipes whose trainedSkill matches the assigned
    // job's family are offerable to queue at all -- see this class's own
    // header comment.
    private void DrawRecipeList()
    {
        var family = current.Job.AssignedJob != null ? current.Job.AssignedJob.family : null;
        var recipes = playerCrafting.Recipes;

        bool any = false;
        if (recipes != null)
        {
            foreach (var recipe in recipes)
            {
                if (recipe == null || recipe.outputItem == null || recipe.trainedSkill != family) continue;
                any = true;
                DrawRecipeRow(recipe);
            }
        }

        if (!any)
            GUILayout.Label("No recipes in this family yet.", DebugGUI.Label);
    }

    private void DrawRecipeRow(CraftingRecipe recipe)
    {
        bool queued = crafting.IsQueued(recipe);
        bool full = !queued && crafting.RecipeQueue.Count >= NPCCrafting.MaxQueueSize;
        bool satisfiable = crafting.IsSatisfiable(recipe);

        GUILayout.BeginHorizontal();

        GUI.enabled = !full;
        string label = $"{(queued ? "[Queued] " : "")}{recipe.outputItem.itemName} x{recipe.outputCount}";
        if (GUILayout.Button(label, GUILayout.Width(260)))
            crafting.ToggleQueue(recipe);
        GUI.enabled = true;

        // Why an NPC sitting idle can't actually run this recipe right
        // now -- materials/tool/skill/space, same four-way check
        // NPCCrafting.IsSatisfiable itself uses, so nothing shown here can
        // be wrong relative to what the loop actually does.
        GUILayout.Label(satisfiable ? "Ready" : "Not ready", satisfiable ? DebugGUI.Label : DebugGUI.Warning);

        GUILayout.EndHorizontal();
    }
}
