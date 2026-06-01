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

            var username = GetUsernameFromInput(emailOrUsername);
            return AuthResponse.Success(
                $"mock-token-{Guid.NewGuid():N}",
                $"mock-user-{Guid.NewGuid():N}",
                username);
        }

        private async Task<AuthResponse> PostJsonAsync(string url, object payload, string fallbackErrorMessage)
        {
            var json = JsonUtility.ToJson(payload);
            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                await SendWebRequestAsync(request);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    return AuthResponse.Failure(string.IsNullOrWhiteSpace(request.error)
                        ? fallbackErrorMessage
                        : request.error);
                }

                try
                {
                    var response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                    if (response == null)
                    {
                        return AuthResponse.Failure(fallbackErrorMessage);
                    }

                    if (!response.success && string.IsNullOrWhiteSpace(response.errorMessage))
                    {
                        response.errorMessage = fallbackErrorMessage;
                    }

                    return response;
                }
                catch (Exception)
                {
                    return AuthResponse.Failure(fallbackErrorMessage);
                }
            }
        }

        private static Task SendWebRequestAsync(UnityWebRequest request)
        {
            var completion = new TaskCompletionSource<bool>();
            var operation = request.SendWebRequest();
            operation.completed += _ => completion.TrySetResult(true);
            return completion.Task;
        }

        private string BuildUrl(string endpoint)
        {
            var trimmedBase = baseApiUrl.TrimEnd('/');
            var trimmedEndpoint = endpoint.StartsWith("/") ? endpoint : $"/{endpoint}";
            return $"{trimmedBase}{trimmedEndpoint}";
        }

        private static string GetUsernameFromInput(string emailOrUsername)
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
