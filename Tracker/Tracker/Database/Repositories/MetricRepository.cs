using Microsoft.EntityFrameworkCore;
using Tracker.DataModels;
using Tracker.Logging;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository for Metric data access operations.
    /// Handles all CRUD operations for metrics, composite metrics, and data sources.
    /// </summary>
    public class MetricRepository : IMetricRepository
    {
        private readonly TrackerDbContext _context;
        private readonly Func<TrackerDbContext> _contextFactory; // For PostgreSQL parallel operations
        private readonly Guid _userId;
        private readonly LoggingManager.Logger _logger;

        /// <summary>
        /// Creates a new instance of MetricRepository.
        /// </summary>
        /// <param name="context">The database context (for SQLite/SQL Server).</param>
        /// <param name="userId">The current user's ID.</param>
        /// <param name="contextFactory">Optional factory for creating contexts (for PostgreSQL).</param>
        public MetricRepository(TrackerDbContext context, Guid userId, Func<TrackerDbContext>? contextFactory = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userId = userId;
            _contextFactory = contextFactory ?? (() => context);
            _logger = new LoggingManager.Logger(nameof(MetricRepository), "DatabaseLog");
        }

        /// <summary>
        /// Retrieves all metrics for the current user.
        /// </summary>
        public async Task<List<Metric>> GetMetricsAsync()
        {
            System.Diagnostics.Debug.WriteLine($"=== GetMetricsAsync: Starting ===");
            var context = _contextFactory();
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetMetricsAsync: No context ===");
                return new List<Metric>();
            }

            try
            {
                var result = await context.Metrics
                    .AsNoTracking()
                    .Where(m => !m.IsDeleted && m.CreatedByUserId == _userId)
                    .Include(m => m.Owner)
                    .Include(m => m.DataSources)
                    .OrderBy(m => m.Name)
                    .ToListAsync();
                System.Diagnostics.Debug.WriteLine($"=== GetMetricsAsync: Query succeeded, got {result.Count} metrics ===");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetMetricsAsync EXCEPTION: {ex.GetType().Name}: {ex.Message} ===");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"=== Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message} ===");
                _logger.Exception(ex, "Error retrieving metrics from database");
                return new List<Metric>();
            }
            finally
            {
                DisposeIfFactory(context);
            }
        }

        /// <summary>
        /// Retrieves a specific metric by ID.
        /// </summary>
        public async Task<Metric?> GetMetricByIdAsync(Guid id)
        {
            if (_context == null) return null;

            try
            {
                return await _context.Metrics
                    .Where(m => !m.IsDeleted && m.CreatedByUserId == _userId)
                    .Include(m => m.Owner)
                    .Include(m => m.DataSources)
                    .FirstOrDefaultAsync(m => m.Id == id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving metric with id {0}", id);
                return null;
            }
        }

        /// <summary>
        /// Adds a new metric.
        /// </summary>
        public async Task<Guid> AddMetricAsync(Metric metric)
        {
            if (_context == null)
            {
                _logger.Error("AddMetricAsync: _context is null");
                return Guid.Empty;
            }

            try
            {
                metric.CreatedByUserId = _userId;
                _context.Metrics.Add(metric);
                await _context.SaveChangesAsync();
                return metric.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding metric");
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Updates an existing metric.
        /// </summary>
        public async Task<bool> UpdateMetricAsync(Metric metric)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.Metrics.FindAsync(metric.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateMetricAsync: Metric ID {0} not found", metric.Id);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(metric);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating metric");
                return false;
            }
        }

        /// <summary>
        /// Deletes a metric by ID.
        /// </summary>
        public async Task<bool> DeleteMetricAsync(Guid id)
        {
            if (_context == null) return false;

            try
            {
                var metric = await _context.Metrics.FindAsync(id);
                if (metric != null)
                {
                    _context.Metrics.Remove(metric);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted metric ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting metric ID: {0}", id);
                return false;
            }
        }

        /// <summary>
        /// Gets metrics for a specific category.
        /// </summary>
        public async Task<List<Metric>> GetMetricsByCategoryAsync(string category)
        {
            if (_context == null || string.IsNullOrEmpty(category)) 
                return new List<Metric>();

            try
            {
                return await _context.Metrics
                    .Where(m => !m.IsDeleted && m.CreatedByUserId == _userId && m.Category == category)
                    .Include(m => m.Owner)
                    .Include(m => m.DataSources)
                    .OrderBy(m => m.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving metrics for category {0}", category);
                return new List<Metric>();
            }
        }

        /// <summary>
        /// Gets metrics owned by a specific team member.
        /// </summary>
        public async Task<List<Metric>> GetTeamMemberMetricsAsync(Guid teamMemberId)
        {
            if (_context == null) return new List<Metric>();

            try
            {
                return await _context.Metrics
                    .Where(m => !m.IsDeleted && m.CreatedByUserId == _userId && m.OwnerTeamMemberId == teamMemberId)
                    .Include(m => m.Owner)
                    .Include(m => m.DataSources)
                    .OrderBy(m => m.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving metrics for team member {0}", teamMemberId);
                return new List<Metric>();
            }
        }

        /// <summary>
        /// Gets child metrics of a composite parent metric.
        /// </summary>
        public async Task<List<Metric>> GetChildMetricsAsync(Guid parentMetricId)
        {
            if (_context == null) return new List<Metric>();

            try
            {
                return await _context.Metrics
                    .Where(m => !m.IsDeleted && m.CreatedByUserId == _userId && m.ParentMetricId == parentMetricId)
                    .Include(m => m.Owner)
                    .Include(m => m.DataSources)
                    .OrderBy(m => m.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving child metrics for parent metric {0}", parentMetricId);
                return new List<Metric>();
            }
        }

        /// <summary>
        /// Gets team-visible metrics for a specific team member.
        /// </summary>
        public async Task<List<Metric>> GetTeamVisibleMetricsAsync(Guid teamMemberId)
        {
            if (_context == null) return new List<Metric>();

            try
            {
                return await _context.Metrics
                    .Where(m => !m.IsDeleted && m.CreatedByUserId == _userId && m.IsTeamVisible && m.OwnerTeamMemberId == teamMemberId)
                    .Include(m => m.Owner)
                    .Include(m => m.DataSources)
                    .OrderBy(m => m.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving team-visible metrics for team member {0}", teamMemberId);
                return new List<Metric>();
            }
        }

        /// <summary>
        /// Gets the count of meetings where a specific metric was discussed.
        /// </summary>
        public async Task<int> GetMetricMeetingCountAsync(Guid metricId)
        {
            if (_context == null) return 0;

            try
            {
                return await _context.MeetingMetricLinks
                    .Where(link => !link.IsDeleted && link.MetricId == metricId && link.UserId == _userId)
                    .Select(link => link.MeetingId)
                    .Distinct()
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error counting meetings for metric {0}", metricId);
                return 0;
            }
        }

        /// <summary>
        /// Gets meeting counts for multiple metrics (batch operation).
        /// Prevents N+1 query problem when loading meeting counts for multiple metrics.
        /// </summary>
        public async Task<Dictionary<Guid, int>> GetMetricMeetingCountsAsync(List<Guid> metricIds)
        {
            if (_context == null || metricIds == null || metricIds.Count == 0)
                return new Dictionary<Guid, int>();

            try
            {
                var counts = await _context.MeetingMetricLinks
                    .Where(link => !link.IsDeleted && metricIds.Contains(link.MetricId) && link.UserId == _userId)
                    .GroupBy(link => link.MetricId)
                    .Select(g => new { MetricId = g.Key, Count = g.Select(x => x.MeetingId).Distinct().Count() })
                    .ToDictionaryAsync(x => x.MetricId, x => x.Count);

                return counts;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error counting meetings for metrics");
                return new Dictionary<Guid, int>();
            }
        }

        /// <summary>
        /// Gets data sources for a metric.
        /// </summary>
        public async Task<List<MetricDataSource>> GetMetricDataSourcesAsync(Guid metricId)
        {
            if (_context == null) return new List<MetricDataSource>();

            try
            {
                return await _context.MetricDataSources
                    .Where(ds => !ds.IsDeleted && ds.MetricId == metricId)
                    .OrderBy(ds => ds.Id)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving data sources for metric {0}", metricId);
                return new List<MetricDataSource>();
            }
        }

        /// <summary>
        /// Adds a data source to a metric.
        /// </summary>
        public async Task<bool> AddMetricDataSourceAsync(MetricDataSource dataSource)
        {
            if (_context == null || dataSource == null)
            {
                _logger.Error("AddMetricDataSourceAsync: _context or dataSource is null");
                return false;
            }

            try
            {
                _context.MetricDataSources.Add(dataSource);
                await _context.SaveChangesAsync();
                _logger.Info("Added data source to metric {0}", dataSource.MetricId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding data source");
                return false;
            }
        }

        /// <summary>
        /// Removes a data source from a metric.
        /// </summary>
        public async Task<bool> RemoveMetricDataSourceAsync(Guid dataSourceId)
        {
            if (_context == null) return false;

            try
            {
                var dataSource = await _context.MetricDataSources.FindAsync(dataSourceId);
                if (dataSource != null)
                {
                    _context.MetricDataSources.Remove(dataSource);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted metric data source ID: {0}", dataSourceId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting metric data source ID: {0}", dataSourceId);
                return false;
            }
        }

        /// <summary>
        /// Links a metric to a meeting discussion.
        /// </summary>
        public async Task<bool> LinkMetricToMeetingAsync(Guid meetingId, Guid metricId, string? discussionNotes = null)
        {
            if (_context == null) return false;

            try
            {
                // Verify Meeting belongs to current user
                var meeting = await _context.Meetings
                    .Where(m => m.Id == meetingId && m.CreatedByUserId == _userId)
                    .FirstOrDefaultAsync();
                
                if (meeting == null)
                {
                    _logger.Warn("Cannot link metric {0} to meeting {1} - meeting not found or doesn't belong to current user", metricId, meetingId);
                    return false;
                }

                // Verify Metric belongs to current user
                var metric = await _context.Metrics
                    .Where(m => m.Id == metricId && m.CreatedByUserId == _userId)
                    .FirstOrDefaultAsync();
                
                if (metric == null)
                {
                    _logger.Warn("Cannot link metric {0} to meeting {1} - metric not found or doesn't belong to current user", metricId, meetingId);
                    return false;
                }

                // Check if link already exists
                var existing = await _context.MeetingMetricLinks
                    .FirstOrDefaultAsync(link => link.MeetingId == meetingId && link.MetricId == metricId && !link.IsDeleted);

                if (existing != null)
                {
                    // Update existing link
                    existing.DiscussionNotes = discussionNotes ?? string.Empty;
                    _context.MeetingMetricLinks.Update(existing);
                }
                else
                {
                    // Create new link
                    var link = new MeetingMetricLink
                    {
                        Id = Guid.NewGuid(),
                        MeetingId = meetingId,
                        MetricId = metricId,
                        DiscussionNotes = discussionNotes ?? string.Empty,
                        UserId = _userId
                    };
                    _context.MeetingMetricLinks.Add(link);
                }

                await _context.SaveChangesAsync();
                _logger.Info("Linked metric {0} to meeting {1}", metricId, meetingId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error linking metric {0} to meeting {1}", metricId, meetingId);
                return false;
            }
        }

        /// <summary>
        /// Unlinks a metric from a meeting.
        /// </summary>
        public async Task<bool> UnlinkMetricFromMeetingAsync(Guid meetingId, Guid metricId)
        {
            if (_context == null) return false;

            try
            {
                var link = await _context.MeetingMetricLinks
                    .FirstOrDefaultAsync(l => l.MeetingId == meetingId && l.MetricId == metricId && !l.IsDeleted);

                if (link != null)
                {
                    link.IsDeleted = true;
                    link.DeletedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    _logger.Info("Unlinked metric {0} from meeting {1}", metricId, meetingId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error unlinking metric {0} from meeting {1}", metricId, meetingId);
                return false;
            }
        }

        /// <summary>
        /// Disposes the context if it was created by the factory.
        /// </summary>
        private void DisposeIfFactory(TrackerDbContext context)
        {
            // Only dispose if it came from the factory and not the primary context
            if (context != _context && context is IAsyncDisposable asyncDisposable)
            {
                asyncDisposable.DisposeAsync().GetAwaiter().GetResult();
            }
        }
    }
}
