using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full-screen "SIGNAL SENT" victory overlay. Builds itself in code and listens
/// for SignalGenerator.OnActivated (the win). Mirrors DeathScreen so the two
/// endings look consistent. No editor setup needed.
/// </summary>
public class WinScreen : MonoBehaviour
{
    // must match the main menu scene's name in Build Settings
    const string MainMenuScene = "MainMenuScene";

    GameObject panel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindObjectOfType<WinScreen>() != null) return;
        var go = new GameObject("WinScreen (auto)");
        go.AddComponent<WinScreen>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        EnsureEventSystem();
        BuildUI();
        panel.SetActive(false);
        SignalGenerator.OnActivated += Show;
    }

    void OnDestroy()
    {
        SignalGenerator.OnActivated -= Show;
    }

    void Show()
    {
        panel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnMainMenu()
    {
        panel.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(MainMenuScene);
    }

    void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
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

        // full-screen dark-blue overlay (distinct from the death red/black)
        var panelRect = NewRect("Panel", transform);
        Stretch(panelRect);
        panel = panelRect.gameObject;
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.05f, 0.12f, 0.92f);

        // title
        var titleRect = NewRect("Title", panelRect);
        titleRect.anchorMin = titleRect.anchorMax = titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 160f);
        titleRect.sizeDelta = new Vector2(1200f, 130f);
        var title = titleRect.gameObject.AddComponent<TextMeshProUGUI>();
        title.text = "SIGNAL SENT";
        title.fontSize = 84f;
        title.color = new Color(0.4f, 0.85f, 1f);
        title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null) title.font = TMP_Settings.defaultFontAsset;

        // subtitle
        var subRect = NewRect("Subtitle", panelRect);
        subRect.anchorMin = subRect.anchorMax = subRect.pivot = new Vector2(0.5f, 0.5f);
        subRect.anchoredPosition = new Vector2(0f, 70f);
        subRect.sizeDelta = new Vector2(1200f, 60f);
        var sub = subRect.gameObject.AddComponent<TextMeshProUGUI>();
        sub.text = "The rescue beacon is live. Help is on the way. You survived Yggdrasil.";
        sub.fontSize = 28f;
        sub.color = new Color(0.8f, 0.9f, 1f);
        sub.alignment = TextAlignmentOptions.Center;
        sub.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null) sub.font = TMP_Settings.defaultFontAsset;

        CreateButton(panelRect, "RETURN TO MAIN MENU", new Vector2(0f, -40f), OnMainMenu);
        CreateButton(panelRect, "QUIT", new Vector2(0f, -120f), OnQuit);
    }

    void CreateButton(RectTransform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var rect = NewRect(label, parent);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(360f, 60f);

        var img = rect.gameObject.AddComponent<Image>();
        img.color = new Color(0.12f, 0.20f, 0.28f, 1f);

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
