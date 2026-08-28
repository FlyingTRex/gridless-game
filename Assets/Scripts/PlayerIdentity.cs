using System;
using System.Text;
using Mirror;
using UnityEngine;

// The player's own display name (2026-08-22, MULTIPLAYER_PLANNING.md's
// player-identity groundwork -- ported from NPCDialogue's proven naming
// shape: a name field + DisplayName). Deliberately NOT IRenameable/
// raycast-triggered like world objects -- right-click-rename doesn't
// make sense on yourself, so the entry point is a dedicated Player-tab
// control instead (PlayerMenuScreen), not PlayerRenaming's world-aim flow.
//
// Persistence restructure chunk 2 (MULTIPLAYER_PLANNING.md section 3
// item 5), 2026-08-23: converted to NetworkBehaviour, plus a real stable
// PlayerId -- DisplayName is renameable and never guaranteed unique, so
// it can't key a save record; chunk 3 needs something that can. The ID
// has to be generated client-side (so the SAME id comes back on this
// person's own machine every session, via PlayerPrefs) but recorded
// server-side (so SaveManager, which will run server-side once this
// restructure is done, can actually read it) -- a genuine client-to-
// server handoff, not something either side can produce alone.
// Deliberately NOT a SyncVar: no other client/observer needs to see
// another player's raw ID, so it's a private field set once via Command
// rather than broadcasting it. Chunk 2 stops here -- SaveManager itself
// isn't touched until chunk 3 actually keys character records on this.
[RequireComponent(typeof(PlayerCurrency))]
[RequireComponent(typeof(PlayerFame))]
public class PlayerIdentity : NetworkBehaviour
{
    private const string DefaultName = "Traveler";
    private const int MaxNameLength = 30;
    private const string PlayerIdPrefKey = "GridlessPlayerId";

    // FIXED (2026-08-28, found live during the real two-machine session):
    // these used to be plain [SerializeField] fields, and TryRename below
    // set them directly with no Command involved -- a rename only ever
    // updated the renaming player's own local copy of this component,
    // never the server's, so it never replicated to anyone else's view
    // of that player (Team roster showed a renamed player as "Traveler"
    // forever). Now real [SyncVar]s, written only from CmdApplyRename
    // (server-side) below.
    [SyncVar] private string playerName = DefaultName;
    [SyncVar] private bool hasBeenNamed;

    private PlayerCurrency wallet;
    private PlayerFame fame;
    private string playerId;

    // Nearby-player-joined announcement (2026-08-25, Ben's ask alongside
    // per-connection spawning): fog of war still hides a new arrival on
    // the Map, but a toast lets players know someone's out there. Server
    // calls TargetNotifyNearbyPlayerJoined on each EXISTING player's own
    // PlayerIdentity, targeting that SAME player's connection, whenever a
    // NEW connection spawns within announceRadius -- delivered only to
    // that one client, same TargetRpc pattern used nowhere else yet in
    // this project but the standard Mirror shape for "tell exactly one
    // client something." No new-arrival-side toast (the new player didn't
    // ask "who's here," they just spawned) -- one-directional by design.
    private string nearbyPlayerMessage;
    private float nearbyPlayerMessageExpireTime;
    private const float NearbyPlayerMessageDuration = 8f;

    public string DisplayName => playerName;
    public bool HasBeenNamed => hasBeenNamed;

    // Server-side only -- empty until CmdSetPlayerId has actually run
    // (right after OnStartLocalPlayer on the owning client). Chunk 3's
    // save/load keying needs to guard against this still being empty
    // for a frame or two after spawn, not assume it's populated
    // immediately.
    public string PlayerId => playerId;

    // Fired exactly once, right after CmdSetPlayerId actually populates
    // playerId server-side. SaveManager (chunk 3) needs this rather than
    // just reading PlayerId in its own Start() -- the Command is a real
    // client-to-server round trip, not instant, so Start() ordering alone
    // can't guarantee PlayerId is populated yet by the time SaveManager
    // would otherwise try to use it.
    public event Action<string> PlayerIdReady;

    // Read by the rename popup to show the real cost before committing --
    // 0 for the still-free first rename.
    public int NextRenameCostGold => hasBeenNamed && fame != null ? fame.RenameCostGold : 0;

    private void Awake()
    {
        wallet = GetComponent<PlayerCurrency>();
        fame = GetComponent<PlayerFame>();
    }

