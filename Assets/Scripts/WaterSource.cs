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
    public float HoldDuration => 0f;

    public void Complete(GameObject player)
    {
        player.GetComponent<PlayerVitals>()?.Restore(VitalType.Thirst, drinkAmount);
    }

    public string GetSecondaryPrompt(GameObject player)
    {
        var carrier = player.GetComponent<PlayerCanteen>()?.Equipped;
        if (carrier == null || carrier.IsFull) return null;
        return $"Fill {carrier.DisplayName}";
    }

    public void CompleteSecondary(GameObject player)
    {
        player.GetComponent<PlayerCanteen>()?.Equipped?.Fill(LiquidType.Water);
    }
}
