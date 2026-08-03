public interface IInteractable
{
    string Prompt { get; }
    bool IsInstant { get; }
    float HoldDuration { get; }
    void Complete(PlayerInventory inventory);
}
