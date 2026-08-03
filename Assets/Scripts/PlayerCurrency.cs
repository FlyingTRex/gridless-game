using UnityEngine;

public enum CoinType
{
    Copper,
    Iron,
    Silver,
    Gold,
    Platinum,
}

// Tracks the player's balance of each coin type, capped at MaxBalance per
// type. No earn/spend mechanic beyond Coin pickups exists yet — Spend is
// ready for whatever wants to draw on it later.
[DisallowMultipleComponent]
public class PlayerCurrency : MonoBehaviour
{
    public const int MaxBalance = 250;

    private static readonly int CoinTypeCount = System.Enum.GetValues(typeof(CoinType)).Length;

    private readonly int[] balances = new int[CoinTypeCount];

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
        return amount - add;
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
