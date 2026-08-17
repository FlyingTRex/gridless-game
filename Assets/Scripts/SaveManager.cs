using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

// Manual-trigger save/load persistence, v1 (SAVE_LOAD_PLANNING.md). A
// single JSON file at Application.persistentDataPath — no autosave, no
// multiple slots, matching Ben's original framing exactly ("continue where
// we're at, instead of restarting at every test"). Captures the player
// (vitals/skills/currency/inventory/equipment, including the full
// recursive nested-equipment state from EquipmentSaveUtility) plus every
// SaveId-tagged StorageBox/ResourceNode/NPCHiring in the scene. Explicitly
// deferred, per the plan: loose world pickups, built structures, Lockbox/
// Bank contents.
[RequireComponent(typeof(FirstPersonController))]
[RequireComponent(typeof(PlayerVitals))]
[RequireComponent(typeof(PlayerSkills))]
[RequireComponent(typeof(PlayerCurrency))]
[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerEquipment))]
[RequireComponent(typeof(PlayerFame))]
[RequireComponent(typeof(PlayerMagic))]
public class SaveManager : MonoBehaviour
{
    private const string FileName = "save.json";

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static bool SaveExists => File.Exists(FilePath);

    private FirstPersonController controller;
    private PlayerVitals vitals;
    private PlayerSkills skills;
    private PlayerCurrency currency;
    private PlayerInventory playerInventory;
    private PlayerEquipment equipment;
    private PlayerBodyModel bodyModel;
    private PlayerFame fame;
    private PlayerMapExploration mapExploration;
    private VillageFlagSpawner villageFlagSpawner;
    private PlayerMagic magic;

    // Set by Load() so a fresh scene's starting-gear auto-equip (Shirt/
    // Jeans/Belt/Canteen, each guarded on "nothing equipped yet") doesn't
    // fight a save that intentionally has that slot empty (dropped, or
    // swapped for something else) — those Start() methods run after this
    // one only if execution order says so, so this is read defensively
    // rather than relied on for ordering; each starting-gear guard already
    // checks Equipped != null on its own, which composes for free once
    // restore has populated the slot before that check runs.
    public bool LoadedFromSave { get; private set; }

    private void Awake()
    {
        controller = GetComponent<FirstPersonController>();
        vitals = GetComponent<PlayerVitals>();
        skills = GetComponent<PlayerSkills>();
        currency = GetComponent<PlayerCurrency>();
        playerInventory = GetComponent<PlayerInventory>();
        equipment = GetComponent<PlayerEquipment>();
        bodyModel = GetComponent<PlayerBodyModel>();
        fame = GetComponent<PlayerFame>();
        mapExploration = GetComponent<PlayerMapExploration>();
        villageFlagSpawner = GetComponent<VillageFlagSpawner>();
        magic = GetComponent<PlayerMagic>();
    }

    private void Start()
    {
        if (SaveExists) Load();
    }

    public void Save()
    {
        var data = new JObject
        {
            ["player"] = CapturePlayer(),
            ["storageBoxes"] = CaptureWorldObjects<StorageBox>(CaptureStorageBox),
            ["resourceNodes"] = CaptureWorldObjects<ResourceNode>(CaptureResourceNode),
            ["npcs"] = CaptureWorldObjects<NPCHiring>(CaptureNpc),
            ["gardenPlots"] = CaptureWorldObjects<GardenPlot>(CaptureGardenPlot),
            ["gardenPlots4x4"] = CaptureWorldObjects<GardenPlot4x4>(CaptureGardenPlot4x4),
            ["placedPieces"] = CaptureWorldObjects<PlacedPiece>(CapturePlacedPiece),
            ["furnaces"] = CaptureWorldObjects<Furnace>((furnace, saveId) =>
                new JObject { ["saveId"] = saveId.Id, ["state"] = CaptureFurnace(furnace) }),
        };

        File.WriteAllText(FilePath, data.ToString());
        Debug.Log($"SaveManager: saved to {FilePath}");
    }

