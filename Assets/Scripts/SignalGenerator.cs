using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The rescue signal generator — the win objective. Place it in the world as an
/// interactable (look + interact key, via SelectionManager, same as doors/pickups).
///
/// When the player interacts while carrying the required part(s) — by default the
/// crafted Signal Generator Core — it consumes them, powers on, and fires
/// OnActivated, which the WinScreen listens for. Until then the prompt tells the
/// player what they still need.
/// </summary>
public class SignalGenerator : MonoBehaviour, IInteractable
{
    [SerializeField] private string displayName = "Signal Generator";

    [Tooltip("What must be in the player's inventory to activate. Default loadout: 1x Signal Generator Core.")]
    [SerializeField] private List<ItemStack> requiredParts = new List<ItemStack>();

    [Tooltip("Remove the required parts from the inventory when the generator activates.")]
    [SerializeField] private bool consumeParts = true;

    [Tooltip("Optional object (VFX/lights/SFX) switched on when the generator powers up.")]
    [SerializeField] private GameObject poweredOnVisual;

    /// <summary>Raised once when the generator is successfully activated — this is the win.</summary>
    public static event Action OnActivated;

    private bool activated;

    public string GetDisplayName()
    {
        if (activated) return displayName + " (online)";

        var inv = FindObjectOfType<Inventory>();
        if (inv != null && HasParts(inv)) return displayName + " — activate";
        return displayName + " — " + MissingSummary(inv);
    }

    public void Interact(GameObject interactor)
    {
        if (activated) return;

        var inv = interactor.GetComponent<Inventory>()
                  ?? interactor.GetComponentInChildren<Inventory>()
                  ?? FindObjectOfType<Inventory>();
        if (inv == null) return;

        if (!HasParts(inv)) return; // prompt already shows what's missing

        if (consumeParts)
            foreach (var part in requiredParts)
                if (part != null && part.item != null) inv.RemoveItem(part.item, part.quantity);

        activated = true;
        if (poweredOnVisual != null) poweredOnVisual.SetActive(true);
        Debug.Log("Signal Generator activated — rescue signal sent!");
        OnActivated?.Invoke();
    }

    // true only if every required part is present (and at least one part is configured,
    // so an unconfigured generator can never be "won" by accident)
    private bool HasParts(Inventory inv)
    {
        if (requiredParts.Count == 0) return false;
        foreach (var part in requiredParts)
            if (part == null || part.item == null || inv.GetCount(part.item) < part.quantity)
                return false;
        return true;
    }

    // e.g. "need Signal Generator Core 0/1"
    private string MissingSummary(Inventory inv)
    {
        foreach (var part in requiredParts)
        {
            if (part == null || part.item == null) continue;
            int have = inv != null ? inv.GetCount(part.item) : 0;
            if (have < part.quantity) return $"need {part.item.itemName} {have}/{part.quantity}";
        }
        return "needs parts";
    }
}
