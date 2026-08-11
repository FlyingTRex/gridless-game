using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Click-to-upgrade / hold-5s-to-destroy on an already-placed building
// piece — see design-brief.md's Building System section (2026-08-08).
// Deliberately NOT built on IInteractable's hold-and-release model: every
// other hold in the game treats releasing early as "cancelled, nothing
// happened," but here releasing early *is* the action (upgrade), and only
// holding past the threshold does something else (destroy) — a genuinely
// different shape, so this runs its own raycast/E-handling rather than
// reusing PlayerInteraction's.
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(PlayerSkills))]
[RequireComponent(typeof(PlayerEquipment))]
[RequireComponent(typeof(PlayerInteraction))]
public class PlayerPieceUpgrade : MonoBehaviour
{
    [SerializeField] private ItemDefinition[] hammerTiers;
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private float destroyDuration = 5f;
    [SerializeField] private float storageRange = 10f;
    private const float MessageDuration = 3f;

    private PlayerInventory inventory;
    private PlayerSkills skills;
    private PlayerEquipment equipment;
    private PlayerInteraction interaction;
    private PlayerBackpack backpackCarrier;
    private readonly List<StorageBox> nearbyStorages = new List<StorageBox>();

    private PlacedPiece currentTarget;
    private PlacedPiece lastTarget;
    private float holdTime;

