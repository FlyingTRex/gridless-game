using Mirror;
using UnityEngine;

// FIXED (2026-08-28, MULTIPLAYER_INTERACTION_AUDIT.md): converted to
// NetworkBehaviour, balances now broadcast via a SyncList (same pattern
// Lockbox.cs/PlayerCurrency.cs's own fixes use). Deposit/Withdraw/
// Exchange -- the ones BankScreen's UI calls directly -- now route
// through Request/Command wrappers; SpendDirect/DepositDirect (used by
// VendorStall, itself still a separate un-fixed system per
// MULTIPLAYER_INTERACTION_AUDIT.md) are left as direct methods for now
// -- that's a pre-existing gap this fix doesn't make any worse.
//
// The player's bank account — separate from PlayerCurrency's carried
// wallet, and global: any BankBox reads/writes this same account, there's
// no per-branch ledger. No balance cap here, unlike the wallet's 250 —
// part of the incentive to bank coins instead of carrying them all.
[RequireComponent(typeof(PlayerCurrency))]
public class PlayerBank : NetworkBehaviour
{
    public const float FeeRate = 0.03f;
    public const int MinFee = 1;

    private static readonly int CoinTypeCount = System.Enum.GetValues(typeof(CoinType)).Length;

    private readonly int[] balances = new int[CoinTypeCount];
    private PlayerCurrency wallet;

    public readonly SyncList<int> syncedBalances = new SyncList<int>();

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (syncedBalances.Count == 0)
            for (int i = 0; i < CoinTypeCount; i++) syncedBalances.Add(balances[i]);
    }

    private void OnDestroy()
    {
        syncedBalances.Callback -= OnSyncedBalancesChanged;
    }

    private void OnSyncedBalancesChanged(SyncList<int>.Operation op, int index, int oldItem, int newItem)
    {
        if (isServer || index < 0 || index >= CoinTypeCount) return;
        balances[index] = newItem;
    }

    private void SyncBalance(int index)
    {
        if (isServer && index < syncedBalances.Count) syncedBalances[index] = balances[index];
    }

    private void Awake()
    {
        wallet = GetComponent<PlayerCurrency>();
        syncedBalances.Callback += OnSyncedBalancesChanged;

        // Starting bank balance — separate from the wallet's own starting
        // purse (PlayerCurrency.Awake).
        balances[(int)CoinType.Gold] = 25;
    }

    public int GetBalance(CoinType type) => balances[(int)type];

    // Direct deduction with no wallet involvement, for VendorStall's
    // Pay-from-Bank fallback (2026-08-22) -- unlike Withdraw, this money
    // never touches the wallet at all, it goes straight from Bank to the
    // vendor. Same bare bool-return shape as Lockbox.Remove/PlayerCurrency
    // .Spend so CoinSpender's delegate-based algorithm works against all
    // three without needing a shared interface.
    public bool SpendDirect(CoinType type, int amount)
    {
        if (amount <= 0) return false;

        int index = (int)type;
        if (balances[index] < amount) return false;

        balances[index] -= amount;
        SyncBalance(index);
        return true;
    }

    // Direct credit with no wallet involvement (2026-08-22) -- for
    // VendorStall's wallet-overflow safety net: a payout that would push
    // a wallet balance past its cap routes the excess straight into Bank
    // instead of vanishing. Unlike Deposit (which assumes the money is
    // ALREADY sitting in the wallet and draws an extra fee from there),
    // this money never touched the wallet at all -- the fee is skimmed
    // off the incoming amount itself before crediting, the only way to
    // charge one when there's no wallet balance to draw the extra from.
    public void DepositDirect(CoinType type, int amount)
    {
        if (amount <= 0) return;

        int fee = FeeFor(amount);
        int credited = Mathf.Max(0, amount - fee);
        int index = (int)type;
        balances[index] += credited;
        SyncBalance(index);
    }

    public static int FeeFor(int amount) => Mathf.Max(MinFee, Mathf.CeilToInt(amount * FeeRate));

    // Largest X such that X + FeeFor(X) <= available — the actual max
    // amount that can be deposited/withdrawn/exchanged out of a pool of
    // `available` coins once the on-top fee is accounted for.
    public static int MaxAffordable(int available)
    {
        for (int x = available; x > 0; x--)
            if (x + FeeFor(x) <= available)
                return x;

        return 0;
    }

    // Moves `amount` from the wallet into the bank. The wallet actually
    // pays amount + fee; the bank receives exactly `amount` — the fee is
    // an extra cost on the source side rather than skimmed off the
    // transferred total, so every transaction type lands as a clean,
    // predictable amount on the receiving side.
    public void RequestDeposit(CoinType type, int amount)
    {
        if (TryGetComponent<NetworkIdentity>(out _) && NetworkClient.active) { CmdDeposit(type, amount); return; }
        Deposit(type, amount);
    }

    [Command]
    private void CmdDeposit(CoinType type, int amount) => Deposit(type, amount);

    public bool Deposit(CoinType type, int amount)
    {
        if (amount <= 0) return false;

        int fee = FeeFor(amount);
        if (!wallet.Spend(type, amount + fee)) return false;

        int index = (int)type;
        balances[index] += amount;
        SyncBalance(index);
        return true;
    }

    public void RequestWithdraw(CoinType type, int amount)
    {
        if (TryGetComponent<NetworkIdentity>(out _) && NetworkClient.active) { CmdWithdraw(type, amount); return; }
        Withdraw(type, amount);
    }

    [Command]
    private void CmdWithdraw(CoinType type, int amount) => Withdraw(type, amount);

    public bool Withdraw(CoinType type, int amount)
    {
        if (amount <= 0) return false;

        int index = (int)type;
        int fee = FeeFor(amount);
        if (balances[index] < amount + fee) return false;
        if (wallet.GetBalance(type) + amount > PlayerCurrency.MaxBalance) return false;

        balances[index] -= amount + fee;
        SyncBalance(index);
        wallet.Add(type, amount);
        return true;
    }

    // Exchanges wallet coins between adjacent CoinType tiers at a fixed
    // 10:1 ratio (Copper->Iron->Silver->Gold->Platinum, per the design
    // brief's ladder). Operates on the wallet, not the bank balance — you
    // bring physical coins to the counter and walk away with different
    // ones, same mental model as Deposit/Withdraw moving between the two
    // pools. spendAmount is how much of `from` to spend; when upgrading to
    // a higher tier only the largest clean multiple of 10 is actually
    // spent, so this never produces a fractional result.
    public void RequestExchange(CoinType from, CoinType to, int spendAmount)
    {
        if (TryGetComponent<NetworkIdentity>(out _) && NetworkClient.active) { CmdExchange(from, to, spendAmount); return; }
        Exchange(from, to, spendAmount);
    }

    [Command]
    private void CmdExchange(CoinType from, CoinType to, int spendAmount) => Exchange(from, to, spendAmount);

    public bool Exchange(CoinType from, CoinType to, int spendAmount)
    {
        if (spendAmount <= 0) return false;

        bool upgrading = (int)to == (int)from + 1;
        bool downgrading = (int)to == (int)from - 1;
        if (!upgrading && !downgrading) return false;

        int usable = upgrading ? (spendAmount / 10) * 10 : spendAmount;
        if (usable <= 0) return false;

        int fee = FeeFor(usable);
        int output = upgrading ? usable / 10 : usable * 10;

        if (wallet.GetBalance(to) + output > PlayerCurrency.MaxBalance) return false;
        if (!wallet.Spend(from, usable + fee)) return false;

        wallet.Add(to, output);
        return true;
    }
}
