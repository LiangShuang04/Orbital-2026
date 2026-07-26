using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full-screen "YOU DIED" overlay with Respawn and Main Menu buttons
/// Builds itself in code and listens for the player's death via PlayerStats.OnDied
/// No editor setup needed
/// </summary>
public class DeathScreen : MonoBehaviour
{
    // must match the main menu scene's name in Build Settings
    const string MainMenuScene = "MainMenuScene";

    GameObject panel;
    PlayerStats stats;
    float nextSearchTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindObjectOfType<DeathScreen>() != null) return;
        var go = new GameObject("DeathScreen (auto)");
        go.AddComponent<DeathScreen>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        EnsureEventSystem();
        BuildUI();
        panel.SetActive(false);
    }

    void OnDestroy()
    {
        if (stats != null) stats.OnDied -= Show;
    }

    void Update()
    {
        // stats becomes null across scene loads, so keep re-finding the player
        if (stats == null && Time.unscaledTime >= nextSearchTime)
        {
            nextSearchTime = Time.unscaledTime + 0.5f;
            var found = FindObjectOfType<PlayerStats>();
            if (found != null)
            {
                stats = found;
                stats.OnDied += Show;
                if (stats.IsDead) Show(); // already dead before we found it
            }
        }
    }

    void Show()
    {
        panel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnRespawn()
    {
        if (stats != null) stats.Respawn();
        panel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnMainMenu()
    {
        panel.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(MainMenuScene);
    }

    void BuildUI()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // above every other HUD

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        // full-screen black overlay
        var panelRect = NewRect("Panel", transform);
        Stretch(panelRect);
        panel = panelRect.gameObject;
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.9f);

        // "YOU DIED" title
        var titleRect = NewRect("Title", panelRect);
        titleRect.anchorMin = titleRect.anchorMax = titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 130f);
        titleRect.sizeDelta = new Vector2(1000f, 130f);
        var title = titleRect.gameObject.AddComponent<TextMeshProUGUI>();
        title.text = "YOU DIED";
        title.fontSize = 84f;
        title.color = new Color(0.8f, 0.1f, 0.1f);
        title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null) title.font = TMP_Settings.defaultFontAsset;

        CreateButton(panelRect, "RESPAWN", new Vector2(0f, -10f), OnRespawn);
        CreateButton(panelRect, "RETURN TO MAIN MENU", new Vector2(0f, -90f), OnMainMenu);
    }

    void CreateButton(RectTransform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var rect = NewRect(label, parent);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(360f, 60f);

        var img = rect.gameObject.AddComponent<Image>();
        img.color = new Color(0.15f, 0.16f, 0.20f, 1f);

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(onClick);

        var textRect = NewRect("Text", rect);
        Stretch(textRect);
        var text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 22f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null) text.font = TMP_Settings.defaultFontAsset;
    }

    // clicking UI needs an EventSystem, create one if the scene has none
    static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(go);
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
