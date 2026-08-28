using System;
using System.Collections.Generic;
using System.IO;
using Mirror;
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
//
// Persistence restructure chunk 5a (MULTIPLAYER_PLANNING.md section 3
// item 5), 2026-08-23: converted to NetworkBehaviour, plus a real
// RequestSave/CmdSave Command. File I/O has to happen on the server's
// own disk (the actual source of truth for a dedicated server), not
// whichever client's machine clicked the button -- today that's
// invisible in host-alone testing (client and server are the same
// process), but a genuine remote client's local Save() call would
// write to that client's own disk, not the server's. Every OTHER
// SaveManager method (Save/Load/CapturePlayer/RestorePlayer/
// ResolveFreshStart/...) stays exactly as it was -- calling Save()
// directly is now only correct when isServer is already true (the
// Command, or a server-only trigger like autosave/disconnect/shutdown
// in chunk 5b); a client calling it directly would still write to its
// own local disk harmlessly, just not to the real save location.
[RequireComponent(typeof(FirstPersonController))]
[RequireComponent(typeof(PlayerVitals))]
[RequireComponent(typeof(PlayerSkills))]
[RequireComponent(typeof(PlayerCurrency))]
[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerEquipment))]
[RequireComponent(typeof(PlayerFame))]
[RequireComponent(typeof(PlayerMagic))]
[RequireComponent(typeof(PlayerIdentity))]
[RequireComponent(typeof(PlayerTeam))]
public class SaveManager : NetworkBehaviour
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
    private PlayerIdentity identity;
    private PlayerTeam team;

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
        identity = GetComponent<PlayerIdentity>();
        team = GetComponent<PlayerTeam>();
    }

    // Chunk 3 (see Save()/Load() below): can't call Load() from Start()
    // unconditionally the way chunk 1 did -- PlayerId is only populated
    // after CmdSetPlayerId's client-to-server round trip completes, which
    // Start()'s own ordering can't guarantee has happened yet. Wait for
    // PlayerIdentity's own readiness signal instead.
    private void Start()
    {
        if (identity != null && !string.IsNullOrEmpty(identity.PlayerId))
        {
            // Already ready by the time this runs (e.g. host testing,
            // where the round trip resolves same-frame) -- no need to
            // wait for an event that already fired.
            ResolveFreshStart();
        }
        else if (identity != null)
        {
            identity.PlayerIdReady += OnPlayerIdReady;
        }
    }

    private void OnPlayerIdReady(string id)
    {
        identity.PlayerIdReady -= OnPlayerIdReady;
        ResolveFreshStart();
    }

    // Chunk 4 (MULTIPLAYER_PLANNING.md section 3 item 5), 2026-08-23:
    // the one place that now decides "is this a genuinely new
    // character" per-player, replacing the old global SaveManager
    // .SaveExists check other scripts (PlayerMagic) used to reach for
    // directly -- that check couldn't tell "no save file exists at
    // all" apart from "a save file exists but doesn't have *my*
    // character in it," and a second player joining a world someone
    // else had already saved needs exactly that distinction. Load()
    // only ever sets LoadedFromSave true when this player's own
    // character record was actually found -- covers both cases
    // (missing file entirely, or file exists without this player's
    // entry) with the same one correct signal.
    private void ResolveFreshStart()
    {
        if (SaveExists) Load();

        if (!LoadedFromSave)
        {
            magic?.AssignRandomLineageIfNone();
            magic?.SelectDefaultWishIfNone();
        }
    }

    private void OnDestroy()
    {
        if (identity != null) identity.PlayerIdReady -= OnPlayerIdReady;
    }

    // Multiplayer persistence restructure, chunk 1 (MULTIPLAYER_PLANNING.md
    // section 3 item 5), 2026-08-23: pure data-shape split -- everything
    // that's per-character lives under "player"/now "characters", every
    // shared world key lives under "world".
    //
    // Chunk 3, same day: "player" (singular) became "characters" (a real
    // dictionary keyed by PlayerIdentity.PlayerId from chunk 2) -- this is
    // the actual "one player" -> "N players" architectural change. Save()
    // does a read-modify-write rather than overwriting the whole file, so
    // one player saving doesn't clobber another player's already-saved
    // character record still sitting in the same file. World data is
    // still captured/restored by every player's own SaveManager
    // regardless of who's saving -- deciding whether that should be
    // server-only is chunk 5's job (real save triggers), not this one.
    //
    // Breaking format change, deliberate: an existing save.json written by
    // an older shape won't load correctly under this Load() -- delete any
    // old save.json before testing this, there's no migration path and
    // none is planned for a pre-restructure dev save.

    // Chunk 5a: the real trigger for a client-initiated save (the manual
    // Save button). Save() itself is unchanged and still callable
    // directly by anything already running server-side (autosave,
    // disconnect, shutdown -- chunk 5b).
    public void RequestSave() => CmdSave();

    [Command]
    private void CmdSave() => Save();

    public void Save()
    {
        string playerId = identity != null ? identity.PlayerId : null;
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogWarning("SaveManager: Save() called before PlayerId was ready -- skipping.");
            return;
        }

        var data = TryReadExisting() ?? new JObject();

        var characters = data["characters"] as JObject ?? new JObject();
        characters[playerId] = CapturePlayer();
        data["characters"] = characters;

        data["world"] = new JObject
        {
            ["storageBoxes"] = CaptureWorldObjects<StorageBox>(CaptureStorageBox),
            ["resourceNodes"] = CaptureWorldObjects<ResourceNode>(CaptureResourceNode),
            ["npcs"] = CaptureWorldObjects<NPCHiring>(CaptureNpc),
            ["gardenPlots"] = CaptureWorldObjects<GardenPlot>(CaptureGardenPlot),
            ["gardenPlots4x4"] = CaptureWorldObjects<GardenPlot4x4>(CaptureGardenPlot4x4),
            ["placedPieces"] = CaptureWorldObjects<PlacedPiece>(CapturePlacedPiece),
            ["furnaces"] = CaptureWorldObjects<Furnace>((furnace, saveId) =>
                new JObject { ["saveId"] = saveId.Id, ["state"] = CaptureFurnace(furnace) }),
            ["vendorStalls"] = CaptureWorldObjects<VendorStall>(CaptureVendorStall),
            // Village Flag spawn countdown (2026-08-21, Ben's ask) -- never
            // saved before, so every reload silently restarted the up-to
            // 30-minute wait from zero. A plain top-level float, same
            // "nothing to validate, just resume from it" shape as
            // PlayerFame's own single-value capture.
            ["villageFlagSpawnTimer"] = villageFlagSpawner != null ? villageFlagSpawner.SpawnTimerSeconds : (float?)null,
        };

        File.WriteAllText(FilePath, data.ToString());
        Debug.Log($"SaveManager: saved character '{playerId}' to {FilePath}");
    }

    private static JObject TryReadExisting()
    {
        if (!SaveExists) return null;
        try
        {
            return JObject.Parse(File.ReadAllText(FilePath));
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveManager: failed to read existing save file — {e.Message}");
            return null;
        }
    }

    public void Load()
    {
        if (!SaveExists) return;

        var data = TryReadExisting();
        if (data == null) return;

        string playerId = identity != null ? identity.PlayerId : null;
        var characters = data["characters"] as JObject;

        // A missing entry for this player's own id means a genuinely new
        // character (or PlayerId genuinely never got set) -- correctly
        // skip RestorePlayer and let Awake()'s own fresh-start defaults
        // stand, same as if no save file existed at all. Chunk 4 (new-
        // vs-returning player logic) is what makes this determination
        // more precise; this is already correct for the common case.
        if (!string.IsNullOrEmpty(playerId) && characters?[playerId] is JObject player)
        {
            RestorePlayer(player);
            LoadedFromSave = true;
        }

        // Chunk 1 (see Save() above): every world key now lives nested
        // under "world" instead of top-level -- everything below reads
        // from this local instead of `data` directly, same restore order
        // as before, just re-pointed at the new location.
        var world = data["world"] as JObject;

        if (villageFlagSpawner != null && world?["villageFlagSpawnTimer"] != null)
            villageFlagSpawner.SpawnTimerSeconds = (float)world["villageFlagSpawnTimer"];

        // RestorePlacedPieces must run before any RestoreWorldObjects<T>
        // call whose T can be player-built via a BuildPiece (StorageBox,
        // GardenPlot/GardenPlot4x4) -- found live, 2026-08-19: a
        // player-built StorageBox doesn't exist yet in a freshly-loaded
        // scene, so restoring its saved inventory/name by SaveId lookup
        // before RestorePlacedPieces has recreated it silently fails
        // (nothing to find), and the later recreation has no saved state
        // to apply, leaving a bare, empty, default-named box. Furnace
        // already had this right (its own RestoreWorldObjects call ran
        // after RestorePlacedPieces) -- that's the correct order, now
        // applied consistently.
        RestorePlacedPieces(world?["placedPieces"] as JArray);

        // Must run before RestoreWorldObjects<StorageBox> below, for the
        // same reason RestorePlacedPieces must run before it (2026-08-21,
        // MVP2B_PLANNING.md item 6) -- a VendorStall's stock box is
        // created dynamically at runtime (VillageVendor.Start(), not
        // baked into the scene), so RestoreWorldObjects<StorageBox>
        // (which only ever restores an *existing* object, never creates
        // one) would silently find nothing to restore into unless this
        // runs first and creates the box under its saved SaveId.
        RestoreVendorStalls(world?["vendorStalls"] as JArray);

        RestoreWorldObjects<StorageBox>(world?["storageBoxes"] as JArray, RestoreStorageBox);
        RestoreWorldObjects<ResourceNode>(world?["resourceNodes"] as JArray, RestoreResourceNode);
        RestoreNpcs(world?["npcs"] as JArray);
        RestoreWorldObjects<GardenPlot>(world?["gardenPlots"] as JArray, RestoreGardenPlot);
        RestoreWorldObjects<GardenPlot4x4>(world?["gardenPlots4x4"] as JArray, RestoreGardenPlot4x4);
        RestoreWorldObjects<Furnace>(world?["furnaces"] as JArray, RestoreFurnace);

        // NavMesh Phase 1 (2026-08-21) only rebakes on live PlayerBuilding/
        // PlayerPieceUpgrade actions -- a wall/door restored here via
        // RestorePlacedPieces above was never baked into the navmesh at all
        // if it predates the current session (the navmesh only reflects
        // whatever existed at Phase 0 bake time, plus anything built live
        // since). Found live: NPCs walked straight through walls that were
        // built in an earlier session and reloaded from a save, with no
        // path-planning anomaly at all -- the agent correctly had no idea
        // the wall existed. One full rebake after every restore is done
        // fixes this the same way a fresh build already does.
        NavMeshRebaker.RequestRebake();
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
            // Player naming (2026-08-22) -- hasBeenNamed tracks whether
            // the free first rename has already been used, so a reload
            // doesn't hand out a second free one.
            ["playerName"] = identity != null ? identity.DisplayName : null,
            ["hasBeenNamed"] = identity != null && identity.HasBeenNamed,
            // Team (2026-08-28) -- per-player, not a separate registry;
            // see PlayerTeam.RestoreTeam's own comment for why this is
            // enough to make reconnecting teammates re-group correctly.
            ["teamId"] = team != null ? team.TeamId : null,
            ["teamName"] = team != null ? team.TeamName : null,
            ["teamRole"] = team != null ? team.Role.ToString() : null,
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

        if (identity != null && data["playerName"] != null)
            identity.RestoreIdentity((string)data["playerName"], (bool)(data["hasBeenNamed"] ?? false));

        if (team != null && data["teamId"] != null
            && Enum.TryParse((string)(data["teamRole"] ?? "Member"), out TeamRole savedRole))
            team.RestoreTeam((string)data["teamId"], (string)data["teamName"], savedRole);

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

    // ---- VendorStall (MVP2B_PLANNING.md item 6, 2026-08-21) ----

    private static JObject CaptureVendorStall(VendorStall stall, SaveId saveId)
    {
        var stockSaveId = stall.Stock != null ? stall.Stock.GetComponent<SaveId>() : null;
        var tillSaveId = stall.Till != null ? stall.Till.GetComponent<SaveId>() : null;
        var villageVendor = stall.GetComponent<VillageVendor>();

        var tillArray = new JArray();
        foreach (CoinType type in System.Enum.GetValues(typeof(CoinType)))
            tillArray.Add(stall.GetTillBalance(type));

        var priceListArray = new JArray();
        foreach (var entry in stall.PriceList)
        {
            if (entry?.item == null) continue;
            priceListArray.Add(new JObject
            {
                ["item"] = ItemDatabase.Instance != null ? ItemDatabase.Instance.IdFor(entry.item) : null,
                ["buyPrice"] = entry.buyPrice,
                ["sellPrice"] = entry.sellPrice,
                ["canBuy"] = entry.canBuy,
                ["canSell"] = entry.canSell,
            });
        }

        return new JObject
        {
            ["saveId"] = saveId.Id,
            ["state"] = new JObject
            {
                // The stock box itself is captured separately, through
                // StorageBox's own normal save path (its contents are
                // just an Inventory, same as any other box) -- this only
                // needs to remember *which* SaveId that box has, so the
                // restore side can recreate it under the same ID before
                // RestoreWorldObjects<StorageBox> looks for it.
                ["stockSaveId"] = stockSaveId != null ? stockSaveId.Id : null,
                // Till is now a real Lockbox (2026-08-22, was a bare
                // int[5]) -- same "remember which SaveId, recreate under
                // it before restoring balances" shape as the stock box
                // above. tillTier is captured directly rather than
                // re-derived from the linked Flag at restore time, since
                // a Flag could in principle be destroyed/changed between
                // save and load.
                ["tillSaveId"] = tillSaveId != null ? tillSaveId.Id : null,
                ["tillTier"] = stall.Till != null ? (int)stall.Till.Tier : (int)CraftTier.Crude,
                ["till"] = tillArray,
                ["priceList"] = priceListArray,
                // Full-refresh countdown (2026-08-22, Ben's ask -- "obviously
                // the timer should be persistent over saves", same call
                // already made for the Village Flag spawn timer) -- only
                // VillageVendor (not every future VendorStall driver) has
                // this concept, so it's null-safe rather than assumed.
                ["nextFullRefreshSeconds"] = villageVendor != null ? villageVendor.NextFullRefreshSeconds : (float?)null,
            },
        };
    }

    // Bespoke, not the generic RestoreWorldObjects<T> -- needs to create
    // the stock box (dynamically spawned at runtime, never baked into the
    // scene) under its saved SaveId before StorageBox's own restore runs,
    // same "find or recreate" shape RestoreNpcs already uses, just for a
    // child object rather than the VendorStall itself (which is always a
    // pre-placed scene object today, so it never needs recreating).
    private void RestoreVendorStalls(JArray data)
    {
        if (data == null) return;

        foreach (var token in data)
        {
            if (!(token is JObject obj) || !(obj["state"] is JObject state)) continue;

            var stall = SaveIdRegistry.Find((string)obj["saveId"])?.GetComponent<VendorStall>();
            if (stall == null) continue;

            string stockSaveId = (string)state["stockSaveId"];
            StorageBox stockBox = !string.IsNullOrEmpty(stockSaveId)
                ? SaveIdRegistry.Find(stockSaveId)?.GetComponent<StorageBox>()
                : null;

            if (stockBox == null && !string.IsNullOrEmpty(stockSaveId))
            {
                var go = new GameObject($"{stall.name} Stock");
                go.transform.SetParent(stall.transform, false);
                // Disabled -- same fix as VillageVendor.EnsureStockBox
                // (2026-08-22): a raycast-hittable internal stock box can
                // shadow the Vendor Stall's own interaction. Kept in sync
                // with that method since this is the save/reload path's
                // own copy of the same box-creation shape.
                var stockCollider = go.AddComponent<BoxCollider>();
                stockCollider.enabled = false;
                stockBox = go.AddComponent<StorageBox>();

                var boxSaveId = stockBox.GetComponent<SaveId>();
                boxSaveId?.GenerateIfMissing();
                boxSaveId?.AssignId(stockSaveId);
            }

            if (stockBox != null) stall.AssignStock(stockBox);

            // Till Lockbox -- same find-or-recreate shape as the stock
            // box above (2026-08-22, till changed from a bare int[5] to a
            // real Lockbox).
            string tillSaveId = (string)state["tillSaveId"];
            Lockbox tillLockbox = !string.IsNullOrEmpty(tillSaveId)
                ? SaveIdRegistry.Find(tillSaveId)?.GetComponent<Lockbox>()
                : null;

            if (tillLockbox == null && !string.IsNullOrEmpty(tillSaveId))
            {
                var go = new GameObject($"{stall.name} Till");
                go.transform.SetParent(stall.transform, false);
                var tillCollider = go.AddComponent<BoxCollider>();
                tillCollider.enabled = false;
                tillLockbox = go.AddComponent<Lockbox>();
                tillLockbox.Configure(state["tillTier"] != null ? (CraftTier)(int)state["tillTier"] : CraftTier.Crude);

                var tillId = tillLockbox.GetComponent<SaveId>();
                tillId?.GenerateIfMissing();
                tillId?.AssignId(tillSaveId);
            }

            if (tillLockbox != null) stall.AssignTill(tillLockbox);

            if (state["till"] is JArray tillArray)
            {
                var types = (CoinType[])System.Enum.GetValues(typeof(CoinType));
                for (int i = 0; i < tillArray.Count && i < types.Length; i++)
                    stall.AddTillBalance(types[i], (int)tillArray[i]);
            }

            if (state["priceList"] is JArray priceListArray)
            {
                var entries = new List<VendorPriceEntry>();
                foreach (var priceToken in priceListArray)
                {
                    if (!(priceToken is JObject priceObj)) continue;
                    var item = ItemDatabase.Instance != null ? ItemDatabase.Instance.Find((string)priceObj["item"]) : null;
                    if (item == null) continue;

                    entries.Add(new VendorPriceEntry
                    {
                        item = item,
                        buyPrice = (int)(priceObj["buyPrice"] ?? 0),
                        sellPrice = (int)(priceObj["sellPrice"] ?? 0),
                        canBuy = (bool)(priceObj["canBuy"] ?? true),
                        canSell = (bool)(priceObj["canSell"] ?? true),
                    });
                }
                stall.SetPriceList(entries.ToArray());
            }

            if (state["nextFullRefreshSeconds"] != null)
                stall.GetComponent<VillageVendor>()?.RestoreFullRefreshTimer((float)state["nextFullRefreshSeconds"]);
        }
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
        var dialogue = npc.GetComponent<NPCDialogue>();
        var gathering = npc.GetComponent<NPCGathering>();
        var guarding = npc.GetComponent<NPCGuarding>();
        var seekFlag = npc.GetComponent<NPCSeekFlag>();

        JObject seekObj = null;
        if (seekFlag != null && seekFlag.IsActivelySeeking)
        {
            var flagSaveId = seekFlag.TargetFlag.GetComponent<SaveId>();
            if (flagSaveId != null)
            {
                seekObj = new JObject
                {
                    ["flagSaveId"] = flagSaveId.Id,
                    ["hasArrived"] = seekFlag.HasArrived,
                    ["stickAroundSecondsRemaining"] = seekFlag.StickAroundSecondsRemaining,
                };
            }
        }

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
                // Auto-assigned at spawn (or player-renamed since) --
                // captured so a recreated-on-load NPC doesn't re-roll to a
                // different random name/gender every reload (2026-08-17,
                // BUGS_AND_ENHANCEMENTS.md "NPC identification").
                ["name"] = dialogue != null ? dialogue.DisplayName : null,
                ["isFemale"] = dialogue != null && dialogue.IsFemale,
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
                // Both leashes (2026-08-18) -- found live not persisting
                // (BUGS_AND_ENHANCEMENTS.md): neither NPCGathering's
                // work-range leash nor NPCGuarding's patrol radius (new
                // this same fix, replacing the old VillageFlagRevealRadius
                // tier-scale reuse) were ever captured, so both silently
                // reset to their component defaults on every reload.
                ["maxRangeFromDeposit"] = gathering != null ? gathering.MaxRangeFromDeposit : (float?)null,
                ["patrolRadius"] = guarding != null ? guarding.PatrolRadius : (float?)null,
                // NPCSeekFlag state (2026-08-21) -- see that component's
                // own comment for the full reasoning. Null (not just
                // omitted) whenever this NPC isn't actively seeking a
                // Flag right now (already hired, or a pre-placed hire
                // that never had this component active) -- restoring
                // seek state onto an NPC that shouldn't have any would be
                // its own new bug.
                ["seekFlag"] = seekObj,
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
                bool isFemale = (bool)(state["isFemale"] ?? false);
                var prefab = villageFlagSpawner != null ? villageFlagSpawner.HireableNpcPrefab(isFemale) : null;
                if (prefab == null) continue;

                var position = ParseVector3(state["position"] as JObject);
                var instance = Instantiate(prefab, position, Quaternion.identity);
                NetworkSpawnHelper.SpawnIfNetworked(instance);
                npc = instance.GetComponent<NPCHiring>();
                if (npc == null) continue;

                // Same defensive GenerateIfMissing()-before-AssignId() as
                // RestorePlacedPieces, cheap insurance against the same
                // ArgumentNullException class of crash even though NPC
                // prefabs are Instantiate()d whole (Reset() reliably fires
                // at least once there, unlike PlacedPiece's AddComponent-
                // after-Instantiate pattern) so this path hasn't actually
                // shown the crash.
                var npcSaveId = instance.GetComponent<SaveId>();
                npcSaveId?.GenerateIfMissing();
                npcSaveId?.AssignId(saveId);
            }

            RestoreNpc(npc, state);
        }
    }

    private static void RestoreNpc(NPCHiring npc, JObject state)
    {
        if (state == null) return;

        // Only reapplies if a name was actually saved -- an old save file
        // from before this fix has no "name" key, and Configure's own
        // null/whitespace guard leaves whatever the fresh Instantiate
        // already carries untouched in that case rather than blanking it.
        if (state["name"] != null)
            npc.GetComponent<NPCDialogue>()?.Configure((string)state["name"], (bool)(state["isFemale"] ?? false));

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

        if (state["maxRangeFromDeposit"] != null)
        {
            var gathering = npc.GetComponent<NPCGathering>();
            if (gathering != null) gathering.MaxRangeFromDeposit = (float)state["maxRangeFromDeposit"];
        }

        if (state["patrolRadius"] != null)
        {
            var guarding = npc.GetComponent<NPCGuarding>();
            if (guarding != null) guarding.PatrolRadius = (float)state["patrolRadius"];
        }

        if (state["seekFlag"] is JObject seekState)
        {
            var seekFlag = npc.GetComponent<NPCSeekFlag>();
            var flag = seekState["flagSaveId"] != null
                ? SaveIdRegistry.Find((string)seekState["flagSaveId"])?.transform
                : null;
            if (seekFlag != null && flag != null)
            {
                seekFlag.RestoreSeekState(
                    flag,
                    (bool)(seekState["hasArrived"] ?? false),
                    (float)(seekState["stickAroundSecondsRemaining"] ?? 0f));
            }
        }
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
        ["autoRun"] = campfire.AutoRunEnabled,
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
                NetworkSpawnHelper.SpawnIfNetworked(instance);

                piece = instance.GetComponent<PlacedPiece>();
                if (piece == null) piece = instance.AddComponent<PlacedPiece>();
                piece.Piece = buildPiece;

                // GenerateIfMissing() before AssignId() -- confirmed live
                // 2026-08-17: AddComponent<PlacedPiece> here triggers
                // RequireComponent's auto-add of SaveId, whose Reset()
                // doesn't reliably fire for a runtime AddComponent (same
                // gotcha the original placement-time fix covers). Without
                // this, AssignId's internal Unregister call hit a null Id
                // and threw ArgumentNullException, silently aborting the
                // rest of this iteration -- including the villageName
                // restore below, which is why a renamed Flag came back
                // with its default name after this exact crash.
                var saveIdComponent = instance.GetComponent<SaveId>();
                saveIdComponent?.GenerateIfMissing();
                saveIdComponent?.AssignId(saveId);
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
            state["output"] as JArray,
            (bool)(state["autoRun"] ?? false));
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
