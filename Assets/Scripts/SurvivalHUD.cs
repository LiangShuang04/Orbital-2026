using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Survival stat HUD built entirely in code so an asset import can never overwrite it
/// Creates itself when the game starts, finds the PlayerStats in the scene and draws
/// health, oxygen, food and toxicity bars
/// No setup needed, having this script in the project is enough
/// </summary>
public class SurvivalHUD : MonoBehaviour
{
    class Bar
    {
        public RectTransform fill;
        public Image fillImage;
        public TextMeshProUGUI label;
        public Color baseColor;
        public string title;
        public bool dangerWhenHigh;
    }

    // tweak these if the bars overlap the FPS framework HUD
    const int BarWidth = 260;
    const int BarHeight = 20;
    const int Gap = 6;
    const int MarginX = 24;
    const int MarginY = 24;

    RectTransform root;
    Bar health, oxygen, saturation, toxicity;
    PlayerStats stats;
    float nextSearchTime;

    // builds the HUD automatically on start, no scene object required
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindObjectOfType<SurvivalHUD>() != null) return;
        var go = new GameObject("SurvivalHUD (auto)");
        go.AddComponent<SurvivalHUD>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        BuildCanvas();
        health     = CreateBar("HEALTH",   new Color(0.85f, 0.27f, 0.27f), 0, false);
        oxygen     = CreateBar("OXYGEN",   new Color(0.30f, 0.70f, 1.00f), 1, false);
        saturation = CreateBar("FOOD",     new Color(0.95f, 0.65f, 0.20f), 2, false);
        toxicity   = CreateBar("TOXICITY", new Color(0.55f, 0.85f, 0.35f), 3, true);
        SetVisible(false);
    }

    void Update()
    {
        // the player may not exist yet in menus or during scene loads, so keep looking
        if (stats == null)
        {
            if (Time.unscaledTime < nextSearchTime) return;
            nextSearchTime = Time.unscaledTime + 0.5f;
            stats = FindObjectOfType<PlayerStats>();
            SetVisible(stats != null);
            if (stats == null) return;
        }

        Apply(health, stats.currentHealth, stats.maxHealth);
        Apply(oxygen, stats.currentOxygen, stats.maxOxygen);
        Apply(saturation, stats.currentSaturation, stats.maxSaturation);
        Apply(toxicity, stats.currentToxicity, stats.maxToxicity);
    }

    void Apply(Bar bar, float current, float max)
    {
        var fraction = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        bar.fill.anchorMax = new Vector2(fraction, 1f);
        bar.label.text = $"{bar.title}   {Mathf.RoundToInt(current)}";

        // turn red as a depleting bar empties, or as toxicity climbs
        var danger = bar.dangerWhenHigh ? fraction > 0.75f : fraction < 0.25f;
        bar.fillImage.color = danger
            ? Color.Lerp(bar.baseColor, new Color(0.9f, 0.15f, 0.15f), 0.75f)
            : bar.baseColor;
    }

    void SetVisible(bool visible)
    {
        if (root != null) root.gameObject.SetActive(visible);
    }

    void BuildCanvas()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // sit above whatever HUD the FPS framework draws

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var rootGO = new GameObject("Bars", typeof(RectTransform));
        root = rootGO.GetComponent<RectTransform>();
        root.SetParent(transform, false);
        Stretch(root);
    }

    Bar CreateBar(string title, Color color, int index, bool dangerWhenHigh)
    {
        var container = NewRect(title, root);
        container.anchorMin = container.anchorMax = new Vector2(0f, 1f);
        container.pivot = new Vector2(0f, 1f);
        container.sizeDelta = new Vector2(BarWidth, BarHeight);
        container.anchoredPosition = new Vector2(MarginX, -(MarginY + index * (BarHeight + Gap)));

        var background = NewRect("Background", container);
        Stretch(background);
        var backgroundImage = background.gameObject.AddComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.55f);
        backgroundImage.raycastTarget = false;

        var fill = NewRect("Fill", container);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        var fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = color;
        fillImage.raycastTarget = false;

        var labelRect = NewRect("Label", container);
        Stretch(labelRect);
        var label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = title;
        label.fontSize = 13f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Left;
        label.margin = new Vector4(8f, 0f, 8f, 0f);
        label.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null) label.font = TMP_Settings.defaultFontAsset;

        return new Bar
        {
            fill = fill,
            fillImage = fillImage,
            label = label,
            baseColor = color,
            title = title,
            dangerWhenHigh = dangerWhenHigh
        };
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
