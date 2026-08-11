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

- [ ] **Target size confirmed: 4x current total area.** `Ground` is
  currently a 100×100 unit plane (Unity's built-in 10×10 Plane primitive,
  scaled ×10 in X/Z, confirmed by reading the scene directly). 4x *area*
  (not 4x each linear dimension, which would've been 16x area/400×400) —
  Ben's pick — means **200×200**, i.e. doubling both X and Z scale.
  Existing placed objects this session all sit within roughly ±20 units of
  center, so there's real headroom even before the resize.
- [ ] **Still fully open:** layout and how "organization" should actually
  look (zoned by system? by biome? something else?) — not decided, work it
  out live at the start of the session rather than guessing here.
- [ ] **Procedurally generated with gentle hills — Ben's ask, confirmed
  direction: Unity Terrain, not a bigger flat Plane.** `Ground` today is a
  flat mesh, can't fake real elevation. Terrain's heightmap supports actual
  hills via Perlin/Simplex noise sampled at a small amplitude (a few
  meters of rise/fall across 200×200, "gentle" not mountainous — exact
  amplitude/steepness left as tune-by-feel next session, not pinned down
  now), scripted in a batch-mode Editor pass same as everything else this
  project builds. **Real bonus, not just flavor:** Terrain's `SetHeights()`
  API supports runtime height modification — adopting Terrain now plausibly
  sets up real free-form dig-anywhere later (lower the heightmap locally at
  a dig point) instead of needing a wholly separate system for that down
  the road.
  - **Two real migration costs, flagged honestly, not deferred as a
    surprise:** (1) every object placed this session assumes flat `y=0`
    ground — re-leveling every existing placement to the new surface
    height at its (x,z) is real work, not just a resize, **still not
    done**. (2) [x] **Movement height-tracking — done ahead of schedule,
    shipped v0.2.3-dev.** New shared `GroundHeight` utility (a Ground-
    layer-restricted raycast-down helper) wired into
    `HostileCreature`/`NPCWander`/`NPCMining` at each spot they already
    compute a new (x,z) — Y now snaps to the real ground surface instead
    of staying untouched. Built and verified on today's still-flat
    `Ground` (confirmed a genuine no-op there — same as before, down to
    float noise) specifically so it's already correct by the time real
    hills exist, no retrofit needed later. Full story in `CHANGELOG.md`.
  - [x] **Grass texture itself already done, ahead of schedule — shipped
    v0.2.1-dev.** `Ground.mat` now uses a real Gemini-generated, hand-fixed
    seamless texture (`Assets/Textures/GrassTexture_Healed.png`) instead
    of the old blurry 1024×1024 placeholder — full story in
    `CHANGELOG.md` v0.2.1-dev (two Gemini attempts, an offset-and-heal
    fix on the better one, verified live at the real 20×20 tiling
    density, not just an isolated test). One faint residual seam line
    remains, Ben's call to accept it for now. Whatever tiling
    scale/repeat frequency the eventual Terrain layer ends up using will
    need revisiting — this was tuned for the current flat Plane's 20×20,
    not necessarily what 200×200 Terrain should use.
- [ ] **Scatter a random number of trees (20-75) across the new scene,
  placed once — Ben's ask.** Baked into the scene in a one-time batch pass
  (same discipline as everything else hand-placed this project), not
  regenerated every launch. [x] **Prerequisite done, ahead of schedule —
  shipped v0.2.2-dev:** `Assets/Prefabs/Tree.prefab` now exists as a real,
  reusable asset (extracted from the old one-off scene instance via
  `PrefabUtility.SaveAsPrefabAssetAndConnect`, also renamed from a stale
  "comparison only" label — full story in `CHANGELOG.md`). Actual
  scattering itself is still not built. Placement rules to build
  in from the start: **respect terrain height** at each tree's (x,z)
  (same height-snapping concern as the hills work above — trees need it
  too, not just moving NPCs), and **minimum spacing** both between trees
  themselves and away from already-placed important objects (the NPC,
  ore nodes, Water Puddle, spawn point) so a random roll can't drop one
  on top of something that matters.
- [ ] **Scatter ore the same way, using Boulders as the shared "ore"
  object — Ben's explicit call, not 5 separately-modeled Ore Node
  types.** [x] **Prerequisite done, ahead of schedule — shipped
  v0.2.2-dev:** `Assets/Prefabs/Boulder.prefab` now exists as a real,
  reusable asset, same extraction as Tree. **Found along the way:**
  Boulder already carries an `AnvilSurface` component alongside
  `ResourceNode` — every future scattered boulder will also work as a
  crafting proximity point, not just an ore/rock source, for free. Actual
  scattering/scarcity-config itself is still not built. **Real
  pivot from how ore works today, not just a reskin:** checked Boulder's
  actual config — it's currently a *plain* generic rock node (yields
  Rock, trains `Gathering`, no tool required, `hiddenMaterial`/
  `revealedMaterial` both unset). The 5 named Ore Nodes (Copper/Iron/
  Silver/Gold/Platinum) are separate, differently-modeled objects today,
  and only Silver/Gold/Platinum use the hidden-material disguise system.
  Ben's ask means unifying these: every scattered rock is visually a
  Boulder, and each instance's `chunkPrefab`/`trainedSkill`/
  `requiredTools`/hidden-material config decides what it *actually*
  yields — mostly plain Rock, with a randomized subset configured as
  Copper/Iron/Silver/Gold/Platinum per the scarcity curve below, reusing
  the same disguise mechanic the named Ore Nodes already use for the rare
  tiers so nothing's knowable without cracking it open (or revealing it
  with a Shield). **Still open, not decided:** whether Copper/Iron should
  also go fully disguised under this new scheme (today they're always
  visibly labeled "ore," not hidden) or keep reading as identifiable via
  their required-tool prompt text before breaking — worth deciding
  explicitly next session rather than assuming either way.
  - **Proposed scarcity curve** (first-pass numbers, tune by feel like
    everything else in this project — not locked): builds on the
    existing 5-tier Copper→Iron→Silver→Gold→Platinum ladder (same order
    as the currency tiers) and the fact Silver+ are already the
    "hidden" tier. Copper ~15-25 (common), Iron ~10-15 (common), Silver
    ~5-8 (hidden), Gold ~2-4 (hidden, rarer), Platinum 1-2 (hidden,
    genuinely scarce — not "always findable" like today's guaranteed
    single instance). Rest of the scattered boulders (not configured as
    any ore tier) yield plain Rock, same as today's Boulder.
  - Same placement rules as trees above (terrain-height respect, minimum
    spacing from other boulders and important objects) apply here too.
