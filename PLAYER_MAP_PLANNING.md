# Player Map Planning

Planning doc for a fog-of-war Player Map (2026-08-16) — designed
conversationally with Ben as a direct extension of the Village Flag/City
Statue work earlier the same session. Decision-locked where noted,
otherwise flagged as open.

**Status: core mechanic built, same day.** `M` opens a real `MapScreen`
(same open/close/cursor-lock shape `GameMenuScreen`'s backquote toggle
already established), showing a live fog-of-war texture driven by a new
`PlayerMapExploration` component — the permanent 25m on-foot reveal
radius from section 1 is live and ticking every frame. `GameMenuScreen.
ControlsList` updated with the new binding, per this project's own
standing rule. **Save/load persistence built and verified, v0.3.99-dev**
— caught live by Ben (explored, saved, reloaded, map reset to fog).
`CaptureRevealedBase64()`/`RestoreRevealedBase64()` bit-pack the grid
into `SaveManager.CapturePlayer`/`RestorePlayer`, verified via a real
batch-mode round-trip (not just a clean compile — the first attempt gave
a false PASS because `AddComponent` doesn't fire `Awake()` in edit-mode
batch scripting; fixed the test via reflection, then got a genuine
666-cells-revealed-and-matched result). **Village Flag/City Statue
reveal hooks built, v0.3.104-dev** — `PlayerBuilding.Confirm` calls
`PlayerMapExploration.RevealCircle` at placement time for both (see
`VILLAGE_FLAG_PLANNING.md` section 6 and `CHANGELOG.md`'s v0.3.104-dev
entry), closing the follow-up flagged here. **Named Flag markers built,
v0.3.105-dev** — `VillageFlag` became nameable (`IRenameable`, same
right-click flow `StorageBox` already uses), and `MapScreen` now draws a
labeled marker at every placed Flag's position using that name, shown
unconditionally rather than gated by fog reveal. Everything else
verified via batch-mode compile + direct scene YAML grep only so far —
not yet live-tested in Play mode beyond the save/load round-trip Ben
confirmed was broken (and that fix addresses).

**Real prerequisite closed same day: `WorldBounds.cs` built and
verified.** Section 2's "no world-size concept exists" gap is resolved —
a new shared utility reads `Terrain.activeTerrain`'s own position +
`TerrainData.size` directly, so it stays correct automatically if the
Terrain is ever resized/regenerated (including the future Terrain/hills
conversion `BUGS_AND_ENHANCEMENTS.md` already has flagged). Verified
against the real `TestScene.unity`, not just compiled: confirmed
**exactly 200×200 units** (X: -100 to 100, Z: -100 to 100) via a direct
batch-mode check, not the "roughly 200×200, unconfirmed" guess this doc
originally had. No hardcoded world size anywhere — `WorldBounds` is the
one place that ever needs to know it, ready for the Player Map's
fog-of-war grid (and anything else that needs world extent) to consume
directly.

## 1. The mechanic

- **`M` opens a Map screen** (confirmed collision-free against
  `GameMenuScreen.ControlsList` — nothing else uses it today). Needs a
  new entry in that list per this project's own standing rule ("Update
  `GameMenuScreen.ControlsList` whenever a new key mapping is added").
- **Starts blank.** Only areas the player has actually explored are
  ever shown — no full-map reveal, no minimap-style "everything nearby"
  view.
- **Base reveal: 25m radius**, permanently, around every point the
  player has physically walked through. Once revealed, always revealed
  (no re-fogging).
