using System;
using System.IO;
using UnityEngine;

// Shared file-based debug logger (2026-08-21, Ben's ask) -- writes
// timestamped lines to one file in Application.persistentDataPath so a
// Claude session (no live Play-mode access of its own) can read exactly
// what happened after a live test, instead of Ben copy-pasting Console
// output by hand. Per-object "Debug" checkboxes (NPCHiringScreen,
// FurnaceScreen, CampfireScreen, ...) opt individual instances into
// writing here -- this class has no opinion on *when* to write, only
// *how*, so every caller shares one file/format instead of each system
// growing its own bespoke log.
public static class DebugLog
{
    public static readonly string FilePath = Path.Combine(Application.persistentDataPath, "debug_log.txt");

    public static void Write(string source, string message)
    {
        try
        {
            File.AppendAllText(FilePath, $"[{DateTime.Now:HH:mm:ss}] {source}: {message}\n");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"DebugLog: failed to write ({e.Message})");
        }
    }
}
