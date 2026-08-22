using System.Collections.Generic;
using UnityEngine;

// One price-list entry -- COMMERCE_PLANNING.md section 4. buyPrice is what
// the stall pays a visitor selling INTO it; sellPrice is what a visitor
// pays to buy OUT of it. Absence from a VendorStall's priceList means not
// tradeable there at all.
[System.Serializable]
public class VendorPriceEntry
{
    public ItemDefinition item;
    public int buyPrice;
    public int sellPrice;
    public bool canBuy = true;
    public bool canSell = true;

#if UNITY_EDITOR
    // Loud at author-time, not a hard runtime clamp (2026-08-21, "be mean"
    // pass before building) -- a hand-authored buyPrice >= sellPrice is a
    // real arbitrage exploit (sell -> buy -> sell in a loop drains the
    // till for free), but a deliberate promo/buyback event might
    // legitimately want an unusual spread later, so this warns instead of
    // forcing a clamp. Moot for anything ItemValueCalculator generates,
    // since that's spread-safe by construction.
    private void OnValidate()
    {
        if (item != null && sellPrice > 0 && buyPrice >= sellPrice)
            Debug.LogWarning($"VendorPriceEntry: '{item.name}' has buyPrice ({buyPrice}) "
                + $">= sellPrice ({sellPrice}) -- a visitor could sell then immediately buy "
                + "back for a free profit loop. Double-check this is intentional.");
    }
#endif
}

// The shared core transaction mechanic (COMMERCE_PLANNING.md section 4,
// MVP2B_PLANNING.md item 3) -- every vendor idea in the design is a thin
// driver around this one non-abstract component, none of them get their
// own price list, till, or screen.
[RequireComponent(typeof(SaveId))]
public class VendorStall : MonoBehaviour, IInteractable
{
    [SerializeField] private string stallName = "Vendor Stall";
    public string DisplayName => stallName;

    // IInteractable -- opens VendorStallScreen (transact mode only for
    // this tier, MVP2B_PLANNING.md item 4), same "world object owns the
    // interact, a player-side screen component owns the UI" shape
    // Furnace/FurnaceScreen already establish.
    public string Prompt => $"Open {DisplayName}";
    public bool IsInstant => true;
    public float GetHoldDuration(GameObject player) => 0f;
    public void Complete(GameObject player) => player.GetComponent<VendorStallScreen>()?.Open(this);

    // Stock -- a reference to an existing StorageBox, reused rather than a
    // new container type (same "assign an existing box" pattern NPC
    // deposit targeting already uses). Must be non-player-owned or the
    // whole buy/sell mechanic is pointless (see StorageBox.isPlayerOwned,
    // MVP2B_PLANNING.md item 1) -- enforced in AssignStock below rather
    // than trusted to whoever wires this up by hand.
    [SerializeField] private StorageBox stock;

    [SerializeField] private VendorPriceEntry[] priceList = System.Array.Empty<VendorPriceEntry>();

    private static readonly int CoinTypeCount = System.Enum.GetValues(typeof(CoinType)).Length;
    private readonly int[] till = new int[CoinTypeCount];

    // All V1 pricing is implicitly Copper (COMMERCE_PLANNING.md section 7,
    // "Multi-currency pricing" explicitly out of scope) -- the till still
    // tracks every CoinType (same shape as Lockbox's own balance array)
    // so it's ready the moment that changes, but SellToVisitor/
    // BuyFromVisitor below only ever touch this one type for now.
    private const CoinType PricingCoin = CoinType.Copper;

    public StorageBox Stock => stock;
    public IReadOnlyList<VendorPriceEntry> PriceList => priceList;

    public int GetTillBalance(CoinType type) => till[(int)type];

    // Called by a driver at setup time (Village Vendor, Player Stall,
    // ...) rather than trusting whoever wires the Inspector reference to
    // remember the ownership rule -- assigning stock through this method
    // is the one enforcement point for MVP2B_PLANNING.md item 1's whole
    // reason for existing.
    public void AssignStock(StorageBox box)
    {
        stock = box;
        stock?.SetPlayerOwned(false);
    }

    public void SetPriceList(VendorPriceEntry[] entries) => priceList = entries ?? System.Array.Empty<VendorPriceEntry>();

    // Direct till manipulation for a driver's own regen/seed logic (Village
    // Vendor's real-time faucet, Player Stall's owner deposits, ...) --
    // not part of a visitor transaction, so no atomicity concerns here.
    public void AddTillBalance(CoinType type, int amount)
    {
        if (amount <= 0) return;
        till[(int)type] += amount;
    }

    private VendorPriceEntry FindEntry(ItemDefinition item)
    {
        foreach (var entry in priceList)
            if (entry != null && entry.item == item) return entry;
        return null;
    }

    // Visitor is buying -- pays sellPrice x qty, receives the item. Fails
    // (no partial state) if: not in the price list / not for sale, out of
    // stock, the visitor's own inventory has no room, or the visitor can't
    // afford it. Every check runs before anything moves.
    public bool SellToVisitor(ItemDefinition item, int qty, PlayerCurrency wallet, Inventory visitorInventory)
    {
        if (item == null || qty <= 0 || wallet == null || visitorInventory == null || stock == null) return false;

        var entry = FindEntry(item);
        if (entry == null || !entry.canSell) return false;
        if (stock.Inventory.GetCount(item) < qty) return false;
        if (!visitorInventory.HasSpaceFor(item, qty)) return false;

        int cost = entry.sellPrice * qty;
        if (wallet.GetBalance(PricingCoin) < cost) return false;

        wallet.Spend(PricingCoin, cost);
        stock.Inventory.RemoveItem(item, qty);
        visitorInventory.AddItem(item, qty);
        till[(int)PricingCoin] += cost;
        return true;
    }

    // Visitor is selling -- gets paid buyPrice x qty, item leaves their
    // inventory. Fails (no partial state) if: not in the price list / not
    // buyable, the visitor doesn't actually have qty to sell, the stock
    // box has no room for the incoming item, or the till can't cover the
    // payout. Every check runs before anything moves -- this is the exact
    // "vendor full or can't hold it -> fail cleanly, no charge, inventory
    // untouched" rule Ben locked in before any code was written.
    public bool BuyFromVisitor(ItemDefinition item, int qty, PlayerCurrency wallet, Inventory visitorInventory)
    {
        if (item == null || qty <= 0 || wallet == null || visitorInventory == null || stock == null) return false;

        var entry = FindEntry(item);
        if (entry == null || !entry.canBuy) return false;
        if (visitorInventory.GetCount(item) < qty) return false;
        if (!stock.Inventory.HasSpaceFor(item, qty)) return false;

        int payout = entry.buyPrice * qty;
        if (till[(int)PricingCoin] < payout) return false;

        visitorInventory.RemoveItem(item, qty);
        stock.Inventory.AddItem(item, qty);
        till[(int)PricingCoin] -= payout;
        wallet.Add(PricingCoin, payout);
        return true;
    }
}
