# Save/Load Persistence Planning

Real implementation plan for MVP2 item 6 (`MVP2_PLANNING.md`), expanding the
narrow v1 draft that's lived in `BUGS_AND_ENHANCEMENTS.md`'s "Save/load
persistence" section since 2026-08-10. That draft is still the accurate
starting framing — this doc turns it into an actual buildable plan, with the
three real design forks it flagged as "discuss before building" now decided.

## 1. Why this, why now

Ben's original framing: "we'll need to do a 'save state' so that the game
can continue where we're at, instead of restarting at every test." Nothing
in this project persists anything today — confirmed by grep, zero
`DateTime`/save-file/serialization code exists anywhere.

`MVP2_PLANNING.md` calls this out as one of two **foundation-tier** items
(alongside animation) — infrastructure other systems either need directly
or are currently working around. The clearest example already shipped:
Hireable NPCs' work-shift timer is a "5 real minutes" stand-in for the
design brief's actual "5 real days," explicitly because there's no
persistence layer to make a multi-day real-world timer meaningful or even
testable. Building this now means that stand-in (and any future
pacing/growth system that wants to reason in real days) can finally be
designed against something real instead of around a known gap.

## 2. Current state audited (2026-08-13)

Confirmed directly against the code, not assumed:

- **`Inventory` slots hold direct `ItemDefinition` references** (a
  ScriptableObject asset) — doesn't serialize to JSON as-is; needs a
  stable ID + runtime lookup to resolve back to the real asset on load.
  Same problem for `PlayerSkills`/`NPCSkills`, which key a
  `Dictionary<SkillDefinition, float>` off the `SkillDefinition` asset
  reference directly.
- **A slot can hold a live `IEquippable` instance**, not just a plain
  stack — `Inventory.Slot.equipment`. A worn Backpack, Canteen, Tool,
  Boot, Belt, Sunglasses, etc. are real physical `GameObject`s, and some
  of them (`Backpack`, `Boot`, `Belt` — anything implementing
  `IInventoryHolder`) carry **their own nested `Inventory`** on top of
  that. This is the single hardest piece of the whole system — see
  section 4.
- **Nothing has stable identity today.** Every world object (a
  `StorageBox`, a `ResourceNode`, a hired NPC) is "whatever happens to sit
  at this spot in the hand-edited scene file." Saved data needs a way to
  reattach to the *same* object on a fresh scene load.
- **Every relevant piece of player/NPC/world state was audited directly**
  this session (`PlayerVitals`, `PlayerSkills`, `PlayerCurrency`,
  `PlayerEquipment`, `PlayerInventory`, `NPCCargo`, `NPCSkills`,
  `NPCJob`, `NPCHiring`, `StorageBox`, `ResourceNode`) — see section 3 for
  the exact payload each one needs.

## 3. What v1 actually captures

Same "ship the real useful slice, document the gaps" discipline as every
other system this project has built.

### Player
- **`PlayerVitals`**: `health`, `hunger`, `thirst`, `stamina`,
  `bodyTemperature`, `will`, `maxWill`. (Heal-over-time state is transient
  — fine to drop on save, same as any other mid-action state.)
- **`PlayerSkills`**: the full `Dictionary<SkillDefinition, float>` —
  serialized as parallel arrays (skill ID string + level), since
  Newtonsoft *can* do dictionaries natively but a `ScriptableObject` key
  still needs the same ID-resolution step as everywhere else.
- **`PlayerCurrency`**: the 5 `CoinType` balances.
- **`PlayerInventory.Inventory`** and **every named `PlayerEquipment`
  slot's `Inventory`**: each slot's item ID + count, or — for an
  equipment-backed slot — the item ID plus that instance's own captured
  sub-state (section 4).
- **Transform**: position + Y-rotation (this is an FPS controller; pitch
  is camera-only, not meaningful to restore).

