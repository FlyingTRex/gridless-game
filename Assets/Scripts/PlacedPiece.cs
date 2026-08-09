using UnityEngine;

// Attached at Instantiate time to every real (non-ghost) placed building
// piece — PlayerBuilding.Confirm sets Piece right after instantiation. Pure
// data; PlayerPieceUpgrade reads it to find the upgrade target
// (Piece.nextTier) and to know what to remove on destroy. Not present on
// ghost previews (PlayerBuilding never adds this to those).
public class PlacedPiece : MonoBehaviour
{
    public BuildPiece Piece { get; set; }
}
