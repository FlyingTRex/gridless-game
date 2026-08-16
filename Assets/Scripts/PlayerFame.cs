using UnityEngine;

// Reputation system — see FAME_PLANNING.md (2026-08-14) for the full
// design conversation. A single overall float, -1000 to 1000, zero as
// neutral. Only the inputs with something real to hook are wired here;
// Kill NPC, Player death, Start/Close a guild, and business-reach Fame
// are all designed but blocked on systems that don't exist yet — see
// BUGS_AND_ENHANCEMENTS.md for each as a standalone follow-up.
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerSkills))]
public class PlayerFame : MonoBehaviour
{
    private const float MinFame = -1000f;
    private const float MaxFame = 1000f;

    private const float HireAmount = 1f;
    private const float FireAmount = -0.5f;
    private const float UnpaidCycleAmount = -0.5f;
    private const float GuildJoinAmount = 1f;
    private const float GuildLeaveAmount = -1f;

    // Smaller than Hire's own +1 (NPC_TRAINING_PLANNING.md, 2026-08-16) --
    // a repeatable action once a player has several NPCs and a steady book
    // supply, not a one-time milestone. Number not pinned down in the
    // design doc beyond "small"; picked to sit clearly below Hire.
    private const float NpcTrainingAmount = 0.25f;

    // Proposed, not yet Ben-confirmed as final (VILLAGE_FLAG_PLANNING.md
    // section 6) -- a real milestone-sized jump, deliberately well above
    // any repeatable action (Hire +1, Training's own +0.25, even a
    // Masterwork skill-tier mastery's +5), since reaching this requires
    // 10 hires *and* a Masterwork Flag *and* the Statue's own real cost.
    private const float CityStatueAmount = 50f;

    [SerializeField] private float fame = 0f;

    private PlayerSkills skills;

    public float Fame => fame;

    // The same 5-band table `PlayerMenuScreen`'s Fame tile has displayed a
    // label for since 2026-08-14 -- moved here 2026-08-16 (Village Flag
    // spawn loop, VILLAGE_FLAG_PLANNING.md section 4) so it's one
    // canonical source of the band boundaries instead of two copies that
    // could drift. Also directly the table `FAME_PLANNING.md`'s Traveling
    // Trader visit-frequency design already confirmed and left unusable
    // until a real spawn mechanism existed -- the Village Flag is that
    // mechanism, so this doubles as the Trader's own future frequency
    // table too, not a Flag-specific one.
    public enum FameBand { Infamous, Notorious, Neutral, Known, Renowned }

    public FameBand Band =>
        fame <= -500f ? FameBand.Infamous :
        fame <= -100f ? FameBand.Notorious :
        fame < 100f ? FameBand.Neutral :
        fame < 500f ? FameBand.Known :
        FameBand.Renowned;

    // Higher = more often = a shorter spawn interval, so the Village Flag
    // spawner divides by this rather than multiplying — see
    // VillageFlagSpawner.CurrentIntervalMinutes.
    public float SpawnFrequencyMultiplier => Band switch
    {
        FameBand.Infamous => 0.5f,
        FameBand.Notorious => 0.75f,
        FameBand.Neutral => 1.0f,
        FameBand.Known => 1.25f,
        FameBand.Renowned => 1.5f,
        _ => 1.0f,
    };

    private void Awake()
    {
        skills = GetComponent<PlayerSkills>();
    }

    private void OnEnable()
    {
        skills.TierUnlocked += OnTierUnlocked;
    }

    private void OnDisable()
    {
        skills.TierUnlocked -= OnTierUnlocked;
    }

    private void OnTierUnlocked(CraftTier tier) => Grant(CraftTierScale.FameOnTierUnlock(tier));

    public void GrantHire() => Grant(HireAmount);
    public void GrantFire() => Grant(FireAmount);
    public void GrantUnpaidCycle() => Grant(UnpaidCycleAmount);
    public void GrantGuildJoin() => Grant(GuildJoinAmount);
    public void GrantGuildLeave() => Grant(GuildLeaveAmount);
    public void GrantNpcTraining() => Grant(NpcTrainingAmount);
    public void GrantCityStatue() => Grant(CityStatueAmount);

    // Called by SaveManager on load — sets the absolute value directly,
    // unlike Grant's relative add, same "restore vs. earn" distinction
    // PlayerSkills.RestoreLevel already draws against GainExperience.
    public void RestoreFame(float value) => fame = Mathf.Clamp(value, MinFame, MaxFame);

    // Every Fame change (Hire/Fire/Training/City Statue/tier-unlocks) flows
    // through this one method -- logging here (2026-08-16, same reasoning
    // as PlayerSkills.GainExperience's own log) covers all of them with a
    // single line, useful for background/autonomous grants (a tier-unlock
    // mid-play, a City Statue placed) that are easy to miss without
    // watching the Player tab continuously.
    private void Grant(float amount)
    {
        fame = Mathf.Clamp(fame + amount, MinFame, MaxFame);
        Debug.Log($"[Fame] {(amount >= 0 ? "+" : "")}{amount:F2} -> {fame:F2} ({Band})");
    }
}
