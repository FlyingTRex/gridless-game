using UnityEngine;
using UnityEngine.InputSystem;

public enum MovementStance
{
    Standing,
    Kneeling,
    Crawling,
    Prone,
}

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerVitals))]
[RequireComponent(typeof(PlayerEncumbrance))]
[RequireComponent(typeof(PlayerDexterity))]
public class FirstPersonController : MonoBehaviour
{
    // Stamina tiers below SprintStaminaThreshold (see PlayerVitals) that
    // further cap movement speed, independent of stance.
    private const float LowStaminaThreshold = 10f;
    private const float LowStaminaSpeedMultiplier = 0.5f;
    private const float ZeroStaminaSpeedMultiplier = 0.1f;

    // Carried-weight speed tiers (2026-08-10, Ben's call: "let's match the
    // movement rates to strength rates") — reuses PlayerEncumbrance's own
    // load-ratio breakpoints (50/80/90/95%) instead of a separate pair of
    // thresholds, so the two systems agree on what "encumbered" means.
    // Full speed and sprint below 50% (matches "no strength gain" —
    // barely a burden); a graduated slowdown from there, sprint cut off
    // once it stops being "marginal" (80%+); heaviest penalty plus the
    // extra stamina drain lines up with the same >95% band that already
    // costs health, so the worst movement state and the dangerous-overload
    // state are the same moment, not two different thresholds to track.
    private const float NoPenaltySpeedMultiplier = 1.0f;
    private const float MarginalSpeedMultiplier = 0.85f;
    private const float BetterTierSpeedMultiplier = 0.65f;
    private const float MostTierSpeedMultiplier = 0.45f;
    private const float OverloadedSpeedMultiplier = 0.25f;
    private const float OverloadedExtraStaminaDrainPerSecond = 5f;

    [SerializeField] private Camera playerCamera;
    [SerializeField] private float moveSpeed = 4.5f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float kneelSpeedMultiplier = 0.4f;
    [SerializeField] private float crawlSpeedMultiplier = 0.2f;
    [SerializeField] private float proneSpeedMultiplier = 0.1f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float jumpStaminaCost = 10f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float lookPitchLimit = 85f;

    private CharacterController controller;
    private PlayerVitals vitals;
    private PlayerEncumbrance encumbrance;
    private PlayerDexterity dexterity;
    private PlayerRenaming renaming;
    private PlayerMenuScreen playerMenuScreen;
    private BankScreen bankScreen;
    private LockboxScreen lockboxScreen;
    private CampfireScreen campfireScreen;
    private FurnaceScreen furnaceScreen;
    private GameMenuScreen gameMenuScreen;
    private NPCHiringScreen npcHiringScreen;
    private NPCJobScreen npcJobScreen;
    private PlayerNPCDeposit npcDeposit;
    private Vector3 velocity;
    private float pitch;
    private MovementStance stance = MovementStance.Standing;

    // Read by PlayerAnimatorDriver/PlayerCameraMode -- avoids re-deriving
    // pitch from the camera's baked local-rotation Euler angles (wraparound-
    // prone) and avoids scattering raw private-field reads elsewhere, same
    // reasoning as NPCGathering's animation-driving properties.
    public float Pitch => pitch;
    public MovementStance CurrentStance => stance;

    // Used by SaveManager on load. CharacterController must be disabled
    // while its Transform is moved directly or it fights the move — same
    // "CharacterController-disable dance" AdminSpawnScreen's own comment
    // describes for placing the player onto a spawned piece.
    public void Teleport(Vector3 position, float yaw)
    {
        controller.enabled = false;
        transform.position = position;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        controller.enabled = true;
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        vitals = GetComponent<PlayerVitals>();
        encumbrance = GetComponent<PlayerEncumbrance>();
        dexterity = GetComponent<PlayerDexterity>();
        renaming = GetComponent<PlayerRenaming>();
        playerMenuScreen = GetComponent<PlayerMenuScreen>();
        bankScreen = GetComponent<BankScreen>();
        lockboxScreen = GetComponent<LockboxScreen>();
        campfireScreen = GetComponent<CampfireScreen>();
        furnaceScreen = GetComponent<FurnaceScreen>();
        gameMenuScreen = GetComponent<GameMenuScreen>();
        npcHiringScreen = GetComponent<NPCHiringScreen>();
        npcJobScreen = GetComponent<NPCJobScreen>();
        npcDeposit = GetComponent<PlayerNPCDeposit>();
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleCursorToggle();
        HandleStance();
        HandleLook();
        HandleMove();
    }

