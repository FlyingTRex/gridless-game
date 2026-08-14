using UnityEngine;

// Stable identity for a world object instance (SAVE_LOAD_PLANNING.md
// section 5) — nothing in this project has persistent identity otherwise,
// so saved data needs a way to reattach to the *same* object after a fresh
// scene load. Same "small single-purpose marker component" convention as
// IWaterSource/IRenameable. GUID auto-generates once (Reset, i.e. the
// moment it's first added in the Editor) and stays fixed afterward, baked
// into the scene/prefab file like any other serialized field.
public class SaveId : MonoBehaviour
{
    [SerializeField] private string id;

    public string Id => id;

    private void Reset() => GenerateIfMissing();

    // Called explicitly by the one-off Editor migration script that adds
    // this component to every pre-existing StorageBox/ResourceNode/
    // NPCHiring instance in the scene — AddComponent from a script isn't
    // guaranteed to invoke Reset() the way the Inspector's "Add Component"
    // button does, so this is the reliable path rather than assuming it.
    public void GenerateIfMissing()
    {
        if (string.IsNullOrEmpty(id))
            id = System.Guid.NewGuid().ToString("N");
    }

    private void OnEnable() => SaveIdRegistry.Register(this);
    private void OnDisable() => SaveIdRegistry.Unregister(this);
}
