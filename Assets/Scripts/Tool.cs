using UnityEngine;

// Physical object for a tool (Knife/Pickaxe/Hammer/Axe, any tier). Same
// structure as Sunglasses.cs/Boot.cs — the pickup/carry/worn-object
// plumbing only. Tool-gated actions (ResourceNode.requiredTools,
// CraftingRecipe.requiredTools) read PlayerEquipment.HasInHand, which
// checks the hand Inventory slot's item count directly and needs no
// awareness of this component at all — that's populated the same way
// whether the slot came from AddItem or AddEquipmentItem.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Tool : MonoBehaviour, IInteractable, IEquippable
{
    // Excluded from the player's own camera while worn — see Backpack.cs.
    private const int DefaultLayer = 0;
    private const int WornEquipmentLayer = 8;

    [SerializeField] private ItemDefinition itemDefinition;

    private Rigidbody body;
    private Collider col;

    public ItemDefinition ItemDefinition => itemDefinition;
    public string DisplayName => itemDefinition != null ? itemDefinition.itemName : "Tool";

    public string Prompt => $"Pick up {DisplayName}";
    public bool IsInstant => true;
    public float GetHoldDuration(GameObject player) => 0f;

    // No belt/body slot — a tool only ever makes sense held in a hand.
    public bool CanEquipToSlot(string slotName) =>
        slotName == "Left Hand" || slotName == "Right Hand";

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    // MULTIPLAYER_INTERACTION_AUDIT.md follow-up (2026-08-31): routed
    // through PlayerInventory.RequestPickUpEquipment (a real Command) --
    // used to call PlayerTool.PickUp directly, entirely client-side.
    public void Complete(GameObject player)
    {
        player.GetComponent<PlayerInventory>()?.RequestPickUpEquipment(this);
    }

    // Fully hides the object while it's stashed in a regular inventory slot
    // rather than sitting in the world or held in a hand.
    public void Stash()
    {
        Despawn.CancelOn(gameObject);
        transform.SetParent(null, false);
        gameObject.SetActive(false);
    }

    // Held (visible, non-collidable, follows anchor) when anchor is set, or
    // released back into the world as a normal physical object when anchor
    // is null.
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
