using UnityEngine;

// Marks this object as a location where the player can fill a canteen.
// Canteen checks for this within fillRange when trying to fill (see
// Canteen.HasNearbyWaterSource). Also offers direct interaction: drinking
// straight from the source is always available, and filling an equipped
// water carrier is offered as a second option whenever the player has one
// equipped that isn't already full.
public class WaterSource : MonoBehaviour, IWaterSource, IInteractable, ISecondaryInteractable
{
    [SerializeField] private float drinkAmount = 25f;

    public string Prompt => "Drink";
    public bool IsInstant => true;
    public float GetHoldDuration(GameObject player) => 0f;

    // FIXED (2026-08-28, found live -- "still can't ... drink from the
    // water"): called Restore() directly, violating PlayerVitals' own
    // stated invariant that every mutating call comes from server-side
    // already. Worked by coincidence for a host; silently did nothing
    // for a real remote client.
    public void Complete(GameObject player)
    {
        player.GetComponent<PlayerVitals>()?.RequestRestore(VitalType.Thirst, drinkAmount);
    }

    public string GetSecondaryPrompt(GameObject player)
    {
        var carrier = player.GetComponent<PlayerCanteen>()?.Equipped;
        if (carrier == null || carrier.IsFull) return null;
        return $"Fill {carrier.DisplayName}";
    }

    // FIXED (2026-08-28, found live -- "still can't fill a canteen"):
    // this called Equipped.Fill(...) directly, bypassing PlayerCanteen's
    // own RequestFill()/CmdFill() Command entirely -- a leftover from
    // before that Command existed, never migrated when it was added.
    // Worked by coincidence for a host (same process as the server);
    // silently did nothing for a real remote client, since a direct
    // field write on a non-authoritative machine doesn't replicate.
    public void CompleteSecondary(GameObject player)
    {
        player.GetComponent<PlayerCanteen>()?.RequestFill();
    }
}
