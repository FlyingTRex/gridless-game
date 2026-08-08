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
    // Must exceed PlayerInteraction.interactRange (3m) — otherwise the F/E
    // prompt can be visible (in range of the interact raycast) while still
    // outside this range, so HasNearbyWaterSource() silently fails with no
    // feedback to the player.
    [SerializeField] private float fillRange = 4f;

    private Rigidbody body;
    private Collider col;
    private Renderer[] renderers;
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
        // The mesh (Body/Cap) lives on child objects, not this root — a
        // plain GetComponent<Renderer>() here would find nothing.
        renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length > 0)
        {
            Material source = emptyMaterial != null ? emptyMaterial : renderers[0].sharedMaterial;
            if (source != null)
            {
                workingMaterial = new Material(source);
                originalColor = GetTint(workingMaterial);
                foreach (var r in renderers)
                    r.material = workingMaterial;
            }
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
        if (renderers == null || renderers.Length == 0) return;

        if (IsEmpty)
        {
            if (emptyMaterial != null)
            {
                var mat = new Material(emptyMaterial);
                foreach (var r in renderers) r.material = mat;
            }
            else if (workingMaterial != null)
            {
                SetTint(workingMaterial, originalColor);
            }
        }
        else
        {
            if (filledMaterial != null)
            {
                var mat = new Material(filledMaterial);
                foreach (var r in renderers) r.material = mat;
            }
            else if (workingMaterial != null)
            {
                SetTint(workingMaterial, new Color(0.2f, 0.6f, 0.9f, 1f));
            }
        }
    }

    // Material.color only affects the shader's "_Color" property. URP's Lit
    // shader (what Canteen.mat uses) renders from "_BaseColor" instead, so
    // plain .color assignments can silently do nothing — set both.
    private static void SetTint(Material mat, Color color)
    {
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.color = color;
    }

    private static Color GetTint(Material mat) =>
        mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;

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
        Despawn.CancelOn(gameObject);
        transform.SetParent(null, false);
        gameObject.SetActive(false);
    }

    // Carried (visible, non-collidable, follows the player) when anchor is
    // set, or released back into the world as a normal physical object when
    // anchor is null.
    public void SetCarried(bool value, Transform anchor)
    {
        if (value) Despawn.CancelOn(gameObject);

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
