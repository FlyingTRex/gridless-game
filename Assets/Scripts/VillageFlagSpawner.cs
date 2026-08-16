using UnityEngine;

// The spawn loop itself (2026-08-16, VILLAGE_FLAG_PLANNING.md sections
// 3-4) -- lives on the Player alongside PlayerFame/PlayerBuilding, same
// "always-ticking manager component" shape Furnace/Campfire's own Update
// already use for unattended timers, just world-scoped instead of
// per-structure. Every currentIntervalMinutes (Fame band + Flag tier both
// shrinking it), if at least one Village Flag exists in the world, spawns
// a fresh hireable NPC out near the strongest placed Flag and sends it
// walking in (NPCSeekFlag).
[RequireComponent(typeof(PlayerFame))]
public class VillageFlagSpawner : MonoBehaviour
{
    // Ben's number, confirmed 2026-08-16.
    private const float BaseIntervalMinutes = 30f;

    // Proposed anchor from the design doc, not yet Ben-confirmed as final
    // -- picked as the working number rather than leaving this unset,
    // same "ship the proposed number, revisit if it plays wrong" approach
    // every other first-pass balance number in this project takes.
    private const float BaseStickAroundMinutes = 10f;

    [SerializeField] private GameObject hireableNpcPrefab;

    // How far out from the target Flag a freshly spawned NPC appears --
    // far enough that the walk-in is a real, visible thing, not
    // instant-adjacent.
    [SerializeField] private float spawnDistanceFromFlag = 40f;

    private PlayerFame playerFame;
    private float spawnTimerSeconds;

    private void Awake()
    {
        playerFame = GetComponent<PlayerFame>();
    }

    private void Update()
    {
        var flags = FindObjectsByType<VillageFlag>(FindObjectsSortMode.None);
        if (flags.Length == 0)
        {
            // "if at least one Village Flag exists in the world" (section
            // 3) -- the timer doesn't even accrue with none placed, so
            // building the first Flag doesn't instantly spawn someone from
            // credit that piled up beforehand.
            spawnTimerSeconds = 0f;
            return;
        }

        spawnTimerSeconds += Time.deltaTime;

        float intervalMinutes = CurrentIntervalMinutes(flags);
        if (spawnTimerSeconds < intervalMinutes * 60f) return;

        spawnTimerSeconds = 0f;
        SpawnAndSendToward(BestFlag(flags), intervalMinutes);
    }

    // "Nearest Flag" is the assumed target once more than one exists per
    // the design doc, but multi-flag balance (do they share one timer, or
    // race?) is explicitly left undesigned. Picked the single strongest
    // placed Flag to drive one shared timer, rather than the nearest-to-
    // spawn-point (which isn't even known yet at this point in the
    // method) -- simplest defensible reading, worth revisiting once
    // multiple Flags are common.
    private VillageFlag BestFlag(VillageFlag[] flags)
    {
        var best = flags[0];
        foreach (var flag in flags)
            if (flag.Tier > best.Tier) best = flag;
        return best;
    }

    private float CurrentIntervalMinutes(VillageFlag[] flags)
    {
        float fameMultiplier = playerFame != null ? playerFame.SpawnFrequencyMultiplier : 1f;
        float flagMultiplier = CraftTierScale.VillageFlagIntervalMultiplier(BestFlag(flags).Tier);
        // Frequency divides the interval (higher Fame = shorter wait);
        // the Flag's own multiplier scales it directly -- see
        // VILLAGE_FLAG_PLANNING.md section 4 for why these compose this
        // way rather than both dividing.
        return BaseIntervalMinutes / fameMultiplier * flagMultiplier;
    }

    private void SpawnAndSendToward(VillageFlag flag, float intervalMinutes)
    {
        if (hireableNpcPrefab == null || flag == null) return;

        Vector2 dir = Random.insideUnitCircle.normalized;
        Vector3 spawnPos = flag.transform.position + new Vector3(dir.x, 0f, dir.y) * spawnDistanceFromFlag;

        var bounds = WorldBounds.GetPlayableBounds();
        spawnPos.x = Mathf.Clamp(spawnPos.x, bounds.min.x, bounds.max.x);
        spawnPos.z = Mathf.Clamp(spawnPos.z, bounds.min.z, bounds.max.z);
        spawnPos.y = GroundHeight.Sample(spawnPos, flag.transform.position.y);

        var instance = Instantiate(hireableNpcPrefab, spawnPos, Quaternion.identity);
        var seek = instance.GetComponent<NPCSeekFlag>();
        if (seek == null) return;

        // The inverse of the same interval just used to decide it was
        // time to spawn -- "the npc wanders away in the inverse of the
        // spawn time... as fame increases, npc shows up sooner and sticks
        // around longer" (Ben's own framing, section 4).
        float stickAroundMinutes = (BaseIntervalMinutes * BaseStickAroundMinutes) / intervalMinutes;
        seek.BeginSeeking(flag.transform, stickAroundMinutes * 60f);

        // Real value here: this takes up to 30 real minutes at baseline --
        // easy to miss if you're not staring at the screen when it
        // happens. Logged once per spawn, not per tick.
        Debug.Log($"[VillageFlag] Spawned NPC near {flag.name} (tier {flag.Tier}), "
            + $"interval was {intervalMinutes:F1}min, stick-around {stickAroundMinutes:F1}min");
    }
}
