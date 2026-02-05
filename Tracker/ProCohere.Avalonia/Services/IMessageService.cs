using System.Threading.Tasks;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Abstraction for messaging services (Slack, Teams, etc.)
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// Name of the messaging provider (e.g., "Slack", "Teams")
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Whether this service is configured and ready to use.
    /// </summary>
    Task<bool> IsConfiguredAsync();

    /// <summary>
    /// Sends a message to a recipient.
    /// </summary>
    /// <param name="recipientEmail">Email address of the recipient</param>
    /// <param name="message">Message content</param>
    /// <returns>True if sent successfully, false otherwise</returns>
    Task<bool> SendMessageAsync(string recipientEmail, string message);
}
