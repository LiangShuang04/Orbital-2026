using UnityEngine;

/// <summary>
/// A simple named interactable used for non-pickup objects (key items such as the
/// generator and dematerialiser, doors, etc.) Pickups should use ItemPickup instead
/// </summary>
public class InteractableObject : MonoBehaviour, IInteractable
{
    public string ItemName;

    // Kept for backwards compatibility with existing callers
    public string getItemName() => ItemName;

    public string GetDisplayName() => ItemName;

    // Base behaviour is a no-op; specialised interactables can override Interact
    public virtual void Interact(GameObject interactor)
    {
        Debug.Log($"Interacted with {ItemName}");
    }
}
