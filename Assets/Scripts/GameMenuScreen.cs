using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Full-screen tabbed menu, toggled with ` (backtick/grave) — same
// open/close/cursor-lock convention as every other screen (Inventory,
// Crafting, Skills, Bank, Lockbox): only opens while the cursor is already
// locked, so it can't stack on top of another open screen.
public class GameMenuScreen : MonoBehaviour
{
    private enum Tab { Player, Audio, Graphics, Controls, Credits }

    // Alphabetized by the key/binding's display name (not grouped by
    // category) — a flat reference list, per the request. Update this
    // whenever a new key mapping is added anywhere in the game.
    private static readonly (string Key, string Action)[] ControlsList =
    {
        ("` (Backtick)", "Open this menu"),
        ("C", "Toggle Crawl stance"),
        ("E", "Interact (pick up / open / use)"),
        ("Escape", "Close the open screen / toggle cursor lock"),
        ("F", "Secondary interact (e.g. Fill Canteen at a water source)"),
        ("I", "Toggle Inventory screen"),
        ("Left Mouse Button", "Punch (break a resource node)"),
        ("Left Shift (hold)", "Sprint"),
        ("Mouse Movement", "Look around"),
        ("O", "Toggle Crafting screen"),
        ("Right Mouse Button", "Rename a world object"),
        ("Space", "Jump"),
        ("U", "Toggle Skills screen"),
        ("W A S D", "Move"),
        ("X", "Toggle Kneel stance"),
        ("Z", "Toggle Prone stance"),
    };

    private const float TabWidth = 140f;
    private const float TabHeight = 32f;
    private const float KeyColumnWidth = 190f;

    private bool isOpen;
    private Tab currentTab = Tab.Player;

    public bool IsOpen => isOpen;

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.backquoteKey.wasPressedThisFrame) return;

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
            case Tab.Player: DrawPlayerTab(); break;
            case Tab.Audio: DrawAudioTab(); break;
            case Tab.Graphics: DrawGraphicsTab(); break;
            case Tab.Controls: DrawControlsTab(); break;
            case Tab.Credits: DrawCreditsTab(); break;
        }

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Close", GUILayout.Width(100)))
            SetOpen(false);

        GUILayout.EndArea();
    }

    private void DrawTabBar()
    {
        GUILayout.BeginHorizontal();
        foreach (Tab tab in Enum.GetValues(typeof(Tab)))
        {
            var style = tab == currentTab ? DebugGUI.Header : DebugGUI.Label;
            if (GUILayout.Button(tab.ToString(), style, GUILayout.Width(TabWidth), GUILayout.Height(TabHeight)))
                currentTab = tab;
        }
        GUILayout.EndHorizontal();
    }

    // Deliberately left blank for now — Ben's call, reserved for future
    // decisions about what belongs here rather than guessing (Vitals?
    // Skills? Something else?) and having to undo it later.
    private void DrawPlayerTab()
    {
        GUILayout.Label("Player", DebugGUI.Header);
    }

    // No audio system exists anywhere in the project yet — placeholder
    // rather than fake controls that wouldn't actually control anything.
    private void DrawAudioTab()
    {
        GUILayout.Label("Audio", DebugGUI.Header);
        GUILayout.Label("No audio system exists yet — nothing to configure.", DebugGUI.Label);
    }

    // Same reasoning as Audio — no graphics/quality settings system exists.
    private void DrawGraphicsTab()
    {
        GUILayout.Label("Graphics", DebugGUI.Header);
        GUILayout.Label("No graphics settings exist yet — nothing to configure.", DebugGUI.Label);
    }

    private void DrawControlsTab()
    {
        GUILayout.Label("Controls", DebugGUI.Header);
        foreach (var (key, action) in ControlsList)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(key, DebugGUI.Label, GUILayout.Width(KeyColumnWidth));
            GUILayout.Label(action, DebugGUI.Label);
            GUILayout.EndHorizontal();
        }
    }

    private void DrawCreditsTab()
    {
        GUILayout.Label("Credits", DebugGUI.Header);
        GUILayout.Label("Tekim", DebugGUI.Label);
        GUILayout.Label("the T-Rex", DebugGUI.Label);
    }
}
