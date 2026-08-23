using Mirror;
using UnityEngine;

// Multiplayer Phase 3 sub-phase 2 (MULTIPLAYER_PLANNING.md), first slice
// (2026-08-22): converted from MonoBehaviour to NetworkBehaviour as its
// own isolated step -- no new synced state yet, purely the base-class
// change, matching the same "prove the foundation before building on it"
// discipline sub-phase 1's Bootstrap used. A real synced Inventory needs
// its own custom SyncList serializer (Mirror doesn't natively sync a
// ScriptableObject reference like ItemDefinition -- it'll need the same
// by-string-ID resolution SaveManager/ItemDatabase.Find(id) already use
// for persistence), which is separate, larger follow-up work, not part
// of this step.
[DisallowMultipleComponent]
public class PlayerInventory : NetworkBehaviour
{
    [SerializeField] private int capacity = 4;

    private Inventory inventory;

    public Inventory Inventory => inventory;

    private void Awake()
    {
        inventory = new Inventory(capacity);
    }

    // Returns the amount that did NOT fit (0 means everything was added).
    public int AddItem(ItemDefinition item, int quantity) => inventory.AddItem(item, quantity);

    public bool RemoveItem(ItemDefinition item, int quantity) => inventory.RemoveItem(item, quantity);

    public int GetCount(ItemDefinition item) => inventory.GetCount(item);
}
