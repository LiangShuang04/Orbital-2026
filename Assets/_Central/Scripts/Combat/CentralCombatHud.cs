using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DontDiePlease.Central.Combat
{
    [DisallowMultipleComponent]
    public sealed class CentralCombatHud : MonoBehaviour
    {
        private CentralCombatSpawner spawner;
        private RectTransform combatInfo;
        private TextMeshProUGUI statusText;
        private TextMeshProUGUI objectiveText;

        public static CentralCombatHud Create(CentralCombatSpawner waves)
        {
            var root = new GameObject("CentralCombatHUD");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            var hud = root.AddComponent<CentralCombatHud>();
            hud.spawner = waves;
            hud.Build();
            return hud;
        }

        private void Build()
        {
            combatInfo = Rect("CombatInfo", transform, new Vector2(24f, -24f), new Vector2(390f, 98f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            var bg = combatInfo.gameObject.AddComponent<Image>();
            bg.color = new Color(0.008f, 0.016f, 0.022f, 0.9f);
            var accent = Rect("CombatAccent", combatInfo, Vector2.zero, new Vector2(5f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f));
            accent.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.88f, 0.94f, 1f);

            statusText = Label("Status", combatInfo, new Vector2(20f, -13f), new Vector2(346f, 30f), 21f, FontStyles.Bold, new Color(0.88f, 0.97f, 1f, 1f));
            objectiveText = Label("Objective", combatInfo, new Vector2(20f, -53f), new Vector2(346f, 26f), 15f, FontStyles.Normal, new Color(0.8f, 0.86f, 0.88f, 1f));

            var crosshairRoot = Rect("Crosshair", transform, Vector2.zero, new Vector2(52f, 52f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            CreateLine("Horizontal", crosshairRoot, new Vector2(0f, 0f), new Vector2(32f, 2f));
            CreateLine("Vertical", crosshairRoot, new Vector2(0f, 0f), new Vector2(2f, 32f));
        }

        private void Update()
        {
            if (spawner == null)
                return;

            var prologue = FenrisFrigatePrologue.Instance;
            combatInfo.gameObject.SetActive(prologue == null || !prologue.BlocksCombat);
            statusText.text = $"WAVE {Mathf.Max(1, spawner.CurrentWave)}   ENEMIES {spawner.ActiveEnemyCount}";
            objectiveText.text = spawner.ActiveEnemyCount > 0
                ? "Clear the foundry and stay alive"
                : "Reload now. Next wave incoming.";
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size, Vector2 min, Vector2 max)
        {
            var obj = new GameObject(name);
            var rect = obj.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(min.x == max.x ? min.x : 0.5f, min.y == max.y ? min.y : 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return rect;
        }

        private static TextMeshProUGUI Label(string name, Transform parent, Vector2 pos, Vector2 size, float fontSize, FontStyles style, Color color)
        {
            var rect = Rect(name, parent, pos, size, new Vector2(0f, 1f), new Vector2(0f, 1f));
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF") ??
                        TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Left;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.outlineColor = new Color(0f, 0f, 0f, 0.8f);
            text.outlineWidth = 0.04f;
            text.raycastTarget = false;
            return text;
        }

        private static void CreateLine(string name, Transform parent, Vector2 anchoredPosition, Vector2 size)
        {
            var rect = Rect(name, parent, anchoredPosition, size, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.72f, 0.88f, 0.9f, 0.82f);
        }
    }
}
