using UnityEngine;

// Marker for a world object that counts as a heat source for smelting —
// attached to the placed Furnace. A recipe that sets
// CraftingRecipe.requiresFurnace passes as long as one of these is within
// range (see PlayerCrafting.HasNearbyFurnace) — same shape as
// AnvilSurface/requiresAnvilSurface, just for heat instead of a hard
// hammering surface. Added 2026-08-11 for IronIngotRecipe.
public class FurnaceSurface : MonoBehaviour
{
}
