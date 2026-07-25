using UnityEngine;

public class InteractableObject : MonoBehaviour, IInteractable
{
    public string ItemName;

    public string getItemName() => ItemName;

    public string GetDisplayName() => ItemName;

    public virtual void Interact(GameObject interactor)
    {
        Debug.Log($"Interacted with {ItemName}");
    }
}
