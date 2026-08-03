using UnityEngine;

// Shows a StorageBox's name near the crosshair while the player is looking
// directly at it. Reuses PlayerInteraction's camera for the raycast (see
// PlayerInteraction.PlayerCamera) but with its own, longer range — reading
// a container's name from across a room shouldn't require being close
// enough to actually use it.
[RequireComponent(typeof(PlayerInteraction))]
public class StorageBoxHover : MonoBehaviour
{
    [SerializeField] private float hoverRange = 20f;

    private PlayerInteraction interaction;
    private string hoveredName;

    private void Awake()
    {
        interaction = GetComponent<PlayerInteraction>();
    }

    private void Update()
    {
        hoveredName = null;

        var camera = interaction.PlayerCamera;
        if (camera == null) return;

        if (Physics.Raycast(camera.transform.position, camera.transform.forward, out var hit, hoverRange))
        {
            var box = hit.collider.GetComponentInParent<StorageBox>();
            if (box != null)
                hoveredName = box.DisplayName;
        }
    }

    // Drawn above the crosshair — PlayerInteraction's own interact prompt
    // (for IInteractable targets) draws below it, so a nameplate here
    // never competes with it for the same spot.
    private void OnGUI()
    {
        if (hoveredName == null) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
        var rect = new Rect(Screen.width / 2f - 150, Screen.height / 2f - 55, 300, 30);
        GUI.Label(rect, hoveredName, style);
    }
}
