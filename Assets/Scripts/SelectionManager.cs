using UnityEngine;
using TMPro;

public class SelectionManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject interaction_Info_UI;
    [SerializeField] private string promptSuffix = "  [E]";

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
        if (Camera.main == null) return;

        IInteractable target = null;
        var ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out var hit, interactDistance, ~0, QueryTriggerInteraction.Ignore))
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
