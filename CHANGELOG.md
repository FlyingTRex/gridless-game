# Changelog

Notable changes to the Gridless project, newest first. Written for whoever (human or
Claude session) picks this repo up next — includes the *why* behind non-obvious
decisions, not just the *what*. Full detail is always in `git log`; this is the
skimmable version.

**Current version:** `0.1.74-dev` — must always match `GameVersion` in
`Assets/Scripts/FirstPersonController.cs` (shown on-screen in the bottom-left debug
panel). Bump both together in the same commit whenever gameplay code/scenes/prefabs
change; see `CLAUDE.md` for the exact rule.

## 2026-08-06

### v0.1.74-dev — Backpack's visual replaced with an AI-generated model

First use of a Tripo3D API-generated (not just third-party CC-BY) model
actually wired into gameplay, and the first head-to-head test of API vs.
Tripo Studio web-UI output on the exact same prompt — see
`Tools/Tripo3D/README.md` for the full comparison writeup. The web UI
version came out rope-laced with no metal hardware; the API version (used
here) has a metal buckle and snap studs and reads more polished. Both
generated from the same "crude leather backpack" text prompt.

- Model: `Assets/Models/CrudeLeatherBackpack.glb` (Tripo3D API,
  commercial use included per API/pay-as-you-go terms — see
  `Tools/Tripo3D/README.md`).
- Replaces the 5-scaled-cube placeholder (Body/Flap/StrapLeft/
  StrapRight/Pocket) on both the scene's standalone "Backpack"
  GameObject in `TestScene.unity` (the actual functional one — equip/
  drop reparents this same instance under `BackpackAnchor` rather than
  instantiating from a prefab) and `Assets/Prefabs/Backpack.prefab`.
  Confirmed via guid search that the prefab is otherwise unreferenced
  anywhere in the project, but kept in sync in case that changes later.
- Scaled by 0.53 to match the old placeholder's measured height
  (renderer bounds, per the CLAUDE.md pivot/bounding-box gotcha); no
  rotation or centering offset needed — the new model was already
  centered on its local origin.
- Root transform's scale was already uniform `(1,1,1)` here, so none of
  the non-uniform-scale/collider-preservation care the Stick swap needed
  applied.

### v0.1.73-dev — Stick's visual replaced with a real branch model

First non-comparison use of an externally-sourced model in this project —
everything before this (the AI-generated berry bush, the Big Tree) was
placed for side-by-side visual review only, not actually wired into
gameplay. This one replaces the Stick item's placeholder box mesh
everywhere it appears: `Assets/Prefabs/StickPickup.prefab` (used when a
Stick is dropped or freshly spawned) and the two pre-placed "Stick
Pickup"/"Stick Pickup 2" world objects in `TestScene.unity` — confirmed
these were never actual prefab instances of `StickPickup.prefab` despite
looking identical, so both had to be updated independently, not just the
prefab.

- Model: "Tree branch by Poly by Google" (CC-BY, via Poly Pizza) —
  `Assets/Models/TreeBranch_PolyByGoogle.glb`, 610 vertices, single mesh.
  Attribution tracked in `Assets/Models/THIRD_PARTY_CREDITS.md`, still
  needs to land in `GameMenuScreen`'s Credits tab before release.
- The model's long axis was vertical (Y) on import; rotated 90° on X to
  lie flat along Z instead, then scaled so its length matches the old
  placeholder's (0.6). Real bug caught before it shipped: the affected
  GameObjects had a **non-uniform** root scale (`(0.1, 0.1, 0.6)`, sizing
  the old box mesh+collider together) — naively parenting the new model
  under that would have multiplied its own scale by the parent's
  non-uniform one, badly distorting it. Fixed by resetting each root's
  scale to identity and explicitly preserving the `BoxCollider`'s
  original world-space size on the collider itself instead of relying on
  transform scale to produce it.
- See CLAUDE.md's new bounding-box-placement gotcha (added earlier this
  session, prompted by the Big Tree sinking into the ground) — the same
  "don't assume an imported model's pivot/orientation matches what you
  expect" discipline applied here too, verified in-script before
  committing to the prefab/scene rather than eyeballed.

### v0.1.72-dev — Trimmed the default startup scene

Ben's planning pass: reviewed everything that spawns in `TestScene.unity`
at startup (29 named spawn points, later corrected to 33 actual root
objects once queried precisely) and cut it down to reduce clutter.

**Removed from `TestScene.unity`** (deleted outright, not disabled):
5 Coins (Copper/Iron/Silver/Gold/Platinum), Secret Wall, Navigation
Computer, Personal Health Monitor, Sunglasses, Mining Face Shield,
Silver/Gold/Platinum Ore Nodes, the larger Storage Box (Small Storage Box
kept), and 3 of the 4 Trees (1 kept). Backpack and Canteen were the two
gadgets explicitly kept as starter gear. Silver/Gold/Platinum Ore Nodes
specifically are needed again later for testing, not gone for good.

Verified via Unity's own `GetRootGameObjects()` (not raw YAML grep — Trees
are prefab instances and don't literally contain `m_Name: Tree` in the
scene file, which produced a false "0 Trees remain" alarm mid-task before
querying the scene directly resolved it): 33 → 16 root objects, exactly
matching the 17 removed.

### v0.1.71-dev — First material-web refining step: Stick + Knife → Trimmed Stick (trains Woodworking)

First real content in the **Woodworking** discipline tab, which has sat
empty since it was created a few hours earlier this same day — Stick
→(Knife, Woodworking)→ Trimmed Stick, straight from the material web in
`docs/design-brief.md`.

- **`CraftingRecipe` gained `requiredTools[]`/`requiredToolLabel`** — a
  recipe can now require a tool *held in a hand, not consumed*, on top of
  its normal consumed `ingredients`. Same "any tier counts" convention as
  `ResourceNode.requiredTools` (any of the 5 Knife tiers satisfies it, not
  just one specific tier). `PlayerCrafting` gained a `PlayerEquipment`
  reference and `HasRequiredTool()`; `TryCraft` checks it up front, and
  `CraftingScreen` greys out Craft and shows `— requires Knife in hand`
  when it's not met, same visual pattern as the existing
  materials/inventory-space gating.
- **5 new Trimmed Stick items + recipes** (Crude through Masterwork,
  Ben's call — full tier treatment from the start this time, not staged
  in as a single item first). Same "identical recipe across all 5 tiers"
  placeholder approach as yesterday's tool tiers: each costs 1 Stick +
  any Knife in hand, differing only in which tier's item comes out.
  Trains `Woodworking` (`skillGain: 2`, matching every other recipe's
  default).
- Both throwaway batch-mode runs hit a stale `bee_backend` lock from an
  earlier run that hadn't fully released — a Unity process sat idle for
  several minutes producing no output before failing. No project files
  were affected; killing the orphaned process and retrying compiled
  clean. Worth watching for again: if a batch-mode run goes unusually
  quiet, check for a lingering `Unity`/`bee_backend` process before
  assuming the run itself is broken.

### v0.1.70-dev — Discipline sub-tabs for Crafting/Skills, folder-tab styling, Crafting skill retired

Implements the discipline-sort model from today's earlier planning
conversation (see `docs/design-brief.md`'s 2026-08-05 Pipeline update) —
both the backend skill/recipe repointing and the UI to actually make a
25-recipe flat list navigable.

- **`SkillDefinition` gained a `category` field** (`SkillCategory`:
  Gathering / CraftingDiscipline / Combat) — which sub-tab of `SkillsScreen`
  a skill's level shows under.
- **6 new discipline skills**: Woodworking, Stonework, Metalworking,
  Forging, Minting, Sewing (all `CraftingDiscipline` category). `Gathering`
  migrated to the `Gathering` category (field didn't exist on it before
  today).
- **`Crafting` skill retired and deleted** (`Crafting.asset` removed —
  verified zero remaining references first, not just assumed). Every item
  now sorts into exactly one discipline by its defining material, so the
  generic catch-all no longer has anything left to cover. All 20 tool
  recipes (Knife/Hammer/Axe/Pickaxe × 5 tiers) repointed to **Stonework**
  — all four are stone-headed tools today. The 5 gadget recipes
  (Sunglasses, Nav Computer, Health Monitor, Mining Face Shield, Canteen)
  now train **no skill at all** (`trainedSkill = null`) rather than being
  force-fit into a discipline that was never designed for them — Ben's
  call, they were "just to test ideas up front anyway."
- **`CraftingScreen` sub-tabbed by discipline** — one tab per discipline
  skill (`disciplines[]`, an explicit hand-maintained list like
  `GameMenuScreen.ControlsList`, not discovered dynamically, so an empty
  discipline still gets its own tab with an honest "No recipes yet."
  placeholder) plus a fixed **Other** tab for the 5 no-skill gadget
  recipes.
- **`SkillsScreen` sub-tabbed by `SkillCategory`** — Gathering / Crafting
  Disciplines / Combat. Combat is permanently empty today (no weapon
  skills exist, no combat system to train them) — same honest-placeholder
  treatment as `GameMenuScreen`'s Audio/Graphics tabs.
- **File-folder tab styling** (`DebugGUI.TabSelected`/`TabUnselected`) —
  Ben's ask, applied consistently to all four tab bars in the game
  (`GameMenuScreen`, `PlayerMenuScreen`, and the two new sub-tab bars).
  The selected tab shares `DrawPanel`'s exact background color and sits
  flush against it (no border between tab and content); inactive tabs use
  a visibly darker, receded surface. Replaces the old bold-vs-plain-text
  distinction everywhere it was used. Pure procedural `GUIStyle`/solid-color
  textures, no imported graphics — first pass, will need the usual
  screenshot-feedback round to actually judge how it reads.

### v0.1.69-dev — Knife/Hammer/Axe/Pickaxe now come in all 5 CraftTiers

First implementation slice of the "next session" plan logged yesterday —
preceded by a planning conversation (see that plan's entry in
`BUGS_AND_ENHANCEMENTS.md` for the forks it resolved and why). Scope for
today, deliberately: get the data/UI scaffolding in place, not tune real
values. Spear and Bow are **not** part of this — deferred, since neither
has a function yet (no combat system) and Bow's designed recipe needs the
unbuilt Rope/Textiles chain; revisit once combat exists.

- **`ItemDefinition` gained a `tier` field** (`CraftTier`, defaults to
  `Normal`) — every item now has one, meaningful for the ones that
  actually come in a 5-tier ladder. Needed groundwork for the eventual
  weakest-link crafting rule, which has to read an ingredient's own tier.
- **Consolidated the 4 existing tools as the Crude tier**, not left as
  parallel duplicates: `Rock Knife`→`Crude Knife`, `Rock Hammer`→
  `Crude Hammer`, `Axe`→`Crude Axe`, `Pickaxe`→`Crude Pickaxe` (renamed via
  `AssetDatabase.RenameAsset`, so GUIDs — and every existing reference —
  stayed intact). Added the other 4 tiers per tool as new assets: 16 new
  `ItemDefinition`s + 16 new `CraftingRecipe`s, 20 of each total.
- **Recipes are intentionally identical across all 5 tiers of a tool**
  right now (Ben's call) — every tier costs the same ingredients as today's
  Crude version. There's no gate yet stopping you from crafting a
  Masterwork Knife as easily as a Crude one; that's expected, not a bug —
  the weakest-link rule that would actually enforce tier progression isn't
  built. Pure scaffolding for now.
- **All 20 recipes train the existing `Crafting` skill**, same as before —
  no new Woodworking/Stonework/Forging assets created. Raised during
  planning (Hammer alone plausibly touches Forging, Woodworking, *and*
  Stonework) and explicitly deferred rather than guess at a mapping nobody
  was confident in; easy to repoint later once the refining pipeline
  exists and settles which skill(s) each tool actually trains.
- **`ResourceNode.requiredTool` (single item) → `requiredTools[]` (any of
  these satisfy the gate) + `requiredToolLabel` (display string for the
  prompt).** Necessary fix, not optional: the old single-reference field
  would've only recognized *one* of the 5 Pickaxe/Axe tiers once they
  split, silently breaking ore/tree gating for the other four. Re-wired
  all 5 Ore Nodes (any Pickaxe tier) and `Tree.prefab` (any Axe tier) to
  the new array; Rock Node and Boulder correctly stay tool-optional
  (empty array).
- **`PlayerMenuScreen`'s Skills and Crafting tabs now scroll** — added
  ahead of the Crafting tab's recipe count jumping from ~9 to 25, which
  would otherwise run off the bottom of the screen. Inventory's tab keeps
  its own existing scroll view (pinned currency row) rather than getting
  double-wrapped.

### v0.1.68-dev — Admin tab: spawn any item in front of the player (Editor-only)

New **Admin** tab on `GameMenuScreen` (` key) — Ben's ask, queued up
yesterday as prep for testing tomorrow's batch of new craft-tier tools
without having to craft each one from zero first.

- New `AdminSpawnScreen`, holding the Admin tab's actual content (same
  split as PlayerMenuScreen's tabs each owning their own component).
  Lists every `ItemDefinition` asset in the project, alphabetized, each
  with a **Spawn** button that materializes one directly in front of the
  player.
- `PlayerDropping` gained a `SpawnPickup(item, count = 1)` method —
  extracted from the tail end of `DropFrom` (instantiate the item's
  `worldPickupPrefab`, or the generic fallback, and `Pickup.Configure` it)
  so Admin-spawning reuses the exact same "materialize a physical item"
  logic a manual Drop already uses, rather than duplicating it. `DropFrom`
  itself is unchanged in behavior, just calls the extracted method now.
- **Editor-only, deliberately:** the item list is discovered via
  `AssetDatabase.FindAssets("t:ItemDefinition")`, which only exists inside
  the Editor — auto-discovery means a newly-created item (like tomorrow's
  tool tiers) just shows up with nothing to remember to register, unlike
  `GameMenuScreen.ControlsList` or `PlayerCrafting`'s recipes array. The
  whole class is wrapped `#if UNITY_EDITOR` with a plain "Editor-only"
  message on the `#else` side, so a standalone build still compiles —
  this was never meant to ship, purely a testing aid.
- **Known gap, not fixed here:** the handful of `IEquippable`-carrier
  items (Backpack, Canteen, Sunglasses, Nav Computer, Health Monitor,
  Mining Face Shield) don't have a real `worldPickupPrefab` of their own
  (their physical form is a dedicated prefab, not the generic
  `Pickup`-based path) — spawning one here falls back to the generic
  dropped-item prefab and adds a plain, non-equippable stack rather than
  a working item. Not a blocker for tomorrow's tool work (those are plain
  stackable items), but worth a follow-up if the Admin tab needs to cover
  gadgets too.

### v0.1.67-dev — Worn backpack contents move to a side column in the Inventory tab

Ben's ask: the Equipment section's rows got hard to scan whenever a
Backpack was worn on Back, since its full contents grid rendered inline
directly under that row, pushing every later slot (Left Arm, Right Arm,
...) down by an unpredictable amount.

- `DrawEquipmentSection()` now returns the currently-worn container
  (`IInventoryHolder`, today only ever a worn Backpack) instead of drawing
  its contents inline. `DrawContent()` lays the equipment list and that
  container's contents out side by side (`GUILayout.BeginHorizontal`) —
  the equipment column stays a uniform single-column list regardless of
  what's worn.
- The Back row's box now just reads **"Equipped"** instead of the item
  name — the actual contents are visible right next to it in the new
  column, so repeating "Backpack" there was redundant.
- Side effect: this also removes the tight-spacing hazard the 2026-08-03
  fix (`SafeButton`, left-click-only) was originally guarding against —
  the contents grid no longer sits close enough beneath Unequip/Drop to
  make a stray click land on the wrong one. `SafeButton` itself stays, no
  reason to relax it.

### v0.1.66-dev — Darker panel background for readability

`DebugGUI.DrawPanel`'s shared background alpha raised from 0.65 to 0.92 —
Ben flagged the new Tab/` menus reading as too washed-out against a bright
sky. Since every screen (Bank, Lockbox, Inventory, Skills, Crafting,
GameMenuScreen, PlayerMenuScreen, plus the bottom-left debug HUD) draws
through this one shared 1x1 texture, the fix applies everywhere at once.

## 2026-08-04

### v0.1.65-dev — Consolidated Inventory/Skills/Crafting into one Tab-key Player Menu

New `PlayerMenuScreen`, toggled with **Tab** — same full-screen tabbed
pattern as `GameMenuScreen` (` key), four tabs: Player (blank, same
placeholder treatment as the ` menu's Player tab), Inventory, Skills,
Crafting. Replaces the three independently-hotkeyed screens (I/U/O) that
existed before — each one's own Update()/isOpen/OnGUI/hotkey was stripped
out and its content turned into a `DrawContent()` method that
`PlayerMenuScreen` calls into for whichever tab is active, so the
underlying logic (fields, dependencies, popups) didn't need to move, only
its screen chrome.

- `InventoryScreen` also gained `DrawPopups()` (its screen-centered move/
  coin-drop popups, drawn after `PlayerMenuScreen` ends its own full-screen
  area, only while the Inventory tab is active) and `ResetPopups()` (called
  when the whole menu closes, so a still-open popup doesn't stay stuck open
  next time it's reopened).
- Dropped the v0.1.50-dev 50%-`GUI.matrix`-scale boost on the Inventory
  content — the Tab menu is already a full-screen area, much larger than
  the old floating window that scale was compensating for. Flagged in
  `TEST_FEATURE_PLAN.md` to re-check readability; easy to reintroduce
  scoped to just that tab if it reads too small in practice.
- `GameMenuScreen.ControlsList` updated per the standing rule: removed the
  now-gone I/U/O rows, added a `Tab` row.
- `FirstPersonController` now holds a single `playerMenuScreen` reference
  (in place of the old `inventoryScreen`/`skillsScreen`/`craftingScreen`
  fields) in its Escape-close list.

### v0.1.64-dev — Full-screen tabbed game menu (` key): Player/Audio/Graphics/Controls/Credits

New `GameMenuScreen`, toggled with `` ` `` (backtick/grave) — same open/close/
cursor-lock convention as every other screen (only opens while the cursor is
already locked, so it can't stack on top of Inventory/Crafting/Skills/Bank/
Lockbox), wired into `FirstPersonController`'s Escape-close list alongside
them. First tabbed-navigation UI in the project — five tabs drawn as buttons
across the top of a full-screen panel, switching which section renders below.

- **Player** — deliberately left blank per explicit instruction, reserved for
  a future decision on what belongs here (Vitals? Skills? something else?)
  rather than guessing and having to undo it. No `PlayerVitals`/`PlayerSkills`
  dependency on the component at all right now, consistent with not adding
  code for something not actually used yet.
- **Audio** / **Graphics** — both honest placeholders ("no system exists yet
  — nothing to configure") rather than fake sliders that wouldn't control
  anything real. Neither an audio system nor a graphics/quality-settings
  system exists anywhere in the project yet.
- **Controls** — a flat, alphabetized (by key name, not grouped by category)
  reference list of every real key binding in the game today: `` ` ``, C, E,
  Escape, F, I, Left Mouse Button, Left Shift, Mouse Movement, O, Right Mouse
  Button, Space, U, WASD, X, Z. Per the request, this list is meant to be kept
  current — update `GameMenuScreen.ControlsList` whenever a new key mapping
  is added anywhere in the game.
- **Credits** — "Tekim" and "the T-Rex," exactly as given, placeholder for now.

### v0.1.63-dev — Fix: Rock/Small Rock chunks bouncing/rolling too far after breaking

User report: chunks scattered way farther than intended after breaking a
Boulder. Two compounding causes:

- `MediumRockChunk.prefab`'s `Rigidbody` had near-default damping (linear `0`,
  angular `0.05`) — never actually set when the prefab was created earlier
  tonight, just left at Unity's defaults.