    public void Load()
    {
        if (!SaveExists) return;

        JObject data;
        try
        {
            data = JObject.Parse(File.ReadAllText(FilePath));
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveManager: failed to read save file — {e.Message}");
            return;
        }

        LoadedFromSave = true;

        if (data["player"] is JObject player) RestorePlayer(player);

        RestoreWorldObjects<StorageBox>(data["storageBoxes"] as JArray, RestoreStorageBox);
        RestoreWorldObjects<ResourceNode>(data["resourceNodes"] as JArray, RestoreResourceNode);
        RestoreNpcs(data["npcs"] as JArray);
        RestoreWorldObjects<GardenPlot>(data["gardenPlots"] as JArray, RestoreGardenPlot);
        RestoreWorldObjects<GardenPlot4x4>(data["gardenPlots4x4"] as JArray, RestoreGardenPlot4x4);
        RestorePlacedPieces(data["placedPieces"] as JArray);
        RestoreWorldObjects<Furnace>(data["furnaces"] as JArray, RestoreFurnace);
    }

    // ---- Player ----

    private JObject CapturePlayer()
    {
        return new JObject
        {
            ["health"] = vitals.Health,
            ["hunger"] = vitals.Hunger,
            ["thirst"] = vitals.Thirst,
            ["stamina"] = vitals.Stamina,
            ["bodyTemperature"] = vitals.BodyTemperature,
            ["will"] = vitals.Will,
            ["maxWill"] = vitals.MaxWill,
            ["maxHealth"] = vitals.MaxHealth,
            ["maxStamina"] = vitals.MaxStamina,
            ["isMale"] = bodyModel == null || bodyModel.IsMale,
            ["position"] = CaptureVector3(transform.position),
            ["yaw"] = transform.eulerAngles.y,
            ["skills"] = CaptureSkills(skills.Levels),
            ["fame"] = fame.Fame,
            ["currency"] = CaptureCurrency(),
            ["inventory"] = InventorySaveUtility.Capture(playerInventory.Inventory),
            ["equipment"] = CaptureEquipmentSlots(),
            ["mapExploration"] = mapExploration != null ? mapExploration.CaptureRevealedBase64() : null,
            ["magicLineages"] = CaptureMagicLineages(),
            ["selectedWish"] = magic != null ? magic.IdForWish(magic.SelectedWish) : null,
        };
    }

