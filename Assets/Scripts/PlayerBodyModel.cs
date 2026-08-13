using UnityEngine;

// Male/Female body toggle for the player's visible model (2026-08-13),
// surfaced as a Player-tab control in GameMenuScreen (the ` menu). Direct
// follow-up to v0.3.34-dev's player-visible-body build (PlayerAnimatorDriver.cs/
// PlayerCameraMode.cs), which shipped only a fixed Male HumanDummy Visual —
// this adds the missing choice.
//
// Both gendered Visual instances are pre-instantiated as siblings under
// the same parent and start/stop active rather than Instantiate/Destroy
// at toggle time -- PlayerAnimatorDriver and NPCVisualGroundFix each hold
// a direct serialized reference into whichever Visual is "the" body, and
// destroying the active one out from under them would orphan those
// references (the same "don't carry a reference across a destroy
// boundary" caution CLAUDE.md documents for editor-script prefab-content
// edits, just applying to a runtime swap instead). SetActive-toggling
// both instead means the only thing that needs to change on a toggle is
// which instance those two components point at.
public class PlayerBodyModel : MonoBehaviour
{
    [SerializeField] private GameObject maleVisual;
    [SerializeField] private GameObject femaleVisual;
    [SerializeField] private PlayerAnimatorDriver animatorDriver;
    [SerializeField] private NPCVisualGroundFix groundFix;

    // Male is the default -- matches what v0.3.34-dev shipped, so a fresh
    // scene load looks identical to before this toggle existed unless the
    // player actually changes it.
    private bool isMale = true;

    public bool IsMale => isMale;

    private void Awake()
    {
        ApplyGender(isMale);
    }

    public void SetGender(bool male)
    {
        if (male == isMale) return;
        ApplyGender(male);
    }

    private void ApplyGender(bool male)
    {
        isMale = male;

        if (maleVisual != null) maleVisual.SetActive(male);
        if (femaleVisual != null) femaleVisual.SetActive(!male);

        var active = male ? maleVisual : femaleVisual;
        if (active == null) return;

        animatorDriver?.SetAnimator(active.GetComponent<Animator>());
        groundFix?.SetVisual(active.transform, active.GetComponentsInChildren<Renderer>());
    }
}
