using System.Collections.Generic;
using UnityEngine;

// A stationary world container (a chest/box placed in the level). Doesn't
// implement IInteractable — there's no pickup/use prompt. Instead
// InventoryScreen auto-detects any box within range every frame the I
// screen is open and draws its contents alongside the player's own
// inventory, so storing/retrieving items just means walking up and
// pressing I.
public class StorageBox : MonoBehaviour, IRenameable
{
    // Every enabled box registers here so InventoryScreen can find nearby
    // ones with a simple distance check instead of a physics query.
    public static readonly List<StorageBox> Active = new List<StorageBox>();

    [SerializeField] private string boxName = "Storage Box";
    [SerializeField] private int capacity = 20;

    private Inventory inventory;

    public string DisplayName => boxName;
    public Inventory Inventory => inventory;

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
