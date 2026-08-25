using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

// Bow draw/fire (Hunting Expansion design, 2026-08-15) — hold left-click
// to draw, release to fire. A separate script from PlayerCombat rather
// than folded into it: the charge-and-release shape is fundamentally
// different from melee's instant tap, same reasoning PlayerCombat's own
// header comment already gives for not reusing IInteractable's
// hold-and-release model. Only engages when a Bow (isRangedWeapon) is in
// one hand and an Arrow (isArrow) in the other — "what if we were lazy"
// design pivot: the off-hand slot doubles as the quiver (whichever Arrow
// tier is physically equipped is what fires), so no dedicated Quiver
// item or two-handed equip system was needed. PlayerCombat.cs itself
// gates out punching whenever a ranged weapon is held, so the two
// scripts never both react to the same click.
//
// Multiplayer Phase 3 sub-phase 4, 2026-08-23: converted to
// NetworkBehaviour, same shape as PlayerCombat's melee Command. The
// client still resolves the aim raycast/target locally (spread, draw
// fraction, camera transform are all only known client-side), then
// RequestFireArrow/CmdFireArrow re-derives the equipped bow/arrow, the
// damage formula, and the arrow consumption entirely server-side off
// real PlayerEquipment/PlayerSkills data — the client only ever tells
// the server *what it hit*, never *how much damage it did*, same trust
// boundary as melee. SpawnFlightVisual stays a pure client-side cosmetic
// call, same as before (the hit already resolved via the Command by the
// time the visual plays). Known deferred gap: the consumed arrow's
// remaining stack *count* doesn't sync to a remote client today --
// PlayerEquipment.syncedSlots only tracks which item occupies a slot, not
// how many, same shape as Crafting's already-deferred progress-sync gap.
[RequireComponent(typeof(PlayerInteraction))]
[RequireComponent(typeof(PlayerSkills))]
[RequireComponent(typeof(PlayerEquipment))]
[RequireComponent(typeof(PlayerBuilding))]
public class PlayerRangedCombat : NetworkBehaviour
{
    private const float DrawTimeSeconds = 1.2f;
    private const float BaseRange = 25f;
    private const float BaseCooldown = 0.5f;
    private const float BaseDamageMin = 2f;
    private const float BaseDamageMax = 4f;
    private const float ArrowFlightSpeed = 40f;

    private static readonly string[] HandSlots = PlayerEquipSlots.Hands;
    private static readonly int IsDrawingBowParam = Animator.StringToHash("IsDrawingBow");
    private static readonly int ReleaseBowParam = Animator.StringToHash("ReleaseBow");

    [SerializeField] private SkillDefinition strengthSkill;
    [SerializeField] private SkillDefinition dexteritySkill;
    [SerializeField] private SkillDefinition archerySkill;
    [SerializeField] private float archerySkillGain = 1f;

    [Header("Aim zoom")]
    [SerializeField] private float zoomFOV = 45f;
    [SerializeField] private float zoomLerpSpeed = 8f;

    [Header("Visuals")]
    [SerializeField] private GameObject arrowFlightVisualPrefab;

    private PlayerInteraction interaction;
    private PlayerSkills skills;
    private PlayerEquipment equipment;
    private PlayerBuilding building;
    private PlayerBodyModel bodyModel;

    private bool isDrawing;
    private float drawStartTime;
    private float cooldownRemaining;
    private float normalFOV = -1f;
    private bool animatorDrawStateSet;

    public bool IsDrawing => isDrawing;

    private void Awake()
    {
        interaction = GetComponent<PlayerInteraction>();
        skills = GetComponent<PlayerSkills>();
        equipment = GetComponent<PlayerEquipment>();
        building = GetComponent<PlayerBuilding>();
        bodyModel = GetComponent<PlayerBodyModel>();
    }

