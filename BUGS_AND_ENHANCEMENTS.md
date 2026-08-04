# Bugs & Enhancements

Known issues and requested features not being worked right now. Not a replacement
for `WORKING_ON.md` (that's for active work) or `CHANGELOG.md` (that's for shipped
work) — this is the backlog between the two. Check off and move the entry to
`CHANGELOG.md` once it's actually fixed/built.

## Bugs

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
- [ ] **No way to move an equipped item (e.g. Canteen) into a backpack.**
  `InventoryTransfer.Move`/`Inventory.AddEquipmentItem` already support carrying an
  equipment reference into any `Inventory`, backpack included, but no UI path ever
  calls it for an equipment-backed slot: `DrawEquipmentSection` draws an
  `entry.equipment != null` slot as a plain `GUILayout.Box` (not a `Button`, so
  it's not clickable at all), and `DrawInventorySection`'s equipment branches
  (Backpack/Canteen/NavigationComputer/PersonalHealthMonitor/Sunglasses) only ever
  offer Equip/Drop — unlike the plain-item branch, there's no "To Backpack"/"To
  Storage". Affects every equippable, not just the Canteen. *(Reported by Ben.)*

## Enhancements

- [ ] **Spawn a starting Pickaxe (and likely Axe) in the world for now.**
  Ben hit this directly playtesting the ore work: `Pickaxe`/`Axe` are craft-only
  today (2 Small Rock + 1 Stick / 1 Small Rock + 2 Stick), with no world pickup
  instance anywhere — unlike Backpack/Canteen/Sunglasses/Nav Computer/Health
  Monitor, which all have at least one pre-placed near spawn so a fresh
  playthrough doesn't have to craft everything from zero before it can do
  anything. Bare-handed Rock Node mining still works without a Pickaxe, so
  gathering the materials to craft one isn't actually blocked — but the friction
  of needing to know that and craft one first before ever touching ore is real.
  A stopgap for now ("for now" — Ben's words), not necessarily a permanent
  design call once the fuller skills/tools pipeline lands: place one Pickaxe
  (and probably one Axe, same bootstrapping situation, not yet confirmed) as a
  world pickup near the other starter gear. *(Reported by Ben.)*
- [ ] **Full crafting/gathering/skills redesign — partially built.** See
  `docs/design-brief.md`'s **Crafting, Gathering & Skills Pipeline (2026-08-04)**
  section for the complete plan: 7 new skills (Mining, Woodworking, Stonework,
  Metalworking, Forging, Minting, Sewing, alongside existing Gathering and
  Crafting — 9 total), a weakest-link tier rule (skill vs. material quality), a full
  gather→refine→assemble material web (wood, stone, metal, textiles), tool-quality
  effects (yield/quality/speed), and a new click-once-and-locked interaction model
  that replaces the current punch-to-break mechanic entirely. Large, cross-cutting,
  and *decided in shape but not in exact numbers* — several sub-questions are
  explicitly still open (see that section's own "Still open" list).
  **Shipped so far (v0.1.60-dev):** the full ore ladder (Iron/Silver/Gold/Platinum
  Ore Nodes) and the Mining Face Shield hidden-ore detection mechanic (visual
  reveal + yield gating both, not just the visual half) — see `CHANGELOG.md`.
  **Still not built:** the Mining skill itself as an actual `SkillDefinition`
  (nodes currently still train `Gathering`, per what already existed, not the
  newly-decided `Mining` split — that decision hasn't been wired into code yet),
  every other skill (Woodworking/Stonework/Metalworking/Forging/Minting/Sewing),
  the weakest-link `CraftTier` determination itself, the full material web beyond
  ore, and the new click-and-locked interaction model (everything still uses the
  old instant-hold-E/punch mechanics). Don't start implementing any further piece
  of this without re-reading the full design-brief section first — it's too
  interlocking to build from memory of this one-line summary.
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

  *(Reported by Ben. The 15-minute despawn timer on dropped items that was
  originally part of this same request shipped separately in `v0.1.48-dev` —
  see `CHANGELOG.md` — and doesn't yet cover the equipped-item unequip-fallback
  drop path described above, since that path isn't built yet either.)*
- [ ] **Equip directly from a container.** Same underlying gap as "Eat/Drink
  directly from a container" below — `DrawContainerContents` (backpack contents
  and storage boxes alike) treats every item as a generic move-popup button
  regardless of `entry.equipment`, so an equippable item sitting in a backpack
  (Sunglasses, a spare Canteen, Navigation Computer, Personal Health Monitor) has
  no direct Equip button; it has to be moved out to a hand or the main inventory
  first. *(Reported by Ben.)*
- [ ] **Eat directly from a container.** Food items sitting in a backpack (or other
  container) can't be eaten in place today — `DrawInventorySection` in
  `InventoryScreen.cs` gives main-inventory items a direct "Eat" button via
  `PlayerEating.FindEdible`/`TryEat`, but `DrawContainerContents` (used for a worn
  backpack's contents and nearby storage boxes) only offers the generic "where
  should this go?" move popup for every item, edible or not. Player has to move food
  out to the main inventory first.
- [ ] **Drink/fill directly from a container.** Same gap for a Canteen sitting in a
  backpack/container — no direct Drink/Fill buttons there, only the generic move
  popup (which, as of 2026-08-03, correctly preserves the equipment reference when
  used, but still requires moving the canteen out before it can be used).
