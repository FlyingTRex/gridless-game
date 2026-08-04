using UnityEngine;

// Bank UI, opened by interacting (E) with any BankBox in the world —
// unlike Inventory/Crafting/Skills there's no dedicated hotkey, since a
// bank is a place you have to actually be at, not always-available.
[RequireComponent(typeof(PlayerCurrency))]
[RequireComponent(typeof(PlayerBank))]
public class BankScreen : MonoBehaviour
{
    private static readonly (string Label, CoinType Type)[] CoinOrder =
    {
        ("Copper", CoinType.Copper),
        ("Iron", CoinType.Iron),
        ("Silver", CoinType.Silver),
        ("Gold", CoinType.Gold),
        ("Platinum", CoinType.Platinum),
    };

    private static readonly CraftTier[] LockboxTierOrder =
    {
        CraftTier.Crude, CraftTier.Rudimentary, CraftTier.Normal, CraftTier.Fine, CraftTier.Masterwork,
    };

    private const float PanelWidth = 480f;
    private const float PanelHeight = 620f;

    // User feedback: the bank window was hard to read. GUILayout uses fixed
    // pixel widths throughout, so just enlarging PanelWidth/PanelHeight would
    // only add empty padding, not bigger text/buttons — scaling the whole
    // GUI matrix around the screen center grows everything (text, buttons,
    // spacing, and the popups drawn later in this same OnGUI call) together.
    private const float UiScale = 1.5f;

    // Lockbox baseline (CraftTier.Normal): capacity per coin type and
    // Gold price, scaled per tier by CraftTierScale.Modifier.
    private const int LockboxBaseCapacity = 2500;
    private const int LockboxBasePrice = 10;

    [SerializeField] private Material[] lockboxMaterials; // indexed by CraftTier
    [SerializeField] private Vector3 lockboxScale = new Vector3(0.5f, 0.4f, 0.4f);
    [SerializeField] private float lockboxSpawnDistance = 2f;

    private PlayerCurrency wallet;
    private PlayerBank bank;
    private Transform bankBoxOrigin;
    private bool isOpen;

    // Pending Deposit/Withdraw quantity popup.
    private CoinType? pendingType;
    private bool pendingIsDeposit;
    private int pendingAmount;

