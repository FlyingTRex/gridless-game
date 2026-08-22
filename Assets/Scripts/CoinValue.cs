// The 10:1 denomination ladder PlayerBank.Exchange already established
// (Copper->Iron->Silver->Gold->Platinum), made explicit and reusable
// (2026-08-22, Vendor Stall multi-denomination pricing) rather than
// re-derived ad hoc anywhere that needs to convert between a Copper-
// equivalent value and real coins.
public static class CoinValue
{
    public static int UnitValue(CoinType type) => type switch
    {
        CoinType.Copper => 1,
        CoinType.Iron => 10,
        CoinType.Silver => 100,
        CoinType.Gold => 1000,
        CoinType.Platinum => 10000,
        _ => 1,
    };

    // Highest-denomination-first -- the order CoinSpender's greedy
    // spend/denominate walk both use.
    public static readonly CoinType[] Descending =
    {
        CoinType.Platinum, CoinType.Gold, CoinType.Silver, CoinType.Iron, CoinType.Copper,
    };
}
