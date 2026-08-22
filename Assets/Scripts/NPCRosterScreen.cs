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
[RequireComponent(typeof(PlayerCurrency))]
[RequireComponent(typeof(PlayerInteraction))]
public class NPCRosterScreen : MonoBehaviour
{
    private Vector2 scrollPos;
    private bool isOpen;
    private NPCHiringScreen hiringScreen;
    private PlayerCurrency wallet;
    private Transform playerTransform;
    private Camera playerCamera;
    private NPCHiring tracked;
    private VillageFlagSpawner villageFlagSpawner;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        hiringScreen = GetComponent<NPCHiringScreen>();
        wallet = GetComponent<PlayerCurrency>();
        playerTransform = transform;
        playerCamera = GetComponent<PlayerInteraction>().PlayerCamera;
        villageFlagSpawner = GetComponent<VillageFlagSpawner>();
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
        // Drawn regardless of whether the Roster itself is open (2026-08-17,
        // "NPC management" -- a waypoint compass toward a Roster-tracked
        // NPC needs to work during normal gameplay, not just while this
        // panel is up). Auto-clears once the tracked NPC no longer exists
        // -- Unity's overridden == null check on a destroyed Object handles
        // that for free, no explicit cleanup needed.
        DrawWaypoint();

        if (!isOpen) return;

        var screenRect = new Rect(0, 0, Screen.width, Screen.height);
        DebugGUI.DrawPanel(screenRect);
        GUILayout.BeginArea(screenRect);

        GUILayout.Space(20);
        GUILayout.Label("NPC Roster", DebugGUI.Header);
        GUILayout.Label(NextVisitLabel(), DebugGUI.Label);
        GUILayout.Space(6);

        var npcs = FindObjectsByType<NPCHiring>(FindObjectsSortMode.None)
            .OrderBy(n => n.GetComponent<NPCDialogue>()?.DisplayName ?? "")
            .ToList();

        // "Needs attention" count + Pay All (2026-08-17, "NPC management"
        // follow-up) -- with several NPCs running at once, walking the
        // list to pay each one individually is exactly the friction this
        // whole screen exists to remove. Skips (doesn't stop at) any NPC
        // whose coin type you can't currently afford, same "best effort,
        // no all-or-nothing" convention as everything else that iterates
        // and transfers in this project.
        var waiting = npcs.Where(n => n.IsHired && n.IsWaitingForPayment).ToList();
        if (waiting.Count > 0)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{waiting.Count} NPC{(waiting.Count == 1 ? "" : "s")} waiting for payment", DebugGUI.Warning, GUILayout.Width(260));
            if (GUILayout.Button("Pay All", GUILayout.Width(100)))
                foreach (var npc in waiting)
                    npc.TryPay(wallet);
            GUILayout.EndHorizontal();
            GUILayout.Space(6);
        }

        if (npcs.Count == 0)
        {
            GUILayout.Label("No NPCs in the world right now.", DebugGUI.Label);
        }
        else
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(Screen.height - 200f));
            foreach (var npc in npcs)
                DrawRow(npc);
            GUILayout.EndScrollView();
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Close", GUILayout.Width(100)))
            SetOpen(false);

        GUILayout.EndArea();
    }

    // Next Village Flag visit countdown (2026-08-21, Ben's ask). No Flag
    // placed yet reads as its own message rather than "0:00" -- the timer
    // genuinely isn't running in that case (VillageFlagSpawner.Update's
    // own early-out), not just imminent.
    private string NextVisitLabel()
    {
        var seconds = villageFlagSpawner?.SecondsUntilNextSpawn();
        if (seconds == null) return "Next NPC visit: no Village Flag placed";

        int total = Mathf.CeilToInt(seconds.Value);
        return $"Next NPC visit: {total / 60}m {total % 60}s";
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

        bool isTracked = tracked == npc;
        if (GUILayout.Button(isTracked ? "Stop" : "Locate", GUILayout.Width(70)))
            tracked = isTracked ? null : npc;

        GUILayout.EndHorizontal();
    }

    // Compass arrow near top-center of the screen, rotated to point toward
    // the tracked NPC's current horizontal direction relative to where the
    // player is looking -- same purpose as a quest-marker in other games,
    // for the "I do want to actually walk over there" case (job
    // reassignment, checking on someone in person) that Take-cargo/Freeze
    // don't already cover remotely.
    private void DrawWaypoint()
    {
        if (tracked == null || playerCamera == null) return;

        Vector3 toTarget = tracked.transform.position - playerCamera.transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.01f) return;

        Vector3 forward = playerCamera.transform.forward;
        forward.y = 0f;
        forward.Normalize();
        float angle = Vector3.SignedAngle(forward, toTarget.normalized, Vector3.up);

        var arrowRect = new Rect(Screen.width / 2f - 15f, 10f, 30f, 30f);
        var arrowStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.MiddleCenter };

        var prevMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, arrowRect.center);
        GUI.Label(arrowRect, "▲", arrowStyle);
        GUI.matrix = prevMatrix;

        string name = tracked.GetComponent<NPCDialogue>()?.DisplayName ?? "NPC";
        float dist = Vector3.Distance(playerCamera.transform.position, tracked.transform.position);
        var labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(Screen.width / 2f - 80f, 42f, 160f, 20f), $"{name} ({dist:F0}m)", labelStyle);
    }
}
