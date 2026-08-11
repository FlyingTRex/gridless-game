using UnityEngine;

// Tracks total carried weight and capacity — see design-brief.md's
// Encumbrance section (2026-08-10). Capacity scales with the Strength
// SkillDefinition, grown via the same skill-via-use model as every other
// skill (PlayerSkills.GainExperience), just displayed on the Player tab
// instead of the Skills tab (see SkillCategory.Attribute).
//
// Only counts what's actually on the player's person — main inventory,
// every PlayerEquipment slot (including a worn Backpack's own item weight),
// and an equipped Backpack's contents. Deliberately NOT nearby StorageBox
// inventories, unlike PlayerBuilding/PlayerPieceUpgrade's ReachableInventories
// — a storage box sitting in the world isn't weight you're carrying.
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerEquipment))]
[RequireComponent(typeof(PlayerBackpack))]
[RequireComponent(typeof(PlayerSkills))]
[RequireComponent(typeof(PlayerVitals))]
public class PlayerEncumbrance : MonoBehaviour
{
    // Small-exponential curve (2026-08-10, Ben's call after reviewing the
    // linear-vs-curved comparison chart): Capacity = 17.3925 x Strength^1.5,
    // where Strength is the .25-10 displayed value (PlayerSkills.
    // GetAttributeValue), not the raw 0-100 skill level. Coefficient
    // derived from anchoring Strength 10.00 to exactly 550 lbs
    // (550 / 10^1.5). Deliberately front-loaded: at the starting Strength
    // of 2.00 this caps at ~49 lbs, not the ~110 lbs flat scaling would
    // give — Ben's intent is for inventory management and Strength to be
    // something an early-game player has to actively work around.
    private const float ExpExponent = 1.5f;
    private const float ExpCoefficient = 17.3925f;

    // Strength-from-load tiers (2026-08-10, Ben's call): no gain at all
    // below half capacity — carrying a light load teaches you nothing.
    // Gain climbs the closer to (and past) full capacity you push, until
    // it tips over into genuinely dangerous territory above 95%, where
    // gain backs off a little (still better than resting under it, per
    // "we lose a little strength gain") and sustained overload starts
    // costing health instead. Public — FirstPersonController's movement
    // speed tiers reuse these exact same breakpoints (2026-08-10, Ben's
    // call: "let's match the movement rates to strength rates") rather
    // than defining a second, driftable set of thresholds.
    public const float MarginalGainThreshold = 0.50f;
    public const float BetterGainThreshold = 0.80f;
    public const float MostGainThreshold = 0.90f;
    public const float OverloadThreshold = 0.95f;

    // Real-time-calibrated pacing (2026-08-10, Ben's call): "at a strength
    // of 2, it should take 2 actual days of playing to raise by .25" — a
    // wall-clock target, not a raw XP number picked by feel. Deliberately
    // NOT a second slowdown mechanism layered on top of Strength's level —
    // PlayerSkills.GainExperience's existing diminishing-returns curve
    // (newLevel = current + amount*(1-current/100)) already goes to
    // exactly zero at level 100 (Strength 10), which is precisely "gain
    // slows as Strength rises, and stops completely at max." All that was
    // ever needed was calibrating the raw per-second rate so that curve,
    // at today's starting Strength, matches the 2-day target — everything
    // beyond level 20 slows down further for free, same formula.
    //
    // Solved by treating GainExperience's per-frame recurrence as the
    // continuous ODE dL/dt = R*(1 - L/100), whose closed form is
    // L(t) = 100 - (100 - L0)*exp(-R*t/100). Anchored to the fastest
    // legitimate tier (Most Gain, 90-95% load) as "how fast Strength CAN
    // grow at best" — solving for R given L0=20 (Strength 2.00),
    // L(t)=22.5 (Strength 2.25), t=2 real days:
    private const float CalibrationStartLevel = 20f;    // Strength 2.00
    private const float CalibrationTargetLevel = 22.5f; // Strength 2.25 (+0.25)
    private const float CalibrationRealDays = 2f;

