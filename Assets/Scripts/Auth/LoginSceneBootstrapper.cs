using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DontDiePlease.Auth
{
    public class LoginSceneBootstrapper : MonoBehaviour
    {
        [SerializeField] private string targetSceneName = "";
        [SerializeField] private Font uiFont;

        private Font resolvedFont;
        private Sprite panelSprite;
        private Sprite fieldSprite;
        private Sprite pillSprite;
        private Sprite circleSprite;
        private Sprite glowSprite;

        private static readonly Color SpaceTop = new Color(0.036f, 0.028f, 0.07f, 1f);
        private static readonly Color SpaceBottom = new Color(0.003f, 0.006f, 0.016f, 1f);
        private static readonly Color MetalDark = new Color(0.035f, 0.07f, 0.095f, 1f);
        private static readonly Color MetalMid = new Color(0.105f, 0.17f, 0.215f, 1f);
        private static readonly Color MetalLight = new Color(0.25f, 0.39f, 0.46f, 1f);
        private static readonly Color Cyan = new Color(0.62f, 0.98f, 1f, 1f);
        private static readonly Color CyanDim = new Color(0.22f, 0.58f, 0.76f, 1f);
        private static readonly Color BlueGlow = new Color(0.25f, 0.68f, 1f, 1f);
        private static readonly Color Lime = new Color(0.77f, 0.94f, 0.22f, 1f);
        private static readonly Color TextDark = new Color(0.035f, 0.075f, 0.095f, 1f);
        private static readonly Color AlertRed = new Color(0.92f, 0.18f, 0.14f, 1f);

        private void Awake()
        {
            EnsureEventSystem();

            if (FindObjectOfType<LoginPageController>() != null)
            {
                return;
            }

            resolvedFont = ResolveFont();
            panelSprite = CreateRoundedRectSprite(96, 96, 10);
            fieldSprite = CreateRoundedRectSprite(96, 48, 8);
            pillSprite = CreateRoundedRectSprite(96, 32, 15);
            circleSprite = CreateCircleSprite(72);
            glowSprite = CreateSoftGlowSprite(160);

            BuildLoginScreen();
        }

        private void BuildLoginScreen()
        {
            var canvas = CreateCanvas();
            var root = CreateBackground(canvas.transform);
            CreateStars(root);
            CreateNebulaGlows(root);
            CreateMoonAndOrbit(root);
            CreateDistantCity(root);
            CreateApocalypseForeground(root);
            CreateSystemStatusPanel(root);
            var rotatingTargets = CreateRightHud(root);
            CreateBottomHud(root);

            var panel = CreateMainPanel(root, out var scanLine, out var panelPulseTargets);
            CreateHazardHeader(panel);

            var title = CreateText(panel, "Title", "DON'T DIE PLEASE", 70, FontStyle.Bold, Cyan, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(760f, 96f));
            AddShadow(title.gameObject, new Color(0f, 0f, 0f, 0.5f), new Vector2(2f, -2f));

            var titleGlow = CreateImage(panel, "TitleBackGlow", new Color(0.1f, 0.35f, 0.48f, 0.18f), glowSprite);
            SetRect(titleGlow, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -134f), new Vector2(730f, 150f));
            titleGlow.SetSiblingIndex(3);

            var modeTitle = CreateText(panel, "ModeTitle", "LOGIN", 34, FontStyle.Bold, new Color(0.9f, 1f, 1f, 1f), TextAnchor.MiddleCenter);
            SetRect(modeTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -238f), new Vector2(580f, 50f));
            AddScanLabel(panel, "PLAYER ACCESS", new Vector2(-326f, -238f));
            AddScanLabel(panel, "SECURE LINK", new Vector2(326f, -238f));
            CreateChevronMarks(panel, new Vector2(-132f, -238f), -1f);
            CreateChevronMarks(panel, new Vector2(132f, -238f), 1f);

            CreateAccessDiagnostics(panel);

            var emailInput = CreateInput(panel, "EmailInput", "EMAIL / USERNAME");
            SetRect(emailInput.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -318f), new Vector2(620f, 68f));

            var passwordInput = CreateInput(panel, "PasswordInput", "PASSWORD");
            SetRect(passwordInput.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -406f), new Vector2(620f, 68f));

            var confirmPasswordInput = CreateInput(panel, "ConfirmPasswordInput", "CONFIRM PASSWORD");
            SetRect(confirmPasswordInput.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -494f), new Vector2(620f, 68f));

            var errorText = CreateText(panel, "ErrorText", "", 18, FontStyle.Bold, new Color(1f, 0.42f, 0.36f, 1f), TextAnchor.MiddleCenter);
            SetRect(errorText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 184f), new Vector2(620f, 42f));

            var loadingText = CreateText(panel, "LoadingText", "", 16, FontStyle.Bold, new Color(0.66f, 1f, 1f, 1f), TextAnchor.MiddleCenter);
            SetRect(loadingText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(620f, 30f));

            var submitButton = CreateButton(panel, "SubmitButton", "LOGIN", Cyan, TextDark, 28, true);
            SetRect(submitButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 118f), new Vector2(620f, 74f));
            CreateChevronMarks(submitButton.transform, new Vector2(232f, 0f), 1f);

            var switchModeButton = CreateButton(panel, "SwitchModeButton", "CREATE ACCOUNT", new Color(0.12f, 0.25f, 0.33f, 1f), Cyan, 17, false);
            SetRect(switchModeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(430f, 54f));

            var controller = panel.gameObject.AddComponent<LoginPageController>();
            controller.SetGeneratedReferences(
                title,
                modeTitle,
                emailInput,
                passwordInput,
                confirmPasswordInput,
                submitButton,
                switchModeButton,
                submitButton.GetComponentInChildren<Text>(),
                switchModeButton.GetComponentInChildren<Text>(),
                errorText,
                loadingText);
            controller.SetTargetSceneName(targetSceneName);

            var pulseTargets = new Graphic[panelPulseTargets.Length + 2];
            for (var i = 0; i < panelPulseTargets.Length; i++)
            {
                pulseTargets[i] = panelPulseTargets[i];
            }

            pulseTargets[pulseTargets.Length - 2] = submitButton.GetComponent<Image>();
            pulseTargets[pulseTargets.Length - 1] = titleGlow.GetComponent<Image>();

            var animator = canvas.gameObject.AddComponent<LoginSciFiAnimator>();
            animator.SetTargets(scanLine, rotatingTargets, pulseTargets);
        }

        private Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("LoginCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private RectTransform CreateBackground(Transform parent)
        {
            var root = CreateImage(parent, "RetroSpaceLoginBackground", SpaceTop);
            Stretch(root);

            var lower = CreateImage(root, "SpaceBottom", SpaceBottom);
            SetRect(lower, new Vector2(0f, 0f), new Vector2(1f, 0.47f), Vector2.zero, Vector2.zero);

            var horizon = CreateImage(root, "HorizonGlow", new Color(0.22f, 0.09f, 0.08f, 0.28f), glowSprite);
            SetRect(horizon, new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.32f), Vector2.zero, new Vector2(1600f, 430f));
            return root;
        }

        private void CreateStars(Transform parent)
        {
            var random = new System.Random(2026);
            for (var i = 0; i < 118; i++)
            {
                var star = CreateImage(parent, "Star", Color.white, circleSprite);
                var x = (float)random.NextDouble();
                var y = (float)random.NextDouble();
                var size = 1.6f + (float)random.NextDouble() * 4.6f;
                star.GetComponent<Image>().color = new Color(0.72f, 0.94f, 1f, 0.18f + (float)random.NextDouble() * 0.68f);
                SetRect(star, new Vector2(x, y), new Vector2(x, y), Vector2.zero, new Vector2(size, size));
            }
        }

        private void CreateNebulaGlows(Transform parent)
        {
            var left = CreateImage(parent, "LeftNebulaGlow", new Color(0.1f, 0.32f, 0.48f, 0.13f), glowSprite);
            SetRect(left, new Vector2(0.22f, 0.63f), new Vector2(0.22f, 0.63f), Vector2.zero, new Vector2(680f, 520f));

            var right = CreateImage(parent, "RightNebulaGlow", new Color(0.28f, 0.08f, 0.26f, 0.16f), glowSprite);
            SetRect(right, new Vector2(0.78f, 0.56f), new Vector2(0.78f, 0.56f), Vector2.zero, new Vector2(700f, 560f));

            var red = CreateImage(parent, "RedEmergencyHaze", new Color(0.5f, 0.04f, 0.03f, 0.18f), glowSprite);
            SetRect(red, new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(1250f, 360f));
        }

        private void CreateMoonAndOrbit(Transform parent)
        {
            var moonGlow = CreateImage(parent, "MoonGlow", new Color(0.38f, 0.62f, 0.95f, 0.18f), glowSprite);
            SetRect(moonGlow, new Vector2(0.18f, 0.76f), new Vector2(0.18f, 0.76f), Vector2.zero, new Vector2(440f, 440f));

            var moon = CreateImage(parent, "Moon", new Color(0.16f, 0.24f, 0.36f, 0.78f), circleSprite);
            SetRect(moon, new Vector2(0.18f, 0.76f), new Vector2(0.18f, 0.76f), Vector2.zero, new Vector2(290f, 290f));

            var shadow = CreateImage(parent, "MoonShadow", SpaceTop, circleSprite);
            SetRect(shadow, new Vector2(0.2f, 0.77f), new Vector2(0.2f, 0.77f), Vector2.zero, new Vector2(285f, 285f));

            var horizon = CreateImage(parent, "PlanetHorizon", new Color(0.18f, 0.21f, 0.48f, 0.42f), pillSprite);
            SetRect(horizon, new Vector2(0.33f, 0.43f), new Vector2(0.33f, 0.43f), Vector2.zero, new Vector2(760f, 34f));
            horizon.localRotation = Quaternion.Euler(0f, 0f, -8f);
        }

        private void CreateDistantCity(Transform parent)
        {
            for (var i = 0; i < 22; i++)
            {
                var leftSide = i < 11;
                var x = leftSide ? 290f + i * 52f : 1290f + (i - 11) * 44f;
                var height = 120f + (i % 5) * 34f + (i % 3 == 0 ? 95f : 0f);
                var tower = CreateImage(parent, "DistantTower", new Color(0.012f, 0.021f, 0.034f, 0.98f));
                SetRect(tower, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x, 260f), new Vector2(30f + (i % 4) * 12f, height));

                var glow = CreateImage(parent, "TowerLight", i % 4 == 0 ? AlertRed : BlueGlow, pillSprite);
                SetRect(glow, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x, 260f + height * 0.5f + 12f), new Vector2(8f, 8f));

                for (var w = 0; w < 3; w++)
                {
                    var window = CreateImage(parent, "TowerWindow", i % 4 == 0 ? new Color(0.9f, 0.15f, 0.5f, 0.65f) : new Color(0.15f, 0.82f, 1f, 0.5f), pillSprite);
                    SetRect(window, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x - 8f + w * 8f, 226f + (i % 5) * 24f), new Vector2(5f, 16f));
                }
            }

            var sector = CreateFrame(parent, "SectorSevenSign", new Vector2(1f, 0f), new Vector2(-368f, 335f), new Vector2(150f, 86f), new Color(0.035f, 0.09f, 0.12f, 0.92f));
            CreateTextAt(sector, "SectorText", "SECTOR 7\nCIVIL ACCESS", 20, FontStyle.Bold, Cyan, Vector2.zero, new Vector2(130f, 52f), TextAnchor.MiddleCenter);
        }

        private void CreateApocalypseForeground(Transform parent)
        {
            var ground = CreateImage(parent, "BlackGround", new Color(0.003f, 0.004f, 0.01f, 0.99f));
            SetRect(ground, new Vector2(0f, 0f), new Vector2(1f, 0.16f), Vector2.zero, Vector2.zero);

            for (var i = 0; i < 10; i++)
            {
                var wreck = CreateImage(parent, "WreckageShard", new Color(0.012f, 0.022f, 0.03f, 1f), pillSprite);
                SetRect(wreck, new Vector2(0.14f + i * 0.085f, 0.13f), new Vector2(0.14f + i * 0.085f, 0.13f), new Vector2(0f, 18f + (i % 3) * 9f), new Vector2(86f + (i % 4) * 18f, 12f));
                wreck.localRotation = Quaternion.Euler(0f, 0f, -22f + i * 7f);
            }

            var shipBody = CreateImage(parent, "CrashedShipSilhouette", new Color(0.011f, 0.022f, 0.032f, 1f), pillSprite);
            SetRect(shipBody, new Vector2(0.52f, 0.17f), new Vector2(0.52f, 0.17f), Vector2.zero, new Vector2(420f, 42f));
            shipBody.localRotation = Quaternion.Euler(0f, 0f, -7f);

            var fireGlow = CreateImage(parent, "DistantEmergencyGlow", new Color(0.85f, 0.13f, 0.06f, 0.24f), glowSprite);
            SetRect(fireGlow, new Vector2(0.39f, 0.16f), new Vector2(0.39f, 0.16f), Vector2.zero, new Vector2(360f, 170f));

            var antenna = CreateImage(parent, "BrokenAntenna", new Color(0.012f, 0.022f, 0.03f, 1f), pillSprite);
            SetRect(antenna, new Vector2(0.68f, 0.17f), new Vector2(0.68f, 0.17f), new Vector2(0f, 62f), new Vector2(18f, 190f));
            antenna.localRotation = Quaternion.Euler(0f, 0f, -14f);

            var beacon = CreateImage(parent, "AntennaBeacon", AlertRed, circleSprite);
            SetRect(beacon, new Vector2(0.68f, 0.17f), new Vector2(0.68f, 0.17f), new Vector2(-24f, 156f), new Vector2(16f, 16f));
        }

        private void CreateSystemStatusPanel(Transform parent)
        {
            var panel = CreateFrame(parent, "SystemStatusPanel", new Vector2(0f, 0.5f), new Vector2(132f, 16f), new Vector2(250f, 850f), new Color(0.012f, 0.032f, 0.052f, 0.96f));
            CreateTextAt(panel, "StatusTitle", "SYSTEM STATUS", 17, FontStyle.Bold, Cyan, new Vector2(0f, 377f), new Vector2(196f, 34f), TextAnchor.MiddleLeft);
            CreateStatusBlock(panel, "PLANETARY SCAN", "ACTIVE", new Vector2(0f, 260f), true);
            CreateStatusBlock(panel, "ENVIRONMENT", "STORM FRONT\nWARNING", new Vector2(0f, 58f), false);
            CreateIntegrityBlock(panel, new Vector2(0f, -154f));
            CreateRadarBlock(panel, new Vector2(0f, -330f));
            CreateTextAt(panel, "BuildLabel", "v.17.2b - BUILD 45691", 10, FontStyle.Bold, new Color(0.42f, 0.82f, 0.9f, 0.8f), new Vector2(-20f, -392f), new Vector2(170f, 24f), TextAnchor.MiddleLeft);
        }

        private void CreateStatusBlock(Transform parent, string title, string value, Vector2 position, bool planet)
        {
            var block = CreateFrame(parent, title.Replace(" ", "") + "Block", new Vector2(0.5f, 0.5f), position, new Vector2(208f, 170f), new Color(0.012f, 0.04f, 0.062f, 0.96f));
            CreateTextAt(block, title + "Title", title, 12, FontStyle.Bold, Cyan, new Vector2(-46f, 61f), new Vector2(110f, 24f), TextAnchor.MiddleLeft);
            CreateTextAt(block, title + "Value", value, 11, FontStyle.Bold, planet ? new Color(0.78f, 1f, 1f, 1f) : new Color(1f, 0.32f, 0.32f, 1f), new Vector2(56f, 52f), new Vector2(82f, 42f), TextAnchor.MiddleRight);

            if (planet)
            {
                var orbit = CreateImage(block, "ScanOrbit", new Color(0.1f, 0.45f, 0.65f, 0.75f), circleSprite);
                SetRect(orbit, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(118f, 118f));
                CreatePlanetIcon(block, "SmallScanPlanet", Vector2.zero, 68f, new Color(0.12f, 0.48f, 0.78f, 1f), new Color(0.15f, 0.82f, 0.65f, 1f));
            }
            else
            {
                var storm = CreateImage(block, "StormGlow", new Color(0.28f, 0.46f, 0.74f, 0.32f), glowSprite);
                SetRect(storm, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(170f, 110f));
                for (var i = 0; i < 8; i++)
                {
                    var ridge = CreateImage(block, "StormRidge", new Color(0.025f, 0.04f, 0.055f, 1f), pillSprite);
                    SetRect(ridge, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-72f + i * 20f, -67f + (i % 4) * 10f), new Vector2(58f, 8f));
                    ridge.localRotation = Quaternion.Euler(0f, 0f, -10f + i * 4f);
                }
            }
        }

        private void CreateIntegrityBlock(Transform parent, Vector2 position)
        {
            var block = CreateFrame(parent, "BaseIntegrityBlock", new Vector2(0.5f, 0.5f), position, new Vector2(208f, 150f), new Color(0.012f, 0.04f, 0.062f, 0.96f));
            CreateTextAt(block, "IntegrityTitle", "BASE INTEGRITY", 12, FontStyle.Bold, Cyan, new Vector2(-42f, 50f), new Vector2(120f, 24f), TextAnchor.MiddleLeft);
            CreateTextAt(block, "IntegrityValue", "87%", 12, FontStyle.Bold, new Color(0.72f, 1f, 1f, 1f), new Vector2(70f, 50f), new Vector2(58f, 24f), TextAnchor.MiddleRight);

            for (var i = 0; i < 12; i++)
            {
                var h = 22f + (i % 5) * 8f + (i > 7 ? 20f : 0f);
                var bar = CreateImage(block, "IntegrityBar", i > 8 ? BlueGlow : CyanDim, pillSprite);
                SetRect(bar, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28f + i * 14f, 28f), new Vector2(8f, h));
            }
        }

        private void CreateRadarBlock(Transform parent, Vector2 position)
        {
            var block = CreateFrame(parent, "ThreatRadarBlock", new Vector2(0.5f, 0.5f), position, new Vector2(208f, 165f), new Color(0.012f, 0.04f, 0.062f, 0.96f));
            CreateTextAt(block, "ThreatTitle", "EXTERNAL THREATS", 12, FontStyle.Bold, Cyan, new Vector2(-34f, 58f), new Vector2(136f, 24f), TextAnchor.MiddleLeft);
            CreateTextAt(block, "ThreatValue", "LOW", 12, FontStyle.Bold, new Color(0.6f, 1f, 1f, 1f), new Vector2(-72f, 36f), new Vector2(60f, 20f), TextAnchor.MiddleLeft);
            var radar = CreateImage(block, "RadarCircle", new Color(0.08f, 0.32f, 0.48f, 0.62f), circleSprite);
            SetRect(radar, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(112f, 112f));
            var dot = CreateImage(block, "RadarDot", Cyan, circleSprite);
            SetRect(dot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(22f, -8f), new Vector2(8f, 8f));
            var sweep = CreateImage(block, "RadarSweep", BlueGlow, pillSprite);
            SetRect(sweep, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(22f, -22f), new Vector2(62f, 4f));
            sweep.localRotation = Quaternion.Euler(0f, 0f, 32f);
        }

        private RectTransform[] CreateRightHud(Transform parent)
        {
            var hud = CreateFrame(parent, "RightIntelPanel", new Vector2(1f, 0.5f), new Vector2(-132f, 16f), new Vector2(250f, 850f), new Color(0.012f, 0.032f, 0.052f, 0.96f));
            CreateTextAt(hud, "ProfileSyncTitle", "PROFILE SYNC", 17, FontStyle.Bold, Cyan, new Vector2(0f, 377f), new Vector2(196f, 34f), TextAnchor.MiddleLeft);

            var profile = CreateFrame(hud, "ProfileCard", new Vector2(0.5f, 0.5f), new Vector2(0f, 262f), new Vector2(208f, 182f), new Color(0.012f, 0.04f, 0.062f, 0.96f));
            var head = CreateProfileWireframe(profile, new Vector2(-42f, 8f));
            CreateTextAt(profile, "LastSync", "LAST SYNC\n2h 41m AGO", 10, FontStyle.Bold, new Color(0.52f, 0.88f, 0.95f, 1f), new Vector2(54f, 18f), new Vector2(76f, 46f), TextAnchor.MiddleLeft);
            var syncButton = CreateButton(profile, "SyncProfileButton", "SYNC PROFILE", new Color(0.02f, 0.13f, 0.18f, 1f), Cyan, 11, false);
            SetRect(syncButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(158f, 34f));

            var lore = CreateFrame(hud, "LoreLogCard", new Vector2(0.5f, 0.5f), new Vector2(0f, 36f), new Vector2(208f, 250f), new Color(0.012f, 0.04f, 0.062f, 0.96f));
            CreateTextAt(lore, "LoreTitle", "LORE LOG", 16, FontStyle.Bold, Cyan, new Vector2(-44f, 100f), new Vector2(110f, 28f), TextAnchor.MiddleLeft);
            CreateTextAt(lore, "LoreEntry", "ENTRY 042\nOUTER RIM COLONY", 11, FontStyle.Bold, new Color(0.48f, 0.86f, 0.94f, 1f), new Vector2(-25f, 57f), new Vector2(142f, 46f), TextAnchor.MiddleLeft);
            var storm = CreateImage(lore, "LoreStorm", new Color(0.24f, 0.38f, 0.7f, 0.3f), glowSprite);
            SetRect(storm, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -15f), new Vector2(158f, 88f));
            for (var i = 0; i < 7; i++)
            {
                var ridge = CreateImage(lore, "LoreRidge", new Color(0.018f, 0.032f, 0.052f, 1f), pillSprite);
                SetRect(ridge, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-68f + i * 22f, -58f + (i % 3) * 11f), new Vector2(62f, 8f));
                ridge.localRotation = Quaternion.Euler(0f, 0f, -9f + i * 5f);
            }
            CreateTextAt(lore, "LoreBody", "The storm is approaching.\nShelter. Survive. Repeat.", 10, FontStyle.Bold, new Color(0.52f, 0.86f, 0.9f, 1f), new Vector2(-4f, -96f), new Vector2(164f, 44f), TextAnchor.MiddleLeft);

            var terminal = CreateFrame(hud, "TerminalCard", new Vector2(0.5f, 0.5f), new Vector2(0f, -256f), new Vector2(208f, 245f), new Color(0.012f, 0.04f, 0.062f, 0.96f));
            CreateTextAt(terminal, "TerminalTitle", "TERMINAL", 16, FontStyle.Bold, Cyan, new Vector2(-48f, 94f), new Vector2(110f, 28f), TextAnchor.MiddleLeft);
            var orbitA = CreateTerminalSeal(terminal, new Vector2(0f, 18f));
            CreateTextAt(terminal, "TerminalHint", "SCAN DATA\nAFTER LOGIN", 13, FontStyle.Bold, Cyan, new Vector2(0f, -74f), new Vector2(142f, 46f), TextAnchor.MiddleCenter);
            CreateChevronMarks(terminal, new Vector2(-76f, -78f), -1f);
            CreateChevronMarks(terminal, new Vector2(76f, -78f), 1f);

            CreateBottomIconButton(parent, new Vector2(1f, 0f), new Vector2(-202f, 54f), "SET");
            CreateBottomIconButton(parent, new Vector2(1f, 0f), new Vector2(-132f, 54f), "?");
            CreateBottomIconButton(parent, new Vector2(1f, 0f), new Vector2(-62f, 54f), "PWR");

            return new[] { orbitA, head };
        }

        private RectTransform CreatePlanetIcon(Transform parent, string name, Vector2 position, float size, Color ocean, Color land)
        {
            var planet = CreateImage(parent, name, ocean, circleSprite);
            SetRect(planet, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(size, size));

            var landA = CreateImage(planet, "LandA", land, pillSprite);
            SetRect(landA, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-size * 0.11f, size * 0.12f), new Vector2(size * 0.42f, size * 0.15f));
            landA.localRotation = Quaternion.Euler(0f, 0f, -14f);

            var landB = CreateImage(planet, "LandB", land, pillSprite);
            SetRect(landB, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(size * 0.12f, -size * 0.13f), new Vector2(size * 0.34f, size * 0.13f));
            landB.localRotation = Quaternion.Euler(0f, 0f, 18f);
            return planet;
        }

        private RectTransform CreateShipIcon(Transform parent, Vector2 position, float scale)
        {
            var shipRoot = new GameObject("ShipIcon", typeof(RectTransform)).GetComponent<RectTransform>();
            shipRoot.SetParent(parent, false);
            SetRect(shipRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(66f * scale, 42f * scale));

            var body = CreateImage(shipRoot, "ShipBody", new Color(0.66f, 0.9f, 0.98f, 1f), pillSprite);
            SetRect(body, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48f * scale, 16f * scale));
            var nose = CreateImage(shipRoot, "ShipNose", BlueGlow, circleSprite);
            SetRect(nose, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-2f * scale, 0f), new Vector2(20f * scale, 20f * scale));
            var wingA = CreateImage(shipRoot, "ShipWingA", CyanDim, pillSprite);
            SetRect(wingA, new Vector2(0.4f, 0.5f), new Vector2(0.4f, 0.5f), new Vector2(-4f * scale, 12f * scale), new Vector2(30f * scale, 8f * scale));
            wingA.localRotation = Quaternion.Euler(0f, 0f, 18f);
            var wingB = CreateImage(shipRoot, "ShipWingB", CyanDim, pillSprite);
            SetRect(wingB, new Vector2(0.4f, 0.5f), new Vector2(0.4f, 0.5f), new Vector2(-4f * scale, -12f * scale), new Vector2(30f * scale, 8f * scale));
            wingB.localRotation = Quaternion.Euler(0f, 0f, -18f);
            return shipRoot;
        }

        private void CreateSpecimenCard(Transform parent, string title, string subtitle, Vector2 position, Color iconColor, bool alien)
        {
            var card = CreateFrame(parent, title + "Card", new Vector2(0.5f, 0.5f), position, new Vector2(235f, 92f), new Color(0.045f, 0.1f, 0.135f, 1f));
            var icon = alien ? CreateAlienIcon(card, new Vector2(-78f, 0f), iconColor) : CreateRobotIcon(card, new Vector2(-78f, 0f), iconColor);
            icon.localScale = Vector3.one;
            CreateTextAt(card, title + "Title", title, 14, FontStyle.Bold, Cyan, new Vector2(38f, 16f), new Vector2(130f, 26f), TextAnchor.MiddleLeft);
            CreateTextAt(card, title + "Subtitle", subtitle, 10, FontStyle.Bold, new Color(0.55f, 0.82f, 0.88f, 1f), new Vector2(38f, -13f), new Vector2(130f, 22f), TextAnchor.MiddleLeft);
        }

        private RectTransform CreateAlienIcon(Transform parent, Vector2 position, Color color)
        {
            var head = CreateImage(parent, "AlienHead", color, circleSprite);
            SetRect(head, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(54f, 46f));
            var eyeA = CreateImage(head, "EyeA", new Color(0.02f, 0.06f, 0.07f, 1f), circleSprite);
            SetRect(eyeA, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-10f, 5f), new Vector2(10f, 14f));
            var eyeB = CreateImage(head, "EyeB", new Color(0.02f, 0.06f, 0.07f, 1f), circleSprite);
            SetRect(eyeB, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(10f, 5f), new Vector2(10f, 14f));
            var antennaA = CreateImage(parent, "AntennaA", color, pillSprite);
            SetRect(antennaA, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position + new Vector2(-15f, 27f), new Vector2(6f, 25f));
            antennaA.localRotation = Quaternion.Euler(0f, 0f, -22f);
            var antennaB = CreateImage(parent, "AntennaB", color, pillSprite);
            SetRect(antennaB, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position + new Vector2(15f, 27f), new Vector2(6f, 25f));
            antennaB.localRotation = Quaternion.Euler(0f, 0f, 22f);
            return head;
        }

        private RectTransform CreateRobotIcon(Transform parent, Vector2 position, Color color)
        {
            var head = CreateImage(parent, "RobotHead", color, fieldSprite);
            SetRect(head, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(52f, 44f));
            head.GetComponent<Image>().type = Image.Type.Sliced;
            var eyeA = CreateImage(head, "RobotEyeA", Lime, circleSprite);
            SetRect(eyeA, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-11f, 4f), new Vector2(9f, 9f));
            var eyeB = CreateImage(head, "RobotEyeB", Lime, circleSprite);
            SetRect(eyeB, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(11f, 4f), new Vector2(9f, 9f));
            var mouth = CreateImage(head, "RobotMouth", new Color(0.06f, 0.09f, 0.1f, 1f), pillSprite);
            SetRect(mouth, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(24f, 5f));
            return head;
        }

        private void CreateWorldButton(Transform parent, Vector2 position, string label, int iconType)
        {
            var button = new GameObject(label + "WorldButton", typeof(Image), typeof(Button));
            button.transform.SetParent(parent, false);
            SetRect(button.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(62f, 58f));
            var image = button.GetComponent<Image>();
            image.sprite = fieldSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.08f, 0.18f, 0.25f, 1f);
            AddButtonFx(button, new Color(0.18f, 0.4f, 0.5f, 1f));
            AddShadow(button, new Color(0f, 0f, 0f, 0.66f), new Vector2(4f, -4f));

            if (iconType == 0)
            {
                CreateShipIcon(button.transform, new Vector2(0f, 8f), 0.58f);
            }
            else if (iconType == 1)
            {
                CreatePlanetIcon(button.transform, "ButtonPlanet", new Vector2(0f, 9f), 28f, new Color(0.24f, 0.63f, 0.95f, 1f), new Color(0.42f, 0.92f, 0.52f, 1f));
            }
            else
            {
                var gem = CreateImage(button.transform, "LogGem", Lime, circleSprite);
                SetRect(gem, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 9f), new Vector2(22f, 22f));
            }

            var text = CreateText(button.transform, "WorldButtonText", label, 9, FontStyle.Bold, Cyan, TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 10f), new Vector2(0f, 16f));
        }

        private RectTransform CreateProfileWireframe(Transform parent, Vector2 position)
        {
            var root = new GameObject("ProfileWireframe", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(parent, false);
            SetRect(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(84f, 112f));

            var head = CreateImage(root, "WireHead", new Color(0.08f, 0.42f, 0.58f, 0.72f), circleSprite);
            SetRect(head, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(58f, 66f));

            var visor = CreateImage(root, "WireVisor", Cyan, pillSprite);
            SetRect(visor, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(38f, 5f));

            var neck = CreateImage(root, "WireNeck", new Color(0.08f, 0.42f, 0.58f, 0.72f), pillSprite);
            SetRect(neck, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(24f, 38f));

            var shoulder = CreateImage(root, "WireShoulder", new Color(0.08f, 0.42f, 0.58f, 0.72f), pillSprite);
            SetRect(shoulder, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 17f), new Vector2(78f, 18f));
            return root;
        }

        private RectTransform CreateTerminalSeal(Transform parent, Vector2 position)
        {
            var seal = CreateImage(parent, "TerminalSeal", new Color(0.08f, 0.38f, 0.5f, 0.72f), circleSprite);
            SetRect(seal, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(96f, 96f));

            var core = CreateImage(seal, "SealCore", new Color(0.02f, 0.08f, 0.1f, 1f), circleSprite);
            SetRect(core, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(58f, 58f));

            for (var i = 0; i < 8; i++)
            {
                var tick = CreateImage(seal, "SealTick", Cyan, pillSprite);
                SetRect(tick, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5f, 44f));
                tick.localRotation = Quaternion.Euler(0f, 0f, i * 45f);
            }

            CreateTextAt(seal, "SealStar", "*", 28, FontStyle.Bold, Cyan, Vector2.zero, new Vector2(44f, 44f), TextAnchor.MiddleCenter);
            return seal;
        }

        private void CreateBottomIconButton(Transform parent, Vector2 anchor, Vector2 position, string label)
        {
            var button = new GameObject(label + "BottomIconButton", typeof(Image), typeof(Button));
            button.transform.SetParent(parent, false);
            SetRect(button.GetComponent<RectTransform>(), anchor, anchor, position, new Vector2(54f, 54f));
            var image = button.GetComponent<Image>();
            image.sprite = fieldSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.018f, 0.07f, 0.1f, 0.98f);
            AddShadow(button, new Color(0f, 0f, 0f, 0.72f), new Vector2(4f, -4f));
            AddButtonFx(button, new Color(0.08f, 0.22f, 0.3f, 1f));

            var text = CreateText(button.transform, "IconText", label, label.Length > 1 ? 12 : 22, FontStyle.Bold, Cyan, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
        }

        private void CreateChevronMarks(Transform parent, Vector2 position, float direction)
        {
            for (var i = 0; i < 3; i++)
            {
                var upper = CreateImage(parent, "ChevronUpper", BlueGlow, pillSprite);
                SetRect(upper, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position + new Vector2(i * 12f * direction, 5f), new Vector2(16f, 4f));
                upper.localRotation = Quaternion.Euler(0f, 0f, 35f * direction);

                var lower = CreateImage(parent, "ChevronLower", BlueGlow, pillSprite);
                SetRect(lower, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position + new Vector2(i * 12f * direction, -5f), new Vector2(16f, 4f));
                lower.localRotation = Quaternion.Euler(0f, 0f, -35f * direction);
            }
        }

        private void CreateAccessDiagnostics(Transform parent)
        {
            var diag = CreateFrame(parent, "AccessDiagnostics", new Vector2(0.5f, 1f), new Vector2(0f, -536f), new Vector2(600f, 76f), new Color(0.028f, 0.05f, 0.063f, 0.96f));
            var redLine = CreateImage(diag, "EmergencyLine", AlertRed, pillSprite);
            SetRect(redLine, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(510f, 5f));
            CreateTextAt(diag, "DiagTitle", "SIGNAL DEGRADED  /  SURFACE CONTACT LOST", 14, FontStyle.Bold, new Color(1f, 0.55f, 0.48f, 1f), new Vector2(0f, 14f), new Vector2(520f, 26f), TextAnchor.MiddleCenter);
            CreateTextAt(diag, "DiagBody", "Recover local profile and sync emergency shelter access.", 13, FontStyle.Bold, new Color(0.6f, 0.86f, 0.9f, 1f), new Vector2(0f, -17f), new Vector2(520f, 24f), TextAnchor.MiddleCenter);
        }

        private void CreateHazardHeader(Transform parent)
        {
            var left = CreateImage(parent, "HeaderLeftWarning", AlertRed, pillSprite);
            SetRect(left, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-250f, -56f), new Vector2(150f, 8f));

            var right = CreateImage(parent, "HeaderRightWarning", AlertRed, pillSprite);
            SetRect(right, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(250f, -56f), new Vector2(150f, 8f));

            for (var i = 0; i < 12; i++)
            {
                var seg = CreateImage(parent, "HazardTick", i % 2 == 0 ? AlertRed : Lime, pillSprite);
                SetRect(seg, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-154f + i * 28f, -60f), new Vector2(16f, 5f));
            }
        }

        private void CreateBottomHud(Transform parent)
        {
            var strip = CreateFrame(parent, "BottomStatusStrip", new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(640f, 116f), new Color(0.012f, 0.034f, 0.052f, 0.98f));
            CreateTextAt(strip, "SyncLabel", "SYNC PROFILE", 12, FontStyle.Bold, Cyan, new Vector2(-236f, 31f), new Vector2(118f, 22f), TextAnchor.MiddleLeft);
            CreateTextAt(strip, "PercentLabel", "72%", 12, FontStyle.Bold, new Color(0.7f, 1f, 1f, 1f), new Vector2(230f, 31f), new Vector2(54f, 22f), TextAnchor.MiddleRight);

            for (var i = 0; i < 29; i++)
            {
                var color = i < 19 ? BlueGlow : new Color(0.08f, 0.13f, 0.15f, 1f);
                var seg = CreateImage(strip, "StatusSegment", color, pillSprite);
                SetRect(seg, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(162f + i * 11f, 32f), new Vector2(7f, 28f));
                seg.localRotation = Quaternion.Euler(0f, 0f, -10f);
            }

            CreateTextAt(strip, "SignalLabel", "SIGNAL LINK", 10, FontStyle.Bold, new Color(0.52f, 0.86f, 0.9f, 1f), new Vector2(-220f, -28f), new Vector2(110f, 20f), TextAnchor.MiddleLeft);

            for (var i = 0; i < 17; i++)
            {
                var dot = CreateImage(strip, "SignalDot", i < 10 ? Cyan : new Color(0.08f, 0.22f, 0.28f, 1f), circleSprite);
                SetRect(dot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-90f + i * 18f, -28f), new Vector2(i == 8 ? 9f : 5f, i == 8 ? 9f : 5f));
            }
        }

        private RectTransform CreateMainPanel(Transform parent, out RectTransform scanLine, out Graphic[] pulseTargets)
        {
            var shadow = CreateImage(parent, "MainPanelShadow", new Color(0f, 0f, 0f, 0.66f), panelSprite);
            SetRect(shadow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(18f, -18f), new Vector2(840f, 820f));
            shadow.GetComponent<Image>().type = Image.Type.Sliced;

            var outer = CreateImage(parent, "LoginPanelOuter", MetalDark, panelSprite);
            SetRect(outer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(840f, 820f));
            outer.GetComponent<Image>().type = Image.Type.Sliced;

            var rim = CreateImage(outer, "LoginPanelRim", MetalLight, panelSprite);
            StretchWithMargin(rim, 16f);
            rim.GetComponent<Image>().type = Image.Type.Sliced;

            var inner = CreateImage(outer, "LoginPanelInner", MetalMid, panelSprite);
            StretchWithMargin(inner, 34f);
            inner.GetComponent<Image>().type = Image.Type.Sliced;

            var inset = CreateImage(outer, "LoginPanelInset", new Color(0.11f, 0.18f, 0.22f, 0.88f), panelSprite);
            SetRect(inset, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -62f), new Vector2(700f, 580f));
            inset.GetComponent<Image>().type = Image.Type.Sliced;

            scanLine = CreateImage(inset, "PanelScanLine", new Color(0.52f, 0.96f, 1f, 0.12f));
            SetRect(scanLine, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(0f, 5f));

            CreateArmorPlate(outer, new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(360f, 50f));
            CreateArmorPlate(outer, new Vector2(0.5f, 0f), new Vector2(0f, 15f), new Vector2(360f, 50f));
            CreateArmorPlate(outer, new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(52f, 360f));
            CreateArmorPlate(outer, new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(52f, 360f));

            CreatePanelCorner(outer, new Vector2(0f, 1f), new Vector2(48f, -48f), 1f, 1f);
            CreatePanelCorner(outer, new Vector2(1f, 1f), new Vector2(-48f, -48f), -1f, 1f);
            CreatePanelCorner(outer, new Vector2(0f, 0f), new Vector2(48f, 48f), 1f, -1f);
            CreatePanelCorner(outer, new Vector2(1f, 0f), new Vector2(-48f, 48f), -1f, -1f);

            var leftLight = CreateGlowSlot(outer, new Vector2(0f, 0.5f), new Vector2(36f, 0f), new Vector2(22f, 360f));
            var rightLight = CreateGlowSlot(outer, new Vector2(1f, 0.5f), new Vector2(-36f, 0f), new Vector2(22f, 360f));
            var topLight = CreateGlowSlot(outer, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(180f, 20f));
            var bottomLight = CreateGlowSlot(outer, new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(180f, 20f));

            for (var i = 0; i < 8; i++)
            {
                var bolt = CreateImage(outer, "PanelBolt", new Color(0.035f, 0.07f, 0.09f, 1f), circleSprite);
                var x = i < 4 ? -360f + i * 240f : -360f + (i - 4) * 240f;
                var y = i < 4 ? 362f : -362f;
                SetRect(bolt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(18f, 18f));
            }

            pulseTargets = new Graphic[]
            {
                leftLight.GetComponent<Image>(),
                rightLight.GetComponent<Image>(),
                topLight.GetComponent<Image>(),
                bottomLight.GetComponent<Image>(),
                scanLine.GetComponent<Image>()
            };
            return outer;
        }

        private RectTransform CreateGlowSlot(Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var back = CreateImage(parent, "GlowSlotBack", new Color(0.035f, 0.08f, 0.12f, 1f), fieldSprite);
            SetRect(back, anchor, anchor, position, size + new Vector2(10f, 10f));
            back.GetComponent<Image>().type = Image.Type.Sliced;

            var light = CreateImage(parent, "GlowSlot", BlueGlow, fieldSprite);
            SetRect(light, anchor, anchor, position, size);
            light.GetComponent<Image>().type = Image.Type.Sliced;
            return light;
        }

        private void CreateArmorPlate(Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var plate = CreateImage(parent, "ArmorPlate", new Color(0.045f, 0.065f, 0.082f, 1f), fieldSprite);
            SetRect(plate, anchor, anchor, position, size);
            plate.GetComponent<Image>().type = Image.Type.Sliced;
            AddShadow(plate.gameObject, new Color(0f, 0f, 0f, 0.65f), new Vector2(3f, -3f));

            var rim = CreateImage(plate, "ArmorPlateRim", new Color(0.13f, 0.2f, 0.24f, 1f), fieldSprite);
            StretchWithMargin(rim, 7f);
            rim.GetComponent<Image>().type = Image.Type.Sliced;

            var line = CreateImage(plate, "ArmorPlateGlow", BlueGlow, pillSprite);
            SetRect(line, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Mathf.Max(14f, size.x - 80f), 5f));
        }

        private void CreatePanelCorner(Transform parent, Vector2 anchor, Vector2 position, float xSign, float ySign)
        {
            var horizontal = CreateImage(parent, "CornerHorizontal", CyanDim, pillSprite);
            SetRect(horizontal, anchor, anchor, position + new Vector2(28f * xSign, 0f), new Vector2(70f, 10f));

            var vertical = CreateImage(parent, "CornerVertical", CyanDim, pillSprite);
            SetRect(vertical, anchor, anchor, position + new Vector2(0f, 28f * ySign), new Vector2(10f, 70f));
        }

        private void AddScanLabel(Transform parent, string label, Vector2 position)
        {
            var text = CreateText(parent, label.Replace(" ", "") + "Label", label, 10, FontStyle.Bold, new Color(0.42f, 0.78f, 0.9f, 0.72f), TextAnchor.MiddleCenter);
            SetRect(text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), position, new Vector2(140f, 22f));
        }

        private RectTransform CreateFrame(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var frame = CreateImage(parent, name, color, panelSprite);
            SetRect(frame, anchor, anchor, position, size);
            frame.GetComponent<Image>().type = Image.Type.Sliced;
            AddShadow(frame.gameObject, new Color(0f, 0f, 0f, 0.72f), new Vector2(6f, -6f));

            var rim = CreateImage(frame, "FrameRim", new Color(0.18f, 0.39f, 0.48f, 1f), panelSprite);
            StretchWithMargin(rim, 8f);
            rim.GetComponent<Image>().type = Image.Type.Sliced;

            var inner = CreateImage(frame, "FrameInner", color, panelSprite);
            StretchWithMargin(inner, 15f);
            inner.GetComponent<Image>().type = Image.Type.Sliced;
            return frame;
        }

        private void CreateRailCap(Transform parent, Vector2 anchor, Vector2 position, float width)
        {
            var cap = CreateImage(parent, "RailCap", new Color(0.17f, 0.3f, 0.36f, 1f), fieldSprite);
            SetRect(cap, anchor, anchor, position, new Vector2(width, 24f));
            cap.GetComponent<Image>().type = Image.Type.Sliced;
        }

        private InputField CreateInput(Transform parent, string name, string placeholder)
        {
            var inputObject = new GameObject(name, typeof(Image), typeof(InputField));
            inputObject.transform.SetParent(parent, false);
            var image = inputObject.GetComponent<Image>();
            image.sprite = fieldSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.07f, 0.15f, 0.2f, 1f);
            AddShadow(inputObject, new Color(0f, 0f, 0f, 0.62f), new Vector2(4f, -4f));

            var rail = CreateImage(inputObject.transform, "InputAccent", CyanDim, pillSprite);
            SetRect(rail, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(8f, 40f));

            CreateInputGlyph(inputObject.transform, name.Contains("Password"));

            var topLine = CreateImage(inputObject.transform, "InputTopLine", new Color(0.82f, 1f, 1f, 0.22f), pillSprite);
            SetRect(topLine, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(8f, -9f), new Vector2(430f, 4f));

            var text = CreateText(inputObject.transform, "Text", "", 24, FontStyle.Bold, Cyan, TextAnchor.MiddleLeft);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(76f, 0f);
            text.rectTransform.offsetMax = new Vector2(name.Contains("Password") ? -74f : -24f, 0f);

            var placeholderText = CreateText(inputObject.transform, "Placeholder", placeholder, 21, FontStyle.Bold, new Color(0.48f, 0.78f, 0.86f, 0.74f), TextAnchor.MiddleLeft);
            placeholderText.rectTransform.anchorMin = Vector2.zero;
            placeholderText.rectTransform.anchorMax = Vector2.one;
            placeholderText.rectTransform.offsetMin = new Vector2(76f, 0f);
            placeholderText.rectTransform.offsetMax = new Vector2(name.Contains("Password") ? -74f : -24f, 0f);

            if (name.Contains("Password"))
            {
                CreateEyeGlyph(inputObject.transform);
            }

            var input = inputObject.GetComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholderText;
            input.selectionColor = new Color(0.64f, 0.96f, 1f, 0.35f);
            input.caretWidth = 3;
            return input;
        }

        private void CreateInputGlyph(Transform parent, bool lockGlyph)
        {
            if (lockGlyph)
            {
                var body = CreateImage(parent, "LockBody", CyanDim, fieldSprite);
                SetRect(body, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(42f, -2f), new Vector2(22f, 22f));
                body.GetComponent<Image>().type = Image.Type.Sliced;
                var shackle = CreateImage(parent, "LockShackle", CyanDim, circleSprite);
                SetRect(shackle, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(42f, 12f), new Vector2(24f, 22f));
                var cut = CreateImage(parent, "LockCut", new Color(0.07f, 0.15f, 0.2f, 1f), circleSprite);
                SetRect(cut, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(42f, 8f), new Vector2(14f, 14f));
                return;
            }

            var head = CreateImage(parent, "UserHead", CyanDim, circleSprite);
            SetRect(head, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(42f, 10f), new Vector2(22f, 22f));
            var bodyGlyph = CreateImage(parent, "UserBody", CyanDim, pillSprite);
            SetRect(bodyGlyph, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(42f, -12f), new Vector2(32f, 18f));
        }

        private void CreateEyeGlyph(Transform parent)
        {
            var eye = CreateImage(parent, "EyeGlyph", CyanDim, pillSprite);
            SetRect(eye, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-38f, 0f), new Vector2(34f, 12f));
            var pupil = CreateImage(parent, "EyePupil", BlueGlow, circleSprite);
            SetRect(pupil, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-38f, 0f), new Vector2(10f, 10f));
            var slash = CreateImage(parent, "EyeSlash", CyanDim, pillSprite);
            SetRect(slash, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-38f, 0f), new Vector2(42f, 4f));
            slash.localRotation = Quaternion.Euler(0f, 0f, -32f);
        }

        private Button CreateButton(Transform parent, string name, string label, Color color, Color textColor, int fontSize, bool primary)
        {
            var buttonObject = new GameObject(name, typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.GetComponent<Image>();
            image.sprite = fieldSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            AddShadow(buttonObject, new Color(0f, 0f, 0f, 0.68f), new Vector2(5f, -5f));
            AddButtonFx(buttonObject, primary ? new Color(0.78f, 1f, 1f, 1f) : new Color(0.18f, 0.39f, 0.48f, 1f));

            if (primary)
            {
                var top = CreateImage(buttonObject.transform, "ButtonTopLine", new Color(1f, 1f, 1f, 0.38f), pillSprite);
                SetRect(top, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -9f), new Vector2(500f, 6f));
            }

            var text = CreateText(buttonObject.transform, "Text", label, fontSize, FontStyle.Bold, textColor, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            AddShadow(text.gameObject, primary ? new Color(1f, 1f, 1f, 0.18f) : new Color(0f, 0f, 0f, 0.4f), new Vector2(1f, -1f));

            var button = buttonObject.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = MultiplyColor(color, 1.12f);
            colors.pressedColor = MultiplyColor(color, 0.82f);
            colors.disabledColor = new Color(0.18f, 0.22f, 0.25f, 0.8f);
            button.colors = colors;
            return button;
        }

        private void AddButtonFx(GameObject target, Color hoverColor)
        {
            var fx = target.AddComponent<LoginButtonFx>();
            fx.Configure(hoverColor);
        }

        private Text CreateTextAt(Transform parent, string name, string value, int size, FontStyle style, Color color, Vector2 position, Vector2 rectSize, TextAnchor alignment)
        {
            var text = CreateText(parent, name, value, size, style, color, alignment);
            SetRect(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, rectSize);
            return text;
        }

        private Text CreateText(Transform parent, string name, string value, int size, FontStyle style, Color color, TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.text = value;
            if (resolvedFont != null)
            {
                text.font = resolvedFont;
            }

            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.lineSpacing = 0.95f;
            text.alignByGeometry = true;
            return text;
        }

        private RectTransform CreateImage(Transform parent, string name, Color color, Sprite sprite = null)
        {
            var imageObject = new GameObject(name, typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.raycastTarget = false;
            return imageObject.GetComponent<RectTransform>();
        }

        private Font ResolveFont()
        {
            if (uiFont != null)
            {
                return uiFont;
            }

            var osFont = Font.CreateDynamicFontFromOSFont(new[] { "Bahnschrift", "Segoe UI", "Arial", "Verdana", "Arial Black", "Segoe UI Black", "Bahnschrift SemiBold", "Segoe UI Semibold", "Agency FB", "Orbitron", "Oxanium", "Audiowide" }, 28);
            if (osFont != null)
            {
                return osFont;
            }

            try
            {
                return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch (System.ArgumentException exception)
            {
                Debug.LogWarning($"Built-in Unity font lookup failed: {exception.Message}");
                return null;
            }
        }

        private static void AddShadow(GameObject target, Color color, Vector2 distance)
        {
            var shadow = target.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void StretchWithMargin(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
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
            texture.name = "RuntimeCircleSprite";
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

        private static Sprite CreateSoftGlowSprite(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.name = "RuntimeSoftGlowSprite";
            var center = (size - 1) * 0.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    var normalized = Mathf.Clamp01(distance / center);
                    var alpha = Mathf.Pow(1f - normalized, 2.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Sprite CreateRoundedRectSprite(int width, int height, int radius)
        {
            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            texture.name = "RuntimeRoundedRectSprite";

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
