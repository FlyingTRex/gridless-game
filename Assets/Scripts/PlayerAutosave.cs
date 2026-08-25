using Mirror;
using UnityEngine;

// Autosave (2026-08-16, Ben's ask: "so I can wait out the 30 minutes to
// see if the NPC spawns" without needing to remember to hit the manual
// Save button first). SAVE_LOAD_PLANNING.md's original v1 scope was
// deliberately manual-only ("no autosave") -- this doesn't replace that,
// GameMenuScreen's Save button still works exactly as before, this is a
// second, automatic trigger on top of it.
//
// Persistence restructure chunk 5b (MULTIPLAYER_PLANNING.md section 3
// item 5), 2026-08-24: converted to NetworkBehaviour, plus an isServer
// guard -- this timer should only ever actually fire server-side (the
// server's disk is the real source of truth), same reasoning as every
// other server-only trigger this chunk adds. Calls SaveManager.Save()
// directly rather than through a Command, since the server is already
// the one running this Update() loop once guarded -- no client-to-
// server round trip needed for something that only ever runs
// server-side to begin with.
[RequireComponent(typeof(SaveManager))]
public class PlayerAutosave : NetworkBehaviour
{
    private const float IntervalSeconds = 600f; // 10 real minutes
    private const float MessageDurationSeconds = 15f;

    private SaveManager saveManager;
    private float timer;
    private string message;
    private float messageExpireTime;

    private void Awake()
    {
        saveManager = GetComponent<SaveManager>();
    }

    private void Update()
    {
        if (!isServer) return;

        timer += Time.deltaTime;
        if (timer < IntervalSeconds) return;

        timer = 0f;
        saveManager.Save();
        message = "Game autosaved.";
        messageExpireTime = Time.time + MessageDurationSeconds;
    }

    // Same top-center toast shape as PlayerSkills.cs (y=70) and
    // PlayerCrafting.cs (y=110, its own separate toast for craft
    // outcomes) -- y=150 sits below both of the top-center toast slots
    // already in use, confirmed by checking every existing OnGUI toast
    // in the project rather than guessing (an earlier pass here picked
    // y=110 without noticing PlayerCrafting already owned that exact
    // rect, which would have fully overlapped its own message).
    //
    // Known deferred gap, same shape as Crafting's own progress-display
    // gap: `message`/`messageExpireTime` are only ever set inside the
    // isServer-guarded Update() above, so a genuine remote client
    // wouldn't see their own "Game autosaved." toast -- only the host
    // would. Not addressed here; sync isn't needed for host-alone
    // testing, and this is purely cosmetic (the save itself is real and
    // correct either way).
    private void OnGUI()
    {
        if (message == null || Time.time >= messageExpireTime) return;

        const float width = 340f;
        const float height = 30f;
        var rect = new Rect((Screen.width - width) / 2f, 150f, width, height);

        DebugGUI.DrawPanel(rect);
        GUI.Label(rect, message, DebugGUI.Header);
    }
}
