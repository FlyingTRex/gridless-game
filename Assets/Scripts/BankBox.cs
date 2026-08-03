using UnityEngine;

// A stationary world bank branch. E opens BankScreen, same as any other
// IInteractable — the bank itself is global (PlayerBank), so any branch
// opens the same account.
[RequireComponent(typeof(Collider))]
public class BankBox : MonoBehaviour, IInteractable
{
    [SerializeField] private string bankName = "Bank";

    public string Prompt => $"Open {bankName}";
    public bool IsInstant => true;
    public float HoldDuration => 0f;

    public void Complete(GameObject player)
    {
        player.GetComponent<BankScreen>()?.Open(this);
    }
}
