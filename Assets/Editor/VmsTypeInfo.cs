using System;
using UnityEditor;
using UnityEngine;

// Descriptor table for VmsWindow's 6 tabs, plus a shared type-parameterized
// asset scan. Deliberately not System.Type-generic per call site like
// DatabaseRepopulator.LoadAll<T>() -- VmsWindow picks its type dynamically
// from a runtime tab index, so it needs a Type-parameter overload instead.
public sealed class VmsTypeInfo
{
    public readonly Type Type;
    public readonly string TabLabel;

    // Items/Skills/NPC Jobs are indexed by ItemDatabase/SkillDatabase/
    // NPCJobDatabase (see DatabaseRepopulator.cs) -- a new asset of one of
    // these types won't resolve via Find(id) at runtime until that database
    // is regenerated. CraftingRecipe/CookableItem/BuildPiece have no such
    // database, so no reminder applies to them.
    public readonly bool HasDatabaseReminder;

    private VmsTypeInfo(Type type, string tabLabel, bool hasDatabaseReminder)
    {
        Type = type;
        TabLabel = tabLabel;
        HasDatabaseReminder = hasDatabaseReminder;
    }

    public static readonly VmsTypeInfo[] All =
    {
        new VmsTypeInfo(typeof(ItemDefinition), "Items", hasDatabaseReminder: true),
        new VmsTypeInfo(typeof(CraftingRecipe), "Recipes", hasDatabaseReminder: false),
        new VmsTypeInfo(typeof(CookableItem), "Cookables", hasDatabaseReminder: false),
        new VmsTypeInfo(typeof(SkillDefinition), "Skills", hasDatabaseReminder: true),
        new VmsTypeInfo(typeof(NPCJobDefinition), "NPC Jobs", hasDatabaseReminder: true),
        new VmsTypeInfo(typeof(BuildPiece), "Build Pieces", hasDatabaseReminder: false),
    };

    // Same AssetDatabase.FindAssets($"t:{T}") -> LoadAssetAtPath scan
    // DatabaseRepopulator.LoadAll<T>() uses, just Type-parameterized instead
    // of generic so VmsWindow can call it with a runtime-selected type.
    public static UnityEngine.Object[] LoadAll(Type type)
    {
        var guids = AssetDatabase.FindAssets($"t:{type.Name}");
        var result = new UnityEngine.Object[guids.Length];
        for (int i = 0; i < guids.Length; i++)
            result[i] = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[i]), type);
        return result;
    }
}
