using UnityEngine;

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
public class GardenPlot : MonoBehaviour, IInteractable
{
    private enum PlotState { Empty, Growing, Ready }

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

    private PlotState state = PlotState.Empty;
    private int seedsRemaining;
    private float growStartedAt;
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

    private void Update()
    {
        if (state != PlotState.Growing) return;

        float elapsed = Time.time - growStartedAt;
        UpdatePlantScale(Mathf.Clamp01(elapsed / GrowDurationSeconds));

        if (elapsed >= GrowDurationSeconds)
            state = PlotState.Ready;
    }

    public void Complete(GameObject player)
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
        if (seedsRemaining > 0)
            StartGrowing();
        else
            ClearPlant();
    }

    private void StartGrowing()
    {
        state = PlotState.Growing;
        growStartedAt = Time.time;

        if (plantInstance == null && plantVisualPrefab != null && plantAnchor != null)
        {
            plantInstance = Instantiate(plantVisualPrefab, plantAnchor);
            plantInstance.transform.localPosition = Vector3.zero;
            plantInstance.transform.localRotation = Quaternion.identity;

            foreach (var collider in plantInstance.GetComponentsInChildren<Collider>())
                Destroy(collider);
            var bush = plantInstance.GetComponent<BerryBush>();
            if (bush != null) Destroy(bush);
        }

        UpdatePlantScale(0f);
    }

    private void UpdatePlantScale(float progress)
    {
        if (plantInstance == null) return;

        float scale = progress < Stage1Fraction ? Stage1Scale
            : progress < Stage2Fraction ? Stage2Scale
            : FullScale;
        plantInstance.transform.localScale = Vector3.one * scale;
    }

    private void ClearPlant()
    {
        state = PlotState.Empty;
        if (plantInstance != null) Destroy(plantInstance);
        plantInstance = null;
    }
}
