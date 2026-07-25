using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace DontDiePlease.Auth
{
    public class AuthApiClient : MonoBehaviour
    {
        [Header("Backend")]
        [SerializeField] private bool useMockAuthentication = true;
        [SerializeField] private string baseApiUrl = "http://localhost:3000";
        [SerializeField] private string loginEndpoint = "/auth/login";
        [SerializeField] private string registerEndpoint = "/auth/register";

        [Header("Mock Settings")]
        [SerializeField] private float mockDelaySeconds = 0.4f;

        public bool UseMockAuthentication
        {
            get => useMockAuthentication;
            set => useMockAuthentication = value;
        }

        public string BaseApiUrl
        {
            get => baseApiUrl;
            set => baseApiUrl = value;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            if (useMockAuthentication)
            {
                return await MockAuthenticateAsync(request.emailOrUsername);
            }

            return await PostJsonAsync(BuildUrl(loginEndpoint), request, "Login failed. Please try again.");
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (useMockAuthentication)
            {
                return await MockAuthenticateAsync(request.emailOrUsername);
            }

            return await PostJsonAsync(BuildUrl(registerEndpoint), request, "Registration failed. Please try again.");
        }

        private async Task<AuthResponse> MockAuthenticateAsync(string emailOrUsername)
        {
            if (mockDelaySeconds > 0f)
            {
                await Task.Delay(Mathf.RoundToInt(mockDelaySeconds * 1000f));
            }

            var username = NameFromLogin(emailOrUsername);
            return AuthResponse.Success(
                $"mock-token-{Guid.NewGuid():N}",
                $"mock-user-{Guid.NewGuid():N}",
                username);
        }

        private async Task<AuthResponse> PostJsonAsync(string url, object payload, string fallback)
        {
            var json = JsonUtility.ToJson(payload);
            using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                await SendWebRequestAsync(req);

                if (req.result != UnityWebRequest.Result.Success)
                {
                    return AuthResponse.Failure(string.IsNullOrWhiteSpace(req.error) ? fallback : req.error);
                }

                try
                {
                    var resp = JsonUtility.FromJson<AuthResponse>(req.downloadHandler.text);
                    if (resp == null)
                    {
                        return AuthResponse.Failure(fallback);
                    }

                    if (!resp.success && string.IsNullOrWhiteSpace(resp.errorMessage))
                    {
                        resp.errorMessage = fallback;
                    }

                    return resp;
                }
                catch (Exception)
                {
                    return AuthResponse.Failure(fallback);
                }
            }
        }

        private static Task SendWebRequestAsync(UnityWebRequest req)
        {
            var tcs = new TaskCompletionSource<bool>();
            var op = req.SendWebRequest();
            op.completed += _ => tcs.TrySetResult(true);
            return tcs.Task;
        }

        private string BuildUrl(string endpoint)
        {
            var trimmedBase = baseApiUrl.TrimEnd('/');
            var trimmedEndpoint = endpoint.StartsWith("/") ? endpoint : $"/{endpoint}";
            return $"{trimmedBase}{trimmedEndpoint}";
        }

        private static string NameFromLogin(string emailOrUsername)
        {
            if (string.IsNullOrWhiteSpace(emailOrUsername))
            {
                return "Player";
            }

            var trimmed = emailOrUsername.Trim();
            var atIndex = trimmed.IndexOf('@');
            return atIndex > 0 ? trimmed.Substring(0, atIndex) : trimmed;
        }
    }
}
