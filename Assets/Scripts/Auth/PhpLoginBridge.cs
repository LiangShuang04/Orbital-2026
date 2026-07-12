using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DontDiePlease.Auth
{
    public sealed class PhpLoginBridge : MonoBehaviour
    {
        [SerializeField] private bool useMockAuth = true;
        [SerializeField] private string targetSceneName = "MainMenuScene";
        [SerializeField] private string serverBaseUrl;
        [SerializeField] private string sharedApiKey;
        [SerializeField] private float mockDelaySeconds = 0.35f;

        private UIGlobal ui;
        private TMP_InputField loginInput;
        private TMP_InputField passwordInput;
        private TMP_InputField registerLoginInput;
        private TMP_InputField registerPasswordInput;
        private TMP_InputField registerConfirmPasswordInput;
        private TMP_InputField registerEmailInput;
        private TMP_InputField registerConfirmEmailInput;
        private TextMeshProUGUI statusText;
        private Button nextButton;
        private Button loginButton;
        private Button newAccountButton;
        private Button registerButton;
        private Button backButton;
        private Button registerBackButton;
        private readonly List<TMP_InputField> inputs = new List<TMP_InputField>();
        private readonly List<Button> controlledButtons = new List<Button>();
        private readonly Dictionary<Button, string> buttonLabels = new Dictionary<Button, string>();
        private bool busy;

        public void Configure(string sceneName, bool mockAuth, string baseUrl)
        {
            targetSceneName = string.IsNullOrWhiteSpace(sceneName) ? targetSceneName : sceneName;
            useMockAuth = mockAuth;
            serverBaseUrl = baseUrl ?? string.Empty;
        }

        private async void Start()
        {
            await Task.Yield();
            ResolveSceneReferences();
            DisablePackageAuthScripts();
            ClearPackageInputEvents();
            BindControls();
            ConfigureCanvasScalers();
            SetStatus(string.Empty);
            SetRegisterVisible(false);
            EnsureEventSystem();
            SetBusy(false);
        }

        private void Update()
        {
            if (busy)
                return;

            if (WasTabPressed())
            {
                FocusNextInput();
                return;
            }

            if (!WasSubmitPressed())
                return;

            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

            if (selected == loginInput?.gameObject)
            {
                ContinueToPassword();
                return;
            }

            if (selected == passwordInput?.gameObject)
            {
                SubmitLogin();
                return;
            }

            if (IsRegisterInputSelected(selected))
            {
                SubmitRegister();
            }
        }

        private void ResolveSceneReferences()
        {
            ui = UIGlobal.instance != null ? UIGlobal.instance : FindObjectsByType<UIGlobal>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();

            if (ui != null && ui.AllInputtext != null)
            {
                loginInput = GetInput(0);
                passwordInput = GetInput(1);
                registerLoginInput = GetInput(2);
                registerPasswordInput = GetInput(3);
                registerConfirmPasswordInput = GetInput(4);
                registerEmailInput = GetInput(5);
                registerConfirmEmailInput = GetInput(6);
            }

            loginInput = loginInput != null ? loginInput : FindInput("InputFieldLogin");
            passwordInput = passwordInput != null ? passwordInput : FindInput("InputFieldPass");
            registerLoginInput = registerLoginInput != null ? registerLoginInput : FindInput("InputFieldRegisterLogin");
            registerPasswordInput = registerPasswordInput != null ? registerPasswordInput : FindInput("InputFieldPass1");
            registerConfirmPasswordInput = registerConfirmPasswordInput != null ? registerConfirmPasswordInput : FindInput("InputFieldPass2");
            registerEmailInput = registerEmailInput != null ? registerEmailInput : FindInput("InputFieldEmail1");
            registerConfirmEmailInput = registerConfirmEmailInput != null ? registerConfirmEmailInput : FindInput("InputFieldEmail2");
            statusText = FindText("ErrorLogin") ?? FindText("ErrorPassword") ?? FindText("ErrorEmail") ?? ui?.AllText?.FirstOrDefault(text => text != null);

            nextButton = GetButton(ui?.BtnNext) ?? FindButton("BtnNext") ?? FindButton("Button");
            loginButton = GetButton(ui?.BtnValidate) ?? FindButton("BtnValidate");
            newAccountButton = GetButton(ui?.BtnNewAccount) ?? FindButton("BtnNewAccount");
            registerButton = GetButton(ui?.BtnCreateAccount) ?? FindButton("BtnCreateAccount");
            backButton = GetButton(ui?.BtnBack) ?? FindButton("BtnBack");
            registerBackButton = GetButton(ui?.BtnBackLogin) ?? FindButton("BtnBackLogin");

            inputs.Clear();
            AddInput(loginInput);
            AddInput(passwordInput);
            AddInput(registerLoginInput);
            AddInput(registerPasswordInput);
            AddInput(registerConfirmPasswordInput);
            AddInput(registerEmailInput);
            AddInput(registerConfirmEmailInput);

            controlledButtons.Clear();
            AddButton(nextButton);
            AddButton(loginButton);
            AddButton(newAccountButton);
            AddButton(registerButton);
            AddButton(backButton);
            AddButton(registerBackButton);
            CacheButtonLabels();
        }

        private void BindControls()
        {
            ReplaceClick(nextButton, ContinueToPassword);
            ReplaceClick(loginButton, SubmitLogin);
            ReplaceClick(newAccountButton, ShowRegister);
            ReplaceClick(registerButton, SubmitRegister);
            ReplaceClick(backButton, ResetLoginFlow);
            ReplaceClick(registerBackButton, HideRegister);
        }

        private void DisablePackageAuthScripts()
        {
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour == null || behaviour.gameObject.scene != gameObject.scene)
                    continue;

                var typeName = behaviour.GetType().Name;

                if (typeName == "PhpLoginInputField" || typeName == "InputField" || typeName == "Register" || typeName == "LoadPHP")
                    behaviour.enabled = false;
            }
        }

        private void ClearPackageInputEvents()
        {
            foreach (var input in inputs)
            {
                if (input == null)
                    continue;

                input.onValueChanged.RemoveAllListeners();
                input.onEndEdit.RemoveAllListeners();
                input.onSubmit.RemoveAllListeners();
            }

            if (passwordInput != null)
                passwordInput.contentType = TMP_InputField.ContentType.Password;

            if (registerPasswordInput != null)
                registerPasswordInput.contentType = TMP_InputField.ContentType.Password;

            if (registerConfirmPasswordInput != null)
                registerConfirmPasswordInput.contentType = TMP_InputField.ContentType.Password;
        }

        private void ContinueToPassword()
        {
            var username = GetText(loginInput);

            if (string.IsNullOrWhiteSpace(username))
            {
                SetStatus("Enter your username.");
                Focus(loginInput);
                return;
            }

            if (ui != null)
            {
                ui.MyName = username.Trim();
                ui.ChangeTitleForPassword();
            }
            else
            {
                SetActive(loginInput, false);
                SetActive(passwordInput, true);
            }

            SetStatus(string.Empty);
            Focus(passwordInput);
        }

        private async void SubmitLogin()
        {
            var username = !string.IsNullOrWhiteSpace(ui?.MyName) ? ui.MyName : GetText(loginInput);
            var password = GetText(passwordInput);

            if (string.IsNullOrWhiteSpace(username))
            {
                SetStatus("Enter your username.");
                Focus(loginInput);
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                SetStatus("Enter your password.");
                Focus(passwordInput);
                return;
            }

            await RunBusyState(async () =>
            {
                var ok = useMockAuth ? await MockLogin(username, password) : await RealLogin(username, password);

                if (!ok)
                    return;

                SetStatus("Access granted.");
                SetActive(ui?.AccessGranted, true);
                SetActive(ui?.AccessDenied, false);
                SceneManager.LoadScene(targetSceneName);
            });
        }

        private void ShowRegister()
        {
            SetRegisterVisible(true);
            SetStatus(string.Empty);

            if (ui != null)
                ui.ShowRegisterPanel();

            Focus(registerLoginInput);
        }

        private void HideRegister()
        {
            SetRegisterVisible(false);
            SetStatus(string.Empty);
            Focus(loginInput);
        }

        private void ResetLoginFlow()
        {
            SetStatus(string.Empty);

            if (ui != null)
            {
                ui.ReloadLoginUI();
                Focus(loginInput);
                return;
            }

            SetActive(loginInput, true);
            SetActive(passwordInput, false);
            SetInput(loginInput, string.Empty);
            SetInput(passwordInput, string.Empty);
            Focus(loginInput);
        }

        private async void SubmitRegister()
        {
            var username = GetText(registerLoginInput);
            var password = GetText(registerPasswordInput);
            var confirmPassword = GetText(registerConfirmPasswordInput);
            var email = GetText(registerEmailInput);
            var confirmEmail = GetText(registerConfirmEmailInput);
            var error = ValidateRegister(username, password, confirmPassword, email, confirmEmail);

            if (!string.IsNullOrWhiteSpace(error))
            {
                SetStatus(error);
                return;
            }

            await RunBusyState(async () =>
            {
                var ok = useMockAuth ? await MockRegister(username, password, email) : await RealRegister(username, password, email);

                if (!ok)
                    return;

                SetStatus("Account ready. Log in with your new credentials.");
                SetInput(loginInput, username.Trim());
                SetInput(passwordInput, string.Empty);
                SetRegisterVisible(false);
                SetActive(ui?.AccountCreated, true);
                Focus(loginInput);
            });
        }

        private async Task RunBusyState(Func<Task> action)
        {
            SetBusy(true);

            try
            {
                await action();
            }
            catch (Exception err)
            {
                Debug.LogWarning($"login bridge blew up: {err.Message}");
                SetStatus(CleanError(err.Message));
                SetActive(ui?.ConnectionError, true);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task<bool> MockLogin(string username, string password)
        {
            await WaitMockDelay();
            var ok = !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password);

            if (!ok)
            {
                SetStatus("Invalid login.");
                SetActive(ui?.AccessDenied, true);
            }

            return ok;
        }

        private async Task<bool> MockRegister(string username, string password, string email)
        {
            await WaitMockDelay();
            return !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password) && IsEmailLike(email);
        }

        private async Task<bool> RealLogin(string username, string password)
        {
            var url = BuildUrl("login.php");
            var form = new WWWForm();
            form.AddField("playerName", username.Trim());
            form.AddField("password", password);
            AddSharedKey(form);
            var response = await PostForm(url, form);
            var ok = response.IndexOf("Login success", StringComparison.OrdinalIgnoreCase) >= 0 || response.IndexOf("success", StringComparison.OrdinalIgnoreCase) >= 0;

            if (!ok)
            {
                SetStatus("Login rejected by server.");
                SetActive(ui?.AccessDenied, true);
            }

            return ok;
        }

        private async Task<bool> RealRegister(string username, string password, string email)
        {
            var url = BuildUrl("register.php");
            var form = new WWWForm();
            form.AddField("playerName", username.Trim());
            form.AddField("password", password);
            form.AddField("email", email.Trim());
            form.AddField("idavatar", ui != null ? ui.AvatarID : 0);
            AddSharedKey(form);
            var response = await PostForm(url, form);
            var ok = response.IndexOf("Ok", StringComparison.OrdinalIgnoreCase) >= 0 || response.IndexOf("success", StringComparison.OrdinalIgnoreCase) >= 0;

            if (!ok)
                SetStatus("Server could not create this account.");

            return ok;
        }

        private async Task<string> PostForm(string url, WWWForm form)
        {
            if (string.IsNullOrWhiteSpace(serverBaseUrl))
                throw new InvalidOperationException("Set the PHP server URL first.");

            using var request = UnityWebRequest.Post(url, form);
            var op = request.SendWebRequest();

            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException(request.error);

            return request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
        }

        private async Task WaitMockDelay()
        {
            var end = Time.unscaledTime + Mathf.Max(0f, mockDelaySeconds);

            while (Time.unscaledTime < end)
                await Task.Yield();
        }

        private string ValidateRegister(string username, string password, string confirmPassword, string email, string confirmEmail)
        {
            if (string.IsNullOrWhiteSpace(username))
                return "Enter a username.";

            if (string.IsNullOrWhiteSpace(password))
                return "Enter a password.";

            if (password != confirmPassword)
                return "Passwords do not match.";

            if (!IsEmailLike(email))
                return "Enter a valid email.";

            if (!string.Equals(email.Trim(), confirmEmail.Trim(), StringComparison.OrdinalIgnoreCase))
                return "Emails do not match.";

            return string.Empty;
        }

        private string BuildUrl(string endpoint)
        {
            var root = serverBaseUrl.Trim().TrimEnd('/');
            return $"{root}/{endpoint}";
        }

        private void AddSharedKey(WWWForm form)
        {
            if (!string.IsNullOrWhiteSpace(sharedApiKey))
                form.AddField("key", sharedApiKey);
        }

        private void SetBusy(bool value)
        {
            busy = value;

            foreach (var button in controlledButtons)
            {
                if (button == null)
                    continue;

                button.interactable = !value;
                SetButtonLabel(button, value ? "PLEASE WAIT" : GetButtonLabel(button));
            }

            foreach (var input in inputs)
            {
                if (input != null)
                    input.interactable = !value;
            }
        }

        private void SetRegisterVisible(bool visible)
        {
            SetActive(ui?.PanelRegister, visible);
        }

        private void FocusNextInput()
        {
            var visibleInputs = new List<TMP_InputField>();

            foreach (var input in inputs)
            {
                if (input != null && input.gameObject.activeInHierarchy && input.interactable)
                    visibleInputs.Add(input);
            }

            if (visibleInputs.Count == 0)
                return;

            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            var idx = visibleInputs.FindIndex(input => input.gameObject == selected);
            Focus(visibleInputs[(idx + 1 + visibleInputs.Count) % visibleInputs.Count]);
        }

        private bool IsRegisterInputSelected(GameObject selected)
        {
            return selected != null && (selected == registerLoginInput?.gameObject || selected == registerPasswordInput?.gameObject || selected == registerConfirmPasswordInput?.gameObject || selected == registerEmailInput?.gameObject || selected == registerConfirmEmailInput?.gameObject);
        }

        private bool WasSubmitPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#else
            return false;
