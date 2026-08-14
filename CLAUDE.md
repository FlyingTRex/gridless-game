# Gridless

First-person survival-crafting game (codename "The Flying T-Rex"), co-designed by
two people (traskmi + Ben) each working with their own Claude Code session against
this shared repo. Unity 6.3 LTS, dedicated-server multiplayer planned.

**Read `CHANGELOG.md` first** when picking up this repo cold — it's a why-focused,
skimmable history of what's been built and non-obvious gotchas hit along the way
(rendering pipeline traps, Editor onboarding fixes, tuning decisions). `git log` has
the full detail; the changelog is the fast path to context. Update it when you land a
meaningful change.

**Check `WORKING_ON.md` before starting a new feature.** Both collaborators' Claude
sessions work against this repo independently, and nothing else catches two sessions
solving the same problem in parallel — a version-bump collision or a fileID
collision self-heals at merge time, but building a whole duplicate system (see the
Waterskin/Canteen entry in `CHANGELOG.md`, 2026-08-02) wastes real design effort.
Add a line before starting non-trivial work; remove it once merged to `origin/main`.
**It's also the running scratch log for that work while it's in progress** — keep
adding detail to the entry as you go (not just the original one-liner). Don't touch
`GameVersion`/`CHANGELOG.md` for each individual step along the way — only bump the
version and write the real changelog entry once actually committing, using the
accumulated `WORKING_ON.md` detail as the source material, then clear that entry.

**Backlog lives in `BUGS_AND_ENHANCEMENTS.md`.** Known issues and requested
features not currently being worked — the space between `WORKING_ON.md` (active)
and `CHANGELOG.md` (shipped). Move an entry to the changelog once it's actually
fixed/built.

**Manual test checklist lives in `TEST_FEATURE_PLAN.md`.** Covers every shipped
player-facing feature with concrete steps/expected results, meant to be walked
through in a live Play-mode session. Add an entry for new features; update an
existing entry when behavior changes; add a **Regression** line under an entry
when a bug slips through it, so the next full pass specifically re-checks that
failure mode.

**MVP 2 scope lives in `MVP2_PLANNING.md`.** A curated subset of
`BUGS_AND_ENHANCEMENTS.md`'s broader Phase 2 backlog — that file still holds
the full long-term list, but `MVP2_PLANNING.md` is the actual next-up scope
Ben picked (2026-08-12) and the live surface for fleshing it out and deciding
build order.

**Medical system evaluation lives in `MEDICAL_SYSTEM_PLANNING.md` +
`MEDICAL_FAMILIES.md`.** Evaluation of a proposed 50-item medical
progression against what's actually built (2026-08-12). The list's 5 tiers
map directly onto the existing `CraftTier` enum (Crude→Masterwork), and its
sci-fi-tier items are scoped as Master Physician/"Ruins of the Old
Engineers" endgame content (see `docs/design-brief.md`'s "Endgame: Leaving
the Planet") rather than a setting mismatch — both resolved after the first
evaluation pass. `MEDICAL_FAMILIES.md` holds the per-item breakdown of which
items collapse into one `CraftTier`-ladder family vs. stay standalone.
Planning only, not yet scoped into a version.

**Craft tier color-coding + Crafting screen sort/filter lives in
`CRAFT_TIER_COLORS_PLANNING.md`.** Decided 2026-08-12, includes a link to
the approved mockup artifact: a 5-color palette (Crude=Gray through
Masterwork=Gold, including Normal), applied as a slot/tile border + tier-
colored item name text (not a full `GUI.color` tint, which would also
recolor icon art). Also specs the Crafting screen's new tier-ascending
default sort plus a tier filter row.

**Wood & Fuel system planning lives in `WOOD_AND_FUEL_PLANNING.md`.**
Audits the current wood item chain (Tree→Log→Plank/Stick; Log is a
stationary chop node, never a pickupable item) and designs a `FuelTier`
system for the Furnace (2026-08-12): tier is burn-duration efficiency
only, never a recipe gate — Stick and all 5 Trimmed Stick craft-tiers are
Tier 1 (5 min), Plank is Tier 2 (10 min), Coal/Gas/Electricity reserved as
future Tiers 3-5. Also specs Furnace state that doesn't exist yet
(lit/unlit, a fuel inventory, a real-time burn timer — burns continuously
once lit, independent of active crafting) and a longer-term autonomous
production-chain vision (NPC-fed storage → Furnace auto-smelts) flagged as
its own future scope, not part of the near-term build.

**Campfire redesign lives in `CAMPFIRE_PLANNING.md`.** Turns the current
single magic-only scene prop into a real craftable/placeable, fuel-burning,
cooking structure (2026-08-12): reuses the `FuelTier`/`FuelItem` system
built for the Furnace (1 fuel slot), adds a 1-slot cooking mechanic (new
`CookableItem` type, Raw Meat → Cooked Meat while lit and nearby, no
accessory required — Raw Meat currently has no `EdibleItem` at all, so it
can't be eaten raw), plus 4 accessory slots (Grill/Soup Pot/Kettle/Frying
Pan, all equippable at once) that gate additional recipes the way
`CraftingRecipe.requiredTools` already gates ordinary crafting. Also gives
Body Temperature its first real gameplay effect (currently 100%
decorative) plus a spot on the actual HUD. Also specs a Blender rebuild of
the model (ring of rocks + charred wood, replacing the pre-Blender
placeholder). Spark becomes an alternate way to light a placed Campfire,
not the only way one can exist.

