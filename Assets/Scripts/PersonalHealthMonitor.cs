using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PersonalHealthMonitor : MonoBehaviour, IInteractable, IEquippable
{
    // Excluded from the player's own camera while worn — see Backpack.cs.
    private const int DefaultLayer = 0;
    private const int WornEquipmentLayer = 8;

    [SerializeField] private string monitorName = "Personal Health Monitor";

    private Rigidbody body;
    private Collider col;

    public string DisplayName => monitorName;

    public string Prompt => $"Pick up {monitorName}";
    public bool IsInstant => true;
    public float GetHoldDuration(GameObject player) => 0f;

    public bool CanEquipToSlot(string slotName) => slotName == "Left Wrist" || slotName == "Right Wrist";

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void Complete(GameObject player)
    {
        var carrier = player.GetComponent<PlayerHealthMonitor>();
        carrier?.PickUp(this);
    }

    // Fully hides the object while it's stashed in a regular inventory slot
    // rather than sitting in the world or worn on a wrist.
    public void Stash()
    {
        Despawn.CancelOn(gameObject);
        transform.SetParent(null, false);
        gameObject.SetActive(false);
    }

    // Worn (visible, non-collidable, follows the player) when anchor is
    // set, or released back into the world as a normal physical object
    // when anchor is null.
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
