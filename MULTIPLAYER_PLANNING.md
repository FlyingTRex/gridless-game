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
      solo play behaviorally unchanged. **Sub-phase 2 (Inventory +
      Equipment) is next.**
   2. **Inventory + Equipment** — most foundational, most-referenced state;
      everything else reads/writes through it.
   3. **Crafting + Building** — depends on Inventory already being synced.
   4. **Magic + Combat**.
   5. **Everything else** — vitals, skills, NPC hiring/job-assignment
      player-side inputs, admin tools.
   Not yet started — deliberately not rushed into the same session as the
   Phase 0/1 pilots above, given the risk of breaking the single-player
   game the pilots didn't carry.
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
