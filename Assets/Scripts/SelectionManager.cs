using UnityEngine;
using TMPro;

// Looks at whatever is under the crosshair and lets the player interact
// Handles both our own IInteractable (pickups, crafting, our doors) and the
// Vattalus ship interactables (ship doors, switches)
public class SelectionManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject interaction_Info_UI;

    [Header("Interaction")]
    [SerializeField] private float interactDistance = 3.5f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private Inventory playerInventory;

    private TMP_Text interactionText;
    private GameObject interactor;

    void Start()
    {
        if (interaction_Info_UI != null)
            interactionText = interaction_Info_UI.GetComponent<TMP_Text>();

        if (playerInventory == null)
            playerInventory = FindObjectOfType<Inventory>();

        interactor = playerInventory != null ? playerInventory.gameObject : gameObject;
    }

    void Update()
    {
        if (Camera.main == null) { HidePrompt(); return; }

        var ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out var hit, interactDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            HidePrompt();
            return;
        }

        // our own interactables first (pickups, crafting stations, our doors)
        var mine = hit.collider.GetComponentInParent<IInteractable>();
        if (mine != null)
        {
            ShowPrompt(mine.GetDisplayName());
            if (Input.GetKeyDown(interactKey)) mine.Interact(interactor);
            return;
        }

        // vattalus ship interactables (ship doors, switches)
        var ship = hit.collider.GetComponentInParent<VattalusInteractable>();
        if (ship != null)
        {
            // Vattalus keeps separate "Open"/"Close" text for its current state
            ShowPrompt(ship.isActivated ? ship.deactivateText : ship.activateText);
            if (Input.GetKeyDown(interactKey) && ship.CanInteract) ship.Interact();
            return;
        }

        HidePrompt();
    }

    void ShowPrompt(string label)
    {
        if (interactionText != null) interactionText.text = label + $"  [{interactKey}]";
        if (interaction_Info_UI != null) interaction_Info_UI.SetActive(true);
    }

    void HidePrompt()
    {
        if (interaction_Info_UI != null) interaction_Info_UI.SetActive(false);
    }
}
