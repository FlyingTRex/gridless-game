using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

// V-key toggle between the player's normal first-person eye camera and a
// behind-the-shoulder third-person chase camera. Keeps the existing single
// mouse-look scheme intact -- third person follows wherever the player is
// already facing (yaw from the root, pitch from FirstPersonController.Pitch)
// rather than orbiting independently, so no new look-input handling is
// needed beyond the toggle itself (2026-08-13, player animation build --
// see this session's approved plan).
//
// Revealing the body in third person is just a cullingMask flip, not a
// per-object layer change: the Visual body's renderers sit permanently on
// WornEquipmentLayer (8), the same layer worn equipment already uses and
// which playerCamera's cullingMask already excludes project-wide. Toggling
// that one bit also makes currently-worn equipment visible as a side
// effect, which is correct behavior, not something to work around.
public class PlayerCameraMode : MonoBehaviour
{
    private const int WornEquipmentLayer = 8;

    [SerializeField] private Camera playerCamera;
    [SerializeField] private FirstPersonController controller;
    [SerializeField] private float thirdPersonDistance = 4f;
    [SerializeField] private float cameraCollisionRadius = 0.2f;

    private enum ViewMode { FirstPerson, ThirdPerson }

    private ViewMode mode = ViewMode.FirstPerson;
    private int firstPersonCullingMask;
    private static readonly Vector3 EyeOffset = new Vector3(0f, 1.6f, 0f);
    // Multiplayer per-connection spawning (2026-08-25) -- see
    // FirstPersonController's own field comment for why every sibling on
    // the Player root needs this same gate.
    private NetworkIdentity netIdentity;

    private void Awake()
    {
        if (playerCamera != null) firstPersonCullingMask = playerCamera.cullingMask;
        netIdentity = GetComponent<NetworkIdentity>();
    }

    private void Update()
    {
        if (netIdentity != null && !netIdentity.isLocalPlayer) return;
        if (Cursor.lockState != CursorLockMode.Locked || Keyboard.current == null) return;
        if (Keyboard.current.vKey.wasPressedThisFrame) Toggle();
    }

    private void Toggle()
    {
        mode = mode == ViewMode.FirstPerson ? ViewMode.ThirdPerson : ViewMode.FirstPerson;
        if (playerCamera == null) return;

        if (mode == ViewMode.ThirdPerson)
        {
            playerCamera.cullingMask = firstPersonCullingMask | (1 << WornEquipmentLayer);
        }
        else
        {
            playerCamera.cullingMask = firstPersonCullingMask;
            playerCamera.transform.localPosition = EyeOffset;
            playerCamera.transform.localRotation = Quaternion.Euler(controller.Pitch, 0f, 0f);
        }
    }

    private void LateUpdate()
    {
        if (netIdentity != null && !netIdentity.isLocalPlayer) return;
        if (mode != ViewMode.ThirdPerson || playerCamera == null || controller == null) return;

        Vector3 pivot = transform.TransformPoint(EyeOffset);
        Quaternion camRot = transform.rotation * Quaternion.Euler(controller.Pitch, 0f, 0f);
        Vector3 direction = -(camRot * Vector3.forward);
        Vector3 desired = pivot + direction * thirdPersonDistance;

        if (Physics.SphereCast(pivot, cameraCollisionRadius, direction, out var hit,
                thirdPersonDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            float clamped = Mathf.Max(hit.distance - cameraCollisionRadius, 0.2f);
            desired = pivot + direction * clamped;
        }

        playerCamera.transform.SetPositionAndRotation(desired, camRot);
    }
}