### World objects
- **`StorageBox`**: `SaveId`, `boxName` (it's renameable), full
  `Inventory` contents (same equipment-aware capture as the player's).
- **`ResourceNode`**: `SaveId`, availability state. Store **seconds
  remaining on the respawn timer**, not an absolute `Time.time` — that
  value is meaningless across a session restart, and this project already
  has a stated future goal of *real* multi-day timers once persistence
  exists, so the format should already be real-world-duration-shaped, not
  session-relative.
- **Hireable NPC**: `SaveId`, `NPCHiring` (`isHired`, `isWaitingForPayment`,
  `workTimer`), `NPCJob` (assigned job ID, equipped-tools dictionary, and
  a `SaveId` *reference* to its deposit container — cross-object
  references need the same ID-resolution pattern as everything else),
  `NPCCargo.Inventory`, `NPCSkills.Levels`, transform position.

### Explicitly deferred — a real scope cut, not a silently-missing gap
- Loose dropped/spawned world pickups (a Pickup sitting on the ground).
- Built structures (`BuildPiece` placements — every wall/foundation/piece
  a player has built).
- Lockbox/Bank contents.
- The NPC work timer staying a 5-minute stand-in — replacing it with a
  real multi-day timer is explicitly a **separate follow-up** once
  persistence exists to make it meaningful, not bundled into this build.

## 4. The hard part: nested equipment state (decided — full recursive capture)

An equipped item isn't just "an `ItemDefinition` ID" — some of them carry
real state of their own:

- `Backpack`/`Boot`/`Belt` (`IInventoryHolder`) each have their **own**
  `Inventory`, which can itself contain more equipped items (a Canteen
  clipped into a Backpack's contents, or clipped to a worn Belt — this is
  already a real, shipped mechanic, not hypothetical).
- `Canteen` has `LiquidType`/`Amount`.
- Other equippables may be pure `ItemDefinition` + nothing else (a plain
  `Tool` has no extra state beyond which tier it is).

**Decided (Ben): full recursive capture**, not the simpler "re-derive from
`ItemDefinition`, contents lost" cut. Concretely:

- New `IEquippableSaveData` (or similar) — a small interface any
  equippable type with *extra* state beyond its `ItemDefinition` can
  implement (`Canteen` for liquid; `IInventoryHolder` types get their
  nested-`Inventory` capture handled generically since `IInventoryHolder`
  is already a shared, known shape, not per-type).
- Restoring a slot with `equipment != null`: look up the `ItemDefinition`,
  instantiate the correct real `worldPickupPrefab`-backed instance (same
  pattern `PlayerCrafting.AddCraftedOutput`/`AdminSpawnScreen` already use
  for spawning a real physical equippable from an `ItemDefinition`), then
  if it implements `IInventoryHolder`, recursively restore its own
  `Inventory` the same way — genuinely recursive, since a Backpack could
  theoretically hold another `IInventoryHolder` in principle, even if no
  content does that today.
- This is real, non-trivial new code, and the part most likely to reveal
  a wrinkle no other part of this plan predicts (e.g. an equippable whose
  `SetCarried`/`Stash` lifecycle doesn't expect to be instantiated
  "already worn" at scene-load time rather than via the normal
  pick-up-then-equip flow). Budget real testing time here specifically.

## 5. Stable identity

Two different ID problems, two different mechanisms:

- **Data assets** (`ItemDefinition`, `SkillDefinition`) — add a stable
  string key (simplest: the asset's own file name, which is already
  effectively unique per type in this project's `Assets/Data/`
  convention) resolved through a small runtime lookup. Build two registry
  `ScriptableObject`s (`ItemDatabase`, `SkillDatabase`), each holding an
  array of every known asset of that type, auto-populated by a one-off
  Editor script scanning `Assets/Data` via `AssetDatabase` — same
  discovery mechanism `AdminSpawnScreen` already uses (confirmed this
  session it auto-finds every `ItemDefinition` this way), just baked into
  a real shipped asset instead of an Editor-only scan, since
  `AssetDatabase` doesn't exist in a built game.
- **World object instances** (`StorageBox`, `ResourceNode`, a hired NPC)
  — new small `SaveId : MonoBehaviour` component (a GUID string,
  generated once and baked into the scene file the first time it's added
  — same "small single-purpose marker component" convention as
  `IWaterSource`/`IRenameable`). On load, a scene-scan registry
  (`Dictionary<string, GameObject>`, same shape `StorageBox.Active`
  already uses for its own nearby-lookup) maps a saved ID back to the
  live instance sitting in the loaded scene.

## 6. Architecture

- **`SaveManager`** — a singleton-ish `MonoBehaviour` (or plain static
  class) owning the actual file read/write:
  `Application.persistentDataPath/save.json`, via Newtonsoft.Json
  (decided over `JsonUtility` — native `Dictionary`/nested-object support
  outweighs the cost of a new package dependency, which this project has
  already taken on twice this session for Mirror/PurrNet evaluation, so
  it's not a new category of decision).
- **`ISaveable`** — `string SaveId { get; }` +
  `object CaptureState()`/`void RestoreState(object state)`, implemented
  by each top-level system (`PlayerSaveState`, `StorageBoxSaveState`,
  `ResourceNodeSaveState`, `NPCSaveState`). A root `SaveData` object holds
  one player payload + a list of each world-object type's payloads.
- **Trigger (decided): manual Save button only**, no autosave for v1 —
  a new button, likely in `GameMenuScreen`'s Player tab (already has the
  Male/Female toggle as its first real content) or a new dedicated tab.
  Matches Ben's original framing directly: a deliberate save before
  stopping covers "continue where we're at" fine without autosave-timing
  edge cases to design around.
  - **Update (v0.3.113-dev, 2026-08-16): autosave added, doesn't replace
    the manual trigger.** Ben's ask, prompted by wanting to safely wait
    out a real-time system (the Village Flag's up-to-30-minute spawn
    timer) without needing to remember to hit Save first. `PlayerAutosave.cs`
    calls the same `SaveManager.Save()` every 10 real minutes and shows a
    15-second top-center toast ("Game autosaved.") — same shape as
    `PlayerSkills.cs`'s own tier-unlock toast, offset lower so the two
    can't overlap. The manual Save button in `GameMenuScreen` is
    untouched and still works exactly as before; this is a second,
    automatic trigger layered on top, not a replacement.
- **Load**: on game start, check for an existing save file — if present,
  suppress whatever `PlayerShirt`/`PlayerJeans`/`PlayerBelt`/etc.'s
  starting-gear auto-equip would normally do (it already guards on
  "nothing equipped yet," so this should compose for free rather than
  needing a special case) and restore from the file instead; if absent,
  fall through to today's fresh-start behavior unchanged.

## 7. Multiplayer-readiness note

Per the standing project convention (weigh choices against the eventual
Mirror conversion without blocking on it, `MULTIPLAYER_PLANNING.md`): this
is being built single-player-shaped on purpose — one save file, one
player payload. That's the right v1 scope, not a mistake to avoid. Two
things worth knowing going in, not blocking anything now:

- Under multiplayer, persistence becomes **server-authoritative and
  per-account**, not a single client-local file — `MULTIPLAYER_PLANNING.md`
  already calls persistence out as one of its later phases, genuinely
  blocking once a server is the sole source of truth for a shared world.
- The `SaveId` + `ISaveable` shape proposed here actually **translates
  reasonably well** to that future — same concept, just relocated
  server-side and keyed per-account instead of a single global file. This
  isn't a redesign-later situation, it's a "same pattern, different
  transport" situation.

## 8. Build order

1. **`SaveId` component** + scene-scan registry.
2. **`ItemDatabase`/`SkillDatabase`** registry assets + the Editor
   auto-population script (throwaway, deleted after running, same
   convention as every other batch-mode setup script this session).
3. **`SaveManager`** (Newtonsoft.Json read/write) + `ISaveable` + the root
   `SaveData` shape — no real content yet, just prove the file
   round-trips.
4. **`StorageBox` capture/restore** — simplest real case, validates the
   "world object with `SaveId` + `Inventory`" pattern once (same
   "pilot on `StorageBox` first" instinct `MULTIPLAYER_PLANNING.md`
   independently landed on for its own first phase).
5. **Player capture/restore** — vitals/skills/currency (straightforward)
   then inventory/equipment including the recursive equipment-state piece
   from section 4 (the real time sink).
6. **`ResourceNode` capture/restore** (respawn timer).
7. **Hireable NPC capture/restore** (the most cross-cutting world object —
   hired state + job + tools + cargo + skills + position + a
   cross-reference to its deposit container's own `SaveId`).
8. **Save button UI** (`GameMenuScreen`) + load-on-boot wiring.
9. **Manual test pass**: save mid-session with a real mix of state (some
   inventory, a worn Backpack with contents, a hired NPC with a job and
   partial cargo, a partially-respawning ore node), fully close the
   Editor (not just re-enter Play mode — needs to survive an actual
   process restart to prove anything), reopen, load, confirm everything
   comes back exactly.

## 9. Still open, not yet decided

- Exact UI placement for the Save button (`GameMenuScreen` Player tab
  reuse, vs. a new dedicated tab) — small, decide during step 8, not
  blocking earlier steps.
- Whether v1 needs more than one save slot at all (assumed no — single
  slot matches the "don't restart every test" motivation and the
  single-player-only scope; revisit only if it turns out to matter).
- Exactly how deep the "recursive" equipment capture in section 4 needs
  to go in practice — no current item actually nests more than one level
  (a Backpack holding a Canteen), so the recursion is a correctness
  guarantee, not something expected to matter at 3+ levels; worth
  confirming this assumption once real content is being tested against
  it rather than guessing further now.

## 10. Known future follow-up: skill books (not v1 scope, flagged for later)

`SKILL_BOOKS_PLANNING.md` (MVP2 item 7, designed 2026-08-13, not yet
built) introduces new player state this v1 save system doesn't capture:
`knownLineages`, `bookGrantedRecipes`, `bookGrantedWishes`, and
`SkillBook` item instances sitting in inventory. Once skill books are
actually built, this needs a small follow-up increment, not a redesign:

- `SkillBook` was deliberately designed as an `IEquippable`, so it
  composes almost for free with the recursive
  `EquipmentSaveUtility`/`InventorySaveUtility` capture already built and
  live-tested here — just needs one more type-specific branch (mirroring
  the existing `Canteen` case) for its
  `targetRecipe`/`targetWish`/`bonusLevel` fields.
- Three new fields on `SaveManager.CapturePlayer`/`RestorePlayer` for
  `knownLineages`/`bookGrantedRecipes`/`bookGrantedWishes` (each
  resolved through `ItemDatabase`/a similar recipe/wish ID lookup, same
  pattern section 5 already established for `ItemDefinition`/
  `SkillDefinition`).

Not urgent — skill books aren't built yet — but flagged here so it isn't
discovered later as a "why didn't my skill books survive a reload" bug.

## Cross-references

- `BUGS_AND_ENHANCEMENTS.md`'s "Save/load persistence (v1, deliberately
  narrow scope)" section — the original draft this doc supersedes with
  real decisions; that section's own three open forks (reference
  resolution, stable identity, proposed shape) are now resolved here.
- `MVP2_PLANNING.md` item 6 — this doc is that item's real implementation
  plan.
- `MULTIPLAYER_PLANNING.md` — source for section 7's framing; also the
  reason `StorageBox` was picked as the pilot world object for step 4,
  independently arrived at by both docs.
- `SKILL_BOOKS_PLANNING.md` — source of section 10 above; that doc's own
  "Cross-references against MVP2_PLANNING.md" section states the same
  follow-up from the other direction.
