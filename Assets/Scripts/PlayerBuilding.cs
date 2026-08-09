using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Placement controller for the Building System (design-brief.md, 2026-08-08).
// Scoped to Foundation-only for this first pass — Wall/Pole/Door reuse the
// same BuildPiece/BuildSocket machinery later, not a second system.
//
// Two flows, matching the design doc exactly:
// - Free placement (nothing compatible nearby): LMB drops a ghost at the
//   camera raycast's hit point; scroll wheel rotates it in place; LMB again
//   confirms. Modeled as Following -> Locked -> confirm.
// - Edge-snapped placement (a compatible open BuildSocket in range): the
//   ghost snaps automatically, position and rotation both implied by the
//   socket, so a single LMB press confirms immediately.
//
// Deliberately NOT hidden like magic (see PlayerMagic/no-UI-hints) —
// Building is a learnable system, not a mystery mechanic, so this draws a
// real ghost and real messages.
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerSkills))]
[RequireComponent(typeof(PlayerInteraction))]
public class PlayerBuilding : MonoBehaviour
{
    private enum Phase { Following, Locked }

    [SerializeField] private BuildPiece[] allPieces;
    [SerializeField] private float interactRange = 10f;
    [SerializeField] private float snapRadius = 1.5f;
    // Foundation is a 5m x 5m square — half-width used to offset a newly
    // snapped panel directly outward from the socket it's attaching to.
    // Only correct for square, axis-aligned pieces like Foundation; Wall/
    // Door will need real per-socket alignment math when they're added.
    [SerializeField] private float panelHalfSize = 2.5f;
    [SerializeField] private float rotationStepDegrees = 90f;
    [SerializeField] private float scrollThreshold = 20f;
    private const float MessageDuration = 3f;

    private PlayerInventory inventory;
    private PlayerSkills skills;
    private PlayerInteraction interaction;

    private BuildPiece armedPiece;
    private SocketType[] armedSocketTypes = System.Array.Empty<SocketType>();
    private GameObject ghost;
    private Phase phase = Phase.Following;
    private BuildSocket snappedSocket;
    private Vector3 lockedPosition;
    private Quaternion lockedRotation;
    private bool currentValid;

    private Material ghostMaterial;
    private string message;
    private float messageExpireTime;

