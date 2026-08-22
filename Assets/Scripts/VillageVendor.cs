using System.Collections.Generic;
using UnityEngine;

// The lowest-risk VendorStall driver (COMMERCE_PLANNING.md section 5,
// MVP2B_PLANNING.md item 5) -- no owner, no Fame math, proves the whole
// mechanic in single-player with the fewest moving parts. Pricing is
// entirely ItemValueCalculator-driven, not hand-authored, converging onto
// the same mechanism the Traveling Trader will need later.
[RequireComponent(typeof(VendorStall))]
public class VillageVendor : MonoBehaviour
{
    private const float FullRefreshIntervalSeconds = 30f * 60f; // 30 real minutes
    private const float TillRegenIntervalSeconds = 30f;
    private const int TillRegenAmount = 1;
    private const int TillCap = 500; // proposed starting numbers, unverified against real prices -- expect retuning
    private const int StockSlotCount = 8;
    private const int StockPerItem = 5;
    private const float Margin = 0.2f; // sellPrice = base*(1+margin), buyPrice = base*(1-margin)
    private const float ReactiveCheckIntervalSeconds = 1f;

    [SerializeField] private VillageFlag linkedFlag;
    [SerializeField] private int stockBoxCapacity = 40;

    private VendorStall stall;
    private StorageBox stockBox;
    private float nextFullRefreshTime;
    private float nextTillRegenTime;
    private float nextReactiveCheckTime;

    private bool initialized;

    private void Awake()
    {
        stall = GetComponent<VendorStall>();
    }

    // Deliberately NOT in Start() (2026-08-21, MVP2B_PLANNING.md item 6).
    // SaveManager.Load() also runs during some object's Start() (its
    // own), and Unity gives no ordering guarantee between different
    // objects' Start() calls -- if this ran in Start() too, it could just
    // as easily fire before SaveManager has restored this stall's saved
    // stock/price list as after, silently clobbering a real save with a
    // fresh random roll. Unity DOES guarantee every object's Start()
    // completes before any object's Update() begins, so deferring to the
    // first Update() tick is a reliable way to run "after any restore
    // that was going to happen, definitely already happened."
    private void Initialize()
    {
        initialized = true;

        if (linkedFlag == null)
            linkedFlag = Object.FindFirstObjectByType<VillageFlag>();

        if (stall.Stock != null)
        {
            // SaveManager.RestoreVendorStalls already ran and populated
            // this stall (stock box + price list) -- don't stomp it with
            // a fresh roll.
            stockBox = stall.Stock;
        }
        else
        {
            EnsureStockBox();
            FullRefresh();
        }

        nextFullRefreshTime = Time.time + FullRefreshIntervalSeconds;
        nextTillRegenTime = Time.time + TillRegenIntervalSeconds;
        nextReactiveCheckTime = Time.time + ReactiveCheckIntervalSeconds;
    }

    private void Update()
    {
        if (!initialized)
        {
            Initialize();
            return;
        }

        if (Time.time >= nextTillRegenTime)
        {
            nextTillRegenTime = Time.time + TillRegenIntervalSeconds;
            if (stall.GetTillBalance(CoinType.Copper) < TillCap)
                stall.AddTillBalance(CoinType.Copper, TillRegenAmount);
        }

        if (Time.time >= nextFullRefreshTime)
        {
            nextFullRefreshTime = Time.time + FullRefreshIntervalSeconds;
            FullRefresh();
        }

        if (Time.time >= nextReactiveCheckTime)
        {
            nextReactiveCheckTime = Time.time + ReactiveCheckIntervalSeconds;
            CheckReactiveRestock();
        }
    }

    private void EnsureStockBox()
    {
        if (stall.Stock != null)
        {
            stockBox = stall.Stock;
            return;
        }

        var go = new GameObject($"{name} Stock");
        go.transform.SetParent(transform, false);
        go.AddComponent<BoxCollider>();
        stockBox = go.AddComponent<StorageBox>();
        stall.AssignStock(stockBox);
    }

    // Current Flag tier gates what's eligible -- same pattern the
    // Traveling Trader already uses for Fame band, applied to Flag tier
    // instead. No Flag placed at all falls back to the lowest tier
    // (Crude) rather than defaulting to "everything available," the
    // safer failure direction.
    private CraftTier CurrentMaxTier() => linkedFlag != null ? linkedFlag.Tier : CraftTier.Crude;

    private List<ItemDefinition> GetEligiblePool()
    {
        var result = new List<ItemDefinition>();
        var database = ItemDatabase.Instance;
        if (database == null) return result;

        CraftTier maxTier = CurrentMaxTier();
        foreach (var item in database.AllItems)
            if (item != null && item.sellableByVendor && item.tier <= maxTier)
                result.Add(item);
        return result;
    }

    // "Complete refresh" -- clears the existing offering and stock
    // entirely, then re-rolls fresh against the Flag's current tier.
    // Deliberately discards whatever didn't sell rather than topping up
    // alongside it, matching the literal "complete" framing.
    private void FullRefresh()
    {
        stockBox.Inventory.Clear();
        var pool = GetEligiblePool();
        var entries = new List<VendorPriceEntry>();

        for (int i = 0; i < StockSlotCount && pool.Count > 0; i++)
        {
            var chosen = pool[Random.Range(0, pool.Count)];
            entries.Add(BuildEntry(chosen));
            stockBox.Inventory.AddItem(chosen, StockPerItem);
        }

        stall.SetPriceList(entries.ToArray());
    }

    // Whenever a slot's item sells out, roll a fresh item into that same
    // slot (not a replenish of the same one) -- keeps a popular slot from
    // sitting empty for up to 30 minutes waiting on the full refresh.
    private void CheckReactiveRestock()
    {
        var priceList = stall.PriceList;
        List<VendorPriceEntry> mutated = null;

        for (int i = 0; i < priceList.Count; i++)
        {
            var entry = priceList[i];
            if (entry?.item == null) continue;
            if (stockBox.Inventory.GetCount(entry.item) > 0) continue;

            var pool = GetEligiblePool();
            if (pool.Count == 0) continue;

            mutated ??= new List<VendorPriceEntry>(priceList);
            var chosen = pool[Random.Range(0, pool.Count)];
            mutated[i] = BuildEntry(chosen);
            stockBox.Inventory.AddItem(chosen, StockPerItem);
        }

        if (mutated != null)
            stall.SetPriceList(mutated.ToArray());
    }

    // Found live via the functional test (2026-08-21): for a low
    // baseValue (Fiber = 1), +/-20% rounds both prices to the same
    // integer (1 and 1) -- silently breaking the "generated entries are
    // spread-safe by construction" guarantee this whole system exists to
    // provide, exactly the arbitrage risk the OnValidate warning on
    // hand-authored entries was meant to catch, just via a different
    // path (generated, not hand-typed). Fixed with a hard floor: if
    // rounding ever collapses the spread, force sellPrice at least 1
    // above buyPrice.
    private VendorPriceEntry BuildEntry(ItemDefinition item)
    {
        float baseValue = ItemValueCalculator.GetBaseValue(item);
        int buyPrice = Mathf.Max(0, Mathf.RoundToInt(baseValue * (1f - Margin)));
        int sellPrice = Mathf.Max(1, Mathf.RoundToInt(baseValue * (1f + Margin)));
        if (sellPrice <= buyPrice) sellPrice = buyPrice + 1;

        return new VendorPriceEntry
        {
            item = item,
            buyPrice = buyPrice,
            sellPrice = sellPrice,
            canBuy = true,
            canSell = true,
        };
    }
}