    private void RestorePlayer(JObject data)
    {
        vitals.RestoreVitals(
            (float)(data["health"] ?? 100f),
            (float)(data["hunger"] ?? 100f),
            (float)(data["thirst"] ?? 100f),
            (float)(data["stamina"] ?? 100f),
            (float)(data["bodyTemperature"] ?? 50f),
            (float)(data["will"] ?? 100f),
            (float)(data["maxWill"] ?? 100f),
            (float)(data["maxHealth"] ?? 100f),
            (float)(data["maxStamina"] ?? 100f));

        if (bodyModel != null && data["isMale"] != null)
            bodyModel.SetGender((bool)data["isMale"]);

        if (data["position"] is JObject pos && controller != null)
            controller.Teleport(ParseVector3(pos), (float)(data["yaw"] ?? 0f));

        if (data["skills"] is JArray skillArray)
            RestoreSkills(skillArray, skills.RestoreLevel);

        if (data["fame"] != null)
            fame.RestoreFame((float)data["fame"]);

        if (data["currency"] is JObject currencyObj)
            foreach (CoinType type in Enum.GetValues(typeof(CoinType)))
                if (currencyObj[type.ToString()] != null)
                    currency.RestoreBalance(type, (int)currencyObj[type.ToString()]);

        if (data["inventory"] is JArray inventoryArray)
            InventorySaveUtility.Restore(playerInventory.Inventory, inventoryArray);

        if (data["equipment"] is JObject equipmentObj)
            foreach (var slotName in equipment.SlotNames)
                if (equipmentObj[slotName] is JArray slotArray)
                    InventorySaveUtility.Restore(equipment.GetSlot(slotName), slotArray);

        if (mapExploration != null && data["mapExploration"] != null)
            mapExploration.RestoreRevealedBase64((string)data["mapExploration"]);

        if (magic != null)
        {
            // Lineages first -- SelectWish below refuses a wish outside a
            // known lineage, so this order matters. LearnLineage no-ops
            // safely if a lineage's already known (nothing is on a fresh
            // character yet, since Awake() skipped its own random
            // assignment specifically because a save exists).
            if (data["magicLineages"] is JArray lineageArray)
                foreach (var token in lineageArray)
                {
                    var lineage = SkillDatabase.Instance?.Find((string)token);
                    if (lineage != null) magic.LearnLineage(lineage);
                }

            // Backward-compat: a save written before this fix existed has
            // no magicLineages key, so the loop above restored nothing --
            // give that character the same free random lineage a new one
            // gets, rather than leaving them with none.
            magic.AssignRandomLineageIfNone();

            var wish = magic.FindWish((string)data["selectedWish"]);
            if (wish != null) magic.SelectWish(wish);

            // Covers both a pre-this-fix save (no magicLineages key at all,
            // so knownLineages is still empty here) and the ordinary case
            // where selectedWish just wasn't saved for some reason -- same
            // "pick the first known wish" fallback a fresh character's own
            // Awake() always ran.
            magic.SelectDefaultWishIfNone();
        }

        // Every worn item above was restored directly into its
        // PlayerEquipment slot's Inventory, hidden (Stash()ed) by
        // EquipmentSaveUtility.Restore — this re-runs the same bone-attach
        // sweep a gender toggle already triggers so each one becomes
        // visible/carried on the correct bone instead of staying invisible.
        bodyModel?.RefreshAllAnchors();
    }

    private JArray CaptureMagicLineages()
    {
        var array = new JArray();
        if (magic == null) return array;
        foreach (var lineage in magic.KnownLineages)
        {
            string id = SkillDatabase.Instance != null ? SkillDatabase.Instance.IdFor(lineage) : null;
            if (id != null) array.Add(id);
        }
        return array;
    }

    // ---- Shared skill/currency/vector helpers ----

    private static JArray CaptureSkills(IReadOnlyDictionary<SkillDefinition, float> levels)
    {
        var array = new JArray();
        foreach (var kv in levels)
        {
            string id = SkillDatabase.Instance != null ? SkillDatabase.Instance.IdFor(kv.Key) : null;
            if (id == null) continue;
            array.Add(new JObject { ["skill"] = id, ["level"] = kv.Value });
        }
        return array;
    }

    private static void RestoreSkills(JArray data, Action<SkillDefinition, float> restore)
    {
        foreach (var token in data)
        {
            if (!(token is JObject skillObj)) continue;
            var skill = SkillDatabase.Instance != null ? SkillDatabase.Instance.Find((string)skillObj["skill"]) : null;
            if (skill != null) restore(skill, (float)skillObj["level"]);
        }
    }

    private JObject CaptureCurrency()
    {
        var obj = new JObject();
        foreach (CoinType type in Enum.GetValues(typeof(CoinType)))
            obj[type.ToString()] = currency.GetBalance(type);
        return obj;
    }

    private JObject CaptureEquipmentSlots()
    {
        var obj = new JObject();
        foreach (var slotName in equipment.SlotNames)
            obj[slotName] = InventorySaveUtility.Capture(equipment.GetSlot(slotName));
        return obj;
    }

    private static JObject CaptureVector3(Vector3 v) => new JObject { ["x"] = v.x, ["y"] = v.y, ["z"] = v.z };

    private static Vector3 ParseVector3(JObject obj) =>
        new Vector3((float)(obj["x"] ?? 0f), (float)(obj["y"] ?? 0f), (float)(obj["z"] ?? 0f));

    // ---- World objects (generic SaveId-keyed capture/restore) ----

