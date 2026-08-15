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

    [SerializeField] private float fame = 0f;

    private PlayerSkills skills;

    public float Fame => fame;

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

    // Called by SaveManager on load — sets the absolute value directly,
    // unlike Grant's relative add, same "restore vs. earn" distinction
    // PlayerSkills.RestoreLevel already draws against GainExperience.
    public void RestoreFame(float value) => fame = Mathf.Clamp(value, MinFame, MaxFame);

    private void Grant(float amount) => fame = Mathf.Clamp(fame + amount, MinFame, MaxFame);
}
