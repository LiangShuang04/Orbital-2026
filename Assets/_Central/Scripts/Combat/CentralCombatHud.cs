using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DontDiePlease.Central.Combat
{
    [DisallowMultipleComponent]
    public sealed class CentralCombatHud : MonoBehaviour
    {
        private CentralCombatSpawner spawner;
        private TextMeshProUGUI statusText;
        private TextMeshProUGUI objectiveText;

        public static CentralCombatHud Create(CentralCombatSpawner waves)
        {
            var root = new GameObject("CentralCombatHUD");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            root.AddComponent<GraphicRaycaster>();

            var hud = root.AddComponent<CentralCombatHud>();
            hud.spawner = waves;
            hud.Build();
            return hud;
        }

        private void Build()
        {
            var panel = Rect("CombatInfo", transform, new Vector2(24f, -24f), new Vector2(340f, 86f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            var bg = panel.gameObject.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.025f, 0.028f, 0.72f);

            statusText = Label("Status", panel, new Vector2(16f, -14f), new Vector2(300f, 26f), 18f, FontStyles.Bold, new Color(0.78f, 0.88f, 0.92f, 1f));
            objectiveText = Label("Objective", panel, new Vector2(16f, -47f), new Vector2(300f, 24f), 13f, FontStyles.Normal, new Color(0.68f, 0.72f, 0.71f, 1f));

            var crosshairRoot = Rect("Crosshair", transform, Vector2.zero, new Vector2(52f, 52f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            CreateLine("Horizontal", crosshairRoot, new Vector2(0f, 0f), new Vector2(32f, 2f));
            CreateLine("Vertical", crosshairRoot, new Vector2(0f, 0f), new Vector2(2f, 32f));
        }

        private void Update()
        {
            if (spawner == null)
                return;

            statusText.text = $"Wave {Mathf.Max(1, spawner.CurrentWave)}  |  Enemies {spawner.ActiveEnemyCount}";
            objectiveText.text = spawner.ActiveEnemyCount > 0 ? "Clear the Central foundry and stay alive" : "Regroup, reload, next wave incoming";
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
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Left;
            text.textWrappingMode = TextWrappingModes.NoWrap;
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
