using UnityEngine;

// Physical object, structured identically to Sunglasses.cs — a single
// Face-slot equippable with no inventory of its own. Its effect (revealing
// hidden ore) lives on ResourceNode/PlayerMiningFaceShield, not here — this
// class is purely the pickup/carry/worn-object plumbing.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class MiningFaceShield : MonoBehaviour, IInteractable, IEquippable
{
    // Excluded from the player's own camera while worn — see Backpack.cs.
    private const int DefaultLayer = 0;
    private const int WornEquipmentLayer = 8;

    [SerializeField] private string shieldName = "Mining Face Shield";

    private Rigidbody body;
    private Collider col;

    public string DisplayName => shieldName;

    public string Prompt => $"Pick up {shieldName}";
    public bool IsInstant => true;
    public float HoldDuration => 0f;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void Complete(GameObject player)
    {
        var carrier = player.GetComponent<PlayerMiningFaceShield>();
        carrier?.PickUp(this);
    }

    public void Stash()
    {
        transform.SetParent(null, false);
        gameObject.SetActive(false);
    }

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
