using System;

namespace DontDiePlease.Auth
{
    [Serializable]
    public class LoginRequest
    {
        public string emailOrUsername;
        public string password;
    }

    [Serializable]
    public class RegisterRequest
    {
        public string emailOrUsername;
        public string password;
    }

    [Serializable]
    public class AuthResponse
    {
        public bool success;
        public string token;
        public string userId;
        public string username;
        public string errorMessage;

        public static AuthResponse Success(string token, string userId, string username)
        {
            return new AuthResponse
            {
                success = true,
                token = token,
                userId = userId,
                username = username,
                errorMessage = string.Empty
            };
        }

        public static AuthResponse Failure(string message)
        {
            return new AuthResponse
            {
                success = false,
                token = string.Empty,
                userId = string.Empty,
                username = string.Empty,
                errorMessage = message
            };
        }
    }

    public class AuthSession
    {
        public bool IsAuthenticated { get; private set; }
        public string Token { get; private set; }
        public string UserId { get; private set; }
        public string Username { get; private set; }

        public void SetSession(string token, string userId, string username)
        {
            IsAuthenticated = !string.IsNullOrWhiteSpace(token);
            Token = token;
            UserId = userId;
            Username = username;
        }

        public void Clear()
        {
            IsAuthenticated = false;
            Token = string.Empty;
            UserId = string.Empty;
            Username = string.Empty;
        }
    }
}
