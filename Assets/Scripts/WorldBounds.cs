using UnityEngine;

// Shared world-bounds lookup (2026-08-16) — built ahead of the Player Map
// (PLAYER_MAP_PLANNING.md) needing a real "how big is the world" number
// instead of a guessed-from-scene-position estimate. Reads the actual
// active Terrain directly, so it stays correct automatically if the
// Terrain is ever resized or regenerated — including the future
// Terrain/hills conversion (BUGS_AND_ENHANCEMENTS.md's scene plan)
// this project already has flagged as upcoming. No hardcoded world size
// anywhere; this is the one place that ever needs to know it.
public static class WorldBounds
{
    // Fallback only, used if no Terrain exists in the scene at all (e.g.
    // a future non-Terrain-based world, or a test scene with no ground)
    // — same "don't silently produce a nonsense answer" convention
    // GroundHeight.Sample's fallbackY parameter already uses, just for a
    // 2D area instead of a single height sample.
    private const float FallbackHalfExtent = 100f;

    // World-space min/max X/Z of the playable area, read from
    // Terrain.activeTerrain (position + TerrainData.size) — not the
    // TerrainCollider's bounds, since a Terrain's own transform.position
    // is already documented (Unity's own convention) to be the min
    // corner, with TerrainData.size giving the full width/length from
    // there, so this needs no extra collider bounds lookup at all.
    public static Bounds GetPlayableBounds()
    {
        var terrain = Terrain.activeTerrain;
        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogWarning("WorldBounds: no active Terrain found — falling back to a " +
                $"{FallbackHalfExtent * 2f}x{FallbackHalfExtent * 2f} guess centered on the origin.");
            return new Bounds(Vector3.zero, new Vector3(FallbackHalfExtent * 2f, 0f, FallbackHalfExtent * 2f));
        }

        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        Vector3 center = origin + new Vector3(size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);
        return new Bounds(center, size);
    }

    // Convenience accessors for the common 2D (X/Z) case — every current
    // caller (the Player Map's fog-of-war grid) only cares about ground-
    // plane extent, not the Terrain's height range.
    public static float MinX => GetPlayableBounds().min.x;
    public static float MaxX => GetPlayableBounds().max.x;
    public static float MinZ => GetPlayableBounds().min.z;
    public static float MaxZ => GetPlayableBounds().max.z;
}
