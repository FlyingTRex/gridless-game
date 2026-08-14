using UnityEngine;

// Shared 5-tier outcome roll, extracted from PlayerCrafting.cs
// (2026-08-13, SKILL_BOOKS_PLANNING.md build order Phase 0 item 2) so
// PlayerWriting can reuse the exact same formula instead of duplicating
// it — the whole point of "writing reuses PlayerCrafting's outcome-roll
// pattern directly" was no new formula, which only holds if there's
// actually one shared implementation. PlayerCrafting keeps calling this
// exactly as it did when RollOutcome was its own private method; nothing
// about its behavior changed, just where the code lives.
public enum CraftOutcome
{
    BrilliantSuccess,
    Success,
    BarelyFail,
    BadFailure,
    SpectacularFailure,
}

public static class CraftOutcomeRoll
{
    // Skill points above a tier's threshold at which risk bottoms out —
    // a flat cap rather than scaling to the next tier's own threshold,
    // since the gaps (10/25/50/100) are unevenly spaced and a single,
    // consistent "meaningfully practiced past the minimum" number is
    // simpler to reason about and tune.
    public const float RiskMarginCap = 20f;

    // Interpolates each outcome's odds between "just barely qualified"
    // (margin 0 — riskiest) and "meaningfully practiced past it" (margin
    // >= RiskMarginCap — safest). SpectacularFailure isn't its own Lerp —
    // it gets whatever probability mass the other four don't use, so they
    // don't need to be hand-tuned to sum to exactly 1.
    public static CraftOutcome Roll(float skillMargin)
    {
        float t = Mathf.Clamp01(skillMargin / RiskMarginCap);

        float brilliant = Mathf.Lerp(0.02f, 0.10f, t);
        float success = Mathf.Lerp(0.63f, 0.85f, t);
        float barelyFail = Mathf.Lerp(0.20f, 0.04f, t);
        float badFailure = Mathf.Lerp(0.12f, 0.01f, t);

        float roll = Random.value;
        if (roll < brilliant) return CraftOutcome.BrilliantSuccess;
        roll -= brilliant;
        if (roll < success) return CraftOutcome.Success;
        roll -= success;
        if (roll < barelyFail) return CraftOutcome.BarelyFail;
        roll -= barelyFail;
        if (roll < badFailure) return CraftOutcome.BadFailure;
        return CraftOutcome.SpectacularFailure;
    }
}
