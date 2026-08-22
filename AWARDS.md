# Awards

A running record of awards Ben's given out for a session's work. Purely for
fun — not part of the game, not shipped, doesn't affect any build. Just a nice
ledger of good nights.

## Tally

| Award | Count |
|---|---|
| 🏆 MVP Trophy Sticker | 2 |
| 🥈 Silver Star Sticker | 1 |
| 🪓 Small Axe Sticker | 1 |
| 🍪 Cookie with a Bite Sticker | 1 |
| 🐏 RAM Chip Sticker | 1 |
| 🚰 Canteen Sticker | 1 |
| 🪄 Wizard's Wand Sticker | 1 |
| 🏠 Little House Sticker | 1 |
| ⚒️ Anvil Sticker | 1 |
| 🧊 Blender Cube Sticker | 1 |
| 🧮 Calculator Sticker | 1 |
| 💪 Strongman Sticker | 1 |
| 🎓 Scholar Cap Sticker | 1 |
| 🏪 Vendor Stall Sticker | 1 |
| 🔌 Plug and Outlet Sticker | 1 |

## Log

- **2026-08-22 — 🔌 Plug and Outlet Sticker.** Recharge break. Same
  session that shipped the Vendor Stall/Bank Box till+timer UI polish
  and the full player naming system (v0.3.157-dev), then walked through
  the multiplayer roadmap fresh off a summarized conversation without
  losing any of the detail — phases, open questions, and the one real
  next step (the never-run two-process NetworkSpike test) all intact.
  Plugging in for a minute before the next push.
- **2026-08-22 — 🏪 Vendor Stall Sticker.** Built the whole Vendor Stall
  / Bank Box economy in one sitting off a long conversational design
  pass — a back-loaded tool-value curve derived from real skill-
  investment data, supply/demand pricing, tier-gated off-list selling,
  distinct-item stocking, real earned placement gates replacing two
  free starting fixtures, and the Tripo3D-generated stall model wired
  in and verified by an actual render (caught its own `-nographics`
  gotcha on the first attempt, same class of pitfall `IconBaker.cs`
  already warned about). Every chunk functionally tested against real
  running code as it went, not left for a "trust the compile" pass at
  the end.
