using UnityEngine;

/// <summary>
/// A pickable item sitting in the world. References an ItemData definition and a
/// quantity; when interacted with, it adds itself to the interactor's Inventory
/// and removes the world object
/// </summary>
public class ItemPickup : MonoBehaviour, IInteractable
{
    [Tooltip("The item definition this pickup represents.")]
    public ItemData itemData;

    [Tooltip("How many of the item this pickup grants.")]
    [Min(1)] public int quantity = 1;

    public string GetDisplayName()
    {
        string itemName = itemData != null ? itemData.itemName : "Unknown Item";
        return quantity > 1 ? $"{itemName} x{quantity}" : itemName;
    }

    public void Interact(GameObject interactor)
    {
        if (itemData == null)
        {
            Debug.LogWarning($"{name}: ItemPickup has no ItemData assigned.", this);
            return;
        }

        // The Inventory may live on the interactor or one of its children
        var inventory = interactor.GetComponent<Inventory>()
                        ?? interactor.GetComponentInChildren<Inventory>();
        if (inventory == null)
        {
            Debug.LogWarning($"{interactor.name} has no Inventory component.", interactor);
            return;
        }

        if (inventory.AddItem(itemData, quantity))
            Destroy(gameObject);
        else
            Debug.Log("Inventory full — cannot pick up " + GetDisplayName());
    }
}
