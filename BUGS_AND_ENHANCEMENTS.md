# Bugs & Enhancements

Known issues and requested features not being worked right now. Not a replacement
for `WORKING_ON.md` (that's for active work) or `CHANGELOG.md` (that's for shipped
work) — this is the backlog between the two. Check off and move the entry to
`CHANGELOG.md` once it's actually fixed/built.

## No placed/built structure tracks who owns it (found 2026-08-23, not started)

Confirmed directly (not assumed): `CapturePlacedPiece` (`SaveManager.cs`)
only saves `buildPiece`/`position`/`yaw` (plus a Village Flag's own
display name) — there's no `placedBy`/owner field anywhere on
`PlacedPiece`, `VillageFlag`, `StorageBox`, `Furnace`, or any other
build-piece-shaped object. Surfaced during the persistence restructure
(chunk 2, `MULTIPLAYER_PLANNING.md` section 3 item 5) when Ben asked
whether a placed Village Flag is tagged with who placed it, needed for
Team/Guild territory later — it isn't, for anything.

**Ben's explicit scope, 2026-08-23**: every *placed/built* structure
needs a real owner — buildings, Furnace, Anvil, StorageBox, VendorStall,
GardenPlot, Village Flag, anything with a fixed world position.
Deliberately **not** portable items — food, tools, clothing, gear
(regular inventory/equipment) should stay untracked, since those already
just belong to whoever's currently carrying them.

This isn't a new requirement — `TEAMS_AND_GUILDS_PLANNING.md` already
calls out that territory needs "the builder explicitly picks the actual
owner at placement time" for both Team Flags and Guild markers, and
`COMMERCE_PLANNING.md` hit the identical gap for `VendorStall`'s stock
box (`StorageBox` "has no ownership concept," logged as a real
prerequisite there too). This is the same underlying gap, now confirmed
to apply project-wide across every placeable, not just those two cases.

**Real prerequisite that just landed**: `PlayerIdentity.PlayerId`
(persistence chunk 2, `MULTIPLAYER_PLANNING.md` section 3 item 5) gives
this a real, stable id to actually stamp a `placedBy` field with — it
didn't exist before tonight. Building this now would mean adding a
`placedBy` (or similar) field to `PlacedPiece` (and any placeable type
that isn't routed through it), set at placement time from the placing
player's `PlayerId`, captured/restored alongside the rest of each
piece's save data. Not started — logged here so it isn't lost before
Team/Guild territory design actually needs it.

## A hold-progress bar got stuck on screen after casting Heal Self (found 2026-08-23, not investigated)

During Multiplayer sub-phase 4 live-testing (v0.3.181-dev+, right after
confirming the new Magic Command — Heal Self itself worked correctly),
an empty progress-bar outline (no visible fill) appeared on screen and
stayed there persistently — confirmed by Ben to remain even after
releasing every key and looking away from any object entirely, ruling
out "just a real in-progress E-hold on some interactable." No Bow was
being drawn at the time (ruled out `PlayerRangedCombat`'s own draw bar,
same `GUI.Box` look). Pressing E again (while a Bow happened to be
equipped but not in use) made the bar disappear. Shape matches
`PlayerInteraction.DrawHoldBar` (the ordinary E-hold progress bar for
non-instant `IInteractable`s like chopping/mining), but the exact
mechanism wasn't traced — `ResolveTarget()` resets `current` to null
unconditionally at the top of every `Update()`, so a naive read of the
code doesn't obviously explain a bar that survives looking away. Not
yet confirmed whether this is a real regression from `PlayerInteraction`
being converted to a `NetworkBehaviour` + gaining the `RequestWish`/
`CmdWish` Command in the same session, or a pre-existing bug that just
happened to surface during this test. Needs a clean, isolated repro
(cast a wish with nothing else touched, watch for the bar) with debug
logging in `OnGUI`/`ResolveTarget` to nail down the exact state
(`current`, `holdProgress`) when it appears.

## A dropped Skill Book vanished shortly after dropping (found 2026-08-22, non-reproducible, not investigated)

During Multiplayer sub-phase 2 live-testing (v0.3.163-dev): Ben dropped a
Skill Book from Inventory, saw it on the ground briefly, then it
disappeared with no Console error. Ruled out the obvious explanation —
`PlayerDropping`'s equipment despawn delay is 120s, not instant. Did NOT
reproduce with a different book dropped right after, so this looks
specific to that one book instance rather than a systemic bug — possibly
some leftover state from earlier testing in the same long session. Likely
unrelated to the `PlayerInventory` `NetworkBehaviour` conversion that
prompted the test (pickup and a separate Stick drop both worked cleanly
in the same pass). Worth a clean, isolated re-test with a fresh book if
it recurs.

## Player naming needs a real profanity filter before multiplayer (found 2026-08-22, not started)

`PlayerIdentity.cs` (built 2026-08-22, player naming) only does basic
sanitization on a chosen name — trim, 30-char length cap, strip non-
printable/control characters. No profanity/inappropriate-content
filtering exists anywhere in this codebase, for ANY rename flow
(`StorageBox`, `Village Flag`, NPCs, and now the player all accept any
non-empty string). That's an acceptable gap in single-player today —
nobody else ever sees these names — but the player's own display name
is explicitly being built as groundwork for multiplayer identity
(`MULTIPLAYER_PLANNING.md`), where it becomes visible to, and could be
used to harass, real other people. A real filter needs a maintained
word-list, leetspeak/spacing-trick normalization, and care around false
positives on legitimate substrings (the classic "Scunthorpe problem") —
genuine scope on its own, not something to bolt on as a side effect of
the naming feature. Must exist before player names are shown to other
real players; not required before then.

## Iron Arrow reported flying backwards, same symptom as the original Stone
Arrow bug (reported 2026-08-22, not yet reproduced/confirmed)

Ben: "iron arrows seem to be backwards like the first stone arrows" —
directly observed while watching one fly (fletching led, arrowhead
trailed), matching the exact symptom `CLAUDE.md`'s own
`Quaternion.LookRotation`-fights-a-nested-model's-baked-rotation gotcha
already documents and fixed for arrows in general (2026-08-16,
`FlyingArrow.Launch()`'s `Quaternion.Euler(0, 180, 0)` correction).

**A real code check found this genuinely surprising, not yet explained**:
`PlayerRangedCombat.SpawnFlightVisual` instantiates a single fixed
`arrowFlightVisualPrefab` for every shot regardless of which arrow item
was actually fired — no per-material mesh/rotation swap anywhere in
`FlyingArrow.cs`. That means Stone and Iron arrows fly using the
literal same generic visual object and the identical orientation
correction; by the code alone, they should look identical in flight,
not one fixed and one still backwards.

**Real open question, not yet resolved**: was the backwards orientation
seen mid-flight (the already-"fixed" code path — if so, something's
regressed or my reading of the code is missing something) or while the
Iron Arrow was just held/equipped in the off-hand before firing (a
genuinely different, never-touched code path — the original 2026-08-16
fix explicitly only corrected the in-flight visual, and its own comment
notes the model's baked rotation is tuned for its *equipped* context,
not flight, implying the held orientation was never separately
verified). Ben's plan: log for now, re-test live with a real screenshot
to pin down which case this actually is before investigating further.

## UI reference: Valheim's Crafting/Inventory screen design (found 2026-08-22, not started)

Ben shared a screenshot of Valheim's crafting/inventory UI as a reference
worth aiming toward eventually. Real, specific differences from this
project's current plain-IMGUI text-tile screens worth naming: an
icon-grid inventory (items shown as art in a grid, not a text row/tile
each needing its own label), a detailed hover tooltip on a specific item
(crafted-by name, weight, quality/durability, armor value — richer than
this project's current inline stat lines), and a crafting panel with a
clean recipe list down one side, a big preview + full stat breakdown
(damage, block armor, parry bonus, knockback, movement-speed penalty,
etc.) for the selected recipe, and live material-cost icons with a
single Craft button. This is a genuinely different visual/UX paradigm
than the current tile-grid `OnGUI` approach (Build/Crafting/Vendor Stall
screens all just got moved *to* tile grids this session, still text-and-
icon tiles, not full art-forward panels like this). Not scoped or
started — a real future UI redesign pass, not a quick reskin, given how
much of the game's screens would need touching to match this level of
visual polish. Worth keeping as a north-star reference for whenever a
real UI/UX pass happens.

## Multiplayer: gated structures need real ownership/visibility rules (found 2026-08-22, not started)

Ben's idea, flagged while building the Vendor Stall/Bank Box's Flag-gated
placement: once real multiplayer exists, a settlement-gated structure
(Vendor Stall, Bank Box, City Statue) shouldn't just be visible/usable by
anyone who happens to walk up — a player who doesn't own (or isn't on
the team/guild that owns) the linked Village Flag arguably shouldn't see
or interact with a structure gated behind that Flag's own founding
conditions. Today everything is single-player, so there's no ownership
concept to check at all yet — this is explicitly a multiplayer-era
follow-up, same "blocked on player-identity/ownership infrastructure
that doesn't exist yet" shape as `MULTIPLAYER_PLANNING.md`'s other open
items, not something to design in detail now.

## Skill book trading needs per-instance vendor stock (found 2026-08-22, not started)

Flagged while designing the Vendor Stall's stocking system (see the
Vendor Stall design work above/`MVP2B_PLANNING.md`/`COMMERCE_PLANNING.md`)
— Ben's idea was letting the Vendor Stall buy/sell Skill Books, tying
`SKILL_BOOKS_PLANNING.md`'s system into Commerce. Real architectural
mismatch found before building it: a physical `SkillBook` instance
carries its own per-instance `TargetRecipe`/`TargetWish` — two books of
the same `ItemDefinition` ("Book") can teach completely different things
and should be priced completely differently. Every part of the
`VendorStall`/`VillageVendor` stocking design so far (price list, stock
box, `Inventory.AddItem(item, qty)` count-based stacking) assumes one
price per `ItemDefinition`, with no concept of per-instance value or
identity — the same class of problem `CLAUDE.md`'s own "generic
`Inventory.RemoveItem`/`AddItem` strip an item's `equipment` reference"
gotcha describes for worn equippables, just for a different instance-
carrying item type. Selling a written skill book through the vendor
system as currently designed would either silently lose its
`TargetRecipe`/`TargetWish` on transfer, or needs genuinely new
per-instance stock tracking (a price list entry keyed to a specific
book instance, not an `ItemDefinition`) that nothing in the current
design has. Real feature, not a quick add — logged separately rather
than folded into the current Vendor Stall build.

## Multiplayer: Vendor Stall funded by a bank account (found 2026-08-22, not started)

Ben's idea, explicitly framed as a multiplayer-era follow-up, not now:
once real multiplayer exists, a Vendor Stall's till could draw on a real
bank account to help fund larger purchases, rather than being limited to
its own slowly-regenerating till. Same shape as the player-built Bank
idea `COMMERCE_PLANNING.md` already deferred (a per-instance-ownable
account concept that only pays off once a second real player exists to
actually transact against) — this is a variant of that same blocked
prerequisite, not a new one. Note: the till's existing "can never drop
below 0" constraint (`BuyFromVisitor`'s till-coverage check, already
built and tested) already gives a real, working self-balancing effect
today — a vendor that only gets sold high-value goods with no buyers
will naturally stop buying until either its slow regen catches up or
players buy stock down to refill it. A bank-funded till would loosen
that constraint deliberately once multiplayer creates a real reason to.

## More food recipes using existing crops (found 2026-08-22, not started)

Flagged while designing the Vendor Stall's 2 dedicated seed slots (see the
Vendor Stall design work above/`MVP2B_PLANNING.md`/`COMMERCE_PLANNING.md`)
— seeds becoming reliably purchasable instead of wild-plant RNG-only
means a real, larger supply of Carrot/Corn/Ginger/Onion/Potato/Sweet
Potato/Turnip is coming, and checking the actual recipe data found a real
gap: of those 7 crops, only **Potato** is used as an ingredient anywhere
(Steak and Potatoes). Carrot, Corn, Ginger, Onion, Sweet Potato, and
Turnip each have their own raw `Edible` asset (eat-raw only) but are
never consumed by any `CraftingRecipe` or `CookableItem` — confirmed via
a direct grep of `Assets/Data/*.asset` for recipe/cookable references,
not assumed. A bigger reliable supply of items nothing actually cooks
with is a real design gap once the Vendor Stall makes them common rather
than rare wild-forage finds. Worth a real design pass (new Campfire/
Cookstove recipes, soups/stews per-crop or a combined dish, Cooking-skill
gates matching the existing `COOKING_SKILL_PLANNING.md` precedent) —
not scoped or started, just flagged so it doesn't get lost.

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
use), and all of Campfire's 6 inventories / Furnace's 3. **Live-tested
2026-08-17, full round trip confirmed for both structures**: a Campfire
came back lit, with the exact remaining fuel seconds, an active Fried
Egg recipe still mid-progress, and a previously-finished Fried Egg
already sitting in the output slot; the Furnace separately confirmed
with real Materials and Fuel contents intact after the same save → exit
→ restart cycle. Not just existence in either case — real state. (This
also needed the separate legacy-fixture migration below — the original
scene Campfire/Furnace predate `PlacedPiece`
entirely and had to be retroactively wired in first.)

