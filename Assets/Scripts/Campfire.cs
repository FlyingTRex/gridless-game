using UnityEngine;

// The first real wish, per design-brief.md's Magic System section — Ben's
// original "wish it would..." pitch made concrete: hold E to wish an unlit
// campfire would catch. Structured like ResourceNode/ChoppableTree (hold,
// skill-tiered duration, Complete() does the effect), except the gate is
// PlayerMagic (lineage known + skill tier + Will), not a tool.
public class Campfire : MonoBehaviour, IInteractable
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
            ? $"Hold to wish it would light (requires {sparkWish.lineage.skillName})"
            : "Hold to wish it would light");

    public bool IsInstant => false;

    public float GetHoldDuration(GameObject player) =>
        player.GetComponent<PlayerSkills>().GetHoldDuration(sparkWish.lineage);

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        SetLit(false);
    }

    public void Complete(GameObject player)
    {
        if (isLit) return;

        var magic = player.GetComponent<PlayerMagic>();
        if (magic == null || !magic.TryWish(sparkWish)) return;

        SetLit(true);
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
