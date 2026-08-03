using UnityEngine;

public class ResourceNode : MonoBehaviour, IPunchable
{
    [SerializeField] private GameObject chunkPrefab;
    [SerializeField] private int chunkCount = 3;
    [SerializeField] private int hitsToBreak = 1;
    [SerializeField] private float scatterForce = 1.2f;
    [SerializeField] private SkillDefinition trainedSkill;
    [SerializeField] private float skillGain = 0.5f;
    [SerializeField] private float respawnDelay = 180f;
    [SerializeField] private float respawnScatter = 0.5f;

    private int hitsTaken;
    private Vector3 spawnPosition;
    private Collider col;
    private Renderer[] renderers;
    // -1 means "not counting down" — the node is either still standing
    // (the timer is held until it's actually broken) or mid-repositioning.
    private float respawnAt = -1f;

    public string Prompt => "Punch to break";

    private void Awake()
    {
        spawnPosition = transform.position;
        col = GetComponent<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void Update()
    {
        if (respawnAt < 0f || Time.time < respawnAt) return;
        Respawn();
    }

    public void OnPunch(GameObject player)
    {
        hitsTaken++;
        player.GetComponent<PlayerSkills>()?.GainExperience(trainedSkill, skillGain);
        if (hitsTaken < hitsToBreak) return;

        for (int i = 0; i < chunkCount; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 0.3f;
            var chunk = Instantiate(chunkPrefab, transform.position + Vector3.up * 0.2f + offset,
                Random.rotation);

            if (chunk.TryGetComponent(out Rigidbody rb))
            {
                Vector3 dir = (Random.insideUnitSphere + Vector3.up).normalized;
                rb.AddForce(dir * scatterForce, ForceMode.Impulse);
            }
        }

        SetVisible(false);
        respawnAt = Time.time + respawnDelay;
    }

    // Same spot it started at, with a small random horizontal shift, fully
    // intact again (hitsTaken reset) so it can be broken again from
    // scratch.
    private void Respawn()
    {
        Vector2 offset = Random.insideUnitCircle * respawnScatter;
        transform.position = spawnPosition + new Vector3(offset.x, 0f, offset.y);
        hitsTaken = 0;
        SetVisible(true);
        respawnAt = -1f;
    }

    private void SetVisible(bool visible)
    {
        if (col != null) col.enabled = visible;
        foreach (var r in renderers)
            r.enabled = visible;
    }
}
