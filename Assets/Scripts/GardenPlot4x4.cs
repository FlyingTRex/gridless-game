using UnityEngine;

// Full 4x4 (16-cell) Garden Plot (COOKING_AND_GARDENING_PLANNING.md section
// 3) — the scaled-up version of GardenPlot.cs's single-plant proof of
// concept, generalized to any number of registered CropDefinitions instead
// of one hardcoded Berry Bush. Each of the 16 cells runs the exact same
// "plant a whole seed stack, harvest one plant, auto-replant the next from
// the same stack" mechanic GardenPlot.cs already proved out, just
// independently per cell instead of once for the whole structure.
//
// Deliberately skips the full design's drag-and-drop seed-grid popup — E
// opens GardenPlotScreen4x4, which shows all 16 cells as buttons and a
// context panel (plant/harvest/progress) for whichever cell is selected.
// A raw Inventory-per-cell would also fight this feature: Inventory's own
// slot list compacts (RemoveAt) whenever a stack empties, so a per-cell
// index into it isn't stable — cells here are a fixed CellCount array
// instead, each tracking its own seed count directly, sidestepping that
// mismatch entirely rather than working around it.
[RequireComponent(typeof(Collider))]
public class GardenPlot4x4 : MonoBehaviour, IInteractable
{
    public const int GridSize = 4;
    public const int CellCount = GridSize * GridSize;

    // Same 3-discrete-stage growth presentation as GardenPlot.cs.
    private const float Stage1Fraction = 1f / 3f;
    private const float Stage2Fraction = 2f / 3f;
    private const float Stage1Scale = 0.35f;
    private const float Stage2Scale = 0.65f;
    private const float FullScale = 1f;

    public enum CellState { Empty, Growing, Ready }

    private struct Cell
    {
        public CropDefinition crop;
        public int count;
        public CellState state;
        public float growStartedAt;
        public GameObject visualInstance;
    }

    [SerializeField] private CropDefinition[] registeredCrops;

    // Index-matched 1:1 with the cells array below — cell i's growing
    // visual is parented under plantAnchors[i]. Sized/placed on the prefab,
    // not computed at runtime.
    [SerializeField] private Transform[] plantAnchors;

    private readonly Cell[] cells = new Cell[CellCount];

    public string DisplayName => "Garden Plot";
    public string Prompt => $"Open {DisplayName}";
    public bool IsInstant => true;
    public float GetHoldDuration(GameObject player) => 0f;
    public CropDefinition[] RegisteredCrops => registeredCrops;

    public void Complete(GameObject player) => player.GetComponent<GardenPlotScreen4x4>()?.Open(this);

    private void Update()
    {
        for (int i = 0; i < CellCount; i++)
        {
            if (cells[i].state != CellState.Growing) continue;

            var crop = cells[i].crop;
            float duration = crop != null ? crop.growDurationSeconds : 1f;
            float elapsed = Time.time - cells[i].growStartedAt;
            UpdateVisualScale(i, Mathf.Clamp01(elapsed / duration));

            if (elapsed >= duration)
                cells[i].state = CellState.Ready;
        }
    }

    public CellState GetState(int index) => cells[index].state;
    public CropDefinition GetCrop(int index) => cells[index].crop;
    public int GetSeedCount(int index) => cells[index].count;

    public float GetProgress01(int index)
    {
        var cell = cells[index];
        if (cell.state != CellState.Growing || cell.crop == null) return 0f;
        return Mathf.Clamp01((Time.time - cell.growStartedAt) / cell.crop.growDurationSeconds);
    }

    private CropDefinition FindCrop(ItemDefinition seed)
    {
        if (registeredCrops == null || seed == null) return null;
        foreach (var crop in registeredCrops)
            if (crop != null && crop.seedItem == seed) return crop;
        return null;
    }

    // Plants the player's ENTIRE current stack of the given seed into the
    // given cell at once — same "whole stack, not one at a time" mechanic
    // as GardenPlot.cs, just scoped to one cell instead of the whole
    // structure. Checks the main inventory AND a worn Backpack's own
    // nested inventory (seeds route into a worn Backpack first on pickup,
    // same reason GardenPlot.TryPlant already checks both).
    public bool TryPlant(int index, ItemDefinition seed, PlayerInventory playerInventory, PlayerBackpack backpackCarrier)
    {
        if (index < 0 || index >= CellCount) return false;
        if (cells[index].state != CellState.Empty) return false;

        var crop = FindCrop(seed);
        if (crop == null || playerInventory == null) return false;

        var backpackInventory = backpackCarrier != null && backpackCarrier.Equipped != null
            ? backpackCarrier.Equipped.Inventory
            : null;

        int mainCount = playerInventory.GetCount(seed);
        int backpackCount = backpackInventory?.GetCount(seed) ?? 0;
        int total = mainCount + backpackCount;
        if (total <= 0) return false;

        if (mainCount > 0) playerInventory.RemoveItem(seed, mainCount);
        if (backpackCount > 0) backpackInventory.RemoveItem(seed, backpackCount);

        cells[index].crop = crop;
        cells[index].count = total;
        StartGrowing(index);
        return true;
    }

    // Deposits into a worn Backpack first if there's room, falling back to
    // the main inventory for whatever didn't fit — same priority
    // GardenPlot.Harvest/PlayerLoot already use.
    public bool TryHarvest(int index, PlayerInventory playerInventory, PlayerBackpack backpackCarrier)
    {
        if (index < 0 || index >= CellCount) return false;
        if (cells[index].state != CellState.Ready) return false;

        var crop = cells[index].crop;
        if (crop == null || crop.cropItem == null || playerInventory == null) return false;

        var backpackInventory = backpackCarrier != null && backpackCarrier.Equipped != null
            ? backpackCarrier.Equipped.Inventory
            : null;

        int leftover = backpackInventory != null ? backpackInventory.AddItem(crop.cropItem, 1) : 1;
        if (leftover > 0) playerInventory.AddItem(crop.cropItem, leftover);

        cells[index].count--;
        if (cells[index].count > 0)
            StartGrowing(index);
        else
            ClearCell(index);
        return true;
    }

    private void StartGrowing(int index)
    {
        cells[index].state = CellState.Growing;
        cells[index].growStartedAt = Time.time;

        var crop = cells[index].crop;
        var anchor = plantAnchors != null && index < plantAnchors.Length ? plantAnchors[index] : null;

        if (cells[index].visualInstance == null && crop != null && crop.growingVisualPrefab != null && anchor != null)
        {
            var instance = Instantiate(crop.growingVisualPrefab, anchor);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            cells[index].visualInstance = instance;
        }

        UpdateVisualScale(index, 0f);
    }

    private void UpdateVisualScale(int index, float progress)
    {
        var instance = cells[index].visualInstance;
        if (instance == null) return;

        float scale = progress < Stage1Fraction ? Stage1Scale
            : progress < Stage2Fraction ? Stage2Scale
            : FullScale;
        instance.transform.localScale = Vector3.one * scale;
    }

    private void ClearCell(int index)
    {
        cells[index].state = CellState.Empty;
        cells[index].crop = null;
        if (cells[index].visualInstance != null) Destroy(cells[index].visualInstance);
        cells[index].visualInstance = null;
    }
}