- `RockChunk.prefab`'s existing damping (`0.5`/`0.5`) was tuned for its
  original Cube shape, which settles almost instantly once a flat face
  touches the ground regardless of damping — a Sphere (what it was swapped to
  a few versions ago this session) rolls far more freely with much less
  resistance at the same values, so the same damping that looked fine on a
  cube now lets it roll for a long distance.

Raised damping on both (`RockChunk`: 1.5/2, `MediumRockChunk`: 2/3) so chunks
still scatter with a visible initial burst but settle down quickly afterward
instead of continuing to roll. Also normalized Boulder's `scatterForce` from
`1.4` down to `1.2`, matching every other `ResourceNode` in the scene (it was
the one outlier).

### v0.1.62-dev — Boulder + Rock (new stone tier), Small Rock's chunk shape fixed

Ben pointed out `RockChunk.prefab` has always been a plain scaled Cube — more
noticeable now that the texture actually looks like rock. Explored shape
options (primitive clustering, a noise-displaced mesh, or a hybrid) and went
with the hybrid: a real displaced-sphere mesh (per-vertex random radial
displacement, not a primitive) for the main irregular silhouette, plus several
small clustered pebble spheres scattered on its surface.

- **`RockChunk.prefab`** (Small Rock's chunk, and Rock Node's broken-piece
  visual — same prefab, both uses) swapped from a Cube mesh/`BoxCollider` to a
  Sphere mesh/`SphereCollider`. Same prefab guid, so every existing reference
  (`Rock.asset`'s `worldPickupPrefab`, Rock Node's `chunkPrefab`, the
  `hiddenChunkPrefab` fallback on the disguised Silver/Gold/Platinum ore
  nodes) stayed valid with no further wiring needed.
- **New `Rock`** (file `MediumRock.asset`, item name "Rock") — a pure
  intermediate stage, same as Small Rock already is: never used directly in a
  recipe. Its chunk (`MediumRockChunk.prefab`) is the new hybrid shape: a
  0.35-radius displaced-sphere body plus 4 small pebbles.
- **New `Boulder`** — a world object (not an item; nothing to pick up
  directly) using the same hybrid technique at a bigger scale (0.9-radius
  body, 8 pebbles), placed in `TestScene` at `(-4, 0.6, 4)`. Breaks via the
  existing `ResourceNode`/`IPunchable` mechanic — bare-handed, no tool
  required, same as Rock Node (2 hits, yields 3 Rock).

**Scope boundary, deliberately not built here:** this only fixes the shapes
and wires Boulder → Rock through the *existing* punch-to-break mechanic. It
does not implement "Rock breaks down further into Small Rock" — that
mechanism (a recipe? a separate mineable object?) was discussed in concept
back when the tier was named but never concretely decided beyond "Rock is a
pure intermediate stage," so nothing was invented here to fill that gap. Also
doesn't touch the separately-planned randomized-size-on-spawn/yield-scaling/
duration-scaling design from that same conversation — this is shapes only.

**Safety net applied again** (same reasoning as the Tree's branching mesh):
can't verify the displaced-sphere triangle winding visually from this headless
session, so `RockChunk.mat`'s `_Cull` was set to `Off` — harmless on the
existing plain-primitive uses (Rock Node, Small Rock) too. Verified the full
guid chain (`MediumRock.asset` ↔ `MediumRockChunk.prefab`, `RockChunk.prefab`'s
new mesh/collider, `Boulder`'s `ResourceNode` fields) directly rather than
trusting the generator's success log, plus a clean duplicate-fileID scan and a
clean batch-mode compile.

### v0.1.61-dev — Fix: all 5 ore textures rendered as solid color blobs, not flecked rock

User screenshots (in-game, without and with the Mining Face Shield equipped)
showed the v0.1.60-dev ore nodes as near-solid colored spheres — reddish-brown,
green — instead of grey rock with metal flecks, and Silver/Platinum appeared not
to reveal at all when the shield was equipped.

Root-caused by reading the actual generated PNGs directly rather than guessing
from the in-game screenshots alone (`CopperOreTexture.png` was, in fact, a nearly
flat solid green image). Two compounding problems, found via standalone test
swatches inspected before touching any real asset:

1. **The real bug:** `Mathf.SmoothStep(low, high, rawNoiseValue)` — used for
   every fleck-coverage mask — doesn't threshold anything the way GLSL's
   `smoothstep(edge0, edge1, x)` does; Unity's version treats its third argument
   as an already-normalized `[0,1]` progress value and the first two as the
   *output range*, not threshold edges. The call was silently remapping every
   pixel into a narrow output band uniformly, never producing sparse flecks
   regardless of what threshold values were tried — confirmed by testing three
   different threshold pairs that all looked nearly identical, which is what
   exposed the real bug rather than a tuning problem. New gotcha documented in
   `CLAUDE.md` with the correct GLSL-style replacement (`SmoothThreshold`).
2. Also darkened every rock-matrix color palette — contrast alone (tested first,
   before finding the SmoothStep bug) didn't fix it, since some fleck colors
   (Silver's near-white especially) were already close to the original "light"
   rock color even before any blending.

All 5 ore textures (`CopperOreTexture.png`, `IronOreTexture.png`,
`SilverOreTexture.png`, `GoldOreTexture.png`, `PlatinumOreTexture.png`)
regenerated in place with the corrected math — same file paths/guids as before,
so no material or scene changes were needed. Verified by reading each
regenerated PNG directly before considering it fixed, not just by re-running the
generator and trusting the log.

**Flagged, not yet fixed:** the sky texture's cloud coverage (v0.1.55–57-dev)
used the identical buggy pattern — very likely the real explanation for why
clouds stayed faint across three tuning rounds that session. Noted in
`BUGS_AND_ENHANCEMENTS.md`'s sky entry for whenever that gets revisited.

### v0.1.60-dev — Full ore ladder (Iron/Silver/Gold/Platinum) + Mining Face Shield

First real implementation slice out of tonight's planning doc — the hidden-ore
detection mechanic from the Crafting, Gathering & Skills Pipeline section, built
in full (visual reveal *and* yield gating, not just the visual half).

- **Iron, Silver, Gold, and Platinum Ore Nodes** added (Copper already existed),
  each with its own procedurally generated texture (same tileable-noise technique
  as grass/sky/rock — a shared `GenerateOreTexture` helper this time, just
  different color palettes per metal) and its own chunk prefab/item, mirroring
  `CopperOreChunk.prefab`'s structure exactly. Placed in `TestScene` near the
  existing Copper Ore Node.
- **Iron stays visible**, same as Copper. **Silver, Gold, and Platinum are
  hidden** — they render as plain `RockChunk.mat` (indistinguishable from an
  ordinary Rock Node) until the player has a **Mining Face Shield** equipped, at
  which point `ResourceNode` swaps their material to the metal's true texture.
  This is the *exact* reveal mechanism already shipped for Sunglasses + the
  Secret Message Wall, generalized from a pure visual effect into one with a real
  gameplay consequence.
- **Yield gating, not just visual:** `ResourceNode` checks whether the node is
  revealed *at the moment it actually breaks* (not when punching started) —
  mining a hidden node without the shield yields `hiddenChunkPrefab`
  (`RockChunk.prefab`, i.e. plain Small Rock, the ore undetected and lost);
  with the shield on, it yields the real ore. New `ResourceNode` fields:
  `hiddenMaterial`, `revealedMaterial`, `hiddenChunkPrefab` — all null by default,
  so every previously-shipped node (Rock Node, Copper Ore, Tree) is completely
  unaffected; only a node that explicitly sets all three opts into this behavior.
- **New `MiningFaceShield`/`PlayerMiningFaceShield`** — structured identically to
  `Sunglasses.cs`/`PlayerSunglasses.cs` (single Face-slot equippable, same
  pickup/equip/unequip/drop chain, same `WornEquipment`-layer-while-worn fix from
  the `CLAUDE.md` equippable checklist), minus the screen-tint overlay — its
  effect is read externally via a new `IsWorn` accessor instead of drawn by the
  component itself. Wired into `InventoryScreen` as a sixth equippable type,
  following the existing Backpack/Canteen/NavComputer/HealthMonitor/Sunglasses
  pattern exactly (both `DrawInventorySection` and `DrawEquipmentSection`).
  Craftable (2 Small Rock + 1 Stick, trains Crafting), and one is placed in
  `TestScene` as a world pickup near the other wearable gadgets.

**Applied a lesson from earlier tonight's stale-reference bug directly:** every
asset-creation step in this run's generator script returns only a path (a plain
string, immune to Unity's object-reference staleness), never an object reference
— the final scene-wiring step opens the scene once and re-fetches *everything*
fresh via `AssetDatabase.LoadAssetAtPath` right there, rather than trusting
anything carried in from earlier in the script. Verified every single new/changed
guid reference directly against its target's `.meta` guid (item↔chunk-prefab both
directions for all 4 new ore types, hidden/revealed material and hidden-chunk
references on all 3 disguised nodes, the shield-item reference on
`PlayerMiningFaceShield`, and the full `PlayerCrafting.recipes` array for stray
nulls) before trusting the script's own success log — none were stale this time.
Also a clean duplicate-fileID scan on the resaved scene and a clean batch-mode
compile.

### Design planning: Mining skill decided, ore byproducts, hidden-ore detection (docs only)

Follow-up planning pass on the pipeline written up earlier tonight. Resolved the
previously-deferred `Mining` skill split from `Gathering`: Mining now owns all
ore-node gathering specifically (Gathering stays scoped to Sticks/Berries/plain
Rock). Added three new pieces to the Metal line in
`docs/design-brief.md`'s Crafting, Gathering & Skills Pipeline section: ore nodes
yield Small Rock alongside their primary ore (mining a vein realistically kicks
loose waste rock too); base ore yield scales down Copper→Platinum so the ladder
has real teeth; and a new **Mining Face Shield** (Face-slot equippable) reveals
Silver/Gold/Platinum nodes that otherwise look like plain rock — same reveal
mechanism already shipped for Sunglasses + the Secret Message Wall, generalized
into a real gameplay system, with a Mining-skill-tier-4 bypass once a player
doesn't need the gear anymore. Updated the `BUGS_AND_ENHANCEMENTS.md` pointer
entry to match. Still docs-only — nothing implemented, no version bump.

### Design planning: full crafting/gathering/skills pipeline (docs only, nothing built)

Extended planning conversation (not a build session) working out the "still open"
gap flagged when the five `CraftTier` names were first decided: what actually
determines an item's tier. Landed on a weakest-link rule (the lower of current
skill level and material quality), then kept expanding — a full gather → refine →
assemble material web across wood, stone, metal, and textiles; 6 new skills
(Woodworking, Stonework, Metalworking, Forging, Minting, Sewing); tool-quality
effects (yield/quality/speed); and a new click-once-and-locked interaction model
intended to replace the current punch-to-break mechanic entirely.

Written up in full in `docs/design-brief.md`'s new **Crafting, Gathering & Skills
Pipeline** section, with a pointer entry added to `BUGS_AND_ENHANCEMENTS.md`.
**Decided in shape, not in exact numbers** — several sub-questions are explicitly
still open (see that section). Nothing in this plan is implemented yet; no game
code, scenes, or prefabs changed in this entry, so no version bump.

### v0.1.59-dev — Rock texture, Copper Ore, and tool-gated gathering (Pickaxe/Axe)

Three-part request: give the rocks a real texture instead of flat grey, add a
Copper Ore resource, and add a couple of craftable tools. Design decisions
confirmed up front rather than assumed: tools (Pickaxe + Axe) actually gate
gathering — a Pickaxe must be held in a hand to mine Copper Ore, an Axe to chop
Trees — and Copper Ore is gathered the same punch-to-break way Rock Node
already works.

- **Rock texture.** Same tileable-noise technique as the grass/sky textures
  (`CHANGELOG.md` v0.1.53-dev onward) — a mottled grey stone texture applied to
  `RockChunk.mat`'s `_BaseMap`, which is shared by every loose Small Rock pickup
  *and* every chunk scattered from breaking Rock Node (they were already the same
  prefab). Also fixed a design inconsistency found along the way: Rock Node's own
  sphere had its own separate **embedded scene material** (created directly via
  `new Material(...)`, serialized inline into `TestScene.unity` rather than as a
  project asset) with no texture — repointed it to the same `RockChunk.mat` asset
  so the whole node and its broken chunks now visibly match.
- **`ResourceNode` gained an optional `requiredTool` (`ItemDefinition`) field.**
  Null (default) means punch bare-handed works, exactly Rock Node's existing
  behavior — nothing about it changed. When set, `OnPunch` checks
  `PlayerEquipment.HasInHand(requiredTool)` (new method — true only if the item is
  actually held in a hand right now, not just carried in inventory/a backpack)
  before registering the hit at all. `Prompt` also changes to `"Punch to break
  (requires X)"` when a tool is required, so the requirement is visible before
  ever swinging.
- **Copper Ore** — new `ItemDefinition`, a mottled-rock texture with scattered
  copper-orange flecks and rare green patina spots (same layered-noise approach,
  new color mapping), a `CopperOreChunk` prefab (mirrors `RockChunk.prefab`:
  scaled Cube, `Rigidbody` `ContinuousDynamic`, `Pickup`), and a new "Copper Ore
  Node" placed in `TestScene` at `(2, 0.4, -4)` — `ResourceNode` with
  `hitsToBreak: 2` (tougher than Rock Node's 1) and `requiredTool` set to
  Pickaxe.
- **Pickaxe and Axe** — plain, non-equippable `ItemDefinition`s (`maxStack: 1`,
  no custom `worldPickupPrefab` — falls back to the generic dropped-item cube,
  same deliberate choice Rock Hammer already made) craftable via two new
  recipes: Pickaxe (2 Small Rock + 1 Stick), Axe (1 Small Rock + 2 Stick), both
  training Crafting +2, added to `PlayerCrafting.recipes` on the Player.
- **Trees are now harvestable.** `Tree.prefab` gained a `ResourceNode` component
  directly on its trunk root (reuses the exact same hide/respawn logic Rock Node
  already has — `GetComponentsInChildren<Renderer>()` already correctly sweeps up
  the foliage children too, no changes needed there): `hitsToBreak: 4`,
  `requiredTool` set to Axe, yields a new **Wood** item via a new `WoodChunk`
  prefab. Previously the tree prefab (v0.1.58-dev) was purely decorative with no
  way to interact with it at all.

**A real bug found and fixed during this work, worth its own note — see the new
"asset references can go stale across `LoadPrefabContents`/`UnloadPrefabContents`"
gotcha in `CLAUDE.md`:** the first version of the generation script silently wrote
`requiredTool: {fileID: 0}` on the Copper Ore Node and failed to add the two new
recipes to `PlayerCrafting.recipes` at all — no exception, no compile error, the
script logged success. Root cause: those specific references were created earlier
in the script and used again *after* an unrelated `PrefabUtility.LoadPrefabContents`/
`UnloadPrefabContents` cycle (adding the `ResourceNode` to `Tree.prefab`), which
appears able to silently invalidate some in-memory asset references — a new,
not-fully-understood sibling to the already-documented `OpenScene` staleness
gotcha. Caught only by directly grepping the saved scene YAML for the expected
guids rather than trusting the script's own success log, and fixed with two small
follow-up scripts that re-fetched the references fresh via `AssetDatabase.LoadAssetAtPath`
immediately before use.

Verified end-to-end: every new/changed guid reference cross-checked directly against
its target asset's `.meta` guid (not just assumed from the script's intent), a
duplicate-fileID scan on the twice-resaved scene (clean), and a final clean
batch-mode compile.

### v0.1.58-dev — Procedural branching tree, real mesh geometry (not primitive composition)

Asked whether tree models could be procedurally generated; offered a choice
between combining stock primitives (Backpack.prefab's existing technique) or
actually generating trunk/branch geometry in code — went with the latter for
a more organic, less "blocky" result.

`GenerateTree.cs` (throwaway, run via batch mode then deleted) builds a tree
via recursive branching: starting from a single trunk segment, each branch
splits into 2–3 children at a random angle within a 32° cone of its parent's
direction (with a slight upward bias so branches don't droop after several
recursive levels), shrinking in length/radius each generation, 4 levels deep
(66 segments total this run — the exact shape is seeded, so it's
reproducible, not different every time the script runs). Each segment is a
tapered-cylinder (hexagonal cross-section, 6 sides) built from real vertex/
triangle data via a from-scratch `AddCylinderSegment` — not `CreatePrimitive`
— all combined into a single `Mesh` asset (`Assets/Data/TreeTrunkMesh.asset`).
Foliage stays simple: 2–3 scaled Sphere primitives clustered at each of the 41
terminal branch tips, colliders removed from the foliage spheres so they
don't block movement/interaction the way the trunk does.

**Risk mitigation, not guesswork:** this session can't render or screenshot
locally, so there was no way to visually confirm the hand-written cylinder
triangle winding order was actually correct — getting it backwards would make
the trunk invisible from outside (only visible from inside, due to backface
culling) with no compile error to catch it. Rather than gamble on it, verified
`_Cull` is a real property on this project's URP/Lit materials first (grepped
an existing `.mat` file), then set it to `Off` on `TreeBark.mat` — the trunk
renders regardless of which way the winding turned out, at the cost of
trivial double-sided overdraw on a low-poly mesh.

New `Assets/Prefabs/Tree.prefab` (mesh + bark material + non-convex
`MeshCollider`, matching `Ground`'s static-collider pattern — no `Rigidbody`
involved) with 4 instances placed in `TestScene`, each with randomized
Y-rotation and a small scale variance (0.85×–1.25×) so four copies of the
same mesh don't look identical, scattered clear of the existing object
cluster near spawn and the Secret Wall. Verified end-to-end: asset files on
disk, exactly 4 real `PrefabInstance` roots linked to `Tree.prefab`'s guid in
the scene (not the much larger raw match count from per-property
modification entries), and a clean duplicate-fileID scan on the resaved
scene.

**Still needs an in-Editor look** — same limitation as the sky work: can't
confirm visually from here whether the branching silhouette actually reads as
tree-like, whether the culling safety net was even necessary, or whether 4
levels/66 segments is too sparse or too dense at actual in-game scale.

### v0.1.57-dev — Fix: sky gradient direction was inverted, clouds still not reading as shapes

Second round of user-screenshot feedback on the sky. v0.1.56-dev's gradient
rendered backwards from intent — a deep blue band sat right at the horizon,
fading to pale going *up* — the opposite of both the code's intent (`Horizon`
pale, `Zenith` deep, blended by an assumed v=0-at-nadir/v=1-at-zenith mapping)
and of real atmospheric haze (pale near the horizon from more scattering,
deeper blue overhead). Strong, clean evidence `Skybox/Panoramic`'s actual
v-axis runs opposite to what was assumed. Rather than keep guessing the exact
convention, flipped `vEff = 1 - v` and used it everywhere instead of raw `v` —
corrects the observed symptom regardless of the precise underlying cause. This
also explains why clouds stayed barely visible in v0.1.56-dev: the cloud band
(meant to fade in right at the horizon) was very likely landing near the true
zenith instead — exactly where a level-pitched camera never looks.

