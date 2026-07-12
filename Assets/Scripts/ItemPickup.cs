using UnityEngine;

public class ItemPickup : MonoBehaviour, IInteractable
{
    public ItemData itemData;

    [Min(1)] public int quantity = 1;

    public string GetDisplayName()
    {
        var itemName = itemData != null ? itemData.itemName : "Unknown Item";
        return quantity > 1 ? $"{itemName} x{quantity}" : itemName;
    }

    public void Interact(GameObject interactor)
    {
        if (itemData == null)
        {
            Debug.LogWarning($"{name}: ItemPickup has no ItemData assigned.", this);
            return;
        }

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
            Debug.Log("Inventory full, cannot pick up " + GetDisplayName());
    }
}
