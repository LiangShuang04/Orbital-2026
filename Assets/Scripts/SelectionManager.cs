using UnityEngine;
using TMPro;

/// <summary>
/// Casts a ray from the centre of the screen (the crosshair) each frame. When it
/// hits an IInteractable within range, it shows the interaction prompt and lets the
/// player interact with the configured key.
/// </summary>
public class SelectionManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject interaction_Info_UI;
    [SerializeField] private string promptSuffix = "  [E]";

    [Header("Interaction")]
    [SerializeField] private float interactDistance = 3.5f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [Tooltip("Inventory that receives picked-up items. Auto-found if left empty.")]
    [SerializeField] private Inventory playerInventory;

    private TMP_Text interactionText;
    private GameObject interactor;   // GameObject passed to Interact() (the player)

    void Start()
    {
        if (interaction_Info_UI != null)
            interactionText = interaction_Info_UI.GetComponent<TMP_Text>();

        if (playerInventory == null)
            playerInventory = FindObjectOfType<Inventory>();

        // The interactor is whatever holds the inventory; fall back to this object.
        interactor = playerInventory != null ? playerInventory.gameObject : gameObject;
    }

    void Update()
    {
        if (Camera.main == null) return;

        IInteractable target = null;
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, ~0, QueryTriggerInteraction.Ignore))
            target = hit.collider.GetComponentInParent<IInteractable>();

        if (target != null)
        {
            if (interactionText != null) interactionText.text = target.GetDisplayName() + promptSuffix;
            if (interaction_Info_UI != null) interaction_Info_UI.SetActive(true);

            if (Input.GetKeyDown(interactKey))
                target.Interact(interactor);
        }
        else if (interaction_Info_UI != null)
        {
            interaction_Info_UI.SetActive(false);
        }
    }
}