Also sharpened the cloud shapes themselves: the one cloud visible in the
previous screenshot was a soft blurry brightening, not a distinct shape.
Narrowed the coverage threshold (0.46–0.58, was 0.42–0.62) for crisper edges,
and weighted the coarsest noise octave more heavily (0.65/0.25/0.10, was
0.55/0.30/0.15) for bigger, more clearly cloud-shaped blobs instead of fine
speckle blurring the outline.

`SkyTexture.png` regenerated in place again — same file path/guid, `Sky.mat`
and `TestScene`'s skybox reference needed no changes, reverified via the guid
chain.

### v0.1.56-dev — Fix: sky clouds barely visible from a normal camera angle

User screenshot from a roughly level-pitched first-person view showed the
v0.1.55-dev sky as an almost flat pale wash — no visible cloud shapes, no
visible horizon-to-zenith blue gradient either, just faint streaks. Not a
shader-compatibility problem (no pink, confirming the `Skybox/Panoramic`
choice was fine) — a content-tuning problem in the generated texture itself.
Two likely causes, both addressed (can't render/screenshot locally to isolate
which dominated):

- **Low color contrast.** `Horizon` (0.75, 0.85, 0.95) and `CloudColor`
  (0.97, 0.97, 1) were close enough to blend into each other rather than read
  as distinct shapes. Made `Horizon`/`Zenith` more saturated blues and
  `CloudColor` pure white.
- **Narrow visible band, coarse noise.** A level-pitched camera most likely
  only ever sees a narrow slice of the texture's v-range near the horizon
  (v≈0.5) — the old noise's coarsest octaves (period 5/10/20 across the
  *entire* pole-to-pole 0–1 range) put very little variation inside any
  narrow slice that close to a single value, so that band looked almost
  uniform regardless of contrast. Doubled every octave's period (10/20/40)
  so a narrow near-horizon slice still crosses enough lattice cells to show
  real variation. Also moved the cloud band's fade-in from starting at
  v=0.35 to v=0.45 (clouds now reach full strength right at the horizon,
  where a level camera actually looks) and lowered/widened the coverage
  threshold for denser, easier-to-spot clouds.

`SkyTexture.png` regenerated in place (`AssetDatabase.ImportAsset` reimport,
same file path/guid) — `Sky.mat`'s `_MainTex` reference and `TestScene`'s
`RenderSettings.skybox` needed no changes, verified by re-checking the guid
chain after regeneration.

### v0.1.55-dev — Procedural cloudy sky, replacing the built-in default skybox

Same technique and same request as the grass ground texture (v0.1.53/54-dev),
applied to the sky. `TestScene`'s `RenderSettings.m_SkyboxMaterial` was pointing at
Unity's built-in `Default-Skybox` (a `Skybox/Procedural` material, referenced via
its all-zero-except-`f` built-in-resource guid) — the plain blue gradient visible
in every prior screenshot, no clouds.

Before writing any code that sets shader properties, ran a throwaway inspection
script logging `Skybox/Panoramic`'s actual properties/defaults via `ShaderUtil`
rather than assuming names from memory — this project has hit "guessed shader
property, silently no-op'd or rendered pink" before (see `CLAUDE.md`'s URP gotcha
notes), and this shader turned out to have both a `_Mapping` and a separate
`_Layout` float property that aren't obviously distinguishable without checking.
Confirmed `_MainTex` is the texture slot, and `_Mapping`/`_ImageType` already
default to exactly what a standard equirectangular panorama needs (Latitude-
Longitude / 360 degrees) — so only `_MainTex` needed setting; `_Tint`/`_Exposure`/
`_Rotation` stay at their neutral shader defaults.

`GenerateSkyTexture.cs` (throwaway, run via batch mode then deleted) generates a
2048×1024 equirectangular texture: a `Horizon`→`Zenith` blue vertical gradient,
plus scattered white clouds from the same tileable value-noise function as the
grass texture — except only wrapped horizontally (`LatticeValue` wraps the U/
longitude coordinate into a period before hashing, same seamless-by-construction
trick, but leaves V/latitude unwrapped since top and bottom are poles, never
adjacent to each other and never need to tile). Cloud coverage is thresholded
(`SmoothStep`) rather than a smooth haze, so it reads as scattered clouds against
clear sky rather than uniform overcast, and fades out near the exact zenith and
below the horizon so clouds don't cap the sky or dip into ground-level view.

Created `Assets/Data/Sky.mat` (new `Skybox/Panoramic` material) and repointed
`TestScene`'s `RenderSettings.skybox` at it via `EditorSceneManager`/
`RenderSettings.skybox` in the script, rather than hand-editing the scene YAML —
verified afterward by cross-checking guids end-to-end (scene → `Sky.mat` →
`SkyTexture.png`) and a duplicate-fileID scan on the resaved scene (clean).

### v0.1.54-dev — Fix visible tiling grid in the grass texture with genuinely seamless noise

User feedback with a screenshot: v0.1.53-dev's grass read as an obvious repeating
checkerboard/waffle grid in play, not natural grass — worse than the "faint seams"
limitation that entry called out. Root cause was two-fold: `Mathf.PerlinNoise` gives
no periodicity guarantee at arbitrary frequencies, so every one of the 1,600 tile
repeats (40×40) had a visible seam *and* showed the exact same low-frequency blob
shape, which is what the eye actually locks onto as "a grid" — the seam alone
wasn't the main problem.

Rewrote the generator with a custom tileable value-noise function: a
`LatticeValue(x, y, period, seed)` hash that wraps `x`/`y` into `period` *before*
hashing, so sampling one full period to the right/down lands on the identical
wrapped lattice point — adjacent copies of the texture flow together with zero
seam by construction, not approximation. `TileableNoise` smoothstep-interpolates
between four such lattice corners. Layered three octaves (periods 5/10/20) for the
large mottled patches, same color gradient as before, plus one more (period 60)
for the fine blade-detail brightness variation.

Also reduced the material's UV tiling from 40×40 to 20×20 and doubled the source
texture to 1024×1024 — fixing the seam alone still leaves the exact same tile
repeating identically at every step, and cutting the repeat count in half reduces
how many chances the eye gets to pattern-match that repetition, independent of the
seamless fix. `Ground.mat`'s `_BaseMap` guid is unchanged (same file path,
re-imported in place), only `m_Scale` and the PNG's pixel content changed.

### v0.1.53-dev — Procedural grass texture on the Ground, replacing the flat green color

First texture-image asset in the project — everything until now was a flat
`_BaseColor` on a primitive. `Ground.mat` (`Universal Render Pipeline/Lit`) had no
`_BaseMap` assigned at all, just a solid green color; asked how to get a "realistic
grass" look, offered three routes (procedural in-engine, a supplied/downloaded
texture, or just explaining the steps) — went with the procedural route.