    private static JArray CaptureWorldObjects<T>(Func<T, SaveId, JObject> capture) where T : Component
    {
        var array = new JArray();
        foreach (var component in FindObjectsByType<T>(FindObjectsSortMode.None))
        {
            var saveId = component.GetComponent<SaveId>();
            if (saveId == null || string.IsNullOrEmpty(saveId.Id)) continue;

            array.Add(capture(component, saveId));
        }
        return array;
    }

    private static void RestoreWorldObjects<T>(JArray data, Action<T, JObject> restore) where T : Component
    {
        if (data == null) return;

        foreach (var token in data)
        {
            if (!(token is JObject obj)) continue;

            var saveId = SaveIdRegistry.Find((string)obj["saveId"]);
            var component = saveId != null ? saveId.GetComponent<T>() : null;
            if (component == null) continue;

            restore(component, obj["state"] as JObject);
        }
    }

    private static JObject CaptureStorageBox(StorageBox box, SaveId saveId) => new JObject
    {
        ["saveId"] = saveId.Id,
        ["state"] = new JObject
        {
            ["name"] = box.DisplayName,
            ["inventory"] = InventorySaveUtility.Capture(box.Inventory),
        },
    };

    private static void RestoreStorageBox(StorageBox box, JObject state)
    {
        if (state == null) return;
        if (state["name"] != null) box.Rename((string)state["name"]);
        if (state["inventory"] is JArray inv) InventorySaveUtility.Restore(box.Inventory, inv);
    }

    private static JObject CaptureResourceNode(ResourceNode node, SaveId saveId) => new JObject
    {
        ["saveId"] = saveId.Id,
        ["state"] = new JObject { ["respawnSecondsRemaining"] = node.RespawnSecondsRemaining },
    };

    private static void RestoreResourceNode(ResourceNode node, JObject state)
    {
        if (state == null) return;
        node.RestoreAvailability((float)(state["respawnSecondsRemaining"] ?? -1f));
    }

    private static JObject CaptureNpc(NPCHiring npc, SaveId saveId)
    {
        var job = npc.Job;

        var toolsObj = new JObject();
        foreach (var kv in job.EquippedTools)
        {
            string id = ItemDatabase.Instance != null ? ItemDatabase.Instance.IdFor(kv.Value) : null;
            if (id != null) toolsObj[kv.Key] = id;
        }

        var depositSaveId = job.DepositContainer != null ? job.DepositContainer.GetComponent<SaveId>() : null;

        return new JObject
        {
            ["saveId"] = saveId.Id,
            ["state"] = new JObject
            {
                ["isHired"] = npc.IsHired,
                ["isWaitingForPayment"] = npc.IsWaitingForPayment,
                ["workTimer"] = npc.WorkTimer,
                ["job"] = job.AssignedJob != null && NPCJobDatabase.Instance != null
                    ? NPCJobDatabase.Instance.IdFor(job.AssignedJob)
                    : null,
                ["tools"] = toolsObj,
                ["depositContainer"] = depositSaveId != null ? depositSaveId.Id : null,
                ["cargo"] = InventorySaveUtility.Capture(npc.Cargo.Inventory),
                ["skills"] = CaptureSkills(npc.Skills.Levels),
                ["position"] = CaptureVector3(npc.transform.position),
            },
        };
    }

    // Bespoke, not the generic RestoreWorldObjects<T> every other category
    // uses -- same reason as RestorePlacedPieces (2026-08-17, once all
    // pre-placed NPCs were removed from the scene, see
    // BUGS_AND_ENHANCEMENTS.md): a hired NPC no longer pre-exists in a
    // fresh scene at all, VillageFlagSpawner is now the only source of
    // hireable NPC instances in the game, so restoring one that's missing
    // means re-instantiating from that same prefab.
    private void RestoreNpcs(JArray data)
    {
        if (data == null) return;

        foreach (var token in data)
        {
            if (!(token is JObject obj) || !(obj["state"] is JObject state)) continue;

            string saveId = (string)obj["saveId"];
            var npc = SaveIdRegistry.Find(saveId)?.GetComponent<NPCHiring>();

            if (npc == null)
            {
                var prefab = villageFlagSpawner != null ? villageFlagSpawner.HireableNpcPrefab : null;
                if (prefab == null) continue;

                var position = ParseVector3(state["position"] as JObject);
                var instance = Instantiate(prefab, position, Quaternion.identity);
                npc = instance.GetComponent<NPCHiring>();
                if (npc == null) continue;

                instance.GetComponent<SaveId>()?.AssignId(saveId);
            }

            RestoreNpc(npc, state);
        }
    }

