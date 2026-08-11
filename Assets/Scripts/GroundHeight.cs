using UnityEngine;

// Shared ground-height sampling (2026-08-10) -- built ahead of the
// Terrain/hills conversion (see BUGS_AND_ENHANCEMENTS.md's scene plan) so
// HostileCreature/NPCWander/NPCMining already snap to the real ground
// surface by the time hills exist, instead of needing three separate
// retrofits later. Works identically on today's flat Ground and a future
// hilly Terrain -- it's just "raycast down, use whatever the Ground layer
// says," terrain-representation-agnostic.
//
// Restricted to a dedicated "Ground" physics layer (not a plain
// Physics.Raycast against everything) so a Wolf/NPC walking near a
// Boulder or Tree doesn't snap onto the TOP of that object's own collider
// instead of the actual ground beside it -- the raycast only ever sees
// the Ground layer, nothing else is on it.
public static class GroundHeight
{
    private const float RaycastStartHeight = 100f;
    private const float RaycastDistance = 200f;

    private static int groundMask = -1;

    // fallbackY (typically the object's current Y) is returned if the
    // raycast finds nothing -- e.g. Ground isn't tagged yet, or the point
    // is genuinely off the playable area -- so a lookup failure holds the
    // object in place instead of snapping it to 0 or falling through.
    public static float Sample(Vector3 position, float fallbackY)
    {
        if (groundMask == -1)
            groundMask = LayerMask.GetMask("Ground");

        Vector3 origin = new Vector3(position.x, position.y + RaycastStartHeight, position.z);
        if (Physics.Raycast(origin, Vector3.down, out var hit, RaycastDistance, groundMask))
            return hit.point.y;

        return fallbackY;
    }
}