Throwaway `Assets/Editor/GenerateGrassTexture.cs` (run via batch mode, then
deleted, per the project's established workflow) generates a 512×512
`Texture2D`: three-octave `Mathf.PerlinNoise` for large mottled dark/mid/light
green patches (blended through a `DarkGreen`→`MidGreen`→`LightGreen` gradient),
plus a higher-frequency noise layer multiplied in as brightness variation to fake
individual-blade detail on top of the smooth patches. Noise sampling is offset
away from the origin — `Mathf.PerlinNoise` always returns exactly 0.5 at integer
coordinates, which otherwise shows up as a visible low-frequency grid artifact.

Saved as `Assets/Textures/GrassTexture.png`, imported with `WrapMode.Repeat` +
mipmaps + Bilinear filtering, then wired onto `Ground.mat`'s `_BaseMap` with
`m_Scale` (tiling) set to `(40, 40)` — `Ground` is a Plane scaled `(10, 1, 10)`
(100×100 world units), so 40 repeats puts each tile at 2.5 units, close enough
for the blade-detail noise to actually read at ground level without the pattern
looking like an obvious repeating grid from a distance. `_BaseColor` reset to
white so it no longer multiplies against (and darkens) the texture's real colors.

**Known limitation:** the noise isn't seamless at the texture's own edges (no
wrap-around blending was added), so at a tiling factor this high, faint repeat
seams may be visible up close on flat, uninterrupted ground. Good enough for a
first pass; a proper tileable-noise version (or a real photo-sourced texture)
would be the next step if the seams read as distracting in actual play.

### v0.1.52-dev — Fix: version number clipped off the bottom-left debug panel

The panel's `Rect` (height 56, positioned `Screen.height - 66`) was sized for 2
label lines back when it only showed Speed/Sprinting and the version. The Stance
line was added later (stance-system work, this session's merge) without resizing
the panel, so with 3 lines the bottom one — the version number — overflowed
`GUILayout.BeginArea`'s bounds and got clipped. Resized to fit 3 lines (height 76,
`Screen.height - 86`), keeping the same 10px margin on every edge.

### v0.1.51-dev — Canteen fill dead zone, and a misclick that dropped/unequipped the backpack instead of the item inside it

Third playtest pass on the water-source/inventory work above.

- **Standing close enough to see the Fill prompt didn't mean close enough to
  actually fill.** `Canteen.fillRange` (2m, measured from the canteen) was smaller
  than `PlayerInteraction.interactRange` (3m, measured from the camera) — the F/E
  prompt could be visible while `HasNearbyWaterSource()` still failed, silently, via
  both the direct F-key interaction and the pre-existing UI Fill button. Raised
  `fillRange` to 4m so it always exceeds `interactRange` with headroom. Same Unity
  serialization gotcha as the overdrink threshold: `TestScene.unity`'s Canteen
  instance had `fillRange: 2` baked in from before the new default existed, so the
  scene value needed its own fix alongside the code default.
- **Clicking an item inside a worn backpack sometimes dropped or unequipped the
  backpack instead.** Two independent reports (a Canteen click dropped it, a Rock
  click unequipped it into the main inventory) — root cause was layout, not logic:
  `DrawEquipmentSection`'s Back-slot row (Label + backpack box + Unequip/Drop
  buttons) sits directly above `DrawContainerContents`' item grid with almost no
  vertical gap, and the grid's `GUILayout.Space(20)` indent doesn't line up with the
  row above it — a middle slot in the grid (confirmed: slot 3) can horizontally
  align under the Unequip/Drop button column. Combined with Unity's `GUILayout.Button`
  responding to *any* mouse button (not just left-click, a long-standing IMGUI
  quirk), a right-click aimed at an item could land on the backpack's own
  Unequip/Drop instead. Fixed two ways: added a 6px gap between the row and the
  grid (reduced frequency but didn't eliminate it — confirms the diagnosis), and,
  more robustly, added an `InventoryScreen.SafeButton` helper that requires an
  actual left-click, applied to every Equip/Unequip/Drop button in the screen. A
  stray right-click can no longer trigger any of them regardless of exact pixel
  alignment.
- **`PlayerDropping.DropFrom` was still the one unguarded generic-removal path.**
  Flagged in `CLAUDE.md`'s gotcha note earlier this session as a latent risk (every
  current call site happened to guard it correctly, but the function itself had no
  check of its own). It's what the move popup's "Drop" option calls — now checks for
  an `equipment` reference first and releases the real object via `SetCarried(false,
  null)` instead of stripping the reference and spawning a fake pickup, matching the
  `InventoryTransfer.Move` fix from the previous entry.

### v0.1.50-dev — Inventory window scaled 50% larger for readability

Same request and same fix as the Bank window in v0.1.49-dev: scaled the whole
`GUI.matrix` by 1.5x around screen center in `InventoryScreen.OnGUI`, covering
the panel, the scroll view, and both popups (move destination, coin drop)
automatically since they draw later in the same `OnGUI` call.

One wrinkle Bank didn't have: `InventoryScreen`'s panel height was already
screen-responsive (`Mathf.Min(Screen.height - 40f, 700f)`) to avoid overflowing
shorter displays. Left unadjusted, scaling that already-capped height by another
1.5x could push the panel off the top/bottom of a smaller window. Divided the
on-screen height budget by `UiScale` before applying the existing cap
(`Mathf.Min((Screen.height - 40f) / UiScale, 700f)`) so the *post-scale* result
still respects the original margin instead of the pre-scale one.

### v0.1.49-dev — Bank window scaled 50% larger for readability

User feedback: the Bank window was hard to read. `GUILayout` uses fixed pixel
widths throughout (`GUILayout.Width(90)` etc.), so just growing `BankScreen`'s
outer panel `Rect` would only have added empty padding around the same small
text and buttons — not actually fixed the readability complaint. Instead scaled
the whole `GUI.matrix` by 1.5x around the screen center at the top of `OnGUI`
(restored at the end), which grows the panel, its text, its buttons, and both
popups (Deposit/Withdraw, Exchange — drawn later in the same `OnGUI` call, so
the scale already applies to them too) proportionally together, all still
centered on screen. `LockboxScreen` wasn't touched — this request was scoped to
the Bank window specifically.

### v0.1.48-dev — Dropped items despawn after 15 minutes

First slice of the item-holding-redesign backlog entry: just the despawn timer, not
the pickup-priority/unequip-fallback rework (still open, needs its own pass).

`Pickup` gained a `despawnAt` countdown (15 minutes, `DespawnDelay`) started inside
`Configure(item, quantity)` — which turns out to be called from exactly one place,
`PlayerDropping.DropFrom`, so it fires for every item the player actually drops
(manual Drop button, and the hand-eviction fallback `PlayerLoot` uses when both
hands are full with no backpack equipped) without needing a separate flag to
distinguish "dropped" from "world-placed" pickups. World-placed pickups (Sticks,
Berry Bush) and `ResourceNode`'s scattered chunks never call `Configure`, so they're
unaffected — they keep whatever `canRespawn` behavior they already had. Deliberately
a distinct timer from `canRespawn`/`respawnDelay` (3 minutes) — that one restores a
resource point in place; this one deletes a dropped item outright once nobody's
picked it up.

**Scope note:** doesn't cover the five equippables (Backpack/Canteen/Sunglasses/
NavigationComputer/PersonalHealthMonitor) — their `Drop()` methods detach an
already-existing physical object rather than instantiating a new `Pickup`, so they
don't go through `Configure` at all. Revisit once/if the equipped-item unequip-
fallback drop path (still unbuilt) needs the same timer.

### v0.1.47-dev — Fix: bank/lockbox popups let the coin type switch mid-transaction

Reported by Ben (filed in `BUGS_AND_ENHANCEMENTS.md`, commit `08d3c89`). In both
`BankScreen.cs` and `LockboxScreen.cs`, the coin-type table underneath a
Deposit/Withdraw (or Exchange) popup stayed fully clickable while the popup was open
— a click that landed on the table instead of the popup silently reassigned
`pendingType`/`pendingExchangeFrom` and reset the pending amount back to 0, so a
withdrawal could switch to a different coin type mid-flow without the player
intending it. Fixed by disabling (`GUI.enabled = false`) every background button on
the panel — coin table, Exchange buttons, Lockbox Buy row, Close — for the duration
any popup is open, consistent with the modal role those popups already play.

### v0.1.46-dev — Second playtest pass: canteen still white, overdrink threshold wrong, Sunglasses orphaned, direct water-source interaction

Merged with Ben's parallel session (v0.1.36-dev through v0.1.44-dev below), which had
already claimed the v0.1.34-dev/v0.1.35-dev numbers for unrelated work before either
session saw the other's commits — this entry and the one below were renumbered up
from their original local v0.1.35-dev/v0.1.34-dev to land after that chain instead of
colliding with it.

The v0.1.45-dev fixes below (originally v0.1.34-dev locally) didn't fully hold up
under a second round of testing —
three of the four "fixed" items had a real bug still hiding underneath, plus one
brand-new feature request.

- **Canteen full but still showing white.** Root cause was never the material/color
  logic added in v0.1.45-dev — it was that `Canteen.cs` looked up its `Renderer` with
  a plain `GetComponent<Renderer>()`, but the prefab's actual mesh renderers live on
  child objects ("Body"/"Cap"), not the root the script sits on. `rend` was `null`
  the entire time, so `UpdateVisuals()` was silently a no-op regardless of fill
  state. Switched to `GetComponentsInChildren<Renderer>()` and apply the tint to all
  of them. Also found the project uses URP, and `Canteen.mat` is a URP/Lit material —
  `Material.color` only reliably touches `_Color`, which URP/Lit doesn't render from
  (`_BaseColor` does); added a `SetTint`/`GetTint` helper that sets both so the color
  change is guaranteed to actually render regardless of shader.
- **Overdrink sickness threshold was wrong.** Implemented in v0.1.45-dev as ">100%
  thirst triggers sickness", but the actual intended design (confirmed by user) is
  "125% is the safe ceiling, sickness triggers only above it." Moved
  `overdrinkSicknessThreshold` from 100 to 125, and raised the `Restore(Thirst)` cap
  from 125 to 150 — without that headroom, thirst could never actually exceed 125
  through drinking and the sickness threshold could never trigger at all. Also found
  the threshold change alone wasn't enough: `TestScene.unity` had
  `overdrinkSicknessThreshold: 100` serialized directly onto the Player's
  `PlayerVitals` component from before this field existed at its new default — Unity
  doesn't retroactively apply a changed C# default to an already-serialized value, so
  the scene was silently overriding the code back to 100. Fixed the scene value
  directly.
- **Sickness could reduce health to 0 with no warning and no actual recovery.**
  Thirst was only draining at the slow ambient rate (~0.14/s) while sick, but
  sickness damage runs at 5 health/s — health hits 0 in 20s, long before thirst could
  ever drain down to the 50% recovery line. Added `overdrinkThirstRecoveryPerSecond`
  (10/s, vomiting/sweating out the excess) so sickness is now actually self-limiting
  and recoverable, and added a "SICK: Overdrank water!" warning to the Vitals HUD
  (`PlayerHealthMonitor`) via a new bold-red `DebugGUI.Warning` style — previously
  there was no UI indication sickness was even happening.
- **Sunglasses moved from a backpack to a hand became permanently unequippable.**
  Same root cause as the Canteen orphaning bug from v0.1.45-dev, still present
  despite that entry's changelog description — see the `CLAUDE.md` gotcha section for
  the full story of why the earlier fix didn't actually ship. `InventoryTransfer.Move`
  now detects an `equipment`-backed slot and preserves the reference across the move
  instead of stripping it.
- **New: interact directly with a water source, no inventory screen needed.** Added
  `ISecondaryInteractable` — a small additive extension to the existing single-key
  (E) interact system that lets an object also offer a second action bound to F,
  shown in the prompt alongside the first (e.g. `[E] Drink    [F] Fill Canteen`) only
  when there's actually a second option available. `WaterSource` now implements both:
  E always offers Drink (works with no carrier equipped); F offers Fill and only
  appears when the player has a water carrier equipped that isn't already full.
  `PlayerCanteen` needed an `Equipped` accessor added for this — unlike
  Sunglasses/PersonalHealthMonitor it never had one, since a canteen has no dedicated
  "worn" slot (holding it in a hand or at the waist is what equipped means for it).

### v0.1.45-dev — Fix bugs from playtesting: canteen visuals, fills from anywhere, overdrinking, equipment transfer

Five bugs found during canteen/backpack testing, rooted in several underlying issues:

- **Canteen visual feedback missing when dropped.** Canteen didn't change appearance
  based on liquid state — added material swapping (blue when filled, gray when empty)
  via `UpdateVisuals()` called after Fill/Drink. Materials are asset references with
  fallback to null if not assigned.
- **Canteen fills from anywhere on the map.** No location check existed — added
  `IWaterSource` interface and `WaterSource` component so only world objects marked as
  water sources allow filling. Canteen.Fill now checks `HasNearbyWaterSource()` within
  `fillRange` (2m default) before allowing a fill. Created `WaterSource.cs` as a simple
  marker component for placement in the scene.
- **Overdrinking mechanic not implemented.** Player could drink past 100% thirst with
  no consequence. `PlayerVitals` now allows Thirst to be restored up to 125% (changed
  `Mathf.Min(100f, ...)` to `Mathf.Min(125f, ...)` in Restore). When thirst exceeds
  `overdrinkSicknessThreshold` (100), player takes `overdrinkSicknessDamagePerSecond`
  (5 default) health damage. Sickness clears once thirst drops back to
  `overdrinkRecoveryThreshold` (50). Stored `isOverdrunkSick` state to gate the logic.
- **Moving items from a backpack/container orphans held equippables.** Root cause:
  `InventoryTransfer.Move` uses generic `RemoveItem`/`AddItem` which strip equipment
  references, leaving the real Canteen/Backpack physically attached but with no
  inventory slot referencing it (and no Fill/Drink/Equip buttons). This was a known
  gotcha already documented in `CLAUDE.md`. Added guard at the top of `Move()`: if any
  slot holding the item has `equipment != null`, refuse to move it (return false).
  Equipment-type items must route through type-specific handlers
  (PlayerCanteen.Equip/Unequip/Drop, PlayerBackpack.Equip/Unequip/Drop) instead.
- **Right-click-to-drop stick in backpack removes backpack.** Likely same root cause as
  the orphaning bug above — when the move popup tried to shift the item via the generic
  path, equipment references were stripped. The guard added to `InventoryTransfer.Move`
  should now prevent this by refusing the move entirely.

### v0.1.44-dev — Five crafting-quality tiers decided; purchasable coin Lockboxes
Updated `docs/design-brief.md`'s Phase 1 wishlist with the five decided
crafting-quality tier names — **Crude, Rudimentary, (no adjective —
Normal), Fine, Masterwork** — superseding `game-overview.md`'s
never-reconciled "Crude/Standard/Mastery" three-tier mention. New
`CraftTier` enum + `CraftTierNames` (the display-name prefix helper —
Normal gets none) + `CraftTierScale` (suggested capacity/price modifiers:
0.2×/0.5×/1×/2×/5×, chosen so every tier's numbers come out a clean whole
number off the Normal baseline).

New `Lockbox` — personal coin storage, purchasable from the bank in any of
the five tiers. Normal holds 2,500 of each coin type for 10 Gold; the
other four tiers scale both capacity and price by the same
`CraftTierScale` modifier (Crude: 500/2g, Rudimentary: 1,250/5g, Fine:
5,000/20g, Masterwork: 12,500/50g). Unlike `PlayerBank`, each Lockbox is
its own world object with its own balances — buying two doesn't pool
their capacity.

New `LockboxScreen` (E to open a specific Lockbox, no hotkey — same
reasoning as the bank) shows wallet vs. that box's balance per coin type
with Deposit/Withdraw. Deposit is capped by the box's remaining capacity
for that type; Withdraw is capped by both what the box holds *and* what
the wallet has room for (`PlayerCurrency.MaxBalance`) — pulling 1,000 Gold
isn't possible if the wallet can't hold that much even if the box does.
Neither direction charges the bank's 3% fee — purchasing a Lockbox isn't
one of the fee-bearing deposit/withdraw/exchange transactions, and moving
coins into your own already-purchased box is closer to personal storage
(like a `StorageBox`) than a bank transaction.

`BankScreen` gained a Lockbox shop section — Buy per tier, greyed out
below the Gold price — and now takes the `BankBox` it was opened from so
a purchased Lockbox spawns 2m in front of *that* box rather than the
player.

### v0.1.43-dev — Global bank: deposit, withdraw, exchange (Phase 3 commerce, early)
New `PlayerBank` — a global account (no per-branch ledger; any `BankBox`
reads/writes the same balances) separate from `PlayerCurrency`'s carried
wallet, with no cap unlike the wallet's 250. Clarified the exchange ladder
with the user before building it: a clean ascending 10:1 chain
(Copper→Iron→Silver→Gold→Platinum, matching both the design brief and the
`CoinType` enum order) — what was actually typed in the request would have
made Copper worth the same as Silver.

