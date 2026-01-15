using Dapper;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services;
using Tracker.Services.Data;

namespace Tracker.Services.Analytics
{
    /// <summary>
    /// Service for capturing and retrieving progress snapshots for trajectory analysis.
    /// 
    /// Snapshots are captured daily on app startup (if >24h since last snapshot).
    /// This data enables predictive analytics including:
    /// - Velocity calculations (progress per day)
    /// - Trajectory projections (will we hit the target?)
    /// - Confidence intervals (how reliable is the prediction?)
    /// - Trend visualization (charts showing progress over time)
    /// </summary>
    public class ProgressSnapshotService
    {
        #region Fields

        private readonly ILogger _logger;
        private static readonly object _lock = new();

        #endregion

        #region Singleton

        private static ProgressSnapshotService? _instance;

        public static ProgressSnapshotService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new ProgressSnapshotService();
                    }
                }
                return _instance;
            }
        }

        private ProgressSnapshotService()
        {
            _logger = LoggingManager.GetComponentLogger("ProgressSnapshotService");
        }

        #endregion

        #region Public Methods - Snapshot Capture

        /// <summary>
        /// Captures snapshots for all trackable entities if needed.
        /// Should be called on app startup. Only captures if >24h since last snapshot.
        /// 
        /// DISABLED: Snapshot capture will be refactored to the unified
        /// goals/targets/projects/tasks model in a future iteration.
        /// </summary>
        public async Task CaptureSnapshotsIfNeededAsync(CancellationToken cancellationToken = default)
        {
            _logger.Warn("ProgressSnapshotService.CaptureSnapshotsIfNeededAsync DISABLED - snapshot capture refactor pending");
            // Snapshot capture disabled pending unified goal/target/project/task tracking
            await Task.CompletedTask;
        }

        /// <summary>
        /// Forces a snapshot capture regardless of last capture time.
        /// Useful for testing or manual refresh.
        /// 
        /// DISABLED: Snapshot capture will be refactored to the unified
        /// goals/targets/projects/tasks model in a future iteration.
        /// </summary>
        public async Task ForceCaptureSnapshotsAsync(CancellationToken cancellationToken = default)
        {
            _logger.Warn("ProgressSnapshotService.ForceCaptureSnapshotsAsync DISABLED - snapshot capture refactor pending");
            // Snapshot capture disabled pending unified goal/target/project/task tracking
            await Task.CompletedTask;
        }

        #endregion

        #region Public Methods - Snapshot Retrieval

        /// <summary>
        /// Gets historical snapshots for a specific entity.
        /// </summary>
        /// <param name="entityType">The entity type (use SnapshotEntityType constants).</param>
        /// <param name="entityId">The entity ID (Guid).</param>
        /// <param name="days">Number of days of history to retrieve (default 90).</param>
        /// <returns>List of snapshots ordered by date ascending.</returns>
        public async Task<List<ProgressSnapshot>> GetHistoryAsync(
            SnapshotEntityType entityType, 
            Guid entityId, 
            int days = 90)
        {
            var orgId = OrganizationContext.Current.OrganizationIdOrNull;
            if (!orgId.HasValue)
                return new List<ProgressSnapshot>();

            var cutoffDate = DateTime.Today.AddDays(-days);

            try
            {
                var connectionFactory = new DapperConnectionFactory();
                using var connection = connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM progress_snapshots
                    WHERE organization_id = @OrgId
                      AND entity_type = @EntityType
                      AND entity_id = @EntityId
                      AND snapshot_date >= @CutoffDate
                    ORDER BY snapshot_date";
                
                var results = await connection.QueryAsync<ProgressSnapshot>(sql, new 
                { 
                    OrgId = orgId.Value, 
                    EntityType = entityType.ToString().ToLowerInvariant(),
                    EntityId = entityId,
                    CutoffDate = cutoffDate
                });
                return results.ToList();
            }
            catch (Exception ex)
            {
                _logger.Error("Error retrieving history for {0}/{1}: {2}", entityType, entityId, ex.Message);
                return new List<ProgressSnapshot>();
            }
        }

        /// <summary>
        /// Gets the most recent snapshot for an entity.
        /// </summary>
        public async Task<ProgressSnapshot?> GetLatestSnapshotAsync(SnapshotEntityType entityType, Guid entityId)
        {
            var orgId = OrganizationContext.Current.OrganizationIdOrNull;
            if (!orgId.HasValue)
                return null;

            try
            {
                var connectionFactory = new DapperConnectionFactory();
                using var connection = connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM progress_snapshots
                    WHERE organization_id = @OrgId
                      AND entity_type = @EntityType
                      AND entity_id = @EntityId
                    ORDER BY snapshot_date DESC
                    LIMIT 1";
                
                return await connection.QueryFirstOrDefaultAsync<ProgressSnapshot>(sql, new 
                { 
                    OrgId = orgId.Value, 
                    EntityType = entityType.ToString().ToLowerInvariant(),
                    EntityId = entityId
                });
            }
            catch (Exception ex)
            {
                _logger.Error("Error retrieving latest snapshot for {0}/{1}: {2}", entityType, entityId, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Gets all snapshots for a specific date across all entities.
        /// </summary>
        public async Task<List<ProgressSnapshot>> GetSnapshotsForDateAsync(DateTime date)
        {
            var orgId = OrganizationContext.Current.OrganizationIdOrNull;
            if (!orgId.HasValue)
                return new List<ProgressSnapshot>();

            try
            {
                var connectionFactory = new DapperConnectionFactory();
                using var connection = connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM progress_snapshots
                    WHERE organization_id = @OrgId
                      AND snapshot_date = @Date";
                
                var results = await connection.QueryAsync<ProgressSnapshot>(sql, new 
                { 
                    OrgId = orgId.Value, 
                    Date = date.Date
                });
                return results.ToList();
            }
            catch (Exception ex)
            {
                _logger.Error("Error retrieving snapshots for date {0}: {1}", date, ex.Message);
                return new List<ProgressSnapshot>();
            }
        }

        /// <summary>
        /// Gets the count of snapshots for an entity (for data sufficiency checks).
        /// </summary>
        public async Task<int> GetSnapshotCountAsync(SnapshotEntityType entityType, Guid entityId)
        {
            var orgId = OrganizationContext.Current.OrganizationIdOrNull;
            if (!orgId.HasValue)
                return 0;

            try
            {
                var connectionFactory = new DapperConnectionFactory();
                using var connection = connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT COUNT(*) FROM progress_snapshots
                    WHERE organization_id = @OrgId
                      AND entity_type = @EntityType
                      AND entity_id = @EntityId";
                
                return await connection.ExecuteScalarAsync<int>(sql, new 
                { 
                    OrgId = orgId.Value, 
                    EntityType = entityType.ToString().ToLowerInvariant(),
                    EntityId = entityId
                });
            }
            catch (Exception ex)
            {
                _logger.Error("Error counting snapshots for {0}/{1}: {2}", entityType, entityId, ex.Message);
                return 0;
            }
        }

        #endregion

        #region Maintenance Methods

        /// <summary>
        /// Cleans up old snapshots beyond the retention period.
        /// </summary>
        /// <param name="retentionDays">Days to retain (default 365).</param>
        public async Task CleanupOldSnapshotsAsync(int retentionDays = 365)
        {
            var orgId = OrganizationContext.Current.OrganizationIdOrNull;
            if (!orgId.HasValue)
                return;

            var cutoffDate = DateTime.Today.AddDays(-retentionDays);

            try
            {
                var connectionFactory = new DapperConnectionFactory();
                using var connection = connectionFactory.CreateConnection();
                const string sql = @"
                    DELETE FROM progress_snapshots
                    WHERE organization_id = @OrgId
                      AND snapshot_date < @CutoffDate";
                
                var deleted = await connection.ExecuteAsync(sql, new 
                { 
                    OrgId = orgId.Value, 
                    CutoffDate = cutoffDate
                });

                if (deleted > 0)
                {
                    _logger.Info("Cleaned up {0} old snapshots (older than {1})", 
                        deleted, cutoffDate.ToString("yyyy-MM-dd"));
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error cleaning up old snapshots: {0}", ex.Message);
            }
        }

        #endregion
    }
}
