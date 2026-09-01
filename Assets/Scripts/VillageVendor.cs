using System.Collections.Generic;
using Mirror;
using UnityEngine;

// The lowest-risk VendorStall driver (COMMERCE_PLANNING.md section 5,
// MVP2B_PLANNING.md item 5) -- no owner, no Fame math, proves the whole
// mechanic in single-player with the fewest moving parts. Pricing is
// entirely ItemValueCalculator-driven, not hand-authored, converging onto
// the same mechanism the Traveling Trader will need later.
//
// MULTIPLAYER_INTERACTION_AUDIT.md follow-up (2026-08-31): converted from
// MonoBehaviour to NetworkBehaviour and Update() gained an isServer guard
// -- previously every connected client independently ticked its own local
// restock/till-regen timers and mutated the (already-networked) stock
// StorageBox/till Lockbox directly, an uncoordinated per-machine
// simulation, the exact same Class A shape already fixed on Furnace/
// Campfire's own Update() loops. The stall's own prefab already carries a
// NetworkIdentity (VendorStallPiece.prefab), so this is a safe conversion.
[RequireComponent(typeof(VendorStall))]
public class VillageVendor : NetworkBehaviour
{
    private const float FullRefreshIntervalSeconds = 30f * 60f; // 30 real minutes
    private const float TillRegenIntervalSeconds = 30f;
    private const int TillRegenAmount = 1; // per CoinType, per tick (2026-08-22 -- was Copper-only)
    private const int GeneralSlotCount = 6;
    private const int SeedSlotCount = 2;
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

        stall.MaxOffListBuyTier = CurrentMaxTier();

        bool restored = stall.Stock != null;

        if (restored)
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

        if (stall.Till == null)
            EnsureTillLockbox();

        // Only start a fresh 30-minute countdown on a genuinely new
        // stall -- RestoreFullRefreshTimer already set nextFullRefreshTime
        // from saved data during SaveManager.Load(), which ran before this
        // method (Initialize is deliberately deferred to the first
        // Update() tick, see this class's own header comment on why).
        // Unconditionally overwriting it here would silently reset the
        // timer to a full 30 minutes on every reload.
        if (!restored)
            nextFullRefreshTime = Time.time + FullRefreshIntervalSeconds;

