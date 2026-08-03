using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerSkills))]
[RequireComponent(typeof(PlayerVitals))]
public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactRange = 3f;

    private IInteractable current;
    private IPunchable currentPunchable;
    private ISecondaryInteractable currentSecondary;
    private string currentSecondaryPrompt;
    private float holdProgress;

    // Exposed so other player components (e.g. PlayerRenaming) can reuse
    // the same camera for their own raycasts instead of each needing their
    // own serialized reference wired up in the scene.
    public Camera PlayerCamera => playerCamera;

    private void Update()
    {
        ResolveTarget();

        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (current != null && keyboard != null)
        {
            if (current.IsInstant)
            {
                if (keyboard.eKey.wasPressedThisFrame)
                    current.Complete(gameObject);
            }
            else
            {
                if (keyboard.eKey.isPressed)
                {
                    holdProgress += Time.deltaTime;
                    if (holdProgress >= current.HoldDuration)
                    {
                        current.Complete(gameObject);
                        holdProgress = 0f;
                    }
                }
                else
                {
                    holdProgress = 0f;
                }
            }
        }
        else
        {
            holdProgress = 0f;
        }

        if (currentPunchable != null && mouse != null && mouse.leftButton.wasPressedThisFrame)
            currentPunchable.OnPunch(gameObject);

        if (!string.IsNullOrEmpty(currentSecondaryPrompt) && keyboard != null && keyboard.fKey.wasPressedThisFrame)
            currentSecondary.CompleteSecondary(gameObject);
    }

    private void ResolveTarget()
    {
        current = null;
        currentPunchable = null;
        currentSecondary = null;
        currentSecondaryPrompt = null;

        if (playerCamera == null) return;

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward,
                out var hit, interactRange))
        {
            current = hit.collider.GetComponentInParent<IInteractable>();
            currentPunchable = hit.collider.GetComponentInParent<IPunchable>();
            currentSecondary = hit.collider.GetComponentInParent<ISecondaryInteractable>();
            if (currentSecondary != null)
                currentSecondaryPrompt = currentSecondary.GetSecondaryPrompt(gameObject);
        }
    }

    private void OnGUI()
    {
        DrawCrosshair();

        string text = null;
        if (current != null)
        {
            text = current.Prompt;
            if (!current.IsInstant)
                text += $" ({Mathf.CeilToInt(current.HoldDuration - holdProgress)}s)";
        }
        else if (currentPunchable != null)
        {
            text = currentPunchable.Prompt;
        }

        // Disambiguate with an explicit key label only when there's a
        // second option to disambiguate from — every other interactable
        // still shows its plain, unprefixed prompt as before.
        if (!string.IsNullOrEmpty(currentSecondaryPrompt))
        {
            if (!string.IsNullOrEmpty(text)) text = $"[E] {text}";
            text = string.IsNullOrEmpty(text) ? $"[F] {currentSecondaryPrompt}" : $"{text}    [F] {currentSecondaryPrompt}";
        }

        if (text == null) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(Screen.width / 2f - 150, Screen.height / 2f + 30, 300, 30), text, style);
    }

    private void DrawCrosshair()
    {
        const float size = 6f;
        const float thickness = 2f;
        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        DrawOutlinedRect(new Rect(cx - thickness / 2f, cy - size, thickness, size * 2f));
        DrawOutlinedRect(new Rect(cx - size, cy - thickness / 2f, size * 2f, thickness));
    }

    private static void DrawOutlinedRect(Rect r)
    {
        GUI.DrawTexture(new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2), Texture2D.blackTexture);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
    }
}
