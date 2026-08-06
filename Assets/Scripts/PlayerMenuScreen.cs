using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Full-screen tabbed menu, toggled with Tab — same open/close/cursor-lock
// convention as every other screen (Bank, Lockbox, GameMenuScreen). Folds
// together three screens that used to be independently hotkeyed
// (Inventory/I, Skills/U, Crafting/O) into one place, alongside a blank
// Player stats tab reserved for later. See GameMenuScreen (` key) for the
// separate settings/controls/credits menu.
[RequireComponent(typeof(InventoryScreen))]
[RequireComponent(typeof(SkillsScreen))]
[RequireComponent(typeof(CraftingScreen))]
public class PlayerMenuScreen : MonoBehaviour
{
    private enum Tab { Player, Inventory, Skills, Crafting }

    private const float TabWidth = 140f;
    private const float TabHeight = 32f;

    private InventoryScreen inventoryScreen;
    private SkillsScreen skillsScreen;
    private CraftingScreen craftingScreen;

    private bool isOpen;
    private Tab currentTab = Tab.Player;
    private Vector2 tabScrollPos;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        inventoryScreen = GetComponent<InventoryScreen>();
        skillsScreen = GetComponent<SkillsScreen>();
        craftingScreen = GetComponent<CraftingScreen>();
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.tabKey.wasPressedThisFrame) return;

        // Always allow closing. Only allow opening from normal gameplay —
        // not while some other screen already has the cursor unlocked,
        // which would stack this on top of it.
        if (isOpen || Cursor.lockState == CursorLockMode.Locked)
            SetOpen(!isOpen);
    }

    // Called by FirstPersonController when Escape re-locks the cursor, so
    // the two toggles can't drift out of sync with each other.
    public void Close() => SetOpen(false);

    private void SetOpen(bool value)
    {
        isOpen = value;
        if (!value)
            inventoryScreen.ResetPopups();
        Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = value;
    }

    private void OnGUI()
    {
        if (!isOpen) return;

        var rect = new Rect(0, 0, Screen.width, Screen.height);
        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);

        DrawTabBar();
        GUILayout.Space(15);

        switch (currentTab)
        {
            case Tab.Player: DrawScrollable(DrawPlayerTab); break;
            // Manages its own internal scroll view already (currency row
            // stays pinned above it) — wrapping it in another one here
            // would nest two scrollbars.
            case Tab.Inventory: inventoryScreen.DrawContent(); break;
            case Tab.Skills: DrawScrollable(skillsScreen.DrawContent); break;
            case Tab.Crafting: DrawScrollable(craftingScreen.DrawContent); break;
        }

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Close", GUILayout.Width(100)))
            SetOpen(false);

        GUILayout.EndArea();

        // Drawn after EndArea, not nested inside it — same reason as
        // GameMenuScreen leaves its own tab content directly in the area:
        // these are screen-centered popups (item move / coin drop) that
        // need to sit on top of the whole menu, not be laid out as part of
        // its GUILayout flow.
        if (currentTab == Tab.Inventory)
            inventoryScreen.DrawPopups();
    }

    // Shared scroll wrapper for tabs whose content can outgrow the screen
    // (Skills, and especially Crafting once the tool tiers land) — added
    // 2026-08-05, before that every tab just rendered directly into the
    // full-screen area with no scroll bound.
    private void DrawScrollable(Action content)
    {
        float scrollHeight = Mathf.Min(Screen.height - 220f, 640f);
        tabScrollPos = GUILayout.BeginScrollView(tabScrollPos, GUILayout.Height(scrollHeight));
        content();
        GUILayout.EndScrollView();
    }

    private void DrawTabBar()
    {
        GUILayout.BeginHorizontal();
        foreach (Tab tab in Enum.GetValues(typeof(Tab)))
        {
            var style = tab == currentTab ? DebugGUI.TabSelected : DebugGUI.TabUnselected;
            if (GUILayout.Button(tab.ToString(), style, GUILayout.Width(TabWidth), GUILayout.Height(TabHeight)))
                currentTab = tab;
        }
        GUILayout.EndHorizontal();
    }

    // Deliberately left blank for now — same call Ben made for
    // GameMenuScreen's Player tab, reserved for future decisions about
    // what belongs here rather than guessing and having to undo it later.
    private void DrawPlayerTab()
    {
        GUILayout.Label("Player", DebugGUI.Header);
    }
}
