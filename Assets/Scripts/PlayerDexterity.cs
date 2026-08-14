using UnityEngine;

// Dexterity's speed multiplier, plus its own growth inputs — see
// DEXTERITY_CONSTITUTION_PLANNING.md (2026-08-14). Speed output is linear
// (not front-loaded like Strength/Constitution, Ben's call: "speed doesn't
// have the same actively-managed-resource feel") — 0% bonus at the display
// floor (0.25), +30% at the cap (10.00).
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerSkills))]
[RequireComponent(typeof(PlayerVitals))]
public class PlayerDexterity : MonoBehaviour
{
    private const float DisplayFloor = 0.25f;
    private const float DisplayCap = 10f;
    private const float SpeedBonusAtCap = 0.30f;

    // Sprinting shares Constitution's own 4-day pacing target (2026-08-14,
    // Ben: "sprinting trains both" — same action, two payoffs). Sneaking is
    // Dexterity-exclusive and has to pull its own weight alone: ~3 real
    // days for +0.25 at Dexterity 2.00. Same ODE-solved-rate approach as
    // PlayerEncumbrance.SolveMostGainRate.
    private const float SprintCalibrationRealDays = 4f;
    private const float SneakCalibrationRealDays = 3f;
    private const float CalibrationStartLevel = 20f;    // Dexterity 2.00
    private const float CalibrationTargetLevel = 22.5f; // Dexterity 2.25 (+0.25)

    private static readonly float SprintGainPerSecond = SolveGainRate(SprintCalibrationRealDays);
    private static readonly float SneakGainPerSecond = SolveGainRate(SneakCalibrationRealDays);

    // Same float-precision reasoning as PlayerEncumbrance.pendingStrengthGain
    // / PlayerConstitution.pendingGain.
    private const float PendingGainFlushThreshold = 0.001f;
    private float pendingGain;

    // First-pass flat grants (2026-08-14, Ben: "small amount, regardless of
    // what was crafted" for crafting; jump matches the same magnitude) —
    // tunable like every other rate in this system.
    private const float JumpGain = 0.1f;
    private const float CraftGain = 0.1f;

    [SerializeField] private SkillDefinition dexteritySkill;

    private PlayerSkills skills;
    private PlayerVitals vitals;

    // Set every frame by FirstPersonController — true while moving in
    // Kneeling/Crawling/Prone stance, the game's "sneaking" states.
    public bool IsSneaking { get; set; }

    public float SpeedMultiplier
    {
        get
        {
            float dex = skills.GetAttributeValue(dexteritySkill);
            float t = Mathf.Clamp01((dex - DisplayFloor) / (DisplayCap - DisplayFloor));
            return 1f + SpeedBonusAtCap * t;
        }
    }

    private void Awake()
    {
        skills = GetComponent<PlayerSkills>();
        vitals = GetComponent<PlayerVitals>();
    }

    private void Update()
    {
        float rate = 0f;
        if (vitals.IsSprinting) rate = SprintGainPerSecond;
        else if (IsSneaking) rate = SneakGainPerSecond;

        if (rate <= 0f) return;

        pendingGain += rate * Time.deltaTime;
        if (pendingGain >= PendingGainFlushThreshold)
        {
            skills.GainExperience(dexteritySkill, pendingGain);
            pendingGain = 0f;
        }
    }

    // Called by FirstPersonController on a successful jump.
    public void GrantJumpGain() => skills.GainExperience(dexteritySkill, JumpGain);

    // Called by PlayerCrafting on every completed craft (any recipe, any
    // outcome) — the manual-vs-machine distinction Ben wanted needs no new
    // field: CraftingRecipe is inherently the "player actively did it" type
    // in this codebase (player-triggered, skill-gated, roll-based), while
    // Furnace/Campfire automation lives in the separate SmeltableItem/
    // CookableItem types this method is never called for.
    public void GrantCraftGain() => skills.GainExperience(dexteritySkill, CraftGain);

    private static float SolveGainRate(float realDays)
    {
        float seconds = realDays * 24f * 3600f;
        float ratio = (100f - CalibrationTargetLevel) / (100f - CalibrationStartLevel);
        return -100f * Mathf.Log(ratio) / seconds;
    }
}
