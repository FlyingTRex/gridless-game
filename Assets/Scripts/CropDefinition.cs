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

    // Instantiated per-cell as the growing-plant visual, scaled through the
    // same 3-stage progression GardenPlot.cs (the single-plot POC) already
    // uses. Null falls back to no visual at all — a cell can still function
    // (state/timer/harvest all work), it just shows nothing growing, same
    // "missing visual isn't a functional blocker" convention as a
    // blank-tile icon.
    public GameObject growingVisualPrefab;
}
