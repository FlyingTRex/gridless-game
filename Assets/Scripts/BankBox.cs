using UnityEngine;

// A stationary world bank branch. E opens BankScreen, same as any other
// IInteractable — the bank itself is global (PlayerBank), so any branch
// opens the same account.
//
// RequireComponent(SaveId) added 2026-08-22 (Vendor Stall design) --
// BankBox previously had no per-instance state worth saving (PlayerBank's
// own balances are the real global ledger), so it never needed one. Now
// that a BankBox is a real player-BUILT structure (BankBoxPiece), it
// needs to persist as a placed piece the same way every other BuildPiece
// does -- PlayerBuilding.Confirm's `real.GetComponent<SaveId>()?.
// GenerateIfMissing()` is a silent no-op without this, which would leave
// a built Bank Box invisible to SaveManager's placedPieces capture, same
// failure shape CLAUDE.md's own SaveId-migration gotcha already warns
// about.
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(SaveId))]
public class BankBox : MonoBehaviour, IInteractable
{
    [SerializeField] private string bankName = "Bank";

    public string Prompt => $"Open {bankName}";
    public bool IsInstant => true;
    public float GetHoldDuration(GameObject player) => 0f;

    public void Complete(GameObject player)
    {
        player.GetComponent<BankScreen>()?.Open(this);
    }

    // Same "does a real instance exist anywhere" gate CityStatue.Exists
    // already establishes -- read by VendorStall's Pay-from-Bank fallback
    // (2026-08-22) to decide whether Bank access has actually been
    // earned yet, since PlayerBank's own balance data lives on the
    // Player component regardless and would otherwise offer this
    // convenience for free before any BankBox is ever built.
    public static bool Exists => FindObjectsByType<BankBox>(FindObjectsSortMode.None).Length > 0;
}
