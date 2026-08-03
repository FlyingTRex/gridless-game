using UnityEngine;

public class Consumable : MonoBehaviour, IInteractable
{
    [SerializeField] private string label = "Berry";
    [SerializeField] private VitalType vital = VitalType.Hunger;
    [SerializeField] private float restoreAmount = 20f;
    [SerializeField] private bool destroyOnUse = true;

    public string Prompt => $"Consume {label}";
    public bool IsInstant => true;
    public float HoldDuration => 0f;

    public void Complete(GameObject player)
    {
        player.GetComponent<PlayerVitals>()?.Restore(vital, restoreAmount);
        if (destroyOnUse)
            Destroy(gameObject);
    }
}
