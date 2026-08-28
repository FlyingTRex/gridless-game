using UnityEngine;

// Writing tab (PlayerMenuScreen, Tab key) — lists every recipe/wish the
// player currently knows well enough to write a book about, each with a
// Write button (SKILL_BOOKS_PLANNING.md Phase 2). Flat list, not a tile
// grid — this feature's scope doesn't warrant CraftingScreen's full
// icon-grid machinery.
[RequireComponent(typeof(PlayerWriting))]
[RequireComponent(typeof(PlayerInventory))]
public class WritingScreen : MonoBehaviour
{
    [SerializeField] private ItemDefinition paperItem;
    [SerializeField] private ItemDefinition inkItem;

    private PlayerWriting writing;
    private PlayerInventory playerInventory;
    private Vector2 scrollPos;

    private void Awake()
    {
        writing = GetComponent<PlayerWriting>();
        playerInventory = GetComponent<PlayerInventory>();
    }

    // Called by PlayerMenuScreen while its Writing tab is active.
    public void DrawContent()
    {
        GUILayout.Label("Writing", DebugGUI.Header);
        GUILayout.Label("Write a skill book about something you already know — risk depends on your Intelligence versus the subject's difficulty.", DebugGUI.Label);
        GUILayout.Space(8);

        int paperCount = playerInventory.Inventory.GetCount(paperItem);
        int inkCount = playerInventory.Inventory.GetCount(inkItem);
        var costStyle = (paperCount > 0 && inkCount > 0) ? DebugGUI.Label : DebugGUI.Warning;
        GUILayout.Label($"Cost per attempt: 1 Paper (have {paperCount}), 1 Ink (have {inkCount})", costStyle);
        GUILayout.Space(10);

        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(Mathf.Min(Screen.height - 260f, 480f)));

        GUILayout.Label("Crafting & Weapon Skills", DebugGUI.Header);
        bool anyRecipe = false;
        foreach (var recipe in writing.WritableRecipes)
        {
            anyRecipe = true;
            DrawRecipeRow(recipe);
        }
        if (!anyRecipe)
            GUILayout.Label("Nothing you currently know how to craft yet.", DebugGUI.Label);

        GUILayout.Space(14);
        GUILayout.Label("Magic Wishes", DebugGUI.Header);
        bool anyWish = false;
        foreach (var wish in writing.WritableWishes)
        {
            anyWish = true;
            DrawWishRow(wish);
        }
        if (!anyWish)
            GUILayout.Label("You don't know any wishes yet.", DebugGUI.Label);

        GUILayout.EndScrollView();
    }

    private void DrawRecipeRow(CraftingRecipe recipe)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(recipe.outputItem.itemName, DebugGUI.TierNameCentered(recipe.outputItem.tier), GUILayout.Width(240));
        if (GUILayout.Button("Write", GUILayout.Width(80)))
            writing.RequestWriteRecipeBook(recipe);
        GUILayout.EndHorizontal();
    }

    private void DrawWishRow(WishRecipe wish)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(wish.wishName, DebugGUI.Label, GUILayout.Width(240));
        if (GUILayout.Button("Write", GUILayout.Width(80)))
            writing.RequestWriteWishBook(wish);
        GUILayout.EndHorizontal();
    }
}
