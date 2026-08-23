using Mirror;
using UnityEngine;

// Multiplayer Phase 3 Bootstrap (MULTIPLAYER_PLANNING.md) -- the
// scope-shape decision this project made is "a solo session is just you
// hosting alone," not a separate single-player code path. Without this,
// clicking Play would leave NetworkManager sitting idle forever (no HUD
// exists in TestScene, and there shouldn't be one for real play) and any
// scene-placed NetworkIdentity stays deactivated until a server spawns it
// -- exactly the bug that broke Player earlier this session. Auto-starts
// hosting the instant the scene loads, so pressing Play keeps working
// exactly like it always has, just now genuinely running through Mirror
// underneath instead of a separate non-networked path.
public class NetworkAutoHost : MonoBehaviour
{
    private void Start()
    {
        if (NetworkServer.active || NetworkClient.active) return;

        var manager = GetComponent<NetworkManager>();
        if (manager == null) return;

        manager.StartHost();
        Debug.Log($"NetworkAutoHost: StartHost() called, NetworkServer.active={NetworkServer.active}, NetworkClient.active={NetworkClient.active}");
    }
}
