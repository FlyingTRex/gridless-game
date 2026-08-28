using Mirror;
using UnityEngine;

public enum CoinType
{
    Copper,
    Iron,
    Silver,
    Gold,
    Platinum,
}

// FIXED (2026-08-28, MULTIPLAYER_INTERACTION_AUDIT.md): converted to
// NetworkBehaviour, balances now broadcast via a SyncList (same pattern
// Lockbox.cs's own fix uses). Add/Spend/RestoreBalance keep their exact
// existing signatures/behavior unchanged -- they're building blocks
// meant to be called from server-side code (a Command elsewhere), not
// something that needs its own Command wrapper. This was only safe to
// turn on once the remaining direct (non-Command) callers were found and
// fixed -- PlayerIdentity's rename cost, PlayerBank's deposit/withdraw/
// exchange, Coin's pickup. NPCHiring's own wallet.Spend calls were
// already correctly isServer-guarded. VendorStall/Coin's own spawning
// (built via CreatePrimitive+AddComponent, never a real prefab -- see
// Lockbox.cs's identical gap) remain open, logged separately.
//
// Tracks the player's balance of each coin type, capped at MaxBalance per
// type.
[DisallowMultipleComponent]
public class PlayerCurrency : NetworkBehaviour
{
    public const int MaxBalance = 250;

    private static readonly int CoinTypeCount = System.Enum.GetValues(typeof(CoinType)).Length;

    private readonly int[] balances = new int[CoinTypeCount];

    public readonly SyncList<int> syncedBalances = new SyncList<int>();

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (syncedBalances.Count == 0)
            for (int i = 0; i < CoinTypeCount; i++) syncedBalances.Add(balances[i]);
    }

    private void Awake()
    {
        syncedBalances.Callback += OnSyncedBalancesChanged;

        // Starting purse — every character begins with a small amount of
        // the lower denominations rather than nothing at all.
        Add(CoinType.Copper, 20);
        Add(CoinType.Silver, 5);
        Add(CoinType.Gold, 1);
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

    public int GetBalance(CoinType type) => balances[(int)type];

    // Returns the amount that did NOT fit under the cap (0 means the full
    // amount was added) — same "leftover" convention as Inventory.AddItem,
    // so a Coin pickup can leave itself in the world when a type is maxed
    // instead of vanishing for nothing.
    public int Add(CoinType type, int amount)
    {
        if (amount <= 0) return Mathf.Max(0, amount);

        int index = (int)type;
        int space = MaxBalance - balances[index];
        int add = Mathf.Clamp(amount, 0, space);
        balances[index] += add;
        SyncBalance(index);
        return amount - add;
    }

    // Written by SaveManager on load — sets a balance directly from save
    // data rather than through Add's cap-and-leftover semantics.
    public void RestoreBalance(CoinType type, int amount)
    {
        int index = (int)type;
        balances[index] = Mathf.Clamp(amount, 0, MaxBalance);
        SyncBalance(index);
    }

    public bool Spend(CoinType type, int amount)
    {
        if (amount <= 0) return false;

        int index = (int)type;
        if (balances[index] < amount) return false;

        balances[index] -= amount;
        SyncBalance(index);
        return true;
    }
}
