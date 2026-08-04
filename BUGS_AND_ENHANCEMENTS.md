# Bugs & Enhancements

Known issues and requested features not being worked right now. Not a replacement
for `WORKING_ON.md` (that's for active work) or `CHANGELOG.md` (that's for shipped
work) — this is the backlog between the two. Check off and move the entry to
`CHANGELOG.md` once it's actually fixed/built.

## Bugs

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

- [ ] **Simplify item-holding to two states: equipped or inventory-stored — no
  ad-hoc "held in a hand" third state — plus a despawn timer on dropped items.**
  Today `PlayerLoot`'s pickup priority is Backpack → Left Hand → Right Hand →
  evict-into-world (`CHANGELOG.md` v0.1.10-dev/v0.1.15-dev), and a plain picked-up
  item can sit directly in a hand slot as an in-between state: not equipped (no
  Equip button was ever pressed) and not really "inventory" either. Requested
  target design:
  - Every object is always either **equipped** into a named equipment slot, or
    **stored** in an inventory slot (main inventory / backpack / storage box) —
    eliminate that third, ad-hoc "just sitting in a hand" holding state.
  - **Pickup:** if a hand is free, the item goes there first; otherwise it goes
    into an inventory slot. *(Scope question for whoever implements this: does
    this replace `PlayerLoot`'s existing Backpack-first priority outright, or
    only fill the gap when no hand is free? Confirm with Ben before changing the
    order — this is a real behavior change from what's shipped today.)*
  - **Unequip:** the item goes to an inventory slot; if every inventory slot is
    full, drop it to the ground instead of failing. (`PlayerBackpack.Unequip`
    already has this exact fallback chain — extend the same guarantee to every
    equippable: Canteen, Sunglasses, NavigationComputer, PersonalHealthMonitor.)
  - **Manual drop from inventory:** unchanged — goes straight to the ground.
  - **New: 15-minute despawn timer on world-dropped items.** Any dropped item
    (manual drop, unequip-fallback drop, or hand-eviction) disappears from the
    world if left unpicked for 15 minutes. This is distinct from `Pickup`'s
    existing `canRespawn`/`respawnDelay` (3 minutes, Stick/Rock Node only) —
    that mechanic *respawns* a resource point at its original location; this is
    a separate *despawn* timeout for player-dropped items specifically, so it
    shouldn't just reuse `canRespawn`/`respawnDelay` as-is.

  *(Reported by Ben.)*
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