    // Client-only setup -- reads (or creates) this machine's own stable
    // id from PlayerPrefs and hands it to the server. Never read
    // PlayerPrefs from anywhere else in this class: Awake()/Update() run
    // on every machine holding a copy of this object (server, owning
    // client, any observers), and PlayerPrefs is local to whichever
    // machine executes the call -- reading it outside this
    // local-player-only hook would silently return the SERVER's own
    // prefs instead of the connecting player's.
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        CmdSetPlayerId(GetOrCreateLocalPlayerId());
    }

    private static string GetOrCreateLocalPlayerId()
    {
        if (PlayerPrefs.HasKey(PlayerIdPrefKey))
            return PlayerPrefs.GetString(PlayerIdPrefKey);

        string id = Guid.NewGuid().ToString("N");
        PlayerPrefs.SetString(PlayerIdPrefKey, id);
        PlayerPrefs.Save();
        return id;
    }

    [Command]
    private void CmdSetPlayerId(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        playerId = id;
        Debug.Log($"[PlayerIdentity] PlayerId set to {playerId}");
        PlayerIdReady?.Invoke(playerId);
    }

    // Basic sanitization only (2026-08-22) -- trim, length cap, strip
    // non-printable/control characters. Deliberately NOT a profanity
    // filter -- see BUGS_AND_ENHANCEMENTS.md's own entry: a real one
    // needs a maintained wordlist and leetspeak/spacing-trick
    // normalization, genuine scope on its own, logged as a real
    // pre-multiplayer requirement rather than built blind here.
    private static string Sanitize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var sb = new StringBuilder();
        foreach (char c in raw.Trim())
            if (!char.IsControl(c)) sb.Append(c);

        string result = sb.ToString();
        if (string.IsNullOrWhiteSpace(result)) return null;
        return result.Length > MaxNameLength ? result.Substring(0, MaxNameLength) : result;
    }

    // Enforces its own rules (name validity, cost, Fame penalty) rather
    // than trusting the UI to have already checked -- same "the
    // component is the one enforcement point" discipline VendorStall
    // .AssignStock already established for the ownership gate. No
    // partial state: fails cleanly (false, nothing charged/changed) if
    // the name is invalid or unaffordable.
    public bool TryRename(string newName)
    {
        string clean = Sanitize(newName);
        if (clean == null) return false;

        if (!hasBeenNamed)
        {
            CmdApplyRename(clean);
            return true;
        }

        int cost = fame != null ? fame.RenameCostGold : 1;
        if (wallet == null || !wallet.Spend(CoinType.Gold, cost)) return false;

        CmdApplyRename(clean);
        fame?.ApplyRenamePenalty();
        return true;
    }

    // The actual [SyncVar] write, split out so it always runs server-side
    // -- a client can't legally set a [SyncVar]'s backing field itself
    // and have it replicate. Re-sanitizes rather than trusting the
    // caller's already-clean string, same "the component enforces its
    // own rules" discipline TryRename's own header comment already
    // states -- cost/wallet stay a client-side check for now (PlayerCurrency
    // isn't networked yet, a separate, larger gap logged in
    // BUGS_AND_ENHANCEMENTS.md, not fixed here).
    [Command]
    private void CmdApplyRename(string newName)
    {
        string clean = Sanitize(newName);
        if (clean == null) return;

        playerName = clean;
        hasBeenNamed = true;
    }

    // Called by SaveManager on load -- sets the name/flag directly,
    // bypassing cost/sanitization (already-saved data is already valid).
    public void RestoreIdentity(string savedName, bool savedHasBeenNamed)
    {
        if (!string.IsNullOrWhiteSpace(savedName)) playerName = savedName;
        hasBeenNamed = savedHasBeenNamed;
    }

    [TargetRpc]
    public void TargetNotifyNearbyPlayerJoined(NetworkConnectionToClient target, string arrivingPlayerName)
    {
        nearbyPlayerMessage = string.IsNullOrEmpty(arrivingPlayerName)
            ? "Someone has arrived nearby."
            : $"{arrivingPlayerName} has arrived nearby.";
        nearbyPlayerMessageExpireTime = Time.time + NearbyPlayerMessageDuration;
    }

    // Same top-center toast shape as PlayerAutosave.cs -- this is only
    // ever set client-side by the TargetRpc above, so unlike PlayerAutosave
    // it doesn't need an isLocalPlayer/isServer guard: a TargetRpc only
    // ever executes on the one connection it targets in the first place.
    private void OnGUI()
    {
        if (nearbyPlayerMessage == null || Time.time >= nearbyPlayerMessageExpireTime) return;

        const float width = 340f;
        const float height = 30f;
        var rect = new Rect((Screen.width - width) / 2f, 190f, width, height);

        DebugGUI.DrawPanel(rect);
        GUI.Label(rect, nearbyPlayerMessage, DebugGUI.Header);
    }
}
