# Multiplayer Planning

Exploration doc for converting Gridless from single-player to dedicated-server
multiplayer using Mirror Networking, imported into the project 2026-08-13.
**Nothing here is built or decided as a locked architecture yet** — this is an
audit of the current codebase plus a map of what Mirror-ifying it would
actually involve, written so whoever picks this up (human or Claude session)
doesn't have to re-derive the shape of the problem from scratch.

Mirror was picked over PurrNet (the other option evaluated the same session)
primarily on one concrete point: Mirror's stated Unity compatibility range
(2019/2020/2021/2022 LTS and 6000.1+) safely covers Gridless's pinned
`6000.3.21f1` (see `ProjectSettings/ProjectVersion.txt`), while PurrNet's
stated minimum (`6000.5.4f1`) is newer than the engine version this project
is actually pinned to. Both are free/open-source and both explicitly support
dedicated + headless-Linux server builds, which is the architecture this game
needs — see design vision below.

## 0. The existing design vision (already written, not new)

`docs/design-brief.md` item 6, quoted in full — this is the target, not
something being decided here:

> **Multiplayer: dedicated servers, each a full replica-Earth copy.** Like
> Valheim/Rust/ARK — anyone can host or rent a server, each running its own
> persistent copy of Gridless. A handful to dozens of players join a given
> server, each spawning near their own real-world (IP-geolocated) location
> within that server's world. The server is the authoritative simulation for
> NPCs, jobs, and settlement growth — they keep working even while a given
> player is offline. This was chosen over a single global shared MMO (more
> meaningful at planetary scale, but needs real backend/hosting/moderation
> investment beyond a solo/small effort) and over session-based play with no
> persistence (conflicts with the city-growth and autonomous-NPC pillars
> above).

Two load-bearing implications worth pulling out explicitly:
- **Server-authoritative, not peer-to-peer.** Matches Mirror's whole design
  center of gravity — this is what Mirror is built for.
- **NPCs/jobs keep running while players are offline.** This means the
  server process is the actual source of truth for world state at all
  times, not just a relay between connected clients — reinforces that
  persistence (section 4) isn't optional scope, it's load-bearing for the
  stated design.

## 1. Current state (audit, 2026-08-13; re-audited 2026-08-19)

Confirmed directly against the codebase before any networking design:

- **Re-audit finding (2026-08-19), the actual reason Phase 0 finally
  started**: Mirror sat completely untouched for the 6 days between import
  and this re-audit — zero `using Mirror` references anywhere, zero
  commits besides the import itself. In that same window the codebase this
  doc will eventually need to convert grew **~50%**: 115→177 total
  scripts, 32→48 `PlayerXXX.cs`, 12→27 NPC scripts (15 of which now have
  their own `Update()` loop, up from 5). Every day this stays unstarted,
  the conversion gets more expensive, not less — a real cost of continuing
  to defer it, not just an abstract risk.
- **A genuinely new complication found in this same re-audit**:
  persistence (`SaveManager`, shipped 2026-08-17, see
  `BUGS_AND_ENHANCEMENTS.md`) is no longer the "greenfield work, build it
  right the first time" this doc originally framed it as in section 2. It
  now exists, works, and is wired into 28 files — as a single omnibus JSON
  file with no per-player keying and no world/character split, exactly as
  single-player-shaped as you'd expect. The persistence phase of a real
  conversion is now "restructure a working, depended-upon system," a
  strictly worse risk profile than building on nothing.
- **115 total scripts in `Assets/Scripts/`** at the time of the original
  audit (now 177 — see above), of which **32 were `PlayerXXX.cs`**
  (PlayerInventory, PlayerVitals, PlayerCrafting, PlayerBuilding,
  PlayerMagic, PlayerCombat, PlayerEquipment, ...) and **12 were NPC/AI**
  (NPCMining, NPCWander, NPCHiring, NPCDialogue, NPCEncumbrance, ...).
  Every one of these currently assumes exactly one local player exists.
