using UnityEngine;

// Which kind of neighbor this anchor point accepts — see design-brief.md's
// Building System section (2026-08-08). Only FoundationEdge exists for the
// v1 Foundation-only pass; WallBottom/WallTop/WallSide/PoleTop are named
// ahead of time so Wall/Pole/Door don't need a second enum pass later.
public enum SocketType
{
    FoundationEdge,
    WallBottom,
    WallTop,
    WallSide,
    PoleTop,
    DoorFrame,
}

// A typed anchor point on a build piece prefab — a child GameObject sitting
// exactly where a neighboring piece should snap to. `occupied` is set once
// something has actually snapped here, so PlayerBuilding's socket search can
// skip already-used anchors instead of stacking two pieces on the same spot.
public class BuildSocket : MonoBehaviour
{
    [SerializeField] private SocketType socketType;

    public SocketType SocketType => socketType;
    public bool Occupied { get; set; }

    // FoundationEdge pairs with another FoundationEdge (two foundation
    // panels tiling side by side) or a Wall's WallBottom (a wall rising
    // from that edge). WallTop self-pairs — a Wall's own top socket and a
    // Roof panel's eave socket are both WallTop, matching FoundationEdge's
    // own self-pairing pattern for the same reason (either side of a Roof
    // pass could arm first). Extend this switch, not a new mechanism, when
    // Pole/Door sockets are added.
    public bool IsCompatibleWith(SocketType other) => socketType switch
    {
        SocketType.FoundationEdge => other == SocketType.FoundationEdge || other == SocketType.WallBottom,
        SocketType.WallBottom => other == SocketType.FoundationEdge,
        SocketType.WallTop => other == SocketType.WallTop,
        // A Door-Frame Wall's own frame opening and a Door piece's hinge
        // attach point — same self-pairing shape as WallTop, since either
        // side could in principle arm first.
        SocketType.DoorFrame => other == SocketType.DoorFrame,
        // A Pole's own top frame and Foundation's new center-bottom socket
        // (2026-08-10) — same self-pairing shape again: a Foundation
        // stacks on top of a Pole to sit elevated on stilts.
        SocketType.PoleTop => other == SocketType.PoleTop,
        _ => false,
    };

    // Called on destroy (PlayerPieceUpgrade, 2026-08-08) — since two
    // snapped sockets end up at the exact same world position by
    // construction (see PlayerBuilding's snap-offset math), freeing "the
    // other side" of a connection is just finding any other Occupied
    // socket sitting at (near enough) the same point, no stored
    // bidirectional link needed. Frees every socket on `instance` and
    // whatever they were touching.
    public static void FreeConnectedSockets(GameObject instance)
    {
        const float samePointTolerance = 0.1f;

        foreach (var mine in instance.GetComponentsInChildren<BuildSocket>())
        {
            mine.Occupied = false;

            foreach (var other in FindObjectsByType<BuildSocket>(FindObjectsSortMode.None))
            {
                if (other == mine || !other.Occupied) continue;
                if (Vector3.Distance(other.transform.position, mine.transform.position) < samePointTolerance)
                    other.Occupied = false;
            }
        }
    }
}
