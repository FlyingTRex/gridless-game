using System.Collections.Generic;
using UnityEngine;

// Craftable/placeable structure (2026-08-12 rework, see
// CAMPFIRE_PLANNING.md — placed via the Build tab as a CampfirePiece
// BuildPiece, same zero-BuildSocket free-placement path StorageBox
// already uses). Two independent ways to light it: a Light button inside
// CampfireScreen (opened by E, IInteractable) or R (the original Spark
// wish, IWishTarget, still a direct one-step cast) — Spark is an
// alternate, not the only way, per Ben's call. Both interfaces declare an
// identical `string Prompt` signature, so one property satisfies both —
// safe to share since PlayerInteraction deliberately shows no UI at all
// for wishes (only IInteractable's copy of Prompt actually renders
// anywhere).
//
// UI redesign (2026-08-13): E used to attempt lighting directly; it now
// always opens CampfireScreen, a focused popup (same family as
// LockboxScreen) showing fuel/cooking slots and a Light button — replaces
// the old "Campfire (nearby)" section that used to sit at the bottom of
// the Inventory tab (removed from InventoryScreen.cs as part of this
// change, now fully superseded).
//
// Fuel (2026-08-12, Chunk 2): reuses FuelTier/FuelItem exactly as built
// for the Furnace — 1 fuel slot, real burn timer ticking in real time
// while lit, independent of anything else happening. Lighting (either E
// or Spark) now requires fuel present and consumes 1 unit; running out
// extinguishes it automatically. `fuelItems` is wired once on the prefab
// (every placed instance is a clone of the same prefab, so one registry
// covers all of them — same reasoning PlayerEating.edibles uses, just
// per-prefab instead of per-player since fuel lives on the structure, not
// the player).
public class Campfire : MonoBehaviour, IInteractable, IWishTarget
{
    [SerializeField] private WishRecipe sparkWish;
    [SerializeField] private Material unlitMaterial;
    [SerializeField] private Material litMaterial;
    // Only the Wood renderer swaps material on light/extinguish — the
    // Rocks renderer stays on its own static material always (2026-08-13
    // Blender rebuild decision: rocks don't visually change, only the
    // wood embers do).
    [SerializeField] private Renderer woodRenderer;
    [SerializeField] private Light fireLight;
    [SerializeField] private FuelItem[] fuelItems;

    // Cooking (2026-08-12, Chunk 3): 1 slot, auto-cooks over time while
    // lit and the player stands within cookRange — same "runs on its own
    // once conditions are met" mental model as the fuel timer above.
    // Progress pauses (not resets) if the fire goes out or the player
    // steps away, and only resets if the slot's contents actually change
    // (the raw item gets pulled back out) — cookableItems wired once on
    // the prefab, same reasoning as fuelItems.
    [SerializeField] private CookableItem[] cookableItems;
    [SerializeField] private float cookRange = 3f;

    // Warmth (2026-08-12, Chunk 4) — Body Temperature's first real
    // gameplay use. warmthTarget (80) sits comfortably above
    // PlayerVitals' neutral (50) so standing near a lit fire visibly and
    // steadily warms the player rather than just barely resisting drift.
    [SerializeField] private float warmthRange = 4f;
    [SerializeField] private float warmthTarget = 80f;
    [SerializeField] private float warmthRatePerSecond = 5f;

    private Inventory fuelInventory;
    private Inventory cookingInventory;
    private Transform player;
    private PlayerVitals playerVitals;
    private bool isLit;
    private float fuelSecondsRemaining;
    private float cookSecondsElapsed;

    public string DisplayName => "Campfire";
    public Inventory FuelInventory => fuelInventory;
    public Inventory CookingInventory => cookingInventory;
    public FuelItem[] FuelItems => fuelItems;
    public CookableItem[] CookableItems => cookableItems;
    public bool IsLit => isLit;
    public float FuelSecondsRemaining => fuelSecondsRemaining;

    // 2026-08-13: E now always opens CampfireScreen instead of attempting
    // to light directly — lighting moved to a button inside that popup
    // (see CAMPFIRE_PLANNING.md's UI redesign). Simple constant prompt,
    // matching Lockbox's "Open {DisplayName}" convention — lit/fuel status
    // is shown inside the popup itself now, not in the world prompt.
    public string Prompt => $"Open {DisplayName}";

    public bool IsInstant => true;
    public float GetHoldDuration(GameObject player) => 0f;

    public bool HasFuel => fuelInventory != null && fuelInventory.Slots.Count > 0;

    public void Complete(GameObject player) => player.GetComponent<CampfireScreen>()?.Open(this);

    // Public wrapper so CampfireScreen's Light button can trigger the same
    // path E used to trigger directly. Spark (OnWishComplete below) still
    // calls the private TryLight() internally, unchanged.
    public bool TryLightFromScreen() => TryLight();