**Fee model:** every Deposit/Withdraw/Exchange charges `max(1, ceil(3% of
amount))`, but as an *extra* cost on the source side rather than skimmed
off the transferred total — depositing 100 costs 103 from the wallet and
the bank receives exactly 100, not 97. Chosen over skimming because it
keeps every transaction's *destination* amount exact and predictable, and
generalizes cleanly to Exchange (which also has a fixed 10:1 output ratio
that a skimmed fee would make fractional). `Exchange` operates on the
wallet, not the bank balance — bring physical coins to the counter, walk
away with different ones — and rounds an upgrade's input down to the
nearest clean multiple of 10 rather than ever producing a fractional coin.

New `BankBox` (`IInteractable`, E to open) and `BankScreen` — unlike
Inventory/Crafting/Skills there's no hotkey, since a bank is a place you
have to be at. Lists wallet vs. bank balance per coin type with
Deposit/Withdraw buttons, plus 8 Exchange buttons (up/down each of the 4
adjacent pairs) — all four routed through the same stepper-button quantity
popup pattern the coin-drop feature established, showing a live fee/total
preview before confirming.

`PlayerBank.Awake` seeds a starting bank balance of 25 Gold, separate from
`PlayerCurrency`'s existing starting wallet purse. One `Bank Box` placed
5m from the Small Storage Box in `TestScene`, with a new navy `BankBox.mat`.

Notably, this is a Phase 3 "Commerce system" feature (per the design
brief) built well ahead of Phase 1 completion — see the MVP status
comparison from earlier this session. Still missing from that section:
trading between players, central banking in cities, and the volatile gem
market; this covers the personal deposit/withdraw/exchange piece only.

### v0.1.42-dev — Regular movement drains stamina too, not just sprinting
Previously, walking (Standing, moving, not getting the sprint bonus) held
stamina flat — no drain, no regen. It now drains at a new, slower rate
(`PlayerVitals.walkStaminaDrainPerSecond`, 2/s vs. sprinting's 10/s) via a
new `IsWalking` flag `FirstPersonController` sets alongside `IsSprinting`
each frame, same pattern. This also covers holding Shift below
`SprintStaminaThreshold` (85%) — no speed bonus there, but it still counts
as active movement, not resting.

The 85% sprint-drain cutoff was already implicit (`CanSprint` requires
`stamina >= 85`, so `IsSprinting` — and its drain — turns off the instant
stamina crosses below it) — confirmed that's still exactly the behavior,
just with the new walk-drain now taking over below that point instead of
stamina holding flat. Regen is unchanged: still only stopped, kneeling,
crawling, or prone.

### v0.1.41-dev — Drop coins from the currency row
Clicking a coin box in `InventoryScreen`'s currency row now opens a
quantity popup (`DrawCoinDropPopup`) instead of doing nothing — stepper
buttons (±1/±10, "All") rather than a slider, matching this screen's
existing button-only popups and giving exact control a slider wouldn't at
a 250-coin scale. **Drop** spends that many via the new
`PlayerCoinDrop.DropCoins`.

New `PlayerCoinDrop` builds each dropped coin procedurally
(`CreatePrimitive(Cylinder)` + the matching material + `Rigidbody` +
`Coin`) rather than needing a prefab per type — `Coin` gained a
`Configure(type, amount)` method for this, the same pattern
`Pickup.Configure` already uses for generic dropped items. Coins spawn
individually (one `Coin` object per unit dropped, not a single
stack-of-N) at a small random horizontal offset in front of the player
and get a small physics impulse — same "scatter" approach
`ResourceNode.OnPunch` already uses for rock chunks — so a multi-coin drop
bounces apart on landing instead of stacking identically. Rigidbody set
to ContinuousDynamic from the start (see
[[gridless-ground-tunneling]]).

### v0.1.40-dev — Prone is its own stance, and the keybinds moved
Follow-up to the previous version: "Crawling" and "Prone" turned out to be
two different things the user wanted, not one stance under two names.
Added `MovementStance.Prone` (0.1× speed — slower than Crawl's 0.2×,
lying flat being more restrictive than moving on hands and knees) and
rebound all three: **X** = Kneel (was Left Ctrl), **C** = Crawl (was Z),
**Z** = Prone (new). Still mutually exclusive — pressing a different
stance's key switches directly to it — and Prone gets the same
sprint/jump-disabled, stamina-regenerates treatment the other two already
had, since it's just as much "not standing" as they are.

### v0.1.39-dev — Stamina-gated movement speed, plus Kneeling/Crawling stances
Reworked stamina's effect on movement into three tiers (checked in
`FirstPersonController.HandleMove`, independent of stance):
- **Stamina ≥ 85%** (`PlayerVitals.SprintStaminaThreshold`) — sprint gives
  its full speed bonus, same as before.
- **10% ≤ stamina < 85%** — sprint no longer gives any bonus; holding
  Shift just moves at normal speed. `PlayerVitals.CanSprint` now checks
  this threshold directly instead of the old hysteresis-based
  `isExhausted`/`staminaExhaustionRecoveryThreshold`, which are gone.
- **0% < stamina < 10%** — movement speed halved.
- **Stamina = 0%** — movement speed cut to 10%.

Also reworked stamina regen: it used to climb back up any time the player
wasn't sprinting, including while just walking. Per this request it now
only regenerates while stopped, kneeling, or crawling — walking normally
holds it flat. `PlayerVitals` gained a `CanRegenStamina` flag (set every
frame by `FirstPersonController`, same pattern as `IsSprinting`) instead
of inferring it from `IsSprinting` alone.

Kneeling and crawling didn't exist as player states before this — added
both as new `MovementStance` values (`Standing`/`Kneeling`/`Crawling`),
toggled with Left Ctrl (kneel) and Z (crawl), mutually exclusive, each
applying its own speed multiplier (kneel 0.4×, crawl 0.2×, both stacking
with the stamina tiers above) and disabling sprint and jump while active.
Current stance now shows in the bottom-left debug panel alongside
speed/sprinting.

### v0.1.38-dev — Starting purse: 20 Copper, 5 Silver, 1 Gold
`PlayerCurrency.Awake` now seeds the wallet via the same `Add` path a Coin
pickup uses (so it still respects `MaxBalance`, though nowhere near it)
instead of starting every character at zero across the board.

### v0.1.37-dev — Coin pickups deposit straight into the wallet, capped at 250
`PlayerCurrency.Add` now clamps each balance at a new `MaxBalance` (250)
and returns the leftover that didn't fit — same convention as
`Inventory.AddItem` — instead of adding unconditionally.

New `Coin` (`IInteractable`, not an inventory item): picking one up calls
`PlayerCurrency.Add` for its `CoinType` and destroys itself, *unless* that
type is already capped, in which case it leaves the (partial) remainder
sitting in the world rather than deleting value for nothing. Coins aren't
carried or manually dropped — there's no inventory step, matching how
picking one up is meant to work as a direct wallet deposit.

Five small round coins (a scaled-down `Cylinder` primitive, one per
`CoinType`) placed in `TestScene`, each with its own color-matched
material (`CopperCoin.mat` etc.) so they visually read as their type both
sitting in the world and while physically dropping onto the ground.
Rigidbody set to ContinuousDynamic collision detection from the start
(see [[gridless-ground-tunneling]]).

### v0.1.36-dev — Currency: Copper/Iron/Silver/Gold/Platinum row on the Inventory screen
New `PlayerCurrency` — a five-coin ledger (`CoinType`: Copper, Iron,
Silver, Gold, Platinum), each balance starting at 0, with `Add`/`Spend`
already there for whatever earns/spends coins later even though nothing
does yet.

`InventoryScreen` gained a fixed header row above the scrollable content
(so it can't scroll out of view): 5 equal-width boxes spanning 90% of the
panel's width, centered, each with its coin type's name above it as a
label. Read-only for now — just displays `PlayerCurrency`'s live balances.
Added `PlayerCurrency` to the `Player` GameObject in `TestScene`.

### v0.1.35-dev — Always-on Health/Stamina/Hunger/Thirst bar HUD
New `VitalsBarHUD`, a permanent bottom-center 2×2 grid (Health/Stamina top
row, Hunger/Thirst bottom row) — deliberately independent of
`PlayerHealthMonitor`'s detailed text panel from a few versions back,
which stays gated behind wearing a monitor; this is a baseline glanceable
readout that's always there.

Each bar's full width represents 150% of a stat's normal max (100), not
100% — `fraction = Mathf.Clamp01(value / 150f)`, so under the game's
ordinary 0-100 range every bar's top third stays visually
empty/transparent by design (reserved headroom, not a bug), only filling
past two-thirds if something ever pushes a stat above 100. Color-coded per
stat (red/gold/orange/blue) with the numeric value overlaid as centered
text. Added to the `Player` GameObject in `TestScene`.

### v0.1.34-dev — Movement locks while any screen has the cursor unlocked
User report: typing a space into the rename box's text field also jumped
the player. Root cause was broader than renaming specifically —
`FirstPersonController.HandleLook` already skipped mouse-look while
`Cursor.lockState` wasn't `Locked` (the shared signal every screen —
Inventory, Crafting, Skills, `PlayerRenaming` — sets when it opens), but
`HandleMove` had no equivalent guard, so WASD and Space always drove the
player regardless of what screen was open.

Added the same `Cursor.lockState != CursorLockMode.Locked` early-return to
`HandleMove` that `HandleLook` already had, fixing it for every screen at
once rather than special-casing the rename box. Movement (including
gravity) now fully pauses while any screen is open, matching "lock the
controls until accepted or cancelled."

### v0.1.33-dev — Fix bugs found after pulling 0.1.5-0.1.32: worn-item visibility, a Canteen-corrupting eviction bug
User report after playtesting the batch of work from the other session (Sunglasses,
Navigation Computer, Health Monitor, Canteen, storage/UI overhaul): four things
looked wrong. Root-caused all four before fixing anything, per this project's own
verify-before-fixing habit — one turned out not to be a bug at all.

- **Worn items visible when looking down/around.** The "hide from your own camera
  while worn" fix `Backpack.cs` got several sessions ago (a `WornEquipment` layer,
  toggled in `SetCarried`) never made it onto the four equippables that shipped
  since: `Canteen`, `Sunglasses`, `NavigationComputer`, `PersonalHealthMonitor`. Added
  the identical `SetLayerRecursively` treatment to all four. Added a checklist to
  `CLAUDE.md` so this stops recurring per new equippable.
- **A held Canteen turning into an inert "CanteenItem" placeholder with no Fill/
  Drink.** Real root cause, found by reading code rather than guessing:
  `PlayerLoot.ReceiveEquipment()` already refuses to evict an equipment-holding hand
  slot via the generic drop path (correctly, since that path doesn't know how to
  detach a physical `IEquippable`) — but the sibling method `Receive()`, used for
  picking up *plain* items, was missing that same guard. Picking up a plain item
  while both hands were full, one occupied by a Canteen, evicted the Canteen through
  `PlayerDropping.DropFrom`, which matches `Inventory.RemoveItem`/`AddItem` by
  `ItemDefinition` alone — stripping the `equipment` reference and leaving the real
  Canteen orphaned (still attached to the player, but referenced by no inventory slot)
  while spawning a fake, non-functional "CanteenItem" stack in its place. Added the
  missing `occupant.equipment == null` guard to `Receive()`, matching
  `ReceiveEquipment()`'s existing conservative behavior. Documented the underlying
  gotcha (`InventoryTransfer.Move`/generic `RemoveItem`+`AddItem` silently strip
  `equipment` references) in `CLAUDE.md` — this is the second equippable-corruption
  bug from the same root cause, and won't be the last new equippable added.
- **Crafted Rock Knife landing in the main inventory instead of the backpack** — not
  a bug. Explicitly documented, intentional behavior from the crafting-materials
  commit two versions prior: crafting output only ever lands in the main inventory;
  only *inputs* can be drawn from the backpack/storage. Flagged to the user as a
  possible follow-up request, not fixed.
- **"Couldn't find a way to fill the Canteen"** — turned out to be the corruption bug
  above, not a missing feature. Fill/Drink already render unconditionally the moment
  a real Canteen occupies any equipment slot (verified by reading
  `InventoryScreen.DrawEquipmentSection` directly) — what the user had was the fake
  placeholder item from the eviction bug, which naturally had no Canteen-specific
  buttons since it wasn't a Canteen anymore.

### v0.1.32-dev — Crafting sees materials in your backpack and nearby storage
User report: materials sitting in an equipped backpack showed in the
Inventory screen but the Crafting screen (and `TryCraft`) only ever
checked the main inventory, so a recipe could read "have 0" while the
backpack clearly held enough.

`PlayerCrafting` now checks (and, on craft, draws from) every reachable
`Inventory`: the main inventory first, then an equipped backpack's
contents, then any `StorageBox` within `storageRange` (10m, same default
as `InventoryScreen`) — same idea as the move popup already being able to
send items to any of those, just applied to what a recipe can consume.
`GetAvailableCount` replaces the direct `PlayerInventory.GetCount` call in
`CraftingScreen`'s "have N" label, so the displayed count now matches what
`HasIngredients` actually checks. Output still only ever lands in the main
inventory — this only changes where *inputs* can come from. When crafting
consumes an ingredient split across sources, it takes from the main
inventory first, then the backpack, then boxes in distance order.

Extracted the nearby-box lookup (previously duplicated between
`InventoryScreen` and now `PlayerCrafting`) into a shared
`StorageBox.FindNearby` static method both call.

### v0.1.31-dev — Secret Wall: a message only Sunglasses can reveal
New `SecretMessageWall` (5m × 5m × 0.5m, medium gray, blocks movement like
any solid object) — a plain wall unless you're wearing Sunglasses
(`PlayerSunglasses.Equipped != null`) *and* actually looking at it
(raycast from `PlayerInteraction`'s camera hits this specific wall's
collider), in which case it draws "Hell Yeah Brother!" in bold black text
at its screen-projected position.

Not a child of `Player` — it's a world object, not player gear — so
rather than wiring a scene reference for one Easter-egg object, it looks
up `PlayerInteraction`/`PlayerSunglasses` once via `FindFirstObjectByType`
in `Start`. Placed one at `(0, 2.5, 8)` in `TestScene`, with a new
medium-gray `Wall.mat`.

### v0.1.30-dev — Multi-ingredient recipes, and a new Rock Hammer
`CraftingRecipe` could only ever hold one input item — no way to express
"needs a Stick *and* a Small Rock" for the Rock Hammer this version adds.
Replaced `inputItem`/`inputCount` with `Ingredient[] ingredients`
(`{item, count}` pairs). `PlayerCrafting.TryCraft`/`HasIngredients` now
take the `CraftingRecipe` itself rather than looking one up by a single
input item — that lookup-by-item indirection only ever worked because
every recipe had exactly one input, and had already gone unused outside
`TryCraft` itself since Crafting moved into its own screen (`CraftingScreen`
already iterates `crafting.Recipes` directly). `FindRecipe` is gone.

Migrated all 5 existing recipes to the new format (each becomes a
single-element `ingredients` array with its old input item/count — verified
each against what was already on disk before changing anything). New
`RockHammerRecipe.asset`: 1 Stick + 1 Small Rock → 1 Rock Hammer, trains
Crafting (+2). New `RockHammerItem.asset` ("Rock Hammer", max stack 1) — a
plain, non-equippable item with no custom prefab, so dropping one falls
back to the generic `DroppedItem` prefab like any other plain resource.

