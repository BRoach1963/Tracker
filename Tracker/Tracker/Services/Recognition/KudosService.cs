using Microsoft.Extensions.Logging;
using Tracker.Classes;
using Tracker.Services.Data;
using Tracker.Services.Data.Repositories;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;
using MsLogging = Microsoft.Extensions.Logging;

namespace Tracker.Services.Recognition
{
    /// <summary>
    /// Options for creating kudos/recognition.
    /// </summary>
    public class KudosOptions
    {
        /// <summary>Optional title/headline for the kudos.</summary>
        public string? Title { get; set; }

        /// <summary>Badge type (team_player, innovator, customer_focus, leader, mentor, etc.).</summary>
        public string? BadgeType { get; set; }

        /// <summary>Company values this recognition acknowledges.</summary>
        public List<string>? CompanyValues { get; set; }

        /// <summary>Whether this recognition is public to the organization.</summary>
        public bool IsPublic { get; set; } = true;
    }

    /// <summary>
    /// Service for managing kudos/recognition between team members.
    /// Handles CRUD operations and statistics for recognition.
    /// </summary>
    public class KudosService
    {
        #region Fields

        private readonly Logging.ILogger _logger;

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
        }
        
        private static KudosRepository CreateKudosRepository()
        {
            var factory = DapperConnectionFactory.Instance;
            var loggerFactory = MsLogging.LoggerFactory.Create(builder => { });
            return new KudosRepository(factory, loggerFactory.CreateLogger<KudosRepository>());
        }
        
        private static TeamMemberRepository CreateTeamMemberRepository()
        {
            var factory = DapperConnectionFactory.Instance;
            var loggerFactory = MsLogging.LoggerFactory.Create(builder => { });
            return new TeamMemberRepository(factory, loggerFactory.CreateLogger<TeamMemberRepository>());
        }
        
        private static async Task<TeamMember?> GetTeamMemberByIdAsync(Guid teamMemberId)
        {
            var repository = CreateTeamMemberRepository();
            return await repository.GetByIdAsync(teamMemberId);
        }

        #endregion

        #region Public Methods - Creating Kudos

        /// <summary>
        /// Creates kudos/recognition from one team member to another.
        /// </summary>
        /// <param name="fromTeamMemberId">The team member giving the recognition.</param>
        /// <param name="toTeamMemberId">The team member receiving the recognition.</param>
        /// <param name="message">The recognition message.</param>
        /// <param name="options">Optional settings for the kudos.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created kudos.</returns>
        public async Task<DataModels.Kudos?> CreateKudosAsync(
            Guid fromTeamMemberId,
            Guid toTeamMemberId,
            string message,
            KudosOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var repository = CreateKudosRepository();
            if (repository == null)
            {
                _logger.Warn("CreateKudosAsync: Could not create repository - no user context");
                return null;
            }

            var toTeamMember = await GetTeamMemberByIdAsync(toTeamMemberId);
            if (toTeamMember == null)
            {
                _logger.Warn("CreateKudosAsync: Team member {0} not found", toTeamMemberId);
                return null;
            }

            var orgId = OrganizationContext.Current.OrganizationIdOrNull;
            if (!orgId.HasValue)
            {
                _logger.Warn("CreateKudosAsync: No organization context");
                return null;
            }

            var kudos = new DataModels.Kudos
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId.Value,
                FromTeamMemberId = fromTeamMemberId,
                ToTeamMemberId = toTeamMemberId,
                Title = options?.Title ?? string.Empty,
                Message = message,
                BadgeType = options?.BadgeType,
                CompanyValues = options?.CompanyValues,
                IsPublic = options?.IsPublic ?? true,
                ReactionsCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var id = await repository.AddKudosAsync(kudos);
            if (id == Guid.Empty)
            {
                _logger.Error("CreateKudosAsync: Failed to add kudos to database");
                return null;
            }

            _logger.Info("Created kudos ID {0} from {1} to {2}", kudos.Id, fromTeamMemberId, toTeamMember.FullName);
            return kudos;
        }

        /// <summary>
        /// Updates existing kudos.
        /// </summary>
        public async Task<bool> UpdateKudosAsync(DataModels.Kudos kudos)
        {
            var repository = CreateKudosRepository();
            if (repository == null) return false;

            kudos.UpdatedAt = DateTime.UtcNow;
            await repository.UpdateKudosAsync(kudos);
            return true;
        }

