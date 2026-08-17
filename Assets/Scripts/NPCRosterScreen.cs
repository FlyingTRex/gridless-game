using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

// NPC roster (2026-08-17, BUGS_AND_ENHANCEMENTS.md "NPC identification")
// -- N toggles it, same self-contained open/close/cursor-lock shape
// MapScreen's own M toggle already established. Built after live testing
// confirmed the real need: diagnosing several different NPCs (a
// wandering Miner, a frozen Guard) one at a time by physically walking
// to and inspecting each. Lists every NPCHiring in the scene with a
// live status readout; "Manage" opens the exact same NPCHiringScreen a
// walk-up-and-press-E interaction would, so this is a faster way to
// reach an NPC's existing menu, not a second copy of it.
[RequireComponent(typeof(NPCHiringScreen))]
public class NPCRosterScreen : MonoBehaviour
{
    private Vector2 scrollPos;
    private bool isOpen;
    private NPCHiringScreen hiringScreen;
    private Transform playerTransform;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        hiringScreen = GetComponent<NPCHiringScreen>();
        playerTransform = transform;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.nKey.wasPressedThisFrame) return;

        // Same "only open from normal gameplay, always allow closing"
        // guard MapScreen/GameMenuScreen's own toggles already use.
        if (isOpen || Cursor.lockState == CursorLockMode.Locked)
            SetOpen(!isOpen);
    }

    // Called by FirstPersonController when Escape re-locks the cursor,
    // same as every other screen it tracks.
    public void Close() => SetOpen(false);

    private void SetOpen(bool value)
    {
        isOpen = value;
        Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = value;
    }

    private void OnGUI()
    {
        if (!isOpen) return;

        var screenRect = new Rect(0, 0, Screen.width, Screen.height);
        DebugGUI.DrawPanel(screenRect);
        GUILayout.BeginArea(screenRect);

        GUILayout.Space(20);
        GUILayout.Label("NPC Roster", DebugGUI.Header);
        GUILayout.Space(6);

        var npcs = FindObjectsByType<NPCHiring>(FindObjectsSortMode.None)
            .OrderBy(n => n.GetComponent<NPCDialogue>()?.DisplayName ?? "")
            .ToList();

        if (npcs.Count == 0)
        {
            GUILayout.Label("No NPCs in the world right now.", DebugGUI.Label);
        }
        else
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(Screen.height - 160f));
            foreach (var npc in npcs)
                DrawRow(npc);
            GUILayout.EndScrollView();
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Close", GUILayout.Width(100)))
            SetOpen(false);

        GUILayout.EndArea();
    }

    private void DrawRow(NPCHiring npc)
    {
        var dialogue = npc.GetComponent<NPCDialogue>();
        string name = dialogue != null ? dialogue.DisplayName : "Unknown";
        string job = npc.Job.AssignedJob != null ? npc.Job.AssignedJob.jobName : "Unassigned";
        string status = !npc.IsHired ? "Not hired"
            : npc.IsWaitingForPayment ? "Waiting for payment"
            : npc.Job.IsReady ? "Working"
            : "Idle (missing tools)";
        float distance = Vector3.Distance(playerTransform.position, npc.transform.position);

        GUILayout.BeginHorizontal();
        GUILayout.Label(name, DebugGUI.Label, GUILayout.Width(140));
        GUILayout.Label(job, DebugGUI.Label, GUILayout.Width(150));
        GUILayout.Label(status, status == "Working" ? DebugGUI.Label : DebugGUI.Warning, GUILayout.Width(160));
        GUILayout.Label($"{distance:F0}m", DebugGUI.Label, GUILayout.Width(60));
        if (GUILayout.Button("Manage", GUILayout.Width(80)))
        {
            SetOpen(false);
            hiringScreen?.Open(npc);
        }
        GUILayout.EndHorizontal();
    }
}
