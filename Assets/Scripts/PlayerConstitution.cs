using UnityEngine;

// Constitution's growable Max Health/Max Stamina, plus its own exercise-based
// growth — see DEXTERITY_CONSTITUTION_PLANNING.md (2026-08-14). Output side
// mirrors PlayerEncumbrance's continuously-recomputed pattern (not
// PlayerVitals.GrowMaxWill's discrete-increment pattern): MaxHealth/
// MaxStamina are a pure function of the current Constitution value, pushed
// into PlayerVitals every frame via SetMaxHealth/SetMaxStamina.
//
// A pure power law (Max = C * Constitution^n) can't hit both a sane low
// anchor (today's flat 100 baseline, no regression for a fresh character)
// and a front-loaded curve (n > 1, like Strength's capacity) at once — a
// front-loaded curve needs output to grow by *more* than the input ratio,
// which a modest 2x-at-cap target can't satisfy. Resolved with an additive
// model instead: Max = 100 + k * (Constitution - 2)^1.5. The baseline stays
// exactly 100 at starting Constitution (2.00); the bonus above that follows
// a genuinely front-loaded curve.
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerSkills))]
[RequireComponent(typeof(PlayerVitals))]
public class PlayerConstitution : MonoBehaviour
{
    private const float ExpExponent = 1.5f;

    // k solved from 100 = k * 8^1.5 (Health, +100 at Constitution 10) and
    // 200 = k * 8^1.5 (Stamina, +200 at Constitution 10) — Ben's picks,
    // 2026-08-14.
    private const float HealthCoefficient = 4.42f;
    private const float StaminaCoefficient = 8.84f;
    private const float BaselineHealth = 100f;
    private const float BaselineStamina = 100f;

    // Constitution starts at displayed 2.00 (PlayerSkills.startingLevels,
    // same baseline every core stat uses) — the bonus curve is anchored to
    // zero there.
    private const float CurveFloor = 2f;

    // Real-time-calibrated pacing (2026-08-14, Ben's call: slower than
    // Strength's 2-day target — "cardio conditioning should take noticeably
    // longer than load-bearing strength gains"). Same ODE-solved-rate
    // approach as PlayerEncumbrance.SolveMostGainRate.
    private const float CalibrationStartLevel = 20f;    // Constitution 2.00
    private const float CalibrationTargetLevel = 22.5f; // Constitution 2.25 (+0.25)
    private const float CalibrationRealDays = 4f;

    private static readonly float SprintGainPerSecond = SolveGainRate();

    // Per-frame gain at these calibrated rates is smaller than float32's
    // precision step at typical skill levels — banked in a local
    // accumulator and only flushed once it can survive the round-trip, same
    // reasoning as PlayerEncumbrance.pendingStrengthGain.
    private const float PendingGainFlushThreshold = 0.001f;
    private float pendingGain;

    // Soccer's secret bonus (Ben, 2026-08-14: "we also have a soccer
    // ball... that could introduce a game within the game") — scaled by
    // kick distance so a hard sprint-kick (5-12m) grants meaningfully more
    // than a light tap (3-7m), not shown anywhere in UI/tooltips. First-pass
    // magnitude, tunable like every other rate in this system.
    private const float SoccerGainPerMeter = 0.02f;

    [SerializeField] private SkillDefinition constitutionSkill;

    private PlayerSkills skills;
    private PlayerVitals vitals;

    private void Awake()
    {
        skills = GetComponent<PlayerSkills>();
        vitals = GetComponent<PlayerVitals>();
    }

    private void Update()
    {
        float con = skills.GetAttributeValue(constitutionSkill);
        float bonus = Mathf.Pow(Mathf.Max(0f, con - CurveFloor), ExpExponent);
        vitals.SetMaxHealth(BaselineHealth + HealthCoefficient * bonus);
        vitals.SetMaxStamina(BaselineStamina + StaminaCoefficient * bonus);

        if (vitals.IsSprinting)
        {
            pendingGain += SprintGainPerSecond * Time.deltaTime;
            if (pendingGain >= PendingGainFlushThreshold)
            {
                skills.GainExperience(constitutionSkill, pendingGain);
                pendingGain = 0f;
            }
        }
    }

    // Called by SoccerBall.TryKick on whoever kicked it.
    public void GrantSoccerKickGain(float kickDistance) =>
        skills.GainExperience(constitutionSkill, kickDistance * SoccerGainPerMeter);

    private static float SolveGainRate()
    {
        float seconds = CalibrationRealDays * 24f * 3600f;
        float ratio = (100f - CalibrationTargetLevel) / (100f - CalibrationStartLevel);
        return -100f * Mathf.Log(ratio) / seconds;
    }
}
