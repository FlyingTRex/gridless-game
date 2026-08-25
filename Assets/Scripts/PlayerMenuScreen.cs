using System;
using Mirror;
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
[RequireComponent(typeof(MagicScreen))]
[RequireComponent(typeof(BuildScreen))]
[RequireComponent(typeof(WritingScreen))]
[RequireComponent(typeof(PlayerSkills))]
[RequireComponent(typeof(PlayerGuilds))]
[RequireComponent(typeof(PlayerEncumbrance))]
[RequireComponent(typeof(PlayerDexterity))]
[RequireComponent(typeof(PlayerConstitution))]
[RequireComponent(typeof(PlayerVitals))]
[RequireComponent(typeof(PlayerFame))]
[RequireComponent(typeof(PlayerIdentity))]
[RequireComponent(typeof(PlayerInteraction))]
public class PlayerMenuScreen : MonoBehaviour
{
    private enum Tab { Player, Inventory, Skills, Crafting, Magic, Build, Writing }

    private const float TabWidth = 140f;
    private const float TabHeight = 32f;

    // Player tab tile grid (2026-08-10, Ben's layout call): stats/Fame/
    // Faction lay out 3-to-a-row; guild tiles are one per row, each the
    // same total width as that 3-tile row. Tile width is computed from
    // Screen.width (not a fixed pixel size) so the grid fills the screen
    // side to side on any resolution, per Ben's follow-up call the same
    // day — TileAreaSidePadding is a rough allowance for the tab area's
    // own margin plus the scroll view's scrollbar.
    private const float TileHeight = 110f;
    private const float TileGap = 10f;
    private const int TilesPerRow = 3;
    private const float TileAreaSidePadding = 40f;

    private static float TileWidth =>
        (Screen.width - TileAreaSidePadding - TileGap * (TilesPerRow - 1)) / TilesPerRow;
    private static float RowWidth => TileWidth * TilesPerRow + TileGap * (TilesPerRow - 1);

    // Growth bar shown on each of the 4 core stat tiles (2026-08-10),
    // styled like VitalsBarHUD's vital bars (colored fill + dark
    // background + a centered label) per Ben's call — progress toward the
    // *next .25 displayed point*, not overall progress to the 0-100 cap
    // (a nearly-always-empty bar for the first many hours of play
    // wouldn't read as useful feedback). Since displayed = level/10, a
    // .25 displayed step is 2.5 raw skill levels — e.g. raw level 20
    // (Strength 2.00) to 22.5 (Strength 2.25) fills this 0->1. Not shown
    // on Fame/Guild tiles — neither has a GainExperience-backed growth
    // track.
    private const float BarHeight = 18f;
    private const float LevelPerQuarterPoint = 2.5f;
    private static Texture2D barBackgroundTex;
    private static Texture2D barFillTex;
    private static GUIStyle barLabelStyle;

    [SerializeField] private SkillDefinition strengthSkill;
    [SerializeField] private SkillDefinition dexteritySkill;
    [SerializeField] private SkillDefinition constitutionSkill;
    [SerializeField] private SkillDefinition intelligenceSkill;

    // Intelligence's own sub-line (2026-08-13) — same "show the concrete
    // mechanic under the raw number" spirit as Strength's Encumbrance
    // sub-line, pointed at reading/writing (SKILL_BOOKS_PLANNING.md)
    // instead of carry capacity.
    [SerializeField] private ItemDefinition paperItem;
    [SerializeField] private ItemDefinition inkItem;

    private InventoryScreen inventoryScreen;
    private SkillsScreen skillsScreen;
    private CraftingScreen craftingScreen;
    private MagicScreen magicScreen;
    private BuildScreen buildScreen;
    private WritingScreen writingScreen;
    private PlayerSkills skills;
    private PlayerGuilds guilds;
    private PlayerEncumbrance encumbrance;
    private PlayerDexterity dexterity;
    private PlayerConstitution constitution;
    private PlayerVitals vitals;
    private PlayerFame fame;
    private PlayerInventory playerInventory;
    private PlayerIdentity identity;
    private PlayerInteraction interaction;

    private bool isOpen;
    private Tab currentTab = Tab.Player;
    private Vector2 tabScrollPos;

