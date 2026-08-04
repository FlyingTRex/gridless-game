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
