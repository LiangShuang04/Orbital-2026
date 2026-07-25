using System;
using UnityEngine;

public class InteractableObject : MonoBehaviour, IInteractable
{
    public string ItemName;

    public static event Action<InteractableObject, GameObject> Interacted;

    public string getItemName() => ItemName;

    public string GetDisplayName() => ItemName;

    public virtual void Interact(GameObject interactor)
    {
        Interacted?.Invoke(this, interactor);
        Debug.Log($"Interacted with {ItemName}");
    }
}
