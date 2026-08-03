public interface IRenameable
{
    string DisplayName { get; }
    void Rename(string newName);
}
