using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One stack of items in the inventory: an item definition plus how many are held
/// </summary>
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

/// <summary>
/// In-memory inventory model held on the player. Handles stacking and slot limits
/// No database / save calls happen here; persistence handled elsewhere later
/// </summary>
public class Inventory : MonoBehaviour
{
    [Tooltip("Maximum number of stacks the inventory can hold.")]
    [SerializeField] private int maxSlots = 24;

    private readonly List<InventorySlot> slots = new List<InventorySlot>();

    /// <summary>Read-only view of the current stacks for UI to render</summary>
    public IReadOnlyList<InventorySlot> Slots => slots;

    /// <summary>Raised whenever the contents change so the UI can refresh</summary>
    public event Action OnInventoryChanged;

    /// <summary>
    /// Adds <paramref name="amount"/> of <paramref name="item"/>, filling existing
    /// stacks first then creating new slots. Returns false if the inventory ran out
    /// of room before everything fit (the portion that fit is still added)
    /// </summary>
    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        // if item is stackable and not at maxstacks -> topup into the stack
        if (item.isStackable)
        {
            foreach (var slot in slots)
            {
                if (slot.item != item || slot.quantity >= item.maxStackSize) continue;
                int space = item.maxStackSize - slot.quantity;
                int moved = Mathf.Min(space, amount);
                slot.quantity += moved;
                amount -= moved;
                if (amount == 0) break;
            }
        }

        // else, place remainder into new slots
        while (amount > 0)
        {
            if (slots.Count >= maxSlots)
            {
                OnInventoryChanged?.Invoke();
                return false; // out of room; partial add already applied
            }
            int stack = item.isStackable ? Mathf.Min(amount, item.maxStackSize) : 1;
            slots.Add(new InventorySlot(item, stack));
            amount -= stack;
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Removes up to <paramref name="amount"/> of <paramref name="item"/> across stacks
    /// Returns true only if the full amount was available and removed
    /// </summary>
    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0 || GetCount(item) < amount) return false;

        for (int i = slots.Count - 1; i >= 0 && amount > 0; i--)
        {
            if (slots[i].item != item) continue;
            int removed = Mathf.Min(slots[i].quantity, amount);
            slots[i].quantity -= removed;
            amount -= removed;
            if (slots[i].quantity == 0) slots.RemoveAt(i);
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>Total quantity of an item held across all stacks</summary>
    public int GetCount(ItemData item)
    {
        int total = 0;
        foreach (var slot in slots)
            if (slot.item == item) total += slot.quantity;
        return total;
    }
}
