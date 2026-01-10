using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.Kudos
{
    /// <summary>
    /// Options for sending kudos.
    /// </summary>
    public class KudosOptions
    {
        /// <summary>Optional title/headline for the kudos.</summary>
        public string? Title { get; set; }

        /// <summary>Link to a specific task.</summary>
        public int? LinkedTaskId { get; set; }

        /// <summary>Link to a specific OKR.</summary>
        public int? LinkedOkrId { get; set; }

        /// <summary>Link to a specific meeting.</summary>
        public int? LinkedMeetingId { get; set; }

        /// <summary>Whether to also post to a team channel.</summary>
        public bool IsPublic { get; set; } = false;

        /// <summary>Whether to show in meeting prep materials.</summary>
        public bool MentionInMeetingPrep { get; set; } = true;

        /// <summary>Schedule for future delivery (UTC).</summary>
        public DateTime? ScheduleFor { get; set; }
    }

    /// <summary>
    /// Service for managing and delivering kudos/recognition.
    /// Orchestrates between the database and various delivery providers.
    /// </summary>
    public class KudosService
    {
        #region Fields

        private readonly ILogger _logger;
        private readonly Dictionary<DeliveryChannel, IKudosDeliveryProvider> _providers;

        #endregion

        #region Singleton

        private static readonly Lazy<KudosService> _instance =
            new(() => new KudosService(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static KudosService Instance => _instance.Value;

        #endregion

        #region Constructor

        private KudosService()
        {
            _logger = LoggingManager.GetComponentLogger("KudosService");

            // Register all delivery providers
            _providers = new Dictionary<DeliveryChannel, IKudosDeliveryProvider>
            {
                { DeliveryChannel.MicrosoftTeams, TeamsDeliveryProvider.Instance },
                { DeliveryChannel.Slack, SlackDeliveryProvider.Instance }
                // Email provider can be added later
            };
        }

        #endregion

        #region Public Methods - Sending Kudos

        /// <summary>
        /// Creates and optionally delivers a kudos to a team member.
        /// </summary>
        public async Task<DataModels.Kudos> SendKudosAsync(
            Guid teamMemberId,
            string message,
            KudosCategory category,
            DeliveryChannel channel,
            KudosOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var teamMember = await TrackerDbManager.Instance.GetTeamMemberByIdAsync(teamMemberId);
            if (teamMember == null)
            {
                throw new ArgumentException($"Team member with ID {teamMemberId} not found.");
            }

            var kudos = new DataModels.Kudos
            {
                UserId = UserSettingsManager.Instance?.CurrentUserId ?? 0,
                TeamMemberId = teamMemberId,
                Message = message,
                Title = options?.Title,
                Category = category,
                DeliveryChannel = channel,
                LinkedTaskId = options?.LinkedTaskId,
                LinkedOkrId = options?.LinkedOkrId,
                LinkedMeetingId = options?.LinkedMeetingId,
                IsPublic = options?.IsPublic ?? false,
                MentionInMeetingPrep = options?.MentionInMeetingPrep ?? true,
                ScheduledFor = options?.ScheduleFor,
                DeliveryStatus = options?.ScheduleFor.HasValue == true
                    ? DeliveryStatus.Scheduled
                    : DeliveryStatus.Sending
            };

            // Save to database first
            await TrackerDbManager.Instance.AddKudosAsync(kudos);
            _logger.Info("Created kudos ID {0} for team member {1}", kudos.Id, teamMember.FullName);

            // Deliver immediately if not scheduled and not internal-only
            if (options?.ScheduleFor.HasValue != true && channel != DeliveryChannel.InternalOnly)
            {
                await DeliverKudosAsync(kudos, teamMember, cancellationToken);
            }
            else if (channel == DeliveryChannel.InternalOnly)
            {
                kudos.DeliveryStatus = DeliveryStatus.Delivered;
                kudos.DeliveredAt = DateTime.UtcNow;
                await TrackerDbManager.Instance.UpdateKudosAsync(kudos);
            }

            return kudos;
        }

        /// <summary>
        /// Delivers a previously created kudos.
        /// </summary>
        public async Task<bool> DeliverKudosAsync(
            DataModels.Kudos kudos,
            TeamMember? teamMember = null,
            CancellationToken cancellationToken = default)
        {
            teamMember ??= await TrackerDbManager.Instance.GetTeamMemberByIdAsync(kudos.TeamMemberId);
            if (teamMember == null)
            {
                kudos.DeliveryStatus = DeliveryStatus.Failed;
                kudos.DeliveryError = "Team member not found.";
                await TrackerDbManager.Instance.UpdateKudosAsync(kudos);
                return false;
            }

            if (!_providers.TryGetValue(kudos.DeliveryChannel, out var provider))
            {
                kudos.DeliveryStatus = DeliveryStatus.Failed;
                kudos.DeliveryError = $"No provider for channel {kudos.DeliveryChannel}";
                await TrackerDbManager.Instance.UpdateKudosAsync(kudos);
                return false;
            }

            if (!provider.IsAvailable)
            {
                kudos.DeliveryStatus = DeliveryStatus.Failed;
                kudos.DeliveryError = $"{provider.DisplayName} is not configured.";
                await TrackerDbManager.Instance.UpdateKudosAsync(kudos);
                return false;
            }

            // Get sender name
            var senderName = UserSettingsManager.Instance?.CurrentUser ?? Environment.UserName;

            _logger.Info("Delivering kudos ID {0} via {1}", kudos.Id, provider.DisplayName);
            var result = await provider.SendKudosAsync(kudos, teamMember, senderName, cancellationToken);

            if (result.Success)
            {
                kudos.DeliveryStatus = DeliveryStatus.Delivered;
                kudos.DeliveredAt = result.DeliveredAt;
                kudos.DeliveryError = null;
                _logger.Info("Kudos ID {0} delivered successfully", kudos.Id);
            }
            else
            {
                kudos.DeliveryStatus = DeliveryStatus.Failed;
                kudos.DeliveryError = result.ErrorMessage;
                _logger.Warn("Kudos ID {0} delivery failed: {1}", kudos.Id, result.ErrorMessage);
            }

            await TrackerDbManager.Instance.UpdateKudosAsync(kudos);
            return result.Success;
        }

        #endregion

        #region Public Methods - Querying

        /// <summary>
        /// Gets all kudos for a specific team member.
        /// </summary>
        public async Task<List<DataModels.Kudos>> GetKudosForTeamMemberAsync(Guid teamMemberId)
        {
            return await TrackerDbManager.Instance.GetKudosForTeamMemberAsync(teamMemberId);
        }

        /// <summary>
        /// Gets all kudos sent by the current user.
        /// </summary>
        public async Task<List<DataModels.Kudos>> GetAllKudosAsync()
        {
            return await TrackerDbManager.Instance.GetAllKudosAsync();
        }

        /// <summary>
        /// Gets kudos that should be mentioned in meeting prep for a team member.
        /// </summary>
        public async Task<List<DataModels.Kudos>> GetRecentKudosForMeetingPrepAsync(
            Guid teamMemberId,
            int daysSince = 30)
        {
            return await TrackerDbManager.Instance.GetRecentKudosForMeetingPrepAsync(teamMemberId, daysSince);
        }

        /// <summary>
        /// Gets statistics about kudos sent to each team member.
        /// </summary>
        public async Task<List<KudosStats>> GetKudosStatsAsync()
        {
            var teamMembers = await TrackerDataManager.Instance.GetTeamData();
            var allKudos = await GetAllKudosAsync();

            return teamMembers.Select(tm => new KudosStats
            {
                TeamMemberId = tm.Id,
                TeamMemberName = tm.FullName,
                TotalKudosCount = allKudos.Count(k => k.TeamMemberId == tm.Id),
                LastKudosDate = allKudos
                    .Where(k => k.TeamMemberId == tm.Id)
                    .OrderByDescending(k => k.CreatedAt)
                    .FirstOrDefault()?.CreatedAt,
                ByCategory = allKudos
                    .Where(k => k.TeamMemberId == tm.Id)
                    .GroupBy(k => k.Category)
                    .ToDictionary(g => g.Key, g => g.Count())
            }).ToList();
        }

        /// <summary>
        /// Gets team members who haven't received kudos recently.
        /// </summary>
        public async Task<List<TeamMember>> GetUnderrecognizedTeamMembersAsync(int dayThreshold = 30)
        {
            var stats = await GetKudosStatsAsync();
            var cutoffDate = DateTime.UtcNow.AddDays(-dayThreshold);

            var underrecognizedIds = stats
                .Where(s => s.TotalKudosCount == 0 || (s.LastKudosDate.HasValue && s.LastKudosDate.Value < cutoffDate))
                .OrderByDescending(s => s.DaysSinceLastKudos)
                .Select(s => s.TeamMemberId)
                .ToList();

            var allTeamMembers = await TrackerDataManager.Instance.GetTeamData();
            return allTeamMembers.Where(tm => underrecognizedIds.Contains(tm.Id)).ToList();
        }

        #endregion

        #region Public Methods - Provider Management

        /// <summary>
        /// Gets all available delivery providers.
        /// </summary>
        public IEnumerable<IKudosDeliveryProvider> GetAvailableProviders()
        {
            return _providers.Values.Where(p => p.IsAvailable);
        }

        /// <summary>
        /// Gets a specific provider by channel.
        /// </summary>
        public IKudosDeliveryProvider? GetProvider(DeliveryChannel channel)
        {
            return _providers.TryGetValue(channel, out var provider) ? provider : null;
        }

        /// <summary>
        /// Tests all configured providers.
        /// </summary>
        public async Task<Dictionary<DeliveryChannel, bool>> TestAllProvidersAsync(
            CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<DeliveryChannel, bool>();

            foreach (var (channel, provider) in _providers)
            {
                if (provider.IsAvailable)
                {
                    results[channel] = await provider.TestConnectionAsync(cancellationToken);
                }
                else
                {
                    results[channel] = false;
                }
            }

            return results;
        }

        #endregion
    }

    /// <summary>
    /// Statistics about kudos for a team member.
    /// </summary>
    public class KudosStats
    {
        public Guid TeamMemberId { get; set; }
        public string TeamMemberName { get; set; } = string.Empty;
        public int TotalKudosCount { get; set; }
        public DateTime? LastKudosDate { get; set; }
        public Dictionary<KudosCategory, int> ByCategory { get; set; } = new();

        public int DaysSinceLastKudos => LastKudosDate.HasValue
            ? (int)(DateTime.UtcNow - LastKudosDate.Value).TotalDays
            : int.MaxValue;
    }
}