    private string message;
    private float messageExpireTime;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        skills = GetComponent<PlayerSkills>();
        equipment = GetComponent<PlayerEquipment>();
        interaction = GetComponent<PlayerInteraction>();
        backpackCarrier = GetComponent<PlayerBackpack>();
    }

    // Same reach as PlayerBuilding.ReachableInventories — main inventory
    // first, then an equipped Backpack, then any nearby StorageBox. Found
    // missing live (2026-08-10): Ben had 20 Plank on hand (well over the
    // 12 an upgrade needed) and still got "Not enough materials" —
    // HasIngredients/RemoveIngredients were only ever checking
    // inventory.GetCount() directly, the player's own main-inventory
    // slots, same class of bug as the original "can't eat a Berry" one
    // (an item landing in a hand/backpack slot via PlayerLoot.Receive()
    // is invisible to a check that only looks at the main list).
    private IEnumerable<Inventory> ReachableInventories()
    {
        yield return inventory.Inventory;

        var backpack = backpackCarrier != null ? backpackCarrier.Equipped : null;
        if (backpack != null)
            yield return backpack.Inventory;

        StorageBox.FindNearby(transform.position, storageRange, nearbyStorages);
        foreach (var box in nearbyStorages)
            yield return box.Inventory;
    }

    private bool HammerInHand
    {
        get
        {
            if (equipment == null || hammerTiers == null) return false;
            foreach (var tier in hammerTiers)
                if (tier != null && equipment.HasInHand(tier)) return true;
            return false;
        }
    }

    private void Update()
    {
        ResolveTarget();
        HandleInput();
    }

    private void ResolveTarget()
    {
        currentTarget = null;
        if (interaction?.PlayerCamera == null) return;

        var cam = interaction.PlayerCamera.transform;
        if (Physics.Raycast(cam.position, cam.forward, out var hit, interactRange))
            currentTarget = hit.collider.GetComponentInParent<PlacedPiece>();
    }

    private void HandleInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || currentTarget == null || !HammerInHand)
        {
            holdTime = 0f;
            lastTarget = null;
            return;
        }

        if (currentTarget != lastTarget)
        {
            holdTime = 0f;
            lastTarget = currentTarget;
        }

        if (keyboard.eKey.isPressed)
        {
            holdTime += Time.deltaTime;
            if (holdTime >= destroyDuration)
            {
                DestroyPiece(currentTarget);
                holdTime = 0f;
            }
        }
        else if (keyboard.eKey.wasReleasedThisFrame && holdTime > 0f)
        {
            Upgrade(currentTarget);
            holdTime = 0f;
        }
    }

    private void Upgrade(PlacedPiece target)
    {
        var next = target.Piece != null ? target.Piece.nextTier : null;
        if (next == null)
        {
            ShowMessage("Already at the highest tier.");
            return;
        }
        if (!HasIngredients(next))
        {
            ShowMessage("Not enough materials.");
            return;
        }

        RemoveIngredients(next);

        // Record old socket occupancy by position before destroying — the
        // new instance's sockets land at the same local offsets (same
        // shape, different material), so nearest-position matching carries
        // the occupied state across without needing a stored link.
        var oldSockets = new System.Collections.Generic.List<(Vector3 pos, bool occupied)>();
        foreach (var s in target.GetComponentsInChildren<BuildSocket>())
            oldSockets.Add((s.transform.position, s.Occupied));

        Vector3 pos = target.transform.position;
        Quaternion rot = target.transform.rotation;
        Destroy(target.gameObject);

        var real = Instantiate(next.prefab, pos, rot);
        real.AddComponent<PlacedPiece>().Piece = next;

        foreach (var newSocket in real.GetComponentsInChildren<BuildSocket>())
        {
            float best = float.MaxValue;
            bool occupied = false;
            foreach (var (oldPos, oldOccupied) in oldSockets)
            {
                float d = Vector3.Distance(oldPos, newSocket.transform.position);
                if (d < best) { best = d; occupied = oldOccupied; }
            }
            newSocket.Occupied = occupied;
        }

        skills?.GainExperience(next.trainedSkill, next.skillGain);
        currentTarget = null;
        lastTarget = null;
    }

    private void DestroyPiece(PlacedPiece target)
    {
        // Pure loss, per Ben's call — no material refund.
        BuildSocket.FreeConnectedSockets(target.gameObject);
        Destroy(target.gameObject);
        currentTarget = null;
        lastTarget = null;
    }

    private bool HasIngredients(BuildPiece piece)
    {
        if (piece.ingredients == null) return true;
        foreach (var ingredient in piece.ingredients)
        {
            if (ingredient == null || ingredient.item == null) continue;
            if (GetAvailableCount(ingredient.item) < ingredient.count) return false;
        }
        return true;
    }

    private int GetAvailableCount(ItemDefinition item)
    {
        int total = 0;
        foreach (var inv in ReachableInventories())
            total += IngredientMatching.GetCount(inv, item);
        return total;
    }

    private void RemoveIngredients(BuildPiece piece)
    {
        if (piece.ingredients == null) return;
        foreach (var ingredient in piece.ingredients)
        {
            if (ingredient == null || ingredient.item == null) continue;

            int amount = ingredient.count;
            foreach (var inv in ReachableInventories())
            {
                if (amount <= 0) break;

                int have = IngredientMatching.GetCount(inv, ingredient.item);
                if (have <= 0) continue;

                int take = Mathf.Min(have, amount);
                IngredientMatching.Remove(inv, ingredient.item, take);
                amount -= take;
            }
        }
    }

    private void ShowMessage(string text)
    {
        message = text;
        messageExpireTime = Time.time + MessageDuration;
    }

    // Full UI on purpose (unlike Magic) — Building is meant to be visible
    // and learnable. Below PlayerBuilding's own message (y=190).
    private void OnGUI()
    {
        if (currentTarget != null && HammerInHand)
        {
            string text;
            if (Keyboard.current != null && Keyboard.current.eKey.isPressed)
            {
                text = $"Hold to destroy ({Mathf.CeilToInt(destroyDuration - holdTime)}s)";
            }
            else
            {
                var next = currentTarget.Piece != null ? currentTarget.Piece.nextTier : null;
                text = next != null
                    ? $"Click to upgrade to {next.pieceName} — hold {destroyDuration:F0}s to destroy"
                    : $"Already highest tier — hold {destroyDuration:F0}s to destroy";
            }

            var style = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(Screen.width / 2f - 200, Screen.height / 2f + 30, 400, 30), text, style);
        }

        if (message != null && Time.time < messageExpireTime)
        {
            const float width = 420f;
            const float height = 30f;
            var rect = new Rect((Screen.width - width) / 2f, 230f, width, height);
            DebugGUI.DrawPanel(rect);
            GUI.Label(rect, message, DebugGUI.Header);
        }
    }
}
