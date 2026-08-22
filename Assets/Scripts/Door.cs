using UnityEngine;

// A door leaf that snaps onto a Door-Frame Wall's SocketFrame (design-
// brief.md's Building System section, door plan added 2026-08-09). Its own
// local origin is both the socket attach point (lands exactly on the
// frame's hinge-side corner — see PlayerBuilding's doorOntoFrame case) and
// the hinge pivot: rotating the whole root transform around its own Y axis
// is the swing, no separate pivot child needed, since the model itself was
// built in Blender spanning local X 0 -> doorWidth rather than centered.
//
// First placed piece with any interaction at all — Wall/Roof/Half-Wall/
// Door-Frame Wall are all inert once placed. Bound to F (ISecondaryInteractable),
// not E (IInteractable) — every placed piece also gets a PlacedPiece
// (PlayerBuilding.Confirm), making it a PlayerPieceUpgrade target too, and
// that system already owns E for click-to-upgrade/hold-to-destroy. Tried
// E first; Ben found it live ("since destroy the panel relies on E,
// there's no key press that opens the door" — a Hammer equipped blocks
// the open entirely, not just a rare overlap). F is a clean fix, not a
// workaround: ISecondaryInteractable exists in this codebase specifically
// for "a second action on its own key," already used elsewhere for the
// exact same shape (an always-available primary plus a conditional
// second). Door has no primary E action at all, so it just skips
// IInteractable entirely rather than implementing an unused one.
public class Door : MonoBehaviour, ISecondaryInteractable
{
    [SerializeField] private float openAngle = 100f;
    [SerializeField] private float swingSpeedDegreesPerSecond = 180f;
    [SerializeField] private float autoCloseDelay = 60f;

    private Quaternion closedRotation;
    private Quaternion targetRotation;
    private bool isOpen;
    private float autoCloseTime;

    public string GetSecondaryPrompt(GameObject player) => isOpen ? "Close Door" : "Open Door";

    // Read by NPCGathering's door-detection check (NavMesh Phase 2,
    // 2026-08-21) to know whether a door blocking an NPC's path still
    // needs opening.
    public bool IsOpen => isOpen;

    // NPC-safe entry point for the same swing-away-from-position logic
    // CompleteSecondary already uses for the player — same class of gap
    // as skinning/StorageBox pickup earlier this session (a player-only
    // action needing a parallel NPC-safe one). No-ops if already open,
    // same guard CompleteSecondary implicitly gets from its own isOpen
    // check.
    public void OpenForNPC(Vector3 npcPosition)
    {
        if (!isOpen) Open(npcPosition);
    }

    private void Awake()
    {
        closedRotation = transform.rotation;
        targetRotation = closedRotation;
    }

    public void CompleteSecondary(GameObject player)
    {
        if (isOpen) Close();
        else Open(player.transform.position);
    }

    // Swings to whichever side the player is NOT standing on, so the leaf
    // never sweeps toward them — Ben's ask: "clicking on the door will
    // cause it to open away from where the player is standing... that way
    // it won't ever cause a problem opening or closing." Decided once, at
    // the moment of the click (using the door's closed-orientation forward,
    // the wall's own outward normal) — not continuously re-aimed as the
    // player moves after.
    private void Open(Vector3 playerPosition)
    {
        Vector3 toPlayer = playerPosition - transform.position;
        float side = Vector3.Dot(toPlayer, transform.forward);
        float sign = side > 0f ? -1f : 1f;

        targetRotation = closedRotation * Quaternion.Euler(0f, sign * openAngle, 0f);
        isOpen = true;
        autoCloseTime = Time.time + autoCloseDelay;
    }

    private void Close()
    {
        targetRotation = closedRotation;
        isOpen = false;
    }

    private void Update()
    {
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, swingSpeedDegreesPerSecond * Time.deltaTime);

        if (isOpen && Time.time >= autoCloseTime)
            Close();
    }
}