    public BuildPiece ArmedPiece => armedPiece;
    public IReadOnlyList<BuildPiece> AllPieces => allPieces;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        skills = GetComponent<PlayerSkills>();
        interaction = GetComponent<PlayerInteraction>();
    }

    public bool CanPlace(BuildPiece piece) =>
        piece != null && (piece.trainedSkill == null
            || skills.GetLevel(piece.trainedSkill) >= CraftTierScale.SkillRequirement(piece.unlockTier));

    // Called by BuildScreen's Select button.
    public void ArmPiece(BuildPiece piece)
    {
        if (piece == armedPiece) return;

        ClearGhost();
        armedPiece = piece;
        armedSocketTypes = piece != null && piece.prefab != null
            ? System.Array.ConvertAll(piece.prefab.GetComponentsInChildren<BuildSocket>(), s => s.SocketType)
            : System.Array.Empty<SocketType>();
        phase = Phase.Following;
    }

    private void Update()
    {
        if (armedPiece == null || armedPiece.prefab == null || interaction?.PlayerCamera == null)
        {
            ClearGhost();
            return;
        }

        if (phase == Phase.Following)
            ResolveFollowing();

        HandleInput();
    }

    private void ResolveFollowing()
    {
        var cam = interaction.PlayerCamera.transform;
        if (!Physics.Raycast(cam.position, cam.forward, out var hit, interactRange))
        {
            ClearGhost();
            currentValid = false;
            return;
        }

        var socket = FindNearbySocket(hit.point);
        if (socket != null)
        {
            snappedSocket = socket;
            Vector3 pos = socket.transform.position + socket.transform.forward * panelHalfSize;
            Quaternion rot = socket.transform.root.rotation;
            ShowGhost(pos, rot);
            currentValid = true;
        }
        else
        {
            snappedSocket = null;
            ShowGhost(hit.point, Quaternion.identity);
            currentValid = true;
        }
    }

    // Any of the armed piece's own socket types compatible with the
    // candidate, unoccupied, and within snapRadius — closest wins.
    private BuildSocket FindNearbySocket(Vector3 point)
    {
        BuildSocket best = null;
        float bestDist = snapRadius;

        foreach (var socket in FindObjectsByType<BuildSocket>(FindObjectsSortMode.None))
        {
            if (socket.Occupied) continue;

            bool compatible = false;
            foreach (var mine in armedSocketTypes)
            {
                if (socket.IsCompatibleWith(mine)) { compatible = true; break; }
            }
            if (!compatible) continue;

            float dist = Vector3.Distance(socket.transform.position, point);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = socket;
            }
        }

        return best;
    }

    private void HandleInput()
    {
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        if (mouse == null) return;

        if (phase == Phase.Following)
        {
            if (ghost != null && currentValid && mouse.leftButton.wasPressedThisFrame)
            {
                if (snappedSocket != null)
                {
                    Confirm(ghost.transform.position, ghost.transform.rotation, snappedSocket);
                }
                else
                {
                    lockedPosition = ghost.transform.position;
                    lockedRotation = ghost.transform.rotation;
                    phase = Phase.Locked;
                }
            }
        }
        else // Locked — free placement's rotate-then-confirm step
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > scrollThreshold)
                lockedRotation *= Quaternion.Euler(0f, rotationStepDegrees, 0f);
            else if (scroll < -scrollThreshold)
                lockedRotation *= Quaternion.Euler(0f, -rotationStepDegrees, 0f);

            ShowGhost(lockedPosition, lockedRotation);

            if (mouse.leftButton.wasPressedThisFrame)
            {
                Confirm(lockedPosition, lockedRotation, null);
            }
            else if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                phase = Phase.Following;
            }
        }
    }

    private void Confirm(Vector3 position, Quaternion rotation, BuildSocket socket)
    {
        if (!HasIngredients(armedPiece))
        {
            ShowMessage("Not enough materials.");
            phase = Phase.Following;
            return;
        }

        RemoveIngredients(armedPiece);
        var real = Instantiate(armedPiece.prefab, position, rotation);
        real.AddComponent<PlacedPiece>().Piece = armedPiece;
        skills?.GainExperience(armedPiece.trainedSkill, armedPiece.skillGain);

        if (socket != null)
        {
            socket.Occupied = true;

            BuildSocket nearest = null;
            float nearestDist = float.MaxValue;
            foreach (var s in real.GetComponentsInChildren<BuildSocket>())
            {
                float dist = Vector3.Distance(s.transform.position, socket.transform.position);
                if (dist < nearestDist) { nearestDist = dist; nearest = s; }
            }
            if (nearest != null) nearest.Occupied = true;
        }

        phase = Phase.Following;
    }

    private bool HasIngredients(BuildPiece piece)
    {
        if (piece.ingredients == null) return true;
        foreach (var ingredient in piece.ingredients)
        {
            if (ingredient == null || ingredient.item == null) continue;
            if (inventory.GetCount(ingredient.item) < ingredient.count) return false;
        }
        return true;
    }

    private void RemoveIngredients(BuildPiece piece)
    {
        if (piece.ingredients == null) return;
        foreach (var ingredient in piece.ingredients)
        {
            if (ingredient == null || ingredient.item == null) continue;
            inventory.RemoveItem(ingredient.item, ingredient.count);
        }
    }

    private void ShowGhost(Vector3 position, Quaternion rotation)
    {
        if (ghost == null)
        {
            ghost = Instantiate(armedPiece.prefab, position, rotation);
            foreach (var col in ghost.GetComponentsInChildren<Collider>())
                col.enabled = false;
            foreach (var s in ghost.GetComponentsInChildren<BuildSocket>())
                Destroy(s);

            if (ghostMaterial == null)
                ghostMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = new Color(0.4f, 0.85f, 1f, 0.6f),
                };
            foreach (var r in ghost.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = ghostMaterial;
        }
        else
        {
            ghost.transform.SetPositionAndRotation(position, rotation);
        }
    }

    private void ClearGhost()
    {
        if (ghost != null) Destroy(ghost);
        ghost = null;
        snappedSocket = null;
        currentValid = false;
    }

    private void ShowMessage(string text)
    {
        message = text;
        messageExpireTime = Time.time + MessageDuration;
    }

    // Below PlayerMagic's message (y=150) — same stacking convention.
    private void OnGUI()
    {
        if (message == null || Time.time >= messageExpireTime) return;

        const float width = 420f;
        const float height = 30f;
        var rect = new Rect((Screen.width - width) / 2f, 190f, width, height);

        DebugGUI.DrawPanel(rect);
        GUI.Label(rect, message, DebugGUI.Header);
    }
}
