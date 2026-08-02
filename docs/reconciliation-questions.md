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
