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
        // instance (e.g. a specific backpack or canteen), as opposed to a
        // plain stackable resource. Such slots never merge with others.
        public IEquippable equipment;
    }

    // Hard ceiling on any single slot's stack, regardless of what an
    // individual ItemDefinition's own maxStack says — enforced centrally
    // here so it applies to every Inventory (main, backpack, any equip
    // slot) without each item asset needing to be trusted individually.
    public const int MaxStackCap = 20;

    [SerializeField] private int capacity = 4;
    private readonly List<Slot> slots = new List<Slot>();

    // Optional — when set, only items in this list can ever be added (via
    // AddItem or AddEquipmentItem). Null/empty (the default) means
    // unrestricted, same as every Inventory before this existed. Used for
    // dedicated single-purpose slots like a boot's knife sheath, distinct
    // from PlayerEquipment's named body slots (which stay unrestricted —
    // this is a stricter, opt-in narrowing for specific containers).
    private readonly ItemDefinition[] restrictedTo;

    public int Capacity => capacity;
    public IReadOnlyList<Slot> Slots => slots;

    public Inventory(int capacity, ItemDefinition[] restrictedTo = null)
    {
        this.capacity = capacity;
        this.restrictedTo = (restrictedTo != null && restrictedTo.Length > 0) ? restrictedTo : null;
    }

    private bool IsAllowed(ItemDefinition item)
    {
        if (restrictedTo == null) return true;
        foreach (var allowed in restrictedTo)
            if (allowed == item) return true;
        return false;
    }

    private static int EffectiveMaxStack(ItemDefinition item) => Mathf.Min(item.maxStack, MaxStackCap);

    public int GetCount(ItemDefinition item)
    {
        int total = 0;
        foreach (var slot in slots)
            if (slot.item == item) total += slot.count;
        return total;
    }

    // How many more of item could be added right now — existing
    // under-cap stacks first, then whatever fits in remaining free slots.
    // Read by InventoryTransfer.MoveAsManyAsFit so a move can cap itself
    // to what actually fits instead of failing outright when the source
    // has more than the destination can hold (e.g. moving two
    // non-stacking Hammers, maxStack 1 so two separate slots, into an
    // empty single-capacity hand slot — only one fits).
    public int SpaceFor(ItemDefinition item)
    {
        if (item == null) return int.MaxValue;
        if (!IsAllowed(item)) return 0;

        int maxStack = EffectiveMaxStack(item);
        int total = 0;
        foreach (var slot in slots)
            if (slot.item == item && slot.count < maxStack)
                total += maxStack - slot.count;

        int freeSlots = capacity - slots.Count;
        total += freeSlots * maxStack;
        return total;
    }

    public bool HasSpaceFor(ItemDefinition item, int quantity)
    {
        if (item == null || quantity <= 0) return true;
        if (!IsAllowed(item)) return false;

        int maxStack = EffectiveMaxStack(item);
        int remaining = quantity;
        foreach (var slot in slots)
        {
            if (slot.item == item && slot.count < maxStack)
            {
                remaining -= Mathf.Min(maxStack - slot.count, remaining);
                if (remaining <= 0) return true;
            }
        }

        int freeSlots = capacity - slots.Count;
        return freeSlots > 0 && remaining <= freeSlots * maxStack;
    }

    // Returns the amount that did NOT fit (0 means everything was added).
    public int AddItem(ItemDefinition item, int quantity)
    {
        if (item == null || quantity <= 0) return quantity;
        if (!IsAllowed(item)) return quantity;

        int maxStack = EffectiveMaxStack(item);

        foreach (var slot in slots)
        {
            if (slot.item == item && slot.count < maxStack)
            {
                int space = maxStack - slot.count;
                int add = Mathf.Min(space, quantity);
                slot.count += add;
                quantity -= add;
                if (quantity <= 0) return 0;
            }
        }

        while (quantity > 0 && slots.Count < capacity)
        {
            int add = Mathf.Min(maxStack, quantity);
            slots.Add(new Slot { item = item, count = add });
            quantity -= add;
        }

        return quantity;
    }

    // Adds a single equippable instance (e.g. a backpack or canteen) as its
    // own slot. Fails if there's no free slot — equipment items don't stack.
    public bool AddEquipmentItem(ItemDefinition item, IEquippable equipment)
    {
        if (item == null || equipment == null) return false;
        if (!IsAllowed(item)) return false;
        if (slots.Count >= capacity) return false;

        slots.Add(new Slot { item = item, count = 1, equipment = equipment });
        return true;
    }

    // Finds and removes the first slot holding the given equipment
    // instance, returning it (null if not found).
    public IEquippable RemoveEquipmentItem(ItemDefinition item)
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
