using UnityEngine;

// First Prey Creature (2026-08-15, "let's add [Feather/Egg] to the
// chicken loot table... when we kill a chicken, we get crafting
// materials") — killable and lootable, same tool-gated hold-to-skin
// death/respawn shape HostileCreature already proved out for the Wolf,
// deliberately stripped of everything aggressive (no detection/chase/
// attack state machine). This is NOT yet the full Prey Creature
// archetype the Hunting Expansion design calls for (idle/wander until
// approached, then flee) — that behavior still doesn't exist. Built
// generic/reusable rather than Chicken-specific so Pig/Deer/Rabbit can
// use the same component later; only the flee movement is still missing.
//
// Death/skin/respawn lifecycle lives in SkinnableCreature (shared with
// HostileCreature, 2026-08-15 efficiency pass) — this class only adds its
// own LootA+LootB loot table.
public class PreyCreature : SkinnableCreature
{
    [SerializeField] private ItemDefinition lootItemA;
    [SerializeField] private int lootAMinCount = 1;
    [SerializeField] private int lootAMaxCount = 1;
    [SerializeField, Range(0f, 1f)] private float lootADropChance = 1f;

    [SerializeField] private ItemDefinition lootItemB;
    [SerializeField] private int lootBMinCount = 1;
    [SerializeField] private int lootBMaxCount = 1;
    [SerializeField, Range(0f, 1f)] private float lootBDropChance = 1f;

    protected override void DropLoot(PlayerDropping dropping)
    {
        if (lootItemA != null && Random.value < lootADropChance)
            dropping?.SpawnPickup(lootItemA, Random.Range(lootAMinCount, lootAMaxCount + 1));
        if (lootItemB != null && Random.value < lootBDropChance)
            dropping?.SpawnPickup(lootItemB, Random.Range(lootBMinCount, lootBMaxCount + 1));
    }
}
