using System.Collections.Generic;
using UnityEngine;

// A stationary world container (a chest/box placed in the level). Doesn't
// implement IInteractable — there's no pickup/use prompt. Instead
// InventoryScreen auto-detects any box within range every frame the Inventory
// tab of PlayerMenuScreen is open and draws its contents alongside the
// player's own inventory, so storing/retrieving items just means walking up
// and pressing Tab.
[RequireComponent(typeof(SaveId))]
public class StorageBox : MonoBehaviour, IRenameable, IInteractable
{
    // Every enabled box registers here so InventoryScreen can find nearby
    // ones with a simple distance check instead of a physics query.
    public static readonly List<StorageBox> Active = new List<StorageBox>();

    [SerializeField] private string boxName = "Storage Box";
    [SerializeField] private int capacity = 20;

    // The portable ItemDefinition this box becomes when picked up (see
    // Complete below). Its own worldPickupPrefab points right back at this
    // same StorageBox prefab — dropping/placing it later spawns a real,
    // working box again, not an inert prop. Null (unset) means this
    // instance simply can't be picked up (e.g. if a future variant
    // shouldn't be portable) — Complete no-ops in that case.
    [SerializeField] private ItemDefinition pickupItem;

    private Inventory inventory;

    public string DisplayName => boxName;
    public Inventory Inventory => inventory;

    // Ben's call (2026-08-09): must be empty to pick up — simple and safe,
    // no risk of silently losing stored items. No tool required, unlike
    // PlayerPieceUpgrade's Hammer-gated upgrade/destroy on build pieces —
    // this is a plain "pick up my furniture" interaction, distinct from
    // that system entirely (StorageBox isn't placed through PlayerBuilding
    // at all here, so PlacedPiece/PlayerPieceUpgrade don't apply).
    public string Prompt => inventory != null && inventory.Slots.Count > 0
        ? $"{boxName} (must be empty to pick up)"
        : $"Pick up {boxName}";
    public bool IsInstant => true;
    public float GetHoldDuration(GameObject player) => 0f;

    public void Complete(GameObject player)
    {
        if (pickupItem == null || inventory.Slots.Count > 0) return;

        var loot = player.GetComponent<PlayerLoot>();
        int leftover = loot != null
            ? loot.Receive(pickupItem, 1)
            : (player.GetComponent<PlayerInventory>()?.AddItem(pickupItem, 1) ?? 1);

        if (leftover > 0) return; // no room anywhere — stays placed

        Destroy(gameObject);
    }

    private void Awake()
    {
        inventory = new Inventory(capacity);
    }

    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);

    // Every active box within range of position, nearest first. Shared by
    // InventoryScreen (the "(nearby)" contents section, storage picker)
    // and PlayerCrafting (letting a recipe draw on a nearby box's
    // materials) so both use the exact same distance rule.
    public static void FindNearby(Vector3 position, float range, List<StorageBox> result)
    {
        result.Clear();
        float rangeSq = range * range;

        foreach (var box in Active)
        {
            if (box == null) continue;
            float distSq = (box.transform.position - position).sqrMagnitude;
            if (distSq <= rangeSq)
                result.Add(box);
        }

        result.Sort((a, b) =>
            (a.transform.position - position).sqrMagnitude
                .CompareTo((b.transform.position - position).sqrMagnitude));
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return;
        boxName = newName.Trim();
    }
}
