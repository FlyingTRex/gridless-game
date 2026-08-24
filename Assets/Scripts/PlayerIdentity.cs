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

    [SerializeField] private string playerName = DefaultName;
    [SerializeField] private bool hasBeenNamed;

    private PlayerCurrency wallet;
    private PlayerFame fame;
    private string playerId;

    public string DisplayName => playerName;
    public bool HasBeenNamed => hasBeenNamed;

    // Server-side only -- empty until CmdSetPlayerId has actually run
    // (right after OnStartLocalPlayer on the owning client). Chunk 3's
    // save/load keying needs to guard against this still being empty
    // for a frame or two after spawn, not assume it's populated
    // immediately.
    public string PlayerId => playerId;

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
            playerName = clean;
            hasBeenNamed = true;
            return true;
        }

        int cost = fame != null ? fame.RenameCostGold : 1;
        if (wallet == null || !wallet.Spend(CoinType.Gold, cost)) return false;

        playerName = clean;
        fame?.ApplyRenamePenalty();
        return true;
    }

    // Called by SaveManager on load -- sets the name/flag directly,
    // bypassing cost/sanitization (already-saved data is already valid).
    public void RestoreIdentity(string savedName, bool savedHasBeenNamed)
    {
        if (!string.IsNullOrWhiteSpace(savedName)) playerName = savedName;
        hasBeenNamed = savedHasBeenNamed;
    }
}
