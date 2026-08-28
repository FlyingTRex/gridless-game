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
//
// FIXED (2026-08-28, found live — "I killed a chicken, didn't see it
// die, but traskmi did"): TakeDamage/Die already ran server-side
// correctly (see above), but `isDead` was a plain field, not a
// [SyncVar] — the server's own copy updated fine, the actual killer's
// own client (if not the host) never heard about it, same shape as
// every other fix tonight. Also found the same "runs entirely local,
// never reaches the server" gap Pickup/ChoppableTree just had: Complete()
// (the skin action) had no Command routing at all — for a real remote
// client, skinning a corpse would spawn loot correctly (PlayerDropping's
// own Command already covers that) but never actually despawn the
// corpse for anyone else, a real double-loot risk, not just a visual
// one. Both fixed with the same patterns already established tonight.
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

    [SyncVar(hook = nameof(OnDeadChanged))]
    protected bool isDead;

    private void OnDeadChanged(bool oldValue, bool newValue)
    {
        // Mirrors exactly what Die()/Respawn() already set directly on
        // whichever machine calls them (the server) -- this is what makes
        // every OTHER observer's own copy show the same fallen-over pose
        // or upright respawn. Fires there too, redundantly but harmlessly
        // (same values, just set twice).
        transform.rotation = newValue ? spawnRotation * Quaternion.Euler(0f, 0f, 90f) : spawnRotation;
    }

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
        // The actual "fallen over" visual is applied by OnDeadChanged
        // (fired locally the moment this SyncVar assignment lands, same
        // as everywhere else it's set) -- no animation system driving a
        // real death pose yet.
        isDead = true;
    }

    // Called by PlayerInteraction once the skin hold completes — only
    // meaningful while dead (Prompt/GetHoldDuration already gate the UI
    // side, but Complete() itself also no-ops defensively).
    //
    // FIXED (2026-08-28): same dual-path dispatch Pickup.Complete()/
    // ChoppableTree.Complete() already established -- this used to run
    // entirely local-only, meaning a real remote client's own skin
    // action would drop loot correctly (PlayerDropping's own Command
    // already covers that) but never actually despawn the corpse for
    // any other observer, a real double-loot risk. requiresAuthority is
    // false since a creature has no client authority the way a Player
    // object does -- the caller's own identity comes through Mirror's
    // sender parameter instead.
    public void Complete(GameObject playerObj)
    {
        if (!isDead) return;

        if (TryGetComponent<NetworkIdentity>(out _) && NetworkClient.active)
        {
            CmdComplete();
            return;
        }

        ServerComplete(playerObj);
    }

    [Command(requiresAuthority = false)]
    private void CmdComplete(NetworkConnectionToClient sender = null)
    {
        if (sender == null || sender.identity == null) return;
        ServerComplete(sender.identity.gameObject);
    }

    public void ServerComplete(GameObject playerObj)
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
            if (NetworkServer.active)
                NetworkServer.Destroy(gameObject);
            else
                Destroy(gameObject);
            return;
        }

        isHidden = true;
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
        isHidden = false;
        if (col != null) col.enabled = true;
    }

    // FIXED (2026-08-28): was a plain method call, only ever applying on
    // whichever machine ran it -- for a networked instance, only the
    // server (ServerComplete now always runs there) ever hid/reshowed
    // the corpse, so every OTHER client kept seeing a "skinned" corpse
    // sitting there fully visible until Respawn's own isDead SyncVar
    // eventually stood it back up. Now backed by a SyncVar so every
    // observer's own renderers toggle together. The collider itself
    // stays a direct local set (not synced) -- physics/interaction
    // reach is already server-authoritative (ServerComplete/TakeDamage
    // both only ever run server-side), so a client's own collider state
    // is cosmetic only here, same reasoning col.enabled's own disable
    // above (inside ServerComplete) already relied on.
    [SyncVar(hook = nameof(OnHiddenChanged))]
    private bool isHidden;

    private void OnHiddenChanged(bool oldValue, bool newValue)
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = !newValue;
    }
}
