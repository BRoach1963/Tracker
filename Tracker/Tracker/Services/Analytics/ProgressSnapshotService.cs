using Microsoft.EntityFrameworkCore;
using Tracker.Classes;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;

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
            var currentUserId = UserSettingsManager.Instance?.CurrentUserId;
            if (!currentUserId.HasValue)
                return new List<ProgressSnapshot>();

            var cutoffDate = DateTime.Today.AddDays(-days);

            try
            {
                var contextFactory = TrackerDbContextFactory.Instance;
                var context = contextFactory.CreateContext();
                return await context.ProgressSnapshots
                    .Where(s => s.UserId == currentUserId.Value
                             && s.EntityType == entityType
                             && s.EntityId == entityId
                             && s.SnapshotDate >= cutoffDate)
                    .OrderBy(s => s.SnapshotDate)
                    .AsNoTracking()
                    .ToListAsync();
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
            var currentUserId = UserSettingsManager.Instance?.CurrentUserId;
            if (!currentUserId.HasValue)
                return null;

            try
            {
                var contextFactory = TrackerDbContextFactory.Instance;
                var context = contextFactory.CreateContext();
                return await context.ProgressSnapshots
                    .Where(s => s.UserId == currentUserId.Value
                             && s.EntityType == entityType
                             && s.EntityId == entityId)
                    .OrderByDescending(s => s.SnapshotDate)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();
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
            var currentUserId = UserSettingsManager.Instance?.CurrentUserId;
            if (!currentUserId.HasValue)
                return new List<ProgressSnapshot>();

            try
            {
                var contextFactory = TrackerDbContextFactory.Instance;
                var context = contextFactory.CreateContext();
                return await context.ProgressSnapshots
                    .Where(s => s.UserId == currentUserId.Value
                             && s.SnapshotDate == date.Date)
                    .AsNoTracking()
                    .ToListAsync();
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
            var currentUserId = UserSettingsManager.Instance?.CurrentUserId;
            if (!currentUserId.HasValue)
                return 0;

            try
            {
                var contextFactory = TrackerDbContextFactory.Instance;
                var context = contextFactory.CreateContext();
                return await context.ProgressSnapshots
                    .Where(s => s.UserId == currentUserId.Value
                             && s.EntityType == entityType
                             && s.EntityId == entityId)
                    .CountAsync();
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
            var currentUserId = UserSettingsManager.Instance?.CurrentUserId;
            if (!currentUserId.HasValue)
                return;

            var cutoffDate = DateTime.Today.AddDays(-retentionDays);

            try
            {
                var contextFactory = TrackerDbContextFactory.Instance;
                var context = contextFactory.CreateContext();
                var oldSnapshots = await context.ProgressSnapshots
                    .Where(s => s.UserId == currentUserId.Value && s.SnapshotDate < cutoffDate)
                    .ToListAsync();

                if (oldSnapshots.Count > 0)
                {
                    context.ProgressSnapshots.RemoveRange(oldSnapshots);
                    await context.SaveChangesAsync();
                    _logger.Info("Cleaned up {0} old snapshots (older than {1})", 
                        oldSnapshots.Count, cutoffDate.ToString("yyyy-MM-dd"));
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
