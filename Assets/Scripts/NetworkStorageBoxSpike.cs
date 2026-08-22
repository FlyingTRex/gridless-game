using Mirror;
using UnityEngine;

// Multiplayer Phase 1 pilot (2026-08-22, MULTIPLAYER_PLANNING.md section 3
// item 2) -- validates "a world object with synced Inventory state,
// interacted with via a server-validated Command" once, cheaply, before
// repeating the pattern across every real interactable (StorageBox,
// Campfire, Lockbox, ...). Deliberately NOT the real StorageBox.cs/
// Inventory -- same "throwaway pilot, not the real 48-script stack" call
// NetworkSpikeMovement.cs already made for movement. A SyncList<string> of
// item names stands in for a real Inventory's slot list; that's the whole
// shape being proven here (server-owned state, client-requested mutation,
// automatic replication to every observer), not the real capacity/stacking/
// ItemDefinition machinery.
public class NetworkStorageBoxSpike : NetworkBehaviour
{
    public readonly SyncList<string> items = new SyncList<string>();

    // Plain OnGUI, always drawn -- this is the throwaway pilot scene, no
    // NetworkSpikePlayer reference to gate visibility on proximity the way
    // a real screen would. Confirms the actual thing under test (does
    // every observing client see the SyncList update live) without
    // building UI machinery this pilot doesn't need.
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 260, 200));
        GUILayout.Label($"{name} contents ({items.Count}):");
        foreach (var item in items) GUILayout.Label($"- {item}");
        GUILayout.Label("E = add TestOre, R = remove top (within 3m)");
        GUILayout.EndArea();
    }
}
