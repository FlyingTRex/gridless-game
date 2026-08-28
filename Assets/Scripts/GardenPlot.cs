using Mirror;
using UnityEngine;

// FIXED (2026-08-28, MULTIPLAYER_INTERACTION_AUDIT.md): converted to
// NetworkBehaviour, same Class A shape as ChoppableTree/ResourceNode --
// Complete() ran entirely client-local, so a real remote client's plant/
// harvest never reached the server at all. `state`/`seedsRemaining` are
// now [SyncVar]s. Growth *timing* stays a real precision-vs-complexity
// tradeoff: the server (and so a host) keeps exact progress using its own
// growStartedAt; a genuine remote client gets a good-enough LOCAL
// approximation (its own Time.time from the moment it observed Growing
// begin) for the purely cosmetic 3-stage scale visual -- the only
// gameplay-relevant moment (Ready, i.e. harvestable) is always the real
// synced `state`, never the approximation.
//
// Single-plant "proof of concept" Garden Plot (COOKING_AND_GARDENING_PLANNING.md,
// single-plant pass 2026-08-14) — proves out the eventual 4x4 GardenPlot's
// core per-cell mechanic (a seed stack goes in, harvesting one plant
// auto-replants the next from the same stack until it's exhausted, staged
// growth visual) standalone, on one cell, before scaling up. Deliberately
// simplified interaction for this pass — a direct E-key plant/harvest, not
// the drag-and-drop popup the full 16-cell design calls for; a real
// Inventory-slot-per-cell only makes sense once there's an actual grid UI
// to drag into.
//
// Reuses the existing BerryBush model directly for the growing-plant
// visual (Ben's "we have a berry bush" idea) instead of new modeling
// work — its own BerryBush behavior/colliders are stripped at runtime
// since this is a purely decorative reuse, not a second independently-
// searchable bush living inside the plot.
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(SaveId))]
public class GardenPlot : NetworkBehaviour, IInteractable
{
    // Public so SaveManager can capture/restore it directly (SaveId +
    // CaptureWorldObjects<T> pattern — see SaveManager.cs's
    // CaptureGardenPlot/RestoreGardenPlot).
    public enum PlotState { Empty, Growing, Ready }

    // 5 real minutes — matches the Carrot number floated in
    // COOKING_AND_GARDENING_PLANNING.md's grow-duration discussion.
    private const float GrowDurationSeconds = 300f;

    // 3 discrete growth stages (Ben's call, 2026-08-14): the plant jumps
    // to a bigger fixed scale at each threshold rather than smoothly
    // interpolating every frame.
    private const float Stage1Fraction = 1f / 3f;
    private const float Stage2Fraction = 2f / 3f;
    private const float Stage1Scale = 0.35f;
    private const float Stage2Scale = 0.65f;
    private const float FullScale = 1f;

    [SerializeField] private ItemDefinition berrySeedItem;
    [SerializeField] private ItemDefinition berryItem;
    [SerializeField] private int harvestYield = 3;
    [SerializeField] private Transform plantAnchor;
    [SerializeField] private GameObject plantVisualPrefab;

    [SyncVar(hook = nameof(OnStateChanged))]
    private PlotState state = PlotState.Empty;
    [SyncVar]
    private int seedsRemaining;
    // Server-only -- Time.time is per-process. The server's own Update()
    // reads this directly for exact progress; a remote client instead
    // approximates from localGrowStartedAt (set the moment it observed
    // Growing begin via the hook below).
    private float growStartedAt;
    private float localGrowStartedAt;
    private GameObject plantInstance;

    public string Prompt => state switch
    {
        PlotState.Empty => "Plant Berry Seed",
        PlotState.Growing => "Growing...",
        PlotState.Ready => "Harvest",
        _ => null,
    };

    public bool IsInstant => true;
    public float GetHoldDuration(GameObject player) => 0f;

    // ---- Save/load support (SaveManager.CaptureGardenPlot/RestoreGardenPlot) ----

    public PlotState State => state;
    public int SeedsRemaining => seedsRemaining;
    public float GetElapsedSeconds() => state == PlotState.Growing ? Time.time - growStartedAt : 0f;

    // Restores directly into the given state, bypassing TryPlant/Harvest
    // entirely — no inventory interaction, nothing consumed twice.
    // elapsedSeconds is only meaningful for Growing (reconstructs
    // growStartedAt against the new session's Time.time, which doesn't
    // carry over from the save). Always runs server-side (SaveManager) --
    // the state assignment below triggers OnStateChanged the normal way,
    // syncing to every observer.
    public void RestoreState(int seeds, PlotState newState, float elapsedSeconds)
    {
        seedsRemaining = seeds;

        growStartedAt = newState == PlotState.Ready
            ? Time.time - GrowDurationSeconds
            : Time.time - elapsedSeconds;

        state = newState;
    }