- **Hard single-player assumptions via scene-scanning, mostly not a
  singleton pattern** — 4 `public static X Instance` singletons exist now
  (`ItemDatabase`/`SkillDatabase`/`NPCJobDatabase`/`BuildPieceDatabase`,
  all added after this doc's original 2026-08-13 audit, which found zero).
  Checked and confirmed benign for multiplayer: all 4 are read-only
  `ScriptableObject` data registries (item/skill/recipe lookups), identical
  on server and every client — nothing to untangle, unlike a genuine
  per-player singleton would be. Instead, individual scripts reach for
  "the" player directly:
  - `Campfire.cs` and `HostileCreature.cs` both call
    `FindFirstObjectByType<PlayerVitals>()` to find their one target/who to
    warm.
  - `PlayerBuilding.cs`, `PlayerCrafting.cs`, `NPCMining.cs`,
    `BuildSocket.cs` all use `FindObjectsByType<T>()` to scan the whole
    scene for sockets/surfaces/nodes every time they need one — works fine
    single-player, doesn't know anything about "which player."
- **Only one class maintains a live registry**: `StorageBox.Active` (a
  static `List<StorageBox>`, added/removed in `OnEnable`/`OnDisable`,
  queried via `FindNearby`). Campfire used to have the identical pattern
  (`Campfire.Active`/`FindNearby`) but it was deleted this session
  (2026-08-13) once nothing called it anymore — worth knowing since it's a
  precedent that existed and got removed, not something to reinvent from
  nothing if a networked version wants a maintained-registry approach
  again.
- **22 files with `OnGUI()`** — every screen in the game (InventoryScreen,
  CampfireScreen, PlayerMenuScreen, CraftingScreen, BuildScreen,
  MagicScreen, SkillsScreen, LockboxScreen, BankScreen, GameMenuScreen,
  AdminSpawnScreen, NPCHiringScreen, NPCJobScreen, VitalsBarHUD, and more)
  is legacy IMGUI. **This turns out not to be a multiplayer problem at
  all** — `OnGUI()` is inherently per-client already (it only ever draws on
  whichever process it's running in), so nothing here needs to change
  structurally for networking. What changes is what data these screens
  *read from* (see section 2) — they currently read local C# fields
  directly; under Mirror they'd read from synced state instead. The
  rendering code itself is largely fine as-is.
- **Stale as of the 2026-08-19 re-audit — persistence now exists.** At the
  original 2026-08-13 audit, zero save/load persistence existed anywhere.
  It shipped 2026-08-17 (`SaveManager.cs`, single JSON file at
  `Application.persistentDataPath`, no autosave, manual Save button) and is
  now wired into 28 files. See this section's opening bullet above — this
  is good news for single-player and a genuine complication for
  multiplayer at the same time: the *concept* is proven, but the *shape*
  (one omnibus file, no per-player keying) will need real restructuring,
  not just a sync hookup, once a "world" vs. "N player characters"
  distinction actually matters.
- **World-state interactable prefabs are already the right conceptual
  shape** — StorageBox, Campfire, Lockbox, Furnace/Anvil surfaces,
  ResourceNode, ChoppableTree, BerryBush, HostileCreature all already exist
  as independent, self-contained prefabs with their own state (an
  `Inventory`, a lit/unlit bool, health, etc.). This is good news: Mirror
  networks state by attaching `NetworkIdentity`/`NetworkBehaviour` to
  exactly this kind of object, so the *shape* of "a world object with its
  own inventory that players interact with" doesn't need to be redesigned
  — it needs to be made server-authoritative, which is a different, more
  contained problem than a redesign.

## 2. What Mirror actually requires, mapped onto this codebase

Brief primer (for whoever reads this without prior Mirror context): Mirror's
core building blocks are `NetworkManager` (spawns/manages
connections/scenes), `NetworkBehaviour` (a `MonoBehaviour` subclass that can
own networked state), `SyncVar`/`SyncList`/`SyncDictionary` (fields that
auto-replicate server→client), and RPCs — `[Command]` (client asks server to
do something, server validates), `[ClientRpc]` (server tells all clients
something happened), `[TargetRpc]` (server tells one specific client
something).

Mapped onto Gridless's actual systems:

- **Player-authoritative gameplay (the 32 `PlayerXXX.cs` scripts) is the
  single biggest chunk of work.** Every action currently triggered by local
  input and applied directly (an `Inventory.AddItem` call, a crafting
  attempt, dropping an item, equipping gear) needs to become: client sends
  a `[Command]` describing the intent → server validates it (does the
  player actually have this item? are they in range? do they have the
  skill?) → server applies the mutation to server-owned state → server
  replicates the result back (`SyncVar`/`SyncList` on the `Inventory`
  itself, or a `[ClientRpc]` for one-off effects). This is the same shape
  for all 32 scripts, but it's still 32 scripts' worth of call sites to
  convert — not a one-time framework change, a systematic pass.
- **World objects (StorageBox, Campfire, Lockbox, ...) each need a
  `NetworkIdentity` + a `NetworkBehaviour` wrapping their `Inventory` as
  synced state**, and their E-key interaction becomes a `[Command]` to the
  server (validated for range/ownership) instead of a direct local method
  call. Most of these don't move once placed, so they don't need
  `NetworkTransform` — only the placement moment itself (`PlayerBuilding`)
  needs to become a server-validated spawn.
- **The player character itself (`FirstPersonController`) needs a movement
  authority model** — Mirror's default is client-authoritative movement via
  `NetworkTransform` (simple, but a client can lie about its position;
  fine for a "trusted enough" small dedicated server, less fine if
  cheating matters). Server-authoritative movement with client-side
  prediction/reconciliation is the more robust alternative but
  meaningfully more work. **Not decided here** — see open questions.
- **NPCs (`NPCMining`, `NPCWander`, `NPCHiring`, `NPCDialogue`,
  `NPCEncumbrance` — the 5 of the 12 NPC scripts that have their own
  `Update()` loop) become server-only simulation**, replicated to observing
  clients. This is a clean, natural fit for Mirror's server-authoritative
  model, and it's also exactly what the design brief's "server keeps NPCs/
  jobs running while a player is offline" goal requires — the dedicated
  server process runs this regardless of whether any client is even
  connected, which a client-side `Update()` loop fundamentally can't do.
- **Persistence is not a Mirror feature** — Mirror handles transport/state-
  sync between server and connected clients, not saving world state to
  disk between server restarts. This is separate, greenfield work no
  matter which netcode library got picked (see section 1's note on this
  already being a known gap). It becomes higher-priority once there's a
  real dedicated server process, since a crash with no persistence now
  loses a whole shared world, not just one player's local save.

## 3. A suggested phased approach (not committed, a starting proposal)

1. **Infrastructure spike — built 2026-08-19, v0.3.145-dev, live-tested
   and confirmed working 2026-08-22.** `Assets/Scenes/NetworkSpike.unity`
   (deliberately isolated — not in `EditorBuildSettings` day-to-day, not
   touching `TestScene.unity` or any of the 48 `PlayerXXX.cs` scripts) +
   `NetworkSpikePlayer.prefab`, a minimal `NetworkIdentity`/
   `NetworkTransformReliable` capsule with a new `NetworkSpikeMovement.cs`
   (not `FirstPersonController` — answering the movement-authority
   question safely needed a throwaway mover, not the real 48-script Player
   stack). **Real two-process test run by Ben (2026-08-22)**: temporarily
   added `NetworkSpike` to the Scene List via Build Profiles (Unity 6's
   renamed Build Settings), built a standalone Windows player, ran it
   alongside an Editor Play-mode Host (Mirror KCP transport, port 7777) —
   confirmed via `NetworkManagerHUD`'s Host/Client buttons on each side.
   Both capsules were visible and moved live in both windows regardless of
   which process's input drove the movement — `NetworkTransformReliable`
   with `syncDirection = ClientToServer` genuinely replicates in both
   directions, not just the direction its name implies. Build Profiles'
   Scene List was restored to `TestScene`-only immediately after. This is
   the first real confirmation Mirror's transport/sync actually works in
   this project outside of a clean compile.
2. **One pilot world object, fully networked — built and live-tested
   2026-08-22.** Not the real `StorageBox.cs`/`Inventory` (same "throwaway
   pilot, not the real stack" call Phase 0's `NetworkSpikeMovement.cs`
   already made for movement) — a new `NetworkStorageBoxSpike.cs`
   (`NetworkBehaviour`, a `SyncList<string> items` standing in for a real
   Inventory's slot list) placed in `NetworkSpike.unity`, interacted with
   via two new keybinds on `NetworkSpikeMovement.cs` (E = add, R = remove
   top), each routed through a `[Command]` that validates a 3m range
   server-side before mutating the box. **Confirmed live by Ben**: added
   and removed items from both the Host and Client windows, saw the same
   updated list reflected on both sides every time — genuinely shared
   server-owned state, not two independent local lists. This validates the
   "world object with synced Inventory, server-validated Command
   interaction" pattern once, cheaply, before repeating it for real across
   StorageBox, Campfire, Lockbox, and every other interactable in Phase 3.
3. **Player-authoritative gameplay.** The big one — systematically convert
   the 32 (now 48) `PlayerXXX.cs` scripts' local-mutation call sites to the
   Command/validate/replicate shape. Likely the single largest phase by
   raw effort. **Scope-shape open question resolved 2026-08-22, no longer
   open**: no feature flag, no permanently-maintained dual single-player/
   networked code path. A solo session becomes "you host alone" —
   Mirror's Command/SyncVar plumbing works identically whether 0 or N
   remote clients are connected, and this is also the only way the design
   brief's "NPCs/jobs keep running while offline" goal holds for a solo
   host too. Split into 5 ordered sub-phases, each its own live-test
   checkpoint before the next starts (same discipline as every other
   major system build in this project):
   1. **Bootstrap — attempted and fully reverted 2026-08-22, two real
      problems found live.** `Player` (75 components — the single scene-
      baked GameObject the entire game runs through) was converted into
      `Assets/Prefabs/Player.prefab` via `SaveAsPrefabAssetAndConnect`,
      then given `NetworkIdentity` + `NetworkTransformReliable`, alongside
      an inert `NetworkManager` + `KcpTransport` added to
      `TestScene.unity`. **Problem 1**: Mirror deactivates any
      scene-placed `NetworkIdentity` object until a server actually spawns
      it, and `TestScene` had no way to start one (no
      `NetworkManagerHUD`) — the entire Player hierarchy, camera
      included, silently went inactive the instant Play mode started
      (blank "No cameras rendering" screen, a downstream `PlayerTool` NRE
      from `Awake()` never running on the now-disabled object). This part
      was root-caused live with Ben (confirmed via the Hierarchy's
      grayed-out inactive-object indicator) and `NetworkIdentity`/
      `NetworkTransformReliable` were reverted, restoring the camera. **A
      second problem then surfaced even in the reverted state**: bare-
      handed combat stopped registering left-click entirely, and E-key
      interaction started resolving to an unexpected "craft" progress-bar
      prompt while aiming at a Wolf (which doesn't implement
      `IInteractable` at all, ruling out that specific target as the real
      raycast hit). `git diff --stat` on `TestScene.unity` showed the
      saved scene had shrunk by ~128KB from the `SaveAsPrefabAssetAndConnect`
      conversion alone — far more change than adding/removing two small
      components could explain, and Force Binary serialization means
      that diff can't be inspected directly (see `CLAUDE.md`'s gotcha on
      this). Rather than debug forward from a scene already shown to
      silently corrupt something once, **the entire experiment was
      reverted via `git checkout -- Assets/Scenes/TestScene.unity`** plus
      deleting the untracked `Player.prefab` — a full rollback to the
      last-committed state, not a partial keep.
      **Real conclusion for next time**: converting the single 75-component
      scene-baked Player object into a prefab via
      `PrefabUtility.SaveAsPrefabAssetAndConnect` is not safe to treat as
      a small, inert first step the way it was here — something about
      that conversion silently altered scene data beyond the two
      Mirror components added afterward, on an object this large and
      cross-referenced. A future attempt at this same slice should
      either: (a) build and test the auto-host-on-load mechanism *first*
      against a much smaller, isolated test object (not the real 75-
      component Player) before ever touching the real Player prefab, or
      (b) go component-by-component/reference-by-reference through what
      `SaveAsPrefabAssetAndConnect` actually changed on an object this
      size before trusting it, rather than a single one-shot conversion
      + live-test.

      **Retried the same night, isolated to just the prefab conversion
      alone (no `NetworkIdentity` this time) — succeeded, with the real
      bug found and fixed.** The exact same `PlayerTool` NRE reproduced
      even with zero Mirror components involved, proving the earlier
      "Mirror deactivation" theory wasn't the whole story. Root cause:
      `PlayerBodyModel.Awake()` called `ApplyGender()`, which calls
      `RefreshAnchor()` on 11 other components (`PlayerTool`,
      `PlayerBackpack`, `PlayerBoot`, ...) that only work once each one's
      own `Awake()` has already populated its fields — an implicit
      ordering dependency on component-list position that Unity doesn't
      guarantee and that `SaveAsPrefabAssetAndConnect` evidently disturbs.
      Fixed by moving the initial `ApplyGender(isMale)` call from
      `Awake()` to `Start()` (Unity guarantees every component's
      `Awake()` on a GameObject completes before any `Start()` runs,
      regardless of component order) — a real, standalone bugfix, not a
      multiplayer-specific workaround. Two apparent "second regressions"
      during debugging both turned out to be false alarms, not new bugs:
      the odd "craft" progress bar on a live Wolf is pre-existing
      `SkinnableCreature`/`IInteractable` behavior
      (`HostileCreature : SkinnableCreature`), and "left-click doesn't
      damage the Wolf" was `PlayerCombat` correctly refusing to punch
      while a Bow was equipped (`IsHoldingRangedWeapon()`) — confirmed
      via temporary debug logging added to `PlayerCombat.cs` (removed
      after diagnosis) that traced the exact gate each time, then a
      clean live kill (3 real hits, 9 damage each, confirmed straight
      from `Editor.log`) once bare-handed. The earlier "~128KB scene
      shrink = corruption" read from attempt #1 was also a red herring
      — that shrink is normal/expected (data moves from the scene file
      into the new prefab file on disk, not lost). **Bootstrap's prefab-
      conversion step is now genuinely done and confirmed live**:
      `Assets/Prefabs/Player.prefab` exists, connected, 75/75 components,
      combat and interaction both confirmed working.

      **Auto-host-on-load built and live-confirmed, same session.**
      `NetworkAutoHost.cs` — attached to the `NetworkManager` GameObject,
      calls `StartHost()` from `Start()` the instant the scene loads if
      neither `NetworkServer.active` nor `NetworkClient.active` is already
      true. This is what makes the "solo session = host alone" scope-shape
      decision actually real rather than aspirational — pressing Play now
      genuinely runs through Mirror's server/client loop underneath, with
      zero change to what the player sees or does. Live-confirmed clean via
      `Editor.log`: `NetworkServer.active=True, NetworkClient.active=True`
      with no warnings (also wired `NetworkManager.transport` explicitly to
      clear a harmless "no Transport assigned" warning), then a full
      gameplay spot-check (Vendor Stall screen, a Restoration wish tier-up)
      confirmed nothing regressed.

      **`NetworkIdentity`/`NetworkTransformReliable` re-added to the real
      Player and live-confirmed — sub-phase 1 (Bootstrap) is done.** The
      hypothesis held: with `NetworkAutoHost` guaranteeing a server exists
      before Mirror ever needs to spawn scene objects, adding
      `NetworkIdentity` back onto Player this time did **not** reproduce
      the original deactivation bug. Live-confirmed with a full gameplay
      pass — camera rendered normally, HUD worked, opened a Furnace,
      killed a Wolf bare-handed, `Editor.log` showed
      `NetworkServer.active=True, NetworkClient.active=True` with no
      errors. One minor unrelated note from that pass, logged separately
      in `BUGS_AND_ENHANCEMENTS.md` rather than blocking here: the Wolf
      didn't fight back when attacked. `Player` is now a genuine
      `NetworkIdentity`-carrying, `NetworkTransformReliable`-synced object
      spawned/kept alive through Mirror's real server/client loop, with
      solo play behaviorally unchanged.

      **A real regression surfaced right after, caught by Ben's own sharp
      observation ("the wolf never moved towards me") and fixed same
      session.** `HostileCreature.Start()`, `Campfire.Start()`,
      `PreyWander.Awake()`, and `ResourceNode.Start()`'s disguised-shield-
      wearer lookup all shared the identical fragile pattern: a **one-shot**
      `FindFirstObjectByType<PlayerVitals>()` (or
      `PlayerMiningFaceShield`) lookup, caching the result forever. Now
      that Player carries a `NetworkIdentity`, it can be transiently
      deactivated (Mirror hides unspawned scene `NetworkIdentity` objects
      until `NetworkAutoHost`'s own `Start()` calls `StartHost()`) at the
      exact moment one of these other objects' own `Awake()`/`Start()`
      runs — Unity doesn't guarantee cross-object execution order, so
      whichever ran first could permanently miss the player and never find
      it again. Fixed uniformly across all four: a `ResolvePlayerTarget()`/
      `ResolveShieldWearer()` helper that's a no-op once already resolved,
      called once at startup and again lazily from `Update()` whenever the
      cached reference is still null — cheap in the common case, self-
      healing in the race case. Live-confirmed fixed: a Wolf genuinely
      attacked and damaged the player, and a lit Campfire genuinely warmed
      them. **This is a real, generalizable lesson for the rest of
      Phase 3**: any script anywhere in the codebase with a one-shot
      cached `FindFirstObjectByType<PlayerXXX>()` lookup is a latent
      landmine now that Player's activation timing is no longer
      instantaneous-at-scene-load — worth a deliberate sweep before
      sub-phase 2 starts, not just reacting to the next one a playtest
      happens to surface.

   2. **Inventory + Equipment — first slice built and live-confirmed
      2026-08-22.** `PlayerInventory.cs` converted from `MonoBehaviour` to
      `NetworkBehaviour` — deliberately isolated to just the base-class
      change, no new synced state yet, matching the same "prove the
      foundation before building on it" discipline sub-phase 1 used.
      Real complication surfaced before writing any code: Mirror doesn't
      natively sync a `ScriptableObject` reference like `ItemDefinition`
      the way it syncs primitives — a genuinely synced Inventory needs a
      custom `SyncList` serializer that resolves items by string ID, the
      same by-ID pattern `SaveManager`/`ItemDatabase.Find(id)` already use
      for persistence, not a trivial mechanical step. Confirmed all 36
      `GetComponent<PlayerInventory>()` call sites are simple reads on the
      Player object (already has `NetworkIdentity`) — no dynamic
      `AddComponent<PlayerInventory>()` anywhere that could break.
      Live-tested: Inventory screen renders normally, picking up a Skill
      Book worked, dropping a Stick worked. One non-reproducible oddity
      (a specific already-in-inventory Skill Book vanished shortly after
      being dropped — no Console error, didn't recur with a different
      book) logged as a minor, likely-unrelated note in
      `BUGS_AND_ENHANCEMENTS.md` rather than blocking here. **Still not
      done**: the actual `SyncList`-backed Inventory data + the custom
      ItemDefinition-by-ID serializer + converting real mutation call
      sites (pickup, drop, equip) to the Command/validate/replicate shape
      — this slice only proved the base class conversion is safe, it
      hasn't networked anything yet.

      **`PlayerEquipment.cs` converted the same way, same session.**
      `MonoBehaviour` → `NetworkBehaviour`, base-class change only — no
      dynamic `AddComponent<PlayerEquipment>()` found anywhere, same "safe
      to convert" profile as `PlayerInventory`. Live-tested the full
      cycle: equipped an Axe (Left Hand slot updated), actually chopped a
      tree with it (confirms the equipped item is genuinely usable, not
      just visually shown), then unequipped it back into a worn Jeans'
      nested inventory slot. Both `PlayerInventory` and `PlayerEquipment`
      are now `NetworkBehaviour` with solo play fully unaffected.

      **The `SyncList` serializer is now built and live-confirmed, same
      session.** `PlayerInventory.SyncedInventorySlot` (a
      `[Serializable] struct { string itemId; int count; }`) +
      `SyncList<SyncedInventorySlot> syncedSlots`, resolved by string ID
      the same way `SaveManager`/`ItemDatabase.Find(id)` already do for
      persistence — Mirror can't natively sync a `ScriptableObject`
      reference. **Deliberately excludes equipment-carrying slots** (a
      worn Backpack/Canteen/etc. — a live GameObject+component, not just
      data) — same complexity boundary `SAVE_LOAD_PLANNING.md` already
      drew for persistence v1's "full recursive nested-equipment capture."
      Server-side, polled from `Update()` (a change-signature string
      comparison, not hooked into every mutation site — `Inventory` isn't
      instrumented with change notifications and dozens of scripts mutate
      it directly through the exposed `Inventory` property, not just
      through `AddItem`/`RemoveItem`) rather than fully efficient, but
      correct and low-risk for a first slice. Live-verified with a
      temporary debug `OnGUI` (removed after confirming): picked up a
      Potato Seed (equipment-routed Skill Book correctly excluded),
      picked up Sticks into an equipped Backpack's own separate
      `Inventory` (correctly NOT reflected, confirming the scope
      boundary holds), moved them into the main inventory (correctly
      then appeared, both `PotatoSeed x1` and `Stick x11`, right counts).
      **Real gap found and fixed while designing the first Command,
      same session: Player had no client connection authority at all.**
      `Player` is a server-owned scene `NetworkIdentity` (`autoCreatePlayer`
      is off, since Player pre-exists rather than needing to spawn fresh
      from a prefab) — without an explicit `NetworkServer.AddPlayerForConnection`
      call, no connection ever owns it, so every `[Command]` on any Player
      `NetworkBehaviour` would fail with an authority error. Fixed via a
      new `GridlessNetworkManager : NetworkManager` subclass (swapped onto
      the scene's `NetworkManager` GameObject, transport/settings
      preserved) overriding `OnServerReady` to call
      `AddPlayerForConnection` for the existing scene Player. **First
      attempt used `OnServerConnect` instead and hit a real, informative
      error** — `NetworkClient can't AddPlayer before being ready` —
      `AddPlayerForConnection` needs the client's ready-handshake to have
      completed first, which hasn't happened yet at raw-connect time even
      in host mode; `OnServerReady` is the correct hook, confirmed via
      debug logging showing `identity.isOwned` flipping `False` → `True`
      exactly once, at the right point.

      **First real `[Command]` built and live-confirmed end to end**:
      `PlayerInventory.RequestAddItem`/`CmdAddItemById` — client resolves
      an item to its string ID, calls the Command, server re-resolves the
      ID and applies the real `AddItem`. Verified via a temporary debug
      keybind (removed after confirming): Console showed the full chain in
      order — client-side request logged, then the Command's own
      server-side log with the correct item/quantity/leftover. This is the
      actual Command/validate/apply shape sub-phase 2 needs, now proven
      correct on the real `PlayerInventory`, not a throwaway pilot.

      **`PlayerEquipment`'s own synced state built and live-confirmed,
      2026-08-23.** Same by-string-ID `SyncList` pattern as
      `PlayerInventory`, but shaped differently — a real complication
      found before writing code: unlike `Inventory` (mostly plain
      stackable slots, only a few equipment-carrying ones excluded from
      sync), an equip slot is *virtually always* equipment-carrying by
      definition, so excluding those the same way would sync nothing at
      all. Fixed by syncing *which item* occupies each named slot (what's
      visibly worn where — `SyncedEquipmentSlot { slotName, itemId }`,
      one entry per configured slot, empty `itemId` string distinguishing
      "confirmed empty" from "no data yet"), while still not syncing the
      equipped object's own deep state (a worn Backpack's nested
      contents, a Canteen's fill level) — the same complexity boundary
      as before, just applied correctly to this class's actual slot-
      shaped data. Live-confirmed via a temporary debug `OnGUI` (removed
      after confirming): correctly showed the real starting gear (Face/
      Chest/Back/Waist/Leg/Feet all populated correctly on scene load),
      then correctly tracked a live equip → unequip → re-equip cycle.

      **Still not done**: only one narrow proof-of-concept Command exists
      (`PlayerInventory.CmdAddItemById`) — the real remaining work is
      converting `AddItem`/`RemoveItem`'s actual callers (`Pickup.cs`'s
      world pickup flow, `PlayerCrafting`'s material consumption, and the
      many other direct-mutation call sites) to route through Command-
      validated methods instead of local calls.

      **Real equip/unequip Command built and live-confirmed, 2026-08-23.**
      `PlayerInventory.RequestMove`/`CmdMoveItem` — moves an item between
      the main inventory and a named `PlayerEquipment` body slot (either
      direction) by reusing `InventoryTransfer.Move` server-side rather
      than reimplementing its already-correct equipment-aware logic
      (carrying an equipped instance's own reference across, not just its
      item+count). Containers are addressed by a simple string key
      (`"main"` or a `PlayerEquipment` slot name) since a Command can't
      carry a raw `Inventory` reference over the wire. Live-confirmed via
      a temporary debug keybind (removed after confirming): moved a Stick
      main→Left Hand, watched it actually appear in the Equipment panel;
      moved it back, watched it correctly re-stack in the main inventory.
      **Deliberately not wired into the real `InventoryScreen.cs` UI** —
      that screen's live drag-and-drop also moves between many other
      container types this narrower string-key scheme doesn't cover
      (Backpack, Furnace zones, NPC cargo), and rewiring its actively-used
      code is a separate, larger, riskier task than this proof-of-concept
      justified touching today. Scope boundary: only covers
      `PlayerEquipment`'s own named body slots — a worn Backpack/Belt's
      own nested `Inventory` is a separate object, out of scope here.

      **Real equippable-instance pilot built and live-confirmed,
      2026-08-23.** Attempting to wire the plain `RequestMove` Command
      into the actual UI surfaced a real, deeper finding first: real worn
      gear (Backpack, Canteen, Tool, ...) doesn't move through
      `InventoryTransfer.Move` at all — `InventoryScreen.cs` dispatches
      per-item-type to ~11 dedicated carrier components
      (`BackpackCarrier.Equip`, `ToolCarrier.EquipTo`, ...), each managing
      its own physical `SetCarried()` re-parenting directly. Those
      methods take a reference to the actual equippable **instance** (a
      live Component/GameObject), and a `[Command]` can only receive a
      `GameObject`/`NetworkIdentity` argument for an object that's
      genuinely spawned on the network — something no equippable
      instance or world Pickup had. So world-pickup networking and real
      equip/unequip Commands turned out to be the same underlying
      blocker, not two separate tasks: physical item instances need real
      `NetworkIdentity` + `NetworkServer.Spawn` before either can work.

      Rather than attempt this across every equippable type, built and
      proved the pattern on **one single prefab**:
      `MasterworkLeatherBackpackPickup.prefab` (chosen since Backpack
      already turned out to have 10 separate tier/material prefab
      variants — even "one type" isn't one asset in this game).
      `NetworkIdentity` added to that one prefab and registered in
      `NetworkManager.spawnPrefabs`; `PlayerDropping.SpawnPickup` now
      calls `NetworkServer.Spawn()` after `Instantiate()` when the
      spawned object carries a `NetworkIdentity` (guarded by
      `NetworkServer.active`), so an Admin-Spawned or dropped instance of
      this specific prefab is genuinely network-addressable;
      `PlayerBackpack.cs` converted to `NetworkBehaviour` with a real
      `RequestEquip`/`CmdEquip` and `RequestUnequip`/`CmdUnequip` pair,
      identifying the target Backpack by its `NetworkIdentity` and
      reusing the existing `Equip`/`Unequip` methods server-side
      unchanged. Live-confirmed via a temporary debug keybind (removed):
      Admin-Spawned a Masterwork Leather Backpack, equipped it through
      the real Command (visually worn, its own nested Inventory contents
      accessible in the UI), then unequipped it (Back slot correctly
      emptied).

      **Where sub-phase 2 actually stands**: both core scripts have real,
      live-confirmed `SyncList` sync, and three real Commands (add-item,
      move-plain-item, equip/unequip-a-real-instance) prove every shape
      of the client-request → server-validate → apply pattern this
      sub-phase needs.

      **World-pickup networking done in full, 2026-08-23 — the biggest
      remaining piece of sub-phase 2 closed out in one session.**
      Bulk pass: every `worldPickupPrefab` carrying a plain `Pickup`
      component (78 of 127 unique prefabs across every `ItemDefinition`
      asset — the other 49 are the equippable types with their own
      carrier flow) given `NetworkIdentity` and registered in
      `NetworkManager.spawnPrefabs`. `Pickup.Complete()` now routes
      through a server-authoritative Command
      (`PlayerInventory.RequestCompletePickup`/`CmdCompletePickup`) when
      networked, reusing the exact same resolution logic (renamed
      `ServerComplete`, unchanged) with `NetworkServer.Destroy` in place
      of a plain `Destroy` — one shared conversion covering all 78
      prefabs at once, not 78 separate ones, since they all share this
      one script.

      **Two real bugs found and fixed during rollout, both root-caused
      via `Editor.log` rather than guessed at:**
      1. Two already-scene-placed pickups (`Stick Pickup`, `Stick Pickup
         2`) turned out to be `NotAPrefab` — plain hand-placed
         GameObjects, not instances of `StickPickup.prefab` — so they
         never inherited the bulk pass's `NetworkIdentity`. Their
         `Complete()` silently fell back to the local-only path instead
         of erroring, which cascaded into real, repeating
         `NullReferenceException`s elsewhere (`PlayerInventory
         .ComputeSignature`, `NPCEncumbrance`/`PlayerEncumbrance`, even
         `Furnace.AutoRefill`) — all traced back to this one root cause
         once diagnosed properly, not independent bugs. Fixed by adding
         `NetworkIdentity` directly to those two specific instances,
         same pattern as the original Player fix.
      2. Two other prefab-instance objects (`Plank`, `SoccerBall`) threw
         a real "scene object has no valid sceneId, needs to be opened
         and resaved" error after the bulk prefab edit — fixed by
         reopening and resaving `TestScene.unity`, which backfills scene
         IDs for prefab instances whose prefab gained a `NetworkIdentity`
         after the scene was last saved. **New process lesson**: any
         prefab-only `NetworkIdentity` addition needs a scene resave pass
         immediately after, or already-placed instances of that prefab
         silently don't get valid scene IDs.
      Live-confirmed clean after both fixes: picked up multiple Sticks,
      full `Editor.log` trace showed the Command round-trip working
      correctly (client request → server resolve → skill gain → destroy),
      zero exceptions anywhere in a fresh session.

      **What's explicitly still NOT done**: (1) none of these Commands
      are wired into the real UI players actually touch — `Pickup`
      interaction already routes through the Command automatically via
      `PlayerInteraction`, but `InventoryScreen.cs`'s drag-and-drop still
      calls local methods directly; (2) the broader mutation surface
      (crafting, NPC deposit, admin tools) is still local-only.

      **Equippable rollout done in full too, same session.** Bulk pass:
      the remaining 48 equippable prefab variants (39 across Belt/Boot/
      Sunglasses/MiningFaceShield/Canteen/NavigationComputer/
      PersonalHealthMonitor/Tool/Shirt/Jeans, plus Backpack's other 9
      tier/material variants the earlier pilot didn't cover, plus
      `SkillBook`/`StorageBox` — both also implement `IEquippable`,
      caught automatically by the generic scan without having to
      enumerate them by hand) given `NetworkIdentity` and registered in
      `NetworkManager.spawnPrefabs` — 127 total prefabs now networked
      (78 Pickups + 49 equippables). **Real design improvement found
      applying the Backpack pattern a second time**: rather than
      converting each of the 10 remaining carrier scripts
      (`PlayerBelt`, `PlayerBoot`, ...) to `NetworkBehaviour` and giving
      each its own Command pair (what the Backpack pilot did), built
      **one generic `RequestEquipInstance`/`CmdEquipInstance` and
      `RequestUnequipInstance`/`CmdUnequipInstance` pair on
      `PlayerInventory`**, mirroring `InventoryScreen.cs`'s own
      `EquipToSlotDispatch`/`UnequipDispatch` switch statements exactly
      — one shared Command dispatching to whichever carrier's existing
      `Equip`/`Unequip` method already handles that type, unchanged.
      None of the 10 carrier scripts needed to become `NetworkBehaviour`
      at all; only the item instances needed `NetworkIdentity`. Live-
      confirmed via a temporary debug keybind (removed): equipped a real
      Belt (Waist slot) and a real Boot (Feet slot) through the same
      shared Command, zero exceptions — proves the generic dispatch
      genuinely covers multiple distinct carrier types.

      Sub-phase 2's core sync + Command infrastructure is now proven
      across every real shape it needs, *and* both major rollout pieces
      (world pickups, all equippables) are fully done — not pilots.

      **First real UI wiring done and live-confirmed: Unequip.**
      `InventoryScreen.UnequipDispatch` now routes through
      `PlayerInventory.RequestUnequipInstance` whenever the target item
      has a `NetworkIdentity` (true for every equippable as of the bulk
      pass), falling back to the original local switch defensively for
      anything that doesn't — chosen first since Unequip needs no "which
      source container" disambiguation the way Equip does (every
      carrier's own `Unequip` already finds wherever the item is
      currently worn on its own), making it the lowest-risk piece of
      real UI wiring to convert.

      **Real gap found and fixed during that test — not every
      Instantiate-a-real-item call site had the `NetworkServer.Spawn`
      treatment.** Only `PlayerDropping.SpawnPickup` had it originally;
      unequipping a *crafted* Knife threw "Attempted to serialize
      unspawned GameObject" the moment its `NetworkIdentity` got
      referenced in a Command, since crafting instantiates its own
      equipment output directly (`PlayerCrafting.cs`) without ever going
      through `SpawnPickup`. Extracted the spawn-or-not check into a
      shared `NetworkSpawnHelper.SpawnIfNetworked(GameObject)` and
      applied it everywhere a real, interactable item instance gets
      created: `PlayerDropping` (refactored to use it too),
      `PlayerCrafting` (the actual bug), `EquipmentSaveUtility` (save/
      load restore), `PlayerWriting` (a written Skill Book).
      Deliberately NOT applied to `NPCEquipmentVisual`'s bone-parented,
      physics-disabled NPC gear display — that's a purely cosmetic
      clone, never independently interacted with, correctly excluded.
      Live-confirmed clean after the fix: equipped and unequipped a
      crafted Knife through the real UI, zero errors.

      **Equip wiring done too, same session.** Two entry points, wired
      differently since they have different ambiguity shapes:
      - `EquipToSlotDispatch` (drag onto a specific, already-known slot)
        routes through the Command unconditionally when the source is the
        main inventory — no ambiguity to resolve, the drag target already
        picked the destination.
      - `EquipWithChoice` (click-to-equip, no known destination yet) adds
        a `FindSingleValidSlot` check first: if `equipment.CanEquipToSlot`
        is true for exactly one `SlotOrder` entry, that's an unambiguous
        single-destination type (Backpack, Belt, Boot, Sunglasses,
        MiningFaceShield, Shirt, Jeans) and routes directly through the
        Command; a genuinely ambiguous multi-destination type (Canteen,
        NavigationComputer, PersonalHealthMonitor, Tool — `CanEquipToSlot`
        true for 2+ slots) falls through to the existing local
        choice-popup flow unchanged, since a click has no destination to
        pass the Command until the player actually picks one from that
        popup. Live-confirmed all three real player-facing paths: drag-
        equip, single-destination click-equip, and multi-destination
        click-equip (popup still appears and works correctly) — zero
        errors across all three.

      **Multi-destination click-equip wired too, completing Equip
      wiring in full, same session.** All four `TryEquipWithChoice`
      overloads (Canteen, NavigationComputer, PersonalHealthMonitor,
      Tool) now route both apply points — the immediate single-remaining-
      destination case and the popup's chosen-destination callback —
      through a shared `TryNetworkedEquip` helper, same source-checked,
      defensive pattern as everything else this session.

      **Real pre-existing bug found and fixed during that test — not
      caused by the networking work, just surfaced by it.**
      `InventoryScreen.IsCurrentlyWorn` checked each carrier's own
      `Equipped` property, which only ever returns the *first* match
      across that type's valid slots (`PlayerTool.Equipped` checks Left
      Hand then Right Hand, returns whichever Tool it finds first). With
      two different Tools worn at once (a Knife in Left Hand, an Axe in
      Right Hand — exactly what testing multi-destination equip
      produces), the second one always read as "not worn." Fixed by
      scanning every body slot directly via `PlayerEquipment.GetEquipped`
      instead of trusting each carrier's narrower single-result property
      — see `CLAUDE.md`'s new gotcha entry, which also flags that
      Canteen/NavComputer/HealthMonitor share the identical shape and
      deserve the same "two worn at once" test whenever that work comes
      up. Live-confirmed clean after the fix: right-clicking the
      previously-misreported Axe now correctly offered Unequip, and both
      equip and unequip worked.

      **Both Equip and Unequip are now real, networked UI actions for
      every case `InventoryScreen.cs` supports from the main
      inventory** — drag-to-slot, single-destination click, and
      multi-destination click-with-choice all confirmed working.

      **A worn Backpack's nested inventory wired in too, same session.**
      `RequestMove`'s container-key scheme extended with `"worn:<slot>"`
      (that slot's worn `IInventoryHolder`'s own `Inventory`), and
      `CmdMoveItem` switched from the fixed-quantity `Move` to
      `MoveAsManyAsFit` to exactly match `InventoryTransfer`'s own local
      semantics (a drag that doesn't fully fit partially succeeds instead
      of failing outright). `InventoryScreen.cs` gained
      `ContainerKeyFor(Inventory)` — resolves a live `Inventory`
      reference back to a container key by checking it against the main
      inventory and every worn slot's own nested inventory; returns
      `null` for anything this scheme doesn't cover (Furnace zones, NPC
      cargo, a Boot's knife sheath), which correctly falls through to the
      original local-only path unchanged. `TryDrop`'s generic
      (non-equip-slot) branch now routes through the Command whenever
      both sides resolve to a known key. Live-confirmed: dragged Sticks
      into a worn Backpack's contents and back out, correct count both
      directions, zero errors.

      **What's left for sub-phase 2**: containers this key scheme
      deliberately doesn't cover — Furnace zones and NPC cargo aren't
      Player state at all (Furnace isn't a `NetworkBehaviour` yet, NPCs
      are an entirely later phase) — and the broader mutation surface
      outside Inventory/Equipment (crafting, NPC deposit, admin tools) is
      still local-only. Both smaller in scope than what's already
      shipped, and arguably belong to their own later phases rather than
      sub-phase 2 itself.
   3. **Crafting + Building — started 2026-08-23.** `PlayerCrafting.cs`
      converted from `MonoBehaviour` to `NetworkBehaviour`, base-class
      change only, same "prove the foundation first" slice as sub-phase
      2's opening move. Real complication flagged before designing any
      Command: crafting is a genuinely different shape of problem than
      Inventory/Equipment's atomic moves — `StartCraft` validates several
      gating conditions (tool, skill, nearby Anvil/Furnace, Canteen
      water), consumes ingredients across multiple *reachable*
      inventories (not just this Player's own main inventory), and runs
      a real timed batch (`activeRecipe`/`activeTotal`/`activeElapsed`)
      ticked in `Update()` — none of that maps cleanly onto the
      request/validate/apply-in-one-shot Command shape sub-phase 2 used
      everywhere. Live-confirmed the base-class conversion alone is
      safe: crafted a Stick, correct output produced, correct ingredients
      consumed, zero errors.

      **The real Command shape turned out simpler than feared, same
      session.** `RequestStartCraft`/`CmdStartCraft` reuses `StartCraft`
      entirely unchanged, server-side — the recipe (a `CraftingRecipe`
      asset, same category as `ItemDefinition`, which Mirror can't
      serialize directly) is resolved by its stable asset name against
      **this player's own `recipes` array** rather than a separate
      database, which validates "is this recipe actually available to
      this player" for free at the same time as resolving it. `Update()`
      gained an `isServer` guard on the batch-progression logic —
      genuinely correct for a future remote client, zero effect on
      solo host-alone testing (`isServer` is true there too). The output/
      ingredient side needed **no new sync code at all** — it rides
      entirely on `PlayerInventory.syncedSlots`, already proven in
      sub-phase 2, since crafting output is still just an `Inventory`
      mutation under the hood. Live-confirmed with a real multi-item
      batch through the actual Craft button: correct total output,
      correct total ingredient consumption, zero errors.

      **One real, known gap, deliberately deferred**: crafting *progress*
      display (`activeRecipe`/`activeCompleted`/`activeTotal`) isn't
      synced to a remote client — invisible in solo testing since host
      and client share the same fields there, but a genuine remote
      player wouldn't see their own crafting progress bar update without
      further work. Logged, not attempted this slice.

      **Building — done, same session.** `PlayerBuilding.cs` converted
      from `MonoBehaviour` to `NetworkBehaviour`, base-class change only,
      same first-slice pattern as everything else in this sub-phase. Real
      complication flagged before designing any Command: `Confirm()`
      takes a live `BuildSocket` reference — not trivially network-
      serializable the way an `ItemDefinition`/`CraftingRecipe`'s stable
      asset name is. Live-confirmed the base-class conversion alone was
      safe first: placed a real piece through the normal flow, correct
      material consumption, correct placement, zero errors.

      **The socket problem solved without networking a reference at
      all**: rather than giving `BuildSocket` its own `NetworkIdentity`
      just to pass it through a Command, the server independently
      re-derives the exact same socket from the placement position via
      the already-existing `FindNearbySocket(position)` — deterministic,
      so client and server always agree without ever syncing the
      reference itself. `RequestConfirmPlacement`/`CmdConfirmPlacement`
      calls `Confirm(position, rotation, socket)` entirely unchanged,
      server-side, same "reuse the real method" pattern as Crafting's
      Command. All 32 `BuildPiece` prefabs got the bulk `NetworkIdentity`
      + `NetworkSpawnHelper.SpawnIfNetworked` treatment (spawnPrefabs
      127→158), and both `HandleInput()` call sites (free placement,
      socket-snapped placement) now route through the Command instead of
      calling `Confirm` directly.

      Live-confirmed both real scenarios: free placement in open space,
      and socket-snapped placement (a second piece placed adjacent to a
      first). One real side effect surfaced and resolved same session:
      giving every `BuildPiece` prefab `NetworkIdentity` meant every
      already-*placed* instance in `TestScene.unity` (GardenPlot,
      Foundation, Campfire, Bookshelf, Desk, every Plank/Twig piece, etc.
      — 67 objects total) needed the scene itself resaved so Mirror could
      assign each a valid `sceneId`; until that resave, Mirror logged one
      `LogError` per affected object ("needs to be opened and resaved").
      Fixed by simply resaving `TestScene.unity` in the Editor — verified
      via a second independent batch-mode process reading `NetworkIdentity
      .sceneId` back through Mirror's own API (Force Binary scene
      serialization means a guid/text grep can't confirm this — see
      CLAUDE.md's own gotcha on that): all 67 scene objects now report a
      non-zero sceneId, zero resave warnings on a fresh check. Crafting +
      Building are both now functionally complete for sub-phase 3's core
      loop; the one deferred gap is Crafting's progress-display sync
      (above), nothing new for Building.
   4. **Magic + Combat — started 2026-08-23.** Melee is done: `PlayerCombat`
      converted to `NetworkBehaviour`, with a real `RequestPunch`/
      `CmdPunch` Command in the same commit (simple enough to skip the
      usual base-class-only first slice — a single raycast + `TakeDamage`
      + `GainExperience`, no multi-flow branching like Building had).
      Client resolves the aim raycast/hit target locally (only the client
      has a current camera transform), then routes through the Command;
      `ResolveAttack` (weapon/skill lookup) runs server-side against real
      `PlayerEquipment` data, no extra sync needed. Networked-first with a
      local-fallback path for any `IDamageable` without a `NetworkIdentity`
      yet. Wolf/Rabbit/Pig/`NPCFactoryWorker` all got `NetworkIdentity`
      (spawnPrefabs 158→162) — the first creature/NPC prefabs converted.
      Live-confirmed: punched a Wolf to death through the real Command,
      correct damage, zero errors.

      **Ranged — done, same session.** `PlayerRangedCombat` converted to
      `NetworkBehaviour`; `RequestFireArrow`/`CmdFireArrow` mirrors
      melee's split exactly — client resolves aim raycast/spread/draw-
      fraction/target locally, server re-derives the equipped bow/arrow
      off real `PlayerEquipment` data, consumes the arrow, computes
      damage. Same known deferred gap as before: arrow-stack *count*
      still isn't synced to a remote client (`PlayerEquipment
      .syncedSlots` tracks which item, not how many), unchanged from the
      original design note.

      **Live-testing found a real bug — not in the new Command, in the
      earlier creature `NetworkIdentity` sweep's coverage.** 40+ arrows
      into a Deer did nothing; root cause was `Deer_001` and (audited at
      the same time) `Chicken_001` both being real, functioning
      `PreyCreature` instances living outside `Assets/Prefabs/` (under
      the third-party `Assets/ithappy/Animals_FREE/Prefabs/` folder), so
      the earlier Wolf/Rabbit/Pig/NPCFactoryWorker sweep's path-scoped
      search missed them entirely — arrows fired/consumed correctly, but
      the raycast's resolved hit target had no `NetworkIdentity` for the
      Command to damage. Fixed (spawnPrefabs 162→164), and audited every
      `PreyCreature`/`HostileCreature`/`NPCVitals` instance actually
      placed in the scene (13 total, all now covered) to rule out a
      third gap rather than stopping at the two reported.

      **Magic — done, same session. Sub-phase 4 is now fully complete.**
      `PlayerInteraction` converted to `NetworkBehaviour`, plus a real
      `RequestWish`/`CmdWish` Command covering wish completion
      specifically (ordinary E/F `IInteractable` interactions stay
      local-only — a much larger surface out of scope here, most of
      which doesn't mutate shared authoritative state the way a wish
      does). Same client-resolves-target/server-decides-outcome split:
      `ResolveWishTarget`'s raycasting stays client-side, the Command
      carries the wish's stable name (`magic.IdForWish`) plus the
      target's `NetworkIdentity` (re-derived into a real `IWishTarget`/
      `Rigidbody` server-side) and the push direction. `PlayerMagic
      .TryWish` (Will spend, skill XP, success roll) now genuinely runs
      server-side. Campfire (the only `IWishTarget` in the project)
      already had `NetworkIdentity` from the Building sweep, so no new
      prefab pass was needed. Live-confirmed: cast Heal Self, wish
      succeeded, real healing applied.

      One real UI bug surfaced during this test and was logged rather
      than chased live (`BUGS_AND_ENHANCEMENTS.md`) — a stuck empty
      hold-progress bar, shape matching `PlayerInteraction.DrawHoldBar`,
      cause not yet confirmed (possible regression from this slice's
      conversion, or pre-existing).
   5. **Everything else — started 2026-08-23.** Vitals, skills, NPC
      hiring/job-assignment player-side inputs, admin tools. First
      slice: `PlayerEating` converted to `NetworkBehaviour`, plus a real
      `RequestEatFrom`/`CmdEatFrom` Command in the same commit (simple
      enough vertical slice, same as melee's own first Command). Carries
      a container key (same "main"/"worn:\<slot\>"/bare-slot-name scheme
      `PlayerInventory.RequestMove` established in sub-phase 2) plus a
      stable item id, resolved server-side into a real `Inventory`/
      `ItemDefinition` — `TryEatFrom` itself (hunger/vital restore,
      inventory removal) runs unchanged. `PlayerInventory` gained a
      small public `ResolveContainerByKey` wrapper for this and future
      Commands in this sub-phase to reuse without duplicating the
      resolution logic. `InventoryScreen.ContainerKeyFor` extended to
      also recognize a bare equipment slot (an item held directly in a
      hand), not just a "worn:" nested container. Live-confirmed: ate an
      MRE, hunger/health restored, item consumed, zero errors.

      **Medicine — done, same session.** `PlayerMedicine` converted to
      `NetworkBehaviour`; `RequestApplyFrom`/`CmdApplyFrom` is the exact
      same shape as Eating's Command (`PlayerMedicine` already mirrored
      `PlayerEating`'s structure field-for-field before this). Live-
      confirmed: used Healing Paste, heal-over-time applied, item
      consumed, zero errors.

      **Not yet started, remaining in this sub-phase**: Canteen
      drink/fill (acts on the physical instance directly, not a
      container-key removal — different shape), skill/attribute point
      spending, NPC hiring/firing/job-assignment inputs, admin tools
      (`AdminSpawnScreen`), and the broader question of whether passive
      vital drain (`PlayerVitals.Update()`'s hunger/thirst/stamina/health
      ticking) needs to move server-side too — not addressed by either
      slice so far, which only touched player-input consumption paths.
4. **NPCs move server-side.** The 5 `Update()`-driven NPC scripts stop
   running client-side entirely; results replicate to observers.
5. **Persistence layer.** Needed regardless, but now genuinely blocking —
   a dedicated server with no save/load can't actually stay up
   indefinitely the way the design calls for.
6. **Everything design-brief item 6 implies beyond core sync** — IP-
   geolocated spawn points, settlement/city growth as shared macro-layer
   state, Warfare/PvP (`docs/design-brief.md`'s Settlement Warfare
   section) — all explicitly later-phase per the brief itself, not
   reachable before 1-5 above exist.

## 4. Open questions (not decided here)

- **Movement authority**: client-authoritative `NetworkTransform` (simple,
  trusts the client) vs. server-authoritative with reconciliation (robust,
  meaningfully more work). **Still not decided, but now has a real data
  point**: the 2026-08-22 spike test confirmed `NetworkTransformReliable`
  with `syncDirection = ClientToServer` genuinely round-trips movement
  between Host and Client. This proves the simple client-authoritative
  path works mechanically — it doesn't yet prove it's the *right* choice
  for a public dedicated server (a client can still lie about its
  position with this model), so the robustness-vs-effort tradeoff is
  still open. What's no longer open is whether Mirror's basic transport/
  sync loop functions in this project at all — it does.
- **Scope shape — resolved 2026-08-22, see section 3 item 3.** No feature
  flag, no dual code path — single-player becomes "host alone" under real
  Mirror plumbing, converted system-by-system (5 ordered sub-phases) with
  a live-test checkpoint after each, not one long uninterruptible branch.
- **Coordination**: this is exactly the kind of change `WORKING_ON.md`
  exists to prevent collisions on — both collaborators' sessions need to
  know this is active before touching any of the 32 `PlayerXXX.cs` files
  or the world-object prefabs, for as long as this stays in progress.
- **Persistence format/storage**: not scoped at all yet — single
  world-state file, a lightweight embedded DB, something else. Deliberately
  left for when phase 5 actually starts.
- **Dev/test workflow**: running 2+ Unity instances against one project for
  testing (ParrelSync or an equivalent clone-workflow tool) vs. build-and-
  run separate client executables. Not evaluated yet.
- **Which of the 22 `OnGUI` screens need any change at all** — per section
  1's finding, likely very few, since `OnGUI` is already inherently local.
  Worth a real pass to confirm rather than assuming zero, once state is
  actually synced and screens need to read from `SyncVar`s instead of
  plain fields.
- **Player identity/naming** (raised 2026-08-17, NPC-management
  discussion) — **closer to solved than when this was written.** The game
  still has zero player-name concept today, but `NPCDialogue`'s
  auto-naming + `IRenameable` rename flow (shipped 2026-08-17, for the
  unrelated reason of telling multiple hired NPCs apart) is exactly the
  shape this needs: a name field, a rename entry point, and — once
  networked — a `SyncVar` instead of a plain field. Not built for the
  player yet, but no longer a from-scratch design question when phase 2+
  needs it, just a port of an already-proven pattern. Entry point still
  the Player tab (right-click-rename doesn't make sense on yourself). **A rename should cost currency** (a real Coin sink,
  ties into `COMMERCE_PLANNING.md`'s "everything's a faucet, nothing's a
  sink" gap) **and, once a second player exists, should hit current Fame
  if it's negative** — not a flat tax on every rename (that would punish
  cosmetic renames identically to reputation-laundering), but scaled to
  discourage using a fresh name to shed an Infamous reputation
  specifically. Not built, design-only.
- **Guild-mate map markers** (raised 2026-08-17, same discussion) — **now
  designed in full in `TEAMS_AND_GUILDS_PLANNING.md`** (2026-08-19), not
  just this one-paragraph sketch anymore. Also covers Team-mate markers
  (a new, related need that didn't exist when this bullet was first
  written), color-priority rules for someone who's both, a distinct
  marker shape from NPC markers, and a map-corner legend. `PlayerGuilds`
  itself also changed shape in that doc — no longer a fixed dev-authored
  join/leave list, but player-founded guild *instances* of dev-authored
  *types*. Still blocked on the same prerequisite this bullet originally
  named: a second real player, and the player-identity/naming system
  below.

## Cross-references

- `docs/design-brief.md` item 6 — the multiplayer vision this whole doc is
  building toward, plus the Settlement Warfare section for the later PvP
  layer.
- `CLAUDE.md`'s pinned-Unity-version rule — applies to any future headless
  dedicated-server build too, not just the Editor.
- `MVP2_PLANNING.md` item 6 (save/load persistence) — the existing
  single-player-motivated version of the same gap this doc's section 2
  flags as now more urgent.
- `WORKING_ON.md` — mandatory coordination point once real implementation
  work on this starts, given the blast radius across the codebase.
- `TEAMS_AND_GUILDS_PLANNING.md` — the social/economic layer built on top of
  this doc's foundation once player-authoritative gameplay and player
  identity land. Team, Guild, general player trade, and Map presentation for
  both — all planning only, depends on this doc's own prerequisites first.
