using Mirror;
using UnityEngine;

// Multiplayer Phase 3 sub-phase 2 (2026-08-23) -- shared by every call
// site that Instantiates a real, interactable item/equippable instance
// (a world drop, craft output, save/load restore, a written skill book)
// so each one doesn't need to duplicate the same NetworkIdentity+
// NetworkServer.active check. Found live: PlayerCrafting's own equipment
// output was missed on the first pass (only PlayerDropping.SpawnPickup
// had this originally), causing "Attempted to serialize unspawned
// GameObject" when a crafted, worn item's NetworkIdentity got referenced
// in a Command. Deliberately NOT used by purely cosmetic clones
// (NPCEquipmentVisual's bone-parented, physics-disabled NPC gear
// display) that never need to be independently network-addressable.
public static class NetworkSpawnHelper
{
    public static void SpawnIfNetworked(GameObject instance)
    {
        if (instance != null && instance.TryGetComponent<NetworkIdentity>(out _) && NetworkServer.active)
            NetworkServer.Spawn(instance);
    }
}