    // Fires on every observer (including the server itself) the moment
    // `state` changes, locally or via sync -- the single place that
    // creates/destroys the cosmetic plant visual and (for a real remote
    // client) starts its own local approximate growth timer.
    private void OnStateChanged(PlotState oldState, PlotState newState)
    {
        switch (newState)
        {
            case PlotState.Empty:
                if (plantInstance != null) Destroy(plantInstance);
                plantInstance = null;
                break;
            case PlotState.Growing:
                localGrowStartedAt = Time.time;
                EnsurePlantInstance();
                UpdatePlantScale(0f);
                break;
            case PlotState.Ready:
                EnsurePlantInstance();
                UpdatePlantScale(1f);
                break;
        }
    }

    private void EnsurePlantInstance()
    {
        if (plantInstance != null || plantVisualPrefab == null || plantAnchor == null) return;

        plantInstance = Instantiate(plantVisualPrefab, plantAnchor);
        plantInstance.transform.localPosition = Vector3.zero;
        plantInstance.transform.localRotation = Quaternion.identity;

        // Purely a decorative reuse of BerryBush's model (see this file's
        // own header) -- never independently network-relevant, so no
        // NetworkSpawnHelper call needed; strip its behavior/colliders
        // locally on every machine identically.
        foreach (var collider in plantInstance.GetComponentsInChildren<Collider>())
            Destroy(collider);
        var bush = plantInstance.GetComponent<BerryBush>();
        if (bush != null) Destroy(bush);
    }

    private void Update()
    {
        if (isServer)
        {
            if (state == PlotState.Growing)
            {
                float elapsed = Time.time - growStartedAt;
                UpdatePlantScale(Mathf.Clamp01(elapsed / GrowDurationSeconds));

                if (elapsed >= GrowDurationSeconds)
                    state = PlotState.Ready;
            }
            return;
        }

        // Real remote client only -- cosmetic-only approximation, see
        // this file's own header comment for why this is an acceptable
        // tradeoff (the actual Ready transition is always the real
        // synced `state`, never this local timer).
        if (state == PlotState.Growing)
            UpdatePlantScale(Mathf.Clamp01((Time.time - localGrowStartedAt) / GrowDurationSeconds));
    }

    // FIXED (2026-08-28): same dual-path dispatch ChoppableTree/
    // ResourceNode already established.
    public void Complete(GameObject player)
    {
        if (TryGetComponent<NetworkIdentity>(out _) && NetworkClient.active)
        {
            CmdComplete();
            return;
        }

        ServerComplete(player);
    }

    [Command(requiresAuthority = false)]
    private void CmdComplete(NetworkConnectionToClient sender = null)
    {
        if (sender == null || sender.identity == null) return;
        ServerComplete(sender.identity.gameObject);
    }

    public void ServerComplete(GameObject player)
    {
        var playerInventory = player.GetComponent<PlayerInventory>();
        if (playerInventory == null) return;

        var backpackCarrier = player.GetComponent<PlayerBackpack>();

        switch (state)
        {
            case PlotState.Empty:
                TryPlant(playerInventory, backpackCarrier);
                break;
            case PlotState.Ready:
                Harvest(playerInventory, backpackCarrier);
                break;
            // Growing: nothing to do yet, E is a no-op.
        }
    }

    // Plants the player's ENTIRE current Berry Seed stack at once — the
    // mechanic being proven out, not a one-seed-at-a-time interaction.
    // Checks the main inventory AND a worn Backpack's own nested
    // inventory — Berry Seed is a stackable item that routes into an
    // equipped Backpack first on pickup (PlayerLoot), so a naive
    // main-inventory-only check would silently see 0 seeds despite the
    // player visibly carrying some.
    private void TryPlant(PlayerInventory playerInventory, PlayerBackpack backpackCarrier)
    {
        var backpackInventory = backpackCarrier?.Equipped?.Inventory;

        int mainCount = playerInventory.GetCount(berrySeedItem);
        int backpackCount = backpackInventory?.GetCount(berrySeedItem) ?? 0;
        int total = mainCount + backpackCount;
        if (total <= 0) return;

        if (mainCount > 0) playerInventory.RemoveItem(berrySeedItem, mainCount);
        if (backpackCount > 0) backpackInventory.RemoveItem(berrySeedItem, backpackCount);

        seedsRemaining = total;
        StartGrowing();
    }

    // Deposits into a worn Backpack first if there's room (same priority
    // PlayerLoot already uses for picked-up items), falling back to the
    // main inventory for whatever didn't fit.
    private void Harvest(PlayerInventory playerInventory, PlayerBackpack backpackCarrier)
    {
        var backpackInventory = backpackCarrier?.Equipped?.Inventory;
        int leftover = backpackInventory != null
            ? backpackInventory.AddItem(berryItem, harvestYield)
            : harvestYield;
        if (leftover > 0)
            playerInventory.AddItem(berryItem, leftover);

        seedsRemaining--;
        state = seedsRemaining > 0 ? PlotState.Growing : PlotState.Empty;
        if (state == PlotState.Growing) growStartedAt = Time.time;
    }

    private void StartGrowing()
    {
        growStartedAt = Time.time;
        state = PlotState.Growing;
    }

    private void UpdatePlantScale(float progress)
    {
        if (plantInstance == null) return;

        float scale = progress < Stage1Fraction ? Stage1Scale
            : progress < Stage2Fraction ? Stage2Scale
            : FullScale;
        plantInstance.transform.localScale = Vector3.one * scale;
    }
}
