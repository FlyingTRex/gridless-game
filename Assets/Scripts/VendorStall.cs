using System.Collections.Generic;
using UnityEngine;

// One price-list entry -- COMMERCE_PLANNING.md section 4. buyPrice is what
// the stall pays a visitor selling INTO it; sellPrice is what a visitor
// pays to buy OUT of it. Absence from a VendorStall's priceList means not
// tradeable there at all. Both prices are Copper-equivalent values --
// actual payment/payout is denominated into real coins at transaction
// time by CoinSpender, not stored per-denomination here.
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

    // Till -- a real Lockbox (2026-08-22, replacing a bare Copper-only
    // int[5]) rather than a bespoke balance array. Reuses Lockbox's
    // already-built per-type capacity/tier scaling instead of reinventing
    // it, same "reuse an existing type" pattern `stock` above already
    // uses for StorageBox. A driver decides the Lockbox's own tier
    // (VillageVendor ties it to its linked Flag's tier).
    [SerializeField] private Lockbox till;

    [SerializeField] private VendorPriceEntry[] priceList = System.Array.Empty<VendorPriceEntry>();

    // Off-list buying margin -- BuyFromVisitor's fallback below pays this
    // much under ItemValueCalculator's live value, same 20% a driver's own
    // generated price-list entries already use (VillageVendor.Margin).
    // Kept here (not duplicated per-driver) since off-list pricing is core
    // VendorStall behavior now, not something each driver reimplements.
    private const float OffListBuyMargin = 0.2f;

    // Tier ceiling for off-list buying (2026-08-21, "be mean" pass before
    // building) -- a driver sets this from its own settlement-progress
    // signal (VillageVendor reads its linked Flag's tier). Defaults to
    // Masterwork (unrestricted) for a driver with no tier concept at all.
    // Exists specifically to close a real exploit: without it, any item
    // flagged sellableByVendor for an unrelated reason (a Masterwork tool,
    // say) could be dumped on a tiny Crude-tier outpost's vendor the
    // moment it's flagged, regardless of whether that vendor could ever
    // plausibly afford or deal in something that valuable -- the same
    // tier gate already protects what a vendor stocks FOR SALE, this
    // closes the matching gap on the buying side.
    public CraftTier MaxOffListBuyTier { get; set; } = CraftTier.Masterwork;

    // Supply/demand price adjustment (Ben's idea, 2026-08-22, "mean
    // enhancement" pass) -- a flat factor on the whole transaction, based
    // on stock level BEFORE it happens, not a per-unit integral across
    // qty. Simpler than pricing each unit of a multi-item purchase
    // separately as stock shifts mid-transaction, and still delivers the
    // real effect asked for: the vendor pays less for an item it's
    // already sitting on plenty of, and charges more for one running low.
    // Bounded deliberately narrow (+/-30%) so it nudges price rather than
    // swinging it wildly enough to reopen the exact arbitrage risk
    // VendorPriceEntry's own OnValidate warning exists to catch.
    private static float StockAdjustmentFactor(int currentStock, int maxStack, bool vendorIsBuying)
    {
        float ratio = maxStack > 0 ? Mathf.Clamp01((float)currentStock / maxStack) : 0f;
        return vendorIsBuying
            ? Mathf.Lerp(1f, 0.7f, ratio)   // has none -> pays full; has plenty -> pays 70%
            : Mathf.Lerp(1.3f, 1f, ratio);  // nearly out -> charges 130%; fully stocked -> charges full
    }

    // Display-only price estimate for an off-list item (2026-08-22) --
    // exposed so VendorStallScreen can show a real number before the
    // player commits to a sale, without duplicating OffListBuyMargin's
    // value on the UI side. Same formula BuyFromVisitor's own fallback
    // branch uses; returns 0 for anything not actually off-list-sellable
    // here (not flagged, or above this stall's tier ceiling).
    public int EstimateOffListBuyPrice(ItemDefinition item)
    {
        if (item == null || !item.sellableByVendor || item.tier > MaxOffListBuyTier) return 0;
        return Mathf.Max(0, Mathf.RoundToInt(ItemValueCalculator.GetBaseValue(item) * (1f - OffListBuyMargin)));
    }

    public StorageBox Stock => stock;
    public Lockbox Till => till;
    public IReadOnlyList<VendorPriceEntry> PriceList => priceList;

    public int GetTillBalance(CoinType type) => till != null ? till.GetBalance(type) : 0;

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

    public void AssignTill(Lockbox box) => till = box;

    public void SetPriceList(VendorPriceEntry[] entries) => priceList = entries ?? System.Array.Empty<VendorPriceEntry>();

    // Direct till manipulation for a driver's own regen/seed logic (Village
    // Vendor's real-time faucet, Player Stall's owner deposits, ...) --
    // not part of a visitor transaction, so no atomicity concerns here.
    public void AddTillBalance(CoinType type, int amount) => till?.Add(type, amount);

    private VendorPriceEntry FindEntry(ItemDefinition item)
    {
        foreach (var entry in priceList)
            if (entry != null && entry.item == item) return entry;
        return null;
    }

    // Shared multi-inventory helpers (2026-08-22, real bug found live --
    // VendorStallScreen previously passed only the player's bare main
    // inventory, so items held in a worn Backpack or a nearby StorageBox
    // were invisible to both directions of trade). Same "reach" shape
    // PlayerCrafting.ReachableInventories/RemoveIngredients already
    // established -- a visitor's "inventory" for trading purposes is
    // however many Inventory instances the caller decides are reachable,
    // not just one.
    private static int SumCount(IReadOnlyList<Inventory> inventories, ItemDefinition item)
    {
        int total = 0;
        foreach (var inv in inventories)
            if (inv != null) total += inv.GetCount(item);
        return total;
    }

    private static Inventory FindDestination(IReadOnlyList<Inventory> inventories, ItemDefinition item, int qty)
    {
        foreach (var inv in inventories)
            if (inv != null && inv.HasSpaceFor(item, qty)) return inv;
        return null;
    }

    private static void RemoveDistributed(IReadOnlyList<Inventory> inventories, ItemDefinition item, int qty)
    {
        foreach (var inv in inventories)
        {
            if (qty <= 0) return;
            if (inv == null) continue;
            int have = inv.GetCount(item);
            if (have <= 0) continue;
            int take = Mathf.Min(have, qty);
            inv.RemoveItem(item, take);
            qty -= take;
        }
    }

    // True if `till` has room for every entry in a denominated plan --
    // checked before committing a payment INTO the till so a genuinely
    // huge sale can fail cleanly instead of silently losing coins over
    // the Lockbox's own per-type capacity.
    private bool TillHasRoomFor(List<(CoinType type, int count)> plan)
    {
        if (till == null) return false;
        foreach (var (type, count) in plan)
            if (till.GetBalance(type) + count > till.CapacityPerType) return false;
        return true;
    }

    // Visitor is buying -- pays sellPrice x qty, receives the item. Fails
    // (no partial state) if: not in the price list / not for sale, out of
    // stock, none of the visitor's reachable inventories has room, the
    // visitor can't afford it from wallet or Bank, or the till has no
    // room for the incoming payment. Every check runs before anything
    // moves. Received items land in the FIRST reachable inventory with
    // room (main inventory first, per the caller's own ordering) --
    // unlike selling, there's nothing to distribute, the item just needs
    // one valid destination.
    //
    // Payment (2026-08-22, multi-denomination pricing): tries the wallet
    // first (CoinSpender.TrySpend, whole-coin-only greedy spend, see
    // CoinSpender's own header for why it can fail even when the wallet
    // holds enough TOTAL value). If that fails and a Bank Box has been
    // unlocked (BankBox.Exists), tries paying directly from the Bank
    // instead (bypassing the wallet entirely), charging PlayerBank's own
    // per-transaction fee on top -- same "Bank access is earned, not
    // free" rule the whole Bank Box gate exists to enforce elsewhere.
    public bool SellToVisitor(ItemDefinition item, int qty, PlayerCurrency wallet, IReadOnlyList<Inventory> visitorInventories, PlayerBank bank = null)
    {
        if (item == null || qty <= 0 || wallet == null || visitorInventories == null || stock == null || till == null) return false;

        var entry = FindEntry(item);
        if (entry == null || !entry.canSell) return false;
        int currentStock = stock.Inventory.GetCount(item);
        if (currentStock < qty) return false;

        var destination = FindDestination(visitorInventories, item, qty);
        if (destination == null) return false;

        float factor = StockAdjustmentFactor(currentStock, item.maxStack, vendorIsBuying: false);
        int cost = Mathf.RoundToInt(entry.sellPrice * qty * factor);

        var plan = CoinSpender.Denominate(cost);
        if (!TillHasRoomFor(plan)) return false;

        bool paidFromWallet = CoinSpender.TrySpend(cost, wallet.GetBalance, wallet.Spend);
        bool paidFromBank = false;
        if (!paidFromWallet && bank != null && BankBox.Exists)
        {
            int fee = PlayerBank.FeeFor(cost);
            paidFromBank = CoinSpender.TrySpend(cost + fee, bank.GetBalance, bank.SpendDirect);
        }
        if (!paidFromWallet && !paidFromBank) return false;

        stock.Inventory.RemoveItem(item, qty);
        destination.AddItem(item, qty);
        foreach (var (type, count) in plan)
            till.Add(type, count);
        return true;
    }

    // Visitor is selling -- gets paid buyPrice x qty, item leaves their
    // inventory. Fails (no partial state) if: not in the price list / not
    // buyable, the visitor doesn't actually have qty to sell, the stock
    // box has no room for the incoming item, the till can't cover the
    // payout in whole coins it actually holds, or the payout would
    // overflow the wallet with no Bank available to catch the excess.
    // Every check runs before anything moves -- this is the exact "vendor
    // full or can't hold it -> fail cleanly, no charge, inventory
    // untouched" rule Ben locked in before any code was written.
    //
    // Payout (2026-08-22): denominated from the till's own real coin
    // balances (CoinSpender.TrySpend against the Lockbox), then handed to
    // the wallet type by type. Any single denomination that would push
    // the wallet past its cap routes the OVERFLOW portion straight to
    // Bank instead of the old behavior (PlayerCurrency.Add's leftover
    // silently discarded -- a real bug found live and fixed here). If no
    // Bank Box is unlocked yet, an overflowing sale is refused outright
    // rather than losing currency.
    public bool BuyFromVisitor(ItemDefinition item, int qty, PlayerCurrency wallet, IReadOnlyList<Inventory> visitorInventories, PlayerBank bank = null)
    {
        if (item == null || qty <= 0 || wallet == null || visitorInventories == null || stock == null || till == null) return false;
        if (SumCount(visitorInventories, item) < qty) return false;
        if (!stock.Inventory.HasSpaceFor(item, qty)) return false;

        var entry = FindEntry(item);
        int unitPrice;
        if (entry != null)
        {
            if (!entry.canBuy) return false;
            unitPrice = entry.buyPrice;
        }
        else
        {
            // Off-list fallback (2026-08-21, "be mean" pass before
            // building) -- an item that isn't currently one of this
            // stall's displayed/stocked entries can still be sold here if
            // it's flagged sellableByVendor, priced live off
            // ItemValueCalculator rather than refusing outright. Gated by
            // MaxOffListBuyTier so a low-tier vendor still can't buy
            // something well outside what it could plausibly afford or
            // deal in, closing the exploit a totally open-ended "buy
            // anything sellable" rule would leave.
            if (!item.sellableByVendor || item.tier > MaxOffListBuyTier) return false;
            unitPrice = Mathf.Max(0, Mathf.RoundToInt(ItemValueCalculator.GetBaseValue(item) * (1f - OffListBuyMargin)));
        }

        int currentStock = stock.Inventory.GetCount(item);
        float factor = StockAdjustmentFactor(currentStock, item.maxStack, vendorIsBuying: true);
        int payout = Mathf.RoundToInt(unitPrice * qty * factor);

        if (!CoinSpender.TryPlan(payout, till.GetBalance, out var tillPlan)) return false;

        bool bankUnlocked = bank != null && BankBox.Exists;
        var walletPlan = CoinSpender.Denominate(payout);
        var overflow = new List<(CoinType type, int amount)>();
        foreach (var (type, count) in walletPlan)
        {
            int over = wallet.GetBalance(type) + count - PlayerCurrency.MaxBalance;
            if (over > 0)
            {
                if (!bankUnlocked) return false; // would silently lose currency -- refuse instead
                overflow.Add((type, over));
            }
        }

        // Everything validated -- commit.
        foreach (var (type, count) in tillPlan)
            till.Remove(type, count);
        RemoveDistributed(visitorInventories, item, qty);
        stock.Inventory.AddItem(item, qty);

        foreach (var (type, count) in walletPlan)
        {
            int over = 0;
            foreach (var (oType, oAmount) in overflow)
                if (oType == type) { over = oAmount; break; }

            int toWallet = count - over;
            if (toWallet > 0) wallet.Add(type, toWallet);
            if (over > 0) bank.DepositDirect(type, over);
        }

        return true;
    }
}
