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

## 1. Current state (audit, 2026-08-13)

Confirmed directly against the codebase before any networking design:

- **115 total scripts in `Assets/Scripts/`**, of which **32 are
  `PlayerXXX.cs`** (PlayerInventory, PlayerVitals, PlayerCrafting,
  PlayerBuilding, PlayerMagic, PlayerCombat, PlayerEquipment, ...) and **12
  are NPC/AI** (NPCMining, NPCWander, NPCHiring, NPCDialogue,
  NPCEncumbrance, ...). Every one of these currently assumes exactly one
  local player exists.
- **Hard single-player assumptions via scene-scanning, not a single
  singleton pattern** — there's no `public static X Instance` anywhere in
  the codebase (checked, zero matches), so at least there's no central
  singleton to untangle. Instead, individual scripts reach for "the"
  player directly:
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
- **Zero save/load persistence exists anywhere.** Confirmed by searching for
  `PlayerPrefs`, `JsonUtility.ToJson`/`FromJson`, and any custom save-file
  I/O — nothing. The only `[System.Serializable]` usages found are on plain
  data classes for Inspector display (Boot, CraftingRecipe, Inventory,
  PlayerEquipment, ...), not persistence. This was already flagged as a
  gap in `MVP2_PLANNING.md` item 6 for single-player reasons (NPC job
  durations currently exist only as a workaround for not having
  persistence) — multiplayer makes it strictly more urgent, not a new
  requirement.
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

1. **Infrastructure spike.** Bare `NetworkManager` in the scene, two Editor
   instances (or a build + Editor) connecting, seeing each other's
   `FirstPersonController` move around — no gameplay systems touched yet.
   Purpose: validate the whole toolchain (build settings, transport,
   firewall/local networking) works before investing further, and make a
   real decision on the movement-authority open question above with a
   working testbed instead of guessing.
2. **One pilot world object, fully networked.** StorageBox is the obvious
   choice — it's the simplest existing world object (one `Inventory`, no
   fuel/lit-state/recipe complexity Campfire has) and already has the
   `Active`/`FindNearby` registry pattern this doc's audit found. Get two
   players opening the same StorageBox and seeing each other's changes
   live. This validates the "world object with synced Inventory" pattern
   once, cheaply, before repeating it across every other interactable.
3. **Player-authoritative gameplay.** The big one — systematically convert
   the 32 `PlayerXXX.cs` scripts' local-mutation call sites to the
   Command/validate/replicate shape. Likely the single largest phase by
   raw effort. Candidate for splitting further (inventory/equipment first,
   then crafting/building, then magic/combat) rather than one giant pass.
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
  meaningfully more work). Needs a real answer before phase 1's spike, or
  at least needs the spike to inform it rather than guessing blind.
- **Scope shape**: given this touches an estimated near-totality of the
  115-script codebase eventually, should this be one long-running
  effort/branch, or done system-by-system with single-player mode kept
  working throughout (e.g. via a feature flag or a "host-only" Mirror mode
  that behaves like today's single-player build)? Not decided — affects
  how disruptive this is to ongoing single-player feature work.
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
  discussion): the game has zero player-name concept today — nothing
  identifies a character beyond "the local player." Not worth building
  in single-player (nothing to distinguish it from), but a real
  prerequisite once a second real player exists — other players need a
  name to see, same reasoning driving the NPC-renaming/nametag work
  logged in `BUGS_AND_ENHANCEMENTS.md`. Likely the same shape either
  way: a small identity component, name entry via the Player tab
  (right-click-rename doesn't make sense on yourself), a `SyncVar` once
  networked. Not designed further here — flagged so it isn't
  rediscovered from scratch when phase 2+ (player-authoritative gameplay)
  starts needing it. **A rename should cost currency** (a real Coin sink,
  ties into `COMMERCE_PLANNING.md`'s "everything's a faucet, nothing's a
  sink" gap) **and, once a second player exists, should hit current Fame
  if it's negative** — not a flat tax on every rename (that would punish
  cosmetic renames identically to reputation-laundering), but scaled to
  discourage using a fresh name to shed an Infamous reputation
  specifically. Not built, design-only.
- **Guild-mate map markers** (raised 2026-08-17, same discussion):
  members of a shared guild should see each other on the Map, same idea
  as the Village Flag/NPC markers. Also blocked on a second real player
  — `PlayerGuilds` today is single-player-only membership (join up to 3
  dev-authored guilds via Admin Spawn; there's no roster of *other*
  members at all, because there's no one else). The mechanism itself is
  nearly free once relevant: `MapScreen.DrawFlagMarkers`' live-marker
  pattern (fresh position scan every `OnGUI` frame) already proves this
  out — filtering to "other players sharing a guild with me" instead of
  "every Village Flag" is a small extension of existing code, not new
  plumbing. Not built, design-only.

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
