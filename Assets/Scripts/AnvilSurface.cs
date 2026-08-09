using UnityEngine;

// Marker for a world object that counts as a hard metalworking surface —
// attached to Boulder and the placed Anvil. A recipe that sets
// CraftingRecipe.requiresAnvilSurface passes as long as any one of these
// is within range (see PlayerCrafting.HasNearbyAnvilSurface) — the same
// "any of several things satisfies this" shape as requiredTools' "any
// tier counts" convention, just for a place instead of an item.
public class AnvilSurface : MonoBehaviour
{
}
