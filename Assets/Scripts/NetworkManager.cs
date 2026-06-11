using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace DontDiePlease.Networking
{
    public sealed class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [SerializeField] private string baseApiUrl = "http://127.0.0.1:5000/api/v1";
        [SerializeField] private int requestTimeoutSeconds = 15;

        private string jwtToken = string.Empty;

        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(jwtToken);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public async Task<ApiResult<AuthSession>> RegisterUser(string username, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return ApiResult<AuthSession>.Fail(400, "Username, email, and password are required");
            }

            var payload = new RegisterPayload
            {
                username = username.Trim(),
                email = email.Trim().ToLowerInvariant(),
                password = password
            };

            return await Authenticate("/auth/register", payload);
        }

        public async Task<ApiResult<AuthSession>> LoginUser(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return ApiResult<AuthSession>.Fail(400, "Email and password are required");
            }

            var payload = new LoginPayload
            {
                email = email.Trim().ToLowerInvariant(),
                password = password
            };

            return await Authenticate("/auth/login", payload);
        }

        public void ClearSession()
        {
            jwtToken = string.Empty;
        }

        public async Task<ApiResult<TResponse>> GetAuthenticatedJson<TResponse>(string endpoint) where TResponse : class
        {
            if (!IsAuthenticated)
            {
                return ApiResult<TResponse>.Fail(401, "Login is required");
            }

            using (var request = UnityWebRequest.Get(BuildUrl(endpoint)))
            {
                ApplyCommonHeaders(request, true);
                return await SendForJson<TResponse>(request, "Request failed");
            }
        }

        public async Task<ApiResult<TResponse>> PostAuthenticatedJson<TPayload, TResponse>(string endpoint, TPayload payload) where TResponse : class
        {
            if (!IsAuthenticated)
            {
                return ApiResult<TResponse>.Fail(401, "Login is required");
            }

            return await SendJsonBody<TPayload, TResponse>(UnityWebRequest.kHttpVerbPOST, endpoint, payload, true, "Request failed");
        }

        public async Task<ApiResult<TResponse>> PutAuthenticatedJson<TPayload, TResponse>(string endpoint, TPayload payload) where TResponse : class
        {
            if (!IsAuthenticated)
            {
                return ApiResult<TResponse>.Fail(401, "Login is required");
            }

            return await SendJsonBody<TPayload, TResponse>(UnityWebRequest.kHttpVerbPUT, endpoint, payload, true, "Request failed");
        }

        private async Task<ApiResult<AuthSession>> Authenticate<TPayload>(string endpoint, TPayload payload)
        {
            var result = await SendJsonBody<TPayload, AuthEnvelope>(UnityWebRequest.kHttpVerbPOST, endpoint, payload, false, "Authentication failed");

            if (!result.Success)
            {
                return ApiResult<AuthSession>.Fail(result.StatusCode, result.Error);
            }

            var envelope = result.Data;

            if (envelope == null || !envelope.success || string.IsNullOrWhiteSpace(envelope.token))
            {
                return ApiResult<AuthSession>.Fail(result.StatusCode, "Authentication response was invalid");
            }

            jwtToken = envelope.token;

            var session = new AuthSession
            {
                userId = envelope.user != null ? envelope.user.id : string.Empty,
                username = envelope.user != null ? envelope.user.username : string.Empty,
                email = envelope.user != null ? envelope.user.email : string.Empty
            };

            return ApiResult<AuthSession>.Ok(result.StatusCode, session);
        }

        private async Task<ApiResult<TResponse>> SendJsonBody<TPayload, TResponse>(string method, string endpoint, TPayload payload, bool requiresAuth, string fallbackError) where TResponse : class
        {
            var json = JsonUtility.ToJson(payload);
            var body = Encoding.UTF8.GetBytes(json);

            using (var request = new UnityWebRequest(BuildUrl(endpoint), method))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                ApplyCommonHeaders(request, requiresAuth);
                return await SendForJson<TResponse>(request, fallbackError);
            }
        }

        private async Task<ApiResult<TResponse>> SendForJson<TResponse>(UnityWebRequest request, string fallbackError) where TResponse : class
        {
            request.timeout = Mathf.Max(1, requestTimeoutSeconds);

            await Send(request);

            if (!IsSuccessStatusCode(request.responseCode) || request.result != UnityWebRequest.Result.Success)
            {
                if (request.responseCode == 401)
                {
                    ClearSession();
                }

                return ApiResult<TResponse>.Fail(request.responseCode, ResolveErrorMessage(request, fallbackError));
            }

            var responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

            if (!TryParseJson(responseText, out TResponse response))
            {
                return ApiResult<TResponse>.Fail(request.responseCode, "Response could not be parsed");
            }

            return ApiResult<TResponse>.Ok(request.responseCode, response);
        }

        private void ApplyCommonHeaders(UnityWebRequest request, bool requiresAuth)
        {
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Content-Type", "application/json");

            if (requiresAuth)
            {
                request.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
            }
        }

        private string BuildUrl(string endpoint)
        {
            var trimmedBase = baseApiUrl.TrimEnd('/');
            var trimmedEndpoint = endpoint.StartsWith("/") ? endpoint : $"/{endpoint}";
            return $"{trimmedBase}{trimmedEndpoint}";
        }

        private static bool IsSuccessStatusCode(long statusCode)
        {
            return statusCode >= 200 && statusCode <= 299;
        }

        private static Task Send(UnityWebRequest request)
        {
            var completion = new TaskCompletionSource<bool>();
            var operation = request.SendWebRequest();
            operation.completed += _ => completion.TrySetResult(true);
            return completion.Task;
        }

        private static string ResolveErrorMessage(UnityWebRequest request, string fallbackError)
        {
            var responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

            if (TryParseJson(responseText, out ErrorEnvelope errorEnvelope))
            {
                var serverMessage = FirstFilled(errorEnvelope.error, errorEnvelope.errorMessage, errorEnvelope.message);

                if (!string.IsNullOrWhiteSpace(serverMessage))
                {
                    return serverMessage;
                }
            }

            if (!string.IsNullOrWhiteSpace(request.error))
            {
                return request.error;
            }

            return fallbackError;
        }

        private static string FirstFilled(params string[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static bool TryParseJson<T>(string json, out T value) where T : class
        {
            value = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                value = JsonUtility.FromJson<T>(json);
                return value != null;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    [Serializable]
    public sealed class ApiResult<T> where T : class
    {
        public bool Success { get; private set; }
        public long StatusCode { get; private set; }
        public string Error { get; private set; }
        public T Data { get; private set; }

        public static ApiResult<T> Ok(long statusCode, T data)
        {
            return new ApiResult<T>
            {
                Success = true,
                StatusCode = statusCode,
                Error = string.Empty,
                Data = data
            };
        }

        public static ApiResult<T> Fail(long statusCode, string error)
        {
            return new ApiResult<T>
            {
                Success = false,
                StatusCode = statusCode,
                Error = string.IsNullOrWhiteSpace(error) ? "Request failed" : error,
                Data = null
            };
        }
    }

    [Serializable]
    public sealed class AuthSession
    {
        public string userId;
        public string username;
        public string email;
    }

    [Serializable]
    internal sealed class RegisterPayload
    {
        public string username;
        public string email;
        public string password;
    }

    [Serializable]
    internal sealed class LoginPayload
    {
        public string email;
        public string password;
    }

    [Serializable]
    internal sealed class AuthEnvelope
    {
        public bool success;
        public string token;
        public AuthUser user;
    }

    [Serializable]
    internal sealed class AuthUser
    {
        public string id;
        public string username;
        public string email;
    }

    [Serializable]
    internal sealed class ErrorEnvelope
    {
        public bool success;
        public string error;
        public string errorMessage;
        public string message;
    }
}