    // Pending Exchange quantity popup.
    private CoinType? pendingExchangeFrom;
    private CoinType? pendingExchangeTo;
    private int pendingExchangeAmount;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        wallet = GetComponent<PlayerCurrency>();
        bank = GetComponent<PlayerBank>();
    }

    // Called by BankBox.Complete. Only opens from normal gameplay — same
    // rule every other screen follows, so it can't stack on top of one
    // that already has the cursor unlocked.
    public void Open(BankBox box)
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        bankBoxOrigin = box != null ? box.transform : transform;
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
            pendingExchangeFrom = null;
        }
        Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = value;
    }

    private void OnGUI()
    {
        if (!isOpen) return;

        // Scale the whole screen (panel + both popups, drawn later in this
        // same call) around the screen center so it grows in place rather
        // than shifting off-center.
        Matrix4x4 savedMatrix = GUI.matrix;
        GUIUtility.ScaleAroundPivot(Vector2.one * UiScale, new Vector2(Screen.width / 2f, Screen.height / 2f));

        // A Deposit/Withdraw/Exchange popup is modal — block every button on
        // the panel underneath it, otherwise a click that lands on the table
        // behind the popup silently reassigns pendingType/pendingExchangeFrom
        // instead of being caught by the popup in front of it.
        bool popupOpen = pendingType != null || pendingExchangeFrom != null;

        var rect = new Rect((Screen.width - PanelWidth) / 2f, (Screen.height - PanelHeight) / 2f, PanelWidth, PanelHeight);
        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Label("Bank", DebugGUI.Header);

        GUI.enabled = !popupOpen;
        GUILayout.BeginHorizontal();
        GUILayout.Label("", GUILayout.Width(90));
        GUILayout.Label("Wallet", DebugGUI.Label, GUILayout.Width(60));
        GUILayout.Label("Bank", DebugGUI.Label, GUILayout.Width(60));
        GUILayout.EndHorizontal();

        CoinType? depositClicked = null;
        CoinType? withdrawClicked = null;

        foreach (var (label, type) in CoinOrder)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, DebugGUI.Label, GUILayout.Width(90));
            GUILayout.Label(wallet.GetBalance(type).ToString(), DebugGUI.Label, GUILayout.Width(60));
            GUILayout.Label(bank.GetBalance(type).ToString(), DebugGUI.Label, GUILayout.Width(60));
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
        GUILayout.Label("Exchange (10:1, adjacent types only)", DebugGUI.Header);

        for (int i = 0; i < CoinOrder.Length - 1; i++)
        {
            var lower = CoinOrder[i];
            var higher = CoinOrder[i + 1];

            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"10 {lower.Label} -> 1 {higher.Label}", GUILayout.Width(190)))
            {
                pendingExchangeFrom = lower.Type;
                pendingExchangeTo = higher.Type;
                pendingExchangeAmount = 0;
            }
            if (GUILayout.Button($"1 {higher.Label} -> 10 {lower.Label}", GUILayout.Width(190)))
            {
                pendingExchangeFrom = higher.Type;
                pendingExchangeTo = lower.Type;
                pendingExchangeAmount = 0;
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        GUILayout.Label("Lockboxes (personal coin storage, purchased with Gold)", DebugGUI.Header);

        CraftTier? buyClicked = null;
        foreach (var tier in LockboxTierOrder)
        {
            int capacity = LockboxCapacity(tier);
            int price = LockboxPrice(tier);
            string name = CraftTierNames.WithPrefix(tier, "Lockbox");

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{name} — {capacity}/type — {price} Gold", DebugGUI.Label, GUILayout.Width(300));
            GUI.enabled = !popupOpen && wallet.GetBalance(CoinType.Gold) >= price;
            if (GUILayout.Button("Buy", GUILayout.Width(60))) buyClicked = tier;
            GUI.enabled = !popupOpen;
            GUILayout.EndHorizontal();
        }

        if (buyClicked != null)
            BuyLockbox(buyClicked.Value);

        GUILayout.Space(10);
        if (GUILayout.Button("Close", GUILayout.Width(100)))
            SetOpen(false);

        GUI.enabled = true;
        GUILayout.EndArea();

        DrawDepositWithdrawPopup();
        DrawExchangePopup();

        GUI.matrix = savedMatrix;
    }

    private static int LockboxCapacity(CraftTier tier) =>
        Mathf.RoundToInt(LockboxBaseCapacity * CraftTierScale.Modifier(tier));

    private static int LockboxPrice(CraftTier tier) =>
        Mathf.RoundToInt(LockboxBasePrice * CraftTierScale.Modifier(tier));

    // Lockbox purchases aren't one of the fee-bearing transaction types
    // (deposit/withdraw/exchange) — just a flat Gold price, paid from the
    // wallet like any other purchase.
    private void BuyLockbox(CraftTier tier)
    {
        int price = LockboxPrice(tier);
        if (!wallet.Spend(CoinType.Gold, price)) return;

        Transform origin = bankBoxOrigin != null ? bankBoxOrigin : transform;
        Vector3 spawnPos = origin.position + origin.forward * lockboxSpawnDistance;
        SpawnLockbox(tier, spawnPos);
    }

    private void SpawnLockbox(CraftTier tier, Vector3 position)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.position = position;
        go.transform.localScale = lockboxScale;

        int index = (int)tier;
        if (lockboxMaterials != null && index < lockboxMaterials.Length && lockboxMaterials[index] != null)
            go.GetComponent<Renderer>().sharedMaterial = lockboxMaterials[index];

        var lockbox = go.AddComponent<Lockbox>();
        lockbox.Configure(tier);
        go.name = lockbox.DisplayName;
    }

    // Deposit: wallet pays amount + fee, bank receives exactly amount.
    // Withdraw: bank pays amount + fee, wallet receives exactly amount.
    private void DrawDepositWithdrawPopup()
    {
        if (pendingType == null) return;

        CoinType type = pendingType.Value;
        int available = pendingIsDeposit ? wallet.GetBalance(type) : bank.GetBalance(type);
        pendingAmount = Mathf.Clamp(pendingAmount, 0, available);

        int fee = pendingAmount > 0 ? PlayerBank.FeeFor(pendingAmount) : 0;
        int totalCost = pendingAmount + fee;
        bool valid = pendingAmount > 0 && totalCost <= available;

        const float width = 280f;
        const float height = 210f;
        var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);

        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Label(pendingIsDeposit ? $"Deposit {type}" : $"Withdraw {type}", DebugGUI.Header);
        GUILayout.Label($"Available: {available}", DebugGUI.Label);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-10", GUILayout.Width(45))) pendingAmount -= 10;
        if (GUILayout.Button("-1", GUILayout.Width(35))) pendingAmount -= 1;
        GUILayout.Label(pendingAmount.ToString(), DebugGUI.Header, GUILayout.Width(50));
        if (GUILayout.Button("+1", GUILayout.Width(35))) pendingAmount += 1;
        if (GUILayout.Button("+10", GUILayout.Width(45))) pendingAmount += 10;
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Max"))
            pendingAmount = PlayerBank.MaxAffordable(available);

        pendingAmount = Mathf.Clamp(pendingAmount, 0, available);

        GUILayout.Label($"Fee: {fee}  (total cost: {totalCost})", DebugGUI.Label);

        bool resolved = false;
        GUILayout.BeginHorizontal();
        GUI.enabled = valid;
        if (GUILayout.Button(pendingIsDeposit ? "Deposit" : "Withdraw"))
        {
            if (pendingIsDeposit) bank.Deposit(type, pendingAmount);
            else bank.Withdraw(type, pendingAmount);
            resolved = true;
        }
        GUI.enabled = true;
        if (GUILayout.Button("Cancel")) resolved = true;
        GUILayout.EndHorizontal();

        GUILayout.EndArea();

        if (resolved) pendingType = null;
    }

    private void DrawExchangePopup()
    {
        if (pendingExchangeFrom == null || pendingExchangeTo == null) return;

        CoinType from = pendingExchangeFrom.Value;
        CoinType to = pendingExchangeTo.Value;
        bool upgrading = (int)to == (int)from + 1;

        int walletBal = wallet.GetBalance(from);
        pendingExchangeAmount = Mathf.Clamp(pendingExchangeAmount, 0, walletBal);

        int usable = upgrading ? (pendingExchangeAmount / 10) * 10 : pendingExchangeAmount;
        int fee = usable > 0 ? PlayerBank.FeeFor(usable) : 0;
        int totalCost = usable + fee;
        int output = upgrading ? usable / 10 : usable * 10;

        bool spaceOk = wallet.GetBalance(to) + output <= PlayerCurrency.MaxBalance;
        bool valid = usable > 0 && totalCost <= walletBal && spaceOk;

        const float width = 300f;
        const float height = 230f;
        var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);

        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Label($"Exchange {from} -> {to}", DebugGUI.Header);
        GUILayout.Label($"Wallet {from}: {walletBal}", DebugGUI.Label);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-10", GUILayout.Width(45))) pendingExchangeAmount -= 10;
        if (GUILayout.Button("-1", GUILayout.Width(35))) pendingExchangeAmount -= 1;
        GUILayout.Label(pendingExchangeAmount.ToString(), DebugGUI.Header, GUILayout.Width(50));
        if (GUILayout.Button("+1", GUILayout.Width(35))) pendingExchangeAmount += 1;
        if (GUILayout.Button("+10", GUILayout.Width(45))) pendingExchangeAmount += 10;
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Max"))
            pendingExchangeAmount = walletBal;

        pendingExchangeAmount = Mathf.Clamp(pendingExchangeAmount, 0, walletBal);

        GUILayout.Label(
            usable > 0
                ? $"Spends {usable} {from} (fee {fee}) -> {output} {to}"
                : "Not enough to exchange",
            DebugGUI.Label);

        bool resolved = false;
        GUILayout.BeginHorizontal();
        GUI.enabled = valid;
        if (GUILayout.Button("Exchange"))
        {
            bank.Exchange(from, to, pendingExchangeAmount);
            resolved = true;
        }
        GUI.enabled = true;
        if (GUILayout.Button("Cancel")) resolved = true;
        GUILayout.EndHorizontal();

        GUILayout.EndArea();

        if (resolved)
        {
            pendingExchangeFrom = null;
            pendingExchangeTo = null;
        }
    }
}
