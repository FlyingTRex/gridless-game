using UnityEngine;

// Holds whatever an NPC has mined/gathered but not yet deposited
// (2026-08-10, Chunk 4 of the Hireable NPCs build). A real `Inventory` --
// same class PlayerInventory/PlayerEquipment slots/Lockbox already use --
// rather than a bare weight counter, so Chunk 5's deposit step can just
// move real items into a Storage Box the same way LockboxScreen already
// moves coins between two containers.
public class NPCCargo : MonoBehaviour
{
    [SerializeField] private int capacity = 20;

    public Inventory Inventory { get; private set; }

    private void Awake()
    {
        Inventory = new Inventory(capacity);
    }
}
