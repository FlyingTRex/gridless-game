using UnityEngine;

// A plain gray wall that reveals hidden black text — only when the local
// player is wearing Sunglasses and is actually looking at this specific
// wall. Without sunglasses (or looking elsewhere), it's just a blank wall.
[RequireComponent(typeof(Collider))]
public class SecretMessageWall : MonoBehaviour
{
    [SerializeField] private string message = "Hell Yeah Brother!";
    [SerializeField] private float viewRange = 20f;

    private Collider col;
    private Camera playerCamera;
    private PlayerSunglasses sunglasses;

    private void Awake()
    {
        col = GetComponent<Collider>();
    }

    private void Start()
    {
        // Not a child of Player (this is a world object, not player gear),
        // so it looks the player up once at startup rather than wiring a
        // scene reference for a single Easter-egg object.
        var interaction = Object.FindFirstObjectByType<PlayerInteraction>();
        playerCamera = interaction != null ? interaction.PlayerCamera : null;
        sunglasses = Object.FindFirstObjectByType<PlayerSunglasses>();
    }

    private bool IsBeingLookedAt()
    {
        if (playerCamera == null) return false;

        if (!Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out var hit, viewRange))
            return false;

        return hit.collider == col;
    }

    private void OnGUI()
    {
        if (sunglasses == null || sunglasses.Equipped == null) return;
        if (!IsBeingLookedAt()) return;

        Vector3 screenPos = playerCamera.WorldToScreenPoint(transform.position);
        if (screenPos.z <= 0f) return;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
        style.normal.textColor = Color.black;

        var rect = new Rect(screenPos.x - 200f, Screen.height - screenPos.y - 30f, 400f, 60f);
        GUI.Label(rect, message, style);
    }
}
