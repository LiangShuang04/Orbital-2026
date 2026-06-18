using UnityEngine;

/// <summary>
/// Anything the player can look at and interact with (pickups, doors, key items).
/// SelectionManager raycasts for this interface rather than a concrete type, so
/// new interactables can be added without touching the selection logic.
/// </summary>
public interface IInteractable
{
    /// <summary>Text shown in the on-screen interaction prompt while looking at this object.</summary>
    string GetDisplayName();

    /// <summary>Called when the player presses the interact key while looking at this object.</summary>
    /// <param name="interactor">The GameObject performing the interaction (the player).</param>
    void Interact(GameObject interactor);
}