    private static void RestoreNpc(NPCHiring npc, JObject state)
    {
        if (state == null) return;

        npc.RestoreHiringState(
            (bool)(state["isHired"] ?? false),
            (bool)(state["isWaitingForPayment"] ?? false),
            (float)(state["workTimer"] ?? 0f));

        var jobDef = state["job"] != null && NPCJobDatabase.Instance != null
            ? NPCJobDatabase.Instance.Find((string)state["job"])
            : null;

        var tools = new Dictionary<string, ItemDefinition>();
        if (state["tools"] is JObject toolsObj && ItemDatabase.Instance != null)
        {
            foreach (var prop in toolsObj.Properties())
            {
                var item = ItemDatabase.Instance.Find((string)prop.Value);
                if (item != null) tools[prop.Name] = item;
            }
        }

        StorageBox deposit = null;
        if (state["depositContainer"] != null)
            deposit = SaveIdRegistry.Find((string)state["depositContainer"])?.GetComponent<StorageBox>();

        npc.Job.RestoreState(jobDef, tools, deposit);

        if (state["cargo"] is JArray cargoArray)
            InventorySaveUtility.Restore(npc.Cargo.Inventory, cargoArray);

        if (state["skills"] is JArray skillArray)
            RestoreSkills(skillArray, npc.Skills.RestoreLevel);

        if (state["position"] is JObject pos)
            npc.transform.position = ParseVector3(pos);
    }

    // ---- Garden Plot (single-cell POC) ----

    private static JObject CaptureGardenPlot(GardenPlot plot, SaveId saveId) => new JObject
    {
        ["saveId"] = saveId.Id,
        ["state"] = new JObject
        {
            ["plotState"] = plot.State.ToString(),
            ["seedsRemaining"] = plot.SeedsRemaining,
            ["elapsed"] = plot.GetElapsedSeconds(),
        },
    };

    private static void RestoreGardenPlot(GardenPlot plot, JObject state)
    {
        if (state == null) return;

        var plotState = Enum.TryParse((string)state["plotState"], out GardenPlot.PlotState parsed)
            ? parsed
            : GardenPlot.PlotState.Empty;

        plot.RestoreState((int)(state["seedsRemaining"] ?? 0), plotState, (float)(state["elapsed"] ?? 0f));
    }

    // ---- Garden Plot 4x4 (16-cell grid) ----

    private static JObject CaptureGardenPlot4x4(GardenPlot4x4 plot, SaveId saveId)
    {
        var cells = new JArray();
        for (int i = 0; i < GardenPlot4x4.CellCount; i++)
        {
            var cellState = plot.GetState(i);
            var cellObj = new JObject { ["cellState"] = cellState.ToString() };

            if (cellState != GardenPlot4x4.CellState.Empty)
            {
                var seed = plot.GetSeedItem(i);
                cellObj["seed"] = ItemDatabase.Instance != null ? ItemDatabase.Instance.IdFor(seed) : null;
                cellObj["count"] = plot.GetSeedCount(i);
                cellObj["elapsed"] = plot.GetElapsedSeconds(i);
            }

            cells.Add(cellObj);
        }

        return new JObject
        {
            ["saveId"] = saveId.Id,
            ["state"] = new JObject { ["cells"] = cells },
        };
    }

