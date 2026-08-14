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

    public string IdFor(NPCJobDefinition job) => job != null ? job.name : null;

    public NPCJobDefinition Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var job in jobs)
            if (job != null && job.name == id) return job;
        return null;
    }

#if UNITY_EDITOR
    public void EditorSetJobs(NPCJobDefinition[] value) => jobs = value;
#endif
}
