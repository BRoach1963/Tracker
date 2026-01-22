namespace ProCohere.Avalonia.Services;

/// <summary>
/// Interface for credential storage services.
/// Windows uses Credential Manager, macOS would use Keychain, Linux would use libsecret.
/// </summary>
public interface ICredentialService
{
    /// <summary>
    /// Stores the Supabase session tokens securely.
    /// </summary>
    bool StoreSession(string accessToken, string refreshToken);

    /// <summary>
    /// Stores the Supabase session tokens along with user identity.
    /// </summary>
    /// <param name="accessToken">The access token</param>
    /// <param name="refreshToken">The refresh token</param>
    /// <param name="userEmail">The user's email address</param>
    /// <param name="userId">The user's Supabase ID</param>
    bool StoreSession(string accessToken, string refreshToken, string? userEmail, string? userId);

    /// <summary>
    /// Retrieves stored Supabase session tokens.
    /// </summary>
    (string? AccessToken, string? RefreshToken) GetStoredSession();

    /// <summary>
    /// Gets the stored user identity (email and user ID) if available.
    /// </summary>
    /// <returns>Tuple of (email, userId), both may be null if not stored.</returns>
    (string? Email, string? UserId) GetStoredUserIdentity();

    /// <summary>
    /// Clears stored session tokens.
    /// </summary>
    bool ClearSession();

    /// <summary>
    /// Checks if a stored session exists.
    /// </summary>
    bool HasStoredSession();
}
