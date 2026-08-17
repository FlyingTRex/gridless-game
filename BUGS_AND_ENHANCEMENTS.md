# Bugs & Enhancements

Known issues and requested features not being worked right now. Not a replacement
for `WORKING_ON.md` (that's for active work) or `CHANGELOG.md` (that's for shipped
work) — this is the backlog between the two. Check off and move the entry to
`CHANGELOG.md` once it's actually fixed/built.

## Next Session: Scene, Save/Load, Digging & Water (ideation only, 2026-08-10 — nothing built yet)

Grew across one ideation conversation from "let's think about digging" into three
related pieces. **Sequencing confirmed by Ben**, build in this order:

### 1. Larger, organized test scene

Ben's framing: "build a larger test scene, so we can start building some
organization, and have space to build the next couple MVPs." Physical space
for digging/water plus whatever Phase 2 work follows.

- [x] **Target size + Terrain/hills conversion — DONE, shipped
  v0.2.4-dev.** `Ground` is now a real 200×200 `Terrain` + `TerrainCollider`
  (the confirmed 4x-area target — was a 100×100 flat Unity Plane),
  positioned at `(-100, -5, -100)` so the playable area stays centered on
  world origin, with gentle rolling hills baked in via low-frequency
  Perlin noise (fixed offset, not a random seed — reproducible, not
  regenerated every launch). Full story, including how Terrain's
  `[position, position+size]` extent (not centered on its own transform)
  and the height-baseline math work, in `CHANGELOG.md` v0.2.4-dev.
  Verified live via `GroundHeight` itself (the same code path Wolf/NPC
  movement uses) — real ~1.6m variation across sampled points, genuinely
  gentle, and `Ground`'s dedicated physics layer carried over with zero
  changes needed.
  - **Punted to a future enhancement (Ben's call, 2026-08-11):** layout and
    how "organization" should actually look (zoned by system? by biome?
    something else?). The scene is now a solid, fully-populated starting
    point (Trees/ore Boulders/bushes/Wolves/NPCs all scattered as of
    v0.2.9-dev) — good enough to build on without deciding this now. See
    the Phase 2 backlog section below.
  - **Real bonus this sets up, not yet used:** Terrain's `SetHeights()`
    API supports runtime height modification — adopting Terrain now
    plausibly enables real free-form dig-anywhere later (lower the
    heightmap locally at a dig point) instead of needing a wholly separate
    system for that down the road. Not built, just now possible.
  - [x] **Re-leveling — DONE, shipped v0.2.5-dev.** All 28 root-level
    scene objects (Player, both Wolves, the NPC, every Ore/Rock Node,
    Boulder, Tree, Water Puddle, both Storage Boxes, Campfire, Anvil,
    Berry Bush, both Herb Bushes, every loose world pickup) re-leveled
    onto the real terrain surface — additively (old Y + sampled ground
    height), preserving each object's original small offset rather than
    flattening everything onto the raw terrain. Verified by re-reading
    the saved scene fresh, not just trusting the script's own log. Full
    story in `CHANGELOG.md`.
  - [x] **Movement height-tracking — done ahead of schedule, shipped
    v0.2.3-dev, confirmed working against the real Terrain in v0.2.4-dev.**
    Shared `GroundHeight` utility (a Ground-layer-restricted raycast-down
    helper) wired into `HostileCreature`/`NPCWander`/`NPCMining` — built
    deliberately terrain-representation-agnostic, and needed zero changes
    once `Ground` actually became hilly.
  - [x] **Grass texture itself already done, ahead of schedule — shipped
    v0.2.1-dev, now the Terrain's actual `TerrainLayer` as of v0.2.4-dev.**
    `Assets/Textures/GrassTexture_Healed.png` (real Gemini-generated,
    hand-fixed seamless texture) — full texture-generation story in
    `CHANGELOG.md` v0.2.1-dev. Tile size set to 5×5m for the new Terrain,
    matching the old flat Plane's density as a starting point — still
    worth a second look now that it's visible at the real 200×200 scale.
- [x] **Scatter a random number of trees (20-75) across the new scene,
  placed once — DONE, shipped v0.2.6-dev.** 29 Trees placed (seeded
  `Random.Range(20, 76)`), 4m clearance from each other and every
  pre-existing object, terrain-height sampled via `GroundHeight`. Full
  story, including the independent re-verification pass, in
  `CHANGELOG.md` v0.2.6-dev. Prerequisite (`Assets/Prefabs/Tree.prefab`)
  shipped v0.2.2-dev.
- [x] **Scatter ore the same way, using Boulders as the shared "ore"
  object — DONE, shipped v0.2.6-dev.** 71 Boulders placed: Copper 25,
  Iron 14, Silver 5, Gold 2, Platinum 1, plain Rock 24 — rolled from the
  scarcity ranges proposed below. Each instance's `ResourceNode` config
  read live off the existing 5 named Ore Nodes via `SerializedObject`,
  not hand-copied. **Copper/Iron disguise question resolved: kept
  non-disguised** (Iron's current revealed material is an
  in-scene-embedded `Material`, not a portable asset, so disguising it
  would need new asset work; kept Copper non-disguised too rather than an
  asymmetric exception) — **confirmed by Ben 2026-08-11: "copper and iron
  can be left alone."** Full story in `CHANGELOG.md` v0.2.6-dev.
  Prerequisite (`Assets/Prefabs/Boulder.prefab`) shipped v0.2.2-dev.
  - Scarcity curve actually used (from the proposed range below): Copper
    25, Iron 14, Silver 5, Gold 2, Platinum 1, plain Rock 24.
- [x] **Scatter Berry Bush and Herb Bush the same way — DONE, shipped
  v0.2.6-dev.** 10 of each placed (Ben's explicit count), 2m clearance
  each, same placement algorithm as Trees/Boulders. Full story in
  `CHANGELOG.md` v0.2.6-dev.
- [x] **5 Wolves scattered — DONE, shipped v0.2.9-dev.** Fixed at 5 (Ben's
  call, not a random range like the passive resources), each kept at least
  15 units from the Player's spawn position on top of the usual terrain-
  height/minimum-spacing rules — verified: closest was 56.9 units out.
  Full story in `CHANGELOG.md` v0.2.9-dev.
- [x] **5 NPCs scattered — DONE, shipped v0.2.9-dev.** Fixed at 5, to really
  stress-test the Hireable NPC system (multiple independent persistent
  identities, real resource contention once boulders are also scattered).
  Both previously-open resource questions resolved **without needing any
  build work**: hiring all 5 (50 Copper) is comfortably covered by the
  existing Bank→Exchange loop (starts with 25 Gold, downgrades 10:1 per
  tier), and deposit container assignment was already fully flexible per-NPC
  (`PlayerNPCDeposit` lets the player target any `StorageBox` individually) —
  Ben's own use case: presort mined resources into a box near the Anvil by
  assigning specific NPCs to it. Full story in `CHANGELOG.md` v0.2.9-dev.

### 2. Save/load persistence (v1) — ✅ DONE, shipped v0.3.51-dev

Real implementation plan filled out and built same day (2026-08-13) — full
design in `SAVE_LOAD_PLANNING.md`, build detail in `CHANGELOG.md`'s
v0.3.51-dev entry. Live-tested by Ben with a real Editor-restart round
trip: worn equipment and nested equipment contents (items inside a worn
Backpack) both survived exactly. Same narrow v1 scope this entry
originally proposed — Player, Storage Boxes, ore/resource nodes, Hireable
NPCs — with loose world pickups/built structures/Lockbox/Bank contents
still explicitly deferred, not silently dropped.

**The real cost of the "built structures" cut, confirmed live
(2026-08-16):** Ben planted and named a Village Flag ("Phoenix"),
saved, and asked whether it would survive a reload — checked
`save.json` directly and confirmed it isn't captured at all (`SaveManager
.Save()`'s exact 5 categories: `StorageBox`/`ResourceNode`/`NPCHiring`/
`GardenPlot`/`GardenPlot4x4` — no generic `BuildPiece` category exists).
Worth being explicit about the actual blast radius, not just "built
structures" as an abstract scope note: **this means every wall,
foundation, Campfire, Furnace, Anvil, City Statue, Lockbox, and Bank
branch a player places also vanishes on reload** — a player who builds
a whole house today, saves, and reopens the game later would load back
into an empty field. Fine for a short test session (nothing is lost as
long as the game stays open), but a real blocker for anyone trying to
actually settle in across multiple play sessions — likely worth
promoting ahead of other Phase 2 backlog items once picked back up,
given how central building a base is to this game's core loop.

**✅ Phase 1 built (2026-08-17), promoted ahead of schedule after the
gap directly broke a second feature.** Full design in
`SAVE_LOAD_PLANNING.md` section 11. The vanishing Flag wasn't just
cosmetic — `NPCGuarding`'s patrol behavior depends on a `VillageFlag`
existing in the world, so a live-tested Guard that should have been
circling one was confirmed wandering aimlessly instead, directly caused
by this gap. New `BuildPieceDatabase` (same `ItemDatabase`-shaped
stable-ID lookup as every other database, wired into
`DatabaseRepopulator`) plus a `["placedPieces"]` capture/restore pair in
`SaveManager.cs`, generic over every `PlacedPiece` (now
`[RequireComponent(typeof(SaveId))]`) in the scene. Unlike every other
category, a placed structure doesn't pre-exist in a fresh scene, so
restore re-instantiates the piece's own prefab at its saved position/
rotation and reassigns the saved `SaveId` (`SaveId.AssignId`, new) —
this alone fixes Village Flag, City Statue (a pure existence check, no
extra state needed), and every plain Wall/Foundation/Roof Panel.
Village Flag additionally gets its display name restored. Verified via
compile + direct `BuildPieceDatabase.asset` YAML grep (28 `BuildPiece`
assets, correctly deterministic-sorted) — **not yet live-tested with a
real save/reload round trip**, that's next once this is committed.
**All 6 pre-placed Factory Worker NPCs removed from `TestScene.unity`
(2026-08-17), Ben's call**: closer to real gameplay — the Village Flag
spawn loop is now the *only* source of hireable NPCs in the game, no
"just walk up and hire the guy standing there" starting option. This
made a third save gap immediately relevant: `SaveManager`'s NPC
restore only ever found-and-restored a pre-existing scene object, same
limitation `BuildPiece` had — with zero NPCs baked into the scene
anymore, a hired NPC would never come back after a reload. Fixed the
same session, same pattern as Phase 1 below: `SaveManager.RestoreNpcs`
now re-instantiates from `VillageFlagSpawner.HireableNpcPrefab` (new
public getter — it's the only prefab any hireable NPC is ever spawned
from now) when a saved NPC's `SaveId` isn't found in the scene, then
restores its job/tools/cargo/skills exactly as before.

**Real bug found live testing Phase 1 itself (2026-08-17): the fix
didn't actually work the first time.** Ben admin-spawned a Village Flag,
saved, restarted — `save.json` showed `"placedPieces": []`, completely
empty. Root cause: `PlacedPiece.cs`'s `[RequireComponent(typeof(SaveId))]`
gets triggered by a runtime `AddComponent<PlacedPiece>()` call (both
`PlayerBuilding.Confirm` and `AdminSpawnScreen.SpawnPiece` add it this
way), and `SaveId.Reset()` — which generates its GUID — doesn't reliably
fire when a required component is auto-added via a scripted
`AddComponent` call, only via the Editor's own "Add Component" button.
This is the exact gotcha `SaveId.cs`'s own migration-script comment
already documents, just not applied to this new code path. Result: every
placed structure's `SaveId.Id` silently stayed empty, and both
`SaveIdRegistry.Register` and `CaptureWorldObjects` quietly skip
anything with an empty ID — no error, no warning. **Fixed** by calling
`GetComponent<SaveId>()?.GenerateIfMissing()` explicitly right after
`AddComponent<PlacedPiece>()` at both call sites. Compile-verified;
still needs the real save/reload round trip to confirm the fix actually
holds this time.

**✅ Phase 2 built the same session, right after Phase 1**: Campfire and
Furnace's own richer runtime state now saves too — `Campfire.
RestoreState`/`Furnace.RestoreState` (new), each capturing/restoring
lit state, the real-time fuel-burn and cook/smelt timers, the active
recipe (resolved by matching the recipe's `outputItem` against the
instance's own registered recipe list — reuses `ItemDatabase` instead
of needing a dedicated `CookableItem`/`SmeltableItem` database), the
Furnace's up-to-4 recipe queue, its 3 linked StorageBox references
(`fuelSourceBox`/`materialsSourceBox`/`outputBox`, resolved via the same
`SaveIdRegistry` cross-reference pattern NPC deposit containers already
use), and all of Campfire's 6 inventories / Furnace's 3. Verified via
compile only so far — **not yet live-tested with a real save/reload
round trip**, that's the next step before this is considered done.

**A second, worse save gap found the same session: `PlayerMagic`
doesn't just fail to persist, it actively re-randomizes on every
reload.** Ben read a Skill Book gaining the Elemental lineage (Spark)
on top of an already-known Kinetic (Push), then asked to verify Magic
state actually saves. Checked both `save.json` (zero `magic`/`lineage`/
`wish` keys anywhere) and `SaveManager.cs` (zero references to
`PlayerMagic` at all — no capture, no restore). Worse than a silent
gap: `PlayerMagic.Awake()` **unconditionally** picks a fresh random
`StartingLineage` and adds it to `knownLineages` every time the
component initializes, with no check for prior state (there's nothing
to check against). So a reload doesn't just lose the newly-learned
Elemental lineage — it can hand the player a completely different
starting lineage than the one they'd been playing, discarding both the
original and any book-granted ones. Skill-level *numbers* for whichever
lineages happen to be trained (Kinetic/Elemental/etc.) likely do
survive via the generic `PlayerSkills.Levels` → `"skills"` array
`SaveManager` already captures for every skill regardless of category
— it's specifically the *which lineages are known* / *which wish is
selected* state that's unsaved and actively clobbered. Needs its own
`SaveManager` capture/restore pair (`knownLineages` as a string array,
`SelectedWish` by id) plus an `Awake()` guard so the random-assignment
path only fires for a genuinely fresh character, not a restored one.

### 3. Digging + water scarcity, built into the new space from day one

Original digging plan (Shovel + dig sites + new raw material) unchanged from
the first pass — see below — plus a new tie-in Ben asked to fold into the same
session:

- [ ] **Shovel — Rudimentary tier built (2026-08-14, v0.3.59-dev); 4 tiers
  still open.** Deviated from this entry's original sketch in two ways,
  both Ben's explicit call: **Metalworking discipline, not Stonework**
  (1 Iron Ingot + 1 Rudimentary Trimmed Stick, requires the Anvil, gated
  at Rudimentary-level Metalworking) — not the stone-tool pattern
  originally floated here. **Tier-matched materials, not Pickaxe's
  same-ingredients-every-tier convention**: each future tier needs its
  matching Trimmed Stick tier (Crude Shovel → Crude Trimmed Stick, Normal
  → Normal, ...), not one fixed ingredient with quality decided by a
  skill-margin roll. Real Blender-generated model (`Tools/Blender/
  GenerateShovelModel.py`, kept as a permanent script for the future tiers
  to reuse/extend) — wood handle + tapered metal blade, ~0.97m, pivot at
  base. Crude/Normal/Fine/Masterwork Shovel still need their own item +
  recipe (presumably non-Iron materials for Crude, per the tier-matched-
  materials rule — not yet decided what).
- [x] **Dig sites — built 2026-08-14 (v0.3.60-dev).** A `SandDigSite`
  `ResourceNode` (`requiredTools` = Rudimentary Shovel, trains the
  existing `Gathering` skill — Ben's call, not a new dedicated skill)
  yields `Sand`, same hold-to-break shape every gathering node uses.
  Ground itself needed no changes, confirmed — a self-contained
  `holeVisualPrefab` (new generic optional `ResourceNode` field, folded
  into `SetVisible` so every call site gets it for free) shows a crater
  prop on break and hides it again on respawn, reusable by any future
  node wanting a "left a mark" visual, not Shovel-only. Three simple
  Blender-generated props (`Tools/Blender/GenerateSandDigModels.py`): the
  standing sand patch, the small clump that scatters as the actual
  `Sand` pickup, and the dirt-brown hole. **Free-form dig-anywhere is
  still explicitly deferred** — unchanged from the original plan, that
  needs real `Ground` volume or a terrain system, its own later arc.
- [ ] **What Sand actually gets used for is scoped to MVP3, not built
  this session (2026-08-14).** Ben's call: both floated consumers — a new
  Building material tier (Clay/Adobe bricks after Plank) and a new
  Glassmaking line (Sand + Furnace → Glass) — are real scope, each its
  own new system, not a quick follow-up. Sand itself is a real,
  obtainable item now; nothing consumes it yet.
- [ ] **Water becomes a locally-limited resource, reusing the same prop
  trick.** There's already a real, working proof this trick works for water
  specifically — a single `WaterSource` in `TestScene.unity`, literally
  named "Water Puddle," a flat disc prop (not a `Ground` cut) that already
  powers both Drink (`IInteractable`) and canteen-Fill
  (`ISecondaryInteractable`) via the existing `IWaterSource` marker
  interface. The only thing missing is scarcity — it's currently
  **unlimited**, no capacity tracked at all. Plan: give `WaterSource` a real
  `remaining` amount that both Drink and Fill draw down, dry up at 0, and
  slowly regenerate over time (same shape as `ResourceNode.respawnDelay`,
  continuous instead of binary) standing in for rain/runoff until a real
  weather system exists. Ponds are just a bigger version of the same prop
  (bigger radius/capacity). **Possible later tie-in, not committed:** a dug
  hole eventually becoming its own small water catchment over time — nice
  connective tissue between the two systems, not part of this pass.
  **Build this through the new save/load system from day one** rather than
  retrofitting persistence onto it afterward — `WaterSource.remaining` and
  each dig site's broken/respawning state are exactly the kind of world
  state save/load v1 needs to prove itself against.

## Next Session: NPC Model, Animation & Equipment Visuals (ideation only, 2026-08-11 — nothing built yet)

Started as Vendor NPC ideation, pivoted hard once Ben actually looked at the
5 freshly-scattered NPCs: "I'm honestly not happy with the NPC model at all
— a step forward for functionality and a step backwards for gameplay."
Specific complaint, narrowed down: **the hiring/job mechanics themselves
are fine for testing** — this is purely a presentation problem. The model
"looks bleh," and movement is a raw `transform.position` slide (confirmed —
neither `NPCWander`/`HostileCreature`/`NPCMining` drive any `Animator`
today, nothing about NPC or Player movement has ever been animated). The
Vendor NPC idea itself is effectively blocked behind this — a new Vendor
would just inherit the same bleh/sliding problem otherwise, so fix the base
NPC presentation first.

- **Player-visible-body is explicitly deferred, not forgotten.** Ben wants
  the player visible eventually — "in multiplayer, that becomes necessary"
  (see [[project_gridless_multiplayer_aspiration]] memory; this is also
  where that project fact first came up). Confirmed today: Player currently
  has **zero visible mesh** — invisible `CharacterController` + camera only.
  Decision: rig whatever character solution we land on as Unity **Humanoid**
  (not Generic) so animations stay reusable later, but only build visible
  rendering for NPCs now — the first-person view-model/third-person-camera
  work stays parked until multiplayer is actually on the table.
- **Equipment visual attachment — confirmed feasible, not yet built, for
  either Player or NPC.** Checked both `PlayerEquipment` and
  `NPCJob.equippedTools`: today, equipping anything (Backpack included) is
  pure bookkeeping (a dictionary entry), nothing renders. Once a Humanoid
  rig exists, the mechanism is straightforward: grab the relevant bone via
  `Animator.GetBoneTransform` (e.g. `Chest`/`Spine` for Back-slot items),
  instantiate the item's existing model as a child positioned there when
  equipped. **Genuinely reuses existing work** — the Backpack's actual
  visual model (`CrudeLeatherBackpack.glb`, already Tripo-generated and
  in-game since v0.1.74-dev) needs zero new generation, only the attachment
  logic and a rig to attach it to.
- **Three model-source candidates being weighed, none chosen yet — Ben is
  browsing the Asset Store by hand to compare actual visual style before
  deciding:**
  1. **Tripo3D custom generation** (extends `Tools/Tripo3D/`, see
     [[reference_gridless_tripo3d]]) — auto-rig (`/animations/rig`, biped
     preset) + animation-retarget (`/animations/retarget`) endpoints exist
     but aren't used by any script yet, and this project has never
     attempted a full character generation before (every prior Tripo asset
     has been a prop). Highest control over unique look, highest
     uncertainty (untested territory). Draft male/female prompts (A-pose,
     full body, isolated background) already written this session if this
     path gets picked.
  2. **Free "Human Character Dummy"** (Kevin Iglesias/"KI", Asset Store
     #178395) + his **Human Mega Animations Pack** (either **Lite I**
     #320526, $65, tags: idle/walk/combat/crafting/dance/fighting; or
     **Full** #162341, $130, tags include spell/farming/villager too —
     the Full pack's categories map almost one-to-one onto actual Gridless
     systems: spell→Wishes, farming→the Gardening Phase-2 item,
     villager→Hireable NPCs, fight→Combat). Dummy is free, male+female,
     already Humanoid/Mecanim-rigged (no auto-rig step needed), URP-
     compatible, same publisher as the animation packs (near-certain
     compatibility). Real risk, unverified: "Dummy" in the name may mean
     a plain mannequin look that doesn't actually fix the original "looks
     bleh" complaint even though it'd fully fix the sliding — but the
     rig/animations stay reusable on a nicer mesh later regardless, so
     nothing here is wasted either way.
  3. **Survivor Models Pack** (AisenCorporation, Asset Store #307249,
     $19.99) — male+female, Humanoid-rigged, all render pipelines
     supported, but no animations included (would still need pairing with
     one of KI's packs) and tagged only "high quality" with no explicit
     low-poly descriptor — real risk it doesn't visually match this
     game's established low-poly aesthetic (every other asset, Tripo or
     Poly Pizza, reads as deliberately low-poly/stylized).
- **EULA/commercial-use checked and cleared for all three Asset Store
  candidates** — all tagged "Extension Asset," standard EULA, none flagged
  "Restricted Asset." Commercial use is explicitly fine once embedded in
  the shipped game; the real catch is **per-seat licensing** (one license
  per person with the asset installed in the project, not per-project) —
  relevant since this repo has a collaborator, per
  [[project_gridless_game]].
- **Not yet decided/started:** which model-source to use, the Animator
  Controller itself (Idle/Walk states, driven by a speed parameter fed
  from `NPCWander`/`HostileCreature`), and the actual equipment-attachment
  script. Vendor NPC ideation (buy/sell mechanic, wandering-on-a-timer,
  `baseValue` field for `ItemDefinition`, always-buys core list) is fully
  captured in conversation history but not yet written down here — revisit
  once the model/animation question resolves, since building a new Vendor
  before fixing the base NPC presentation would be building on the same
  shaky foundation Ben just flagged.
- **First concrete piece shipped, 2026-08-11 — see `CHANGELOG.md`
  v0.3.0-dev.** `Assets/Models/CombatBoot.glb`, the project's first
  all-Blender (no Tripo) model — accepted despite real rough edges (a hard
  seam at the foot-to-shaft transition, weak toe taper, incomplete laces)
  per Ben's "let's use it for now." Wired into three real equippable
  items (Civilian/Hiking/Military Boots) with a genuinely new mechanic —
  type-restricted equipment slots (`Inventory.restrictedTo`) — but **not
  yet connected to the equipment-visual-attachment plan above**: Boots
  equip into the "Feet" slot exactly like any other equippable today
  (bookkeeping only, `SetCarried` anchors to `transform` since there's no
  bone to attach to yet), same unsolved gap as Backpack/Belt. Revisit once
  a rigged model exists.
- **Model-source decision resolved, animation controller shipped —
  v0.3.33-dev, see `CHANGELOG.md`.** Option 2 (KI Human Character Dummy +
  Human Mega Animations Pack, Full #162341) was picked; both are imported
  and the Male/Female Factory Worker prefabs use the dummy as a `Visual`
  child. `NPCIdle.controller`'s placeholder single-state graph is now a
  real Idle/Walk/WorkMining/WorkChopping/WorkGathering controller per
  gender, driven by a new `NPCAnimatorDriver.cs`. **Still open from this
  entry:** the equipment-visual-attachment script (`Animator.
  GetBoneTransform` + instantiate-as-child) is unbuilt — Boots/Backpack/
  Belt still equip as pure bookkeeping with no visible attachment. Vendor
  NPC ideation also still unwritten.
- **Player-visible-body shipped — v0.3.34-dev, see `CHANGELOG.md`.** The
  "deferred, not forgotten" note above is resolved: the player has a KI
  dummy `Visual` child (same rig approach as NPCs), a stance-aware
  `PlayerAnimatorMale/Female.controller`, and a V-key toggle
  (`PlayerCameraMode.cs`) between the normal first-person view and a
  SphereCast-clamped third-person chase camera. **Still explicitly out of
  scope, same as before:** a first-person arms/view-model (still parked
  until multiplayer's on the table), and equipment-visual attachment —
  worn gear becomes camera-visible in third person (a `cullingMask` side
  effect) but isn't positioned on the body via a bone attachment, so it
  won't look anchored correctly yet.

## Furnace Fuel System (real state + automation shipped v0.3.31-dev)

**Full design lives in `WOOD_AND_FUEL_PLANNING.md`; built system described
in `CHANGELOG.md`'s v0.3.31-dev entry.** The Furnace now has real state
(`Furnace.cs`/`FurnaceScreen.cs`, opened by E) — on-board Fuel/Materials/
Output inventories, an up-to-4 sequential smelting queue
(`SmeltableItem.cs`), and true unattended automation (`Update()` ticks
regardless of player presence, same as Campfire's fuel timer). Remaining
gaps:

- [x] **Furnace on/off toggle.** Shipped as the `FurnaceScreen` "Auto-Run"
  toggle — with it off, the Furnace won't auto-light or auto-refill, so it
  doesn't burn through fuel unattended unless the player wants it to.
- [ ] **Woodshed auto-feed (future, not scoped).** Ben floated an
  alternative/additional fuel-loading path: a Woodshed building (not
  designed or built yet) that auto-feeds fuel into any Furnace within
  15m. Superseded in the near term by the shipped StorageBox-link
  mechanism (any nearby StorageBox can be designated the Fuel Source),
  but a dedicated Woodshed could still layer on top later.
- [ ] **Autonomous production chain — Furnace side shipped, NPC side
  still not built.** The Furnace itself now auto-pulls fuel/materials from
  designated StorageBoxes and auto-drains output into one, continuously,
  with no player nearby (v0.3.31-dev). What's still missing for the
  *fully* unattended vision (a Woodcutting NPC filling the fuel box, a
  Mining NPC filling the materials box, nobody ever touching it):
  Woodcutting doesn't exist as an NPC job family yet (Mining is the only
  one — see `MVP2_PLANNING.md` item 2). Until then, a player has to keep
  the linked boxes stocked by hand — the Furnace no longer needs to be
  *watched* while running, just periodically restocked.

## StorageBox nearby-section UI — same popup treatment as Campfire?

**Campfire's dedicated E-key popup UI shipped v0.3.28-dev** (was tracked
here as a backlog item; now in `CHANGELOG.md` and `CAMPFIRE_PLANNING.md`).
One open question from that work is still unresolved:

- [ ] **Open question, not decided:** should StorageBox's identical
  "nearby StorageBox" section (still living at the bottom of the
  Inventory tab, untouched by the Campfire change) get the same focused-
  popup treatment for consistency? Raised as a natural follow-on, not
  committed either way.

## Multiplayer conversion (Mirror) — exploration only, 2026-08-13, nothing built

Mirror Networking imported into the project this session. Full audit +
phased proposal in `MULTIPLAYER_PLANNING.md` — summary:

- **Why Mirror**: free/open-source, dedicated-server-first architecture
  matching `docs/design-brief.md` item 6 exactly (Valheim/Rust/ARK-style
  replica servers). Picked over PurrNet (also evaluated) mainly because
  PurrNet's stated minimum Unity version (`6000.5.4f1`) is newer than this
  project's pinned `6000.3.21f1`.
- **Audit findings**: 115 scripts total; 32 `PlayerXXX.cs` files all
  assume exactly one local player (no singletons, but heavy
  `FindFirstObjectByType`/`FindObjectsByType` scene-scanning); only
  `StorageBox` maintains a live registry (`Active`/`FindNearby`) —
  everything else scans the whole scene; **zero save/load persistence
  exists anywhere**; the 22 `OnGUI` screens turn out to need no
  structural change at all, since IMGUI already only ever draws on the
  local client.
- **What converting actually requires**: player actions become
  Command→validate→replicate instead of direct local mutation (the
  biggest single chunk of work, spread across those 32 scripts); world
  objects (StorageBox, Campfire, Lockbox, ...) get `NetworkIdentity` +
  synced `Inventory` state; NPCs with their own `Update()` loop
  (`NPCMining`/`NPCWander`/`NPCHiring`/`NPCDialogue`/`NPCEncumbrance`)
  move to server-only simulation; persistence becomes genuinely
  blocking, not just a nice-to-have, once the server is sole source of
  truth for a shared world.
- [ ] **Phase 0 (proposed, not started): infra spike** — bare
  `NetworkManager`, two clients seeing each other move, before touching
  any gameplay system. Would also settle the open movement-authority
  question (client-authoritative `NetworkTransform` vs.
  server-authoritative with reconciliation) with a real testbed instead
  of guessing.
- [ ] **Phase 1 (proposed): one pilot networked world object** —
  StorageBox, the simplest existing interactable, validates the
  "world object with synced Inventory" pattern once before repeating it
  everywhere else.
- [ ] Phases 2-6 (player-authoritative gameplay, NPCs server-side,
  persistence, then the macro-layer stuff — geolocated spawn,
  settlement/city growth, Warfare/PvP) — see `MULTIPLAYER_PLANNING.md`
  section 3 for the full proposed order.
- **Open, not decided**: movement authority model, whether this is one
  long-running effort/branch or done system-by-system with single-player
  still working throughout, dev/test workflow for running multiple
  clients, persistence storage format. `WORKING_ON.md` coordination is
  called out as mandatory once real implementation starts, given the
  blast radius across nearly the whole codebase.

## Enhancements — Phase 2 (MVP 2) Backlog

**Draft, not finalized (2026-08-10) — Ben's explicit call: "we won't
consider this finalized yet."** Pulled together from `docs/design-brief.md`'s
existing "Phase 2 — Settlement depth" list (Systems Wishlist section) and its
dedicated Factions, Guilds & Warbands section (now "Guilds & Warbands" —
Factions was removed from the design entirely 2026-08-14), now that Phase 1 closed out in
full, so there's a working list to pick off chunk-by-chunk the same way
Hireable NPCs was — same discipline, not yet scoped/ordered/agreed to. Treat
every item below as a discussion candidate, not a committed plan, until Ben
signs off on scope and order.

- [ ] **Dumbbell — a held exercise item, trains Strength faster + trains
  Constitution too (Ben's idea, 2026-08-16).** Same "secret exercise
  bonus" shape Soccer already established (`PlayerConstitution
  .GrantSoccerKickGain`, triggered by `SoccerBall.cs` on a real kick) —
  holding a ~5lb Dumbbell in hand would need its own trigger (an active-
  use/swing action, not just passive equip, to avoid being strictly
  better than actually doing something) that grants a Strength gain
  rate boost on top of `PlayerEncumbrance`'s existing load-ratio system,
  plus a direct Constitution gain the way Soccer's kick distance already
  does. Not scoped in detail — needs a decision on the actual trigger
  (hold-and-swing? a repeated-click rep count?) before it's buildable.
  - **Recipe idea (Ben, 2026-08-16): Furnace, 20 Ingots.** Metal type
    left unspecified — needs a decision (any of the 5 ore types? a
    specific one, e.g. Iron?) before this is buildable, and 20 is a
    large raw-material ask relative to every other Furnace recipe
    checked so far (`IronIngotRecipe` is the only shipped Furnace
    recipe today) — worth sanity-checking that number against real
    ore-gathering pacing before treating it as final.
  - **Soccer Ball recipe change idea (Ben, 2026-08-16): Ink + Leather.**
    Flagging a real conflict: `SoccerBallRecipe.asset` already exists
    and ships with 3x Cloth (trains Sewing) — not confirmed whether
    this is meant to *replace* that recipe or whether the existing one
    was overlooked. Needs a decision before either changing the shipped
    recipe or leaving Cloth as-is.
  - **Soccer Nets idea (Ben, 2026-08-16): a real multiplayer minigame,
    not just the existing solo kick-around.** A placeable Build piece
    (a goal/net) would turn `SoccerBall.cs`'s existing physics-toy
    kicking into an actual playable game once real players exist to
    play against — explicitly a multiplayer-only payoff, ties directly
    into `MULTIPLAYER_PLANNING.md`'s still-unbuilt shared-world-object
    work rather than anything buildable solo today. Worth keeping this
    tied to that doc rather than scoping it in isolation. Not designed
    further (goal detection/scoring, match rules, etc. all open).
    **Recipe idea (Ben, 2026-08-16): Sticks and Rope.**
- [x] **Player Map explored-state save/load — fixed, v0.3.99-dev
  (2026-08-16).** Caught live by Ben (explored, saved, reloaded, map
  reset to fog). `PlayerMapExploration.CaptureRevealedBase64()`/
  `RestoreRevealedBase64()` bit-pack the grid (1,250 bytes regardless of
  how much is revealed, not one bool per cell), wired into
  `SaveManager.CapturePlayer`/`RestorePlayer`. Verified via a real
  batch-mode round-trip check (666 cells revealed → captured → restored
  → confirmed identical cell-by-cell) — the first verification attempt
  gave a false PASS because `AddComponent` doesn't fire `Awake()` in
  edit-mode batch scripting (fixed the *test*, via reflection, not the
  real code).
- [ ] **Player Map — Village Flag/City Statue reveal hooks not wired
  yet (v0.3.98-dev).** `PlayerMapExploration.RevealCircle(worldPos,
  radius)` is public and ready; nothing calls it from a Flag or Statue
  yet since the Flag itself is still being built in a separate pass.
  Follow-up once that lands — see `PLAYER_MAP_PLANNING.md`'s reveal-
  radius table (Crude 35m through Masterwork 75m; City Statue 125m).
- [ ] **Per-foot Boot/Sneaker attachment — investigated 2026-08-15, not
  built, no decision made yet.** Today `Boot`/`PlayerBoot.cs` treats each
  boot item as a single combined-pair mesh (both feet baked into one
  model) anchored once to the `Hips` bone with a static offset — it
  doesn't track either foot's actual animation, and can't show a
  genuinely separate left/right shoe. Ben's idea: split each existing
  combined model into a real Left and Right half (reusing the "overlay
  two models together" merge trick in reverse — cut one out via a
  filtering technique) and attach each half to its own `LeftFoot`/
  `RightFoot` bone.
  **Investigated via real Blender renders, not just theory:**
  `CombatBoot.glb` (Civilian/Hiking/Military Boots' shared model) splits
  **cleanly** with a simple per-vertex left/right position cut — verified
  by rendering both halves, each comes out as one complete, correct
  boot. `Sneakers.glb` (Sneakers/Settler's Sneakers) does **not** split
  cleanly — the two shoes' geometry is genuinely intertwined/overlapping
  in the model's display pose, not side-by-side. Tried two different
  geometric approaches (a plane cut, then 2-means clustering on vertex
  position) and both leave fragments of each shoe on both sides; this
  isn't fixable with a smarter cut, the source mesh has no clean seam.
  Also tried Blender's "separate by loose parts" as a connectivity-based
  alternative — useless here, the Tripo3D-generated mesh is already
  fragmented into ~500 disconnected shell pieces internally regardless
  of which shoe/boot they belong to, so connectivity doesn't correspond
  to "left boot" vs. "right boot" at all.
  **Historical precedent for the fix:** `CombatBoot.glb` itself once had
  an analogous problem (a "3 boots fused into one mesh" bug, fixed
  v0.3.15-dev) — resolved by *regenerating* the model via Tripo3D with a
  corrected prompt, not by surgically editing the bad mesh. The same
  playbook likely applies to Sneakers: a fresh single-shoe Tripo3D
  generation, not a split of the current asset.
  **Two real open design questions, neither decided:**
  (1) Split-into-two vs. one-clean-model-mirrored (`scale.x = -1` for the
  other foot) — mirroring halves the asset work and guarantees perfect
  symmetry, but needs confirming a negative-scale mirror doesn't produce
  inside-out-face rendering artifacts before relying on it; using the
  actual split halves avoids that risk but doesn't help Sneakers either
  way. (2) For Sneakers specifically: regenerate a single clean shoe via
  Tripo3D, or build a fresh sneaker procedurally in Blender (like this
  session's cookware/food models), or leave Sneakers combined for now and
  ship the per-foot treatment for the 3 CombatBoot-tier items only.
  **Also real scope, not yet touched:** even with clean per-side models
  in hand, `Boot.cs`/`PlayerBoot.cs`/`EquipmentAttach` need real code
  changes — a single equipped Boot item would need to carry/attach *two*
  child visual instances (one per foot bone) instead of one combined
  mesh at `Hips`, more like how a two-handed item might work than
  anything currently built. Nothing here is scheduled — logged for
  later thought per Ben's call.
- [ ] **Constitution — cold/heat resistance (2026-08-14).** The original
  item-1 "Expand Stats" brainstorm (`MVP2_PLANNING.md`) listed "resistance
  to cold/heat/poison" as a candidate Constitution effect alongside max
  Health/Stamina, but only the Health/Stamina half actually shipped
  (`DEXTERITY_CONSTITUTION_PLANNING.md`, v0.3.55-dev) — Constitution
  currently does nothing to blunt `PlayerWeatherEffects`' weather-driven
  `bodyTemperature` cooling. Natural hook once picked up: scale
  `PlayerWeatherEffects.maxCoolingRatePerSecond` down (or add a separate
  resistance multiplier) by the player's Constitution value, same
  `GetAttributeValue`-driven pattern every other stat effect already uses.
  Ties into item 9's warm-food/tea half too — see `MVP2_PLANNING.md`'s
  stat/world-sim cluster note.
- [ ] **Max Will should scale with Intelligence (2026-08-14, Ben's call).**
  Today `PlayerVitals.maxWill` only grows via discrete per-event
  `GrowMaxWill` increments (called by `PlayerMagic` on every successfully
  completed wish) — no stat drives it at all. Constitution already grows
  Max Health/Max Stamina via a real formula
  (`100 + k × (Constitution-2)^1.5`, `DEXTERITY_CONSTITUTION_PLANNING.md`);
  Will should get the analogous treatment off Intelligence instead,
  likely via `PlayerVitals.SetMaxWill` the same shape as
  `SetMaxHealth`/`SetMaxStamina`, called from wherever Intelligence's
  value is read each frame. Needs a design pass to pick the actual
  curve/anchor points (does the existing per-wish `GrowMaxWill` stay as
  an *additional* source on top, or is it superseded entirely?) before
  building.
- [ ] **Universal degradation** — nothing lasts forever; gear, buildings, and
  vehicles decay if left unmaintained.
- [x] **Gardening — 16-cell grid built, v0.3.79-dev (2026-08-15).** See
  `COOKING_AND_GARDENING_PLANNING.md` section 3/6. `GardenPlot4x4`, 3
  crops (Carrot/Potato/Corn, 5/10/15 real minutes), click-based UI instead
  of drag-and-drop (lower-risk, same mechanic). Two deliberate gaps left
  open, tracked below.
- [x] **Seed Packets — real shared model + 7 color variants, v0.3.80-dev
  (2026-08-15).** Seed *items* now have real art (a shared Blender
  packet model, 7 crop-color materials) — closes the "seed items have no
  visual" gap. All 7 crops now stock 10 seeds/packet (`maxStack = 10`).
- [x] **Garden Plot growing-plant visuals — real art via Wild Harvest:
  Root Vegetables, v0.3.81-dev (2026-08-15).** 6 of 7 crops (all but
  Corn) now grow through their real 12-stage pack models instead of
  placeholder primitives — `GardenPlot4x4` swaps stage prefabs directly
  as the cell's real-time timer progresses. Corn keeps its placeholder
  cube (pack doesn't include it, and it's not a root vegetable anyway).
- [x] **Garden Plot seeds are Admin-Spawn-only — closed for real
  2026-08-16 (not via the originally-envisioned wild-forage nodes).**
  All 7 seed items have real packet art but had no in-world source at
  all. Two pieces close the loop: **`CropDefinition.seedDropChance`**
  (30%, all 7 crops) gives harvesting a chance to also return 1 seed,
  so an established garden sustains/expands itself; **7 `GardenPlot4x4`
  instances scattered around `TestScene.unity`** (one per crop, ~14
  units apart, well clear of spawn/buildings/Chicken/Deer), each
  pre-seeded with 7 already-`Ready` cells of that one crop via a new
  `GardenPlot4x4.PreplantedCell`/`Start()` mechanism (guarded on
  `SaveManager.SaveExists` so a loaded save always wins — same
  "only apply to a truly fresh game" convention starting gear already
  uses) — a genuine first-ever seed source, no Admin Spawn required.
  The `WildCarrotPatch`-style wild forage nodes from
  `COOKING_AND_GARDENING_PLANNING.md` section 4 still don't exist, but
  functionally this closes the same gap a different, cheaper way (Ben's
  call, 2026-08-16) — reusing the existing plot/cell mechanic instead of
  building a whole new standalone-wild-plant object type.
- [x] **Garden Plot growth state save/load — built, v0.3.85-dev
  (2026-08-15).** Both `GardenPlot` (single-plot POC) and `GardenPlot4x4`
  now capture/restore via the same `SaveId` + `CaptureWorldObjects<T>`
  pattern `StorageBox`/`ResourceNode`/`NPCHiring` already use — full
  per-cell state (crop, seed count, elapsed grow time) for all 16 cells.
  **Not yet live-tested** — needs a real Play-mode save/load round trip,
  including the multi-instance scenario the fix below exists for.
- [ ] **Found and fixed a real, pre-existing SaveId collision bug while
  building the above (2026-08-15) — needs live multi-instance
  verification.** `RequireComponent(typeof(SaveId))`'s auto-add only
  runs `Reset()` once per loaded prefab *template* in a session, not
  once per placement — confirmed live via two freshly-instantiated
  `GardenPlot4x4` clones reporting the identical GUID. Since
  `SaveIdRegistry.Register` silently overwrites on collision, this means
  **every instance of the same placeable built in one session (2+
  StorageBoxes, 2+ Garden Plots, etc.) very likely shared one SaveId**,
  so only the last-registered one would restore correctly on load —
  every earlier one silently comes back empty, no error. This is not
  new to Garden Plot; it affects `StorageBox` too and may already have
  cost saved data before tonight. Fixed at the root in `SaveId.cs`
  itself (self-healing collision detection in `OnEnable` — regenerates
  a fresh id if the current one's already claimed by a different live
  instance), so it protects every current and future `SaveId` user
  without touching each placement call site. **Could not be verified in
  batch mode** — `OnEnable` doesn't fire for `Instantiate()` calls made
  from pure edit-mode batch scripting, so the fix is architecturally
  sound (standard `OnEnable` behavior, reliable in real Play mode) but
  unconfirmed live. Test: build 2+ StorageBoxes (or Garden Plots) in one
  session, save, reload, confirm both restore their own contents
  correctly, not just one of them.
- [ ] **One pre-existing single-plot `GardenPlot` scene instance (near
  4,-4 in `TestScene.unity`, from an earlier session) never got a
  `SaveId` retrofitted (2026-08-15).** Adding a component to an
  already-placed `PrefabInstance` via batch script didn't persist to the
  saved scene file after two different attempts (including the
  `PrefabUtility.RecordPrefabInstancePropertyModifications` fix that
  normally solves this class of problem) — not investigated further
  since it's a leftover test object from the now-superseded single-plot
  proof of concept, not something actively used. Harmless either way —
  `CaptureWorldObjects` just silently skips anything without a
  `SaveId`, same as before this fix. Any *new* single-plot or 4x4 Garden
  Plot built from now on gets one automatically.
- [ ] **Planting/harvesting a Garden Plot cell grants no skill XP at all
  (2026-08-15, Ben's question: "is cooking and planting rolled up under
  gathering?").** Checked live — no, and there's no "Planting" skill
  either. `GardenPlot4x4.cs`/`GardenPlot.cs` have zero `PlayerSkills`
  references; Cooking is only ever trained by the one-time act of
  *building* a Garden Plot piece (same as any other `BuildPiece`), never
  by the ongoing plant/harvest actions themselves. Open design question,
  not decided: should harvesting grant Cooking XP (reinforcing "growing
  your own ingredients" as part of the Cooking discipline, the original
  design rationale in `COOKING_AND_GARDENING_PLANNING.md`), a dedicated
  new skill, or stay structure-building-only as-is?
- [x] **Harvested-crop world-pickup visuals — built, v0.3.83-dev
  (2026-08-15).** All 6 crops (Carrot/Potato/Ginger/Turnip/Onion/Sweet
  Potato) now have a real, correctly-scaled `worldPickupPrefab` built
  from Wild Harvest's own Bunch models — no more gray-cube fallback.
  Onion's model lives at `Prefabs/P_OnionBunch.prefab`, not
  `Prefabs/Plants/` like the numbered ones.
- [ ] **Animal & hunting module — "hunt" half built, "tame" still open,
  "harvest diversity" mostly closed (updated 2026-08-16).** Ranged combat
  (Bow/Arrow, v0.3.86-dev) built, directly extending Phase 1's Combat/
  wolf-skinning loop (`HostileCreature`) rather than replacing it. Of the
  4 new animals designed (Chicken/Pig/Deer/Rabbit), 3 are built: Chicken
  (v0.3.87-dev), Deer (v0.3.95-dev), Rabbit (v0.3.107-dev) — all via the
  shared generic `PreyCreature.cs`. **The Prey Creature behavior
  archetype (passive, idle/wander until approached, then flee) now
  exists too** — `PreyWander.cs` (2026-08-16), built for Rabbit first
  (the only one of the three with real Idle/Run animation clips to
  drive), generic enough for Chicken/Deer to adopt later without a
  rewrite. **Pig is the one real remaining gap** — Ben picked the
  [LowPoly Pigs Pack](https://assetstore.unity.com/packages/3d/characters/animals/mammals/lowpoly-pigs-pack-183313)
  (Red Deer, $20) as the best fit among several Asset Store candidates
  checked 2026-08-16, not yet purchased/imported. Flagged in advance:
  it's built for Unity 2018.4.23, old enough it likely ships legacy
  Built-in shaders — expect the same URP-conversion pass Rabbit's own
  materials just needed (`WeatherMakerCloudProbeScript`/HumanDummy-style
  gotcha), and its animation clips (walk/run/idle) weren't confirmed
  from the store page alone. Taming explicitly pinned for a later MVP.
- [x] **Fame/reputation system — built 2026-08-14, see
  `FAME_PLANNING.md`.** A real `PlayerFame` component, single -1000 to
  1000 float. Built: Hire +1/Fire -0.5/unpaid-wages -0.5-per-cycle (all
  hooked into `NPCHiring`/`NPCHiringScreen`), skill-tier mastery in any
  discipline including core stats (`PlayerSkills.TierUnlocked` event +
  `CraftTierScale.FameOnTierUnlock`, the "everyone knows the Hulk for his
  strength" case), guild Join +1/Leave -1 (`PlayerGuilds`), and the
  negative-Fame NPC-flee output effect (`NPCFlee.cs`, every NPC within
  ~10m, pausing their job until the player leaves). Real Player-tab tile
  with a band-name sub-line, replacing the old placeholder. Four pieces
  are designed but blocked on real prerequisites — see the four separate
  entries below, each its own follow-up. **Still open, flagged but not
  resolved:** Ben separately floated Fame/Reputation as a possible
  *later* phase (pushed past Phase 2 entirely) — never confirmed either
  way against this Phase 2 placement; doesn't change that it's now built.
  Verified via batch-mode compile + YAML grep only so far — not yet
  live-tested in Play mode.
- [ ] **Fame: Kill NPC (-10) — the blocking gap just closed, hook still not
  built.** `NPCVitals.cs` (2026-08-16, `GUARDING_PLANNING.md`) gave hired
  NPCs a real `IDamageable`/health/permanent-death system for the first
  time, built for Guard NPCs fighting hostile creatures — so
  `PlayerCombat`'s attack raycast actually can hit and kill a hired NPC
  now (`NPCVitals.Die()` clears the job and destroys the GameObject, same
  shape). What's still missing is the Fame hook itself: nothing currently
  distinguishes "the player did this" from "a Wolf did this" before
  awarding the -10 — `NPCVitals.Die()` has no attacker-source concept at
  all yet. A real follow-up, not attempted as part of Guarding.
- [ ] **Fame: Player death (-2) blocked on death detection not existing
  at all.** Found live while building the rest of Fame (2026-08-14):
  `PlayerVitals.health` just clamps at 0 via `Mathf.Max` — nothing ever
  fires a "player died" event, no respawn/game-over exists anywhere.
  Needs real death handling before this Fame hook can wire in. See
  `FAME_PLANNING.md`.
- [ ] **Fame: Start a guild (+3)/Close (-6) blocked on player-driven
  guild creation not existing.** `GuildDefinition` is a plain
  pre-authored `ScriptableObject` asset (`[CreateAssetMenu]`, hand-built
  in the Editor like `SkillDefinition`) — a player "starting a guild"
  doesn't exist as a concept, only joining/leaving a developer-authored
  one does. See `FAME_PLANNING.md`.
- [ ] **Fame: business-reach input (Inn/Trader) + the Traveling Trader
  output effect, both blocked on an entire commerce system that doesn't
  exist — real design now exists, see `COMMERCE_PLANNING.md`
  (2026-08-16).** No Inn, Trader, or vendor/customer/transaction system
  exists in code yet — this entry is still open, not built — but a
  shared `VendorStall` mechanic is now designed (one buy/sell/till
  primitive, three thin drivers: Player Stall, Traveling Trader,
  prespawned Village Vendor) rather than three separate vendor systems.
  The Traveling Trader driver reuses `FAME_PLANNING.md`'s existing
  5-band pricing table directly. **The Trader's spawn/visit mechanism
  specifically is already built** (`VILLAGE_FLAG_PLANNING.md`, the
  Village Flag beacon system) — `COMMERCE_PLANNING.md` is what wires
  actual commerce onto it. Recommended build order: `VendorStall` +
  prespawned Village Vendor first (works in single-player today, doubles
  as a real currency-earning faucet via a regenerating till, no minting
  pipeline required) → Traveling Trader → Player Stall (needs new
  Lockbox-assignment plumbing and a "bank in town" locality concept
  neither exist yet, see that doc's section 6).
- [ ] **Player-built Bank keeping half the transaction fee — deliberately
  kept out of `COMMERCE_PLANNING.md`'s scope (2026-08-16), logged
  separately.** Ben's idea: a player-constructed Bank earns half of
  every transaction fee run through it. Not a vendor, so it doesn't fit
  the `VendorStall` shape — it requires `PlayerBank`/`BankBox` to become
  a per-instance, ownable entity, a real redesign of the current
  single-global-ledger architecture (`BankBox`'s own code comment: "any
  branch opens the same account... there's no per-branch ledger"). Also
  only pays off once a second real player is transacting at your
  specific branch — priced for post-multiplayer, not useful to build
  before then.
- [ ] **Basic transportation** — log raft/boat up through a cart; a tamed
  animal can pull a cart or carry loot.
- [ ] **Larger/settlement-level storage** — distinct from Phase 1's personal
  `StorageBox`.
- [ ] **Building tiers beyond shelter** — progressing toward town-scale
  construction; includes real-estate options beyond building from scratch
  (rent, buy, construct).
- [ ] **Combat/medical tiers deepen** — ranged weapons; first aid grows
  toward surgery. Includes equippable infirmaries within a player's
  compound, staffable with hired NPC medics — direct extension of the
  Hireable NPCs work that just shipped (a new job family/type, same
  `NPCJob`/`NPCJobDefinition` shape Mining already uses). **See
  `MEDICAL_SYSTEM_PLANNING.md` + `MEDICAL_FAMILIES.md` (2026-08-12)** for a
  full proposed 50-item medical progression evaluated against the current
  system, including which items map to Master Physician endgame content.
- [ ] **Endgame ("Leaving the Planet") mechanically doesn't exist yet —
  real audit + build-order plan now in `ENDGAME_PLANNING.md`
  (2026-08-16).** The 8-discipline Keystone → Ruins Gateway → 4-route →
  Escape Velocity design (`docs/skill-path-space.md`) has zero code
  behind it: no Gateway trigger, no route-check, and 3 of 8 Keystone
  disciplines have no matching skill at all (Engineering doesn't exist
  in any form; Combat has no unifying skill across Melee/Archery/
  Guarding; Trade/Financier has nothing beyond join/leave a
  dev-authored Guild). Recommends building Arcane Propulsion (Magic)
  first and only — it's the one route needing zero new disciplines.
  Orbital Engineering needs an entire 8th discipline invented from
  scratch; Chartered Expedition is blocked on `COMMERCE_PLANNING.md`
  shipping; Conquered Launch Site is blocked on Settlement Warfare
  (below), the least-ready of the four by a wide margin.
- [ ] **Reverse engineering & manuals** — disassemble items to learn their
  schematics, then write instructional manuals/grimoires to mentor other
  players or NPCs. Ties into the skill-books item above as the inverse
  (author your own instead of finding a pre-made one).
- [ ] **Merchant Guilds** — craft-skill bonuses and trade perks, not
  territorial. Structured apprenticeships for advanced crafting tiers,
  exclusive trade contracts, preferential exchange rates on volatile
  assets (gems), guild-backed caravan protection. **Partially seeded**: a
  small real "join up to 3 Guilds" system (`PlayerGuilds`) already shipped
  ahead of schedule this session — membership only, none of the
  bonus/perk/apprenticeship mechanics described here yet.
- [ ] **Warbands/Militias** (Phase 3, listed here for context since it's
  part of the same Guilds/Warbands pair — originally a trio with Factions,
  which was removed from the design entirely 2026-08-14; Fame now covers
  its role) — the literal combatant groups in Settlement Warfare. A
  Warband's conduct moves its members' Fame directly.
- [ ] **Scene layout/organization** (punted from the scene-prep work,
  2026-08-11) — the 200×200 Terrain is now fully populated (Trees, ore
  Boulders, Berry/Herb Bushes, Wolves, and hireable NPCs all scattered as
  of `CHANGELOG.md` v0.2.6-dev through v0.2.9-dev) but placed randomly, not
  organized. Whether "organization" should mean zoning by system, by
  biome, something else entirely, or nothing at all (random is fine for a
  survival sandbox) — genuinely undecided, not even a first-pass proposal
  yet.

## Bugs

- [x] **Cooking skill can never be trained from 0 through normal play — a
  real progression deadlock, found live by Ben (2026-08-16). Fixed
  v0.3.112-dev.** Every `CookableItem` recipe that actually grants
  Cooking XP required `requiredSkillLevel: 5` or higher, while the only
  recipe reachable at Cooking 0 (`RawMeatToCookedMeatCookable`) grants no
  XP at all — confirmed live via Ben's save file (no Cooking entry) and
  a Grill+Herb+Raw Meat load that still only offered Cooked Meat. Fixed
  by lowering `FriedEggCookable.requiredSkillLevel` from 5 to 0, giving
  the game a real single-ingredient entry-level Cooking recipe (Egg +
  Frying Pan) instead of touching the deliberately risk-free base recipe
  or adding new Skill Book plumbing. See `CHANGELOG.md` v0.3.112-dev.
- [x] **Feather has no icon at all, found live by Ben (2026-08-16). Fixed
  v0.3.112-dev — turned out to be a broken model, not just a missing
  bake.** `Feather.glb`'s mesh had 2 of its 4 quad vertices coincident at
  the origin (a genuinely degenerate quad, confirmed via direct render —
  only a thin spike existed, not a feather shape) — nobody could have
  ever baked a good icon from it. Replaced with a real Blender-generated
  model (`Tools/Blender/GenerateFeatherModel.py`), which also turned up
  the same glTF-remap-doesn't-apply bug as Chicken Meat (fixed the same
  way) plus a double-sided-material need for the thin vane geometry. See
  `CHANGELOG.md` v0.3.112-dev.
- [x] **Dropped loot (at least Egg and Leather) falls through the world,
  found live by Ben killing a Deer and a Chicken (2026-08-16). Fixed
  v0.3.112-dev, full audit done.** Root cause was two compounding
  issues, both fixed: (1) `SkinnableCreature.Complete()` dropped loot
  *before* disabling the corpse's own Collider, so a freshly-spawned
  pickup could land overlapping still-solid geometry and get physics-
  ejected through terrain — reordered to disable the collider first.
  (2) A full audit found 49 of the project's 74 `Pickup` prefabs still
  used Discrete collision detection (the exact tunneling risk
  `PlayerCoinDrop.cs` was already fixed for) — all 49 switched to
  `ContinuousDynamic`, matching the 25 that already had it right. See
  `CHANGELOG.md` v0.3.112-dev.

- [ ] **Egg has no icon at all, found live by Ben (2026-08-16) — not yet
  fixed.** `Egg.asset` has both `icon` and `previewIcon` set to
  `{fileID: 0}` (null), same as Feather's bug before its fix. The world
  model exists (`EggPickup.prefab` has a real mesh + collider, confirmed
  separately during the dropped-loot audit above), so this is very
  likely the same "never baked, not broken" case Feather turned out to
  be — needs an `IconBaker` pass, not necessarily a model fix. Not
  attempted yet.
- [ ] **`NPCSeekFlag` has no timeout while still approaching — only
  after arrival, found live by Ben (2026-08-16) while the Village Flag
  spawn loop's first real test ran long.** `Update()`'s countdown
  (`stickAroundSecondsRemaining -= Time.deltaTime`) sits *after* the
  early-return for `!hasArrived && distance > ArriveRange` — so the
  despawn timer never starts ticking until the NPC gets within 2m of
  the Flag. `MoveToward`'s obstacle handling is a simple raycast-and-
  deflect with no stuck-detection at all, so an NPC that gets wedged
  against terrain/a tree/a rock on the way in could plausibly wander
  there forever, never arriving and never timing out either — a real
  possible soft-lock. Live evidence: a Flag's first real spawn (24.0min
  interval, matching the hand-computed formula exactly) never produced
  a visible arrival after 14+ minutes of searching, well past the
  expected ~27-second walk time at `moveSpeed=1.5`. Not confirmed
  whether this specific instance was actually stuck vs. just not found
  — but the missing "stuck while still walking" timeout is a real gap
  either way. Worth a `MoveToward`-level stuck-detection fallback (e.g.
  "hasn't made meaningful progress in N seconds, despawn/reset anyway")
  on top of the existing arrival-based one.
- [ ] **`SaveId` collision-regeneration observed live for the first
  time, found by Ben (2026-08-16) diffing two saves.** CLAUDE.md's own
  `SaveId` gotcha entry already flagged this couldn't be verified in
  batch mode, only a real Play session — this may be the first real
  evidence either way. Between two save-file reads taken earlier vs.
  later in the same continuous session, 6 of 7 `NPCHiring`-tagged NPCs
  had completely different `saveId` strings (only the one carrying old
  Mining cargo matched). Mechanically this is expected to be harmless —
  `SaveId.OnEnable()`'s collision-healing only reassigns the ID string
  on the colliding instance, never touches the underlying state — but
  it has a real practical cost: it made reliably diffing "is this the
  same NPC across two saves" impossible during this same session's live
  investigation (see the Village Flag spawn entry above). Worth a
  closer look at what actually triggered the mass regeneration here
  (suspected: the compile-during-Play-mode incident earlier this same
  session forced an unsaved exit/restart, likely reloading the scene
  and re-triggering every NPC's `Awake`/`OnEnable` in a new, differently
  -ordered pass) — not confirmed, just the leading theory.

- [ ] **Verify Berry Seed's embedded-material remap actually took (2026-08-16
  follow-up).** Chicken Meat's icon bug (see `CHANGELOG.md` v0.3.108-dev,
  and the "Update (2026-08-16, Chicken Meat)" addendum to CLAUDE.md's
  embedded-glTF-material gotcha) turned out to be `AssetImporter.AddRemap`
  silently not applying to the instantiated renderers, even though the
  `.meta`'s `externalObjects` block and `GetExternalObjectMap()` both
  looked correct. The original Berry Seed fix this pattern came from
  (2026-08-14) was only ever verified via a `.meta` guid grep, the same
  way Chicken Meat's initially looked fine too — never confirmed by
  actually instantiating the model and checking the real assigned
  material's `AssetDatabase.GetAssetPath()`. Worth a quick check: load
  `BerrySeedPickup` (or whatever its world-pickup prefab is), instantiate,
  and confirm its renderer's material actually resolves to the extracted
  `.mat` asset and not back to the embedded `Shader Graphs/glTF...`
  material. If it's also silently wrong, apply the same fix used for
  Chicken Meat (`PrefabUtility.LoadPrefabContents` + direct
  `sharedMaterial` assignment on the wrapper prefab, bypassing the
  importer remap entirely).
- [ ] **Unconfirmed: a Combat-category skill tier-unlock (killing a Wolf,
  Rudimentary) may not have granted Fame (2026-08-14/15).** Ben reported
  a "Rudimentary skill notice" after a Wolf kill, then checked the Player
  tab's Fame tile and found it unchanged (still 1.0, matching only the
  earlier Hire grant). Code review found nothing wrong —
  `PlayerSkills.GainExperience` fires `TierUnlocked` on any genuine tier
  crossing for any skill/category, `PlayerFame` is present, enabled, and
  correctly subscribed in `TestScene.unity`, and `CraftTierScale
  .FameOnTierUnlock(Rudimentary)` = 1. Couldn't confirm whether the
  message he saw was a genuine "tier unlocked!" banner (which should
  grant Fame) versus a plain level-up message that just happened to show
  a number near the Rudimentary threshold without actually crossing it
  (which correctly wouldn't) — the message had already expired by the
  time this was raised, so the exact wording is lost. **Next step**: the
  next time a tier-unlock banner appears, check the Fame tile
  immediately afterward, before the message expires, to confirm either a
  real bug or a false alarm.
- [ ] **Skill books: extreme `CraftOutcomeRoll` outcomes never confirmed
  live (2026-08-16).** Live testing (`TEST_FEATURE_PLAN.md` section 31,
  Ben + traskmi) confirmed 7 of 8 checklist lines for Skill Books,
  including a full write→read loop and real Intelligence growth — but
  every write attempt actually run landed a plain `Success`, never the
  extreme ends of the roll. Two things still genuinely unconfirmed:
  - **`SpectacularFailure`** — should deal 2–10 damage to the writer and
    produce no book, materials still consumed. Needs a deliberately bad
    margin (low Intelligence against a hard/high-tier subject) to force
    the roll toward this end — comfortable margins never reach it.
  - **`BrilliantSuccess` lineage bonus** — should grant the resulting
    book's read a 1–10 starting lineage level instead of exactly 0.
    Needs a deliberately generous margin (high Intelligence against an
    easy/low-tier subject).
  - **Scope check** — after reading a book for one specific recipe,
    confirm you still can't craft *other* recipes at that same tier you
    haven't separately unlocked (the grant should be scoped to exactly
    one recipe, not the whole tier). Not attempted at all yet.
  Not blocking — `MVP2_PLANNING.md` item 7 is considered done; this is a
  verification follow-up, not a known-broken feature.
- [ ] **`RectangularHouseTwig`/`RectangularHousePlank` prefab buildings have
  broken roof geometry at both gable ends (2026-08-14).** The existing
  `RoofPanel`/`Roof` build piece is designed for a square footprint where
  4 equal panels meet at one center point (confirmed correct on the
  square `SmallHutTwig`/`SmallHutPlank` prefab buildings) — on the
  elongated 2-Foundation Rectangular House, the ridge is a *line*, not a
  point, and the short end walls' roof panels visibly poke through past
  the ridge instead of closing off a proper gable end. Not a placement-
  math bug (confirmed via render screenshot, verified against
  `PlayerBuilding`'s own socket-snap formulas) — a real content gap, no
  gable-end roof piece exists yet. Ben's call: ship as-is for now, revisit
  once a real gable-end/hip-roof piece exists. See `MVP2_PLANNING.md`
  item 10.
- [ ] **`WovenGrassCloth.mat` also has `metallicFactor: 1` (2026-08-14).**
  Found while checking whether the `IconBaker` near-black-metallic bug
  (fixed same day, see `CHANGELOG.md`'s v0.3.58-dev entry) affected
  anything besides the new Ingot family — this material shares the same
  property but hasn't been checked for the same near-black icon problem.
  Not investigated further this session; worth a quick look if its icon
  ever looks suspiciously dark/flat.
- [x] **Hireable, autonomous NPCs — v1 COMPLETE (2026-08-10), all 6 chunks
  shipped same day (v0.1.192-dev through v0.1.198-dev).** This closes out
  the last of Phase 1's 11 MVP items — see `docs/design-brief.md`'s MVP
  Progress Check-In for the full tally. Kept here (not moved to
  `CHANGELOG.md` outright) because several real follow-ups below are
  still genuinely open for a v2 pass, not resolved by v1 shipping.
  Ideation session straight after placing
  `NPCFactoryWorker`, working out Core Pillar 3's actual shape
  (design-brief.md line 36: "you assign them jobs... they execute
  autonomously over time — Dwarf Fortress-style delegation"). Full
  mechanic, as agreed:
  - **Hire/Fire/Pay is a click-driven menu on the NPC, separate from the
    existing Talk interaction — shipped, Chunk 1.** `NPCHiring` +
    `NPCHiringScreen`, see `CHANGELOG.md` v0.1.192-dev. Hiring costs 10
    Copper via `PlayerCurrency.Spend`. `IsWaitingForPayment`/`TryPay`
    finally have a real caller — Chunk 6's work timer, see below.
  - **Job assignment reuses `CraftingScreen`'s family→tiles shape — shipped,
    Chunk 2.** `NPCJobDefinition`/`NPCJob`/`NPCJobScreen`, see
    `CHANGELOG.md` v0.1.193-dev. Pick a job family (a real discipline
    `SkillDefinition` — `Mining`, newly created, not a separate NPC-only
    skill system), then a job tile within it (`Mine Ore`, the only one
    that exists). **Tier gating shipped, Chunk 3** — see below. **Can be
    reassigned to a different family later** — an already-hired NPC
    isn't locked to its first job forever, though reassigning wipes its
    currently-equipped tools (see below).
  - **Core stats start at a flat 3 — shipped, Chunk 3; growth now actually
    happens — shipped, Chunk 4.** New `NPCSkills`
    (Strength/Dexterity/Constitution/Intelligence, on the same 0.25-10
    displayed scale `PlayerEncumbrance` already uses for the player —
    Strength 3 ≈ 90 lb capacity via the existing `17.3925 × Strength^1.5`
    curve, confirmed live) and Mining at true zero. `NPCMining` now calls
    `GainExperience` on the job's family skill (Mining) every time it
    mines a node — confirmed live via batch (0 → 0.5 after one mine).
    Visible in `NPCHiringScreen`'s Stats section.
  - **Never picks up past 80% loaded — shipped, Chunk 3; now actually
    fed real weight — shipped, Chunk 4.** New `NPCEncumbrance.CanPickUp`,
    reuses `PlayerEncumbrance.BetterGainThreshold` directly rather than a
    new NPC-only constant. `CarriedWeight` is computed from a real
    `NPCCargo` inventory (Chunk 4) rather than a manually-incremented
    number — Chunk 3's original `AddCarriedWeight`/`RemoveCarriedWeight`
    never got a real caller and were removed in favor of this. No
    Strength-grows-from-carrying-load tick exists yet (unlike the player)
    — Mining trains directly off the job's skill-gain instead.
  - **Job tiers now actually gate on skill — shipped, Chunk 3.** Reuses
    `CraftTierScale.SkillRequirement` directly (job tier 1 → Crude → level
    0, tier 2 → Rudimentary → level 10, ...) instead of a second threshold
    curve. `Mine Ore` requires level 0, so it's always available at a
    fresh NPC's Mining 0 — the gating is real even though today's single
    job never actually gets hidden by it.
  - **Player supplies the tools (mining: shield, pickaxe, backpack) at
    assignment time — shipped, Chunk 2**, one "Give" button per tool
    category, pulling from the player's main inventory only (not hands/
    backpack — simplest first pass). **Tools are lost for good on Fire or
    on reassignment to a different job** — deliberately no
    return-to-player-inventory step, Ben's explicit call for simplicity.
    **No visual equip** — `NPCFactoryWorker` has no rig/attachment points,
    so this is data-only for now, matching `HostileCreature`'s "death is
    just a rotation" level of visual investment.
  - **The autonomous mining loop itself — shipped, Chunk 4.** New
    `NPCMining`: finds the nearest available `ResourceNode` within 50m
    (real world objects — every Ore Node/Rock Node/Boulder in the scene,
    not a fake parallel system) it can use and carry, walks to it, mines
    it via a new `ResourceNode.TryMineForNPC`/`PeekYield` pair (the
    existing `Complete()` is hard-wired to `PlayerEquipment`/
    `PlayerSkills`), repeats. **Stops entirely once full** — no deposit
    destination exists yet, that's Chunk 5.
  - **Real discovery mid-build: ore nodes are multi-stage.** Copper Ore
    Node's `chunkPrefab` is itself another `ResourceNode`
    (`CopperOreChunk`), not a `Pickup` — only that yields the real item.
    `PeekYield` now walks the chain recursively (guarded depth, same
    shape `IngredientMatching.Satisfies`'s `baseItem` walk already uses),
    multiplying counts (3 × 2 × 1 = 6 Copper, confirmed live). See
    `CHANGELOG.md` v0.1.195-dev for the full story.
  - **Deposits mined ore at a player-designated container — shipped,
    Chunk 5.** New `PlayerNPCDeposit` (point-and-confirm targeting, same
    shape Ben compared to Building's socket selection) sets
    `NPCJob.DepositContainer`; `NPCMining` walks back once it can't find
    anything else to mine, drains cargo into the box (leftover-safe if it
    doesn't fully fit), then resumes searching. **Falls back to Chunk 4's
    "just stop" behavior if no deposit point has ever been set** — a job
    assigned before targeting a container still works, just doesn't
    self-manage. New `PlayerInteraction.SuppressInteraction` flag so
    confirming the target (E) doesn't also trigger `StorageBox`'s own
    pickup interaction (also E) in the same keystroke.
  - **No NavMesh in this project (same constraint `HostileCreature`/
    `NPCWander` already live with) — bump-and-turn shipped, Chunk 4.**
    A short forward raycast before each move step; if blocked, slides
    along the obstacle's surface tangent instead of pushing through or
    getting stuck. Not real pathfinding — an NPC boxed in on all sides
    (e.g. inside an unfinished building) could still get stuck; not yet
    hit live, flagged proactively.
  - **NPC trains its own job-family skill (Mining), not the node's own
    `trainedSkill` (still `Gathering` on every ore node — `Mining` didn't
    exist before this session's Chunk 2).** The same physical action
    training a different skill depending on who's doing it is a real,
    known quirk — not fixed here, since retroactively repointing every
    ore node's `trainedSkill` would also change what the *player* trains
    by mining them, not something to decide silently mid-chunk. Worth a
    real decision from Ben before Mining/Gathering diverge further.
  - **Work period is a 5-minute real-world timer for now, explicitly a
    stand-in — shipped, Chunk 6.** This project has zero persistence
    anywhere (`grep` confirmed no `DateTime`/save-load/`PlayerPrefs` code
    exists at all), so the design brief's original "5 real days" can't be
    built or even tested without a save system that survives closing the
    Editor. **Real persistence (replacing the 5-minute stand-in with an
    actual multi-day real-world timer) stays a separate, later
    prerequisite, not part of this feature.** New `NPCJob.IsReady`
    (pulled out of `NPCMining`'s own duplicated check) gates the timer —
    only ticks while actually working. `NPCMining` now also refuses to
    work while `IsWaitingForPayment`, as a third condition in its own
    readiness gate rather than routing through the `SetPaused` mechanism
    `NPCDialogue` already uses (multiple independent pausers fighting over
    one shared bool was a real risk — Talk ending mid-payment-wait could
    have wrongly resumed a should-still-be-stopped NPC).
  - **Scope, deliberately chunked rather than one build** (Ben's call,
    matches how every other big system this session shipped in
    reviewable passes) — all 6 shipped: **(1)** Hire/Fire/Pay state
    machine + currency spend — v0.1.192-dev. **(2)** job family/tier
    picker screen + tool hand-off (data-only, no auto-equip visual) —
    v0.1.193-dev. **(3)** NPC core stats (flat 3) + the 80% encumbrance
    cap + skill-gated job tiers — v0.1.194-dev. **(4)** the actual
    autonomous mining loop, including the bump-and-turn obstacle behavior
    and the multi-stage ore-node discovery — v0.1.195-dev. **(5)**
    container-targeted deposit + return-to-mining — v0.1.197-dev. **(6)**
    the work timer/waiting-for-payment state — v0.1.198-dev.
  - **Still genuinely open for a v2 pass** (not resolved by v1 shipping):
    real persistence + the actual multi-day timer; more job families/jobs
    beyond Mining → Mine Ore; visual tool equip (`NPCFactoryWorker` has no
    rig/attachment points); unifying Mining vs. the older Gathering skill
    that every ore node still trains for the player; real pathfinding
    (today's bump-and-turn can still get an NPC boxed in stuck); hiring
    more than one NPC at a time (only one exists in the world today).
  - [ ] **Worker management interface (2026-08-11) — a single screen to see
    and manage every hired NPC at once, not just the one currently in
    front of the player.** Real pain point now that 5 NPCs are scattered
    in-scene (`CHANGELOG.md` v0.2.9-dev) and each can independently be
    hired/waiting-for-payment/assigned a job: today the only way to check
    on any of them is walk up, press E, and open that one NPC's own
    `NPCHiringScreen`/`NPCJobScreen` — there's no way to see "who's
    unpaid right now" or "who's idle vs. working" across the whole roster
    without physically visiting each one. Data already exists to surface
    per-worker, just needs a consolidated view: `NPCHiring.IsHired`/
    `IsWaitingForPayment`/`DisplayName`/`WorkTimeRemaining`, `NPCJob.
    AssignedJob`/`IsReady`, `NPCSkills.Levels`. Likely shape: a new tab or
    a Player-tab section listing every hired NPC (found the same way
    `NPCFactoryWorkerMale/Female.prefab` swap verification did —
    `FindObjectsByType<NPCHiring>`, filtered to `IsHired`), each row
    showing name/job/status/pay-due at a glance, with a way to jump into
    that NPC's existing per-NPC screens for the detailed actions
    (assign job, give tools, pay, fire) rather than duplicating that UI.
    Not scoped/started — logging so it doesn't get lost, not committed to
    a specific design yet.
- [ ] **`CraftingRecipe.requiresCanteenWater` only checks a Canteen held
  in a hand, not one attached to a Belt (2026-08-10).**
  `PlayerCrafting.FindEquippedCanteen` only looks at `PlayerEquipment`'s
  Left/Right Hand slots. A Belt-worn Canteen (the Belt system supports
  carrying a Canteen on an attachment point as an alternative to a hand,
  per `CHANGELOG.md`'s Belt entry) would silently fail Healing Paste's
  water-gate check even with plenty of water aboard. Not yet hit live,
  flagged proactively rather than found the hard way — fix would mean
  reaching into `Belt`'s own attachment points the same way `PlayerLoot`/
  `PlayerCanteen` already do for equip-destination purposes.
- [ ] **Melee weapon damage framework built (2026-08-14, v0.3.61-dev) —
  ranged still open.** Superseded the original "five weapon-usage skills"
  plan (Archery/Spear/Sword/Gun/Bare-handed) with one shared **Melee**
  skill (Ben's call: "generalize it under Melee") covering every melee
  weapon instead of one skill per weapon type. New generic
  `ItemDefinition.isMeleeWeapon` flag + `CraftTierScale
  .WeaponDamageBonus(tier)` (Crude/Rudimentary +0, Normal +1, Fine +1.5,
  Masterwork +2, on top of the base 9-damage punch) — `PlayerCombat`
  checks `PlayerEquipment.GetHandItems()` for a flagged weapon each swing
  and trains Melee instead of Bare-handed when one's held. First applied
  to the Knife (all 5 tiers flagged); any future melee weapon (Spear,
  Sword) just needs the same flag, no `PlayerCombat` changes required.
  **Ranged combat (Archery) — built, v0.3.86-dev through v0.3.88-dev
  (2026-08-15), see `PlayerRangedCombat.cs`.** Bow/Stone Arrow (both
  5-tier), draw/fire mechanic, new Archery skill, icons, a visible
  flying-arrow effect, draw-progress UI, aim zoom, and a real full-body
  draw/hold/release animation (both `PlayerAnimatorFemale`/`Male`
  controllers — found and used the pack's own `HumanF/M@BowShot01`
  clips after initially wrongly assuming no archery animation existed;
  Ben caught it). Not yet live-tested in Play mode — see
  `TEST_FEATURE_PLAN.md` section 42. **NPC archery (the Guarding job) is
  now built** — see `GUARDING_PLANNING.md` and `CHANGELOG.md`'s
  v0.3.106-dev entry: `NPCGuarding.cs` fires a ranged attack using the
  same `CraftTierScale.ArrowDamageBonus`/`BowDamageBonus` math, just on a
  fixed cooldown instead of the player's manual draw-and-hold. Gun, Iron
  Arrowhead, and gameplay sound (combat hits, arrow whoosh, footsteps,
  crafting/UI — no such system exists; **not** the same gap as ambient
  weather audio, which works — see the "Gameplay audio system" entry
  below) are still separate, explicitly open gaps. Bare-handed's own
  numbers (9 dmg, 0.7s cooldown) are still first-pass, not vetted against
  a real weapon-tier progression.
- [ ] **Gameplay audio system — genuinely doesn't exist yet; a survey of
  every imported asset pack found nothing worth reusing (2026-08-16).**
  Prompted by traskmi reporting he heard rain in a live session — real,
  confirmed via `WeatherMakerFallingParticleScript`'s own Light/Medium/
  Heavy `AudioSource` trio (see `CLAUDE.md`'s Weather Maker section for
  the full mechanism), riding on the `AudioListener` already added to
  the Player for Weather Maker. That's ambient weather audio only, not a
  general system — no combat hit sounds, arrow whoosh, footsteps,
  crafting/UI sounds, or anything player-triggered exists anywhere.
  Checked whether any already-imported pack has usable audio sitting
  dormant before assuming a from-scratch build is the only option:
  `LJPackages` (All Seasons environment pack) ships one ambient sound
  file each for Desert/Spring/Winter, unreferenced by `TestScene.unity`;
  `Mirror/Examples` bundles its own demo audio (Kenney RPG pack + 10
  OpenGameArt sounds), also unreferenced — both are generic pack filler,
  not tailored to this game, not worth wiring up as a shortcut. Rabbits,
  Animal pack deluxe v2, ithappy, Kevin Iglesias, and NV3D ship no audio
  at all. **`Assets/Audio/` already exists as an empty scaffold** (just
  a `.gitkeep`) — presumably set up in advance by an earlier session for
  whenever this actually gets built, no content in it yet. Real system
  design (which events trigger what, how clips get sourced/generated,
  mixer setup) not started.
- [ ] **Bow Release animation always returns to StandingIdle
  specifically (2026-08-15), not whatever stance the player was
  actually in before drawing.** Known limitation from choosing a
  full-body state swap over a masked upper-body layer — fine for
  standing, but drawing a bow while Kneeling/Crawling/Prone will snap
  the player's visual stance back to standing after the shot. Fix would
  mean either a masked layer (bigger rework) or per-stance return
  transitions in both Animator Controllers.
- [ ] **32 `ItemDefinition` items still need a deliberate `weight` value —
  all currently sitting at the untuned 1 lb default (2026-08-10).**
  `CraftTierScale.WeightModifier` (Backpack/Knife/Axe/Hammer/Pickaxe
  ladders) and the Small Rock/Ore hand-tuned values are done; everything
  else (raw/refined materials, the Trimmed Stick and Leather Backpack
  ladders, standalone gear, wearable gadgets, Soccer Ball) hasn't been
  touched yet. Full categorized list, with the already-tuned values for
  reference:
  https://claude.ai/code/artifact/7d9bc035-141e-457d-98bf-c7e45da9464c
  *(Reported by Ben — "go through all items, and create an artifact of
  the items that need a weight assigned... log an enhancement with the
  link... so we can go back and build this later.")*
- [ ] **Upgrading a placed Twig Door to Plank Door visibly misaligns it
  in the frame — a real gap on one side, live-confirmed by Ben
  2026-08-10 ("door issue is really bad when upgraded to plank").**
  Suspected root cause, not yet confirmed: `PlayerPieceUpgrade.Upgrade()`
  doesn't re-run `PlayerBuilding`'s own `doorOntoFrame` placement
  formula when swapping a piece to its next tier — it just copies the
  *old* instance's exact world position/rotation onto the new prefab
  (`Vector3 pos = target.transform.position; Quaternion rot = target.
  transform.rotation;`, same for every piece type, not door-specific).
  That only stays correct if Twig Door and Plank Door share the *exact*
  same local convention (hinge at local origin, body extending the same
  direction post-export) — worth directly measuring both models' own
  bounds at identical transforms to confirm whether they actually
  match, rather than assuming. Deliberately not investigated further
  yet — Ben's call to log it and revisit later rather than keep
  debugging in the moment.
- [ ] **Every Plank-tier icon (Wall/Half-Wall/Door/Door-Frame Wall/Roof/
  Gable/Pole/Foundation, all 8) bakes visibly pale/washed-out under
  `IconBaker`, unlike every Twig-tier icon with the identical lighting
  rig (2026-08-10).** Root cause identified, not yet fixed: Plank's own
  established base color (0.78, 0.65, 0.42 — matching `PlankFoundation`'s
  pre-existing flat material) is light enough that `IconBaker`'s ambient
  (flat white, intensity 1.0) + 2 directional lights push it toward
  white, while Twig's much darker wood-grain tones (0.10-0.34 range)
  have enough headroom under the identical rig to not clip. Confirmed
  by ruling out two other hypotheses first: bumping material roughness
  0.55→0.82 (matching Twig's own value) made no difference; neither did
  switching from smooth to flat shading (a real, separate bug found and
  fixed along the way — see `CHANGELOG.md` v0.1.188-dev — but not the
  cause of the paleness). Fixing this for real means either darkening
  Plank's own color (which would then mismatch `PlankFoundation`'s
  already-established shade) or adjusting `IconBaker`'s lighting
  intensity (shared by every icon, Twig included — risky to touch
  without re-baking the whole existing set). Left unfixed per Ben's
  call rather than picking one of those trade-offs unilaterally.
- [ ] **`IconBaker`'s tight-fit framing renders `TwigGablePanelPieceIcon`
  tiny and off-center, tried multiple camera directions, none worked
  (2026-08-10).** Not a bad-angle problem — a bad angle reads as
  foreshortened-but-full-frame (what Roof Panel's icon looked like
  before its own fix), not tiny-in-a-corner. Tried the exact direction
  already proven working for Roof Panel's own flat/wide shape
  (`(0, 0.6, 1.5)`) and it still came out tiny. A simpler debug camera
  using a fixed `orthographicSize` (bypassing `IconBaker`'s tight-fit
  corner-projection math in `BakeOne()` entirely) produced a clean,
  correctly-framed result with the *same* direction — isolating the
  bug specifically to that corner-projection/offset logic, not the
  camera angle or this piece's own geometry. Root cause not found;
  shipped with the rough icon (Ben's call, rather than keep guessing
  blind) — see `CHANGELOG.md` v0.1.186-dev. Worth investigating if
  another asset hits the same failure, since a real fix there would
  also un-block using `IconBaker`'s normal path for this piece instead
  of the manual bake-and-wire workaround currently in place.
  **Second confirmed case, 2026-08-10 (`BandageIcon`):** identical
  symptom — baked as two thin crossing lines, not the actual roll+tail
  model — isolated the same way (a manual fixed-orthographic bake of the
  identical geometry came out clean). Not an elongated-shape-specific
  quirk either — Gable Panel is flat/wide, Bandage is a short chunky roll
  with a thin tail, different proportions entirely — so whatever's wrong
  in `BakeOne()`'s corner-projection math isn't narrowly scoped to one
  geometry class. Shipped with the same manual-bake workaround again.
  Two independent confirmations now; worth prioritizing a real fix if a
  third asset hits it, rather than accumulating more manual-bake
  one-offs.
- [x] **"Rock" item (`MediumRock.asset`) — deleted, v0.3.89-dev.** Confirmed
  orphaned (nothing referenced it) during the 2026-08-15 efficiency
  audit; deleted outright per Ben's call rather than inventing a use for
  it.
- [x] **Can't eat a Berry — fixed v0.1.161-dev.** Reported by Ben during playtest, 2026-08-07.
  Root cause confirmed via investigation: the data wiring is actually
  correct (`Berry.asset`/`BerryEdible.asset` match, and
  `PlayerEating.edibles` has `BerryEdible` wired in) — the bug is that
  `InventoryScreen.DrawInventorySection`'s "Eat" button
  (`InventoryScreen.cs`) is only drawn for items sitting in the **main
  inventory list**, which iterates `playerInventory.Inventory.Slots`
  specifically. A freshly picked-up Berry never lands there — `Pickup.
  Complete()` routes it through `PlayerLoot.Receive()`, which stashes
  plain items into a **hand** slot first. Hand/backpack slots are drawn
  by `DrawEquipmentSection`/`DrawContainerContents`, and both only offer
  the generic "where should this go?" move popup — no Eat option exists
  there at all. A player has to know to manually move the Berry "To
  Inventory" via that popup before an Eat button ever appears, which
  isn't discoverable and just reads as "can't eat it." Same underlying
  gap as the already-logged "Eat directly from a container" item below —
  this is really that bug, just hit for the first time via a real edible
  pickup rather than found in code review.
  **Fixed alongside that item, v0.1.161-dev:** new
  `PlayerEating.TryEatFrom(Inventory source, item)`; the generic move
  popup (`InventoryScreen.DrawMoveDestinations`, used for hand slots,
  backpack, and storage boxes alike) now shows a real Eat button
  whenever the selected item is edible, instead of only ever offering
  move-elsewhere options. Root cause of the silent failure this fix
  also caught: `PlayerEating.TryEat` always removed from the main
  inventory specifically regardless of where the item actually was, so
  even a manually-added Eat button would have found the edible but
  silently failed to remove it.
- [ ] **Chunks/bonus-chunks spawned by `ResourceNode.SpawnChunk` can be
  un-pickupable if their prefab expects `Pickup.Configure()`.** Reported
  by Ben during playtest, 2026-08-07, as "when I chop the tree, if it
  spawns a branch, I can't pick it up" (the new 30% bonus-Stick chance on
  chopping a Log, v0.1.83-dev). Root cause confirmed: `Stick.asset`'s
  `worldPickupPrefab` is `StickPickup.prefab`, whose `Pickup` component
  has `item: {fileID: 0}` baked in — by design, it's meant to be filled
  in at runtime via `Pickup.Configure(item, quantity)`, which today is
  **only** ever called from `PlayerDropping.SpawnPickup()`. `ResourceNode.
  SpawnChunk()` (used for both the guaranteed `chunkPrefab` and the new
  `bonusChunkPrefab`) just does a plain `Instantiate(prefab, position,
  Random.rotation)` — it never calls `Configure()`. With `item` left
  null, `Pickup.Complete()` calls `PlayerLoot.Receive(null, ...)`, which
  immediately no-ops, so the spawned object can never be picked up.
  **This is a latent bug for any future `ResourceNode.chunkPrefab`/
  `bonusChunkPrefab` that (like `StickPickup`) relies on runtime
  `Configure()` rather than a hardcoded `item` field** — it happened not
  to matter before now because every existing chunk prefab
  (`WoodChunk`/`RockChunk`/`PlankChunk`/etc.) hardcodes its `item`
  directly in the asset instead. Fix is likely either: give
  `ResourceNode.SpawnChunk` an item-aware overload that calls `Configure`
  when the spawned prefab has a `Pickup` with a null `item`, or simply
  avoid pointing `bonusChunkPrefab`/`chunkPrefab` at `Configure()`-style
  prefabs and use hardcoded-item prefabs (like `WoodChunk`) instead.
  **Hit again, 2026-08-07 (v0.1.117-dev):** built `CopperChunk.prefab`
  (the refined-Copper chunk spawned when a Copper Ore chunk breaks)
  following `StickPickup`'s empty-`item`/`Configure()` convention
  instead of `RockChunk`'s hardcoded-`item` one — same bug, same
  symptom ("can't pick up the smaller blocks", reported live during
  playtest). Confirms this isn't a one-off risk; it's the default
  failure mode any time a new chunk prefab is built by copying the
  *wrong* one of these two established patterns. Fixed locally by
  hardcoding `item` directly on `CopperChunk.prefab` (option 2 above),
  but the underlying systemic gap — `ResourceNode.SpawnChunk()` still
  never calls `Configure()` — remains unfixed, and `StickPickup` as a
  Log's `bonusChunkPrefab` is still affected by it.
  **`StickPickup` itself fixed v0.1.164-dev** (option 2 again — `item`
  now hardcoded directly on `StickPickup.prefab`, confirmed still in
  place), alongside the same null-`item` pattern found and fixed on
  `RopeCoilPickup.prefab` and `RockKnifePickup.prefab` in the same
  sweep. **The specific reported symptom (Log's bonus branch) is
  resolved. The systemic gap is not** — `ResourceNode.SpawnChunk()`
  still never calls `Configure()` (reconfirmed by reading the method
  directly, 2026-08-09), so this remains the default failure mode for
  the *next* chunk prefab built by copying the wrong convention. Leaving
  this open for that reason — it's a pattern risk, not a one-off.
- [ ] **The two `TreeBranch_PolyByGoogle` instances in the scene are
  still non-interactive decoration.** Follow-up to the "only the
  procedural Tree is choppable" report from Ben's 2026-08-07 playtest —
  Big Tree by 3Donimus got `ChoppableTree` in v0.1.91-dev (see
  `CHANGELOG.md`), but the two `TreeBranch_PolyByGoogle` instances
  placed for visual comparison during art exploration
  (`THIRD_PARTY_CREDITS.md`) still have no script component at all.
  Not necessarily a bug — a Tree branch is a much smaller prop than a
  full tree, chopping it may not make sense — but worth an explicit
  decision either way so it doesn't read as an oversight.
- [ ] **Berry Bush searching — random 0-4 berry yield, plus a rare "super
  success" chance of a Berry Seed.** Ben's idea, 2026-08-07: "search the
  berry function... random chance of finding up to 4 berries.
  additionally, a super success chance of finding a berry seed."
  **Berry Seed chance shipped v0.1.179-dev — the base yield range is
  the one remaining gap.** `BerryBush.cs`'s F/search action rolls
  `Random.Range(minBerries, maxBerries + 1)` (`minBerries=0`,
  `maxBerries=3`, so 0-3 not 0-4 — `maxBerries` would need bumping to 4
  to match exactly) for the normal yield, unchanged from v0.1.169-dev.
  **New:** a separate, independent `berrySeedChance` roll (`[Range(0,1)]`,
  wired to 0.02 = 2%) on every search regardless of the berry roll's own
  outcome, spawning a real new `BerrySeed.asset`/`BerrySeedPickup.prefab`
  (Blender-modeled, own icon) on success. Whether Berry Seed still
  implies a future plantable/farmable system is exactly as open as it
  was when first asked — this only added the item and its spawn chance.
  *(Reported by Ben.)*
- [ ] **Procedural tree (v0.1.58-dev) doesn't read as a tree yet.** Confirmed
  via screenshot: `GenerateTree.cs`'s branching mesh renders and is visible
  (the untested backface-culling safety net wasn't even needed, or at least
  didn't hide anything), but the result looks wrong in three specific ways:
  - **Proportions read as a pole with a ball stuck on top**, not a tree. The
    trunk barely tapers and stays near-vertical for most of its height —
    lateral branch spread (32° max deviation per split, `RandomConeDirection`
    in `GenerateTree.cs`) only becomes visually obvious in the last couple of
    generations right below the canopy, because each generation's segments
    are shorter than the last (0.62–0.8× length falloff per level) — spread
    needs to happen gradually up the whole tree, not compress into the top.
  - **Foliage reads as a cluster of grapes/balloons**, not a canopy — the
    sphere clusters at each branch tip are too separated; they need to
    overlap into one rounded mass (larger radius and/or tighter placement
    per cluster, or bigger spheres with more overlap between adjacent tips).
  - **Bark color renders pale grey-tan instead of the brown actually set**
    (`TreeBark.mat`'s `_BaseColor` is `(0.32, 0.20, 0.11)`). Suspect but
    unconfirmed: the new procedural sky (v0.1.55/57-dev) may be contributing
    more ambient light than the old default skybox did, washing out
    unrelated materials — worth checking `RenderSettings` ambient source/
    intensity before assuming the material itself is wrong.

  *(Reported by Ben, deferred rather than iterated on immediately —
  "we will have to work on the trees.")*
- [ ] **Crafted items land in the plain main inventory instead of a free hand
  or an equipped container's slot.** Surfaced 2026-08-05 when Ben crafted a
  Pickaxe and couldn't find it — it wasn't missing, `PlayerCrafting.TryCraft`
  had correctly placed it in the main inventory's "uncategorized" list
  (verified: recipe/item data all wired correctly, this is a real behavior,
  not a data bug). Ben's expectation was that it should've gone to a free
  hand or an equipped container's inventory slot instead, matching the
  intended end state of "Simplify item-holding to two states" below — that
  item is about the *pickup* path specifically (Backpack → free hand →
  inventory slot → drop), and *crafting* output was never actually brought
  in line with it; `TryCraft` has unconditionally targeted the main
  inventory since before this session, documented as intentional at the
  time (see the Crafting-tab test-plan section). Logging as a bug now since
  that's no longer the wanted behavior — fix should route crafted output
  through the same equip-or-store priority once "Simplify item-holding to
  two states" is built, rather than hardcoding straight to main inventory.
  *(Reported by Ben.)*
- [ ] **No way to move an equipped item (e.g. Canteen) into a backpack.**
  `InventoryTransfer.Move`/`Inventory.AddEquipmentItem` already support carrying an
  equipment reference into any `Inventory`, backpack included, but no UI path ever
  calls it for an equipment-backed slot: `DrawEquipmentSection` draws an
  `entry.equipment != null` slot as a plain `GUILayout.Box` (not a `Button`, so
  it's not clickable at all), and `DrawInventorySection`'s equipment branches
  (Backpack/Canteen/NavigationComputer/PersonalHealthMonitor/Sunglasses) only ever
  offer Equip/Drop — unlike the plain-item branch, there's no "To Backpack"/"To
  Storage". Affects every equippable, not just the Canteen. *(Reported by Ben.)*
- [x] **Only one worn container's contents show in the Inventory tab's side
  column at a time — fixed v0.1.124-dev, refined v0.1.125-dev.** Surfaced
  2026-08-06 building Belt (`CHANGELOG.md` v0.1.75-dev), confirmed via
  playtest 2026-08-07 ("when you equip the belt, the backpack
  disappears"), and hit again 2026-08-08 testing the Crude Fiber Belt's
  new attachment points (a Canteen equipped to the Belt was invisible
  because the Backpack, also worn, was winning). `InventoryScreen.
  GetWornContainer()` returned only the first worn `IInventoryHolder`
  found (Back beat Waist); replaced with `GetWornContainers()` returning
  all of them. `DrawContent()` first rendered one bordered panel per
  worn container side by side (v0.1.124-dev), then merged into a single
  "Inventory" panel with one preview+contents row per container stacked
  inside it (v0.1.125-dev, Ben's call after seeing the two-panel look).

- [x] **Backpack — folded into the 5-tier CraftTier ladder, capacity scales
  by tier — shipped 2026-08-06, see `CHANGELOG.md` v0.1.75-dev.** Grew out
  of the Belt discussion just below: same "container capacity scales with
  crafted tier" idea, applied to Backpack.
  - **Renamed**, not just cosmetic: `"Rough Backpack"` → plain `Backpack`
    (Normal, no prefix, per `CraftTierNames`' convention), alongside new
    `Crude Backpack`/`Rudimentary Backpack`/`Fine Backpack`/`Masterwork
    Backpack` `ItemDefinition`s.
  - **Capacity curve, shipped as designed:**

    | Tier | Capacity |
    |---|---|
    | Crude | 4 |
    | Rudimentary | 6 |
    | Normal | 8 |
    | Fine | 12 |
    | Masterwork | 16 |

  - **Update, v0.1.134-dev:** all 5 tiers now have a real world pickup
    (grass-basket model, `IconBaker`-baked icons) — Ben's call to go
    ahead and wire the models even though real per-tier recipes still
    don't exist. **Recipes for Crude/Rudimentary/Fine/Masterwork
    Backpack specifically are still NOT built** — only reachable via
    Admin spawn or a future recipe. The Normal tier is craft-adjacent
    only via the separate `Leather Backpack` (new item, not this
    ladder) and `Crude Fiber Backpack` (also not this ladder). Still
    holding off on real Backpack-ladder recipes until there's an actual
    Fiber → Cloth / Leather material progression to gate tiers on,
    rather than 4 recipes that all cost the same placeholder materials
    with nothing but a name distinguishing them.
  *(Reported by Ben.)*
- [ ] **`LeatherBackpackRecipe.asset` (new, v0.1.134-dev) uses placeholder
  ingredients (6x Cloth + 4x Rope) — explicitly temporary.** Ben's
  direct call: build the recipe shape now, swap in real
  Leather/hide-tanning materials once that chain exists (no raw
  "Leather"/"Hide" material exists in the game yet — no
  hunting/skinning system built). Don't read the current ingredient
  list as a design decision; it's a placeholder standing in until a
  real material exists to replace it.
- [x] **Belt — new equippable, worn at Waist, holds generic attachment
  points instead of a normal inventory — shipped 2026-08-06 (Normal tier
  only), see `CHANGELOG.md` v0.1.75-dev.**
  - Equipping a Belt occupies the `Waist` slot in `PlayerEquipment`, which
    replaces Canteen's old direct-to-Waist fallback — a bare Canteen's
    carry locations are now Left Hand → Right Hand → the equipped Belt's
    attachment points, not the body's Waist slot directly.
  - Attachment points are **generic**, not typed — any attachment
    (Canteen, Knife Scabbard, Pouch, Holster) consumes exactly 1 point
    regardless of kind.
  - Point count scales with the Belt's own `CraftTier`, hand-picked (like
    Lockbox) rather than fit to the existing `CraftTierScale.Modifier()`
    ratio, since 2→12 doesn't match that curve: Crude 2, Rudimentary 4,
    Normal 6, Fine 9, Masterwork 12. **Normal tier renamed to `Fiber
    Belt` 2026-08-07 (v0.1.79-dev)** — establishes "Fiber Belt" as the
    ladder's actual base name (not just "Belt"), and **`Crude Fiber Belt`
    shipped the same day** — first tier with a real recipe (8x Fiber, 2
    points, trains Sewing), and the first-ever crafted equippable that
    actually works (see the equippable-crafting-output fix in the
    Textiles/Leather item below). Rudimentary/Fine/Masterwork Fiber Belt
    still don't exist. **Update, v0.1.140-dev:** the Normal-tier `Fiber
    Belt` item itself (`BeltItem.asset`, the standalone placeholder
    behind this rename, never given its own real model) was removed
    outright — Ben's call, redundant with `Crude Fiber Belt` which
    already has real content. This ladder's remaining open question
    (Rudimentary/Fine/Masterwork) is now moot unless the ladder concept
    gets revived under a different base tier.
  - **Attachments, in the order they'd likely get built:** Canteen (built
    — can now carry on a belt point as an alternative to a hand), Knife
    Scabbard (holds exactly 1 Knife, any tier, nothing else), Pouch (grants
    1-3 general-item storage slots — sized independently of the Belt's own
    tier, so a Crude Belt can carry a 3-pocket Pouch), Holster (deferred —
    no ranged/melee weapon exists yet to holster). **The underlying
    mechanism for "holds exactly 1 Knife, any tier, nothing else" now
    exists** (`Inventory`'s optional `restrictedTo` list, built 2026-08-11
    for `Boot`'s Knife Sheath/Pistol Holster slots — see `CHANGELOG.md`
    v0.3.0-dev) — a Belt-side Knife Scabbard would reuse the exact same
    mechanism, not need new plumbing.
  - **Explicitly open, not decided:** whether attachments themselves get
    quality tiers that change their function, not just belt-slot
    occupancy — Ben's example: a higher-tier Canteen could hold more
    water than a Crude one. Same question would presumably apply to
    Scabbard/Pouch/Holster once those exist (does a Masterwork Scabbard do
    anything a Crude one doesn't?). Left as a question for whenever
    attachments actually get built, not resolved now.
  - **Ties into Encumbrance (design-brief.md Phase 1, not built —
    `ItemDefinition` has no weight field yet):** once carried weight
    affects movement/stamina, belt capacity stops being a free number —
    a bigger Belt is presumably heavier to wear, and a full Canteen/loaded
    Pouch weighs more than an empty one. Gives a real capacity-vs-mobility
    trade instead of just "more slots is strictly better." Same logic
    applies to Backpack's flat 8 slots once weight exists. Third lever
    already named in design-brief.md's Phase 1 encumbrance item, not new
    here: carry capacity/movement efficiency also improve as
    Strength/Athletics grows through use (Pillar 2's skill-via-use model)
    — so a heavy belt+pouch loadout is viable for a character who's
    trained for it, not just a flat gear tax on everyone equally.
  *(Reported by Ben.)*
- [x] **Equip destination picker for multi-slot equippables — shipped
  2026-08-06, see `CHANGELOG.md` v0.1.76-dev.** Ben's follow-up right
  after Belt landed: Canteen can now go to Left Hand, Right Hand, or a
  worn Belt's points, and clicking Equip silently picking the first match
  isn't good enough. `PlayerCanteen`/`PlayerNavComputer`/
  `PlayerHealthMonitor` (the only 3 equippables with more than one
  possible destination) gained `AvailableDestinations`/`EquipTo`; a new
  popup in `InventoryScreen.cs` shows the real options when there are 2+,
  and still equips immediately with 0 or 1 (no needless click for
  Backpack/Belt/Sunglasses/Mining Face Shield, which only ever have one
  destination each). **Related but NOT the same fix as** "No way to move
  an equipped item into a backpack" and "Equip directly from a container"
  (both under Bugs, above) — those are about different actions (moving an
  already-equipped item elsewhere, and equipping straight from a
  container's contents) and are both still open. *(Reported by Ben.)*
- [ ] **Fiber → Cloth textile chain, and a way to source Leather — needed
  before Backpack/Belt (or any future Sewing-discipline item) can get real
  recipes.** Ben's call, 2026-08-06, made mid-build on the Backpack/Belt
  retier: rather than faking their recipes with placeholder ingredients
  (Stick/Wood, the way the tool tiers did as pure scaffolding), hold off
  until there's an actual textile/leather material web. Ties directly into
  the still-open "full material web beyond wood/stone (metal, textiles)"
  gap already logged under "Full crafting/gathering/skills redesign"
  below, and gives the empty `Sewing` skill (exists as a `SkillDefinition`,
  zero recipes train it today) its first real reason to exist.
  **"Where Fiber comes from" answered 2026-08-07 (`CHANGELOG.md`
  v0.1.77-dev):** all 5 `TrimmedStick` recipes now also yield 1 Fiber
  (guaranteed, flat across tiers) — trimming a branch with a Knife leaves
  you with usable fiber alongside the Trimmed Stick. Ben's framing: "if we
  use the rock knife on the tree branch... outcome would be maybe some
  fiber and the trimmed stick." **Rope/Cloth recipes shipped 2026-08-07
  (`CHANGELOG.md` v0.1.78-dev):** `RopeRecipe` (5x Fiber → 1 Rope) and
  `ClothRecipe` (10x Fiber → 1 Cloth), both training `Sewing` directly
  (skillGain 2, no intermediate step) — the first two recipes to ever
  populate that skill. **First real starter gear shipped 2026-08-07
  (`CHANGELOG.md` v0.1.79-dev):** `Crude Fiber Belt` (8x Fiber, 2 points)
  and a new, distinct `Crude Fiber Backpack` (15x Fiber, capacity 4) —
  see the Belt and Backpack entries below for the full detail. Also
  required fixing `PlayerCrafting.TryCraft` so a crafted equippable
  actually works (see the Admin-spawn-tab entry above — same root cause,
  only the crafting side is fixed). **"Where Leather comes from" answered
  2026-08-15 (`CHANGELOG.md` v0.3.95-dev):** hunting — a new `Leather.asset`
  (own real model, `Tools/Blender/GenerateLeatherModel.py`) drops from the
  Deer, placed in `TestScene.unity` the same `PreyCreature` way Chicken
  was (killable/skinnable with a Knife, trains Gathering). Raw Meat also
  drops (2-4, guaranteed) — the shared meat item every creature already
  uses. **Still open:** no recipe actually consumes Leather yet (still
  just a sourced-but-unspent material — the still-open Backpack/Belt
  Sewing-tier recipes below are the natural next consumer), and
  Rudimentary/Fine/Masterwork tiers of either new Fiber item.
  *(Reported by Ben.)*
- [x] **Skill-gated crafting tiers — shipped 2026-08-07, see
  `CHANGELOG.md` v0.1.80-dev.** Ben's call: use skill level 1/10/25/50/100
  to denote the 5 `CraftTier`s. Real bootstrap deadlock caught before
  building: skills start at 0, and the only way to gain most disciplines
  (Stonework/Woodworking/Sewing) is crafting the exact items this gate
  would restrict — requiring Crude ≥ 1 would make a fresh character
  unable to ever craft a first item in that discipline at all. **Resolved:
  Crude requires 0** (no real gate, same as today), curve applies from
  Rudimentary up: Rudimentary 10, Normal 25, Fine 50, Masterwork 100.
  - New `CraftTierScale.SkillRequirement(tier)`, alongside the existing
    `Modifier(tier)`. `PlayerCrafting.HasRequiredSkill(recipe)` checks
    `recipe.trainedSkill`'s current level against it (recipes with no
    `trainedSkill`, e.g. the 5 gadgets, are unaffected — same as
    `HasRequiredTool`'s pattern). Wired into `TryCraft` and
    `CraftingScreen`'s enabled/label logic (`— requires Stonework 25`,
    same style as the tool-requirement label).
  - **Real bug caught before it shipped:** `Rope`/`Cloth` never had an
    explicit `tier` set, silently defaulting to `CraftTier.Normal` —
    would have required Sewing ≥ 25 just to make basic Rope, breaking the
    very recipes meant to build up Sewing in the first place. Fixed by
    setting both to `tier: 0` explicitly (they're single-tier items with
    no real ladder, so Crude/0 — meaning "no gate" — is the correct
    value, not a real tier claim).
  - Verified via a scripted read-back of all 34 recipes confirming every
    tier's required level resolved correctly, not just that individual
    values parsed.
  - **Immediate effect:** the previously-documented "known, expected
    placeholder behavior" of all 5 tool tiers being craftable side by
    side with nothing gating the player (see the Knife/Hammer/Axe/Pickaxe
    entry below) is now real gating, not a placeholder — a fresh
    character can only craft Crude tools until Stonework reaches 10.
  *(Reported by Ben.)*
- [x] **Knife/Hammer/Axe/Pickaxe across all 5 CraftTiers — shipped
  2026-08-05, see `CHANGELOG.md` v0.1.69-dev.** Originally scoped 2026-08-04
  as "six base tools" (including Spear and Bow); a planning pass the next
  day resolved several open forks before building:
  - **Spear and Bow deferred entirely**, not part of this batch — neither
    has a function yet (no combat/damage/projectile system exists
    anywhere), and Bow's design-brief recipe (Stick + Rope) needs the
    unbuilt Textiles chain (Fiber/Fabric/Rope, Sewing skill). Revisit once
    combat exists and there's a real reason to give them stats.
  - **Consolidated, not duplicated:** the existing `Rock Knife`/
    `Rock Hammer`/`Axe`/`Pickaxe` became the Crude tier in place (renamed,
    same GUIDs) rather than sitting alongside 30 brand-new parallel items.
  - **Recipes are identical across all 5 tiers of a tool for now** — pure
    scaffolding. **Skill-side gating shipped 2026-08-07 (`CHANGELOG.md`
    v0.1.80-dev):** crafting a given tier now requires trainedSkill at or
    above `CraftTierScale.SkillRequirement(tier)` (Crude 0, Rudimentary
    10, Normal 25, Fine 50, Masterwork 100) — a real progression gate now
    exists. Ingredient-quality-side of weakest-link (below) still doesn't
    — every tier still costs identical ingredients, so skill is the only
    thing gating tier today, not material quality too.
  - **Skill wiring deferred, not guessed at:** all 20 recipes train the
    existing `Crafting` skill rather than inventing Woodworking/Stonework/
    Forging assignments now — raised during planning that a Hammer alone
    plausibly touches at least 3 different future skills, with no way to
    know today which is right. Revisit once the refining pipeline (which
    is what would actually exercise those skills) is built.
  - `Admin spawn tab — shipped 2026-08-05` (`AdminSpawnScreen.cs`, Admin
    tab on the `` ` `` menu) landed first specifically to make testing this
    batch easier. See the follow-up item just below for its one known gap.
    *(Reported by Ben.)*
- [ ] **Apply the Boulder/Rock hybrid shape technique to the ore nodes too,
  once the rock/boulder look itself is finalized.** Ben's explicit intent
  (2026-08-04) — the ore nodes (Copper/Iron/Silver/Gold/Platinum) are still
  plain Sphere primitives. Deliberately not done yet: waiting until the
  rock/boulder shape (displaced-mesh body + clustered pebbles, `CHANGELOG.md`
  v0.1.62/63-dev) is confirmed good, since ore would reuse the exact same
  `GenerateDisplacedSphere`/`BuildClusteredRock`-style technique rather than
  reinventing it. Note the hidden-ore nodes (Silver/Gold/Platinum) would need
  this applied to *both* their hidden and revealed materials/meshes.
- [ ] **Full crafting/gathering/skills redesign — partially built.** See
  `docs/design-brief.md`'s **Crafting, Gathering & Skills Pipeline (2026-08-04,
  amended 2026-08-05)** section for the complete plan: 7 new refining skills
  (Mining, Woodworking, Stonework, Metalworking, Forging, Minting, Sewing),
  alongside existing Gathering — **8 total**, `Crafting` having retired as a
  distinct skill on 2026-08-05 (see next) — a weakest-link tier rule
  (skill vs. material quality), a full gather→refine→assemble material web
  (wood, stone, metal, textiles), tool-quality effects (yield/quality/speed),
  and a new click-once-and-locked interaction model that replaces the current
  punch-to-break mechanic entirely. Large, cross-cutting, and *decided in shape
  but not in exact numbers* — several sub-questions are explicitly still open
  (see that section's own "Still open" list).
  **New 2026-08-05:** a planning conversation following the tool-tier work
  above resolved three more open questions — every finished item now sorts
  into exactly one discipline skill by its *defining* material (not every
  ingredient); crafting an item trains that broad discipline *and* a narrow
  per-item proficiency together, with the broad skill also gating recipe
  unlocks, not just `CraftTier`; and a new, separate weapon-usage skill tier
  (Archery/Spear/Sword/Gun/Bare-handed) was named for whenever combat/hunting
  eventually exists. See the design-brief section for the full reasoning.
  **The discipline-sort half shipped same-day, v0.1.70-dev:** `Crafting`
  retired, 6 new discipline `SkillDefinition`s created, all 25 recipes
  repointed (20 tools → Stonework, 5 gadgets → no skill), and both
  `CraftingScreen`/`SkillsScreen` got sub-tabs to make the now much-longer
  lists navigable. **Still purely design, nothing built:** the narrow
  per-item-proficiency track (no data structure exists for it yet) and the
  weapon-usage skill tier (needs a combat system that doesn't exist).
  **First real material-web step shipped v0.1.71-dev:** Stick + Knife (held,
  not consumed) → Trimmed Stick, trains Woodworking — the first thing to
  ever populate that tab. `CraftingRecipe` gained a `requiredTools[]`/
  `requiredToolLabel` pair for this (a tool held but not consumed, distinct
  from `ingredients`), same "any tier counts" convention as
  `ResourceNode.requiredTools`.
  **Shipped so far:** the full ore ladder (Iron/Silver/Gold/Platinum Ore Nodes)
  and the Mining Face Shield hidden-ore detection mechanic (visual reveal +
  yield gating both, not just the visual half) — v0.1.60/61-dev. Also
  **Boulder + Rock** (v0.1.62-dev) — the new stone size tier (Boulder → Rock →
  Small Rock) got its shapes built (a hybrid displaced-mesh-body-plus-pebbles
  look) and Boulder → Rock wired through the *existing* punch mechanic, but
  **Rock → Small Rock is still not built** — that specific refinement step (a
  recipe? a separate mineable object? never decided) remains exactly as open
  as it was when the tier was first discussed. See `CHANGELOG.md`.
  **Interaction model shipped v0.1.147-dev** — `IPunchable` is deleted;
  `ResourceNode`/`ChoppableTree` now use hold-and-release `IInteractable`
  (Ben's call over the design-brief's original tap-once-and-locked version —
  simpler, and the hold-progress plumbing already existed), with duration
  read from the player's live skill tier via `CraftTierScale.HoldDuration`/
  `TierForSkillLevel`. **Two real gaps left even within this piece:** tool
  tier doesn't speed up the hold on top of skill tier yet (the design-brief's
  own "Tool-quality effects" promise, not implemented), and the Crafting
  screen's own instant "Craft" button is still untimed — a different UI
  surface (menu-driven, not world-raycast) that needs its own progress/cancel
  affordance, deliberately deferred rather than folded into the same pass.
  **Still not built:** the Mining skill itself as an actual `SkillDefinition`
  (nodes currently still train `Gathering`, per what already existed, not the
  newly-decided `Mining` split — that decision hasn't been wired into code yet,
  confirmed again during the v0.1.147-dev work — every `ResourceNode` still
  points `trainedSkill` at `Gathering`),
  three of the six discipline skills (Metalworking/Forging/Minting —
  Woodworking, Stonework, and Sewing all now have real actions training them
  as of v0.1.78/79-dev), the **ingredient-quality half** of the weakest-link
  `CraftTier` determination (the **skill half** shipped 2026-08-07,
  v0.1.80-dev — see the Knife/Hammer/Axe/Pickaxe entry above), the full
  material web beyond wood/stone (metal, textiles — though Fiber/Rope/Cloth
  are now a real start, v0.1.77/78-dev), and the randomized-size-on-spawn/
  yield-scaling design for Boulder/Rock and Rock → Small Rock refinement.
  Don't start implementing any further piece of this without
  re-reading the full design-brief section first — it's too
  interlocking to build from memory of this one-line summary.
- [ ] **Magic System — three real wishes shipped (v0.1.148 through
  v0.1.155-dev), most of it still design-only.** See `docs/design-brief.md`'s
  **Magic System** section for the full plan. **Shipped:** Will (sixth
  vital, `PlayerVitals`, regens 1/5s), `SkillCategory.Magic` + 4 lineage
  `SkillDefinition`s, `PlayerMagic` (random starting lineage at spawn),
  `WishRecipe`, the `Magic` tab (`MagicScreen`), and three working wishes —
  **Spark** (Elemental, lights a `Campfire`), **Push** (Kinetic, shoves
  whatever loose Rigidbody you're aiming at), and **Heal Self**
  (Restoration, `Unconditional` targeting — no aiming needed, 10 health
  over 30 seconds via `PlayerVitals.StartHealOverTime`). **Illusion is
  still the only lineage with nothing.** **All magic activates with R**
  (v0.1.151-dev) via a new `IWishTarget` interface for specific targets
  like Campfire, with a generic-Rigidbody fallback for Push — Spark
  briefly rode E/`IInteractable` in v0.1.148-dev before being unified onto
  R. **No UI hint at all for any wish (v0.1.155-dev, deliberate)** — no
  prompt text, no progress bar, no Controls-tab entry; the only feedback
  is the world reacting or not, "something people play with in order to
  explore it" per Ben. Anyone testing/onboarding to this system needs to
  know that up front, or R will look completely broken/inert even when
  it's working correctly. **Player-selectable "default skill" (v0.1.152-dev):**
  `PlayerMagic.SelectedWish` (chosen via a Select button in the Magic
  tab) decides which wish R attempts, dispatched by a new
  `WishRecipe.WishTargeting` mode (`SpecificObject`/`AnyRigidbody`/
  `Unconditional` — Heal Self is the first real user of the last one).
  Still barely exercisable — each lineage has at most one wish, so
  selection auto-defaults and there's nothing to actually choose between
  until a lineage gets a second. All three wishes use the same
  skill-tiered hold mechanic gathering uses and the same success/failure
  roll (50%→90% by skill margin, mirroring `PlayerCrafting`'s
  chance-of-creation shape) — success costs 60 Will, failure costs 40 and
  still trains the skill (same numbers for all three so far, no reason
  given yet to differ). **Still not built:** Fireball (needs a combat
  system that doesn't exist), Illusion's own wish (still completely
  empty), found and scribed Scrolls, learnable additional lineages (both
  ride the not-yet-built Phase 2 skill-books mechanic — every character is
  permanently stuck on their one starting lineage until that's built), the
  Scribing skill itself, and tool-tier speed bonuses (same gap gathering
  has). **Real simplification, not an oversight:** no wish's roll
  weakest-links against any material/fuel-tier input — the design-brief's
  original weakest-link-quality idea for wishes was superseded by the
  success/failure roll instead, flagged directly in that doc, not left
  implying both are true. Don't assume any of the deferred pieces exist
  without checking — this is a large, only-partially-built system.
- [ ] **Building System — Foundation + Plank upgrade, shipped v0.1.156
  through v0.1.157-dev, most of it still design-only.** See
  `docs/design-brief.md`'s **Building System** section for the full
  plan. **Shipped:** `BuildPiece`/`BuildSocket` data shapes,
  `PlayerBuilding` (full placement state machine — free placement *and*
  edge-snapping both work, Left Mouse Button + scroll wheel per the
  Valheim/Rust/Raft-borrowed scheme), a new `Build` tab (`BuildScreen`,
  fully visible on purpose — unlike Magic, Building is meant to show its
  costs/prompts/ghost preview), **Foundation** (5m×5m, 4 edge sockets,
  Twig material, 6 Stick + 3 Rope, Woodworking-trained), and
  **click-to-upgrade/5s-hold-to-destroy** (`PlayerPieceUpgrade`, its own
  dedicated interaction logic, not a reuse of `IInteractable` — releasing
  early is the upgrade action here, backwards from every other hold in
  the game) with a real upgrade target, **Plank Foundation** (8 Plank).
  Requires a Hammer (any tier) in hand for both actions; destroy refunds
  nothing. Two panels correctly tile edge-to-edge, with the second
  inheriting the first's exact top height; upgrading preserves existing
  snap connections, destroying frees them. **Scoped down from the
  design, flagged not hidden:** Foundation/Plank Foundation are flat
  slabs with no support-column/stilt visual — the design doc's
  buried-block-vs-stilts question is still open; the 5-second destroy
  hold shows a text countdown only, no graphical bar. **Nails + a
  buildable Storage Box shipped v0.1.160-dev/161-dev** — `Nail.asset`
  (1 Iron → 5 Nails, requires any Hammer tier in hand + a nearby
  `AnvilSurface`; trains **Metalworking**, not Forging as originally
  speculated here — a real decision made when actually building it, not
  an error) and `StorageBoxPiece` (4 Plank + 6 Nail, a real
  `BuildPiece` reusing the existing `StorageBox`/`Inventory`
  components exactly as planned, plus pick-up-when-empty support added
  to `StorageBox.cs` itself). **Wall shipped v0.1.180-dev** — a real
  placeable Twig Wall (modeled and textured entirely in Blender, no
  Tripo3D), snapping to a Foundation edge via a new `FoundationEdge`↔
  `WallBottom` `BuildSocket` pairing and real per-socket placement math
  in `PlayerBuilding` (previously only Foundation-to-Foundation flat
  tiling worked). Shipped at 5.1m × ~2.6m, not the design-brief's
  spec'd 3m height — a real, not-yet-resolved deviation, see that doc's
  Building System section. **Still not built:** Pole, Door (both meant
  to reuse this exact same machinery, not a second pass),
  Floor/Ceiling/Window/Roof, Stairs/Ramps (vertical connectors — need a
  new two-height socket shape), Shelves/furniture (mount to Wall, not
  designed), Rock/Metal material tiers beyond Nails (blocked on their
  own crafting-pipeline chains), mixed-material-structure rules,
  structural-integrity requirements beyond "a socket exists,"
  Equip-to-Define (no equipment-function system for a shell to plug
  into yet), and territory/ownership restrictions (no multiplayer/
  macro-layer exists). Don't assume any deferred piece exists without
  checking.
- [ ] **Simplify item-holding to two states: equipped or inventory-stored — no
  ad-hoc "held in a hand" third state.** Today `PlayerLoot`'s pickup priority is
  Backpack → Left Hand → Right Hand → evict-into-world (`CHANGELOG.md`
  v0.1.10-dev/v0.1.15-dev), and a plain picked-up item can sit directly in a hand
  slot as an in-between state: not equipped (no Equip button was ever pressed)
  and not really "inventory" either. Requested target design:
  - Every object is always either **equipped** into a named equipment slot, or
    **stored** in an inventory slot (main inventory / backpack / storage box) —
    eliminate that third, ad-hoc "just sitting in a hand" holding state.
  - **Pickup — decided:** `PlayerLoot`'s existing Backpack-first priority stays
    unchanged. This rule fills the specific gap in the *current* fallback
    instead — today, when no backpack is equipped and both hands are already
    occupied by non-stacking items, the pickup evicts (physically drops)
    whatever's in Left Hand to make room. Replace that eviction with: route the
    new item into an inventory slot instead. Full resulting order: Backpack (if
    equipped) → a free hand (Left, then Right) → an inventory slot → drop to the
    ground if the inventory is also full (picking something up should never
    silently fail or destroy value).
  - **Unequip:** the item goes to an inventory slot; if every inventory slot is
    full, drop it to the ground instead of failing. (`PlayerBackpack.Unequip`
    already has this exact fallback chain — extend the same guarantee to every
    equippable: Canteen, Sunglasses, NavigationComputer, PersonalHealthMonitor.)
  - **Manual drop from inventory:** unchanged — goes straight to the ground.

  *(Reported by Ben. The despawn timer on dropped items that was originally
  part of this same request shipped separately in `v0.1.48-dev` (15 min),
  shortened to 2 min and extended to cover equipment/coins too in
  `v0.1.85-dev` — see `CHANGELOG.md` for both. Still doesn't cover the
  equipped-item unequip-fallback drop path described above, since that
  path isn't built yet either — despawn now covers every *existing* drop
  action, not this still-hypothetical one.)*
- [ ] **Equip directly from a container.** Same underlying gap as "Eat/Drink
  directly from a container" below — `DrawContainerContents` (backpack contents
  and storage boxes alike) treats every item as a generic move-popup button
  regardless of `entry.equipment`, so an equippable item sitting in a backpack
  (Sunglasses, a spare Canteen, Navigation Computer, Personal Health Monitor) has
  no direct Equip button; it has to be moved out to a hand or the main inventory
  first. *(Reported by Ben.)*
- [x] **Eat directly from a container — fixed v0.1.161-dev, same fix as
  "Can't eat a Berry" above.** Food items sitting in a backpack (or other
  container) couldn't be eaten in place — `DrawInventorySection` in
  `InventoryScreen.cs` gives main-inventory items a direct "Eat" button via
  `PlayerEating.FindEdible`/`TryEat`, but `DrawContainerContents` (used for a worn
  backpack's contents and nearby storage boxes) only offered the generic "where
  should this go?" move popup for every item, edible or not. Now fixed generically
  for every popup use (hand slots too, not just containers) via
  `PlayerEating.TryEatFrom` — see the Berry entry above for the full detail.
  **Note: Drink/fill from a container is a separate, still-open gap** — the
  fix only added an Eat button, not Drink/Fill (see below).
- [x] **Drink/fill directly from a container — fixed v0.3.6-dev.** Same gap
  as the Eat/Apply-from-container fixes above, for a Canteen sitting in a
  backpack/storage box. The generic move popup now shows real Drink/Fill
  buttons when the selected slot holds a Canteen, via a new
  `pendingMoveEquipment` field (the physical instance, not just the item
  type — Drink/Fill mutate the Canteen directly rather than consuming a
  stack). Full story in `CHANGELOG.md`.
