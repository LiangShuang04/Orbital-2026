using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A world object the player crafts at, like the dematerialiser
/// Works through IInteractable same as doors and pickups. Each station has its
/// own recipe list so different stations can offer different recipes
/// </summary>
public class CraftingStation : MonoBehaviour, IInteractable
{
    [Tooltip("Name shown in the interaction prompt")]
    [SerializeField] private string stationName = "Dematerialiser";

    [Tooltip("Recipes this station offers, first 9 are usable via number keys")]
    [SerializeField] private List<CraftingRecipe> recipes = new List<CraftingRecipe>();

    public IReadOnlyList<CraftingRecipe> Recipes => recipes;

    public string GetDisplayName() => stationName;

    public void Interact(GameObject interactor)
    {
        var ui = FindObjectOfType<CraftingUI>();
        if (ui == null)
        {
            Debug.LogWarning($"{name}: no CraftingUI found in the scene.", this);
            return;
        }

        var inventory = interactor.GetComponent<Inventory>()
                        ?? interactor.GetComponentInChildren<Inventory>();
        if (inventory == null)
        {
            Debug.LogWarning($"{interactor.name} has no Inventory component.", interactor);
            return;
        }

        ui.Open(this, inventory);
    }
}