    private void Update()
    {
        // Multiplayer per-connection spawning (2026-08-25) -- a non-local
        // replica must never read this machine's own mouse/keyboard for
        // aim/draw/fire input on someone else's behalf.
        if (!isLocalPlayer) return;

        if (cooldownRemaining > 0f)
            cooldownRemaining -= Time.deltaTime;

        var mouse = Mouse.current;
        bool canOperate = mouse != null && Cursor.lockState == CursorLockMode.Locked && building.ArmedPiece == null;

        ItemDefinition bow = null, arrow = null;
        string arrowHand = null;
        if (canOperate)
            (bow, arrow, arrowHand) = GetEquippedBowAndArrow();

        if (bow == null || arrow == null)
        {
            if (isDrawing) Debug.Log($"[RangedDebug] isDrawing->false (bow/arrow gone) canOperate={canOperate}");
            isDrawing = false;
        }
        else if (!isDrawing && mouse.leftButton.wasPressedThisFrame && cooldownRemaining <= 0f)
        {
            isDrawing = true;
            drawStartTime = Time.time;
            Debug.Log($"[RangedDebug] isDrawing->true at Time.time={Time.time:F2}");
        }
        else if (isDrawing && mouse.leftButton.wasReleasedThisFrame)
        {
            Debug.Log($"[RangedDebug] isDrawing->false (fired) heldTime={Time.time - drawStartTime:F2}");
            Fire(bow, arrow, arrowHand);
            isDrawing = false;
        }

        UpdateAnimatorState();
        UpdateZoom();
    }

    // A Bow held in one hand with any isArrow item in the other — same
    // priority-free scan PlayerCombat.ResolveAttack already uses for
    // isMeleeWeapon, just needing two flags satisfied instead of one.
    private (ItemDefinition bow, ItemDefinition arrow, string arrowHand) GetEquippedBowAndArrow()
    {
        ItemDefinition bow = null;
        ItemDefinition arrow = null;
        string arrowHand = null;

        foreach (var handName in HandSlots)
        {
            var slot = equipment.GetSlot(handName);
            if (slot == null || slot.Slots.Count == 0) continue;

            var item = slot.Slots[0].item;
            if (item == null) continue;

            if (item.isRangedWeapon) bow = item;
            else if (item.isArrow) { arrow = item; arrowHand = handName; }
        }

        return (bow, arrow, arrowHand);
    }

    private void Fire(ItemDefinition bow, ItemDefinition arrow, string arrowHand)
    {
        float strength = strengthSkill != null ? skills.GetAttributeValue(strengthSkill) : 0f;
        float dexterity = dexteritySkill != null ? skills.GetAttributeValue(dexteritySkill) : 0f;

        cooldownRemaining = BaseCooldown * (1f - dexterity / 100f * 0.5f);

        float heldTime = Time.time - drawStartTime;
        float maxDraw = 0.5f + 0.5f * (strength / 100f);
        float drawFraction = Mathf.Clamp01(Mathf.Min(heldTime / DrawTimeSeconds, maxDraw));

        var camera = interaction.PlayerCamera;
        if (camera == null) return;

        float range = BaseRange * drawFraction;
        float spread = CraftTierScale.ArrowAccuracySpreadDegrees(arrow.tier) * (1f - dexterity / 100f * 0.3f);

        Vector3 direction = ApplySpread(camera.transform.forward, Mathf.Max(spread, 0f));

        // Client resolves what got hit (aim/spread/range are only known
        // client-side); the server decides how much damage it did and
        // whether the arrow is actually there to consume, same trust
        // boundary PlayerCombat's melee Command uses.
        Vector3 endPoint;
        NetworkIdentity targetIdentity = null;
        if (Physics.Raycast(camera.transform.position, direction, out var hit, range))
        {
            endPoint = hit.point;
            targetIdentity = hit.collider.GetComponentInParent<NetworkIdentity>();
        }
        else
        {
            endPoint = camera.transform.position + direction * range;
        }

        if (isClient)
            RequestFireArrow(arrowHand, targetIdentity, drawFraction);

        SpawnFlightVisual(camera.transform.position, endPoint);

        var animator = bodyModel != null ? bodyModel.ActiveAnimator : null;
        animator?.SetTrigger(ReleaseBowParam);
    }

    public void RequestFireArrow(string arrowHand, NetworkIdentity targetIdentity, float drawFraction)
    {
        CmdFireArrow(arrowHand, targetIdentity, drawFraction);
    }

