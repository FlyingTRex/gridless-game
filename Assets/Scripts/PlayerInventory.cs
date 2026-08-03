using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInventory : MonoBehaviour
{
    [System.Serializable]
    public class Slot
    {
        public ItemDefinition item;
        public int count;
    }

    private readonly List<Slot> slots = new List<Slot>();
    public IReadOnlyList<Slot> Slots => slots;

    public void AddItem(ItemDefinition item, int quantity)
    {
        if (item == null || quantity <= 0) return;

        foreach (var slot in slots)
        {
            if (slot.item == item && slot.count < item.maxStack)
            {
                int space = item.maxStack - slot.count;
                int add = Mathf.Min(space, quantity);
                slot.count += add;
                quantity -= add;
                if (quantity <= 0) return;
            }
        }

        while (quantity > 0)
        {
            int add = Mathf.Min(item.maxStack, quantity);
            slots.Add(new Slot { item = item, count = add });
            quantity -= add;
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 220, 300));
        GUILayout.Label("Inventory", GUI.skin.box);
        foreach (var slot in slots)
            GUILayout.Label($"{slot.item.itemName} x{slot.count}");
        GUILayout.EndArea();
    }
}