`CraftingScreen` now lists every ingredient per recipe ("needs 1x Stick
(have 3), 1x Small Rock (have 5)") instead of just one, still greying out
Craft when anything's short or the inventory has no room for the output.
Widened the panel (380×300 → 460×320) for the longer multi-ingredient
labels.

### v0.1.29-dev — Rock Node pieces are now "Small Rock"
Pure data change, no code touched. `Rock.asset`'s `itemName` changed from
"Rock" to "Small Rock" — it's the same `ItemDefinition` asset `RockChunk`
(what the Rock Node scatters when broken) and `RockKnifeRecipe` already
both pointed at, so renaming it in place means the Rock Knife recipe
automatically now reads as requiring Small Rock, with no reference to
update. Checked first that nothing else in the project referenced this
asset (only those two), so an in-place rename was safe rather than
needing a new item + reference migration.

### v0.1.28-dev — Crafting screen explains why a recipe can't be made
User report: clicking Craft on a Rock Knife with 6 Rock in hand did
nothing. `PlayerCrafting.TryCraft` was already correctly failing —
`Inventory.HasSpaceFor` returns false when the main inventory (only 4
slots by default) has no room for the output — but nothing ever told the
player that's what happened, so a full inventory and a genuine bug looked
identical from the UI.

`CraftingScreen` now checks `hasEnough`/`hasSpace` per recipe before
drawing its Craft button: `GUI.enabled = hasEnough && hasSpace` greys the
button out and makes it unclickable when the recipe can't be made, and the
label appends "— inventory full" when that's specifically why (insufficient
input already shows via the existing "have N" count). Widened the panel
(340 → 380) to fit the longer label.

### v0.1.27-dev — Sticks and the Rock Node respawn 3 minutes after being taken
`Pickup` gained an opt-in `canRespawn` (default off, so every existing
usage — items the player drops, chunks scattered out of a broken
`ResourceNode` — is unaffected). When enabled, taking the item no longer
destroys the GameObject: it hides the renderer/collider instead and starts
a `respawnDelay` (180s) countdown from `Time.time`, only running once
something's actually been taken — sitting there unpicked holds the timer
indefinitely, per the request. On expiry it reappears at its original
spawn position (captured in `Awake`) plus a small random horizontal
offset (`Random.insideUnitCircle * respawnScatter`, 0.5m).

`ResourceNode` (the Rock Node) got the identical pattern directly, since
it's never used for anything but a persistent world resource point —
breaking it now hides+times out the same way instead of destroying the
GameObject, and respawning also resets `hitsTaken` so it can be broken
again from scratch.

Enabled `canRespawn` on both `Stick Pickup` and `Stick Pickup 2` in
`TestScene`; left the Berry Pickup and everything else untouched.

### v0.1.26-dev — Sunglasses: a silver screen tint while worn on the face
New `Sunglasses` (`IInteractable` + `IEquippable`) carried by
`PlayerSunglasses`, built like `PlayerBackpack` rather than the wrist
gadgets — a single destination slot (`Face`) instead of trying two. Face
has capacity 2 (room for a second accessory later), so unlike the
capacity-1 wrist/back slots, `PlayerEquipment.GetEquipped`'s "first
equipped item" isn't reliable for finding *this* instance — `Equipped`
and `FindSlot` instead scan the Face slot's own entries for a `Sunglasses`
specifically. Same pickup-priority/Unequip-fallback/Drop pattern as every
other equippable this session.

While worn, `PlayerSunglasses.OnGUI` draws a light silver, 25%-alpha
full-screen texture over everything — a pure visual filter with no
gameplay effect. Unequipping or dropping them means `Equipped` goes null
and the overlay stops drawing, the same "equip gates the HUD" pattern as
the Nav Computer's compass and the Health Monitor's vitals panel.

Craftable from 1 Rock Knife (`SunglassesRecipe.asset`, trains Crafting,
skill gain 2 — cheaper than the electronic gadgets' 3), and one is placed
in `TestScene` at `(-3.5, 0.3, 1.5)` as a world pickup. World Rigidbody
set to ContinuousDynamic collision detection from the start (see
[[gridless-ground-tunneling]]).

### v0.1.25-dev — Personal Health Monitor: wrist-worn vitals HUD
Second wrist-worn gadget alongside the Navigation Computer, built the same
way: `PersonalHealthMonitor` (`IInteractable` + `IEquippable`, no inventory
of its own) carried by `PlayerHealthMonitor`, same
pickup-priority/Equip/Unequip-with-fallback/Drop pattern as
Backpack/Canteen/NavComputer. Craftable from 1 Rock Knife
(`HealthMonitorRecipe.asset`, trains Crafting), and one is placed in
`TestScene` at `(-3, 0.3, 1)` as a world pickup. Its world Rigidbody was
set to ContinuousDynamic collision detection from the start — see
[[gridless-ground-tunneling]] — instead of repeating the bug just fixed in
the previous version.

While worn on either wrist, it draws the exact "Vitals" panel that used to
be `PlayerVitals`'s own always-on top-right `OnGUI` — Health/Hunger/
Thirst/Stamina/Body Temp — which is now gone from `PlayerVitals` entirely.
`PlayerVitals` just exposes the numbers (`Health`, `Hunger`, etc., already
public) for `PlayerHealthMonitor` to read; the game no longer always shows
vitals, only while a monitor is equipped, matching how the Nav Computer
gates its compass. `InventoryScreen` got the same three-way
Equip/Unequip/Drop wiring the other wrist item already has, in both the
main inventory list and the Equipment section.

### v0.1.24-dev — Skills (U) and Crafting (O) become their own toggleable screens
Pulled both out of where they used to live — Skills was an always-on
bottom-left panel drawn directly by `PlayerSkills.OnGUI`; Crafting was an
inline "(craft X)" button next to a matching item in `InventoryScreen`'s
main list — into dedicated screens matching `InventoryScreen`'s own
open/close convention (centered panel, Close button, cursor
lock/unlock, only opens from normal gameplay so it can't stack on another
open screen).

New `SkillsScreen` (U) reads `PlayerSkills.Levels` (newly exposed —
`PlayerSkills` no longer draws anything itself) and lists each skill's
level. New `CraftingScreen` (O) reads `PlayerCrafting.Recipes` (also newly
exposed) and lists *every* known recipe with how many of its input you
currently have, rather than only showing a craft option for items already
sitting in your inventory. `InventoryScreen` no longer has any
crafting-related code — that recipe-lookup button is gone from its main
list.

`FirstPersonController`'s Escape handling now closes both new screens
alongside Inventory and the rename prompt, so they can't be left open with
a locked cursor. Added `SkillsScreen`/`CraftingScreen` to the `Player`
GameObject in `TestScene`.

### v0.1.23-dev — Fix dropped Backpack/Canteen/Navigation Computer falling through the floor
User report: dropping a Canteen or Navigation Computer made it appear
briefly then vanish. Root cause: `Backpack.prefab`, `Canteen.prefab`, and
the Navigation Computer's world Rigidbody all had `m_CollisionDetection: 0`
(Discrete) — every other droppable prefab (`DroppedItem`, `BerryPickup`,
`StickPickup`, `RockKnifePickup`, `RockChunk`) already used `2`
(ContinuousDynamic). `Ground` is a Plane mesh scaled to (10, 1, 10) — a
paper-thin `MeshCollider` — so a Discrete-mode Rigidbody falling even the
~1m `dropHeight` drop distance could tunnel straight through it with no
tolerance for the gap Discrete detection leaves, meaning it just kept
falling, off into the void.

Switched all three to ContinuousDynamic: `Backpack.prefab`,
`Canteen.prefab`, and the world-placed instances of Backpack, Canteen, and
Navigation Computer already sitting in `TestScene` (editing the prefabs
alone doesn't retroactively fix instances that aren't live prefab
connections — these three were baked copies, so each needed the same
scalar field fixed directly).

### v0.1.22-dev — Move popup can send an item straight to the backpack
User feedback: moving an item out of a nearby storage box's contents (or
a hand) always landed in the main inventory or a hand — there was no way
to send it straight into an equipped backpack, unlike the main inventory
list's row buttons, which already had "To Pack" alongside "To Storage".

