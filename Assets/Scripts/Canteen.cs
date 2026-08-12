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
    public float GetHoldDuration(GameObject player) => 0f;

    // "Belt" is the sentinel PlayerCanteen uses for "the worn Belt's own
    // attachment points" — not a real PlayerEquipment slot name.
    public bool CanEquipToSlot(string slotName) =>
        slotName == "Left Hand" || slotName == "Right Hand" || slotName == "Belt";

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

    // Blue glow used for the filled state — additive emission rather than
    // just a _BaseColor tint, since the real metal canteen model's own
    // albedo is near-black; a base-color-only tint barely reads as blue
    // against that (multiply, not overlay). Emission actually glows blue
    // on top regardless of how dark the underlying material is. Pushed
    // well above 1.0 (HDR) — Ben's report that the first, sub-1.0 values
    // ((0.1, 0.35, 0.6)) were imperceptible outdoors in bright daylight;
    // this is deliberately strong enough to be unmistakable even without
    // a Bloom post-process pass to spread it further.
    private static readonly Color FilledTint = new Color(0.2f, 0.6f, 0.9f, 1f);
    private static readonly Color FilledEmission = new Color(0.5f, 2.5f, 5f, 1f);

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
                SetEmission(workingMaterial, Color.black);
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
                SetTint(workingMaterial, FilledTint);
                SetEmission(workingMaterial, FilledEmission);
            }
        }
    }

    // Material.color only affects the shader's "_Color" property. URP's Lit
    // shader uses "_BaseColor" instead; glTFast's own "Shader Graphs/
    // glTF-pbrMetallicRoughness" (what every Tripo3D-imported model
    // actually uses, including this Canteen's real model) uses neither —
    // it exposes "baseColorFactor"/"emissiveFactor" (glTF-spec names, no
    // underscore prefix). Checking all three rather than assuming one —
    // confirmed via HasProperty that the URP names silently no-op on a
    // glTFast material (found via ShaderUtil property dump after two
    // rounds of the tint/emission fix both doing nothing visible).
    private static readonly string[] BaseColorProperties = { "_BaseColor", "_Color", "baseColorFactor" };
    private static readonly string[] EmissionProperties = { "_EmissionColor", "emissiveFactor" };

    private static void SetTint(Material mat, Color color)
    {
        foreach (var prop in BaseColorProperties)
            if (mat.HasProperty(prop)) mat.SetColor(prop, color);
    }

    private static Color GetTint(Material mat)
    {
        foreach (var prop in BaseColorProperties)
            if (mat.HasProperty(prop)) return mat.GetColor(prop);
        return mat.color;
    }

    // Color.black effectively turns emission off (0 additive contribution)
    // without needing to track/toggle the _EMISSION keyword state per call.
    private static void SetEmission(Material mat, Color color)
    {
        bool any = false;
        foreach (var prop in EmissionProperties)
        {
            if (!mat.HasProperty(prop)) continue;
            any = true;
            mat.SetColor(prop, color);
        }
        if (!any) return;

        if (color == Color.black)
            mat.DisableKeyword("_EMISSION");
        else
            mat.EnableKeyword("_EMISSION");
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

    // Same mechanics as Drink (decrements Amount, empties out at 0) but
    // feeds a recipe instead of Thirst — added 2026-08-10 for
    // CraftingRecipe.requiresCanteenWater. Only succeeds against Water
    // specifically (a recipe combining herbs with, say, carried juice
    // wouldn't make sense) and only if amount is fully available — no
    // partial consumption, matching how ingredient counts work elsewhere.
    public bool ConsumeWater(float amount)
    {
        if (Liquid != LiquidType.Water || Amount < amount) return false;

        Amount -= amount;
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
