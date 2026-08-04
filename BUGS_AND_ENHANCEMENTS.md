# Bugs & Enhancements

Known issues and requested features not being worked right now. Not a replacement
for `WORKING_ON.md` (that's for active work) or `CHANGELOG.md` (that's for shipped
work) — this is the backlog between the two. Check off and move the entry to
`CHANGELOG.md` once it's actually fixed/built.

## Bugs

(none right now)

## Enhancements

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
