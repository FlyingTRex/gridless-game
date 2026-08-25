using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

// Bare-handed melee — the first Basic Combat action (2026-08-10, first
// target: HostileCreature/Wolf). Deliberately its own input handling, not
// IInteractable's hold-and-release model (same reasoning PlayerPieceUpgrade
// already applied to a different action) — an attack needs to resolve on a
// quick tap, not a multi-second hold.
//
// Multiplayer Phase 3 sub-phase 4 (MULTIPLAYER_PLANNING.md), 2026-08-23:
// converted to NetworkBehaviour, plus a real RequestPunch/CmdPunch Command.
// The client still does the aim raycast locally (only the client has an
// up-to-date camera transform) and resolves the hit target, but damage/XP
// application itself runs server-side via the Command, same "client
// resolves what, server decides whether/how much" split every other
// Command in this project uses. ResolveAttack() reads equipment.
// GetHandItems() directly, which is real server-authoritative data
// regardless of who's driving the object (host or remote client), so no
// separate weapon-resolution needs to travel over the wire.
[RequireComponent(typeof(PlayerInteraction))]
[RequireComponent(typeof(PlayerSkills))]
[RequireComponent(typeof(PlayerBuilding))]
[RequireComponent(typeof(PlayerEquipment))]
public class PlayerCombat : NetworkBehaviour
{
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float punchDamage = 9f;
    [SerializeField] private float punchCooldown = 0.7f;
    [SerializeField] private SkillDefinition bareHandedSkill;
    // Trained instead of bareHandedSkill whenever an ItemDefinition.
    // isMeleeWeapon item is held (Knife, first use case, 2026-08-14) — one
    // shared skill for every melee weapon, not one per weapon type.
    [SerializeField] private SkillDefinition meleeSkill;
    [SerializeField] private float skillGain = 0.5f;

    private PlayerInteraction interaction;
    private PlayerSkills skills;
    private PlayerBuilding building;
    private PlayerEquipment equipment;
    private float cooldownRemaining;

    private void Awake()
    {
        interaction = GetComponent<PlayerInteraction>();
        skills = GetComponent<PlayerSkills>();
        building = GetComponent<PlayerBuilding>();
        equipment = GetComponent<PlayerEquipment>();
    }

    private void Update()
    {
        // Multiplayer per-connection spawning (2026-08-25) -- a non-local
        // replica must never read this machine's own mouse/keyboard for
        // punch input on someone else's behalf.
        if (!isLocalPlayer) return;

        if (cooldownRemaining > 0f)
            cooldownRemaining -= Time.deltaTime;

        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
        // Same cursor-lock guard as movement/look — no punching while a
        // menu has the cursor unlocked.
        if (Cursor.lockState != CursorLockMode.Locked) return;
        // A left-click with a piece armed is PlayerBuilding's to handle
        // (placing it) — building takes priority, punching stays silent.
        if (building.ArmedPiece != null) return;
        if (cooldownRemaining > 0f) return;
        // A held Bow means left-click is PlayerRangedCombat's draw/fire
        // gesture instead — you can't punch while holding a bow. Checked
        // fresh per click, same "gate evaluated at the moment it
        // matters" spirit as ResolveAttack's own melee-weapon check.
        if (IsHoldingRangedWeapon()) return;

        TryPunch();
    }

    private bool IsHoldingRangedWeapon()
    {
        foreach (var item in equipment.GetHandItems())
            if (item != null && item.isRangedWeapon) return true;
        return false;
    }

    private void TryPunch()
    {
        var camera = interaction.PlayerCamera;
        if (camera == null) return;

        // Cooldown starts on the swing itself, hit or miss — matches a
        // real punch's own recovery time, not just "successful hits."
        cooldownRemaining = punchCooldown;

        if (!Physics.Raycast(camera.transform.position, camera.transform.forward, out var hit, attackRange))
            return;

        var target = hit.collider.GetComponentInParent<IDamageable>();
        if (target == null) return;

        // Networked-first with a local fallback for any target that
        // doesn't have a NetworkIdentity yet (defensive, same pattern as
        // InventoryScreen's networked-Command call sites) -- so an
        // untested/edge-case IDamageable never silently stops taking
        // damage just because it wasn't included in the creature/NPC
        // NetworkIdentity pass.
        if (isClient && hit.collider.GetComponentInParent<NetworkIdentity>() is NetworkIdentity targetIdentity)
        {
            RequestPunch(targetIdentity);
            return;
        }

        var (damage, trainedSkill) = ResolveAttack();
        target.TakeDamage(damage);
        skills.GainExperience(trainedSkill, skillGain);
    }

    public void RequestPunch(NetworkIdentity targetIdentity)
    {
        CmdPunch(targetIdentity);
    }

    [Command]
    private void CmdPunch(NetworkIdentity targetIdentity)
    {
        if (targetIdentity == null) return;
        var target = targetIdentity.GetComponent<IDamageable>();
        if (target == null) return;

        var (damage, trainedSkill) = ResolveAttack();
        target.TakeDamage(damage);
        skills.GainExperience(trainedSkill, skillGain);
    }

    // Bare-handed unless a melee weapon is actually held in a hand right
    // now — checked fresh per swing (not cached), same "gate evaluated at
    // the moment it matters" spirit as every other equipped-tool check in
    // this project. First hand slot holding an isMeleeWeapon item wins;
    // two weapons at once isn't a real case today.
    private (float damage, SkillDefinition skill) ResolveAttack()
    {
        foreach (var item in equipment.GetHandItems())
        {
            if (item != null && item.isMeleeWeapon)
                return (punchDamage + CraftTierScale.WeaponDamageBonus(item.tier), meleeSkill);
        }
        return (punchDamage, bareHandedSkill);
    }
}
