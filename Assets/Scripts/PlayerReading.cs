using UnityEngine;

// Reading half of the skill-books mechanic (SKILL_BOOKS_PLANNING.md
// Phase 3) — consumes a written SkillBook and applies whatever it was
// written to grant. A SkillBook is equipment-backed (a real physical
// instance, not a plain stackable item), so this is triggered from
// InventoryScreen's pendingActionEquipment branch, same shape Canteen's
// Drink/Fill buttons already use there — not FindEdible-style item
// lookup, which only works for plain ItemDefinition-only consumables.
[RequireComponent(typeof(PlayerCrafting))]
[RequireComponent(typeof(PlayerMagic))]
[RequireComponent(typeof(PlayerSkills))]
public class PlayerReading : MonoBehaviour
{
    [SerializeField] private SkillDefinition intelligenceSkill;

    // Reading trains Intelligence too (SKILL_BOOKS_PLANNING.md's
    // Foundation section) — smaller than any of writing's own gains,
    // since reading is the passive half of the loop. Only paid out when
    // the read actually teaches the reader something new — otherwise
    // reading your own self-authored book (always already-known content,
    // since writing itself requires already knowing the subject) or a
    // duplicate found book double-dips for free. Found live 2026-08-14
    // (Ben), see BUGS_AND_ENHANCEMENTS.md's now-closed entry.
    private const float ReadIntelligenceGain = 0.25f;

    private PlayerCrafting crafting;
    private PlayerMagic magic;
    private PlayerSkills skills;

    private void Awake()
    {
        crafting = GetComponent<PlayerCrafting>();
        magic = GetComponent<PlayerMagic>();
        skills = GetComponent<PlayerSkills>();
    }

    // source is whichever Inventory the book is actually sitting in (main
    // inventory, a worn Backpack, ...) — same "don't assume main
    // inventory" discipline every other equipment-aware action in
    // InventoryScreen already follows. Consumed permanently on read, same
    // as a Scroll — not stashed/returned, the physical instance is
    // destroyed.
    public bool TryRead(Inventory source, SkillBook book)
    {
        if (source == null || book == null) return false;
        if (book.TargetRecipe == null && book.TargetWish == null) return false;

        // Checked before granting anything below — granting the recipe/
        // wish would make these true unconditionally, hiding the "was
        // this already known" answer this Intelligence gain depends on.
        bool alreadyKnown = book.TargetRecipe != null
            ? crafting.HasRequiredSkill(book.TargetRecipe)
            : magic.IsWishUsable(book.TargetWish);

        if (book.TargetRecipe != null)
        {
            crafting.GrantRecipe(book.TargetRecipe);
        }
        else
        {
            // Order matters: LearnLineage first (a no-op if already
            // known) so GrantWish's own gate has a known lineage to
            // register against — CanAttempt still separately requires
            // IsLineageKnown even with the wish exception granted.
            magic.LearnLineage(book.TargetWish.lineage, book.BonusLevel);
            magic.GrantWish(book.TargetWish);
        }

        if (!alreadyKnown)
            skills.GainExperience(intelligenceSkill, ReadIntelligenceGain);

        source.RemoveEquipmentItem(book.ItemDefinition);
        Destroy(book.gameObject);
        return true;
    }
}
