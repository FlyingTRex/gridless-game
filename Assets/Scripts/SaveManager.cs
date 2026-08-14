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
        RestoreWorldObjects<NPCHiring>(data["npcs"] as JArray, RestoreNpc);
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
            ["isMale"] = bodyModel == null || bodyModel.IsMale,
            ["position"] = CaptureVector3(transform.position),
            ["yaw"] = transform.eulerAngles.y,
            ["skills"] = CaptureSkills(skills.Levels),
            ["currency"] = CaptureCurrency(),
            ["inventory"] = InventorySaveUtility.Capture(playerInventory.Inventory),
            ["equipment"] = CaptureEquipmentSlots(),
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
            (float)(data["maxWill"] ?? 100f));

        if (bodyModel != null && data["isMale"] != null)
            bodyModel.SetGender((bool)data["isMale"]);

        if (data["position"] is JObject pos && controller != null)
            controller.Teleport(ParseVector3(pos), (float)(data["yaw"] ?? 0f));

        if (data["skills"] is JArray skillArray)
            RestoreSkills(skillArray, skills.RestoreLevel);

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

        // Every worn item above was restored directly into its
        // PlayerEquipment slot's Inventory, hidden (Stash()ed) by
        // EquipmentSaveUtility.Restore — this re-runs the same bone-attach
        // sweep a gender toggle already triggers so each one becomes
        // visible/carried on the correct bone instead of staying invisible.
        bodyModel?.RefreshAllAnchors();
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
}