    [Command]
    private void CmdFireArrow(string arrowHand, NetworkIdentity targetIdentity, float drawFraction)
    {
        var (bow, arrow, resolvedHand) = GetEquippedBowAndArrow();
        if (bow == null || arrow == null || resolvedHand != arrowHand) return;

        // Consume 1 arrow from the exact hand slot it came from — if the
        // stack was already gone by release time (dragged away mid-draw,
        // edge case), the shot still goes on cooldown but doesn't fire.
        var arrowSlot = equipment.GetSlot(arrowHand);
        if (arrowSlot == null || !arrowSlot.RemoveItem(arrow, 1))
            return;

        float baseDamage = Random.Range(BaseDamageMin, BaseDamageMax);
        float damage = (baseDamage + arrow.EffectiveArrowDamageBonus + CraftTierScale.BowDamageBonus(bow.tier))
            * Mathf.Clamp01(drawFraction);

        if (targetIdentity != null)
            targetIdentity.GetComponent<IDamageable>()?.TakeDamage(damage);

        if (archerySkill != null)
            skills.GainExperience(archerySkill, archerySkillGain);
    }

    // Purely cosmetic — the hit itself already resolved above via
    // instant hitscan, same as PlayerCombat's punch. This just gives the
    // shot a visible flight instead of a silent invisible hit.
    private void SpawnFlightVisual(Vector3 start, Vector3 end)
    {
        if (arrowFlightVisualPrefab == null) return;

        var instance = Instantiate(arrowFlightVisualPrefab);
        var flying = instance.GetComponent<FlyingArrow>();
        if (flying != null)
            flying.Launch(start, end, ArrowFlightSpeed);
        else
            Destroy(instance);
    }

    // Random deviation within a cone of the given half-angle around
    // forward — the mechanism behind the whole "spread cone" accuracy
    // model, not a flat hit/miss percentage roll.
    private static Vector3 ApplySpread(Vector3 forward, float spreadDegrees)
    {
        if (spreadDegrees <= 0f) return forward;

        Vector3 perpendicular = Vector3.Cross(forward, Vector3.up);
        if (perpendicular.sqrMagnitude < 0.001f) perpendicular = Vector3.right;
        perpendicular.Normalize();

        float tiltAngle = Random.Range(0f, spreadDegrees);
        float spinAngle = Random.Range(0f, 360f);

        Quaternion tilt = Quaternion.AngleAxis(tiltAngle, perpendicular);
        Quaternion spin = Quaternion.AngleAxis(spinAngle, forward);
        return spin * tilt * forward;
    }

    // Full-body Load->Hold state swap while drawing (Ben's call,
    // 2026-08-15: simpler and lower-risk than a masked upper-body layer,
    // reasonable since drawing-while-sprinting isn't a supported case
    // here). IsDrawingBow only gets set on an actual change, same
    // "trigger only on transition" discipline PlayerAnimatorDriver's own
    // StanceChanged already uses, so this doesn't fight the Animator's
    // own Load-finishes-then-Hold-loops transition every frame.
    private void UpdateAnimatorState()
    {
        var animator = bodyModel != null ? bodyModel.ActiveAnimator : null;
        if (animator == null) return;

        if (isDrawing != animatorDrawStateSet)
        {
            animator.SetBool(IsDrawingBowParam, isDrawing);
            animatorDrawStateSet = isDrawing;
        }
    }

    private void UpdateZoom()
    {
        var camera = interaction.PlayerCamera;
        if (camera == null) return;

        if (normalFOV < 0f) normalFOV = camera.fieldOfView;

        float target = isDrawing ? zoomFOV : normalFOV;
        camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, target, Time.deltaTime * zoomLerpSpeed);
    }

    private void OnGUI()
    {
        if (!isDrawing) return;

        float heldTime = Time.time - drawStartTime;
        float progress = Mathf.Clamp01(heldTime / DrawTimeSeconds);

        const float width = 160f;
        const float height = 12f;
        var rect = new Rect((Screen.width - width) / 2f, Screen.height * 0.68f, width, height);

        GUI.Box(rect, GUIContent.none);
        var fillRect = new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * progress, rect.height - 4f);
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, Color.white, 0f, 0f);
    }
}
