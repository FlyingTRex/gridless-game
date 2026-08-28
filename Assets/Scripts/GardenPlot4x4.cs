using Mirror;
using UnityEngine;

// FIXED (2026-08-28, MULTIPLAYER_INTERACTION_AUDIT.md): converted to
// NetworkBehaviour. Complete() only ever opened GardenPlotScreen4x4 (a
// local UI action, fine as-is) -- the REAL mutations (TryPlant/
// TryHarvest) were called directly from that screen's own button
// handlers, same "screen-driven mutation, no Command anywhere" shape
// Furnace/Campfire have. Each cell's crop/count/state is now broadcast
// via a syncedCells SyncList (crop identified by its seedItem's stable
// ItemDatabase id, same by-string-id pattern used everywhere else);
// growStartedAt/visualInstance/visualStageIndex stay local-only --
// server keeps exact timing, a real remote client approximates its own
// local growStartedAt from the moment it observed a cell start Growing
// (same cosmetic-only tradeoff GardenPlot.cs's own fix uses; the
// Ready/Growing/Empty state itself is always the real synced value).
//
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
[RequireComponent(typeof(SaveId))]
public class GardenPlot4x4 : NetworkBehaviour, IInteractable
{
    public const int GridSize = 4;
    public const int CellCount = GridSize * GridSize;

    public enum CellState { Empty, Growing, Ready }

    private struct Cell
    {
        public CropDefinition crop;
        public int count;
        public CellState state;
        public float growStartedAt;
        public GameObject visualInstance;
        public int visualStageIndex;
    }

    [SerializeField] private CropDefinition[] registeredCrops;

    // Index-matched 1:1 with the cells array below — cell i's growing
    // visual is parented under plantAnchors[i]. Sized/placed on the prefab,
    // not computed at runtime.
    [SerializeField] private Transform[] plantAnchors;

    // World-placed "already growing" cells (2026-08-16) — e.g. the 7
    // scattered-plot instances seeded with 7 already-Ready cells each,
    // giving a fresh game a real in-world seed source (see
    // BUGS_AND_ENHANCEMENTS.md's "Garden Plot seeds are Admin-Spawn-only"
    // entry). Applied once in Start(), guarded on SaveManager.SaveExists
    // so a loaded save always wins — matches the existing starting-gear
    // convention (PlayerShirt et al.'s "only equip if nothing's there yet"
    // guard), just applied to a runtime-only cells array instead of a
    // slot that can be inspected directly.
    [System.Serializable]
    public struct PreplantedCell
    {
        public int cellIndex;
        public CropDefinition crop;
        public int count;
    }
    [SerializeField] private PreplantedCell[] preplantedCells;

    [System.Serializable]
    public struct SyncedCell
    {
        public string cropSeedItemId; // empty = no crop planted
        public int count;
        public CellState state;
    }

