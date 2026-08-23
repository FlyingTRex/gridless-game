using Mirror;
using UnityEngine;

// Multiplayer Phase 3 sub-phase 2 (MULTIPLAYER_PLANNING.md), 2026-08-22 --
// a real gap found while designing the first Command: Player is currently
// a server-owned scene NetworkIdentity with no client authority assigned
// to any connection (autoCreatePlayer is deliberately off, since Player
// pre-exists in the scene rather than needing to be spawned fresh from a
// playerPrefab). Without authority, ANY [Command] called from Player's own
// NetworkBehaviours would fail -- Mirror requires hasAuthority on the
// calling side. AddPlayerForConnection works for an already-existing
// scene identity too, not just a freshly instantiated prefab, so this is
// the correct fix rather than switching to autoCreatePlayer=true (which
// would try to Instantiate a NEW player, duplicating the real one).
public class GridlessNetworkManager : NetworkManager
{
    // AddPlayerForConnection must happen after the client has signaled
    // ready (NetworkClient.Ready()), not on raw connect -- found live,
    // 2026-08-22: doing it from OnServerConnect threw "NetworkClient
    // can't AddPlayer before being ready," since the ready handshake
    // hasn't completed yet at that point even in host mode. OnServerReady
    // is the correct hook; call base first so the normal SetClientReady
    // bookkeeping still happens.
    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        base.OnServerReady(conn);

        var player = FindFirstObjectByType<FirstPersonController>();
        if (player == null)
        {
            Debug.LogError("GridlessNetworkManager: no Player found to assign connection authority to.");
            return;
        }

        var identity = player.GetComponent<NetworkIdentity>();
        if (identity == null)
        {
            Debug.LogError("GridlessNetworkManager: Player has no NetworkIdentity.");
            return;
        }

        if (identity.connectionToClient != null)
        {
            // Already owned (e.g. a second connection later, once real
            // per-connection player spawning exists) -- not this
            // connection's player, don't steal authority.
            return;
        }

        NetworkServer.AddPlayerForConnection(conn, player.gameObject);
    }
}
