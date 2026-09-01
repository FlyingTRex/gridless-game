# Working On

What's actively in progress right now, one line per active session. Check this
before starting new feature work — if something here overlaps what you're about
to build, coordinate before duplicating effort (see the Waterskin/Canteen
collision in `CHANGELOG.md`, 2026-08-02, for what happens when this doesn't get
checked).

Add a line when you start a non-trivial feature; remove it once merged to
`origin/main`. Stale entries are worse than none — if you're not sure whether an
entry is still active, ask before trusting it.

Note: "merged to origin/main" means the code is in — it doesn't require a live
Play-mode pass first. Manual test status for a shipped feature belongs in
`TEST_FEATURE_PLAN.md`, not here; don't keep an entry alive just to track that a
live test is still pending.

Format: `- YYYY-MM-DD — who — one-sentence description`

**Everything through v0.3.213-dev is merged to `origin/main`** —
`PlayerReading` and `PlayerWriting` both fixed (real permanent-unlock
loss risk for a remote client — writing was worse, since it also spawns
a brand-new physical object that never network-spawned at all), plus
the identical unsynced-`HashSet` gap found and fixed in both
`PlayerCrafting.bookGrantedRecipes` and `PlayerMagic.bookGrantedWishes`.

## Multiplayer roadmap — Phase 3 done, a much bigger world-interaction gap found and mostly closed since

`MULTIPLAYER_PLANNING.md`'s entire phased plan (Phase 3 player-
authoritative conversion, NPCs server-side, the persistence restructure,
and real per-connection spawning) is done and live-confirmed. Full
chunk-by-chunk detail lives in that doc and in `CHANGELOG.md`'s
2026-08-25 entries — summary:

- Persistence restructure (chunks 1-6): per-player save keying, a real
  `RequestSave` Command, autosave/disconnect/shutdown saves, and a
  Host/Join Connect screen — all live-confirmed.
- Real per-connection Player spawning: a second connection now gets a
  genuine second character instead of being refused. Surfaced (and
  fixed) that none of the 48 `PlayerXXX.cs` scripts gated local input
  by `isLocalPlayer` — invisible until a second Player object could
  ever exist.
- The first real live two-connection test (Editor host + standalone
  exe, localhost) found and fixed two rounds of the same bug live: a
  remote player's body, then their worn equipment, both rendering
  invisible due to `WornEquipmentLayer`'s camera-cullingMask exclusion
  being global-per-camera rather than "hide my own stuff." Both
  confirmed working from the host's side (the standalone exe itself
  hasn't been rebuilt with the fix — a known, non-urgent asymmetry, not
  a bug).
- NPC Talk/Freeze now route through real Commands, with `NPCDialogue
  .isTalking`/`NPCFreeze.IsFrozen` converted to this project's first
  `[SyncVar]`s so the fix actually displays correctly on the calling
  client, not just the server. Live-confirmed.

**Chunk 7 — the real live test with traskmi, from two separate physical
locations — actually ran, 2026-08-26.** Connecting itself took real
troubleshooting (a stray-text bug in the Connect screen's IP field, then
a literal comma-vs-period typo in the address — neither one a networking
problem at all, just worth remembering to check the exact error string
before chasing router/firewall/CGNAT theories). Once connected, the test
did exactly what it was for: **found a real gap Phase 3 had missed,
same night fixed** (v0.3.205-dev). Admin-spawning, dropping, and
renaming all ran as plain local method calls with no `[Command]`/
`[SyncVar]` — full root-cause detail is in `CHANGELOG.md`'s v0.3.205-dev
entry.

**A second live session the following night (2026-08-27/28) found 11
more instances of the same root shape** (server-authoritative state
never converted to sync back to the owning client) across Inventory,
Team-name, Skills, Magic, Equipment, tree-chopping, and creature death/
skinning — all fixed same night, `CHANGELOG.md`'s v0.3.207-dev entry has
full detail. Compile-verified only — **still needs a real live re-test
with traskmi** to confirm the client side now actually replicates
correctly, the same two-machine scenario that found every one of these
bugs in the first place.

**Update, 2026-08-31 — the real live two-machine re-test happened
(Editor host + a freshly rebuilt standalone `Gridless.exe`, not just
host-alone).** Confirmed v0.3.207-dev through v0.3.213-dev holds, then
found and fixed a full night's worth more, all merged to `origin/main`
as of v0.3.225-dev: `MULTIPLAYER_INTERACTION_AUDIT.md`'s punch-list
items 1/3/4/5 (CancelCraft refund, StorageBox re-placement, the whole
equippable pickup/drop/equip-from-nested-container family,
VendorStall/VillageVendor), plus three real bugs the standalone-client
test surfaced that weren't on any prior list: 90 prefabs missing from
`NetworkManager`'s spawnable-prefab registration (including `Boulder`/
`BerryBush`/`HerbBush`/`Tree` themselves, not just their chunk chains),
`PlayerInventory`/`Backpack`/`StorageBox` never syncing equipment-backed
inventory slots to a remote client at all, and a same-night regression
in that fix (fought local-only moves every frame instead of the proven
additive-delta approach). Also escalated item 2 from MVP5-deferred to a
narrow live fix (moving into/out of a `StorageBox` is now
Command-routed) once testing showed it was actively desyncing state,
not just staying invisible. All live-tested and confirmed working by
Ben during the session, except the very last item-2 fix — not yet
re-verified live before testing stopped for the night.

**Next up:** live-confirm the item-2 StorageBox-move fix (last change
of the night, unverified). Then: `InventoryTransfer.Move`'s other still-
unrouted callers (`FurnaceScreen`/`CampfireScreen`/`NPCHiringScreen`,
deferred to MVP5), and the bigger confirmed-but-unstarted gap: NPCs
aren't real networked objects at all (`NPCFactoryWorkerFemale`/`Male`
prefabs carry zero `NetworkIdentity`) — `MULTIPLAYER_PLANNING.md`'s own
"NPCs move server-side" phase, never begun. See
`MULTIPLAYER_INTERACTION_AUDIT.md` for full current status of every
item.

## Open backlog, not currently being worked

See `BUGS_AND_ENHANCEMENTS.md` for full detail on each. Highlights as
of 2026-08-25: `ReachableInventories`' Belt/Shirt/Jeans gap (4 systems),
Nail's icon rendering as a Hammer, an unconfirmed Nail/Copper Nail
stack-cap-at-10 report, a stuck hold-progress-bar bug from 2026-08-23
(still unreproduced), and tool tiers giving no functional benefit within
their own class.
