using System.Collections.Generic;
using UnityEngine;

// Guild membership — a player can belong to up to MaxGuilds at once
// (Ben's call, 2026-08-10: "a player can be part of a total of 3
// guilds"). Each joined guild gets its own tile on PlayerMenuScreen's
// Player tab, appearing/disappearing live as membership changes. No
// in-world way to join a guild exists yet — exercised for now via
// AdminSpawnScreen's Join/Leave buttons.
[DisallowMultipleComponent]
public class PlayerGuilds : MonoBehaviour
{
    public const int MaxGuilds = 3;

    private readonly List<GuildDefinition> joined = new List<GuildDefinition>();

    public IReadOnlyList<GuildDefinition> Joined => joined;

    public bool IsMember(GuildDefinition guild) => guild != null && joined.Contains(guild);

    // False (no state change) if already a member, guild is null, or
    // membership is already at MaxGuilds.
    public bool Join(GuildDefinition guild)
    {
        if (guild == null || joined.Contains(guild) || joined.Count >= MaxGuilds) return false;
        joined.Add(guild);
        return true;
    }

    public bool Leave(GuildDefinition guild) => guild != null && joined.Remove(guild);
}
