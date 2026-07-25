using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventorySlot
{
    public ItemData item;
    public int quantity;

    public InventorySlot(ItemData item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }
}

public class Inventory : MonoBehaviour
{
    [SerializeField] private int maxSlots = 24;

    private readonly List<InventorySlot> slots = new List<InventorySlot>();

    public IReadOnlyList<InventorySlot> Slots => slots;

    public event Action OnInventoryChanged;

    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        if (item.isStackable)
        {
            foreach (var slot in slots)
            {
                if (slot.item != item || slot.quantity >= item.maxStackSize) continue;
                var space = item.maxStackSize - slot.quantity;
                var moved = Mathf.Min(space, amount);
                slot.quantity += moved;
                amount -= moved;
                if (amount == 0) break;
            }
        }

        while (amount > 0)
        {
            if (slots.Count >= maxSlots)
            {
                OnInventoryChanged?.Invoke();
                return false;
            }
            var stack = item.isStackable ? Mathf.Min(amount, item.maxStackSize) : 1;
            slots.Add(new InventorySlot(item, stack));
            amount -= stack;
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0 || GetCount(item) < amount) return false;

        for (var i = slots.Count - 1; i >= 0 && amount > 0; i--)
        {
            if (slots[i].item != item) continue;
            var removed = Mathf.Min(slots[i].quantity, amount);
            slots[i].quantity -= removed;
            amount -= removed;
            if (slots[i].quantity == 0) slots.RemoveAt(i);
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public int GetCount(ItemData item)
    {
        var total = 0;
        foreach (var slot in slots)
            if (slot.item == item) total += slot.quantity;
        return total;
    }
}
