using Akila.FPSFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DontDiePlease.Systems
{
    public sealed class PauseSettingsMenuController : MonoBehaviour
    {
        private const string MouseSensitivityKey = "settings.mouseSensitivity";
        private const string MasterVolumeKey = "settings.masterVolume";
        private const string FullscreenKey = "settings.fullscreen";

        [SerializeField] private GameObject pauseMenuRoot;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Slider mouseSensitivitySlider;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Text mouseSensitivityValueText;
        [SerializeField] private Text masterVolumeValueText;
        [SerializeField] private string mainMenuSceneName = "MainMenuScene";
        [SerializeField] private bool pauseWithEscape = true;
        [SerializeField] private bool lockCursorOnResume = true;
        [SerializeField] private float defaultMouseSensitivity = 1f;
        [SerializeField] private float defaultMasterVolume = 1f;

        private bool isPaused;

        public float MouseSensitivity { get; private set; }
        public float MasterVolume { get; private set; }
        public bool IsPaused => isPaused;

        private void Awake()
        {
            LoadSettings();
            RegisterHandlers();
            ApplySettingsToControls();
            ResumeGame();
        }

        public void SetGeneratedReferences(
            GameObject menuRoot,
            GameObject panel,
            Button pause,
            Button resume,
            Button restart,
            Button settings,
            Button mainMenu,
            Button quit,
            Button back,
            Slider sensitivity,
            Slider volume,
            Toggle fullscreen,
            Text sensitivityValue,
            Text volumeValue)
        {
            UnregisterHandlers();
            pauseMenuRoot = menuRoot;
            settingsPanel = panel;
            pauseButton = pause;
            resumeButton = resume;
            restartButton = restart;
            settingsButton = settings;
            mainMenuButton = mainMenu;
            quitButton = quit;
            backButton = back;
            mouseSensitivitySlider = sensitivity;
            masterVolumeSlider = volume;
            fullscreenToggle = fullscreen;
            mouseSensitivityValueText = sensitivityValue;
            masterVolumeValueText = volumeValue;
            RegisterHandlers();
            ApplySettingsToControls();
            ResumeGame();
        }

        public void SetMainMenuSceneName(string sceneName)
        {
            mainMenuSceneName = sceneName;
        }

        private void Update()
        {
            if (!pauseWithEscape || !Input.GetKeyDown(KeyCode.Escape))
                return;

            if (isPaused)
                ResumeGame();
            else
                OpenPauseMenu();
        }

        private void OnDestroy()
        {
            UnregisterHandlers();
            SetPausedState(false);
        }

        public void OpenPauseMenu()
        {
            isPaused = true;
            SetPausedState(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (pauseMenuRoot != null)
            {
                pauseMenuRoot.SetActive(true);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        public void ResumeGame()
        {
            isPaused = false;
            SetPausedState(false);
            Cursor.visible = !lockCursorOnResume;
            Cursor.lockState = lockCursorOnResume ? CursorLockMode.Locked : CursorLockMode.None;

            if (pauseMenuRoot != null)
            {
                pauseMenuRoot.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        public void OpenSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
        }

        public void CloseSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        public void RestartScene()
        {
            SetPausedState(false);
            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex);
        }

        public void LoadMainMenu()
        {
            SetPausedState(false);

            if (!string.IsNullOrWhiteSpace(mainMenuSceneName) && Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
                return;
            }

            SceneManager.LoadScene(0);
        }

        public void QuitGame()
        {
            SetPausedState(false);
            Application.Quit();
        }

        private static void SetPausedState(bool paused)
        {
            Time.timeScale = paused ? 0f : 1f;
            FPSFrameworkCore.IsPaused = paused;
            FPSFrameworkCore.IsInputActive = !paused;
        }

        public void SetMouseSensitivity(float value)
        {
            MouseSensitivity = Mathf.Max(0.01f, value);
            PlayerPrefs.SetFloat(MouseSensitivityKey, MouseSensitivity);
            PlayerPrefs.Save();
            RefreshMouseSensitivityText();
        }

        public void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            AudioListener.volume = MasterVolume;
            PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
            PlayerPrefs.Save();
            RefreshMasterVolumeText();
        }

        public void SetFullscreen(bool fullscreen)
        {
            Screen.fullScreen = fullscreen;
            PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void LoadSettings()
        {
            MouseSensitivity = PlayerPrefs.GetFloat(MouseSensitivityKey, Mathf.Max(0.01f, defaultMouseSensitivity));
            MasterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, Mathf.Clamp01(defaultMasterVolume));
            AudioListener.volume = MasterVolume;
            Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        }

        private void ApplySettingsToControls()
        {
            if (mouseSensitivitySlider != null)
                mouseSensitivitySlider.SetValueWithoutNotify(MouseSensitivity);

            if (masterVolumeSlider != null)
                masterVolumeSlider.SetValueWithoutNotify(MasterVolume);

            if (fullscreenToggle != null)
                fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);

            RefreshMouseSensitivityText();
            RefreshMasterVolumeText();
        }

        private void RefreshMouseSensitivityText()
        {
            if (mouseSensitivityValueText != null)
            {
                mouseSensitivityValueText.text = MouseSensitivity.ToString("0.00");
            }
        }

        private void RefreshMasterVolumeText()
        {
            if (masterVolumeValueText != null)
            {
                masterVolumeValueText.text = $"{Mathf.RoundToInt(MasterVolume * 100f)}%";
            }
        }

        private void RegisterHandlers()
        {
            AddButtonHandler(pauseButton, OpenPauseMenu);
            AddButtonHandler(resumeButton, ResumeGame);
            AddButtonHandler(restartButton, RestartScene);
            AddButtonHandler(settingsButton, OpenSettings);
            AddButtonHandler(mainMenuButton, LoadMainMenu);
            AddButtonHandler(quitButton, QuitGame);
            AddButtonHandler(backButton, CloseSettings);

            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSensitivity);
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
            }
        }

        private void UnregisterHandlers()
        {
            RemoveButtonHandler(pauseButton, OpenPauseMenu);
            RemoveButtonHandler(resumeButton, ResumeGame);
            RemoveButtonHandler(restartButton, RestartScene);
            RemoveButtonHandler(settingsButton, OpenSettings);
            RemoveButtonHandler(mainMenuButton, LoadMainMenu);
            RemoveButtonHandler(quitButton, QuitGame);
            RemoveButtonHandler(backButton, CloseSettings);

            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.onValueChanged.RemoveListener(SetMouseSensitivity);
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
            }
        }

        private static void AddButtonHandler(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
                button.onClick.AddListener(action);
            }
        }

        private static void RemoveButtonHandler(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }
    }
}
