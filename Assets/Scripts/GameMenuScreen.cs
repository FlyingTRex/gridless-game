using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Full-screen tabbed menu, toggled with ` (backtick/grave) — same
// open/close/cursor-lock convention as every other screen (Inventory,
// Crafting, Skills, Bank, Lockbox): only opens while the cursor is already
// locked, so it can't stack on top of another open screen.
[RequireComponent(typeof(AdminSpawnScreen))]
public class GameMenuScreen : MonoBehaviour
{
    private enum Tab { Player, Audio, Graphics, Controls, Credits, Admin }

    // Alphabetized by the key/binding's display name (not grouped by
    // category) — a flat reference list, per the request. Update this
    // whenever a new key mapping is added anywhere in the game.
    private static readonly (string Key, string Action)[] ControlsList =
    {
        ("` (Backtick)", "Open this menu"),
        ("C", "Toggle Crawl stance"),
        ("E", "Interact (pick up / open / use) — hold to gather/chop, release to cancel"),
        ("Escape", "Close the open screen / toggle cursor lock"),
        ("F", "Secondary interact (e.g. Fill Canteen at a water source)"),
        ("Left Shift (hold)", "Sprint"),
        ("Mouse Movement", "Look around"),
        ("Right Mouse Button", "Rename a world object"),
        ("Space", "Jump"),
        ("Tab", "Open the player menu (Inventory / Skills / Crafting)"),
        ("W A S D", "Move"),
        ("X", "Toggle Kneel stance"),
        ("Z", "Toggle Prone stance"),
    };

    private const float TabWidth = 140f;
    private const float TabHeight = 32f;
    private const float KeyColumnWidth = 190f;

    // 90% of the full screen width, per Ben's call — height is derived
    // from the actual source texture's own aspect ratio at draw time
    // rather than a hardcoded ratio, so it stays correct if the image is
    // ever swapped for a different one. Also capped by
    // CreditsImageMaxHeightFraction: GUILayout doesn't clip or scroll on
    // its own, so a width-only fit could grow tall enough to push the
    // attribution lines and Close button off-screen with no way back.
    private const float CreditsImageWidthFraction = 0.9f;
    private const float CreditsImageMaxHeightFraction = 0.5f;

    [SerializeField] private Texture2D creditsImage;

    private bool isOpen;
    private Tab currentTab = Tab.Player;
    private AdminSpawnScreen adminSpawnScreen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        adminSpawnScreen = GetComponent<AdminSpawnScreen>();
    }

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
            case Tab.Admin: adminSpawnScreen.DrawContent(); break;
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
            var style = tab == currentTab ? DebugGUI.TabSelected : DebugGUI.TabUnselected;
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

        if (creditsImage != null)
        {
            float aspect = (float)creditsImage.height / creditsImage.width;
            float imageWidth = Screen.width * CreditsImageWidthFraction;
            float imageHeight = imageWidth * aspect;

            float maxHeight = Screen.height * CreditsImageMaxHeightFraction;
            if (imageHeight > maxHeight)
            {
                imageHeight = maxHeight;
                imageWidth = imageHeight / aspect;
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(creditsImage, GUILayout.Width(imageWidth), GUILayout.Height(imageHeight));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("Tekim & The T-Rex", DebugGUI.Label);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        // Exact required attribution text per Assets/Models/THIRD_PARTY_CREDITS.md
        // — only entries actively shipping in the game go here.
        GUILayout.Space(10);
        GUILayout.Label("Third-Party Assets", DebugGUI.Header);
        GUILayout.Label("Tree branch by Poly by Google [CC-BY] via Poly Pizza", DebugGUI.Label);
        GUILayout.Label("Stone by Poly by Google [CC-BY] via Poly Pizza", DebugGUI.Label);
        GUILayout.Label("Big Tree by 3Donimus [CC-BY] via Poly Pizza", DebugGUI.Label);
        GUILayout.Label("Wood Planks by Quaternius [Public Domain] via Poly Pizza", DebugGUI.Label);
        GUILayout.Label("Strawberries by Jarlan Perez [CC-BY] via Poly Pizza", DebugGUI.Label);
        GUILayout.Label("Pickaxe by CreativeTrio [Public Domain] via Poly Pizza", DebugGUI.Label);
        GUILayout.Label("Low Poly Axe by suerozcelik [CC-BY] via Poly Pizza", DebugGUI.Label);
        GUILayout.Label("Grass Wispy by Quaternius [Public Domain] via Poly Pizza", DebugGUI.Label);
    }
}
