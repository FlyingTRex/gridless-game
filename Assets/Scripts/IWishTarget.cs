using UnityEngine;

// A world object with its own specific wish, as opposed to the generic
// "any nearby Rigidbody can be Pushed" fallback (see PlayerInteraction's
// R-handling) — Campfire/Spark is the first example. Distinct from
// IInteractable: every wish is bound to R, not E, and gates on PlayerMagic
// (lineage known + skill tier + Will), not a tool.
public interface IWishTarget
{
    string Prompt { get; }

    // Null if this target has nothing to offer the given magic right now
    // (wrong lineage known, or e.g. the campfire's already lit) — signals
    // PlayerInteraction that R shouldn't do anything here at all.
    WishRecipe GetWish(PlayerMagic magic);

    void OnWishComplete(GameObject player, bool succeeded);
}
