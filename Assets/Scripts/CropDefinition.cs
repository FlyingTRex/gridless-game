using UnityEngine;

// One row of the 4x4 GardenPlot's crop table (COOKING_AND_GARDENING_PLANNING.md
// section 3) — ties a seed item to what it grows into, how long that takes,
// and what to show in the cell while it's growing. GardenPlot4x4 looks up
// the matching CropDefinition by seedItem whenever a cell is planted; an
// unregistered seed simply can't be planted (see GardenPlot4x4.FindCrop).
[CreateAssetMenu(menuName = "Gridless/Crop Definition", fileName = "NewCrop")]
public class CropDefinition : ScriptableObject
{
    public string cropName = "New Crop";
    public ItemDefinition seedItem;
    public ItemDefinition cropItem;
    public float growDurationSeconds = 300f;

    // Chance (2026-08-16) that harvesting one unit of this crop also
    // returns 1 seedItem alongside the cropItem — a partial answer to the
    // still-open "wild forage seed sourcing" gap (MVP2_PLANNING.md item
    // 9): doesn't replace real wild-forage nodes, but means a garden can
    // now sustain/expand itself instead of being purely Admin-Spawn-fed
    // for every planting. Rolled independently per harvested unit in
    // GardenPlot4x4.TryHarvest, same "flat chance, not guaranteed"
    // convention as WolfPelt/PreyCreature's own loot-chance fields.
    [Range(0f, 1f)] public float seedDropChance = 0.3f;

    // Ordered growth-stage visuals — index 0 is the earliest stage, the
    // last entry is the fully-grown plant. GardenPlot4x4 swaps between
    // these directly as a cell's grow timer progresses (no scaling — each
    // stage is real, differently-shaped geometry, e.g. Wild Harvest: Root
    // Vegetables' own numbered growth-stage prefabs), picking whichever
    // stage index the current progress falls into. A single-entry array
    // still works fine (one static visual, e.g. this project's own
    // placeholder primitives for Corn) — GardenPlot4x4 clamps to whatever
    // length is actually provided. Empty/null means no visual at all — a
    // cell still functions (state/timer/harvest all work), same
    // "missing visual isn't a functional blocker" convention as a
    // blank-tile icon.
    public GameObject[] growthStagePrefabs;
}
