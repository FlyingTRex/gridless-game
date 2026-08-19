using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

// Multiplayer Phase 0 infra spike (2026-08-19, MULTIPLAYER_PLANNING.md) --
// deliberately NOT FirstPersonController. A minimal client-authoritative
// mover whose only job is validating the Mirror toolchain end to end (two
// processes connecting, seeing each other move) and giving a first real read
// on the movement-authority open question, before any of the 48
// PlayerXXX.cs scripts get touched. Lives only in the throwaway
// NetworkSpike.unity scene alongside a NetworkTransformReliable component
// (syncDirection = ClientToServer on the prefab) -- this script only ever
// moves the transform when isLocalPlayer is true, exactly the "trusts the
// client" baseline the planning doc's open question is weighing against a
// server-authoritative alternative.
public class NetworkSpikeMovement : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float turnSpeed = 90f;

    private void Update()
    {
        if (!isLocalPlayer) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        float turn = 0f;
        if (keyboard.aKey.isPressed) turn -= 1f;
        if (keyboard.dKey.isPressed) turn += 1f;
        transform.Rotate(Vector3.up, turn * turnSpeed * Time.deltaTime);

        float move = 0f;
        if (keyboard.wKey.isPressed) move += 1f;
        if (keyboard.sKey.isPressed) move -= 1f;
        transform.position += transform.forward * (move * moveSpeed * Time.deltaTime);
    }
}
