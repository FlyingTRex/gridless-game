# NPC Training Planning

**Status: built, v0.3.102-dev (2026-08-16)** — see `CHANGELOG.md`'s
v0.3.102-dev entry for the built shape. Design below was written and
decision-locked earlier the same day, then built as written; left in
present/future tense rather than rewritten past-tense, same convention
`NPC_JOB_GENERALIZATION_PLANNING.md` uses for its own built sections.
Section 6's open items (exact Fame amount, interrupted-training refund
question) were resolved at build time: Fame gain is `PlayerFame.
GrantNpcTraining()` (+0.25), and an interrupted training loses the
consumed book (not refunded) — see `NPCHiring.Fire`/`NPCTraining.
CancelTraining`.

Planning doc for training hired NPCs via skill books (2026-08-16). Designed
conversationally with Ben across several rounds — this is the write-up of
that conversation, decision-locked.

**Supersedes `SKILL_BOOKS_PLANNING.md`'s existing Phase 4 stub.** That
stub (written 2026-08-13, before this conversation) sketched a much
simpler "give NPC a book" hand-over interaction, instant, with magic
books fully excluded. This design replaces it entirely: a real Desk/
Bookshelf-based ritual with a 2-minute wait, and magic books included
(banked inertly — see below). `SKILL_BOOKS_PLANNING.md` should be updated
to point here rather than describe the old shape.

## 1. Why now

This closes the loop skill books have been missing since they shipped:
writing a crafting/weapon book has had nothing to actually consume it
except the player's own one-time read. Training gives an ongoing reason
to keep writing books — and it's the direct payoff for
`NPC_JOB_GENERALIZATION_PLANNING.md` section 7's bench-crafting design
(built the same session, a few hours earlier): a trained NPC's granted
recipe feeds straight into `NPCCrafting`'s queue.

## 2. The two new Build pieces

- **Desk** — a real functional piece, not decoration. When training is
  triggered, the NPC walks to the nearest Desk and spends the training
  duration there (mirrors `NPCCrafting`'s walk-to-`AnvilSurface`/
  `FurnaceSurface` shape from section 7, and `NPCGathering`'s walk-to-
  target shape before that — same "nearest qualifying thing in range"
  scan pattern this project already uses three times).
- **Bookshelf** — a restricted storage box (same `Inventory(capacity,
  restrictedTo)` shape every other restricted container in this project
  uses) holding real `SkillBook` instances, not just a count. **Needs an
  auto-populated allowed-items list**, not a hand-maintained one — a
  flat `ItemDefinition[]` restriction (`Inventory`'s only restriction
  mechanism today) would silently miss any new skill book type the same
  way `EFFICIENCY_AUDIT.md` already flagged for `ItemDatabase` et al.
  Populate via the same `AssetDatabase.FindAssets` + "does its
  `worldPickupPrefab` have a `SkillBook` component" scan
  `DatabaseRepopulator.cs` already established, not by hand.

Both new `BuildPiece`s, same Build-tab flow every other structure uses.
Exact recipes not specced here (out of scope for this doc — just need to
exist; low-tier materials, matching how Campfire/Garden Plot were both
"an early, low-skill unlock").

## 3. Training flow

1. Player opens an NPC's screen (extends `NPCJobScreen`, the existing
   per-NPC management UI, with a new Training section — same "add a
   section to the screen that already manages this NPC" pattern the
   crafting-queue UI from section 7.5 already uses) and hits Training.
2. The book picker reads from **two pools**: the Bookshelf's current
   contents *and* the player's own inventory — shelving a book first
   isn't required, the shelf is just wherever spare books happen to
   live. **Decided (Ben, 2026-08-16): explicitly not restricted to
   crafting/weapon books — magic (`WishRecipe`-targeting) books are
   included too**, see section 5 for what training with one actually
   does.
