# Changelog

Notable changes to the Gridless project, newest first. Written for whoever (human or
Claude session) picks this repo up next — includes the *why* behind non-obvious
decisions, not just the *what*. Full detail is always in `git log`; this is the
skimmable version.

**Current version:** `0.1.179-dev` — must always match `GameVersion` in
`Assets/Scripts/FirstPersonController.cs` (shown on-screen in the bottom-left debug
panel). Bump both together in the same commit whenever gameplay code/scenes/prefabs
change; see `CLAUDE.md` for the exact rule.

## 2026-08-09

### v0.1.179-dev — Berry Bush searching gets its "super success" bonus: a 2% Berry Seed chance

Closes most of a long-open enhancement request (`BUGS_AND_ENHANCEMENTS.md`,
originally 2026-08-07): "search the berry function... random chance of
finding up to 4 berries. additionally, a super success chance of
finding a berry seed." The base random-yield search already existed
(v0.1.169-dev); this adds the missing second half.

`BerryBush.CompleteSecondary` now rolls a separate, independent chance
(`berrySeedChance`, `[Range(0,1)]`, wired to 0.02 = 2%) on every search,
regardless of the normal 0-3 berry roll's outcome — a search that finds
zero berries can still find a seed, and a full-yield search can find
one too. Deliberately independent rolls, not a bonus conditioned on
"finding the max," since nothing in the original ask implied that
coupling.

New Berry Seed item, modeled the same way as the recent Blender props:
a small teardrop/almond shape built via `bmesh` (136 verts, one
material, dark reddish-brown). Two real bugs hit building it, both
from working at a genuinely tiny scale (0.014m long) for the first
time this session:
- The Blender preview render came back blank — `obj.bound_box` read as
  a stale zero-size box with no depsgraph evaluation pass between
  building the mesh and reading it back; switched to computing bounds
  directly from `mesh.vertices` instead.
- Still blank after that fix — the real cause was the camera's default
  near-clip plane (0.1m), well *larger* than the camera-to-object
  distance for an object this small, clipping the entire model out of
  frame. Fixed by setting `clip_start`/`clip_end` proportional to the
  object's own measured radius instead of leaving Blender's default.
- Also needed one round of the now-familiar color-darkening fix
  (same root cause as the Stone Hammer, v0.1.177/178-dev) — the first
  material read too light once baked through `IconBaker`.

Unity side: new `BerrySeed.asset`/`BerrySeedPickup.prefab`
(`SphereCollider`, `ContinuousDynamic` Rigidbody per the known thin-
Ground-collider tunneling gotcha), icon baked via `IconBaker`, wired
into the scene's `BerryBush.prefab` and verified resolving correctly
via a batch-mode read-back before considering this done.

**Not done, and not asked for:** whether a Berry Seed ever becomes
plantable — that question is exactly as open as when first raised.
This entry only adds the item and its spawn chance.

### v0.1.178-dev — Stone Hammer head redesigned: crosswise, not a fatter cylinder

Same-day follow-up to v0.1.177-dev. Ben's reaction to the shipped
result, verbatim: "these models are horrible. can we make the hammer
head look progressively like a real hammer instead of a cylinder of
rock on a handle?" — accurate. The first version built the head as a
continuation of the *same* axis as the handle, just widening — a
lollipop/mace silhouette, not a hammer, regardless of how well the
surface detail or tier progression worked.

Rebuilt from scratch with the one change that actually mattered: the
head is now a **separate tube built along a perpendicular axis** (Z,
crosswise) centered where the handle (built along X, as before) meets
it — the classic sledgehammer/maul silhouette, immediately readable at
icon scale. Two independent ring-lofted meshes merged into one bmesh
rather than one continuous profile function; the handle's far end
extends slightly into the head's solid volume so there's no visible
seam. Tier progression (head size shrinking from chunky/lumpy to
compact/refined, surface noise fading, color darkening, a lashing
collar at Fine/Masterwork) carried over unchanged from v0.1.177-dev's
logic, just now applied along the head's own Z-axis length instead of
continuing the handle's X-axis.

Caught the fix's own rendering trap before it shipped: the throwaway
Blender preview script had a fixed guessed camera position left over
from the old shaft-and-blob layout, which badly cropped the new
off-center head. Fixed by computing the camera position/orthographic
scale from the object's actual bounding box instead of a hand-tuned
guess - the same lesson `IconBaker` already applies for the real game
icons, now applied to the throwaway Blender-side preview tooling too.

Unity side: same in-place model swap as before. All 5 pickup prefabs
re-swapped and re-baked; visually confirmed as a real, recognizable
hammer at every tier before considering this done.

### v0.1.177-dev — Stone Hammer tiers get real Blender models; design constraint from Ben: the shaft doesn't improve with Hammer tier

Same Blender pipeline as the Trimmed Stick and Stone Knife, applied to
the Stone Hammer — all 5 tiers previously shared one placeholder model
at the same scale. Ben's direction shaped the design directly: "since
the hammer requires a trimmed stick, let's make the shaft of the
hammer a wooden shaft, and the improvement would be in the shape of
the hammer head" — a Trimmed Stick is a real crafting ingredient with
its own separate tier ladder, so the Hammer's own tier shouldn't
re-skin it. The shaft is one plain wooden material/shape across all 5
tiers; every bit of tier progression lives in the head instead — both
its silhouette (large and organically lumpy at Crude, shrinking and
tightening toward a compact precise cylinder by Masterwork) and its
surface (chipped-stone noise fading to smooth, color darkening from
grey flint toward near-black polished stone), plus a lashing-cord
carving detail at the neck once refined enough (Fine/Masterwork).

Two real bugs hit and fixed along the way, both worth remembering for
the next tiered-prop build:

- **Crude/Rudimentary's silhouette came out as illegible white
  "feathers"** in the baked icon, not a solid stone head. Root cause:
  fixing it required bumping those tiers from 5 to 6 sides so they'd
  clear the `sides >= 6` smooth-shading threshold (5 sides + per-vertex
  chip noise was producing near-degenerate sliver faces that read as
  thin bright streaks from IconBaker's fixed camera angle) — a genuine
  geometry fix, confirmed first in a plain Blender render before ever
  touching Unity.
- **That same shading fix then washed the color out to near-white**,
  even after darkening it once (the same fix that worked for the Stone
  Knife). Root cause this time was physical, not a bug: a rough/diffuse
  (high-Roughness) material under IconBaker's uncapped directional
  lights (no tonemapping) reflects a much larger share of incident
  light back toward camera once its surface is smooth-shaded and
  facing the lights broadly, than the same material flat-shaded (where
  roughly half the faces sit in shadow) ever did — every previously-
  baked rough/matte icon happened to be flat-shaded, so this never
  surfaced before. Tried lowering `IconBaker`'s ambient intensity
  first (1.0 → 0.3); barely moved the result, confirming the
  directional lights themselves were the real driver, not ambient —
  reverted that change to stay consistent with the already-completed
  full re-bake sweep. Fixed instead by pushing Crude/Rudimentary/Normal
  head color much darker than their apparent "rough stone grey" input
  value would suggest (down to ~0.04-0.08 linear) to compensate.

Unity side: same in-place model-swap pattern as the Knife (all 5
existing pickup prefabs already correctly referenced by their item
assets, so no rewiring needed), collider re-measured from each
model's actual bounds (~0.40m long, head bounds shrinking tier over
tier from the shape change alone). The original placeholder
`StoneHammer.glb` is left in place, unreferenced.

### v0.1.176-dev — Full icon re-bake against the IconBaker ambient fix

Follow-up to the blue-tint bug found while baking the Stone Knife
(previous entry): re-baked every existing icon rather than leaving the
other 52 to carry a possibly-subtler version of the same wrong blue
ambient cast. One throwaway sweep script, `IconRebakeSweep.cs`, walked
every `ItemDefinition` and `BuildPiece` asset and called
`IconBaker.BakeAndWire` again straight from each item's own
`worldPickupPrefab`/`prefab` — no need to track down each item's
original source model separately, since `IconBaker` only needs a
`Renderer` anywhere in the hierarchy and doesn't care about the extra
`Pickup`/`Collider`/`Rigidbody` components a pickup prefab carries.
56 items + 2 build pieces re-baked, 0 failures. Spot-checked a spread
across material types (metal axe head, silver ore, copper, stone) —
all read correctly with no unwanted color cast; nothing regressed.

### v0.1.175-dev — Stone Knife tiers get real Blender models; IconBaker's blue-tint bug found and fixed

Same approach as the Trimmed Stick tiers (v0.1.173-dev), applied to the
Stone Knife: all 5 craft tiers previously shared one placeholder
Tripo3D model (`CrudeStoneKnife.glb`) at different non-uniformly
stretched scales — a real gap, and Ben's ask: "let's see if we can use
blender to create better models for all 5 tiers... I'm thinking we can
use noise applied base colors."

Built via `bpy`/`bmesh`: a blade+handle shaft assembled ring by ring
(60 segments), with a flattened diamond/lens cross-section (4 points
for Crude/Rudimentary, 6-8 for Normal through Masterwork) instead of
the stick's round one — width and thickness both follow a length-wise
profile that stays roughly round through the grip, then widens into a
leaf-shaped blade before tapering to a point. Two materials per mesh
(blade and handle get independent face `material_index` assignment),
so tier progression covers both shape and color at once:

- **Crude → Normal**: blade edge noise (two layered sine components
  per angular slot — chip-sized + fine micro-texture; a single
  low-frequency term first read as a smooth wavy ribbon, not chipped
  flint) fades to 0, radial segments rise 4→6, blade color moves from
  dull grey flint toward a cleaner grey.
- **Fine/Masterwork only**: a decorative handle-wrap detail (shallow
  carved rings around the grip, like a wound cord binding) — first
  attempt used the same depth/spacing as the stick's carving and came
  out as a stack of beads, not a wrapped cord; widened and shallowed
  until it read as ribbed/fluted instead.
- **Blade color** shifts from grey flint to near-black glossy obsidian
  by Masterwork (paired with falling Roughness, 0.90 → 0.10); handle
  color warms from dark leather-brown to a lighter wood/bone tone.

Real bug found and fixed in the shared `IconBaker` tool while baking
these: every icon renders in a scratch scene that `IconBaker` never
configured explicitly, so it inherited Unity's default skybox-ambient
lighting (a blue-gradient procedural sky) — invisible on every icon
baked so far since they all happened to be warm/saturated colors, but
a strong, wrong blue cast on the knife's neutral grey stone (confirmed
via a debug pass: the actual material `baseColorFactor` values were
exactly correct, gamma-encoded as expected — the bug was purely in
the render environment, not the material data). Fixed by setting flat
white ambient and disabling environment reflections in `BakeOne()`
before rendering. Not yet re-applied to the other 52 existing icons —
they may have a subtle version of the same cast that was never
noticeable against their own warmer palettes; worth a full re-bake
sweep at some point but not done here.

Separately, darkened the actual blade base colors after the ambient
fix — the original values (chosen against the *buggy* blue-tinted
render) turned out too light/washed-out once the render was color-
correct, nearly blending into the icon background at the Crude end.

Unity side: swapped each of the 5 existing pickup prefabs' model child
in place (`RockKnifePickup`, `RudimentaryKnifePickup`,
`NormalKnifePickup`, `FineKnifePickup`, `MasterworkKnifePickup` — all
already correctly referenced by their item assets, so no rewiring
needed) rather than creating new prefabs, mirroring the Masterwork
stick retexture swap. Collider re-measured from each model's actual
bounds (~0.28m long, consistent across all 5 — a real size chosen
directly rather than inherited from the old placeholder's arbitrary
non-uniform stretch). The original `CrudeStoneKnife.glb` placeholder is
left in place, unreferenced.

### v0.1.174-dev — Masterwork Trimmed Stick gets a real Tripo3D-generated wood texture

Same-day follow-up to the flat-color Blender sticks: tested whether
Tripo3D can texture a model *we* built rather than one it generated,
since that combines controlled procedural geometry with real PBR
texture quality. It can — `texture_model` accepts any model registered
via `import_model` (an uploaded external file), not just Tripo3D's own
generations.

New `Tools/Tripo3D/Texture-Model.ps1`, a genuinely different pipeline
from `Generate-Model.ps1`: that script's `v3` REST API
(`openapi.tripo3d.ai/v3/generation/...`) has no documented endpoint for
texturing an existing mesh, so this one uses the task-based `v2` API
(`api.tripo3d.ai/v2/openapi`, `POST /task` with a `type` field) instead —
confirmed against Tripo3D's own official Python SDK source
(`VAST-AI-Research/tripo-python-sdk` on GitHub), since the interactive
docs site is a JS-rendered SPA no simple fetch can read. Same API key
works for both surfaces. Three real dead ends hit before landing on the
working shape:

- The obvious `/upload` endpoint rejected the `.glb` outright — turned
  out to be image-only despite the SDK routing model files through it
  as a "legacy" fallback.
- The real path is STS-credentialed S3 upload (`POST /upload/sts/token`
  returns temporary AWS credentials), which needs actual SigV4 request
  signing — installed the `AWS.Tools.S3` PowerShell module
  (`Install-Module -Name AWS.Tools.S3 -Scope CurrentUser`, plus the
  NuGet provider it depends on) rather than hand-rolling AWS's signing
  algorithm.
- The STS response's `s3_host` pointed at a real `us-west-2` AWS
  bucket, not a custom endpoint — the first attempt hardcoded
  `us-east-1` and got a clean "region is wrong" error back.

Ran the real pipeline once end to end: uploaded
`TrimmedStickMasterwork.glb`, imported it as a Tripo3D task (free, 0
credits), then textured it with a wood-grain prompt ("rich polished
walnut wood, fine warm honey-brown grain... hand-oiled lacquered
finish... photorealistic PBR wood material") at detailed quality (20
credits). Confirmed via `GET /user/balance`: 340 credits remaining.
Notable finding — texturing an existing model costs the *same* as a
full from-scratch `text_to_model` generation (both 20 credits at
default settings, confirmed against this session's own Twig Foundation
log) — so the real advantage of building geometry in Blender first was
never cost, it's that Blender can guarantee a coherent tier family in a
way independent Tripo3D generations can't.

Tripo3D's pipeline re-normalized the model's scale during import/
texture (came back 1.0m long instead of the original 0.6m) — caught by
re-measuring bounds after the swap rather than trusting the source
file's known dimensions, and corrected by rescaling the pickup's model
instance back to match the other 4 tiers and Stick's own real length.
`TrimmedStickMasterworkPickup.prefab`'s model child swapped to the new
textured asset (`Assets/Models/TrimmedStickMasterworkTextured.glb`),
collider re-measured, icon re-baked. The original flat-color
`TrimmedStickMasterwork.glb` is left in place, unreferenced, in case the
pure-Blender version is wanted again. Crude/Rudimentary/Normal/Fine
still use the flat-color material — this was a single-tier test, not
yet applied to the rest of the set.

### v0.1.173-dev — Trimmed Stick tiers get real models/icons, generated entirely in Blender (no Tripo3D)