    // Ratios preserved from the original first-pass rates (0.5 / 1.5 /
    // 3.0 / 1.0 relative to a "most gain" of 3.0) — only the overall
    // magnitude changed, via SolveMostGainRate() below.
    private static readonly float MostGainPerSecond = SolveMostGainRate();
    private static readonly float BetterGainPerSecond = MostGainPerSecond / 2f;
    private static readonly float MarginalGainPerSecond = MostGainPerSecond / 6f;
    private static readonly float OverloadedGainPerSecond = MostGainPerSecond / 3f;

    private const float OverloadedDamagePerSecond = 2f;

    private static float SolveMostGainRate()
    {
        float seconds = CalibrationRealDays * 24f * 3600f;
        float ratio = (100f - CalibrationTargetLevel) / (100f - CalibrationStartLevel);
        return -100f * Mathf.Log(ratio) / seconds;
    }

    // Per-frame gain at these calibrated rates (e.g. ~1.8e-5 * ~0.016s ~=
    // 3e-7 at 60fps) is smaller than float32's precision step (ULP) at
    // Strength's current raw level (~2e-6 at level 20) — added directly
    // every frame, `level + amount*diminish` silently rounds back to
    // `level` and gain never actually accumulates (confirmed by
    // simulation before shipping, not assumed). Bank it in a local
    // accumulator instead and only call GainExperience once enough has
    // built up to survive float precision at any level up to 100 — see
    // CLAUDE.md's "tier-scaling ratio" gotcha neighborhood for this
    // session's other float-precision-adjacent lessons.
    private const float PendingGainFlushThreshold = 0.001f;
    private float pendingStrengthGain;

    [SerializeField] private SkillDefinition strengthSkill;

    private PlayerInventory inventory;
    private PlayerEquipment equipment;
    private PlayerBackpack backpack;
    private PlayerSkills skills;
    private PlayerVitals vitals;

    public float CarriedWeight { get; private set; }
    public float Capacity { get; private set; }
    public float LoadRatio => Capacity > 0f ? CarriedWeight / Capacity : 0f;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        equipment = GetComponent<PlayerEquipment>();
        backpack = GetComponent<PlayerBackpack>();
        skills = GetComponent<PlayerSkills>();
        vitals = GetComponent<PlayerVitals>();
    }

    private void Update()
    {
        float strength = skills.GetAttributeValue(strengthSkill);
        Capacity = ExpCoefficient * Mathf.Pow(strength, ExpExponent);
        CarriedWeight = ComputeCarriedWeight();

        TickStrengthGrowth(Time.deltaTime);
    }

    private void TickStrengthGrowth(float dt)
    {
        float ratio = LoadRatio;

        float gainRate = GainRateFor(ratio);
        if (gainRate > 0f)
        {
            pendingStrengthGain += gainRate * dt;
            if (pendingStrengthGain >= PendingGainFlushThreshold)
            {
                skills.GainExperience(strengthSkill, pendingStrengthGain);
                pendingStrengthGain = 0f;
            }
        }

        if (ratio > OverloadThreshold)
            vitals.Damage(OverloadedDamagePerSecond * dt);
    }

    // Checked highest threshold first so each band's upper bound wins.
    private static float GainRateFor(float ratio)
    {
        if (ratio > OverloadThreshold) return OverloadedGainPerSecond;   // >95%: reduced, plus health cost
        if (ratio > MostGainThreshold) return MostGainPerSecond;         // 90-95%: most gain
        if (ratio > BetterGainThreshold) return BetterGainPerSecond;     // 80-90%: better gain
        if (ratio > MarginalGainThreshold) return MarginalGainPerSecond; // 50-80%: marginal gain
        return 0f;                                                      // <=50%: no gain
    }

    private float ComputeCarriedWeight()
    {
        float total = WeightOf(inventory.Inventory);

        foreach (var slotName in equipment.SlotNames)
            total += WeightOf(equipment.GetSlot(slotName));

        var equippedBackpack = backpack.Equipped;
        if (equippedBackpack != null)
            total += WeightOf(equippedBackpack.Inventory);

        return total;
    }

    private static float WeightOf(Inventory inv)
    {
        if (inv == null) return 0f;

        float total = 0f;
        foreach (var slot in inv.Slots)
            if (slot.item != null) total += slot.item.weight * slot.count;
        return total;
    }
}
