# Reconciliation Questions — Design Brief vs. Game Overview

Context: two design docs grew independently — the "Flying T-Rex" design brief (world
scope, multiplayer architecture, City Growth, Settlement Warfare, phased MVP) and
this repo's `docs/game-overview.md` (setting, magic, factions/guilds, crafting).
They overlap in places and genuinely conflict in others. Before merging into one
canonical doc, we need answers on the following:

1. **Magic** — Is the magic system (Elemental/Illusion/Kinetic/Restoration
   lineages) meant to be a real, core part of the game, or was that more of a
   brainstorm/placeholder idea? If it's in, how central — does every player get a
   lineage, or is it rare/optional?

2. **Factions & Guilds vs. city capture** — Are player Factions the same thing as
   the "attacking group" in a city capture/destroy fight (see Settlement Warfare in
   the design brief), or two separate systems that both need to exist? Do Merchant
   Guilds control/own cities, or just operate businesses inside them regardless of
   who holds the city?

3. **Currency ladder** — `game-overview.md` has Copper → Iron → Silver → Gold; the
   design brief has Copper → Silver → Gold → Platinum. Do we merge into a 5-tier
   ladder with both Iron and Platinum, or pick one 4-tier version? If merging,
   where does Iron sit relative to Silver?

4. **Real Earth vs. replica** — Is the world literally real Earth (design brief
   plans IP-geolocating players to real upstate NY cities — Buffalo, Rochester,
   Syracuse, Albany), or is it a fictional replica with its own invented
   geography/city names? If it's a replica that mirrors real Earth's layout, are we
   okay still using real city names, or should they be renamed to make the "it's
   not actually Earth" twist land?

5. **Scope check** — With magic, factions, and guilds now in the picture, are we
   still aligned that Phase 1 (the first playable build) is just the solo
   survival-craft loop with no magic/factions/guilds yet — or does adding these
   systems change what counts as MVP?


Reconciliation Decisions — Design Brief vs. Game Overview

Resolves the open questions in reconciliation-questions.md. These decisions are now reflected in game-overview.md.

1. Magic

Decision: Core and universal. Every player is randomly assigned a magical lineage (Elemental, Illusion, Kinetic, Restoration) by the game — not optional, not rare.

2. Factions, Guilds & Settlement Warfare

Decision: Three separate systems, not one:

Factions = reputation/perception. How trusted or feared a player/group is, driven by behavior (safe productive settlements build trust; raiding erodes it).
Merchant Guilds = craft-skill bonuses and trade perks. Not territorial — guild benefits apply regardless of who controls the surrounding settlement.
Warbands / Militias = the literal combatant groups in Settlement Warfare (city capture/destroy fights). Separate from reputation Factions, though a Warband's conduct can affect the Faction standing of players associated with it.
3. Currency Ladder

Decision: 5-tier ladder, merging both docs' versions: Copper → Iron (×10) → Silver (×10) → Gold (×10) → Platinum (×10)

4. World & City Names

Decision: Replica Earth. Real-world geography is preserved (settlements sit at their correct real-world locations), but city names are invented rather than using real names — this is what makes the "it's not actually Earth" reveal land. Present-day settlements are small, not full modern cities.

5. Phase 1 / MVP Scope

Decision: Solo survival-craft loop plus lightweight magic (lineage assignment, early-tier ability use). Factions, Guilds, Warbands, and Settlement Warfare are deferred to later phases — they need more players/world infrastructure to be meaningful.