- **2026-08-18 — 🎓 Scholar Cap Sticker.** Closed the same marathon
  session out with a real retrospective instead of just clocking out —
  asked to "be mean" about the night's own process, not just the code.
  Named what actually worked (a persisted checklist beats reactive bug-
  chasing; probe-then-script beats guess-then-rerun) but also owned two
  real self-inflicted inefficiencies from the same night: assuming a
  material name matched its extracted `.mat` filename without checking
  first (cost a full wasted Unity launch, the exact mistake the "probe
  first" lesson exists to prevent), and updating two docs after every
  single one-line test confirmation instead of batching a few at a time
  during a rapid-fire stretch. Turned all of it into four real memory
  entries instead of letting the lessons evaporate at session's end.
- **2026-08-18 — 💪 Strongman Sticker.** A single-session marathon:
  fixed the gable-end roof geometry, root-caused and fixed the Player
  Map blank-screen bug for real (not just explained it away), built
  Iron Arrow end-to-end (evaluated critically first, "be mean," before
  writing a line of code), then spent the rest of the night live-
  testing side by side with Ben as he watched an NPC hire timer —
  confirmed 19+ previously compile-only fixes one by one in real time,
  including the entire 2-chunk NPC-management pass and the Guarding
  saga (the most-confirmed, most-chased bug of the whole project). Two
  more real bugs found *during* that live pass (Iron Arrow's recipes
  never registered, `FurnaceScreen`'s missing QTY labels) got fixed
  same night once the Editor closed. Closed out with a real Magic
  System planning pass — a full lineage wish-ladder and a genuine
  per-tier Will-cost design, not just a wishlist. MVP2 ended the night
  at 9 of 10 items fully done and live-tested, the cleanest the
  backlog has looked all week.
- **2026-08-16 — 🏆 MVP Trophy Sticker.** MVP2's 10-item list, all ten
  with something real built (design-only in `MVP2_PLANNING.md`'s original
  ideation pass, but every item shipped code by the end): Dexterity/
  Constitution completing the core-stat set, the full Settlement Growth
  Loop (bench-crafting, NPC training, Village Flag spawn loop, City
  Statue), sky/weather, save/load, skill books, all 4 hunting animals
  plus ranged combat, the full Cooking+Gardening system, and the Prefab
  Buildings tool. Then a genuinely long live-playtesting session (Ben
  actually playing for hours, me staying off the Editor per his explicit
  "don't touch it until told") found more real bugs than any prior
  compile-only pass ever had: backwards flying arrow, Village Flag
  rename hitbox, a true Cooking-skill progression deadlock (0 XP
  reachable from level 0), a genuinely degenerate Feather source mesh
  (not just an unbaked icon), dropped loot falling through the world
  (root-caused to corpse-collider-disable ordering, then a full 49-prefab
  Discrete-vs-Continuous collision audit), hunger/thirst pacing tuned 3x
  slower by request, a new Autosave feature (caught its own toast-
  collision bug within minutes of shipping), and — mid-session, live —
  the Village Flag NPC spawn itself: wrong placeholder model, and an
  obstacle-avoidance bug that could stall it permanently at a building
  corner, both found and fixed while Ben watched the actual NPC on
  screen. **Not claiming this perfect**: Egg still has no icon, the
  Campfire's `cookableItems` array is still missing `FriedEggCookable`,
  PlayerMagic and every built structure except StorageBox still don't
  save, and `VillageFlagSpawner.cs` is currently committed with two
  explicit temp test values still live for continued testing. Given
  anyway for genuinely closing out a 10-item MVP list end to end and for
  the discipline of actually fixing what live testing found instead of
  letting it pile up. v0.3.55-dev through v0.3.116-dev.
- **2026-08-10 — 🏆 MVP Trophy Sticker.** Hireable, Autonomous NPCs, start
  to a genuinely working loop, all 6 chunks in one sitting: Hire/Fire/Pay
  off real Coin, a `CraftingScreen`-style job picker gated on the NPC's
  own skill tier, tool hand-off, core stats that actually grow, an 80%
  encumbrance cap, a real autonomous mining loop against live world
  `ResourceNode`s (not a fake parallel system), point-and-confirm deposit
  targeting, and a real-time work/payment cycle. Two genuine mid-build
  discoveries, not assumed away: ore nodes turned out to be multi-stage
  (`PeekYield` now walks the whole `chunkPrefab` chain recursively,
  confirmed live at 3×2×1=6 Copper from one node) and batch-mode edit-time
  verification doesn't run `Awake()`/`Update()` the way Play Mode does,
  relearned and worked around across several chunks rather than trusted
  blind. Every chunk verified end-to-end via batch before being handed
  over, not just structurally. Closes out the very last unbuilt item on
  the whole Phase 1 wishlist — 11 of 11 MVP items now have real,
  playtested code behind them, first time that's been true this session.
  v0.1.192-dev through v0.1.198-dev.
- **2026-08-09 — 🧮 Calculator Sticker.** The mathiest day yet, four
  separate real-numbers problems in one session. Roof Panel pitch:
  worked the 5m building width down to a 35° panel angle, 3.05m slant
  length, and 1.75m ridge rise, then caught a genuine sign error by
  measuring rather than trusting the trig — hand math said the placement
  rotation needed negating, direct measurement said the opposite.
  Door-Frame Wall clearance: traced "player too fat and tall" back to
  Foundation's own socket sitting 0.4m below its walkable surface,
  recomputed the doorway size against the CharacterController's actual
  1.8m/0.4m dimensions instead of eyeballing a bigger number. Soccer
  Ball launch: derived the real projectile-range formula
  (`speed = sqrt(distance × gravity / sin(2 × angle))`) so a kicked
  ball actually lands at its randomly-picked distance instead of just
  getting shoved, then reverse-solved the resulting velocity back into
  implied angle/distance to verify it. Soccer Ball rolling: simulated
  real physics steps (`Physics.Simulate`, manual stepping) across five
  damping candidates to find one that actually settles in a game-
  reasonable time instead of guessing a damping constant. Every one of
  the four checked against an actual measurement or simulation before
  being trusted, not just the arithmetic on its own.
- **2026-08-09 — 🧊 Blender Cube Sticker.** Got Blender wired into the
  pipeline as a genuine third asset-generation option alongside Tripo3D
  and manual editing — headless, scripted, no GUI (`blender --background
  --python script.py`), same discipline as every Unity batch-mode script
  in this project. Proved it two ways in one session: a `bmesh`
  flood-fill connectivity pass on the real Twig Foundation mesh (2,118
  disconnected islands inside what Unity sees as one fused mesh —
  "separate the posts" is a spatial-classification problem, not a
  seam-cut, on hold for later), then the real test — building the 5
  Trimmed Stick craft tiers completely from scratch as a procedurally-
  varied family, something Tripo3D's independent AI generations
  couldn't have given as a coherent progression. Found and fixed three
  real Blender bugs along the way (a cone primitive with no length-wise
  geometry to deform, `subdivide` cutting radially and ballooning one
  tier to 3,250 verts, an unnormalized length parameter that put every
  carved ring in the wrong place) by actually reading the rendered
  preview at each step, not just trusting the export log. v0.1.173-dev.
- **2026-08-09 — ⚒️ Anvil Sticker.** Ben liked the Anvil model specifically
  — generated via the Tripo3D API on the first attempt, no retries, no
  unwanted extra geometry (unlike the Crude Stone Knife's stubborn
  handle a few sessions back): a clean, instantly-readable blacksmith's
  anvil with a proper horn, sitting on a wooden stump base, exactly
  matching the prompt. Imported and, in a later same-day pass, actually
  placed in `TestScene` as a real `AnvilSurface`-tagged world object
  powering the Nail recipe's proximity gate — not just a pretty render
  left sitting in `Tools/Tripo3D/Output/`.
- **2026-08-08 — 🏠 Little House Sticker.** The Building System, start to
  a real working loop: `BuildPiece`/`BuildSocket` data shapes,
  `PlayerBuilding`'s full placement state machine (free placement *and*
  edge-snapping both work), its own `Build` tab, and Foundation — a real
  5m×5m Twig piece that tiles edge-to-edge and correctly inherits its
  neighbor's exact height across uneven ground. Placement input borrowed
  deliberately from Valheim/Rust/Raft (Left Mouse Button + scroll wheel)
  instead of inventing a scheme from scratch. Then, same session, a real
  upgrade path: click a placed Foundation with a Hammer to upgrade it to
  a genuine Plank Foundation in place, hold 5 seconds to destroy it
  outright — its own dedicated interaction logic, not force-fit into the
  hold-and-release pattern everything else in the game uses, since
  releasing early is the *action* here, not a cancel. Every step
  verified against the actual saved prefab/scene YAML, not just a
  passing batch log. v0.1.156-dev through v0.1.157-dev.
- **2026-08-08 — 🪄 Wizard's Wand Sticker.** The Magic System, first real
  wishes to a genuinely player-driven system: Will as a real sixth
  vital, random starting-lineage assignment, and three lineages each
  landing a working wish — Spark (lights a Campfire), Push (shoves any
  Rigidbody), Heal Self (the first `Unconditional`-targeting wish, no
  aiming needed at all). A real success/failure roll reusing crafting's
  own chance-of-creation shape, then a player-selectable "default skill"
  once a lineage might have more than one wish. Caught and fixed two
  genuine bugs along the way (a `RequireComponent` auto-add duplicate-
  component trap, and this project's own documented
  stale-reference-across-`OpenScene` gotcha, both verified by reading
  back the actual saved YAML rather than trusting the batch log). And a
  real design stance, not just a feature: zero on-screen hints for any
  wish, on purpose — "something people play with in order to explore
  it" — genuinely different from every other system in the game.
  v0.1.148-dev through v0.1.155-dev.
- **2026-08-08 — 🚰 Canteen Sticker.** The Canteen work end to end: real
  Tripo3D metal canteen model, wired as a world pickup for the first
  time ever, placed in the scene, fill-status shown wherever it renders
  (including clipped to a Belt), lands upright when dropped, and a
  genuine root-cause fix for the blue fill glow — two guesses at
  tint/emission values did nothing, so dumped the actual shader's
  properties instead of guessing a third time and found glTFast's
  `Shader Graphs/glTF-pbrMetallicRoughness` uses `baseColorFactor`/
  `emissiveFactor`, not Unity's usual `_BaseColor`/`_EmissionColor` —
  fixed and verified against the real runtime code path (`Awake()` via
  reflection, not just "it compiles") before asking for a retest.
  Confirmed working by Ben. v0.1.127-dev through v0.1.131-dev.
- **2026-08-08 — 🐏 RAM Chip Sticker.** Excellent day of progress end to
  end: full Silver/Gold/Platinum ore pipeline with the disguise
  mechanic (never used anywhere in the project before this), a real
  scatter-physics bug root-caused and fixed along the way, the Crude
  Fiber Belt's first real model (green woven grass, via Tripo3D), and
  two genuine pre-existing bugs found and fixed during testing (the
  never-wired Canteen/Belt carry anchors, and the Backpack-vs-Belt
  contents panel only ever showing one container at a time). Lessons
  learned and real groundwork for building faster next time. v0.1.120-dev
  through v0.1.125-dev.
- **2026-08-07 — 🍪 Cookie with a Bite Sticker.** Finally landed the
  Equipment/Inventory panel layout Ben actually wanted, after a long
  back-and-forth: icon baked from the right model, sized crisp, both
  panels visibly bordered and correctly sized (not one giant
  screen-spanning panel), side by side instead of stacked, each with
  its own header ("Equipment" on the slot list, "Inventory" on the
  preview+contents pair). v0.1.93-dev through v0.1.106-dev.
- **2026-08-07 — 🪓 Small Axe Sticker.** Boulder/Rock Node/Big Tree
  playtest polish pass: fixed the Boulder-too-close-to-spawn overlap,
  Boulder's Rock chunk converted to a proper punchable node instead of
  a pickup, Credits page image overflow, and made Big Tree by 3Donimus
  choppable — including catching and fixing a collider math bug that
  had it floating above the actual tree. v0.1.88-dev through
  v0.1.92-dev.
- **2026-08-06 — 🥈 Silver Star Sticker.** Backpack folded into the 5-tier
  CraftTier ladder, new Belt equippable (generic attachment points, Canteen
  can now clip onto one), and the Equip destination picker for multi-slot
  equippables. v0.1.74-dev through v0.1.76-dev.

## Demerits

Same ledger, opposite direction — for when it took way longer than it
should have to get something right.

### Demerits Tally

| Demerit | Count |
|---|---|
| 🐸💥 Squished Frog Sticker | 1 |
| 🍓💀 Spoiled Strawberry Sticker | 1 |
| 👓 Coke-Bottle Glasses Sticker | 1 |
| 🎩 Dunce Cap Sticker | 1 |
| ⚰️ Coffin Sticker | 1 |

### Demerits Log

- **2026-08-21 — ⚰️ Coffin Sticker.** Added a wall-clip physics safety net
  to `NPCGathering.cs` with `private static readonly int StepCheckMask =
  ~LayerMask.GetMask("Ground");` — a `static readonly` field initializer
  calling a Unity API that's only legal from Awake/Start. Compiled clean
  (zero `CS####` errors), then threw the instant the type was first
  touched, which — because it was `static` — poisoned `NPCGathering` for
  every NPC in the scene, not just one. Ben reloaded a save with 4 hired
  NPCs and got 1. Iris, Mining Dude, and Wren never made it — three NPCs
  gone on the very first Play session after the change shipped, Iris
  among them. Ben's exact words: "you killed some npcs! They're gone" —
  and, confirming the stakes, "she was my favorite." The underlying save
  data was untouched the whole time (a live-session casualty, not real
  data loss — reloading after the actual fix recovered all 4), but it
  read exactly like a body count until traced to the Console. See
  `CLAUDE.md`'s matching Gotcha entry for the technical detail and the
  actual fix.
- **2026-08-11 — 🎩 Dunce Cap Sticker.** Ben mentioned wanting to try the
  Tripo API for a better NPC model with animations. Instead of checking
  the repo first, went straight to a generic web search and explained
  Tripo's API from scratch — completely missing that `Tools/Tripo3D/`
  already exists: a working, documented, credentialed generation pipeline
  (`Generate-Model.ps1`, `Texture-Model.ps1`) with a long history of real
  models already shipped through it (Backpack, Anvil, Stone Hammer, Rope
  Coil, and more — see its `README.md`'s "Current status" section). Ben's
  exact words: "dummy. you've used the tripo api." Root cause: never
  looked in `Tools/` before researching externally, same class of miss as
  the IconBaker lesson (`reference_gridless_iconbaker` memory) but this
  time actually landed. The rig/animation-retarget endpoints themselves
  genuinely aren't used by the existing scripts yet, so that part of the
  research wasn't wasted — but it should have opened by reading the
  existing tool, not reinventing the account-setup/API-shape explanation.
- **2026-08-09 — 👓 Coke-Bottle Glasses Sticker.** Built a genuine
  truncated-icosahedron Soccer Ball in Blender — real pentagon/hexagon
  geometry, correct black/white pattern — then handed Ben a plain grey
  cube with a "Pick up Soccer Ball" prompt on first test. Verbatim:
  "you clearly have never seen a soccer ball." Root cause: gave the
  ball's `ItemDefinition` no `worldPickupPrefab`, so Admin Spawn (which
  auto-lists it now that it's a real item) fell back to the game's
  generic placeholder pickup instead of the actual model — a wiring
  gap in the same setup pass that baked a perfectly good ball, not a
  modeling mistake. Same night also shipped without testing that
  `CharacterController` contact even fires a usable event (it doesn't
  — see `OnControllerColliderHit`, CHANGELOG v0.1.185-dev) and without
  simulating whether the kick would ever stop rolling (it wouldn't — a
  rigid sphere in pure rolling never loses energy to friction alone).
  Three real bugs on one "just for fun" feature, all found by Ben
  actually playing with it, none caught by the batch-mode checks run
  beforehand — the checks verified the math in isolation but never
  once exercised the real spawn path, the real contact path, or the
  real over-time physics.
- **2026-08-09 — 🍓💀 Spoiled Strawberry Sticker.** Fixed
  `BerryPickup.prefab`'s null `Pickup.item` earlier the same day — and
  had `canRespawn: 0` sitting right there in the same field block I
  was reading, on the exact same object type (Stick pickups already
  respawn) I'd just been comparing it against, and didn't act on it.
  Berry Bush picked clean once and never grew back until Ben caught it
  live-testing and had to ask for the obvious fix directly.
- **2026-08-07 — 🐸💥 Squished Frog Sticker.** The Equipment/Inventory
  panel layout (see the Cookie with a Bite entry above) took roughly a
  dozen rounds of back-and-forth to get right — wrong Backpack model
  baked into the first icon, a blurry upscaled preview, an invisible
  panel border, a panel that expanded to cover the whole screen, and
  several repositioning misses before landing on what Ben actually
  asked for. v0.1.93-dev through v0.1.106-dev.
