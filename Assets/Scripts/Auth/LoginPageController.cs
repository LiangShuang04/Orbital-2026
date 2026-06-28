using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityInputField = UnityEngine.UI.InputField;

namespace DontDiePlease.Auth
{
    public class LoginPageController : MonoBehaviour
    {
        private const int MinimumPasswordLength = 6;

        [Header("Auth")]
        [SerializeField] private AuthManager authManager;
        [SerializeField] private string targetSceneName = "";

        [Header("UI")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text modeTitleText;
        [SerializeField] private UnityInputField emailInput;
        [SerializeField] private UnityInputField passwordInput;
        [SerializeField] private UnityInputField confirmPasswordInput;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button switchModeButton;
        [SerializeField] private Text submitButtonText;
        [SerializeField] private Text switchModeButtonText;
        [SerializeField] private Text errorText;
        [SerializeField] private Text loadingText;

        private bool isRegisterMode;
        private bool isSubmitting;

        private void Awake()
        {
            EnsureAuthManager();
            ConfigurePasswordFields();
            RegisterButtonHandlers();
            RefreshMode();
            SetLoading(false);
            SetError(string.Empty);
        }

        public void SetGeneratedReferences(
            Text generatedTitleText,
            Text generatedModeTitleText,
            UnityInputField generatedEmailInput,
            UnityInputField generatedPasswordInput,
            UnityInputField generatedConfirmPasswordInput,
            Button generatedSubmitButton,
            Button generatedSwitchModeButton,
            Text generatedSubmitButtonText,
            Text generatedSwitchModeButtonText,
            Text generatedErrorText,
            Text generatedLoadingText)
        {
            titleText = generatedTitleText;
            modeTitleText = generatedModeTitleText;
            emailInput = generatedEmailInput;
            passwordInput = generatedPasswordInput;
            confirmPasswordInput = generatedConfirmPasswordInput;
            submitButton = generatedSubmitButton;
            switchModeButton = generatedSwitchModeButton;
            submitButtonText = generatedSubmitButtonText;
            switchModeButtonText = generatedSwitchModeButtonText;
            errorText = generatedErrorText;
            loadingText = generatedLoadingText;

            ConfigurePasswordFields();
            RegisterButtonHandlers();
            RefreshMode();
            SetLoading(false);
            SetError(string.Empty);
        }

        public void SetTargetSceneName(string sceneName)
        {
            targetSceneName = sceneName;
        }

        public async void OnSubmitClicked()
        {
            if (isSubmitting)
            {
                return;
            }

            var validationError = ValidateInputs();
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                SetError(validationError);
                return;
            }

            await SubmitAsync();
        }

        public void OnSwitchModeClicked()
        {
            if (isSubmitting)
            {
                return;
            }

            isRegisterMode = !isRegisterMode;
            SetError(string.Empty);
            RefreshMode();
        }

        private async Task SubmitAsync()
        {
            SetLoading(true);
            SetError(string.Empty);

            var emailOrUsername = emailInput.text.Trim();
            var password = passwordInput.text;
            AuthResponse response = isRegisterMode
                ? await authManager.RegisterAsync(emailOrUsername, password)
                : await authManager.LoginAsync(emailOrUsername, password);

            SetLoading(false);

            if (response == null || !response.success)
            {
                SetError(response != null && !string.IsNullOrWhiteSpace(response.errorMessage)
                    ? response.errorMessage
                    : "Login failed. Please try again.");
                return;
            }

            LoadTargetScene();
        }

        private string ValidateInputs()
        {
            if (emailInput == null || string.IsNullOrWhiteSpace(emailInput.text))
            {
                return "Email cannot be empty";
            }

            if (passwordInput == null || string.IsNullOrWhiteSpace(passwordInput.text))
            {
                return "Password cannot be empty";
            }

            if (passwordInput.text.Length < MinimumPasswordLength)
            {
                return "Password must be at least 6 characters";
            }

            if (isRegisterMode)
            {
                if (confirmPasswordInput == null || string.IsNullOrWhiteSpace(confirmPasswordInput.text))
                {
                    return "Confirm password cannot be empty";
                }

                if (passwordInput.text != confirmPasswordInput.text)
                {
                    return "Passwords do not match";
                }
            }

            return string.Empty;
        }

