using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Canteen : MonoBehaviour, IInteractable, IEquippable
{
    // Excluded from the player's own camera while worn — see Backpack.cs for
    // why (turning to look at your own held/worn gear would otherwise fill
    // the screen with it from ~0.5 units away).
    private const int DefaultLayer = 0;
    private const int WornEquipmentLayer = 8;

    [SerializeField] private string canteenName = "Canteen";
    [SerializeField] private float capacity = 100f;
    [SerializeField] private float drinkAmount = 25f;
    [SerializeField] private Material emptyMaterial;
    [SerializeField] private Material filledMaterial;
    [SerializeField] private float fillRange = 2f;

    private Rigidbody body;
    private Collider col;
    private Renderer rend;
    private Color originalColor;
    private Material workingMaterial;

    public string DisplayName => canteenName;
    public LiquidType? Liquid { get; private set; }
    public float Amount { get; private set; }
    public float Capacity => capacity;
    public bool IsEmpty => Amount <= 0f;
    public bool IsFull => Liquid.HasValue && Amount >= capacity;

    public string Prompt => $"Pick up {canteenName}";
    public bool IsInstant => true;
    public float HoldDuration => 0f;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        rend = GetComponent<Renderer>();

        if (rend != null && emptyMaterial != null)
        {
            workingMaterial = new Material(emptyMaterial);
            rend.material = workingMaterial;
            originalColor = workingMaterial.color;
        }
    }

    public void Complete(GameObject player)
    {
        var carrier = player.GetComponent<PlayerCanteen>();
        carrier?.PickUp(this);
    }

    // A canteen only ever holds liquid, never items — filling replaces
    // whatever's in it unless it's already carrying a different liquid.
    // Only succeeds if there's a water source nearby.
    public bool Fill(LiquidType type)
    {
        if (Liquid.HasValue && Liquid.Value != type) return false;

        if (!HasNearbyWaterSource())
            return false;

        Liquid = type;
        Amount = capacity;
        UpdateVisuals();
        return true;
    }

    private bool HasNearbyWaterSource()
    {
        var colliders = Physics.OverlapSphere(transform.position, fillRange);
        foreach (var col in colliders)
        {
            if (col.GetComponent<IWaterSource>() != null)
                return true;
        }
        return false;
    }

    private void UpdateVisuals()
    {
        if (rend == null) return;

        if (IsEmpty)
        {
            if (emptyMaterial != null)
                rend.material = new Material(emptyMaterial);
        }
        else
        {
            if (filledMaterial != null)
                rend.material = new Material(filledMaterial);
            else if (workingMaterial != null)
            {
                workingMaterial.color = new Color(0.2f, 0.6f, 0.9f, 1f);
                rend.material = workingMaterial;
            }
        }
    }

    public bool Drink(PlayerVitals vitals)
    {
        if (!Liquid.HasValue || Amount <= 0f) return false;

        float used = Mathf.Min(drinkAmount, Amount);
        Amount -= used;
        vitals?.Restore(VitalType.Thirst, used);
        if (Amount <= 0f) Liquid = null;
        UpdateVisuals();
        return true;
    }

    // Fully hides the object while it's stashed in a regular inventory slot
    // rather than sitting in the world or carried in hand/on the belt.
    public void Stash()
    {
        transform.SetParent(null, false);
        gameObject.SetActive(false);
    }

    // Carried (visible, non-collidable, follows the player) when anchor is
    // set, or released back into the world as a normal physical object when
    // anchor is null.
    public void SetCarried(bool value, Transform anchor)
    {
        gameObject.SetActive(true);
        col.enabled = !value;
        body.isKinematic = value;
        SetLayerRecursively(transform, value ? WornEquipmentLayer : DefaultLayer);

        if (value)
        {
            transform.SetParent(anchor, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
        else
        {
            transform.SetParent(null, true);
        }
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root)
            SetLayerRecursively(child, layer);
    }
}
