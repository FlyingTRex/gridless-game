using UnityEngine;

// Marker for a placed City Statue (2026-08-16, VILLAGE_FLAG_PLANNING.md
// section 6) -- same bare-marker shape as VillageFlag/AnvilSurface/
// FurnaceSurface/DeskSurface. Permanent once placed (Ben's explicit call):
// City status never reverts even if the founding Flag is lost or NPC
// count later drops below 10 -- the Statue standing in the world *is* the
// proof, so Exists is a pure "does one exist right now" scan, nothing
// tracks how it got there.
public class CityStatue : MonoBehaviour
{
    public static bool Exists => FindObjectsByType<CityStatue>(FindObjectsSortMode.None).Length > 0;
}
