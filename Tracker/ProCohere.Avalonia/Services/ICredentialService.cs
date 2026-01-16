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
    /// Retrieves stored Supabase session tokens.
    /// </summary>
    (string? AccessToken, string? RefreshToken) GetStoredSession();

    /// <summary>
    /// Clears stored session tokens.
    /// </summary>
    bool ClearSession();

    /// <summary>
    /// Checks if a stored session exists.
    /// </summary>
    bool HasStoredSession();
}
