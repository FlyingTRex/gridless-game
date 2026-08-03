using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PersonalHealthMonitor : MonoBehaviour, IInteractable, IEquippable
{
    [SerializeField] private string monitorName = "Personal Health Monitor";

    private Rigidbody body;
    private Collider col;

    public string DisplayName => monitorName;

    public string Prompt => $"Pick up {monitorName}";
    public bool IsInstant => true;
    public float HoldDuration => 0f;

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
        transform.SetParent(null, false);
        gameObject.SetActive(false);
    }

    // Worn (visible, non-collidable, follows the player) when anchor is
    // set, or released back into the world as a normal physical object
    // when anchor is null.
    public void SetCarried(bool value, Transform anchor)
    {
        gameObject.SetActive(true);
        col.enabled = !value;
        body.isKinematic = value;

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
}
