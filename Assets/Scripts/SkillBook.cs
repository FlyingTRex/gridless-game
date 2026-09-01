using UnityEngine;

// Per-instance "written book" carrier (SKILL_BOOKS_PLANNING.md Phase 1,
// item 4) — a book's target (CraftingRecipe or WishRecipe) and, for a
// BrilliantSuccess-written lineage tome, its rolled bonus level, are
// baked in at write time. Same per-instance-state-via-equipment-slot
// shape Canteen already established for its own Liquid/Amount. Never
// worn on a body slot (CanEquipToSlot always false) — held/read only,
// but still carried through Inventory.AddEquipmentItem so this instance
// data survives being picked up, moved, and (once SAVE_LOAD_PLANNING.md's
// section 10 follow-up lands) saved.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class SkillBook : MonoBehaviour, IInteractable, IEquippable
{
    private const int DefaultLayer = 0;
    private const int WornEquipmentLayer = 8;

    [SerializeField] private ItemDefinition itemDefinition;

    // [SerializeField] (not plain auto-properties) is deliberate — a
    // book's target has to survive being placed directly in a saved
    // scene at edit-time (a "found" book, SKILL_BOOKS_PLANNING.md Phase
    // 5), not just live in memory for one Play session. A plain C#
    // auto-property is invisible to Unity's scene serializer; caught
    // live when two edit-time-placed found books came back with null
    // targets after a scene reload despite SetTargetRecipe/SetTargetWish
    // having been called and the scene reporting a successful save.
    [SerializeField] private CraftingRecipe targetRecipe;
    [SerializeField] private WishRecipe targetWish;
    [SerializeField] private float bonusLevel;

    private Rigidbody body;
    private Collider col;

    // Exactly one of these two is set, never both — a crafting/weapon
    // book targets a CraftingRecipe, a magic book targets a WishRecipe.
    public CraftingRecipe TargetRecipe => targetRecipe;
    public WishRecipe TargetWish => targetWish;

    // Only meaningful alongside TargetWish, and only non-zero when the
    // write roll was a BrilliantSuccess (SKILL_BOOKS_PLANNING.md's
    // "Lineage tome starting level" section) — the head start above 0
    // reading grants the lineage. Always 0 for a TargetRecipe book; a
    // recipe grant has no "above 0" concept to begin with.
    public float BonusLevel => bonusLevel;

    public ItemDefinition ItemDefinition => itemDefinition;
    public string DisplayName => itemDefinition != null ? itemDefinition.itemName : "Skill Book";

    public string Prompt => $"Pick up {DisplayName}";
    public bool IsInstant => true;
    public float GetHoldDuration(GameObject player) => 0f;

    public bool CanEquipToSlot(string slotName) => false;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    // Written by PlayerWriting (Phase 2) immediately after Instantiate —
    // exactly one of these two is ever called on a given instance.
    public void SetTargetRecipe(CraftingRecipe recipe)
    {
        targetRecipe = recipe;
        targetWish = null;
        bonusLevel = 0f;
    }

    public void SetTargetWish(WishRecipe wish, float bonusLevel)
    {
        targetWish = wish;
        targetRecipe = null;
        this.bonusLevel = bonusLevel;
    }

    // Called when the player interacts with a book lying in the world.
    // Routes through PlayerLoot first (equipped backpack's own contents,
    // then a free hand) — falls back to stashing as a regular (hidden)
    // inventory item only if PlayerLoot found nowhere else for it. Same
    // shape as Backpack.PickUp/Canteen.PickUp.
    // MULTIPLAYER_INTERACTION_AUDIT.md follow-up (2026-08-31): routed
    // through PlayerInventory.RequestPickUpEquipment (a real Command) --
    // used to run this entirely client-side.
    public void Complete(GameObject player)
    {
        player.GetComponent<PlayerInventory>()?.RequestPickUpEquipment(this);
    }

    // Fully hides the object while it's stashed in a regular inventory
    // slot rather than sitting in the world or held in hand.
    public void Stash()
    {
        Despawn.CancelOn(gameObject);
        transform.SetParent(null, false);
        gameObject.SetActive(false);
    }

    // Carried (visible, non-collidable, follows the player) when anchor
    // is set, or released back into the world as a normal physical
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
