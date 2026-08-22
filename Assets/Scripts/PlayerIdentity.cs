using System.Text;
using UnityEngine;

// The player's own display name (2026-08-22, MULTIPLAYER_PLANNING.md's
// player-identity groundwork -- ported from NPCDialogue's proven naming
// shape: a name field + DisplayName). Deliberately NOT IRenameable/
// raycast-triggered like world objects -- right-click-rename doesn't
// make sense on yourself, so the entry point is a dedicated Player-tab
// control instead (PlayerMenuScreen), not PlayerRenaming's world-aim flow.
[RequireComponent(typeof(PlayerCurrency))]
[RequireComponent(typeof(PlayerFame))]
public class PlayerIdentity : MonoBehaviour
{
    private const string DefaultName = "Traveler";
    private const int MaxNameLength = 30;

    [SerializeField] private string playerName = DefaultName;
    [SerializeField] private bool hasBeenNamed;

    private PlayerCurrency wallet;
    private PlayerFame fame;

    public string DisplayName => playerName;
    public bool HasBeenNamed => hasBeenNamed;

    // Read by the rename popup to show the real cost before committing --
    // 0 for the still-free first rename.
    public int NextRenameCostGold => hasBeenNamed && fame != null ? fame.RenameCostGold : 0;

    private void Awake()
    {
        wallet = GetComponent<PlayerCurrency>();
        fame = GetComponent<PlayerFame>();
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
