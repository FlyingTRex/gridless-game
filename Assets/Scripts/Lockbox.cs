using UnityEngine;

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
public class Lockbox : MonoBehaviour, IInteractable, IRenameable
{
    [SerializeField] private CraftTier tier;
    [SerializeField] private string customName;

    private static readonly int CoinTypeCount = System.Enum.GetValues(typeof(CoinType)).Length;
    private readonly int[] balances = new int[CoinTypeCount];

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
        return amount - add;
    }

    public bool Remove(CoinType type, int amount)
    {
        if (amount <= 0) return false;

        int index = (int)type;
        if (balances[index] < amount) return false;

        balances[index] -= amount;
        return true;
    }

    public void Complete(GameObject player)
    {
        player.GetComponent<LockboxScreen>()?.Open(this);
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return;
        customName = newName.Trim();
    }
}
