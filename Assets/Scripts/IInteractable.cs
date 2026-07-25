using UnityEngine;

public interface IInteractable
{
    string GetDisplayName();
    void Interact(GameObject interactor);
}
