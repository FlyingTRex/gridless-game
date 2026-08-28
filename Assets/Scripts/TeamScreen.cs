using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

// Team management UI -- MVP4_PLANNING.md's resolved design (built
// 2026-08-28). T toggles it, same open/close/cursor-lock shape every
// other screen in this project already uses (MapScreen's M toggle is the
// closest direct precedent, copied here).
[RequireComponent(typeof(PlayerTeam))]
[RequireComponent(typeof(PlayerIdentity))]
public class TeamScreen : MonoBehaviour
{
    private const float NearbyScanRadius = 15f;
    private const float PanelWidth = 480f;

    private PlayerTeam myTeam;
    private PlayerIdentity myIdentity;
    private NetworkIdentity netIdentity;
    private bool isOpen;
    private string createNameField = "";

    public bool IsOpen => isOpen;

    private void Awake()
    {
        myTeam = GetComponent<PlayerTeam>();
        myIdentity = GetComponent<PlayerIdentity>();
        netIdentity = GetComponent<NetworkIdentity>();
    }

    private void Update()
    {
        if (netIdentity != null && !netIdentity.isLocalPlayer) return;
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.tKey.wasPressedThisFrame) return;

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

    // Same "only the local player's own copy draws anything" gate every
    // other per-player screen in this project uses.
    private void OnGUI()
    {
        if (netIdentity != null && !netIdentity.isLocalPlayer) return;
        if (!isOpen) return;

        var rect = new Rect((Screen.width - PanelWidth) / 2f, 60f, PanelWidth, Screen.height - 120f);
        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);

        if (myTeam.IsInTeam)
            DrawInTeam();
        else
            DrawNoTeam();

        if (LeftClickButton("Close"))
            Close();

        GUILayout.EndArea();
    }

    // GUILayout.Button fires on ANY mouse button, not just left-click
    // (this project's own established gotcha -- see CLAUDE.md) -- every
    // button here changes real state (kick/disband/promote/invite), so
    // all of them go through this instead of a raw GUILayout.Button.
    private static bool LeftClickButton(string label, params GUILayoutOption[] options)
    {
        bool clicked = GUILayout.Button(label, options);
        return clicked && Event.current.button == 0;
    }

    private void DrawNoTeam()
    {
        GUILayout.Label("Team", DebugGUI.Header);
        GUILayout.Space(8);

        GUILayout.Label("Not currently on a team.", DebugGUI.Label);
        GUILayout.BeginHorizontal();
        createNameField = GUILayout.TextField(createNameField, 30);
        if (LeftClickButton("Create Team", GUILayout.Width(110)) && !string.IsNullOrWhiteSpace(createNameField))
            myTeam.CmdCreateTeam(createNameField);
        GUILayout.EndHorizontal();

        GUILayout.Space(12);

        if (myTeam.HasPendingInvite)
        {
            GUILayout.Label("Invitation", DebugGUI.Header);
            GUILayout.Label($"{myTeam.PendingInviteFromName} invited you to join " +
                             $"\"{myTeam.PendingInviteTeamName}\".", DebugGUI.Label);
            GUILayout.BeginHorizontal();
            if (LeftClickButton("Join Team", GUILayout.Width(100)))
                myTeam.CmdAcceptInvite();
            if (LeftClickButton("Decline", GUILayout.Width(100)))
                myTeam.CmdDeclineInvite();
            GUILayout.EndHorizontal();
        }
    }

    private void DrawInTeam()
    {
        GUILayout.Label($"Team: {myTeam.TeamName}", DebugGUI.Header);
        GUILayout.Label($"Your role: {myTeam.Role}", DebugGUI.Label);
        GUILayout.Space(8);

        bool canManage = myTeam.Role != TeamRole.Member;
        var members = PlayerTeam.MembersOf(myTeam.TeamId)
            .OrderByDescending(m => m.Role)
            .ToList();

        GUILayout.Label($"Roster ({members.Count}/{PlayerTeam.MaxMembers})", DebugGUI.Header);
        for (int i = 0; i < PlayerTeam.MaxMembers; i++)
        {
            GUILayout.BeginHorizontal();
            if (i < members.Count)
            {
                var member = members[i];
                var memberIdentity = member.GetComponent<PlayerIdentity>();
                string name = memberIdentity != null ? memberIdentity.DisplayName : "?";
                GUILayout.Label($"{name} — {member.Role}", DebugGUI.Label, GUILayout.Width(240));

                bool isSelf = member == myTeam;
                var memberNetId = member.GetComponent<NetworkIdentity>();
                if (!isSelf && canManage && memberNetId != null)
                {
                    // Officers can only act on plain Members; the Owner
                    // can act on anyone but themselves. Matches
                    // PlayerTeam.CmdKick's own server-side enforcement --
                    // this is just keeping the UI honest about what will
                    // actually succeed, not the real gate.
                    bool canActOnThisRow = myTeam.Role == TeamRole.Owner || member.Role == TeamRole.Member;
                    if (canActOnThisRow)
                    {
                        if (LeftClickButton("Kick", GUILayout.Width(60)))
                            myTeam.CmdKick(memberNetId);
                        if (member.Role == TeamRole.Member && myTeam.Role == TeamRole.Owner &&
                            LeftClickButton("-> Officer", GUILayout.Width(80)))
                            myTeam.CmdPromoteToOfficer(memberNetId);
                        if (myTeam.Role == TeamRole.Owner &&
                            LeftClickButton("-> Owner", GUILayout.Width(80)))
                            myTeam.CmdPromoteToOwner(memberNetId);
                    }
                }
            }
            else
            {
                GUILayout.Label("(empty)", DebugGUI.Label);
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(12);

        if (myTeam.Role == TeamRole.Owner)
        {
            if (LeftClickButton("Disband Team", GUILayout.Width(120)))
                myTeam.CmdDisbandTeam();
        }
        else
        {
            if (LeftClickButton("Leave Team", GUILayout.Width(120)))
                myTeam.CmdLeaveTeam();
        }

        GUILayout.Space(12);

        if (canManage)
            DrawNearbyInvite(members);
    }

    // Bottom section -- a simple radius scan around the local player
    // (NOT scoped to team territory, per the resolved design: a freshly
    // created team has zero territory until someone places a Flag, and
    // gating invites on that would leave it with nobody to invite).
    private void DrawNearbyInvite(List<PlayerTeam> currentMembers)
    {
        GUILayout.Label("Nearby players", DebugGUI.Header);

        var nearby = FindObjectsByType<PlayerTeam>(FindObjectsSortMode.None)
            .Where(pt => !pt.IsInTeam)
            .Where(pt => Vector3.Distance(pt.transform.position, transform.position) <= NearbyScanRadius)
            .ToList();

        if (nearby.Count == 0)
        {
            GUILayout.Label("(none in range)", DebugGUI.Label);
            return;
        }

        bool roomLeft = currentMembers.Count < PlayerTeam.MaxMembers;
        foreach (var candidate in nearby)
        {
            var candidateIdentity = candidate.GetComponent<PlayerIdentity>();
            var candidateNetId = candidate.GetComponent<NetworkIdentity>();
            string name = candidateIdentity != null ? candidateIdentity.DisplayName : "?";

            GUILayout.BeginHorizontal();
            GUILayout.Label(name, DebugGUI.Label, GUILayout.Width(240));
            if (roomLeft && candidateNetId != null && LeftClickButton("Invite", GUILayout.Width(80)))
                myTeam.CmdInvite(candidateNetId);
            GUILayout.EndHorizontal();
        }
    }
}
