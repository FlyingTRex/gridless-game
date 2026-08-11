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
public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float punchDamage = 9f;
    [SerializeField] private float punchCooldown = 0.7f;
    [SerializeField] private SkillDefinition bareHandedSkill;
    [SerializeField] private float skillGain = 0.5f;

    private PlayerInteraction interaction;
    private PlayerSkills skills;
    private PlayerBuilding building;
    private float cooldownRemaining;

    private void Awake()
    {
        interaction = GetComponent<PlayerInteraction>();
        skills = GetComponent<PlayerSkills>();
        building = GetComponent<PlayerBuilding>();
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

        target.TakeDamage(punchDamage);
        skills.GainExperience(bareHandedSkill, skillGain);
    }
}
