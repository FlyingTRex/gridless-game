using System.Collections.Generic;
using UnityEngine;

// Real Furnace state (2026-08-13), replacing the "FurnaceSurface is a bare
// proximity marker, nothing else exists" state described in
// WOOD_AND_FUEL_PLANNING.md. FurnaceSurface stays untouched on the same
// GameObject — CraftingRecipe.requiresFurnace / PlayerCrafting.
// HasNearbyFurnace still gate the player-driven, skill-based bench recipes
// (IronIngotRecipe) purely on that marker's presence, same as before. This
// component is the Furnace's *own* separate, unattended production line —
// see SmeltableItem.cs for why that's a deliberately different recipe type
// from CraftingRecipe.
//
// Fuel: same FuelTier/FuelItem system as Campfire (2-slot inventory per
// WOOD_AND_FUEL_PLANNING.md's spec, vs. Campfire's 1 — a Furnace is meant to
// run longer unattended). Lighting is automatic, not a player button: with
// Auto-Run on, a non-empty recipe queue, and fuel on hand, the Furnace
// lights itself; runs out of fuel, goes dark, and simply waits for more.
//
// True unattended automation (Ben's call, 2026-08-13 — pulls forward part of
// WOOD_AND_FUEL_PLANNING.md's section 5 "autonomous production chain"
// vision): Update() ticks every frame regardless of whether the player is
// anywhere nearby or has the popup open, exactly like Campfire's own fuel
// timer already does. Three optional StorageBox links (assigned via
// FurnaceScreen's picker, not auto-detected) let it pull fuel/raw materials
// and push finished output into player-designated boxes within
// storageLinkRange, entirely on its own. On-board Fuel/Materials/Output
// inventories still exist underneath (Ben's call: on-board slots + auto-
// top-up/drain, not a raw passthrough) so a temporarily unlinked or
// out-of-range box doesn't stall production outright.
public class Furnace : MonoBehaviour, IInteractable
{
    public const int MaxQueueSize = 4;
    private const int FuelCapacity = 2;
    private const int MaterialsCapacity = 4;
    private const int OutputCapacity = 4;

    [SerializeField] private FuelItem[] fuelItems;
    [SerializeField] private SmeltableItem[] smeltableItems;
    [SerializeField] private float storageLinkRange = 10f;

    private Inventory fuelInventory;
    private Inventory materialsInventory;
    private Inventory outputInventory;

    private readonly List<SmeltableItem> recipeQueue = new List<SmeltableItem>();
    private int nextQueueIndex;
    private SmeltableItem activeRecipe;
    private float smeltSecondsElapsed;

    private bool isLit;
    private float fuelSecondsRemaining;
    private bool autoRunEnabled;

    private StorageBox fuelSourceBox;
    private StorageBox materialsSourceBox;
    private StorageBox outputBox;

    public string DisplayName => "Furnace";
    public string Prompt => $"Open {DisplayName}";
    public bool IsInstant => true;
    public float GetHoldDuration(GameObject player) => 0f;

    public Inventory FuelInventory => fuelInventory;
    public Inventory MaterialsInventory => materialsInventory;
    public Inventory OutputInventory => outputInventory;
    public FuelItem[] FuelItems => fuelItems;
    public SmeltableItem[] SmeltableItems => smeltableItems;
    public IReadOnlyList<SmeltableItem> RecipeQueue => recipeQueue;
    public SmeltableItem ActiveRecipe => activeRecipe;
    public float SmeltSecondsElapsed => smeltSecondsElapsed;
    public bool IsLit => isLit;
    public float FuelSecondsRemaining => fuelSecondsRemaining;
    public bool AutoRunEnabled => autoRunEnabled;
    public bool HasFuel => fuelInventory != null && fuelInventory.Slots.Count > 0;

    public StorageBox FuelSourceBox => fuelSourceBox;
    public StorageBox MaterialsSourceBox => materialsSourceBox;
    public StorageBox OutputBox => outputBox;

