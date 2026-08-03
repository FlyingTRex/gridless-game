using UnityEngine;

// Spends coins from PlayerCurrency and spawns them as individual physical
// Coin objects in the world — the inverse of Coin.Complete's pickup path.
// Builds each coin procedurally (CreatePrimitive) rather than needing a
// dedicated prefab per type, since a coin is just a scaled Cylinder with a
// type-specific material.
[RequireComponent(typeof(PlayerCurrency))]
public class PlayerCoinDrop : MonoBehaviour
{
    [System.Serializable]
    private class CoinMaterialEntry
    {
        public CoinType type;
        public Material material;
    }

    [SerializeField] private CoinMaterialEntry[] coinMaterials;
    [SerializeField] private Vector3 coinScale = new Vector3(0.08f, 0.01f, 0.08f);
    [SerializeField] private float dropDistance = 1.2f;
    [SerializeField] private float dropHeight = 1f;
    [SerializeField] private float scatterForce = 0.4f;
    [SerializeField] private float coinSpacing = 0.05f;

    private PlayerCurrency currency;

    private void Awake()
    {
        currency = GetComponent<PlayerCurrency>();
    }

    private Material MaterialFor(CoinType type)
    {
        if (coinMaterials == null) return null;

        foreach (var entry in coinMaterials)
            if (entry != null && entry.type == type) return entry.material;

        return null;
    }

    // Spends up to `amount` of the given coin type (stopping early if the
    // balance runs out) and spawns that many individual coins on the
    // ground in front of the player. Returns how many were actually
    // dropped.
    public int DropCoins(CoinType type, int amount)
    {
        if (amount <= 0) return 0;

        var material = MaterialFor(type);
        if (material == null) return 0;

        int dropped = 0;
        for (int i = 0; i < amount; i++)
        {
            if (!currency.Spend(type, 1)) break;
            SpawnCoin(type, material);
            dropped++;
        }

        return dropped;
    }

    // Each coin gets a small random horizontal offset at spawn plus a
    // small physics impulse, so a multi-coin drop scatters and bounces
    // apart instead of landing in an identical stack.
    private void SpawnCoin(CoinType type, Material material)
    {
        Vector3 offset = new Vector3(
            Random.Range(-coinSpacing, coinSpacing), 0f,
            Random.Range(-coinSpacing, coinSpacing));
        Vector3 spawnPos = transform.position + transform.forward * dropDistance + Vector3.up * dropHeight + offset;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = $"{type} Coin";
        go.transform.position = spawnPos;
        go.transform.rotation = Random.rotation;
        go.transform.localScale = coinScale;
        go.GetComponent<Renderer>().sharedMaterial = material;

        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.1f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Vector3 dir = (Random.insideUnitSphere + Vector3.up).normalized;
        rb.AddForce(dir * scatterForce, ForceMode.Impulse);

        go.AddComponent<Coin>().Configure(type, 1);
    }
}