    // Rename popup state (2026-08-22, player naming) -- same small
    // text-entry-window shape PlayerRenaming already establishes for
    // world objects, just triggered from this tab instead of a raycast.
    private bool renamePopupOpen;
    private string renameEditingName;
    private string renameMessage;
    // Multiplayer per-connection spawning (2026-08-25) -- see
    // FirstPersonController's own field comment for why every sibling on
    // the Player root needs this same gate.
    private NetworkIdentity netIdentity;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        netIdentity = GetComponent<NetworkIdentity>();
        inventoryScreen = GetComponent<InventoryScreen>();
        skillsScreen = GetComponent<SkillsScreen>();
        craftingScreen = GetComponent<CraftingScreen>();
        magicScreen = GetComponent<MagicScreen>();
        buildScreen = GetComponent<BuildScreen>();
        writingScreen = GetComponent<WritingScreen>();
        skills = GetComponent<PlayerSkills>();
        guilds = GetComponent<PlayerGuilds>();
        encumbrance = GetComponent<PlayerEncumbrance>();
        dexterity = GetComponent<PlayerDexterity>();
        constitution = GetComponent<PlayerConstitution>();
        vitals = GetComponent<PlayerVitals>();
        fame = GetComponent<PlayerFame>();
        playerInventory = GetComponent<PlayerInventory>();
        identity = GetComponent<PlayerIdentity>();
        interaction = GetComponent<PlayerInteraction>();
    }

    private void Update()
    {
        if (netIdentity != null && !netIdentity.isLocalPlayer) return;
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
        {
            inventoryScreen.ResetPopups();
            // Same "don't leave SuppressInteraction stuck true" concern
            // as any other popup that sets it -- closing the whole menu
            // (Tab/Escape) while the rename popup happens to be open
            // must not leave world interaction permanently suppressed.
            if (renamePopupOpen) CloseRenamePopup();
        }
        Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = value;
    }

    private void OnGUI()
    {
        if (netIdentity != null && !netIdentity.isLocalPlayer) return;
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
            case Tab.Magic: DrawScrollable(magicScreen.DrawContent); break;
            case Tab.Build: DrawScrollable(buildScreen.DrawContent); break;
            // Manages its own internal scroll view already, same reason
            // as Inventory above — wrapping it in DrawScrollable would
            // nest two scrollbars.
            case Tab.Writing: writingScreen.DrawContent(); break;
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

        DrawRenamePopup();
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
            if (GUILayout.Button(tab.ToString(), style, GUILayout.Width(TabWidth), GUILayout.Height(TabHeight)) && tab != currentTab)
            {
                // Leaving Inventory mid-drag needs an explicit reset — its
                // DrawPopups() (the only place HandleGlobalDragRelease runs)
                // is gated on currentTab == Tab.Inventory, so switching tabs
                // before releasing the mouse otherwise freezes dragCandidate/
                // dragItem/dragEquipment indefinitely: the next drag-release
                // back on this tab, possibly much later, resolves against
                // those stale fields instead of a fresh drag. Real crash hit
                // live (2026-08-14): the dragged Tool had since been dropped
                // and despawned, and the stale drag's eventual TryDrop call
                // threw MissingReferenceException calling Stash() on it.
                if (currentTab == Tab.Inventory)
                    inventoryScreen.ResetPopups();
                currentTab = tab;
            }
        }
        GUILayout.EndHorizontal();
    }

    // Stats + Fame fill a 3-tile-per-row grid (5 entries = one full row of
    // 3 plus a partial row of 2, since Faction was removed from the
    // design 2026-08-14); guild tiles follow as their own one-per-row
    // section, full RowWidth each (2026-08-10, Ben's layout call).
    private void DrawPlayerTab()
    {
        GUILayout.Label("Player", DebugGUI.Header);
        GUILayout.Space(10);

        DrawNameRow();
        GUILayout.Space(10);

        DrawTileRows(new Action[]
        {
            DrawStrengthTile,
            DrawDexterityTile,
            DrawConstitutionTile,
            DrawIntelligenceTile,
            // Fame — reputation-style stat, conceptually different from
            // the skill-via-use core stats above (driven by NPC treatment/
            // guild membership/tier mastery, not personal GainExperience
            // directly — see FAME_PLANNING.md, 2026-08-14). Faction was
            // removed from the design entirely (2026-08-14) — Fame now
            // covers the trust/reputation role Faction was meant to fill.
            DrawFameTile,
        });

        DrawGuildTiles();
    }

    // Lays tiles out TilesPerRow to a row, TileGap between both columns
    // and rows — the shared grid the stat/Fame section is built from.
    private void DrawTileRows(Action[] tiles)
    {
        for (int i = 0; i < tiles.Length; i += TilesPerRow)
        {
            GUILayout.BeginHorizontal();
            for (int j = i; j < Mathf.Min(i + TilesPerRow, tiles.Length); j++)
            {
                if (j > i) GUILayout.Space(TileGap);
                tiles[j]();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(TileGap);
        }
    }

    // One full-RowWidth tile per row, one per joined guild, appearing/
    // disappearing live as membership changes — no rows at all while the
    // player hasn't joined any (Ben's spec: "a tile show up when they
    // join that guild"). Test guilds joined/left via AdminSpawnScreen's
    // Admin tab for now — no in-world way to join a guild exists yet.
    private void DrawGuildTiles()
    {
        if (guilds == null || guilds.Joined.Count == 0) return;

        foreach (var guild in guilds.Joined)
        {
            if (guild == null) continue;
            DrawTile(guild.guildName, "Joined", RowWidth);
            GUILayout.Space(TileGap);
        }
    }

    // Name + Rename control (2026-08-22, player naming) -- sits above the
    // stat grid, not as a tile itself (a single row reads better than a
    // squarish tile for "one line of text + one button"). Free the first
    // time (identity.HasBeenNamed false); after that, Ben's call on cost
    // shows live via identity.NextRenameCostGold, gated at PlayerIdentity
    // .TryRename itself, not just here.
    private void DrawNameRow()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Name: {identity.DisplayName}", DebugGUI.Label);
        string buttonLabel = identity.HasBeenNamed ? $"Rename ({identity.NextRenameCostGold} Gold)" : "Set Name (free)";
        if (GUILayout.Button(buttonLabel, GUILayout.Width(180)))
            OpenRenamePopup();
        GUILayout.EndHorizontal();
    }

    private void OpenRenamePopup()
    {
        renamePopupOpen = true;
        renameEditingName = identity.HasBeenNamed ? identity.DisplayName : "";
        renameMessage = null;
        // Same reasoning as PlayerRenaming's own SuppressInteraction use
        // (2026-08-21 gotcha) -- ResolveTarget's world raycast keeps
        // running even while this menu is open, so typing "e" while aimed
        // at a live IInteractable could otherwise fire it through the New
        // Input System's own unconsumed keystroke view.
        if (interaction != null) interaction.SuppressInteraction = true;
    }

    private void CloseRenamePopup()
    {
        renamePopupOpen = false;
        if (interaction != null) interaction.SuppressInteraction = false;
    }

    private void DrawRenamePopup()
    {
        if (!renamePopupOpen) return;

        const float width = 280f;
        const float height = 130f;
        var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);

        bool enterPressed = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return;

        DebugGUI.DrawPanel(rect);
        GUILayout.BeginArea(rect);
        GUILayout.Label("Set Your Name", DebugGUI.Header);

        GUI.SetNextControlName("PlayerNameField");
        renameEditingName = GUILayout.TextField(renameEditingName, 40);
        GUI.FocusControl("PlayerNameField");

        if (renameMessage != null)
            GUILayout.Label(renameMessage, DebugGUI.Label);

        GUILayout.BeginHorizontal();
        bool save = GUILayout.Button("Save") || enterPressed;
        bool cancel = GUILayout.Button("Cancel");
        GUILayout.EndHorizontal();

        GUILayout.EndArea();

        if (save)
        {
            if (identity.TryRename(renameEditingName))
                CloseRenamePopup();
            else
                renameMessage = "Can't rename right now (invalid name or can't afford it).";
        }
        else if (cancel)
        {
            CloseRenamePopup();
        }
    }

    // The one stat tile with a derived sub-line — Encumbrance, per Ben's
    // original spec for this tab ("Strength: 3. under that stat...
    // Encumbrance: 120/300 lbs"). Formula finalized 2026-08-10 (small
    // exponential curve, see PlayerEncumbrance) after reviewing a
    // linear-vs-curved comparison chart.
    private void DrawStrengthTile()
    {
        float value = skills != null ? skills.GetAttributeValue(strengthSkill) : 0.25f;
        string sub = encumbrance != null
            ? $"Encumbrance: {encumbrance.CarriedWeight:F0}/{encumbrance.Capacity:F0} lbs"
            : null;
        DrawTile("Strength", value.ToString("F2"), TileWidth, sub, GrowthProgress(strengthSkill));
    }

    // Intelligence's own derived sub-line (2026-08-13, Ben's ask — "no
    // read and write under the intelligence box"), same spirit as
    // Strength's Encumbrance line above: Paper/Ink on hand, since those
    // gate the Writing tab's actual use of this stat
    // (SKILL_BOOKS_PLANNING.md).
    private void DrawIntelligenceTile()
    {
        float value = skills != null ? skills.GetAttributeValue(intelligenceSkill) : 0.25f;
        string sub = playerInventory != null
            ? $"Reading & Writing — Paper: {playerInventory.Inventory.GetCount(paperItem)}, Ink: {playerInventory.Inventory.GetCount(inkItem)}"
            : null;
        DrawTile("Intelligence", value.ToString("F2"), TileWidth, sub, GrowthProgress(intelligenceSkill));
    }

    // Dexterity's own derived sub-line (2026-08-14,
    // DEXTERITY_CONSTITUTION_PLANNING.md) — the live speed bonus its
    // PlayerDexterity.SpeedMultiplier grants, same "show the concrete
    // mechanic under the raw number" spirit as Strength/Intelligence above.
    private void DrawDexterityTile()
    {
        float value = skills != null ? skills.GetAttributeValue(dexteritySkill) : 0.25f;
        string sub = dexterity != null
            ? $"Speed: +{(dexterity.SpeedMultiplier - 1f) * 100f:F0}%"
            : null;
        DrawTile("Dexterity", value.ToString("F2"), TileWidth, sub, GrowthProgress(dexteritySkill));
    }

    // Constitution's own derived sub-line (2026-08-14,
    // DEXTERITY_CONSTITUTION_PLANNING.md) — its two growable vital caps.
    private void DrawConstitutionTile()
    {
        float value = skills != null ? skills.GetAttributeValue(constitutionSkill) : 0.25f;
        string sub = vitals != null
            ? $"Max Health: {vitals.MaxHealth:F0}  Max Stamina: {vitals.MaxStamina:F0}"
            : null;
        DrawTile("Constitution", value.ToString("F2"), TileWidth, sub, GrowthProgress(constitutionSkill));
    }

    // Fame's own derived sub-line (2026-08-14, FAME_PLANNING.md) — the
    // band name a Traveling Trader (once built) would react to, same
    // "show the concrete mechanic under the raw number" spirit as every
    // other custom tile above. No growth bar — Fame isn't a 0-100
    // skill-via-use track, it can move in either direction.
    private void DrawFameTile()
    {
        float value = fame != null ? fame.Fame : 0f;
        // Band-name lookup moved onto PlayerFame itself (2026-08-16,
        // Village Flag spawn loop) since the Village Flag's own spawn-
        // interval formula needs the identical 5-band thresholds as a
        // numeric multiplier, not just a display label -- consolidated
        // into one canonical source rather than two copies of the same
        // boundaries drifting apart.
        string band = fame != null ? fame.Band.ToString() : PlayerFame.FameBand.Neutral.ToString();
        DrawTile("Fame", value.ToString("F1"), TileWidth, band);
    }

    // 0-1 progress from the current .25 displayed point toward the next —
    // see the field comment above for why this isn't just level/100.
    private float GrowthProgress(SkillDefinition skill)
    {
        if (skills == null || skill == null) return 0f;

        float level = skills.GetLevel(skill);
        if (level >= 100f) return 1f;

        float lower = Mathf.Floor(level / LevelPerQuarterPoint) * LevelPerQuarterPoint;
        return (level - lower) / LevelPerQuarterPoint;
    }

    private void DrawTile(string label, string value, float width, string subLine = null, float? growthProgress = null)
    {
        GUILayout.BeginVertical(DebugGUI.Slot, GUILayout.Width(width), GUILayout.Height(TileHeight));

        GUILayout.Label($"{label}: {value}", DebugGUI.Header);
        if (subLine != null)
        {
            GUILayout.Space(6);
            GUILayout.Label(subLine, DebugGUI.Label);
        }

        if (growthProgress.HasValue)
        {
            GUILayout.FlexibleSpace();
            DrawGrowthBar(growthProgress.Value);
        }

        GUILayout.EndVertical();
    }

    // Same anatomy as VitalsBarHUD.DrawBar (background + proportional
    // fill + a centered label drawn directly on the bar), per Ben's call
    // to make this "look like the health bar."
    private void DrawGrowthBar(float progress)
    {
        if (barBackgroundTex == null) barBackgroundTex = MakeTex(new Color(0f, 0f, 0f, 0.5f));
        if (barFillTex == null) barFillTex = MakeTex(new Color(0.78f, 0.63f, 0.36f));
        if (barLabelStyle == null)
        {
            barLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            };
            barLabelStyle.normal.textColor = Color.white;
        }

        var rect = GUILayoutUtility.GetRect(10f, BarHeight, GUILayout.ExpandWidth(true));
        GUI.DrawTexture(rect, barBackgroundTex);

        float fraction = Mathf.Clamp01(progress);
        if (fraction > 0f)
        {
            var fillRect = new Rect(rect.x, rect.y, rect.width * fraction, rect.height);
            GUI.DrawTexture(fillRect, barFillTex);
        }

        GUI.Label(rect, "Growth", barLabelStyle);
    }

    private static Texture2D MakeTex(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
}