- [ ] **Scatter Berry Bush and Herb Bush the same way — Ben's ask.** No
  prerequisite gap here, unlike Tree/Boulder: `BerryBush.prefab` and
  `HerbBush.prefab` already exist as real, reusable prefabs (`HerbBush`
  was built earlier this session, `BerryBush` already proper) — this is
  placement work only. No tier/scarcity curve needed either, unlike ore —
  each is a single gatherable type, not a 5-tier ladder. **Still open, not
  decided:** how many of each (proposing a smaller range than trees as a
  starting point, e.g. ~10-20 each, since understory bushes read as
  sparser than trees in most games — not confirmed, adjust freely). Same
  placement rules apply (terrain-height respect, minimum spacing from
  each other and from important objects).
- [ ] **Up to 5 Wolves the same way — Ben's ask.** No prerequisite gap:
  `Wolf.prefab` already exists as a real, reusable prefab (built during
  this session's Combat work). Random count, capped at 5 rather than an
  open range like the passive resources — wolves are hostile, more of
  them changes how dangerous the scene feels to move around in while
  testing everything else, not just a density choice. **One placement
  rule that's genuinely different from the passive scattering above:**
  keep a deliberate minimum distance from the *player's spawn point*
  specifically, not just from other objects — the two wolves already in
  the scene were hand-placed at ±14/±8 for exactly that reason (a fresh
  spawn shouldn't get immediately jumped). Same terrain-height/minimum-
  spacing rules otherwise apply.
- [ ] **3-5 NPCs the same way — Ben's ask, to really stress-test the
  Hireable NPC system.** No prerequisite gap: `NPCFactoryWorker.prefab`
  already exists as a real, reusable prefab (every Hireable NPC chunk
  built onto it). **Genuinely valuable beyond just more content:**
  multiple clones of the same prefab each need their own persistent
  identity (hired state, job, tools, cargo, skill growth) rather than
  sharing one — exactly the kind of case that would catch a broken
  stable-ID design in the save/load work early instead of after the
  fact. Also gives real resource contention for free once boulders are
  scattered too — two NPCs' `FindTarget` competing for the same nearby
  node, one falling back to the next-nearest once the first claims it.
  **Two real resource considerations to have ready, not blockers:**
  hiring costs 10 Copper each (5 NPCs = 50 Copper, but the player starts
  with only 20 — needs Admin-spawned currency or a starting-balance
  bump to actually test "hire all 5"), and each Mining-assigned NPC needs
  its own Pickaxe + Mining Face Shield + Backpack (5 NPCs = 15 tools
  total to fully equip everyone). **Still open:** whether multiple NPCs
  share one deposit container or each get their own — either is
  technically fine (`NPCJob.DepositContainer` has no exclusivity today),
  worth deciding live next session depending on what's more useful to
  test. Same placement rules as Wolves (spawn-point distance, terrain
  height, minimum spacing) apply.

### 2. Save/load persistence (v1, deliberately narrow scope)

Ben's framing: "we'll need to do a 'save state' so that the game can continue
where we're at, instead of restarting at every test." Nothing in this project
persists anything today — confirmed by grepping the whole codebase, zero
`DateTime`/save-file/serialization code exists anywhere. This is the biggest
of the three pieces and the one most worth getting the shape right on before
building more content on top of it. Real design problems to solve, not just a
file format choice:

- **Reference resolution** — `Inventory` slots store direct `ItemDefinition`
  references (a ScriptableObject asset), and `PlayerSkills`/`NPCSkills` key
  off `SkillDefinition` the same way. Neither serializes cleanly to
  JSON/binary as-is; needs a stable string ID per asset (name or a assigned
  key) plus a runtime lookup registry to resolve back to the real asset on
  load.
- **Stable identity for world objects** — nothing in this project has a
  persistent identity today; everything is "whatever object happens to sit at
  this spot in the hand-edited scene file." Saved data (a Storage Box's
  contents, an ore node's broken/respawning state, the NPC's hired/job/cargo
  state) needs some way to reattach to the *same* object when the scene loads
  fresh next time — likely a small `SaveId`-style component/GUID tag on each
  persistent object, mirroring how small single-purpose interfaces
  (`IWaterSource`, `IRenameable`) are already this project's convention for
  "mark an object as having this capability."
- **Proposed shape** (not committed, discuss before building): a central
  `SaveManager` writing/reading one JSON file
  (`Application.persistentDataPath`), plus a small interface (e.g.
  `ISaveable` — `CaptureState()`/`RestoreState()`) that relevant components
  implement, same convention as everything else in this codebase.
- **Deliberately narrow v1 scope** — same "ship the real useful slice,
  document the gaps" discipline as every other system this session. Covers:
  Player (inventory, vitals, skills, currency, equipment, position),
  Storage Boxes, ore/resource nodes, and the Hireable NPC (hired state, job,
  tools, cargo, stats, position). **Explicitly deferred, not a v1 gap to
  silently fix:** loose dropped/spawned world pickups, built structures
  (`BuildPiece` placements), Lockbox/Bank contents — revisit once v1's
  actually proven.

### 3. Digging + water scarcity, built into the new space from day one

Original digging plan (Shovel + dig sites + new raw material) unchanged from
the first pass — see below — plus a new tie-in Ben asked to fold into the same
session:

- [ ] **Shovel — 5-tier item + recipe**, same pattern as Pickaxe/Knife/
  Hammer/Axe (Crude/Rudimentary/Normal/Fine/Masterwork). Stonework
  discipline — same "stone head/edge defines the tool" rule the other
  stone tools already follow, no new discipline needed. Needs a real
  model (Blender tier-shape family or a Poly Pizza source, session
  execution detail, not decided here).
- [ ] **Dig sites, not free-form digging (Ben's pick, session 1)** — a
  `ResourceNode` instance dressed as a loose dirt/clay/sand patch,
  `requiredTools` = the Shovel tiers, same hold-to-break shape every
  gathering node in this game already uses. **Ground itself almost
  certainly does NOT need to change for this** — a self-contained "hole"
  prop (its own small crater mesh, walls + a fake-depth floor) can sit at
  the dig site and appear on break, the same "swap in a prop" trick a
  chopped tree's stump or a broken Rock Node's chunks already use. Needs
  one small, generic addition to `ResourceNode`: an optional
  `holeVisualPrefab` field shown on break / hidden on respawn — reusable
  by any future node that wants a "left a mark" visual, not Shovel-only.
  **Free-form dig-anywhere (point the shovel at any ground point) is
  explicitly deferred** — that's what would actually require giving
  `Ground` real volume (it's currently a bare Unity Plane primitive, zero
  thickness, confirmed by reading the scene directly) or moving to a real
  terrain/heightmap system. Genuinely harder, its own later multi-session
  arc, not part of this first pass.
- [ ] **New raw material — Clay/Dirt/Sand (Ben's pick over buried loot or
  earthworks for session 1)**. Exact name/count of materials not decided.
  **Still fully open, explicitly deferred to next session:** what actually
  consumes it. Leading idea floated but not committed: a new Building
  material tier (Clay/Adobe bricks after Plank, or a mortar ingredient) —
  gives Building a real next step and digging an immediate payoff, but
  Ben's call was "figure it out next session," not decided now. Don't
  assume this without checking in first.
- [ ] **Still open, same shape as the Mining-vs-Gathering question from
  Hireable NPCs:** does digging train the existing generic `Gathering`
  skill, or does it warrant its own dedicated skill? Worth deciding
  explicitly rather than defaulting either way.
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

## Enhancements — Phase 2 (MVP 2) Backlog

**Draft, not finalized (2026-08-10) — Ben's explicit call: "we won't
consider this finalized yet."** Pulled together from `docs/design-brief.md`'s
existing "Phase 2 — Settlement depth" list (Systems Wishlist section) and its
dedicated Factions, Guilds & Warbands section, now that Phase 1 closed out in
full, so there's a working list to pick off chunk-by-chunk the same way
Hireable NPCs was — same discipline, not yet scoped/ordered/agreed to. Treat
every item below as a discussion candidate, not a committed plan, until Ben
signs off on scope and order.

- [ ] **Universal degradation** — nothing lasts forever; gear, buildings, and
  vehicles decay if left unmaintained.
- [ ] **Skill books/magazines** — readable items granting basic training or
  boosting an existing skill, an alternate path alongside learn-by-doing.
  Also the unlock vehicle for learning a second magic lineage and for
  found/scribed Scrolls (see design-brief.md's Magic System section) — those
  ride this same mechanic, not separate systems.
- [ ] **Gardening** — harvest seeds, plant and grow crops.
- [ ] **Animal & hunting module** — tame, hunt, harvest, skin. Directly
  extends Phase 1's Combat/wolf-skinning loop (`HostileCreature`) rather
  than replacing it.
- [ ] **Fame/reputation system** — skill mastery earns fame in that trade
  line, and fame feeds back into the world (a renowned hunter attracts
  rarer/better game, a famous blacksmith draws better customers/prices).
  Distinct from Phase 1's skill-tied quality mechanic: quality is about
  *your* competence, fame is the world *recognizing* it. **Already has
  inert placeholder UI** (`Fame: 0` tile on the Player tab, zero backing
  system) — see design-brief.md line ~124. **Still open, flagged but not
  resolved:** Ben separately floated Fame/Reputation as a possible *later*
  phase (pushed past Phase 2 entirely) — never confirmed either way against
  this Phase 2 placement. Worth deciding explicitly before building.
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
  `NPCJob`/`NPCJobDefinition` shape Mining already uses).
- [ ] **Reverse engineering & manuals** — disassemble items to learn their
  schematics, then write instructional manuals/grimoires to mentor other
  players or NPCs. Ties into the skill-books item above as the inverse
  (author your own instead of finding a pre-made one).
- [ ] **Factions** — reputation/trust standing driven by behavior (safe,
  productive settlements build trust; raiding erodes it). Separate from
  Fame above and from Warbands below. **Already has inert placeholder UI**
  (`Faction: None` tile), same as Fame.
- [ ] **Merchant Guilds** — craft-skill bonuses and trade perks, not
  territorial. Structured apprenticeships for advanced crafting tiers,
  exclusive trade contracts, preferential exchange rates on volatile
  assets (gems), guild-backed caravan protection. **Partially seeded**: a
  small real "join up to 3 Guilds" system (`PlayerGuilds`) already shipped
  ahead of schedule this session — membership only, none of the
  bonus/perk/apprenticeship mechanics described here yet.
- [ ] **Warbands/Militias** (Phase 3, listed here for context since it's
  part of the same Factions/Guilds/Warbands trio) — the literal combatant
  groups in Settlement Warfare. A Warband's conduct can move its members'
  Faction standing even though the two systems are otherwise separate.

## Bugs

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
- [ ] **Only Bare-handed exists of the five weapon-usage skills named back
  in the 2026-08-05 Crafting/Gathering/Skills Pipeline planning
  (Archery/Spear/Sword/Gun/Bare-handed) — 2026-08-10.** Basic Combat
  shipped with fists-only; no melee weapon (Spear, Knife-as-weapon) or
  ranged weapon (Bow) actually deals combat damage yet, so those four
  skills still have nothing to train them, same as before this session's
  Combat work. Bare-handed's own numbers (9 dmg, 0.7s cooldown) were
  picked to fit a first-pass placeholder Wolf, not vetted against a real
  weapon-tier progression.
- [ ] **Dexterity / Constitution / Intelligence — display-only, no growth
  hooks or mechanical effects yet (2026-08-10).** Strength shipped fully
  (see `CHANGELOG.md` v0.1.189-dev); the other three core stats exist as
  real `SkillDefinition`s (`SkillCategory.Attribute`) with a Player-tab
  tile and a "Growth" bar each, but nothing ever calls `GainExperience`
  on them and they have no mechanical effect on anything. Ben's explicit
  scope call: build these later, following Strength's exact established
  pattern for consistency. Planned shape for each, from the original
  ideation conversation (not yet built, not yet confirmed final):
  - **Dexterity** — grows from sprinting, jumping, sneaking, ranged
    combat. Drives movement efficiency under load — though note
    Encumbrance's own movement-efficiency question was explicitly
    closed as *not wanted* (Ben, 2026-08-10: "I think that the relative
    amounts apply nicely. no change to that") — worth confirming
    Dexterity's hook still makes sense before building it, rather than
    assuming the original ideation note still holds.
  - **Constitution** — grows from surviving damage, repeatedly hitting 0
    Stamina, environmental exposure. Drives max Health/Stamina growth
    over time (both vitals are currently a fixed 100 cap in
    `PlayerVitals` — would need the same "grows through use" treatment
    `Will`/`GrowMaxWill` already has).
  - **Intelligence** — grows from completing wishes (magic discovery).
    Drives `PlayerVitals.GrowMaxWill` growth, *and* a proposed global
    multiplier on XP gained by every other skill (smart characters learn
    faster) — rough shape floated: `xpGained *= 1 + (intLevel / 200)`,
    capping at +50% at Intelligence 100. Not vetted against real numbers
    the way Strength's capacity/gain-rate curves were (comparison
    artifacts, calibrated pacing) — treat as a starting point, not a
    locked formula.
  *(Reported by Ben.)*
- [ ] **Fame / Faction — placeholder tiles only, no backing system
  (2026-08-10).** Added to the Player tab alongside the 4 core stats
  purely so the full tab layout could be seen and judged together; both
  read a static `Fame: 0` / `Faction: None` with nothing feeding them.
  Conceptually different from the core stats — reputation/standing
  driven by other NPCs'/factions' view of the player, not personal
  `GainExperience` — so building these out is a different kind of system
  than Dexterity/Constitution/Intelligence above, not just "the next
  stat in line." No design work done beyond the placeholder tiles.
  *(Reported by Ben.)*
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
- [ ] **Silver/Gold/Platinum still don't have a refined "bar" item the
  way Copper/Iron do** (v0.1.121-dev gave them the missing punchable
  mid-tier, matching Copper/Iron structurally, but the final tier still
  yields the existing `SilverOre`/`GoldOre`/`PlatinumOre` item directly,
  not a further-refined material). Worth deciding whether these three
  should eventually get a true refined tier too (a smelting recipe
  consuming the raw Ore?), or whether raw Ore is the intended final form
  for these three specifically (more currency/jewelry-flavored than
  Copper/Iron's tool-material role) — not an oversight, just an open
  design question same as the ones below.
- [ ] **New "Iron" item (`Iron.asset`, v0.1.119-dev) has no crafting
  recipe consuming it yet either.** Same situation as Copper below —
  built ahead of the crafting need when the Boulder→chunk→refined-
  material tier structure was extended to Iron Ore too. Nothing turns
  Iron into anything, and no recipe consumes it as an ingredient.
- [ ] **New "Copper" item (`Copper.asset`, v0.1.117-dev) has no crafting
  recipe consuming it yet.** Built ahead of the crafting need, same
  situation as Rock below and the pre-existing "Copper Ore" item —
  Ben's explicit call when extending the Boulder→chunk→refined-material
  tier structure to copper, not an oversight. Nothing currently turns
  Copper into anything (no smelting/refining recipe exists), and no
  recipe consumes it as an ingredient either. Worth deciding what
  Copper is actually *for* (a Bronze-tier alloy ingredient? a currency
  material distinct from Copper Coin?) before it reads as forgotten
  dead content the way Rock/Wood below already do.
- [ ] **"Rock" item (`MediumRock.asset`) is now completely orphaned.** Side
  effect of the v0.1.90-dev change making Boulder's chunk punchable
  instead of directly pickupable (per Ben's request): `MediumRockChunk.
  prefab` used to be a `Pickup` granting 1 Rock, now it's a
  `ResourceNode` that breaks into 2 Small Rock instead and never grants
  Rock at all. Confirmed via guid search — nothing in the project
  references `MediumRock.asset` anymore (no recipe ever did, per the
  entry below this one). Same situation the Wood item used to be in
  before it was removed outright (v0.1.136-dev, Ben's call — the
  Stick/Plank material line covers that role, Wood was redundant) —
  worth deciding whether to delete `MediumRock.asset` outright too, or
  give "Rock" a real purpose (a crafting ingredient? a coarser material
  than Small Rock for some recipe?) before it reads as forgotten dead
  content.
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
    no ranged/melee weapon exists yet to holster).
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
  only the crafting side is fixed). **Still open:** where Leather comes
  from (implies hunting/animals, which don't exist at all yet — or some
  other source?), and Rudimentary/Fine/Masterwork tiers of either new
  Fiber item.
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
- [ ] **Sky texture could use another pass.** Procedural cloudy skybox shipped
  v0.1.55-dev through v0.1.57-dev (`GenerateSkyTexture.cs`, throwaway —
  `Assets/Data/Sky.mat` + `Assets/Textures/SkyTexture.png` are the persistent
  result) — gradient direction and basic visibility are now correct per Ben's
  screenshots, but clouds are still sparse/barely present in a normal view
  ("this is fine for now," not "this is done"). Worth another round of tuning
  cloud coverage/density and possibly the cloud shape/softness if it comes up
  again — same tileable-noise technique as the grass texture, see
  `CHANGELOG.md`'s v0.1.55/56/57-dev entries for the full history of what was
  tried (including the inverted-gradient bug already fixed) before changing
  anything.
  **Likely root cause identified 2026-08-04, not yet applied here:** while
  fixing the ore textures, found that `Mathf.SmoothStep(low, high, rawValue)`
  doesn't threshold anything the way GLSL's `smoothstep` does — see `CLAUDE.md`'s
  new gotcha on this exact bug. The sky's cloud-coverage code used the identical
  pattern, so the persistently-faint clouds across three tuning rounds were very
  likely this, not a frequency/contrast problem. Try the corrected
  `SmoothThreshold` helper from that gotcha before anything else. *(Reported by
  Ben.)*
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
- [ ] **Drink/fill directly from a container.** Same gap for a Canteen sitting in a
  backpack/container — no direct Drink/Fill buttons there, only the generic move
  popup (which, as of 2026-08-03, correctly preserves the equipment reference when
  used, but still requires moving the canteen out before it can be used).
