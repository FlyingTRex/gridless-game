using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Backpack : MonoBehaviour, IInteractable, IInventoryHolder
{
    // Excluded from the player's own camera (see Main Camera's cullingMask in
    // TestScene) so worn gear doesn't fill the screen if you turn to look at
    // your own back. Only applied while worn — SetCarried resets it to Default
    // on drop/unequip so a world-sitting backpack stays visible.
    private const int DefaultLayer = 0;
    private const int WornEquipmentLayer = 8;

    [SerializeField] private ItemDefinition itemDefinition;
    [SerializeField] private int capacity = 8;

    private Inventory inventory;
    private Rigidbody body;
    private Collider col;

    public Inventory Inventory => inventory;
    public ItemDefinition ItemDefinition => itemDefinition;
    public string DisplayName => itemDefinition != null ? itemDefinition.itemName : "Backpack";

    public string Prompt => $"Pick up {DisplayName}";
    public bool IsInstant => true;
    public float GetHoldDuration(GameObject player) => 0f;

    private void Awake()
    {
        inventory = new Inventory(capacity);
        body = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void Complete(GameObject player)
    {
        var carrier = player.GetComponent<PlayerBackpack>();
        carrier?.PickUp(this);
    }

    // Fully hides the object while it's stashed in a regular inventory slot
    // rather than sitting in the world or worn on the back.
    public void Stash()
    {
        Despawn.CancelOn(gameObject);
        transform.SetParent(null, false);
        gameObject.SetActive(false);
    }

    // Worn on the back (visible, non-collidable, follows the player) when
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
        }
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root)
            SetLayerRecursively(child, layer);
    }
}
