using UnityEngine;

public interface IPunchable
{
    string Prompt { get; }
    void OnPunch(GameObject player);
}
