using UnityEngine;

// Worn on Chest. Same shape as Backpack.cs (general-purpose cargo, not
// named/restricted sub-slots like Boot) — just a smaller capacity and a
// different body slot. First equippable with its own storage that isn't
// Back/Waist, so InventoryScreen.GetWornContainers() needed "Chest" added
// to the slot names it checks for an IInventoryHolder (2026-08-12).
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Shirt : MonoBehaviour, IInteractable, IInventoryHolder
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
    public string DisplayName => itemDefinition != null ? itemDefinition.itemName : "Shirt";

    public string Prompt => $"Pick up {DisplayName}";
    public bool IsInstant => true;
    public float GetHoldDuration(GameObject player) => 0f;

    public bool CanEquipToSlot(string slotName) => slotName == "Chest";

    private void Awake()
    {
        inventory = new Inventory(capacity);
        body = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    // MULTIPLAYER_INTERACTION_AUDIT.md follow-up (2026-08-31): routed
    // through PlayerInventory.RequestPickUpEquipment (a real Command) --
    // used to call PlayerShirt.PickUp directly, entirely client-side.
    public void Complete(GameObject player)
    {
        player.GetComponent<PlayerInventory>()?.RequestPickUpEquipment(this);
    }

    // Fully hides the object while it's stashed in a regular inventory slot
    // rather than sitting in the world or worn on the chest.
    public void Stash()
    {
        Despawn.CancelOn(gameObject);
        transform.SetParent(null, false);
        gameObject.SetActive(false);
    }

    // Worn on the chest (visible, non-collidable, follows the player) when
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
            // The model came back from Tripo3D in its worn/fitted shape
            // (a rigid torso pose, not flat cloth), so dropping it with no
            // rotation change left it standing upright in that same worn
            // shape, floating over the ground — read as oversized and
            // wrong (Ben's report, 2026-08-12). This lays it down instead:
            // the model's thin front-to-back axis (local X, ~0.42 units,
            // vs. ~1.0 for its height and shoulder width) becomes vertical
            // instead of its tall collar-to-hem axis, matching how an
            // actual discarded shirt reads lying on the ground rather than
            // still torso-shaped.
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
