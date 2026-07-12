using System.Threading.Tasks;
using UnityEngine;

namespace DontDiePlease.Auth
{
    public class AuthManager : MonoBehaviour
    {
        private const string TokenKey = "DontDiePlease.Auth.Token";
        private const string UserIdKey = "DontDiePlease.Auth.UserId";
        private const string UsernameKey = "DontDiePlease.Auth.Username";

        public static AuthManager Instance { get; private set; }

        [SerializeField] private AuthApiClient apiClient;

        public AuthSession Session { get; } = new AuthSession();

        public bool IsAuthenticated => Session.IsAuthenticated;
        public string Token => Session.Token;
        public string UserId => Session.UserId;
        public string Username => Session.Username;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureApiClient();
            LoadStoredSession();
        }

        public async Task<AuthResponse> LoginAsync(string emailOrUsername, string password)
        {
            EnsureApiClient();
            var resp = await apiClient.LoginAsync(new LoginRequest
            {
                emailOrUsername = emailOrUsername,
                password = password
            });

            ApplySuccessfulResponse(resp);
            return resp;
        }

        public async Task<AuthResponse> RegisterAsync(string emailOrUsername, string password)
        {
            EnsureApiClient();
            var resp = await apiClient.RegisterAsync(new RegisterRequest
            {
                emailOrUsername = emailOrUsername,
                password = password
            });

            ApplySuccessfulResponse(resp);
            return resp;
        }

        public void ClearSession()
        {
            Session.Clear();
            PlayerPrefs.DeleteKey(TokenKey);
            PlayerPrefs.DeleteKey(UserIdKey);
            PlayerPrefs.DeleteKey(UsernameKey);
            PlayerPrefs.Save();
        }

        private void ApplySuccessfulResponse(AuthResponse resp)
        {
            if (resp == null || !resp.success)
            {
                return;
            }

            Session.SetSession(resp.token, resp.userId, resp.username);
            PlayerPrefs.SetString(TokenKey, resp.token);
            PlayerPrefs.SetString(UserIdKey, resp.userId);
            PlayerPrefs.SetString(UsernameKey, resp.username);
            PlayerPrefs.Save();
        }

        private void LoadStoredSession()
        {
            var token = PlayerPrefs.GetString(TokenKey, string.Empty);
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            Session.SetSession(
                token,
                PlayerPrefs.GetString(UserIdKey, string.Empty),
                PlayerPrefs.GetString(UsernameKey, "Player"));
        }

        private void EnsureApiClient()
        {
            if (apiClient != null)
            {
                return;
            }

            apiClient = GetComponent<AuthApiClient>();
            if (apiClient == null)
            {
                apiClient = gameObject.AddComponent<AuthApiClient>();
            }
        }
    }
}
