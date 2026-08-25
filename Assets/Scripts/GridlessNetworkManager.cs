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
    // Nearby-player-joined announcement radius (2026-08-25, Ben's ask).
    // Fog of war still hides the arrival on the Map -- this is just a
    // toast so existing players know someone's out there.
    [SerializeField] private float nearbyPlayerAnnounceRadius = 1000f;

    // AddPlayerForConnection must happen after the client has signaled
    // ready (NetworkClient.Ready()), not on raw connect -- found live,
    // 2026-08-22: doing it from OnServerConnect threw "NetworkClient
    // can't AddPlayer before being ready," since the ready handshake
    // hasn't completed yet at that point even in host mode. OnServerReady
    // is the correct hook; call base first so the normal SetClientReady
    // bookkeeping still happens.
    //
    // Real per-connection spawning (2026-08-25, MULTIPLAYER_PLANNING.md
    // section 3 item 6): until now this always handed the ONE
    // pre-existing scene Player to whichever connection asked first and
    // explicitly refused a second connection anything at all -- found
    // while building chunk 5b's OnStopServer loop, logged in
    // BUGS_AND_ENHANCEMENTS.md. The scene Player is a genuinely
    // connected instance of Assets/Prefabs/Player.prefab (confirmed via
    // PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot), so a fresh
    // Instantiate(playerPrefab) for every connection AFTER the first is
    // structurally identical to it -- same NetworkIdentity,
    // NetworkTransformReliable (client-authoritative), and all 48
    // PlayerXXX.cs components, now all isLocalPlayer-gated (see
    // FirstPersonController's own field comment) so a second real Player
    // object doesn't fight the first one for local input/UI on either
    // machine. The FIRST connection still claims the pre-existing scene
    // object rather than switching everyone to prefab-spawned -- keeps
    // solo/host testing behavior (position, any scene-specific tweaks)
    // exactly as it's always been.
    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        base.OnServerReady(conn);

        var scenePlayer = FindFirstObjectByType<FirstPersonController>();
        if (scenePlayer == null)
        {
            Debug.LogError("GridlessNetworkManager: no Player found in scene.");
            return;
        }

        var sceneIdentity = scenePlayer.GetComponent<NetworkIdentity>();
        if (sceneIdentity == null)
        {
            Debug.LogError("GridlessNetworkManager: Player has no NetworkIdentity.");
            return;
        }

        GameObject spawned;
        if (sceneIdentity.connectionToClient == null)
        {
            // First connection: claim the pre-existing scene Player, same
            // as always.
            NetworkServer.AddPlayerForConnection(conn, scenePlayer.gameObject);
            spawned = scenePlayer.gameObject;
        }
        else
        {
            if (playerPrefab == null)
            {
                Debug.LogError("GridlessNetworkManager: playerPrefab not assigned -- can't spawn a second player.");
                return;
            }

            // Simple side-by-side offset so a second player doesn't spawn
            // literally inside the first -- not a real spawn-point system,
            // just enough to unblock a live two-connection test.
            Vector3 spawnPos = scenePlayer.transform.position + scenePlayer.transform.right * 2f;
            var instance = Instantiate(playerPrefab, spawnPos, scenePlayer.transform.rotation);
            NetworkServer.AddPlayerForConnection(conn, instance);
            spawned = instance;
        }

        AnnounceNearbyPlayers(spawned, conn);
    }

    private void AnnounceNearbyPlayers(GameObject arriving, NetworkConnectionToClient arrivingConn)
    {
        var arrivingIdentityComp = arriving.GetComponent<PlayerIdentity>();
        string arrivingName = arrivingIdentityComp != null ? arrivingIdentityComp.DisplayName : null;

        foreach (var other in FindObjectsByType<FirstPersonController>(FindObjectsSortMode.None))
        {
            if (other.gameObject == arriving) continue;

            var otherIdentity = other.GetComponent<NetworkIdentity>();
            var otherConn = otherIdentity != null ? otherIdentity.connectionToClient : null;
            if (otherConn == null || otherConn == arrivingConn) continue;

            if (Vector3.Distance(other.transform.position, arriving.transform.position) > nearbyPlayerAnnounceRadius)
                continue;

            var otherPlayerIdentity = other.GetComponent<PlayerIdentity>();
            otherPlayerIdentity?.TargetNotifyNearbyPlayerJoined(otherConn, arrivingName);
        }
    }

    // Persistence restructure chunk 5b (MULTIPLAYER_PLANNING.md section
    // 3 item 5), 2026-08-24: save-on-disconnect. Must run BEFORE
    // base.OnServerDisconnect -- Mirror's own default implementation
    // calls NetworkServer.DestroyPlayerForConnection(conn), which clears
    // the connection's association with its player before we'd get a
    // chance to read it. Saves whichever player this specific connection
    // owned, not every connected player -- OnStopServer below is the
    // save-everyone case.
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        var saveManager = conn.identity != null ? conn.identity.GetComponent<SaveManager>() : null;
        saveManager?.Save();

        base.OnServerDisconnect(conn);
    }

    // Save-on-shutdown -- the server itself stopping (not just one
    // connection dropping), e.g. a dedicated server process exiting or
    // leaving Play mode. Iterates every SaveManager actually present
    // rather than assuming "the" single player -- forward-compatible
    // with real per-connection player spawning once that exists (today
    // there's only ever the one pre-existing scene Player, see this
    // class's own header comment, so this loop runs once in practice).
    public override void OnStopServer()
    {
        foreach (var saveManager in FindObjectsByType<SaveManager>(FindObjectsSortMode.None))
            saveManager.Save();

        base.OnStopServer();
    }
}
