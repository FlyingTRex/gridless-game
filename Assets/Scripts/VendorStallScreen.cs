using System.Collections.Generic;
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

    // Tile grid (2026-08-22, Ben's ask) -- same pattern Crafting/Build
    // already moved to (see BuildScreen's own header comment), just sized
    // for this screen's narrower panel: 3 columns of 140px instead of
    // Build's 4 of 200px.
    private const float TileWidth = 140f;
    private const int TilesPerRow = 3;
    private const float TileSpacing = 10f;
    private const float IconSize = 56f;
    private const float IconPadding = 5f;

    [SerializeField] private float storageRange = 10f;

    // Same coin-order convention BankScreen already establishes -- the
    // till's own full breakdown (2026-08-22, Ben's ask) needs to be
    // visible now that it's a real multi-denomination Lockbox, not just
    // a single Copper number that hid what the other 4 balances were
    // quietly doing (regen adds 1 of every type per tick).
    private static readonly (string Label, CoinType Type)[] CoinOrder =
    {
        ("Copper", CoinType.Copper),
        ("Iron", CoinType.Iron),
        ("Silver", CoinType.Silver),
        ("Gold", CoinType.Gold),
        ("Platinum", CoinType.Platinum),
    };

    private PlayerCurrency wallet;
    private PlayerInventory playerInventory;
    private PlayerBackpack backpackCarrier;
    private PlayerBank bank;
    private readonly List<StorageBox> nearbyStorages = new();
    private readonly List<Inventory> reachable = new();
    private readonly List<ItemDefinition> offListCandidates = new();

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
        backpackCarrier = GetComponent<PlayerBackpack>();
        bank = GetComponent<PlayerBank>();
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

    // Same reach PlayerCrafting.ReachableInventories already established
    // (main inventory, worn Backpack, nearby StorageBox) -- real bug found
    // live 2026-08-22: this screen used to only ever check the player's
    // bare main inventory, so items in a Backpack or a nearby box were
    // invisible to both buying and selling. Rebuilt fresh each call rather
    // than cached, since what's "nearby" changes as the player moves.
    private List<Inventory> ReachableInventories()
    {
        reachable.Clear();
        reachable.Add(playerInventory.Inventory);

        var backpack = backpackCarrier != null ? backpackCarrier.Equipped : null;
        if (backpack != null)
            reachable.Add(backpack.Inventory);

        StorageBox.FindNearby(transform.position, storageRange, nearbyStorages);
        foreach (var box in nearbyStorages)
            reachable.Add(box.Inventory);

        return reachable;
    }

    private void OnGUI()
    {
        if (!isOpen || current == null) return;

        var rect = new Rect((Screen.width - PanelWidth) / 2f, (Screen.height - PanelHeight) / 2f, PanelWidth, PanelHeight);
        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);

        GUILayout.Label(current.DisplayName, DebugGUI.Header);
        GUILayout.Label($"Your Copper: {wallet.GetBalance(CoinType.Copper)}", DebugGUI.Label);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Stall's till:", DebugGUI.Label, GUILayout.Width(80));
        foreach (var (label, type) in CoinOrder)
            GUILayout.Label($"{label} {current.GetTillBalance(type)}", DebugGUI.Label, GUILayout.Width(80));
        GUILayout.EndHorizontal();

        // Same "payment due in Ns" display convention NPCHiringScreen
        // already uses (2026-08-22, Ben's ask) -- only VillageVendor (not
        // every future VendorStall driver) has a refresh timer, so this
        // is null-safe rather than assumed.
        var villageVendor = current.GetComponent<VillageVendor>();
        if (villageVendor != null)
            GUILayout.Label($"Next restock in {villageVendor.NextFullRefreshSeconds:F0}s", DebugGUI.Label);

        if (message != null && Time.time < messageExpireTime)
            GUILayout.Label(message, DebugGUI.Label);

        var inventories = ReachableInventories();

        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(ScrollHeight));

        if (current.PriceList.Count == 0)
        {
            GUILayout.Label("Nothing for sale right now.", DebugGUI.Label);
        }
        else
        {
            DrawTileGrid(current.PriceList.Count, i =>
            {
                var entry = current.PriceList[i];
                return entry?.item == null ? null : (System.Action)(() => DrawStockedTile(entry, inventories));
            });
        }

        // Off-list selling (2026-08-22, real gap found live) -- an item
        // that isn't currently one of the 8 displayed/stocked slots can
        // still be sold here via VendorStall.BuyFromVisitor's own
        // off-list fallback, but nothing exposed a way to actually DO
        // that until now. Scans the player's reachable inventories for
        // any distinct item that's sellableByVendor, within this stall's
        // tier ceiling, and not already shown above.
        BuildOffListCandidates(inventories);
        if (offListCandidates.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label("Sell Other Items", DebugGUI.Header);
            DrawTileGrid(offListCandidates.Count, i =>
            {
                var item = offListCandidates[i];
                return (System.Action)(() => DrawOffListTile(item, inventories));
            });
        }

        GUILayout.EndScrollView();

        GUILayout.Space(5);
        if (GUILayout.Button("Close"))
            SetOpen(false);

        GUILayout.EndArea();
    }

    private void BuildOffListCandidates(List<Inventory> inventories)
    {
        offListCandidates.Clear();
        foreach (var inv in inventories)
        {
            if (inv == null) continue;
            foreach (var slot in inv.Slots)
            {
                var item = slot?.item;
                if (item == null || slot.count <= 0) continue;
                if (offListCandidates.Contains(item)) continue;
                if (IsAlreadyStocked(item)) continue;
                if (current.EstimateOffListBuyPrice(item) <= 0) continue;
                offListCandidates.Add(item);
            }
        }
    }

    private bool IsAlreadyStocked(ItemDefinition item)
    {
        foreach (var entry in current.PriceList)
            if (entry?.item == item) return true;
        return false;
    }

    // Shared row-wrapping loop -- same shape BuildScreen.DrawContent
    // already establishes, generalized here since this screen draws two
    // separate tile sections (stocked items, off-list candidates) that
    // both need identical wrapping.
    private void DrawTileGrid(int count, System.Func<int, System.Action> drawTile)
    {
        int column = 0;
        for (int i = 0; i < count; i++)
        {
            var draw = drawTile(i);
            if (draw == null) continue;

            if (column == 0) GUILayout.BeginHorizontal();
            draw();

            column++;
            if (column >= TilesPerRow)
            {
                GUILayout.EndHorizontal();
                GUILayout.Space(TileSpacing);
                column = 0;
            }
            else
            {
                GUILayout.Space(TileSpacing);
            }
        }

        if (column > 0)
            GUILayout.EndHorizontal();
    }

    // Tile layout (2026-08-22) -- same shape as BuildScreen.DrawTile: icon,
    // name, key stats, action button(s). A Vendor Stall tile can carry
    // BOTH a Buy and a Sell button at once (unlike Build's single Arm),
    // stacked vertically to stay within TileWidth.
    private void DrawStockedTile(VendorPriceEntry entry, List<Inventory> inventories)
    {
        int stockCount = current.Stock != null ? current.Stock.Inventory.GetCount(entry.item) : 0;

        GUILayout.BeginVertical(DebugGUI.Panel, GUILayout.Width(TileWidth));

        DrawIcon(entry.item);

        GUILayout.Label(entry.item.itemName, DebugGUI.Header);
        GUILayout.Label($"stock {stockCount}", DebugGUI.Label);

        // entry.canSell means the STALL can sell TO the visitor -- from
        // the player's side, that's a Buy button.
        if (entry.canSell)
        {
            if (GUILayout.Button($"Buy {entry.sellPrice}c", GUILayout.Width(TileWidth - 20f)))
            {
                if (current.SellToVisitor(entry.item, 1, wallet, inventories, bank))
                    ShowMessage($"Bought 1 {entry.item.itemName}.");
                else
                    ShowMessage("Can't buy that right now.");
            }
        }

        // entry.canBuy means the STALL can buy FROM the visitor -- from
        // the player's side, that's a Sell button.
        if (entry.canBuy)
        {
            if (GUILayout.Button($"Sell {entry.buyPrice}c", GUILayout.Width(TileWidth - 20f)))
            {
                if (current.BuyFromVisitor(entry.item, 1, wallet, inventories, bank))
                    ShowMessage($"Sold 1 {entry.item.itemName}.");
                else
                    ShowMessage("Can't sell that right now.");
            }
        }

        GUILayout.EndVertical();
    }

    // Off-list tile -- Sell only (this item isn't one of the stall's
    // displayed offerings, so there's nothing to Buy). Price shown is an
    // estimate (VendorStall.EstimateOffListBuyPrice); the real payout at
    // sale time also factors in the stock-based supply/demand adjustment,
    // so it can differ slightly -- same "estimate, not a locked quote"
    // convention every other price display in this project already uses.
    private void DrawOffListTile(ItemDefinition item, List<Inventory> inventories)
    {
        GUILayout.BeginVertical(DebugGUI.Panel, GUILayout.Width(TileWidth));

        DrawIcon(item);

        GUILayout.Label(item.itemName, DebugGUI.Header);

        int estimate = current.EstimateOffListBuyPrice(item);
        if (GUILayout.Button($"Sell ~{estimate}c", GUILayout.Width(TileWidth - 20f)))
        {
            if (current.BuyFromVisitor(item, 1, wallet, inventories, bank))
                ShowMessage($"Sold 1 {item.itemName}.");
            else
                ShowMessage("Can't sell that right now.");
        }

        GUILayout.EndVertical();
    }

    // Same convention as BuildScreen.DrawIcon -- previewIcon preferred
    // over icon, blank spacer (not a placeholder glyph) if neither is set.
    private void DrawIcon(ItemDefinition item)
    {
        var sprite = item.previewIcon != null ? item.previewIcon : item.icon;

        GUILayout.Box(GUIContent.none, GUILayout.Width(IconSize), GUILayout.Height(IconSize));
        if (sprite == null) return;

        var rect = GUILayoutUtility.GetLastRect();
        var iconRect = new Rect(
            rect.x + IconPadding, rect.y + IconPadding,
            rect.width - IconPadding * 2f, rect.height - IconPadding * 2f);
        GUI.DrawTexture(iconRect, sprite.texture, ScaleMode.ScaleToFit);
    }

    private void ShowMessage(string text)
    {
        message = text;
        messageExpireTime = Time.time + MessageDuration;
    }
}
