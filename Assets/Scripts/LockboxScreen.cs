using UnityEngine;

// Opened by interacting (E) with a specific Lockbox — shows that one
// box's per-type balance next to the wallet, with Deposit/Withdraw per
// coin type. No exchange here; that's a bank-only service.
[RequireComponent(typeof(PlayerCurrency))]
public class LockboxScreen : MonoBehaviour
{
    private static readonly (string Label, CoinType Type)[] CoinOrder =
    {
        ("Copper", CoinType.Copper),
        ("Iron", CoinType.Iron),
        ("Silver", CoinType.Silver),
        ("Gold", CoinType.Gold),
        ("Platinum", CoinType.Platinum),
    };

    private const float PanelWidth = 460f;
    private const float PanelHeight = 380f;

    private PlayerCurrency wallet;
    private Lockbox current;
    private bool isOpen;

    private CoinType? pendingType;
    private bool pendingIsDeposit;
    private int pendingAmount;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        wallet = GetComponent<PlayerCurrency>();
    }

    // Called by Lockbox.Complete. Only opens from normal gameplay — same
    // rule every other screen follows, so it can't stack on top of one
    // that already has the cursor unlocked.
    public void Open(Lockbox lockbox)
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        current = lockbox;
        SetOpen(true);
    }

    // Called by FirstPersonController when Escape re-locks the cursor, so
    // the two toggles can't drift out of sync with each other.
    public void Close() => SetOpen(false);

    private void SetOpen(bool value)
    {
        isOpen = value;
        if (!value)
        {
            pendingType = null;
            current = null;
        }
        Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = value;
    }

    private void OnGUI()
    {
        if (!isOpen || current == null) return;

        var rect = new Rect((Screen.width - PanelWidth) / 2f, (Screen.height - PanelHeight) / 2f, PanelWidth, PanelHeight);
        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Label(current.DisplayName, DebugGUI.Header);
        GUILayout.Label($"Capacity per coin type: {current.CapacityPerType}", DebugGUI.Label);

        GUILayout.BeginHorizontal();
        GUILayout.Label("", GUILayout.Width(90));
        GUILayout.Label("Wallet", DebugGUI.Label, GUILayout.Width(60));
        GUILayout.Label("Lockbox", DebugGUI.Label, GUILayout.Width(70));
        GUILayout.EndHorizontal();

        CoinType? depositClicked = null;
        CoinType? withdrawClicked = null;

        foreach (var (label, type) in CoinOrder)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, DebugGUI.Label, GUILayout.Width(90));
            GUILayout.Label(wallet.GetBalance(type).ToString(), DebugGUI.Label, GUILayout.Width(60));
            GUILayout.Label(current.GetBalance(type).ToString(), DebugGUI.Label, GUILayout.Width(70));
            if (GUILayout.Button("Deposit", GUILayout.Width(75))) depositClicked = type;
            if (GUILayout.Button("Withdraw", GUILayout.Width(80))) withdrawClicked = type;
            GUILayout.EndHorizontal();
        }

        if (depositClicked != null)
        {
            pendingType = depositClicked;
            pendingIsDeposit = true;
            pendingAmount = 0;
        }
        if (withdrawClicked != null)
        {
            pendingType = withdrawClicked;
            pendingIsDeposit = false;
            pendingAmount = 0;
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Close", GUILayout.Width(100)))
            SetOpen(false);

        GUILayout.EndArea();

        DrawPendingPopup();
    }

    // Deposit is capped by the lockbox's remaining capacity for that coin
    // type; Withdraw is capped by both what the lockbox holds AND what the
    // wallet has room left for (PlayerCurrency.MaxBalance) — pulling 1000
    // Gold isn't possible if the wallet can't hold that much, even if the
    // lockbox does.
    private void DrawPendingPopup()
    {
        if (pendingType == null) return;

        CoinType type = pendingType.Value;
        int walletBal = wallet.GetBalance(type);
        int boxBal = current.GetBalance(type);
        int boxSpace = current.CapacityPerType - boxBal;
        int walletSpace = PlayerCurrency.MaxBalance - walletBal;

        int available = Mathf.Max(0, pendingIsDeposit
            ? Mathf.Min(walletBal, boxSpace)
            : Mathf.Min(boxBal, walletSpace));

        pendingAmount = Mathf.Clamp(pendingAmount, 0, available);

        const float width = 260f;
        const float height = 190f;
        var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);

        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Label(pendingIsDeposit ? $"Deposit {type}" : $"Withdraw {type}", DebugGUI.Header);
        GUILayout.Label(pendingIsDeposit
            ? $"Wallet: {walletBal}   Box space: {boxSpace}"
            : $"Box: {boxBal}   Wallet space: {walletSpace}", DebugGUI.Label);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-10", GUILayout.Width(45))) pendingAmount -= 10;
        if (GUILayout.Button("-1", GUILayout.Width(35))) pendingAmount -= 1;
        GUILayout.Label(pendingAmount.ToString(), DebugGUI.Header, GUILayout.Width(50));
        if (GUILayout.Button("+1", GUILayout.Width(35))) pendingAmount += 1;
        if (GUILayout.Button("+10", GUILayout.Width(45))) pendingAmount += 10;
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Max"))
            pendingAmount = available;

        pendingAmount = Mathf.Clamp(pendingAmount, 0, available);

        bool resolved = false;
        GUILayout.BeginHorizontal();
        GUI.enabled = pendingAmount > 0;
        if (GUILayout.Button(pendingIsDeposit ? "Deposit" : "Withdraw"))
        {
            if (pendingIsDeposit)
            {
                wallet.Spend(type, pendingAmount);
                int leftover = current.Add(type, pendingAmount);
                if (leftover > 0) wallet.Add(type, leftover); // shouldn't happen given the clamp above, but never lose coins
            }
            else
            {
                current.Remove(type, pendingAmount);
                wallet.Add(type, pendingAmount);
            }
            resolved = true;
        }
        GUI.enabled = true;
        if (GUILayout.Button("Cancel")) resolved = true;
        GUILayout.EndHorizontal();

        GUILayout.EndArea();

        if (resolved) pendingType = null;
    }
}
