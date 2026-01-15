using Tracker.DataModels;

namespace Tracker.Services.Kudos
{
    /// <summary>
    /// Result of a kudos delivery attempt.
    /// </summary>
    public class KudosDeliveryResult
    {
        /// <summary>
        /// Whether the delivery was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if delivery failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// When the delivery completed.
        /// </summary>
        public DateTime DeliveredAt { get; set; }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        public static KudosDeliveryResult Succeeded() => new()
        {
            Success = true,
            DeliveredAt = DateTime.UtcNow
        };

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        public static KudosDeliveryResult Failed(string error) => new()
        {
            Success = false,
            ErrorMessage = error,
            DeliveredAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Interface for kudos delivery providers.
    /// Each provider implements delivery to a specific channel (Teams, Slack, Email).
    /// </summary>
    public interface IKudosDeliveryProvider
    {
        /// <summary>
        /// Gets the delivery channel this provider handles.
        /// </summary>
        Common.Enums.DeliveryChannel Channel { get; }

        /// <summary>
        /// Gets whether this provider is currently available/configured.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Gets the display name for this provider.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Sends kudos to a team member.
        /// </summary>
        /// <param name="kudos">The kudos to deliver.</param>
        /// <param name="teamMember">The team member to receive the kudos.</param>
        /// <param name="senderName">Name of the manager sending the kudos.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result of the delivery attempt.</returns>
        Task<KudosDeliveryResult> SendKudosAsync(
            DataModels.Kudos kudos,
            TeamMember teamMember,
            string senderName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tests connectivity to the delivery channel.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if connection is working.</returns>
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets setup instructions for this provider.
        /// </summary>
        string GetSetupInstructions();
    }
}