**Multiplayer conversion exploration lives in `MULTIPLAYER_PLANNING.md`.**
Audits the single-player codebase (2026-08-13) against what converting to
dedicated-server multiplayer via Mirror Networking (imported this session)
would actually require — 32 `PlayerXXX.cs` scripts assume exactly one
local player, only `StorageBox` maintains a live registry (everything else
scene-scans via `FindObjectsByType`), zero save/load persistence exists
anywhere, and the 22 `OnGUI`-based screens turn out to need no structural
change at all (IMGUI is already inherently per-client). Nothing here is a
locked architecture — it's a phased proposal (infra spike → one pilot
networked world object → player-authoritative gameplay → NPCs server-side
→ persistence) plus a list of real open questions (movement-authority
model, dev/test workflow, scope shape). Mirror was picked over PurrNet
mainly because PurrNet's stated minimum Unity version is newer than this
project's pinned one.

**NPC job generalization (Mining + Woodworking + Berry/Herb foraging) is
built — see `NPC_JOB_GENERALIZATION_PLANNING.md`.** Ben's ask (2026-08-13):
let the player assign an NPC to Woodworking or foraging (and eventually
any craft family except Building), same as Mining today. Audit found the
Hireable NPC system already mostly generic (`NPCJobDefinition`/
`NPCJobScreen` are pure data-driven, `NPCMining`'s loop already targets
any `ResourceNode` and trains whatever skill the assigned job names) — the
real gaps were standing Trees (`ChoppableTree`) using a different
interaction shape than `ResourceNode`, and Berry/Herb bushes (`BerryBush`/
`HerbBush`) not yielding an item directly at all (their search action only
scatters `Pickup` objects onto the ground — a genuine two-step
search-then-collect task, not a shortcut opportunity). Built same day
(v0.3.32-dev, committed and pushed): a shared `INPCHarvestable` interface
for direct-yield targets (ResourceNode, ChoppableTree) plus a separate
`INPCSearchable` interface for trigger-only targets (BerryBush/HerbBush's
search half only — their chop-for-stick action stays player-only),
`Pickup.cs` gained an NPC-safe collection path, and `NPCMining.cs` was
renamed to `NPCGathering.cs` (GUID preserved) with a target search
spanning all three pools. Bench-crafting families (Metalworking, Sewing,
etc.) are sketched in the planning doc but explicitly deferred to a later
pass. Verified via compile + YAML grep only so far — no live Play-mode
confirmation yet.

**Save/load persistence real implementation plan lives in
`SAVE_LOAD_PLANNING.md`.** Expands the narrow v1 draft in
`BUGS_AND_ENHANCEMENTS.md` (and `MVP2_PLANNING.md` item 6) into a buildable
plan (2026-08-13): a `SaveId` component + scene-scan registry for world-
object identity, `ItemDatabase`/`SkillDatabase` lookup assets for
resolving `ItemDefinition`/`SkillDefinition` references by stable string
ID, `ISaveable` + a `SaveManager` writing Newtonsoft.Json (chosen over
built-in `JsonUtility` for native Dictionary/nested-object support) to
`Application.persistentDataPath`, and a manual Save-button-only trigger
for v1 (no autosave). The hardest piece is full recursive nested-equipment
capture — a worn Backpack/Boot/Belt can itself hold another equipped item
(a Canteen clipped to a Belt or stashed in a Backpack), each with its own
state beyond a plain `ItemDefinition` reference. Loose world pickups,
built structures, and Lockbox/Bank contents are explicitly deferred out of
v1. Planning only, not yet built.