- **Village Flag and City Statue both add their own reveal circles at
  the point they're placed** — not something the player has to walk the
  full radius of personally. Locked-in numbers (2026-08-16, Ben
  confirmed the proposed starting scale as-is):

  | Source | Total reveal radius | Note |
  |---|---|---|
  | Base (on-foot exploration) | 25m | Everywhere the player has walked |
  | Crude Flag | 35m | 25m base + 10m |
  | Rudimentary Flag | 45m | 25m base + 20m |
  | Normal Flag | 55m | 25m base + 30m |
  | Fine Flag | 65m | 25m base + 40m |
  | Masterwork Flag | 75m | 25m base + 50m |
  | City Statue | 125m | 25m base + 100m — a flat jump, not a ladder (one-time milestone, not a craftable tier) |

  **Interpretation flagged explicitly**: the per-tier numbers Ben
  confirmed were proposed as additive bonuses on top of the 25m walking
  base, not standalone totals — written that way above so there's no
  ambiguity, but worth a quick confirm before building since it wasn't
  spelled out digit-by-digit in conversation.
- **Shared visibility (Flag marker, Statue circle visible to *other*
  players) is explicitly future/multiplayer-only** (Ben's own framing).
  No multiplayer state-sharing infrastructure exists at all today
  (`MULTIPLAYER_PLANNING.md` is still exploration-only) — this doc
  designs the single-player fog-of-war as the real, buildable-now core,
  and logs the shared half as a note for whenever multiplayer actually
  lands, same phased discipline that doc already uses. Not attempted
  here.

## 2. Real technical groundwork — not yet decided

- **World bounds.** No "how big is the world" concept exists anywhere
  in this project today (confirmed via grep). The current Ground/
  Terrain looks to be roughly 200×200 units based on its scene
  position (`(-100, -5, -100)` origin corner) — worth confirming
  precisely before treating it as a real number, and worth remembering
  this becomes a bigger question once/if the world ever grows past a
  single flat Terrain (the design brief's own "Terrain/hills
  conversion" backlog item).
- **Fog-of-war representation.** The practical approach is a grid —
  divide the world into cells, mark a cell revealed once the player (or
  a Flag/Statue) has been within range, render the Map screen from that
  grid (same "sample a data structure into an IMGUI draw call" shape
  every other screen in this project already uses, just 2D-spatial
  instead of list-shaped). Cell size not chosen — affects both memory
  footprint and how smooth/blocky revealed edges look on the rendered
  map.
- **Save/load.** Explored-state needs to persist — a real new save-
  state surface, same category of follow-up Skill Books needed after it
  shipped (`SAVE_LOAD_PLANNING.md` section 10). A raw per-cell bool grid
  could get large depending on cell size/world size; worth a compact
  encoding (e.g. a bitset) rather than one bool per cell verbatim, but
  not designed in detail here.
- **Rendering the map itself.** Needs *some* visual representation of
  the terrain/world to draw revealed regions against — a top-down
  render/snapshot, a hand-authored map texture, or just solid-color
  revealed-vs-fog regions with landmark icons (Flag/Statue markers,
  maybe the player's own position) — not decided. Simplest first pass
  is probably flat colors + icons, matching this project's consistent
  "function before polish" build order elsewhere.

## 3. Explicitly out of scope for this pass

- Multiplayer shared visibility (section 1's last bullet).
- Exact world bounds (needs confirming, not just estimating from the
  Terrain's scene position).
- Fog-of-war grid cell size, and the compact save encoding for it.
- How the map actually renders the terrain underneath revealed areas
  (flat color vs. a real snapshot/texture).
- Any additional map features beyond fog-of-war + Flag/Statue markers
  (waypoints, fast travel, etc.) — not raised in conversation, not
  assumed.

## Cross-references

- `VILLAGE_FLAG_PLANNING.md` — the Flag's 5-tier ladder this reuses
  directly for reveal-radius scaling, and the City Statue gate this
  also hooks into.
- `MULTIPLAYER_PLANNING.md` — the phased-scope precedent for logging
  the shared-visibility half as future rather than attempting it now.
- `SAVE_LOAD_PLANNING.md` — the kind of follow-up this system will need
  once built (a new persisted-state surface).
- `CLAUDE.md`'s standing rule on `GameMenuScreen.ControlsList` — the `M`
  keybind needs an entry there the moment this ships, not as an
  afterthought.

Planning only, not yet built.
