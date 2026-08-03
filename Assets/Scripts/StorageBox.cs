using System.Collections.Generic;
using UnityEngine;

// A stationary world container (a chest/box placed in the level). Doesn't
// implement IInteractable — there's no pickup/use prompt. Instead
// InventoryScreen auto-detects any box within range every frame the I
// screen is open and draws its contents alongside the player's own
// inventory, so storing/retrieving items just means walking up and
// pressing I.
public class StorageBox : MonoBehaviour
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
}
