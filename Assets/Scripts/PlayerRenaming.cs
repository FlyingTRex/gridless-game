using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

// Right-click prompt for renaming a world object that implements
// IRenameable (e.g. a StorageBox). Uses the same camera/range as
// PlayerInteraction's E-to-interact raycast, just triggered by the right
// mouse button and opening a small text-entry window instead of acting
// immediately.
[RequireComponent(typeof(PlayerInteraction))]
public class PlayerRenaming : NetworkBehaviour
{
    [SerializeField] private float interactRange = 3f;

    private PlayerInteraction interaction;
    private IRenameable target;
    private NetworkIdentity targetIdentity;
    private string editingName;
    // Multiplayer per-connection spawning (2026-08-25) -- see
    // FirstPersonController's own field comment for why every sibling on
    // the Player root needs this same gate.
    private NetworkIdentity netIdentity;

    public bool IsOpen => target != null;

    private void Awake()
    {
        interaction = GetComponent<PlayerInteraction>();
        netIdentity = GetComponent<NetworkIdentity>();
    }

    private void Update()
    {
        if (netIdentity != null && !netIdentity.isLocalPlayer) return;
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
        // FIXED (2026-08-26, found live with traskmi): a rename used to
        // just call target.Rename() directly on whichever machine
        // triggered it -- no networking at all, so it never replicated
        // to anyone else. The actual write now happens server-side via
        // CmdRename, referencing the target by its NetworkIdentity since
        // that's the only way to point at a specific networked object
        // across a Command. See BUGS_AND_ENHANCEMENTS.md.
        targetIdentity = (renameable as Component)?.GetComponent<NetworkIdentity>();
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
        targetIdentity = null;
        editingName = null;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        interaction.SuppressInteraction = false;
    }

    private void OnGUI()
    {
        if (netIdentity != null && !netIdentity.isLocalPlayer) return;
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
            if (targetIdentity != null)
                CmdRename(targetIdentity, editingName);
            Close();
        }
        else if (cancel)
        {
            Close();
        }
    }

    [Command]
    private void CmdRename(NetworkIdentity identity, string newName)
    {
        if (identity == null) return;
        var renameable = identity.GetComponent<IRenameable>();
        renameable?.Rename(newName);
    }
}
