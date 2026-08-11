using UnityEngine;

// A kickable soccer ball — pure physics toy, not an IInteractable (E/F
// aren't involved at all). Touching it while it's not on cooldown launches
// it a random distance in front of whoever touched it, using the real
// projectile-range formula (speed = sqrt(distance * g / sin(2 * angle)))
// so it actually lands roughly where the random distance says, not just
// "some force in some direction." Sprinting kicks it farther and higher —
// Ben's ask (2026-08-09): normal 3-7m at up to 30 degrees, sprinting
// 5-12m at up to 45 degrees.
//
// TryKick is public and called from FirstPersonController.OnControllerColliderHit,
// not just OnCollisionEnter below — found live, Ben walked straight through
// the ball with no kick at all. Root cause: CharacterController.Move()
// resolves movement through its own kinematic capsule cast, not the normal
// PhysX solver, so it never fires OnCollisionEnter on whatever it touches
// (a well-known Unity quirk, not a physics setting). OnCollisionEnter stays
// here too, for genuine Rigidbody-vs-Rigidbody contact (e.g. a thrown or
// rolling object hitting the ball) — the two paths cover the two different
// ways something can touch it.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class SoccerBall : MonoBehaviour
{
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 7f;
    [SerializeField] private float maxAngle = 30f;
    [SerializeField] private float sprintMinDistance = 5f;
    [SerializeField] private float sprintMaxDistance = 12f;
    [SerializeField] private float sprintMaxAngle = 45f;

    // A real kick's arc still has some loft even at its flattest — this
    // also keeps the projectile-range formula (which divides by
    // sin(2*angle)) away from the angle=0 singularity.
    [SerializeField] private float minAngle = 5f;

    // Guards against re-kicking every physics tick while still in contact
    // with the player (a rolling ball can stay touching for several
    // frames) — not a real gameplay cooldown, just enough to let the ball
    // separate from the collider first.
    [SerializeField] private float kickCooldown = 0.4f;

    private Rigidbody rb;
    private float nextKickTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryKick(collision.gameObject);
    }

    public void TryKick(GameObject other)
    {
        if (Time.time < nextKickTime) return;

        var controller = other.GetComponent<CharacterController>();
        if (controller == null) return;

        var vitals = other.GetComponent<PlayerVitals>();
        bool sprinting = vitals != null && vitals.IsSprinting;

        float distance = sprinting
            ? Random.Range(sprintMinDistance, sprintMaxDistance)
            : Random.Range(minDistance, maxDistance);
        float angle = Random.Range(minAngle, sprinting ? sprintMaxAngle : maxAngle);

        Vector3 flatForward = other.transform.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.0001f) flatForward = other.transform.forward;
        flatForward.Normalize();

        Vector3 launchDir = Quaternion.AngleAxis(-angle, other.transform.right) * flatForward;

        float gravity = Mathf.Abs(Physics.gravity.y);
        float angleRad = angle * Mathf.Deg2Rad;
        float speed = Mathf.Sqrt(distance * gravity / Mathf.Sin(2f * angleRad));

        rb.linearVelocity = launchDir * speed;
        nextKickTime = Time.time + kickCooldown;
    }
}
