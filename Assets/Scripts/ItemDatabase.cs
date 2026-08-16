using System.Collections.Generic;
using UnityEngine;

// Stable-ID lookup for ItemDefinition assets (SAVE_LOAD_PLANNING.md section
// 5) — a ScriptableObject reference can't serialize into save JSON
// directly. ID is just the asset's own file name, already effectively
// unique per this project's Assets/Data/ convention. Populated via
// DatabaseRepopulator (Editor-only, full regen from an AssetDatabase scan —
// see that file's header). Lives at Assets/Resources/ItemDatabase.asset so
// Resources.Load works in a build, unlike an AssetDatabase scan.
[CreateAssetMenu(menuName = "Gridless/Item Database", fileName = "ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private ItemDefinition[] items = System.Array.Empty<ItemDefinition>();

    private static ItemDatabase instance;
    public static ItemDatabase Instance =>
        instance != null ? instance : instance = Resources.Load<ItemDatabase>("ItemDatabase");

    private Dictionary<string, ItemDefinition> lookup;

    // Read by Bookshelf (StorageBox.restrictToSkillBooks, 2026-08-16) to
    // compute its own allowed-items list at runtime from a scan of every
    // ItemDefinition's worldPickupPrefab -- avoids a second hand-maintained
    // registration array for exactly the reason EFFICIENCY_AUDIT.md already
    // flagged for this database itself.
    public IReadOnlyList<ItemDefinition> AllItems => items;

    public string IdFor(ItemDefinition item) => item != null ? item.name : null;

    public ItemDefinition Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (lookup == null) BuildLookup();
        return lookup.TryGetValue(id, out var item) ? item : null;
    }

    private void BuildLookup()
    {
        lookup = new Dictionary<string, ItemDefinition>(items.Length);
        foreach (var item in items)
            if (item != null) lookup[item.name] = item;
    }

#if UNITY_EDITOR
    // Sorted by stable ID (asset name) before assigning so two independent
    // regenerations of the same underlying item set produce byte-identical
    // output. AssetDatabase.FindAssets' enumeration order is not stable
    // across machines/runs — without this sort, every regen reshuffles the
    // whole array and guarantees a merge conflict between collaborators
    // even when the actual item set didn't change. See CHANGELOG 2026-08-16.
    public void EditorSetItems(ItemDefinition[] value)
    {
        System.Array.Sort(value, (a, b) =>
            string.CompareOrdinal(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty));
        items = value;
        lookup = null;
    }
#endif
}
