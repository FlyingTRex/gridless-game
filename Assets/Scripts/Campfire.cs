using System.Collections.Generic;
using Newtonsoft.Json.Linq;
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
//
// Cooking rework (2026-08-13): was a single auto-cooking slot; now a real
// recipe system mirroring CraftingRecipe's shape — 4 accessory slots
// (Grill/Cooking Pot/Kettle/Frying Pan, each capacity-1 restricted to its
// own item), a 4-slot ingredient input pool, and a 4-slot output bank.
// Cooking is a manual action now (Ben's call): the player loads
// utensils/ingredients, CampfireScreen shows only the CookableItem
// recipes currently satisfiable (accessory present + all ingredients
// present), and clicking one commits — ingredients consumed immediately
// (same "consumed upfront" convention as PlayerCrafting), one recipe
// cooking at a time, real-time timer pausing (not resetting) while unlit
// or the player's away.
//
// Cooking skill/quality-tier system (2026-08-15,
// COOKING_SKILL_PLANNING.md) — completion is no longer deterministic for
// any CookableItem with a trainedSkill set: ResolveCookingOutcome rolls
// the same shared CraftOutcomeRoll crafting uses, collapsed to binary
// (Ben's call — no crafting-style tier-swap items for food) plus a mild
// Health hit on the worst outcome. Recipes with no trainedSkill (the
// original RawMeatToCookedMeatCookable) are untouched — always succeed,
// same as before this system existed.
public class Campfire : MonoBehaviour, IInteractable, IWishTarget
{
    // Mild — half PlayerCrafting.SpectacularFailureDamage (10), per Ben's
    // "mild Health hit" framing for a cooking disaster vs. crafting's full
    // one (2026-08-15, COOKING_SKILL_PLANNING.md).
    private const float CookingFailureDamage = 5f;
    private const float MessageDuration = 4f;

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

    [SerializeField] private CookableItem[] cookableItems;
    [SerializeField] private float cookRange = 3f;

    // Accessory items (2026-08-13) — each gates a capacity-1 Inventory
    // restricted to exactly that item, wired once on the prefab like
    // fuelItems/cookableItems.
    [SerializeField] private ItemDefinition grillItem;
    [SerializeField] private ItemDefinition cookingPotItem;
    [SerializeField] private ItemDefinition kettleItem;
    [SerializeField] private ItemDefinition fryingPanItem;

    // Warmth (2026-08-12, Chunk 4) — Body Temperature's first real
    // gameplay use. warmthTarget (80) sits comfortably above
    // PlayerVitals' neutral (50) so standing near a lit fire visibly and
    // steadily warms the player rather than just barely resisting drift.
    [SerializeField] private float warmthRange = 4f;
    [SerializeField] private float warmthTarget = 80f;
    [SerializeField] private float warmthRatePerSecond = 5f;

    private Inventory fuelInventory;
    private Inventory grillSlot;
    private Inventory cookingPotSlot;
    private Inventory kettleSlot;
    private Inventory fryingPanSlot;
    private Inventory inputInventory;
    private Inventory outputInventory;
    private Transform player;
    private PlayerVitals playerVitals;
    private PlayerSkills playerSkills;
    private bool isLit;
    private float fuelSecondsRemaining;
    private CookableItem activeRecipe;
    private float cookSecondsElapsed;
    private string lastCookMessage;
    private float lastCookMessageExpireTime;
    // Auto-Run (2026-08-18) -- opt-in continuous operation, same shape as
    // Furnace.autoRunEnabled: auto-relights from whatever fuel remains in
    // the slot once the current unit burns out, and auto-repeats the last
    // recipe cooked as long as its ingredients/accessory/skill are still
    // satisfiable. Off by default, same as Furnace -- found live by Ben
    // (2026-08-18): cooking a stack of Egg took 15 separate clicks, and the
    // fire went out after exactly one Stick even with more stacked in the
    // slot, since neither half of "keep going" ever existed for Campfire
    // the way it already does for Furnace.
    private bool autoRunEnabled;

