using System.Collections.Generic;

// Scene-scan registry mapping a saved SaveId string back to the live
// instance sitting in the loaded scene — every SaveId self-registers on
// enable/disable, so SaveManager.Load doesn't need to walk the scene
// itself for each lookup.
public static class SaveIdRegistry
{
    private static readonly Dictionary<string, SaveId> byId = new Dictionary<string, SaveId>();

    public static void Register(SaveId saveId)
    {
        if (saveId == null || string.IsNullOrEmpty(saveId.Id)) return;
        byId[saveId.Id] = saveId;
    }

    public static void Unregister(SaveId saveId)
    {
        if (saveId == null) return;
        if (byId.TryGetValue(saveId.Id, out var current) && current == saveId)
            byId.Remove(saveId.Id);
    }

    public static SaveId Find(string id) =>
        !string.IsNullOrEmpty(id) && byId.TryGetValue(id, out var found) ? found : null;
}
