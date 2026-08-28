using UnityEngine;

// Opened by interacting (E) with a placed GardenPlot4x4 — same focused-popup
// family as CampfireScreen/LockboxScreen. Shows all 16 cells as a 4x4 grid
// of buttons; clicking a cell selects it, and a context panel below the
// grid offers whatever action that cell's state allows (plant one of the
// registered crops, watch progress, or harvest). Deliberately simpler than
// CampfireScreen's drag-and-drop — there's nothing here that needs a
// quantity or a specific slot, just "which cell, which crop, plant or
// harvest," so a click-based context panel covers the whole mechanic
// without a second self-contained drag implementation to maintain.
[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerBackpack))]
public class GardenPlotScreen4x4 : MonoBehaviour
{
    private const float PanelWidth = 420f;
    private const float MaxPanelHeight = 620f;
    private const float CellSize = 48f;
    private const float CellGap = 6f;

    private PlayerInventory playerInventory;
    private PlayerBackpack backpackCarrier;
    private GardenPlot4x4 current;
    private bool isOpen;
    private int selectedCell = -1;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
        backpackCarrier = GetComponent<PlayerBackpack>();
    }

    // Only opens from normal gameplay — same rule every other screen
    // follows, so it can't stack on top of one that already has the
    // cursor unlocked.
    public void Open(GardenPlot4x4 plot)
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        current = plot;
        selectedCell = -1;
        SetOpen(true);
    }

    // Called by FirstPersonController when Escape re-locks the cursor.
    public void Close() => SetOpen(false);

    private void SetOpen(bool value)
    {
        isOpen = value;
        if (!value)
        {
            current = null;
            selectedCell = -1;
        }
        Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = value;
    }

    private void OnGUI()
    {
        if (!isOpen || current == null) return;

        float panelHeight = Mathf.Min(MaxPanelHeight, Screen.height * 0.92f);
        var rect = new Rect((Screen.width - PanelWidth) / 2f, (Screen.height - panelHeight) / 2f, PanelWidth, panelHeight);
        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);

        GUILayout.Label(current.DisplayName, DebugGUI.Header);

        GUILayout.Space(8);
        DrawGrid();

        GUILayout.Space(10);
        DrawContextPanel();

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Close"))
            SetOpen(false);

        GUILayout.EndArea();
    }

    private void DrawGrid()
    {
        for (int row = 0; row < GardenPlot4x4.GridSize; row++)
        {
            GUILayout.BeginHorizontal();
            for (int col = 0; col < GardenPlot4x4.GridSize; col++)
            {
                if (col > 0) GUILayout.Space(CellGap);

                int index = row * GardenPlot4x4.GridSize + col;
                DrawCell(index);
            }
            GUILayout.EndHorizontal();

            if (row < GardenPlot4x4.GridSize - 1) GUILayout.Space(CellGap);
        }
    }

    private void DrawCell(int index)
    {
        var state = current.GetState(index);
        var crop = current.GetCrop(index);

        string label = state switch
        {
            GardenPlot4x4.CellState.Empty => "-",
            GardenPlot4x4.CellState.Growing => crop != null ? $"{crop.cropName}\n{Mathf.RoundToInt(current.GetProgress01(index) * 100f)}%" : "...",
            GardenPlot4x4.CellState.Ready => crop != null ? $"{crop.cropName}\nReady!" : "Ready!",
            _ => "-",
        };

        var style = index == selectedCell ? DebugGUI.TabSelected : DebugGUI.Slot;
        if (GUILayout.Button(label, style, GUILayout.Width(CellSize), GUILayout.Height(CellSize)))
            selectedCell = index;
    }

    private void DrawContextPanel()
    {
        GUILayout.Label("Selected Cell", DebugGUI.Header);

        if (selectedCell < 0)
        {
            GUILayout.Label("Click a cell above.", DebugGUI.Label);
            return;
        }

        var state = current.GetState(selectedCell);
        switch (state)
        {
            case GardenPlot4x4.CellState.Empty:
                DrawPlantOptions();
                break;
            case GardenPlot4x4.CellState.Growing:
                var crop = current.GetCrop(selectedCell);
                string cropName = crop != null ? crop.cropName : "Crop";
                GUILayout.Label($"Growing {cropName} — {Mathf.RoundToInt(current.GetProgress01(selectedCell) * 100f)}%", DebugGUI.Label);
                GUILayout.Label($"Seeds remaining in this cell (after this plant): {current.GetSeedCount(selectedCell) - 1}", DebugGUI.Label);
                break;
            case GardenPlot4x4.CellState.Ready:
                var readyCrop = current.GetCrop(selectedCell);
                string readyName = readyCrop != null ? readyCrop.cropName : "Crop";
                GUILayout.Label($"Seeds remaining in this cell (after this plant): {current.GetSeedCount(selectedCell) - 1}", DebugGUI.Label);
                if (GUILayout.Button($"Harvest {readyName}"))
                    current.RequestHarvest(gameObject, selectedCell);
                break;
        }
    }

    private void DrawPlantOptions()
    {
        var crops = current.RegisteredCrops;
        if (crops == null || crops.Length == 0)
        {
            GUILayout.Label("No crops registered.", DebugGUI.Label);
            return;
        }

        bool anyOffered = false;
        foreach (var crop in crops)
        {
            if (crop == null || crop.seedItem == null) continue;

            int have = SeedCount(crop.seedItem);
            if (have <= 0) continue;

            anyOffered = true;
            if (GUILayout.Button($"Plant {crop.cropName} ({have} seed{(have > 1 ? "s" : "")})"))
                current.RequestPlant(gameObject, selectedCell, crop.seedItem);
        }

        if (!anyOffered)
            GUILayout.Label("No seeds carried for any registered crop.", DebugGUI.Label);
    }

    private int SeedCount(ItemDefinition seed)
    {
        int main = playerInventory.GetCount(seed);
        var backpackInventory = backpackCarrier != null && backpackCarrier.Equipped != null
            ? backpackCarrier.Equipped.Inventory
            : null;
        return main + (backpackInventory?.GetCount(seed) ?? 0);
    }
}
