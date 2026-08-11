using UnityEngine;

// Duplicated from BerryBush (2026-08-10, Ben's explicit call: "we don't
// want to use the existing berrybush... we need to duplicate and rename
// it") rather than reusing/repurposing it — a separate, independent
// gatherable so the two plants can be tuned/extended without one's
// changes rippling into the other. Simplified down to just the search
// mechanic — BerryBush's chop-for-Trimmed-Stick action doesn't make sense
// on an herb patch (no branches to trim), so it isn't carried over.
//
// Single F action: search for Herb, rolling minHerbs-maxHerbs and
// scattering that many loose pickups on the ground (not handed directly
// to inventory) — same shape as BerryBush's own F-search half, including
// the fixed-ring scatter offset (see SpawnScattered) that avoids the bug
// where a pickup could land inside the bush's own collider and become
// permanently unreachable.
//
// On F (ISecondaryInteractable), not E (IInteractable) — Ben's call,
// 2026-08-10: this reuses Berry Bush's exact visual model, so it looks
// identical, and Berry Bush's own search is on F (E there is taken by its
// chop action). Matching that key avoids the "looks the same, acts
// different" confusion a first pass on E caused live. No IInteractable
// at all now, since there's no second/primary action to reserve E for —
// PlayerInteraction's secondary-prompt line ("[F] ...") shows correctly
// on its own with no primary text alongside it.
[DisallowMultipleComponent]
public class HerbBush : MonoBehaviour, ISecondaryInteractable
{
    [SerializeField] private GameObject herbPrefab;
    [SerializeField] private int minHerbs = 1;
    [SerializeField] private int maxHerbs = 3;
    [SerializeField] private float scatterForce = 0.8f;
    // <= 0 means never goes on cooldown, same convention as
    // ResourceNode.respawnDelay/BerryBush.searchRespawnDelay.
    [SerializeField] private float respawnDelay = 180f;

    private float respawnAt = -1f;

    private bool IsOnCooldown => respawnAt >= 0f;

    // Hides entirely while on cooldown, same convention
    // ISecondaryInteractable documents (return null/empty to hide) —
    // matches BerryBush's own GetSecondaryPrompt exactly.
    public string GetSecondaryPrompt(GameObject player) =>
        IsOnCooldown ? null : "Search for herbs";

    private void Update()
    {
        if (respawnAt >= 0f && Time.time >= respawnAt) respawnAt = -1f;
    }

    public void CompleteSecondary(GameObject player)
    {
        if (IsOnCooldown) return;

        int count = Random.Range(minHerbs, maxHerbs + 1);
        for (int i = 0; i < count; i++)
            SpawnScattered(herbPrefab, scatterForce);

        if (respawnDelay > 0f)
            respawnAt = Time.time + respawnDelay;
    }

    // Same fixed-ring scatter as BerryBush.SpawnScattered — spawning on a
    // fully random offset let a pickup land inside the bush's own
    // collider (never disabled, unlike ResourceNode/ChoppableTree) and
    // become permanently unreachable; a fixed 0.45 horizontal ring keeps
    // every spawn clearly outside it.
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