    // Server-owned, broadcast to every observer -- see this file's own
    // header comment for why growStartedAt/visuals stay local-only.
    public readonly SyncList<SyncedCell> syncedCells = new SyncList<SyncedCell>();

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (syncedCells.Count == 0)
            for (int i = 0; i < CellCount; i++)
                syncedCells.Add(new SyncedCell { cropSeedItemId = "", count = 0, state = CellState.Empty });
    }

    private void Awake()
    {
        syncedCells.Callback += OnSyncedCellsChanged;
    }

    private void OnDestroy()
    {
        syncedCells.Callback -= OnSyncedCellsChanged;
    }

    // Client-side reconciliation -- pushes the server's confirmed crop/
    // count/state into this client's own local `cells[]`, and (only on a
    // real state change) resets/creates the cosmetic visual + starts this
    // client's own local approximate growth timer.
    private void OnSyncedCellsChanged(SyncList<SyncedCell>.Operation op, int index, SyncedCell oldItem, SyncedCell newItem)
    {
        if (isServer) return;
        if (index < 0 || index >= CellCount) return;

        var crop = FindCrop(ItemDatabase.Instance != null ? ItemDatabase.Instance.Find(newItem.cropSeedItemId) : null);
        cells[index].crop = crop;
        cells[index].count = newItem.count;

        bool stateChanged = cells[index].state != newItem.state;
        cells[index].state = newItem.state;
        if (!stateChanged) return;

        switch (newItem.state)
        {
            case CellState.Growing:
                cells[index].growStartedAt = Time.time;
                cells[index].visualStageIndex = -1;
                UpdateVisualStage(index, 0f);
                break;
            case CellState.Ready:
                UpdateVisualStage(index, 1f);
                break;
            case CellState.Empty:
                if (cells[index].visualInstance != null) Destroy(cells[index].visualInstance);
                cells[index].visualInstance = null;
                cells[index].visualStageIndex = -1;
                break;
        }
    }

    // Server-only -- writes the current cell's crop/count/state into
    // syncedCells, broadcasting it to every observer. Called right after
    // any server-side mutation of cells[index] below, rather than a
    // polled signature check (the mutation points are already small and
    // well-known here, unlike PlayerInventory's untracked direct-mutation
    // problem).
    private void PushSyncedCell(int index)
    {
        if (!isServer) return;
        string seedId = cells[index].crop != null && cells[index].crop.seedItem != null && ItemDatabase.Instance != null
            ? ItemDatabase.Instance.IdFor(cells[index].crop.seedItem)
            : null;
        syncedCells[index] = new SyncedCell { cropSeedItemId = seedId ?? "", count = cells[index].count, state = cells[index].state };
    }

    private void Start()
    {
        if (SaveManager.SaveExists || preplantedCells == null) return;

        foreach (var p in preplantedCells)
        {
            if (p.crop == null || p.crop.seedItem == null) continue;
            if (p.cellIndex < 0 || p.cellIndex >= CellCount) continue;
            RestoreCell(p.cellIndex, p.crop.seedItem, p.count, CellState.Ready, 0f);
        }
    }

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
            UpdateVisualStage(i, Mathf.Clamp01(elapsed / duration));

            if (isServer && elapsed >= duration)
            {
                cells[i].state = CellState.Ready;
                PushSyncedCell(i);
            }
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

    // ---- Save/load support (SaveManager.CaptureGardenPlot4x4/RestoreGardenPlot4x4) ----

    public ItemDefinition GetSeedItem(int index) => cells[index].crop != null ? cells[index].crop.seedItem : null;

    public float GetElapsedSeconds(int index) =>
        cells[index].state == CellState.Growing ? Time.time - cells[index].growStartedAt : 0f;

    // Restores a single cell directly into the given state, bypassing
    // TryPlant/TryHarvest entirely — no inventory interaction, nothing
    // consumed twice. elapsedSeconds is only meaningful for Growing
    // (reconstructs growStartedAt against the new session's Time.time,
    // which doesn't carry over from the save).
    public void RestoreCell(int index, ItemDefinition seed, int count, CellState state, float elapsedSeconds)
    {
        if (index < 0 || index >= CellCount) return;

        var crop = FindCrop(seed);
        cells[index].crop = crop;
        cells[index].count = count;
        cells[index].state = state;
        cells[index].visualStageIndex = -1;

        if (state != CellState.Empty && crop != null)
        {
            cells[index].growStartedAt = state == CellState.Ready
                ? Time.time - crop.growDurationSeconds
                : Time.time - elapsedSeconds;

            float progress = state == CellState.Ready ? 1f : Mathf.Clamp01(elapsedSeconds / crop.growDurationSeconds);
            UpdateVisualStage(index, progress);
        }

        PushSyncedCell(index);
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
    // FIXED (2026-08-28): dual-path dispatch, same shape as ChoppableTree/
    // ResourceNode -- called from GardenPlotScreen4x4's own button
    // handler, which used to call TryPlant/TryHarvest (below) directly.
    public void RequestPlant(GameObject player, int index, ItemDefinition seed)
    {
        if (TryGetComponent<NetworkIdentity>(out _) && NetworkClient.active)
        {
            string seedId = ItemDatabase.Instance != null ? ItemDatabase.Instance.IdFor(seed) : null;
            if (seedId == null) return;
            CmdPlant(index, seedId);
            return;
        }

        TryPlant(index, seed, player);
    }

    [Command(requiresAuthority = false)]
    private void CmdPlant(int index, string seedId, NetworkConnectionToClient sender = null)
    {
        if (sender == null || sender.identity == null) return;
        var seed = ItemDatabase.Instance != null ? ItemDatabase.Instance.Find(seedId) : null;
        if (seed == null) return;
        TryPlant(index, seed, sender.identity.gameObject);
    }

    public bool TryPlant(int index, ItemDefinition seed, GameObject player)
    {
        if (index < 0 || index >= CellCount) return false;
        if (cells[index].state != CellState.Empty) return false;

        var crop = FindCrop(seed);
        var playerInventory = player != null ? player.GetComponent<PlayerInventory>() : null;
        if (crop == null || playerInventory == null) return false;

        var backpackCarrier = player.GetComponent<PlayerBackpack>();
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
    public void RequestHarvest(GameObject player, int index)
    {
        if (TryGetComponent<NetworkIdentity>(out _) && NetworkClient.active)
        {
            CmdHarvest(index);
            return;
        }

        TryHarvest(index, player);
    }

    [Command(requiresAuthority = false)]
    private void CmdHarvest(int index, NetworkConnectionToClient sender = null)
    {
        if (sender == null || sender.identity == null) return;
        TryHarvest(index, sender.identity.gameObject);
    }

    public bool TryHarvest(int index, GameObject player)
    {
        if (index < 0 || index >= CellCount) return false;
        if (cells[index].state != CellState.Ready) return false;

        var crop = cells[index].crop;
        var playerInventory = player != null ? player.GetComponent<PlayerInventory>() : null;
        if (crop == null || crop.cropItem == null || playerInventory == null) return false;

        var backpackCarrier = player.GetComponent<PlayerBackpack>();
        var backpackInventory = backpackCarrier != null && backpackCarrier.Equipped != null
            ? backpackCarrier.Equipped.Inventory
            : null;

        int leftover = backpackInventory != null ? backpackInventory.AddItem(crop.cropItem, 1) : 1;
        if (leftover > 0) playerInventory.AddItem(crop.cropItem, leftover);

        // Seed-back chance (2026-08-16) — see CropDefinition.seedDropChance.
        if (crop.seedItem != null && Random.value < crop.seedDropChance)
        {
            int seedLeftover = backpackInventory != null ? backpackInventory.AddItem(crop.seedItem, 1) : 1;
            if (seedLeftover > 0) playerInventory.AddItem(crop.seedItem, seedLeftover);
        }

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
        cells[index].visualStageIndex = -1; // forces UpdateVisualStage to instantiate stage 0 below

        UpdateVisualStage(index, 0f);
        PushSyncedCell(index);
    }

    // Swaps the cell's visual to whichever growth-stage prefab the given
    // progress (0-1) falls into — real, differently-shaped geometry per
    // stage (e.g. Wild Harvest's own numbered growth-stage models), not a
    // single mesh scaled up. Only actually destroys/instantiates when the
    // target stage index changes, so this is cheap to call every frame.
    private void UpdateVisualStage(int index, float progress)
    {
        var crop = cells[index].crop;
        var stages = crop != null ? crop.growthStagePrefabs : null;
        int stageCount = stages != null ? stages.Length : 0;
        if (stageCount == 0) return;

        int targetStage = Mathf.Clamp(Mathf.FloorToInt(progress * stageCount), 0, stageCount - 1);
        if (targetStage == cells[index].visualStageIndex) return;

        var anchor = plantAnchors != null && index < plantAnchors.Length ? plantAnchors[index] : null;
        if (anchor == null) return;

        if (cells[index].visualInstance != null) Destroy(cells[index].visualInstance);

        var prefab = stages[targetStage];
        if (prefab != null)
        {
            var instance = Instantiate(prefab, anchor);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            cells[index].visualInstance = instance;
        }
        else
        {
            cells[index].visualInstance = null;
        }

        cells[index].visualStageIndex = targetStage;
    }

    private void ClearCell(int index)
    {
        cells[index].state = CellState.Empty;
        cells[index].crop = null;
        if (cells[index].visualInstance != null) Destroy(cells[index].visualInstance);
        cells[index].visualInstance = null;
        cells[index].visualStageIndex = -1;
        PushSyncedCell(index);
    }
}
