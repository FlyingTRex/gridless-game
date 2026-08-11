using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerSkills))]
[RequireComponent(typeof(PlayerVitals))]
[RequireComponent(typeof(PlayerMagic))]
public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactRange = 3f;

    // Effect magnitude for any AnyRigidbody-targeting wish (just Push
    // today) — kept here rather than per-WishRecipe since only one wish
    // needs it so far; move it onto WishRecipe if a second AnyRigidbody
    // wish ever wants a different force.
    [SerializeField] private float pushForce = 6f;

    // Restoration's Heal Self — the first Unconditional wish (2026-08-08).
    // Special-cased by asset reference, same pattern as pushForce above:
    // fine for one wish, revisit (e.g. a small per-wish effect interface)
    // if a second Unconditional wish ships with a genuinely different effect.
    [SerializeField] private WishRecipe healSelfWish;
    [SerializeField] private float healSelfAmount = 10f;
    [SerializeField] private float healSelfDuration = 30f;

    private IInteractable current;
    private ISecondaryInteractable currentSecondary;
    private string currentSecondaryPrompt;
    private float holdProgress;

    // Lets an external "point at a world object and press E to confirm"
    // flow (e.g. PlayerNPCDeposit, Chunk 5 of the Hireable NPCs build,
    // 2026-08-10) claim E for its own purposes without also triggering
    // whatever IInteractable the player happens to be aiming at (a
    // StorageBox's own E is "pick up the box" -- confirming it as a
    // deposit target would otherwise also pick it up in the same
    // keystroke). Only suppresses Complete()/CompleteSecondary() calls,
    // not ResolveTarget() itself, so `current` doesn't go stale while
    // suppressed and hold progress can't silently keep counting up either.
    public bool SuppressInteraction { get; set; }

    // Resolved fresh each frame by ResolveWishTarget. currentWishTarget is
    // null for the generic Rigidbody-push case (no specific IWishTarget on
    // the hit object) — HandleWish branches on that to know which
    // completion path to run.
    private GameObject currentWishGameObject;
    private WishRecipe currentWish;
    private IWishTarget currentWishTarget;
    private float wishHoldProgress;

    private PlayerSkills skills;
    private PlayerMagic magic;
    private PlayerVitals vitals;

    private Texture2D barBackgroundTex;
    private Texture2D barFillTex;

    // Exposed so other player components (e.g. PlayerRenaming) can reuse
    // the same camera for their own raycasts instead of each needing their
    // own serialized reference wired up in the scene.
    public Camera PlayerCamera => playerCamera;

    private void Awake()
    {
        skills = GetComponent<PlayerSkills>();
        magic = GetComponent<PlayerMagic>();
        vitals = GetComponent<PlayerVitals>();
    }

    private void Update()
    {
        ResolveTarget();

        var keyboard = Keyboard.current;

        if (current != null && keyboard != null && !SuppressInteraction)
        {
            if (current.IsInstant)
            {
                if (keyboard.eKey.wasPressedThisFrame)
                    current.Complete(gameObject);
            }
            else
            {
                // Hold-and-release: keep E down to fill the bar, let go (or
                // look away — ResolveTarget clears `current` too) to cancel
                // and forfeit progress. Replaces the old punch-N-times/
                // hitsToBreak model on resource nodes and trees, and covers
                // every other non-instant IInteractable the same way.
                if (keyboard.eKey.isPressed)
                {
                    holdProgress += Time.deltaTime;
                    if (holdProgress >= current.GetHoldDuration(gameObject))
                    {
                        current.Complete(gameObject);
                        holdProgress = 0f;
                    }
                }
                else
                {
                    holdProgress = 0f;
                }
            }
        }
        else
        {
            holdProgress = 0f;
        }

        if (!SuppressInteraction && !string.IsNullOrEmpty(currentSecondaryPrompt) && keyboard != null && keyboard.fKey.wasPressedThisFrame)
            currentSecondary.CompleteSecondary(gameObject);

        ResolveWishTarget();
        HandleWish(keyboard);
    }

    private void ResolveTarget()
    {
        current = null;
        currentSecondary = null;
        currentSecondaryPrompt = null;

        if (playerCamera == null) return;

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward,
                out var hit, interactRange))
        {
            current = hit.collider.GetComponentInParent<IInteractable>();
            currentSecondary = hit.collider.GetComponentInParent<ISecondaryInteractable>();
            if (currentSecondary != null)
                currentSecondaryPrompt = currentSecondary.GetSecondaryPrompt(gameObject);
        }
    }

    // Separate raycast from ResolveTarget's — R needs its own resolution
    // pass since it dispatches off the player's *selected* wish
    // (PlayerMagic.SelectedWish, 2026-08-08 — "default skill" per Ben)
    // rather than whatever IInteractable the crosshair happens to hit.
    // How a valid target is found depends entirely on the selected wish's
    // own WishTargeting mode — this no longer tries IWishTarget first and
    // falls back to Rigidbody; it only ever checks the one mode the
    // selected wish actually needs.
    private void ResolveWishTarget()
    {
        currentWishGameObject = null;
        currentWish = null;
        currentWishTarget = null;

        if (magic == null) return;
        var selected = magic.SelectedWish;
        if (selected == null) return;

        switch (selected.targeting)
        {
            case WishTargeting.Unconditional:
                // No physical target needed — always "present" once
                // selected; PlayerMagic.CanAttempt still gates the actual
                // attempt on lineage/skill/Will when the hold completes.
                // Uses this object's own GameObject as a stable sentinel
                // so it never reads as "changed" frame to frame.
                currentWishGameObject = gameObject;
                currentWish = selected;
                break;

            case WishTargeting.SpecificObject:
                if (RaycastCrosshair(out var hit1))
                {
                    var wishable = hit1.collider.GetComponentInParent<IWishTarget>();
                    // Must match the *selected* wish specifically, not just
                    // "any wish this object happens to offer" — an object
                    // could in principle offer a different wish than the
                    // one currently selected.
                    if (wishable != null && wishable.GetWish(magic) == selected)
                    {
                        currentWishGameObject = hit1.collider.gameObject;
                        currentWish = selected;
                        currentWishTarget = wishable;
                    }
                }
                break;

            case WishTargeting.AnyRigidbody:
                if (RaycastCrosshair(out var hit2))
                {
                    var rb = hit2.collider.GetComponentInParent<Rigidbody>();
                    if (rb != null)
                    {
                        currentWishGameObject = rb.gameObject;
                        currentWish = selected;
                    }
                }
                break;
        }
    }

    private bool RaycastCrosshair(out RaycastHit hit)
    {
        hit = default;
        return playerCamera != null && Physics.Raycast(
            playerCamera.transform.position, playerCamera.transform.forward, out hit, interactRange);
    }

    // Same hold-and-release shape as E: keep R down on a valid target to
    // fill the bar, let go (or look away — ResolveWishTarget clears the
    // target too) to cancel and forfeit progress. On completion, resolves
    // through PlayerMagic.TryWish's success/failure roll either way, then
    // routes the effect to the target's own OnWishComplete (specific
    // wishes) or a direct AddForce (the generic Push fallback).
    //
    // Bug fixed 2026-08-09: this used to also require
    // currentWishGameObject == <the previous frame's resolved object>,
    // stricter than E's hold (which has no such check at all) — any
    // one-frame raycast flicker (aim jitter, a multi-collider model like
    // Backpack briefly resolving a different collider) silently reset
    // progress to 0 with zero feedback, since wishes deliberately show no
    // progress bar. Confirmed live: holding R on a Backpack (which does
    // have a Rigidbody) for several seconds produced nothing at all, no
    // message either way — the hold was never actually completing.
    private void HandleWish(Keyboard keyboard)
    {
        if (keyboard == null)
        {
            wishHoldProgress = 0f;
            return;
        }

        if (currentWishGameObject != null && keyboard.rKey.isPressed)
        {
            wishHoldProgress += Time.deltaTime;
            if (wishHoldProgress >= skills.GetHoldDuration(currentWish.lineage))
            {
                bool succeeded = magic.TryWish(currentWish);

                // Dispatch explicitly on targeting mode, not on whether
                // currentWishTarget happens to be null — Unconditional
                // wishes have no target object either, but aren't Push.
                if (currentWishTarget != null)
                {
                    currentWishTarget.OnWishComplete(gameObject, succeeded);
                }
                else if (currentWish.targeting == WishTargeting.AnyRigidbody
                    && succeeded && currentWishGameObject.TryGetComponent(out Rigidbody rb))
                {
                    rb.AddForce(playerCamera.transform.forward * pushForce, ForceMode.Impulse);
                }
                else if (currentWish.targeting == WishTargeting.Unconditional
                    && succeeded && currentWish == healSelfWish)
                {
                    vitals.StartHealOverTime(healSelfAmount, healSelfDuration);
                }
                // Any other Unconditional wish: succeeded/failed still
                // resolves through TryWish above (Will spent, skill trained,
                // message shown on failure) — just no visible effect until
                // it gets its own case here, same as Heal Self just did.

                wishHoldProgress = 0f;
            }
        }
        else
        {
            wishHoldProgress = 0f;
        }
    }

    private const float BarWidth = 200f;
    private const float BarHeight = 10f;

    private void OnGUI()
    {
        DrawCrosshair();

        string text = null;
        float holdDuration = 0f;
        if (current != null)
        {
            text = current.Prompt;
            if (!current.IsInstant)
            {
                holdDuration = current.GetHoldDuration(gameObject);
                text += $" ({Mathf.CeilToInt(holdDuration - holdProgress)}s)";
            }
        }

        // E/F still disambiguate with a bracketed key label between each
        // other, unchanged.
        if (!string.IsNullOrEmpty(currentSecondaryPrompt))
        {
            if (!string.IsNullOrEmpty(text)) text = $"[E] {text}";
            text = string.IsNullOrEmpty(text) ? $"[F] {currentSecondaryPrompt}" : $"{text}    [F] {currentSecondaryPrompt}";
        }

        // Deliberately no UI for wishes at all (Ben's call, 2026-08-08) —
        // no prompt text, no progress bar, nothing naming R or what it
        // does. Magic is meant to be discovered and played with, not
        // explained on screen; ResolveWishTarget/HandleWish still run
        // exactly as before, this only removes the player-facing hint.
        if (text != null)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(Screen.width / 2f - 150, Screen.height / 2f + 30, 300, 30), text, style);
        }

        if (current != null && !current.IsInstant && holdProgress > 0f)
            DrawHoldBar(holdProgress / Mathf.Max(holdDuration, 0.01f));
    }

    private void DrawHoldBar(float fraction)
    {
        if (barBackgroundTex == null) barBackgroundTex = SolidTexture(new Color(0f, 0f, 0f, 0.6f));
        if (barFillTex == null) barFillTex = SolidTexture(new Color(0.25f, 0.85f, 0.25f));

        var rect = new Rect(Screen.width / 2f - BarWidth / 2f, Screen.height / 2f + 62f, BarWidth, BarHeight);
        GUI.DrawTexture(rect, barBackgroundTex);

        var fillRect = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height);
        GUI.DrawTexture(fillRect, barFillTex);
    }

    private static Texture2D SolidTexture(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }

    private void DrawCrosshair()
    {
        const float size = 6f;
        const float thickness = 2f;
        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        DrawOutlinedRect(new Rect(cx - thickness / 2f, cy - size, thickness, size * 2f));
        DrawOutlinedRect(new Rect(cx - size, cy - thickness / 2f, size * 2f, thickness));
    }

    private static void DrawOutlinedRect(Rect r)
    {
        GUI.DrawTexture(new Rect(r.x - 1, r.y - 1, r.width + 2, r.height + 2), Texture2D.blackTexture);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
    }
}
