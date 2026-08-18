using UnityEngine;

// Passive toast when any NPC's work cycle completes and it starts
// waiting for payment (2026-08-17, BUGS_AND_ENHANCEMENTS.md "NPC
// management" -- Ben's ask: awareness of payment coming due without
// having to actively check the Roster). Subscribes to NPCHiring's static
// OnPaymentDue event rather than polling every NPC every frame for a
// state transition.
//
// Y=270 -- checked against every existing top-center toast in the project
// before picking it (10 PlayerNavComputer, 70 PlayerSkills, 110
// PlayerCrafting, 150 PlayerAutosave/PlayerMagic, 190 PlayerBuilding/
// PlayerWriting, 230 PlayerPieceUpgrade), same discipline the
// PlayerAutosave/PlayerCrafting toast-collision bug (v0.3.115-dev)
// established -- that bug happened specifically from checking against
// only one of two existing toasts instead of all of them.
public class PlayerNPCPaymentToast : MonoBehaviour
{
    private const float MessageDuration = 4f;
    private const float ToastY = 270f;

    private string message;
    private float expireTime;

    private void OnEnable() => NPCHiring.OnPaymentDue += HandlePaymentDue;
    private void OnDisable() => NPCHiring.OnPaymentDue -= HandlePaymentDue;

    private void HandlePaymentDue(NPCHiring npc)
    {
        string name = npc.GetComponent<NPCDialogue>()?.DisplayName ?? "An NPC";
        message = $"{name} is now waiting for payment.";
        expireTime = Time.time + MessageDuration;
    }

    private void OnGUI()
    {
        if (message == null || Time.time >= expireTime) return;

        const float width = 420f;
        const float height = 30f;
        var rect = new Rect((Screen.width - width) / 2f, ToastY, width, height);

        DebugGUI.DrawPanel(rect);
        GUI.Label(rect, message, DebugGUI.Header);
    }
}
