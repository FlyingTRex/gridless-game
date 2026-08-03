# Changelog

Notable changes to the Gridless project, newest first. Written for whoever (human or
Claude session) picks this repo up next — includes the *why* behind non-obvious
decisions, not just the *what*. Full detail is always in `git log`; this is the
skimmable version.

**Current version:** `0.1.1-dev` — must always match `GameVersion` in
`Assets/Scripts/FirstPersonController.cs` (shown on-screen in the bottom-left debug
panel). Bump both together in the same commit whenever gameplay code/scenes/prefabs
change; see `CLAUDE.md` for the exact rule.

## 2026-08-02

### v0.1.1-dev — Cursor-lock fix, backpack anchor fix, debug panel readability
Fixed a real interaction bug: clicking any on-screen debug button (Equip, craft,
Drop) while the cursor was unlocked would immediately re-lock and hide the cursor
before the click could register, since `FirstPersonController` re-locked on *any*
left-click rather than requiring an explicit toggle. Changed Escape to toggle the
cursor lock both directions instead.

Also fixed the equipped backpack rendering at the player's feet instead of "on the
back" — its `carrySlot` anchor was never wired up, so it fell back to the player
root's zero-offset transform. Added a real `BackpackAnchor` child transform and wired
it in. (What looked like a *third* bug in the same session — the Berry Bush, Water
Puddle, and two stick pickups appearing to float/overlap — turned out to be correct
positions in every case, just a flat featureless plane with no depth cues making
perspective hard to read. Verified each with exact Transform values before touching
anything, rather than guessing fixes for things that weren't broken.)

Debug panels (Inventory, Skills, Vitals, the new speed/version readout) now draw a
solid dark background via a shared `DebugGUI` helper instead of default IMGUI
styling, which had poor contrast against the green ground. That same readability fix
exposed a real, pre-existing layout bug: the Skills and Backpack panel `Rect`s
overlapped the Inventory panel's edges by 10-30px. Harmless with transparent labels,
but visually obvious once every panel had a solid background — moved Skills and
Backpack to clear Inventory's actual bottom/right edges.

Also gave worn equipment (starting with the backpack) a proper first-person
"can't-see-your-own-back" treatment: a new `WornEquipment` layer (project layer 8,
`ProjectSettings/TagManager.asset`), the `Backpack` prefab set to it, and the
player's `Camera.cullingMask` excluding that layer. Without this, turning around to
look at your own back would show the backpack mesh from ~0.5 units away, filling the
screen — not a positioning bug, just the standard reason FPS games hide self-worn
gear from their own camera.

Also established the version-tracking convention itself: `GameVersion` in
`FirstPersonController.cs` and the "Current version" line at the top of this file
must be bumped together on every commit touching gameplay code/scenes/prefabs — see
`CLAUDE.md`.

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
