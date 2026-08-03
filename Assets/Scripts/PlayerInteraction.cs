using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerSkills))]
public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactRange = 3f;

    private PlayerInventory inventory;
    private PlayerSkills skills;
    private IInteractable current;
    private IPunchable currentPunchable;
    private float holdProgress;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        skills = GetComponent<PlayerSkills>();
    }

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
                    current.Complete(inventory, skills);
            }
            else
            {
                if (keyboard.eKey.isPressed)
                {
                    holdProgress += Time.deltaTime;
                    if (holdProgress >= current.HoldDuration)
                    {
                        current.Complete(inventory, skills);
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
            currentPunchable.OnPunch(skills);
    }

    private void ResolveTarget()
    {
        current = null;
        currentPunchable = null;

        if (playerCamera == null) return;

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward,
                out var hit, interactRange))
        {
            current = hit.collider.GetComponentInParent<IInteractable>();
            currentPunchable = hit.collider.GetComponentInParent<IPunchable>();
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