`DrawMoveDestinations` (the popup opened by clicking an item in a
container's contents grid, a hand, or the equipment section) gained a
"To Backpack" option, shown whenever a backpack is worn and isn't already
the source — same guard pattern as the existing hand/storage options.
Bumped the popup's fixed height (240 → 270) to fit the extra button.

Also removed the temporary `Debug.Log` calls added earlier this session
while diagnosing what turned out to be a stale `Library` cache, not an
actual code bug, on the "To Storage" picker (see previous entry).

### v0.1.21-dev — Hover a storage container to see its name
New `StorageBoxHover`, attached to `Player` alongside `PlayerInteraction`.
Raycasts from the same camera every `Update` (its own `hoverRange`, 20m —
deliberately longer than interact range, since reading a label shouldn't
require being close enough to use the box) and, when the ray hits a
`StorageBox`, draws its `DisplayName` above the crosshair in `OnGUI`.
Reads `DisplayName` directly, so a renamed box's name shows immediately.

Positioned above the crosshair rather than below, where
`PlayerInteraction`'s own interact-prompt text draws — the two never
compete for the same spot since `StorageBox` isn't `IInteractable`.

### v0.1.20-dev — "To Storage" now lists nearby boxes by name
User feedback: `InventoryScreen` only ever offered the single *nearest*
StorageBox as a move destination, silently ignoring any others in range.
`storageRange` (10m) can easily contain more than one box, so there was no
way to choose which.

`nearbyStorage` (single) became `nearbyStorages` (`List<StorageBox>`,
nearest first — `FindNearbyStorageBoxes` now populates it instead of
returning one). Clicking "To Storage" — from either the main inventory
list or the move popup — no longer moves immediately; it switches the
popup into a picker mode (`choosingStorage`) listing every nearby box by
`DisplayName` (so a rename shows up here too), with **Back** to return to
the normal destination list and **Cancel** as before. The auto-expanding
"(nearby)" contents section still shows just the nearest box, unchanged.

### v0.1.19-dev — Navigation Computer: wrist-worn compass + speed HUD
New equippable gadget, `NavigationComputer` (`IInteractable` + `IEquippable`,
no inventory of its own — just a wearable), carried by new `PlayerNavComputer`
(`RequireComponent`s `PlayerInventory`/`PlayerEquipment`/`CharacterController`).
Pickup follows the same priority as Backpack/Canteen (equipped backpack, then
a free hand, then stashed in the main inventory), and `Equip` tries Left
Wrist then Right Wrist. `Unequip` uses the same fallback chain added for
`PlayerBackpack` a few versions back — main inventory, then a hand, then
drop — instead of risking the old no-op-when-full bug.

While a computer is worn on either wrist, `PlayerNavComputer.OnGUI` draws a
scrolling compass strip across the top-center of the screen (cardinal
labels positioned by their angular offset from `transform.eulerAngles.y`,
so they slide past as the player turns) with current horizontal speed
(from `CharacterController.velocity`, y-component zeroed) shown underneath.
Unequipping just stops drawing it — `Equipped` going null is the only
condition `OnGUI` checks.

`InventoryScreen` got the same three-way Equip/Unequip/Drop wiring
Backpack/Canteen already have, in both the main inventory list and the
Equipment section (worn = shown on Left/Right Wrist specifically, unlike
Canteen where any of its slots count as worn).

Craftable from 1 Rock Knife (`NavComputerRecipe.asset`, trains Crafting),
and one is placed in `TestScene` at `(-1.5, 0.3, 0.5)` as a world pickup so
it's usable without crafting first.

### v0.1.18-dev — Right-click a world object to rename it
New `IRenameable` (`DisplayName`, `Rename(string)`) and `PlayerRenaming`,
which right-click-raycasts using the same camera/range as
`PlayerInteraction`'s E-prompt. Hitting an `IRenameable` opens a small
text-entry window (Enter or Save to commit, Cancel or Escape to discard),
unlocking the cursor the same way `InventoryScreen` does. `StorageBox` is
the first (and so far only) `IRenameable` — since `InventoryScreen`
already reads a nearby box's name through `DisplayName`, a rename shows up
there automatically with no further changes needed.

Wired `PlayerRenaming.Close()` into `FirstPersonController`'s Escape
handling alongside `InventoryScreen.Close()`, and gated `InventoryScreen`'s
I-key toggle to only *open* while the cursor is locked — otherwise
pressing I while the rename window was open would stack the inventory
screen on top of it. Added the `PlayerRenaming` component to the `Player`
GameObject in `TestScene`.

### v0.1.17-dev — Small Storage Box spawned 20m from player start
No code changes — `StorageBox`'s capacity was already a `[SerializeField]`,
so this is purely a scene addition. Added a second, smaller box ("Small
Storage Box", 10 slots vs. the original's 20) to `TestScene` at
`(0, 0.2, -20)`, 20 meters from the player's spawn point `(0, 1.05, 0)`
and clear of every other placed object (all of which sit within ~3.4m of
spawn). Reuses the existing `Assets/Data/StorageBox.mat`, just scaled down
(0.45 x 0.35 x 0.35) to read as the smaller of the two at a glance.

### v0.1.16-dev — Storage boxes: auto-expand the inventory screen near a nearby box
New `StorageBox` — a stationary world container (not `IInteractable`, no
pickup/use prompt). Every enabled box registers itself in a static
`StorageBox.Active` list; `InventoryScreen` checks that list once per
`OnGUI` frame and finds the nearest box within `storageRange` (10m by
default). When one's in range, opening the I screen adds a third section
below Inventory/Equipment showing that box's contents as a clickable grid,
reusing the existing "where should this go?" move popup (now with a "To
Storage" destination alongside Drop/hands/inventory) so items can move
either direction. Plain inventory items also get a "To Storage" button
next to "To Pack", mirroring how backpack transfers already worked.

`DrawContainerContents` was generalized from taking an `IInventoryHolder`
to a plain `(Inventory, caption)` pair, since a `StorageBox` has no
Stash/SetCarried/equip-slot concept to justify that interface — it's just
another `Inventory` to render the same way a worn backpack's contents
already were.

Added one Storage Box to `TestScene` at `(3, 0.25, 0)`, clear of the
existing Backpack/Canteen/resource spawns, with a new
`Assets/Data/StorageBox.mat` (brown) so it reads as a container at a
glance.

### v0.1.15-dev — Unequip falls back to a hand/drop instead of no-op'ing, canteen spawns at start
User feedback: unequipping a worn backpack when the main inventory is full
did nothing — `PlayerBackpack.Unequip` only ever attempted
`playerInventory.Inventory.AddEquipmentItem` and returned `false` with no
other recourse. It now mirrors the fallback chain `PickUp`/`ReceiveEquipment`
already used: main inventory first, then Left Hand, then Right Hand, and
if all of those are full, drops the backpack into the world in front of the
player rather than leaving it stuck on the back.

Also added a Canteen to `TestScene` at `(-1, 0.3, 1.5)`, spawned alongside
the existing world-start Backpack so there's a liquid container to pick up
without needing to craft one first.

### v0.1.14-dev — Plain items in a hand use the same move popup as backpack contents
Follow-up to the previous version's popup, closing the scope gap flagged
there: clicking a plain item sitting directly in an equip slot (e.g.
something picked up into a hand) now sets `pendingMoveItem`/
`pendingMoveSource` and opens `DrawPendingMovePopup`, same as clicking an
item inside a backpack's contents grid — instead of moving straight to the
main inventory with no other choice. The two click sites now share one
popup and one set of destination rules instead of each hardcoding its own
single target.

### v0.1.13-dev — Popup for where a backpack item should go, instead of a hardcoded move
User feedback: clicking an item inside the backpack's contents grid always
moved it straight to the main inventory with no other option — should
offer Drop or move-to-hand instead, ideally as a menu of choices.

`DrawContainerContents` no longer moves anything itself — clicking an
occupied box now just records `pendingMoveItem`/`pendingMoveSource` and a
small popup (`DrawPendingMovePopup`) opens with the real set of
destinations: **Drop**, **To Left Hand**, **To Right Hand**, **To
Inventory**, **Cancel** — each hand/inventory option only shown if it
isn't already the source. Drawn last in `OnGUI`, after `GUILayout.EndArea()`
of the main panel, so it renders on top regardless of scroll position.
Cleared whenever the screen closes (`SetOpen(false)`), so a stale popup
can't reappear the next time it's opened.

Scope note: this only changes the backpack-*contents* click (the thing
actually reported). The separate "click a plain item sitting directly in a
hand" case (added two versions ago) still moves straight to inventory —
left alone since it wasn't part of what was asked, though it'd be a
straightforward follow-up to route through the same popup if wanted.

### v0.1.12-dev — A held (not worn) backpack isn't usable storage yet
User feedback on the previous version's routing change: a backpack picked
up into a hand showed "Unequip" (as if already worn) and exposed its
contents grid, when thematically holding a backpack in your hand isn't the
same as wearing it — you shouldn't be able to use it as storage, or
"unequip" something that was never equipped.

`InventoryScreen` now branches on which slot a backpack is actually in: on
`Back`, unchanged (Unequip + contents grid). Anywhere else (a hand), shows
**Equip** instead of Unequip, and the contents grid doesn't render at all —
`nestedHolder` is only set when `slotName == "Back"`.

Fixing this exposed a real duplicate-occupancy bug in `PlayerBackpack.Equip`:
it unconditionally removed the backpack from the *main inventory* before
placing it on `Back`, regardless of where it actually was. If it was
sitting in a hand instead (the new common case after last version's
routing change), that removal call found nothing there and silently did
nothing — the backpack would end up occupying *both* the hand slot and
`Back` simultaneously. `Equip` now calls the same `FindSlot()` used by
`Unequip`/`Drop` to locate it first, then removes it from wherever that
actually is.

### v0.1.11-dev — Backpack/Canteen pickup routes through PlayerLoot too; 20-cap
User-reported gap: picking up a Backpack (or Canteen) from the world always
stashed it straight into the main inventory — `Backpack.Complete`/
`Canteen.Complete` never went through the `PlayerLoot` hand/backpack
priority added last version at all, only `Pickup.Complete` did. Sticks
correctly went to a hand; the backpack itself didn't.

- `PlayerLoot` gained `ReceiveEquipment(item, IEquippable)`, same priority
  as `Receive()` but using `AddEquipmentItem`/`RemoveEquipmentItem` since
  Backpack/Canteen aren't stackable counts. Deliberately does *not* evict
  another equipment item from a hand to make room (only plain items) —
  swapping someone's held Canteen out for a picked-up Backpack felt like a
  rarer case not worth the added complexity.
- This exposed a real gap in `IEquippable`: it only had `DisplayName`, so
  there was no way to generically tell a newly-routed item to become
  visible (carried, e.g. landed in a hand) vs. stay hidden (stashed, e.g.
  packed inside a container). Promoted `Stash()`/`SetCarried(bool,
  Transform)` onto the interface — `Backpack` and `Canteen` needed zero
  code changes, since both already implemented matching methods.
- That promotion broke compilation: `PlayerInventory` also declared
  `IInventoryHolder` (which extends `IEquippable`), so it was suddenly on
  the hook for `Stash`/`SetCarried` too, despite never being a physical
  object. Checked whether anything actually used `PlayerInventory` as an
  `IInventoryHolder`/`IEquippable` polymorphically — nothing did, anywhere
  — so removed that conformance (and the `DisplayName` property that only
  existed to satisfy it) rather than bolting on meaningless no-op methods.
- `PlayerBackpack.Unequip`/`Drop` had the same latent bug already fixed for
  the routing itself: both assumed a backpack was either in `Back` or the
  main inventory, so a backpack that ended up in a hand couldn't actually
  be removed from it — clicking Drop would detach the physical object
  while leaving a "ghost" entry stuck occupying the hand slot. Added
  `FindSlot()` (Back, then both hands) so both methods find it wherever it
  actually is. `PlayerCanteen` already searched all its valid slots this
  way, so it wasn't affected.

Also, per a second request in the same message: `Inventory` now enforces a
hard `MaxStackCap = 20` centrally (`Mathf.Min(item.maxStack, MaxStackCap)`
wherever `maxStack` was used), rather than trusting each `ItemDefinition`'s
own value — applies to every `Inventory` (main, backpack, any equip slot)
from one place. A no-op today (Rock/Stick are already 20), but a real
ceiling against a future item being configured with an unintended stack size.

### v0.1.10-dev — Pickups route to Backpack, then hands, evicting if needed
User-requested mechanics change: picked-up items no longer go straight to
the main 4-slot inventory. New priority order, implemented in a new
`PlayerLoot` component:
1. **Backpack equipped** → item goes straight into its `Inventory`
   (`AddItem`, normal stacking/capacity rules — if the backpack is full the
   remainder stays on the ground, same as the existing full-inventory
   behavior).
2. **No backpack** → tries Left Hand, then Right Hand (`Inventory.AddItem`
   on each slot — stacks into a hand already holding the same item before
   trying an empty one).
3. **Both hands occupied by something that won't stack** → evicts whatever
   is in Left Hand (physically dropped into the world, not deleted), then
   places the new item there. Picking something up now never simply fails
   when there's no backpack — worst case it swaps out what's in your hand.

`PlayerDropping` gained a `DropFrom(Inventory, item)` alongside the existing
`Drop(item)`, so eviction reuses the exact same "spawn a physical pickup in
the world" path as the manual Drop button instead of duplicating it —
`Drop(item)` is now a one-line call to `DropFrom(playerInventory.Inventory,
item)`.

`Pickup.Complete` now calls `PlayerLoot.Receive` instead of
`PlayerInventory.AddItem` directly (falls back to the old direct-to-
inventory behavior if `PlayerLoot` is somehow missing).

**Necessary follow-on:** hands can now hold plain stackable items, not just
equippables like Canteen — but `InventoryScreen`'s equipment boxes were only
ever interactive for backpack/canteen contents. A plain item picked into a
hand would've been visible but permanently stuck with no UI path back out.
Made plain-item boxes in any equip slot clickable-to-move-to-inventory too,
same pattern as backpack contents.

### v0.1.9-dev — Consolidate all inventory UI into the I screen
User request: the always-on Inventory box and Back-slot (Backpack) panel
should be gone from the normal HUD entirely, with inventory only visible via
I. Rather than just hiding those panels behind an `IsOpen` check (already in
place from the previous overlap fix), folded their actual content into
`InventoryScreen` and deleted the three source `OnGUI` methods outright —
one screen, one place the logic lives, instead of three panels coordinating
visibility with a fourth.

- `PlayerInventory.OnGUI` (item list, craft/eat/drop/equip/to-pack buttons)
  → `InventoryScreen.DrawInventorySection`.
- `PlayerBackpack.OnGUI` (Unequip/Drop Backpack, per-item "To Inventory")
  → folded into `InventoryScreen.DrawEquipmentSection`'s Back row: Unequip/
  Drop buttons appear next to the slot, and each nested content box is now
  itself a button — click an item to move it back to the main inventory,
  replacing the old separate "To Inventory" button per row.
- `PlayerCanteen.OnGUI` (Drink/Fill/Unequip/Drop) → same treatment, appended
  to whichever slot (Left Hand/Right Hand/Waist) the canteen currently
  occupies.

`PlayerInventory`/`PlayerBackpack`/`PlayerCanteen` lost their now-dead
`crafting`/`dropping`/`eating`/`vitals`/`inventoryScreen` cross-references
along with the removed `OnGUI`s — they're back to pure state/logic holders,
UI-agnostic.

Stacking the full inventory list + all 14 equipment rows + nested container
contents in one fixed-height panel would have badly overflowed most window
heights (a rough estimate came out near 900px). Switched to a
`GUILayout.BeginScrollView` inside a screen-clamped panel
(`Mathf.Min(Screen.height - 40, 700)`) instead of hand-computing exact
content height — robust regardless of how many slots end up occupied.

### v0.1.8-dev — Inventory screen: show container contents, fix panel overlap
User-reported bug, two real causes:
- `InventoryScreen`'s per-slot boxes only ever reflected the *slot's* own
  capacity (Back = 1 box), so a box just displayed "Rough Backpack" and
  never looked inside it — adding Sticks to the backpack via "To Pack"
  changed nothing on screen. Fixed by detecting when an equipped item is
  itself a container (`is IInventoryHolder`) and drawing a nested row of
  *that* container's own capacity/contents underneath the slot row, wrapped
  at 6 per line. Panel height is now computed per-frame from whatever's
  actually equipped, rather than a fixed constant, so it doesn't reserve
  wasted space when nothing equipped is a container.
- A screenshot from testing showed `PlayerBackpack`'s own always-on panel
  (`Unequip`/`Drop Backpack`/`To Inventory`) rendered directly on top of the
  Equipment screen — both draw in overlapping screen regions. `PlayerBackpack`
  and `PlayerCanteen` now skip their own `OnGUI` entirely while
  `InventoryScreen.IsOpen` is true, since the Equipment screen is meant to be
  the single source of truth when it's up. Trade-off: Unequip/Drop for those
  two aren't reachable while the Equipment screen is open — close it (I or
  Escape) to use them, consistent with the screen being read-only for now.

### v0.1.7-dev — Sync Escape and I so the cursor/inventory-screen state can't drift
`InventoryScreen` (I) and `FirstPersonController`'s Escape toggle each
managed `Cursor.lockState` independently, with no knowledge of each other.
Opening the inventory with I then pressing Escape would re-lock the cursor
via `FirstPersonController` while `InventoryScreen.isOpen` stayed `true` —
the panel kept rendering, mouse-look resumed under it, and a second I press
would then close it instead of reopening it. Caught by the user asking
"do we have a way to close the inventory screen" and pointing out the two
controls could disagree.

Fix: `InventoryScreen` exposes a public `Close()`; `FirstPersonController`
calls it whenever Escape transitions the cursor *into* the locked state
(`!wasLocked`) — "cursor just got re-locked" now always implies "any open
screen is closed" as an invariant, regardless of which control the player
used or which order their presses happened in. Deliberately not building a
general cursor-state stack/owner system for this — two toggles was simple
enough to reconcile directly; revisit if a third one shows up.
### v0.1.6-dev — Inventory management screen (I)
`InventoryScreen`, toggled with I, lists all 14 `PlayerEquipment` slots in
one place (previously only visible piecemeal — Backpack/Canteen each drew
their own panel only while equipped, and there was no view at all for the
other 12 slots since nothing equips into them yet). Each row is a slot name
plus one box per unit of that slot's `Inventory` capacity (so `Face` draws
two boxes, everything else one), showing the occupying item's name if
filled or "Empty" if not — reads `Inventory.Slots`/`Capacity` directly, so
it stays correct automatically as items get added/removed elsewhere.

Read-only for now: no equip actions live here, since nothing yet targets the
12 slots beyond Back/Hand/Waist. Opening it unlocks and shows the cursor
directly (mirrors what Escape already does in `FirstPersonController`,
kept intentionally simple rather than building a shared cursor-state
stack for two independent toggles).

Existing debug panels (Inventory, Backpack, Canteen, Vitals, Skills) are
unchanged and still always-on — this is an additional full-picture view,
not a replacement.

### v0.1.5-dev — Full body-equipment slot layout
`PlayerEquipment` reworked from "one named slot holds one `IEquippable`" to
"each named slot is its own small `Inventory`" (capacity usually 1, `Face` is
2), since some requested slots needed to hold more than one item — the same
`AddEquipmentItem`/`RemoveEquipmentItem` flow already used for the main
inventory and for Backpack/Canteen's own internal storage, just applied one
level up. Full slot list: `Head`, `Face` (×2), `Neck`, `Chest`, `Back`,
`Left Arm`, `Right Arm`, `Left Wrist`, `Right Wrist`, `Left Hand`,
`Right Hand`, `Waist`, `Leg`, `Feet`. `Back` was already named `Back`, not
`Backpack` — no rename needed there.

`PlayerBackpack`/`PlayerCanteen` updated to equip through
`equipment.GetSlot(name).AddEquipmentItem(...)` instead of the old
single-slot `Equip`/`Unequip`/`CanEquip` API, which no longer exists.
`PlayerCanteen` also simplified from two explicit destination buttons
(To Hand / To Belt) to one `Equip` button that tries `Left Hand` → `Right
Hand` → `Waist` in order — matches how `Backpack`'s row already works, and
avoids the button row growing by one for every additional slot a future
equippable might be able to target.

No scene changes needed: `PlayerEquipment.slotNames` and
`PlayerCanteen`'s old `handSlotAnchor`/`beltSlotAnchor` fields were renamed/
restructured, and `TestScene.unity` still has the old serialized values for
them — Unity just ignores orphaned fields on load and falls back to the new
fields' C# defaults, which happen to already be what's wanted (the full slot
list; unassigned anchors falling back to the player transform). Validated
with a full batch-mode compile check rather than assuming that fallback
holds.

### v0.1.4-dev — Merge: reconcile Waterskin with Canteen (keep Canteen)
Both sessions independently landed on the exact string `"0.1.3-dev"` for
`GameVersion` despite representing different code — a version-number collision
git's text diff can't catch, since identical text isn't a conflict. Bumped to a
genuinely new number for this merge.

Bigger reconciliation than a technical merge: this session's Empty/Filled
Waterskin (found container, filled at the Water Puddle, drunk via `EdibleItem`)
and the other session's Canteen below solve the same problem — carrying and
drinking water — built in parallel with no coordination. Not something to
mechanically merge; the game would end up with two redundant, unrelated ways to
carry water. Kept Canteen (craftable, equippable to Hand/Belt, fits the game's
first-person/embodied-crafting pillar better than a passively-found container)
and removed Waterskin entirely — `WaterSource.cs`, `EmptyWaterskin`/
`FilledWaterskin`/`WaterskinDrink` assets, their pickup prefabs/materials, and
the `WaterSource` component on the Water Puddle (now just a decorative prop;
Canteen's `Fill` isn't tied to a specific world location). Berry's `EdibleItem`/
`PlayerEating` system is unaffected and still ships — it doesn't overlap with
Canteen at all, and Canteen deliberately doesn't use it (holds liquid state
directly rather than wrapping an `Inventory`).

### v0.1.3-dev — Berry eat/drink system, per-item drop visuals, physics fixes
Berry went from an instant-eat-on-touch world object to a real inventory item:
`Pickup` it like anything else, carry it, move it to the backpack, and `EdibleItem`
(new ScriptableObject, mirrors the existing `CraftingRecipe` pattern) drives an
"Eat"/"Drink" button that only appears in the personal-inventory panel — never in
the backpack panel, so a stored berry can't be eaten without taking it out first.
The `verb` field ("Eat" vs "Drink") is data-driven per `EdibleItem` rather than
hardcoded, so future consumables (soup, potions, whatever) don't need a code change.

**New general mechanism:** `ItemDefinition.worldPickupPrefab` — what a dropped item
looks like now depends on the item, not a single generic gray-cube fallback shared
by everything. Built one for Berry, Stick, Rock (reusing the existing
`RockChunk.prefab` instead of duplicating it) and Rock Knife; the backpack already
had its own dedicated drop visual and didn't need one. (Also built one for the
Empty/Filled Waterskin at the time — removed along with the rest of that system in
the merge above.)

