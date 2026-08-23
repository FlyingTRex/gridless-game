using Mirror;
using UnityEngine;

// Shared base for killable/lootable/skinnable creatures (2026-08-15 efficiency
// pass — extracted from HostileCreature/PreyCreature, which had grown ~90
// lines of near-identical Awake/TakeDamage/Die/Complete/Respawn/SetVisible
// logic between them). Holds everything that doesn't depend on whether the
// creature has AI: health, the dead/alive lifecycle, the tool-gated
// hold-to-skin interaction, and respawn. Subclasses own their own loot shape
// via DropLoot() and anything AI-specific (HostileCreature keeps its own
// Update() state machine entirely).
//
// Multiplayer, 2026-08-23 (roaming wildlife should be server-side too,
// per Ben's explicit call — extending the "NPCs move server-side" phase
// beyond the 5 job-driven scripts): converted to NetworkBehaviour so
// both subclasses (HostileCreature/PreyCreature) inherit it in one
// move. TakeDamage itself needs no isServer guard — it's only ever
// called from a Command already (PlayerCombat/PlayerRangedCombat), so
// it already runs server-side regardless.
[RequireComponent(typeof(Collider))]
public abstract class SkinnableCreature : NetworkBehaviour, IDamageable, IInteractable
{
    [SerializeField] private float maxHealth = 15f;

    // Null/empty means no tool needed, same convention as
    // ResourceNode.requiredTools.
    [SerializeField] private ItemDefinition[] requiredTools;
    [SerializeField] private string requiredToolLabel = "Knife";
    [SerializeField] private SkillDefinition skinningSkill;
    [SerializeField] private float skinningSkillGain = 0.3f;
    [SerializeField] private float skinHoldDuration = 1.5f;

    // <=0 disables respawn entirely, same "0 disables it" convention as
    // ResourceNode.respawnDelay/ChoppableTree.regrowDelay.
    [SerializeField] private float respawnDelay = 180f;

    protected float health;
    protected bool isDead;
    protected Vector3 spawnPosition;
    protected Quaternion spawnRotation;
    protected Collider col;

    public string Prompt => isDead
        ? (HasToolRequirement ? $"Hold to skin (requires {requiredToolLabel})" : "Hold to skin")
        : "";

    // Read by sibling movement components (PreyWander, 2026-08-16) that
    // aren't subclasses of this one and so can't see the protected isDead
    // field directly -- stop driving movement/animation once the creature
    // is dead, same as HostileCreature's own isDead check already does
    // internally.
    public bool IsDead => isDead;

    public bool IsInstant => false;
    public float GetHoldDuration(GameObject player) => skinHoldDuration;

    protected bool HasToolRequirement => requiredTools != null && requiredTools.Length > 0;

    protected virtual void Awake()
    {
        col = GetComponent<Collider>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        health = maxHealth;
    }

    public virtual void TakeDamage(float amount)
    {
        if (isDead) return;

        health -= amount;
        if (health <= 0f)
            Die();
    }

    protected virtual void Die()
    {
        isDead = true;
        // Crude "fallen over" visual — no animation system driving a death
        // pose yet.
        transform.rotation = spawnRotation * Quaternion.Euler(0f, 0f, 90f);
    }

    // Called by PlayerInteraction once the skin hold completes — only
    // meaningful while dead (Prompt/GetHoldDuration already gate the UI
    // side, but Complete() itself also no-ops defensively).
    public void Complete(GameObject playerObj)
    {
        if (!isDead) return;

        if (HasToolRequirement)
        {
            var equipment = playerObj.GetComponent<PlayerEquipment>();
            if (equipment == null || !HasAnyRequiredToolInHand(equipment)) return;
        }

        playerObj.GetComponent<PlayerSkills>()?.GainExperience(skinningSkill, skinningSkillGain);

        // Disable the corpse's own collider before dropping loot, not
        // after — loot spawns right next to (often overlapping) this
        // collider, and Unity's physics-overlap separation impulse can
        // eject a small pickup clean through nearby terrain (2026-08-16,
        // found live: Egg/Leather falling through the world after a
        // kill). SetVisible(false) below used to be the only place this
        // got disabled, well after loot had already spawned.
        if (col != null) col.enabled = false;

        var dropping = playerObj.GetComponent<PlayerDropping>();
        DropLoot(dropping);

        if (respawnDelay <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        SetVisible(false);
        Invoke(nameof(Respawn), respawnDelay);
    }

    // Subclass-specific loot table — HostileCreature's Pelt+Meat shape,
    // PreyCreature's LootA+LootB shape, etc.
    protected abstract void DropLoot(PlayerDropping dropping);

    private bool HasAnyRequiredToolInHand(PlayerEquipment equipment)
    {
        foreach (var tool in requiredTools)
        {
            if (tool != null && equipment.HasInHand(tool)) return true;
        }
        return false;
    }

    protected virtual void Respawn()
    {
        health = maxHealth;
        isDead = false;
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        SetVisible(true);
    }

    protected void SetVisible(bool visible)
    {
        if (col != null) col.enabled = visible;
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = visible;
    }
}
