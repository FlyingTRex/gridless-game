# Working On

What's actively in progress right now, one line per active session. Check this
before starting new feature work — if something here overlaps what you're about
to build, coordinate before duplicating effort (see the Waterskin/Canteen
collision in `CHANGELOG.md`, 2026-08-02, for what happens when this doesn't get
checked).

Add a line when you start a non-trivial feature; remove it once merged to
`origin/main`. Stale entries are worse than none — if you're not sure whether an
entry is still active, ask before trusting it.

Format: `- YYYY-MM-DD — who — one-sentence description`

- 2026-08-13 — Ben — Skill books, MVP2 item 7 (design + build starting
  same day). Full design in `SKILL_BOOKS_PLANNING.md` — reading grants a
  bounded head start (a specific `CraftingRecipe`/`WishRecipe` exception,
  or a lineage unlock for magic), writing risks a bounded failure
  (reuses `PlayerCrafting`'s `CraftOutcome` roll, margin = author's
  Intelligence vs. the subject's tier). 16-step build order across 6
  phases, cross-referenced against `MVP2_PLANNING.md` (advances item 1's
  Intelligence trigger for free, item 7's NPC-training phase blocked on
  item 2's bench-crafting, creates a small follow-up for item 6's save
  system once built). **Phase 1 item 7 (models) done**: Book/Scroll/
  Paper/Ink generated via a new Blender script
  (`Tools/Blender/GenerateSkillBookModels.py`, Ben's call over the usual
  Tripo3D pipeline), imported into `Assets/Models/`, measured via
  batch-mode script per `CLAUDE.md`'s mandatory bounds-check — all four
  land exactly on their intended real-world size and are correctly
  grounded. Two real bugs caught and fixed live: `Material.diffuse_color`
  alone doesn't drive the actual EEVEE render (needs explicit Principled
  BSDF node wiring), and `primitive_cube_add(size=1)` scaled by
  `length/2` produced exactly half the intended size for Book/Paper — both
  caught by measurement, not assumed correct.
  **Phase 0 + most of Phase 1 done, same session:** `PlayerMagic`
  (`StartingLineage` → a real `knownLineages` set + `LearnLineage`),
  `CraftOutcome`/`RollOutcome` extracted into a new shared
  `CraftOutcomeRoll.cs` (`PlayerCrafting` calls it exactly as before —
  no behavior change), new `SkillBook : IEquippable` (per-instance
  `TargetRecipe`/`TargetWish`/`BonusLevel`, mirrors `Canteen`'s
  Stash/SetCarried shape), `bookGrantedRecipes`/`bookGrantedWishes`
  exception sets wired into `HasRequiredSkill`/`CanAttempt`. New
  `ItemDefinition`s + pickup prefabs + baked icons for `SkillBookItem`
  (Book model, shared by both crafting/weapon and magic targets — Scroll
  stays unused, reserved for a future separate item), `Paper`, and `Ink`.
  **Phase 2 (writing) done too, same session:** new `PlayerWriting.cs` +
  `WritingScreen.cs` — a new "Writing" tab in `PlayerMenuScreen` (Tab
  key, no new keybinding) listing every recipe/wish the player currently
  knows, each with a Write button; consumes 1 Paper + 1 Ink per attempt,
  rolls via the shared `CraftOutcomeRoll` (margin = Intelligence vs. the
  subject's tier), spawns a real `SkillBook` on anything but a failure,
  damages the author 2–10 on `SpectacularFailure`, grants Intelligence
  XP scaled by outcome (0.5/1.5/3, first-pass numbers). Verified via 6
  rounds of batch-mode compile (0 CS errors) + direct YAML grep of the
  new scene wiring (`PlayerWriting`/`WritingScreen` on the Player object,
  all 4 references correctly set).
  **Phase 3 (reading) done too, same session — the loop is now closed
  end to end:** new `PlayerReading.cs`. A `SkillBook` is equipment-backed
  (not a plain stackable item), so Read hooks into `InventoryScreen`'s
  `pendingActionEquipment` popup the same way Canteen's Drink/Fill
  already do there, rather than the originally-sketched
  `PlayerEating.TryEatFrom` shape (that only fits plain-item
  consumables) — a real mid-build design correction, not just an
  implementation detail. Grants the recipe/wish exception
  (`PlayerCrafting.GrantRecipe`/`PlayerMagic.GrantWish`, plus
  `LearnLineage` first for a magic target), a small Intelligence tick
  (0.25), then permanently destroys the book. `PlayerReading` needed
  manual scene wiring (confirmed via grep that nothing
  RequireComponents its sibling group — `PlayerEating`/`PlayerMedicine`
  are added directly in the scene too, not chained), unlike
  `PlayerWriting`/`WritingScreen` which rode `PlayerMenuScreen`'s
  existing `RequireComponent` chain automatically. Verified via 8 rounds
  of batch-mode compile (0 CS errors) across the whole build so far +
  direct YAML grep of every new asset/prefab/scene reference.
  **Phase 5 (sourcing) done too, same session:** player-to-player trade
  confirmed free (existing pickup/drop flow); real Paper/Ink source —
  `PaperRecipe` (1 Plank → 4 Paper) and `InkRecipe` (2 Berry → 1 Ink),
  both Crude/no-skill-gate, registered on `PlayerCrafting.recipes`;
  "random world drops" placed as two found `SkillBook`s directly in
  `TestScene.unity` (one targeting `MasterworkKnifeRecipe`, one
  `SparkWish`) — a `StorageBox` turned out unable to be pre-filled at
  scene-authoring time at all (its `Inventory` is created fresh in
  `Awake`, never serialized), so a bare world `SkillBook` replaced the
  originally-planned pre-filled-box stopgap. Rare magic-teaching NPCs
  stay explicitly deferred.
  **Real bug caught and fixed placing the found books**:
  `SkillBook.TargetRecipe`/`TargetWish`/`BonusLevel` were plain C#
  auto-properties, not `[SerializeField]` — invisible to Unity's scene
  serializer, so a book placed directly in a saved scene silently lost
  its target on reload (never affected a book written/read within one
  Play session, which lives entirely in memory). Fixed with real
  `[SerializeField]` backing fields — then hit a *second*, related trap:
  even with that fix, a plain C# field assignment on a prefab instance's
  component still didn't register as a serializable override without an
  explicit `PrefabUtility.RecordPrefabInstancePropertyModifications`
  call. Both caught by directly grepping the saved scene YAML for the
  actual `propertyPath:` entry, not by trusting "the script logged
  success" — new general-purpose gotcha written up in `CLAUDE.md` for
  any future batch-mode script doing the same thing.
  **Phase 6 (verification) done too, same session — build is feature-
  complete except the one blocked phase:** final comprehensive batch-mode
  compile + YAML sweep across every Phase 0–5 artifact, all clean. New
  `TEST_FEATURE_PLAN.md` section 31 written — a real manual Play-mode
  checklist (write/read loops for both crafting and magic, the two
  pre-placed found books, both failure/success extremes, Intelligence
  training, Writing tab UI regressions). Only Phase 4 (NPC training)
  remains unbuilt, correctly blocked on NPC bench-crafting existing.
  **v0.3.53-dev, committed and pushed** (also added an Intelligence tile
  Reading & Writing sub-line after Ben's first live look flagged it
  missing next to Strength's Encumbrance line). **Not yet live-tested in
  Play mode** — every check so far has been compile + YAML grep only,
  same status save/load carried until Ben's own live round-trip
  confirmed it. That live pass is the natural next step before this can
  be called done.
- 2026-08-13 — Ben — Full equipment-visual sweep: every `IEquippable` now
  bone-attaches (Boot/Belt/Canteen/Sunglasses/Face Shield/Health Monitor/
  Nav Computer/Shirt/Jeans, plus a real bug fix in `PlayerLoot` that
  explains why the Pickaxe was invisible — see `CHANGELOG.md`'s
  v0.3.41-dev entry for full detail). v0.3.41-dev, **committed and
  pushed.** Triggered by Ben's live report ("backpack and sneakers are
  not aligned properly, pickaxe isn't wired to the hand") + an explicit
  ask to fully audit before implementing further — ran an `Explore` agent
  across every `IEquippable` carrier first, found the real bug
  (`PlayerLoot.ReceiveEquipment`'s hand-fill branch bypassed
  bone-attachment entirely) plus 9 more types never touched by the
  earlier bone-attach work, then confirmed scope ("all 9 remaining types
  now") before building. New `EquipmentAttach.Carry()` shared helper
  dedupes the resolve-bone/SetCarried/Place pattern across all 11
  carriers.
  **Live-feedback round 2 (v0.3.42-dev, committed and pushed):** Boots
  confirmed correctly at the feet; Backpack sat far too high (near the
  neck, not the back) — root cause identified (`HumanBodyBones.Chest`
  sits high on the rig, original offset never pushed downward), fixed
  with a real `y: -0.3` correction, mirrored onto the 3 NPC job assets to
  keep player/NPC placement consistent. Belt also looked off in the
  screenshot but left untouched — plausibly just rear-angle camera
  occlusion, not a confirmed bug.
  **Live-feedback round 3 (v0.3.43-dev, committed and pushed):** the
  Backpack's bag body now sits correctly (the Y fix worked), but a
  blue/black shape kept jutting up past the head — theory at the time:
  the same rigid Backpack model has a bedroll-style top extension, fixed
  with a `-90°` X (pitch) addition. Boots also got a 180° yaw trial.
  **Live-feedback round 4 (v0.3.44-dev, committed and pushed):** the
  round-3 Backpack pitch fix had zero visible effect on the floating
  shape — meaning it was misdiagnosed, never the Backpack. Re-diagnosed
  as Jeans (color matches denim) — `-90°` X pitch fix applied there.
  **Live-feedback round 5 (v0.3.45-dev, committed and pushed):** the
  Jeans `-90°` was real progress (moved from "above the head" to
  "sideways near the hand") — doubled to `-180°` to finish the swing to
  pointing down. Also attempted a fix for a real bug (dropping a worn
  Belt didn't drop its clipped Canteen) — but that fix landed in the
  wrong place (`PlayerBelt.Drop`, not the actual path the UI's Drop
  button uses).
  **Live-feedback round 6 (v0.3.46-dev, committed and pushed):** found
  the real Belt/Canteen drop path (`PlayerDropping.DropFrom`, called
  directly by `InventoryScreen.DrawItemDropPopup`) and moved the cascade
  fix there, generalized to any `IInventoryHolder` equippable. Also
  reverted a real mistake: the round-3 Backpack `-90°` X pitch was based
  on a wrong diagnosis (the floating shape was Jeans, not part of the
  Backpack) and was never undone once that was discovered — it broke an
  already-correct Backpack rotation. Reverted to yaw-only.
  **Live-feedback round 7 (v0.3.47-dev, implementation complete, not yet
  committed/pushed):** Backpack revert confirmed correct (matches a
  reference photo Ben supplied). Boots' `180°` yaw trial from round 3
  made the shoes look jumbled/overlapping — same mistake pattern
  (speculative rotation added on request to already-working placement,
  never reverted once it didn't help) — reverted to identity.
  **Live-feedback round 8 (v0.3.48-dev, implementation complete, not yet
  committed/pushed):** Boot's "confirmed correct" from round 7 turned out
  wrong too — Ben's actual complaint was pitch, not yaw ("shoes should be
  parallel with the feet... not perpendicular," with a reference photo).
  Re-diagnosed as the same ground-lying-vs-mounted pitch mismatch already
  fixed on Backpack/Jeans — applied the same `-90°` X correction, yaw
  removed entirely. Backpack also got a precise, non-guessed instruction
  this round ("rotated on the vertical axis 90 degrees") — added that 90°
  yaw on top of the existing 180°, mirrored onto the 3 NPC job assets.
  **Live-feedback round 9 (v0.3.49-dev, committed and pushed):** first
  fully-positive confirmation in this whole chain — Backpack rotation
  confirmed correct, only needed raising (`y: -0.3` → `-0.15`), mirrored
  onto the 3 NPC job assets. **Backpack is done.** Boots: the pitch fix
  was real progress (no longer standing on its end) but still wrong
  (facing sideways, stacked front-to-back) — switched approach rather
  than keep guessing: Ben is now live-tweaking `PlayerBoot`'s
  `wornPositionOffset`/`wornEulerOffset` directly in the Play-mode
  Inspector.
  **Live-feedback round 10 (v0.3.50-dev, committed and pushed):** both
  Boot and Backpack got live-tweaked values baked in as the new script
  defaults. Boot: `(0, -0.93, 0.35)` / `(0, 90, 0)` — a **yaw** fix, not
  the pitch this session's guessing kept assuming; "looks closer," not
  yet declared final. Backpack: `(0, 0.05, -0.18)` / `(0, -90, 0)` —
  refines round 9's guess with a precise value, confirms the round-8
  yaw instruction was exactly right; mirrored onto the 3 NPC job assets.
  **Backpack effectively done** (two confirmations in a row). **Boot
  still open** — "closer," not confirmed final. Belt/Canteen drop
  cascade not yet re-verified live. The other 7 types (Sunglasses,
  Mining Face Shield, Personal Health Monitor, Navigation Computer,
  Shirt, Jeans, Canteen worn-position) still have zero live
  confirmation. See `TEST_FEATURE_PLAN.md` section 29.
- 2026-08-13 — Ben — Player equipment now bone-attaches too (same
  RightHand/Chest system NPCs just got, applied to the player's real
  `Tool`/`Backpack` carry objects instead of a decorative copy). v0.3.40-dev,
  **committed and pushed.** New `EquipmentAttach.cs` (shared placement
  math, `NPCEquipmentVisual` refactored to use it too), `PlayerBodyModel.
  GetBone`, `PlayerTool`/`PlayerBackpack` resolve anchors through it with
  gender-switch re-anchoring. Superseded/expanded by the full sweep above
  the same day. Full detail in `CHANGELOG.md`'s v0.3.40-dev entry.
- 2026-08-13 — Ben — NPC equipment visual attachment: Pickaxe/Axe/Mining
  Face Shield/Backpack now render on the NPC model instead of being pure
  bookkeeping. v0.3.38-dev then v0.3.39-dev (live-feedback fix round, same
  day), **both committed and pushed.** New `NPCEquipmentVisual.cs`
  (bone-attaches each given tool's own `worldPickupPrefab`, RightHand/
  Head/Chest per a new `ToolRequirement.attachBone` field), added to both
  `NPCFactoryWorkerMale/Female.prefab`. Also fixed two live bug reports
  from earlier the same session: (1) `MineOreJob`/`ChopWoodJob`/
  `ForageJob`'s Backpack (and Mining's Pickaxe) tool requirements only
  listed one `CraftTier` variant instead of all 5 — a Fine Backpack
  silently couldn't be given, which also explains why the NPC never
  actually mined or animated (its job could never reach Ready) —
  v0.3.36-dev; (2) a Mining NPC got "stuck gathering sticks" because the
  loose-`Pickup` collection pool (meant only for Forage) was scanned
  unconditionally for every job — new `NPCJobDefinition.
  collectLoosePickups` gates it to Forage only — v0.3.37-dev.
  **v0.3.39-dev fix round** (Ben tried it live: Pickaxe didn't show at
  all, Backpack showed but misplaced): the Pickaxe wasn't just
  misplaced — `Tool.cs` requires `Rigidbody`/`Collider`, and the original
  code tried to `Destroy()` exactly those, which Unity silently refuses
  when something still requires them, leaving a live non-kinematic
  Rigidbody that likely fell away under gravity; fixed by disabling
  physics instead of destroying it. Position/rotation offsets were also
  being interpreted in each attach bone's own unpredictable local space
  instead of relative to the NPC's root — fixed so "0.15 behind" reliably
  means behind the character regardless of which bone it's parented to.
  Verified via compile + YAML grep only so far — **still no live
  Play-mode confirmation**, see `TEST_FEATURE_PLAN.md` section 27. Full
  detail across all versions in `CHANGELOG.md`.
- 2026-08-13 — Ben — Player body Male/Female toggle in the ` menu's
  Player tab (MVP2 item 4, direct follow-up to the player-visible-body
  entry below). v0.3.35-dev, **committed and pushed.** New `PlayerBodyModel.cs`
  (both gendered `Visual` instances pre-instantiated, `SetActive`-toggled,
  not created/destroyed at toggle time), small setter additions to
  `PlayerAnimatorDriver`/`NPCVisualGroundFix`, two tab-style buttons in
  `GameMenuScreen.DrawPlayerTab()`. Verified via compile + YAML grep only
  so far — **no live Play-mode test yet**, see `TEST_FEATURE_PLAN.md`
  section 26. Full detail in `CHANGELOG.md`'s v0.3.35-dev entry.
- 2026-08-13 — Ben — NPC animation, MVP2 item 4 (scope narrowed to NPC-only).
  v0.3.33-dev, **committed and pushed to origin/main** (confirmed —
  fetch shows origin/main at the same commit; an earlier push report of
  "remote rejected" turned out to be a transient ref-lock race, not a real
  collision — all LFS objects had already uploaded and the ref updated
  moments later). **No live Play-mode test yet**, see
  `TEST_FEATURE_PLAN.md` section 24.
- 2026-08-13 — Ben — Player visible body + first/third-person camera
  toggle (MVP2 item 4, player half). v0.3.34-dev, **committed and pushed
  to origin/main** (confirmed — local `HEAD` and `origin/main` both at
  `fb158df`). Full detail in `CHANGELOG.md`'s v0.3.34-dev entry. **No live
  Play-mode test yet**, see `TEST_FEATURE_PLAN.md` section 25 — remove
  this line (and the NPC animation line above it, same status) once a
  Play-mode pass covers both.
- 2026-08-13 — Ben — NPC job generalization: Woodworking + Berry/Herb
  foraging (v0.3.32-dev, **committed and pushed**). Full design in
  `NPC_JOB_GENERALIZATION_PLANNING.md`,
  built same day: `NPCMining.cs` renamed to `NPCGathering.cs` (functional
  rename-only, script GUID preserved so existing prefab references
  survived), a new `INPCHarvestable` interface lets `ChoppableTree`
  (standing Trees) join `ResourceNode` as a valid gathering target
  (direct-yield for NPCs, scatter behavior unchanged for players), a new
  `INPCSearchable` interface lets `BerryBush`/`HerbBush`'s search action
  (chop-for-stick skipped, player-only) trigger for NPCs too, and
  `Pickup.cs` gained an NPC-safe collection path so a foraging NPC can
  actually walk over and pick up whatever a search scattered — closes the
  loop with no new state machine, just a third scanned target pool.
  `ChopWoodJob.asset`/`ForageJob.asset` wired into `NPCJobScreen`.
  Deliberately deferred: bench-crafting families (Metalworking, Sewing,
  etc. — section 7 of the planning doc). Verified via compile + YAML grep
  only so far — **no live Play-mode test yet**, see `TEST_FEATURE_PLAN.md`.
- 2026-08-13 — Ben — Furnace real state + unattended automation
  (v0.3.31-dev, **committed and pushed**). Applied the Campfire
  treatment (E-key popup, `FurnaceScreen`, real Fuel/Materials/Output
  inventories) plus three new asks: an up-to-4 sequential smelting queue
  (new `SmeltableItem` type, deliberately separate from `CraftingRecipe`
  — see `CHANGELOG.md`), a player-selectable Output StorageBox, and
  player-designated nearby StorageBoxes for Fuel/Materials that the
  Furnace auto-pulls from. Confirmed via 3 `AskUserQuestion` items before
  building: sequential (not parallel) queue, **true unattended
  automation** (Furnace ticks fuel/queue/auto-feed every frame regardless
  of player presence — pulls forward part of `WOOD_AND_FUEL_PLANNING.md`
  section 5's "autonomous production chain" vision), on-board slots +
  auto-refill/drain (not a raw passthrough to the linked boxes). Verified
  via batch-mode compile (0 CS errors) + YAML grep of the saved scene/
  asset. **Not yet tested in Play mode** — `TEST_FEATURE_PLAN.md` needs a
  new section for this.
- 2026-08-12 — Ben — Campfire rebuild shipped (v0.3.26-dev). Full design
  in `CAMPFIRE_PLANNING.md`, built in 4 approved chunks: real craftable/
  placeable `BuildPiece` (Crude unlock, 4 Rock + 3 Stick), two ways to
  light it (E, tool-free, or the original Spark wish — now an alternate,
  not the only way), a real fuel system (1 slot, reuses `FuelTier`/
  `FuelItem` from the Furnace work, burns down and auto-extinguishes), a
  real cooking system (new `CookableItem` type, Raw Meat → Cooked Meat,
  auto-cooks while lit and the player's nearby), and Body Temperature's
  first real gameplay effect (warmth) plus a spot on the real HUD.
  `InventoryScreen` gained a "Campfire (nearby)" section for loading
  fuel/food. Deliberately deferred, per the plan: the Blender model
  rebuild (still the pre-Blender placeholder, not started), the 4
  accessory items + slots (Grill/Soup Pot/Kettle/Frying Pan — need
  models/icons before they're usable, noted in `CAMPFIRE_PLANNING.md`),
  Wood Stove, and the water-safety mechanic. Implementation complete:
  verified via YAML grep throughout, one real bug caught and fixed
  mid-build (a stale `ItemDefinition` reference across a
  `PrefabUtility.LoadPrefabContents` cycle — the exact CLAUDE.md-documented
  gotcha), throwaway scripts deleted, batch-mode compile check passed
  (0 CS errors) after every chunk.
  **Live-testing finding, same day:** the "Campfire (nearby)" fuel-loading
  UI is a real discoverability failure (an unlabeled row at the bottom of
  an already-busy Inventory scroll) — decided replacement is a focused
  popup opened by E (see `CAMPFIRE_PLANNING.md` and
  `BUGS_AND_ENHANCEMENTS.md`'s new "Campfire dedicated popup UI" section),
  **design decided, explicitly not built yet** (Ben's call: "let's wait on
  doing this further" for now) — current embedded mechanism stays in place
  as a working stopgap. **Still needed:** manual Play-mode pass — see
  `TEST_FEATURE_PLAN.md` section 21 (written against the current
  mechanism; will need a follow-up pass once the popup replaces it).
  **2026-08-13 update:** the Blender model rebuild (previously deferred)
  is now also done — v0.3.27-dev, ring of 8 rocks + 6 charred sticks,
  reused `RockChunk.mat` for rocks, generated new charred/ember-glow
  materials for the wood via the `SmoothThreshold` technique, script kept
  at `Tools/Blender/GenerateCampfireModel.py`. Full writeup in
  `CHANGELOG.md`. Verified via YAML grep + rendered preview screenshots,
  not yet checked live in Play mode.
  **2026-08-13 update 2:** the dedicated E-key popup UI (previously
  deferred) is now also done — v0.3.28-dev, new `CampfireScreen.cs`
  (same family as `LockboxScreen`), old "Campfire (nearby)" Inventory-tab
  section removed entirely (fully superseded, along with the now-unused
  `Campfire.Active`/`FindNearby`). This closes out everything logged
  above except the 4 accessory items, Wood Stove, and the water-safety
  mechanic. **Still needed:** manual Play-mode pass for both the model
  and the new popup — `TEST_FEATURE_PLAN.md` section 21 needs a rewrite
  against the new E-key-opens-popup flow, not just a re-check.
- 2026-08-12 — Ben — Set up a second texture/model API alongside
  `Tools/Tripo3D/`: `Tools/TextureAPI/` (3D AI Studio). Confirmed it's
  genuinely Tripo's own texturing tech exposed through a different
  endpoint (`/v1/3d-models/tripo/texture-model/`), not an unrelated
  vendor. Scaffolding + `Generate-Texture.ps1` built and documented
  (`Tools/TextureAPI/README.md`), API key stored in a gitignored `.env`
  (verified via `git check-ignore`, never committed). **Not yet tested
  against a real API call** — response field names inside the `results[]`
  array are a best-effort guess from the docs, script dumps raw JSON for
  diagnosis on first real use.
- 2026-08-13 — Ben — Campfire cooking system reworked again (v0.3.30-dev,
  **committed and pushed**). Grew out
  of Ben looking at the just-shipped v0.3.28-dev popup live and asking
  for more, in sequence: 4 Cooking Utensil boxes (Grill/Cooking Pot/
  Kettle/Frying Pan, one item each), a wood-only Fuel box, a Transfer
  section scoped to exactly Backpack + Hands, then — once he saw the
  utensil boxes — a bigger ask: 4 output boxes for cooked items, a
  multi-slot ingredient input area, and a Recipe button that only shows
  what's currently cookable given the loaded utensils/ingredients.
  Confirmed via `AskUserQuestion` before building: 4 input slots, single
  cook at a time (4 output boxes are just banked storage, not parallel
  cooking), and Recipe is a manual trigger (real philosophy change from
  the old always-auto-cook design). `CookableItem` restructured to
  mirror `CraftingRecipe`'s `ingredients[]`/`outputItem` shape;
  `RawMeatToCookedMeatCookable.asset` migrated in place. 4 new plain
  `ItemDefinition`s created (Grill/Cooking Pot/Kettle/Frying Pan — no
  model/icon yet, admin-spawnable, same known-placeholder gap
  `CAMPFIRE_PLANNING.md` already flagged). `CampfireScreen.cs` rewritten
  from button-based to real drag-and-drop — a self-contained
  implementation mirroring `InventoryScreen.cs`'s interaction pattern,
  not a literal shared-code extraction (avoided touching that heavily-
  tested file for a screen that needs none of its 11-equippable-type
  dispatch logic). One infra hiccup: a batch-mode Unity process hung
  after colliding with a leftover `bee_backend` process from an earlier
  interrupted run; confirmed with Ben before killing the stuck PID, then
  a fresh invocation compiled clean. Verified via compile checks + YAML
  grep only so far — **no live Play-mode test yet**, and this is a much
  bigger surface area than the earlier single-chunk changes, so that
  pass matters more than usual before calling it done. Full writeup in
  `CHANGELOG.md`'s v0.3.30-dev entry; `TEST_FEATURE_PLAN.md` section 21
  still needs a rewrite for this flow (currently written against
  v0.3.28-dev's simpler button UI).
  **Two live-feedback fixes, same day, before any test pass completed:**
  (1) the Ingredients/Cooked Items grid had zero `GUILayout.Space`
  between boxes, so 4 adjacent empty slots visually merged into one
  solid rectangle instead of reading as 4 separate boxes — fixed with a
  consistent `BoxGap` (8px) applied between every box in the grid, the
  Utensils row, and the Hands row. (2) The popup's fixed 520x640 panel
  didn't fit Ben's screen, and his touchpad has no working scroll
  gesture in this window, so the overflow content (and the Close button)
  was genuinely unreachable, not just inconvenient — panel width/height
  are now responsive (`Mathf.Min(max, Screen.dimension * 0.92f)`, same
  pattern `PlayerMenuScreen.DrawScrollable` already uses), so it shrinks
  to fit rather than relying on scrolling to cover the gap. Neither fix
  has been visually confirmed yet (I can't screenshot a live IMGUI popup
  the way I could the 3D Blender model — this needs Ben's own eyes).
