using UnityEngine;

// First Prey Creature (2026-08-15, "let's add [Feather/Egg] to the
// chicken loot table... when we kill a chicken, we get crafting
// materials") — killable and lootable, same tool-gated hold-to-skin
// death/respawn shape HostileCreature already proved out for the Wolf,
// deliberately stripped of everything aggressive (no detection/chase/
// attack state machine). This is NOT yet the full Prey Creature
// archetype the Hunting Expansion design calls for (idle/wander until
// approached, then flee) — that behavior still doesn't exist. Built
// generic/reusable rather than Chicken-specific so Pig/Deer/Rabbit can
// use the same component later; only the flee movement is still missing.
[RequireComponent(typeof(Collider))]
public class PreyCreature : MonoBehaviour, IDamageable, IInteractable
{
    private enum State { Alive, Dead }

    [SerializeField] private float maxHealth = 15f;

    [SerializeField] private ItemDefinition lootItemA;
    [SerializeField] private int lootAMinCount = 1;
    [SerializeField] private int lootAMaxCount = 1;
    [SerializeField, Range(0f, 1f)] private float lootADropChance = 1f;

    [SerializeField] private ItemDefinition lootItemB;
    [SerializeField] private int lootBMinCount = 1;
    [SerializeField] private int lootBMaxCount = 1;
    [SerializeField, Range(0f, 1f)] private float lootBDropChance = 1f;

    // Null/empty means no tool needed — same convention as
    // HostileCreature.requiredTools/ResourceNode.requiredTools.
    [SerializeField] private ItemDefinition[] requiredTools;
    [SerializeField] private string requiredToolLabel = "Knife";
    [SerializeField] private SkillDefinition skinningSkill;
    [SerializeField] private float skinningSkillGain = 0.3f;
    [SerializeField] private float skinHoldDuration = 1.5f;

    // <=0 disables respawn entirely, same convention as HostileCreature.
    [SerializeField] private float respawnDelay = 180f;

    private float health;
    private State state = State.Alive;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private Collider col;

    public string Prompt => state == State.Dead
        ? (HasToolRequirement ? $"Hold to skin (requires {requiredToolLabel})" : "Hold to skin")
        : "";

    public bool IsInstant => false;
    public float GetHoldDuration(GameObject player) => skinHoldDuration;

    private bool HasToolRequirement => requiredTools != null && requiredTools.Length > 0;

    private void Awake()
    {
        col = GetComponent<Collider>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        health = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (state == State.Dead) return;

        health -= amount;
        if (health <= 0f)
            Die();
    }

    private void Die()
    {
        state = State.Dead;
        // Same crude "fallen over" visual as HostileCreature — no
        // animation system driving a death pose yet.
        transform.rotation = spawnRotation * Quaternion.Euler(0f, 0f, 90f);
    }

    public void Complete(GameObject playerObj)
    {
        if (state != State.Dead) return;

        if (HasToolRequirement)
        {
            var equipment = playerObj.GetComponent<PlayerEquipment>();
            if (equipment == null || !HasAnyRequiredToolInHand(equipment)) return;
        }

        playerObj.GetComponent<PlayerSkills>()?.GainExperience(skinningSkill, skinningSkillGain);

        var dropping = playerObj.GetComponent<PlayerDropping>();
        if (lootItemA != null && Random.value < lootADropChance)
            dropping?.SpawnPickup(lootItemA, Random.Range(lootAMinCount, lootAMaxCount + 1));
        if (lootItemB != null && Random.value < lootBDropChance)
            dropping?.SpawnPickup(lootItemB, Random.Range(lootBMinCount, lootBMaxCount + 1));

        if (respawnDelay <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        SetVisible(false);
        Invoke(nameof(Respawn), respawnDelay);
    }

    private bool HasAnyRequiredToolInHand(PlayerEquipment equipment)
    {
        foreach (var tool in requiredTools)
        {
            if (tool != null && equipment.HasInHand(tool)) return true;
        }
        return false;
    }

    private void Respawn()
    {
        health = maxHealth;
        state = State.Alive;
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        if (col != null) col.enabled = visible;
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = visible;
    }
}
