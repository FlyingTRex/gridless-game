using UnityEngine;

// Stable-ID lookup for ItemDefinition assets (SAVE_LOAD_PLANNING.md section
// 5) — a ScriptableObject reference can't serialize into save JSON
// directly. ID is just the asset's own file name, already effectively
// unique per this project's Assets/Data/ convention. Populated once via
// the Editor-only auto-populate script (throwaway, deleted after running —
// same batch-mode setup convention as every other one-off asset-creation
// script in this project). Lives at Assets/Resources/ItemDatabase.asset so
// Resources.Load works in a build, unlike an AssetDatabase scan.
[CreateAssetMenu(menuName = "Gridless/Item Database", fileName = "ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private ItemDefinition[] items = System.Array.Empty<ItemDefinition>();

    private static ItemDatabase instance;
    public static ItemDatabase Instance =>
        instance != null ? instance : instance = Resources.Load<ItemDatabase>("ItemDatabase");

    public string IdFor(ItemDefinition item) => item != null ? item.name : null;

    public ItemDefinition Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var item in items)
            if (item != null && item.name == id) return item;
        return null;
    }

#if UNITY_EDITOR
    public void EditorSetItems(ItemDefinition[] value) => items = value;
#endif
}
