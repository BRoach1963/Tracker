using Microsoft.EntityFrameworkCore;
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
        private DateTime? _lastSnapshotDate;

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
        /// </summary>
        public async Task CaptureSnapshotsIfNeededAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var dbManager = TrackerDbManager.Instance;
                if (dbManager == null || !dbManager.IsInitialized)
                {
                    _logger.Debug("Database not initialized, skipping snapshot capture");
                    return;
                }

                var currentUserId = UserSettingsManager.Instance?.CurrentUserId;
                if (!currentUserId.HasValue)
                {
                    _logger.Debug("No user logged in, skipping snapshot capture");
                    return;
                }

                var today = DateTime.Today;

                // Check if we already captured today
                if (_lastSnapshotDate == today)
                {
                    _logger.Debug("Already captured snapshots today");
                    return;
                }

                // Check database for last snapshot
                var lastSnapshot = await GetLastSnapshotDateAsync(currentUserId.Value);
                if (lastSnapshot == today)
                {
                    _lastSnapshotDate = today;
                    _logger.Debug("Snapshots already exist for today");
                    return;
                }

                _logger.Info("Capturing progress snapshots for {0}", today.ToString("yyyy-MM-dd"));

                await CaptureAllSnapshotsAsync(currentUserId.Value, today, cancellationToken);

                _lastSnapshotDate = today;
                _logger.Info("Snapshot capture complete");
            }
            catch (Exception ex)
            {
                _logger.Error("Error capturing snapshots: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Forces a snapshot capture regardless of last capture time.
        /// Useful for testing or manual refresh.
        /// </summary>
        public async Task ForceCaptureSnapshotsAsync(CancellationToken cancellationToken = default)
        {
            var currentUserId = UserSettingsManager.Instance?.CurrentUserId;
            if (!currentUserId.HasValue)
            {
                _logger.Warn("Cannot capture snapshots: no user logged in");
                return;
            }

            var today = DateTime.Today;
            _logger.Info("Force capturing progress snapshots for {0}", today.ToString("yyyy-MM-dd"));

            await CaptureAllSnapshotsAsync(currentUserId.Value, today, cancellationToken);

            _lastSnapshotDate = today;
            _logger.Info("Force snapshot capture complete");
        }

        #endregion

        #region Public Methods - Snapshot Retrieval

        /// <summary>
        /// Gets historical snapshots for a specific entity.
        /// </summary>
        /// <param name="entityType">The entity type (use SnapshotEntityType constants).</param>
        /// <param name="entityId">The entity ID.</param>
        /// <param name="days">Number of days of history to retrieve (default 90).</param>
        /// <returns>List of snapshots ordered by date ascending.</returns>
        public async Task<List<ProgressSnapshot>> GetHistoryAsync(
            string entityType, 
            int entityId, 
            int days = 90)
        {
            var context = TrackerDbManager.Instance.GetDbContext();
            if (context == null)
                return new List<ProgressSnapshot>();

            var currentUserId = UserSettingsManager.Instance?.CurrentUserId;
            if (!currentUserId.HasValue)
                return new List<ProgressSnapshot>();

            var cutoffDate = DateTime.Today.AddDays(-days);

            try
            {
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
        public async Task<ProgressSnapshot?> GetLatestSnapshotAsync(string entityType, int entityId)
        {
            var context = TrackerDbManager.Instance.GetDbContext();
            if (context == null)
                return null;

            var currentUserId = UserSettingsManager.Instance?.CurrentUserId;
            if (!currentUserId.HasValue)
                return null;

            try
            {
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
            var context = TrackerDbManager.Instance.GetDbContext();
            if (context == null)
                return new List<ProgressSnapshot>();

            var currentUserId = UserSettingsManager.Instance?.CurrentUserId;
            if (!currentUserId.HasValue)
                return new List<ProgressSnapshot>();

            try
            {
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
        public async Task<int> GetSnapshotCountAsync(string entityType, int entityId)
        {
            var context = TrackerDbManager.Instance.GetDbContext();
            if (context == null)
                return 0;

            var currentUserId = UserSettingsManager.Instance?.CurrentUserId;
            if (!currentUserId.HasValue)
                return 0;

            try
            {
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

        #region Private Methods

        private async Task<DateTime?> GetLastSnapshotDateAsync(int userId)
        {
            var context = TrackerDbManager.Instance.GetDbContext();
            if (context == null)
                return null;

            try
            {
                var lastDate = await context.ProgressSnapshots
                    .Where(s => s.UserId == userId)
                    .MaxAsync(s => (DateTime?)s.SnapshotDate);

                return lastDate;
            }
            catch (Exception ex)
            {
                _logger.Debug("No existing snapshots found: {0}", ex.Message);
                return null;
            }
        }

        private async Task CaptureAllSnapshotsAsync(int userId, DateTime date, CancellationToken cancellationToken)
        {
            var context = TrackerDbManager.Instance.GetDbContext();
            if (context == null)
                return;

            var snapshots = new List<ProgressSnapshot>();

            // Capture OKR snapshots
            await CaptureOkrSnapshotsAsync(userId, date, snapshots, cancellationToken);

            // Capture KPI snapshots
            await CaptureKpiSnapshotsAsync(userId, date, snapshots, cancellationToken);

            // Capture Project snapshots
            await CaptureProjectSnapshotsAsync(userId, date, snapshots, cancellationToken);

            if (snapshots.Count == 0)
            {
                _logger.Debug("No entities to snapshot");
                return;
            }

            // Save all snapshots in one transaction
            try
            {
                context.ProgressSnapshots.AddRange(snapshots);
                await context.SaveChangesAsync(cancellationToken);
                _logger.Info("Saved {0} progress snapshots", snapshots.Count);
            }
            catch (Exception ex)
            {
                _logger.Error("Error saving snapshots: {0}", ex.Message);
            }
        }

        private async Task CaptureOkrSnapshotsAsync(
            int userId, 
            DateTime date, 
            List<ProgressSnapshot> snapshots,
            CancellationToken cancellationToken)
        {
            try
            {
                var okrs = await TrackerDbManager.Instance.GetOKRsAsync();
                if (okrs == null) return;

                foreach (var okr in okrs)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    // Skip OKRs that haven't started or have ended
                    if (okr.StartDate > date || okr.EndDate < date)
                        continue;

                    // OKR snapshot
                    snapshots.Add(new ProgressSnapshot
                    {
                        UserId = userId,
                        EntityType = SnapshotEntityType.OKR,
                        EntityId = okr.ObjectiveId,
                        SnapshotDate = date,
                        CurrentValue = (decimal)okr.CompletionPercentage,
                        TargetValue = 100m,
                        Progress = (decimal)okr.CompletionPercentage
                    });

                    // Also capture each Key Result
                    if (okr.KeyResults != null)
                    {
                        foreach (var kr in okr.KeyResults)
                        {
                            snapshots.Add(new ProgressSnapshot
                            {
                                UserId = userId,
                                EntityType = SnapshotEntityType.KeyResult,
                                EntityId = kr.Id,
                                SnapshotDate = date,
                                CurrentValue = kr.CurrentValue,
                                TargetValue = kr.TargetValue,
                                Progress = kr.Progress
                            });
                        }
                    }
                }

                _logger.Debug("Captured {0} OKR/KeyResult snapshots", 
                    snapshots.Count(s => s.EntityType == SnapshotEntityType.OKR || s.EntityType == SnapshotEntityType.KeyResult));
            }
            catch (Exception ex)
            {
                _logger.Error("Error capturing OKR snapshots: {0}", ex.Message);
            }
        }

        private async Task CaptureKpiSnapshotsAsync(
            int userId, 
            DateTime date, 
            List<ProgressSnapshot> snapshots,
            CancellationToken cancellationToken)
        {
            try
            {
                var kpis = await TrackerDbManager.Instance.GetKPIsAsync();
                if (kpis == null) return;

                foreach (var kpi in kpis)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    // Skip KPIs with no target (can't calculate progress)
                    if (kpi.TargetValue == 0)
                        continue;

                    snapshots.Add(new ProgressSnapshot
                    {
                        UserId = userId,
                        EntityType = SnapshotEntityType.KPI,
                        EntityId = kpi.KpiId,
                        SnapshotDate = date,
                        CurrentValue = (decimal)kpi.Value,
                        TargetValue = (decimal)kpi.TargetValue,
                        Progress = (decimal)kpi.PercentComplete
                    });
                }

                _logger.Debug("Captured {0} KPI snapshots", 
                    snapshots.Count(s => s.EntityType == SnapshotEntityType.KPI));
            }
            catch (Exception ex)
            {
                _logger.Error("Error capturing KPI snapshots: {0}", ex.Message);
            }
        }

        private async Task CaptureProjectSnapshotsAsync(
            int userId, 
            DateTime date, 
            List<ProgressSnapshot> snapshots,
            CancellationToken cancellationToken)
        {
            try
            {
                var projects = await TrackerDbManager.Instance.GetProjectsAsync();
                if (projects == null) return;

                foreach (var project in projects)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    // Skip completed or not-started projects
                    if (project.Progress >= 100 || project.StartDate > date)
                        continue;

                    snapshots.Add(new ProgressSnapshot
                    {
                        UserId = userId,
                        EntityType = SnapshotEntityType.Project,
                        EntityId = project.ID,
                        SnapshotDate = date,
                        CurrentValue = (decimal)project.Progress,
                        TargetValue = 100m,
                        Progress = (decimal)project.Progress
                    });
                }

                _logger.Debug("Captured {0} Project snapshots", 
                    snapshots.Count(s => s.EntityType == SnapshotEntityType.Project));
            }
            catch (Exception ex)
            {
                _logger.Error("Error capturing Project snapshots: {0}", ex.Message);
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
            var context = TrackerDbManager.Instance.GetDbContext();
            if (context == null)
                return;

            var currentUserId = UserSettingsManager.Instance?.CurrentUserId;
            if (!currentUserId.HasValue)
                return;

            var cutoffDate = DateTime.Today.AddDays(-retentionDays);

            try
            {
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
