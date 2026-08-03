using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerVitals))]
public class FirstPersonController : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float moveSpeed = 4.5f;
    [SerializeField] private float sprintSpeed = 7f;
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
    private Vector3 velocity;
    private float pitch;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        vitals = GetComponent<PlayerVitals>();
        inventoryScreen = GetComponent<InventoryScreen>();
        renaming = GetComponent<PlayerRenaming>();
        skillsScreen = GetComponent<SkillsScreen>();
        craftingScreen = GetComponent<CraftingScreen>();
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleCursorToggle();
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
            }
        }
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

        bool wantsSprint = keyboard.leftShiftKey.isPressed && input.sqrMagnitude > 0.01f;
        bool isSprinting = wantsSprint && vitals.CanSprint;
        vitals.IsSprinting = isSprinting;

        float speed = isSprinting ? sprintSpeed : moveSpeed;
        Vector3 move = (transform.right * input.x + transform.forward * input.y) * speed;

        if (controller.isGrounded)
        {
            velocity.y = -1f;
            if (keyboard.spaceKey.wasPressedThisFrame)
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

    private const string GameVersion = "0.1.36-dev";

    private float lastSpeed;
    private bool lastSprinting;

    private void OnGUI()
    {
        var rect = new Rect(10, Screen.height - 50, 300, 40);
        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Label($"Speed: {lastSpeed:F1} m/s  Sprinting: {lastSprinting}", DebugGUI.Label);
        GUILayout.Label($"Gridless {GameVersion}", DebugGUI.Label);
        GUILayout.EndArea();
    }
}
