using System.Collections.Generic;
using UnityEngine;

// Stable-ID lookup for NPCJobDefinition assets — same shape/reasoning as
// ItemDatabase, needed because a hired NPC's assigned job (NPCJob.
// AssignedJob) is itself a ScriptableObject reference save data can't
// serialize directly.
[CreateAssetMenu(menuName = "Gridless/NPC Job Database", fileName = "NPCJobDatabase")]
public class NPCJobDatabase : ScriptableObject
{
    [SerializeField] private NPCJobDefinition[] jobs = System.Array.Empty<NPCJobDefinition>();

    private static NPCJobDatabase instance;
    public static NPCJobDatabase Instance =>
        instance != null ? instance : instance = Resources.Load<NPCJobDatabase>("NPCJobDatabase");

    private Dictionary<string, NPCJobDefinition> lookup;

    public string IdFor(NPCJobDefinition job) => job != null ? job.name : null;

    public NPCJobDefinition Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (lookup == null) BuildLookup();
        return lookup.TryGetValue(id, out var job) ? job : null;
    }

    private void BuildLookup()
    {
        lookup = new Dictionary<string, NPCJobDefinition>(jobs.Length);
        foreach (var job in jobs)
            if (job != null) lookup[job.name] = job;
    }

#if UNITY_EDITOR
    // See ItemDatabase.EditorSetItems — same deterministic-sort reasoning.
    public void EditorSetJobs(NPCJobDefinition[] value)
    {
        System.Array.Sort(value, (a, b) =>
            string.CompareOrdinal(a != null ? a.name : string.Empty, b != null ? b.name : string.Empty));
        jobs = value;
        lookup = null;
    }
#endif
}
