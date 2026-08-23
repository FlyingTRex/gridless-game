using Mirror;
using UnityEngine;

// Multiplayer, 2026-08-23 -- found during the same audit: passive
// health regen in Update() was unguarded. Converted to NetworkBehaviour,
// isServer guard added below. TakeDamage itself needs no guard -- every
// call site (HostileCreature's attack logic, PlayerCombat/
// PlayerRangedCombat's Commands) is already server-side.
//
// The project's first real NPC health/death system (2026-08-16,
// GUARDING_PLANNING.md section 2) -- mirrors SkinnableCreature's own
// TakeDamage/Die shape, but without the skin/loot/respawn half: a hired
// NPC is a person, not a resource node. Death here is permanent.
//
// Lives on every hireable NPC (same always-present convention NPCCrafting/
// NPCTraining/NPCSeekFlag already use), not just Guard-assigned ones --
// harmless for a Mining/Woodworking NPC today since nothing currently
// attacks a non-Guard NPC, but means any future hostile-vs-NPC interaction
// doesn't need this component retrofitted later.
public class NPCVitals : NetworkBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 30f;

    // First-pass number, not deeply tuned (GUARDING_PLANNING.md section 7)
    // -- only regenerates while not actively fighting, same "recovers when
    // safe" spirit as PlayerVitals' own stamina regen, just simpler (flat
    // rate, no threshold gating).
    [SerializeField] private float regenPerSecond = 1f;

    private float health;
    private bool isDead;

    private NPCJob job;
    private NPCGuarding guarding;

    public bool IsDead => isDead;
    public float Health => health;
    public float MaxHealth => maxHealth;

    private void Awake()
    {
        health = maxHealth;
        job = GetComponent<NPCJob>();
        // Optional -- a non-Guard NPC still has vitals (see header comment)
        // but has no combat state to check before regenerating.
        guarding = GetComponent<NPCGuarding>();
    }

    private void Update()
    {
        if (!isServer) return;
        if (isDead || health >= maxHealth) return;
        // Only regenerate while not mid-fight -- a Guard trading blows with
        // a Wolf shouldn't out-heal the damage it's currently taking.
        if (guarding != null && guarding.IsFighting) return;

        health = Mathf.Min(maxHealth, health + regenPerSecond * Time.deltaTime);
    }

    public void TakeDamage(float amount)
    {
        if (isDead || amount <= 0f) return;

        health -= amount;
        if (health <= 0f)
            Die();
    }

    // Permanent -- no respawn timer, unlike SkinnableCreature. Clears the
    // job (same tool-loss-for-good convention NPCHiring.Fire() already
    // uses for a fired NPC) before destroying the GameObject, so nothing
    // is left holding a reference to a job assignment that no longer has
    // an NPC behind it.
    private void Die()
    {
        isDead = true;
        // Permanent and could happen off-screen during an unattended Guard
        // fight -- worth a log line since there's nothing else to notice
        // it by afterward (no corpse, no message).
        Debug.Log($"[NPCVitals] {name} died (permanent).");
        job?.ClearJob();
        Destroy(gameObject);
    }
}
