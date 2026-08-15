using UnityEngine;

// Purely cosmetic — PlayerRangedCombat's hit resolution is already
// instant hitscan (same as PlayerCombat's punch), so this doesn't affect
// damage timing at all; it just gives the shot a visible flight instead
// of the previous "silent, invisible hit" the punch precedent left it
// with. One shared visual reused for every Arrow tier, matching the
// Hunting Expansion design's own choice not to give arrows per-tier
// visual variation.
public class FlyingArrow : MonoBehaviour
{
    private const float LingerSecondsAfterArrival = 2f;

    private Vector3 start;
    private Vector3 end;
    private float duration;
    private float elapsed;
    private bool arrived;

    public void Launch(Vector3 startPos, Vector3 endPos, float speed)
    {
        start = startPos;
        end = endPos;
        transform.position = start;
        if ((end - start).sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(end - start);
        duration = Mathf.Max((end - start).magnitude / Mathf.Max(speed, 0.1f), 0.05f);
        elapsed = 0f;
        arrived = false;
    }

    private void Update()
    {
        if (arrived) return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        transform.position = Vector3.Lerp(start, end, t);

        if (t >= 1f)
        {
            arrived = true;
            Destroy(gameObject, LingerSecondsAfterArrival);
        }
    }
}