        /// <summary>
        /// Deletes kudos by ID.
        /// </summary>
        public async Task<bool> DeleteKudosAsync(Guid kudosId)
        {
            var repository = CreateKudosRepository();
            if (repository == null) return false;

            await repository.DeleteKudosAsync(kudosId);
            return true;
        }

        #endregion

        #region Public Methods - Querying

        /// <summary>
        /// Gets all kudos received by a specific team member.
        /// </summary>
        public async Task<List<DataModels.Kudos>> GetKudosForTeamMemberAsync(Guid teamMemberId)
        {
            var repository = CreateKudosRepository();
            if (repository == null) return new List<DataModels.Kudos>();

            return (await repository.GetKudosToAsync(teamMemberId)).ToList();
        }

        /// <summary>
        /// Gets all kudos given by a specific team member.
        /// </summary>
        public async Task<List<DataModels.Kudos>> GetKudosFromTeamMemberAsync(Guid teamMemberId)
        {
            var repository = CreateKudosRepository();
            if (repository == null) return new List<DataModels.Kudos>();

            return (await repository.GetKudosFromAsync(teamMemberId)).ToList();
        }

        /// <summary>
        /// Gets all kudos in the organization.
        /// </summary>
        public async Task<List<DataModels.Kudos>> GetAllKudosAsync()
        {
            var repository = CreateKudosRepository();
            if (repository == null) return new List<DataModels.Kudos>();

            return (await repository.GetKudosAsync()).ToList();
        }

        /// <summary>
        /// Gets public kudos only.
        /// </summary>
        public async Task<List<DataModels.Kudos>> GetPublicKudosAsync()
        {
            var repository = CreateKudosRepository();
            if (repository == null) return new List<DataModels.Kudos>();

            return (await repository.GetPublicKudosAsync()).ToList();
        }

        /// <summary>
        /// Gets recent kudos for a team member within a time period.
        /// </summary>
        public async Task<List<DataModels.Kudos>> GetRecentKudosForTeamMemberAsync(
            Guid teamMemberId,
            int daysSince = 30)
        {
            var repository = CreateKudosRepository();
            if (repository == null) return new List<DataModels.Kudos>();

            var recent = await repository.GetRecentKudosAsync(daysSince);
            return recent.Where(k => k.ToTeamMemberId == teamMemberId).ToList();
        }

        /// <summary>
        /// Gets kudos by badge type.
        /// </summary>
        public async Task<List<DataModels.Kudos>> GetKudosByBadgeTypeAsync(string? badgeType)
        {
            var repository = CreateKudosRepository();
            if (repository == null) return new List<DataModels.Kudos>();

            return (await repository.GetKudosByBadgeTypeAsync(badgeType!)).ToList();
        }

        /// <summary>
        /// Gets statistics about kudos received by each team member.
        /// </summary>
        public async Task<List<KudosStats>> GetKudosStatsAsync()
        {
            var teamMembers = await TrackerDataManager.Instance.GetTeamData();
            var allKudos = await GetAllKudosAsync();

            return teamMembers.Select(tm => new KudosStats
            {
                TeamMemberId = tm.Id,
                TeamMemberName = tm.FullName,
                TotalKudosReceived = allKudos.Count(k => k.ToTeamMemberId == tm.Id),
                TotalKudosGiven = allKudos.Count(k => k.FromTeamMemberId == tm.Id),
                LastKudosDate = allKudos
                    .Where(k => k.ToTeamMemberId == tm.Id)
                    .OrderByDescending(k => k.CreatedAt)
                    .FirstOrDefault()?.CreatedAt,
                ByBadgeType = allKudos
                    .Where(k => k.ToTeamMemberId == tm.Id && !string.IsNullOrEmpty(k.BadgeType))
                    .GroupBy(k => k.BadgeType!)
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
                .Where(s => s.TotalKudosReceived == 0 || (s.LastKudosDate.HasValue && s.LastKudosDate.Value < cutoffDate))
                .OrderByDescending(s => s.DaysSinceLastKudos)
                .Select(s => s.TeamMemberId)
                .ToList();

            var allTeamMembers = await TrackerDataManager.Instance.GetTeamData();
            return allTeamMembers.Where(tm => underrecognizedIds.Contains(tm.Id)).ToList();
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
        public int TotalKudosReceived { get; set; }
        public int TotalKudosGiven { get; set; }
        public DateTime? LastKudosDate { get; set; }
        public Dictionary<string, int> ByBadgeType { get; set; } = new();

        public int DaysSinceLastKudos => LastKudosDate.HasValue
            ? (int)(DateTime.UtcNow - LastKudosDate.Value).TotalDays
            : int.MaxValue;
    }
}
