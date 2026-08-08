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
    private ISecondaryInteractable currentSecondary;
    private string currentSecondaryPrompt;
    private float holdProgress;

    private Texture2D barBackgroundTex;
    private Texture2D barFillTex;

    // Exposed so other player components (e.g. PlayerRenaming) can reuse
    // the same camera for their own raycasts instead of each needing their
    // own serialized reference wired up in the scene.
    public Camera PlayerCamera => playerCamera;

    private void Update()
    {
        ResolveTarget();

        var keyboard = Keyboard.current;

        if (current != null && keyboard != null)
        {
            if (current.IsInstant)
            {
                if (keyboard.eKey.wasPressedThisFrame)
                    current.Complete(gameObject);
            }
            else
            {
                // Hold-and-release: keep E down to fill the bar, let go (or
                // look away — ResolveTarget clears `current` too) to cancel
                // and forfeit progress. Replaces the old punch-N-times/
                // hitsToBreak model on resource nodes and trees, and covers
                // every other non-instant IInteractable the same way.
                if (keyboard.eKey.isPressed)
                {
                    holdProgress += Time.deltaTime;
                    if (holdProgress >= current.GetHoldDuration(gameObject))
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

        if (!string.IsNullOrEmpty(currentSecondaryPrompt) && keyboard != null && keyboard.fKey.wasPressedThisFrame)
            currentSecondary.CompleteSecondary(gameObject);
    }

    private void ResolveTarget()
    {
        current = null;
        currentSecondary = null;
        currentSecondaryPrompt = null;

        if (playerCamera == null) return;

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward,
                out var hit, interactRange))
        {
            current = hit.collider.GetComponentInParent<IInteractable>();
            currentSecondary = hit.collider.GetComponentInParent<ISecondaryInteractable>();
            if (currentSecondary != null)
                currentSecondaryPrompt = currentSecondary.GetSecondaryPrompt(gameObject);
        }
    }

    private const float BarWidth = 200f;
    private const float BarHeight = 10f;

    private void OnGUI()
    {
        DrawCrosshair();

        string text = null;
        float holdDuration = 0f;
        if (current != null)
        {
            text = current.Prompt;
            if (!current.IsInstant)
            {
                holdDuration = current.GetHoldDuration(gameObject);
                text += $" ({Mathf.CeilToInt(holdDuration - holdProgress)}s)";
            }
        }

        // Disambiguate with an explicit key label only when there's a
        // second option to disambiguate from — every other interactable
        // still shows its plain, unprefixed prompt as before.
        if (!string.IsNullOrEmpty(currentSecondaryPrompt))
        {
            if (!string.IsNullOrEmpty(text)) text = $"[E] {text}";
            text = string.IsNullOrEmpty(text) ? $"[F] {currentSecondaryPrompt}" : $"{text}    [F] {currentSecondaryPrompt}";
        }

        if (text != null)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(Screen.width / 2f - 150, Screen.height / 2f + 30, 300, 30), text, style);
        }

        // Green fill bar under the prompt, only while a hold is actually in
        // progress — the design brief's "green progress bar" callout, on
        // top of the text countdown rather than replacing it.
        if (current != null && !current.IsInstant && holdProgress > 0f)
            DrawHoldBar(holdProgress / Mathf.Max(holdDuration, 0.01f));
    }

    private void DrawHoldBar(float fraction)
    {
        if (barBackgroundTex == null) barBackgroundTex = SolidTexture(new Color(0f, 0f, 0f, 0.6f));
        if (barFillTex == null) barFillTex = SolidTexture(new Color(0.25f, 0.85f, 0.25f));

        var rect = new Rect(Screen.width / 2f - BarWidth / 2f, Screen.height / 2f + 62f, BarWidth, BarHeight);
        GUI.DrawTexture(rect, barBackgroundTex);

        var fillRect = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height);
        GUI.DrawTexture(fillRect, barFillTex);
    }

    private static Texture2D SolidTexture(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
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
