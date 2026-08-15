# Efficiency Audit

Ben's ask (2026-08-15): after a long stretch of rapid feature-building,
look across the whole codebase for patterns that are costing more than
they should — not a style nitpick pass, specifically opportunities to
reduce real risk and repeated work. Researched via two parallel
read-only agent passes (registration-array/duplication scan; Editor
tooling/dead-asset scan) plus direct knowledge from tonight's own
builds. Nothing in this doc has been acted on yet — it's the findings,
prioritized, waiting on a decision about what to fix now vs. log.

## 1. Manually-curated registration arrays — the big one

**This is not a one-off gap — it's a pattern used in ~20 different
fields across ~18 scripts**, and it already caused a real, live bug
tonight: `ItemDatabase.items` silently missed 22 items (not just
tonight's new ones), which would have broken `Find()`-based save/load
restore for all of them until caught by directly comparing counts.

The shape every time: a `[SerializeField] private T[] someField;` on a
scene object or asset, hand-populated by whoever built that feature,
with no auto-discovery — so a new asset that should be in the list
silently isn't, until something downstream (a lookup, a save/restore,
a crafting-tab listing) fails quietly.

| Field | Script | Scale | Risk if missed |
|---|---|---|---|
| `items` | `ItemDatabase.cs` | ~112 entries | **Confirmed — broke tonight.** Save/load `Find()` silently returns null. |
| `recipes` | `PlayerCrafting.cs` | 57 | New recipe never appears in Crafting tab. |
| `allPieces` | `PlayerBuilding.cs` | 20 | New piece never appears in Build tab. |
| `jobs` | `NPCJobDatabase.cs` | — | Same shape/risk as `ItemDatabase`. |
| `skills` | `SkillDatabase.cs` | — | Same shape/risk as `ItemDatabase`. |
| `edibles` | `PlayerEating.cs` | 10 | New food item can't be eaten. |
| `fuelItems` / `cookableItems` | `Campfire.cs` | 7 / small | New fuel/recipe invisible to that station. |
| `smeltableItems` | `Furnace.cs` | 5 | Same. |
| `hammerTiers` | `PlayerPieceUpgrade.cs` | 5 | — |
| `disciplines` | `CraftingScreen.cs` | 7 | — |
| `families`, `jobs` | `NPCJobScreen.cs` | 3 / 3 | — |
| `allLineages`, `allWishes` | `PlayerMagic.cs` | 4 / 3 | — |
| `registeredCrops` | `GardenPlot4x4.cs` | small | New crop can't be planted. |
| `medicines` | `PlayerMedicine.cs` | 2 | — |
| `startingLevels` | `NPCSkills.cs` *and* `PlayerSkills.cs` (duplicated shape, two scripts) | — | — |

Not every array-of-ScriptableObject field is this risk — `requiredTools`
on `HostileCreature`/`PreyCreature`/`ResourceNode`/`ChoppableTree`/
`BerryBush` and `plantAnchors` on `GardenPlot4x4` are legitimate
per-instance config (which tools work on *this specific node*), not a
global registry, and shouldn't be touched.

**The fix already exists in miniature and just needs generalizing.**
`AdminSpawnScreen.cs` already does `AssetDatabase.FindAssets("t:X")`
scans for `ItemDefinition`/`BuildPiece`/`GuildDefinition` at Editor
time — that pattern can't run at runtime in a build (which is
presumably why every field above went manual instead), but it's exactly
right as an **Editor-time auto-populate step**. `ItemDatabase.cs`
already has an `EditorSetItems` hook sitting unused for this — nothing
currently calls it automatically.

**Recommendation:** one small shared Editor utility (a generic
"re-scan and repopulate this array from all assets of type T" button/
menu item, callable per-field) rather than fixing each of the ~15 call
sites by hand. Run it as a standing habit before any commit that adds
new content, the same way a compile check already is. Medium-sized
build, high value given tonight's incident was real, not theoretical.

## 2. Duplicated class logic

**`HostileCreature.cs` (226 lines) vs. `PreyCreature.cs` (133 lines) —
built four hours apart tonight, and it shows.** Roughly 90 of
`PreyCreature`'s 133 lines are near-line-for-line copies of
`HostileCreature`: `Awake`, `TakeDamage`, `Die` (the tip-over-90° pose),
the tool-gated `Complete()` skin flow, `HasAnyRequiredToolInHand`,
`Respawn`, `SetVisible`. The only genuine difference is
`HostileCreature`'s aggro state machine (Idle/Chasing/Attacking,
~70 lines) and its single pelt+meat drop shape vs. `PreyCreature`'s
two-independent-loot-slot drop.

