using UnityEngine;

// Berry Bush's two independent gather actions (2026-08-09, Ben's call),
// replacing the old single instant-E-grabs-a-berry model:
// - E: hold to chop (requires a Knife or Axe in hand, same tool-gating
//   shape as ChoppableTree) — scatters loose Trimmed Stick pickups on the
//   ground, doesn't hand them over directly.
// - F: search for berries (no tool needed) — rolls 0 to maxBerries and
//   scatters that many loose Berry pickups on the ground, plus a separate
//   low-chance "super success" roll (berrySeedChance) for a bonus Berry
//   Seed, independent of the normal yield (added 2026-08-09).
// Both actions stay on their own independent respawn timer (same "hide
// for a while, come back" shape as ResourceNode/ChoppableTree) rather
// than the bush itself ever disappearing — chopping and searching are
// two separate resources on the same plant, not one depleting object.
[DisallowMultipleComponent]
public class BerryBush : MonoBehaviour, IInteractable, ISecondaryInteractable, INPCSearchable
{
    [SerializeField] private ItemDefinition[] chopTools;
    [SerializeField] private string chopToolLabel = "Knife or Axe";
    [SerializeField] private GameObject trimmedStickPrefab;
    [SerializeField] private int trimmedStickCount = 2;
    [SerializeField] private float chopScatterForce = 1f;
    [SerializeField] private SkillDefinition chopSkill;
    [SerializeField] private float chopSkillGain = 0.5f;
    // <= 0 means chopping never goes on cooldown at all — same "0
    // disables it" reading as ResourceNode.respawnDelay.
    [SerializeField] private float chopRespawnDelay = 180f;

    [SerializeField] private GameObject berryPrefab;
    [SerializeField] private int minBerries = 0;
    [SerializeField] private int maxBerries = 3;
    [SerializeField] private float searchScatterForce = 0.8f;
    [SerializeField] private float searchRespawnDelay = 180f;

    // "Super success" bonus (2026-08-09, Ben's ask) - independent of the
    // normal berry yield above, not a replacement for it. Rolled
    // separately every search, including on a 0-berry roll.
    [SerializeField] private GameObject berrySeedPrefab;
    [SerializeField] [Range(0f, 1f)] private float berrySeedChance = 0.02f;

    private float chopRespawnAt = -1f;
    private float searchRespawnAt = -1f;

    private bool IsChopOnCooldown => chopRespawnAt >= 0f;
    private bool IsSearchOnCooldown => searchRespawnAt >= 0f;

    // E's prompt always shows (same convention as ChoppableTree/
    // ResourceNode) — the tool requirement is stated up front, not hidden
    // until you happen to be holding the right thing.
    public string Prompt => IsChopOnCooldown
        ? "Bush (branches regrowing)"
        : $"Hold to chop (requires {chopToolLabel})";
    public bool IsInstant => false;

    public float GetHoldDuration(GameObject player) =>
        player.GetComponent<PlayerSkills>().GetHoldDuration(chopSkill);

    // F hides entirely while on cooldown, unlike E — matches
    // ISecondaryInteractable's own documented "return null to hide"
    // convention, since there's no "regrowing" label slot for a
    // secondary prompt the way E's Prompt has.
    public string GetSecondaryPrompt(GameObject player) =>
        IsSearchOnCooldown ? null : "Search for berries";

    // INPCSearchable (2026-08-13) — only the F-search half; the E-chop
    // action stays player-only, untouched (Ben's explicit call, see
    // NPC_JOB_GENERALIZATION_PLANNING.md section 3a).
    public bool IsAvailable => !IsSearchOnCooldown;

    private void Update()
    {
        if (chopRespawnAt >= 0f && Time.time >= chopRespawnAt) chopRespawnAt = -1f;
        if (searchRespawnAt >= 0f && Time.time >= searchRespawnAt) searchRespawnAt = -1f;
    }

    public void Complete(GameObject player)
    {
        if (IsChopOnCooldown) return;

        if (chopTools != null && chopTools.Length > 0)
        {
            var equipment = player.GetComponent<PlayerEquipment>();
            if (equipment == null || !HasAnyToolInHand(equipment, chopTools)) return;
        }

        player.GetComponent<PlayerSkills>()?.GainExperience(chopSkill, chopSkillGain);

        for (int i = 0; i < trimmedStickCount; i++)
            SpawnScattered(trimmedStickPrefab, chopScatterForce);

        if (chopRespawnDelay > 0f)
            chopRespawnAt = Time.time + chopRespawnDelay;
    }

    public void CompleteSecondary(GameObject player) => TriggerSearchForNPC();

    // Same search logic the player's F action always ran — renamed/shared
    // so NPCGathering can trigger it directly without a GameObject player
    // to pass through. Returns false (no-op) while on cooldown.
    public bool TriggerSearchForNPC()
    {
        if (IsSearchOnCooldown) return false;

        int count = Random.Range(minBerries, maxBerries + 1);
        for (int i = 0; i < count; i++)
            SpawnScattered(berryPrefab, searchScatterForce);

        if (Random.value < berrySeedChance)
            SpawnScattered(berrySeedPrefab, searchScatterForce);

        if (searchRespawnDelay > 0f)
            searchRespawnAt = Time.time + searchRespawnDelay;

        return true;
    }

    private static bool HasAnyToolInHand(PlayerEquipment equipment, ItemDefinition[] tools)
    {
        foreach (var tool in tools)
            if (tool != null && equipment.HasInHand(tool)) return true;
        return false;
    }

    // Bug fixed 2026-08-09: Random.insideUnitSphere can land arbitrarily
    // close to center, including well inside the bush's own SphereCollider
    // (radius 0.175) — unlike ResourceNode/ChoppableTree, this bush never
    // disables its collider (the whole point of the redesign is it stays
    // interactable throughout), so a scattered pickup landing that close
    // got its raycast permanently shadowed by the bush itself, completely
    // unpickable. Fixed by spawning on a fixed 0.45 horizontal ring
    // (clearly outside the collider) and pushing further in that same
    // outward direction, instead of a fully random offset/impulse.
    private void SpawnScattered(GameObject prefab, float force)
    {
        if (prefab == null) return;

        Vector2 horizontal = Random.insideUnitCircle.normalized * 0.45f;
        Vector3 offset = new Vector3(horizontal.x, 0.15f, horizontal.y);
        var obj = Instantiate(prefab, transform.position + offset, Random.rotation);

        if (obj.TryGetComponent(out Rigidbody rb))
        {
            Vector3 dir = new Vector3(horizontal.x, 0.6f, horizontal.y).normalized;
            rb.AddForce(dir * force, ForceMode.Impulse);
        }
    }
}
