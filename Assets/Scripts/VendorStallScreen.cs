using UnityEngine;

// Opened by interacting (E) with a VendorStall -- same "world object owns
// the interact, a player-side screen component owns the UI" shape
// FurnaceScreen already establishes. Transact mode only for this tier
// (COMMERCE_PLANNING.md section 4's configure/owner mode is moot with one
// local player, deferred with the same reasoning the design doc gives it).
[RequireComponent(typeof(PlayerCurrency))]
[RequireComponent(typeof(PlayerInventory))]
public class VendorStallScreen : MonoBehaviour
{
    private const float MaxPanelWidth = 480f;
    private const float MaxPanelHeight = 560f;
    private const float ChromeReserve = 110f;
    private static float PanelWidth => Mathf.Min(MaxPanelWidth, Screen.width * 0.9f);
    private static float PanelHeight => Mathf.Min(MaxPanelHeight, Screen.height * 0.9f);
    private static float ScrollHeight => Mathf.Max(120f, PanelHeight - ChromeReserve);

    private PlayerCurrency wallet;
    private PlayerInventory playerInventory;
    private VendorStall current;
    private bool isOpen;
    private Vector2 scrollPos;

    // Cleared on open/close and whenever a transaction actually goes
    // through, same "brief confirmation, not a permanent log" convention
    // Furnace/Campfire's own toast-style messages already use.
    private string message;
    private float messageExpireTime;
    private const float MessageDuration = 2.5f;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        wallet = GetComponent<PlayerCurrency>();
        playerInventory = GetComponent<PlayerInventory>();
    }

    public void Open(VendorStall stall)
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        current = stall;
        message = null;
        SetOpen(true);
    }

    public void Close() => SetOpen(false);

    private void SetOpen(bool value)
    {
        isOpen = value;
        if (!value) current = null;
        Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = value;
    }

    private void OnGUI()
    {
        if (!isOpen || current == null) return;

        var rect = new Rect((Screen.width - PanelWidth) / 2f, (Screen.height - PanelHeight) / 2f, PanelWidth, PanelHeight);
        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);

        GUILayout.Label(current.DisplayName, DebugGUI.Header);
        GUILayout.Label($"Your Copper: {wallet.GetBalance(CoinType.Copper)}   "
            + $"Stall's till: {current.GetTillBalance(CoinType.Copper)}", DebugGUI.Label);

        if (message != null && Time.time < messageExpireTime)
            GUILayout.Label(message, DebugGUI.Label);

        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(ScrollHeight));

        if (current.PriceList.Count == 0)
        {
            GUILayout.Label("Nothing for sale right now.", DebugGUI.Label);
        }
        else
        {
            foreach (var entry in current.PriceList)
                DrawEntry(entry);
        }

        GUILayout.EndScrollView();

        GUILayout.Space(5);
        if (GUILayout.Button("Close"))
            SetOpen(false);

        GUILayout.EndArea();
    }

    private void DrawEntry(VendorPriceEntry entry)
    {
        if (entry == null || entry.item == null) return;

        int stockCount = current.Stock != null ? current.Stock.Inventory.GetCount(entry.item) : 0;

        GUILayout.BeginHorizontal();
        GUILayout.Label(entry.item.itemName, DebugGUI.Label, GUILayout.Width(140));
        GUILayout.Label($"stock {stockCount}", DebugGUI.Label, GUILayout.Width(70));

        // entry.canSell means the STALL can sell TO the visitor -- from
        // the player's side, that's a Buy button.
        if (entry.canSell)
        {
            GUILayout.Label($"Buy {entry.sellPrice}c", DebugGUI.Label, GUILayout.Width(70));
            if (GUILayout.Button("Buy", GUILayout.Width(60)))
            {
                if (current.SellToVisitor(entry.item, 1, wallet, playerInventory.Inventory))
                    ShowMessage($"Bought 1 {entry.item.itemName}.");
                else
                    ShowMessage("Can't buy that right now.");
            }
        }

        // entry.canBuy means the STALL can buy FROM the visitor -- from
        // the player's side, that's a Sell button.
        if (entry.canBuy)
        {
            GUILayout.Label($"Sell {entry.buyPrice}c", DebugGUI.Label, GUILayout.Width(70));
            if (GUILayout.Button("Sell", GUILayout.Width(60)))
            {
                if (current.BuyFromVisitor(entry.item, 1, wallet, playerInventory.Inventory))
                    ShowMessage($"Sold 1 {entry.item.itemName}.");
                else
                    ShowMessage("Can't sell that right now.");
            }
        }

        GUILayout.EndHorizontal();
    }

    private void ShowMessage(string text)
    {
        message = text;
        messageExpireTime = Time.time + MessageDuration;
    }
}
