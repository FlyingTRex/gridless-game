using UnityEngine;

// A physical coin lying in the world. Picking it up deposits straight into
// PlayerCurrency's matching balance and destroys the object — coins aren't
// inventory items, so there's no separate "carry then drop" step.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Coin : MonoBehaviour, IInteractable
{
    [SerializeField] private CoinType coinType;
    [SerializeField] private int amount = 1;

    public string Prompt => $"Pick up {coinType} Coin";
    public bool IsInstant => true;
    public float GetHoldDuration(GameObject player) => 0f;

    // Same purpose as Pickup.Configure — lets code that spawns a Coin at
    // runtime (PlayerCoinDrop) set its type/amount without needing a
    // dedicated prefab per coin type.
    public void Configure(CoinType type, int coinAmount)
    {
        coinType = type;
        amount = coinAmount;
    }

    public void Complete(GameObject player)
    {
        var currency = player.GetComponent<PlayerCurrency>();
        if (currency == null) return;

        int leftover = currency.Add(coinType, amount);
        if (leftover > 0)
        {
            // That coin type's balance is already at (or near) the cap —
            // leave the remainder as a coin in the world instead of
            // deleting value for nothing.
            amount = leftover;
            return;
        }

        Destroy(gameObject);
    }
}
