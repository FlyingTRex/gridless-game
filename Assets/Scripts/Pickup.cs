using UnityEngine;

public class Pickup : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemDefinition item;
    [SerializeField] private int quantity = 1;
    [SerializeField] private SkillDefinition trainedSkill;
    [SerializeField] private float skillGain = 0.05f;

    // Only set on persistent world resource points (e.g. the Stick
    // pickups) — never on items the player dropped or that scattered out
    // of a broken ResourceNode, which shouldn't reappear on their own.
    [SerializeField] private bool canRespawn = false;
    [SerializeField] private float respawnDelay = 180f;
    [SerializeField] private float respawnScatter = 0.5f;

    // A player-dropped item (manual Drop, unequip-with-full-inventory
    // fallback, or hand-eviction on pickup) despawns from the world if left
    // unpicked this long. Distinct from respawnDelay above — this deletes
    // a dropped item outright rather than restoring a resource point.
    private const float DespawnDelay = 900f; // 15 minutes

    private Vector3 spawnPosition;
    private Collider col;
    private Renderer[] renderers;
    // -1 means "not counting down" — either still sitting there unpicked
    // (the timer is held until something actually takes it) or respawn
    // isn't enabled at all.
    private float respawnAt = -1f;
    // -1 means "not a dropped item" — only Configure() (called exclusively
    // by PlayerDropping.DropFrom) starts this countdown; world-placed
    // pickups set up directly in the scene/prefab never get one.
    private float despawnAt = -1f;

    public string Prompt => item != null ? $"Pick up {item.itemName}" : "Pick up";
    public bool IsInstant => true;
    public float HoldDuration => 0f;

    private void Awake()
    {
        spawnPosition = transform.position;
        col = GetComponent<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    public void Configure(ItemDefinition newItem, int newQuantity)
    {
        item = newItem;
        quantity = newQuantity;
        despawnAt = Time.time + DespawnDelay;
    }

    private void Update()
    {
        if (despawnAt >= 0f && Time.time >= despawnAt)
        {
            Destroy(gameObject);
            return;
        }

        if (respawnAt < 0f || Time.time < respawnAt) return;
        Respawn();
    }

    public void Complete(GameObject player)
    {
        var loot = player.GetComponent<PlayerLoot>();
        int leftover;
        if (loot != null)
        {
            leftover = loot.Receive(item, quantity);
        }
        else
        {
            var inventory = player.GetComponent<PlayerInventory>();
            leftover = inventory != null ? inventory.AddItem(item, quantity) : quantity;
        }

        if (leftover > 0)
        {
            // Nowhere to put it (backpack/hands full with PlayerLoot, or
            // inventory full without it) — leave the remainder on the
            // ground instead of deleting it.
            quantity = leftover;
            return;
        }

        player.GetComponent<PlayerSkills>()?.GainExperience(trainedSkill, skillGain);

        if (canRespawn)
        {
            SetVisible(false);
            respawnAt = Time.time + respawnDelay;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Same spot it started at, with a small random horizontal shift so
    // respawns don't all land pixel-identical to the original.
    private void Respawn()
    {
        Vector2 offset = Random.insideUnitCircle * respawnScatter;
        transform.position = spawnPosition + new Vector3(offset.x, 0f, offset.y);
        SetVisible(true);
        respawnAt = -1f;
    }

    private void SetVisible(bool visible)
    {
        if (col != null) col.enabled = visible;
        foreach (var r in renderers)
            r.enabled = visible;
    }
}
