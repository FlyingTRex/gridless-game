# Medical Item Families

Companion to `MEDICAL_SYSTEM_PLANNING.md` (read that first for the full
evaluation — this document is just the family-grouping breakdown pulled out
on its own, 2026-08-12, since it's the piece most likely to get referenced
and revised on its own as actual items get built).

**The core decision this document implements:** where a proposed medical
item recurs across tiers as "the same purpose, better version," it should
be one base `ItemDefinition` with a real `CraftTier` ladder
(`lowerTierItem`/`higherTierItem`, same pattern Knife/Axe/Pickaxe/Hammer/
Backpack/Lockbox already use) rather than 5 separately-named items that all
coexist and get crafted independently. Ben's framing: using a Bandage gives
a smaller health benefit, using a Gauze Roll (its next tier up) gives a
better one — the tier *is* the progression, not a separate item choice.

Not every item collapses into a family this way — some are genuinely
distinct effects, not a better-made version of something else in the list.
Those stay standalone below.

Source list: the 50-item proposal evaluated in `MEDICAL_SYSTEM_PLANNING.md`
("Medical Items and Medicines Progression System," Ben, 2026-08-12).

## Clear families — Part 2 (Gear & Tools)

| Family | T1 (Crude) | T2 (Rudimentary) | T3 (Normal) | T4 (Fine) | T5 (Masterwork) |
|---|---|---|---|---|---|
| Wound dressing | Cloth Bandage *(exists — `Bandage.asset`)* | Gauze Roll | Military Trauma Dressing | — | — |
| Limb immobilization | Splint Stick | — | Aluminum Molded Splint | — | Exo-Skeleton Trauma Brace *(thematic stretch — "immobilize" becomes "mechanically support")* |
| Bleeding control (mechanical) | Makeshift Tourniquet | — | Combat Tourniquet (CAT) | — | — |
| Wound closure | Bone Needle & Thread | — | Suture Kit | Surgical Scalpel Set *(adjacent — access/removal, arguable fit)* | Laser Cauterizer |
| Sling/joint support | Soiled Sling | Basic Sling & Brace | — | — | — |

## Clear families — Part 1 (Consumable Medicines)

| Family | T1 (Crude) | T2 (Rudimentary) | T3 (Normal) | T4 (Fine) | T5 (Masterwork) |
|---|---|---|---|---|---|
| Burn treatment | — | Wild Salve | Burn Gel | — | Bio-Synth Skin Graft |
| Pain relief | Bitter Root Tea | Crushed Bark Extract *(Poppy Milk is a side-grade here, not a ladder rung — see note)* | Synthetic Painkillers | — | Neural Stim-Patch |
| Bleeding/vessel repair | Sap Sealant | — | Coagulant Powder | — | Nanobot Bloodstream Swarm *(multi-purpose top rung)* |
| Infection control | Antiseptic Wash | Fermented Tonic | Broad-Spectrum Antibiotics | Broad-Spectrum Antiviral | Universal Antidote Matrix |
| General restoration | `HealingPaste` *(exists, currently single-tier)* / Poultice Slurry | Nutrient Mash | — | — | Rejuvenation Stasis Fluid |

**Note on Poppy Milk:** trades a drowsiness downside for stronger pain
relief at the *same* tier tier Crushed Bark Extract occupies — a
side-grade choice within a tier, not a ladder rung above or below it.
Worth keeping as a distinct item alongside the Pain Relief family rather
than folding it in, once the family becomes a real ladder.

**Infection control is the cleanest full 5-rung family in the entire
list** — worth being the first family built end-to-end as a template for
the others, if/when this gets built.

## No clear family — standalone items

These have no same-purpose sibling elsewhere in the 50-item list. Not a
verdict on whether to build them — just flagging that they don't fit a
ladder the way the families above do, so each is a one-off `ItemDefinition`
decision on its own:

Herbal Stimulant, Trauma Adrenaline (Epi-Pen), Immune Booster Serum,
Radiation Flush, Neuro-Regen Ampoule, Adhesive Medical Tape, First-Aid
Scissors, Over-the-Counter Ice Pack, Burn Blanket, Automated Defibrillator
(AED), Portable IV Fluid Bag, Pneumatic Chest Seal, Handheld Bio-Scanner
*(candidate to extend the existing `HealthMonitorItem`/`PersonalHealthMonitor`
rather than build new — see `MEDICAL_SYSTEM_PLANNING.md`)*, Auto-Injector
Rig, Portable Med-Pod.

## Open gaps in the families above

Several families are missing a rung at one or more tiers (marked `—`
above) — not necessarily a problem (a family doesn't have to span all 5
tiers to be real), but worth a deliberate look before building:

- Should missing rungs be filled with new items, or is a gap intentional
  (e.g. no invented Tier 4 Wound Dressing item forced into existence just
  to complete the grid)?
- Limb Immobilization's Tier 5 rung (Exo-Skeleton Trauma Brace) is a real
  thematic jump from Aluminum Molded Splint — confirm that's wanted before
  building, not just carried over because it filled a gap.

## Practical build note

Heal-amount scaling per tier needs its **own** dedicated scale table once
any of this gets built — not a reuse of `CraftTierScale.Modifier`
(capacity/price) or `WeightModifier`. Same lesson already documented in
`CLAUDE.md`'s tier-scaling gotcha (reusing `Modifier`'s 25x capacity/price
spread for Encumbrance's weight produced a nonsensical 25lb Crude
Backpack) — a heal-amount table needs its own from-scratch numbers,
sanity-checked in real units, same as `WeightModifier` was built.
