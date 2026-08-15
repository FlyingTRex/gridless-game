using UnityEditor;
using UnityEngine;

// Permanent tool (EFFICIENCY_AUDIT.md, 2026-08-15) — re-scans every
// ItemDefinition/SkillDefinition/NPCJobDefinition asset in the project
// and rebuilds ItemDatabase/SkillDatabase/NPCJobDatabase from scratch.
// These three are hand-populated arrays with zero auto-discovery, and
// forgetting to add a new asset silently breaks Find()-based save/load
// restore for it — confirmed as a real, live bug the same night this
// tool was built (ItemDatabase was missing 22 items, not just that
// session's new ones). Run this before any commit that adds a new
// ItemDefinition/SkillDefinition/NPCJobDefinition asset, same standing
// habit as a compile check.
public static class DatabaseRepopulator
{
    [MenuItem("Gridless/Repopulate Databases")]
    public static void RepopulateAll()
    {
        int items = RepopulateItemDatabase();
        int skills = RepopulateSkillDatabase();
        int jobs = RepopulateNpcJobDatabase();
        Debug.Log($"[DatabaseRepopulator] Items={items} Skills={skills} Jobs={jobs}");
    }

    private static int RepopulateItemDatabase()
    {
        var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/Resources/ItemDatabase.asset");
        if (database == null)
        {
            Debug.LogError("[DatabaseRepopulator] Could not load ItemDatabase.asset");
            return 0;
        }

        var items = LoadAll<ItemDefinition>();
        database.EditorSetItems(items);
        EditorUtility.SetDirty(database);
        return items.Length;
    }

    private static int RepopulateSkillDatabase()
    {
        var database = AssetDatabase.LoadAssetAtPath<SkillDatabase>("Assets/Resources/SkillDatabase.asset");
        if (database == null)
        {
            Debug.LogError("[DatabaseRepopulator] Could not load SkillDatabase.asset");
            return 0;
        }

        var skills = LoadAll<SkillDefinition>();
        database.EditorSetSkills(skills);
        EditorUtility.SetDirty(database);
        return skills.Length;
    }

    private static int RepopulateNpcJobDatabase()
    {
        var database = AssetDatabase.LoadAssetAtPath<NPCJobDatabase>("Assets/Resources/NPCJobDatabase.asset");
        if (database == null)
        {
            Debug.LogError("[DatabaseRepopulator] Could not load NPCJobDatabase.asset");
            return 0;
        }

        var jobs = LoadAll<NPCJobDefinition>();
        database.EditorSetJobs(jobs);
        EditorUtility.SetDirty(database);
        return jobs.Length;
    }

    private static T[] LoadAll<T>() where T : Object
    {
        var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        var result = new T[guids.Length];
        for (int i = 0; i < guids.Length; i++)
            result[i] = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[i]));
        return result;
    }
}