    private static void RestoreGardenPlot4x4(GardenPlot4x4 plot, JObject state)
    {
        if (state == null || !(state["cells"] is JArray cells)) return;

        for (int i = 0; i < cells.Count && i < GardenPlot4x4.CellCount; i++)
        {
            if (!(cells[i] is JObject cellObj)) continue;

            var cellState = Enum.TryParse((string)cellObj["cellState"], out GardenPlot4x4.CellState parsed)
                ? parsed
                : GardenPlot4x4.CellState.Empty;
            if (cellState == GardenPlot4x4.CellState.Empty) continue; // cell already starts Empty

            var seed = ItemDatabase.Instance != null ? ItemDatabase.Instance.Find((string)cellObj["seed"]) : null;
            int count = (int)(cellObj["count"] ?? 0);
            float elapsed = (float)(cellObj["elapsed"] ?? 0f);
            plot.RestoreCell(i, seed, count, cellState, elapsed);
        }
    }

    // ---- Placed structures (SAVE_LOAD_PLANNING.md section 11) ----
    //
    // Unlike every capture above, a placed structure doesn't pre-exist in
    // a freshly loaded scene -- a player-built Village Flag/wall/Campfire
    // has to be recreated from scratch, not just found-and-restored. Base
    // state (which BuildPiece, where) covers every plain piece (Wall,
    // Foundation, Roof Panel, City Statue -- CityStatue.Exists is a pure
    // scene scan, so just re-existing is the whole fix). A per-type extra-
    // state hook layers on top for pieces that carry their own runtime
    // state; only VillageFlag's is built so far (its display name --
    // tier is already implied by which BuildPiece was placed). Campfire/
    // Furnace's own richer state (fuel, lit/burn timer, recipe queue,
    // linked StorageBoxes) is a real separate follow-up, not built here --
    // see SAVE_LOAD_PLANNING.md section 11's scope note.

    private static JObject CapturePlacedPiece(PlacedPiece piece, SaveId saveId)
    {
        var state = new JObject
        {
            ["buildPiece"] = BuildPieceDatabase.Instance != null ? BuildPieceDatabase.Instance.IdFor(piece.Piece) : null,
            ["position"] = CaptureVector3(piece.transform.position),
            ["yaw"] = piece.transform.eulerAngles.y,
        };

        if (piece.GetComponent<VillageFlag>() is { } flag)
            state["villageName"] = flag.DisplayName;

        if (piece.GetComponent<Campfire>() is { } campfire)
            state["campfire"] = CaptureCampfire(campfire);

        // Furnace is NOT handled here -- there's no BuildPiece/prefab for
        // one at all (a single fixed world fixture, not player-buildable),
        // so it's its own top-level SaveManager category instead (see
        // Save()/Load() and CaptureFurnace/RestoreFurnace below).

        return new JObject { ["saveId"] = saveId.Id, ["state"] = state };
    }

    private static JObject CaptureCampfire(Campfire campfire) => new JObject
    {
        ["isLit"] = campfire.IsLit,
        ["fuelSecondsRemaining"] = campfire.FuelSecondsRemaining,
        ["activeRecipe"] = campfire.ActiveRecipeId,
        ["cookSecondsElapsed"] = campfire.CookSecondsElapsed,
        ["fuel"] = InventorySaveUtility.Capture(campfire.FuelInventory),
        ["grill"] = InventorySaveUtility.Capture(campfire.GrillSlot),
        ["cookingPot"] = InventorySaveUtility.Capture(campfire.CookingPotSlot),
        ["kettle"] = InventorySaveUtility.Capture(campfire.KettleSlot),
        ["fryingPan"] = InventorySaveUtility.Capture(campfire.FryingPanSlot),
        ["input"] = InventorySaveUtility.Capture(campfire.InputInventory),
        ["output"] = InventorySaveUtility.Capture(campfire.OutputInventory),
    };

