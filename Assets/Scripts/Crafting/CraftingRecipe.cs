using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An item plus a quantity, used for both ingredients and results
/// </summary>
[Serializable]
public class ItemStack
{
    public ItemData item;
    [Min(1)] public int quantity = 1;
}

/// <summary>
/// A crafting recipe as an asset, same idea as ItemData and EnemyStats
/// results is a list so the same asset also works for dematerialising
/// (one item in, several raw materials out)
/// Create through Create > Crafting > Recipe
/// </summary>
[CreateAssetMenu(fileName = "New Recipe", menuName = "Crafting/Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Tooltip("Name shown in the crafting UI, falls back to the first result's item name if empty")]
    public string recipeName;

    [Tooltip("Items consumed when crafting")]
    public List<ItemStack> ingredients = new List<ItemStack>();

    [Tooltip("Items produced, one entry for normal crafting or several for dematerialising")]
    public List<ItemStack> results = new List<ItemStack>();

    /// <summary>recipeName if set, otherwise the first result's item name</summary>
    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrEmpty(recipeName)) return recipeName;
            if (results.Count > 0 && results[0].item != null) return results[0].item.itemName;
            return name; // asset file name as last resort
        }
    }

    /// <summary>true if the inventory has enough of every ingredient</summary>
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
    /// Consumes the ingredients then adds the results, returns false if ingredients missing
    /// removing ingredients first frees up slots so the results almost always fit
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
                Debug.LogWarning($"Inventory full, could not add all of {res.item.itemName} from recipe {DisplayName}");
        }
        return true;
    }
}
