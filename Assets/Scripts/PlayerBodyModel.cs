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

    private PlayerTool tool;
    private PlayerBackpack backpack;
    private PlayerBoot boot;
    private PlayerBelt belt;
    private PlayerCanteen canteen;
    private PlayerSunglasses sunglasses;
    private PlayerMiningFaceShield faceShield;
    private PlayerHealthMonitor healthMonitor;
    private PlayerNavComputer navComputer;
    private PlayerShirt shirt;
    private PlayerJeans jeans;

    public bool IsMale => isMale;

    private void Awake()
    {
        tool = GetComponent<PlayerTool>();
        backpack = GetComponent<PlayerBackpack>();
        boot = GetComponent<PlayerBoot>();
        belt = GetComponent<PlayerBelt>();
        canteen = GetComponent<PlayerCanteen>();
        sunglasses = GetComponent<PlayerSunglasses>();
        faceShield = GetComponent<PlayerMiningFaceShield>();
        healthMonitor = GetComponent<PlayerHealthMonitor>();
        navComputer = GetComponent<PlayerNavComputer>();
        shirt = GetComponent<PlayerShirt>();
        jeans = GetComponent<PlayerJeans>();
    }

    // Deliberately Start(), not the end of Awake() above -- ApplyGender's
    // RefreshAnchor() calls reach into 11 other components (PlayerTool,
    // PlayerBackpack, ...) that only work once each one's own Awake() has
    // already populated its fields. Unity guarantees every component's
    // Awake() on this GameObject completes before any Start() runs,
    // regardless of component-list order -- the previous Awake()-time call
    // instead relied on an unguaranteed implicit ordering (component list
    // position) that a prefab conversion can silently disturb. Found live
    // 2026-08-22 via a real NRE (PlayerTool.get_Equipped) during the
    // Multiplayer Phase 3 Bootstrap attempt -- see MULTIPLAYER_PLANNING.md.
    private void Start()
    {
        ApplyGender(isMale);
    }

    // Read by PlayerTool/PlayerBackpack (2026-08-13, equipment visual
    // attachment) — the currently active gendered Visual's own Animator,
    // not a fixed scene reference, since which gender is active can
    // change at runtime.
    public Transform GetBone(HumanBodyBones bone)
    {
        var active = isMale ? maleVisual : femaleVisual;
        var anim = active != null ? active.GetComponent<Animator>() : null;
        return anim != null ? anim.GetBoneTransform(bone) : null;
    }

    // Same "current gender's live Visual" lookup as GetBone, exposed
    // directly for callers (PlayerRangedCombat, 2026-08-15) that need to
    // set Animator parameters rather than just read a bone transform.
    public Animator ActiveAnimator
    {
        get
        {
            var active = isMale ? maleVisual : femaleVisual;
            return active != null ? active.GetComponent<Animator>() : null;
        }
    }

    // Re-anchors every equipped carrier onto the current gender's bones —
    // same sweep a gender toggle already runs. Also the correct way to
    // make a save-restored equipment slot's now-populated-but-still-hidden
    // item become visible/attached (SaveManager, 2026-08-13) without
    // duplicating the 11-carrier list a second place.
    public void RefreshAllAnchors() => ApplyGender(isMale);

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

        // Whatever's currently held/worn was bone-parented under the
        // *previous* gender's bones — re-anchor every equipped carrier
        // onto the newly active model's own bones, or each stays attached
        // to a now-inactive (invisible) body.
        tool?.RefreshAnchor();
        backpack?.RefreshAnchor();
        boot?.RefreshAnchor();
        belt?.RefreshAnchor();
        canteen?.RefreshAnchor();
        sunglasses?.RefreshAnchor();
        faceShield?.RefreshAnchor();
        healthMonitor?.RefreshAnchor();
        navComputer?.RefreshAnchor();
        shirt?.RefreshAnchor();
        jeans?.RefreshAnchor();
    }
}
