using UnityEngine;

public interface IInteractable
{
    string Prompt { get; }
    bool IsInstant { get; }
    // Takes the acting player because duration is skill-dependent (see
    // CraftTierScale.HoldDuration/PlayerSkills.GetHoldDuration) — a fixed
    // per-item constant isn't enough once low-skill/high-skill players
    // take different amounts of time on the same node.
    float GetHoldDuration(GameObject player);
    void Complete(GameObject player);
}
