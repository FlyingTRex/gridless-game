# Medical Items & Medicines Progression — Evaluation

Ben's proposed 50-item medical system (2026-08-12): 5 tiers of consumable
medicines + 5 tiers of medical gear/tools, "Primitive & Foraged" through
"High-Tech & Experimental." This document evaluates it against what's
actually in the game today and lays out per-item build needs. Planning
only — nothing here is built or scoped into a version yet.

**Companion document: `MEDICAL_FAMILIES.md`** — which of these 50 items
collapse into one `CraftTier`-ladder item family (Bandage → Gauze Roll →
Military Trauma Dressing, etc.) vs. stay standalone. Split out on its own
since it's the piece most likely to get revised as real items get built.

## Verdict

**As a content list, this is genuinely comprehensive** — it covers a wide,
coherent spread of real trauma-medicine concepts (bleeding, infection,
pain, burns, fractures, shock, chest trauma, radiation, nerve damage) with
no obvious gaps in *kind* of injury addressed. It reads as a well-researched
progression, not a random item dump.

**One structural problem resolved, one still open:**

1. **RESOLVED (2026-08-12, Ben's call): the list's 5 tiers map directly
   onto `CraftTier`** — Tier 1→Crude, Tier 2→Rudimentary, Tier 3→Normal,
   Tier 4→Fine, Tier 5→Masterwork. No new field. This is a looser use of
   `CraftTier` than the Knife/Axe/Backpack-style same-item ladders (each
   list-tier is a *different* item, not the same item crafted better) —
   but it matches how single-tier items like `WolfPelt.asset` (`tier: 2`,
   no ladder siblings) already use the field as a general
   progression/rarity marker, not exclusively for formal 5-rung ladders.
   **Real side benefit, not just a naming fix:** `CraftTierScale.
   SkillRequirement(tier)` already gates crafting behind a Medicine skill
   level (Crude=0, Rudimentary=10, Normal=25, Fine=50, Masterwork=100) —
   this mapping means that pacing mechanism just works immediately, no new
   gating system needed. Tier 1 items are craftable from Medicine level 0;
   Tier 5 needs Medicine mastery.
   **Still worth deciding separately, not implied by this mapping:** if a
   later request wants a *true* skill-quality ladder on top of this (e.g. a
   Crude vs. Masterwork version of the same Suture Kit), that's a second,
   independent axis this mapping doesn't provide — `CraftTier` would then
   be doing double duty on that specific item, which is fine as long as
   it's a deliberate choice, not an accident.
2. **RESOLVED 2026-08-12, reversed from the first pass — Tiers 4-5 fit
   the setting after all, via the endgame.** Missed on the first pass:
   `docs/design-brief.md`'s **"Endgame: Leaving the Planet"** section
   (line 538) already specs reaching any one of 8 discipline Keystones —
   **Master Physician is one of them** — as revealing the **"Ruins of the
   Old Engineers,"** the advanced civilization's own launch complex, as the
   gateway to actually escaping the planet. Ben's framing (2026-08-12):
   if the game's real end state is "build a spaceship home," advanced
   Ruins-sourced tech isn't a departure, it's the whole point of reaching
   that point. Tier 4-5 medical items read naturally as **Master-Physician
   endgame content**, not early-game crafting. This also lines up cleanly
   with the `CraftTier` mapping from item 1 above — Masterwork already
   requires Medicine skill level 100, a believable "Master Physician" bar,
   so the existing skill-gate alone keeps this content late-game without
   extra work.
   **Still worth deciding, not automatically implied:** whether Tier 4-5
   items should be gated specifically behind *discovering the Ruins*
   (a real unlock-gate, per the endgame spec) in addition to the Medicine
   skill-level gate, or whether skill level 100 alone is sufficient friction
   for now given the Ruins/endgame system itself isn't built yet.

**Recommendation, not a decision:** treat Tiers 1-3 as the near-term,
immediately-buildable medical progression (foraged remedies through
WWI/WWII-era
industrial/pharmaceutical medicine — disinfectant alcohol, antibiotics,
coagulant powder, suture kits, AEDs are all plausible "scavenged from the
crashed colonist ship's medbay" content, consistent with the
pre-industrial-village-plus-crashed-modern-colonist premise) as the
near-term build target. Treat Tiers 4-5 as real, wanted **endgame** content
tied to the Master Physician Keystone/Ruins of the Old Engineers — build
them later, alongside (or gated by) that system, rather than in the same
pass as Tiers 1-3.

**Third, smaller problem — no damage types exist yet.** `PlayerVitals`
has one `Health` value and `PlayerCombat` does flat damage; there's no
bleeding/infection/burn/fracture state anywhere in the code (confirmed via
grep — zero matches). Most of this list's differentiation is *flavor text*
about what kind of trauma each item treats — without matching damage-type
mechanics, every item mechanically reduces to the same heal-over-time
`MedicineItem.healAmount`/`healDuration` pair `HealingPaste`/`Bandage`
already use, just with a different name and picture. Actually
distinguishing "stops arterial bleeding" from "reduces fever" needs the
underlying status-effect system built first (or alongside) — this is
probably the single biggest piece of new work implied by this list, well
beyond building 50 items.

## Current state (confirmed, not assumed)

- `PlayerVitals.cs` — passive health regen deliberately nerfed 20x
  (2026-08-10) specifically so first aid would matter; `StartHealOverTime`
  is the one mechanism both `PlayerMedicine` and the Restoration magic
  wish use. No damage-type fields.
- `PlayerMedicine.cs` + `MedicineItem.cs` — mirrors `PlayerEating`/
  `EdibleItem`. A `MedicineItem` asset is just `item` + `consumeCount` +
  `healAmount` + `healDuration` + `verb`. No skill-gating, no
  cure-a-specific-status fields.
- Exactly **two** medicine items exist: `HealingPaste.asset`
  (`healAmount: 10`, `healDuration: 10`) and `Bandage.asset`
  (`healAmount: 15`, `healDuration: 10`), both `tier: 0`, both **single-tier**
  (no Crude→Masterwork ladder, unlike Knife/Axe/Backpack/Lockbox which all
  have real 5-rung ladders).
- `Medicine` already exists as a `SkillDefinition` (a `CraftingDiscipline`
  category, same shape as Woodworking/Stonework/Forging/Sewing/Gathering)
  — a real hook for skill-gating a genuine progression, currently unused
  for that (today's two recipes just train it, nothing is gated behind a
  level).
- `Herb.asset` + a dedicated Herb Bush gather node already exist and
  currently feed `HealingPasteRecipe` (3× Herb + Canteen water) — a
  natural ingredient source for Tier 1-2 items on this list.
- No `Splint`/`Tourniquet`/`Antiseptic`/gear-category items exist at all —
  the entire "Medical Gear & Tools" half of this list (bandages aside) is
  net-new territory, not an extension of something partially built.
- `HealthMonitorItem.asset` ("Personal Health Monitor") already exists as
  a wearable gadget item, craftable, but is cosmetic/placeholder — no
  actual vitals hookup. Worth reusing/extending for this list's
  "Handheld Bio-Scanner" (Tier 4 gear) if that tier ships, rather than
  building a second, separate scanner item.

## Per-item breakdown

Legend — **New?**: everything is new except the two marked *(existing)*.
**Model?**: whether it plausibly needs its own 3D world-pickup model (Y),
could ship as a data-only consumable with just an icon and no distinct
world model (icon-only), or could reuse an existing model. **Depends on**:
what doesn't exist yet that this item's *described effect* needs to be
more than a reskinned heal-over-time number.

### Part 1 — Consumable Medicines

| Item | Tier | Model? | Depends on |
|---|---|---|---|
| Poultice Slurry | 1 | icon-only | none beyond existing HoT pattern |
| Bitter Root Tea | 1 | icon-only | a "fever" status, or ships as flavor-only HoT |
| Sap Sealant | 1 | icon-only | a "bleeding/wound" status to actually seal |
| Antiseptic Wash | 1 | icon-only | an "infection risk" status to prevent |
| Herbal Stimulant | 1 | icon-only | stamina-restore-at-health-cost is new (inverse of normal heal) |
| Crushed Bark Extract | 2 | icon-only | fever/pain status |
| Wild Salve | 2 | icon-only | a "burn" damage type |
| Fermented Tonic | 2 | icon-only | infection status |
| Poppy Milk | 2 | icon-only | pain status + a drowsiness/debuff side effect (new mechanic) |
| Nutrient Mash | 2 | icon-only | none — this is exactly `HealingPaste`'s existing shape |
| Disinfectant Alcohol | 3 | icon-only | infection status; also a plausible tool-cleaning use (Suture Kit synergy) |
| Broad-Spectrum Antibiotics | 3 | icon-only | infection status w/ severity levels ("medium infections") |
| Synthetic Painkillers | 3 | icon-only | pain status, notably *without* the Poppy Milk drowsiness side effect — needs the status system to support both |
| Coagulant Powder | 3 | icon-only | bleeding status w/ severity ("severe arterial") |
| Burn Gel | 3 | icon-only | burn status |
| Broad-Spectrum Antiviral | 4 | icon-only | a distinct "viral infection" status, separate from bacterial |
| Cellular Regenerator | 4 | Y (injector prop?) | "deep tissue tear" — a wound-severity concept beyond flat HP |
| Trauma Adrenaline (Epi-Pen) | 4 | Y (recognizable injector) | a "shock" status effect (new) |
| Immune Booster Serum | 4 | icon-only | a temporary-immunity buff (new buff category, not just heal) |
| Radiation Flush | 4 | icon-only | **radiation doesn't exist as a mechanic in this game at all** — no source of rad damage to flush |
| Nanobot Bloodstream Swarm | 5 | Y | setting fit (see Verdict); real-time multi-status cure is a big new system |
| Bio-Synth Skin Graft | 5 | icon-only or spray prop | "third-degree burn" severity tiering |
| Neuro-Regen Ampoule | 5 | Y (injector) | nerve-damage/paralysis status (doesn't exist) |
| Universal Antidote Matrix | 5 | icon-only | a generic "toxin/poison" status category (doesn't exist) |
| Rejuvenation Stasis Fluid | 5 | Y (distinctive) | "near-death trauma" as a distinct state from low HP; setting fit |

### Part 2 — Medical Gear & Tools

| Item | Tier | Model? | Depends on |
|---|---|---|---|
| Cloth Bandage | 1 | *(existing — `Bandage.asset`)* | already shipped |
| Splint Stick | 1 | Y | a "fracture/broken limb" status (doesn't exist) |
| Makeshift Tourniquet | 1 | Y | bleeding-severity status + a limb-specific model (blood flow "cut off") |
| Bone Needle & Thread | 1 | Y | "laceration" as a treatable wound type, distinct from generic HP loss |
| Soiled Sling | 1 | Y | shoulder/arm injury status |
| Gauze Roll | 2 | Y (or reuse Bandage model) | same dependency as Bandage — likely a tier-ladder sibling of it, not a new item |
| Adhesive Medical Tape | 2 | icon-only | supporting item for Splint/dressing application, not standalone-useful without those |
| First-Aid Scissors | 2 | Y (tool) | mostly flavor/access (cut clothing) — lowest-dependency gear item on the list |
| Over-the-Counter Ice Pack | 2 | Y | swelling/bruising status (doesn't exist) |
| Basic Sling & Brace | 2 | Y | joint-sprain status |
| Military Trauma Dressing | 3 | Y | bleeding-severity status |
| Suture Kit | 3 | Y (multi-part prop) | laceration status; pairs naturally with Bone Needle & Thread as its tier-3 upgrade |
| Aluminum Molded Splint | 3 | Y | fracture status; natural tier-3 sibling of Splint Stick |
| Combat Tourniquet (CAT) | 3 | Y | same as Makeshift Tourniquet, severity-scaled |
| Burn Blanket | 3 | Y | burn status |
| Automated Defibrillator (AED) | 4 | Y (distinctive) | a "cardiac arrest/death-adjacent" state — needs a real down-but-revivable state machine, not just low HP |
| Surgical Scalpel Set | 4 | Y | "embedded shrapnel" as a removable object state (new) |
| Portable IV Fluid Bag | 4 | Y | dehydration/blood-volume as tracked separately from `Thirst` (overlaps existing vital — needs a design call on whether it's the same stat or a new one) |
| Pneumatic Chest Seal | 4 | Y | "sucking chest wound"/pneumothorax status (new, fairly specific) |
| Handheld Bio-Scanner | 4 | Y — **could extend existing `PersonalHealthMonitor`/`HealthMonitorItem`** rather than building new | needs the other statuses above to exist first to have anything to scan for |
| Auto-Injector Rig | 5 | Y | setting fit; "automatically administers based on real-time vitals" implies an AI-driven consumption system — a real new subsystem |
| Laser Cauterizer | 5 | Y | setting fit (hard clash — directed-energy tool); bleeding status |
| Exo-Skeleton Trauma Brace | 5 | Y (large, distinctive) | setting fit (hard clash); "paralyzed or severely shattered limbs" — a mobility-impairment status that doesn't exist |
| Portable Med-Pod | 5 | Y (large prop) | setting fit; "robotic stitching arms" implies automation beyond a consumable — closer to a placeable structure than an item |
| Neural Stim-Patch | 5 | icon-only | setting fit (hard clash — "neurological level" pain blocking); pain status |

## Open questions before this becomes buildable

1. ~~Does "tier" need its own field, separate from `CraftTier`?~~
   **RESOLVED 2026-08-12 — no, map the list's 5 tiers directly onto
   `CraftTier` (Tier 1→Crude ... Tier 5→Masterwork).** See Verdict above.
2. ~~Tiers 4-5: cut, defer, or explicitly re-theme for the "advanced ruins"
   hook?~~ **RESOLVED 2026-08-12 — re-themed, not cut: Tiers 4-5 are
   Master Physician / Ruins-of-the-Old-Engineers endgame content** (see
   `docs/design-brief.md`'s "Endgame: Leaving the Planet" section), built
   later alongside that system rather than in the same pass as Tiers 1-3.
   Still open underneath this: whether they need an explicit Ruins-discovery
   gate in addition to the Medicine-skill-100 gate, or whether the skill
   gate alone is enough for now — see Verdict above.
3. **Does this project want a real status-effect system** (bleeding,
   infection, burns, fractures, shock, poison, as tracked, stacking,
   time-limited states) **before** building tiered medicine, or should the
   first pass of new items ship as reskinned `HealingPaste`-style flat
   heal-over-time (like today's 2 items) with status effects added later?
   Building the items first without the statuses means re-touching all of
   them again once statuses exist.
4. ~~Gauze Roll vs. Bandage, Splint Stick vs. Aluminum Molded Splint,
   Makeshift vs. Combat Tourniquet~~ **RESOLVED IN PRINCIPLE 2026-08-12 —
   yes: where an item family clearly recurs across tiers, it should be one
   base item with a real `CraftTier` ladder (`lowerTierItem`/
   `higherTierItem`, matching Knife/Axe/Backpack), not 5 separately-named,
   simultaneously-craftable items.** Ben's framing: using a Bandage gives a
   smaller health benefit, using a Gauze Roll (its next tier up) gives a
   better one — the tier *is* the progression, not a separate item choice.
   **Not every item collapses this way** — the actual family breakdown
   (which items group into one ladder, which stay standalone) now lives in
   its own document, `MEDICAL_FAMILIES.md`, since it's the piece most
   likely to get revised on its own as real items get built.