    public void Complete(GameObject player) => player.GetComponent<FurnaceScreen>()?.Open(this);

    private void Awake()
    {
        var allowedFuel = new ItemDefinition[fuelItems != null ? fuelItems.Length : 0];
        for (int i = 0; i < allowedFuel.Length; i++)
            allowedFuel[i] = fuelItems[i] != null ? fuelItems[i].item : null;
        fuelInventory = new Inventory(FuelCapacity, allowedFuel);

        var allowedMaterials = new List<ItemDefinition>();
        var allowedOutputs = new List<ItemDefinition>();
        if (smeltableItems != null)
        {
            foreach (var recipe in smeltableItems)
            {
                if (recipe == null) continue;
                if (recipe.ingredients != null)
                    foreach (var ingredient in recipe.ingredients)
                        if (ingredient != null && ingredient.item != null) allowedMaterials.Add(ingredient.item);
                if (recipe.outputItem != null) allowedOutputs.Add(recipe.outputItem);
            }
        }
        materialsInventory = new Inventory(MaterialsCapacity, allowedMaterials.ToArray());
        outputInventory = new Inventory(OutputCapacity, allowedOutputs.ToArray());
    }

    private void Update()
    {
        if (autoRunEnabled)
        {
            AutoRefill(fuelSourceBox, fuelInventory);
            AutoRefill(materialsSourceBox, materialsInventory);
        }

        if (isLit)
        {
            fuelSecondsRemaining -= Time.deltaTime;
            if (fuelSecondsRemaining <= 0f)
                isLit = false;
        }
        else if (autoRunEnabled && recipeQueue.Count > 0 && HasFuel)
        {
            TryAutoLight();
        }

        TickSmelting();

        if (autoRunEnabled)
            AutoDrain(outputBox, outputInventory);
    }

    // Recipes registered on this Furnace's prefab, all of them — not just
    // the up-to-4 currently queued ones. FurnaceScreen shows this full list
    // so the player can add/remove from the queue.
    public bool IsQueued(SmeltableItem recipe) => recipe != null && recipeQueue.Contains(recipe);

    // Toggles recipe in/out of the up-to-4 production queue. Returns the
    // resulting queued state (true = now queued). No-ops (returns false)
    // if the queue is already full and this recipe wasn't already in it.
    public bool ToggleQueue(SmeltableItem recipe)
    {
        if (recipe == null) return false;

        int idx = recipeQueue.IndexOf(recipe);
        if (idx >= 0)
        {
            // If this recipe is currently mid-smelt, let it finish — its
            // materials were already consumed — it just won't be picked
            // again once StartNextQueuedRecipe runs next.
            recipeQueue.RemoveAt(idx);
            return false;
        }

        if (recipeQueue.Count >= MaxQueueSize) return false;
        recipeQueue.Add(recipe);
        return true;
    }

    public void SetAutoRun(bool value) => autoRunEnabled = value;

    public void SetFuelSourceBox(StorageBox box) => fuelSourceBox = box;
    public void SetMaterialsSourceBox(StorageBox box) => materialsSourceBox = box;
    public void SetOutputBox(StorageBox box) => outputBox = box;

    // Every active StorageBox within storageLinkRange of this Furnace,
    // nearest first — what FurnaceScreen's Fuel/Materials/Output pickers
    // list as assignable candidates. Same distance basis AutoRefill/
    // AutoDrain use, so anything offered in the picker actually works once
    // assigned.
    public void FindNearbyStorageBoxes(List<StorageBox> result) =>
        StorageBox.FindNearby(transform.position, storageLinkRange, result);

    private bool TryAutoLight()
    {
        if (isLit || !HasFuel) return false;

        var slot = fuelInventory.Slots[0];
        var fuel = FindFuel(slot.item);
        if (fuel == null) return false;

        fuelInventory.RemoveItem(slot.item, 1);
        fuelSecondsRemaining = FuelTierScale.BurnMinutes(fuel.fuelTier) * 60f;
        isLit = true;
        return true;
    }