**Real bugs hit building this, in order:**
- A `SerializedObject.objectReferenceValue` assignment silently produced a null
  reference (`fileID: 0`) for several fields despite no error and an identical
  pattern elsewhere in the same script succeeding. Root cause: assets created via
  `AssetDatabase.CreateAsset` earlier in the script, then referenced *after* an
  `EditorSceneManager.OpenScene()` call later in the same script, without an
  intervening `AssetDatabase.SaveAssets()` — the scene-open silently invalidated
  the uncommitted in-memory asset references. Fixed by re-fetching via
  `AssetDatabase.LoadAssetAtPath` *after* the scene is already open, rather than
  trusting pre-open references to survive. General rule worth remembering: never
  let object references cross an `OpenScene` call within the same batch-mode
  script — save assets first, or re-fetch after.
- Repeated the exact material-into-prefab mistake this project's own `CLAUDE.md`
  already documents: used `new Material(Shader.Find(...))` directly on new drop
  prefabs instead of saving it as a real `.mat` asset first. All five new drop
  prefabs rendered pink until fixed. Worth noting because it's a *documented*
  gotcha that still got missed under time pressure — a reminder to actually check
  `CLAUDE.md` conventions before repeating a pattern, not just after something
  breaks.
- The two thinnest new drop prefabs (Rock Knife at 0.05 units tall, Stick at 0.1)
  fell straight through the Ground collider — classic tunneling: Unity's default
  Discrete collision detection can miss a collision entirely if a thin, fast-moving
  collider passes a thin static collider between physics steps. Berry (a chunky
  sphere) was thick enough to never hit this. Fixed by setting
  `Rigidbody.collisionDetectionMode` to `ContinuousDynamic` on every
  Rigidbody-bearing pickup/dropped-item prefab, not just the two that visibly broke.

### Merge: canteen + panel-layout/versioning reconciliation
Built in parallel with the `v0.1.2-dev` work below on a separate Claude Code
session, discovered on push — same recurring situation as the two merge
entries further down, but a cleaner one this time: no fileID collision, just
a text conflict in this file's own version line/entries. Two real things to
reconcile though, not just text:
- The other session's Backpack debug panel moved to `Rect(320, 10, 280, 320)`
  as part of its own panel-overlap cleanup — which put its right edge at
  `x=600`, ten pixels inside where this session's new canteen Hand/Belt panels
  had been placed (`x=590`). Moved the canteen panels to `x=610` and gave them
  the same `DebugGUI.DrawPanel`/`Header`/`Label` treatment the other panels
  now use, instead of plain unstyled `GUILayout`.
- First time this session's Claude instance saw the new
  `CLAUDE.md`/`CHANGELOG.md` version-bump convention introduced by the other
  session (`GameVersion` + this file's "Current version" line, bumped
  together on every gameplay-affecting commit). The canteen commit predated
  discovering that rule, so this merge is also where it first gets applied
  here — bumped `0.1.2-dev` → `0.1.3-dev`.

### Canteen: craftable liquid container, first `IEquippable` beyond Backpack (`8670677`)
Craftable from 3 Sticks (trains Crafting), cylinder-shaped (body + cap
primitives, steel-grey `Canteen.mat`), can sit in the regular inventory or be
equipped to two new slots — Hand or Belt (`PlayerEquipment.slotNames` grew
from just `Back`). Holds liquid, not items: `Canteen` tracks a
`LiquidType?`/`Amount`/`Capacity` triplet directly rather than wrapping an
`Inventory`, with `Fill`/`Drink` (the latter restores `PlayerVitals` Thirst).

**Refactor forced by this:** `Inventory.Slot.equipment` and
`AddEquipmentItem`/`RemoveEquipmentItem` were typed to `IInventoryHolder`,
which assumes the equipped thing wraps an `Inventory` — true for `Backpack`,
false for `Canteen`. Pulled the common bit (`DisplayName`) out into a new
`IEquippable` base interface; `IInventoryHolder : IEquippable` adds
`Inventory` on top for container-type equippables. `PlayerEquipment` now
stores `IEquippable`, not `IInventoryHolder` — `Backpack` needed no code
changes, since it still satisfies the wider interface through the narrower
one.

Built via the batch-mode Editor-script workflow throughout (prefab
composition + wiring `PlayerCanteen`/the new recipe into `TestScene` via
`SerializedObject`, not hand-authored YAML) — validated with a full batch-mode
compile check and a duplicate-fileID scan before committing.

### v0.1.2-dev — Merge: backpack silhouette + cursor-lock/panel/worn-equipment fixes
Built in parallel with the silhouette rebuild below on a separate Claude Code
session, discovered on push (same situation as the vitals merge further down).
Real fileID collision again: this session's edit to `Backpack.prefab` (via
`PrefabUtility.LoadPrefabContents` → `SaveAsPrefabAsset`, round-tripping the same
asset) silently reassigned the root GameObject's fileID instead of preserving it —
a new gotcha distinct from the hand-authored-YAML case in the vitals merge. That
reassigned fileID then collided with a `StrapLeft` object the other session
independently created while rebuilding the same prefab into a multi-part
hierarchy. Resolved by taking the other session's full prefab/scene structure as
the base (correct fileID continuity with shared history) and re-applying this
session's changes on top, rather than trying to reconcile two structurally
different versions of the same file by hand.

Also corrected a design mistake caught during the merge: this session's first pass
set `m_Layer` to a new `WornEquipment` layer (excluded from the player's own
`Camera.cullingMask`) directly on the `Backpack` prefab asset. That's wrong — it
would make the backpack invisible even while just sitting in the world, since
nothing ever reset the layer back. Moved the logic into `Backpack.SetCarried()`
instead, toggling the whole hierarchy's layer at runtime (`WornEquipment` while
worn, `Default` on drop/unequip) — the prefab itself stays on `Default`.

Otherwise unchanged from this session's original fixes: clicking on-screen debug
buttons (Equip/craft/Drop) was unusable because any left-click while the cursor was
unlocked immediately re-locked and hid it before the click could register — Escape
now toggles the lock both directions instead of any-click relocking. Debug panels
(Inventory/Skills/Vitals/speed+version) got a shared `DebugGUI` background for
readability, which exposed a real pre-existing overlap between the Inventory,
Skills, and Backpack panel `Rect`s — repositioned to clear each other's edges.
(Also chased and ruled out a *third* apparent bug — Berry Bush, Water Puddle, and
two stick pickups looking like they were floating/overlapping — that was just a
flat featureless plane with no depth cues; verified exact Transform values before
touching anything rather than guessing fixes for things that weren't broken.)

### Backpack silhouette instead of a box (`69a79b8`)
Rebuilt `Backpack.prefab` and its `TestScene` instance as a body + tilted flap
+ two side straps + front pocket (all primitives, same `Backpack.mat`), instead
of one flattened cube. Built via the batch-mode Editor-script workflow — a
throwaway `Assets/Editor` script that composed the hierarchy with real Unity
APIs (`GameObject.CreatePrimitive`, `PrefabUtility.SaveAsPrefabAsset`,
`EditorSceneManager`) and was deleted after — rather than hand-authoring the
multi-child YAML directly. Composing a parent/several-children hierarchy by
hand is exactly the kind of edit that produces silent fileID mistakes (see the
merge entry above); letting Unity allocate the fileIDs itself sidesteps that
class of bug entirely.

### Merge with survival vitals (`91240b3`)
Built in parallel with the vitals work below on a separate Claude Code session,
discovered only on push. Real gotcha, not just a text conflict: both branches
independently added new Player components starting at the same scene fileID
(`1681626235`) — this session's `PlayerCrafting` vs. the other session's
`PlayerVitals`. Git's line-based merge didn't flag it, since the line itself
(`- component: {fileID: 1681626235}`) was identical on both sides — only the
*object it points to* differed. Caught by diffing the full fileID list of both
branches' `TestScene.unity` rather than trusting a clean `git merge` exit.
Resolution: kept `PlayerVitals` at `1681626235`, renumbered
`PlayerCrafting`/`PlayerDropping`/`PlayerBackpack`/`PlayerEquipment` to
`1681626239`–`242`. Also updated `Pickup.cs` and `Backpack.cs` to the
`IInteractable`/`IPunchable` → `GameObject` signature change introduced by the
vitals branch (see below) — `Backpack.cs` wasn't even flagged as conflicted by
git, since the other branch never touched it, so it would have silently failed
to compile if not caught by hand. Validated the merge with a Unity batch-mode
compile check rather than trusting the text merge alone — worth doing for any
future merge that touches `.unity`/`.prefab` files by hand, since those can
"merge cleanly" by git's rules while still being semantically broken.

### Crafting, dropping, and a backpack equipment system (`abb8a3a`)
Click-to-craft (Rock → Rock Knife, training a new Crafting skill), click-to-drop
on any inventory stack, and a carryable/wearable backpack. Extracted a reusable
`Inventory` class (capacity, slots, `HasSpaceFor`) out of `PlayerInventory`,
which is now capped at 4 slots; the backpack is a separate 8-slot container.
`InventoryTransfer.Move()` moves items between any two inventory-capable
objects (`IInventoryHolder`). `PlayerEquipment` adds named equip slots
(starting with "Back") — picking up the backpack stashes it as a regular
inventory item, an Equip button moves it onto the Back slot (visible, worn,
contents accessible), Unequip/Drop reverse that without ever losing contents.

**Consequence of the new slot cap:** `Pickup.Complete` and
`PlayerCrafting.TryCraft` both had to start checking for space *before*
consuming anything — otherwise a full inventory would silently delete a
picked-up item, or eat a crafting input without producing the output.

### Survival vitals: Health, Hunger, Thirst, Stamina, Body Temperature (`ba34403`)
`PlayerVitals` ticks Hunger/Thirst down over real time, drains Health on starvation/
dehydration and regens it when well-fed, and gates sprint on Stamina. Two consumables
(Berry Bush → Hunger, Water Puddle → Thirst, reusable) make the loop testable without
a full item-use/hotbar system.

**Refactor:** `IInteractable.Complete` / `IPunchable.OnPunch` now take the player's
`GameObject` instead of individual component references (inventory, skills, vitals).
The parameter list was about to keep growing with every new player subsystem — third
one (vitals) was the trigger to stop and pass the GameObject instead, letting each
interactable pull what it needs via `GetComponent`.

**Playtesting fixes:**
- Stamina drain/regen initially caused a same-frame flicker between sprinting/not at
  exactly 0 — regen resumed for a single frame, immediately re-enabling sprint, which
  drained it right back to 0, repeating every frame. Fixed with a proper exhaustion/
  recovery hysteresis: once exhausted, sprint stays locked out until stamina climbs
  back to 25, not just `> 0`. Worth remembering as a general pattern for any future
  binary gate driven by a continuously-draining/regenerating value.
- Jumping now costs stamina too (flat cost per jump, not per-second like sprint).
- Berry Bush's color turned out to be a genuinely bad pink/magenta choice
  (`0.55, 0.05, 0.35`), not a rendering bug — spent a round wrongly chasing it as a
  "one-off shader compile glitch" before actually computing what that RGB reads as.
  Changed to a proper deep red (`0.35, 0.05, 0.08`).

### Auto-open default scene when the Editor has none loaded (`600e631`)
Fixes a real onboarding bug: Unity's "last opened scene" state lives in the
gitignored, machine-local `Library/` folder, so a fresh clone opens to a blank
`Untitled` scene — looks like an empty grey world with nothing to move, even though
everything is actually there. `Assets/Editor/SceneAutoOpen.cs` runs on Editor load
and opens `EditorBuildSettings`' first scene whenever none is currently loaded.
Registering `TestScene` in `EditorBuildSettings.asset` (see next entry) was a
prerequisite for this — that setting only affects Player *builds* by itself, not
Editor auto-open behavior.

### Skill-via-use progression + register TestScene in Build Settings (`393bd76`)
`PlayerSkills` tracks per-skill level (0–100) with diminishing gains as level rises
(SCUM-style "slow mastery," per the design brief). Wired into gathering (sticks) and
mining (rock node) via a `trainedSkill`/`skillGain` pair on `Pickup` and
`ResourceNode`. Initial gain values (stick=1, rock=5) were roughly 10x too generous
after playtesting — hitting level 6.9 off 3 actions — tuned down to 0.05/0.5.

Also: `TestScene` had never been added to `ProjectSettings/EditorBuildSettings.asset`
(`m_Scenes: []`), which is what caused a real empty-world bug for a collaborator on a
fresh clone.

### Loot & gathering with punch-to-break resource nodes (`88f51a9`)
`IInteractable` (E to pick up/hold-gather) and `IPunchable` (left-click to break)
interfaces, a minimal `PlayerInventory`. Loose items (sticks) are instant E-pickup;
rocks are punched to break into 3 physical chunks (via `RockChunk.prefab`, with
`Rigidbody`) that scatter and get picked up individually. Originally rocks used
hold-E-to-gather like a generic resource node; changed to punch-to-break per explicit
request, tying into the design brief's Basic Combat pillar.

**Gotcha found and fixed in the same commit:** the project had the URP package
installed and URP-only shaders on every material, but `ProjectSettings/GraphicsSettings.asset`
had no pipeline asset assigned (`m_CustomRenderPipeline: {fileID: 0}`) — so everything
was still rendering under the Built-in pipeline, which shows pink for any shader it
doesn't recognize. Created `Assets/Data/URP-Asset.asset` + `URP-Renderer.asset` and
wired them into Graphics Settings. (A related but distinct bug hit later: a Material
created at runtime via `new Material(...)` embeds fine into a *scene* file but not
reliably into a *prefab* — the `RockChunk` prefab's chunks rendered pink until the
material was saved as a real `.mat` asset first, then referenced.)

### Add first-person player controller (`1d02e9a`)
`FirstPersonController.cs` — `CharacterController`-based WASD move, mouse look,
sprint, jump — using the new Input System directly (`Keyboard.current`/`Mouse.current`),
no `.inputactions` asset. `ProjectSettings/ProjectSettings.asset` already had
`activeInputHandler: 1` (new Input System only) from project creation.

### Add minimal test scene (`bc460c6`) / project scaffold (`d2e9641`)
First scene in the repo — ground plane, directional light, camera — built via Unity
batch mode (`Unity.exe -batchmode -nographics -quit -executeMethod ...`) rather than
the Editor GUI, since these sessions run headless. The general pattern used throughout
this project: write a throwaway `Assets/Editor/*.cs` script, run it via batch mode,
verify the result by grepping the saved `.unity`/`.asset` YAML, then delete the script
— keeps the repo free of one-off setup code while still allowing scene edits without
a human driving the Editor UI.

### Unity version: 6.3 LTS, not 6.0 LTS (`78d0c44`)
Originally targeted 6.0 LTS (`6000.0.32f1`), but that version has a disclosed
vulnerability (CVE-2025-59489, patched at `6000.0.58f2`+). Since nothing had been
built yet, moved to 6.3 LTS instead of just patching 6.0, for a longer support runway
at the same near-zero switching cost.

### Design doc reconciliation (`0e4b1a2` through `adfd358`)
Ben's `game-overview.md` (narrative/setting pitch) and this repo's `design-brief.md`
(systems/technical brief) started as independent docs and were reconciled — magic
system, currency ladder, real-Earth-vs-replica, factions/guilds/warbands split. See
`docs/reconciliation-questions.md` for the decisions made.

### Initial commit (`7e8f5d5`)
Repo scaffold + `game-overview.md`. Predates the Unity project itself — see
`docs/design-brief.md` for the full systems design.
