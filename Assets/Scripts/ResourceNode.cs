using UnityEngine;

public class ResourceNode : MonoBehaviour, IPunchable
{
    [SerializeField] private GameObject chunkPrefab;
    [SerializeField] private int chunkCount = 3;
    [SerializeField] private int hitsToBreak = 1;
    [SerializeField] private float scatterForce = 1.2f;

    private int hitsTaken;

    public string Prompt => "Punch to break";

    public void OnPunch()
    {
        hitsTaken++;
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

        Destroy(gameObject);
    }
}
