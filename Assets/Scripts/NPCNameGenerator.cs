using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Auto-naming for Village-Flag-spawned NPCs (2026-08-17,
// BUGS_AND_ENHANCEMENTS.md "NPC identification") -- every NPC starts
// with a real, unique identity instead of the shared "Factory Worker"
// default, with zero player effort. IRenameable (NPCDialogue.Rename)
// still layers on top for anyone who wants to personalize further.
public static class NPCNameGenerator
{
    private static readonly string[] MaleNames =
    {
        "Marcus", "Owen", "Felix", "Gabriel", "Silas", "Theo", "Dominic",
        "Rowan", "Cyrus", "Julian", "Elias", "Bram", "Gideon", "Anders",
        "Percival",
    };

    private static readonly string[] FemaleNames =
    {
        "Sarah", "Nadia", "Wren", "Cora", "Freya", "Iris", "Sable",
        "Marisol", "Talia", "Esme", "Rosalind", "Juno", "Vera", "Sian",
        "Odette",
    };

    // Picks a name from the matching list, preferring one not already in
    // use by a currently-active NPC (avoids "two NPCs named Sarah" —
    // the exact confusion this whole feature exists to prevent). Falls
    // back to a random pick from the full list once every name in it is
    // already taken, rather than failing or blocking a spawn.
    public static string PickUnique(bool isFemale)
    {
        var pool = isFemale ? FemaleNames : MaleNames;

        var taken = new HashSet<string>(
            Object.FindObjectsByType<NPCDialogue>(FindObjectsSortMode.None).Select(d => d.DisplayName));

        var available = pool.Where(n => !taken.Contains(n)).ToArray();
        var choices = available.Length > 0 ? available : pool;

        return choices[Random.Range(0, choices.Length)];
    }
}
