using UnityEngine;

// An optional second action alongside an object's normal IInteractable one,
// bound to its own key (F) instead of E — e.g. a water source offering
// "Drink" (primary, always available) and "Fill" (secondary, only when the
// player actually has something to fill). Return null/empty from
// GetSecondaryPrompt to hide the secondary option entirely for the current
// player state — PlayerInteraction won't show the prompt or respond to the
// key while it's null/empty.
public interface ISecondaryInteractable
{
    string GetSecondaryPrompt(GameObject player);
    void CompleteSecondary(GameObject player);
}
