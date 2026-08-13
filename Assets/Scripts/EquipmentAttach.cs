using UnityEngine;

// Shared placement math for anything bone-attached to a Humanoid rig —
// extracted 2026-08-13 when the player got the same treatment NPCs
// already had (NPCEquipmentVisual.cs), so both use identical, already-
// live-tested logic instead of two copies quietly drifting apart.
//
// positionOffset/eulerOffset are interpreted relative to root's own
// forward/right/up, not the target bone's own local axes — a hand/chest/
// head bone's local space reflects its bind-pose orientation, which is
// rig-specific and not something to guess blind (this is the actual fix
// behind v0.3.39-dev's "Backpack misplaced" bug: the original code used
// the bone's local space, so "0.15 behind" meant whatever direction that
// bone's own Z axis happened to point, not behind the character).
public static class EquipmentAttach
{
    public static void Place(Transform target, Transform bone, Transform root, Vector3 positionOffset, Vector3 eulerOffset)
    {
        target.position = bone.position + root.TransformVector(positionOffset);
        target.rotation = root.rotation * Quaternion.Euler(eulerOffset);
    }

    // One-call version of the pattern every PlayerXXX carrier repeats:
    // resolve a bone (falling back to a fixed scene Transform, then root,
    // if PlayerBodyModel or the bone lookup comes back null), SetCarried
    // onto it, then place with the root-relative offset. Added 2026-08-13
    // when the full equipment-visual sweep (Boot/Belt/Canteen/Sunglasses/
    // MiningFaceShield/PersonalHealthMonitor/NavigationComputer/Shirt/
    // Jeans) made the two-line Resolve+Place pattern worth deduplicating
    // — used by every carrier now, including Tool/Backpack (retrofitted,
    // no behavior change for either).
    public static void Carry(IEquippable equippable, Transform equippableTransform, PlayerBodyModel bodyModel,
        HumanBodyBones bone, Transform fallback, Transform root, Vector3 positionOffset, Vector3 eulerOffset)
    {
        var anchorBone = bodyModel != null ? bodyModel.GetBone(bone) : null;
        var anchor = anchorBone != null ? anchorBone : (fallback != null ? fallback : root);

        equippable.SetCarried(true, anchor);
        Place(equippableTransform, anchor, root, positionOffset, eulerOffset);
    }
}