    // Null (no wish available) once already lit, out of fuel, or if the
    // looking player doesn't know Spark's lineage — PlayerInteraction
    // treats a null return as "R does nothing here."
    public WishRecipe GetWish(PlayerMagic magic) =>
        !isLit && HasFuel && magic != null && magic.IsLineageKnown(sparkWish != null ? sparkWish.lineage : null)
            ? sparkWish
            : null;

    private void Awake()
    {
        var allowedFuel = new ItemDefinition[fuelItems != null ? fuelItems.Length : 0];
        for (int i = 0; i < allowedFuel.Length; i++)
            allowedFuel[i] = fuelItems[i] != null ? fuelItems[i].item : null;
        fuelInventory = new Inventory(1, allowedFuel);

        // The cooking slot accepts both the raw and cooked form of every
        // registered recipe — it holds whichever state is currently in
        // progress, and the player retrieves the finished item from the
        // same slot they loaded the raw one into (same nearby-Campfire UI
        // as fuel, no auto-delivery into the player's own inventory).
        var allowedCooking = new List<ItemDefinition>();
        if (cookableItems != null)
        {
            foreach (var recipe in cookableItems)
            {
                if (recipe == null) continue;
                if (recipe.rawItem != null) allowedCooking.Add(recipe.rawItem);
                if (recipe.cookedItem != null) allowedCooking.Add(recipe.cookedItem);
            }
        }
        cookingInventory = new Inventory(1, allowedCooking.ToArray());

        SetLit(false);
    }

    private void Start()
    {
        playerVitals = FindFirstObjectByType<PlayerVitals>();
        player = playerVitals != null ? playerVitals.transform : null;
    }

    private void Update()
    {
        if (isLit)
        {
            fuelSecondsRemaining -= Time.deltaTime;
            if (fuelSecondsRemaining <= 0f)
                SetLit(false);
        }

        TickCooking();
        TickWarmth();
    }

    private void TickWarmth()
    {
        if (!isLit || playerVitals == null) return;

        float distSq = (player.position - transform.position).sqrMagnitude;
        if (distSq <= warmthRange * warmthRange)
            playerVitals.WarmNear(warmthTarget, warmthRatePerSecond);
    }

    private void TickCooking()
    {
        if (cookingInventory.Slots.Count == 0)
        {
            cookSecondsElapsed = 0f;
            return;
        }

        var slot = cookingInventory.Slots[0];
        var recipe = FindCookable(slot.item);
        if (recipe == null)
        {
            // Either empty, or the slot already holds the cooked result —
            // nothing left to do either way.
            cookSecondsElapsed = 0f;
            return;
        }

        // No accessory slots exist yet (2026-08-12 — deliberately not
        // built until real accessory items do; see CAMPFIRE_PLANNING.md),
        // so any recipe that requires one can never actually complete —
        // only the null-requiredAccessory (open-flame) recipes work today.
        bool accessorySatisfied = recipe.requiredAccessory == null;
        bool playerNearby = player != null
            && (player.position - transform.position).sqrMagnitude <= cookRange * cookRange;

        // Paused, not reset, when conditions aren't met right now — the
        // player can walk away and come back, or let the fire go out and
        // relight it, without losing progress already made.
        if (!isLit || !playerNearby || !accessorySatisfied) return;

        cookSecondsElapsed += Time.deltaTime;
        if (cookSecondsElapsed < recipe.cookDurationSeconds) return;

        cookingInventory.RemoveItem(recipe.rawItem, 1);
        cookingInventory.AddItem(recipe.cookedItem, 1);
        cookSecondsElapsed = 0f;
    }

    private CookableItem FindCookable(ItemDefinition item)
    {
        if (item == null || cookableItems == null) return null;
        foreach (var recipe in cookableItems)
            if (recipe != null && recipe.rawItem == item) return recipe;
        return null;
    }

    public void OnWishComplete(GameObject player, bool succeeded)
    {
        if (succeeded) TryLight();
    }

    // Shared by both lighting paths (E and Spark) — consumes 1 fuel unit
    // from whatever's actually in the slot and starts the burn timer at
    // that item's own FuelTier duration. False (no state change) if
    // already lit or the fuel slot is empty.
    private bool TryLight()
    {
        if (isLit || !HasFuel) return false;

        var slot = fuelInventory.Slots[0];
        var fuel = FindFuel(slot.item);
        if (fuel == null) return false;

        fuelInventory.RemoveItem(slot.item, 1);
        fuelSecondsRemaining = FuelTierScale.BurnMinutes(fuel.fuelTier) * 60f;
        SetLit(true);
        return true;
    }

    private FuelItem FindFuel(ItemDefinition item)
    {
        if (item == null || fuelItems == null) return null;
        foreach (var fuel in fuelItems)
            if (fuel != null && fuel.item == item) return fuel;
        return null;
    }

    private void SetLit(bool lit)
    {
        isLit = lit;

        var mat = lit ? litMaterial : unlitMaterial;
        if (mat != null && woodRenderer != null)
            woodRenderer.sharedMaterial = mat;

        if (fireLight != null)
            fireLight.enabled = lit;
    }
}
