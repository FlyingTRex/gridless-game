using UnityEngine;

// Marker for a placed Village Flag (2026-08-16, VILLAGE_FLAG_PLANNING.md
// section 2-4) -- one component per prefab in the existing 5-tier ladder
// (VillageFlag_Crude.prefab through VillageFlag_Masterwork.prefab, built
// earlier this session by a parallel pass), tier baked in per-prefab since
// each tier is a genuinely different prefab (bigger pole/banner), not one
// mesh scaled. Read by VillageFlagSpawner to find every placed Flag and
// pick the spawn-interval multiplier.
//
// Nameable (Ben's follow-up ask, 2026-08-16) -- same IRenameable shape
// StorageBox already uses, so the existing PlayerRenaming right-click flow
// (raycast -> GetComponentInParent<IRenameable>) picks this up for free
// with no new interaction code. The chosen name is what MapScreen labels
// this Flag's marker with on the Player Map.
public class VillageFlag : MonoBehaviour, IRenameable
{
    [SerializeField] private CraftTier tier;
    [SerializeField] private string villageName = "Unnamed Village";

    public CraftTier Tier => tier;
    public string DisplayName => villageName;

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return;
        villageName = newName.Trim();
    }
}
