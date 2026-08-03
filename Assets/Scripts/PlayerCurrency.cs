using UnityEngine;

public enum CoinType
{
    Copper,
    Iron,
    Silver,
    Gold,
    Platinum,
}

// Tracks the player's balance of each coin type. No earn/spend mechanic
// exists yet — this is the ledger a future one would read/write; every
// balance just starts at 0.
[DisallowMultipleComponent]
public class PlayerCurrency : MonoBehaviour
{
    private static readonly int CoinTypeCount = System.Enum.GetValues(typeof(CoinType)).Length;

    private readonly int[] balances = new int[CoinTypeCount];

    public int GetBalance(CoinType type) => balances[(int)type];

    public void Add(CoinType type, int amount)
    {
        if (amount <= 0) return;
        balances[(int)type] += amount;
    }

    public bool Spend(CoinType type, int amount)
    {
        if (amount <= 0) return false;

        int index = (int)type;
        if (balances[index] < amount) return false;

        balances[index] -= amount;
        return true;
    }
}