**Recommendation:** extract a shared base (e.g. `SkinnableCreature`)
holding health/death/skin-complete/respawn/visibility; `HostileCreature`
adds the aggro state machine on top. Cuts ~90 duplicated lines to one
copy, and — more importantly — means the next prey animal (Pig/Deer/
Rabbit) or the eventual flee-AI upgrade only has to change logic in one
place. Small-to-medium size, low risk (both scripts are young, nothing
else depends on their exact shape yet).

**`HandSlots = { "Left Hand", "Right Hand" }` is independently declared
in 13 separate files** — every equip-carrier script
(`PlayerBackpack`, `PlayerBelt`, `PlayerBoot`, `PlayerCanteen`,
`PlayerHealthMonitor`, `PlayerLoot`, `PlayerJeans`, `PlayerNavComputer`,
`PlayerMiningFaceShield`, `PlayerRangedCombat`, `PlayerShirt`,
`PlayerTool`, `PlayerSunglasses`). Confirms Ben's instinct exactly.
**Recommendation:** one shared `static readonly` (a small
`PlayerEquipSlots` helper), referenced everywhere instead of restated.
Small, safe, mechanical fix — genuinely just find-and-replace once the
shared constant exists. Given how many scripts share this one line,
it's worth a follow-up look at whether these 13 "PlayerXxx" carriers
have a bigger shared shape worth a common base class too — flagged as a
separate, not-yet-scoped follow-up, not sized here.

## 3. Editor tooling — one more permanent utility justified

Git history confirms the "write a throwaway `Assets/Editor/*.cs`
script, run it, delete it" convention is followed rigorously — only 3
`.cs` files have EVER been added to `Assets/Editor/` across the
project's full history without later deletion (`SceneAutoOpen.cs`,
`IconBaker.cs`, `PrefabBuildingPlacer.cs`), meaning every other
Editor script (an unknown but clearly large number, given 83
`*Pickup.prefab` files exist and most needed one) was written from
scratch, used once, and thrown away — including its "build a pickup
prefab" logic (instantiate model → measure/ground bounds → add
Rigidbody+BoxCollider+Pickup → wire item/quantity → save prefab), which
is structurally identical every single time.

**Recommendation:** a permanent `Assets/Editor/PickupPrefabBuilder.cs`,
directly parallel to `IconBaker.cs` (which exists for exactly this
reason — its own header comment says it replaced "a new bespoke
throwaway script per item"). Would remove the need to re-derive the
bounds/grounding/collider-sizing math every session, which is also
where several of tonight's real bugs originated (the arrowhead's
arbitrary 2.4x oversizing, the VFX-rig bounds contamination on crop
pickups). Small build, meaningful ongoing time savings.

## 4. Orphaned/dead content

- **`MediumRock.asset`** ("Rock" item) — already flagged in
  `BUGS_AND_ENHANCEMENTS.md`. No recipe, no prefab, nothing references
  it since `MediumRockChunk.prefab` switched from `Pickup` to
  `ResourceNode` in v0.1.90-dev.
- **`SoccerBall.asset` — new finding, not yet logged.** Registered in
  `ItemDatabase.asset` but otherwise unreachable: no recipe grants or
  consumes it, no pickup prefab exists, never placed in `TestScene.unity`.
  Same "registered but unreachable" shape as `MediumRock`. (Not to be
  confused with `SoccerBall.prefab`/`SoccerBall.cs`, an unrelated
  kickable-ball GameObject that's very much alive.)

Sampled 11 of the 120 `ItemDefinition` assets under `Assets/Data/`
(265 total `.asset` files there, the rest being recipes/skills/jobs/
etc.) — only these two came back orphaned. Not a systemic problem, just
two real entries worth cleaning up or building out.

## Suggested priority, if tackling now

1. ✅ **`ItemDatabase`/`SkillDatabase`/`NPCJobDatabase` auto-populate**
   — `DatabaseRepopulator.cs` (new permanent `Assets/Editor/` tool).
2. ✅ **`HandSlots` consolidation** — `PlayerEquipSlots.cs`, all 13
   carrier scripts updated.
3. ✅ **`SkinnableCreature` base class** — `HostileCreature`/
   `PreyCreature` both refactored onto it; Wolf/Chicken's already-
   serialized field values verified intact via direct YAML grep.
4. ✅ **`PickupPrefabBuilder.cs`** — new permanent tool, parallel to
   `IconBaker.cs`.
5. ✅ **Orphaned items** — `MediumRock.asset` deleted; `SoccerBall`
   wired in for real (Pickup component added to its prefab +
   `SoccerBallRecipe.asset`, Cloth ×3/Sewing).

All five items built and verified 2026-08-15, v0.3.89-dev — see
`CHANGELOG.md`.
