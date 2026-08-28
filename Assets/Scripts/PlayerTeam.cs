using System.Collections.Generic;
using Mirror;
using UnityEngine;

public enum TeamRole { Member, Officer, Owner }

// Team roster/lifecycle -- MVP4_PLANNING.md / TEAMS_AND_GUILDS_PLANNING.md's
// "Team" (built 2026-08-28, fully resolved conversationally with Ben the
// prior session). Deliberately NOT an access-control system -- a real
// codebase survey done during planning found there is no object-level
// ownership/permission plumbing anywhere in this project yet (StorageBox's
// own isPlayerOwned is the closest thing, and it's a single crude
// boolean), and building that out is real scope this tier explicitly
// doesn't need. Team is roster + cosmetic territory + vendor-split only --
// every structure/StorageBox stays exactly as open as it is today,
// regardless of team membership.
//
// One component per player, three SyncVars, no separate global registry.
// A roster is built by scanning every OTHER player's own PlayerTeam via
// FindObjectsByType and filtering by teamId -- same "live scan every
// frame, no cached/stale state" pattern MapScreen's DrawFlagMarkers
// already established for Village Flags. This works identically
// server-side (inside a Command, for validation) and client-side (for
// TeamScreen's own display), since SyncVars are already replicated to
// every observer the same way any other SyncVar is.
[RequireComponent(typeof(PlayerIdentity))]
public class PlayerTeam : NetworkBehaviour
{
    public const int MaxMembers = 6;
    private const int MaxNameLength = 30;

    [SyncVar] private string teamId = "";
    [SyncVar] private string teamName = "";
    [SyncVar] private TeamRole role = TeamRole.Member;

    // At most one pending invite per player at a time (accepting/
    // declining/a newer invite arriving all just overwrite this) --
    // simpler than a list, and matches "you can only usefully consider
    // one team invitation at a time" being a fine real-world constraint.
    // FLAGGED as the actual open design question TEAMS_AND_GUILDS_PLANNING.md
    // left unresolved ("needs a real design decision when this gets
    // built") -- this is that decision, made here: a SyncVar on the
    // recipient's own identity, not a server-side tracked list, so it
    // persists until acted on (unlike PlayerIdentity
    // .TargetNotifyNearbyPlayerJoined's one-shot toast, which wouldn't
    // survive the player not noticing it in time).
    [SyncVar] private string pendingInviteTeamId = "";
    [SyncVar] private string pendingInviteTeamName = "";
    [SyncVar] private string pendingInviteFromName = "";

    private PlayerIdentity identity;

    public string TeamId => teamId;
    public string TeamName => teamName;
    public TeamRole Role => role;
    public bool IsInTeam => !string.IsNullOrEmpty(teamId);

    public string PendingInviteTeamId => pendingInviteTeamId;
    public string PendingInviteTeamName => pendingInviteTeamName;
    public string PendingInviteFromName => pendingInviteFromName;
    public bool HasPendingInvite => !string.IsNullOrEmpty(pendingInviteTeamId);

    private void Awake()
    {
        identity = GetComponent<PlayerIdentity>();
    }

    // Every current member of teamId, on whichever machine calls this --
    // works identically server-side (Commands) or client-side (TeamScreen).
    public static List<PlayerTeam> MembersOf(string teamId)
    {
        var result = new List<PlayerTeam>();
        if (string.IsNullOrEmpty(teamId)) return result;
        foreach (var pt in FindObjectsByType<PlayerTeam>(FindObjectsSortMode.None))
            if (pt.teamId == teamId) result.Add(pt);
        return result;
    }

