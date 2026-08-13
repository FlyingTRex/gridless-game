using UnityEngine;

// Worn on Leg. Same shape as Shirt.cs (general-purpose cargo via its own
// pockets, not named/restricted sub-slots like Boot) — just a different
// body slot. Shared by both the "Settler's Jeans" (auto-equipped at spawn)
// and plain "Jeans" ItemDefinitions/prefabs, same pattern as Boot.cs being
// reused across Civilian/Hiking/Military Boots (2026-08-12).
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Jeans : MonoBehaviour, IInteractable, IInventoryHolder
{
    // See Backpack.cs for why worn gear sits on this layer.
    private const int DefaultLayer = 0;
    private const int WornEquipmentLayer = 8;

    [SerializeField] private ItemDefinition itemDefinition;
    [SerializeField] private int capacity = 4;

    private Inventory inventory;
    private Rigidbody body;
    private Collider col;

    public Inventory Inventory => inventory;
    public ItemDefinition ItemDefinition => itemDefinition;
    public string DisplayName => itemDefinition != null ? itemDefinition.itemName : "Jeans";

    public string Prompt => $"Pick up {DisplayName}";
    public bool IsInstant => true;
    public float GetHoldDuration(GameObject player) => 0f;

    public bool CanEquipToSlot(string slotName) => slotName == "Leg";

    private void Awake()
    {
        inventory = new Inventory(capacity);
        body = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void Complete(GameObject player)
    {
        var carrier = player.GetComponent<PlayerJeans>();
        carrier?.PickUp(this);
    }

    // Fully hides the object while it's stashed in a regular inventory slot
    // rather than sitting in the world or worn on the legs.
    public void Stash()
    {
        Despawn.CancelOn(gameObject);
        transform.SetParent(null, false);
        gameObject.SetActive(false);
    }

    // Worn on the legs (visible, non-collidable, follows the player) when
    // anchor is set, or released back into the world as a normal physical
    // object when anchor is null.
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
            // Same lesson as Shirt.cs (2026-08-12): the model comes back
            // from Tripo3D in its worn/fitted shape (rigid legs in a
            // standing pose, not flat cloth), so dropping it with no
            // rotation change would leave it standing upright in that same
            // worn shape instead of reading as a discarded pair of jeans.
            // Lie it down — actual rotation/axis confirmed against this
            // model's real measured bounds before this shipped (see
            // CLAUDE.md's "scale every generated model against the
            // player" rule), not assumed from the Shirt's numbers.
            transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        }
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root)
            SetLayerRecursively(child, layer);
    }
}
