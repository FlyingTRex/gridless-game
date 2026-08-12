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

- 2026-08-11 — Claude (traskmi's session) — Scope grew from "model only" to
  a real new item + recipe + crafting-station system. Current plan:
  - **Iron Ingot model — DONE.** Low-poly (54 tris) + metallic material via
    headless Blender, exported to `Assets/Models/IronIngot.glb`. This is
    NOT a replacement for `Iron.asset`'s existing rock-placeholder visual
    (that item stays as-is) — it's the visual for a brand new item.
  - **New item + recipe (not yet built):** `IronIngot.asset`
    (`ItemDefinition`, tier TBD) + `IronIngotRecipe.asset`
    (`CraftingRecipe`: 10x Iron → 1x Iron Ingot, trainedSkill =
    Metalworking, matching `NailRecipe.asset`'s structure/tier-2 pattern)
    + a `Pickup` prefab wired to the new Blender model.
  - **New "requires a Furnace" gate (not yet built) — mirrors the existing
    `requiresAnvilSurface` pattern exactly:** add
    `CraftingRecipe.requiresFurnace` (bool), a trivial `FurnaceSurface`
    marker component (same shape as `AnvilSurface.cs`), a
    `PlayerCrafting.HasNearbyFurnace` check called alongside
    `HasNearbyAnvilSurface` in `StartCraft`. Needs one placed Furnace
    object in `TestScene.unity` (Anvil's existing placement is the
    template) — `Assets/Models/CrudeFurnace.glb` already exists
    (generated v0.3.5-dev) but isn't placed/wired into anything yet.
  - **Icon:** bake via the existing `IconBaker.BakeAndWire` (reusable
    tool, no new baking code needed).
  - **Admin Spawn Screen — item auto-discovery already covers the new
    item for free** (`AssetDatabase.FindAssets("t:ItemDefinition")`,
    confirmed by reading `AdminSpawnScreen.cs` — no work needed there).
  - **New, separate small ask (Ben's/traskmi's, 2026-08-11): add a search
    box to `AdminSpawnScreen`** — flat unfiltered list is getting long.
    Not started.
  - **Blocked on confirming Unity Editor lock status before any of the
    above gets built** — batch mode fails outright if the Editor's open
    (`CLAUDE.md` rule), asked traskmi, awaiting confirmation.
