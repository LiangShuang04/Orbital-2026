using UnityEngine;

/// <summary>
/// Anything the player can look at and interact with (pickups, doors, key items)
/// </summary>
public interface IInteractable
{
    /// <summary>Text shown in the on-screen interaction prompt while looking at this object</summary>
    string GetDisplayName();

    /// <summary>Called when the player presses the interact key while looking at this object</summary>
    /// <param name="interactor">The GameObject performing the interaction, i.e ususally the player</param>
    void Interact(GameObject interactor);
}