#endif
        }

        private bool WasTabPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Tab);
#else
            return false;
#endif
        }

        private void EnsureEventSystem()
        {
            var eventSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(system => system != null && system.gameObject.scene == gameObject.scene)
                .ToArray();
            var eventSystem = eventSystems.FirstOrDefault();

            if (eventSystem == null)
            {
                var obj = new GameObject("EventSystem");
                eventSystem = obj.AddComponent<EventSystem>();
            }

            foreach (var extra in eventSystems.Skip(1))
            {
                if (extra != null && extra.gameObject.scene == gameObject.scene)
                    Destroy(extra.gameObject);
            }

#if ENABLE_INPUT_SYSTEM
            foreach (var module in eventSystem.GetComponents<StandaloneInputModule>())
                Destroy(module);

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
#else
            if (eventSystem.GetComponent<StandaloneInputModule>() == null)
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
#endif
        }

        private void ConfigureCanvasScalers()
        {
            foreach (var scaler in FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (scaler == null || scaler.gameObject.scene != gameObject.scene)
                    continue;

                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        private TMP_InputField GetInput(int idx)
        {
            return ui != null && ui.AllInputtext != null && idx >= 0 && idx < ui.AllInputtext.Length ? ui.AllInputtext[idx] : null;
        }

        private TMP_InputField FindInput(string objectName)
        {
            return FindByName<TMP_InputField>(objectName);
        }

        private TextMeshProUGUI FindText(string objectName)
        {
            return FindByName<TextMeshProUGUI>(objectName);
        }

        private Button FindButton(string objectName)
        {
            return FindByName<Button>(objectName);
        }

        private T FindByName<T>(string objectName) where T : Component
        {
            return FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(component => component != null && component.gameObject.name == objectName);
        }

        private Button GetButton(GameObject obj)
        {
            return obj != null ? obj.GetComponent<Button>() : null;
        }

        private void ReplaceClick(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void AddInput(TMP_InputField input)
        {
            if (input != null && !inputs.Contains(input))
                inputs.Add(input);
        }

        private void AddButton(Button button)
        {
            if (button != null && !controlledButtons.Contains(button))
                controlledButtons.Add(button);
        }

        private void CacheButtonLabels()
        {
            buttonLabels.Clear();

            foreach (var button in controlledButtons)
            {
                if (button != null)
                    buttonLabels[button] = ReadButtonLabel(button);
            }
        }

        private string GetButtonLabel(Button button)
        {
            return button != null && buttonLabels.TryGetValue(button, out var value) ? value : ReadButtonLabel(button);
        }

        private string ReadButtonLabel(Button button)
        {
            var label = button != null ? button.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            return label != null ? label.text : string.Empty;
        }

        private void SetButtonLabel(Button button, string value)
        {
            var label = button != null ? button.GetComponentInChildren<TextMeshProUGUI>(true) : null;

            if (label != null)
                label.text = value;
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        private void Focus(TMP_InputField input)
        {
            if (input == null || EventSystem.current == null)
                return;

            input.gameObject.SetActive(true);
            EventSystem.current.SetSelectedGameObject(input.gameObject);
            input.ActivateInputField();
        }

        private static string GetText(TMP_InputField input)
        {
            return input != null ? input.text : string.Empty;
        }

        private static void SetInput(TMP_InputField input, string value)
        {
            if (input != null)
                input.text = value ?? string.Empty;
        }

        private static void SetActive(GameObject obj, bool active)
        {
            if (obj != null)
                obj.SetActive(active);
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null)
                component.gameObject.SetActive(active);
        }

        private static bool IsEmailLike(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var trimmed = value.Trim();
            var at = trimmed.IndexOf('@');
            var dot = trimmed.LastIndexOf('.');
            return at > 0 && dot > at + 1 && dot < trimmed.Length - 1;
        }

        private static string CleanError(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Request failed." : value.Trim();
        }
    }

    public static class PhpLoginSceneRuntimeInstaller
    {
        private static bool subscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallForLoadedScene()
        {
            if (!subscribed)
            {
                SceneManager.sceneLoaded += HandleSceneLoaded;
                subscribed = true;
            }

            TryInstall(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryInstall(scene);
        }

        private static void TryInstall(Scene scene)
        {
            if (!scene.IsValid() || scene.name != "Login")
                return;

            if (UnityEngine.Object.FindObjectsByType<PhpLoginBridge>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(bridge => bridge != null && bridge.gameObject.scene == scene))
                return;

            if (!UnityEngine.Object.FindObjectsByType<UIGlobal>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(ui => ui != null && ui.gameObject.scene == scene))
                return;

            var obj = new GameObject("PHPLoginBridge");
            obj.AddComponent<PhpLoginBridge>();
        }
    }
}
