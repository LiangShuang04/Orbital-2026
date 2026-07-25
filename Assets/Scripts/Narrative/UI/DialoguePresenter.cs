using System;
using DontDiePlease.Narrative.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DontDiePlease.Narrative.UI
{
    public sealed class DialoguePresenter : MonoBehaviour
    {
        private const float CharactersPerSecond = 36f;
        private const float MinimumAutoAdvanceSeconds = 5.5f;
        private const float ReadingCharactersPerSecond = 21f;
        private static readonly Color PanelColor = new Color(0.008f, 0.016f, 0.022f, 0.92f);
        private static readonly Color Cyan = new Color(0.25f, 0.92f, 0.98f, 1f);
        private static readonly Color TextColor = new Color(0.96f, 0.98f, 0.99f, 1f);
        private static readonly Color MutedText = new Color(0.68f, 0.78f, 0.81f, 1f);
        private static TMP_FontAsset readableFont;

        private GameObject fullRoot;
        private GameObject subtitleRoot;
        private GameObject notificationRoot;
        private GameObject objectiveRoot;
        private RectTransform fullRect;
        private TextMeshProUGUI fullSpeaker;
        private TextMeshProUGUI fullText;
        private TextMeshProUGUI fullHint;
        private TextMeshProUGUI subtitleSpeaker;
        private TextMeshProUGUI subtitleText;
        private TextMeshProUGUI notificationText;
        private TextMeshProUGUI objectiveTitle;
        private TextMeshProUGUI objectiveDescription;
        private readonly Button[] choiceButtons = new Button[3];
        private NarrativeDatabase.Line currentLine;
        private TextMeshProUGUI activeText;
        private Action advanceRequested;
        private Action<int> choiceRequested;
        private Action skipRequested;
        private float visibleCharacterCount;
        private float autoAdvanceAt;
        private bool lineComplete;
        private bool skippable;
        private string mode = "Full";

        public bool IsVisible => currentLine != null;

        private void Awake()
        {
            BuildInterface();
            HideDialogue();
        }

        private void Update()
        {
            if (currentLine == null)
            {
                return;
            }

            if (!lineComplete)
            {
                visibleCharacterCount += CharactersPerSecond * Time.unscaledDeltaTime;
                activeText.maxVisibleCharacters = Mathf.FloorToInt(visibleCharacterCount);

                if (activeText.maxVisibleCharacters >= activeText.textInfo.characterCount)
                {
                    CompleteCurrentLine();
                }
            }

            if (Input.GetKeyDown(KeyCode.Backspace) && skippable)
            {
                skipRequested?.Invoke();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (!lineComplete)
                {
                    CompleteCurrentLine();
                }
                else if (currentLine.choices == null || currentLine.choices.Length == 0)
                {
                    advanceRequested?.Invoke();
                }
            }

            if (currentLine == null)
                return;

            if (lineComplete && currentLine.choices != null)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                {
                    SelectChoice(0);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                {
                    SelectChoice(1);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
                {
                    SelectChoice(2);
                }
            }

            if (lineComplete && autoAdvanceAt > 0f && Time.unscaledTime >= autoAdvanceAt)
            {
                autoAdvanceAt = 0f;
                advanceRequested?.Invoke();
            }
        }

        public void Present(
            NarrativeDatabase.Line line,
            string presentationMode,
            bool canSkip,
            Action onAdvance,
            Action<int> onChoice,
            Action onSkip)
        {
            if (line == null)
            {
                return;
            }

            currentLine = line;
            mode = string.IsNullOrWhiteSpace(presentationMode) ? "Full" : presentationMode;
            skippable = canSkip;
            advanceRequested = onAdvance;
            choiceRequested = onChoice;
            skipRequested = onSkip;
            visibleCharacterCount = 0f;
            autoAdvanceAt = 0f;
            lineComplete = false;

            if (fullRect != null)
                fullRect.sizeDelta = new Vector2(fullRect.sizeDelta.x, 220f);

            ShowMode(mode);
            SetLineText(line);
            HideChoices();
            activeText.ForceMeshUpdate();
            activeText.maxVisibleCharacters = 0;
        }

        public void HideDialogue()
        {
            currentLine = null;
            activeText = null;
            advanceRequested = null;
            choiceRequested = null;
            skipRequested = null;
            autoAdvanceAt = 0f;
            fullRoot.SetActive(false);
            subtitleRoot.SetActive(false);
            notificationRoot.SetActive(false);
            HideChoices();
        }

        public void SetObjective(string title, string description)
        {
            var visible = !string.IsNullOrWhiteSpace(title);
            objectiveRoot.SetActive(visible);

            if (!visible)
            {
                return;
            }

            objectiveTitle.text = title;
            objectiveDescription.text = description ?? string.Empty;
        }

        private void CompleteCurrentLine()
        {
            if (activeText == null)
            {
                return;
            }

            activeText.maxVisibleCharacters = int.MaxValue;
            lineComplete = true;
            SetChoices(currentLine.choices);

            if (currentLine.autoAdvanceSeconds > 0f && (currentLine.choices == null || currentLine.choices.Length == 0))
            {
                var readingTime = currentLine.text?.Length / ReadingCharactersPerSecond ?? 0f;
                var delay = Mathf.Max(currentLine.autoAdvanceSeconds, MinimumAutoAdvanceSeconds, readingTime + 2f);
                autoAdvanceAt = Time.unscaledTime + delay;
            }
        }

        private void SetLineText(NarrativeDatabase.Line line)
        {
            var speaker = string.IsNullOrWhiteSpace(line.speaker) ? "NARRATION" : line.speaker.Trim();
            var speakerColor = ResolveSpeakerColor(speaker);

            if (string.Equals(mode, "Full", StringComparison.OrdinalIgnoreCase))
            {
                fullSpeaker.text = speaker;
                fullSpeaker.color = speakerColor;
                fullText.text = line.text;
                fullHint.text = skippable ? "[SPACE] CONTINUE   [BACKSPACE] SKIP" : "[SPACE] CONTINUE";
                activeText = fullText;
                return;
            }

            if (string.Equals(mode, "System", StringComparison.OrdinalIgnoreCase))
            {
                notificationText.text = line.text;
                notificationText.color = speakerColor;
                activeText = notificationText;
                return;
            }

            subtitleSpeaker.text = speaker;
            subtitleSpeaker.color = speakerColor;
            subtitleText.text = line.text;
            activeText = subtitleText;
        }

        private void ShowMode(string presentationMode)
        {
            var full = string.Equals(presentationMode, "Full", StringComparison.OrdinalIgnoreCase);
            var system = string.Equals(presentationMode, "System", StringComparison.OrdinalIgnoreCase);
            fullRoot.SetActive(full);
            subtitleRoot.SetActive(!full && !system);
            notificationRoot.SetActive(system);
        }

        private void SetChoices(NarrativeDatabase.Choice[] choices)
        {
            HideChoices();

            if (!string.Equals(mode, "Full", StringComparison.OrdinalIgnoreCase) || choices == null)
            {
                return;
            }

            var count = Mathf.Min(choices.Length, choiceButtons.Length);

            if (fullRect != null && count > 0)
                fullRect.sizeDelta = new Vector2(fullRect.sizeDelta.x, 360f);

            for (var index = 0; index < count; index++)
            {
                var button = choiceButtons[index];
                var label = button.GetComponentInChildren<TextMeshProUGUI>();
                var choiceIndex = index;
                label.text = $"{index + 1}. {choices[index].text}";
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => choiceRequested?.Invoke(choiceIndex));
                button.gameObject.SetActive(true);
            }

            if (count > 0 && EventSystem.current != null)
            {
                fullHint.text = "[1-3] SELECT   [BACKSPACE] SKIP";
                EventSystem.current.SetSelectedGameObject(choiceButtons[0].gameObject);
            }
        }

        private void SelectChoice(int index)
        {
            if (currentLine.choices == null || index < 0 || index >= currentLine.choices.Length)
            {
                return;
            }

            choiceRequested?.Invoke(index);
        }

        private void HideChoices()
        {
            foreach (var button in choiceButtons)
            {
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                button.gameObject.SetActive(false);
            }
        }

        private void BuildInterface()
        {
            EnsureEventSystem();
            var canvas = CreateCanvas();
            readableFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF") ??
                           TMP_Settings.defaultFontAsset;
            fullRoot = CreatePanel(canvas.transform, "FullDialogue", new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(1080f, 220f));
            fullRect = fullRoot.GetComponent<RectTransform>();
            subtitleRoot = CreatePanel(canvas.transform, "ExplorationSubtitle", new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(1140f, 156f));
            notificationRoot = CreatePanel(canvas.transform, "SystemNotification", new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(820f, 68f));
            objectiveRoot = CreatePanel(canvas.transform, "ObjectivePanel", new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(380f, 116f));

            BuildFullDialogue();
            BuildSubtitle();
            BuildNotification();
            BuildObjective();
        }

        private Canvas CreateCanvas()
        {
            var go = new GameObject("NarrativeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private void BuildFullDialogue()
        {
            AddRail(fullRoot.transform, Cyan);
            fullSpeaker = CreateText(fullRoot.transform, "Speaker", 18f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(fullSpeaker.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(34f, -14f), new Vector2(-68f, 24f));
            fullText = CreateText(fullRoot.transform, "Dialogue", 26f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            fullText.enableAutoSizing = true;
            fullText.fontSizeMin = 20f;
            fullText.fontSizeMax = 26f;
            SetRect(fullText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(34f, -48f), new Vector2(-68f, 108f));
            fullHint = CreateText(fullRoot.transform, "Hint", 15f, FontStyles.Bold, TextAlignmentOptions.BottomRight);
            fullHint.color = MutedText;
            SetRect(fullHint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(32f, 10f), new Vector2(-64f, 24f));

            for (var index = 0; index < choiceButtons.Length; index++)
            {
                var y = 124f - index * 46f;
                choiceButtons[index] = CreateChoiceButton(fullRoot.transform, index, y);
            }
        }

        private void BuildSubtitle()
        {
            AddRail(subtitleRoot.transform, Cyan);
            subtitleSpeaker = CreateText(subtitleRoot.transform, "Speaker", 18f, FontStyles.Bold, TextAlignmentOptions.Left);
            SetRect(subtitleSpeaker.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(32f, -14f), new Vector2(-64f, 24f));
            subtitleText = CreateText(subtitleRoot.transform, "Subtitle", 24f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            subtitleText.enableAutoSizing = true;
            subtitleText.fontSizeMin = 19f;
            subtitleText.fontSizeMax = 24f;
            SetRect(subtitleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(32f, -48f), new Vector2(-64f, 92f));
        }

        private void BuildNotification()
        {
            AddRail(notificationRoot.transform, new Color(1f, 0.72f, 0.18f, 1f));
            notificationText = CreateText(notificationRoot.transform, "Notification", 26f, FontStyles.Bold, TextAlignmentOptions.Center);
            SetRect(notificationText.rectTransform, Vector2.zero, Vector2.one, new Vector2(22f, 8f), new Vector2(-44f, -16f));
        }

        private void BuildObjective()
        {
            AddRail(objectiveRoot.transform, new Color(1f, 0.72f, 0.18f, 1f));
            objectiveTitle = CreateText(objectiveRoot.transform, "ObjectiveTitle", 20f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            objectiveTitle.color = new Color(1f, 0.78f, 0.32f, 1f);
            SetRect(objectiveTitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(22f, -14f), new Vector2(-40f, 26f));
            objectiveDescription = CreateText(objectiveRoot.transform, "ObjectiveDescription", 17f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            objectiveDescription.color = TextColor;
            SetRect(objectiveDescription.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(22f, 12f), new Vector2(-40f, -50f));
        }

        private Button CreateChoiceButton(Transform parent, int index, float y)
        {
            var go = new GameObject($"Choice{index + 1}", typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(28f, y), new Vector2(-56f, 36f));
            var image = go.GetComponent<Image>();
            image.color = new Color(0.08f, 0.16f, 0.18f, 0.98f);
            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.55f, 0.95f, 1f, 1f);
            colors.pressedColor = new Color(0.3f, 0.72f, 0.78f, 1f);
            button.colors = colors;
            var label = CreateText(go.transform, "Label", 19f, FontStyles.Normal, TextAlignmentOptions.Left);
            SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 3f), new Vector2(-28f, -6f));
            return button;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = PanelColor;
            return go;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, float size, FontStyles style, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = readableFont;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = TextColor;
            text.characterSpacing = 0f;
            text.lineSpacing = 3f;
            text.outlineColor = new Color(0f, 0f, 0f, 0.88f);
            text.outlineWidth = 0.06f;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static void AddRail(Transform parent, Color color)
        {
            var rail = new GameObject("AccentRail", typeof(Image));
            rail.transform.SetParent(parent, false);
            var rect = rail.GetComponent<RectTransform>();
            SetRect(rect, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(5f, 0f));
            rail.GetComponent<Image>().color = color;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(anchorMin.x, anchorMax.y);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Color ResolveSpeakerColor(string speaker)
        {
            if (speaker.Equals("MIMIR", StringComparison.OrdinalIgnoreCase))
            {
                return Cyan;
            }

            if (speaker.Equals("ARCHE", StringComparison.OrdinalIgnoreCase))
            {
                return new Color(0.76f, 0.68f, 1f, 1f);
            }

            if (speaker.Contains("CAPTAIN", StringComparison.OrdinalIgnoreCase))
            {
                return new Color(1f, 0.72f, 0.38f, 1f);
            }

            if (speaker.Contains("WARDEN", StringComparison.OrdinalIgnoreCase) || speaker.Contains("OMPHALOS", StringComparison.OrdinalIgnoreCase))
            {
                return new Color(1f, 0.38f, 0.32f, 1f);
            }

            if (speaker.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase))
            {
                return new Color(1f, 0.8f, 0.34f, 1f);
            }

            return TextColor;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
