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

## Bugs Found (2026-08-03)

- Canteen visual feedback — Filling then dropping doesn't render it blue
- Backpack drop bug — Right-clicking stick in backpack removes entire backpack instead of just the stick
- Canteen fills from anywhere — Can fill equipped canteen from any location (should be limited to water sources)
- Overdrinking not implemented — Can drink past 100% thrust without consequence; should allow to 125% then cause sickness, health loss, thirst → 50%
- Canteen transfer breaks interaction — Moving canteen from backpack to hands removes ability to fill or drink