**Skill books are built — see `SKILL_BOOKS_PLANNING.md`.** MVP2 item 7,
designed and built 2026-08-13 (v0.3.53-dev, committed and pushed).
Reading/writing became a direct trigger on Intelligence (mirroring
`PlayerEncumbrance`'s Strength pattern); a crafting/weapon skill book
grants one specific `CraftingRecipe` as a standing exception to the normal
skill gate (never a level/XP boost); a magic wish book (e.g. "Fireball")
does the same for a `WishRecipe` *and* unlocks its lineage if not already
known — confirmed against `PlayerMagic.cs` as one unified mechanic, not
two separate systems. Writing reuses `PlayerCrafting`'s `CraftOutcome`
roll directly (margin = author's Intelligence vs. the subject's tier
requirement), extracted into shared `CraftOutcomeRoll.cs`; a catastrophic
writing failure destroys the book and damages the author (2–10), while
only the best outcome grants a lineage tome a random 1–10 head start above
0. `PlayerMagic.IsLineageKnown` now checks a real `knownLineages` set
instead of a single `StartingLineage` field, so a player can know more
than one lineage. Rare magic-teaching NPCs and NPCs writing/reading their
own books are both explicitly deferred to a later MVP (blocked on NPC
bench-crafting for the crafting-book half). Verified via compile + YAML
grep only so far — no live Play-mode confirmation yet, see
`TEST_FEATURE_PLAN.md` section 31. Rendered summary:
https://claude.ai/code/artifact/2af217f7-450e-4e4b-9b09-6411a8b72115

**Weather Maker is built and live-tested — see `WEATHER_MAKER_PLANNING.md`.**
MVP2 item 5, built 2026-08-13. Digital Ruby's Weather Maker (v8.1.0,
`Assets/WeatherMaker/`) replaced the old procedural sky texture (deleted —
see the `Mathf.SmoothStep` gotcha above, now resolved by replacement, not
by fixing the math in place). URP Render Pipeline Asset now points at
`WeatherMakerURPProfile`; color space switched Gamma → Linear (both
project-wide, both explicitly confirmed with Ben first). Player object
gained an `AudioListener` + kinematic `Rigidbody` + tiny trigger
`SphereCollider` (Weather Maker uses this to identify the local player).
New `PlayerWeatherEffects.cs` bridges live precipitation intensity to
`PlayerVitals.bodyTemperature` via the existing `WarmNear` (no separate
"cool" method needed — it's already symmetric). Ben watched a full
day/night cycle live end to end (day → sunset → night → moon) — real
confirmation, not just a clean compile. Three real bugs hit and fixed
along the way: two missing built-in Unity modules (`com.unity.modules
.wind`/`screencapture`), a Mirror API version mismatch in Weather Maker's
optional (and out-of-scope) network-sync script, and a shipped day/night
profile with `Speed`/`NightSpeed` both frozen at `0` — found live when
asked "how long until night" and the honest answer required reading the
binary asset directly, not guessing.

**Dexterity & Constitution are built — see
`DEXTERITY_CONSTITUTION_PLANNING.md`.** Follow-up to Strength/Intelligence,
designed and built same session (2026-08-14, v0.3.55-dev). Constitution grows Max
Health/Max Stamina via an additive front-loaded curve (`100 + k ×
(Constitution-2)^1.5` — a pure power law couldn't hit both a sane low
anchor and a front-loaded shape at once, worked out live in the doc),
trained by exercise (sprinting, plus a secret bonus on soccer kicks) rather
than the originally-sketched adversity triggers. Dexterity drives movement
speed as one more multiplier in `FirstPersonController`'s existing
`speed = baseSpeed * staminaMultiplier * encumbranceMultiplier` chain,
trained by sprinting/sneaking/jumping plus completing any `CraftingRecipe`
— the manual-vs-machine distinction Ben wanted (hand-crafting trains it,
Furnace/Campfire automation doesn't) turned out to need no new field at
all, since `CraftingRecipe` vs. `SmeltableItem`/`CookableItem` already is
that exact boundary in the data model. Also folds in a small refinement to
the already-shipped Intelligence system: a small (+5% at cap) global XP
multiplier on every *other* skill's gains, superseding the original
`BUGS_AND_ENHANCEMENTS.md` sketch's much bigger (+50%) version.

**Fame system input side is fully designed — see `FAME_PLANNING.md`.**
Worked out 2026-08-14, planning only, nothing built yet. A single
-1000-to-1000 Fame float (not per-trade, despite the original design-brief
framing), fed by NPC treatment (hire +1, fire -0.5, unpaid wages -0.5 per
missed cycle, killing any humanoid NPC -10 — the last one blocked on hired
NPCs not implementing `IDamageable` at all yet), player death (-2), and
skill-tier mastery in any discipline including the core stats (Rudimentary
+1 through Masterwork +5, reusing `PlayerSkills`' existing tier-unlock
detection for free — the "everyone knows the Hulk for his strength" case
needed no new component, just confirming the hook isn't scoped to exclude
the `Attribute` skill category). Guild membership adds Join +1/Leave -1
(flat and symmetric, hooking `PlayerGuilds.Join`/`Leave`, which already
work today) plus Start-a-guild +3/Close -6, both blocked on a
player-driven guild-creation mechanic that doesn't exist yet
(`GuildDefinition` is a plain pre-authored asset, not player-creatable).
Business-reach Fame (Inn/Trader,
scaling with customers served) is designed but blocked on an entire
commerce/vendor system that doesn't exist anywhere in this project yet —
the biggest prerequisite gap in the doc. Output side has two real effects
designed too: negative Fame makes every NPC (including already-hired
ones) flee within ~10m, pausing their current job until the player
leaves; and a 5-band Fame system (Infamous/Notorious/Neutral/Known/
Renowned, mirroring `CraftTier`'s own 5-tier shape) scales a Traveling
Trader's visit frequency and pricing, with item quality only improving
at the top Renowned band. Both blocked on real prerequisites (an
`NPCFlee` component; the same nonexistent vendor system). The design
brief's original per-trade "better prices/rarer game/luckier mining"
examples still don't cleanly carry over now that Fame is a single overall
number — that part's still open.

## Design docs (`docs/`)

Read these directly rather than trusting a summary — they're actively evolving:
- `design-brief.md` — the systems/technical design brief (world scope, multiplayer
  architecture, Phase 1/2/3 MVP roadmap, magic, factions). The current build-order
  reference: work through the Phase 1 list before Phase 2.
- `game-overview.md` — narrative/setting pitch.
- `reconciliation-questions.md` — record of decisions made reconciling the above two
  when they first diverged.
- `skill-path-space.md` — endgame skill-tree spec.

## Project conventions

- **Unity version is pinned** — check `ProjectSettings/ProjectVersion.txt` before
  assuming a version; don't silently bump it.
- **New Input System only** (`activeInputHandler: 1`), no `.inputactions` asset yet —
  scripts read `Keyboard.current`/`Mouse.current` directly.
- **Single scene today**: `Assets/Scenes/TestScene.unity`, registered as scene 0 in
  `EditorBuildSettings` and auto-opened by `Assets/Editor/SceneAutoOpen.cs` when the
  Editor has none loaded (fixes a real empty-world bug on fresh clones — see
  changelog).
- **Scene/asset edits from a headless session**: these sessions can't drive the Unity
  Editor GUI. The established pattern is: write a throwaway `Assets/Editor/*.cs`
  script with a static method, run it via
  `Unity.exe -batchmode -nographics -quit -projectPath <path> -executeMethod <Class.Method> -logFile <path>`,
  check the log for `CS####` compile errors, verify the result by grepping the saved
  `.unity`/`.asset` YAML for the expected fields, then **delete the throwaway
  script** (keep `Assets/Editor/` free of one-off setup code — `SceneAutoOpen.cs` is
  the one permanent exception).
- **Unity locks the project while its Editor is open** — batch mode will fail with
  "another Unity instance is running." Ask before assuming it's safe to run.
- Materials created at runtime (`new Material(Shader.Find(...))`) embed fine directly
  into a **scene** file but not reliably into a **prefab** — save as a real `.mat`
  asset via `AssetDatabase.CreateAsset` first, then reference it, or the prefab
  renders pink.
- **Bump the version number on every commit that touches gameplay code or scenes**
  (`Assets/Scripts/**`, `Assets/Scenes/**`, `Assets/Prefabs/**`). Two places must
  match, updated together in the same commit:
  - `GameVersion` in `Assets/Scripts/FirstPersonController.cs` (shown on-screen in
    the bottom-left debug panel)
  - the **"Current version"** line near the top of `CHANGELOG.md`

  Format: `MAJOR.MINOR.PATCH-dev` — increment PATCH for a normal commit; bump MINOR
  by hand for a completed Phase 1 milestone; MAJOR stays `0` until there's a real
  release. Doc-only commits (design docs, changelog, this file) don't need a bump.
  Both collaborators' Claude sessions should follow this — the in-game number and the
  changelog are meant to be cross-checkable at a glance, without digging into git.
- **Update `GameMenuScreen.ControlsList` whenever a new key mapping is added**
  anywhere in the game (`` ` `` to open — Player/Audio/Graphics/Controls/Credits
  tabs, added 2026-08-04). The Controls tab is meant to always reflect every real
  binding, alphabetized by key name; a new hotkey isn't done until this list
  says so too, same spirit as the changelog/version-bump rule above.

## Checklist: adding a new `IEquippable` (worn item)

This exact class of bug has recurred across sessions — Backpack got these fixes
first, then four more equippables (Canteen, Sunglasses, Navigation Computer,
Health Monitor) shipped without them and needed the identical fix applied
retroactively (2026-08-03). When adding a new physical object that can be worn
(implements `IEquippable`, gets carried via `SetCarried`), copy the pattern from
`Backpack.cs`, specifically:
- **Hide it from the player's own camera while worn.** `SetCarried(true, ...)`
  must set the object (and all children — use `SetLayerRecursively`) to the
  `WornEquipment` layer (project layer 8); `SetCarried(false, ...)` must set it
  back to `Default` (layer 0). Without this, turning to look at your own worn/held
  gear shows it filling the screen from ~0.5 units away. The layer itself and the
  `Main Camera`'s `cullingMask` excluding it already exist project-wide — this is
  a per-script fix, not a scene/project-settings change.
- **If it's a plain (non-equippable) `ItemDefinition`** rather than a physical
  `IEquippable`, give it a `worldPickupPrefab` if it should look distinct when
  dropped — otherwise it silently falls back to the generic gray `DroppedItem`
  cube. Not always wrong (e.g. Rock Hammer currently does this on purpose), but
  should be a deliberate choice, not an oversight.

## Gotcha: generic `Inventory.RemoveItem`+`AddItem` strip an item's `equipment`
reference

They're built for plain stackable resources and don't know `IEquippable` exists.
Calling them on a slot that might hold one (a Canteen, Backpack, Sunglasses, etc.)
removes the real object's slot entry and replaces it with a plain count-only stack of
the same `ItemDefinition` — the physical object itself is orphaned (usually still
visibly attached to the player, just with no inventory reference to it anymore, and
no way to interact with it).

**`InventoryTransfer.Move` handles this automatically now (2026-08-03, take 2).** It
detects when the item being moved has a live `equipment` reference and routes through
`AddEquipmentItem`/`RemoveEquipmentItem` instead, preserving the object. The first
attempt at this fix (v0.1.34-dev, same day) added a blanket guard that refused the
move entirely whenever *any* slot held an `equipment` reference — too broad, it also
blocked ordinary item moves and got reverted, silently leaving the underlying bug
unfixed despite the changelog describing it as resolved. Root-caused for real via a
second bug report (Sunglasses moved from a backpack to a hand through the inventory
screen's move popup, then couldn't be equipped) that turned out to be the identical
issue. The new fix is scoped to just the specific item/slot being moved, not a
blanket refusal.

**Update (v0.1.51-dev):** `PlayerDropping.DropFrom` is now equipment-aware too, same
pattern as `Move()` — it was the last caller that reached for the generic
`RemoveItem`/`AddItem` pair directly. Any *new* code that reaches for those two
methods directly on a slot that might hold an equippable is still on the hook to
check `slot.equipment` first and route through the type's own carrier
(`PlayerBackpack.Drop`, `PlayerCanteen.Equip`, etc.) — the fix lives in the two call
sites that had it, not in `Inventory` itself.

## Gotcha: a changed `[SerializeField]` default doesn't apply to existing scene/prefab instances

Unity serializes the field's *current value* onto a GameObject the first time the
scene/prefab is saved. Changing the C# default afterward only affects **new**
instances — anything already placed in `TestScene.unity` (or baked into a prefab)
keeps whatever value was captured at that point, silently overriding the new code
default with no error or warning. Hit twice in one session (2026-08-03):
`PlayerVitals.overdrinkSicknessThreshold` (scene had `100` baked in from the
original field addition; code default changed to `125`, scene kept overriding it
back to `100`) and `Canteen.fillRange` (same pattern, `2` → `4`). Both looked like
the code fix didn't work at all when actually it just never took effect.

**How to apply:** after changing a numeric/bool default on an existing
`[SerializeField]`, grep `Assets/Scenes/*.unity` and any relevant `.prefab` for the
field name — if it's already serialized there with the old value, the scene/prefab
needs the same edit, not just the script. This is also why the Editor needs a scene
reload after a script-only fix that touches a field's default: a currently-open
scene keeps its in-memory (pre-fix) values until reloaded from disk, so testing
immediately after a fix that *did* touch the scene value can still show the bug if
the Editor hasn't picked up the on-disk change yet.

## Gotcha: `GUILayout.Button` fires on *any* mouse button, not just left-click

Unity's legacy IMGUI doesn't filter by mouse button — a right-click (or middle-click)
lands on a `GUILayout.Button` exactly like a left-click would, as long as the cursor
is inside its rect on both press and release. This is surprising because right-click
is also legitimately used elsewhere in this project (`PlayerRenaming`), so players
reach for it out of habit.

Confirmed as the root cause of a real bug (2026-08-03): `InventoryScreen`'s Back-slot
equipment row (Unequip/Drop buttons) sits directly above the backpack's nested
contents grid (`DrawContainerContents`) with very little vertical gap, and the grid's
own horizontal indent doesn't line up with the row above it — a middle slot in the
grid can land under the Unequip/Drop button column. A right-click aimed at an item
*inside* the backpack could silently drop or unequip the backpack itself instead.
Spacing helped but didn't eliminate it (confirms the mechanism, doesn't fix it).

**The actual fix, not just mitigation:** `InventoryScreen.SafeButton(...)` wraps
`GUILayout.Button` and only returns `true` when `Event.current.button == 0` (left
click). Applied to every Equip/Unequip/Drop button in the screen — the ones where an
accidental trigger changes real state. Reach for this instead of `GUILayout.Button`
directly for any *new* button in this screen (or any other `OnGUI` panel) whose
accidental click has a real consequence; plain `GUILayout.Button` is still fine for
low-stakes clicks (opening a popup, a Cancel button).

## Gotcha: asset references can go stale across a `PrefabUtility.LoadPrefabContents`/`UnloadPrefabContents` cycle, not just `OpenScene`

The existing "don't cross an `OpenScene` call" rule above isn't the only operation
that can silently invalidate an in-memory `ScriptableObject`/`GameObject` reference
in a batch-mode editor script. Hit for real (2026-08-04, Copper Ore/tools bundle): a
script created several assets (`ItemDefinition`s, `CraftingRecipe`s) early on, then
called `PrefabUtility.LoadPrefabContents` / `SaveAsPrefabAsset` / `UnloadPrefabContents`
on an unrelated prefab (`Tree.prefab`, to add a component to it), then tried to use
those *earlier* references in a later method — two of them (`ItemDefinition` for a
required-tool field, two `CraftingRecipe`s being appended to an array) silently
serialized as `{fileID: 0}` (null) instead of throwing, with **no compile error and
no exception in the log** — the batch run reported success. A `SerializedProperty`
array insert on a null reference doesn't throw either; it just writes an empty
array slot, which went undetected until directly grepping the saved YAML for the
expected guid and finding it missing.

**Not fully understood, and not consistent:** other references created at the exact
same point in the script, then also used *after* the same `LoadPrefabContents`/
`UnloadPrefabContents` cycle, survived fine (e.g. a chunk-prefab reference and a
`SkillDefinition` used inside the same call that broke the tool reference). No
confirmed theory for why some survive and others don't — possibly related to
whether something else keeps the reference "hot" in between, but that's a guess,
not a verified mechanism.

**How to apply:** don't trust *any* `ScriptableObject`/`GameObject`/`Material`
reference that was created or loaded before a `PrefabUtility.LoadPrefabContents`/
`UnloadPrefabContents` cycle (or an `OpenScene` call) if it's used *after* one,
even within the same method or the same overall script run. Re-fetch via
`AssetDatabase.LoadAssetAtPath` immediately before the point of use instead of
carrying the value across the boundary as a variable/parameter. After any script
that does both prefab-content editing and scene editing in the same run,
**verify by grepping the saved YAML for the actual expected guid** — don't trust
"the script logged success with no exceptions" as proof the references landed
correctly; a stale reference degrades silently to `{fileID: 0}`, not a thrown
error.

## Gotcha: `Mathf.SmoothStep(a, b, t)` is not GLSL's `smoothstep(edge0, edge1, x)` despite the identical-looking name

Hit for real (2026-08-04, ore textures): every procedural-texture script this
session that wanted "0 below a threshold, 1 above another threshold, smooth
transition between" (i.e. a coverage/fleck mask from a raw noise value) called
`Mathf.SmoothStep(low, high, rawNoiseValue)`, modeled on GLSL/HLSL's
`smoothstep(edge0, edge1, x)` — which clamps `x` against the `[edge0, edge1]`
*input* range, remaps it to `[0, 1]`, then smooths. **Unity's `Mathf.SmoothStep`
does something different**: `t` (the third argument) is treated as an
*already-normalized* `[0, 1]` progress value, and the first two arguments are the
**output value range** to interpolate between —
`t = Clamp01(t); t = smooth(t); return from + (to - from) * t;`. Passing a raw,
unnormalized noise value as `t` doesn't threshold anything; it just remaps every
pixel's noise into a narrow output band uniformly, regardless of the noise's
actual magnitude — the opposite of a sparse threshold.

**Symptom:** a procedurally generated "flecked" texture (ore, and likely
anything else built this session using the same pattern) came out looking like a
near-solid single-color wash instead of sparse, distinct speckles — and *raising*
the threshold values didn't fix it, because the bug isn't about picking the wrong
threshold, it's that the call never thresholds anything at all. Confirmed by
generating standalone test-swatch textures and inspecting the actual pixel output
directly (Claude can read image files) *before* touching any real game asset —
much faster than the round-trip of applying it in-game and waiting for a
screenshot to find out it's still wrong.

**The fix — implement GLSL-style thresholding by hand, don't reach for
`Mathf.SmoothStep` for this purpose:**
```csharp
private static float SmoothThreshold(float x, float edge0, float edge1)
{
    float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
    return t * t * (3f - 2f * t);
}
```
Call as `SmoothThreshold(rawNoiseValue, low, high)`, not
`Mathf.SmoothStep(low, high, rawNoiseValue)`.

**Was affected, now moot:** the sky texture's cloud coverage
(`GenerateSkyTexture.cs`, deleted after running, shipped v0.1.55-dev through
v0.1.57-dev) used the exact same `Mathf.SmoothStep(low, high, cloudNoise)`
pattern for both the cloud-coverage mask and the vertical fade band. The clouds
being persistently faint/hard-to-see across three tuning rounds that night — see
`BUGS_AND_ENHANCEMENTS.md`'s sky-texture entry — was very likely this exact bug,
not (only) a frequency/contrast problem as diagnosed at the time. **Resolved
2026-08-13 by replacement, not by fixing the math in place**: the whole
procedural sky system (`Assets/Data/Sky.mat`/`Assets/Textures/SkyTexture.png`)
was deleted once Weather Maker's own sky/cloud system was live-confirmed
working — see `WEATHER_MAKER_PLANNING.md`. The `SmoothThreshold` pattern above
is still the right fix for any *future* procedural-texture code that hits this
same trap; it just no longer applies to this specific instance.

## Gotcha: an imported model's pivot is not reliably at its base — check actual mesh bounds before placing it at `y = 0`

Hit for real (2026-08-06): a third-party `.glb` (`Big Tree by 3Donimus`, from
Poly Pizza — same applies to AI-generated `.glb`s, e.g. via the Tripo3D
tooling in `Tools/Tripo3D/`) was instantiated in `TestScene.unity` at
`(x, 0, z)`, same convention as every hand-placed procedural object in this
project. It rendered sunk into the ground — roughly a third of the model
below the visible terrain. **Cause:** unlike this project's own procedural
meshes (always authored/generated with their pivot at the base), an
imported third-party or AI-generated model's pivot can be anywhere — center
of the bounding box is a common default for scan/export/generation
pipelines that don't know or care about a "base" — so `y = 0` doesn't mean
"sitting on the ground," it means "the model's origin point is at ground
level," which is a different thing entirely.

**Symptom:** the object visually clips into the ground/terrain by some
fraction of its height, worse the further off-base the pivot is. Easy to
misread as a scale or terrain-collision problem instead of a pivot problem.

**The fix — measure actual world-space bounds across every renderer, don't
guess or eyeball an offset:**
```csharp
var renderers = instance.GetComponentsInChildren<Renderer>();
Bounds worldBounds = renderers[0].bounds;
foreach (var r in renderers) worldBounds.Encapsulate(r.bounds);

float groundOffset = -worldBounds.min.y; // how far below y=0 the mesh's lowest point currently sits
instance.transform.position += new Vector3(0f, groundOffset, 0f);
```
Applies to **every** imported model placed in a scene going forward, not
just this one — procedural objects built in this project don't need this
(their pivot is already correct by construction), but anything pulled in
from outside (Tripo3D, Poly Pizza, or any future source) should have its
bounds checked before assuming `y = 0` is correct.

**Recurred despite being documented here** (2026-08-12, `CrudeFurnace.glb`,
v0.3.8-dev → fixed in v0.3.9-dev): placed with a base-pivot assumption and not
checked at the time, so it sat at a slightly wrong height until traskmi's
live look at it (asking to scale it up) prompted a re-inspection. **This is a
mandatory checklist step for placing any new imported model, not just advice
to remember if something looks wrong** — run the bounds check above as part
of every placement script, before the model is considered done, not only
when the sink is visually obvious.

## Rule: scale every generated model against the player, not against its own raw import size

Tripo3D (and likely any other text-to-3D source) does **not** generate
models at real-world scale relative to each other — each generation comes
back normalized to roughly its own unit-cube-ish bounding box regardless of
whether the prompt describes something pocket-sized or room-sized. Confirmed
by the actual numbers seen across generations in this project: the Furnace
needed scaling **up** 2x after import (read as too small next to the
player/Anvil), while Combat Boots needed scaling **down** — the raw import
measured roughly 0.93 x 1.00 x 0.98, i.e. **a boot the size of a washing
machine** (caught live, 2026-08-12, Ben: "way too big"), despite both
coming out of the same pipeline with no scale hint in either prompt. There
is no default that's "usually right" — every model needs its size checked
explicitly.

**The reference is the player, not intuition about the mesh.** This
project's player `CharacterController` is height `1.8`, radius `0.4`
(`Assets/Scenes/TestScene.unity`) — 1 world unit = 1 meter, confirmed. Before
considering a new model's import done, compare its measured bounds (same
`Renderer.bounds` measurement the pivot-grounding check above already does)
against a real-world estimate for that object relative to a 1.8m person —
a combat boot is roughly 0.3-0.35m tall, a backpack roughly 0.4-0.5m tall,
a hand tool 0.3-0.4m long, etc. — and scale the prefab (root `localScale`,
uniform) until it lands in a believable range. Sanity-check by imagining
the object physically next to a 1.8m-tall person, not by whether the number
"1.0" happens to look reasonable in isolation.

**How to apply:** this is a mandatory step for every new or regenerated
model brought in via `Tools/Tripo3D` (or any future model source) — not
just a fix for when a live look happens to catch something oversized.
Measure, compare to the player, scale, *then* run the pivot-grounding check
above (order matters — grounding needs the final scale already applied,
since scale changes the measured bounds the ground offset is computed
from).

## Gotcha: a tier-scaling ratio tuned for one quantity doesn't transfer to another

`CraftTierScale.Modifier(tier)` (Crude 0.2x → Masterwork 5x) was tuned for capacity
and price, where a 25x spread top-to-bottom reads as normal RPG-tier-ladder scaling —
a Masterwork Lockbox holding 25x a Crude one's capacity, or costing 25x as much, both
feel right. Encumbrance (2026-08-10) needed the *opposite* relationship for a related
but different quantity — better-made gear should be **lighter**, not heavier — and
the first attempt just inverted the existing table (`weight = base / Modifier(tier)`).
That produced a 25 lb Crude Backpack and a hypothetical 5 lb Crude Knife (a Normal
Knife weighing 1 lb) — Ben's call: "a 5lb knife would be horrible... a 25lb backpack
would be terrible as well." The ratio that's sane for "how much more capacity/value"
is a wildly different claim from "how much heavier" — a 25x spread on weight reads as
broken, not epic.

**The fix — a dedicated table, not a repurposed one:** `CraftTierScale.WeightModifier(tier)`
is deliberately narrow (Crude 1.5x, Rudimentary 1.2x, Normal 1x, Fine 0.8x,
Masterwork 0.6x) — better tiers still get lighter, but by a believable amount.
`weight = normalTierWeight * WeightModifier(tier)`. Applied first to the Backpack
ladder (Normal = 5 lbs → Crude 7.5, Rudimentary 6, Fine 4, Masterwork 3 lbs) —
apply the same table to any other tiered `ItemDefinition` that should get lighter
with quality (tools are the obvious next candidate).

**How to apply:** before reusing *any* existing per-tier scale for a new quantity,
compute the actual resulting numbers at both ends of the tier ladder and sanity-check
them in real units (lbs, seconds, coins) — don't assume a ratio that's correct for
one quantity is correct for another just because both are "tier scaling." When in
doubt, give the new quantity its own table (see `WeightModifier` for the pattern)
rather than a formula derived from an unrelated one.

## Gotcha: per-instance runtime data on a `MonoBehaviour` needs `[SerializeField]`
*and* an explicit `RecordPrefabInstancePropertyModifications` call to survive a
batch-mode scene save

Hit live (2026-08-13, `SkillBook.cs` — see `SKILL_BOOKS_PLANNING.md`): a script
whose per-instance state (which `CraftingRecipe`/`WishRecipe` a written book
targets) is only ever set at runtime, never hand-authored in the Inspector, is easy
to write as plain C# auto-properties (`public CraftingRecipe TargetRecipe { get;
private set; }`) instead of `[SerializeField]` fields — nothing about writing it
that way looks wrong, and it works perfectly for the duration of a single Play
session, since in-memory field values don't need Unity's serializer at all while
the game is just running. It silently breaks the moment that same object needs to
survive a **saved scene file** instead — for example, placing a pre-configured
instance directly into `TestScene.unity` at edit-time (a "found" item sitting in
the world) rather than spawning it at runtime. A plain auto-property is invisible
to Unity's scene serializer; the object still reports a fully successful
instantiate-and-save with no error, but reloading the scene comes back with the
field silently reset to its type default (`null`/`0`), even though nothing in the
log ever said so.

**Two fixes needed together, not one:**
1. Back the property with a real `[SerializeField] private` field
   (`[SerializeField] private CraftingRecipe targetRecipe;` +
   `public CraftingRecipe TargetRecipe => targetRecipe;`), so the value is at least
   part of the object's serialized data at all.
2. **Still not sufficient on its own** for an object created via
   `PrefabUtility.InstantiatePrefab` in a batch-mode script — a plain C# field
   assignment on a prefab instance's component (`component.someField = x`) doesn't
   automatically register as a serializable prefab-instance *override* the way an
   Inspector edit or a `SerializedObject`-based change does. The fix needs an
   explicit `PrefabUtility.RecordPrefabInstancePropertyModifications(component)`
   call immediately after setting the field, or the change still silently doesn't
   make it into the saved scene YAML despite step 1 being correct.

**How to apply:** for any new component with runtime-only state that might ever be
authored into a saved scene/prefab (not just set-and-read within one Play
session), use real `[SerializeField]` fields from the start, not auto-properties —
and for any batch-mode Editor script that instantiates a prefab and configures its
fields via direct C# rather than `SerializedObject`, call
`PrefabUtility.RecordPrefabInstancePropertyModifications` afterward. Verify by
grepping the saved scene YAML for the actual field name under that
`PrefabInstance`'s `m_Modifications` (`propertyPath: <fieldName>` — not a bare
`fieldName: value` line, which is only how it looks for a fully-serialized
component, not a prefab-instance override) — not by trusting "the script logged
success," same discipline as every other stale-reference gotcha in this file.

## Gotcha: a batch-mode tool run without `-nographics` can silently mutate a
global `ProjectSettings` file as a side effect — don't wave an unexpected diff
through as "benign" without actually testing it

Hit live (2026-08-14, Copper/Silver/Gold/Platinum Ingots, v0.3.57-dev): running
`IconBaker` (which requires a real graphics device — see its own `-nographics`
warning) left `ProjectSettings/GraphicsSettings.asset`'s
`m_LightsUseLinearIntensity` flipped `0` → `1`, an unrequested, unexplained
change picked up alongside the actual intended asset changes. It was
rationalized in the moment as "benign, arguably correct — consistent with the
project's Linear color space" and committed without loading the Editor to
actually look at anything.

**Correction, same day:** when Ben reported Player/NPC models fully invisible
two commits later, this diff was the prime suspect and got reverted first —
but that revert **did not fix the actual bug** (see the next gotcha below for
the real cause), and the setting drifted back to `1` on its own shortly after
anyway (confirmed intentional/Editor-driven, not something to keep fighting).
The underlying lesson still holds even though this specific diff turned out
innocent: an unreviewed `ProjectSettings` diff is a real enough regression
risk to justify treating with suspicion, but **don't stop investigating once
you've found *a* plausible-looking suspect** — reverting it and moving on
without confirming the symptom actually went away wastes a report cycle and
delays finding the real cause, same failure shape as declaring a fix "done"
off a clean compile instead of an actual look.

**How to apply:** any diff in a `ProjectSettings/*.asset` file that isn't the
change you actually intended is still worth a real hypothesis for *why* the
tool touched it before committing — but if a bug report comes in, verify your
fix actually resolves the reported symptom before considering it closed, not
just that you found and reverted something suspicious-looking.

## Gotcha: a legacy Built-in-only shader can render fully invisible under URP,
not the usual pink "shader missing" indicator — and `Material.GetColor` on a
property the shader doesn't have silently returns transparent black, not an
error

**The real cause of the 2026-08-14 invisible-Player/NPC bug**, found by
walking the Inspector live with Ben (Hierarchy → body mesh child → Skinned
Mesh Renderer → Materials) after the `ProjectSettings` red herring above led
nowhere: all 7 `HumanDummy*.mat` variants (`Assets/Kevin Iglesias/Human
Character Dummy/Materials/`) used the legacy Built-in Render Pipeline shader
`Unlit/Texture` (recognizable by `m_Shader: {fileID: 10752, guid:
0000000000000000f000000000000000, type: 0}` — the all-zeros-with-an-f guid is
Unity's convention for a built-in shader reference, not a real asset guid).
This project has been on URP since early in its history, and a genuinely
Built-in-only shader like this one is incompatible — but instead of the usual
bright pink "shader failed" fallback, it apparently rendered fully invisible
under this project's specific pipeline/lighting setup. Latent since
v0.3.4-dev (confirmed via `git log`/`git status` on the material file — zero
commits or uncommitted changes since the original NPC visual import), so
unrelated to any change from this session; it just never actually manifested
as full invisibility until now, and nobody had looked closely at an NPC's
material assignment specifically before.

**Fixing the shader introduced a second bug in the same edit, from a
different pitfall**: swapping to `Universal Render Pipeline/Unlit` and
migrating properties (`_MainTex` → `_BaseMap`, `_Color` → `_BaseColor`) is the
right move, but `Unlit/Texture` never actually exposed a `_Color` property to
begin with — Unity logged `Material 'HumanDummy' with Shader 'Unlit/Texture'
doesn't have a color property '_Color'` and `Material.GetColor("_Color")`
silently returned `(0, 0, 0, 0)` (transparent black) instead of throwing.
Written straight into the new shader's `_BaseColor`, this made every model
fully transparent all over again — correct shader, correct texture, wrong
tint, same end symptom. Caught by grepping the saved `.mat` YAML for the
actual `_BaseColor` value after the "fix," not by trusting the batch script's
clean log output.

**How to apply:** (1) don't assume a legacy/incompatible shader always shows
pink — it can render fully invisible instead, so "the object is just gone,
no error" doesn't rule out a shader-compatibility problem the way you might
expect. (2) When migrating a material off a legacy shader, don't blindly
carry over every old property by name — check first whether the *old* shader
actually exposed that property (a `GetColor`/`GetFloat`/`GetTexture` call on a
nonexistent property warns but doesn't throw, and returns a zeroed-out
default that will silently corrupt the new value). Verify the migrated
material's actual saved color/texture values in the YAML afterward, the same
"don't trust a clean log" discipline as every other asset-creation gotcha in
this file.
