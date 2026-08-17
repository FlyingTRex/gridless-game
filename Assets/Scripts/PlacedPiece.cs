using UnityEngine;

// Attached at Instantiate time to every real (non-ghost) placed building
// piece — PlayerBuilding.Confirm sets Piece right after instantiation. Pure
// data; PlayerPieceUpgrade reads it to find the upgrade target
// (Piece.nextTier) and to know what to remove on destroy. Not present on
// ghost previews (PlayerBuilding never adds this to those).
//
// RequireComponent(SaveId) added 2026-08-17 (SAVE_LOAD_PLANNING.md section
// 11) so SaveManager can give every placed structure a stable identity —
// unlike StorageBox/ResourceNode/etc., a built piece doesn't pre-exist in
// the scene at all, so restoring one means instantiating it fresh and
// reattaching the same saved id, not just finding-and-restoring data on an
// object that was already there.
[RequireComponent(typeof(SaveId))]
public class PlacedPiece : MonoBehaviour
{
    public BuildPiece Piece { get; set; }
}
