using Mirror;
using UnityEngine;

// FIXED (2026-08-28, MULTIPLAYER_INTERACTION_AUDIT.md): converted to
// NetworkBehaviour -- balances lived in plain fields, mutated directly
// from LockboxScreen's deposit/withdraw buttons with no Command
// anywhere. Add/Remove are now server-authoritative (RequestDeposit/
// RequestWithdraw Commands), and each coin type's balance is its own
// [SyncVar] (only 5 of them, simpler than a SyncList for a fixed-size
// array this small). Deposit/withdraw still also touches the player's
// own PlayerCurrency wallet, which is a separate, still-open gap
// (MULTIPLAYER_INTERACTION_AUDIT.md) -- fixing that is tracked
// independently, not blocking this fix.
//
// A personal coin-storage container, purchased from the bank in one of
// five CraftTier qualities. Unlike PlayerBank (a single global account),
// each Lockbox is its own world object with its own balances — buy two
// and they don't share capacity.
// SaveId requirement added 2026-08-22 (Vendor Stall till design) -- a
// VendorStall's till is now a real Lockbox, and it needs to persist
// through save/reload the same as any other placed structure. Standalone
// player-purchased Lockbox persistence remains a separate, pre-existing,
// deliberately-deferred gap (see SAVE_LOAD_PLANNING.md's own "Lockbox...
// deferred" note) -- this only guarantees a Lockbox CAN be found/restored
// by SaveId going forward, not that every Lockbox use case is wired up.
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(SaveId))]
public class Lockbox : NetworkBehaviour, IInteractable, IRenameable
{
    [SerializeField] private CraftTier tier;
    // Renamed via PlayerRenaming.CmdRename (already a real Command,
    // resolves any IRenameable target server-side) -- needed a real
    // [SyncVar] for that to actually replicate anywhere beyond the
    // renaming player's own screen.
    [SyncVar] private string customName;

    private static readonly int CoinTypeCount = System.Enum.GetValues(typeof(CoinType)).Length;
    private readonly int[] balances = new int[CoinTypeCount];

    // Server-owned, broadcast to every observer -- index-matched to
    // CoinType, same as `balances` above.
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

    public CraftTier Tier => tier;

    // Baseline (Normal) holds 2500 of each coin type; other tiers scale by
    // CraftTierScale.Modifier.
    public int CapacityPerType => Mathf.RoundToInt(2500f * CraftTierScale.Modifier(tier));

    public string DisplayName => string.IsNullOrEmpty(customName)
        ? CraftTierNames.WithPrefix(tier, "Lockbox")
        : customName;

    public string Prompt => $"Open {DisplayName}";
    public bool IsInstant => true;
    public float GetHoldDuration(GameObject player) => 0f;

    // Set once, right after Instantiate, by whatever spawned this (the
    // bank purchase flow) — mirrors Pickup.Configure/Coin.Configure.
    public void Configure(CraftTier newTier)
    {
        tier = newTier;
    }

    public int GetBalance(CoinType type) => balances[(int)type];

    // Returns the amount that did NOT fit under this box's capacity for
    // that coin type (0 means the full amount was added) — same leftover
    // convention as Inventory.AddItem/PlayerCurrency.Add.
    public int Add(CoinType type, int amount)
    {
        if (amount <= 0) return Mathf.Max(0, amount);

        int index = (int)type;
        int space = CapacityPerType - balances[index];
        int add = Mathf.Clamp(amount, 0, space);
        balances[index] += add;
        if (isServer && index < syncedBalances.Count) syncedBalances[index] = balances[index];
        return amount - add;
    }

    public bool Remove(CoinType type, int amount)
    {
        if (amount <= 0) return false;

        int index = (int)type;
        if (balances[index] < amount) return false;

        balances[index] -= amount;
        if (isServer && index < syncedBalances.Count) syncedBalances[index] = balances[index];
        return true;
    }

    public void Complete(GameObject player)
    {
        player.GetComponent<LockboxScreen>()?.Open(this);
    }

    // FIXED (2026-08-28): a single atomic deposit/withdraw Command
    // covering both this box's own balance AND the calling player's
    // PlayerCurrency wallet -- LockboxScreen's Deposit/Withdraw button
    // used to mutate both directly, client-local, no Command at all.
    public void RequestTransaction(GameObject player, CoinType type, int amount, bool isDeposit)
    {
        if (TryGetComponent<NetworkIdentity>(out _) && NetworkClient.active)
        {
            CmdTransaction(type, amount, isDeposit);
            return;
        }

        ServerTransaction(player, type, amount, isDeposit);
    }

    [Command(requiresAuthority = false)]
    private void CmdTransaction(CoinType type, int amount, bool isDeposit, NetworkConnectionToClient sender = null)
    {
        if (sender == null || sender.identity == null) return;
        ServerTransaction(sender.identity.gameObject, type, amount, isDeposit);
    }

    private void ServerTransaction(GameObject player, CoinType type, int amount, bool isDeposit)
    {
        var wallet = player != null ? player.GetComponent<PlayerCurrency>() : null;
        if (wallet == null || amount <= 0) return;

        if (isDeposit)
        {
            if (!wallet.Spend(type, amount)) return;
            int leftover = Add(type, amount);
            if (leftover > 0) wallet.Add(type, leftover); // shouldn't happen given the UI's own clamp, but never lose coins
        }
        else
        {
            if (!Remove(type, amount)) return;
            wallet.Add(type, amount);
        }
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return;
        customName = newName.Trim();
    }
}
