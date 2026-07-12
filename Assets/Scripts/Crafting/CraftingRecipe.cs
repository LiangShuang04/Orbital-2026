using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An item plus a quantity. Used for both a recipe's ingredients and its results
/// </summary>
[Serializable]
public class ItemStack
{
    public ItemData item;
    [Min(1)] public int quantity = 1;
}

/// <summary>
/// A crafting recipe as a data asset (same data-driven pattern as ItemData and
/// EnemyStats): a list of ingredient stacks consumed and result stacks produced.
/// Because results is a list, the same asset type also covers dematerialising —
/// one ingredient in, several raw materials out.
///
/// Create via: right-click in Project ▸ Create ▸ Crafting ▸ Recipe.
/// </summary>
[CreateAssetMenu(fileName = "New Recipe", menuName = "Crafting/Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Tooltip("Name shown in the crafting UI. Falls back to the first result's item name if empty.")]
    public string recipeName;

    [Tooltip("Items consumed when this recipe is crafted.")]
    public List<ItemStack> ingredients = new List<ItemStack>();

    [Tooltip("Items produced. One entry for normal crafting; several for dematerialising.")]
    public List<ItemStack> results = new List<ItemStack>();

    /// <summary>Display name for UI: explicit recipeName, or the first result's item name.</summary>
    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrEmpty(recipeName)) return recipeName;
            if (results.Count > 0 && results[0].item != null) return results[0].item.itemName;
            return name; // asset file name as last resort
        }
    }

    /// <summary>True if the inventory holds every ingredient in sufficient quantity.</summary>
    public bool CanCraft(Inventory inventory)
    {
        if (inventory == null) return false;
        foreach (var ing in ingredients)
        {
            if (ing.item == null) return false;
            if (inventory.GetCount(ing.item) < ing.quantity) return false;
        }
        return true;
    }

    /// <summary>
    /// Consumes the ingredients and adds the results. Returns false if the
    /// ingredients weren't available. Removing ingredients first frees slots,
    /// so results almost always fit; if the inventory is still full, the
    /// overflow is logged rather than blocking the craft.
    /// </summary>
    public bool TryCraft(Inventory inventory)
    {
        if (!CanCraft(inventory)) return false;

        foreach (var ing in ingredients)
            inventory.RemoveItem(ing.item, ing.quantity);

        foreach (var res in results)
        {
            if (res.item == null) continue;
            if (!inventory.AddItem(res.item, res.quantity))
                Debug.LogWarning($"Inventory full — could not add all of {res.item.itemName} from recipe {DisplayName}");
        }
        return true;
    }
}
