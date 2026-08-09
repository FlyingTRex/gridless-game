using UnityEngine;

// The first real wish, per design-brief.md's Magic System section — Ben's
// original "wish it would..." pitch made concrete: hold R (unified across
// all magic, 2026-08-08) to wish an unlit campfire would catch. IWishTarget,
// not IInteractable — R is a dedicated magic channel, gated on PlayerMagic
// (lineage known + skill tier + Will), not a tool, and PlayerInteraction
// itself handles the hold/roll; this class only supplies the prompt and
// applies the effect once a wish actually resolves.
public class Campfire : MonoBehaviour, IWishTarget
{
    [SerializeField] private WishRecipe sparkWish;
    [SerializeField] private Material unlitMaterial;
    [SerializeField] private Material litMaterial;
    [SerializeField] private Light fireLight;

    private Renderer[] renderers;
    private bool isLit;

    public string Prompt => isLit
        ? "Campfire (lit)"
        : (sparkWish != null && sparkWish.lineage != null
            ? $"Wish it would light (requires {sparkWish.lineage.skillName})"
            : "Wish it would light");

    // Null (no wish available) once already lit, or if the looking player
    // doesn't know Spark's lineage — PlayerInteraction treats a null return
    // as "R does nothing here."
    public WishRecipe GetWish(PlayerMagic magic) =>
        !isLit && magic != null && magic.IsLineageKnown(sparkWish != null ? sparkWish.lineage : null)
            ? sparkWish
            : null;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        SetLit(false);
    }

    public void OnWishComplete(GameObject player, bool succeeded)
    {
        if (succeeded) SetLit(true);
    }

    private void SetLit(bool lit)
    {
        isLit = lit;

        var mat = lit ? litMaterial : unlitMaterial;
        if (mat != null)
            foreach (var r in renderers)
                r.sharedMaterial = mat;

        if (fireLight != null)
            fireLight.enabled = lit;
    }
}