    private static string SanitizeName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        string trimmed = raw.Trim();
        return trimmed.Length > MaxNameLength ? trimmed.Substring(0, MaxNameLength) : trimmed;
    }

    // --- Lifecycle -----------------------------------------------------

    [Command]
    public void CmdCreateTeam(string desiredName)
    {
        if (IsInTeam) return;
        string clean = SanitizeName(desiredName);
        if (clean == null) return;

        teamId = System.Guid.NewGuid().ToString("N");
        teamName = clean;
        role = TeamRole.Owner;
    }

    [Command]
    public void CmdDisbandTeam()
    {
        if (!IsInTeam || role != TeamRole.Owner) return;
        foreach (var member in MembersOf(teamId))
        {
            member.teamId = "";
            member.teamName = "";
            member.role = TeamRole.Member;
        }
    }

    // Owner must explicitly hand off (CmdPromoteToOwner) before leaving --
    // no automatic succession. A sole-member Owner has nobody to hand off
    // to, so leaving in that case just disbands instead.
    [Command]
    public void CmdLeaveTeam()
    {
        if (!IsInTeam) return;

        if (role == TeamRole.Owner)
        {
            if (MembersOf(teamId).Count > 1)
            {
                Debug.Log("[PlayerTeam] Owner must promote someone else to Owner before leaving.");
                return;
            }
            CmdDisbandTeam();
            return;
        }

        teamId = "";
        teamName = "";
        role = TeamRole.Member;
    }

    // --- Membership changes ---------------------------------------------

    // Invite/kick are Officer+ actions (itemized in
    // TEAMS_AND_GUILDS_PLANNING.md's resolved Team section); promotion to
    // Officer or Owner stays Owner-only, not delegated.
    [Command(requiresAuthority = false)]
    public void CmdInvite(NetworkIdentity targetIdentity)
    {
        if (!IsInTeam || role == TeamRole.Member) return;
        if (targetIdentity == null) return;
        if (!targetIdentity.TryGetComponent(out PlayerTeam target)) return;
        if (target.IsInTeam) return;
        if (MembersOf(teamId).Count >= MaxMembers) return;

        target.pendingInviteTeamId = teamId;
        target.pendingInviteTeamName = teamName;
        target.pendingInviteFromName = identity != null ? identity.DisplayName : "";
    }

    [Command]
    public void CmdAcceptInvite()
    {
        if (IsInTeam || !HasPendingInvite) return;
        if (MembersOf(pendingInviteTeamId).Count >= MaxMembers)
        {
            CmdDeclineInvite();
            return;
        }

        teamId = pendingInviteTeamId;
        teamName = pendingInviteTeamName;
        role = TeamRole.Member;
        CmdDeclineInvite(); // clears the now-consumed pending fields
    }

    [Command]
    public void CmdDeclineInvite()
    {
        pendingInviteTeamId = "";
        pendingInviteTeamName = "";
        pendingInviteFromName = "";
    }

    // requiresAuthority = false: this targets ANOTHER player's own
    // PlayerTeam, not the caller's -- same shape PlayerRenaming.CmdRename
    // already established for acting on a different networked object.
    [Command(requiresAuthority = false)]
    public void CmdKick(NetworkIdentity targetIdentity)
    {
        if (!IsInTeam || role == TeamRole.Member) return;
        if (targetIdentity == null) return;
        if (!targetIdentity.TryGetComponent(out PlayerTeam target)) return;
        if (target.teamId != teamId) return;
        // An Officer may kick a Member but not another Officer or the
        // Owner; the Owner may kick anyone but themselves (use
        // CmdDisbandTeam/CmdLeaveTeam for that).
        if (target == this) return;
        if (role == TeamRole.Officer && target.role != TeamRole.Member) return;
        if (role == TeamRole.Owner && target.role == TeamRole.Owner) return;

        target.teamId = "";
        target.teamName = "";
        target.role = TeamRole.Member;
    }

    [Command(requiresAuthority = false)]
    public void CmdPromoteToOfficer(NetworkIdentity targetIdentity)
    {
        if (!IsInTeam || role != TeamRole.Owner) return;
        if (targetIdentity == null) return;
        if (!targetIdentity.TryGetComponent(out PlayerTeam target)) return;
        if (target.teamId != teamId || target.role != TeamRole.Member) return;

        target.role = TeamRole.Officer;
    }

    // The Owner hand-off -- lets an Owner then legally CmdLeaveTeam. The
    // former Owner lands as Officer, not plain Member -- still trusted,
    // just no longer solely responsible (a reasonable default, not an
    // explicit design-doc decision -- flag if this should differ).
    [Command(requiresAuthority = false)]
    public void CmdPromoteToOwner(NetworkIdentity targetIdentity)
    {
        if (!IsInTeam || role != TeamRole.Owner) return;
        if (targetIdentity == null) return;
        if (!targetIdentity.TryGetComponent(out PlayerTeam target)) return;
        if (target.teamId != teamId || target == this) return;

        target.role = TeamRole.Owner;
        role = TeamRole.Officer;
    }
}
