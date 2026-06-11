using System;
using System.Threading.Tasks;
using DontDiePlease.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DontDiePlease.Auth
{
    public sealed class AuthUIController : MonoBehaviour
    {
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button registerButton;
        [SerializeField] private Button toggleModeButton;
        [SerializeField] private TextMeshProUGUI statusMessageText;
        [SerializeField] private GameObject loadingOverlay;
        [SerializeField] private GameObject registerPanelGroup;
        [SerializeField] private GameObject loginPanelGroup;
        [SerializeField] private string mainGameSceneName = "MainGame";
        [SerializeField] private int mainGameSceneBuildIndex = -1;
        [SerializeField] private bool startInRegisterMode;

        private NetworkManager networkManager;
        private bool isRegisterMode;
        private bool isSubmitting;

        private void Awake()
        {
            networkManager = ResolveNetworkManager();
            isRegisterMode = startInRegisterMode;
            ConfigureInputs();
            RegisterButtonHandlers();
            RefreshModeView();
            SetSubmitting(false);
            SetStatus(string.Empty);
        }

        private void OnDestroy()
        {
            UnregisterButtonHandlers();
        }

        private async void HandleLoginClicked()
        {
            await SubmitLogin();
        }

        private async void HandleRegisterClicked()
        {
            await SubmitRegistration();
        }

        private void HandleToggleModeClicked()
        {
            if (isSubmitting)
            {
                return;
            }

            isRegisterMode = !isRegisterMode;
            ClearFields();
            SetStatus(string.Empty);
            RefreshModeView();
        }

        private async Task SubmitLogin()
        {
            if (isSubmitting)
            {
                return;
            }

            var validationError = ValidateLoginInput();
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                SetStatus(validationError);
                return;
            }

            var shouldStayLoading = false;
            SetSubmitting(true);
            SetStatus("Signing in...");

            try
            {
                var result = await networkManager.LoginUser(emailInput.text.Trim(), passwordInput.text);

                if (!result.Success)
                {
                    SetStatus(result.Error);
                    return;
                }

                shouldStayLoading = TryLoadMainGame();
            }
            catch (Exception exception)
            {
                SetStatus(ResolveExceptionMessage(exception));
            }
            finally
            {
                if (!shouldStayLoading)
                {
                    SetSubmitting(false);
                }
            }
        }

        private async Task SubmitRegistration()
        {
            if (isSubmitting)
            {
                return;
            }

            var validationError = ValidateRegistrationInput();
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                SetStatus(validationError);
                return;
            }

            var shouldStayLoading = false;
            SetSubmitting(true);
            SetStatus("Creating account...");

            try
            {
                var result = await networkManager.RegisterUser(usernameInput.text.Trim(), emailInput.text.Trim(), passwordInput.text);

                if (!result.Success)
                {
                    SetStatus(result.Error);
                    return;
                }

                shouldStayLoading = TryLoadMainGame();
            }
            catch (Exception exception)
            {
                SetStatus(ResolveExceptionMessage(exception));
            }
            finally
            {
                if (!shouldStayLoading)
                {
                    SetSubmitting(false);
                }
            }
        }

        private string ValidateLoginInput()
        {
            if (emailInput == null || string.IsNullOrWhiteSpace(emailInput.text))
            {
                return "Email is required";
            }

            if (passwordInput == null || string.IsNullOrWhiteSpace(passwordInput.text))
            {
                return "Password is required";
            }

            return string.Empty;
        }

        private string ValidateRegistrationInput()
        {
            if (usernameInput == null || string.IsNullOrWhiteSpace(usernameInput.text))
            {
                return "Username is required";
            }

            if (emailInput == null || string.IsNullOrWhiteSpace(emailInput.text))
            {
                return "Email is required";
            }

            if (passwordInput == null || string.IsNullOrWhiteSpace(passwordInput.text))
            {
                return "Password is required";
            }

            return string.Empty;
        }

        private bool TryLoadMainGame()
        {
            if (mainGameSceneBuildIndex >= 0)
            {
                SceneManager.LoadScene(mainGameSceneBuildIndex);
                return true;
            }

            if (string.IsNullOrWhiteSpace(mainGameSceneName))
            {
                SetStatus("Main game scene is not configured");
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(mainGameSceneName))
            {
                SetStatus($"Scene '{mainGameSceneName}' is not in Build Settings");
                return false;
            }

            SceneManager.LoadScene(mainGameSceneName);
            return true;
        }

        private void RefreshModeView()
        {
            if (loginPanelGroup != null)
            {
                loginPanelGroup.SetActive(!isRegisterMode);
            }

            if (registerPanelGroup != null)
            {
                registerPanelGroup.SetActive(isRegisterMode);
            }

            if (usernameInput != null)
            {
                usernameInput.gameObject.SetActive(isRegisterMode);
            }

            SetButtonVisible(loginButton, !isRegisterMode);
            SetButtonVisible(registerButton, isRegisterMode);
        }

        private void SetSubmitting(bool submitting)
        {
            isSubmitting = submitting;

            if (loadingOverlay != null)
            {
                loadingOverlay.SetActive(submitting);
            }

            SetButtonInteractable(loginButton, !submitting);
            SetButtonInteractable(registerButton, !submitting);
            SetButtonInteractable(toggleModeButton, !submitting);
            SetInputInteractable(usernameInput, !submitting);
            SetInputInteractable(emailInput, !submitting);
            SetInputInteractable(passwordInput, !submitting);
        }

        private void SetStatus(string message)
        {
            if (statusMessageText == null)
            {
                return;
            }

            statusMessageText.text = message ?? string.Empty;
            statusMessageText.gameObject.SetActive(!string.IsNullOrWhiteSpace(statusMessageText.text));
        }

        private void ClearFields()
        {
            SetInputText(usernameInput, string.Empty);
            SetInputText(emailInput, string.Empty);
            SetInputText(passwordInput, string.Empty);
        }

        private void ConfigureInputs()
        {
            if (passwordInput != null)
            {
                passwordInput.contentType = TMP_InputField.ContentType.Password;
                passwordInput.ForceLabelUpdate();
            }
        }

        private void RegisterButtonHandlers()
        {
            if (loginButton != null)
            {
                loginButton.onClick.RemoveListener(HandleLoginClicked);
                loginButton.onClick.AddListener(HandleLoginClicked);
            }

            if (registerButton != null)
            {
                registerButton.onClick.RemoveListener(HandleRegisterClicked);
                registerButton.onClick.AddListener(HandleRegisterClicked);
            }

            if (toggleModeButton != null)
            {
                toggleModeButton.onClick.RemoveListener(HandleToggleModeClicked);
                toggleModeButton.onClick.AddListener(HandleToggleModeClicked);
            }
        }

        private void UnregisterButtonHandlers()
        {
            if (loginButton != null)
            {
                loginButton.onClick.RemoveListener(HandleLoginClicked);
            }

            if (registerButton != null)
            {
                registerButton.onClick.RemoveListener(HandleRegisterClicked);
            }

            if (toggleModeButton != null)
            {
                toggleModeButton.onClick.RemoveListener(HandleToggleModeClicked);
            }
        }

        private NetworkManager ResolveNetworkManager()
        {
            if (NetworkManager.Instance != null)
            {
                return NetworkManager.Instance;
            }

            var existingNetworkManager = FindObjectOfType<NetworkManager>();
            if (existingNetworkManager != null)
            {
                return existingNetworkManager;
            }

            var networkObject = new GameObject("NetworkManager");
            return networkObject.AddComponent<NetworkManager>();
        }

        private static void SetButtonInteractable(Button button, bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        private static void SetButtonVisible(Button button, bool visible)
        {
            if (button != null)
            {
                button.gameObject.SetActive(visible);
            }
        }

        private static void SetInputInteractable(TMP_InputField input, bool interactable)
        {
            if (input != null)
            {
                input.interactable = interactable;
            }
        }

        private static void SetInputText(TMP_InputField input, string value)
        {
            if (input != null)
            {
                input.text = value;
            }
        }

        private static string ResolveExceptionMessage(Exception exception)
        {
            if (exception == null || string.IsNullOrWhiteSpace(exception.Message))
            {
                return "Request failed";
            }

            return exception.Message;
        }
    }
}
