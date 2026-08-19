using System;
using UnityEngine;

// Fog-of-war reveal tracking for the Player Map (PLAYER_MAP_PLANNING.md,
// 2026-08-16). Splits the playable world (WorldBounds.cs) into a grid of
// square cells and marks a cell permanently revealed once anything gets
// close enough to it — the player's own walking radius today, and
// (future, not wired yet — see RevealCircle) a Village Flag/City Statue's
// bigger one-shot reveal once those exist. Deliberately state-only, no
// drawing here — MapScreen.cs owns turning this into pixels.
//
// Cell size (2m) is a first-pass number, not derived from anything —
// small enough that a 25m reveal radius reads as a reasonably smooth
// circle (~12 cells across), coarse enough that a 200x200 world is only
// 100x100 = 10,000 cells (a plain bool[,] is a trivial ~10KB, no need for
// a sparse/bitset representation at this scale).
//
// Explicitly NOT save/loaded yet (PLAYER_MAP_PLANNING.md section 3 flags
// this as real follow-up scope, not silently skipped) — explored state
// resets each session until that lands, same "ship the mechanic, flag
// persistence as a follow-up" pattern Skill Books already went through.
public class PlayerMapExploration : MonoBehaviour
{
    private const float CellSize = 2f;
    private const float WalkRevealRadius = 25f;

    private bool[,] revealed;
    private int gridWidth;
    private int gridHeight;
    private Bounds worldBounds;

    // Bumped whenever a new cell gets revealed — MapScreen checks this to
    // know whether its cached texture needs updating, instead of
    // re-reading the whole grid every OnGUI frame regardless of change.
    public int RevealVersion { get; private set; }

    public int GridWidth { get { EnsureInitialized(); return gridWidth; } }
    public int GridHeight { get { EnsureInitialized(); return gridHeight; } }
    public Bounds WorldBounds { get { EnsureInitialized(); return worldBounds; } }

    private void Awake()
    {
        EnsureInitialized();
    }

    // Lazily (re)builds the grid if it's ever found missing when accessed —
    // not just a defensive no-op. A live incident (2026-08-18, see
    // BUGS_AND_ENHANCEMENTS.md's "Player Map screen rendered blank" entry)
    // traced to exactly this: `revealed`/`gridWidth`/`gridHeight` are plain
    // fields, not [SerializeField], so a mid-Play-mode domain reload
    // (CLAUDE.md's own documented hazard) resets them to null/0 without
    // Awake() running again — MapScreen.EnsureTexture then silently built a
    // 0x0 Texture2D instead of throwing, which is why the screen went
    // blank with nothing in the log. Guarding every public entry point
    // means the Map self-heals (loses only unsaved-this-session reveal
    // progress) instead of rendering blank, regardless of what causes
    // `revealed` to go missing.
    private void EnsureInitialized()
    {
        if (revealed != null) return;
        worldBounds = global::WorldBounds.GetPlayableBounds();
        gridWidth = Mathf.Max(1, Mathf.CeilToInt(worldBounds.size.x / CellSize));
        gridHeight = Mathf.Max(1, Mathf.CeilToInt(worldBounds.size.z / CellSize));
        revealed = new bool[gridWidth, gridHeight];
    }

    private void Update()
    {
        RevealCircle(transform.position, WalkRevealRadius);
    }

    public bool IsRevealed(int cellX, int cellZ)
    {
        EnsureInitialized();
        if (cellX < 0 || cellX >= gridWidth || cellZ < 0 || cellZ >= gridHeight) return false;
        return revealed[cellX, cellZ];
    }

    // Public so a Village Flag/City Statue can call this once built (see
    // PLAYER_MAP_PLANNING.md section 1's reveal-radius table) — nothing
    // calls it with a source other than the player's own walking radius
    // yet, but the method doesn't need to change when that happens, just
    // a new caller.
    public void RevealCircle(Vector3 worldPos, float radiusMeters)
    {
        EnsureInitialized();
        WorldToCell(worldPos, out int centerX, out int centerZ);
        int cellRadius = Mathf.CeilToInt(radiusMeters / CellSize);
        float radiusSqCells = (radiusMeters / CellSize) * (radiusMeters / CellSize);

        int minX = Mathf.Max(0, centerX - cellRadius);
        int maxX = Mathf.Min(gridWidth - 1, centerX + cellRadius);
        int minZ = Mathf.Max(0, centerZ - cellRadius);
        int maxZ = Mathf.Min(gridHeight - 1, centerZ + cellRadius);

        bool revealedAny = false;
        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                if (revealed[x, z]) continue;
                float dx = x - centerX;
                float dz = z - centerZ;
                if (dx * dx + dz * dz > radiusSqCells) continue;

                revealed[x, z] = true;
                revealedAny = true;
            }
        }

        if (revealedAny) RevealVersion++;
    }

    public void WorldToCell(Vector3 worldPos, out int cellX, out int cellZ)
    {
        EnsureInitialized();
        cellX = Mathf.FloorToInt((worldPos.x - worldBounds.min.x) / CellSize);
        cellZ = Mathf.FloorToInt((worldPos.z - worldBounds.min.z) / CellSize);
    }

    // ---- Save/load support (SaveManager.CapturePlayer/RestorePlayer) ----
    //
    // Bit-packed, not one bool/byte per cell verbatim (BUGS_AND_ENHANCEMENTS.md
    // flagged this explicitly when the fog-of-war grid shipped without
    // persistence) — a 100x100 grid is 10,000 cells, which packs into
    // 1,250 bytes regardless of how much is actually revealed, instead of
    // a much larger plain array/JSON-bool-list encoding.

    public string CaptureRevealedBase64()
    {
        EnsureInitialized();
        int totalBits = gridWidth * gridHeight;
        var bytes = new byte[(totalBits + 7) / 8];
        int bitIndex = 0;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                if (revealed[x, z])
                    bytes[bitIndex / 8] |= (byte)(1 << (bitIndex % 8));
                bitIndex++;
            }
        }
        return Convert.ToBase64String(bytes);
    }

    // Defensive against a mismatched grid size (e.g. a future Terrain
    // resize between save and load) — restores whatever bits are actually
    // present in the decoded byte array and simply stops there, rather
    // than assuming it exactly matches the current gridWidth/gridHeight.
    public void RestoreRevealedBase64(string base64)
    {
        EnsureInitialized();
        if (string.IsNullOrEmpty(base64)) return;

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            Debug.LogWarning("PlayerMapExploration: save data had malformed map-exploration " +
                "data, skipping restore.");
            return;
        }

        int bitIndex = 0;
        bool revealedAny = false;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                if (bitIndex / 8 >= bytes.Length) return;

                bool bit = (bytes[bitIndex / 8] & (1 << (bitIndex % 8))) != 0;
                if (bit && !revealed[x, z])
                {
                    revealed[x, z] = true;
                    revealedAny = true;
                }
                bitIndex++;
            }
        }

        if (revealedAny) RevealVersion++;
    }
}