        nextTillRegenTime = Time.time + TillRegenIntervalSeconds;
        nextReactiveCheckTime = Time.time + ReactiveCheckIntervalSeconds;
    }

    private void Update()
    {
        if (!isServer) return;

        if (!initialized)
        {
            // A ghost preview (PlayerBuilding.ShowGhost) fully Instantiates
            // armedPiece.prefab, colliders/BuildSockets stripped but every
            // gameplay component -- including this one -- still fully live.
            // Without this guard, merely aiming a Vendor Stall placement
            // creates a real stock box and rolls real stock as a side
            // effect, before the player ever confirms anything -- found
            // live 2026-08-22 alongside the matching ghost bug in
            // PlayerBuilding.NearestFlagAlreadyHasStructure. PlacedPiece is
            // only ever added by PlayerBuilding.Confirm on a genuine
            // placement, never present on a ghost.
            if (GetComponent<PlacedPiece>() == null) return;
            Initialize();
            return;
        }

        stall.MaxOffListBuyTier = CurrentMaxTier();

        if (Time.time >= nextTillRegenTime)
        {
            nextTillRegenTime = Time.time + TillRegenIntervalSeconds;
            // All 5 CoinTypes at once (2026-08-22, Ben's ask) -- Lockbox
            // .Add already clamps to CapacityPerType per type, no manual
            // cap check needed the way the old Copper-only int[] did.
            foreach (CoinType type in System.Enum.GetValues(typeof(CoinType)))
                stall.AddTillBalance(type, TillRegenAmount);
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
        // Collider present only because StorageBox.RequireComponent needs
        // one -- disabled immediately (2026-08-22, real bug found live):
        // this is a purely programmatic data container the player should
        // never be able to raycast-interact with directly. PlayerInteraction
        // finds the target via GetComponentInParent<IInteractable> on
        // whatever collider the crosshair ray hits first -- with this
        // enabled, an internal child collider sitting right at the Vendor
        // Stall's own position could shadow the stall's own IInteractable
        // and surface as a default-named "Storage Box" instead. Doesn't
        // affect StorageBox.FindNearby (transform-distance based, no
        // Collider dependency), and the box is already non-player-owned
        // (AssignStock below) as defense-in-depth either way.
        var stockCollider = go.AddComponent<BoxCollider>();
        stockCollider.enabled = false;
        stockBox = go.AddComponent<StorageBox>();
        // RequireComponent auto-adds SaveId, but a script-driven
        // AddComponent isn't guaranteed to fire that SaveId's own Reset()
        // (see SaveId.cs's own header comment) -- without this explicit
        // call, its id silently stays empty, and a real live save/reload
        // test found exactly that: stockSaveId came back null in the save
        // file, so RestoreVendorStalls had nothing to reattach to and the
        // vendor re-rolled completely fresh on every reload. Every other
        // creation site in this codebase (RestorePlacedPieces, RestoreNpcs)
        // already calls this explicitly -- this one just never had it.
        stockBox.GetComponent<SaveId>()?.GenerateIfMissing();
        stall.AssignStock(stockBox);
    }

    // Till Lockbox (2026-08-22, replacing the old Copper-only int[]) --
    // same "raycast-hittable internal collider can shadow the stall's own
    // IInteractable" fix as EnsureStockBox above, since this is also a
    // purely programmatic container the player should never directly
    // open. Tier scales with the linked Flag's own tier (Ben's call --
    // ties till capacity to the same settlement-growth signal already
    // gating what's stocked/off-list-buyable), falling back to Crude if
    // no Flag exists yet, matching CurrentMaxTier's own safer-failure
    // direction.
    private void EnsureTillLockbox()
    {
        if (stall.Till != null) return;

        var go = new GameObject($"{name} Till");
        go.transform.SetParent(transform, false);
        var tillCollider = go.AddComponent<BoxCollider>();
        tillCollider.enabled = false;
        var lockbox = go.AddComponent<Lockbox>();
        lockbox.Configure(CurrentMaxTier());
        // Same GenerateIfMissing fix as EnsureStockBox above -- same bug,
        // same root cause (RequireComponent's auto-added SaveId doesn't
        // reliably get its Reset() called from a script-driven AddComponent).
        lockbox.GetComponent<SaveId>()?.GenerateIfMissing();
        stall.AssignTill(lockbox);
    }

    // Seconds until the next full stock reroll (2026-08-22, Ben's ask --
    // same "payment due in Ns" display convention NPCHiringScreen already
    // uses for NPCHiring.WorkTimeRemaining). Persisted explicitly (see
    // SaveManager.CaptureVendorStall/RestoreVendorStalls) rather than
    // left to silently reset to a full 30 minutes on every reload, same
    // "obviously the timer should be persistent over saves" call Ben made
    // for the Village Flag spawn timer back on 2026-08-21.
    public float NextFullRefreshSeconds => Mathf.Max(0f, nextFullRefreshTime - Time.time);

    // Called by SaveManager.RestoreVendorStalls -- sets the timer so it
    // resumes from where it was saved rather than restarting a fresh
    // 30-minute countdown.
    public void RestoreFullRefreshTimer(float secondsRemaining) =>
        nextFullRefreshTime = Time.time + Mathf.Max(0f, secondsRemaining);

    // Current Flag tier gates what's eligible -- same pattern the
    // Traveling Trader already uses for Fame band, applied to Flag tier
    // instead. No Flag placed at all falls back to the lowest tier
    // (Crude) rather than defaulting to "everything available," the
    // safer failure direction.
    private CraftTier CurrentMaxTier() => linkedFlag != null ? linkedFlag.Tier : CraftTier.Crude;

    private List<ItemDefinition> GetEligiblePool(bool seedPool)
    {
        var result = new List<ItemDefinition>();
        var database = ItemDatabase.Instance;
        if (database == null) return result;

        CraftTier maxTier = CurrentMaxTier();
        foreach (var item in database.AllItems)
            if (item != null && item.sellableByVendor && item.isSeed == seedPool && item.tier <= maxTier)
                result.Add(item);
        return result;
    }

    // Picks up to `count` DISTINCT items from pool (no repeats within one
    // category -- 2026-08-22, Ben's ask) and adds each at a random
    // quantity from 1 up to its own maxStack (e.g. Iron Ore, maxStack 20,
    // could land anywhere 1-20, not always a flat StockPerItem). If pool
    // has fewer than `count` eligible items (a real case at a low Flag
    // tier), fills what it can rather than repeating one item into
    // multiple slots -- incidentally fixes the old duplicate-with-
    // replacement problem at low tiers for free.
    private void RollDistinctEntries(List<ItemDefinition> pool, int count, List<VendorPriceEntry> entries)
    {
        var available = new List<ItemDefinition>(pool);
        for (int i = 0; i < count && available.Count > 0; i++)
        {
            int index = Random.Range(0, available.Count);
            var chosen = available[index];
            available.RemoveAt(index);

            int quantity = Random.Range(1, Mathf.Max(1, chosen.maxStack) + 1);
            entries.Add(BuildEntry(chosen));
            stockBox.Inventory.AddItem(chosen, quantity);
        }
    }

    // "Complete refresh" -- clears the existing offering and stock
    // entirely, then re-rolls fresh against the Flag's current tier.
    // Deliberately discards whatever didn't sell rather than topping up
    // alongside it, matching the literal "complete" framing. 6 general
    // slots + 2 dedicated seed slots, each category distinct within
    // itself (a general item and a seed can't collide anyway, different
    // pools).
    private void FullRefresh()
    {
        stockBox.Inventory.Clear();
        var entries = new List<VendorPriceEntry>();
        RollDistinctEntries(GetEligiblePool(seedPool: false), GeneralSlotCount, entries);
        RollDistinctEntries(GetEligiblePool(seedPool: true), SeedSlotCount, entries);
        stall.SetPriceList(entries.ToArray());
    }

    // Whenever a slot's item sells out, roll a fresh item into that same
    // slot (not a replenish of the same one) -- keeps a popular slot from
    // sitting empty for up to 30 minutes waiting on the full refresh.
    // Replacement stays within the same category (general vs. seed) the
    // sold-out slot belonged to, and excludes whatever's still stocked in
    // OTHER slots so the distinct-items rule holds after a reactive
    // reroll too, not just right after a FullRefresh.
    private void CheckReactiveRestock()
    {
        var priceList = stall.PriceList;
        List<VendorPriceEntry> mutated = null;

        for (int i = 0; i < priceList.Count; i++)
        {
            var entry = priceList[i];
            if (entry?.item == null) continue;
            if (stockBox.Inventory.GetCount(entry.item) > 0) continue;

            bool seedSlot = entry.item.isSeed;
            var pool = GetEligiblePool(seedPool: seedSlot);
            for (int j = 0; j < priceList.Count; j++)
                if (j != i && priceList[j]?.item != null)
                    pool.Remove(priceList[j].item);
            if (pool.Count == 0) continue;

            mutated ??= new List<VendorPriceEntry>(priceList);
            var chosen = pool[Random.Range(0, pool.Count)];
            int quantity = Random.Range(1, Mathf.Max(1, chosen.maxStack) + 1);
            mutated[i] = BuildEntry(chosen);
            stockBox.Inventory.AddItem(chosen, quantity);
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
