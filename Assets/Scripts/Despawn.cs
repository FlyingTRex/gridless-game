using UnityEngine;

// Attached at runtime (not pre-authored on a prefab) to anything the
// player drops that doesn't already have its own despawn concept —
// equipment (Backpack/Belt/Canteen/Sunglasses/NavigationComputer/
// PersonalHealthMonitor/MiningFaceShield, via PlayerDropping.DropFrom's
// equipment branch) and dropped Coins (PlayerCoinDrop.SpawnCoin). Plain
// stackable items have their own separate mechanism (Pickup.Configure's
// despawnAt field) and don't use this.
//
// Uses an absolute Time.time deadline, not elapsed active-time — that
// matters for equipment specifically, since Stash() deactivates the
// GameObject (pausing Update) but a re-equip later reactivates it. A
// deadline already in the past would otherwise fire immediately on
// reactivation and destroy a WORN item. Equipment carriers avoid this by
// destroying their Despawn component at the top of Stash() (the first
// thing that happens on every pickup path) rather than relying on
// inactivity to "pause" the timer.
public class Despawn : MonoBehaviour
{
    public float delay = 120f;

    private float despawnAt;

    private void Start()
    {
        despawnAt = Time.time + delay;
    }

    private void Update()
    {
        if (Time.time >= despawnAt)
            Destroy(gameObject);
    }

    // Called from Stash()/SetCarried(true, ...) by anything that might
    // have a Despawn attached from having been dropped — safe no-op if
    // there isn't one. A single shared helper instead of duplicating the
    // get-null-check-destroy pattern in every equippable class.
    public static void CancelOn(GameObject target)
    {
        var despawn = target.GetComponent<Despawn>();
        if (despawn != null) Destroy(despawn);
    }
}
