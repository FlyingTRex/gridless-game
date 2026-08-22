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

    // Phase 1 pilot (2026-08-22) -- proves "client requests a world-object
    // mutation, server validates range and applies it" once, on the
    // NetworkStorageBoxSpike pilot, before the real 32+ PlayerXXX.cs
    // scripts get converted to this shape in Phase 3. Deliberately a flat
    // distance check, not the real game's raycast-based PlayerInteraction --
    // that system doesn't exist on this throwaway mover.
    private const float InteractRange = 3f;
    private const string TestItemName = "TestOre";

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

        if (keyboard.eKey.wasPressedThisFrame) TryInteract(add: true);
        if (keyboard.rKey.wasPressedThisFrame) TryInteract(add: false);
    }

    private void TryInteract(bool add)
    {
        var box = FindNearestBoxInRange();
        if (box == null) return;

        if (add) CmdAddItem(box, TestItemName);
        else CmdRemoveTopItem(box);
    }

    private NetworkStorageBoxSpike FindNearestBoxInRange()
    {
        NetworkStorageBoxSpike nearest = null;
        float nearestDistance = InteractRange;

        foreach (var box in FindObjectsByType<NetworkStorageBoxSpike>(FindObjectsSortMode.None))
        {
            float distance = Vector3.Distance(transform.position, box.transform.position);
            if (distance > nearestDistance) continue;
            nearest = box;
            nearestDistance = distance;
        }

        return nearest;
    }

    [Command]
    private void CmdAddItem(NetworkStorageBoxSpike box, string itemName)
    {
        if (box == null) return;
        if (Vector3.Distance(transform.position, box.transform.position) > InteractRange) return;
        box.items.Add(itemName);
    }

    [Command]
    private void CmdRemoveTopItem(NetworkStorageBoxSpike box)
    {
        if (box == null || box.items.Count == 0) return;
        if (Vector3.Distance(transform.position, box.transform.position) > InteractRange) return;
        box.items.RemoveAt(box.items.Count - 1);
    }
}
