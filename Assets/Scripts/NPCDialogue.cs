using UnityEngine;

// Minimal placeholder Talk behavior (2026-08-10). No branching conversation
// system -- one static line, matching how far NPCs are actually designed
// right now (just the "Hireable autonomous NPCs" name from the Phase 1
// wishlist, nothing mechanical). Pauses NPCWander for the duration via
// SetPaused so the NPC holds still instead of wandering off mid-conversation.
//
// No longer IInteractable itself (2026-08-10, Chunk 1 of the Hireable NPCs
// build) -- E now opens NPCHiring's menu first, and "Talk" is one of that
// menu's buttons, calling BeginDialogue() here. Two IInteractable
// implementers on the same GameObject would leave PlayerInteraction's
// GetComponentInParent<IInteractable>() picking one arbitrarily, so only
// NPCHiring owns that interface now.
[RequireComponent(typeof(NPCWander))]
public class NPCDialogue : MonoBehaviour
{
    [SerializeField] private string npcName = "Factory Worker";
    [SerializeField] private string dialogueLine = "Been a long shift out here. Never seen anything like this place before.";
    [SerializeField] private float dialogueDuration = 4f;

    private NPCWander wander;
    private NPCMining mining;
    private bool isTalking;
    private float talkTimer;

    public string DisplayName => npcName;

    private void Awake()
    {
        wander = GetComponent<NPCWander>();
        // Optional -- not every NPC has a job loop (Chunk 4), and
        // NPCDialogue shouldn't hard-require one just to pause it.
        mining = GetComponent<NPCMining>();
    }

    private void Update()
    {
        if (!isTalking) return;

        talkTimer -= Time.deltaTime;
        if (talkTimer <= 0f)
            EndDialogue();
    }

    // Re-calling while already talking restarts the timer rather than
    // stacking/ignoring -- simplest behavior for a menu "Talk" button that
    // can be clicked again mid-line.
    public void BeginDialogue()
    {
        isTalking = true;
        talkTimer = dialogueDuration;
        wander.SetPaused(true);
        mining?.SetPaused(true);
    }

    private void EndDialogue()
    {
        isTalking = false;
        wander.SetPaused(false);
        mining?.SetPaused(false);
    }

    private void OnGUI()
    {
        if (!isTalking) return;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
        };
        var rect = new Rect(Screen.width / 2f - 250, Screen.height / 2f - 120, 500, 60);
        GUI.Label(rect, $"{npcName}: \"{dialogueLine}\"", style);
    }
}
