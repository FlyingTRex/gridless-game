using UnityEngine;

// Autosave (2026-08-16, Ben's ask: "so I can wait out the 30 minutes to
// see if the NPC spawns" without needing to remember to hit the manual
// Save button first). SAVE_LOAD_PLANNING.md's original v1 scope was
// deliberately manual-only ("no autosave") -- this doesn't replace that,
// GameMenuScreen's Save button still works exactly as before, this is a
// second, automatic trigger on top of it.
[RequireComponent(typeof(SaveManager))]
public class PlayerAutosave : MonoBehaviour
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
