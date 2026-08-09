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

    // FoundationEdge only pairs with another FoundationEdge for now (two
    // foundation panels tiling side by side) — the only pairing the
    // Foundation-only v1 needs. Extend this switch, not a new mechanism,
    // when Wall/Pole/Door sockets are added.
    public bool IsCompatibleWith(SocketType other) => socketType switch
    {
        SocketType.FoundationEdge => other == SocketType.FoundationEdge,
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
