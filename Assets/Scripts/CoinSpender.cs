using System.Collections.Generic;

// Shared multi-denomination spend/payout logic (2026-08-22, Vendor Stall
// design) -- works against any coin-holding source via plain delegates
// (PlayerCurrency.GetBalance/Spend, Lockbox.GetBalance/Remove,
// PlayerBank.GetBalance/SpendDirect) rather than a shared interface, so
// none of those three existing types needed their public API reshaped.
//
// Deliberately the SIMPLE half of a real change-making problem, not the
// full one -- see CLAUDE.md's own note on this: it only ever spends WHOLE
// coins a source actually holds, greedily from the highest denomination
// down, and never "breaks" a large coin into smaller change. A source
// that holds enough TOTAL value but in a shape that can't be spent
// without breaking a coin (e.g. exactly one Gold piece, needing to cover
// 500c) will report failure rather than succeed via a more clever
// algorithm. Accepted trade-off -- real coin-breaking is a meaningfully
// harder problem (loop/rounding/partial-state risk) that wasn't worth
// building for this pass.
public static class CoinSpender
{
    // Attempts to cover `amount` (a Copper-equivalent value) using only
    // whole coins the source already holds. All-or-nothing: either finds
    // a complete plan and commits it (calling `spend` for each entry), or
    // finds none and commits nothing. Returns the plan either way (empty
    // on failure) so a caller can inspect what WOULD have been spent
    // without necessarily committing (see TryPlan below for the
    // non-committing variant).
    public static bool TrySpend(int amount, System.Func<CoinType, int> getBalance, System.Func<CoinType, int, bool> spend)
    {
        if (!TryPlan(amount, getBalance, out var plan)) return false;

        foreach (var (type, count) in plan)
            spend(type, count); // already validated available by TryPlan -- expected to always succeed

        return true;
    }

    // Non-committing dry run -- computes the same greedy plan without
    // touching any balance. Used when a caller needs to know whether a
    // spend WOULD succeed before deciding which source to draw from (e.g.
    // VendorStall trying the wallet first, falling back to Bank only if
    // the wallet's own plan comes up short).
    public static bool TryPlan(int amount, System.Func<CoinType, int> getBalance, out List<(CoinType type, int count)> plan)
    {
        plan = new List<(CoinType, int)>();
        if (amount <= 0) return true;

        int remaining = amount;
        foreach (var type in CoinValue.Descending)
        {
            int unit = CoinValue.UnitValue(type);
            int maxUnits = remaining / unit;
            if (maxUnits <= 0) continue;

            int have = getBalance(type);
            int use = have < maxUnits ? have : maxUnits;
            if (use <= 0) continue;

            plan.Add((type, use));
            remaining -= use * unit;
        }

        return remaining <= 0;
    }

    // The other direction -- breaking a Copper-equivalent value DOWN into
    // real coins to hand over (a payout, a till receiving payment). Unlike
    // TryPlan, this never fails -- straight digit decomposition against
    // the exact 10:1 ladder always represents any non-negative integer
    // exactly, with no "does the source have enough" question (the payer
    // already validated that via TrySpend before this is ever called).
    public static List<(CoinType type, int count)> Denominate(int amount)
    {
        var result = new List<(CoinType, int)>();
        int remaining = amount;
        foreach (var type in CoinValue.Descending)
        {
            int unit = CoinValue.UnitValue(type);
            int count = remaining / unit;
            if (count > 0)
            {
                result.Add((type, count));
                remaining -= count * unit;
            }
        }
        return result;
    }
}