- [x] **A renamed Village Flag lost its name on the *next* reload after
  that, found live by Ben (2026-08-17). Fixed same session.** Root cause
  found via a temporary diagnostic log (added specifically to chase
  this, then removed once fixed): `SaveManager.RestorePlacedPieces`
  re-instantiates a missing structure from its raw `BuildPiece.prefab`,
  which has no `PlacedPiece`/`SaveId` baked in — those only ever get
  added via `AddComponent` after placement. Adding `PlacedPiece` there
  triggers `RequireComponent`'s auto-add of `SaveId`, whose `Reset()`
  doesn't reliably fire for a runtime `AddComponent` (the exact same
  gotcha the original v0.3.119-dev placement-time fix already covers —
  just hit a second time, in the restore path instead of the placement
  path, and missed the first time). The freshly-added `SaveId.id` stayed
  `null`, and calling `AssignId(saveId)` on it threw a real
  `ArgumentNullException` inside `SaveIdRegistry.Unregister`
  (`Dictionary.TryGetValue(null, ...)`, confirmed live in the Console) —
  which silently aborted the rest of that loop iteration in
  `RestorePlacedPieces`, including the `villageName` restore step that
  runs right after. That's why the Flag itself kept coming back (its
  base restoration already completed before the crash) while its name
  quietly reverted to default every time.

  Two fixes, not one: `SaveIdRegistry.Unregister` now guards against a
  null/empty `Id` (the same defensive guard `Register` already had,
  protects every future caller of this pattern, not just this one call
  site); and `RestorePlacedPieces` (plus `RestoreNpcs`, same pattern,
  cheap insurance even though it hadn't shown the crash) now calls
  `GenerateIfMissing()` immediately before `AssignId()`, matching the
  original fix's own pattern. **Live-tested end to end**: renamed a
  Flag, saved (confirmed via the diagnostic log — full correct entry
  with the renamed `villageName` captured), exited, relaunched — the
  crash is gone and the mechanism is confirmed sound; the rename-survival
  round trip itself is the next thing to verify with the fix actually in
  place (the repro that found this bug happened *before* the fix, so a
  fresh round trip is still worth double-checking).

**✅ Fixed 2026-08-17.** A second, worse save gap found the previous
session: `PlayerMagic` didn't just fail to persist, it actively
re-randomized on every reload. Ben read a Skill Book gaining the
Elemental lineage (Spark) on top of an already-known Kinetic (Push), then
hit it live: a blank-screen crash forced a restart, and the Elemental
lineage was simply gone (a coincidental reroll of the same lineage on
restart made it briefly look intact before Ben checked the actual Magic
tab and caught it). Root cause exactly as diagnosed: `PlayerMagic
.Awake()` unconditionally picked a fresh random `StartingLineage` every
time, with no save-state check at all — `SaveManager.cs` had zero
references to `PlayerMagic`, no capture, no restore.

**Fixed**: `Awake()` now only randomizes for a genuinely new character,
guarded on `SaveManager.SaveExists` (the same pattern `GardenPlot4x4`'s
own fresh-start init already used). `SaveManager` gained a real
capture/restore pair — every known lineage (via `SkillDatabase`
resolution, reusing `PlayerMagic.LearnLineage`) and the selected wish
(new `PlayerMagic.FindWish`/`IdForWish`). A new `AssignRandomLineageIfNone`
keeps old save files (written before this fix, with no lineage data at
all) from ending up with zero magic — they get the same free random
lineage a new character would. Also fixed `MagicScreen.cs`'s "Lineage:"
header, which was still reading the single old `StartingLineage` field
and could show a stale/misleading lineage once a player knew more than
one — now lists every known lineage from the real `KnownLineages` set.
**Live-tested 2026-08-17, full confirmation**: read a second lineage
book (Elemental, on top of an already-known Restoration), saved, exited,
relaunched — both lineages survived, the Magic tab header correctly
listed both, both wishes (Heal Self and Spark) worked when cast, and
both trained their skills live (Restoration → 3.0, Elemental → 2.0).
Genuinely fixed, not just compile-verified.

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
- [x] **Furnace throughput vs. gathering rate — confirmed intentional, not
  a bug (2026-08-21).** Live-tested: 2 Mining NPCs gather ore roughly 20x
  faster than a single Furnace can smelt it (harvest tick ~3s vs. one
  60s serial smelt, one recipe active at a time — see
  `NPCGathering.harvestDuration`/`SmeltableItem.smeltDurationSeconds`).
  Raised as a possible bug/need for a second Furnace; Ben's call after a
  "be mean" pass laid out the actual ratio (a 2nd furnace would barely
  dent a ~20x mismatch) — **keep the bottleneck as-is on purpose**. It
  forces active worker management and creates a real, ongoing reason to
  keep expanding infrastructure/NPC headcount rather than "build once and
  forget it," which fits the game's overall progression better than
  smoothing the mismatch away. Also surfaced along the way (checked
  against real data, not assumed): Furnace's own tier (Crude→Masterwork,
  it's had a real `FurnaceBuildPiece` since some point — the "no
  BuildPiece/prefab for a Furnace" comment in `Furnace.cs` is stale)
  currently has **zero effect on throughput** — `MaxQueueSize`/
  `MaterialsCapacity`/`OutputCapacity`/`FuelCapacity` are flat constants
  and `smeltDurationSeconds` lives only on the recipe, unlike almost
  every other tiered system in this project (`CraftTierScale`). Not
  acted on given the decision above, but logged as the "if we ever do
  want Furnace tier to matter" hook, distinct from the settled
  throughput question. **Separate real bug, not closed by this
  decision**: an NPC whose deposit box is full gets stuck oscillating
  between "returning"/"idle" every ~1-2s indefinitely (`DepositCargo`
  silently drops unfit leftover, `UpdateReturning` immediately
  re-triggers next tick) rather than pausing/waiting visibly — confirmed
  live on Mining Dude the same session. Worth its own fix regardless of
  the throughput decision, since a frozen-looking NPC reads as broken,
  not as "the economy is working as intended."
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
- [ ] **Three more Furnace-shaped automated structures (raised 2026-08-20,
  Ben's framing: "part of our game strategy is automation of tasks to get
  to the end game"), not built — see `HUNTER_PLANNING.md` for the related
  Hunter NPC that would feed the Cookstove/Tanning Bench half of this.**
  Furnace's own automation shape (3 optional StorageBox links, on-board
  fuel/materials/output buffers, auto-refill/auto-drain, ticks with no
  player nearby) is real and reusable — but it's currently hardcoded onto
  the `Furnace` class itself, not factored into a shared base component,
  so each of these needs either a real "AutomatedProcessor" refactor or a
  copy-adapted clone of Furnace's shape. Checked against actual current
  data before logging this (not just assumed) — the three ideas are in
  very different states of readiness:
  - **Cookstove (auto food) — full plan now written, see
    `COOKSTOVE_PLANNING.md` (2026-08-20, revised same day).** A new
    dedicated `BuildPiece` structure (not automation bolted onto
    Campfire — its 1-slot fuel capacity was a deliberate "not meant to
    run unattended long" choice), gated to build at all on **Cooking 25**
    (`BuildPiece.trainedSkill`/`unlockTier`, the same fields every other
    buildable piece already has) — since Cooking is only trainable by
    hand-cooking at a Campfire, this genuinely mandates real Campfire use
    first (Ben's ask), for free. Once unlocked it hosts a real recipe
    list including Campfire's fancier recipes (Grilled Meat/Fried Egg/
    Steak and Potatoes — Ben's call), which turned out to need its own
    4-slot accessory system too (every one of those recipes needs a
    specific accessory in 100% of the existing data, not just skill) —
    Herbal Tea/Meat Stew stay permanently excluded regardless
    (`requiresCanteenWater`, no live player Canteen to draw from
    unattended). `BuildPiece` ingredient cost meant to require smelted
    Iron — a second progression gate stacked on the skill one. Otherwise
    a field-for-field copy of `Furnace.cs`'s queue/fuel/StorageBox-link
    logic, reusing the existing `FuelItem`/`FuelTier` assets as-is.
  - **Lumber Mill — actually two unrelated chains bundled under one
    name:** Log→Plank isn't a recipe at all anymore (deliberately removed
    in favor of the drop-and-chop mechanic, see the dropped-Log fix
    above) — automating it means reinventing a recipe just cut. Stick→
    Trimmed Stick (5 tiers) **already are real `CraftingRecipe`s** today
    and don't need a Furnace-clone at all — this is exactly the
    already-scoped "data-only follow-up" the bench-crafting/`NPCCrafting`
    system's own header comment already flags (a Woodworking
    `NPCJobDefinition` + a workbench marker, reusing the Metalworking
    pilot's machinery as-is) — the cheapest of everything in this list.
  - **Tanning Bench — premise doesn't match current state.** Leather
    already drops directly from Deer (its own independent hunting item,
    not Pelt-derived). Wolf Pelt, meanwhile, is a confirmed complete
    dead end — grepped every asset in the project, zero recipes
    reference it at all. So this isn't "automate an existing chain," it's
    "invent a brand-new Pelt→Leather recipe from scratch" — real and
    useful (closes a genuine dead-end item, gives the planned Hunter's
    Wolf-pelt scavenging somewhere to go) but the biggest lift of the
    three.
  Ranked cheapest to most work: Lumber Mill (Stick tier) < Cookstove <
  Lumber Mill (Log/Plank) ≈ Tanning Bench. Not scoped/ordered/committed —
  logged as a strategic direction (automation as the on-ramp to endgame),
  not a build plan yet.

## StorageBox nearby-section UI — same popup treatment as Campfire?

**Campfire's dedicated E-key popup UI shipped v0.3.28-dev** (was tracked
here as a backlog item; now in `CHANGELOG.md` and `CAMPFIRE_PLANNING.md`).
One open question from that work is still unresolved:

- [ ] **Open question, not decided:** should StorageBox's identical
  "nearby StorageBox" section (still living at the bottom of the
  Inventory tab, untouched by the Campfire change) get the same focused-
  popup treatment for consistency? Raised as a natural follow-on, not
  committed either way.
- [x] **Real bug in this section, found live 2026-08-19, fixed
  v0.3.149-dev: only the nearest of multiple nearby boxes was ever
  shown.** Two boxes placed next to each other made the second one
  completely inaccessible — `nearbyStorages` was already a full,
  distance-sorted list of every box in range, the display code just only
  ever indexed `[0]`. Fixed by drawing a section for every box in the
  list, reusing the exact same multi-call pattern the worn-containers
  section already uses safely. Not yet live-tested.

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

## NPC identification (raised 2026-08-17, real live-testing pain point)

With the Village Flag spawn loop now the only NPC source and multiple
hires accumulating in the world, Ben hit a genuine usability gap: every
NPC is visually and textually identical (only 2 base models — Male/
Female Kevin Iglesias — and every one defaults to the same hardcoded
"Factory Worker" name), making it hard to find the right one to give
tools/fire/pay once several are running around doing different jobs.
Audited before proposing fixes — confirmed via code read, not assumed:

- **NPCs can't be renamed at all** — no `IRenameable` on `NPCHiring`/
  `NPCDialogue`, unlike `StorageBox`/`VillageFlag`, which already have
  the full right-click-rename flow working.
- **No worldspace nametag** floats above an NPC in the 3D world — the
  name only ever shows inside `NPCDialogue`'s on-screen dialogue box
  during a Talk interaction.
- **No map markers for NPCs** — `MapScreen.cs` already draws live
  markers for every `VillageFlag` (`DrawFlagMarkers`, a fresh
  `FindObjectsByType<VillageFlag>()` scan every `OnGUI` frame, so
  markers track live position for free) but has no equivalent for
  `NPCHiring`.

**✅ Auto-naming + rename built same day (2026-08-17), items 1-2 of the
recommended first pass**:
1. **Auto-assigned name + gender at spawn**: `VillageFlagSpawner
   .hireableNpcPrefab` split into `hireableNpcPrefabMale`/
   `hireableNpcPrefabFemale` (both wired in `TestScene.unity`), a coin
   flip picks between them per spawn, and a new `NPCNameGenerator.
   PickUnique` (static male/female name lists) assigns a name —
   preferring one not already in use by a currently-active NPC, falling
   back to a random pick only once every name in the matching list is
   taken. `NPCDialogue.Configure(name, isFemale)` sets both together,
   called once at spawn.
2. **`NPCDialogue` now implements `IRenameable`** — same right-click
   `PlayerRenaming` flow `StorageBox`/`VillageFlag` already use, layered
   on top of the auto-assigned name as an override, not a replacement.
3. **Persisted correctly** — `SaveManager.CaptureNpc`/`RestoreNpc` now
   capture/restore both `name` and `isFemale`, and critically,
   `RestoreNpcs`' recreate-on-load path reads the saved gender *before*
   instantiating so a recreated NPC comes back as the same gender (and
   the correct model) it was, not a fresh coin flip. Old saves (no
   `name`/`isFemale` keys) are handled gracefully — a null/missing name
   leaves whatever the fresh `Instantiate` already carries untouched
   rather than blanking it.

Compile-verified (scene YAML confirmed for both new prefab slots); not
yet live-tested with a real spawn + save/reload round trip.

**✅ Map markers + NPC Roster screen both built same night (2026-08-17)**,
prompted directly by live-testing pain — diagnosing several different
NPCs one at a time (a wandering Miner, a frozen Guard) by physically
walking to and inspecting each was exactly the friction these were
meant to remove:

- **`MapScreen.DrawNpcMarkers`** — copied `DrawFlagMarkers`' exact
  pattern (a fresh `FindObjectsByType<NPCHiring>()` scan every `OnGUI`
  frame, so markers track real live position for free), blue dots
  labeled by name.
- **`NPCRosterScreen`, bound to `N`** (Ben's call — an NPC roster is
  about assets managed out in the world, closer in spirit to `M`/Map
  than to the Tab menu's player-self screens; checked for collision,
  `N` was unbound). Lists every `NPCHiring` in the scene with name/job/
  status/distance; "Manage" opens the exact same `NPCHiringScreen` a
  walk-up-and-E interaction would (not a second copy of that UI).
  `GameMenuScreen.ControlsList` updated.

Compile-verified, component confirmed added to the Player in
`TestScene.unity` via direct YAML check. **Live-confirmed 2026-08-18** —
Ben confirmed every Roster row shows Manage/Locate as expected, Manage
genuinely opens the real `NPCHiringScreen` (not a second copy), named
NPCs (auto-assigned + player-renamed) show correctly on the Map, and the
color-coded dots are visibly differentiated (green for working NPCs like
"Mining Dude"/"Wren", blue for unhired "Factory Worker" spawns), and the
markers genuinely track live position (a moving NPC's dot moves with it
in real time, not a stale per-open snapshot), and the Locate toggle
correctly flips to "Stop" when activated *and* actually shows a real
waypoint compass — pointer, name, and live distance ("Wren (17m)") —
and flips back to "Locate" (compass gone) on Stop, the full round trip.
**Payment-due toast confirmed 2026-08-18** — Ben saw the toast fire the
moment his NPC's work cycle completed, no Roster/Map check needed.
**"Pay All" also confirmed 2026-08-18** — closes out all 5 chunks of the
2026-08-17 NPC-management pass, every piece now live-confirmed.

**✅ A further "NPC management" pass built the same night (2026-08-17),
in 5 chunks, each compiled clean before moving to the next**:

1. **Tool Swap, not just Give.** `NPCJobScreen.DrawToolRequirements` used
   to only ever show a "Give" button while a tool slot was empty —
   upgrading an already-equipped tool meant firing the NPC and losing
   every other tool too. Now lists every owned tier the player could
   hand over (excluding whichever is already equipped), so a specific
   tier can be picked deliberately rather than `TryGiveTool`'s old
   "whichever comes first" behavior. New `NPCJob.SwapTool` handles both
   the empty-slot and already-equipped cases identically, returning the
   replaced tool to the player's inventory instead of destroying it.
   **Live-confirmed 2026-08-18** — Ben swapped a Miner's equipped
   Masterwork Pickaxe for a Fine one pulled straight from his worn
   Backpack, and confirmed the displaced Masterwork Pickaxe came back to
   his own inventory afterward instead of vanishing.
2. **`NPCFreeze`** — a "Frozen (stay in place)" checkbox toggle on
   `NPCHiringScreen` (`GUILayout.Toggle`, matching `FurnaceScreen`'s
   Auto-Run convention rather than a relabeled button). Built as a
   standalone, reusable component (optional `GetComponent` references,
   no `RequireComponent` chain) specifically so a future Traveling
   Trader — which won't be an `NPCHiring` at all per
   `COMMERCE_PLANNING.md` — can reuse it later instead of it being
   Hiring-screen-specific. Re-asserts pause every frame while frozen so
   it wins over any other system trying to unpause the same components.
   Added to both NPC prefabs, verified in the saved prefab YAML.
3. **Take / Take All cargo buttons.** `NPCHiringScreen.DrawCargo` used to
   be read-only — an unpaid or fired NPC's cargo was never actually lost
   (`Fire()`/`ClearJob()` don't touch `NPCCargo`), just permanently
   unreachable with no player-facing way to get it back. Reuses
   `InventoryTransfer.MoveAsManyAsFit`, same utility every other
   inventory-to-inventory transfer in this project already uses. Works
   remotely from the Roster too — `NPCHiringScreen.Open` has no
   proximity check.
4. **Deposit-anchored work-range leash**, prompted by the live Miner-
   wandering-far incident. `NPCGathering.searchRadius` re-centers on
   wherever the NPC currently stands, which let it drift outward
   indefinitely across successive hops — each hop only ever needed to
   be within range of wherever it had *already* wandered to, not of
   home. New `MaxRangeFromDeposit` (configurable via a text field +
   "Set" button on `NPCHiringScreen`, only shown for a Gathering NPC)
   is a second, independent check anchored to the NPC's actual
   `DepositContainer` position — a fixed point that can't drift no
   matter how many hops it's taken. Deliberately **not** anchored to
   the Village Flag — that's the right anchor for `NPCGuarding`'s
   existing patrol radius, but a Gatherer's real "home" is the box it
   walks back to, not a Flag that might be placed far from it.
5. **Color-coded Map markers + Roster "needs attention" tools.**
   `MapScreen.DrawNpcMarkers` now colors each dot by status (green =
   working, orange = waiting for payment, yellow = idle/missing tools,
   blue = not hired) instead of one flat color, so "who needs
   attention" reads at a glance without opening the Roster.
   `NPCRosterScreen` gained a waiting-count header + "Pay All" button
   (skips, doesn't block on, any NPC whose coin type isn't currently
   affordable), a per-row "Locate"/"Stop" toggle that drives a new
   waypoint compass (rotates to point toward the tracked NPC, drawn
   even while the Roster itself is closed), and `NPCHiring` gained a
   static `OnPaymentDue` event, subscribed to by a new
   `PlayerNPCPaymentToast` for a passive notification the moment any
   NPC's work cycle completes — checked its toast Y-position (270)
   against all 7 other existing top-center toasts in the project before
   picking it, the exact discipline the original `PlayerAutosave`/
   `PlayerCrafting` collision (v0.3.115-dev) established.

All 5 compiled clean; `PlayerNPCPaymentToast` confirmed added to the
Player via direct scene YAML check. **Chunk 3 (Take/Take All) live-
confirmed 2026-08-18** — Ben pulled an NPC's full cargo via Take All.
Chunks 1, 2, 4, 5 still not live-tested in Play mode.

**Related, logged separately**: the identical need applies to the
player once multiplayer exists (other players need a name to see) — see
`MULTIPLAYER_PLANNING.md`'s open questions. Decided there: entry via the
` menu's Player tab (a text field), not the right-click flow — renaming
yourself via right-click doesn't make sense the way it does for a
world object.

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

- [ ] **NPC "find door" pathing — Ben's idea, 2026-08-20, prompted by
  live-testing NPCs walking into walls trying to reach a StorageBox
  inside a walled building.** Not designed or built — logged as an idea.
  Checked before logging, not assumed: this is a genuine two-part build,
  not a quick pathing tweak.
  1. **Doors are player-only today.** `Door.Open()` takes the player's
     own `transform.position` directly and is wired to
     `ISecondaryInteractable` (F-key) — there's no NPC-safe way to open
     one at all, same class of gap as the skinning/`SkinnableCreature`
     discussion earlier tonight (a player-only action needing a parallel
     NPC-safe entry point, e.g. an `OpenForNPC()`-shaped generalization).
  2. **The routing itself needs real new logic.** No NavMesh exists in
     this project — every NPC mover is straight-line movement plus local
     raycast deflection (`NPCMovement.cs`). "Find the door" means
     detecting that a straight path to a target is blocked by a wall,
     searching for the nearest `Door` component within some radius, and
     injecting it as an intermediate waypoint before continuing toward
     the real target — a real new behavior layered on top of
     `NPCMovement`, not a config value.
  Given the size, this would get the same "real design pass before
  touching code" treatment as the Hunter/Cookstove plans from earlier
  tonight, not a quick patch. Not scoped or ordered yet.

- [ ] **New-player-experience gaps found live during the 2026-08-19
  playtest (Ben's own framing: "what would a new player need to get
  going").** Six real ideas, none built:
  - **A Bed/rest mechanic** — genuinely doesn't exist. Confirmed via
    code: Health only has a flat, always-on `healthRegenPerSecond = 0.05`
    passive trickle (~33 real minutes for a full heal), nothing
    accelerates it. A new mechanic, not a fix.
  - **A Quick Access Bar / hotbar** — numbered slots assignable to tools/
    weapons/items, a keypress equips directly to a hand and swaps
    whatever was equipped back to inventory. Not a new system underneath
    — `PlayerEquipment` already has hand slots and an Equip action, this
    would add a quick-slot UI layer that triggers the same logic.
  - **Relocate a placed structure** — only Deconstruct exists today; a
    badly-placed StorageBox/Furnace means losing it and rebuilding from
    scratch rather than repositioning it. **Ben's follow-up, 2026-08-21:
    specifically wants a drag-and-drop-style reposition** — grab an
    already-placed piece and move it to better placement directly,
    rather than a menu-driven "pick up, walk, place again" flow. Ties
    directly into tonight's real live-testing pain points (StorageBox/
    Bookshelf/Desk needing precise `groundOffset` tuning to sit right,
    Foundation-tiling needing precise aim) — a drag-reposition tool
    would make correcting a slightly-off placement much less punishing
    than the current pick-up/re-place-from-scratch loop. Not designed —
    real open questions: does it reuse `PlayerBuilding`'s existing ghost-
    preview/snap machinery (probably yes, closest existing analog), does
    it require a tool in hand (Hammer, matching `PlayerPieceUpgrade`'s
    existing gate), and does relocating a Foundation need to also carry
    whatever's resting on top of it (Anvil/Furnace/StorageBoxes) or
    leave them behind.
  - **A "Cure Thirst" wish for Restoration** — `MAGIC_PLANNING.md`
    already sketched a Tier 2 "status-cure" slot for Restoration
    (deliberately not Heal Other, to avoid Medicine redundancy) but never
    said what it cures; this is a concrete, on-theme proposal for that
    exact slot.
  - **Larger storage capacity options** — existing `StorageBox` tiers
    fill up too fast under real sustained NPC-driven automation (multiple
    NPCs continuously depositing, not a player manually managing one
    box). Related to but distinct from the item above — that one's about
    *knowing* a box is full, this one's about the underlying capacity
    being too small in the first place.
  - **Tool tier should improve yield, not just gate access** — Axe/
    Pickaxe/etc. tier currently only determines *whether* you can harvest
    something, never *how much* you get. Would bring wood/ore gathering
    in line with how tiered gear already works everywhere else in this
    project (Iron Arrow damage, Backpack capacity, ...).
  - **NPC target-type filter** (Mining: restrict to a specific ore type;
    Woodworking: prefer Logs over standing Trees) — raised alongside the
    Bed idea, then "be mean"-critiqued live: scope shrank once the
    dead-end "Log" item bug (see `## Bugs` above, fixed v0.3.146-dev) was
    found, since there was previously no real reason to ever prefer
    felling Trees (useless raw-Log cargo) over chopping placed Logs
    (useful Plank output) — that imbalance is now largely resolved by the
    bug fix itself. Genuinely useful for Mining ore-type restriction
    either way (e.g. "keep my Miner on Iron only"); the Woodworking half
    may not need its own toggle now that Logs are the correct default.
  - **NPCs work too fast — may unbalance resource gathering (Ben's idea,
    2026-08-19).** Raised live after watching Iris fell/collect at a pace
    that felt too efficient relative to player-paced gathering. Not
    scoped — could mean a slower base harvest duration, a real work-speed
    stat, or something else; needs a design pass before any numbers.
    **Ben's own follow-up idea**: rather than slowing the NPC's own
    action speed directly, lengthen `ResourceNode`/`ChoppableTree`'s own
    `respawnDelay`/`regrowDelay` — the node itself becomes the throttle,
    which also slows down player-paced gathering the same way (consistent
    scarcity for everyone) rather than only NPCs feeling artificially
    slower than a player at the same task. Not scoped/built.
  - [x] **A dropped Log couldn't be chopped, only picked back up (found
    live, 2026-08-19) — fixed same night, v0.3.150-dev.** Was working
    exactly as built, not a bug, but inconsistent with a felled Tree:
    `Log.asset.worldPickupPrefab` pointed to `LogPickup.prefab`, a plain
    `Pickup` with no chop mechanic, distinct from the real choppable
    `Log.prefab` (`ResourceNode`) a felled Tree actually spawns. Fixed
    with a one-line swap — `worldPickupPrefab` now points at the real
    choppable `Log.prefab` instead. Confirmed safe first:
    `PlayerDropping.SpawnPickup` already gracefully handles a prefab with
    no `Pickup` component. **Made the `LogToPlankRecipe` crafting recipe
    (built earlier the same session) redundant** — Ben's call: removed
    it, v0.3.150-dev, since drop-and-chop is strictly better (same Plank
    output, plus a Stick chance the plain recipe never had).
    **Follow-up bug, found live the same night, fixed v0.3.151-dev**: the
    original fix didn't preserve dropped quantity — breaking 5 dropped
    Logs only ever yielded 2 Planks total (one node's worth), since
    `SpawnPickup` only applied `count` via `Pickup.Configure`, which
    `Log.prefab` doesn't have. Fixed generally (not Log-specifically):
    when the spawned prefab isn't a stack-representable `Pickup`,
    `SpawnPickup` now spawns the remaining `count - 1` as separate
    instances, scattered the same way `ChoppableTree.Complete()` already
    scatters a felled tree's own logs — also fixes Admin-spawning
    multiple Logs at once for free, since `AdminSpawnScreen` shares the
    same method. Compile-verified, not yet live-tested.
- [ ] **Cursor object-inspector debug overlay (designed 2026-08-20, not
  built — Ben's ask, logged as a plan rather than built now).** A small
  corner-of-screen debug panel that IDs whatever GameObject the cursor is
  currently over, meant to speed up future diagnosis of exactly the kind
  of "which prefab/mesh is this actually" confusion this session hit
  twice live while rebuilding the Anvil (grabbing the wrong ancestor via
  `.transform.root`, then grabbing a random scattered Boulder via
  `FindFirstObjectByType<AnvilSurface>()` instead of the real one).
  Design, not yet built:
  - New Editor-only script (working name `CursorObjectInspector.cs`),
    styled like the existing `DebugGUI` panel — a toggle key (off by
    default, add to `GameMenuScreen.ControlsList` once built per this
    file's standing new-key-binding rule).
  - Raycasts from the camera every frame against all layers, with
    `QueryTriggerInteraction.Collide` so trigger-only colliders (a common
    shape in this project) still get picked up, not just the gameplay
    interact layer/mask.
  - Resolves identity via Unity's own
    `PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot`, not a
    hand-rolled hierarchy walk — chosen specifically after this session's
    two related hierarchy-tracing mistakes.
  - Displays the resolved prefab asset path *and* its guid together (a
    bare guid alone isn't searchable/readable at a glance), plus,
    separately, the hit object's own mesh asset guid when it has one —
    the real Anvil bug was a mesh-level mismatch inside an
    otherwise-correctly-resolving object, so the mesh guid needs its own
    line rather than being assumed to match the prefab's.
  - Must be wrapped in `#if UNITY_EDITOR` — `PrefabUtility`/
    `AssetDatabase` don't exist in a compiled Player build, so this is a
    permanent Editor-only limitation, not a "build it later" gap.
- [x] **"Next NPC visit" countdown — built and live-confirmed 2026-08-21;
  "Next Trader visit" half still blocked.**
  - **NPC visit countdown: done.** `VillageFlagSpawner.SecondsUntilNextSpawn()`
    exposes the real countdown, shown on `NPCRosterScreen` as "Next NPC
    visit: Xm Ys" right under the header. Also gained real save/restore
    (`spawnTimerSeconds` was never persisted before — every reload
    silently reset the up-to-30-minute wait to zero). Live-confirmed
    across a real multi-spawn session, including the countdown matching
    the hand-computed formula exactly at each new spawn.
  - **Trader visit countdown has nothing to surface yet.** There is no
    Traveling Trader spawn system at all — `VILLAGE_FLAG_PLANNING.md`
    explicitly built only the reusable spawn-and-seek mechanism the
    Trader would eventually reuse, and `COMMERCE_PLANNING.md` still lists
    Traveling Trader as blocked behind the Village Vendor driver shipping
    first. A "next Trader" countdown can't be built before the Trader's
    own spawn timer exists — this half is blocked on Commerce, not a UI
    gap.
- [ ] **A deposit `StorageBox` filling up should notify the player, not
  just silently sit there (Ben's idea, 2026-08-18).** A full box the
  player hasn't noticed can silently strand a Gathering NPC's cargo or
  stall its work loop (it walks back to deposit, has nowhere to put
  anything, presumably keeps trying) with no way to find out short of
  physically checking. Worth a toast (same pattern as
  `PlayerNPCPaymentToast`) fired once a linked deposit box crosses some
  "nearly full" threshold, or once an NPC's own deposit attempt actually
  fails to fit anything. Not scoped/built — needs a decision on the
  exact trigger condition (box capacity threshold vs. an actual failed-
  deposit event) before implementation.
  **Partially addressed 2026-08-21**: the actual broken *behavior* this
  caused (an NPC oscillating between "returning"/"idle" every 1-2s
  forever once its box filled, confirmed live) is fixed —
  `NPCGathering.UpdateReturning` now parks cleanly and reports "waiting
  — deposit box 'X' is full, holding cargo" instead of thrashing. That's
  still only visible via the debug-log status text, not a proactive
  player-facing toast — this entry's original ask (notify the player
  without them having to go look) is still open.
- [ ] **Only 1 of the Leather Backpack ladder's 5 tiers has a recipe at
  all (Ben's ask, 2026-08-18).** Checked directly: `Assets/Data/` only
  has `LeatherBackpackRecipe.asset` (the plain/Normal tier, just fixed to
  use real Leather+Rope) — Crude/Rudimentary/Fine/Masterwork Leather
  Backpack are all real `ItemDefinition`s (weights already tuned, see the
  v0.3.139-dev weight pass) with **no way to craft them at all**. Real
  open design question before building, not just "copy the pattern 4
  more times": which convention does this family follow? The plain
  Backpack/Knife/Bow ladder uses a skill+`CraftOutcomeRoll` quality
  ladder off one fixed recipe (better Sewing skill → better tier, same
  ingredients every time); Stone/Iron Arrow instead uses **tier-matched
  ingredients** (a Crude Trimmed Stick deterministically produces a Crude
  Arrow, no roll). Leather Backpack's single existing recipe has no
  `lowerTierItem`/`higherTierItem` wired and no obvious tier-matched raw
  material the way Arrow has Trimmed Stick — needs a decision on which
  shape before the other 4 tiers get built, not assumed from the ask
  alone.
- [ ] **A Guard below ~30% health should pause patrolling and heal in
  place, still fighting back if attacked (Ben's idea, 2026-08-18).**
  `NPCVitals` regen (1 HP/sec) already only runs while `!IsFighting`
  (i.e. while `Patrolling`, not `Chasing`/`Attacking`), so this wouldn't
  change the regen *rate* — a wounded Guard already heals whenever it's
  not mid-fight. What it would actually buy: right now a critically
  wounded Guard keeps walking its patrol circle exactly like a full-health
  one, so it can wander into a *second* threat before recovering from the
  first. Holding position instead reduces that exposure without going
  passive — `FindNearestThreat()` already runs every frame ahead of the
  `Patrolling` branch, so "still fights if attacked" falls out for free,
  no change needed to `Chasing`/`Attacking` at all, just skip the
  `UpdatePatrol()` movement call while below threshold.
  Two things worth deciding before building: (1) **hysteresis** — this
  file already debounces two other flicker risks this way
  (`ApproachTolerance`, `wasActive`); resuming patrol at the same 30%
  line it paused at would let it flap right at the boundary, so pause at
  30% but only resume once healed back up to something higher (e.g.
  60%). (2) **hold position vs. retreat toward the Flag** — freezing in
  place is the simple version; retreating toward the Flag while healing
  reads better but is a bigger change. Not scoped/built.
- [ ] **Gameplay audio system — genuinely doesn't exist yet; a survey of
  every imported asset pack found nothing worth reusing (2026-08-16).
  Reclassified from Bugs to Enhancements, 2026-08-18 — this was never a
  regression, just a system that hasn't been built yet.** Prompted by
  traskmi reporting he heard rain in a live session — real, confirmed via
  `WeatherMakerFallingParticleScript`'s own Light/Medium/Heavy
  `AudioSource` trio (see `CLAUDE.md`'s Weather Maker section for the full
  mechanism), riding on the `AudioListener` already added to the Player
  for Weather Maker. That's ambient weather audio only, not a general
  system — no combat hit sounds, arrow whoosh, footsteps, crafting/UI
  sounds, or anything player-triggered exists anywhere. Checked whether
  any already-imported pack has usable audio sitting dormant before
  assuming a from-scratch build is the only option: `LJPackages` (All
  Seasons environment pack) ships one ambient sound file each for
  Desert/Spring/Winter, unreferenced by `TestScene.unity`; `Mirror/
  Examples` bundles its own demo audio (Kenney RPG pack + 10 OpenGameArt
  sounds), also unreferenced — both are generic pack filler, not tailored
  to this game, not worth wiring up as a shortcut. Rabbits, Animal pack
  deluxe v2, ithappy, Kevin Iglesias, and NV3D ship no audio at all.
  **`Assets/Audio/` already exists as an empty scaffold** (just a
  `.gitkeep`) — presumably set up in advance by an earlier session for
  whenever this actually gets built, no content in it yet. Real system
  design (which events trigger what, how clips get sourced/generated,
  mixer setup) not started.
- [x] **Campfire cooking was single-shot only (no auto-repeat) and had
  no auto-relight — found live by Ben (2026-08-18) asking "is that by
  design?" No, both were just gaps. Fixed same night, v0.3.126-dev.**
  `Campfire` gained an opt-in `Auto-Run` toggle (off by default, same
  shape as `Furnace.AutoRunEnabled`/`FurnaceScreen`'s own toggle): when
  on, `Update()` calls `TryLight()` whenever unlit with fuel still
  present, and `TickCooking()` re-calls `StartCooking()` with the same
  recipe right after one finishes (safe to call unconditionally —
  `StartCooking` already refuses if ingredients/accessory/skill/output
  space aren't satisfiable, so no new checks were needed). Saved/restored
  via a new `["autoRun"]` key alongside the rest of Campfire's state.
  **Confirmed live 2026-08-18** — both halves work (relight and
  auto-repeat). First test looked like a false negative ("autocooking
  doesn't appear to work") but turned out to be the toggle just never
  switched on — not a bug. **Follow-up fixed same night, v0.3.127-dev**:
  moved the toggle up to right under the Lit/Unlit status line, above
  Cooking Utensils, so it's no longer buried at the bottom of the panel.
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
  building. **Design pass done, 2026-08-18 — see `MAGIC_PLANNING.md`'s
  Will-cost section.** Resolved: `GrowMaxWill` stays as an additive
  per-wish-mastery bonus layered on top of a new Intelligence-driven
  baseline (`100 + 4.42 × (Intelligence_displayed - 2)^1.5`, same curve
  shape as Constitution, a fresh coefficient), not superseded — a
  high-Intelligence character who's never cast a wish shouldn't have the
  same ceiling as one who's actually practiced. Not built yet.
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

- [x] **A spawned-then-placed StorageBox never actually saved — found
  live 2026-08-21 in a clean isolated repro (spawn, pick up, Place,
  rename, save, reload — box gone). Fixed same day, real bug in the
  new Place feature itself, not just a pre-existing gap.**
  `AdminSpawnScreen`'s Items tab spawns via `PlayerDropping.SpawnPickup`,
  which never attaches a `PlacedPiece` (only the separate Pieces-tab path
  does that) — a pre-existing gap in the dev-only Admin Spawn tool. But
  the real bug was in tonight's own `PlayerBuilding.Confirm()` re-place
  branch: it **assumed** an existing instance already had a `PlacedPiece`
  and just repositioned it, never checking. Since `SaveManager`'s
  `CaptureWorldObjects<PlacedPiece>()` only ever finds objects with a
  live `PlacedPiece` + non-empty `SaveId`, a box that never had one
  stayed invisible to saving through the entire pickup→Place round trip.
  **Fixed**: the existing-instance branch now self-heals — adds
  `PlacedPiece`/`SaveId` if missing, same fields the fresh-build branch
  already sets, before repositioning. **Deliberately did NOT** add this
  self-heal to `StorageBox.Awake()` (a broader, tempting-looking fix) —
  caught in time that `StorageBox` is also used for pre-existing
  scene-baked fixtures (see the documented `RestorePlacedPieces` vs.
  `RestoreWorldObjects<StorageBox>` ordering gotcha), and tagging every
  scene-baked box with a `PlacedPiece` too would make it get recreated
  from scratch on load, likely duplicating it. Known remaining edge
  case, left as-is: a box spawned via Admin Spawn's Items tab and never
  placed (just left sitting where it landed) still won't save — Admin
  Spawn is an Editor-only dev/QA tool, not real gameplay, so this wasn't
  worth the broader risk to close. Compile-verified only, not yet
  live-tested against this exact repro.
- [x] **Typing a rename with the letter "e" in it also picked up the box
  being renamed — found live 2026-08-21, fixed same night.** Root cause:
  `PlayerRenaming`'s text field uses IMGUI's legacy `Event` system, but
  `PlayerInteraction`'s E-to-interact reads `Keyboard.current.eKey`
  directly (the New Input System) — two completely independent views of
  the same physical keypress, so typing "e" into the rename field never
  stopped `PlayerInteraction` from also seeing it and firing whatever
  `IInteractable` the crosshair was still resting on (the box itself,
  since the camera hadn't moved). Fixed by wiring `PlayerRenaming` into
  the existing `PlayerInteraction.SuppressInteraction` flag — already
  built for exactly this class of problem (`PlayerNPCDeposit` already
  uses it) but never applied here. Also found and fixed the same gap in
  `HandleWish` (R-key wish casting), which wasn't gated by
  `SuppressInteraction` at all — typing "r" into any open text field
  could plausibly have misfired a wish cast the same way. Compile-
  verified only, not yet live-tested.
- [x] **StorageBox lost its custom name when picked up, and floated when
  dropped — found live 2026-08-21, fixed same night.** Both bugs traced
  to the same root shape: pickup converted the box into a bare
  `pickupItem` stack (`loot.Receive`), destroying the actual GameObject
  and its `boxName`/`SaveId`/`PlacedPiece`, then `PlayerDropping`
  (no `groundOffset` awareness at all) spawned a fresh, disconnected
  copy that floated by the same 0.25-unit pivot gap fixed for building-
  placement earlier tonight. **Investigating the fix surfaced a real
  design fork** (confirmed with Ben before building): should a picked-up
  box go back down as a genuine permanent structure (real aimed
  placement) or as temporary carried gear (instant drop, despawn timer,
  needs a Rigidbody to physically settle)? Ben's call: real placement —
  a StorageBox is meant to be permanent, not a dropped item. **Built**:
  - `StorageBox` now implements `IEquippable` (`Stash`/`SetCarried`/
    `CanEquipToSlot => false`) — the same mechanism Backpack/Canteen
    already use. The original GameObject persists the whole time (just
    hidden while carried), so its name/`SaveId`/`PlacedPiece` survive
    the round trip automatically — no new state-carrying plumbing
    needed. `Complete()` now routes through `PlayerLoot.ReceiveEquipment`
    (mirroring `PlayerBackpack.PickUp`'s exact fallback shape) instead
    of the old generic `Receive`.
  - New `PlayerBuilding.ArmExistingPiece(BuildPiece, GameObject)` — a
    second placement mode alongside the existing fresh-build one, reusing
    the same ghost-preview/aim/confirm pipeline but skipping the
    ingredient cost and skill-XP grant, and reusing the existing instance
    in `Confirm()` instead of instantiating a new one. Handles the
    cancel case too — backing out mid-placement (or re-arming a fresh
    piece while a re-placement was in progress) restores the box to the
    main inventory via `RestoreExistingInstanceToInventory` rather than
    orphaning it.
  - `InventoryScreen`'s action popup gets a new "Place" button for a
    carried StorageBox (same `is SkillBook`-style dedicated branch,
    skipping the generic Equip/Unequip block since a box is never worn),
    which removes it from its current slot, arms `ArmExistingPiece`, and
    closes the Tab menu so the player can aim. `storageBoxPiece` wired
    to `StorageBoxPiece.asset` on the scene's `InventoryScreen` instance
    via a batch script, verified by guid match in the saved scene YAML.
  **Live-confirmed 2026-08-21**: Ben picked up a named box, used Place,
  confirmed it — name survived the full round trip exactly as designed.
  Cancel-mid-placement path still untested.
- [x] **Player visually clips through the floor near the building/
  Foundation area — root-caused 2026-08-20, confirmed as a direct
  symptom of the missing-Foundation bug, not a separate issue.**
  Screenshot showed the player model's lower half submerged into a
  pitch-black void near the Anvil/Furnace/StorageBox cluster.
  **Measured directly** (via `Terrain.SampleHeight` at each structure's
  exact X/Z): bare terrain there sits at Y≈0.21–0.26, while the
  Anvil/Furnace/StorageBoxes themselves sit at Y≈0.65 — a real **0.39–
  0.44 unit gap**, exactly enough for a `CharacterController` to sink
  into and get physically stopped by the actual terrain below. This
  fully explains "stuck/clipped but not still falling" — there
  genuinely is no floor there anymore, since the Foundation that used to
  provide it never made it into the save (see the entry below). Not a
  code bug on its own — it's the same missing-Foundation problem,
  confirmed rather than assumed. **Resolution is a real in-game action,
  not a code fix**: placing a new Foundation under that cluster patches
  the hole. Tonight's 4 freshly-placed Foundations elsewhere all saved
  correctly with real `saveId`s, so this isn't expected to recur for a
  new one built here.
- [x] **Foundation-to-Foundation tiling snap requires aiming almost
  exactly at the edge socket — found live 2026-08-20, live-confirmed
  fixed 2026-08-21.** A tuning gap, not a code bug. Ben tried snapping
  a new Foundation next to an existing
  one, aimed at the middle of the existing piece's face, and it didn't
  snap (fell through to free-placement, producing a visible seam/height
  mismatch between the two tiles). Investigated read-only (Ben was in
  Play mode, no edits made): the actual snap logic checks out completely
  — `BuildSocket.IsCompatibleWith` explicitly supports FoundationEdge-to-
  FoundationEdge self-pairing ("two foundation panels tiling side by
  side," a real intended feature), and `panelHalfSize = 2.5` exactly
  matches Foundation's real half-width (5-unit `BoxCollider` ÷ 2) — no
  offset math is wrong. **Root cause is just `snapRadius = 1.5`** being
  small relative to a 5×5 Foundation panel: aiming at the middle of a
  piece's face puts the crosshair ~2.5 units from the nearest edge
  socket, well outside the 1.5-unit window, so the snap correctly never
  fires — the player has to aim almost exactly at the socket point
  itself, not just "near the piece," which isn't how anyone would
  naturally try to tile floor panels together.
  **Fixed 2026-08-20**: bumped `snapRadius` 1.5 → 2.5, matching
  Foundation's own half-width exactly — aiming anywhere on the near half
  of an adjacent piece now catches its edge socket. Went with the simple
  global bump over the smarter edge-projection alternative (less new
  code, and this field only gates *whether* a socket is even considered
  a candidate — `FindNearbySocket` still always picks the *closest*
  compatible socket within range, so a larger radius doesn't create
  wrong-socket mis-snaps in a dense build, just widens the net for
  finding the right one). **Also fixed the same "changed default doesn't
  apply to an existing scene instance" gotcha this project already has
  documented** — `TestScene.unity`'s own `PlayerBuilding` component had
  `snapRadius: 1.5` baked in from before, which would have silently
  overridden the new C# default with no error; updated directly in the
  scene YAML too. Compile-verified only, not yet live-tested.
- [x] **NPCs can walk through walls — found live 2026-08-20, both
  candidate causes fixed same night.** Ben's report; lead-up moment not
  caught (unclear whether it followed a stuck/wedged moment or was
  plain walking with no stall first). Two real candidate causes, not
  yet distinguished:
  1. **Tonight's new `NPCMovement` hard-bump fix is a real, concrete
     risk here, and worth owning directly**: the 4m stuck-escape
     teleport (`mover.position = mover.position + escapeDir *
     StuckBumpDistance`) only re-samples ground *height* at the
     destination — it never raycasts to check whether that 4m path is
     actually clear first. If an NPC gets wedged against a wall, the
     "escape direction" could be straight through that same wall (or a
     different one) instead of around it — the old weak reverse-nudge
     could never do this (it moved at normal `moveSpeed`, which
     ordinary collision would still stop), but an instant, uncollided
     teleport genuinely can clip through geometry. This is a plausible
     side effect of a fix built earlier tonight, not a pre-existing bug.
  2. **Pre-existing weak obstacle-avoidance gap, unrelated to tonight**:
     `NPCMovement.FindClearDirection`'s normal (non-stuck) raycast probe
     could simply be missing a wall collider for some geometry (wrong
     layer, missing collider, raycast height not intersecting a thin
     wall), letting an NPC walk straight through with no stall moment
     at all — this would be a pre-existing gap, not something today's
     changes caused.
  **Candidate 2 confirmed as the real mechanism, found via a follow-up
  report the same night**: Ben's next repro bundled both symptoms
  together — "walked through the wall, and doesn't walk on the floor,
  it sinks to the ground." That second half was the real clue.
  `GroundHeight.Sample` (used by every NPC movement script for its
  per-frame Y position) only raycasts against a dedicated "Ground"
  physics layer, deliberately excluding everything else (its own header
  comment: avoids an NPC "snapping onto the TOP of a Boulder/Tree's own
  collider" when walking past one) — but `Foundation.prefab`/
  `PlankFoundation.prefab` were on layer 0 (Default), not Ground.
  So an NPC crossing a player-built floor got its height sampled
  straight through to bare terrain below (confirmed: Terrain sits on
  layer 3/"Ground", Foundation didn't), sinking it well below where a
  Wall's collider actually blocks — low enough to slip underneath it.
  One root cause fully explains both symptoms. **Fixed**: both
  `Foundation.prefab` and `PlankFoundation.prefab` (every GameObject in
  each, matched by count before/after the edit) moved onto the Ground
  layer — grepped the whole `Assets/Scripts` tree first to confirm
  nothing else keys off that layer by name, so this can't have a side
  effect elsewhere. Wall/Pole/Door/Roof deliberately were **not** added
  — they're not meant to be walked on top of, same reasoning
  `GroundHeight.cs` already uses for excluding Boulder/Tree.
  **Candidate 1 fixed earlier the same night too**, whether or not it
  was the actual cause — worth doing either way since it was a real gap
  regardless. The hard-bump path now validates its escape path before
  committing: `FindClearBumpDirection` runs the same widening-angle
  search `FindClearDirection` already does for normal deflection, just
  checked
  against the full `StuckBumpDistance` (4m) instead of the caller's
  short probe distance, and `ClearDistance` clamps the actual teleport
  to whatever's genuinely open (with a small safety margin) rather than
  assuming the full 4m is always safe. A stuck NPC can no longer be
  teleported through a wall it's wedged against. Both candidates are now
  addressed — compile-verified only, not yet live-tested. Existing
  Foundation instances already placed in a live Play session (including
  Ben's current test building) won't retroactively pick up the layer
  change until the scene/prefab is reloaded fresh.
- [ ] **Roaming wildlife (`PreyCreature`, confirmed on a Rabbit) can also
  walk through player-built walls — found live 2026-08-21, flagged for
  later, not fixed.** Same underlying gap as hired NPCs' own idle-wander
  state (`NPCWander`), just on the wildlife side: `NPC_NAVIGATION_
  PLANNING.md` deliberately scoped `HostileCreature`/`PreyWander`/
  `NPCWander` out of every NavMesh phase ("roaming wildlife has no fixed
  distant target, so it doesn't hit this ceiling and doesn't need
  converting") — correct reasoning for the *pathfinding-ceiling* problem
  (no long-distance target to route toward a wall in the way of), but it
  didn't anticipate wildlife wandering close enough to a player-built
  wall to clip straight through it locally. None of tonight's NavMesh
  work (Phases 0-2, the wall `NavMeshObstacle`s, or `NPCGathering`'s new
  physics safety-net sweep) touches this movement system at all — it's a
  distinct, still-fully-unconverted code path. Would need either a
  cheap version of the same physics-sweep safety net applied to
  `NPCWander`/`PreyWander`/`HostileCreature`'s own movement, or a real
  NavMesh conversion for wildlife roaming (bigger scope, since it means
  picking a *local* target-aware approach rather than the long-distance
  routing the existing phases were built for).
- [x] **Closed 2026-08-21, Ben's call.** A placed Foundation is genuinely gone after a save/reload — not
  underground, actually never saved at all. Root mechanism identified
  2026-08-20, exact trigger still unconfirmed.** Ben's screenshot showed
  the Anvil/Furnace/StorageBoxes sitting correctly on bare grass with no
  Foundation beneath them — first suspected "moved underground," but a
  direct `save.json` inspection (not a guess) shows the real story:
  **no Foundation entry exists anywhere in `placedPieces`**, and neither
  `Foundation.prefab`'s nor `PlankFoundation.prefab`'s guid appears
  anywhere in `TestScene.unity` either — the object is completely gone,
  not misplaced. Checked the last 2 backup saves too
  (`save.json.bak`/`.bak2`) — **absent from all three**, so this isn't a
  fresh regression from tonight's other fixes, it goes back further.
  **This also fully explains Ben's separate report the same session**
  ("tried to snap a new Foundation/Wall next to the old one, it wouldn't
  snap") — `FindNearbySocket` only matches live `BuildSocket` components
  actually present in the scene; if the Foundation object is truly gone,
  every socket that used to live on it is gone too. One root cause, two
  symptoms, not two separate bugs.
  **Mechanism traced, trigger not yet confirmed**:
  `SaveManager.CaptureWorldObjects<T>` (`SaveManager.cs:307`) silently
  skips any object whose `SaveId.Id` is empty — and there's already a
  documented instance of exactly this gotcha in `PlayerBuilding.Confirm
  ()`'s own comments (a Village Flag hit it once, 2026-08-17, "fixed"
  with an explicit `SaveId.GenerateIfMissing()` call right after
  `AddComponent<PlacedPiece>()`). That fix is present in `Confirm()` and
  looks structurally correct for every `BuildPiece` type, Foundation
  included — nothing in the code path treats Foundation differently from
  Wall/Anvil/Furnace/StorageBox, all of which **are** present and
  correct in the current save. Can't fully explain why Foundation's
  `SaveId.Id` specifically ends up empty without live debugging, which
  isn't possible from a batch-mode investigation.
  **Next step completed same night — not currently reproducible.** Ben
  placed 4 fresh Foundations during the same session; checked the
  resulting `save.json` directly and all 4 came back with real,
  non-empty `saveId`s and correct `placedPieces` entries (clean 5-unit
  tiling, matching their in-game snap positions exactly). So whatever
  caused the *original* Foundation to lose its id isn't happening to new
  placements right now — likely a one-off from earlier in this project's
  history (possibly predating some of this session's own earlier SaveId
  fixes) rather than a live, currently-reproducible bug. **Downstream
  impact confirmed and explained**: the missing floor left a real
  0.39-0.44 unit gap between the Anvil/Furnace/StorageBox cluster and
  bare terrain — see the now-closed "Player visually clips through the
  floor" entry above. **Practical resolution, not a code fix**: place a
  new Foundation under that cluster to patch the hole. Leaving this
  entry open only as a "watch for recurrence" flag, not an active
  investigation — nothing left to chase without a fresh live repro.
- [x] **No friction anywhere — physics objects roll/slide down hills
  across the board, found live 2026-08-20. Fixed same day.** Ben's report: "lots of things roll down hills." Checked
  before planning: grepped every `.physicMaterial` asset in the project —
  zero exist anywhere in `Assets/Data`, `Assets/Prefabs`, or on the
  Terrain itself (every `.physicMaterial` on disk belongs to third-party
  package examples, Mirror/WeatherMaker, unreferenced by anything of
  ours). Scope check: **132 prefabs have a live `Rigidbody`** — almost
  all use `BoxCollider`; only `BerryPickup`/`BerrySeedPickup` use
  `SphereCollider`. That split matters: a round collider physically can't
  be held by friction at all (friction opposes sliding, not rolling — a
  sphere given any spin rolls downhill forever regardless of friction
  value), so it needs a different fix than everything else.
  **Plan (Ben's calls, confirmed via `AskUserQuestion`):**
  1. **One new `Assets/Data/HighFrictionGround.physicMaterial`**
     (`dynamicFriction`/`staticFriction` ≈ 1.0, **`frictionCombine =
     Maximum`**, `bounciness = 0`), applied to the Terrain's
     `TerrainCollider` only — not batched across all 132 prefabs. Ben's
     own framing, confirmed correct: `Maximum` combine means the higher
     of the two touching colliders' friction always wins, so this one
     ground-level material affects every item uniformly regardless of
     what (if anything) the item's own collider has — no per-prefab
     changes needed.
  2. **Berry/Berry Seed need `Rigidbody.constraints` (freeze rotation)**,
     separately and unconditionally — friction alone can never stop a
     rolling sphere, so this isn't optional even with the new material.
  3. **A settle-then-freeze safety net**: once a Rigidbody's velocity
     stays near-zero for ~1-2 seconds, set it kinematic. Catches any
     residual slope creep the friction fix alone might not fully kill
     (a real, common Unity issue even at correct friction values,
     especially near moving colliders like the player/NPCs), and these
     are decorative dropped props that don't need continuous simulation
     once settled anyway.
  **Built 2026-08-20** — all three pieces landed exactly as planned:
  `Assets/Data/HighFrictionGround.physicMaterial` (friction 1.0,
  `frictionCombine = Maximum`) applied to Terrain, `BerryPickup`/
  `BerrySeedPickup` rotation-frozen, new `RigidbodySettler.cs` batch-
  added to all 132 Rigidbody prefabs (verified 132/132 via direct YAML
  guid grep, not just the batch log). **Live-confirmed 2026-08-21**:
  Ben spawned Berries on sloped terrain — they no longer roll away. The
  original bug that kicked off this whole friction/physics/NavMesh
  investigation is now genuinely closed, not just compile-verified.
- [x] **NPC stuck wedged into wall/floor geometry (Miner), found live
  2026-08-20 — fixed same day.** `NPCMovement.StuckTracker`'s recovery
  used to just reverse the mover's desired *direction* for one frame,
  walked at normal `moveSpeed` — too weak to clear a real corner wedge
  where both forward and reverse can be blocked by different colliders.
  **Fixed with Ben's own proposed fix**: the tracker-based
  `FindClearDirection` overload now takes the mover's actual `Transform`
  and, once stuck, hard-teleports it 4m in the escape direction
  (`NPCMovement.StuckBumpDistance`), re-sampling ground height at the
  new spot via `GroundHeight.Sample` so it doesn't land floating/
  embedded. All 5 callers (`NPCGathering`/`NPCCrafting`/`NPCTraining`/
  `NPCGuarding`/`NPCSeekFlag`) updated to pass `transform` through.
  Compile-verified only — not yet live-tested against a real wedge.
- [x] **StorageBoxes sink into whatever they're placed on — found live
  2026-08-20 after a Foundation upgrade, actual root cause was
  unrelated to upgrading at all, fixed same day.** Original theory (a
  Foundation tier upgrade changing floor height out from under an
  already-placed box) was checked directly and **ruled out**:
  `PlayerPieceUpgrade.Upgrade()` instantiates the new-tier Foundation at
  the exact same `transform.position` as the old one, and a direct bounds
  measurement of `Foundation.prefab` vs. `PlankFoundation.prefab` showed
  their mesh/collider bounds are byte-for-byte identical (both tiers,
  same height) — the floor genuinely never moves on upgrade. **Real
  cause, found by re-measuring `StorageBox.prefab` the same way**: its
  model pivot sits at its vertical center (mesh spans Y=-0.25 to Y=0.25,
  the exact same "pivot not at base" issue Furnace/Anvil had earlier this
  session), but `StorageBoxPiece.asset` never had a `groundOffset` field
  set at all (defaulted to 0) — every StorageBox ever placed has always
  sunk 0.25 units into whatever surface it's on, on any Foundation tier
  or bare ground, completely unrelated to upgrading. Ben just happened to
  notice it clearly against the new Plank floor's clean lines. **Fixed**:
  `StorageBoxPiece.asset` now has `groundOffset: 0.25`. **A quick audit
  of every other free-placed `BuildPiece` for the same gap found two
  more real, previously-undetected instances**: Bookshelf (needed 0.9,
  now fixed) and Desk (needed 0.4, now fixed) — both silently sinking
  since they were built, nobody had checked them against this specific
  gotcha before. Campfire, City Statue, both Garden Plot sizes, and all
  5 Village Flag tiers were also audited and came back clean (correctly
  base-pivoted already, `groundOffset: 0` is right for them). Socket-
  snapped pieces (Wall/Door/Roof/Pole/Foundation/Gable) were excluded
  from the audit — their position comes from `BuildSocket.transform
  .position` directly, never from `groundOffset`, so they can't have
  this bug by construction. Compile-verified only — not yet live-tested
  (needs a fresh StorageBox/Bookshelf/Desk placement to confirm each
  sits flush).
- [x] **Closed 2026-08-21, Ben confirmed working correctly.** Player-built Anvil/Furnace placed on a Foundation sink into the
  floor — found live 2026-08-20, not yet fixed.** Screenshot showed both
  structures visibly embedded into a wood Foundation's flooring rather
  than sitting flush on top, even though `BuildPiece.groundOffset`
  (v0.3.152-dev) was specifically built to fix exactly this class of
  sinking. Ben's own read, worth checking first: free-placement's
  `groundPos = hit.point + Vector3.up * armedPiece.groundOffset`
  (`PlayerBuilding.ResolveFollowing`) adds the same fixed pivot-to-base
  offset regardless of what surface `hit.point` landed on — that part
  should already be surface-agnostic in theory (the offset only corrects
  for the model's own local pivot, not world height), so the more likely
  real cause is that a Foundation's physical collider doesn't actually
  sit at its visible top-surface height. There's real precedent for
  exactly that mismatch already in this codebase:
  `PlayerBuilding.cs`'s own `wallOntoFoundation` comment notes
  Foundation's `FoundationEdge` socket "sits ~0.2m below its visible top
  surface" by design (the slab is deliberately "mostly buried"). If the
  raycast hit-test also resolves against that same buried collider rather
  than the rendered floor-plank surface, `hit.point` would land ~0.2m
  below where the floor visually appears, and `groundOffset` alone
  can't compensate for a wrong `hit.point` to begin with.
  **Investigated 2026-08-20 — the collider-mismatch theory was wrong,
  checked directly rather than assumed.** Measured `Foundation.prefab`/
  `PlankFoundation.prefab` via `PrefabUtility.LoadPrefabContents` +
  `Renderer.bounds`: their `BoxCollider` matches the visual mesh
  **exactly** (top at Y=0.4, identical for both tiers) — no buried-
  collider mismatch exists for either Foundation. Also re-measured
  `AnvilPiece.prefab`/`FurnacePiece.prefab`'s actual pivot-to-base
  distance fresh and compared against their stored `groundOffset`
  values — both match to 4 decimal places (Anvil 0.3784, Furnace
  1.0000). So the placement math itself is provably correct in
  isolation for a fresh placement on either Foundation tier — **no code
  bug found**. Most likely real explanation: the structures in Ben's
  screenshot were placed *before* the `groundOffset` fix landed earlier
  this session and were never rebuilt afterward — an old placement
  doesn't retroactively pick up a later data fix. **Needs a live
  re-test**: place a brand-new Anvil/Furnace on a Foundation now and
  confirm whether the sinking still happens — if it does, this
  investigation's conclusion is wrong and needs revisiting; if not,
  this closes as a stale-placement non-issue.
- [x] **Two real save/load regressions, found live during the 2026-08-19
  playtest continuation, both fixed same day, v0.3.148-dev.**
  1. **A worn Canteen lost its fill, and a Hammer stashed in a worn pair
     of Jeans vanished, both after a real save/reload.** Root-caused via
     a direct `save.json` inspection rather than guesswork — the
     *capture* side was already 100% correct (confirmed the Jeans'
     `"equipment": {"nested": [{"item": "MasterworkHammer", ...}]}` and
     the Belt's Canteen `"liquid": "Water", "amount": 100.0` were both
     genuinely written to disk). The bug was entirely on restore:
     `Inventory.AddEquipmentItem` silently fails when a slot's already at
     capacity, and `PlayerEquipment`'s body slots (Leg, Waist, ...) start
     pre-occupied by the scene's own baked-in default "Settlers" starting
     gear — so the real restored Jeans/Belt were silently discarded,
     leaving the scene's empty defaults in place. A genuine counter-
     example confirmed the mechanism precisely: the Chest slot's Shirt
     (with Rations inside) restored *correctly*, because unlike Jeans/
     Belt/Sneakers (baked directly into the scene, no guard), the Shirt
     uses a runtime `Start()` auto-equip with an "already equipped?"
     check that safely loses the race if `SaveManager.Load()` runs
     first. Fixed at the root: new `Inventory.Clear()`, called by
     `InventorySaveUtility.Restore` before every restore, so a restore
     always produces exactly the saved state regardless of what the
     inventory already held — fixes every call site at once (player
     inventory, every equipment slot, NPCCargo, StorageBox), not just
     the two reported cases.
  2. **A StorageBox named and stocked before saving came back after
     reload with a generic name and empty.** A serious regression in
     what used to be one of the most solid parts of the save system.
     Root cause: `SaveManager.Load()` restored `StorageBox`/
     `ResourceNode`/`GardenPlot`/`GardenPlot4x4` by SaveId lookup
     *before* `RestorePlacedPieces` (which actually recreates a
     player-built structure that doesn't exist yet in a fresh scene
     load) — fine for a box already sitting in the scene, broken for one
     built during play, since the lookup silently fails and
     `RestorePlacedPieces` then recreates a bare, empty, default-named
     copy with nothing to backfill it. `Furnace`'s own restore already
     ran in the correct order (after `RestorePlacedPieces`) — exactly
     why its own richer state-saving worked when tested earlier; every
     other world-object restore call reordered to match. **Live-confirmed
     2026-08-19** — a renamed "ore box" correctly kept its name across a
     real save/reload (contents not separately re-checked, but very
     likely fine given both were failing via the exact same mechanism).
  3. **Standing item, not yet built**: audit save/restore for *every*
     worn-equipment slot with a nested inventory (not just the two that
     happened to get reported), and — a new permanent addition to
     `CLAUDE.md`'s `IEquippable` checklist — any future gear with its own
     inventory slots must have its nested contents explicitly verified
     against a real save/reload before being considered done, not
     assumed to work because the general mechanism exists.

  Compile-verified via full-project batch mode; not yet live-tested —
  both need a real save/reload pass to confirm.
- [x] **A new-player-experience playtest (2026-08-19) — build a
  structure, place Furnace/Anvil/StorageBoxes, hire and assign NPCs, cook
  — surfaced 5 real findings, 4 fixed same session, 1 still open.**
  Fixed, v0.3.146-dev: **Crude Fiber Backpack rejected as an NPC tool**
  (a third Backpack family, never registered in the 4 jobs' tool lists —
  same shape as the earlier Leather Backpack fix); **the "Log" item was a
  genuine dead end** (a Woodworking NPC felling a Tree yields a raw Log
  to cargo, but zero recipe anywhere consumed it — fixed with a new
  `LogToPlankRecipe.asset`, 1 Log → 2 Plank; **superseded and removed,
  v0.3.150-dev** once dropping a Log became choppable again (see below),
  since drop-and-chop yields the same Plank output plus a Stick chance
  the plain recipe never had — a real fix, just not the one that stuck
  around); **Small Rock mystery, fully
  root-caused** (a brand-new Woodworking-only NPC turned up with 8 Small
  Rock in cargo — `NPCGathering.FindTarget()`'s `ResourceNode` scan had
  zero job-kind gating, and `Boulder.prefab`'s plain-Rock variant has
  `requiredTools: []`, so the tool check never triggered for it; fixed
  with a new `NPCJobDefinition.harvestsToollessRock` field, same shape as
  `searchesBushes`/`collectLoosePickups`, only `MineOreJob` sets it
  true — this also falsifies a comment already in the codebase claiming
  the Harvestable pool was "naturally segregated by RequiredTools," true
  for ore Boulders, false for plain Rock). **The `ChoppableTree` stuck
  bug is also fixed now, v0.3.148-dev — Ben's collider-mismatch theory
  confirmed exactly right.** The `[TreeStuckDiagnostic]` logging caught
  the real numbers live, identical across every tree tested:
  `pivotDistance=3.99 harvestRange=3.00 colliderSurfaceDistance=0.00` —
  an NPC could be physically touching the tree and the game would still
  think it was a meter too far away, since the approach check measured
  distance to the tree's transform pivot, not its actual collider
  surface (chopping a placed Log worked fine for the same NPC because
  `ResourceNode`/`StorageBox` don't have this offset). Fixed by measuring
  against `Collider.ClosestPoint` for `ChoppableTree` targets
  specifically; diagnostic logging removed. Also confirmed working during the same session, not bugs:
  Fame-on-tier-unlock grant (via a live Console log,
  `[Fame] +1.00 -> 31.00` at the same timestamp as a Restoration
  tier-unlock — resolves the long-open "unconfirmed" item below as a
  genuine non-bug), wish fizzle/skill-margin success roll, Will growth
  past 100 via successful wish mastery, the crafting quality-roll
  (a Masterwork Trimmed Stick from a lower attempt, training Dexterity),
  StorageBox rename + empty-to-pickup guard, NPC deposit-container
  retargeting (both a Woodworking and a Mining NPC), and NPCs
  deliberately skipping bonus chunks (confirmed intentional, not a gap).
- [x] **No player-craftable/placeable Anvil or Furnace, found live
  during the 2026-08-19 playtest — fixed same day, v0.3.147-dev.** Both
  existed only as a single fixed pre-placed object in `TestScene.unity`.
  **Furnace**: 8 Nail + 6 Small Rock + 4 Plank, a clean duplicate of the
  real fixture (model/behavior intact). **Anvil**: 6 Small Rock + 2 Plank
  + 2 Iron — hit a real snag first (the scene's actual Anvil object has
  no visual mesh at all, just an `AnvilSurface` trigger parented under
  the Boulders container; a first attempt duplicated that whole
  214-object container by mistake, caught via direct file check and
  cleaned out), resolved by building from `Boulder.prefab` directly
  (which already carries both `ResourceNode` and `AnvilSurface` as a
  shared template) — a deliberate placeholder visual (reads as a plain
  stone slab, not a recognizable anvil), Ben's explicit call over
  spending a Tripo3D generation on a real model right now. Neither recipe
  needs an Ingot; Furnace's use of Nail is safe specifically because
  `NailRecipe.asset` (checked directly) needs an Anvil surface but only
  raw Iron ore, no Ingot — Anvil-first is a real requirement, not just a
  suggested order. Both registered in `PlayerBuilding.allPieces` (hand-
  maintained scene array, confirmed) and `BuildPieceDatabase`. Compile-
  verified, every step confirmed directly in the saved files. **Anvil's
  placeholder visual is a known follow-up** — worth a real model
  eventually, logged as a placeholder, not a finished asset.
  **Live-tested 2026-08-20 — found and fixed a real bug: both pieces were
  sinking into the terrain when player-placed.** A built Furnace looked
  much smaller than the original fixed one; measured directly (per
  `CLAUDE.md`'s model-grounding protocol) — the reused model's pivot
  sits a full world unit above its actual base, and `PlayerBuilding`'s
  free-placement code plants the pivot directly at the ground-hit point
  with no correction, sinking it in by that gap. The Anvil placeholder
  had the same issue, worse (1.17 units). Fixed generally with a new
  `BuildPiece.groundOffset` field (opt-in, reusable for any future piece
  built from a reused/extracted model with the same pivot mismatch), set
  to the measured value on both pieces. Compile-verified, not yet
  live-tested against the actual fix.
  **Anvil placeholder replaced with the real model, v0.3.153-dev** — Ben
  asked directly why the placeholder couldn't be the real anvil visible
  in the pre-placed scene; turns out it could. `Assets/Models/Anvil.glb`
  (already imported) was parented as a child of the same `AnvilSurface`
  trigger object found the night before — last night's extraction used
  `.transform.root`, which walked past this child entirely. Hit a second
  real mistake fixing the first: `FindFirstObjectByType<AnvilSurface>()`
  isn't reliable in this scene, since `Boulder.prefab` bakes an
  `AnvilSurface` onto every scattered plain-Rock instance too — the first
  rebuild attempt grabbed a random scattered Boulder instead (caught by
  checking the mesh guid directly, which resolved to `Rock_Quaternius
  .glb`, not `Anvil.glb`). Fixed by disambiguating on the real Anvil's
  known exact position instead of "whichever enumerates first."
  Re-measured `groundOffset` for the correct model (0.378, down from the
  Boulder placeholder's 1.169). Verified via mesh guid and by reading the
  freshly-baked icon directly — a real anvil on a wood stump. Compile-
  verified, not yet live-tested.
- [x] **None of the 6 new Iron Arrow recipes (`IronArrowheadRecipe` + 5
  tier recipes) actually appear in the Crafting screen, found live by
  Ben (2026-08-18) — a real miss from the same session that built them,
  not a stale claim. Fixed 2026-08-18.** Root cause confirmed directly,
  not guessed: `PlayerCrafting.recipes` is a hand-maintained
  `[SerializeField]` array on the Player object in `TestScene.unity`, not
  a dynamic `AssetDatabase` scan — the exact same "registration array,
  easy to forget a new entry" shape that already bit `PlayerEating.edibles`
  (Fried Egg) and `GuardRangedJob`'s tool-acceptance lists this same
  session. `BuildIronArrows.cs` created all 6 new `CraftingRecipe` assets
  correctly (confirmed via direct guid grep) but never added them to this
  array. Fixed via a throwaway batch-mode script
  (`Assets/Editor/RegisterIronRecipes.cs`, deleted after running):
  appended all 6 new recipe guids to the Player's `recipes` array
  (61 → 67), explicit `Scene`-handle `SaveScene()` (not the ambient
  `SaveOpenScenes()`, per `CLAUDE.md`'s own silent-no-op gotcha for this
  exact pattern). Verified by grepping `TestScene.unity` directly for all
  6 new recipe guids, not trusting the script's log. Compile-verified;
  not yet live-confirmed.
- [x] **`FurnaceScreen` never shows a stack-count label for any item with
  a baked icon, found live by Ben (2026-08-18) while making Iron
  Ingots. Fixed 2026-08-18.** Same exact shape as the already-fixed
  `CampfireScreen` Ingredients/Output/Fuel gap (v0.3.126-dev):
  `FurnaceScreen.DrawBox` builds its `GUIContent` text as
  `itemName + " x{count}"`, but unconditionally replaces it with an
  empty string whenever `slot.item.icon != null`. Fixed with the
  identical pattern `CampfireScreen.DrawBox` already has: a separate
  `QTY: {count}` label drawn below the icon box, independent of whether
  the item has an icon. Compile-verified; not yet live-confirmed.
- [x] **`InventoryScreen`'s action popup (Equip/Unequip/Eat/Drop, etc.)
  lost clicks to whatever inventory slot is visually underneath it —
  found live by Ben (2026-08-18). Fixed same night, v0.3.139-dev.**
  "when I use the drop function, if the window is over an inventory
  slot, I can't click the buttons — it registers the click on the
  inventory slot instead." Root cause: `DrawPendingActionMenu`'s own
  comment says it's "Drawn last so it sits on top" — true visually, but
  `HandleSlotEvents` (the underlying grid's own click handling, called
  earlier in the same `OnGUI` pass while laying out the grid)
  unconditionally consumed the `MouseDown` event via `e.Use()` with no
  check for whether the action popup was currently open. Since this
  screen uses plain `GUILayout`/`GUI.Button` calls rather than real
  `GUI.Window`-based modal layering, draw order alone doesn't grant
  input priority — code order does, and the grid's own handler ran
  first. Fixed by gating `HandleSlotEvents` on `pendingActionItem ==
  null`, so the underlying grid stops consuming clicks entirely while
  the action popup is open. **Live-confirmed 2026-08-18** — Ben confirmed
  the Drop popup no longer loses clicks to the slot underneath it.
- [x] **`NPCHiringScreen`/`NPCJobScreen` never paused the NPC while open —
  only `Talk` did, found live by Ben (2026-08-18): "walked up, talked,
  and the npc still moved" while the Assign Job menu was open. Fixed
  same night, v0.3.138-dev.** Neither screen ever called
  `wander.SetPaused` (or anything else), so an NPC being actively
  managed via either screen kept wandering/gathering/patrolling the
  whole time — unlike `Talk`, which explicitly pauses via `NPCDialogue
  .BeginDialogue`/`EndDialogue`'s four-component pattern
  (`NPCWander`/`NPCGathering`/`NPCCrafting`/`NPCGuarding`). Fixed by
  adding `NPCHiring.SetMovementPaused(bool)`, mirroring that exact
  pattern, and calling it from both screens' `SetOpen()`. Deliberately
  not routed through `NPCFreeze` — that toggle represents a deliberate
  player "stay frozen" choice that a temporary UI-open pause must not
  silently clear when the screen closes. **Live-confirmed 2026-08-18** —
  Ben opened an NPC's management screen and confirmed it stops moving
  while open.
- [x] **Player Map screen rendered blank, found live by Ben (2026-08-18) —
  root-caused and fixed 2026-08-18.** Genuinely root-caused this time, not
  just explained away by the domain-reload incident that was the prime
  suspect: `PlayerMapExploration.revealed`/`gridWidth`/`gridHeight`/
  `worldBounds` are plain fields, not `[SerializeField]` — a mid-Play-mode
  domain reload (the exact hazard confirmed elsewhere this same session,
  see `CLAUDE.md`) resets them to null/0 without `Awake()` running again,
  since Unity's domain reload only restores serialized state. `MapScreen
  .EnsureTexture()` then silently built a 0×0 `Texture2D` from the zeroed
  grid dimensions instead of throwing — no exception, no log line, just a
  blank map, exactly matching the original report. Fixed with a new
  `EnsureInitialized()` lazily called from every public entry point
  (`GridWidth`/`GridHeight`/`WorldBounds`/`IsRevealed`/`RevealCircle`/
  `WorldToCell`/`CaptureRevealedBase64`/`RestoreRevealedBase64`), not just
  `Awake()` — the Map now self-heals (rebuilding a fresh, empty-fog grid)
  the moment anything touches it after the backing state goes missing,
  regardless of what causes that, instead of rendering blank. **Live-
  confirmed 2026-08-18** — Ben opened the Map normally (no forced repro
  of the original trigger — that mechanism is one the project has since
  agreed to never do again) and confirmed it renders correctly: fog,
  revealed terrain, Village Flag/NPC markers, vitals HUD all showing.
- [x] **Fried Egg can't be eaten — no "Eat" option in its right-click
  popup, found live by Ben (2026-08-18). Fixed same night, v0.3.126-dev.**
  `PlayerEating.edibles` is a hand-maintained array on the Player object
  in `TestScene.unity`, not an auto-discovered list — checked directly,
  and Cooked Meat/Grilled Meat/Steak and Potatoes/Herbal Tea/Meat Stew
  are all correctly present, but `FriedEggEdible.asset` (guid
  `4d80ad9c55cab6542bbf76f8adbd8bb3`) is missing from the list.
  `FriedEggEdible.asset` itself is fully correct (Eat, restores 5
  Hunger + 5 Health) — this is purely a scene-data registration gap,
  same shape as the `Campfire.cookableItems`/`FriedEggCookable`
  registration gap already logged below. Likely missed at the same
  time Fried Egg was added as Cooking's level-0 entry point
  (v0.3.112-dev) — its Cookable got registered, its Edible didn't.
  Fixed by adding the guid to `PlayerEating.edibles` in the scene.
  **Confirmed live 2026-08-18** — a follow-up audit of all 6 cooked-item
  Edibles found every one correctly registered and eatable/drinkable now
  (Herbal Tea correctly uses `verb: Drink`, read directly by
  `InventoryScreen`'s popup button, not hardcoded) — Fried Egg was the
  only gap, and it's closed.
- [x] **`CampfireScreen`'s Ingredients grid never shows a stack count for
  any item with a baked icon — found live by Ben (2026-08-18). Fixed
  same night, v0.3.126-dev.** `CampfireScreen.DrawBox` puts the count into
  the slot's `GUIContent` text (`itemName + " x{count}"`), but that text
  is unconditionally replaced with an empty string whenever
  `slot.item.icon != null` — so a stack of 15-16 Egg (which got a real
  baked icon last session) renders as a single unlabeled icon with no
  quantity anywhere. `InventoryScreen.DrawSlotBox` hit this exact same
  shape once and already has the fix: a separate `QTY: {count}` label
  drawn *below* the icon box, independent of whether the item has an
  icon. `CampfireScreen.DrawBox` never got that treatment when it was
  built — needs the same below-box label added (Grill/Cooking Pot/
  Kettle/Frying Pan/Left Hand/Right Hand are all capacity-1 so this
  barely matters there, but Ingredients/Output/Fuel are real stacks).
  **Confirmed hitting Output too, same session** — 2 successfully-cooked
  Egg in a row landed in the Cooked Items box as one unlabeled icon,
  no "x2" anywhere, same root cause (`DrawGrid(current.OutputInventory,
  ...)` goes through the same `DrawBox`). **Also confirmed hitting Fuel**
  — the wood stack in `DrawSingleBox(current.FuelInventory)` shows the
  same way, no count, same root cause (`fuelInventory`'s capacity is 1
  *slot*, not 1 item — it can still stack several Sticks, same as any
  other slot). **Confirmed live 2026-08-18** — Ingredients, Output, and
  Backpack/Hands transfer slots all showed correct `QTY:` labels.
- [x] **Campfire cooking-utensil slots (Grill/Cooking Pot/Kettle/Frying
  Pan) appeared not to survive save/reload — found live by Ben
  (2026-08-18), resolved 2026-08-18, was never a real bug.** After
  equipping accessories and saving/reloading, `CampfireScreen` had
  appeared to come back with all 4 utensil slots empty. Checked the real
  save file directly first: capture was already confirmed correct (a
  genuine `"campfire"` block with `fryingPan: [{item: "FryingPan",
  count: 1}]`, matching what was actually equipped). Added temporary
  `[CampfireSaveDiagnostic]` logging to `SaveManager.RestorePlacedPieces`
  rather than guess further — **a live save→reload with the Console open
  confirmed the restore path was correct all along**:
  `existing=Campfire` resolved properly (not falling into the broken
  buildPiece-instantiate branch), and the Frying Pan slot count went
  cleanly 0→1 across the restore. The original failure report simply
  didn't reproduce; nothing needed fixing. Diagnostic logging removed
  (v0.3.129-dev).
- [x] **Payment timer genuinely ran a ~300s cycle instead of 3600s —
  found live by Ben (2026-08-17), root-caused and fixed 2026-08-18,
  v0.3.130-dev.** A Miner ("Tekim Robot") read "Working — payment due in
  298s" right after being paid, then "5s" shortly after — a real ~300s
  cycle, not a display glitch, despite `NPCHiring.workDurationSeconds =
  3600f` checking out correct on every direct `.cs` grep. Root cause,
  found by grepping the actual prefab instead of just the script:
  `NPCFactoryWorker.prefab` had a stale serialized override,
  `workDurationSeconds: 300`, left over from before the field's C#
  default was bumped 300→3600 — the exact "changed `[SerializeField]`
  default doesn't apply to existing scene/prefab instances" gotcha
  `CLAUDE.md` already documents. Both `NPCFactoryWorkerMale.prefab` and
  `NPCFactoryWorkerFemale.prefab` (what `VillageFlagSpawner` actually
  spawns) are nested prefab variants of this same base with no override
  of their own, so both silently inherited the stale value too. Fixed by
  correcting the base prefab's value to 3600; confirmed no other override
  exists on Male/Female or anywhere in `TestScene.unity`. Compile-verified
  (all 3 prefabs reimported cleanly); not yet live-confirmed.
- [x] **An NPC's custom name reverted to default at the same moment its
  payment timer flipped to "Waiting for payment" — found live by Ben
  (2026-08-17). Closed 2026-08-20 — never actually reproduced again
  despite real effort to catch it.** Split off from the timer bug above,
  which was fixed and explained the *short-cycle* half of the original
  report but not this half. `PlayerAutosave` was checked and ruled out
  (`Update()` only ever calls `saveManager.Save()`, never `Load()`).
  Non-repro logged 2026-08-18 (a paid-off named Miner kept its name), and
  now a much stronger non-repro 2026-08-20: Ben confirms NPC custom names
  have survived multiple full save/reload cycles *and* multiple rounds of
  equipment changes since, with no recurrence at all. Whatever the
  original mechanism was (never confirmed — `NPCHiring.OnPaymentDue` was
  the prime suspect but its only known subscriber doesn't touch naming),
  it isn't reproducible anymore — likely an incidental side effect of one
  of the many save/load fixes landed since 2026-08-17 (the `Inventory
  .Clear()` equipment-restore fix and the `RestorePlacedPieces` ordering
  fix both touched adjacent restore paths around the same time). Closing
  without a confirmed root cause, on the strength of the repro attempts.
- [x] **`NPCGathering.MaxRangeFromDeposit` (the work-range leash, added
  2026-08-17) doesn't survive save/reload — found live by Ben same
  night ("the npc leash isn't saving on reset either"). Fixed
  2026-08-18, v0.3.128-dev.** `SaveManager`'s NPC capture/restore never
  read or wrote this field, so a leash value set via `NPCHiringScreen`'s
  "Work range" control silently reset to the 50f default on every
  reload — same shape as every other "new field, forgot to wire it into
  save/load" gap this project has hit before. Fixed by adding
  `maxRangeFromDeposit` to `SaveManager.CaptureNpc`/`RestoreNpc` (fixed
  alongside `NPCGuarding.PatrolRadius`, the new equivalent leash below,
  same fix in the same pass). **Live-confirmed 2026-08-18** — Ben set a
  leash value, reopened the Editor, confirmed it was still there instead
  of reset to the 50f default.
- [x] **Deposit Container UI shown for job kinds that never use it,
  found live by Ben (2026-08-17). Fixed same night.** `NPCJobScreen`
  showed a "Set Deposit Container" button for *every* non-Crafting job,
  including Guarding — but `NPCGuarding` never reads
  `job.DepositContainer` at all (it patrols a Village Flag instead), so
  setting one on a Guard did nothing and genuinely misled Ben live ("has
  a set point" / "isn't moving toward it"). Same root pattern hit the
  new work-range leash field too — it only checked "does this NPC have
  an `NPCGathering` component" (true for every NPC, since all three job
  components coexist on the same prefab), not "is Gathering actually
  this NPC's current job," so it also showed (harmlessly, but
  confusingly) for a Guard. Both fixed: `NPCJobScreen` now checks
  `kind == Gathering` explicitly instead of `!= Crafting`; the leash
  field checks the NPC's actual assigned job kind. Compile-verified,
  confirmed live — leash field now only appears for the Mine Ore job.
- [x] **`NPCGuarding`'s patrol radius reused `VillageFlagRevealRadius`,
  a scale tuned for a different quantity — found live by Ben
  (2026-08-17). Fully resolved 2026-08-18 across 3 fix passes
  (v0.3.128/129/132-dev) — three separate real bugs, not one.** A Guard
  visibly wandering far from its Flag on the Map turned out to be
  exactly correct per the code: a Masterwork Flag's
  `VillageFlagRevealRadius` is 75f, giving the Guard a 75m patrol
  radius — huge relative to this project's 200×200 unit terrain, the
  exact "a tier-scaling ratio tuned for one quantity doesn't transfer to
  another" gotcha `CLAUDE.md` already documents. **Bug 1, fixed
  v0.3.128-dev**: rather than a second dedicated tier table, made patrol
  radius a player-set leash instead, same shape as
  `NPCGathering.MaxRangeFromDeposit` — `NPCGuarding.PatrolRadius`, a
  matching "Patrol radius (around Flag):" row in `NPCHiringScreen`, both
  leashes now persisting through save/reload. **Bug 2, found live
  testing the same night, fixed v0.3.129-dev**: with a small (2m) radius
  set, the Guard never got any closer to the Flag at all — the orbiting
  patrol target's tangential speed works out to exactly
  `radius × (moveSpeed/radius) = moveSpeed`, the Guard's own top speed,
  *regardless of radius*, so a small radius made the target uncatchable
  from outside (the original 35-75m radii always masked this). Fixed by
  splitting `UpdatePatrol()` into an approach phase (walk straight at
  the nearest point on the circle) and an orbit phase (only once already
  within range). **Bug 3, found live testing that same fix, fixed
  v0.3.132-dev — the real final answer**: the Guard still wasn't
  approaching the Flag, but this time it turned out to not be a movement
  bug at all — `[GuardDiagnostic]` logging (added, then removed once
  this was solved) showed it correctly `Attacking` a Wolf, and a
  screenshot confirmed the Wolf was already dead. The actual bug:
  `ThreatStillValid()` only ever checked distance, never whether the
  creature was still alive, and a killed creature's `GameObject` is
  never destroyed (`SkinnableCreature.Complete()` just
  `SetVisible(false)`s it and schedules a much-later `Respawn()`), so
  `currentThreat` never went null — the Guard stayed locked in
  `Attacking` forever, futilely trying to re-damage a corpse
  (`TakeDamage` early-returns once `isDead`). Confirmed live: even
  manually skinning the Wolf didn't unstick it, since skinning only
  hides the object, it doesn't destroy or revive it. Fixed by checking
  `IsDead` in both `ThreatStillValid()` and `FindNearestThreat()`.
  **Live-confirmed 2026-08-18** — Ben watched a Guard actually engage and
  patrol correctly, all 3 fixes holding together.
- [x] **A Mining NPC's target ore node appeared not to break/deplete,
  observed live by Ben (2026-08-17) — resolved 2026-08-18, was never a
  real bug.** Watched a Miner (confirmed genuinely working — 12 Silver
  Ore in cargo, Mining skill training) standing at a node that didn't
  visibly disappear. Temporary `[GatherDiagnostic]` logging confirmed it
  live: `Harvest on 'Boulder' ... succeeded=True item=Small Rock count=6
  stillAvailable=False` — a successful harvest correctly leaves the node
  unavailable immediately after, proving `ResourceNode.TryHarvestForNPC`
  was always working correctly. Was just a normal mid-`harvestDuration`
  snapshot, not a real bug. Diagnostic logging removed (v0.3.129-dev).
- [x] **Leather Backpack silently rejected as an NPC tool, found live by
  Ben (2026-08-17). Fixed 2026-08-18, v0.3.127-dev.** There are two
  separate Backpack item families — the original plain `Backpack` ladder
  and the newer `Leather Backpack` ladder (added later, once Deer/Hide
  closed the raw-material gap). All 4 jobs that need a Backpack tool
  (`MineOreJob`/`ChopWoodJob`/`ForageJob`/`MetalworkingJob`) only listed
  the original plain Backpack's 5 tier guids in their "Backpack"
  `ToolRequirement.acceptableItems` — none of them were ever updated to
  also accept the Leather Backpack tiers, so giving an NPC a Leather
  Backpack of any tier was flatly rejected. Same shape as the Campfire/
  `FriedEggCookable` registration gap from earlier this session — a
  newer item variant never backfilled into an existing acceptable-items
  list. Fixed by adding all 5 Leather Backpack tier guids to each of the
  4 jobs' Backpack requirement — confirmed via direct grep, no other job
  asset has a "Backpack" requirement besides these 4. **Confirmed live
  2026-08-18** — a Miner shown visibly wearing a Leather Backpack
  (distinct mottled texture from the plain canvas Backpack).
- [x] **NPC tool-giving only checks the player's top-level inventory,
  never a worn container's nested contents, found live by Ben
  (2026-08-17). Fixed 2026-08-18, v0.3.138-dev.** `NPCJobScreen`/`NPCJob
  .TryGiveTool`/`SwapTool` all called
  `playerInventory.GetCount(item)` directly — a tool sitting inside a
  worn Backpack's nested inventory didn't count, so it had to be pulled
  out to the main inventory first before an NPC could be given it.
  Reproduced live exactly as described: every tool requirement for a
  new hire read "(none in inventory)" despite the player visibly
  carrying a Pickaxe, Mining Face Shield, and Backpack candidates — all
  correctly stored inside a worn Masterwork Leather Backpack. Fixed with
  a new `PlayerCarriedItems.cs` (mirrors `InventoryScreen
  .GetWornContainers()`'s exact slot list/`IInventoryHolder` lookup),
  giving `GetTotalCount`/`RemoveOne` that check the main inventory first
  then every worn container (Back/Waist/Chest/Leg). `NPCJob
  .TryGiveTool`/`SwapTool` and `NPCJobScreen`'s "have N" display all
  route through it now. **Live-confirmed 2026-08-18** — Ben gave a Guard
  a Knife straight out of a worn Backpack, no manual pull-out-first step
  needed.
- [x] **Weak single-deflection obstacle avoidance still present in
  `NPCGathering`/`NPCCrafting`/`NPCTraining`/`NPCGuarding`, found live by
  Ben (2026-08-17) via a Guard permanently stuck near a Boulder — fixed for
  real 2026-08-19, v0.3.144-dev, see the entry's final paragraph below.**
  `NPCSeekFlag.MoveToward` had this exact bug (a single normal-
  based deflection that can point straight into a second obstacle at an
  odd-shaped collider and stall permanently) and was fixed in v0.3.116-dev
  with a widening directional search. The identical ~15-line pattern is
  still duplicated, unfixed, in these 4 other NPC movement scripts. Worth
  considering pulling into one shared helper this time instead of
  fixing/copy-pasting a 5th near-identical block — see `CHANGELOG.md`
  v0.3.123-dev. **Confirmed live a second and third time the same
  night** — a Miner stalled next to a Boulder mid-harvest, and again
  after its work-range leash was tightened to 15m (giving it even less
  room to route around anything in the way). Now the most-confirmed
  live bug of the whole session — worth prioritizing over most other
  open items next time this is picked up. **Confirmed a fourth+fifth
  time (2026-08-18)**: a Miner visibly stuck oscillating between move
  and mining animations near a small bush, and separately near a
  Boulder — a related but distinct symptom from the earlier full-freeze
  reports (cycling, not fully frozen). Theory: `IsActingOnTarget` flips
  purely on straight-line distance to the target crossing `harvestRange`,
  so an obstacle between the NPC and its target could make the
  deflection oscillate distance back and forth across that boundary
  without ever actually routing around the obstacle. **v0.3.133-dev**:
  re-added temporary `[MinerStuckDiagnostic]` logging to
  `NPCGathering.cs` (every move↔harvest transition, plus throttled
  obstacle-hit detail). **Investigated live 2026-08-18 — the obstacle-
  deflection theory was wrong for this specific repro.** The log capture
  showed zero `obstacle hit` lines despite many move↔harvest flips,
  ruling out the raycast-deflection mechanism entirely for this case.
  Also ruled out live: Apply Root Motion (confirmed enabled on the NPC
  Animator, but disabling it live changed nothing) and physics
  push-back (neither the NPC nor `HerbBush` has a `Rigidbody`). The
  *actual* cause of this specific repro turned out to be a completely
  different bug — see the `searchesBushes` entry below, now fixed. The
  raw distance-oscillation mechanism itself is still technically
  unexplained, but may not recur now that Mining/Woodworking NPCs can no
  longer target bushes at all. Diagnostic logging left in place in case
  a legitimate Forage NPC hits the same oscillation against a real bush.
  **Removed 2026-08-18** — the real root cause (below) was found and
  live-confirmed durable across a second, later session the same day, so
  the logging had done its job; all `[MinerStuckDiagnostic]` lines and
  their backing fields pulled from `NPCGathering.cs`.
  **Confirmed still happening against real ore too, same night** — with
  the bush bug fixed, a Miner correctly targeted a Copper Ore Node but
  showed the exact same oscillation pattern, proving it's target-type-
  agnostic, not specific to bushes. **Mitigated (not root-caused),
  v0.3.135-dev**: `NPCGathering.harvestRange` bumped 2m → 3m (Ben's
  call) — the observed drift is only ~0.1m each transition, so a full
  extra meter of margin absorbs it regardless of cause. Checked for (and
  found) a stale `harvestRange: 2` override baked into
  `NPCFactoryWorker.prefab`, same gotcha as `workDurationSeconds` two
  passes earlier — fixed both the code default and the prefab value
  together.

  **The oscillation itself: root-caused and fixed for real, v0.3.137-dev
  — a different bug from obstacle avoidance entirely.** Found by fully
  enumerating every component on the NPC prefab instead of continuing to
  test individual movement-system theories: `NPCGathering`/`NPCCrafting`/
  `NPCGuarding` all live permanently on every NPC prefab and each one's
  own `!ready` branch called `wander.SetPaused(false)`
  **unconditionally on every idle frame**, not just on a genuine
  active→inactive transition. So for a Mining-job NPC, `NPCCrafting`'s
  and `NPCGuarding`'s own `!ready` branches were both independently
  calling `SetPaused(false)` every single frame, racing against
  `NPCGathering`'s own `SetPaused(true)` with no defined winner (Unity
  doesn't guarantee `Update()` order between sibling components). On
  whichever frames the "wrong kind" component ran after the active one,
  `NPCWander`'s own independent wander-target-seeking silently took over
  movement for a frame before the active job reclaimed control —
  matching the observed drift exactly. Fixed in all three components
  with a `wasActive` flag (only releases the pause on a genuine
  transition); `NPCTraining`/`NPCSeekFlag`/`NPCFlee` checked and
  confirmed to not have this pattern. Also added a belt-and-suspenders
  safeguard in `NPCGathering`'s Harvesting branch (Ben's idea): position
  is snapshotted on settling into range and forcibly re-asserted every
  frame while harvesting. **Live-confirmed immediately** — clean single
  MOVING→HARVESTING transitions per target, no oscillation, correctly
  moved to a new ore node after each harvest. **Reconfirmed in a later
  session the same day** — still holding up after the subsequent NPC-
  management pass and Iron Arrow work, not a one-off.

  **Original Boulder full-freeze reports — resolved, confirmed live
  2026-08-18.** Ben retested directly and the full-freeze symptom no
  longer reproduces; the `wander.SetPaused` race fix above appears to
  have explained this symptom too, not just the oscillation. **What's
  still genuinely open, downgraded from "active bug" to "known code
  smell":** the underlying weak single-deflection obstacle-avoidance
  pattern this entry's title describes is still duplicated, unfixed, in
  `NPCGathering`/`NPCCrafting`/`NPCTraining`/`NPCGuarding` — a single
  normal-based deflection that *could in principle* still point straight
  into a second obstacle at an odd-shaped collider and stall, the same
  bug `NPCSeekFlag.MoveToward` had before its v0.3.116-dev widening-
  search fix. With no live repro left to chase, this is now proactive
  robustness work, not an active bug fix — worth doing (port the
  widening-search pattern, or better, pull it into one shared helper
  instead of a 5th near-identical copy-paste, per the note above) but no
  longer urgent.

  **Actually done, 2026-08-19, v0.3.144-dev — the "pull into one shared
  helper" option above, not another copy-paste.** New
  `Assets/Scripts/NPCMovement.cs` (a plain static helper, same shape as
  `GroundHeight.cs`) pulls `NPCSeekFlag`'s widening-search deflection out
  into one shared `FindClearDirection`, used by all 5 scripts' `MoveToward`
  now instead of 4 separate copies of the old weak single-normal-deflection
  block. Also added a small `StuckTracker` (per-mover, ~2s check interval,
  3 consecutive near-zero-progress checks before triggering) shared the
  same way — on trigger, the next move gets a hard reverse shove instead of
  a normal probe, then resets. This substantially mitigates (but doesn't
  formally close) the separately-logged `NPCSeekFlag`
  no-timeout-while-approaching gap further down this file — it can no
  longer wedge forever, but there's still no hard timeout backstop for a
  genuinely escape-proof pocket. Compile-verified via batch mode (zero
  `CS####` errors); not yet live-tested.
- [x] **Mining/Woodworking-job NPCs could target bushes meant for
  Forage — found live by Ben (2026-08-18) investigating the bug above:
  a Mining NPC walked past ore to reach the nearest HerbBush, then tried
  to play its Mining swing animation on it. Fixed same night,
  v0.3.134-dev.** `NPCGathering.FindTarget()`'s `INPCSearchable` pool
  (BerryBush/HerbBush) has no tool requirement at all (searching is
  bare-handed), so unlike the `INPCHarvestable` pool (naturally
  segregated by `RequiredTools` — a Miner's Pickaxe can't satisfy a
  Tree's Axe requirement), nothing stopped *any* Gathering-kind job from
  freely targeting a bush purely on distance. Exact same shape as the
  already-fixed `collectLoosePickups` gap from 2026-08-13 (a Mine Ore
  NPC "stuck gathering sticks"). Fixed with the same pattern:
  `NPCJobDefinition` gained a `searchesBushes` bool (default false)
  gating the Searchable pool scan; only `ForageJob.asset` sets it true.
  **Live-confirmed 2026-08-18** — Ben watched a Miner correctly ignore
  nearby bushes and stay on ore.
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

- [x] **Egg has no icon at all, found live by Ben (2026-08-16) — fixed
  2026-08-17.** Confirmed it was exactly the predicted "never baked, not
  broken" case, not a model problem — a plain `IconBaker` pass (`-modelPath
  Assets/Prefabs/EggPickup.prefab -itemAssetPath Assets/Data/Egg.asset
  -previewResolution 128`) wired both `icon` and `previewIcon`. Verified by
  actually reading the rendered PNGs, not just trusting the batch log — both
  show a real, visible egg, not a blank/black frame.
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
- [x] **Unconfirmed: a Combat-category skill tier-unlock (killing a Wolf,
  Rudimentary) may not have granted Fame (2026-08-14/15) — confirmed a
  false alarm, 2026-08-19.** A Restoration tier-unlock during live
  testing gave the "next time" repro this entry asked for: a live Console
  log showed `[Fame] +1.00 -> 31.00 (Neutral)` at the exact same
  timestamp as `Restoration TIER UNLOCKED: Rudimentary`. The mechanism
  works correctly — the original report was very likely the "message had
  already expired" ambiguity this entry already flagged, not a real bug.
  Ben reported
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
- [x] **`RectangularHouseTwig`/`RectangularHousePlank` prefab buildings had
  broken roof geometry at both gable ends (2026-08-14) — fixed
  2026-08-18.** Stale by the time it was picked back up: a real Gable
  Panel piece (`TwigGablePanelPiece`/`PlankGablePiece` — full `BuildPiece`
  + model + recipe + icons, self-pairing on a Wall's `WallTop` socket the
  same way `RoofPanel` does) already existed in the project, just never
  placed anywhere — this entry's original "no gable-end roof piece exists
  yet" claim no longer held. The actual bug was narrower than described:
  both pre-built house prefabs capped their short (gable) ends with a
  `RoofPanel`/`PlankRoofPiece` instance rotated 90° sideways instead of a
  real vertical gable infill — confirmed by reading the prefab YAML
  directly (6 roof-tagged children each, 4 correctly tiling the long
  eaves, the other 2 at the short-end `WallTop` sockets, x=-2.5 and x=7.5,
  rotated ±90°). Fixed via a throwaway batch-mode Editor script
  (`Assets/Editor/FixGableEnds.cs`, deleted after running, per
  `CLAUDE.md`'s scene/prefab-edit convention): swapped those 2 misapplied
  roof-panel instances per prefab for the correct Gable Panel piece at the
  exact same transform (same socket, same position/rotation — no new
  placement math needed, since the Gable Panel's own local origin is
  already its base-center attach point, matching the convention
  `RoofPanel`'s eave-at-origin trick already established). Verified by
  grepping both saved prefabs' YAML directly for the new piece's guid at
  the expected transforms, not just trusting the script's log — confirmed
  correct in both `RectangularHouseTwig.prefab` and
  `RectangularHousePlank.prefab`. **Live-confirmed 2026-08-18** — Ben
  looked at the actual building in Play mode, gable end closes correctly,
  no more roof panel poking through. See `MVP2_PLANNING.md` item 10.
- [x] **`WovenGrassCloth.mat` also has `metallicFactor: 1` (2026-08-14) —
  checked 2026-08-18, closed as a non-issue.** Found while checking
  whether the `IconBaker` near-black-metallic bug (fixed same day, see
  `CHANGELOG.md`'s v0.3.58-dev entry) affected anything besides the new
  Ingot family. Directly viewed `WovenGrassClothItemIcon.png` — renders
  as a clear, legible green woven-cloth icon, not the near-black
  silhouette symptom the Ingots had. `metallicFactor: 1` alone doesn't
  automatically trigger the bug; no re-bake needed here.
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
  - [ ] **Multi-day NPC work-shift timer — moved here from MVP2_PLANNING.md
    as an enhancement, 2026-08-21 (Ben's call).** Still the 5-real-minute
    stand-in described above. Persistence now exists and is trustworthy
    across sessions (unlike when the stand-in was first shipped), so the
    real prerequisite this was blocked on is gone — but replacing the
    stand-in with an actual multi-day real-world timer is a real design
    decision (how many real days per shift, whether it should scale with
    anything) worth making deliberately, not a currently-blocking bug.
    Logged as backlog rather than an MVP2 blocker.
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
- [x] **`CraftingRecipe.requiresCanteenWater` only checked a Canteen held
  in a hand, not one attached to a Belt (2026-08-10). Fixed 2026-08-18,
  v0.3.139-dev.** `PlayerCrafting.FindEquippedCanteen` only looked at
  `PlayerEquipment`'s Left/Right Hand slots — a Belt-worn Canteen would
  have silently failed Healing Paste's water-gate check even with
  plenty of water aboard. Fixed by also checking the worn `Belt`'s own
  `Inventory` for a clipped Canteen (`slot.equipment is Canteen`), the
  same data relationship `PlayerBelt.DropClippedEquipment` already
  accounts for elsewhere. **Also found and fixed the identical gap in
  `Campfire.FindPlayerCanteen`** (Herbal Tea's own water check) while
  fixing this — same root cause, same fix, both call sites now
  consistent. **Live-confirmed 2026-08-18** — Ben confirmed with a
  worn-Belt Canteen (not held), Healing Paste crafted successfully. The
  Campfire/Herbal Tea half shares the identical fix but hasn't been
  separately re-tested — low risk given it's the same code shape.
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
  fixed cooldown instead of the player's manual draw-and-hold. Gun and
  gameplay sound (combat hits, arrow whoosh, footsteps, crafting/UI — no
  such system exists; **not** the same gap as ambient weather audio, which
  works — see the "Gameplay audio system" entry in the Enhancements
  backlog above) are still separate, explicitly open gaps. **Iron Arrow —
  built 2026-08-18, see its own entry below.** Bare-handed's own
  numbers (9 dmg, 0.7s cooldown) are still first-pass, not vetted against
  a real weapon-tier progression.
- [x] **Iron Arrow — built 2026-08-18.** Ben's ask: an Iron equivalent of
  Stone Arrow, arrowheads from Iron Ingot instead of Stone, no tier ladder
  on the arrowhead itself, assembled arrow tier matched to the crafted
  Trimmed Stick, and stronger damage than Stone. Asked to "be mean" and
  evaluate first — real critique before building: a literal ×2 on
  `ArrowDamageBonus` would leave Crude Iron dealing the same 0 bonus as
  Crude Stone (the table starts at 0), break the tier system's promise
  that "Masterwork" means the same relative power across every item
  family, and every arrow is destroyed on fire with no recovery
  (`PlayerRangedCombat.Fire` calls `RemoveItem` unconditionally,
  hit or miss) — so an Iron Arrow economy burns a real, competed-for Iron
  Ingot per shot, forever, not a one-time craft. Also flagged upfront:
  `GuardRangedJob`'s Arrow `ToolRequirement.acceptableItems` is a
  hardcoded guid list, the exact "Leather Backpack silently rejected as
  an NPC tool" gotcha shape — a new arrow family needed that list updated
  or a Guard handed Iron Arrows would silently refuse them.

  Built to a countered proposal instead of the literal ask: Iron beats
  Stone at every tier including Crude (Stone 0/1/2/4/6 → Iron
  **2/3/4.5/7/9.5**, ~58% higher ceiling, not 100%), `IronArrowheadRecipe`
  (1 Iron Ingot → 2 Iron Arrowhead, trains Metalworking, requires the
  Anvil — mirrors Shovel's own Metalworking+Anvil precedent, not Stone
  Arrowhead's Stonework) plus 5 assembly recipes (1 Iron Arrowhead + 1
  tier-matched Trimmed Stick → 5 arrows, trains Woodworking — identical
  shape to the existing Stone Arrow recipes). New
  `ItemDefinition.arrowDamageBonus` (sentinel `-1` = "use the shared
  `CraftTierScale.ArrowDamageBonus(tier)` table," every existing arrow
  unaffected) lets a different arrow *material* deal different damage at
  the same nominal `CraftTier` — deliberately not a second
  material-keyed table bolted onto `CraftTierScale` itself, per that
  file's own "a scale tuned for one thing doesn't transfer to another"
  gotcha. `PlayerRangedCombat.Fire`/`NPCGuarding`'s ranged-attack roll
  both read the new `EffectiveArrowDamageBonus` property instead of
  calling `CraftTierScale.ArrowDamageBonus` directly.

  Visually distinct from Stone, not just a reskin-by-name: reused the
  exact same `StoneArrow.glb`/`StoneArrowhead.glb` geometry (Stone
  Arrow's own 5 tiers already share one identical mesh — no per-tier
  visual differentiation exists in this family to begin with) but swapped
  the Tip/Arrowhead submaterial to a new metallic `IronArrowheadMetal.mat`
  via a batch script's per-instance material retarget (matched by the
  *embedded* glTF material name, `StoneArrowTip_mat`/
  `StoneArrowheadStone_mat` — first attempt matched on the extracted
  `.mat` asset's name instead and silently swapped nothing, caught by a
  small diagnostic dump of every renderer's actual material name rather
  than assuming). Confirmed by reading the baked icon PNGs directly:
  `IronArrowheadIcon.png` renders visibly darker steel-gray with a
  metallic sheen against `StoneArrowheadIcon.png`'s pale tan, not
  identical images with different names.

  `GuardRangedJob`'s Arrow `acceptableItems` updated to include all 5 new
  guids alongside the existing 5 Stone ones (10 total) — verified via
  direct YAML grep, no duplicates. `DatabaseRepopulator` re-run
  (Items 126 → 132). Compile-verified; not yet live-confirmed (craft an
  Iron Arrowhead, assemble arrows, fire one, hand a set to a Guard).
- [ ] **Bow Release animation always returns to StandingIdle
  specifically (2026-08-15), not whatever stance the player was
  actually in before drawing.** Known limitation from choosing a
  full-body state swap over a masked upper-body layer — fine for
  standing, but drawing a bow while Kneeling/Crawling/Prone will snap
  the player's visual stance back to standing after the shot. Fix would
  mean either a masked layer (bigger rework) or per-stance return
  transitions in both Animator Controllers.
  **Reconsidered 2026-08-18 during a bug-list clearing pass** — genuinely
  bigger than a quick fix, needs real Animator Controller state-graph
  work verified live, not safe to attempt blind. Left as-is.
- [x] **32 `ItemDefinition` items needed a deliberate `weight` value —
  all sitting at the untuned 1 lb default (2026-08-10). Fixed
  2026-08-18, v0.3.139-dev.** The original artifact turned out partly
  stale by the time this was picked up — checked every listed item's
  actual current state directly rather than trusting the doc: Stick
  (0.5), Plank (3), the full Trimmed Stick ladder (all 0.5), and Rock
  (1.5) had all already been tuned since 2026-08-10, leaving 24 genuinely
  untouched. Proposed a full table (calibrated against the already-tuned
  Backpack/Knife/ore families) for Ben's review before applying — raw
  materials (Fiber 0.1, Iron Ore 1.2, Berry 0.05, Berry Seed 0.02),
  refined materials (Rope 0.4, Cloth 0.3, Iron 2.5, Copper 2, Nail 0.02,
  Woven Grass Cloth 0.3), the Leather Backpack ladder (7.5/6/5/4/3,
  mirroring the plain Backpack ladder exactly), standalone gear (Crude
  Fiber Backpack 5, Crude Fiber Belt 0.5, Storage Box 15), wearable
  gadgets (Canteen 1, Sunglasses 0.2, Navigation Computer 1.5, Personal
  Health Monitor 1, Mining Face Shield 2), and Soccer Ball (1). Approved
  as-is, applied to all 24 assets, confirmed via direct grep. 3 of the
  24 (Sunglasses/NavComputer/HealthMonitor) turned out to be legacy
  assets predating several `ItemDefinition` fields (`tier`/`icon`/
  `previewIcon` not even serialized) — only `weight` was added, the rest
  is a separate, out-of-scope gap. Compile-verified; not yet
  live-confirmed.
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
  **Reconsidered 2026-08-18 during a bug-list clearing pass** — still
  correctly left as Ben's own deliberate trade-off call, not touched.
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
  **Reconsidered 2026-08-18 during a bug-list clearing pass** — this is
  an already-investigated dead end (root cause not found after real
  effort, shipped deliberately per Ben's call), not a quick fix; left
  untouched rather than repeat the same blind guessing.
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
- [x] **Procedural tree (v0.1.58-dev) — superseded, closed 2026-08-20,
  nothing left to remove.** The real scattered tree models (`Big Tree by
  3Donimus`, imported via the Poly Pizza/Tripo3D pipeline, see this
  file's own "Scatter a random number of trees" entry) replaced this
  system for real gameplay use a while ago, same way Weather Maker
  superseded the old procedural sky texture this entry's own bark-color
  theory blamed. **Checked before deleting anything, per this file's own
  "confirm before deleting" discipline**: `GenerateTree.cs` no longer
  exists in the repo at all (no file, no git history for it either — it
  was a throwaway Editor script from early in the project, already
  cleaned up per the standing convention, same as every other one-off
  generation tool). The only artifact that shared its name,
  `TreeBark.mat`, is **not dead** — it's genuinely still referenced by
  `Log.prefab`/`LogPickup.prefab` today (the choppable/dropped Log
  item's real bark texture), so it stays. No leftover procedural-tree
  GameObject exists in `TestScene.unity` either. Closed with zero files
  deleted — the cleanup was already complete, just not marked as such.
  Original report preserved below for context on what was wrong with it,
  now moot:
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
- [x] **`LeatherBackpackRecipe.asset` (new, v0.1.134-dev) used placeholder
  ingredients (6x Cloth + 4x Rope) — explicitly temporary. Fixed
  2026-08-18, v0.3.139-dev.** Ben's original call: build the recipe
  shape now, swap in real Leather/hide-tanning materials once that
  chain exists. Leather has existed as a real, obtainable item since
  Deer hunting shipped (2026-08-15, v0.3.95-dev) — swapped the
  placeholder for 4x Leather + 2x Rope. **Live-confirmed 2026-08-18** —
  Ben opened the recipe in-game, shows a real 4 Leather / 2 Rope
  ingredient list.
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
- [x] **Closed 2026-08-21, Ben's call — Leather sourcing (Deer hunting,
  since v0.3.95-dev) is a real, working path going forward.** Fiber → Cloth textile chain, and a way to source Leather — needed
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
  **Re-audited 2026-08-18 (Category C pass) — this specific claim
  confirmed still accurate, not stale**: checked every ore-related
  `ResourceNode` prefab directly (`Boulder`/`CopperOreChunk`/
  `GoldOreChunk`/`IronOreChunk`/`SilverOreChunk`/`PlatinumOreChunk`/
  `MediumRockChunk`) — every single one still trains `Gathering`, none
  train `Mining`, even though a real `Mining.asset` `SkillDefinition`
  now exists (added later purely for the NPC job-family system —
  `MineOreJob.family` — never wired to the player's own mining action
  at all). `Log.prefab` by contrast does correctly train `Woodworking`.
  The rest of this entry (the much larger material-web/tier/interaction
  redesign) was not independently re-verified clause-by-clause this
  pass — still assume it needs a fresh read against current code before
  touching, per its own standing warning above.
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
  given yet to differ). **Still not built:** Fireball, Illusion's own
  wish (still completely empty), found and scribed Scrolls, and the
  Scribing skill itself. **Real simplification, not an oversight:** no
  wish's roll weakest-links against any material/fuel-tier input — the
  design-brief's original weakest-link-quality idea for wishes was
  superseded by the success/failure roll instead, flagged directly in
  that doc, not left implying both are true. Don't assume any of the
  deferred pieces exist without checking — this is a large,
  only-partially-built system.
  **Re-audited 2026-08-18 (Category C pass) — two claims here were
  stale, now corrected above.** "Learnable additional lineages... ride
  the not-yet-built Phase 2 skill-books mechanic" was true when written
  but Skill Books shipped 2026-08-13 (`SKILL_BOOKS_PLANNING.md`) and
  `PlayerMagic` now has a real `knownLineages` set + `LearnLineage`, not
  a single fixed `StartingLineage` — a player genuinely can learn
  additional lineages today via a found/written wish book. "Fireball
  (needs a combat system that doesn't exist)" is also stale — melee
  (v0.3.61-dev) and ranged (v0.3.86-dev) combat both shipped since this
  was written. Confirmed via direct check that no `Fireball` `WishRecipe`
  asset exists yet, so it's still genuinely unbuilt — just not for the
  reason originally stated; nothing currently blocks building it.
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
  Building System section. **Still not built:** Floor/Ceiling/Window,
  Stairs/Ramps (vertical connectors — need a new two-height socket
  shape), Shelves/furniture (mount to Wall, not designed), Rock/Metal
  material tiers beyond Nails (blocked on their own crafting-pipeline
  chains), mixed-material-structure rules, structural-integrity
  requirements beyond "a socket exists," Equip-to-Define (no
  equipment-function system for a shell to plug into yet), and
  territory/ownership restrictions (no multiplayer/macro-layer exists).
  Don't assume any deferred piece exists without checking.
  **Re-audited 2026-08-18 (Category C pass) — 3 of the originally-listed
  "still not built" pieces are stale, now removed above.** Pole
  (`TwigPolePiece`/`PlankPolePiece`), Door
  (`TwigDoorPiece`/`PlankDoorPiece` + matching Door-Frame-Wall variants),
  and Roof (`TwigRoofPanelPiece`/`PlankRoofPiece`) are all real, built
  prefabs today — confirmed via direct file check, not just a doc
  cross-reference. (Roof's own gable-end-geometry bug, logged separately
  above, is now fixed and live-confirmed as of 2026-08-18.) Floor/Ceiling/Window/Stairs/
  Ramps/Shelves genuinely still don't exist — checked directly, no
  matching prefabs found for any of them.
  **A more robust piece-variety system is needed — Ben's live-testing
  finding, 2026-08-20.** Building a real multi-Foundation building (this
  session's playtest tiled 4 Foundations together) exposed a genuine
  structural gap, not just a missing-piece checklist item: a roof spanning
  2+ Foundations wide can't actually seal, because `Roof`
  (`TwigRoofPanelPiece`/`PlankRoofPiece`) only has the one panel size/
  shape, sized for a single-Foundation span — there's no way to close the
  gap over a wider footprint. This is the same underlying limitation as
  the already-tracked missing Floor/Ceiling piece (nothing spans multiple
  Foundations at all yet, roof or floor), just surfaced concretely by an
  actual multi-tile build instead of staying abstract. Not scoped yet —
  would need either wider roof/floor panel variants (a real new size
  tier, not just reusing the existing single-Foundation-span mesh scaled
  up, since scaling would distort the panel's baked pitch/proportions)
  or a genuinely tileable roof/floor system (multiple same-size panels
  covering a span, closer to how Foundation itself already tiles). Worth
  a real design pass before building — logged here as a live-confirmed
  need, not designed further yet.
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

  **Re-audited 2026-08-18 (Category C pass) — confirmed still accurate,
  not stale.** Checked `PlayerLoot.cs` directly: it still evicts
  (physically drops) whatever's occupying the first hand to make room
  for a new item when no backpack is equipped, exactly as described —
  this gap was never closed.

  *(Reported by Ben. The despawn timer on dropped items that was originally
  part of this same request shipped separately in `v0.1.48-dev` (15 min),
  shortened to 2 min and extended to cover equipment/coins too in
  `v0.1.85-dev` — see `CHANGELOG.md` for both. Still doesn't cover the
  equipped-item unequip-fallback drop path described above, since that
  path isn't built yet either — despawn now covers every *existing* drop
  action, not this still-hypothetical one.)*
- [x] **Equip directly from a container — stale, already fixed, closed
  2026-08-18 (Category C re-audit).** Same underlying gap as "Eat/Drink
  directly from a container" below was originally, but right-clicking
  any slot (`InventoryScreen.HandleSlotEvents`) now opens a real action
  popup with a genuine "Equip"/"Unequip" button whenever the slot holds
  an `IEquippable`, and that handler is explicitly shared by "the main
  inventory grid, the equipment slot list, and every container's
  contents grid (backpack, boot slots, storage boxes)" per its own
  comment — confirmed by reading the code directly, not just trusting
  the doc. Very likely fixed as a side effect of the 2026-08-12
  drag-and-drop rework that unified all three grid types onto one
  shared popup-based interaction model, well after this was originally
  logged; nobody circled back to close the entry out at the time.
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
