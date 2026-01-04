using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Tracker.Services.Backend
{
    /// <summary>
    /// Securely stores authentication tokens and credentials using Windows Data Protection API.
    /// Data is encrypted and can only be decrypted by the same Windows user.
    /// </summary>
    public static class SecureTokenStorage
    {
        private static readonly string TokenDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Tracker", "auth");

        private static readonly string RefreshTokenFile = Path.Combine(TokenDirectory, "rt.dat");
        private static readonly string PasswordFile = Path.Combine(TokenDirectory, "pwd.dat");
        private static readonly string SlackTokenFile = Path.Combine(TokenDirectory, "slack.dat");
        private static readonly string AccessTokenFile = Path.Combine(TokenDirectory, "at.dat");

        #region Refresh Token

        /// <summary>
        /// Saves the refresh token securely.
        /// </summary>
        public static void SaveRefreshToken(string token)
        {
            SaveEncrypted(RefreshTokenFile, token);
        }

        /// <summary>
        /// Gets the stored refresh token.
        /// </summary>
        public static string? GetRefreshToken()
        {
            return GetEncrypted(RefreshTokenFile);
        }

        #endregion

        #region Password (Remember Me)

        /// <summary>
        /// Saves the password securely for "Remember Me" feature.
        /// </summary>
        public static void SavePassword(string password)
        {
            SaveEncrypted(PasswordFile, password);
        }

        /// <summary>
        /// Gets the saved password if "Remember Me" was enabled.
        /// </summary>
        public static string? GetSavedPassword()
        {
            return GetEncrypted(PasswordFile);
        }

        /// <summary>
        /// Clears the saved password.
        /// </summary>
        public static void ClearPassword()
        {
            try
            {
                if (File.Exists(PasswordFile))
                    File.Delete(PasswordFile);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        #endregion

        #region Slack Token

        /// <summary>
        /// Saves the Slack user access token securely.
        /// </summary>
        public static void SaveSlackUserToken(string token)
        {
            SaveEncrypted(SlackTokenFile, token);
        }

        /// <summary>
        /// Gets the stored Slack user access token.
        /// </summary>
        public static string? GetSlackUserToken()
        {
            return GetEncrypted(SlackTokenFile);
        }

        /// <summary>
        /// Clears the Slack token.
        /// </summary>
        public static void ClearSlackToken()
        {
            try
            {
                if (File.Exists(SlackTokenFile))
                    File.Delete(SlackTokenFile);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        #endregion

        #region Access Token (JWT)

        /// <summary>
        /// Saves the JWT access token securely for session restore.
        /// </summary>
        public static void SaveAccessToken(string token)
        {
            SaveEncrypted(AccessTokenFile, token);
        }

        /// <summary>
        /// Gets the stored JWT access token.
        /// </summary>
        public static string? GetAccessToken()
        {
            return GetEncrypted(AccessTokenFile);
        }

        /// <summary>
        /// Clears the access token.
        /// </summary>
        public static void ClearAccessToken()
        {
            try
            {
                if (File.Exists(AccessTokenFile))
                    File.Delete(AccessTokenFile);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        #endregion

        #region Common Methods

        /// <summary>
        /// Clears all stored tokens and credentials.
        /// </summary>
        public static void ClearTokens()
        {
            try
            {
                if (File.Exists(RefreshTokenFile))
                    File.Delete(RefreshTokenFile);
                if (File.Exists(PasswordFile))
                    File.Delete(PasswordFile);
                if (File.Exists(SlackTokenFile))
                    File.Delete(SlackTokenFile);
                if (File.Exists(AccessTokenFile))
                    File.Delete(AccessTokenFile);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        /// <summary>
        /// Checks if there are stored credentials.
        /// </summary>
        public static bool HasStoredCredentials()
        {
            return File.Exists(RefreshTokenFile) || File.Exists(PasswordFile);
        }

        private static void SaveEncrypted(string filePath, string data)
        {
            try
            {
                Directory.CreateDirectory(TokenDirectory);

                var plainBytes = Encoding.UTF8.GetBytes(data);
                var encryptedBytes = ProtectedData.Protect(
                    plainBytes,
                    null,
                    DataProtectionScope.CurrentUser);

                File.WriteAllBytes(filePath, encryptedBytes);
            }
            catch
            {
                // Silently fail - worst case user has to log in again
            }
        }

        private static string? GetEncrypted(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                var encryptedBytes = File.ReadAllBytes(filePath);
                var plainBytes = ProtectedData.Unprotect(
                    encryptedBytes,
                    null,
                    DataProtectionScope.CurrentUser);

                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                // Data corrupted or from different user
                return null;
            }
        }

        #endregion
    }
}

