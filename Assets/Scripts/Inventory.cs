using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Inventory
{
    [System.Serializable]
    public class Slot
    {
        public ItemDefinition item;
        public int count;

        // Non-null only for a slot representing a single equippable
        // container instance (e.g. a specific backpack), as opposed to a
        // plain stackable resource. Such slots never merge with others.
        public IInventoryHolder equipment;
    }

    [SerializeField] private int capacity = 4;
    private readonly List<Slot> slots = new List<Slot>();

    public int Capacity => capacity;
    public IReadOnlyList<Slot> Slots => slots;

    public Inventory(int capacity)
    {
        this.capacity = capacity;
    }

    public int GetCount(ItemDefinition item)
    {
        int total = 0;
        foreach (var slot in slots)
            if (slot.item == item) total += slot.count;
        return total;
    }

    public bool HasSpaceFor(ItemDefinition item, int quantity)
    {
        if (item == null || quantity <= 0) return true;

        int remaining = quantity;
        foreach (var slot in slots)
        {
            if (slot.item == item && slot.count < item.maxStack)
            {
                remaining -= Mathf.Min(item.maxStack - slot.count, remaining);
                if (remaining <= 0) return true;
            }
        }

        int freeSlots = capacity - slots.Count;
        return freeSlots > 0 && remaining <= freeSlots * item.maxStack;
    }

    // Returns the amount that did NOT fit (0 means everything was added).
    public int AddItem(ItemDefinition item, int quantity)
    {
        if (item == null || quantity <= 0) return quantity;

        foreach (var slot in slots)
        {
            if (slot.item == item && slot.count < item.maxStack)
            {
                int space = item.maxStack - slot.count;
                int add = Mathf.Min(space, quantity);
                slot.count += add;
                quantity -= add;
                if (quantity <= 0) return 0;
            }
        }

        while (quantity > 0 && slots.Count < capacity)
        {
            int add = Mathf.Min(item.maxStack, quantity);
            slots.Add(new Slot { item = item, count = add });
            quantity -= add;
        }

        return quantity;
    }

    // Adds a single equippable container instance (e.g. a backpack) as its
    // own slot. Fails if there's no free slot — equipment items don't stack.
    public bool AddEquipmentItem(ItemDefinition item, IInventoryHolder equipment)
    {
        if (item == null || equipment == null) return false;
        if (slots.Count >= capacity) return false;

        slots.Add(new Slot { item = item, count = 1, equipment = equipment });
        return true;
    }

    // Finds and removes the first slot holding the given equipment
    // instance, returning it (null if not found).
    public IInventoryHolder RemoveEquipmentItem(ItemDefinition item)
    {
        for (int i = slots.Count - 1; i >= 0; i--)
        {
            if (slots[i].item == item && slots[i].equipment != null)
            {
                var holder = slots[i].equipment;
                slots.RemoveAt(i);
                return holder;
            }
        }

        return null;
    }

    public bool RemoveItem(ItemDefinition item, int quantity)
    {
        if (item == null || quantity <= 0) return false;
        if (GetCount(item) < quantity) return false;

        for (int i = slots.Count - 1; i >= 0 && quantity > 0; i--)
        {
            var slot = slots[i];
            if (slot.item != item) continue;

            int remove = Mathf.Min(slot.count, quantity);
            slot.count -= remove;
            quantity -= remove;
            if (slot.count <= 0) slots.RemoveAt(i);
        }

        return true;
    }
}
