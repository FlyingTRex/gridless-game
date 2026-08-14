using UnityEngine;
using UnityEngine.InputSystem;

// Bare-handed melee — the first Basic Combat action (2026-08-10, first
// target: HostileCreature/Wolf). Deliberately its own input handling, not
// IInteractable's hold-and-release model (same reasoning PlayerPieceUpgrade
// already applied to a different action) — an attack needs to resolve on a
// quick tap, not a multi-second hold.
[RequireComponent(typeof(PlayerInteraction))]
[RequireComponent(typeof(PlayerSkills))]
[RequireComponent(typeof(PlayerBuilding))]
[RequireComponent(typeof(PlayerEquipment))]
public class PlayerCombat : MonoBehaviour
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

        TryPunch();
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
