using UnityEngine;

// NPC-side carry capacity (2026-08-10, Chunk 3 of the Hireable NPCs build
// -- see BUGS_AND_ENHANCEMENTS.md). Same capacity curve as
// PlayerEncumbrance (Capacity = 17.3925 x Strength^1.5, Strength on the
// .25-10 displayed scale) -- duplicated as a constant here rather than
// referenced, since PlayerEncumbrance's copies are private; keep the two
// numbers in sync if either ever changes.
//
// CarriedWeight is computed from NPCCargo's real Inventory (Chunk 4), same
// pattern PlayerEncumbrance.ComputeCarriedWeight uses for the player,
// rather than a manually-incremented number -- keeps weight and actual
// carried items impossible to drift out of sync with each other. Unlike
// the player, an NPC never grows Strength from load here either (no
// TickStrengthGrowth) -- Chunk 4's mining loop trains Strength directly
// through the job's own skill-gain call instead.
[RequireComponent(typeof(NPCSkills))]
[RequireComponent(typeof(NPCCargo))]
public class NPCEncumbrance : MonoBehaviour
{
    private const float ExpExponent = 1.5f;
    private const float ExpCoefficient = 17.3925f;

    [SerializeField] private SkillDefinition strengthSkill;

    private NPCSkills skills;
    private NPCCargo cargo;

    public float CarriedWeight { get; private set; }
    public float Capacity { get; private set; }
    public float LoadRatio => Capacity > 0f ? CarriedWeight / Capacity : 0f;

    private void Awake()
    {
        skills = GetComponent<NPCSkills>();
        cargo = GetComponent<NPCCargo>();
    }

    private void Update()
    {
        float strength = skills.GetAttributeValue(strengthSkill);
        Capacity = ExpCoefficient * Mathf.Pow(strength, ExpExponent);
        CarriedWeight = ComputeCarriedWeight();
    }

    // Never picks up past 80% loaded -- reuses PlayerEncumbrance's own
    // BetterGainThreshold constant directly rather than a new NPC-only
    // number, so "encumbered" means the same thing for player and NPC
    // (Ben's explicit call, 2026-08-10). Deliberately stricter than the
    // player's own pickup gate (blocked at/over 100% capacity) -- an NPC
    // always keeps a buffer instead of maxing out.
    public bool CanPickUp(float weight) =>
        Capacity > 0f && (CarriedWeight + weight) <= Capacity * PlayerEncumbrance.BetterGainThreshold;

    private float ComputeCarriedWeight()
    {
        float total = 0f;
        foreach (var slot in cargo.Inventory.Slots)
            if (slot.item != null) total += slot.item.weight * slot.count;
        return total;
    }
}
