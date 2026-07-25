using System.Threading.Tasks;
using DontDiePlease.Networking;
using DontDiePlease.Narrative.Persistence;
using DontDiePlease.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DontDiePlease.Narrative.UI
{
    public sealed class NarrativeMainMenuController : MonoBehaviour
    {
        private const string MainGameplaySceneName = "MainGameplayScene";
        private static readonly Color Background = new Color(0.018f, 0.027f, 0.032f, 1f);
        private static readonly Color Primary = new Color(0.18f, 0.78f, 0.82f, 1f);
        private static readonly Color Secondary = new Color(0.11f, 0.17f, 0.19f, 1f);
        private static readonly Color TextColor = new Color(0.9f, 0.95f, 0.96f, 1f);
        private NarrativeSaveAdapter saveAdapter;
        private SaveProfileService saveService;
        private GameSeedManager seedManager;
        private Button newGameButton;
        private Button continueButton;
        private TextMeshProUGUI statusText;
        private bool busy;

        public void Configure(
            NarrativeSaveAdapter persistence,
            SaveProfileService profileService,
            GameSeedManager gameSeedManager)
        {
            saveAdapter = persistence;
            saveService = profileService;
            seedManager = gameSeedManager;
            BuildInterface();
        }

        private void OnDestroy()
        {
            if (newGameButton != null)
            {
                newGameButton.onClick.RemoveListener(StartNewGame);
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(ContinueGame);
            }
        }

        private async void StartNewGame()
        {
            if (busy)
            {
                return;
            }

            SetBusy(true, "INITIALISING NEW SURVIVAL RUN");
            var seed = seedManager.InitialiseRun();
            var result = await saveAdapter.ResetForNewGame(seed);

            if (!result.RemoteSynced && !string.IsNullOrWhiteSpace(result.Error))
            {
                SetStatus("CLOUD UNAVAILABLE. STARTING WITH LOCAL SAVE");
                await Task.Delay(650);
            }

            LoadGameplayScene();
        }

        private async void ContinueGame()
        {
            if (busy)
            {
                return;
            }

            SetBusy(true, "RESTORING SURVIVAL RUN");
            var localState = saveAdapter.LoadLocal();

            if (localState.worldSeed != 0)
            {
                seedManager.SetSeed(localState.worldSeed);
            }

            if (NetworkManager.Instance != null && NetworkManager.Instance.IsAuthenticated)
            {
                var result = await saveService.LoadSeedIntoManager();

                if (!result.Success && result.StatusCode != 404)
                {
                    SetStatus("CLOUD UNAVAILABLE. CONTINUING LOCAL SAVE");
                    await Task.Delay(650);
                }
            }

            LoadGameplayScene();
        }

        private void LoadGameplayScene()
        {
            if (!Application.CanStreamedLevelBeLoaded(MainGameplaySceneName))
            {
                SetBusy(false, $"SCENE '{MainGameplaySceneName}' IS NOT IN BUILD SETTINGS");
                return;
            }

            SceneManager.LoadScene(MainGameplaySceneName, LoadSceneMode.Single);
        }

        private void BuildInterface()
        {
            EnsureEventSystem();
            var canvasObject = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var backdrop = CreateImage(canvasObject.transform, "Backdrop", Background);
            SetRect(backdrop.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var rail = CreateImage(backdrop.transform, "SignalRail", Primary);
            SetRect(rail.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-286f, 0f), new Vector2(4f, 410f));

            var title = CreateText(backdrop.transform, "Title", 56f, FontStyles.Bold, TextAlignmentOptions.Left);
            title.text = "DON'T DIE PLEASE";
            SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-245f, 154f), new Vector2(680f, 74f));

            var subtitle = CreateText(backdrop.transform, "Subtitle", 17f, FontStyles.Normal, TextAlignmentOptions.Left);
            subtitle.color = Primary;
            subtitle.text = "YGGDRASIL SURVIVAL PROTOCOL";
            SetRect(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-242f, 108f), new Vector2(520f, 30f));

            newGameButton = CreateButton(backdrop.transform, "NewGameButton", "NEW GAME", new Vector2(-242f, 24f));
            continueButton = CreateButton(backdrop.transform, "ContinueButton", "CONTINUE", new Vector2(-242f, -50f));
            newGameButton.onClick.AddListener(StartNewGame);
            continueButton.onClick.AddListener(ContinueGame);

            statusText = CreateText(backdrop.transform, "Status", 14f, FontStyles.Bold, TextAlignmentOptions.Left);
            statusText.color = new Color(0.62f, 0.72f, 0.74f, 1f);
            SetRect(statusText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-242f, -126f), new Vector2(600f, 36f));
            statusText.text = string.Empty;
        }

        private Button CreateButton(Transform parent, string objectName, string labelText, Vector2 position)
        {
            var go = new GameObject(objectName, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(420f, 58f));
            go.GetComponent<Image>().color = Secondary;
            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.65f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.35f, 0.72f, 0.76f, 1f);
            colors.disabledColor = new Color(0.34f, 0.38f, 0.39f, 0.7f);
            button.colors = colors;
            var label = CreateText(go.transform, "Label", 19f, FontStyles.Bold, TextAlignmentOptions.Center);
            label.text = labelText;
            SetRect(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private void SetBusy(bool value, string status)
        {
            busy = value;

            if (newGameButton != null)
            {
                newGameButton.interactable = !value;
            }

            if (continueButton != null)
            {
                continueButton.interactable = !value;
            }

            SetStatus(status);
        }

        private void SetStatus(string value)
        {
            if (statusText != null)
            {
                statusText.text = value ?? string.Empty;
            }
        }

        private static Image CreateImage(Transform parent, string objectName, Color color)
        {
            var go = new GameObject(objectName, typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string objectName,
            float fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment)
        {
            var go = new GameObject(objectName, typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = TextColor;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }
    }
}
