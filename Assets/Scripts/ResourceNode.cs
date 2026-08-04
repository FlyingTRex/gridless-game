using UnityEngine;

public class ResourceNode : MonoBehaviour, IPunchable
{
    [SerializeField] private GameObject chunkPrefab;
    [SerializeField] private int chunkCount = 3;
    [SerializeField] private int hitsToBreak = 1;
    [SerializeField] private float scatterForce = 1.2f;
    [SerializeField] private SkillDefinition trainedSkill;
    [SerializeField] private float skillGain = 0.5f;
    [SerializeField] private float respawnDelay = 180f;
    [SerializeField] private float respawnScatter = 0.5f;

    // Null (default) means no tool needed — punching bare-handed works,
    // same as Rock Node's existing behavior. Set to gate a node behind a
    // specific tool (e.g. Copper Ore requires a Pickaxe, Tree requires an
    // Axe) — checked via PlayerEquipment.HasInHand, so the tool has to
    // actually be held in a hand, not just carried somewhere in inventory.
    [SerializeField] private ItemDefinition requiredTool;

    // Null (default) means no disguise — this node always looks like
    // chunkPrefab's true resource and always yields it, same as every node
    // shipped before this. Set both materials to make a node look like
    // plain rock until the player has a Mining Face Shield equipped, at
    // which point it swaps to revealedMaterial — same reveal mechanism
    // already shipped for Sunglasses + the Secret Message Wall, generalized
    // into a gameplay effect instead of a pure visual one.
    [SerializeField] private Material hiddenMaterial;
    [SerializeField] private Material revealedMaterial;
    // What actually gets spawned if the node is broken while NOT revealed —
    // the ore goes undetected, so this should be a plain Small Rock chunk
    // prefab, not the real ore. Ignored entirely when hiddenMaterial is null.
    [SerializeField] private GameObject hiddenChunkPrefab;

    private int hitsTaken;
    private Vector3 spawnPosition;
    private Collider col;
    private Renderer[] renderers;
    // -1 means "not counting down" — the node is either still standing
    // (the timer is held until it's actually broken) or mid-repositioning.
    private float respawnAt = -1f;

    // Looked up once rather than per-frame — this node isn't parented under
    // Player, so there's no cheap direct reference, same pattern
    // SecretMessageWall already uses for finding PlayerSunglasses.
    private PlayerMiningFaceShield shieldWearer;

    public string Prompt => requiredTool != null
        ? $"Punch to break (requires {requiredTool.itemName})"
        : "Punch to break";

    private bool IsDisguised => hiddenMaterial != null;
    private bool IsRevealed => shieldWearer != null && shieldWearer.IsWorn;

    private void Awake()
    {
        spawnPosition = transform.position;
        col = GetComponent<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void Start()
    {
        if (IsDisguised)
            shieldWearer = FindFirstObjectByType<PlayerMiningFaceShield>();
    }

    private void Update()
    {
        if (IsDisguised)
        {
            var material = IsRevealed ? revealedMaterial : hiddenMaterial;
            foreach (var r in renderers)
                r.sharedMaterial = material;
        }

        if (respawnAt < 0f || Time.time < respawnAt) return;
        Respawn();
    }

    public void OnPunch(GameObject player)
    {
        if (requiredTool != null)
        {
            var equipment = player.GetComponent<PlayerEquipment>();
            if (equipment == null || !equipment.HasInHand(requiredTool)) return;
        }

        hitsTaken++;
        player.GetComponent<PlayerSkills>()?.GainExperience(trainedSkill, skillGain);
        if (hitsTaken < hitsToBreak) return;

        // Checked at the moment it actually breaks, not when punching
        // started — a node revealed only partway through wouldn't make
        // sense to then punish, and this matches how every other tier/skill
        // check in this system is evaluated per-attempt, not locked in early.
        GameObject prefabToSpawn = (IsDisguised && !IsRevealed) ? hiddenChunkPrefab : chunkPrefab;

        for (int i = 0; i < chunkCount; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 0.3f;
            var chunk = Instantiate(prefabToSpawn, transform.position + Vector3.up * 0.2f + offset,
                Random.rotation);

            if (chunk.TryGetComponent(out Rigidbody rb))
            {
                Vector3 dir = (Random.insideUnitSphere + Vector3.up).normalized;
                rb.AddForce(dir * scatterForce, ForceMode.Impulse);
            }
        }

        SetVisible(false);
        respawnAt = Time.time + respawnDelay;
    }

    // Same spot it started at, with a small random horizontal shift, fully
    // intact again (hitsTaken reset) so it can be broken again from
    // scratch.
    private void Respawn()
    {
        Vector2 offset = Random.insideUnitCircle * respawnScatter;
        transform.position = spawnPosition + new Vector3(offset.x, 0f, offset.y);
        hitsTaken = 0;
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
