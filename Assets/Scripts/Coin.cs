using Mirror;
using UnityEngine;

// FIXED (2026-08-28, MULTIPLAYER_INTERACTION_AUDIT.md): converted to
// NetworkBehaviour, Complete() now dual-path dispatched. Note: Coin
// instances are still built via CreatePrimitive+AddComponent
// (PlayerCoinDrop.SpawnCoin), never a real prefab -- same gap
// Lockbox.cs's own fix flagged -- so a genuinely remote-spawned Coin
// still won't visually replicate to other clients until that's fixed
// too. This fix is still correct and necessary on its own: it stops a
// real client's own pickup from silently getting reverted by
// PlayerCurrency's now-networked balance sync.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Coin : NetworkBehaviour, IInteractable
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
        if (TryGetComponent<NetworkIdentity>(out _) && NetworkClient.active)
        {
            CmdComplete();
            return;
        }

        ServerComplete(player);
    }

    [Command(requiresAuthority = false)]
    private void CmdComplete(NetworkConnectionToClient sender = null)
    {
        if (sender == null || sender.identity == null) return;
        ServerComplete(sender.identity.gameObject);
    }

    public void ServerComplete(GameObject player)
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

        if (NetworkServer.active) NetworkServer.Destroy(gameObject);
        else Destroy(gameObject);
    }
}