3. Player picks a book. **That specific `SkillBook` instance is consumed
   immediately** — removed from wherever it came from (Bookshelf's
   `Inventory` via `RemoveEquipmentItem`, or the player's own inventory
   the same way) the moment training starts, not on completion. Matches
   every other "consume upfront, not on completion" convention already
   established (`PlayerCrafting.StartCraft`, `Campfire.StartCooking`,
   section 7.3's own `NPCCrafting` loop).
4. The NPC pauses whatever it was doing (same `SetPaused` mechanism
   `NPCDialogue` already uses on `NPCWander`/`NPCGathering` — needs
   extending to also pause `NPCCrafting` once that exists) and walks to
   the nearest Desk.
5. Waits **2 real minutes** (`Ben's number, confirmed 2026-08-16`) —
   real-time, matching every other NPC/structure timer in this project
   (`NPCHiring`'s work-shift timer, `Furnace`'s burn timer) rather than
   an instant resolve.
6. On completion (section 5 for the actual grant), a small Fame gain
   (amount TBD — small, same "repeatable action, not a one-time
   milestone" scale as Hire's own +1, likely smaller than that since
   this can happen far more often once a player has several NPCs and a
   steady book supply — not locked to a specific number yet).
7. NPC un-pauses, resumes whatever job it was doing before.

## 4. Satisfiability / edge cases

- **Book already granted**: if the NPC already has this exact recipe/
  lineage, the book shouldn't be offered as a training option at all
  (mirrors `PlayerCrafting.GrantRecipe`'s own set — granting twice is a
  no-op there, but wasting a real, now-consumed book on a redundant
  grant reads as a real player-facing footgun worth preventing upfront,
  not just silently absorbing).
- **NPC reassigned/fired mid-training**: same "bail out cleanly" pattern
  `NPCGathering.UpdateReturning` already uses when its deposit box goes
  null mid-walk — training should cancel cleanly rather than softlock,
  though whether the consumed book is lost or refunded isn't decided
  here (leaning toward lost, matching the "materials consumed upfront,
  not refunded on interruption" convention everywhere else in this
  project, but flagging as a real open question, not a silent default).
- **No Desk in range**: same "can't proceed" outcome as a Metalworking
  NPC with no Anvil/Furnace in range (section 7.3) — training simply
  can't start, surfaced in the UI rather than failing silently.

## 5. What training actually grants

- **Crafting/weapon books** (`SkillBook.TargetRecipe` set): grants the
  recipe exception, mirroring `PlayerCrafting.GrantRecipe` exactly but
  on the NPC's own side (a new `NPCJob`- or `NPCCrafting`-owned granted-
  recipes set, parallel to `PlayerCrafting.bookGrantedRecipes`). This is
  the functional payoff — the granted recipe becomes queueable on
  `NPCCrafting` (section 7.4's satisfiability check needs one more
  `|| npcGrantedRecipes.Contains(recipe)` clause, same shape
  `PlayerCrafting.HasRequiredSkill` already has).
- **Magic books** (`SkillBook.TargetWish` set): **banked inertly.**
  Decided explicitly (Ben, 2026-08-16): NPCs have no spellcasting system
  at all today, so there's nothing for a granted lineage/wish to *do*
  yet. Store it anyway — a small `knownLineages`-shaped set on the NPC,
  same shape `PlayerMagic.knownLineages` already has, just with zero
  current readers. Ben's framing: "as we explore magic later, we may
  give some interesting magic abilities that enhance the NPC's
  abilities" — this is explicit forward-compatibility, not a stub to be
  embarrassed about. The book still gets consumed and Fame still ticks
  up even though nothing reads the grant yet — training a magic book
  isn't wasted, it just doesn't unlock a new NPC capability today.

## 6. Explicitly out of scope for this pass

- Actual Desk/Bookshelf recipes and Build-tab tier placement (low-skill
  unlock, exact materials not chosen here).
- The Fame amount per training (small, not numbered yet).
- Whether an interrupted training refunds or loses the consumed book
  (leaning lost, not locked in).
- Any NPC magic-ability system that would ever actually read the banked
  lineage grants — a real future piece, not touched here.

## Cross-references

- `NPC_JOB_GENERALIZATION_PLANNING.md` section 7 — `NPCCrafting`, the
  direct consumer of a crafting-book training grant.
- `SKILL_BOOKS_PLANNING.md` — `SkillBook.cs`'s existing shape (this doc
  reuses it as-is, no changes needed to the book itself), and the Phase
  4 stub this design supersedes.
- `EFFICIENCY_AUDIT.md` item 1 — the registration-array risk the
  Bookshelf's allowed-items list needs to avoid via auto-population.
- `FAME_PLANNING.md` — the Fame gain this grants into.
