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

    // Self-healing collision guard (2026-08-15) — RequireComponent's
    // auto-add only runs Reset() once per loaded prefab *template* within
    // a session, not once per Instantiate() call: every runtime clone
    // just copies whatever id the template already has at that point, so
    // every instance placed from the same prefab in one session ends up
    // sharing the identical id (confirmed live — two freshly instantiated
    // GardenPlot4x4 clones both reported the same GUID). Registry.Register
    // silently overwrites on collision, so only the last-registered
    // instance would ever restore correctly — every earlier one built
    // from the same prefab would silently come back empty on load, no
    // error. Detecting the collision here, at registration time, fixes
    // every current and future SaveId user (StorageBox, GardenPlot,
    // GardenPlot4x4, ...) without needing each placement call site to
    // know about the problem.
    private void OnEnable()
    {
        var existing = SaveIdRegistry.Find(id);
        if (existing != null && existing != this)
            id = System.Guid.NewGuid().ToString("N");

        SaveIdRegistry.Register(this);
    }

    private void OnDisable() => SaveIdRegistry.Unregister(this);
}
