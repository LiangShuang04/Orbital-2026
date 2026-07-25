using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectionManager : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Optional, leave empty to auto-build a prompt at runtime")]
    public GameObject interaction_Info_UI;
    [SerializeField] private string promptSuffix = "  [E]";

    [Header("Interaction")]
    [SerializeField] private float interactDistance = 3.5f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private Inventory playerInventory;

    private TMP_Text interactionText;
    private GameObject interactor;
    private GameObject autoCanvas;

    void Start()
    {
        if (interaction_Info_UI == null)
            BuildPromptUI();
        else
            interactionText = interaction_Info_UI.GetComponent<TMP_Text>();

        if (playerInventory == null)
            playerInventory = FindObjectOfType<Inventory>();

        interactor = playerInventory != null ? playerInventory.gameObject : gameObject;

        HidePrompt();
    }

    void OnDestroy()
    {
        if (autoCanvas != null) Destroy(autoCanvas);
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

        var mine = hit.collider.GetComponentInParent<IInteractable>();
        if (mine != null)
        {
            ShowPrompt(mine.GetDisplayName());
            if (Input.GetKeyDown(interactKey)) mine.Interact(interactor);
            return;
        }

        var ship = FindShipInteractable(hit.collider);
        if (ship != null)
        {
            var type = ship.GetType();
            var active = ReadBool(ship, type, "isActivated");
            var label = ReadString(ship, type, active ? "deactivateText" : "activateText");
            ShowPrompt(label);

            if (Input.GetKeyDown(interactKey) && ReadBool(ship, type, "CanInteract"))
                type.GetMethod("Interact", System.Type.EmptyTypes)?.Invoke(ship, null);

            return;
        }

        HidePrompt();
    }

    void ShowPrompt(string label)
    {
        if (interactionText != null) interactionText.text = label + promptSuffix;
        if (interaction_Info_UI != null) interaction_Info_UI.SetActive(true);
    }

    void HidePrompt()
    {
        if (interaction_Info_UI != null) interaction_Info_UI.SetActive(false);
    }

    void BuildPromptUI()
    {
        autoCanvas = new GameObject("InteractPrompt (auto)");
        var canvas = autoCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 550;

        var scaler = autoCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var textGO = new GameObject("PromptText", typeof(RectTransform));
        var rect = textGO.GetComponent<RectTransform>();
        rect.SetParent(autoCanvas.transform, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -70f);
        rect.sizeDelta = new Vector2(700f, 40f);

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 24f;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;

        interaction_Info_UI = textGO;
        interactionText = tmp;
    }

    MonoBehaviour FindShipInteractable(Collider target)
    {
        var behaviours = target.GetComponentsInParent<MonoBehaviour>(true);

        foreach (var behaviour in behaviours)
        {
            if (behaviour != null && behaviour.GetType().Name == "VattalusInteractable")
                return behaviour;
        }

        return null;
    }

    bool ReadBool(MonoBehaviour target, System.Type type, string name)
    {
        var property = type.GetProperty(name);

        if (property != null && property.PropertyType == typeof(bool))
            return (bool)property.GetValue(target);

        var field = type.GetField(name);
        return field != null && field.FieldType == typeof(bool) && (bool)field.GetValue(target);
    }

    string ReadString(MonoBehaviour target, System.Type type, string name)
    {
        var property = type.GetProperty(name);

        if (property != null && property.PropertyType == typeof(string))
            return (string)property.GetValue(target);

        var field = type.GetField(name);
        return field != null && field.FieldType == typeof(string) ? (string)field.GetValue(target) : "INTERACT";
    }
}
