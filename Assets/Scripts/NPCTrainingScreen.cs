using System.Collections.Generic;
using UnityEngine;

// Opened from NPCHiringScreen's "Train" button (2026-08-16,
// NPC_TRAINING_PLANNING.md section 3.1). Book picker reads from two pools
// -- the player's own main inventory, and every nearby StorageBox's
// contents (a Bookshelf is just a StorageBox restricted to skill books;
// scanning every nearby box rather than gating on that flag means a book
// left in an ordinary box still counts too, matching the design's own
// "shelving first isn't required, the shelf is just wherever spare books
// happen to live" framing).
[RequireComponent(typeof(PlayerInventory))]
public class NPCTrainingScreen : MonoBehaviour
{
    private const float PanelWidth = 460f;
    private const float PanelHeight = 420f;

    // Same "within 10m" convention PlayerCrafting.storageRange/Furnace.
    // storageLinkRange already use for "how far can this reach for
    // materials."
    private const float BoxSearchRange = 10f;

    private PlayerInventory playerInventory;
    private NPCHiring current;
    private NPCTraining training;
    private bool isOpen;
    private Vector2 scrollPos;
    private readonly List<StorageBox> nearbyBoxes = new List<StorageBox>();

    public bool IsOpen => isOpen;

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
    }

    public void Open(NPCHiring npc)
    {
        if (Cursor.lockState != CursorLockMode.Locked || npc == null) return;

        training = npc.GetComponent<NPCTraining>();
        if (training == null) return;

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
            training = null;
        }
        Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = value;
    }

    private void OnGUI()
    {
        if (!isOpen || current == null || training == null) return;

        var rect = new Rect((Screen.width - PanelWidth) / 2f, (Screen.height - PanelHeight) / 2f, PanelWidth, PanelHeight);
        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);

        GUILayout.Label($"Train — {current.DisplayName}", DebugGUI.Header);

        if (training.IsTraining)
        {
            float progress = Mathf.Clamp01(training.TrainSecondsElapsed / training.TrainDurationSeconds);
            GUILayout.Label($"Studying at the Desk — {Mathf.RoundToInt(progress * 100f)}%", DebugGUI.Label);
        }
        else
        {
            GUILayout.Label("Pick a book below — it's consumed immediately, and the NPC "
                + "walks to the nearest Desk to study for 2 minutes.", DebugGUI.Label);
        }

        GUILayout.Space(6);
        scrollPos = GUILayout.BeginScrollView(scrollPos);
        DrawBookList();
        GUILayout.EndScrollView();

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Close", GUILayout.Width(100)))
            SetOpen(false);

        GUILayout.EndArea();
    }

    private void DrawBookList()
    {
        bool any = false;

        foreach (var slot in playerInventory.Inventory.Slots)
            if (slot.equipment is SkillBook book)
            {
                DrawBookRow(book, playerInventory.Inventory, "Inventory");
                any = true;
            }

        StorageBox.FindNearby(transform.position, BoxSearchRange, nearbyBoxes);
        foreach (var box in nearbyBoxes)
            foreach (var slot in box.Inventory.Slots)
                if (slot.equipment is SkillBook book)
                {
                    DrawBookRow(book, box.Inventory, box.DisplayName);
                    any = true;
                }

        if (!any)
            GUILayout.Label("No skill books found in your inventory or nearby.", DebugGUI.Label);
    }

    private void DrawBookRow(SkillBook book, Inventory source, string sourceLabel)
    {
        GUILayout.BeginHorizontal();

        string target = book.TargetRecipe != null
            ? (book.TargetRecipe.outputItem != null ? book.TargetRecipe.outputItem.itemName : "(recipe)")
            : (book.TargetWish != null ? book.TargetWish.wishName : "(unknown)");
        GUILayout.Label($"{book.DisplayName} — {target} [{sourceLabel}]", DebugGUI.Label, GUILayout.Width(300));

        bool alreadyKnown = !training.CanTrainWith(book);
        GUI.enabled = !training.IsTraining && !alreadyKnown;
        if (GUILayout.Button("Train", GUILayout.Width(80)))
        {
            if (!training.TryBeginTraining(book, source, out var failReason) && failReason != null)
                Debug.Log($"[NPCTrainingScreen] Could not start training: {failReason}");
        }
        GUI.enabled = true;

        if (alreadyKnown)
            GUILayout.Label("(already known)", DebugGUI.Label, GUILayout.Width(110));

        GUILayout.EndHorizontal();
    }
}