    private static JObject CaptureFurnace(Furnace furnace)
    {
        var queue = new JArray();
        foreach (var id in furnace.RecipeQueueIds()) queue.Add(id);

        var fuelSourceId = furnace.FuelSourceBox != null ? furnace.FuelSourceBox.GetComponent<SaveId>() : null;
        var materialsSourceId = furnace.MaterialsSourceBox != null ? furnace.MaterialsSourceBox.GetComponent<SaveId>() : null;
        var outputId = furnace.OutputBox != null ? furnace.OutputBox.GetComponent<SaveId>() : null;

        return new JObject
        {
            ["recipeQueue"] = queue,
            ["activeRecipe"] = furnace.ActiveRecipeId,
            ["smeltSecondsElapsed"] = furnace.SmeltSecondsElapsed,
            ["isLit"] = furnace.IsLit,
            ["fuelSecondsRemaining"] = furnace.FuelSecondsRemaining,
            ["autoRun"] = furnace.AutoRunEnabled,
            ["fuelSourceBox"] = fuelSourceId != null ? fuelSourceId.Id : null,
            ["materialsSourceBox"] = materialsSourceId != null ? materialsSourceId.Id : null,
            ["outputBox"] = outputId != null ? outputId.Id : null,
            ["fuel"] = InventorySaveUtility.Capture(furnace.FuelInventory),
            ["materials"] = InventorySaveUtility.Capture(furnace.MaterialsInventory),
            ["output"] = InventorySaveUtility.Capture(furnace.OutputInventory),
        };
    }

    private void RestorePlacedPieces(JArray data)
    {
        if (data == null) return;

        foreach (var token in data)
        {
            if (!(token is JObject obj) || !(obj["state"] is JObject state)) continue;

            string saveId = (string)obj["saveId"];
            var existing = SaveIdRegistry.Find(saveId)?.GetComponent<PlacedPiece>();
            var piece = existing;

            if (piece == null)
            {
                var buildPiece = BuildPieceDatabase.Instance != null
                    ? BuildPieceDatabase.Instance.Find((string)state["buildPiece"])
                    : null;
                if (buildPiece == null || buildPiece.prefab == null) continue;

                var position = ParseVector3(state["position"] as JObject);
                var rotation = Quaternion.Euler(0f, (float)(state["yaw"] ?? 0f), 0f);
                var instance = Instantiate(buildPiece.prefab, position, rotation);

                piece = instance.GetComponent<PlacedPiece>();
                if (piece == null) piece = instance.AddComponent<PlacedPiece>();
                piece.Piece = buildPiece;

                instance.GetComponent<SaveId>()?.AssignId(saveId);
            }

            if (state["villageName"] != null && piece.GetComponent<VillageFlag>() is { } flag)
                flag.Rename((string)state["villageName"]);

            if (state["campfire"] is JObject campfireState && piece.GetComponent<Campfire>() is { } campfire)
                RestoreCampfire(campfire, campfireState);
        }
    }

    private static void RestoreCampfire(Campfire campfire, JObject state)
    {
        campfire.RestoreState(
            (bool)(state["isLit"] ?? false),
            (float)(state["fuelSecondsRemaining"] ?? 0f),
            (string)state["activeRecipe"],
            (float)(state["cookSecondsElapsed"] ?? 0f),
            state["fuel"] as JArray,
            state["grill"] as JArray,
            state["cookingPot"] as JArray,
            state["kettle"] as JArray,
            state["fryingPan"] as JArray,
            state["input"] as JArray,
            state["output"] as JArray);
    }

    private static void RestoreFurnace(Furnace furnace, JObject state)
    {
        var queueIds = new List<string>();
        if (state["recipeQueue"] is JArray queueArray)
            foreach (var token in queueArray)
                queueIds.Add((string)token);

        StorageBox Resolve(string key) =>
            state[key] != null ? SaveIdRegistry.Find((string)state[key])?.GetComponent<StorageBox>() : null;

        furnace.RestoreState(
            queueIds,
            (string)state["activeRecipe"],
            (float)(state["smeltSecondsElapsed"] ?? 0f),
            (bool)(state["isLit"] ?? false),
            (float)(state["fuelSecondsRemaining"] ?? 0f),
            (bool)(state["autoRun"] ?? false),
            Resolve("fuelSourceBox"),
            Resolve("materialsSourceBox"),
            Resolve("outputBox"),
            state["fuel"] as JArray,
            state["materials"] as JArray,
            state["output"] as JArray);
    }
}