    private FuelItem FindFuel(ItemDefinition item)
    {
        if (item == null || fuelItems == null) return null;
        foreach (var fuel in fuelItems)
            if (fuel != null && fuel.item == item) return fuel;
        return null;
    }

    // Paused (not reset) while unlit — same "runs on its own, pauses on
    // interruption" convention Campfire's cooking timer already uses.
    private void TickSmelting()
    {
        if (activeRecipe == null)
        {
            if (!isLit || recipeQueue.Count == 0) return;
            StartNextQueuedRecipe();
            if (activeRecipe == null) return;
        }

        if (!isLit) return;

        smeltSecondsElapsed += Time.deltaTime;
        if (smeltSecondsElapsed < activeRecipe.smeltDurationSeconds) return;

        outputInventory.AddItem(activeRecipe.outputItem, activeRecipe.outputCount);
        activeRecipe = null;
        smeltSecondsElapsed = 0f;
    }

    // Round-robins through the queue starting at nextQueueIndex so one
    // always-satisfiable recipe at the front doesn't starve the others —
    // picks the first queued recipe (in that rotated order) whose
    // ingredients are on hand and whose output currently fits.
    private void StartNextQueuedRecipe()
    {
        if (recipeQueue.Count == 0) return;

        for (int i = 0; i < recipeQueue.Count; i++)
        {
            int idx = (nextQueueIndex + i) % recipeQueue.Count;
            var recipe = recipeQueue[idx];
            if (recipe == null) continue;
            if (!HasAllIngredients(recipe)) continue;
            if (!outputInventory.HasSpaceFor(recipe.outputItem, recipe.outputCount)) continue;

            if (recipe.ingredients != null)
                foreach (var ingredient in recipe.ingredients)
                    if (ingredient != null && ingredient.item != null)
                        materialsInventory.RemoveItem(ingredient.item, ingredient.count);

            activeRecipe = recipe;
            smeltSecondsElapsed = 0f;
            nextQueueIndex = (idx + 1) % recipeQueue.Count;
            return;
        }
    }

    private bool HasAllIngredients(SmeltableItem recipe)
    {
        if (recipe.ingredients == null) return true;
        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient == null || ingredient.item == null) continue;
            if (materialsInventory.GetCount(ingredient.item) < ingredient.count) return false;
        }
        return true;
    }

    // Pulls whatever target's restrictedTo list allows out of source's box,
    // as much as currently fits — target.SpaceFor already returns 0 for a
    // disallowed item, so this naturally only ever moves fuel into
    // fuelInventory and recipe ingredients into materialsInventory.
    private void AutoRefill(StorageBox source, Inventory target)
    {
        if (source == null || target == null) return;
        if ((source.transform.position - transform.position).sqrMagnitude > storageLinkRange * storageLinkRange) return;

        foreach (var slot in new List<Inventory.Slot>(source.Inventory.Slots))
        {
            if (slot.item == null || slot.equipment != null) continue;
            int take = Mathf.Min(target.SpaceFor(slot.item), slot.count);
            if (take <= 0) continue;

            source.Inventory.RemoveItem(slot.item, take);
            target.AddItem(slot.item, take);
        }
    }

    // Symmetric opposite of AutoRefill — pushes everything out of source
    // (the Furnace's own onboard inventory) into target (a linked box) as
    // it has room for.
    private void AutoDrain(StorageBox target, Inventory source)
    {
        if (target == null || source == null) return;
        if ((target.transform.position - transform.position).sqrMagnitude > storageLinkRange * storageLinkRange) return;

        foreach (var slot in new List<Inventory.Slot>(source.Slots))
        {
            if (slot.item == null || slot.equipment != null) continue;
            int take = Mathf.Min(target.Inventory.SpaceFor(slot.item), slot.count);
            if (take <= 0) continue;

            source.RemoveItem(slot.item, take);
            target.Inventory.AddItem(slot.item, take);
        }
    }
}
