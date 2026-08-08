using UnityEngine;

// Worn at Waist. Unlike Backpack, a Belt has no general-purpose storage of
// its own — Points is a fixed number of generic attachment slots (any
// IEquippable attachment consumes exactly 1, regardless of kind), scaling
// with the Belt's own CraftTier (see BUGS_AND_ENHANCEMENTS.md's Belt entry
// for the full design). Today the only attachment that actually exists is
// a Canteen (see PlayerCanteen) — Scabbard/Pouch/Holster are still open
// design questions, not built.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Belt : MonoBehaviour, IInteractable, IInventoryHolder
{
    // See Backpack.cs for why worn gear sits on this layer.
    private const int DefaultLayer = 0;
    private const int WornEquipmentLayer = 8;

    [SerializeField] private ItemDefinition itemDefinition;
    [SerializeField] private int points = 6;

    private Inventory pointsInventory;
    private Rigidbody body;
    private Collider col;

    public Inventory Inventory => pointsInventory;
    public ItemDefinition ItemDefinition => itemDefinition;
    public string DisplayName => itemDefinition != null ? itemDefinition.itemName : "Belt";

    public string Prompt => $"Pick up {DisplayName}";
    public bool IsInstant => true;
    public float HoldDuration => 0f;

    private void Awake()
    {
        pointsInventory = new Inventory(points);
        body = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void Complete(GameObject player)
    {
        var carrier = player.GetComponent<PlayerBelt>();
        carrier?.PickUp(this);
    }

    public void Stash()
    {
        Despawn.CancelOn(gameObject);
        transform.SetParent(null, false);
        gameObject.SetActive(false);
    }

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