        private void LoadTargetScene()
        {
            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                SetError("Login succeeded. Set Target Scene Name on LoginPageController to continue.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
            {
                SetError($"Login succeeded, but scene '{targetSceneName}' is not in Build Settings.");
                return;
            }

            SceneManager.LoadScene(targetSceneName);
        }

        private void RefreshMode()
        {
            if (titleText != null)
            {
                titleText.text = "DON'T DIE PLEASE";
            }

            if (modeTitleText != null)
            {
                modeTitleText.text = isRegisterMode ? "CREATE ACCOUNT" : "LOGIN";
            }

            if (submitButtonText != null)
            {
                submitButtonText.text = isRegisterMode ? "CREATE ACCOUNT" : "LOGIN";
            }

            if (switchModeButtonText != null)
            {
                switchModeButtonText.text = isRegisterMode ? "BACK TO LOGIN" : "CREATE ACCOUNT";
            }

            if (confirmPasswordInput != null)
            {
                confirmPasswordInput.gameObject.SetActive(isRegisterMode);
                confirmPasswordInput.text = string.Empty;
            }

            RefreshLayout();
        }

        private void RefreshLayout()
        {
            if (isRegisterMode)
            {
                SetAnchoredY(errorText, 204f);
                SetAnchoredY(loadingText, 170f);
                SetAnchoredY(submitButton, 118f);
                SetAnchoredY(switchModeButton, 42f);
                return;
            }

            SetAnchoredY(errorText, 286f);
            SetAnchoredY(loadingText, 252f);
            SetAnchoredY(submitButton, 192f);
            SetAnchoredY(switchModeButton, 118f);
        }

        private static void SetAnchoredY(Component component, float y)
        {
            if (component == null)
            {
                return;
            }

            var rectTransform = component.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, y);
        }

        private void SetLoading(bool loading)
        {
            isSubmitting = loading;

            if (submitButton != null)
            {
                submitButton.interactable = !loading;
            }

            if (switchModeButton != null)
            {
                switchModeButton.interactable = !loading;
            }

            if (emailInput != null)
            {
                emailInput.interactable = !loading;
            }

            if (passwordInput != null)
            {
                passwordInput.interactable = !loading;
            }

            if (confirmPasswordInput != null)
            {
                confirmPasswordInput.interactable = !loading;
            }

            if (loadingText != null)
            {
                loadingText.gameObject.SetActive(loading);
                loadingText.text = loading ? "Authenticating..." : string.Empty;
            }
        }

        private void SetError(string message)
        {
            if (errorText == null)
            {
                return;
            }

            errorText.text = message;
            errorText.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
        }

        private void ConfigurePasswordFields()
        {
            if (passwordInput != null)
            {
                passwordInput.contentType = UnityInputField.ContentType.Password;
            }

            if (confirmPasswordInput != null)
            {
                confirmPasswordInput.contentType = UnityInputField.ContentType.Password;
            }
        }

        private void RegisterButtonHandlers()
        {
            if (submitButton != null)
            {
                submitButton.onClick.RemoveListener(OnSubmitClicked);
                submitButton.onClick.AddListener(OnSubmitClicked);
            }

            if (switchModeButton != null)
            {
                switchModeButton.onClick.RemoveListener(OnSwitchModeClicked);
                switchModeButton.onClick.AddListener(OnSwitchModeClicked);
            }
        }

        private void EnsureAuthManager()
        {
            if (authManager != null)
            {
                return;
            }

            authManager = AuthManager.Instance;
            if (authManager != null)
            {
                return;
            }

            var authObject = new GameObject("AuthManager");
            authManager = authObject.AddComponent<AuthManager>();
        }
    }
}
