using System.Collections.Generic;
using UnityEngine;

// Worn at Feet. Unlike Belt's generic attachment points (any IEquippable
// counts the same), a Boot's slots are named and type-restricted — e.g. a
// Hiking Boot's "Knife Sheath" only accepts a Knife (any tier; Knife's 5
// tiers don't chain via ItemDefinition.baseItem the way Trimmed Stick does,
// so the slot lists all 5 tiers explicitly rather than relying on
// IngredientMatching). Civilian boots have zero slots (slots left empty) —
// same component, just no extra configuration, rather than a separate
// no-op class.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Boot : MonoBehaviour, IInteractable, IEquippable
{
    [System.Serializable]
    public class SlotConfig
    {
        public string label;

        // Empty/unset means "no items allowed yet" -- used for the
        // Military boot's Pistol Holster, which is deliberately left
        // unusable until a Pistol ItemDefinition actually exists (Ben's
        // call, 2026-08-11: "we'll leave the pistol slot as a something
        // to complete later").
        public ItemDefinition[] allowedItems;
    }

    // See Backpack.cs for why worn gear sits on this layer.
    private const int DefaultLayer = 0;
    private const int WornEquipmentLayer = 8;

    [SerializeField] private ItemDefinition itemDefinition;
    [SerializeField] private SlotConfig[] slots = new SlotConfig[0];

    private readonly Dictionary<string, Inventory> slotInventories = new Dictionary<string, Inventory>();
    private Rigidbody body;
    private Collider col;

    public ItemDefinition ItemDefinition => itemDefinition;
    public string DisplayName => itemDefinition != null ? itemDefinition.itemName : "Boot";
    public IReadOnlyCollection<string> SlotNames => slotInventories.Keys;

    public string Prompt => $"Pick up {DisplayName}";
    public bool IsInstant => true;
    public float GetHoldDuration(GameObject player) => 0f;

    public bool CanEquipToSlot(string slotName) => slotName == "Feet";

    private void Awake()
    {
        foreach (var slot in slots)
            slotInventories[slot.label] = new Inventory(1, slot.allowedItems);

        body = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public Inventory GetSlot(string label) =>
        slotInventories.TryGetValue(label, out var inv) ? inv : null;

    public void Complete(GameObject player)
    {
        var carrier = player.GetComponent<PlayerBoot>();
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