Filled a real gap — all 5 Trimmed Stick craft tiers (Crude through
Masterwork) previously had no icon at all, and only Crude had a world
pickup (a placeholder reusing the plain Stick's branch model). This
doubled as the test Ben asked for after the Blender wall-separation
research earlier in the session: could Blender build models from
scratch, not just edit/split existing Tripo3D output?

It can. `bpy`/`bmesh` scripted headless (`blender --background --python
...`) built a tapered shaft (`bmesh`, ~40 rings along the length, quad
bridged between them) and varied it procedurally per tier — a case
Tripo3D genuinely can't do well, since 5 independent AI generations
wouldn't relate to each other as a coherent progression:

- **Crude → Normal**: fewer radial sides (5/6/8) and a smooth
  low-frequency per-angular-slot wobble (not independent per-ring
  noise, which read as spiky static — first attempt looked like a
  crystal, not a branch) fading to ~0 by Normal, plus a single-arc bend
  fading out the same way. Reads as an unevenly-shaped, roughly-trimmed
  branch, straightening out tier by tier.
- **Fine**: dead straight, smooth-shaded 12 sides, two shallow carved
  rings (a Gaussian falloff cut inward at fixed points along the
  shaft) — "a few little carvings," per Ben's direction.
- **Masterwork**: 20 sides, straight, plus a finer/shallower ring
  pattern and a wrapping spiral groove layered on top for a genuinely
  ornate, engraved look (first attempt used the same depth as Fine's
  rings plus a strong spiral — read as lumpy/caterpillar-like, not
  ornate; toned down to a crisper, narrower carve).

Real bugs hit and fixed along the way: (1) `primitive_cone_add` only
has two vertex rings (base + tip) — bend/noise/carving had nothing to
act on until rebuilding the shaft manually via `bmesh` with real
length-wise resolution; (2) `bpy.ops.mesh.subdivide` on the cone
subdivided radially too, not just along the length, ballooning one
tier from 10 to 3,250 verts; (3) the length-position parameter `t` was
computed as `x / length` (range roughly ±0.5) instead of normalized to
0–1, so ring/spiral positions landed off the actual mesh and the bend
formula tilted the stick diagonally instead of bowing it.

Unity side: one throwaway batch-mode script (`TrimmedStickSetup.cs`,
deleted after running, matching every other one-shot Editor script in
this repo) built a `Pickup` prefab per tier (`BoxCollider`/`Rigidbody`
sized from the model's actual measured bounds, `ContinuousDynamic`
collision per the known thin-Ground-collider tunneling gotcha) and
called `IconBaker.BakeAndWire` per tier (32px icon, 128px preview —
matches the existing convention). All 5 came out at 0.6m long,
matching the original Stick's own collider size. `BerryBush.prefab`'s
chop-drop (`trimmedStickPrefab`) was repointed from the old placeholder
to the new Crude-tier pickup, and the placeholder `TrimmedStickPickup.prefab`
was deleted as orphaned once nothing referenced it anymore.

Verified by rendering a quick preview PNG per tier straight out of
Blender before ever touching Unity (caught the bugs above early/cheap),
then by reading back each `ItemDefinition.asset`'s wired `icon`/
`previewIcon`/`worldPickupPrefab` GUIDs and visually inspecting the
baked icons themselves.

### v0.1.172-dev — Admin-spawned pieces were floating at head height, not on the ground

Same-day follow-up to the previous entry's "no longer bury the player"
fix — Ben spawned a Twig Foundation, reported "can't climb onto it...
can't even try running and jumping," and a corrected screenshot showed
why: the piece was floating roughly 1.8m above real ground, legs
dangling in open air, not sitting flush with a small lip like it's
supposed to.

Root cause: `AdminSpawnScreen.SpawnPiece`'s ground-detection raycast
(cast straight down from 2m above the player) hit the **player's own
`CharacterController` capsule** (top at `Center.y + Height/2` = 1.8)
before it ever reached the actual terrain — `Physics.Raycast` doesn't
exclude the caster's own collider by default. The piece spawned at
that wrong, elevated hit point instead of on the ground. Fixed by
disabling the `CharacterController` for the duration of the raycast
(and the subsequent stand-the-player-on-top repositioning from the
previous fix, which needed the same treatment to avoid fighting a
direct `transform.position` set).

This likely explains most of the original "can't climb onto it, need
stairs/ramp" report on its own — a real ~1.8m gap obviously isn't
reachable by a normal jump, versus the intended ~0.2m lip. Worth
re-testing before concluding stairs/ramps are urgently needed for
Foundation specifically.

Verified via a full batch-mode compile check.

### v0.1.171-dev — Berry Bush gets a genuinely distinct look; Admin-spawned pieces no longer bury the player

Live-testing found two more real bugs, plus a design fix that resolves
a confusing report from earlier in the session:

- **Berry Bush now uses the "Generated Berry Bush" leafy model instead
  of the Strawberries cluster.** Root cause of "the berries didn't get
  fixed... you can't do anything with the bush": the standing bush and
  every loose dropped Berry used the *exact same* Strawberries model —
  visually indistinguishable, so a correctly-scattered, perfectly
  pickable berry looked identical to the bush itself sitting right next
  to it. Ben's call: reuse the "Generated Berry Bush" model (an
  existing decorative comparison prop from an earlier session, `Assets/Models/GeneratedBerryBush.glb`,
  never wired to any script) as the bush's real visual instead — now
  genuinely different shapes, can't be confused again. All
  `BerryBush` field wiring (chop tools, Trimmed Stick/Berry prefabs,
  skill, cooldowns) carried over untouched; `berryPrefab` still points
  at the original `BerryPickup.prefab`, so search still drops the same
  strawberry-cluster pickup as before — that prefab was never touched
  either, still plain `Pickup` (pick up + eat), never had chop/search.
  Removed the now-redundant decorative duplicate from the scene, since
  its model is doing real work now instead of just sitting there for
  comparison. Also **shrank the loose Berry pickup** (0.35m bounds →
  0.18m) — sized to look right next to itself as the old bush, it read
  as oversized (a third of the *new*, bigger bush's size) once the bush
  became a visually distinct, larger plant; re-baked its icon at the
  new size.
- **Admin-spawned Build Pieces no longer bury the player.** Ben:
  "when I spawned the twig foundation, it pushed me under the world. I
  should be pushed up onto it instead." Root cause: the spawn raycast
  originates from the player's own position, so Foundation's collider
  (extends 0.8m below ground, only 0.2m above) materialized wrapped
  around the player's feet — `CharacterController`'s own depenetration
  resolved downward instead of up. Rather than rely on physics to
  guess correctly, `AdminSpawnScreen.SpawnPiece` now explicitly stands
  the player on the freshly-spawned piece's own *measured* top surface
  afterward (disabling/re-enabling `CharacterController` around the
  direct position set) — generalizes to any piece shape, not just
  Foundation's specific dimensions.

Verified via full batch-mode compile checks; every visual re-baked and
spot-checked directly (Berry's icon still reads clearly as strawberries
at the smaller size, thanks to the tight-fit framing from the icon fix
just before this).

### v0.1.170-dev — Admin tab can spawn Build Pieces directly, for testing

Ben: "let's spawn a foundation in place for now" — extended the
existing Editor-only `AdminSpawnScreen` (already spawns any
`ItemDefinition` for free) with a second list for `BuildPiece`s. Spawn
places the piece on the ground directly under the player via a
straight-down raycast, free of materials/skill gates, and tags it with
a real `PlacedPiece` component so upgrade/destroy still works on it
exactly like a normally-built one — a genuine placed piece, not a
lookalike prop. Same Editor/testing-only scoping as the item list
(`#if UNITY_EDITOR`, won't appear in a build).

Verified via a full batch-mode compile check.

### v0.1.169-dev — Scattered berry unpickable; real Twig Foundation model; icons re-baked to fill the frame

Three pieces, caught/requested in quick succession:

- **A scattered Berry could be permanently unpickable.** Root cause:
  `BerryBush.SpawnScattered` used `Random.insideUnitSphere` for both the
  spawn offset and the launch direction — capable of landing (and
  settling, after gravity) close enough to overlap the bush's own
  `SphereCollider` (radius 0.175). Unlike `ResourceNode`/`ChoppableTree`,
  `BerryBush` never disables its collider (the whole point of the
  redesign is it stays interactable throughout), so a scattered item
  landing that close got its raycast permanently shadowed by the bush
  itself — confirmed live via a screenshot showing the bush's own chop/
  search prompt while aimed at what looked like a loose berry. Fixed by
  spawning on a fixed 0.45 horizontal ring (guaranteed outside the
  collider) and pushing further in that same outward direction instead
  of a fully random one. Affects both chop's Trimmed Sticks and search's
  Berries, since both go through the same helper.
- **Real Twig Foundation model**, via the Tripo3D API — a genuine
  lashed-twig-and-rope platform on short legs, replacing the plain
  procedural Cube slab. Hit the same "stuck at 99%, actually succeeded
  server-side" pattern documented in `Tools/Tripo3D/README.md`; recovered
  by polling `GET /v3/tasks/{id}` directly rather than re-generating.
  Swapped into `Foundation.prefab`'s `Slab` child only — the root's
  `BoxCollider` and all 4 `BuildSocket`s are completely untouched, so
  gameplay footprint/snapping/upgrades need no changes. Hit and fixed a
  real double-scaling bug along the way (the `Slab` parent still carried
  its old `{5,1,5}` cube-fitting scale, which stacked with the new
  model's own footprint-fit scale to 25x instead of 5x) — caught by
  re-baking the icon and seeing obviously-wrong bounds in the log before
  it ever got visually reviewed.
- **`IconBaker` reframed to actually fill the icon** — Ben: "when I look
  at it, its hard to see the object clearly." Root cause: camera framing
  sized off `maxDim` (the single largest axis) with a flat padding
  guess, which was never what the fixed 3/4-angle camera actually
  projects — the further a shape diverges from a cube (a wide flat
  Foundation, a long thin Nail), the more empty space that guess left.
  Now projects the AABB's 8 corners into camera space and sizes/centers
  to the *true* on-screen extent, ~8% margin. Exposed a new
  `IconBaker.BakeAndWire` so a sweep script could re-bake all existing
  icons in one process instead of 50+ separate Unity launches — every
  icon and BuildPiece tile in the game (53 total) re-baked in one pass,
  0 skipped, 0 failed.

Verified via full batch-mode compile checks throughout; every visual
result (Twig Foundation, Storage Box, Nail) spot-checked directly before
moving on.

### v0.1.168-dev — Build tab gets the same tile-grid + search treatment as Crafting

Ben: "let's do the same thing with the build tab" — same visual/browsing
layer as Crafting's redesign, but deliberately **not** the batch/timer/
cancel machinery, since placement has no analog for it: each piece is
still one deliberate walk-and-aim act in the world, not something that
produces instantly into inventory. `PlayerBuilding.ArmPiece` and the
whole placement flow are completely untouched.

- **`BuildPiece` gained `icon`/`previewIcon` fields**, same shape as
  `ItemDefinition`'s. Baked both existing pieces (Twig Foundation,
  Storage Box) via `IconBaker` from their own placed-piece prefabs.
- **`IconBaker` generalized** — it was hardcoded to `ItemDefinition`
  (a `LoadAssetAtPath<ItemDefinition>` type-gate), which silently
  rejected `BuildPiece`. Wiring already happened generically via
  `SerializedObject.FindProperty` by field name, so the fix was just
  loosening the load/type-check to `UnityEngine.Object` — no other tool
  needed for this, matching its own "one reusable icon tool" intent.
  (Hit a real `CS0104` ambiguous-`Object` compile error along the way —
  `System.Object` vs `UnityEngine.Object` — fixed by fully qualifying.)
- **`BuildScreen` rewritten** from a text list to the same tile shape as
  Crafting: big icon (blank spacer if unset), live materials have/need,
  a skill-requirement line, and the existing Arm/Armed button —
  unchanged interaction, just a tile instead of a row. `PlayerBuilding`
  gained a public `GetAvailableCount` (same reach as its existing
  `ReachableInventories`) so the tile can show live counts; `HasIngredients`
  now just calls it instead of duplicating the summation.
- **Search bar**, same shape as Crafting's — Build has no discipline
  tabs to override, so this is a plain substring filter over
  `pieceName`.

Verified via a full batch-mode compile check; both new icons read
clearly at preview size before wiring.

Ideated first (an HTML mockup, matching the game's existing dark
debug-panel look), then Ben resolved every open question in one message:
background-continuing timer, `CraftTierScale.HoldDuration` reused for
per-item time, cancel-with-refund, tool-break stops the batch. Two real
systems, not just a reskin:

- **`PlayerCrafting` gained a real batch-crafting queue**, replacing the
  old instant single-craft `TryCraft` entirely. `StartCraft(recipe,
  quantity)` removes ingredients for the *whole* batch up front (same
  all-or-nothing gate checks `TryCraft` always had — tool, skill, Anvil
  surface, output space), then `Update()` ticks one item at a time on a
  timer sized by `PlayerSkills.GetHoldDuration` — the exact same
  skill-scaled duration ladder gathering already uses, so higher skill
  crafts faster, not a bespoke number. Deliberately **not** gated on the
  Crafting tab being open or any key held — closing the menu or walking
  away doesn't pause it, unlike every hold-and-release interaction
  elsewhere in the game. `MaxCraftable(recipe)` (materials-only, read by
  the new Max button) and `CancelCraft()` (refunds ingredients for
  whatever hadn't completed yet — already-crafted items stay, nothing to
  undo there) round it out. **Tool-break stops the batch:** if a
  spectacular-failure roll breaks the required tool mid-batch, the next
  tick detects the tool's gone and stops with a refund instead of
  silently no-oping through the rest of the queue.
- **`CraftingScreen` rewritten from a flat text list to a tile grid** —
  each tile: a big icon (`previewIcon`, falling back to `icon`, falling
  back to a blank spacer — Ben's call — rather than a placeholder glyph;
  8 recipe outputs, mostly the Trimmed Stick tiers, don't have one baked
  yet), materials with live have/need counts, tool/skill/Anvil
  requirement lines, a quantity stepper, Craft, and Max. While a tile's
  own batch is running, the stepper/Craft/Max row is replaced by a
  progress bar + Cancel, reusing the exact green-fill bar look
  `PlayerInteraction`'s gathering hold already uses. Only one batch at a
  time — every other tile's Craft/Max greys out with "Crafting queue
  busy" while one's active.
- **Search bar**, right above the grid: case-insensitive substring match
  against the recipe's output item name, ignoring the discipline tab
  filter entirely while active (searches every discipline at once, not
  just whichever tab happens to be selected) — "ax" finds every unlocked
  or locked Axe tier in one view. Clearing the box reverts to the normal
  per-discipline tab view.

Verified two ways: a full batch-mode compile check, and a direct
state-machine test (via reflection, since `Time.deltaTime` doesn't tick
meaningfully in a non-play batch script) confirming `StartCraft`'s
upfront removal, a second `StartCraft` correctly refusing while one's
active, and `CancelCraft`'s partial refund math, all matched exactly.

See `docs/design-brief.md`'s new section for the full shape and the
ideation mockup.

Ben: "let's add an action to the berry bush as well. e should chop it if
you have a knife or ax in your hand. you should get trimmed sticks. f
should search the bush to find 0 to 3 berries which would drop to the
ground..." — replaces the old single instant-E-grabs-a-berry model
entirely with two independent gather actions.

- New `BerryBush.cs` implements both `IInteractable` (E — hold to chop,
  gated on any Knife or Axe tier in hand, same shape as
  `ChoppableTree`) and `ISecondaryInteractable` (F — search, no tool
  needed). Each action has its own independent 180s respawn cooldown
  (`chopRespawnAt`/`searchRespawnAt`) — the bush itself never
  disappears, only each specific action goes quiet for a while, unlike
  `ResourceNode`/`ChoppableTree`'s hide-the-whole-object model. Chopping
  scatters 2 loose Trimmed Stick pickups (Crude tier) and trains
  Woodworking; searching rolls 0–3 and scatters that many loose Berry
  pickups — both reuse `ResourceNode`'s exact scatter-with-`Rigidbody.AddForce`
  shape.
- **Real structural snag, resolved:** `Berry.asset.worldPickupPrefab`
  turned out to point at the *same* `BerryPickup.prefab` used for the
  placed bush — a dual-purpose prefab. Repurposing it in place would
  have broken dropping a Berry from inventory (no more `Pickup`
  component to receive `PlayerDropping`'s `Configure` call). Split into
  three: `BerryPickup.prefab` stays exactly as it was (the loose,
  droppable single Berry — still `Berry.asset`'s `worldPickupPrefab`,
  and now also what `BerryBush`'s search action spawns), a new
  `BerryBush.prefab` (no `Pickup`, no `Rigidbody` — static, reuses the
  same Strawberries visual) is the actual world bush, and a new
  `TrimmedStickPickup.prefab` (reuses `StickPickup`'s branch model as a
  placeholder visual) is `CrudeTrimmedStick.asset`'s new
  `worldPickupPrefab`, since chopping needed a real ground-pickup
  prefab to scatter and Trimmed Stick never had one before (it was
  always crafted straight to inventory).
- Scene's placed "Berry Bush" swapped from a `BerryPickup.prefab`
  instance to the new `BerryBush.prefab`, same position — verified by
  reading back the saved scene YAML, not just the batch log.

See `docs/design-brief.md`'s new section for the full shape. Verified
via a full batch-mode compile check.

Ben: "the berry doesn't respawn. fix it" — fair demerit (🍓💀, see
`AWARDS.md`): `canRespawn: 0` was sitting right in the `Pickup` field
block I read and edited earlier today fixing Berry's null `item`
reference, on the same object type (Stick pickups) I'd just been
comparing it against for their own respawn behavior, and I didn't act
on it. Added a `canRespawn: 1` override on the Berry Bush's scene
`PrefabInstance` (mirroring exactly how the two Stick Pickup scene
instances already override it, rather than changing
`BerryPickup.prefab`'s own default — keeps a future non-respawning use
of the same prefab, e.g. a dropped Berry, unaffected). Verified by
having Unity actually open the scene and read back the resolved
component value, not just trusting the hand-edited YAML: `canRespawn=True
respawnDelay=180`.

### v0.1.164-dev — Stick pickup never worked at all; Push's hold was fragile to aim jitter

The likely real explanation for the whole-session "stick doesn't decrease"
mystery, plus a genuine Magic System bug found live-diagnosing the
"kinetic skill isn't pushing anything" report:

- **`StickPickup.prefab`'s `Pickup.item` was null** — third instance of
  the exact same bug class as Berry (`BerryPickup.prefab`, fixed
  earlier today). This pickup point (world model literally named
  `TreeBranch_PolyByGoogle`) never actually granted a real Stick at
  all; walking up and picking one up did nothing. Swept every other
  `*Pickup.prefab` in the project for the same pattern and found two
  more: `RopeCoilPickup.prefab` and `RockKnifePickup.prefab` (the
  Crude Knife's world pickup) — both fixed. (`DroppedItem.prefab`'s
  null `item` is correct as-is — it's the generic fallback template
  `PlayerDropping` configures dynamically per-instance at spawn time,
  not a bug.)
- **Push's hold was fragile to any one-frame raycast flicker.**
  `PlayerInteraction.HandleWish` required the raycast to resolve the
  *exact same* GameObject on every single frame of the hold — stricter
  than the E-interaction hold (`HandleInput`), which has no such check
  at all. Any momentary aim jitter, or a multi-collider model (like
  Backpack) briefly resolving a different collider, silently reset
  progress to 0 — and since wishes deliberately show no progress bar
  (Ben's "zero on-screen hints" call), this was completely invisible.
  Confirmed live: holding R on a Backpack (which does have a
  Rigidbody) for several seconds produced nothing, no message either
  way — ruled out lineage (Kinetic, correct), Will (100/100, well
  above Push's 60 cost), and hold-duration awareness (held
  continuously) before finding this. Relaxed to match E's proven
  model — accumulate whenever a valid target is resolved and R is
  held, no frame-to-frame identity requirement. `lastWishGameObject`
  removed entirely.

Verified via a full batch-mode compile check.

### v0.1.163-dev — Foundation: 1m thick, mostly buried (superseding the "raised above ground" pass)

Ben, immediately after the previous fix: "let's make the foundation 1
meter thick. that way it will appear to be sitting in the ground with
the top slightly above the ground and visible as a real foundation" —
a different, more specific look than fully-raised. `Foundation.prefab`
and `PlankFoundation.prefab`'s Slab child + collider now both scale to
`y: 1` (was `0.3`) and sit at `y: -0.3` (was `0.15`), putting the top
0.2m above ground and burying the remaining 0.8m — reads as a real
poured foundation wall rather than a thin raised deck. Same pure-offset
approach as the prior pass: sockets stay root-relative, so
snapping/upgrades need no other changes.

### v0.1.162-dev — Foundation raised above ground; Drop gets a quantity picker

Two follow-ups from the same bug report, both Ben's call:

- **Foundation raised above ground.** Ben thought "5m" meant Foundation
  was 5m *thick* and expected it to stand above the grass — it's
  actually a 5m × 5m *footprint*, 0.3m thick, and was positioned with
  its top surface flush with y=0 (a poured-slab look). Ben's call: raise
  it instead, so the whole 0.3m slab sits above ground level (bottom at
  y=0) and reads as a visible platform. Moved both `Foundation.prefab`
  and `PlankFoundation.prefab`'s Slab child + collider from
  `y: -0.15` to `y: 0.15` — a pure offset change, so every socket/
  snapping/upgrade calculation (all relative to the shared root)
  stays correct automatically, no other logic touched.
- **Drop gets a quantity picker.** Previously Drop always removed an
  item's *entire* stack with no way to choose less — fine for most
  stackable items, but the exact bug Ben hit: 2 Hammers (non-stacking,
  `maxStack: 1`, so 2 separate slots) meant "Drop" dropped both when
  only one was wanted. New `DrawItemDropPopup` mirrors the existing
  Coin-drop popup exactly (-10/-1/+1/+10/All steppers) — except it
  defaults to the *full* count already held rather than 0, since
  "drop everything" is the common case for items (unlike coins), so
  the popup doesn't turn a one-click action into a two-click one for
  that case. `PlayerDropping.DropFrom` gained a quantity parameter
  (old 2-arg call sites, e.g. `PlayerLoot`'s hand-eviction, are
  untouched — still drop everything, no popup needed there).

Verified via a full batch-mode compile check.

### v0.1.161-dev — Nail's wrong skill gate; Eat and Move both broke on non-main-inventory items

More bugs caught immediately in the same live-testing pass:

- **Nail required Metalworking 25, with no way to reach it.** `Nail.asset`
  (and `StorageBoxItem.asset`, same latent issue, not yet visibly broken)
  were created via `ScriptableObject.CreateInstance`, which left `tier`
  at its default `Normal` — `PlayerCrafting.HasRequiredSkill` reads
  `outputItem.tier` directly to compute the skill gate, so an item with
  no real tier ladder needs to explicitly opt out with `tier: 0` (Crude),
  same as Rope/Cloth already do. Fixed both.
- **Eating from a hand slot or a Backpack/Storage popup silently did
  nothing.** Root cause: `PlayerEating.TryEat` always removed from the
  main inventory specifically, regardless of where the item actually was
  — the new Eat button (added earlier today) found the edible fine and
  showed the button, but `RemoveItem` on the wrong inventory found zero
  and quietly failed, while the popup still closed as if it worked. Added
  `TryEatFrom(Inventory source, item)`; `TryEat` is now a thin wrapper
  for the main-inventory case.
- **Moving more than fits failed outright instead of moving what fits.**
  Every "To Left Hand"/"To Right Hand"/"To Backpack"/"To Inventory"/"To
  Storage" button passed the source's *full* matched count as the move
  quantity. For a stacking item this is usually fine, but a non-stacking
  item (Hammer, `maxStack: 1` — each occupies its own slot) breaks
  immediately: 2 Hammers into an empty single-capacity hand slot failed
  completely instead of moving the 1 that actually fits. New
  `Inventory.SpaceFor(item)` (how many more fit) and
  `InventoryTransfer.MoveAsManyAsFit(from, to, item)` (caps the move to
  `min(available, space)`) — every move call site in `InventoryScreen`
  now goes through it. Verified directly: 2 Hammers, empty hand, old path
  moved 0; new path moves 1, leaves 1 behind.
- **Investigated, not reproducible:** Ben's report that crafting Trimmed
  Stick didn't decrement Stick or increment Trimmed Stick. A faithful
  full-pipeline batch test (real `PlayerInventory`/`PlayerSkills`/
  `PlayerEquipment`/`PlayerCrafting`, Sticks in inventory, Knife
  equipped, `CrudeTrimmedStickRecipe`, 5 consecutive `TryCraft` calls)
  showed correct behavior every time — Stick decremented, Trimmed Stick
  incremented, skill rose, every attempt. No code bug found; needs a
  clearer repro (see `TEST_FEATURE_PLAN.md`/design-brief for the open
  question).

Verified via full batch-mode compile checks; the Hammer-move fix also
verified directly against the exact reported scenario, not just by
reading the code.

### v0.1.160-dev — Nail, the AnvilSurface gate, and a real buildable/pickupable Storage Box

Ben: "let's use the api to create a nail model... the recipe will call for
the iron chunks that are in inventory. you need a boulder or an anvil
within 2m and a hammer in hand" — followed by "let's create a recipe for
the storage box... 4 planks and 6 nails" and "we need to build icons for
the storage box as well. we should be able to pick it up."

- **Nail** — generated via Tripo3D (clean first attempt), imported as
  `Assets/Models/Nail.glb`, icon baked via `IconBaker`. `NailPickup.prefab`
  built from scratch (Pickup/Rigidbody/BoxCollider, same shape as
  `RopeCoilPickup.prefab`). `NailRecipe.asset`: 1 Iron → 5 Nails, trains
  Metalworking, any Hammer tier in hand (not consumed).
- **New general gate: `CraftingRecipe.requiresAnvilSurface`.** Not
  Nail-specific — a new `AnvilSurface` marker component (empty, just a
  tag) that any world object can carry; `PlayerCrafting.HasNearbyAnvilSurface`
  passes if any one is within 2m. Boulder is now tagged with it, and a
  real placed Anvil object (the model from the prior session, previously
  import-only) now sits in `TestScene` near the Boulder, also tagged —
  positioned using its actual measured bounds so it sits on the ground
  rather than floating/sinking (the project's documented model-pivot
  gotcha). `CraftingScreen` shows "— requires a Boulder or Anvil nearby"
  when out of range, same convention as the tool-in-hand gate.
- **Storage Box, built.** `StorageBoxPiece.asset` — a real `BuildPiece`
  (4 Plank + 6 Nail, trains Woodworking — Plank is the defining structural
  material per the established discipline-sort rule, Nail is a fastener
  like Rope was for Twig Foundation), placed through the existing Building
  System exactly like Foundation. Reuses the placeholder Cube-primitive
  look the fixed "Small Storage Box" scene object already had, extracted
  into a real reusable `StorageBox.prefab`.
- **Storage Box, pickupable.** `StorageBox.cs` now implements
  `IInteractable` directly — Ben's call: must be empty first (no risk of
  silently losing stored items), no tool required (a plain "pick up my
  furniture" interaction, deliberately not routed through
  `PlayerPieceUpgrade`'s Hammer-gated system at all). Picking one up
  destroys the placed instance and adds a new portable `StorageBoxItem`
  (icon baked) to inventory. That item's own `worldPickupPrefab` points
  right back at the same `StorageBox.prefab` — dropping/placing it later
  spawns a real, working, empty box again, not an inert prop, for free
  (`PlayerDropping.SpawnPickup` already gracefully skips its `Pickup.Configure`
  call when a prefab has no `Pickup` component, so this needed zero
  changes to the drop path). Wired onto both the new buildable Storage Box
  *and* the original pre-existing "Small Storage Box" scene object, so
  every box in the game is pickupable, not just newly-built ones.

See `docs/design-brief.md`'s new "Storage Box: Build, Pick Up, Place
Again" section for the full shape. Verified via full batch-mode compile
checks after each step, every asset/scene edit verified by reading back
the actual saved YAML.

### Anvil model generated and imported (doc-only, no version bump)

Ben: "let's use the api to create an anvil" — generated via
`Tools/Tripo3D/Generate-Model.ps1`, clean on the first attempt, imported
as `Assets/Models/Anvil.glb`. Deliberately stopped there per Ben's
call — no prefab, no scene placement, no recipe. There's no Forging/
Metalworking mechanic to attach it to yet (Core Pillars' "hammer + anvil
+ wood fuel + steel → sword" is still aspirational text, not a designed
system), so this is just the model sitting ready for whenever that gets
built. See `Tools/Tripo3D/README.md`'s "Current status" for the prompt
and details. No gameplay code changed, nothing on-screen differs.

### v0.1.159-dev (follow-up) — Build-cancel key conflicted with cursor unlock; Building couldn't see Backpack/Storage materials

Caught immediately by Ben re-testing the fixes above: arming a Foundation,
failing to place it ("Not enough materials"), then pressing Escape to get
out left the Player Menu unable to reopen at all ("nothing there" when
pressing Tab). Two separate real bugs, not one:

- **Escape was double-booked.** The build-cancel fix above bound cancel to
  Escape, but `FirstPersonController` already reads Escape the same frame
  to unlock the cursor. Both firing together left the cursor unlocked
  with nothing actually open — and `PlayerMenuScreen`'s Tab handler
  deliberately refuses to reopen while the cursor's already unlocked (so
  it can't stack on top of another open screen), so Tab silently did
  nothing. Moved build-cancel to **Right Mouse Button** instead, which
  nothing else in `FirstPersonController` reads.
- **`PlayerBuilding` only ever checked the main 4-slot inventory.** Ben
  reported having enough Stick/Rope and still getting "Not enough
  materials" — root cause: unlike `PlayerCrafting` (which already reaches
  main inventory → equipped Backpack → nearby Storage Box), Building
  never looked past the main inventory at all, from the very first
  version of the system. Gave `PlayerBuilding` its own
  `ReachableInventories()` mirroring Crafting's exact reach.
- **Couldn't eat a Berry sitting in a hand.** Same shape of gap as the
  Pickaxe-to-hand fix above, mirrored: Eat only ever showed in the main
  inventory list (`DrawInventorySection`), never in the shared move-popup
  used for a hand slot, Backpack, or Storage Box contents
  (`DrawMoveDestinations`). Added an Eat button there too, shown first
  when the item is edible.

Verified via a full batch-mode compile check.

### v0.1.159-dev — Four more live-testing bugs: Berry pickup, Plank size, build-cancel, ingredient substitution

Continuing the same-day system-test pass. Four issues from a single round
of feedback, all fixed:

- **Berry pickup did nothing.** `BerryPickup.prefab`'s `Pickup.item`
  field was never wired to the Berry `ItemDefinition` — `{fileID: 0}`,
  silently null since the prefab was made. The v0.1.139-dev model swap
  fixed the visual but not the underlying reference. Set directly.
- **Plank looked too small on the ground.** Bumped both the visual
  model's scale and the pickup `BoxCollider`'s size by 1.5x together, so
  the clickable area still matches what's visible.
- **No way to cancel out of build placement.** Once a piece was armed,
  Escape only stepped back from the rotate/confirm sub-phase to the
  following-ghost phase — never fully disarmed. Combined with "Not
  enough materials" leaving you re-armed (not un-armed), a failed
  placement could strand you following a ghost with no way out. Fixed:
  Escape while following now calls `ArmPiece(null)`. Also made
  `BuildScreen`'s "Armed" button itself clickable to un-arm, for a mouse
  path alongside the keyboard one.
- **Ingredient matching was exact-item-only.** Crude Axe (needs raw
  Stick) rejected an inventory full of Trimmed Stick; Crude Fiber
  Backpack/Belt (need raw Fiber) had no way to use Woven Grass Cloth, a
  pickup with no use anywhere until now. Ben's call: build a general
  mechanism rather than patch these two recipes. New
  `ItemDefinition.baseItem` field (refined item → the raw material it
  came from) plus a new `IngredientMatching` helper
  (`Satisfies`/`GetCount`/`Remove`) that both `PlayerCrafting` and
  `PlayerBuilding` now route through — exact stock is always spent
  before substitutes. See `docs/design-brief.md`'s new "Ingredient
  Substitution" section for the full shape.

Verified via a full batch-mode compile check.

### v0.1.158-dev — Fixed: no way to move a plain item from the main inventory to a hand

Caught by Ben during the first real system-test pass: a freshly crafted
Pickaxe (or any plain tool) sitting in the main 4-slot inventory
(`PlayerCrafting.AddCraftedOutput` sends plain output straight there,
not to a backpack) had **no path to a hand at all**. The "To Left Hand"/
"To Right Hand" options only ever existed inside a Backpack/Belt/Storage
Box's contents grid (`DrawContainerContents`, which makes every occupied
slot clickable to open the full move-destination popup) or on an item
already sitting in an equip slot — the main inventory list
(`DrawInventorySection`) only ever offered Eat/Drop/To Pack/To Storage
for a plain item, never a hand. A tool crafted with no backpack equipped
was effectively stuck — usable as a tool-gate check nowhere, since
`ResourceNode`/`ChoppableTree` both require it actually held in a hand,
not just carried.

- Added "To L Hand"/"To R Hand" buttons directly to the main inventory
  row, same `InventoryTransfer.Move` call the popup's own hand buttons
  already use — no new mechanism, just closing a real gap in an
  existing one.

Verified via a full batch-mode compile check.

## 2026-08-08

### MVP progress re-check, third pass (doc-only, no version bump)

Ben: "what's left in the mvp to work on" — updated `docs/design-brief.md`'s
MVP Progress Check-In section again rather than re-deriving from scratch.
Basic building moves from not-built to built (Foundation is real, not
complete — no Wall/Door/Pole/Floor/Ceiling/Roof/Equip-to-Define yet).
**Revised tally: 8 of Phase 1's 11 items built, 3 entirely unstarted:
Encumbrance & skill-based movement, Basic combat + first aid, and
Hireable autonomous NPCs** — nothing exists for any of the three, not
even partially. No gameplay code touched.

## 2026-08-08

### v0.1.157-dev — Upgrade/destroy: click a placed piece to upgrade, hold 5s to destroy

Ben: "lets go ahead and build it" — implements the click-vs-5s-hold
mechanic from the ideation above, plus a real Plank Foundation to
upgrade *to* (otherwise the mechanic would have nothing to prove out
end-to-end, same "ship a real working example" discipline as every
other system this session).

- **`BuildPiece.nextTier`** — the next rung of the material ladder, null
  at the top or if no upgrade exists yet.
- **`BuildSocket.FreeConnectedSockets`** (static) — frees every socket on
  a destroyed instance *and* whatever they were touching, without a
  stored bidirectional link: two snapped sockets end up at the exact
  same world position by construction (confirmed from the placement
  math), so "find the other side" is just "find any other occupied
  socket at that same point."
- **`PlacedPiece`** (new, trivial) — tags a real (non-ghost) instance
  with which `BuildPiece` it is; `PlayerBuilding.Confirm` now attaches
  one to everything it places.
- **`PlayerPieceUpgrade`** (new) — its own raycast/E-handling, not a
  reuse of `IInteractable`'s hold-and-release: releasing early *is* the
  upgrade action here, only holding past 5 seconds does something else
  (destroy), which is backwards from how every other hold in the game
  works (release early = cancelled). Requires a Hammer (any tier) in
  hand for both actions. Upgrade is destroy-and-replace-in-place at the
  identical transform, with old socket-occupied state carried over by
  nearest-position match. Destroy frees connected sockets and refunds
  nothing — a pure loss, per Ben's call.
- **`PlankFoundation.prefab`/`PlankFoundationPiece.asset`** — identical
  shape to Foundation (same 5×5 slab + 4 sockets), lighter material, 8
  Plank, Woodworking-trained. `TwigFoundationPiece.nextTier` now points
  to it, so the whole ladder step is real and testable, not just wired
  infrastructure with nothing on the other end.
- **Full UI on purpose** (unlike Magic) — a prompt names the upgrade
  target and shows the destroy countdown live, plus a "not enough
  materials"/"already highest tier" message, all deliberately visible.

Verified via a full batch-mode compile check and by reading back the
saved scene/asset YAML: `PlayerPieceUpgrade.hammerTiers` (all 5 real
references), `TwigFoundationPiece.nextTier` (guid matches
`PlankFoundationPiece.asset`'s own), and `PlankFoundation.prefab` (4
sockets) — not just trusting the batch log.

**Known gaps, flagged not hidden:** no progress bar for the 5s destroy
hold (text countdown only); Rock/Metal tiers still don't exist, so the
ladder stops at Plank for now; Wall/Pole/Door still don't exist, so
Foundation is still the only upgradable/destroyable piece.

### Roadmap notes: Nails + buildable Storage Box, storage-capacity motivation (doc-only, no version bump)

Ben, mid-build of the upgrade/destroy system: "we need to implement
nails (requiring iron and a hammer). this allows us to add a storage
box that can be built with planks and nails," then "with the amount of
materials to build a structure, we'll need to make sure we have storage
so we can collect enough resources." Both captured in `docs/design-brief.md`'s
Building System roadmap rather than expanding the in-progress
implementation pass — Nails fits the material web's already-sketched
but unbuilt Ingot→Forging→Forged Component branch (Forging-trained,
consuming `Iron` directly for now); the Storage Box would reuse the
existing `StorageBox`/`Inventory` components as a placeable `BuildPiece`
rather than a new storage mechanism; the storage-capacity concern is the
stated motivation for building it, not a separate ask. Not designed in
detail or built. Continuing the already-committed upgrade/destroy +
Plank Foundation build.

### Upgrade/destroy: Hammer required for both, destroy refunds nothing (doc-only, no version bump)

Ben: "destroying doesn't return materiel. upgrade or destroy requires
the hammer" — resolves the two open questions left from the previous
entry. Both now settled in `docs/design-brief.md`: destroy is **not**
bare-handed after all (Hammer required for both actions), and destroying
a piece is a **pure loss**, no partial material refund.

No code touched — pure design.

### Upgrade/destroy interaction corrected: click vs. 5s hold, not a skill-tiered hold (doc-only, no version bump)

Ben: "we should have click to upgrade, and a click and hold to destroy
- a 5 second timer." Corrects the upgrade-path entry from earlier the
same session (which wrongly modeled upgrade as a skill-tiered hold) and
adds a genuinely new mechanic — destroy — that hadn't been captured at
all. Updated `docs/design-brief.md`'s Building System section:

- **Click (instant, Hammer in hand) upgrades** one material tier
  (Twig→Plank→Rock→Metal). Same destroy-and-replace-in-place mechanics
  as before, just triggered by a tap, not a hold.
- **Click-and-hold for a flat 5 seconds destroys** the piece outright —
  not skill-tiered, unlike every other timed action in the game so far.
- **Flagged as architecturally new**: this is tap-vs-hold-threshold on
  one object (release early = upgrade, hold past 5s = destroy), not a
  hold building toward one single outcome the way every other
  `IInteractable` works (where releasing early always means "cancelled,
  nothing happened"). Needs its own dedicated logic on placed pieces,
  not a straight reuse of the existing hold-and-release code path.
- **Left open**: whether destroy needs the Hammer too (leaning no — bare-
  handed, just slow) and whether destroying refunds any materials.

No code touched — pure design correction/addition.

### Building upgrade path: Hammer + E upgrades a placed piece one material tier (doc-only, no version bump)

Ben: "we also want to have an upgrade path. if you have a hammer in
hand, you can upgrade from twig to plank etc." Added to `docs/design-brief.md`'s
Building System section, reusing existing pieces rather than inventing
new ones:

- **Reuses the existing 5-tier Hammer item** as the upgrade tool (same
  "any tier counts" gate convention every tool check already uses) —
  not a new dedicated tool.
- **Rides E, not the Left Mouse Button/scroll placement scheme** —
  upgrading an existing placed piece is an `IInteractable` hold-and-
  release action like everything else in the game, not a new placement.
- **Destroy-and-replace in place**: old instance destroyed, target
  tier's prefab instantiated at the same transform, socket-occupied
  state carried over so neighbors don't read the connection as freed.
- **Cost/skill training is just the target tier's own `BuildPiece`
  data** — upgrading to Plank costs and trains exactly what building a
  fresh Plank piece would, not a separate rule.

No code touched — pure design, added alongside the Stairs/Ramps/Shelves
roadmap note above in the same session.

### Building roadmap: Stairs, Ramps, Shelves added (doc-only, no version bump)

Ben: "we will need recipes for stairs, ramps, shelves, etc" — added to
`docs/design-brief.md`'s Building System section as tracked-but-not-
designed, split into the two categories they actually fall into:
Stairs/Ramps are **vertical connectors** (need sockets at two different
heights, which the current horizontal-only Foundation-to-Foundation
socket system doesn't support yet); Shelves and other furniture/fixtures
**mount onto a Wall** rather than tiling edge-to-edge with the
structural shell, closer to how `IWishTarget`/`IEquippable` attach to
something else. No code touched — Wall/Pole/Door are still the nearer
gap.

### v0.1.156-dev — Building System first slice: Foundation, free + edge-snapped placement

Ben: "well, no time for the present, let's build it in" — first
implementation off the Building System ideation above. Scoped to
**Foundation only**, same "skeleton + one real path" discipline as
Magic's first pass (Spark before Push/Heal Self) — Wall/Pole/Door reuse
this exact machinery later, not a second system.

- **`BuildPiece`** (new `ScriptableObject`) — sibling to `CraftingRecipe`/
  `WishRecipe`: prefab, ingredients (reuses `CraftingRecipe.Ingredient`
  directly), trainedSkill, unlockTier, skillGain, groundReach.
- **`BuildSocket`** (new component) — typed anchor point
  (`SocketType.FoundationEdge` is the only one used yet; `WallBottom`/
  `WallTop`/`WallSide`/`PoleTop` are named ahead of time so the enum
  doesn't need a second pass), `IsCompatibleWith` for pairing, `Occupied`
  flag so a used socket can't be double-claimed.
- **`PlayerBuilding`** (new component) — the placement state machine.
  Every frame while a piece is armed: raycast for a nearby unoccupied
  compatible socket first (edge-snap, position+rotation both implied,
  one click confirms); otherwise a free-placement ghost follows the
  raycast hit point. **Left Mouse Button** places/confirms, **scroll
  wheel** rotates during the free-placement pending step — the exact
  Valheim/Rust/Raft-borrowed scheme from the ideation, not mouse
  movement (which is already camera look in this game).
- **`BuildScreen`** (new tab, `PlayerMenuScreen`) — same select/arm shape
  as `MagicScreen`, but **unlike Magic, fully visible on purpose**: shows
  ingredient costs, skill-gate state, and a live ghost preview in the
  world. Building is a deliberate, learnable system, not a hidden one.
- **`Foundation.prefab`** — 5m×5m flat slab (collider matches), 4
  `BuildSocket`s at the mid-edges facing outward. **Scoped down from the
  full design**: no support-column/stilt visual yet (the design doc's
  "buried block vs. stilted platform" question is still open) — a second
  foundation still correctly inherits the first's exact top height when
  snapped, and the 5m ground-reach tolerance is checked before allowing
  a *snapped* placement, but the free-placement case (nothing to snap
  to) always matches the raycast hit exactly, so there's no visible
  pedestal to get wrong yet.
- **`TwigFoundationPiece.asset`** — 6 Stick + 3 Rope, Woodworking-trained
  (matches the existing Bow precedent: wood-defining material trains
  Woodworking even with Rope also consumed), Crude unlock (always
  available).

**Real gotcha avoided, not hit this time:** `PlayerMenuScreen`'s new
`[RequireComponent(typeof(BuildScreen))]` (and `BuildScreen`'s own
requirement of `PlayerBuilding`) meant Unity auto-created both the
moment the scene loaded, same as the `MagicScreen`/`PlayerMagic`
incident in v0.1.148-dev — but both new components already had
`[DisallowMultipleComponent]` from the start and the setup script used
`GetComponent ?? AddComponent` throughout, so no duplicates landed this
time. Verified by reading back the saved scene YAML for exactly one of
each.

Verified via a full batch-mode compile check and by reading back
`Foundation.prefab` (4 sockets, correct `socketType`) and
`TwigFoundationPiece.asset` (both ingredients, correct guids) directly
rather than trusting the batch log alone.

**Known gaps, flagged not hidden:** no support-column/stilt visual;
mixed-material structures, Pole/Wall/Door, structural-integrity
requirements beyond "a socket exists," and territory restrictions all
remain exactly as open as the design doc already says.

### Building System: own tab + Left Mouse Button/scroll-wheel placement (doc-only, no version bump)

Follow-on to the Building System ideation above, same session. Ben: "we
will need to add a building tab to our crafting area. it may be its own
tab," then "can we borrow the mechanics from another similar game?" —
two more real decisions added to `docs/design-brief.md`'s Building
System section:

- **Own tab, not folded into Crafting** — same reasoning that kept Magic
  out of the Crafting tab: neither wishes nor building pieces resolve
  via a click-Craft-into-inventory button, both happen out in the world.
  A Build tab lists unlocked pieces and lets the player select which one
  is armed, same select/active shape `MagicScreen` already has — but
  **unlike Magic, Building gets full UI support** (ghost preview,
  prompts, everything), since it's a deliberate learnable system, not a
  hidden one. Worth keeping the two visually distinct on purpose.
- **Placement input borrowed directly from Valheim/Rust/Raft's shared
  convention**: Left Mouse Button places and confirms, scroll wheel
  rotates in between. Not mouse movement — that's already camera look in
  this game, so it can't also drive rotation without fighting itself,
  which is exactly why those games use scroll/a dedicated key instead.
  Not R (reserved for hidden magic) or E (already overloaded). Left
  Mouse Button turned out to be genuinely unbound today — it did nothing
  since punch-to-break was retired — so this is a clean reuse, not a
  displaced binding.

No code written — pure design.

### Building System designed — Foundation/Pole/Wall/Door, socket-based placement (doc-only, no version bump)

Ideation session on Phase 1's last untouched item, "Basic building" —
Ben noticed Rope and Sticks already exist as real items and asked to
explore a "twig" building tier: "we shouldn't give the 'Use R' type
hint" energy but for construction — click to place, snap to edges.
Converged on a real, buildable shape. Full detail in `docs/design-brief.md`'s
new **Building System** section; summary:

- **Modular by shape, not material** — Foundation/Wall/Door (and later
  Floor/Ceiling/Window/Roof) each define a fixed shape+socket contract
  once; material (Twig now, presumably Plank/Rock/Metal later) is a
  separate layered axis, same "orthogonal" relationship the ore family
  already has between metal type and CraftTier. Building material tiers
  ride the *existing* Crafting pipeline's material web rather than
  inventing a new one — Plank/Rock/Metal building pieces can't exist
  before their own material refinement chain does.
- **Two placement flows**: free (click-drop, release-to-rotate,
  click-to-confirm) for anything with nothing to snap to; one-click
  socket-snap for anything with a compatible edge in range — position
  and rotation are both implied by the socket in that case.
- **Foundation** — 5m×5m, reaches up to 5m downward from the aimed point
  (top-anchored, not center) to level across moderate terrain; a
  second panel snapped to a first inherits its exact top height rather
  than re-raycasting, which is the actual leveling mechanism.
- **Pole** — up to 10m reach, manually placed ahead of a Foundation when
  5m isn't enough (cliffs, water), exposes its own top socket so it's
  usable standalone too. No pole-to-pole stacking; unreachable terrain
  just fails placement, no escalation path.
- **Wall** — 5m wide × 3m high, one segment per Foundation edge exactly.
  Height deliberately decoupled from Foundation's 5m (a burial-depth
  tolerance, not a room-height statement).
- **Door** — its own full piece, socket-compatible with the same slot a
  Wall would occupy — a swap, not a runtime cutout.

**Explicitly still open, written into the doc rather than assumed:**
Foundation's visual (buried block vs. stilted platform), whether mixed-
material structures are allowed, Floor/Ceiling/Window/Roof shapes,
structural-support requirements beyond "a socket exists," where building
is allowed once territory/multiplayer exist, and exact material costs.

No code written this session — pure design, same status the Magic System
had before its own first implementation pass.

### MVP progress re-check (doc-only, no version bump)

Ben: "how are we doing on our mvp progress" — updated the "MVP Progress
Check-In" section in `docs/design-brief.md` (originally written earlier
the same session, before the interaction-model rebuild and the whole
Magic System) rather than re-answering from scratch.

- **Magic lineage assignment + early-tier ability use moves from
  not-built to built** — the single real status change. Three of four
  lineages (Elemental, Kinetic, Restoration) now have one genuinely
  working wish each; Illusion is still empty, so this is "built," not
  "complete."
- **Loot & gathering's interaction model was rebuilt** (`IPunchable`
  retired, skill-tiered hold-and-release) — doesn't change its
  built/not-built status, already counted as built, but flagged as a
  real mechanical change, not just polish.
- **Revised tally: 7 of Phase 1's 11 items built, 4 entirely unstarted**
  (encumbrance, building, combat/first aid, NPCs) — was 6/11 and 5
  unstarted at the last check-in.

No gameplay code touched.

### v0.1.155-dev — Magic gets zero UI hints, by design: "something people play with in order to explore it"

Ben, from a screenshot of "Pick up Backpack    Wish it would move (3s)"
showing simultaneously: "let's not share the 'wish' on the r at all. I
want this to be something people play with in order to explore it." A
real design stance, not just removing a redundant label — magic should
be discovered through experimentation, not explained on screen.

- **`PlayerInteraction.OnGUI` no longer shows any wish prompt text or
  progress bar at all.** `ResolveWishTarget`/`HandleWish` are completely
  unchanged — holding R still fills progress, still rolls success/
  failure, still spends Will and trains skills exactly as before. Only
  the player-facing hint is gone; the only feedback now is the world
  itself reacting (a campfire lighting, an object sliding, health
  climbing) or not.
- **Removed the R entry from `GameMenuScreen.ControlsList`** (the `` ` ``
  Game Menu's Controls tab) too — leaving an explicit "R: cast a wish"
  reference there would undercut the same goal for anyone who checks
  Controls, which is a normal, non-spoiler-breaking thing players do
  early. E and F keep their existing prompts/entries; this is specifically
  about hiding magic, not interaction in general.

Verified via a full batch-mode compile check.

### v0.1.154-dev — Fixed: R wish prompts always showed a "[R]" hint, even alone

Ben, from a screenshot of "[R] Heal Self (3s)" showing while looking at
plain grass with nothing else active: "we shouldn't give the 'Use R'
type hint... for any skill." Real bug, not a style nitpick — the
disambiguation logic (`bool multiple = ... || wishText != null`) bracketed
R the moment *any* wish was present at all, regardless of whether
anything else was actually competing for the same prompt line, unlike E
(which only ever got bracketed when F was also active).

- `PlayerInteraction.OnGUI` rewritten: E/F keep their existing bracketed
  disambiguation between each other, unchanged. The wish prompt is now
  always appended plain, with no `[R]` prefix, whether it's alone or
  (hypothetically, not shipped anywhere) alongside E/F.

Verified via a full batch-mode compile check.

### v0.1.153-dev — Restoration's Heal Self: the first Unconditional wish

Ben: "let's add a 'heal self' skill that give 10 health over 30 seconds.
add to restoration skill set." First real use of the `Unconditional`
targeting mode added in v0.1.152-dev specifically for a wish like this —
no world object involved at all, just Will and skill.

- **`PlayerVitals` gained heal-over-time state** — `StartHealOverTime
  (amount, duration)` computes a flat rate and ticks it down each frame,
  same shape as `bodyTemperature`'s drift-toward-neutral. Re-casting
  while one's already active replaces it outright rather than stacking
  or extending (simplest behavior, no spec given otherwise).
- **New `HealSelfWish.asset`** — Restoration, Crude unlock,
  `targeting = Unconditional`, same 60/40 Will split as Spark/Push (no
  different numbers specified, kept consistent rather than inventing a
  third placeholder pair).
- **`PlayerInteraction` special-cases Heal Self** in its Unconditional
  dispatch branch (`currentWish == healSelfWish` → `StartHealOverTime
  (10, 30)`), same "fine for one wish, revisit if a second Unconditional
  wish needs a real effect-dispatch abstraction" placeholder status as
  `pushForce`'s handling of Push.
- Added to `PlayerMagic.allWishes` (now 3 entries total) — Restoration
  finally has a wish of its own, joining Elemental (Spark) and Kinetic
  (Push); Illusion is still empty.
- **No aiming required** — Unconditional wishes don't raycast at all
  (see `ResolveWishTarget`'s `Unconditional` branch, v0.1.152-dev); a
  Restoration character can hold R to heal anywhere, looking at anything.

Verified via a full batch-mode compile check and by reading back the
saved scene/asset YAML to confirm `targeting: 2` (Unconditional) on the
new asset and real (non-`fileID: 0`) references throughout.

### v0.1.152-dev — "Default skill" selection: the player picks which wish R attempts

Ben: "let's consider the thought of being able to set a default skill.
for example, I could set 'push' as default, and even if I was aiming at
a fire, it would try to push if I had that skill... setting the default
skill to 'fireball' means you could shoot a fireball anytime you had
enough will." Real problem this solves: once a lineage has more than one
wish (Fireball alongside Spark, per the design brief's own Elemental
ladder sketch), the old model — R does whatever the crosshair happens to
offer — has no way to choose between them, and no path at all for a wish
that needs no physical target (Fireball flying at nothing in particular).

- **`WishRecipe` gained a `WishTargeting` enum** (`SpecificObject` —
  needs an `IWishTarget` offering this exact wish, the default, matches
  Spark; `AnyRigidbody` — matches Push; `Unconditional` — no target
  needed at all, gated purely on lineage/skill/Will, not used by any
  shipped wish yet but the dispatch path exists for when Fireball lands).
- **`PlayerMagic` is now the single source of truth for the wish list**
  (`allWishes`, moved off both `MagicScreen` and `PlayerInteraction`,
  which each held their own separate references before — only worked
  because there were exactly two wishes total). Added `KnownWishes`
  (filtered by lineage), `SelectedWish`, and `SelectWish(wish)`.
  Auto-selects the first known wish in `Awake` so single-wish gameplay
  keeps working with zero menu trips — explicit selection only matters
  once a lineage actually has two.
- **`MagicScreen` gained a real action** — a Select/Active button per
  known wish, previously pure read-only reference.
- **`PlayerInteraction`'s `ResolveWishTarget` rewritten to dispatch off
  `magic.SelectedWish.targeting`** instead of "try IWishTarget, fall back
  to Rigidbody" — it now only ever checks the one targeting mode the
  selected wish actually needs. `HandleWish`'s completion routing
  branches explicitly on targeting mode too, not on "is currentWishTarget
  null," so a future Unconditional wish doesn't misfire down the Push
  AddForce path.
- `PushWish.asset` set to `targeting = AnyRigidbody`; `SparkWish.asset`
  needed no change (`SpecificObject` is the default).

**Real ops hiccup, not a code bug:** the first batch-mode rewiring
attempt hung for 5+ minutes — a stale `bee_backend` process left over
from an earlier session was holding the project's compile lock, so the
new Unity instance sat blocked rather than failing fast the way "another
Unity instance is running" normally does. Diagnosed by reading the
partial log (`bee_backend: error: More than one copy of bee_backend
running... PID waiting`), killed the stuck process, reran clean.

Verified via a full batch-mode compile check, a grep for dangling
references to the removed `pushWish` field, and by reading back the
saved scene YAML to confirm `PlayerMagic.allWishes` holds both real
references and `PushWish.asset`'s `targeting` reads `1` (AnyRigidbody).

### v0.1.151-dev — All magic unified onto R; new IWishTarget interface

Ben: "let's change the spark and all magic to activate with r. we'll use
the mouse cursor to determine the target." Clarified on ask: no change to
the mouse/camera model itself — still look-based, same crosshair raycast
as everything else; "the cursor" just meant "wherever you're looking,"
not a literal free-moving pointer. Net change: Spark moves off E onto R,
joining Push, so all magic now shares one input.

- **New `IWishTarget` interface** — `Prompt`, `GetWish(PlayerMagic)`
  (returns null if this target has nothing for the given magic right now:
  wrong lineage, or e.g. an already-lit campfire), `OnWishComplete(player,
  succeeded)`. Distinct from `IInteractable`: every wish rides R, not E,
  and gates on `PlayerMagic`, not a tool.
- **`Campfire` converted from `IInteractable` to `IWishTarget`** — no
  longer part of the E/hold-to-gather system at all. Same effect
  (`SetLit`), just invoked via `OnWishComplete` instead of `Complete`.
- **`PlayerInteraction` gained a unified `ResolveWishTarget`/`HandleWish`
  pair**, replacing the Push-only version from v0.1.150-dev. Each frame:
  raycast for an `IWishTarget` first (a specific object like Campfire);
  if none, or its `GetWish` returns null, fall back to a plain
  `Rigidbody` for the generic Push case. Same hold-and-release shape
  either way — one shared progress counter, one shared green bar, one
  shared "[R] ..." prompt slot, whichever kind of target is in play.
- `GameMenuScreen.ControlsList`'s R entry generalized from
  "Kinetic: wish it would move" to "Wish at whatever you're looking at —
  Spark/Push/etc."

Verified via a full batch-mode compile check and a grep for dangling
references to the removed Push-only fields (`currentPushTarget`,
`CanPush`, `pushHoldProgress`) — none found.

### v0.1.150-dev — Kinetic's Push wish: a second, R-bound interaction channel

Ben: "I think we need to bind a new key to magic, like maybe r if not
used. that way we can use a kinetic 'push' skill to push the mid size
rock a short distance." Confirmed `R` was genuinely unused (grepped the
whole `Assets/Scripts/` tree) before binding it.

- **Deliberately a new channel, not IInteractable/E like Spark.** Spark
  targets one specific pre-flagged object (Campfire); Push needed to
  target *any* nearby Rigidbody the player picked ("any nearby
  Rigidbody-bearing chunk," Ben's call over a single dedicated pushable
  object), which doesn't fit IInteractable's "one wishable object" shape.
  Retrofitting every Rigidbody-bearing prefab in the game with a wish
  interface wasn't worth it for one wish — `PlayerInteraction` instead
  runs a second, independent raycast for `R`, generic against
  `GetComponentInParent<Rigidbody>()` rather than a specific interface.
- Same hold-and-release shape as E: hold R while aiming at a Rigidbody,
  a green bar fills (same `DrawHoldBar` visual, shared with E's), duration
  set by `PlayerSkills.GetHoldDuration(Kinetic)` — same skill-tiered
  curve as everything else. On completion, `PlayerMagic.TryWish` runs the
  same success/failure roll Spark uses (50%→90% by margin, 60/40 Will);
  on success, `Rigidbody.AddForce` shoves the target (`ForceMode.Impulse`,
  magnitude 6, placeholder/tunable) in the camera's forward direction.
- **New `PushWish.asset`** (Kinetic, Crude unlock, 60/40 Will, same
  numbers as Spark — no reason given yet to differ, kept consistent
  rather than inventing new placeholders). Added to `MagicScreen.allWishes`
  alongside Spark, so a Kinetic character's Magic tab now lists it.
- **Prompt only shown to a player who actually knows Kinetic** — a real,
  deliberate divergence from the tool-gated prompts elsewhere (Pickaxe
  requirement shows to everyone, since anyone could pick one up). Under
  today's single-starting-lineage rule a non-Kinetic character can never
  attempt Push at all, so showing the prompt to them would be dead,
  misleading UI rather than an honest "here's what you're missing" like
  the tool prompts are.
- `GameMenuScreen.ControlsList` updated with the new R binding, noted as
  Kinetic-only.

Verified via a full batch-mode compile check and by reading back the
saved scene YAML to confirm both `pushWish` (on `PlayerInteraction`) and
the 2-element `allWishes` array (on `MagicScreen`) hold real references,
not `fileID: 0` — no repeat of the earlier stale-reference gotcha this
time, since assets were loaded after `OpenScene` from the start.

**Known gap, not fixed here:** if a player somehow holds both E and R at
once, both progress bars share the same screen position/texture — an
unlikely edge case, not engineered around.

### v0.1.149-dev — Spark gets a real success/failure roll; Will costs and regen tuned

Ben tested v0.1.148-dev live, confirmed Spark works end-to-end, then gave
real tuning numbers: "at a successful use of the wish, will should drop
by 60 points. it should regen 1 point every 5 seconds. a failure should
cost 40 points."

- **`WishRecipe.willCost` split into `successWillCost` (60) and
  `failureWillCost` (40)** — different outcomes now cost different
  amounts, which meant a wish attempt needed an actual outcome to
  determine first.
- **`PlayerMagic.TryWish` gained a binary success/failure roll**, same
  interpolated-by-skill-margin shape as `PlayerCrafting`'s existing
  chance-of-creation system (`RollOutcome`) — 50% success chance right at
  the unlock threshold, rising to 90% once ~20 skill points past it.
  Either outcome still trains the skill and spends Will (a failed attempt
  isn't a non-attempt); only success grows Will's max and lets `Campfire`
  actually light. This is closer to the ideation session's original
  "with luck, it would actually start" pitch than the "weakest-link
  against fuel tier" idea design-brief.md had settled on — **that idea
  was never built**, flagged directly in the doc rather than left to look
  like both are simultaneously true.
- **`CanAttempt` gates on `successWillCost`, not `failureWillCost`** —
  deliberate: success costs more, so requiring only the cheaper amount
  could let a roll succeed and then be unable to actually pay for it.
- Added a failure message ("The wish didn't take — Spark fizzled."),
  same stacking convention as `PlayerSkills`'/`PlayerCrafting`'s own
  messages (`y=150`, below both) — a held-and-completed action that does
  nothing with zero feedback was exactly the kind of silent-failure gap
  this project has repeatedly fixed elsewhere (see the chance-of-creation
  system's own history).
- **Will regen changed from a 4/s placeholder to 1 point per 5 seconds**
  (`0.2f`), per Ben's number.
- `SparkWish.asset` verified to actually deserialize the new fields
  correctly (`successWillCost=60 failureWillCost=40`, confirmed via a
  throwaway batch-mode script's log output, not just assumed) and
  resaved to drop the now-dead `willCost: 10` YAML.

Verified via a full batch-mode compile check, throwaway scripts deleted
after. Docs updated: `docs/design-brief.md`'s Magic System section now
flags the weakest-link-vs-actual-roll divergence explicitly.

### v0.1.148-dev — Magic System: first real slice — Will, starting lineage, and Spark lighting a Campfire

Ben: "let's build the magic system" — the first real implementation off
the same-day ideation session (see the doc-only entries below for the
design conversation). Scoped deliberately: build the full skeleton plus
one genuinely working wish, not all four lineages at once.

- **Will**, a real sixth `PlayerVitals` field — starts at 100, regens
  passively like Stamina (no drain-state needed, since Will is spent as
  one lump per completed wish, not continuously). `ConsumeWill`/
  `GrowMaxWill` added; `GrowMaxWill` raises the ceiling *and* tops up
  current Will, so growth reads as a real gain, not just cap-raising.
  Added to `VitalsBarHUD` as a new third row (single full-width bar,
  scaled against its own live `MaxWill`, not the other four bars' fixed
  150% scale — Will's ceiling grows, so a fixed scale would read as
  permanently-near-full over time).
- **`SkillCategory.Magic`** added (`SkillDefinition.cs`) — the four
  lineages' home in the Skills tab, alongside Gathering/CraftingDiscipline/
  Combat. Four new `SkillDefinition` assets: `Elemental`, `Illusion`,
  `Kinetic`, `Restoration`.
- **`PlayerMagic`** (new component) — assigns one random starting lineage
  per character at spawn (keeps Pillar 7's "no lineage-less players"),
  exposes `IsLineageKnown`/`CanAttempt`/`TryWish`. Learning additional
  lineages later is explicitly **not built** — rides the Phase 2
  skill-books mechanic, which doesn't exist yet, so every character only
  ever knows their one starting lineage for now.
- **`WishRecipe`** (new `ScriptableObject`) — sibling to `CraftingRecipe`:
  `lineage`, `unlockTier` (reuses `CraftTierScale.SkillRequirement`
  directly), `willCost`, `skillGain`. No material-tier weakest-link input
  on the data class itself — that's decided per wish target instead (see
  Campfire below).
- **Spark**, the first real wish, and **`Campfire`**, its target: an
  unlit campfire (primitive logs + kindling + a `Light`, same
  "primitives first" precedent Backpack set) that lights when a player
  who knows Elemental holds E through a skill-tiered duration (same
  `PlayerSkills.GetHoldDuration` mechanic gathering uses) with enough
  Will. **Simplification from the design doc, flagged not hidden:**
  lighting is unconditional once the gates pass — there's no fuel-tier
  input to cap quality against, so the "weakest-link vs. tinder tier"
  idea from the ideation session isn't actually implemented here.
- **New `Magic` tab** (`MagicScreen`, `PlayerMenuScreen`) — read-only
  reference (lineage known, Will current/max, known wishes with
  locked/unlocked state), not a clickable list, since wishes fire from
  the in-world E-hold prompt on their target, not a menu button.
- Placed one Campfire in `TestScene.unity` at `(-4, 0.3, -2)`.

**Real bug hit and fixed while wiring the scene:** adding
`[RequireComponent(typeof(MagicScreen))]` to `PlayerMenuScreen` meant
Unity auto-created an *empty* `MagicScreen`/`PlayerMagic` on `Player` the
moment the scene loaded — **before** the setup script's own
`AddComponent` calls ran, leaving two of each (the auto-created empty
one and the script's own). Fixed two ways: added `[DisallowMultipleComponent]`
to both (matching `PlayerVitals`/`PlayerSkills`'s existing convention,
should have been there from the start) and rewrote the wiring script to
`GetComponent ?? AddComponent` instead of assuming a fresh add. Also
re-hit this project's own documented gotcha in the process — object
references fetched *before* `EditorSceneManager.OpenScene()` go stale
(`fileID: 0`) once the scene opens; fixed by loading the lineage/wish
assets after opening the scene, not before.

Verified via a full batch-mode compile check (throwaway
`Assets/Editor/CompileCheck.cs`, deleted after) and by reading back the
saved scene YAML to confirm exactly one `PlayerMagic`/`MagicScreen` each
with real (non-`fileID: 0`) references, not just trusting the batch log.

**Known gaps, not fixed here:** Fireball (needs combat), scrolls and
learnable second lineages (Phase 2, ride skill-books), Illusion/Kinetic/
Restoration's own wishes, and Spark's missing weakest-link fuel-tier
input (see above).

### v0.1.147-dev — Punch-to-break retired: gathering/chopping now hold-and-release, skill-tiered

Ben: "let's build this pig!" — implements the interaction-model ideation
from this same session (see the doc-only entries below/above for the
design conversation this comes from). `IPunchable` is gone entirely.

- **`IPunchable` deleted outright.** `ResourceNode` (Rock Node, Boulder,
  the full Copper/Iron/Silver/Gold/Platinum Ore family) and `ChoppableTree`
  now implement `IInteractable` instead — same hold-E-to-fill/release-to-
  cancel model every other interactable already used, just with a real
  non-zero duration for the first time. `hitsToBreak`/`hitsToChop` counter
  fields removed; `OnPunch` became `Complete`, called once when the hold
  finishes rather than once per punch.
- **`IInteractable.HoldDuration` (a flat per-item constant, silently unused
  by anything until now) became `GetHoldDuration(GameObject player)`** —
  needs the acting player because duration is skill-dependent. All ~12
  always-instant implementers (Pickup, Backpack, Belt, Canteen, Coin,
  Lockbox, NavigationComputer, PersonalHealthMonitor, MiningFaceShield,
  Sunglasses, WaterSource, BankBox) got the mechanical one-line signature
  update, unchanged behavior (still instant).
- **Duration is skill-tiered, low tier takes longest**: `CraftTierScale`
  gained `HoldDuration(CraftTier)` (Crude 3s → Masterwork 0.5s — placeholder
  numbers, same "tune by playtesting" status as every other value in that
  table) and `TierForSkillLevel(float)` (the inverse of the existing
  `SkillRequirement`, walks the same 0/10/25/50/100 thresholds). `PlayerSkills`
  gained `GetHoldDuration(SkillDefinition)` tying the two together — a
  node/tree reads the player's live skill level, buckets it into a tier,
  looks up that tier's duration. No new per-instance scene data needed.
- **Real green progress bar added** to `PlayerInteraction`'s crosshair HUD,
  under the existing countdown-seconds text — only draws while a hold is
  actually filling.
- **Scoped to gathering/chopping only**, not every interactable — Pickup,
  equip, drink, bank, etc. all stay instant, matching "replaces punch-to-hit"
  rather than "everything now takes time." The Crafting screen's own
  instant "Craft" button is a deliberate **fast-follow, not done here** —
  different UI surface (menu-driven, not world-raycast), needs its own
  progress/cancel affordance.
- Updated `GameMenuScreen.ControlsList`: removed the dead "Left Mouse
  Button — Punch" entry, folded the hold behavior into the existing "E" row.
- Verified via a full batch-mode compile check (throwaway
  `Assets/Editor/CompileCheck.cs`, deleted after) — clean, no `CS####`
  errors.

**Known gaps, not fixed here:** tool-tier doesn't yet speed this up on top
of skill tier (the pipeline's "Tool-quality effects" bullet promises this,
not implemented); the Crafting screen's Craft button (see above); Escape
has no explicit cancel wiring (release already cancels, judged sufficient
per Ben's call during ideation).

### Magic System fully fleshed out — Will, tiered wishes, learnable lineages, scrolls (doc-only, no version bump)

Ideation session with Ben on the previously-thin Magic System placeholder,
sparked by his original "wish it would..." pitch (emote a wish, luck-based
success). Converged on a real, buildable shape reusing crafting's existing
mechanics rather than inventing parallel ones — see `docs/design-brief.md`'s
Magic System section for the full writeup. Summary of what got decided:

- **Wishes** trigger off pre-flagged contextual moments (same
  `IInteractable`/`ISecondaryInteractable` prompt pattern already shipped),
  not free-form intent parsing.
- **Will** — new sixth survival vital, added to Character Creation & Stats.
  Starts full like the other five; unlike them, its max pool grows through
  use rather than staying fixed. One shared pool per character.
- **Wishes are tiered `CraftingRecipe`-style recipes**, reusing two rules
  crafting already has: recipe-unlock gating (skill threshold before a wish
  is attemptable) and weakest-link output quality (capped by both caster
  skill tier and the tier of whatever material is present). Sketched an
  illustrative Elemental ladder (Spark → Fireball → forge-grade Spark) — the
  other three lineages' ladders are still unsketched, flagged Still Open.
- **Lineages are learnable, not a lifetime lock** — free starting lineage
  (keeps Pillar 7's "no lineage-less players"), any other lineage trainable
  later exactly like any of the other 16 skills in the game, no cap, pure
  player choice. Rides the existing Phase 2 skill-books mechanic as its
  unlock vehicle — **this piece is Phase 2 scope**, not Phase 1.
  Cross-referenced from the Phase 2 skill-books/magazines bullet.
- **Two scroll paths**, both Phase 2: found scrolls roll their lineage+wish
  **on read**, not on spawn (keeps the luck flavor genuine rather than being
  ordinary hidden loot); scribed scrolls are deterministic, gated on a
  dedicated Scribing skill *and* the source wish at Normal tier, and grant
  only the unlock — never skill progress — so buying a scroll never skips
  training.
- Updated Pillar 7 and the Character Creation & Stats "Magic lineage" bullet
  to match (randomized-at-start, not randomized-forever).
- **UI impact assessed against the real current code**, not imagined: a new
  `Magic` tab/`MagicScreen` on `PlayerMenuScreen` (read-only reference list,
  same shape as Skills — wishes fire from in-world prompts, not a Craft
  button); a new `Magic` value on the `SkillCategory` enum; Scribing needs
  no new UI at all (rides the existing Crafting tab as an ordinary
  discipline + recipes); `InventoryScreen` needs a new "Read" per-item
  action for Unidentified Scrolls; `VitalsBarHUD`'s hardcoded 2×2 grid has
  no slot for Will yet (same pre-existing gap Body Temperature already has).

**Still open, written into the doc rather than assumed:** the other three
lineages' wish ladders; whether the free starting lineage keeps any
permanent edge; whether Scribing should be its own skill or shared with the
Phase 2 crafting-manuals idea; Will's regen rule and whether Scribing itself
costs Will/materials; and whether the wish-trigger emote is a literal
chat/emote-wheel action or just reuses the E/F-interact pattern.

No gameplay code touched — pure design-doc session, no version bump per
`CLAUDE.md`'s doc-only-commit rule.

### Design brief comparison pass — MVP progress check-in (doc-only, no version bump)

Ben: "let's update, and do a comparison of our mvp doc again," after the long
item/model/icon audit stretch below. Read `docs/design-brief.md` end to end
and checked its claims directly against `Assets/Scripts/`, `Assets/Data/`, and
`TestScene.unity` rather than trusting the doc's own prior "shipped"/"still
open" notes.

- Added a new **"MVP Progress Check-In (2026-08-08)"** section rolling up
  Phase 1's 11 items against real code: 6 genuinely built (skill progression,
  food/water, loot & gathering, crafting-quality content, storage, skills UI),
  5 entirely unstarted (encumbrance, building, combat/first aid, magic,
  hireable NPCs). Net finding: tonight's very large volume of work was almost
  entirely deepening the two already-started pillars (loot & gathering,
  crafting-tier content), not starting a new Phase 1 pillar.
- **Found and fixed a real doc/code mismatch**: the design brief declared the
  `Mining` skill split from `Gathering` "decided... no longer open" back on
  2026-08-05, but no `Mining.asset` `SkillDefinition` was ever created — every
  `ResourceNode` in the scene, including the now-fully-shipped Silver/Gold/
  Platinum ore family, still trains `Gathering`. Flagged directly in the
  Skills section rather than left implied.
- Marked the Silver/Gold/Platinum hidden-ore + Mining Face Shield mechanic as
  **shipped** (it was written as a future plan; it's been real and working
  since `v0.1.60-dev`, confirmed by Ben's own playtest) — while also noting
  two real gaps: the Mining-tier-4 shield-bypass has no code to check (no
  Mining skill exists yet), and the Shield's own model is still the original
  placeholder Cylinder despite everything else in its recipe chain being real.
- Corrected a stale reference to the deleted Secret Message Wall (removed
  `v0.1.126-dev`) in the same ore/shield paragraph.
- Updated the Wood and Textiles material-web bullets to describe what
  actually shipped (Tree→Log→Plank has no Twigs/Saw step; Cloth/Fiber have
  real models now but no recipe or gather source yet) rather than only the
  original plan.
- Updated the "5 items without a defining discipline" note — Canteen is now a
  fully real item (model/fill/tint), not just a placeholder, even though it
  still trains no skill per that rule.

No gameplay code touched — `TEST_FEATURE_PLAN.md` unchanged, no version bump
per `CLAUDE.md`'s doc-only-commit rule.

### v0.1.146-dev — Fiber gets a real model (Grass Wispy by Quaternius)

Ben downloaded "Grass Wispy by Quaternius" (Poly Pizza, public domain)
by hand — last of the two raw materials off the audit list.

- Imported as `Assets/Models/GrassWispy_Quaternius.glb`, built
  `Assets/Prefabs/FiberPickup.prefab` (hardcoded item,
  `ContinuousDynamic`, measured bounds `0.23x0.25x0.24`), wired to
  `Fiber.asset.worldPickupPrefab` for the first time. Icon +
  previewIcon baked via `IconBaker` — reads clearly as a wispy tuft of
  grass/fiber strands.
- **Credits**: added to `Assets/Models/THIRD_PARTY_CREDITS.md` and the
  live Credits tab — `"Grass Wispy by Quaternius [Public Domain] via
  Poly Pizza"` — full treatment despite being public domain, same
  precedent as Wood Planks and Pickaxe.
- **Cloth and Fiber are now both done** — the last two items in the
  "raw materials" category from tonight's original audit.

### v0.1.145-dev — New "Woven Grass Cloth" item — second material path, per the tint experiment

Ben: "let's duplicate the cloth model and call it 'woven grass cloth'.
then run the standard path on it for tiers." Turns the v0.1.144-dev
tint evaluation into a real, permanent second item rather than a
throwaway test render.

- New `WovenGrassClothItem.asset` (itemName "Woven Grass Cloth",
  maxStack 20) — standalone, not part of any CraftTier ladder, same as
  `Cloth` itself.
- New `Assets/Data/WovenGrassCloth.mat` — a clone of Cloth's actual
  in-game material with `baseColorFactor`/`_BaseColor`/`_Color` tinted
  green, same static-variant pattern as the Copper/Iron/Silver/Gold/
  Platinum ore family (one shared mesh, separate tinted `.mat` assets)
  rather than Canteen's runtime-script approach — this is a
  permanently-different item, not one object whose state changes live.
- New `WovenGrassClothPickup.prefab` — reuses `PaleCloth.glb`'s mesh
  with the new green material, same measured-fit discipline as every
  other pickup (`0.25x0.20x0.28`, `ContinuousDynamic`, hardcoded item).
- Icon + previewIcon baked via `IconBaker`.
- **No recipe yet** — this is the material existing for a future
  clothing system to consume, not a craftable item today. Visually
  it's still the same smooth-folded cloth shape tinted green (the
  known limitation from the v0.1.144-dev evaluation — reads as green
  cloth, not distinctly "woven grass"), accepted as good enough for now
  per Ben's call.

### v0.1.144-dev — Cloth gets a real model (pale folded cloth); tint trick confirmed reusable

Ben wanted to ideate on Cloth/Fiber's visual treatment before building
anything — landed on: generate a pale cloth, confirm the Canteen-style
runtime tint trick generalizes to it (for potential future dyed/colored
cloth variants), then just ship the pale version since this was mostly
an evaluation pass.

- Generated via Tripo3D's API (`"a small folded square piece of cloth,
  pale off-white plain-woven fabric, visible woven texture and fold
  creases, isolated on a plain background, no person, no model,
  low-poly game asset"`, 20 credits) — clean on the first attempt.
- Imported as `Assets/Models/PaleCloth.glb`, built
  `Assets/Prefabs/ClothPickup.prefab` (hardcoded item, `ContinuousDynamic`,
  measured bounds `0.25x0.20x0.28`), wired to `Cloth.asset.
  worldPickupPrefab` for the first time. Icon + previewIcon baked via
  `IconBaker`.
- **Confirmed the material-tint technique generalizes**: cloned the
  material, set `baseColorFactor` to a green tint, rendered a
  throwaway evaluation preview (not committed to any asset) to check
  whether a tinted "woven grass cloth" variant would read well.
  Mechanically it worked identically to the Canteen fix — but visually
  it just read as a solid green cushion, not a grass texture, since
  tinting multiplies against the existing (smooth-folded, not woven-
  grain) albedo rather than adding new texture detail. **Conclusion**:
  the tint trick is solid for simple flat-color variants of the same
  base cloth (same pattern as the Copper/Iron/Silver/Gold/Platinum ore
  family sharing one rock mesh), but a genuinely "woven grass" look
  would need its own separately-generated texture, not a tint on this
  model. Not pursued further this session — pale cloth ships as-is.

### v0.1.143-dev — Hammer CraftTier ladder gets a real model (AI-generated stone hammer)

Ben: "I don't see a decent stone hammer, so let's go the api route" —
third tool ladder off the backlog, via Tripo3D this time instead of a
hand-downloaded model.

- Generated via Tripo3D's API (`"a crude stone hammer with a wooden
  handle, primitive tool, rough grey stone head bound to the handle
  with cord, isolated on a plain background, no person, no model,
  low-poly game asset"`, 20 credits) — clean on the first attempt, no
  500s, no timeout. Reads clearly as a stone-headed hammer bound to a
  wooden handle with cord, matching the game's established "crude
  primitive tool" aesthetic (same family as Crude Stone Knife).
- Imported as `Assets/Models/StoneHammer.glb`. Same 5-tier build as
  Pickaxe/Axe: first tier measured fresh (target length `0.6`, final
  bounds `0.60x0.55x0.59` — chunkier than the bladed tools, as expected
  for a hammer head), the other 4 reuse that exact fit.
- Icons + previewIcons baked for all 5 via `IconBaker`.
- No credits needed — Tripo3D API content has its own no-attribution
  commercial license (see `Tools/Tripo3D/README.md`), unlike the
  CC-BY/public-domain downloads used for Pickaxe and Axe.
- Same note as the other tool ladders: these 5 `ItemDefinition`s are
  referenced by `ResourceNode.requiredTools` wherever Hammer is gated
  (Lockbox, per `BUGS_AND_ENHANCEMENTS.md`'s Belt entry) — only model/
  icon/`worldPickupPrefab` touched, guids untouched.

### v0.1.142-dev — Axe CraftTier ladder gets a real model (Low Poly Axe by suerozcelik)

Ben downloaded "Low Poly Axe by suerozcelik" (Poly Pizza, CC-BY) by
hand — second tool ladder off the backlog, same shape as Pickaxe.

- Imported as `Assets/Models/Axe_suerozcelik.fbx` — **first `.fbx`
  import this session** (everything before was `.glb`). Unity's native
  FBX importer handled it directly with no extra steps; materials/
  colors came through intact with no separate texture files needed
  (confirmed by eye before baking the rest — reads clearly as a
  wood-handled axe with a metal head).
- Built 5 new prefabs from scratch (`CrudeAxePickup` through
  `MasterworkAxePickup`), same pattern as Pickaxe: first tier measured
  fresh (target length `0.6`, final bounds `0.25x0.60x0.04`), the other
  4 reuse that exact fit.
- Icons + previewIcons baked for all 5 via `IconBaker`.
- **Credits — CC-BY, attribution required**: added to
  `Assets/Models/THIRD_PARTY_CREDITS.md` and the live Credits tab —
  `"Low Poly Axe by suerozcelik [CC-BY] via Poly Pizza"`.
- Same note as Pickaxe: these 5 `ItemDefinition`s are referenced by
  every `ResourceNode.requiredTools` array gated on Axe (Tree, Log) —
  only model/icon/`worldPickupPrefab` touched, guids untouched.

### v0.1.141-dev — Pickaxe CraftTier ladder gets a real model (Pickaxe by CreativeTrio)

Ben downloaded "Pickaxe by CreativeTrio" (Poly Pizza, public domain) by
hand — first tool ladder tackled from the remaining backlog, same
"wire one model to all 5 tiers" shape as Knife.

- Imported as `Assets/Models/Pickaxe_CreativeTrio.glb`. Unlike Knife,
  no placeholder prefab existed at all for any Pickaxe tier — built 5
  new prefabs from scratch (`CrudePickaxePickup` through
  `MasterworkPickaxePickup`), each hardcoding its own tier's item.
  First tier measured fresh (uniform-scaled to a `0.6` target length,
  matching Stick's own held-tool scale — final bounds
  `0.39x0.07x0.60`), the other 4 reuse that exact fit so all 5 render
  identically instead of accumulating per-bake variance.
- Icons + previewIcons baked for all 5 via `IconBaker`.
- **Credits**: added to `Assets/Models/THIRD_PARTY_CREDITS.md` and the
  live Credits tab — `"Pickaxe by CreativeTrio [Public Domain] via
  Poly Pizza"` — full treatment despite being public domain, matching
  the precedent set for Wood Planks by Quaternius.
- **Note:** these 5 Pickaxe `ItemDefinition`s are also referenced by
  every `ResourceNode.requiredTools` array in the game (Copper/Iron/
  Silver/Gold/Platinum Ore Nodes, Boulder, Rock Node) — only the model/
  icon/`worldPickupPrefab` fields were touched, guids and all existing
  references untouched, confirmed nothing there needed updating.

### v0.1.140-dev — Removed the redundant "Fiber Belt" item

Ben, after reviewing what was actually left to build for it: "I think
the fiber belt is the grass belt already. so we can likely remove all
references for it." The Normal-tier `Fiber Belt` (`BeltItem.asset`) was
the original pre-ladder "Belt" item, renamed in v0.1.79-dev when
`Crude Fiber Belt` shipped as the ladder's first real tier — it had
never been given its own model/icon, was still a bare Cube placeholder
standalone GameObject in the scene (not even a real `PrefabInstance`),
and Rudimentary/Fine/Masterwork Fiber Belt were never built either.
Redundant with `Crude Fiber Belt`, which already has real content.
Confirmed via guid search before deleting (same discipline as every
other removal tonight) — nothing referenced `BeltItem.asset` except its
own `.meta` and the scene object. Deleted `BeltItem.asset` and the
scene's standalone "Belt" GameObject at `(-2, 0.3, 1.5)`.
`BUGS_AND_ENHANCEMENTS.md`'s Belt-ladder entry updated to note the
removal; `TEST_FEATURE_PLAN.md`'s regression check referencing the old
found Belt updated to say it's gone, not to expect it.

### v0.1.139-dev — Berry gets a real model (Strawberries by Jarlan Perez)

Ben downloaded "Strawberries by Jarlan Perez" (Poly Pizza, CC-BY) by
hand — last item off the double-gap list from tonight's audit.

- Imported as `Assets/Models/Strawberries_JarlanPerez.glb`, replacing
  `BerryPickup.prefab`'s placeholder Sphere. `ContinuousDynamic`
  confirmed/set, collider resized to the real measured bounds
  (`0.35x0.31x0.31`).
- **Found the same "standalone copy, not a real `PrefabInstance`" bug**
  on the scene's pre-placed "Berry Bush" (same class as Canteen in
  v0.1.128-dev and Backpack in v0.1.132-dev) — replaced with a real
  `PrefabInstance` at the same position so the model swap actually
  reaches it.
- Icon + previewIcon baked via `IconBaker`.
- **Credits — CC-BY, attribution required this time** (unlike Rock/Wood
  Planks by Quaternius, both public domain): added to
  `Assets/Models/THIRD_PARTY_CREDITS.md` and the live Credits tab
  (`GameMenuScreen.cs`) — `"Strawberries by Jarlan Perez [CC-BY] via
  Poly Pizza"`, exact text from the download popup.

### v0.1.138-dev — Crude Knife's model wired to the other 4 Knife tiers

Ben, after confirming Crude Knife already had the real Tripo3D model
and just needed the other tiers matched up to it — same shape as the
Backpack ladders: "let's wire up the crudeknife asset to the other 4
tiers and do the icon work."

- `RudimentaryKnife`/`Knife` (Normal)/`FineKnife`/`MasterworkKnife` all
  had real recipes already (`v0.1.69-dev`) but zero model/icon/
  `worldPickupPrefab` — same gap the Backpack ladder tiers were in
  before tonight.
- New prefabs (`RudimentaryKnifePickup`/`NormalKnifePickup`/
  `FineKnifePickup`/`MasterworkKnifePickup`), each hardcoding its own
  tier's item on `Pickup` (matching standard rules, even though — like
  `RockKnifePickup` itself — these are only ever used as a
  `worldPickupPrefab`, never a `chunkPrefab`). Rather than re-measuring
  the model fresh per tier, copied `RockKnifePickup.prefab`'s
  already-proven child scale/collider values directly, so all 5 tiers
  render pixel-identical instead of accumulating small per-bake
  variance.
- Icons + previewIcons baked for all 4 via `IconBaker` — confirmed
  identical bounds (`0.08x0.05x0.35`) to Crude Knife's own bake, proving
  the copied-fit approach worked exactly.

### v0.1.137-dev — Plank gets a real model (Wood Planks by Quaternius)

Ben downloaded "Wood Planks by Quaternius" (Poly Pizza, public domain)
by hand and asked for the full treatment: credits, model, icon, and a
scene spawn.

- Imported as `Assets/Models/WoodPlanks_Quaternius.glb`, replacing
  `PlankChunk.prefab`'s placeholder Cube — this is the real chunk
  `Log.prefab` drops when chopped (confirmed via guid cross-reference:
  `Log.prefab`'s `ResourceNode.chunkPrefab` already pointed to
  `PlankChunk.prefab`, and its `Pickup.item` was already correctly
  hardcoded to `Plank.asset` — the chop-drop path itself was never
  broken, just showing a placeholder). `ContinuousDynamic` already set,
  collider resized to the real measured bounds (`0.25x0.04x0.60`).
- **`Plank.asset.worldPickupPrefab` wired for the first time** — it was
  empty (`{fileID: 0}`) despite `PlankChunk.prefab` already existing
  and already being the correct chunk; Admin spawn / drop-and-repickup
  would have fallen back to a generic grey cube before this.
- Icon + previewIcon baked via `IconBaker`.
- **Credits**: added to `Assets/Models/THIRD_PARTY_CREDITS.md` and, per
  Ben's explicit ask this time, also to the live Credits tab
  (`GameMenuScreen.cs`) — `"Wood Planks by Quaternius [Public Domain]
  via Poly Pizza"`. Public domain doesn't strictly require this (see
  Rock by Quaternius above, which was deliberately left out of the live
  tab), but Ben asked for the full treatment here.
- Placed one in `TestScene.unity` at `(6, 0.3, 2)`.

### v0.1.136-dev — Removed the orphaned Wood item

Ben's call while triaging the model/icon audit's remaining double-gap
items: rather than give `Wood` a real model/icon, eliminate it outright
— the Stick/Plank material line already covers that role, and Wood had
been completely un-gatherable (`BUGS_AND_ENHANCEMENTS.md`) since the
tree-chopping rework in v0.1.83-dev replaced its old direct drop with
Log/Plank. Confirmed via guid search before deleting (same discipline
as the Tree/Secret Wall removal in v0.1.126-dev): `WoodChunk.prefab`
and `Wood.mat` were referenced by nothing except `Wood.asset` itself.
Deleted `Wood.asset`, `WoodChunk.prefab`, `Wood.mat`, and their `.meta`
files. `BUGS_AND_ENHANCEMENTS.md`'s Wood entry removed; its
cross-reference from the still-open `MediumRock.asset` (Rock item)
entry updated to point at this instead.

### v0.1.135-dev — Leather Backpack becomes its own 5-tier CraftTier ladder

Ben: "let's wire the leather backpack model to all 5 leather backpack
tiers" — same treatment the grass `Backpack` ladder just got
(v0.1.134-dev), applied to the brand-new `Leather Backpack` item.
`LeatherBackpackItem` (built last version) becomes the Normal tier;
built the other 4 from scratch:

- New `CrudeLeatherBackpackItem`/`RudimentaryLeatherBackpackItem`/
  `FineLeatherBackpackItem`/`MasterworkLeatherBackpackItem`, each with
  its own prefab (`CrudeLeatherBackpackPickup.prefab`, etc.),
  instantiating the same `CrudeLeatherBackpack.glb` model. Same
  capacity curve as every other tiered container this session (Crude 4
  / Rudimentary 6 / Normal 8 / Fine 12 / Masterwork 16), `tier` field
  set correctly on each (0/1/2/3/4, matching `CraftTierNames`'
  convention). All `ContinuousDynamic`, all wired to their own
  `worldPickupPrefab`.
- Icons + previewIcons baked for all 4 new tiers via `IconBaker`.
- **Only the Normal tier (`Leather Backpack`) has a crafting recipe**
  (the placeholder 6x Cloth + 4x Rope one from v0.1.134-dev) — the
  other 4 tiers are data + a real model/icon, but Admin-spawn-only for
  now, same situation as the grass `Backpack` ladder's own
  Crude/Rudimentary/Fine/Masterwork tiers.

### v0.1.134-dev — Grass model across the whole Backpack CraftTier ladder; new Leather Backpack

Ben: "let's wire the model to all tiers of the grass backpack" →
clarified as all 5 tiers of the `Backpack` `CraftTier` ladder (Crude/
Rudimentary/Normal/Fine/Masterwork — distinct from the already-real
`Crude Fiber Backpack`, a separate single-tier item). Then, catching
that this would orphan the Normal tier's existing leather model: "that
should orphan the leather backpack. let's create a leather backpack
crafting tier, under sewing. create recipes per our standard, and
we'll adjust the materials later."

- **All 5 `Backpack` ladder tiers now use the Grass Backpack model**
  (`Assets/Models/GrassBackpack.glb`, from v0.1.133-dev): `Backpack.
  prefab` (Normal) had its visual swapped from `CrudeLeatherBackpack.
  glb` to grass; four brand-new prefabs (`CrudeBackpackPickup`,
  `RudimentaryBackpackPickup`, `FineBackpackPickup`,
  `MasterworkBackpackPickup`) built from scratch for the other four
  tiers, which previously had **no prefab, no icon, no world pickup at
  all** — data-only, unreachable in play, per `BUGS_AND_ENHANCEMENTS.md`.
  Capacity per tier matches the design already logged there (Crude 4,
  Rudimentary 6, Normal 8, Fine 12, Masterwork 16); all wired to their
  `ItemDefinition.worldPickupPrefab`, all `ContinuousDynamic`.
- **New `Leather Backpack`** — a standalone item (same "single item
  outside the ladder" pattern as `Crude Fiber Backpack`), giving the
  leather model a real home instead of leaving it unused. New
  `LeatherBackpackItem.asset`, `LeatherBackpack.prefab` (instantiates
  `CrudeLeatherBackpack.glb` fresh — the model file itself was never
  touched, just no longer referenced by the Normal tier), capacity 8.
  **`LeatherBackpackRecipe.asset` — placeholder ingredients (6x Cloth +
  4x Rope), Sewing-trained, per Ben's explicit call to build the recipe
  shape now and swap in real Leather/hide materials once that material
  chain exists** (`BUGS_AND_ENHANCEMENTS.md` had previously held off on
  any Backpack-ladder recipes for exactly this reason — this doesn't
  fill in the ladder itself, just unblocks the new standalone item).
  Wired into `PlayerCrafting.recipes` in `TestScene.unity`.
- Icons + previewIcons baked for all 6 items (5 ladder tiers + Leather
  Backpack) via `IconBaker`. Cleaned up two more orphaned old icon
  files (`BackpackIcon.png`/`BackpackPreviewIcon.png`, superseded by
  `BackpackItemIcon.png`/`...Preview.png` once the Normal tier's model
  changed and its icon got re-baked under the item-asset-name
  convention).

### v0.1.133-dev — Crude Fiber Backpack gets a real model (woven grass basket)

Second double-gap item off tonight's audit. Ben: "let's use the api to
generate a woven grass backpack, create a good prompt and we'll just
use what it produces."

- Generated via Tripo3D's API (`"a small woven grass backpack, plant
  fiber cordage bag with shoulder straps, isolated on a plain
  background, no person, no model, low-poly game asset"`, 20 credits)
  — hit the same 20-minute server-side timeout pattern as the Grass
  Belt/Knife before it (client gave up, task actually succeeded a bit
  later; caught via direct task polling). **A clean, strong result on
  the first attempt** — a proper backpack silhouette this time (unlike
  the Grass Belt, which came back as a closed ring rather than an open
  strap): woven straw/grass basket body, brown leather straps, buckle
  closure. Used as-is per Ben's call.
- Download itself needed a resumed retry (`curl -C -`) — the 42MB file
  was still transferring when the tool's timeout killed the first two
  attempts; resuming from the partial download with a freshly-repolled
  URL (each `GET /v3/tasks/{id}` call returns a new signed URL, even
  well after success) finished it.
- Imported as `Assets/Models/GrassBackpack.glb`, replacing
  `Assets/Prefabs/CrudeFiberBackpack.prefab`'s placeholder Cube.
  **Also fixed the ground-tunneling gap while rebuilding it** —
  `Rigidbody.collisionDetectionMode` was still left at the
  `AddComponent<Rigidbody>()` default (`Discrete`), same standing
  lesson as every other chunk/pickup built this session.
- Icon + previewIcon re-baked against the real model.

### v0.1.132-dev — Icon/model audit "quick wins": orphaned Backpack wired, 15 missing previewIcons batch-baked

First items off the model/icon audit's punch list from planning
tonight's session:

- **`BackpackItem.asset.worldPickupPrefab` wired to `Backpack.prefab`
  for the first time** — the real `CrudeLeatherBackpack.glb` model
  existed and was even already sitting in `TestScene.unity`, but the
  `ItemDefinition` never actually referenced the prefab (a dropped
  Backpack would have fallen back to the plain grey `DroppedItem`
  cube).
- **Found the same "standalone copy, not a real `PrefabInstance`" bug
  the Canteen had, on the scene's own Backpack this time** — its visual
  happened to already be correct (someone had manually given it the
  right child model), but future prefab edits would never have reached
  it. Replaced with a real `PrefabInstance` at the same position,
  matching the Canteen fix.
- **Batch-baked `previewIcon` for 15 items that already had a small
  `icon` but no bigger preview image**: Copper, Canteen, Copper Ore,
  Crude Fiber Backpack, Crude Fiber Belt, Crude Knife, Iron, Mining
  Face Shield, Gold Ore, Iron Ore, Small Rock, Platinum Ore, Rope,
  Stick, Silver Ore — all via `IconBaker -previewResolution 128`, no
  code changes needed, the tool already supported this. Deleted one
  orphaned old icon file (`CrudeFiberBackpackIcon.png`, superseded by
  `CrudeFiberBackpackItemIcon.png` once this item got a `previewIcon`
  too — the default output name is derived from the `.asset` filename,
  which didn't match the original bake's naming).

v0.1.129/130-dev's tint and emission fixes both did nothing visible,
even after boosting the emission value into clearly-HDR territory —
because neither was ever actually reaching the material. Diagnosed by
dumping the real model's shader properties directly rather than
guessing a third time: the Canteen's real model (like every other
Tripo3D/glTFast-imported model in the project) renders with `Shader
Graphs/glTF-pbrMetallicRoughness`, which has **none** of the Unity/URP
property names the code was checking (`_BaseColor`, `_Color`,
`_EmissionColor` — all `HasProperty() == false`). It exposes glTF-spec
names instead: `baseColorFactor` and `emissiveFactor`. Every
`SetColor("_BaseColor", ...)` / `SetColor("_EmissionColor", ...)` call
since v0.1.46-dev's original tint fix has been silently doing nothing
on this model — it happened to go unnoticed because the game had no
glTFast-shaded Canteen until v0.1.127-dev's model swap; the old
placeholder Cylinder used a hand-authored URP/Lit `Canteen.mat`, where
`_BaseColor` genuinely did work.

- `Canteen.SetTint()`/`GetTint()`/`SetEmission()` now check a list of
  candidate property names (`_BaseColor`/`_Color`/`baseColorFactor` for
  tint, `_EmissionColor`/`emissiveFactor` for emission) instead of
  assuming one shader family — works correctly against both the old
  URP/Lit convention (still used by any hand-authored `.mat`, e.g. a
  future `emptyMaterial`/`filledMaterial` override) and glTFast's
  Shader Graph.
- **Verified against the actual runtime code path this time**, not just
  compiled: instantiated the prefab in an Editor script, manually
  invoked `Awake()` via reflection (edit-mode instantiation doesn't
  call it automatically, unlike Play mode — a real gap in how the
  previous two attempts were "checked"), called `Fill()`, and confirmed
  `emissiveFactor` actually reads back `(0.5, 2.5, 5)` afterward.
  **Confirmed live by Ben** — filled reads as a clear blue-navy tint
  against empty's neutral dark brown/black, both in a side-by-side
  comparison. Resolved.

Ben's playtest of v0.1.129-dev's landing fix worked cleanly, but
"not sure that I can tell the canteen has a blue glow" — the first
emission value (`(0.1, 0.35, 0.6)`, all channels under 1.0) was too
dim to register against this scene's bright outdoor daylight. Pushed
`Canteen.FilledEmission` to `(0.5, 2.5, 5)` — genuinely HDR (well above
1.0), strong enough to read clearly even without a Bloom post-process
pass spreading it further. **Not yet re-confirmed live.**

### v0.1.129-dev — Canteen: fill status in the contents grid, a real blue glow when full, lands upright

Three small Canteen enhancements from continued playtesting:

- **Fill status now shows in a container's contents grid** (e.g. clipped
  to a Belt's attachment point), not just the main Equipment row —
  `InventoryScreen.DrawContainerContents` shows `Water 100/100`/`Empty`
  in the same spot a stackable item's `QTY: N` label sits, so a Canteen
  reads the same way no matter which UI location it's shown in.
- **Filled state now uses actual emission, not just a `_BaseColor`
  tint.** The real metal canteen model's own albedo is near-black, and
  a `_BaseColor` tint multiplies against that — barely visible. Added
  `Canteen.SetEmission()` (enables `_EMISSION`, sets `_EmissionColor`)
  alongside the existing tint, so filled genuinely glows blue on top
  regardless of how dark the underlying material is; empty clears
  emission back to black (off).
- **Dropped/scattered canteens no longer tip onto their side.** Root
  `Rigidbody.constraints` set to freeze X/Z rotation (still free to
  spin/settle around its own vertical Y axis) — a `BoxCollider`'s flat
  edges catching against the ground didn't perfectly match the
  cylindrical mesh, so it could land tipped over. Now it always lands
  upright, like a real canteen would.

### v0.1.128-dev — Crude Fiber Belt placed in the scene; found a Canteen that wasn't a real prefab instance

Ben: "let's spawn it in the game on start for now as well" (the
Canteen), then mid-turn: "let's also spawn a grass belt."

- **`Crude Fiber Belt` placed in `TestScene.unity`** at `(4, 0.3, 1.5)`,
  a real `CrudeFiberBelt.prefab` instance — first time it's existed as
  a world pickup rather than craft-only.
- **Found a pre-existing standalone "Canteen" GameObject at `(-1, 0.3,
  1.5)`** while trying to place a new one — turned out one already
  existed in the scene, but it was a fully independent embedded copy
  (its own `Body`/`Cap` Cylinder children), not a `PrefabInstance` of
  `Canteen.prefab`. This meant the v0.1.127-dev model swap never
  actually reached it — it was silently still showing the old
  two-piece grey placeholder despite the prefab itself being fixed.
  Replaced it with a real `PrefabInstance` at the same position (so it
  picks up the new model, and any future prefab edit automatically),
  matching how every other world pickup this session is placed.
  **Lesson:** a prefab swap only reaches instances that are actually
  linked as `PrefabInstance`s — a standalone embedded copy (same
  pattern as the old "Belt"/tier-2 "Fiber Belt" object) silently
  diverges and needs checking for independently.

### v0.1.127-dev — Canteen gets a real model (simple metal canteen)

Ben: "let's use the api to create the canteen. we can make a simple
metal canteen. standard rules apply for item creation and icons."

- Generated via Tripo3D's API (`"a simple metal canteen with a screw
  cap, isolated on a plain background, no person, no model, low-poly
  game asset"`, 20 credits) — clean on the first attempt, no 500s, no
  timeout, no unwanted extra geometry. Reads clearly as a cylindrical
  metal canteen with a dark threaded cap.
- Imported as `Assets/Models/MetalCanteen.glb`, replacing
  `Canteen.prefab`'s old two-piece placeholder (a scaled Cylinder
  "Body" + a smaller scaled Cylinder "Cap") with a single real-mesh
  child, uniformly scaled to match the old footprint's height (`0.42`).
  Root `Rigidbody`/`BoxCollider`/`Canteen` component untouched — both
  were already correctly built (`ContinuousDynamic` already set),
  collider resized to the newly-measured bounds.
- **`CanteenItem.asset.worldPickupPrefab` wired for the first time** —
  previously empty/unset entirely, meaning Canteen was craft-only and
  couldn't be dropped-and-repicked-up or spawned via the Admin tool.
  Now it can.
- `Canteen.cs`'s runtime empty/filled tinting (creates a material clone
  from whatever the model's own material is, no dedicated
  `emptyMaterial`/`filledMaterial` assets were ever set) continues to
  work unchanged against the new single-renderer model — simpler than
  before, since there's only one renderer to tint instead of two.
- Icon baked via `IconBaker`. Fifteenth item with an icon.

### v0.1.126-dev — Removed the procedural Tree and the unused Secret Message Wall

Planning cleanup, Ben's call: while auditing every model in the project
(real vs. procedural vs. placeholder, for tomorrow's session planning),
two long-standing pieces of dead/redundant weight got flagged and
removed outright rather than just noted:

- **Procedural "Tree" removed entirely.** Built in v0.1.58-dev (branching
  trunk mesh + ~100 primitive-sphere foliage clusters + 2 real
  `TreeBranch_PolyByGoogle.glb` branches), it was the game's only
  harvestable tree until **Big Tree by 3Donimus** (`BigTree_3Donimus.glb`,
  a real CC-BY model) was made choppable in v0.1.91-dev specifically to
  replace it — the procedural version had documented shape problems
  (pole-like trunk, floating foliage, washed-out bark) that Big Tree
  fixed outright. It had already been trimmed from 4 scene instances
  down to 1 in the 2026-08-06 declutter pass; now the last one, plus its
  prefab (`Assets/Prefabs/Tree.prefab`) and two dedicated assets
  (`TreeTrunkMesh.asset`, `TreeFoliage.mat`), are gone. **Kept**:
  `TreeBark.mat` and `Log.prefab` — both still genuinely shared with Big
  Tree's own chop-drop chain (Log → Plank), confirmed via guid
  cross-reference before deleting anything, not assumed. Big Tree is now
  the game's only tree.
- **`SecretMessageWall.cs` deleted.** A self-contained Easter-egg script
  (reveals hidden text to a Sunglasses-wearing player looking at a
  specific wall) that, per this session's model audit, was never
  actually placed anywhere in `TestScene.unity` — confirmed via guid
  search before deleting, only reference anywhere was a comment in
  `ResourceNode.cs` (updated to drop the dangling mention). Dead code
  with zero scene footprint; no gameplay lost.
- `TEST_FEATURE_PLAN.md` updated: removed checklist entries that only
  ever tested the procedural Tree or referenced Secret Wall re-adding
  instructions; Big Tree's own chopping entry rewritten to stand alone
  (previously phrased as "same as the real Tree above/differs by X").

### v0.1.125-dev — Backpack + Belt contents merged into one Inventory panel

Ben, after seeing v0.1.124-dev's fix render Backpack and Belt as two
separate bordered "Inventory" panels side by side: "let's add the belt
to the inventory panel with the backpack instead of its own panel."
Restructured `InventoryScreen.DrawContent()` so there's a single
"Inventory" panel again (matching the pre-v0.1.124-dev look) that now
stacks one preview+contents row per worn container vertically inside
itself, instead of one bordered panel per container. Still 0 panels
(nothing at all) when no container is worn, same as before either fix.

### v0.1.124-dev — Backpack + Belt worn together only ever showed one contents panel

Ben's playtest of v0.1.123-dev's anchor fix: equipped a Canteen onto
the Belt's attachment point and it "still does not show up on the
inventory panel when equipped." Turned out to be a *different*,
already-tracked bug (`BUGS_AND_ENHANCEMENTS.md`, flagged 2026-08-06,
confirmed via playtest 2026-08-07) that just hadn't been fixed yet: Ben
had a Backpack equipped (Back) at the same time as the Belt (Waist),
and `InventoryScreen`'s side "contents" panel only ever rendered
**one** worn container at a time — `GetWornContainer()` checked Back
before Waist and returned on the first match, so the Backpack's panel
always won and the Belt's (with the Canteen genuinely inside it) never
rendered at all.

- `GetWornContainer()` (singular) replaced with `GetWornContainers()`,
  returning every worn `IInventoryHolder` across Back and Waist instead
  of just the first.
- `DrawContent()` now loops over that list, rendering one
  preview+contents panel per worn container side by side, instead of
  at most one.
- `DrawBackPreview()`/`GetBackSlotPreviewIcon()` (Back-only) generalized
  to `DrawContainerPreview(Sprite)`/`GetSlotPreviewIcon(string
  slotName)`, since there can now be more than one preview box on
  screen at once.
- No items were ever actually lost by this bug — the Canteen was
  correctly inside `Belt.Inventory` the whole time, just not rendered
  anywhere visible. Worth confirming in the next playtest that this
  reads clearly (nothing to recover, just now visible) rather than
  alarming.

### v0.1.123-dev — Canteen/Belt carry anchors were never wired up

Ben's playtest of v0.1.122-dev: equipped a Canteen onto the newly-visible
Crude Fiber Belt's attachment point and it "doesn't show up anyplace."
Not related to the belt's new model — investigation found `PlayerCanteen`
(`leftHandSlotAnchor`, `rightHandSlotAnchor`, `beltSlotAnchor`) and
`PlayerBelt` (`carrySlot`) were all pointing at `{fileID: 0}` (unset) on
the Player in `TestScene.unity`. Each falls back to the player's own
root `transform` when unset, so both the Belt itself (worn on Waist) and
anything equipped to it (or to a hand) were being parented at the
player's exact pivot point instead of a sensible carry position —
functionally equipped (shows correctly in the Equipment/contents UI,
`Belt.Inventory` genuinely holds the Canteen) but effectively invisible
in the 3D world.

Found `HandAnchor` (`0.3, 1.3, 0.4`) and `BeltAnchor` (`0.25, 0.9, 0`)
already sitting as real child transforms on the Player, alongside the
already-correctly-wired `BackpackAnchor` — these look like they were
built for exactly this purpose and just never got connected. Wired:

- `PlayerCanteen.leftHandSlotAnchor` and `.rightHandSlotAnchor` → both
  to the single existing `HandAnchor` (only one hand-anchor object
  exists, not a separate one per hand).
- `PlayerCanteen.beltSlotAnchor` → `BeltAnchor`.
- `PlayerBelt.carrySlot` → `BeltAnchor` (the belt itself was never
  anchored either — worth a specific look at whether the belt's own
  worn position looks right now, not just the Canteen clipped to it).

**Not yet re-verified live** — worth confirming a worn Canteen (both
via a hand and via a Belt point) now actually appears at a sensible
position, and that the Belt itself looks right worn on the body.
**Scope note:** Sunglasses/Mining Face Shield/Nav Computer/Health
Monitor equip with no dedicated anchor field at all (always the
player's root, by their code's design, not a similar oversight) —
untouched here, out of scope unless one of those turns out to have the
same visibility problem.

### v0.1.122-dev — Crude Fiber Belt gets a real model (green woven grass)

Ben: "let's use the api to create a green, woven grass belt. let's
import it into the game." Turned out most of the plumbing already
existed — `CrudeFiberBeltItem`/`CrudeFiberBeltRecipe` (8 Fiber → 1 Crude
Fiber Belt, trains Sewing) were already built and already wired into
`PlayerCrafting.recipes` in `TestScene.unity`; only the visual was
missing (`CrudeFiberBelt.prefab` was a plain scaled grey Cube). This
was purely an art-pass swap, not new gameplay.

- Generated via Tripo3D's API (`"a green woven grass belt, plant fiber
  cordage wrapped in a coil, isolated on a plain background, no person,
  no model, low-poly game asset"`, 20 credits). **Hit the same
  20-minute server-side processing timeout the Crude Stone Knife's
  first real attempt hit** (`CHANGELOG.md` v0.1.115-dev) — the script's
  own polling gave up, but the task kept running server-side and
  actually succeeded a few minutes later; polled `GET /v3/tasks/{id}`
  directly to catch the `model_url` before its 5-minute expiry instead
  of re-spending credits on a second attempt.
- Came back as a closed woven ring/wreath shape rather than an open
  strap with overlapping ends — confirmed with Ben this was fine to use
  as-is rather than regenerating (matches the existing placeholder's
  own "not a final art pass" caveat in `TEST_FEATURE_PLAN.md` — this
  is a real improvement over a flat grey box regardless of exact
  strap shape).
- Imported as `Assets/Models/GrassBelt.glb`, swapped into
  `Assets/Prefabs/CrudeFiberBelt.prefab` (uniformly scaled to match the
  old placeholder's `0.5` max dimension, `BoxCollider` resized to the
  real measured bounds `0.50x0.12x0.50`).
- Icon baked via `IconBaker`. Fourteenth item with an icon.
- **No `THIRD_PARTY_CREDITS.md` entry needed** — that ledger only
  tracks CC-BY-licensed third-party models; Tripo3D API-generated
  content has its own no-attribution-required commercial license (see
  `Tools/Tripo3D/README.md`), same as the Backpack/Knife/Rope before it.
- **Note:** the separate pre-placed "Fiber Belt" (`BeltItem.asset`,
  tier 2, found near `(-2, 0.3, 1.5)`) is a different item on a
  different standalone prefab, not a `CrudeFiberBelt.prefab` instance —
  still a plain grey Cube placeholder, unaffected by this change.

### v0.1.121-dev — Silver/Gold/Platinum were missing their mid tier; also fixed a near-frictionless scatter bug

Ben's playtest of v0.1.120-dev: broke a Silver Ore Node and the pieces
"bounced out of the game" before he could pick them up, and separately
noted Silver/Gold/Platinum "missed the mid tier size that required
breaking" — v0.1.120-dev shipped them as a 2-tier structure (Ground
Node → final Ore item directly), reusing the pre-existing `SilverOre`/
`GoldOre`/`PlatinumOre` items and their `*OreChunk.prefab`s as-is. Ben
confirmed he wanted full parity with Copper/Iron's 3-tier structure
instead, so:

- **`SilverOreChunk`/`GoldOreChunk`/`PlatinumOreChunk.prefab` converted
  from the final `Pickup` tier into the punchable mid-tier
  `ResourceNode`** (mirrors `CopperOreChunk`/`IronOreChunk`) —
  bare-handed, 1 hit, breaks into 2 of a new final tier, `respawnDelay:
  0`.
- **New `SilverOrePiece`/`GoldOrePiece`/`PlatinumOrePiece.prefab`** —
  the actual final `Pickup` tier now, smaller than the mid-tier chunk
  (same Gold-smallest/Platinum-largest ordering), `Pickup.item`
  hardcoded to the existing `SilverOre`/`GoldOre`/`PlatinumOre` items
  (no new item assets needed — these three stay 2-item-tiers total,
  just with a punchable step added in front of the existing one, unlike
  Copper/Iron which needed a whole new refined-metal item).
  `SilverOre`/`GoldOre`/`PlatinumOre.worldPickupPrefab` re-pointed from
  the (now mid-tier, no-longer-a-Pickup) `OreChunk` prefabs to these.
- **Root cause of the scatter/bounce report, found while converting**:
  the original pre-existing `SilverOreChunk`/`GoldOreChunk`/
  `PlatinumOreChunk` prefabs had `Rigidbody.linearDamping: 0`,
  `angularDamping: 0.05` — nearly frictionless, unlike every other
  chunk in the project (`RockChunk`: 1.5/2, this session's `CopperChunk`:
  2/3). Same impulse force, near-zero drag — pieces kept rolling long
  after landing instead of settling near the break point. Set both the
  mid-tier and new final-tier Rigidbodies to `2`/`3` (matching
  `CopperChunk`'s already-proven values) while doing this conversion
  anyway. **Confirmed fixed by Ben's playtest** — scatter behavior is
  "working much better" now, pieces settle near the break point instead
  of rolling away.
- Icons for `SilverOre`/`GoldOre`/`PlatinumOre` re-baked against the new
  final-tier `*Piece.prefab`s (previously baked against the mid-tier
  visual, which is fine but the final pickup is what most often shows in
  inventory).

### v0.1.120-dev — Silver/Gold/Platinum Ore Nodes rebuilt, disguised via Mining Face Shield

Ben: "let's now do silver, gold and platinum. let's use the same lessons.
vary the size of the boulders for each type. make sure that can only see
them with the mining shield on. spawn one of each into the game, and
spawn the mining shield into the game as well."

These three used to exist (`TEST_FEATURE_PLAN.md` still had a whole
section for them at v0.1.60-dev/v0.1.61-dev, disguise mechanic and all)
but were removed from `TestScene.unity` in the 2026-08-06 startup-scene
trim along with the Mining Face Shield itself — they were scene-embedded
GameObjects, never saved as reusable prefabs, so trimming them left no
trace beyond that stale test-plan section. Rebuilt from scratch rather
than restored, applying every lesson from this session's Copper/Iron
work:

- **New disguised Ground Node per metal** (`Silver Ore Node` at
  `(6, 0.4, -4)`, `Gold Ore Node` at `(8, 0.4, -4)`, `Platinum Ore Node`
  at `(10, 0.4, -4)`) — `Rock_Quaternius.glb`, deliberately distinct
  sizes per Ben's request: Gold smallest (`0.70x0.65x0.72`, rarest/
  smallest veins), Silver medium (`1.00x0.95x1.05`), Platinum largest
  (`1.80x1.15x1.35`, most imposing). `ResourceNode.hiddenMaterial`/
  `revealedMaterial`/`hiddenChunkPrefab` populated for the first time
  anywhere in the project — `hiddenMaterial` is Rock_Quaternius' own
  default imported material (read straight off the model, the same
  "generic rock" look Boulder already uses undyed, not a hand-picked
  stand-in), `revealedMaterial` is each metal's existing `*OreRevealed.
  mat`, `hiddenChunkPrefab` is the existing plain `RockChunk.prefab` —
  matching the code comment's own suggestion ("should be a plain Small
  Rock chunk prefab") that had sat unused until now. Gated behind any of
  the 5 Pickaxe tiers, same as Copper/Iron.
- **Existing `SilverOreChunk`/`GoldOreChunk`/`PlatinumOreChunk` prefabs
  kept as the final pickup tier** (already correctly built — hardcoded
  `Pickup.item`, `Rigidbody.collisionDetectionMode` already
  `ContinuousDynamic` — no fixes needed there) but upgraded from a
  placeholder `Cube` to the real `Rock_Quaternius` mesh + the metal's
  `*OreRevealed.mat`, for visual consistency with every other ore tier
  shipped this session. Sizes varied to match the Ground Node ordering
  (Gold smallest, Platinum largest).
- Same UV-mismatch smearing bug hit on Copper/Iron applied here too —
  `SilverOreRevealed.mat`/`GoldOreRevealed.mat`/`PlatinumOreRevealed.mat`
  were all still at the 1x tiling that smears on `Rock_Quaternius`'
  UV layout; fixed to 6x proactively rather than waiting for a bug
  report, same fix already confirmed twice this session.
- **New `MiningFaceShieldPickup.prefab`** — the item was craft-only
  until now (`MiningFaceShieldItem.asset.worldPickupPrefab` was empty).
  No custom model exists for it yet, so it's a simple flattened-
  cylinder placeholder visor (same "primitive until it's worth a
  Tripo3D generation" convention the Stick/Knife started with), root
  `Rigidbody` set to `ContinuousDynamic` from the start. Wired to the
  item and placed in `TestScene.unity` at `(6, 0.5, -6)`.
- Icons baked for `SilverOre`, `GoldOre`, `PlatinumOre` (existing items
  that never had one) and the new `MiningFaceShieldItem`, via
  `IconBaker`. Tenth through thirteenth items with icons.
- **`TEST_FEATURE_PLAN.md` updated**: the stale v0.1.60/61-dev section
  describing the old pre-trim nodes replaced with current coordinates/
  sizes; the 2026-08-06 trim note no longer lists these as missing.

### v0.1.119-dev — Copper resized bigger, Iron gets the full pipeline too

Copper Ore Node made noticeably bigger (`0.71x0.65x0.80` →
`1.15x1.06x1.30`) per Ben's request — and since that also exposed a
collider that was never actually resized to match (still radius 0.5 in
a leftover 0.8-scaled parent, effective 0.4 world radius), fixed that
too while resizing rather than leaving it undersized again.

Then mirrored the entire Copper pipeline onto Iron, applying every
lesson from building Copper the first time instead of rediscovering
each one:

- **Iron Ore Node** swapped from its own plain Sphere to
  `Rock_Quaternius.glb` + `IronOre.mat`, sized deliberately **flatter
  and wider than Copper** (`1.50x0.85x1.60` vs Copper's
  `1.15x1.06x1.30` — shorter in Y, bigger footprint) per Ben's request,
  so the two ore types read as distinct silhouettes rather than
  recolored copies of the same shape. Applied the 6x texture tiling
  fix to `IronOre.mat` up front (same UV-mismatch cause as Copper,
  same fix) instead of shipping the 1x-tiling smear again — still
  rendered an isolated preview to actually confirm it before finalizing,
  rather than assuming the lesson transfers without checking. Collider
  properly resized to cover the new visual from the start this time
  (parent scale reset to 1 up front too, so there's no repeat of the
  Copper Ore Node's leftover-0.8-scale collider gap).
- **`IronOreChunk.prefab`** converted `Pickup` → punchable `ResourceNode`
  (mirrors `MediumRockChunk`/`CopperOreChunk`), visual swapped to the
  same mesh/material family, sized between the Ore Node and the new
  Iron chunk.
- **New `Iron` item + `IronChunk.prefab`** — built with both Copper
  lessons applied from the start instead of needing a follow-up fix:
  `Pickup.item` hardcoded directly (not left for `Configure()`, which
  `ResourceNode.SpawnChunk()` never calls), and `Rigidbody.
  collisionDetectionMode` set to `ContinuousDynamic` explicitly in the
  same edit that created it.
- Icons baked for `IronOre` and `Iron` via `IconBaker`. Eighth and
  ninth items with icons.
- **Flagged in `BUGS_AND_ENHANCEMENTS.md`:** `Iron` has no crafting
  recipe consuming it yet either, same situation as `Copper`/Rock/Wood.

### v0.1.118-dev — Copper chunks were spawning permanently un-pickupable

Ben's playtest, walking the full break chain (Ore Node → Copper Ore
chunk → Copper): the smallest tier scattered correctly but couldn't be
picked up at all. This is the exact bug already flagged in
`BUGS_AND_ENHANCEMENTS.md` from the Stick-bonus-chunk incident earlier
this session — `CopperChunk.prefab` was built copying
`StickPickup.prefab`'s "leave `item` empty, `Pickup.Configure()` fills
it in later" convention, but that convention only works for prefabs
reached via `PlayerDropping.SpawnPickup()` (which calls `Configure()`).
`ResourceNode.SpawnChunk()` — the actual path that spawns
`CopperChunk` when a Copper Ore chunk breaks — never calls
`Configure()` at all, so the chunk's `item` stayed null and
`Pickup.Complete()` silently no-oped. Fixed by hardcoding `item`
directly on the prefab instead, the same way `RockChunk.prefab`
already does — works correctly in both the drop-from-inventory path
(`Configure()` just harmlessly re-sets the same value) and the
break-into-chunks path (now has a real value to begin with). The
underlying systemic gap (`SpawnChunk()` still never calls `Configure()`)
remains open for `StickPickup`'s existing use as a `bonusChunkPrefab`.

### v0.1.117-dev — Copper gets the Boulder-family treatment: real shape, two tiers, icons

Ben's idea: reuse `Rock_Quaternius.glb` (Boulder's mesh) with the
existing copper-speckled `CopperOre.mat` for a real Copper Ore shape,
and mirror the exact Boulder → punchable chunk → refined-material
tier structure the rock family got in v0.1.87/90-dev.

- **Copper Ore Node** (was a plain built-in Sphere since it was first
  added) now uses `Rock_Quaternius.glb`, sized/grounded to match the
  old sphere's exact footprint (measure-old-bounds-first discipline,
  same as every other visual swap this project).
- **Real bug caught before it shipped, not after:** rendered a quick
  isolated preview (reusing the icon-baking camera/lighting technique,
  in a fresh throwaway scene) before committing to anything, and the
  reused texture looked wrong on the new mesh — its small repeating
  copper-fleck pattern stretched into one big diagonal smear, because
  `CopperOreTexture.png` was tuned for a sphere's simple UV unwrap and
  `Rock_Quaternius` has a completely different UV layout from its
  Quaternius source. Fixed by bumping `CopperOre.mat`'s `_BaseMap`/
  `_MainTex` tiling from 1x1 to 6x6 — confirmed by re-rendering the
  same preview before touching the real scene object. (First preview
  attempt rendered the wrong thing entirely — a wide gameplay view
  instead of an isolated object — because it opened the live
  `TestScene` directly; switched to a fresh empty scene, the same
  technique `IconBaker` already uses, and that fixed it.)
- **`CopperOreChunk.prefab`** converted from a `Pickup` (plain Capsule
  primitve) into a punchable `ResourceNode` — same conversion
  `MediumRockChunk` got in v0.1.90-dev. No longer directly pickupable;
  punching it (1 hit) breaks it into 2 of a brand-new **Copper** item.
  Visual also swapped to `Rock_Quaternius` + `CopperOre.mat`, sized
  distinctly from both the Ore Node above it and the Copper chunk
  below it.
- **New `Copper` item + `CopperChunk.prefab`** — didn't exist before.
  Same mesh/material family, smallest tier's proportions. Rigidbody
  explicitly set to `ContinuousDynamic` collision detection in the same
  edit that created it — the exact mistake that broke Rope's drop
  earlier this session, this time caught before it shipped by applying
  [[project_gridless_ground_tunneling]] proactively instead of after a
  bug report.
- Icons baked for both `CopperOre` and `Copper` via `IconBaker` — one
  command each, no new script needed. Sixth and seventh items with
  icons.
- **Flagged in `BUGS_AND_ENHANCEMENTS.md`:** `Copper` has no crafting
  recipe consuming it yet, same situation as Rock and Wood — built
  ahead of the crafting need per Ben's call, not an oversight.

### v0.1.116-dev — Rope gets a real visual and an icon, first from scratch

`Rope.asset` never had a `worldPickupPrefab` at all — no placeholder
to swap, a genuinely new visual. Generated cleanly on the first
attempt this time (no 500s, no timeout, no unwanted extra parts like
the knife's handle) — `"a photorealistic small coil of rope, hemp
fiber texture, tightly wound, isolated on a plain background"`,
20 credits, reads exactly as asked: a tidy bundled coil.

- **New `Assets/Prefabs/RopeCoilPickup.prefab`**, built from scratch
  (root `BoxCollider` + `Rigidbody` + `Pickup` with `item` left unset,
  same "configured at drop time via `PlayerDropping.SpawnPickup()`"
  convention as `StickPickup.prefab`/`RockKnifePickup.prefab`) rather
  than modifying an existing one. Model uniformly scaled to a 0.28
  max-dimension target (no old footprint to match against, since there
  was never a placeholder — picked to sit in the same size range as
  other small hand-carried pickups like Small Rock). Wired directly
  onto `Rope.asset.worldPickupPrefab`.
- Icon baked via `IconBaker` — reads clearly as a small tan coiled
  bundle. Fifth item with an icon.

### v0.1.115-dev — Crude Knife gets a real visual and an icon

The Tripo3D API finally cooperated — see `Tools/Tripo3D/README.md` for
the full 4-failed-500s-then-timeout-then-success saga. Real model
imported and wired in this version:

- `Assets/Models/CrudeStoneKnife.glb` swapped in for
  `RockKnifePickup.prefab`'s old placeholder Capsule primitive (the
  Crude Knife's world pickup, referenced by `CrudeKnife.asset`) —
  sized to match the old placeholder's exact footprint (`0.08 x 0.05 x
  0.35`), collider/Rigidbody/Pickup untouched. Measured old bounds and
  collider size before removing anything, same discipline as every
  other visual swap this project.
- Icon baked via `IconBaker` (`-modelPath
  "Assets/Prefabs/RockKnifePickup.prefab" -itemAssetPath
  "Assets/Data/CrudeKnife.asset"`) — reads as a small dark blade at a
  diagonal, same treatment as Stick and Small Rock. Fourth item with
  an icon.
- **Known limitation, accepted as-is (Ben's call):** the model has a
  full handle/crossguard despite every prompt attempt explicitly
  saying "no handle" — Tripo3D seems to default "knife" toward a
  hilted shape regardless. Doesn't match `CrudeKnifeRecipe.asset`'s
  actual ingredient (1 Small Rock, no wood) implying a bare blade, but
  visually reads well as a crude knapped weapon either way.

### v0.1.114-dev — Stick gets an icon, first real use of IconBaker

`IconBaker.Bake -modelPath "Assets/Prefabs/StickPickup.prefab"
-itemAssetPath "Assets/Data/Stick.asset"` — one command, no new script.
Baked cleanly on the first try (32x32, reads as a small branch at a
diagonal). Third item with an icon overall, first one built entirely
through the new tool rather than a bespoke script.

### v0.1.113-dev — IconBaker: permanent tool for baking item icons

Every icon so far (Backpack, Crude Fiber Backpack, Small Rock) was a
bespoke throwaway `Assets/Editor/*.cs` script, rewritten from scratch
each time. Ben's call: consolidate it into one reusable tool so adding
an icon for a new model going forward is a single command, not a new
script.

- **New permanent `Assets/Editor/IconBaker.cs`** (not a throwaway —
  stays in the project). Batch-mode usage:
  ```
  Unity.exe -batchmode -quit -projectPath . -executeMethod IconBaker.Bake ^
    -modelPath "Assets/Prefabs/X.prefab" -itemAssetPath "Assets/Data/X.asset"
  ```
  Optional `-resolution` (default 32), `-previewResolution` (default 0 —
  skipped unless set; also bakes a bigger image and wires it to
  `ItemDefinition.previewIcon`), `-outputName` (defaults to the item
  asset's own filename + "Icon").
- Instantiates the model in a throwaway scene, frames it with an
  orthographic camera at a fixed 3/4-from-above angle sized to its
  measured bounds, renders to a transparent PNG, imports it as a
  Sprite, wires it onto the `ItemDefinition`. Same technique as every
  icon baked by hand so far, just parameterized.
- **Bakes in every trap discovered the hard way this whole icon
  effort:** aborts loudly if launched with `-nographics` (disables
  `RenderTexture` entirely — silent failure otherwise) instead of
  producing a blank icon; explicitly sets `spriteImportMode = Single`
  (default is Multiple, which produces no actual `Sprite` object at
  all without hand-sliced sub-sprites — `LoadAssetAtPath<Sprite>`
  silently returns null otherwise); reloads the `ItemDefinition`
  reference *after* baking rather than trusting one held across the
  `AssetDatabase.ImportAsset`/`SaveAndReimport` calls, which can
  invalidate it (hit this immediately on the tool's own first test run
  — `ArgumentException: Object at index 0 is null`).
- Verified end-to-end by re-baking Small Rock's icon through the new
  tool instead of its original bespoke script — output was pixel-
  equivalent (same model, same resolution); deleted the now-duplicate
  original file (`SmallRockIcon.png`) and kept the tool's own naming
  convention (`RockIcon.png`, matching `Rock.asset`'s filename).

### v0.1.112-dev — Hover tooltip shows an icon-only slot's item name

Contents-grid slots with an icon show nothing but the picture now (no
text at all) — Ben's ask: hovering the icon should show the item's
name, since it's otherwise not visible anywhere in that slot.

- Unity's **runtime** IMGUI (unlike the Editor's) never draws
  `GUI.tooltip` on its own — setting a `GUIContent`'s tooltip just
  makes the string available, nothing renders it without doing so
  explicitly. New `InventoryScreen.DrawTooltip()` checks `GUI.tooltip`
  each frame and draws a small floating panel-backed label near the
  cursor when it's non-empty.
- Drawn from `DrawPopups()` (called by `PlayerMenuScreen` after the
  scroll view/`BeginArea` end), not inside `DrawContent()`'s scroll
  view — same reasoning as the other popups there: needs to sit on
  top of everything, unclipped by the scroll rect, positioned in real
  screen space via `Event.current.mousePosition`.
- Scoped to icon-bearing contents-grid slots specifically — items with
  no icon still show their name as visible text in the slot itself,
  so a tooltip would just be redundant there.

### v0.1.111-dev — Empty contents grid slots were invisible, fixed

Ben's report right after v0.1.110-dev: removing the "Empty" text left
nothing visible at all where those slots used to be — no way to tell
how many total slots a container has when some are empty. Root cause:
`GUI.skin.box`'s default runtime appearance has too little contrast
against `DebugGUI.Panel`'s dark background to read as a box on its
own — it only looked fine before because the "Empty"/item text (and
its own default label coloring) was doing the actual visible work,
not the box style.

- New `DebugGUI.Slot` — an explicit solid mid-gray background (same
  `SolidTexture` technique `DrawPanel`/`Panel` already use, not a
  default skin style) — guarantees a slot reads as a distinct box
  regardless of what's inside it. Both empty and occupied contents-grid
  slots now use this instead of `GUI.skin.box`.

### v0.1.110-dev — Contents grid empty slots drop the "Empty" text

Ben confirmed the v0.1.109-dev icon fix worked (Small Rock renders
correctly with QTY: 9 beneath it), then pointed out empty slots in
this same grid still said "Empty" in text — wanted a plain gray box
instead, matching how the occupied slots read now that they're
icon-driven. Scoped to the contents grid specifically; the equipment
slot list's own "Empty" labels (Head/Face/Neck/...) are unchanged.

### v0.1.109-dev — Contents grid icon overlay, replacing broken GUIContent combo

Ben caught it immediately: the Small Rock icon didn't render in the
contents grid at all — the slot just showed truncated text ("ill Rock
x9"). Root cause: `ItemContent()`'s `GUIContent`(icon+text) combo,
which works fine in wider rows, silently breaks down at this grid's
tight 70x30 box — no room for a 32x32 icon and a full name/count
string together, and Unity dropped the icon rather than the text.

- Contents grid slots with an icon now draw it as a **separate overlay
  on top of a plain box** (`GUI.DrawTexture` after `GUILayout.Button`),
  the same technique the Back preview box already uses successfully —
  sidesteps `GUIContent` sizing entirely instead of fighting it again.
  Items with no icon still fall back to the old text-in-button
  rendering, unchanged.
- Both empty and occupied slots now use **`GUI.skin.box`** as their
  visual style (occupied ones via `GUILayout.Button(..., GUI.skin.box,
  ...)`, still fully clickable) — Ben's ask for the two to read as the
  same "gray filled box," not visually different states.
- `SubBoxHeight` bumped from 30 to 44 — it was literally shorter than
  the 32x32 icon itself before any padding, let alone room to fit one
  comfortably.

### v0.1.108-dev — "QTY: N" label under each backpack/storage contents slot

Ben's call, scoped specifically to the contents grid (`DrawContainerContents`)
— not the main inventory list, equipment slots, or move popup, which
all keep their current icon+text-beside-it look.

- Each occupied slot is now a small vertical group: the existing
  icon+name button on top, a new `"QTY: {count}"` label directly below
  it. Blank (not "QTY: 1") for a non-stackable item (`maxStack <= 1`,
  e.g. a Backpack) — still drawn as an empty label either way so every
  column in the row reserves the same height, keeping the grid aligned.
  Empty slots get the same blank label treatment for the same reason.

### v0.1.107-dev — Small Rock gets an icon (second item to have one)

Baked from the actual in-game model (`RockChunk.prefab`, same asset
Rock Node's chunks already use — a pale rock/pebble silhouette),
32x32, same offscreen-camera technique as the Backpack icons. Wired to
`Rock.asset` (the `Small Rock` item — yes, the filename and the item
name don't match, a pre-existing quirk, not something this change
touches). No `previewIcon` this time — Small Rock has no dedicated
big-preview UI the way a worn Backpack does, so only the small inline
icon was worth baking. Shows up automatically everywhere `ItemContent()`
already renders an item's icon (main inventory list, equipment slots,
container grids, move popup) — no `InventoryScreen.cs` changes needed,
that plumbing was already generic from the Backpack work.

### v0.1.106-dev — "Equipment"/"Inventory" relabeled onto their own panels

Ben's call: "Equipment" now labels the slot list panel specifically
(drawn inside it, not above the whole row), and "Inventory" moved down
from its old spot above the main inventory list to label the
preview+contents panel instead — the main inventory list above now has
no header at all, per Ben's choice when asked what should happen to
that spot once the text moved off it.

### v0.1.105-dev — The two panels sit side by side now, not stacked

Final layout pass on this back-and-forth: the slot list panel and the
preview-icon+contents panel were two separate `GUILayout.BeginHorizontal`
rows, so they stacked vertically. Combined them into one row — slot
list panel first (left), preview+contents panel second (right, only
when something's worn on Back/Waist) — matching Ben's original
red-box/green-box mockup. Dropped the `GUILayoutUtility`-measured
header-alignment math from v0.1.102-dev along with it; it doesn't
apply to a plain side-by-side row.

### v0.1.104-dev — Panel style was covering the whole screen, fixed

The `DebugGUI.Panel` style added in v0.1.103-dev rendered as one giant
black rectangle spanning nearly the entire screen instead of framing
just the equipment slot list and the icon+contents pair separately —
both sections merged into one indistinguishable black expanse with no
visible gap between them. Root cause: `new GUIStyle()` defaults to
`stretchWidth`/`stretchHeight = true`, so `GUILayout.BeginVertical`/
`BeginHorizontal` using it expand to fill all available space in their
parent row rather than shrink-wrapping to their actual content —
explicitly set both to `false`. Also added a `GUILayout.Space(10)`
between the two panels, which had no gap between them at all before.

### v0.1.103-dev — Equipment slot list and icon+contents get real panel backgrounds

Ben's mockup: both sections should read as distinct bordered panels
sitting on top of the 3D game view, not floating content directly on
top of it with no visual boundary.

- New `DebugGUI.Panel` — a `GUIStyle` wrapping the same background
  `DrawPanel()` already draws (matches the rest of the game's panel
  look), but usable directly with `GUILayout.BeginVertical`/
  `BeginHorizontal` so it auto-sizes to whatever's inside it instead of
  needing a pre-computed `Rect`.
- The equipment slot list (`Head`/`Face`/.../`Back`/...) and the
  Back-preview-icon-plus-contents-grid pair each now draw inside their
  own `DebugGUI.Panel`-styled group — two visibly separate boxed
  sections instead of everything floating loose.

### v0.1.102-dev — Icon+contents aligned under "Equipment" by measurement, not guesswork

Ben marked up a screenshot: equipment slot list contained cleanly on
its own (left column), icon+contents pair fully separate, positioned
under "Equipment" in the open area to the right — not bleeding into
or overlapping the slot list column the way the FlexibleSpace-centered
version from v0.1.101-dev did.

Root cause of that: centering the icon+contents *group* via symmetric
`FlexibleSpace` shifts the icon left of the group's own midpoint
(since the contents grid trailing after it is much wider than the
leading gap) — it can never land the icon under a header centered on
the full row width, only under the *group's* center, which isn't the
same point. Rather than fight that math, `DrawContent()` now measures
`GUILayoutUtility.GetLastRect()` right after drawing the header and
uses its actual real center to place the icon+contents row via
`GUILayout.Space()` — matching real numbers instead of assumptions
about how the surrounding layout distributes width.

### v0.1.101-dev — Preview box AND contents grid together, under "Equipment"

The two requirements from v0.1.99 and v0.1.100 actually both needed to
hold at once: icon under the header, contents grid right beside the
icon — not one or the other. Fixed by finding the worn container
*before* the slot list draws instead of after: new
`GetWornContainer()` does the same Back/Waist lookup
`DrawEquipmentSection()` used to do as a side effect of drawing (and
returned once it finished), so `DrawContent()` can now put the
icon+contents row directly under "Equipment" — centered as one group
via `FlexibleSpace` on both sides — with the slot list drawn
separately below. `DrawEquipmentSection()` is `void` now; nothing
needed its return value anymore once the lookup moved out.

### v0.1.100-dev — Back preview box moved under "Equipment" (final spot)

Back to its own centered row, right under the "Equipment" header — no
longer tied to the slot list or the contents grid's position. It's
independent of `wornContainer` now too: `DrawBackPreview()` checks the
Back slot's icon directly and draws nothing at all (not even a blank
frame) when there's nothing to preview, rather than needing
`DrawEquipmentSection()` to run first.

### v0.1.99-dev — Back preview box moved beside the backpack's own contents

Misread the previous request: "inventory slots" meant the worn
container's own storage grid ("Backpack contents"), not the player's
equipment slot list (Head/Face/Neck/...). Moved `DrawBackPreview()` to
sit between the equipment slot list and `DrawContainerContents()`, so
the picture is paired with what's actually inside it. As a side effect
this also fixes a leftover oddity from v0.1.98-dev: the box no longer
shows (blank frame) when nothing's worn on Back — it's now only drawn
inside the same `wornContainer != null` block as the contents grid it
sits beside, so it only appears when there's actually a contents grid
for it to sit next to.

### v0.1.98-dev — Back preview box moved beside the slot list, not above it

Ben's call after seeing v0.1.97-dev's centered-but-stacked layout: the
preview box and the Equipment slot list (Head/Face/Neck/.../Back/...)
were still two separate rows, box on top, slots below starting back at
the left edge. Restructured `DrawContent()` so the preview box is the
leftmost element of the *same* horizontal row as the slot list, with
the "Backpack contents" side column still following after — one row:
[preview box] [slot list] [container contents, if worn]. Removed the
`FlexibleSpace()` self-centering `DrawBackPreview()` grew in
v0.1.97-dev, since the box's position now comes from where it sits in
that row, not from centering itself in a lone one.

### v0.1.97-dev — Back preview box wasn't centered under "Equipment"

Ben compared two screenshots — the preview box was hugging the left
edge, sitting right above the "Head" row, instead of centered under
the "Equipment" header the way it should read as belonging to it.
Root cause: `DebugGUI.Header`'s `TextAnchor.MiddleCenter` centers the
*text* within a label that expands to fill its row, but `GUILayout.Box`
doesn't get the same treatment for a fixed pixel size — it just sits
at the left edge of whatever space it's given. Wrapped it in
`GUILayout.BeginHorizontal()` + `FlexibleSpace()` on both sides to
actually center the box control itself, matching where the header
text sits.

### v0.1.96-dev — Icon-only in every equip slot, crisp preview icon

Two more follow-ups once the preview box and hand-slot icon were both
visible: Ben pointed out a hand-held Backpack still said "Backpack"
next to its icon (the icon-only treatment from v0.1.95-dev only
applied to worn Back/Waist containers, not hand slots), and the new
96x96 preview box looked visibly blurry with no visible border.

- **Icon-only now applies to every equipment-section slot**, not just
  worn Back/Waist containers — any item with an icon shows icon-only
  there (hand, back, waist, wherever), falling back to the old text
  only for items with no icon. A hand-held Backpack now shows just its
  picture, no redundant "Backpack" label next to it.
- **New `ItemDefinition.previewIcon` field** — a separately-baked,
  higher-resolution image for large-preview UI, distinct from `icon`
  (kept small, ~32x32, for inline rows). Root cause of the blur:
  `GUIContent` images render at native pixel size with no fit-to-
  control scaling, so the 96x96 preview box was stretching a 32x32
  source 3x — genuinely blurry, not a bug in the box itself. Baked
  `BackpackPreviewIcon.png` at 128x128 directly from `Backpack.prefab`
  (not upscaled from the small one) and wired it to
  `BackpackItem.asset.previewIcon`; `DrawBackPreview()` now prefers it,
  falling back to `icon` for items that never get a dedicated one.
- **Preview box border was invisible** — it used `DebugGUI.DrawPanel`
  (a near-black overlay meant to sit on an already-dark full-screen
  panel), which blended into the game view behind it with no visible
  edge. Switched to a plain `GUILayout.Box`, the same visibly-bordered
  style every other slot box on this screen already uses.

### v0.1.95-dev — Icon polish: drop "Equipped" text, add a Back preview box

Two follow-up requests once the Backpack icon was actually visible.

- **A worn Back/Waist container's row now shows icon-only, no "Equipped"
  text**, when the item has an icon — falls back to the "Equipped" text
  it always showed if the item has none (Belt, for now), so nothing
  regresses for items that never get an icon.
- **New fixed 96x96 framed preview box** right under the "Equipment"
  header (`DrawBackPreview()`/`GetBackSlotIcon()` in
  `InventoryScreen.cs`) — shows a bigger version of whatever's worn on
  Back, blank (just the dark frame) when nothing's equipped there or
  the equipped item has no icon. Scoped to Back only, not a general
  "last clicked item" viewer — Ben's call between the two options.

### v0.1.94-dev — Icon baked for the wrong Backpack, fixed

Ben still saw no icon after v0.1.93-dev, even in the fixed all-render-
sites version. Root cause: **there are two entirely separate Backpack
items** — `CrudeFiberBackpackItem.asset` (the Sewing-craftable one,
`CrudeFiberBackpack.prefab`/`CrudeFiberBackpack.glb`) which is what I
baked an icon for, and `BackpackItem.asset` (the plain pre-placed
"Backpack", tier Normal, visual is `Backpack.prefab` wrapping
`CrudeLeatherBackpack.glb`) — a completely different item and model.
Ben's playtest had the **pre-placed** one equipped, not the crafted
one, so the icon I built was simply never going to show up regardless
of how many render sites it was wired into. Should have checked which
item was actually equipped before picking one to bake — didn't.

- Baked a new icon from `Backpack.prefab` (32x32, same offscreen-camera
  technique as before) and wired it to `BackpackItem.asset.icon`. This
  one has visible straps and reads more clearly as a backpack than the
  Fiber one's simpler low-poly shape.
- `CrudeFiberBackpackItem.asset`'s icon from v0.1.93-dev is left as-is
  — it's still correctly wired to its own real item, just wasn't the
  one on screen. Not wasted, just not what was being tested.

### v0.1.93-dev — Item icons: first one, on the Crude Fiber Backpack

Ben's request: show a real 2D image in the inventory instead of the
Crude Fiber Backpack always reading as plain text. First use of a new
`ItemDefinition.icon` field (`Sprite`, null by default) — every other
item stays text-only until it gets one, no behavior change for them.

- **Icon baked from the actual 3D model**, not hand-drawn: a batch-mode
  Editor script instantiates `CrudeFiberBackpack.prefab` in a throwaway
  scene, frames it with an orthographic camera at a 3/4 angle sized to
  its measured bounds, renders to a transparent 256x256
  `RenderTexture`, and saves the result as
  `Assets/Textures/Icons/CrudeFiberBackpackIcon.png`.
- **Two real gotchas hit along the way**, worth remembering for the next
  icon: (1) `-nographics` disables the graphics device entirely, so
  `RenderTexture.Create` silently fails in batch mode — dropped the
  flag for this one script (batch mode still shows no window without
  it, it just also initializes the GPU device); (2) the importer
  defaults a fresh PNG's `spriteMode` to Multiple, which needs
  hand-sliced sub-sprites before Unity will produce an actual `Sprite`
  object at all — `AssetDatabase.LoadAssetAtPath<Sprite>` silently
  returned null until `TextureImporter.spriteImportMode` was set to
  `Single` explicitly.
- Every place `InventoryScreen` renders an item — the main Inventory
  list, equipment slot boxes (including a worn Backpack's "Equipped"
  box, not just its unequipped stack), the Backpack/StorageBox contents
  grid, and the move popup's header — now goes through a shared
  `ItemContent()` helper (`GUIContent` with the item's icon texture set
  when present) instead of a plain string, so an icon shows up
  everywhere an item does, not just one list. Text label stays either
  way — icon is additive, never icon-only.
- **Regression caught by Ben immediately after the first version
  shipped:** the backpack was equipped, not sitting in the main
  Inventory list, so the icon (only wired into that one list at first)
  never showed — fixed by generalizing to every render site above.
  Separately, the icon was originally baked at 256x256; `GUIContent`'s
  image renders at the texture's **native pixel size** in a plain
  `GUIStyle` (no auto-fit-to-control), which would have blown out every
  40px-tall row. Re-baked at 32x32 — the actual intended display size —
  once this was caught before it ever reached Ben.

### v0.1.92-dev — Fixed Big Tree's collider floating above the tree

Ben reported still being unable to chop Big Tree right after v0.1.91-dev
shipped it. Root cause: a math error in the `CapsuleCollider` placement
— the center-Y formula had a spurious extra `+ height * 0.5f` term that
shifted the collider's world-space position up by half its own height
(~3.6 units) from where it should've been. Punches were landing on
empty air well above the visible trunk/canopy instead of the actual
mesh. Confirmed and fixed by comparing the collider's computed
world-space Y range directly against the tree's measured renderer
bounds — they now match exactly (`[-0.15, 7.04]` both). No change to
`ChoppableTree`'s config, only the collider's `center`.

### v0.1.91-dev — Big Tree by 3Donimus is now choppable

Ben's request: it's been sitting as a comparison-only decoration since
v0.1.86-dev, never interactive — make it work like the real Tree.

- Added `ChoppableTree` (the same component the procedural Tree uses,
  no new code needed) directly onto the Big Tree scene object, plus a
  `CapsuleCollider` sized from its actual measured bounds (it had no
  collider at all before — glTFast doesn't auto-generate one on import,
  same gotcha already known from the Boulder/Rock Node swaps). Config
  mirrors the real Tree exactly: 3 hits with an Axe (any of the 5
  tiers), drops 3 Logs, 0.5 Gathering skill gain per hit, 180s regrow.
- **Known simplification:** Big Tree has no "Stump" child the way the
  procedural Tree does, and `ChoppableTree` gracefully degrades to
  "just disappear, then reappear" when there's no Stump to swap to —
  so chopping it fully vanishes it for the regrow window rather than
  leaving a visible stump. Fine as a first pass; a real stump visual
  would be a follow-up if Ben wants one.
- Since Big Tree is now an actively-used gameplay object instead of a
  comparison prop, its CC-BY attribution ("Big Tree by 3Donimus [CC-BY]
  via Poly Pizza") now belongs in the Credits tab too, per the standing
  rule every other shipping asset already follows — added to
  `GameMenuScreen.DrawCreditsTab()` and `THIRD_PARTY_CREDITS.md` updated
  to match.

### v0.1.90-dev — Boulder's Rock chunk is now a punchable node, not a pickup

Ben's call, in response to playtesting the v0.1.89-dev chunk visual
fix: breaking the Boulder shouldn't just hand you a "Rock" item — the
Rock chunk should itself be a small resource you punch open into Small
Rock, matching Rock Node's own break-it-down pattern rather than acting
like a loose ground pickup (Stick, Berry, etc.).

- **`MediumRockChunk.prefab`** (the chunk Boulder spawns) had its
  `Pickup` component replaced with a `ResourceNode` — same component
  Rock Node and Boulder themselves use, implementing `IPunchable`. It's
  no longer directly pickupable at all: punching it (bare-handed, 1
  hit) breaks it into 2 **Small Rock** via `RockChunk.prefab` (the same
  chunk Rock Node already spawns), scattering with the same
  `scatterForce` (1.2) and `Gathering` skill gain (0.5) every other
  node uses. Its `Rigidbody`/`SphereCollider` from the v0.1.89-dev
  visual fix are untouched, so it still launches and settles physically
  when Boulder breaks — it just can't be picked up once it lands
  anymore, only punched. `respawnDelay` set to 0 (destroyed outright
  once broken) — it's a one-off spawn, not a fixed environmental node,
  same convention as a Log dropped by a chopped Tree.
- **The "Rock" item (`MediumRock.asset`) is now orphaned** as a direct
  side effect — nothing spawns it into inventory anymore, and nothing
  ever consumed it via a recipe either. Flagged in
  `BUGS_AND_ENHANCEMENTS.md` rather than deleted outright, since
  keeping vs. repurposing it is a content decision, not an
  implementation detail.

### v0.1.89-dev — Boulder's real chunk fixed, Credits image overflow fixed

Two follow-ups from playtesting the v0.1.88-dev fixes.

- **`MediumRockChunk.prefab`** (the "Rock" item's chunk — what actually
  spawns when Boulder breaks, distinct from `RockChunk.prefab` which
  only feeds Rock Node's Small Rock) was still the old procedural
  4-pebble sphere cluster from before the Stone model swap — missed
  because the Boulder work in v0.1.87-dev only touched Boulder's own
  root visual, not the separate chunk prefab it spawns. Confirmed via
  Ben's screenshot after successfully breaking the Boulder (proving the
  proximity fix in v0.1.88-dev worked) — the scattered chunks were
  plain fused grey spheres. Swapped in `Stone_PolyByGoogle.glb` at
  `(0.5, 0.42, 0.48)`, non-uniform and distinctly proportioned from
  `RockChunk.prefab`'s Small Rock target (`0.32, 0.22, 0.28`) so it
  reads as the tier above rather than a scaled duplicate. Measured the
  old pebble cluster's bounds before removing it, `SphereCollider`
  (radius 0.35) and `Pickup`/`Rigidbody` config left untouched —
  same discipline as every other stone swap this week.
- **Credits page image could overflow the tab vertically** — it was
  only bounded by 90% of screen width, so a wide image at a tall aspect
  ratio (like `tekim_trex.png`) could render taller than the visible
  menu area. GUILayout doesn't clip or auto-scroll, so anything below
  the image (the name line, Third-Party Assets list, Close button) just
  got pushed off-screen with no way back — Ben reported this as "no
  scroll bar" and the image looking uncentered (it was centered; the
  cut-off bottom/right just made it look wrong). Fixed by also capping
  height to 50% of screen height and shrinking width to match if the
  height cap binds first — the image can no longer push anything else
  off-screen regardless of window size or the image's own aspect ratio.

### v0.1.88-dev — Credits page polish, Boulder/Rock Node/Big Tree separated

Playtest catches after v0.1.86/87-dev shipped their visuals.

- **Credits page**: the attribution image now sizes itself to 90% of
  screen width with height derived from the actual source texture's own
  aspect ratio at draw time (`Screen.width * 0.9`, then
  `height/width` from `creditsImage`) instead of a hardcoded ratio, so it
  stays correct if the image is ever swapped. "Tekim" and "the T-Rex"
  combined onto a single centered line, "Tekim & The T-Rex".
- **Boulder, Rock Node, and Big Tree by 3Donimus were crowding each
  other at game start** — Ben's report ("they spawn on top of each
  other") pointed at the real gameplay Tree, but that object was already
  at `(-8, 0, -6)`, nowhere near the cluster. The actual culprit was
  **Big Tree by 3Donimus** (the CC-BY comparison-only decorative prop,
  never wired to any gameplay script), sitting at `(-3, 3.99, 3)` at
  3.02x scale — almost on top of both Boulder `(-4, 0.6, 4)` and Rock
  Node `(-2, 0.35, 3)`, and large enough at that scale to loom over both
  in Ben's screenshot. Moved Big Tree out to `(10, 3.99, 10)`, clear of
  everything. Separately, Boulder and Rock Node's own visuals grew
  noticeably larger in v0.1.87-dev/86-dev (real meshes replacing a plain
  sphere and hand-tuned procedural shape), so their old ~2.24-unit
  spacing read as cramped even without Big Tree involved — moved Rock
  Node to `(-2, 0.35, 8)`, now ~4.48 units from Boulder.
- **"Can't break the boulder into rocks"** — investigated but no
  code-level bug found: `PlayerInteraction.cs` resolves hits via
  `GetComponentInParent<IPunchable>()`, so even a child collider under
  Boulder's new visual would still correctly resolve to its
  `ResourceNode`; confirmed via `git diff` that the Rock_Quaternius swap
  added zero colliders anywhere, and glTFast has no collider-generation
  option in the version this project uses. Leading theory is the
  Boulder/Rock Node proximity above was causing misaimed punches to land
  on the wrong node — needs Ben to retest specifically now that they're
  separated; **not confirmed fixed yet**.

### v0.1.87-dev — Rock Chunk and Boulder get real visuals too

Continuing the Stone swap from v0.1.86-dev to the rest of the stone
family.

- **`RockChunk.prefab`** (the Small Rock pieces that scatter when Rock
  Node breaks) now reuses `Stone_PolyByGoogle.glb` instead of a plain
  Sphere — but scaled **non-uniformly** (0.32 × 0.22 × 0.28, not a
  uniform shrink of the parent's proportions) so it reads as a distinct
  broken fragment rather than a miniature clone of the main rock. No
  mesh-reshaping tool exists in this pipeline — per-axis scale variation
  is the actual lever available, and that's what "tweak the shape" means
  here. Collider's physical world-space size preserved through the
  root-scale reset (same discipline as the Stick swap's non-uniform-scale
  hazard fix, v0.1.73-dev). Verified with full float precision, not
  Vector3's default 2-decimal `ToString()` — the resulting scale is
  ~1.2e-7 (matches the source mesh's enormous native coordinates) and
  briefly logged as "0.00" in a first verification pass, which looked
  like corruption but wasn't; a fresh instantiate-and-measure confirmed
  the actual rendered size hits the target exactly.
- **Boulder's visual replaced** — `Rock_Quaternius.glb` (public domain,
  Poly Pizza), swapped in for the old hand-tuned procedural shape
  (displaced-mesh body + 8 clustered pebbles, v0.1.62-dev) rather than a
  crude placeholder like Rock Node's sphere was. Target size/position
  came from measuring the OLD visual's actual current bounds *before*
  removing it, not a fresh guess — the new model lands centered on the
  exact same X/Z and grounded to the exact same depth (min.y) the old
  one occupied, size-matched to its largest dimension. The old
  "Pebbles" child wrapper is gone entirely (a completely different art
  style mixed with leftover procedural pebbles would've looked
  incoherent) and the `SphereCollider`'s center reset to origin (the old
  offset was hand-tuned for the old mesh's asymmetric centroid, not
  meaningful for the new one) — radius (0.9) kept as-is.
- Both verified via scripted read-backs confirming old
  MeshFilter/MeshRenderer/child objects are actually gone (not just
  added-alongside) and the new child/collider state matches what was
  intended.

**Licensing note:** Rock by Quaternius is public domain — no
Credits-tab attribution required (unlike the CC-BY Poly Pizza models),
though `Assets/Models/THIRD_PARTY_CREDITS.md` tracks it anyway for
sourcing consistency. Optional credit text noted there if ever wanted.

### v0.1.86-dev — Fixed a real Tree naming collision, Rock Node's real visual, Credits tab catches up

Three unrelated fixes/additions that landed together during a live
playtest session:

- **`Tree.cs` renamed to `ChoppableTree.cs`.** Found via the Console
  during Ben's playtest: `Tree` collided with `UnityEngine.Tree` (the
  built-in Terrain component) — Unity's own warning: "AddComponent and
  GetComponent will not work with this script." Real correctness bug,
  not just noise; fixed by renaming the class (kept the same script
  guid where possible — Unity's file-watcher raced the rename since the
  Editor was open, assigned its own fresh guid, so `Tree.prefab`'s
  component reference was updated to match the actual guid Unity landed
  on rather than the one originally intended). The missing Play button
  Ben hit in the same session turned out to be an unrelated toolbar
  rendering glitch (confirmed via `Ctrl+P` working fine) — not caused by
  this, but found while investigating it.
- **Rock Node's visual replaced** — was a plain built-in Sphere
  primitive, now `Stone_PolyByGoogle.glb` (CC-BY, Poly Pizza). The raw
  glTF's mesh coordinates are enormous (millions of units) with a pivot
  far outside the visible geometry — instead of hand-deriving the
  correct scale/position, the swap script instantiates once to measure
  actual world-space bounds, computes scale from that, then re-measures
  and corrects position on all three axes (not just Y grounding, like
  the Big Tree fix — this pivot was off-center on X/Z too) from the real
  result. Landed centered exactly on the original position and grounded
  exactly where the old sphere touched down, confirmed via direct
  measurement, not assumption.
- **Credits tab actually has content now.** Ben caught, mid-playtest,
  that the Third-Party Credits ledger (`Assets/Models/
  THIRD_PARTY_CREDITS.md`) had been flagging this gap for two entries
  running without anyone actually closing it. `GameMenuScreen.
  DrawCreditsTab()` now shows the Tree branch and Stone CC-BY
  attributions (exact required text, Big Tree excluded — still
  comparison-only, not confirmed shipping) plus a new credits image
  (`Assets/Textures/CreditsImage.png`, from Ben's `tekim_trex.png`),
  centered horizontally above the existing "Tekim"/"the T-Rex" names.
- All three verified via scripted scene/asset read-backs (component
  presence, actual measured bounds, serialized field references) rather
  than just "it compiled" — same discipline as every other Editor swap
  this session.

### v0.1.85-dev — Despawn timer (120s) now covers everything a player drops, not just plain items

Ben's ask: shortened from the existing 15-minute plain-item despawn to
2 minutes, and extended to cover equipment/coins, which previously had
no despawn timer at all.

- **`Pickup.DespawnDelay`: 900s (15 min) → 120s (2 min).** Already
  existed for plain stackable items (manual Drop, hand-eviction
  fallback, Admin spawn) — just a number change.
- **New shared `Despawn.cs` component** — attached at runtime (not
  pre-authored) to anything without its own despawn concept. Investigated
  first and found a real gap: `PlayerDropping.DropFrom`'s equipment
  branch (Backpack/Belt/Canteen/etc. dropped from a container's move
  popup) and all 7 equippable carriers' own dedicated `Drop()` methods
  (the Equipment section's Drop button — a *separate* code path from
  `PlayerDropping`, confirmed by reading each one) never applied any
  despawn timer at all — a dropped Backpack would sit in the world
  forever. Same gap for dropped Coins (`PlayerCoinDrop`, fully custom
  spawn path, no `Pickup` involved).
- **Real risk caught and designed around, not just bolted on:** `Despawn`
  uses an absolute `Time.time` deadline, not elapsed active-time. That
  distinction matters specifically for equipment — `Stash()` deactivates
  the GameObject (pausing `Update()`), but a later re-equip reactivates
  it; a deadline already in the past would otherwise fire immediately on
  reactivation and destroy something the player is now *wearing*.
  Fixed by having every equippable's `Stash()` **and**
  `SetCarried(true, ...)` (both paths a pickup can end on, depending on
  where `PlayerLoot` lands it) destroy any `Despawn` component on
  themselves — new `Despawn.CancelOn(GameObject)` static helper, one
  line per call site rather than duplicating the get/null-check/destroy
  pattern 14 times across 7 classes.
- Dropped Coins get `Despawn` too, but need **no** cancellation logic
  anywhere — `Coin.Complete()` already destroys the whole GameObject
  outright on a successful pickup, so there's no "stashed then worn
  again later" lifecycle for a lingering timer to wrongly fire against.
- Verified via a clean full-project batch-mode recompile (all 12 touched
  scripts) and a `Grep` sweep confirming exactly 9 `AddComponent<Despawn>`
  attachment sites (7 carrier `Drop()`s + `PlayerDropping.DropFrom` +
  `PlayerCoinDrop.SpawnCoin`) and 14 `Despawn.CancelOn` cancellation
  sites (`Stash()` + `SetCarried(true, ...)` × 7 equippable classes) —
  matches the design exactly, not just "it compiles."

### v0.1.84-dev — Stick Pickup now grants 10 at once (playtest convenience)

Ben's ask: needed enough Sticks on hand to actually exercise the Trimmed
Stick tiers (5 recipes, each consuming 1 Stick, now also risking loss
entirely on a Bad/Spectacular chance-of-creation failure — see
v0.1.82-dev) without repeatedly walking back to re-gather one at a time.

- `TestScene.unity`'s "Stick Pickup" (`(2, 0.15, 3)`) now grants 10 per
  grab (`Pickup.quantity` 1 → 10), verified via a scripted scene
  read-back. "Stick Pickup 2" left untouched at 1, so there's still a
  normal single-Stick pickup to test that path too. Both still respawn
  after 180s, unchanged.
- Playtest convenience, not a balance decision — easy to revert or retune
  once actual playtesting is done.

### v0.1.83-dev — Tree chopping: Tree → Log(s) → Plank (+ chance of a Stick)

Ben's ask, reshaped an existing (undocumented-as-such) mechanic rather
than building from scratch: Tree.prefab already had a `ResourceNode` —
4 Axe hits, drop 3 Wood chunks, hide-then-respawn. Replaced with a real
two-stage chop, matching how Boulder → Rock → Small Rock already works
for stone.

- **New `Tree.cs`** (not a `ResourceNode` reuse — deliberately different
  shape): 3 Axe hits (down from 4) drop `logCount` (3) `Log` instances
  scattered nearby, then the tree visual swaps to a **Stump** child
  instead of fully disappearing. `Awake()` caches every `Renderer` under
  the object except the Stump's own as "the tree" — no need to hand-wire
  dozens of the procedural mesh's "Leaf Cluster" children individually.
  Stump regrows into a full tree after `regrowDelay` (180s, same number
  the old `ResourceNode` used) — decided in conversation to keep trees a
  renewable resource like every other `ResourceNode`, not a one-time
  removal.
- **`Log.prefab`** (new): a placeholder cylinder, physically scattered by
  the falling tree, chopped down like any other `ResourceNode` — 2 Axe
  hits → 2 `PlankChunk` (new `Plank` item, new `ItemDefinition`, plain
  untiered material) — **plus a new 30% chance of also dropping a Stick**
  (reusing the existing item/model rather than inventing a redundant
  "Branch" — Stick already got a real branch model this session,
  v0.1.73-dev). Trains Woodworking (refining raw wood), while the Tree
  chop itself still trains Gathering (raw extraction) — same discipline
  split as everything else.
- **`ResourceNode.cs` gained two small, generically reusable additions**
  (not Tree/Log-specific) to make the Log stage possible:
  - `bonusChunkPrefab`/`bonusChunkChance` — an optional chance-based
    extra spawn alongside the guaranteed `chunkPrefab`/`chunkCount`,
    rolled once per break. Unlike `CraftingRecipe.bonusItem` (always
    guaranteed), this one is a real roll — could be reused later for
    e.g. ore nodes occasionally yielding a gem.
  - `respawnDelay <= 0` now destroys the node outright instead of
    scheduling a respawn — needed since a `Log` is itself a one-off
    spawn from the tree, with no sensible "same spot" to respawn at the
    way a fixed Boulder/ore node has. Every existing `ResourceNode`
    already has a positive `respawnDelay`, so this is additive, not a
    behavior change for anything else.
- Verified end-to-end via a scripted read-back of the rebuilt
  `Tree.prefab` and `Log.prefab`/`PlankChunk.prefab` — confirmed the old
  `ResourceNode` is actually gone (not just added-alongside), the Stump
  starts inactive, and every field (tool list, skills, chunk counts,
  bonus chance) resolved to the intended value, not just that references
  parsed.

**First-pass numbers, not deeply tuned:** log count (3), Log's own hit
count (2), Plank yield (2), Stick chance (30%). Easy to retune later —
none of this shape depends on the exact values.

### v0.1.82-dev — Chance-of-creation: crafting can now succeed brilliantly, barely fail, or go badly wrong

Ben's framing ("I'm feeling mean"): every craft now rolls between five
outcomes instead of always just succeeding — a real risk/reward layer on
top of v0.1.80-dev's skill gate, using the same skill-margin math.

- **Five outcomes**, resolved after ingredients are already spent (a bad
  or spectacular failure is "the materials were wasted," not "the attempt
  silently didn't happen"):
  - **Brilliant success** — produces the *next tier up* (`CraftingRecipe.
    higherTierItem`, new field) instead of what was attempted.
  - **Success** — the intended item, as always.
  - **Barely fail** — produces the *next tier down*
    (`lowerTierItem`) instead.
  - **Bad failure** — materials lost, nothing produced.
  - **Spectacular failure** — materials lost, the held tool breaks (only
    for recipes with a real `requiredTools` list — just Trimmed Stick
    today; skipped everywhere else, per Ben's call), and 10 direct health
    damage via a new `PlayerVitals.Damage(amount)` (the game only had a
    healing API before this).
- **Both edge cases resolve to plain Success**, decided in conversation:
  Crude has no `lowerTierItem` (nowhere lower — "barely fail" just
  works), Masterwork has no `higherTierItem` (nowhere higher — "brilliant
  success" just works), and every single-tier item (Rope, Cloth, Crude
  Fiber Belt/Backpack, all 5 gadgets) has neither, so the same collapse
  applies uniformly without needing a separate "is this item tiered?"
  flag.
- **Odds scale with skill margin** — how far `trainedSkill`'s level is
  above the tier's `CraftTierScale.SkillRequirement`, capped at 20 points
  (`RiskMarginCap`). At margin 0 (just barely qualified): ~63% Success,
  ~20% Barely Fail, ~12% Bad Failure, ~3% Spectacular, ~2% Brilliant. At
  margin ≥20 (well-practiced): ~85% Success, ~4%/~1%/~0% for the three
  failure-side outcomes, ~10% Brilliant. First-pass numbers, not deeply
  tuned. Recipes with no `trainedSkill` (the 5 gadgets) skip the roll
  entirely and always succeed plainly, same as before this system
  existed.
- New `CraftingRecipe.lowerTierItem`/`higherTierItem` populated across
  all 25 existing ladder recipes (4 tools × 5 tiers + Trimmed Stick × 5)
  via a batch-mode script, verified against every single one (not just a
  sample) — confirmed Crude/Masterwork boundaries are null in the right
  direction and every middle tier points at its actual neighbors, plus a
  20,000-trial simulated-roll check at several margins confirming the
  live distribution matches the intended curve.
- Outcome messages (Brilliant/Barely-Fail-with-a-real-downgrade/Bad/
  Spectacular) show as a new on-screen message on `PlayerCrafting`, same
  pattern as `PlayerSkills`' skill-up toast but positioned just below it
  (`y=110`) so both can show at once without overlapping. Plain Success
  stays silent, same as before.

### v0.1.81-dev — Positive, randomized message when a skill improves — special line on tier unlock

Ben's ask, refined twice in conversation: echo a statement when a craft
skill improves (previously the only feedback was checking the Skills tab
manually), then made positive/celebratory and randomized rather than one
fixed line, then given a distinct message specifically for the moment a
gain unlocks a new `CraftTier` — the natural pairing with v0.1.80-dev's
skill-gated tiers.

- `PlayerSkills.GainExperience` fires on every gain that actually raises
  the level (silent at MaxLevel 100, where diminishing returns make
  `newLevel == current` — no false "increased" message once capped).
  Picks a random line from one of two pools:
  - **`MessageTemplates`** (6 variants, e.g. "Congratulations! You have
    increased your {skill} skill to {level}!") for an ordinary gain.
  - **`TierUnlockTemplates`** (2 variants per tier) instead, specifically
    when the gain crosses a `CraftTierScale.SkillRequirement` threshold
    for the first time (10/25/50/100 — Rudimentary/Normal/Fine/
    Masterwork). No entry for Crude — its threshold is 0, so there's
    never a real "just unlocked Crude" crossing to celebrate.
  - Crossing-detection compares `current`/`newLevel` directly (not just
    the rounded display), so a gain like 9.6 → 11.4 correctly fires the
    Rudimentary line even though neither endpoint is a whole number.
    Verified via a throwaway script simulating repeated gains and
    checking the exact message at each step, including that boundary.
- Drawn as a 3-second on-screen message, top-center at `y=70` — placed
  just below where `PlayerNavComputer`'s compass sits (`y=10` to `y=62`
  when worn) so the two never overlap regardless of whether a Navigation
  Computer happens to be equipped.
- No queue — a gain while a message is already showing replaces it and
  resets the timer, rather than stacking. Rapid repeat crafting (e.g. 10
  Crude Knives in a row) will mostly just keep refreshing the same
  message rather than showing 10 in sequence.

### v0.1.80-dev — Skill-gated crafting tiers (1/10/25/50/100)

Ben's call: use skill level 1, 10, 25, 50, 100 to denote the 5 `CraftTier`s
— crafting a given tier now actually requires having trained for it, not
just having the ingredients and (where applicable) a tool in hand.

- **Real deadlock caught and resolved before building:** skills start at
  0, and the only way to gain most disciplines today
  (Stonework/Woodworking/Sewing) is crafting the exact items this gate
  would restrict. Requiring Crude ≥ 1 would mean a fresh character could
  never craft a first item in that discipline at all — nothing else
  feeds these skills yet. **Resolved: Crude requires 0** (no real gate,
  identical to today's behavior); the curve applies starting at
  Rudimentary: Rudimentary 10, Normal 25, Fine 50, Masterwork 100.
- New `CraftTierScale.SkillRequirement(tier)` alongside the existing
  `Modifier(tier)`. New `PlayerCrafting.HasRequiredSkill(recipe)` checks
  `recipe.trainedSkill`'s current level against it — recipes with no
  `trainedSkill` (the 5 gadgets: Canteen/Sunglasses/Nav Computer/Health
  Monitor/Mining Face Shield) are completely unaffected, same pattern as
  `HasRequiredTool`. Wired into `TryCraft` (blocks the craft) and
  `CraftingScreen` (greys out the button, shows `— requires Stonework
  25`-style label).
- **Real bug caught before it shipped:** `Rope`/`Cloth` never had an
  explicit `tier` set on their `ItemDefinition`s, silently defaulting to
  `CraftTier.Normal` — would have required Sewing ≥ 25 just to make
  basic Rope, breaking the very recipes meant to build up Sewing from
  zero in the first place. Fixed by setting both explicitly to `tier: 0`
  — they're single-tier items with no real ladder, so "Crude" here just
  means "no gate," not a real quality claim.
- Verified via a scripted read-back of all 34 recipes in
  `PlayerCrafting.Recipes`, confirming every single one's resolved
  required-skill level (not just that the new fields parsed) — every
  Crude/Rudimentary/Normal/Fine/Masterwork tool and Trimmed Stick tier,
  Rope/Cloth, Crude Fiber Belt/Backpack, and all 5 gadgets.

**Immediate effect:** closes out the previously-documented "known,
expected placeholder behavior" that all 5 tiers of every tool were
craftable side by side with nothing gating the higher ones
(`TEST_FEATURE_PLAN.md`) — a fresh character can now only craft Crude
tools until Stonework reaches 10. **Still open:** the *ingredient*-quality
half of the weakest-link rule (every tier still costs identical
ingredients) — skill is the only thing gating tier today.

### v0.1.79-dev — Crude Fiber Belt / Crude Fiber Backpack: first real starter gear recipes

Ben's framing: now that Fiber exists, a real starter Belt and Backpack
should be craftable from it. First recipes ever to output actual
equipment (not a plain stackable), which required fixing a real
architecture gap along the way.

- **Fixed: `PlayerCrafting.TryCraft` couldn't produce a working
  equippable.** It always called `inventory.AddItem(...)` — a plain
  stackable add with no `.equipment` reference — so any equippable output
  would have landed as an inert, non-wearable stack. New
  `AddCraftedOutput` helper: if `outputItem.worldPickupPrefab` has an
  `IEquippable` component, instantiate it, `Stash()` it, and add it via
  `AddEquipmentItem` instead. Applies to `bonusItem` too, though nothing
  uses that combination yet. **Only fixes the crafting-output side** of
  the gap logged in `BUGS_AND_ENHANCEMENTS.md` — the Admin spawn tab's
  matching bug (`PlayerDropping.SpawnPickup`, a different code path) is
  still separately broken; corrected that entry rather than claiming both
  fixed.
- **`Belt` renamed to `Fiber Belt`** (guid unchanged, so the existing
  world pickup and `PlayerCanteen`'s belt-attachment logic didn't need
  touching) — establishes "Fiber Belt" as the ladder's base name per
  Ben's call, so future tiers read `Crude Fiber Belt` … `Masterwork Fiber
  Belt`, consistent with `CraftTierNames`.
- **New `Crude Fiber Belt`** — 8x Fiber → 1 Crude Fiber Belt, 2 attachment
  points (matches the already-decided Crude-tier point count), trains
  Sewing. First Belt tier to ever have a recipe.
- **New `Crude Fiber Backpack`** — a *distinct* item from the existing
  `Backpack` ladder (Ben's explicit call, not filling in the existing
  recipe-less `Crude Backpack`) — 15x Fiber → 1 Crude Fiber Backpack,
  capacity 4 (matches the existing Crude-tier capacity number). Trains
  Sewing.
- Both new items got real `.prefab` assets for the first time (previous
  world pickups were standalone scene GameObjects, not reusable prefabs)
  — `Assets/Prefabs/CrudeFiberBelt.prefab` /
  `Assets/Prefabs/CrudeFiberBackpack.prefab`, needed so
  `ItemDefinition.worldPickupPrefab` has something to instantiate.
  Placeholder flat-box visuals reused from the existing Belt/Backpack
  placeholders (`Backpack.mat` tint) — no dedicated art pass, and
  thematically a woven-fiber item probably shouldn't share a leather
  material long-term.
- Fiber costs (8 / 15) and Sewing `skillGain: 2` are first-pass numbers,
  not deeply tuned — same "reasonable starting point, adjust after
  playtesting" spirit as everything else in the tool tiers.

**Still open:** Rudimentary/Fine/Masterwork Fiber Belt and the
corresponding Fiber Backpack tiers aren't built — this pass only covers
the Crude "starter" tier of each, per the original ask. Leather sourcing
and the original (non-Fiber) Backpack ladder's recipes remain unbuilt too.

### v0.1.78-dev — Rope and Cloth recipes, both trained by Sewing

Next link in the Fiber chain, right after Fiber itself landed. Both new
recipes are pure Fiber refinement — no tool required, no byproduct.

- New `Rope.asset`/`Cloth.asset` `ItemDefinition`s — plain stackable
  materials (no tier), same shape as Fiber/Stick/Wood.
- `RopeRecipe`: 5x Fiber → 1 Rope. `ClothRecipe`: 10x Fiber → 1 Cloth.
  Both train **Sewing** (`skillGain: 2`, matching the flat rate every
  other base recipe uses) — the first two recipes to ever train that
  skill, which existed as an empty `SkillDefinition` since the discipline
  split (v0.1.70-dev) with nothing populating it. Already listed in
  `CraftingScreen`'s discipline tabs, so no scene wiring needed there.
- Both added to `PlayerCrafting.recipes` on the Player GameObject in
  `TestScene.unity` (32 recipes now, up from 30). Verified via a throwaway
  batch-mode script that opened the scene and read back
  `PlayerCrafting.Recipes` directly — confirmed ingredient counts, output
  items, and `trainedSkill` all resolved correctly, not just that the
  guids parsed.

**Still open:** Leather sourcing (implies hunting/animals — doesn't exist
at all yet), and the actual Backpack/Belt recipes now that Cloth (and
maybe Rope, for straps/drawstrings) exist as real ingredients. See
`BUGS_AND_ENHANCEMENTS.md`.

### v0.1.77-dev — Trimming a Stick now also yields Fiber

First real step on the Fiber → Cloth / Leather material chain flagged as a
blocker for Backpack/Belt recipes (`BUGS_AND_ENHANCEMENTS.md`). Ben's
framing: trimming a branch with a knife should realistically leave you
with some usable fiber, not just the trimmed stick.

- `CraftingRecipe` gained an optional secondary output — `bonusItem`/
  `bonusCount` (default null/1) — alongside the existing `outputItem`/
  `outputCount`. Guaranteed when set, same as every other recipe in the
  game; no randomness introduced. Most recipes don't need it and leave it
  unset.
- All 5 `TrimmedStick` recipes (Crude through Masterwork) now also output
  1 Fiber, flat across every tier — deliberately not scaled like the
  point/capacity curves elsewhere, since the Trimmed Stick recipes
  themselves are still identical scaffolding across tiers with no real
  differentiation yet.
- New `Fiber.asset` `ItemDefinition` — plain stackable raw material (no
  tier, no world pickup), same shape as Stick/Wood.
- `PlayerCrafting.TryCraft` checks space for and adds the bonus output
  alongside the primary one; `CraftingScreen`'s recipe list now shows it
  (`Trimmed Stick + 1x Fiber  (needs ...)`) and factors it into the
  "inventory full" check.
- No scene/prefab changes needed — this only touches recipe data and the
  crafting scripts, since Fiber has no physical world presence yet (only
  produced as crafting output).

**Still open:** this only answers "where does Fiber come from" — the
Fiber → Cloth refining step, a Leather source, and any real recipe for
Backpack/Belt are all still unbuilt. See `BUGS_AND_ENHANCEMENTS.md`.

## 2026-08-06

### v0.1.76-dev — Equip destination picker for multi-slot equippables

Ben's follow-up thought right after the Belt landed: now that Canteen can
go to Left Hand, Right Hand, *or* a worn Belt's attachment points,
clicking Equip silently picking whichever one the carrier tried first
isn't good enough — the player should see the real options and choose.
Same gap already existed for NavigationComputer/PersonalHealthMonitor
(Left Wrist or Right Wrist), just less noticeable with only 2 options.

- `PlayerCanteen`/`PlayerNavComputer`/`PlayerHealthMonitor` each gained
  `AvailableDestinations(item)` (every currently-free slot that would
  actually accept the item right now) and `EquipTo(item, destination)`
  (commit to one specific destination, instead of `Equip`'s old
  first-match-wins loop). `Equip(item)` is now a thin wrapper —
  `AvailableDestinations(item)[0]` through `EquipTo` — so existing callers
  keep working unchanged.
- Backpack/Belt/Sunglasses/MiningFaceShield **don't** get this treatment —
  each only ever has exactly one possible destination (Back/Waist/Face),
  so there's nothing to choose between; their Equip buttons still equip
  immediately, same as before.
- `InventoryScreen.cs`: new `TryEquipWithChoice` overloads (one per
  multi-destination type) replace the direct `.Equip()` calls at both
  click sites — the main inventory list, and a not-yet-worn item sitting
  in a hand slot in the Equipment section. 0 or 1 available destinations
  still equips immediately; 2+ opens a new popup (`DrawPendingEquipPopup`,
  same visual pattern as the existing "where should this go?" move popup)
  listing them as buttons.
- **Doesn't close** the two related, still-open gaps in
  `BUGS_AND_ENHANCEMENTS.md` ("No way to move an equipped item into a
  backpack" and "Equip directly from a container") — those are about
  different actions (moving an already-equipped item elsewhere, and
  equipping straight from a container's contents) that this popup doesn't
  touch. Related, not the same fix.

### v0.1.75-dev — Backpack retiered, new Belt equippable (design-only recipes)

Built the two pieces from the same design conversation logged in
`BUGS_AND_ENHANCEMENTS.md`: Backpack folded into the 5-tier `CraftTier`
ladder, and a new Belt equippable with generic attachment points. Neither
got a real recipe this pass — see "Still open" below for why.

**Backpack:**
- `Backpack.cs` gained an `itemDefinition` field (replacing the hardcoded
  `backpackName` string) so a physical Backpack instance can point at any
  tier's `ItemDefinition` — previously `PlayerBackpack` only ever
  recognized a single hardcoded `backpackItem`, which couldn't represent
  multiple coexisting tiers. `PlayerBackpack`'s Pick Up/Equip/Unequip/Drop
  now all read `backpack.ItemDefinition` instead.
- `RoughBackpackItem.asset` renamed to `BackpackItem.asset`, `itemName`
  "Rough Backpack" → "Backpack" (Normal tier, no prefix, per the existing
  `CraftTierNames` convention — same as `Rock Knife` → `Crude Knife`).
  Same guid, so the existing world pickup and `Assets/Prefabs/
  Backpack.prefab` (still orphaned/unreferenced, kept in sync anyway)
  didn't need re-linking.
- New `ItemDefinition`s for the other 4 tiers: `CrudeBackpackItem`,
  `RudimentaryBackpackItem`, `FineBackpackItem`, `MasterworkBackpackItem` —
  capacity table from the design conversation (4/6/8/12/16). **Data only
  for now** — no recipe, no world pickup, nothing can spawn them yet
  (see "Still open").

**Belt (new):**
- `Belt.cs` (new, mirrors `Backpack.cs`): worn at Waist, holds a fixed
  number of generic attachment points (`points = 6` for this Normal-tier
  instance) as its own `Inventory` rather than general storage — any
  attachment consumes exactly 1 point regardless of kind.
- `PlayerBelt.cs` (new, mirrors `PlayerBackpack.cs`): Pick Up/Equip/
  Unequip/Drop into the Waist slot.
- `PlayerCanteen.cs` reworked: a worn Belt now occupies the body's actual
  `Waist` `PlayerEquipment` slot, so a bare Canteen's fallback chain
  changed from `Left Hand → Right Hand → Waist` to `Left Hand → Right
  Hand → the equipped Belt's attachment points` (not a named
  `PlayerEquipment` slot, so this needed its own branch rather than
  reusing the old string-array slot list).
- `InventoryScreen.cs`: Equip/Drop buttons for a Belt sitting in the main
  inventory, Equip/Unequip/Drop + a nested contents side-column for the
  Waist slot (reusing the same `DrawContainerContents`/wornContainer path
  Backpack's Back slot already had — widened from Back-only to Back-or-
  Waist).
- New `BeltItem.asset` (Normal tier) + one world pickup (simple flat-box
  placeholder, no dedicated art this pass, tinted with the existing
  `Backpack.mat`) placed near Canteen's starter-gear spot at
  `(-2, 0.3, 1.5)`.

**Still open, deliberately not built this pass:**
- **No recipes for Backpack or Belt.** Ben's call mid-build: hold off
  until there's a real Fiber → Cloth textile chain and a way to source
  Leather, rather than faking it with placeholder ingredients (Stick/Wood)
  the way the tool tiers did. New backlog item in
  `BUGS_AND_ENHANCEMENTS.md` for that material chain.
- **Crafting can't produce a working equippable at all yet, independent of
  the above.** Discovered mid-build: `PlayerCrafting.TryCraft` always
  calls `inventory.AddItem(...)` — a plain stackable add with no
  `.equipment` reference — so even with a recipe, a "crafted" Backpack/
  Belt would land as an inert, non-wearable stack. Same root cause as the
  already-logged "Admin spawn tab can't spawn a working equippable
  gadget" bug. Logged as its own BUGS item; needs fixing before either
  recipe can actually work.
- **Only one worn container's contents show in the Inventory tab's side
  column at a time.** `InventoryScreen.DrawEquipmentSection`'s
  `wornContainer` is a single value, last-writer-wins across the
  `SlotOrder` loop — if both a Backpack (Back) and a Belt (Waist) are worn
  simultaneously, only the Belt's points render in the side column (Waist
  comes after Back in `SlotOrder`); the Backpack's contents don't disappear
  functionally, just visually. Pre-existing code only ever needed to
  support one worn container before Belt existed. Logged in
  `BUGS_AND_ENHANCEMENTS.md`.
- Only the Normal-tier Backpack is reachable in play today (the existing
  world pickup) — Crude/Rudimentary/Fine/Masterwork `ItemDefinition`s
  exist as data but have no spawn path (no recipe, not pre-placed, Admin
  spawn tab doesn't work for equippables either). Intentional — no point
  building world pickups/prefabs for tiers nothing can craft yet.
- Attachment types beyond a bare Canteen (Scabbard/Pouch/Holster) are
  still just the open design question already logged, not built.

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