    public string DisplayName => "Campfire";
    public Inventory FuelInventory => fuelInventory;
    public Inventory GrillSlot => grillSlot;
    public Inventory CookingPotSlot => cookingPotSlot;
    public Inventory KettleSlot => kettleSlot;
    public Inventory FryingPanSlot => fryingPanSlot;
    public Inventory InputInventory => inputInventory;
    public Inventory OutputInventory => outputInventory;
    public FuelItem[] FuelItems => fuelItems;
    public bool IsLit => isLit;
    public float FuelSecondsRemaining => fuelSecondsRemaining;
    public CookableItem ActiveRecipe => activeRecipe;
    public float CookSecondsElapsed => cookSecondsElapsed;
    public bool AutoRunEnabled => autoRunEnabled;
    public void SetAutoRun(bool value) => autoRunEnabled = value;

    // Read by CampfireScreen to show a brief result toast under the Recipe
    // section once cooking completes — Campfire has no OnGUI of its own
    // (unlike PlayerCrafting, which draws its own), so this is exposed as
    // a property instead (2026-08-15, COOKING_SKILL_PLANNING.md).
    public string LastCookMessage => Time.time < lastCookMessageExpireTime ? lastCookMessage : null;

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

        grillSlot = new Inventory(1, grillItem != null ? new[] { grillItem } : null);
        cookingPotSlot = new Inventory(1, cookingPotItem != null ? new[] { cookingPotItem } : null);
        kettleSlot = new Inventory(1, kettleItem != null ? new[] { kettleItem } : null);
        fryingPanSlot = new Inventory(1, fryingPanItem != null ? new[] { fryingPanItem } : null);

        var allowedIngredients = new List<ItemDefinition>();
        var allowedOutputs = new List<ItemDefinition>();
        if (cookableItems != null)
        {
            foreach (var recipe in cookableItems)
            {
                if (recipe == null) continue;
                if (recipe.ingredients != null)
                    foreach (var ingredient in recipe.ingredients)
                        if (ingredient != null && ingredient.item != null) allowedIngredients.Add(ingredient.item);
                if (recipe.outputItem != null) allowedOutputs.Add(recipe.outputItem);
            }
        }
        inputInventory = new Inventory(4, allowedIngredients.ToArray());
        outputInventory = new Inventory(4, allowedOutputs.ToArray());

