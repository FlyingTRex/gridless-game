using UnityEngine;
using UnityEngine.InputSystem;

// Right-click prompt for renaming a world object that implements
// IRenameable (e.g. a StorageBox). Uses the same camera/range as
// PlayerInteraction's E-to-interact raycast, just triggered by the right
// mouse button and opening a small text-entry window instead of acting
// immediately.
[RequireComponent(typeof(PlayerInteraction))]
public class PlayerRenaming : MonoBehaviour
{
    [SerializeField] private float interactRange = 3f;

    private PlayerInteraction interaction;
    private IRenameable target;
    private string editingName;

    public bool IsOpen => target != null;

    private void Awake()
    {
        interaction = GetComponent<PlayerInteraction>();
    }

    private void Update()
    {
        if (IsOpen) return;
        // Only open from normal gameplay — not while some other screen
        // already has the cursor unlocked.
        if (Cursor.lockState != CursorLockMode.Locked) return;

        var mouse = Mouse.current;
        if (mouse == null || !mouse.rightButton.wasPressedThisFrame) return;

        var camera = interaction.PlayerCamera;
        if (camera == null) return;

        if (Physics.Raycast(camera.transform.position, camera.transform.forward, out var hit, interactRange))
        {
            var renameable = hit.collider.GetComponentInParent<IRenameable>();
            if (renameable != null)
                Open(renameable);
        }
    }

    private void Open(IRenameable renameable)
    {
        target = renameable;
        editingName = renameable.DisplayName;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        // Claim E for this text field, same as PlayerNPCDeposit already
        // does for its own "point and confirm" flow (2026-08-21, found
        // live: typing a name containing "e" also fired whatever
        // IInteractable the crosshair happened to be resting on -- the
        // IMGUI TextField consumes the legacy Event system's keystroke,
        // but PlayerInteraction reads the New Input System's raw
        // Keyboard.current state directly, which is a completely
        // separate, unconsumed view of the same physical keypress.
        interaction.SuppressInteraction = true;
    }

    // Called by FirstPersonController when Escape re-locks the cursor, so
    // the two toggles can't drift out of sync with each other (mirrors
    // PlayerMenuScreen.Close()).
    public void Close()
    {
        target = null;
        editingName = null;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        interaction.SuppressInteraction = false;
    }

    private void OnGUI()
    {
        if (!IsOpen) return;

        const float width = 260f;
        const float height = 110f;
        var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);

        bool enterPressed = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return;

        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Label($"Rename {target.DisplayName}", DebugGUI.Header);

        GUI.SetNextControlName("RenameField");
        editingName = GUILayout.TextField(editingName, 40);
        GUI.FocusControl("RenameField");

        GUILayout.BeginHorizontal();
        bool save = GUILayout.Button("Save") || enterPressed;
        bool cancel = GUILayout.Button("Cancel");
        GUILayout.EndHorizontal();

        GUILayout.EndArea();

        if (save && !string.IsNullOrWhiteSpace(editingName))
        {
            target.Rename(editingName);
            Close();
        }
        else if (cancel)
        {
            Close();
        }
    }
}
