using Mirror;
using UnityEngine;

// Multiplayer chunk 6 (MULTIPLAYER_PLANNING.md section 3 item 6),
// 2026-08-25: repurposed from its original "always auto-host the instant
// Play starts, no choice" behavior (Phase 3 Bootstrap) into a real
// Host/Join startup screen -- Ben's explicit call over keeping the old
// silent auto-host default, so two genuinely separate players (not
// sharing a machine) have a real way to choose "I'm hosting" vs "I'm
// joining someone else's game" instead of every instance unconditionally
// starting its own separate server. The old auto-host behavior meant a
// second real connection was never actually reachable through normal
// play at all -- there was no path that didn't immediately self-host.
//
// Class/file name deliberately kept as NetworkAutoHost rather than
// renamed to ConnectScreen -- a rename changes what Unity's serializer
// resolves an existing scene component reference to, and re-wiring that
// safely needs the Editor open. This is still the exact same component
// instance already placed on the NetworkManager GameObject in
// TestScene.unity; only its behavior changed.
//
// This screen is intentionally NOT on the Player prefab (unlike every
// other *Screen.cs in this project) -- no Player object exists yet on a
// joining client's machine before a connection is actually established,
// so it lives on the same GameObject as NetworkManager instead, and is
// the one screen in the project that doesn't need an isLocalPlayer gate
// (there's no local player to gate against yet).
public class NetworkAutoHost : MonoBehaviour
{
    private NetworkManager manager;
    private string joinAddress = "localhost";
    private string statusMessage;
    private float statusSetTime;

    // Mirror raises OnClientError followed by OnClientDisconnect for the
    // SAME failed connection attempt -- without this, the specific error
    // ("Connection failed: <reason>") would get immediately stomped by
    // the generic "Disconnected from server" a frame or two later.
    private const float StatusStickySeconds = 2f;

    private void Awake()
    {
        manager = GetComponent<NetworkManager>();
    }

    // Called by GridlessNetworkManager's OnClientError override -- always
    // wins, since a specific error is more useful than whatever was
    // showing before.
    public void ShowStatus(string message)
    {
        statusMessage = message;
        statusSetTime = Time.time;
    }

    // Called by GridlessNetworkManager's OnClientDisconnect override --
    // a generic message that backs off if a more specific one (from
    // OnClientError, moments earlier) is still fresh.
    public void ShowStatusIfNotRecentlySet(string message)
    {
        if (Time.time - statusSetTime < StatusStickySeconds) return;
        ShowStatus(message);
    }

    private void OnGUI()
    {
        if (manager == null) return;

        // Once a connection is active (hosting, or a client that's
        // connected or still mid-handshake), there's nothing for this
        // screen to do -- get out of the way. A failed/dropped connection
        // clears both flags again via Mirror's own disconnect handling,
        // which brings this screen back automatically.
        if (NetworkServer.active || NetworkClient.active) return;

        const float width = 340f;
        const float height = 190f;
        var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);

        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Space(6f);
        GUILayout.Label("Gridless", DebugGUI.Header);
        GUILayout.Space(10f);

        if (GUILayout.Button("Host", GUILayout.Height(32f)))
        {
            statusMessage = null;
            manager.StartHost();
        }

        GUILayout.Space(14f);
        GUILayout.Label("Join by IP address:");
        joinAddress = GUILayout.TextField(joinAddress);
        GUILayout.Space(4f);

        if (GUILayout.Button("Connect", GUILayout.Height(32f)))
        {
            string address = string.IsNullOrWhiteSpace(joinAddress) ? "localhost" : joinAddress.Trim();
            manager.networkAddress = address;
            statusMessage = $"Connecting to {address}...";
            manager.StartClient();
        }

        if (statusMessage != null)
        {
            GUILayout.Space(8f);
            GUILayout.Label(statusMessage);
        }

        GUILayout.EndArea();
    }
}