        SetLit(false);
    }

    private void Start()
    {
        playerVitals = FindFirstObjectByType<PlayerVitals>();
        player = playerVitals != null ? playerVitals.transform : null;
        playerSkills = player != null ? player.GetComponent<PlayerSkills>() : null;
    }

    private void Update()
    {
        if (isLit)
        {
            fuelSecondsRemaining -= Time.deltaTime;
            if (fuelSecondsRemaining <= 0f)
                SetLit(false);
        }
        else if (autoRunEnabled && HasFuel)
        {
            TryLight();
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

    // Real-time timer for whichever recipe StartCooking() committed to —
    // paused (not reset) while unlit or the player's out of range, same
    // "runs on its own, pauses on interruption" mental model as before.
    // Ingredients were already consumed at StartCooking time; whether the
    // dish actually lands now depends on ResolveCookingOutcome's roll
    // (2026-08-15, COOKING_SKILL_PLANNING.md) rather than always
    // succeeding.
    private void TickCooking()
    {
        if (activeRecipe == null) return;

        bool playerNearby = player != null
            && (player.position - transform.position).sqrMagnitude <= cookRange * cookRange;
        if (!isLit || !playerNearby) return;

        cookSecondsElapsed += Time.deltaTime;
        if (cookSecondsElapsed < activeRecipe.cookDurationSeconds) return;

        var finishedRecipe = activeRecipe;
        ResolveCookingOutcome(finishedRecipe);
        activeRecipe = null;
        cookSecondsElapsed = 0f;

        // Auto-repeat (2026-08-18) -- StartCooking() already does every
        // satisfiability check (accessory/ingredients/water/skill/output
        // space) and simply refuses if any of them fail now, so this is
        // safe to just try unconditionally rather than duplicating those
        // checks here.
        if (autoRunEnabled)
            StartCooking(finishedRecipe);
    }

    // Chance-of-creation roll for cooking, mirroring
    // PlayerCrafting.ResolveOutcome but collapsed to binary (Ben's call,
    // 2026-08-15 — no crafting-style tier-swap items): recipes with no
    // trainedSkill skip the roll entirely and always succeed, same as
    // PlayerCrafting's skill-less gadget recipes. Otherwise rolls between
    // five outcomes via the shared CraftOutcomeRoll based on how far
    // Cooking is above this recipe's requiredSkillLevel — Brilliant/
    // Success both just give the dish (no tier-swap to give instead),
    // Barely/BadFailure waste the (already-consumed) ingredients, and
    // SpectacularFailure also deals a mild Health hit.
    private void ResolveCookingOutcome(CookableItem recipe)
    {
        if (recipe.trainedSkill == null)
        {
            outputInventory.AddItem(recipe.outputItem, recipe.outputCount);
            return;
        }

        float margin = playerSkills != null
            ? playerSkills.GetLevel(recipe.trainedSkill) - recipe.requiredSkillLevel
            : 0f;
        var outcome = CraftOutcomeRoll.Roll(Mathf.Max(0f, margin));

        switch (outcome)
        {
            case CraftOutcome.BrilliantSuccess:
                outputInventory.AddItem(recipe.outputItem, recipe.outputCount);
                playerSkills?.GainExperience(recipe.trainedSkill, recipe.skillGain);
                ShowCookMessage($"{recipe.outputItem.itemName} turned out perfectly!");
                break;

            case CraftOutcome.Success:
                outputInventory.AddItem(recipe.outputItem, recipe.outputCount);
                playerSkills?.GainExperience(recipe.trainedSkill, recipe.skillGain);
                break;

            case CraftOutcome.BarelyFail:
            case CraftOutcome.BadFailure:
                ShowCookMessage("It didn't turn out — the ingredients were wasted.");
                break;

            case CraftOutcome.SpectacularFailure:
                playerVitals?.Damage(CookingFailureDamage);
                ShowCookMessage("Disaster! Burnt beyond saving, and it made you feel sick.");
                break;
        }
    }

    private void ShowCookMessage(string text)
    {
        lastCookMessage = text;
        lastCookMessageExpireTime = Time.time + MessageDuration;
    }

    // True if the recipe has no trainedSkill set, or Cooking is currently
    // at or above requiredSkillLevel. Mirrors PlayerCrafting.HasRequiredSkill
    // (2026-08-15, COOKING_SKILL_PLANNING.md).
    private bool HasRequiredCookingSkill(CookableItem recipe)
    {
        if (recipe.trainedSkill == null) return true;
        return playerSkills != null && playerSkills.GetLevel(recipe.trainedSkill) >= recipe.requiredSkillLevel;
    }

    // Every registered recipe whose accessory (if any) is currently seated
    // in one of the 4 utensil slots and whose full ingredient list is
    // currently present in the input pool — what CampfireScreen's Recipe
    // list actually offers the player.
    public List<CookableItem> GetAvailableRecipes()
    {
        var result = new List<CookableItem>();
        if (cookableItems == null) return result;

        foreach (var recipe in cookableItems)
        {
            if (recipe == null) continue;
            if (!AccessoryPresent(recipe.requiredAccessory)) continue;
            if (!HasAllIngredients(recipe)) continue;
            if (!HasCanteenWater(recipe)) continue;
            if (!HasRequiredCookingSkill(recipe)) continue;
            result.Add(recipe);
        }
        return result;
    }

    private bool AccessoryPresent(ItemDefinition accessory)
    {
        if (accessory == null) return true;
        return grillSlot.GetCount(accessory) > 0 || cookingPotSlot.GetCount(accessory) > 0
            || kettleSlot.GetCount(accessory) > 0 || fryingPanSlot.GetCount(accessory) > 0;
    }

    private bool HasAllIngredients(CookableItem recipe)
    {
        if (recipe.ingredients == null) return true;
        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient == null || ingredient.item == null) continue;
            if (inputInventory.GetCount(ingredient.item) < ingredient.count) return false;
        }
        return true;
    }

    // True if the recipe has no requiresCanteenWater flag set, or the
    // nearby player has an equipped Canteen currently holding at least
    // canteenWaterAmount of Water. Mirrors PlayerCrafting.HasCanteenWater
    // — hands only, same scope as that method (2026-08-15, Herbal Tea).
    private bool HasCanteenWater(CookableItem recipe)
    {
        if (recipe == null || !recipe.requiresCanteenWater) return true;

        var canteen = FindPlayerCanteen();
        return canteen != null && canteen.Liquid == LiquidType.Water && canteen.Amount >= recipe.canteenWaterAmount;
    }

    // Checks both hands and a Belt-clipped Canteen (2026-08-18 -- same
    // gap as PlayerCrafting.FindEquippedCanteen, fixed the same way; see
    // that method's own comment for why).
    private Canteen FindPlayerCanteen()
    {
        var equipment = player != null ? player.GetComponent<PlayerEquipment>() : null;
        if (equipment == null) return null;

        foreach (var handSlotName in PlayerEquipSlots.Hands)
            if (equipment.GetEquipped(handSlotName) is Canteen canteen)
                return canteen;

        var wornBelt = player != null ? player.GetComponent<PlayerBelt>()?.Equipped : null;
        if (wornBelt != null)
        {
            foreach (var slot in wornBelt.Inventory.Slots)
                if (slot.equipment is Canteen clippedCanteen)
                    return clippedCanteen;
        }

        return null;
    }

    // Commits to cooking recipe: consumes its ingredients from the input
    // pool immediately (same upfront-consume convention as
    // PlayerCrafting), starts the real-time timer. Only one recipe cooks
    // at a time — refuses if something's already cooking, the recipe
    // isn't currently satisfiable, or the output bank has no room for the
    // result.
    public bool StartCooking(CookableItem recipe)
    {
        if (recipe == null || activeRecipe != null) return false;
        if (!AccessoryPresent(recipe.requiredAccessory) || !HasAllIngredients(recipe)) return false;
        if (!HasCanteenWater(recipe)) return false;
        if (!HasRequiredCookingSkill(recipe)) return false;
        if (!outputInventory.HasSpaceFor(recipe.outputItem, recipe.outputCount)) return false;

        if (recipe.ingredients != null)
            foreach (var ingredient in recipe.ingredients)
                if (ingredient != null && ingredient.item != null)
                    inputInventory.RemoveItem(ingredient.item, ingredient.count);

        // Consumed upfront, same as ingredients above — deliberately NOT
        // refunded if cooking is somehow interrupted (there's no cancel
        // path for cooking today, unlike PlayerCrafting.CancelCraft, so
        // this matches that method's own "water isn't refunded" choice).
        if (recipe.requiresCanteenWater)
            FindPlayerCanteen()?.ConsumeWater(recipe.canteenWaterAmount);

        activeRecipe = recipe;
        cookSecondsElapsed = 0f;
        return true;
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

    // Read by SaveManager.CaptureCampfire — outputItem's stable ItemDatabase
    // ID doubles as the recipe's own ID, since a given output is only ever
    // registered once on a given Campfire (avoids needing a dedicated
    // CookableItem database just for this one field).
    public string ActiveRecipeId =>
        activeRecipe != null && ItemDatabase.Instance != null ? ItemDatabase.Instance.IdFor(activeRecipe.outputItem) : null;

    // Restore path for SAVE_LOAD_PLANNING.md section 11's Campfire follow-up
    // (2026-08-17) -- called once by SaveManager right after this instance
    // is re-created (or found already in the scene) on load. SetLit handles
    // the material/light swap so a restored-lit Campfire actually looks lit,
    // not just internally flagged. activeRecipeId is resolved against this
    // instance's own cookableItems by matching outputItem, not a global
    // database (see ActiveRecipeId above).
    public void RestoreState(bool lit, float fuelRemaining, string activeRecipeId, float cookElapsed,
        JArray fuelData, JArray grillData, JArray cookingPotData, JArray kettleData, JArray fryingPanData,
        JArray inputData, JArray outputData, bool autoRun)
    {
        SetLit(lit);
        fuelSecondsRemaining = fuelRemaining;
        cookSecondsElapsed = cookElapsed;
        autoRunEnabled = autoRun;

        activeRecipe = null;
        if (!string.IsNullOrEmpty(activeRecipeId) && cookableItems != null)
        {
            var targetItem = ItemDatabase.Instance != null ? ItemDatabase.Instance.Find(activeRecipeId) : null;
            foreach (var recipe in cookableItems)
                if (recipe != null && recipe.outputItem == targetItem) { activeRecipe = recipe; break; }
        }

        if (fuelData != null) InventorySaveUtility.Restore(fuelInventory, fuelData);
        if (grillData != null) InventorySaveUtility.Restore(grillSlot, grillData);
        if (cookingPotData != null) InventorySaveUtility.Restore(cookingPotSlot, cookingPotData);
        if (kettleData != null) InventorySaveUtility.Restore(kettleSlot, kettleData);
        if (fryingPanData != null) InventorySaveUtility.Restore(fryingPanSlot, fryingPanData);
        if (inputData != null) InventorySaveUtility.Restore(inputInventory, inputData);
        if (outputData != null) InventorySaveUtility.Restore(outputInventory, outputData);
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
