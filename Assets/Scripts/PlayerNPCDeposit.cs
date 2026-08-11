using UnityEngine;
using UnityEngine.InputSystem;

// Point-and-confirm targeting for "which Storage Box should this NPC
// deposit mined ore into" (2026-08-10, Chunk 5 of the Hireable NPCs build
// -- see BUGS_AND_ENHANCEMENTS.md). Same "point at a target, confirm"
// shape Ben's own design called for (comparing it to Building's socket
// selection) -- BeginTargeting is triggered from NPCJobScreen's "Set
// Deposit Container" button, which closes the menu and re-locks the
// cursor first so the player can aim normally.
//
// Reuses E to confirm, so PlayerInteraction.SuppressInteraction is set
// for the duration -- StorageBox already implements IInteractable for its
// own "pick up the box" action, and confirming a deposit target on the
// same keystroke shouldn't also pick the box up.
[RequireComponent(typeof(PlayerInteraction))]
public class PlayerNPCDeposit : MonoBehaviour
{
    [SerializeField] private float range = 5f;

    private PlayerInteraction interaction;
    private NPCJob targetJob;
    private bool isTargeting;
    private StorageBox hovered;

    public bool IsTargeting => isTargeting;

    private void Awake()
    {
        interaction = GetComponent<PlayerInteraction>();
    }

    public void BeginTargeting(NPCJob job)
    {
        targetJob = job;
        isTargeting = true;
        interaction.SuppressInteraction = true;
    }

    // Called by FirstPersonController's Escape handling, same as every
    // other screen's Close() -- Escape while targeting cancels it instead
    // of leaving SuppressInteraction stuck on.
    public void CancelTargeting()
    {
        if (!isTargeting) return;

        isTargeting = false;
        targetJob = null;
        hovered = null;
        interaction.SuppressInteraction = false;
    }

    private void Update()
    {
        if (!isTargeting) return;

        hovered = null;
        var camera = interaction.PlayerCamera;
        if (camera != null && Physics.Raycast(camera.transform.position, camera.transform.forward, out var hit, range))
            hovered = hit.collider.GetComponentInParent<StorageBox>();

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (hovered != null && keyboard.eKey.wasPressedThisFrame)
        {
            targetJob.SetDepositContainer(hovered);
            CancelTargeting();
        }
    }

    private void OnGUI()
    {
        if (!isTargeting) return;

        string text = hovered != null
            ? $"[E] Set {hovered.DisplayName} as deposit point"
            : "Look at a Storage Box to set it as the deposit point";

        var style = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(Screen.width / 2f - 200, Screen.height / 2f + 30, 400, 30), text, style);
    }
}
