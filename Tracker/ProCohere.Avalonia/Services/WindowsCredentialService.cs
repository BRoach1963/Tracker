using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for securely storing Supabase session tokens using Windows DPAPI.
/// Tokens are encrypted and isolated per Windows user account.
/// Only the same Windows user can decrypt the data.
/// </summary>
public class WindowsCredentialService : ICredentialService
{
    private static readonly string SessionFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere",
        "session.protected");

    /// <summary>
    /// Stores the Supabase session tokens securely using DPAPI encryption.
    /// </summary>
    public bool StoreSession(string accessToken, string refreshToken)
    {
        return StoreSession(accessToken, refreshToken, null, null);
    }

    /// <summary>
    /// Stores the Supabase session tokens along with user identity using DPAPI encryption.
    /// </summary>
    public bool StoreSession(string accessToken, string refreshToken, string? userEmail, string? userId)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return false;
        }

        try
        {
            var sessionData = new SessionData
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                UserEmail = userEmail,
                UserId = userId,
                StoredAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(sessionData);
            var plainBytes = Encoding.UTF8.GetBytes(json);
            
            // Encrypt using DPAPI - only current Windows user can decrypt
            var encryptedBytes = ProtectedData.Protect(
                plainBytes, 
                null, 
                DataProtectionScope.CurrentUser);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(SessionFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(SessionFilePath, encryptedBytes);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to store session: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Retrieves stored Supabase session tokens, decrypting with DPAPI.
    /// </summary>
    public (string? AccessToken, string? RefreshToken) GetStoredSession()
    {
        var sessionData = GetStoredSessionData();
        return sessionData != null 
            ? (sessionData.AccessToken, sessionData.RefreshToken) 
            : (null, null);
    }

    /// <summary>
    /// Gets the stored user identity (email and user ID) if available.
    /// </summary>
    public (string? Email, string? UserId) GetStoredUserIdentity()
    {
        var sessionData = GetStoredSessionData();
        return sessionData != null 
            ? (sessionData.UserEmail, sessionData.UserId) 
            : (null, null);
    }

    /// <summary>
    /// Internal method to retrieve and decrypt the full session data.
    /// </summary>
    private SessionData? GetStoredSessionData()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return null;
        }

        try
        {
            if (!File.Exists(SessionFilePath))
            {
                return null;
            }

            var encryptedBytes = File.ReadAllBytes(SessionFilePath);
            
            // Decrypt using DPAPI
            var plainBytes = ProtectedData.Unprotect(
                encryptedBytes, 
                null, 
                DataProtectionScope.CurrentUser);

            var json = Encoding.UTF8.GetString(plainBytes);
            return JsonSerializer.Deserialize<SessionData>(json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to retrieve session: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Clears stored session tokens.
    /// </summary>
    public bool ClearSession()
    {
        try
        {
            if (File.Exists(SessionFilePath))
            {
                File.Delete(SessionFilePath);
            }
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to clear session: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Checks if a stored session exists.
    /// </summary>
    public bool HasStoredSession()
    {
        if (!File.Exists(SessionFilePath))
        {
            return false;
        }

        var (access, refresh) = GetStoredSession();
        return !string.IsNullOrEmpty(refresh);
    }

    /// <summary>
    /// Internal class for JSON serialization of session data.
    /// </summary>
    private class SessionData
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? UserEmail { get; set; }
        public string? UserId { get; set; }
        public DateTime StoredAt { get; set; }
    }
}
