using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DontDiePlease.Systems
{
    public sealed class PauseMenuBootstrapper : MonoBehaviour
    {
        [SerializeField] private string mainMenuSceneName = "MainMenuScene";
        [SerializeField] private Font uiFont;
        [SerializeField] private bool buildOnAwake = true;

        private Font resolvedFont;
        private Sprite panelSprite;
        private Sprite buttonSprite;
        private Sprite circleSprite;

        private static readonly Color BarColor = new Color(0.025f, 0.04f, 0.05f, 0.92f);
        private static readonly Color PanelColor = new Color(0.045f, 0.075f, 0.095f, 0.96f);
        private static readonly Color PanelRimColor = new Color(0.12f, 0.24f, 0.29f, 1f);
        private static readonly Color PrimaryColor = new Color(0.52f, 0.95f, 1f, 1f);
        private static readonly Color SecondaryColor = new Color(0.11f, 0.22f, 0.27f, 1f);
        private static readonly Color DangerColor = new Color(0.86f, 0.22f, 0.18f, 1f);
        private static readonly Color GlowColor = new Color(0.2f, 0.72f, 1f, 0.34f);
        private static readonly Color LineColor = new Color(0.55f, 0.96f, 1f, 0.42f);
        private static readonly Color MutedText = new Color(0.48f, 0.72f, 0.78f, 0.78f);
        private static readonly Color TextLight = new Color(0.9f, 1f, 1f, 1f);
        private static readonly Color TextDark = new Color(0.02f, 0.05f, 0.065f, 1f);

        private void Awake()
        {
            if (buildOnAwake)
            {
                Build();
            }
        }

        public void Build()
        {
            if (FindObjectOfType<PauseSettingsMenuController>() != null)
            {
                return;
            }

            EnsureEventSystem();
            resolvedFont = ResolveFont();
            panelSprite = CreateRoundedRectSprite(96, 96, 10);
            buttonSprite = CreateRoundedRectSprite(96, 48, 8);
            circleSprite = CreateCircleSprite(48);

            var canvas = CreateCanvas();
            var topBar = CreateTopBar(canvas.transform, out var pauseButton);
            var pauseRoot = CreatePauseRoot(canvas.transform, out var settingsPanel, out var resumeButton, out var restartButton, out var settingsButton, out var mainMenuButton, out var quitButton, out var backButton, out var mouseSensitivitySlider, out var masterVolumeSlider, out var fullscreenToggle, out var mouseSensitivityValueText, out var masterVolumeValueText);

            topBar.SetAsFirstSibling();

            var controller = canvas.gameObject.AddComponent<PauseSettingsMenuController>();
            controller.SetMainMenuSceneName(mainMenuSceneName);
            controller.SetGeneratedReferences(
                pauseRoot.gameObject,
                settingsPanel.gameObject,
                pauseButton,
                resumeButton,
                restartButton,
                settingsButton,
                mainMenuButton,
                quitButton,
                backButton,
                mouseSensitivitySlider,
                masterVolumeSlider,
                fullscreenToggle,
                mouseSensitivityValueText,
                masterVolumeValueText);
        }

        private RectTransform CreateTopBar(Transform parent, out Button pauseButton)
        {
            var bar = CreateImage(parent, "MenuBar", BarColor, null);
            StretchTop(bar, 64f);

            var bottomGlow = CreateImage(bar, "BottomGlow", GlowColor, null);
            SetRect(bottomGlow, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1920f, 3f));
            bottomGlow.GetComponent<Image>().raycastTarget = false;

            var leftPanel = CreateImage(bar, "LeftTelemetryPanel", new Color(0.04f, 0.12f, 0.15f, 0.78f), buttonSprite);
            SetRect(leftPanel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(275f, 0f), new Vector2(510f, 46f));
            leftPanel.GetComponent<Image>().type = Image.Type.Sliced;
            leftPanel.GetComponent<Image>().raycastTarget = false;

            var title = CreateText(bar, "Title", "DON'T DIE PLEASE", 22, FontStyle.Bold, PrimaryColor, TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(168f, 0f), new Vector2(300f, 44f));
            AddShadow(title.gameObject, new Color(0f, 0.55f, 0.7f, 0.48f), new Vector2(1f, -1f));

            var status = CreateText(bar, "Status", "SURVIVAL LINK ACTIVE", 13, FontStyle.Bold, new Color(0.62f, 0.9f, 0.95f, 0.78f), TextAnchor.MiddleLeft);
            SetRect(status.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(430f, 0f), new Vector2(250f, 34f));

            CreateStatusDot(bar, new Vector2(28f, 0f), PrimaryColor);
            CreateStatusDot(bar, new Vector2(56f, 0f), new Color(0.75f, 0.95f, 0.28f, 1f));
            CreateStatusDot(bar, new Vector2(84f, 0f), new Color(0.95f, 0.38f, 0.26f, 1f));

            CreateTopBarReadout(bar, "O2", "STABLE", new Vector2(685f, 0f));
            CreateTopBarReadout(bar, "TOX", "LOW", new Vector2(835f, 0f));
            CreateTopBarReadout(bar, "SIG", "OFFLINE", new Vector2(990f, 0f));

            var rightFrame = CreateImage(bar, "RightMenuFrame", new Color(0.04f, 0.12f, 0.15f, 0.76f), buttonSprite);
            SetRect(rightFrame, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-92f, 0f), new Vector2(148f, 46f));
            rightFrame.GetComponent<Image>().type = Image.Type.Sliced;
            rightFrame.GetComponent<Image>().raycastTarget = false;

            pauseButton = CreateButton(bar, "PauseButton", "MENU", SecondaryColor, TextLight, 15);
            SetRect(pauseButton.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-88f, 0f), new Vector2(120f, 42f));

            return bar;
        }

        private RectTransform CreatePauseRoot(
            Transform parent,
            out RectTransform settingsPanel,
            out Button resumeButton,
            out Button restartButton,
            out Button settingsButton,
            out Button mainMenuButton,
            out Button quitButton,
            out Button backButton,
            out Slider mouseSensitivitySlider,
            out Slider masterVolumeSlider,
            out Toggle fullscreenToggle,
            out Text mouseSensitivityValueText,
            out Text masterVolumeValueText)
        {
            var root = CreateImage(parent, "PauseMenuRoot", new Color(0f, 0f, 0f, 0.66f), null);
            Stretch(root);
            AddBackdropTexture(root);

            var panel = CreateImage(root, "PausePanel", PanelColor, panelSprite);
            SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 690f));
            panel.GetComponent<Image>().type = Image.Type.Sliced;

            var rim = CreateImage(panel, "PausePanelRim", PanelRimColor, panelSprite);
            StretchWithMargin(rim, 9f);
            rim.GetComponent<Image>().type = Image.Type.Sliced;

            var inner = CreateImage(panel, "PausePanelInner", new Color(0.025f, 0.052f, 0.068f, 0.96f), panelSprite);
            StretchWithMargin(inner, 22f);
            inner.GetComponent<Image>().type = Image.Type.Sliced;
            AddPanelDetails(inner, new Vector2(560f, 630f));

            var title = CreateText(inner, "PauseTitle", "MISSION MENU", 34, FontStyle.Bold, PrimaryColor, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(480f, 60f));
            AddShadow(title.gameObject, new Color(0f, 0.52f, 0.7f, 0.6f), new Vector2(2f, -2f));

            var subtitle = CreateText(inner, "PauseSubtitle", "DON'T DIE PLEASE", 14, FontStyle.Bold, new Color(0.62f, 0.9f, 0.95f, 0.8f), TextAnchor.MiddleCenter);
            SetRect(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -116f), new Vector2(420f, 30f));

            CreateThinLine(inner, new Vector2(0.5f, 1f), new Vector2(0f, -145f), new Vector2(420f, 2f), LineColor);
            CreateSideLabel(inner, "SYSTEM", new Vector2(-252f, -88f));
            CreateSideLabel(inner, "RESCUE", new Vector2(252f, -88f));

            resumeButton = CreateButton(inner, "ResumeButton", "RESUME", PrimaryColor, TextDark, 22);
            restartButton = CreateButton(inner, "RestartButton", "RESTART", SecondaryColor, TextLight, 20);
            settingsButton = CreateButton(inner, "SettingsButton", "SETTINGS", SecondaryColor, TextLight, 20);
            mainMenuButton = CreateButton(inner, "MainMenuButton", "MAIN MENU", SecondaryColor, TextLight, 20);
            quitButton = CreateButton(inner, "QuitButton", "QUIT", DangerColor, TextLight, 20);

            SetRect(resumeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(420f, 62f));
            SetRect(restartButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -270f), new Vector2(420f, 58f));
            SetRect(settingsButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -348f), new Vector2(420f, 58f));
            SetRect(mainMenuButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -426f), new Vector2(420f, 58f));
            SetRect(quitButton.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -504f), new Vector2(420f, 58f));

            var hint = CreateText(inner, "PauseHint", "ESC toggles this menu", 13, FontStyle.Bold, new Color(0.66f, 0.88f, 0.92f, 0.7f), TextAnchor.MiddleCenter);
            SetRect(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(400f, 26f));

            CreateThinLine(inner, new Vector2(0.5f, 0f), new Vector2(0f, 86f), new Vector2(360f, 2f), new Color(0.55f, 0.96f, 1f, 0.22f));

            settingsPanel = CreateSettingsPanel(root, out backButton, out mouseSensitivitySlider, out masterVolumeSlider, out fullscreenToggle, out mouseSensitivityValueText, out masterVolumeValueText);
            return root;
        }

        private RectTransform CreateSettingsPanel(
            Transform parent,
            out Button backButton,
            out Slider mouseSensitivitySlider,
            out Slider masterVolumeSlider,
            out Toggle fullscreenToggle,
            out Text mouseSensitivityValueText,
            out Text masterVolumeValueText)
        {
            var panel = CreateImage(parent, "SettingsPanel", new Color(0.018f, 0.036f, 0.048f, 0.98f), panelSprite);
            SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 560f));
            panel.GetComponent<Image>().type = Image.Type.Sliced;

            var rim = CreateImage(panel, "SettingsRim", PanelRimColor, panelSprite);
            StretchWithMargin(rim, 9f);
            rim.GetComponent<Image>().type = Image.Type.Sliced;
            AddPanelDetails(panel, new Vector2(620f, 500f));

            var title = CreateText(panel, "SettingsTitle", "SETTINGS", 32, FontStyle.Bold, PrimaryColor, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(480f, 56f));
            AddShadow(title.gameObject, new Color(0f, 0.52f, 0.7f, 0.55f), new Vector2(2f, -2f));
            CreateThinLine(panel, new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(430f, 2f), LineColor);

            CreateText(panel, "MouseSensitivityLabel", "MOUSE SENSITIVITY", 16, FontStyle.Bold, TextLight, TextAnchor.MiddleLeft, new Vector2(0.5f, 1f), new Vector2(0f, -158f), new Vector2(470f, 32f));
            mouseSensitivitySlider = CreateSlider(panel, "MouseSensitivitySlider", 0.2f, 5f);
            SetRect(mouseSensitivitySlider.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-38f, -205f), new Vector2(392f, 34f));
            mouseSensitivityValueText = CreateText(panel, "MouseSensitivityValue", "1.00", 16, FontStyle.Bold, PrimaryColor, TextAnchor.MiddleRight);
            SetRect(mouseSensitivityValueText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(238f, -205f), new Vector2(80f, 34f));

            CreateText(panel, "MasterVolumeLabel", "MASTER VOLUME", 16, FontStyle.Bold, TextLight, TextAnchor.MiddleLeft, new Vector2(0.5f, 1f), new Vector2(0f, -274f), new Vector2(470f, 32f));
            masterVolumeSlider = CreateSlider(panel, "MasterVolumeSlider", 0f, 1f);
            SetRect(masterVolumeSlider.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-38f, -321f), new Vector2(392f, 34f));
            masterVolumeValueText = CreateText(panel, "MasterVolumeValue", "100%", 16, FontStyle.Bold, PrimaryColor, TextAnchor.MiddleRight);
            SetRect(masterVolumeValueText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(238f, -321f), new Vector2(80f, 34f));

            fullscreenToggle = CreateToggle(panel, "FullscreenToggle", "FULLSCREEN");
            SetRect(fullscreenToggle.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-95f, -398f), new Vector2(280f, 42f));

            CreateThinLine(panel, new Vector2(0.5f, 0f), new Vector2(0f, 132f), new Vector2(430f, 2f), new Color(0.55f, 0.96f, 1f, 0.22f));

            backButton = CreateButton(panel, "BackButton", "BACK", PrimaryColor, TextDark, 20);
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 68f), new Vector2(320f, 56f));

            return panel;
        }

        private Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("RuntimeMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private Button CreateButton(Transform parent, string name, string label, Color color, Color textColor, int fontSize)
        {
            var buttonObject = new GameObject(name, typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.GetComponent<Image>();
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            AddShadow(buttonObject, new Color(0f, 0f, 0f, 0.48f), new Vector2(4f, -4f));
            AddButtonAccents(buttonObject.transform, color == PrimaryColor ? TextDark : PrimaryColor);

            var text = CreateText(buttonObject.transform, "Text", label, fontSize, FontStyle.Bold, textColor, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            AddShadow(text.gameObject, color == PrimaryColor ? new Color(1f, 1f, 1f, 0.2f) : new Color(0f, 0f, 0f, 0.45f), new Vector2(1f, -1f));

            var button = buttonObject.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = MultiplyColor(color, 1.14f);
            colors.pressedColor = MultiplyColor(color, 0.82f);
            colors.disabledColor = new Color(0.16f, 0.18f, 0.2f, 0.8f);
            button.colors = colors;
            return button;
        }

        private Slider CreateSlider(Transform parent, string name, float minValue, float maxValue)
        {
            var sliderObject = new GameObject(name, typeof(Slider));
            sliderObject.transform.SetParent(parent, false);

            var background = CreateImage(sliderObject.transform, "Background", new Color(0.08f, 0.14f, 0.17f, 1f), buttonSprite);
            Stretch(background);
            background.GetComponent<Image>().type = Image.Type.Sliced;
            AddOutline(background.gameObject, new Color(0.22f, 0.54f, 0.62f, 0.62f), new Vector2(1f, -1f));

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObject.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(6f, 6f);
            fillAreaRect.offsetMax = new Vector2(-6f, -6f);

            var fill = CreateImage(fillArea.transform, "Fill", PrimaryColor, buttonSprite);
            Stretch(fill);
            fill.GetComponent<Image>().type = Image.Type.Sliced;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObject.transform, false);
            Stretch(handleArea.GetComponent<RectTransform>());

            var handle = CreateImage(handleArea.transform, "Handle", TextLight, circleSprite);
            SetRect(handle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f));
            AddOutline(handle.gameObject, PrimaryColor, new Vector2(2f, -2f));

            var slider = sliderObject.GetComponent<Slider>();
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            return slider;
        }

        private Toggle CreateToggle(Transform parent, string name, string label)
        {
            var toggleObject = new GameObject(name, typeof(Toggle));
            toggleObject.transform.SetParent(parent, false);

            var box = CreateImage(toggleObject.transform, "Background", new Color(0.08f, 0.14f, 0.17f, 1f), buttonSprite);
            SetRect(box, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(34f, 34f));
            box.GetComponent<Image>().type = Image.Type.Sliced;
            AddOutline(box.gameObject, new Color(0.22f, 0.54f, 0.62f, 0.62f), new Vector2(1f, -1f));

            var checkmark = CreateImage(box, "Checkmark", PrimaryColor, buttonSprite);
            StretchWithMargin(checkmark, 7f);
            checkmark.GetComponent<Image>().type = Image.Type.Sliced;

            var text = CreateText(toggleObject.transform, "Label", label, 16, FontStyle.Bold, TextLight, TextAnchor.MiddleLeft);
            SetRect(text.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(174f, 0f), new Vector2(220f, 36f));

            var toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = box.GetComponent<Image>();
            toggle.graphic = checkmark.GetComponent<Image>();
            return toggle;
        }

        private void AddBackdropTexture(Transform parent)
        {
            var horizontal = CreateImage(parent, "BackdropHorizontalLine", new Color(0.42f, 0.9f, 1f, 0.08f), null);
            SetRect(horizontal, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -245f), new Vector2(1240f, 2f));
            horizontal.GetComponent<Image>().raycastTarget = false;

            for (var index = 0; index < 8; index++)
            {
                var line = CreateImage(parent, "BackdropScanLine", new Color(0.42f, 0.9f, 1f, 0.035f), null);
                SetRect(line, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -330f + index * 92f), new Vector2(1120f, 1f));
                line.GetComponent<Image>().raycastTarget = false;
            }
        }

        private void AddPanelDetails(Transform parent, Vector2 size)
        {
            CreateCorner(parent, new Vector2(0f, 1f), new Vector2(42f, -42f), 1f, 1f);
            CreateCorner(parent, new Vector2(1f, 1f), new Vector2(-42f, -42f), -1f, 1f);
            CreateCorner(parent, new Vector2(0f, 0f), new Vector2(42f, 42f), 1f, -1f);
            CreateCorner(parent, new Vector2(1f, 0f), new Vector2(-42f, 42f), -1f, -1f);

            var topLine = CreateImage(parent, "PanelTopGlow", new Color(0.52f, 0.96f, 1f, 0.22f), null);
            SetRect(topLine, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(size.x - 130f, 2f));
            topLine.GetComponent<Image>().raycastTarget = false;

            var bottomLine = CreateImage(parent, "PanelBottomGlow", new Color(0.52f, 0.96f, 1f, 0.14f), null);
            SetRect(bottomLine, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(size.x - 130f, 2f));
            bottomLine.GetComponent<Image>().raycastTarget = false;
        }

        private void CreateCorner(Transform parent, Vector2 anchor, Vector2 position, float xSign, float ySign)
        {
            var horizontal = CreateImage(parent, "CornerHorizontal", PrimaryColor, null);
            SetRect(horizontal, anchor, anchor, position + new Vector2(18f * xSign, 0f), new Vector2(64f, 4f));
            horizontal.GetComponent<Image>().raycastTarget = false;

            var vertical = CreateImage(parent, "CornerVertical", PrimaryColor, null);
            SetRect(vertical, anchor, anchor, position + new Vector2(0f, 18f * ySign), new Vector2(4f, 64f));
            vertical.GetComponent<Image>().raycastTarget = false;
        }

        private void CreateThinLine(Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var line = CreateImage(parent, "ThinLine", color, null);
            SetRect(line, anchor, anchor, position, size);
            line.GetComponent<Image>().raycastTarget = false;
        }

        private void CreateSideLabel(Transform parent, string label, Vector2 position)
        {
            var text = CreateText(parent, $"{label}SideLabel", label, 10, FontStyle.Bold, MutedText, TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), position, new Vector2(110f, 24f));
        }

        private void CreateTopBarReadout(Transform parent, string label, string value, Vector2 position)
        {
            var frame = CreateImage(parent, $"{label}Readout", new Color(0.035f, 0.09f, 0.11f, 0.72f), buttonSprite);
            SetRect(frame, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), position, new Vector2(132f, 38f));
            frame.GetComponent<Image>().type = Image.Type.Sliced;
            frame.GetComponent<Image>().raycastTarget = false;

            var labelText = CreateText(frame, "Label", label, 10, FontStyle.Bold, MutedText, TextAnchor.MiddleLeft);
            SetRect(labelText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(31f, 7f), new Vector2(45f, 18f));

            var valueText = CreateText(frame, "Value", value, 11, FontStyle.Bold, PrimaryColor, TextAnchor.MiddleRight);
            SetRect(valueText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-48f, -6f), new Vector2(78f, 18f));
        }

        private void AddButtonAccents(Transform parent, Color color)
        {
            var leftRail = CreateImage(parent, "ButtonLeftRail", color, null);
            SetRect(leftRail, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(4f, 30f));
            leftRail.GetComponent<Image>().raycastTarget = false;

            var topLine = CreateImage(parent, "ButtonTopLine", new Color(color.r, color.g, color.b, 0.45f), null);
            SetRect(topLine, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -7f), new Vector2(240f, 2f));
            topLine.GetComponent<Image>().raycastTarget = false;
        }

        private RectTransform CreateImage(Transform parent, string name, Color color, Sprite sprite)
        {
            var imageObject = new GameObject(name, typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            return imageObject.GetComponent<RectTransform>();
        }

        private Text CreateText(Transform parent, string name, string value, int size, FontStyle style, Color color, TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = resolvedFont;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static void AddShadow(GameObject target, Color color, Vector2 distance)
        {
            var shadow = target.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            var outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        private Text CreateText(Transform parent, string name, string value, int size, FontStyle style, Color color, TextAnchor alignment, Vector2 anchor, Vector2 position, Vector2 sizeDelta)
        {
            var text = CreateText(parent, name, value, size, style, color, alignment);
            SetRect(text.rectTransform, anchor, anchor, position, sizeDelta);
            return text;
        }

        private void CreateStatusDot(Transform parent, Vector2 position, Color color)
        {
            var dot = CreateImage(parent, "StatusDot", color, circleSprite);
            SetRect(dot, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), position, new Vector2(12f, 12f));
            dot.GetComponent<Image>().raycastTarget = false;
        }

        private Font ResolveFont()
        {
            if (uiFont != null)
            {
                return uiFont;
            }

            var font = Font.CreateDynamicFontFromOSFont(new[] { "Bahnschrift", "Segoe UI", "Arial", "Verdana" }, 24);

            if (font != null)
            {
                return font;
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void StretchTop(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, height);
        }

        private static void StretchWithMargin(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Color MultiplyColor(Color color, float multiplier)
        {
            return new Color(
                Mathf.Clamp01(color.r * multiplier),
                Mathf.Clamp01(color.g * multiplier),
                Mathf.Clamp01(color.b * multiplier),
                color.a);
        }

        private static Sprite CreateCircleSprite(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.name = "RuntimeMenuCircleSprite";
            var center = (size - 1) * 0.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var alpha = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) <= center ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Sprite CreateRoundedRectSprite(int width, int height, int radius)
        {
            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            texture.name = "RuntimeMenuRoundedRectSprite";

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var px = x < radius ? radius : x > width - radius - 1 ? width - radius - 1 : x;
                    var py = y < radius ? radius : y > height - radius - 1 ? height - radius - 1 : y;
                    var alpha = Vector2.Distance(new Vector2(x, y), new Vector2(px, py)) <= radius ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            var border = new Vector4(radius, radius, radius, radius);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }
    }
}
