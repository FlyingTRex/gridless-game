using UnityEngine;

public interface IEquippable
{
    string DisplayName { get; }

    // Fully hides the object (used while it's stored as inert data — in
    // the main inventory, or inside another container's Inventory).
    void Stash();

    // Worn/held (visible, non-collidable, follows anchor) when anchor is
    // set, or released into the world as a normal physical object when
    // anchor is null.
    void SetCarried(bool value, Transform anchor);
}