    private void HandleCursorToggle()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            // Targeting runs WITH the cursor locked (normal aiming), unlike
            // every other screen here -- Escape should just cancel it and
            // stay in gameplay, not also unlock the cursor into a state
            // nothing else is expecting.
            if (npcDeposit != null && npcDeposit.IsTargeting)
            {
                npcDeposit.CancelTargeting();
                return;
            }

            bool wasLocked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = wasLocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = wasLocked;

            // Re-locking the cursor always means "back to gameplay" — make
            // sure any open screen agrees, so I and Escape can't leave the
            // cursor and the screen's own open/closed state disagreeing.
            if (!wasLocked)
            {
                renaming?.Close();
                playerMenuScreen?.Close();
                bankScreen?.Close();
                lockboxScreen?.Close();
                campfireScreen?.Close();
                furnaceScreen?.Close();
                gameMenuScreen?.Close();
                npcHiringScreen?.Close();
                npcJobScreen?.Close();
            }
        }
    }

    // X toggles Kneeling, C toggles Crawling, Z toggles Prone — pressing
    // the key for the current stance again returns to Standing, and all
    // three are mutually exclusive (switching to one drops whichever of
    // the others was active).
    private void HandleStance()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || Cursor.lockState != CursorLockMode.Locked) return;

        if (keyboard.xKey.wasPressedThisFrame)
            stance = stance == MovementStance.Kneeling ? MovementStance.Standing : MovementStance.Kneeling;
        else if (keyboard.cKey.wasPressedThisFrame)
            stance = stance == MovementStance.Crawling ? MovementStance.Standing : MovementStance.Crawling;
        else if (keyboard.zKey.wasPressedThisFrame)
            stance = stance == MovementStance.Prone ? MovementStance.Standing : MovementStance.Prone;
    }

    private void HandleLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked || Mouse.current == null || playerCamera == null)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue() * mouseSensitivity;
        transform.Rotate(Vector3.up, delta.x);

        pitch = Mathf.Clamp(pitch - delta.y, -lookPitchLimit, lookPitchLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMove()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Same guard as HandleLook — while any screen has the cursor
        // unlocked (the player menu, renaming a world object), WASD/Space
        // shouldn't move or jump the player. Previously only
        // look was gated, so e.g. typing a space into the rename box's
        // text field also triggered a jump.
        if (Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 input = Vector2.zero;
        if (keyboard.wKey.isPressed) input.y += 1f;
        if (keyboard.sKey.isPressed) input.y -= 1f;
        if (keyboard.dKey.isPressed) input.x += 1f;
        if (keyboard.aKey.isPressed) input.x -= 1f;
        input = Vector2.ClampMagnitude(input, 1f);

        float loadRatio = encumbrance.LoadRatio;

        bool isMoving = input.sqrMagnitude > 0.01f;
        bool wantsSprint = keyboard.leftShiftKey.isPressed && isMoving && stance == MovementStance.Standing;
        bool isSprinting = wantsSprint && vitals.CanSprint && loadRatio <= PlayerEncumbrance.BetterGainThreshold;
        vitals.IsSprinting = isSprinting;

        // Regular movement drains stamina too, just slower than sprinting
        // — this covers both plain walking and holding Shift below
        // SprintStaminaThreshold (sprint gives no speed bonus there, but
        // still counts as "moving", not "resting"). Kneeling/crawling/
        // prone don't drain even while moving in those stances — only
        // Standing movement does.
        vitals.IsWalking = isMoving && stance == MovementStance.Standing && !isSprinting;

        // Stamina only climbs back up while stopped, kneeling, crawling,
        // or prone — any Standing movement (walking or sprinting) holds
        // it flat or drains it instead.
        vitals.CanRegenStamina = !isMoving || stance != MovementStance.Standing;

        // Dexterity's "sneaking" input (DEXTERITY_CONSTITUTION_PLANNING.md)
        // — moving in any of the three non-Standing stances.
        dexterity.IsSneaking = isMoving && stance != MovementStance.Standing;

        float baseSpeed = stance switch
        {
            MovementStance.Kneeling => moveSpeed * kneelSpeedMultiplier,
            MovementStance.Crawling => moveSpeed * crawlSpeedMultiplier,
            MovementStance.Prone => moveSpeed * proneSpeedMultiplier,
            _ => isSprinting ? sprintSpeed : moveSpeed,
        };

        float staminaMultiplier = 1f;
        if (vitals.Stamina <= 0f)
            staminaMultiplier = ZeroStaminaSpeedMultiplier;
        else if (vitals.Stamina < LowStaminaThreshold)
            staminaMultiplier = LowStaminaSpeedMultiplier;

        float encumbranceMultiplier = NoPenaltySpeedMultiplier;
        if (loadRatio > PlayerEncumbrance.OverloadThreshold)
            encumbranceMultiplier = OverloadedSpeedMultiplier;
        else if (loadRatio > PlayerEncumbrance.MostGainThreshold)
            encumbranceMultiplier = MostTierSpeedMultiplier;
        else if (loadRatio > PlayerEncumbrance.BetterGainThreshold)
            encumbranceMultiplier = BetterTierSpeedMultiplier;
        else if (loadRatio > PlayerEncumbrance.MarginalGainThreshold)
            encumbranceMultiplier = MarginalSpeedMultiplier;

        if (isMoving && loadRatio > PlayerEncumbrance.OverloadThreshold)
            vitals.ConsumeStamina(OverloadedExtraStaminaDrainPerSecond * Time.deltaTime);

        float speed = baseSpeed * dexterity.SpeedMultiplier * staminaMultiplier * encumbranceMultiplier;
        Vector3 move = (transform.right * input.x + transform.forward * input.y) * speed;

        if (controller.isGrounded)
        {
            velocity.y = -1f;
            if (keyboard.spaceKey.wasPressedThisFrame && stance == MovementStance.Standing)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                vitals.ConsumeStamina(jumpStaminaCost);
                dexterity.GrantJumpGain();
            }
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        Vector3 motion = move + Vector3.up * velocity.y;
        controller.Move(motion * Time.deltaTime);

        lastSpeed = speed;
        lastSprinting = isSprinting;
    }

    // CharacterController.Move() resolves through its own kinematic capsule
    // cast, not the normal PhysX solver, so it never fires OnCollisionEnter
    // on whatever it touches — this is the actual message it does send, on
    // the controller's own GameObject, once per thing it bumps into. Only
    // consumer today is SoccerBall (2026-08-09), found live after it turned
    // out completely un-kickable — walking into a Rigidbody without this
    // hook just walks through it, no contact event fires anywhere.
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.TryGetComponent(out SoccerBall ball))
            ball.TryKick(gameObject);
    }

    private const string GameVersion = "0.3.73-dev";

    private float lastSpeed;
    private bool lastSprinting;

    private void OnGUI()
    {
        // Height/Y sized for 3 label lines (Speed/Sprinting, Stance, version)
        // with a 10px margin on all sides — previously fixed at 56/Screen.height-66
        // (2 lines' worth) from before the Stance line existed, clipping the
        // version line off the bottom.
        var rect = new Rect(10, Screen.height - 86, 300, 76);
        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Label($"Speed: {lastSpeed:F1} m/s  Sprinting: {lastSprinting}", DebugGUI.Label);
        GUILayout.Label($"Stance: {stance}", DebugGUI.Label);
        GUILayout.Label($"Gridless {GameVersion}", DebugGUI.Label);
        GUILayout.EndArea();
    }
}
