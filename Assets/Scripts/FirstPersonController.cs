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
public class FirstPersonController : MonoBehaviour
{
    // Stamina tiers below SprintStaminaThreshold (see PlayerVitals) that
    // further cap movement speed, independent of stance.
    private const float LowStaminaThreshold = 10f;
    private const float LowStaminaSpeedMultiplier = 0.5f;
    private const float ZeroStaminaSpeedMultiplier = 0.1f;

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
    private InventoryScreen inventoryScreen;
    private PlayerRenaming renaming;
    private SkillsScreen skillsScreen;
    private CraftingScreen craftingScreen;
    private BankScreen bankScreen;
    private LockboxScreen lockboxScreen;
    private Vector3 velocity;
    private float pitch;
    private MovementStance stance = MovementStance.Standing;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        vitals = GetComponent<PlayerVitals>();
        inventoryScreen = GetComponent<InventoryScreen>();
        renaming = GetComponent<PlayerRenaming>();
        skillsScreen = GetComponent<SkillsScreen>();
        craftingScreen = GetComponent<CraftingScreen>();
        bankScreen = GetComponent<BankScreen>();
        lockboxScreen = GetComponent<LockboxScreen>();
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
            bool wasLocked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = wasLocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = wasLocked;

            // Re-locking the cursor always means "back to gameplay" — make
            // sure any open screen agrees, so I and Escape can't leave the
            // cursor and the screen's own open/closed state disagreeing.
            if (!wasLocked)
            {
                inventoryScreen?.Close();
                renaming?.Close();
                skillsScreen?.Close();
                craftingScreen?.Close();
                bankScreen?.Close();
                lockboxScreen?.Close();
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
        // unlocked (Inventory, Crafting, Skills, renaming a world object),
        // WASD/Space shouldn't move or jump the player. Previously only
        // look was gated, so e.g. typing a space into the rename box's
        // text field also triggered a jump.
        if (Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 input = Vector2.zero;
        if (keyboard.wKey.isPressed) input.y += 1f;
        if (keyboard.sKey.isPressed) input.y -= 1f;
        if (keyboard.dKey.isPressed) input.x += 1f;
        if (keyboard.aKey.isPressed) input.x -= 1f;
        input = Vector2.ClampMagnitude(input, 1f);

        bool isMoving = input.sqrMagnitude > 0.01f;
        bool wantsSprint = keyboard.leftShiftKey.isPressed && isMoving && stance == MovementStance.Standing;
        bool isSprinting = wantsSprint && vitals.CanSprint;
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

        float speed = baseSpeed * staminaMultiplier;
        Vector3 move = (transform.right * input.x + transform.forward * input.y) * speed;

        if (controller.isGrounded)
        {
            velocity.y = -1f;
            if (keyboard.spaceKey.wasPressedThisFrame && stance == MovementStance.Standing)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                vitals.ConsumeStamina(jumpStaminaCost);
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

    private const string GameVersion = "0.1.48-dev";

    private float lastSpeed;
    private bool lastSprinting;

    private void OnGUI()
    {
        var rect = new Rect(10, Screen.height - 66, 300, 56);
        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Label($"Speed: {lastSpeed:F1} m/s  Sprinting: {lastSprinting}", DebugGUI.Label);
        GUILayout.Label($"Stance: {stance}", DebugGUI.Label);
        GUILayout.Label($"Gridless {GameVersion}", DebugGUI.Label);
        GUILayout.EndArea();
    }
}
