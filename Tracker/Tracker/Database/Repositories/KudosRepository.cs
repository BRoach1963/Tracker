using Microsoft.EntityFrameworkCore;
using Tracker.DataModels;
using Tracker.Logging;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository for Kudos data access operations.
    /// Handles all CRUD operations for recognition/kudos between team members.
    /// </summary>
    public class KudosRepository : IKudosRepository
    {
        private readonly TrackerDbContext _context;
        private readonly Func<TrackerDbContext> _contextFactory; // For PostgreSQL parallel operations
        private readonly Guid _userId;
        private readonly LoggingManager.Logger _logger;

        /// <summary>
        /// Creates a new instance of KudosRepository.
        /// </summary>
        /// <param name="context">The database context (for SQLite/SQL Server).</param>
        /// <param name="userId">The current user's ID.</param>
        /// <param name="contextFactory">Optional factory for creating contexts (for PostgreSQL).</param>
        public KudosRepository(TrackerDbContext context, Guid userId, Func<TrackerDbContext>? contextFactory = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userId = userId;
            _contextFactory = contextFactory ?? (() => context);
            _logger = new LoggingManager.Logger(nameof(KudosRepository), "DatabaseLog");
        }

        /// <summary>
        /// Retrieves all kudos for the current user's organization.
        /// </summary>
        public async Task<List<Kudos>> GetKudosAsync()
        {
            System.Diagnostics.Debug.WriteLine($"=== GetKudosAsync: Starting ===");
            var context = _contextFactory();
            if (context == null)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetKudosAsync: No context ===");
                return new List<Kudos>();
            }

            try
            {
                var result = await context.Kudos
                    .AsNoTracking()
                    .Where(k => !k.IsDeleted)
                    .Include(k => k.FromTeamMember)
                    .Include(k => k.ToTeamMember)
                    .OrderByDescending(k => k.CreatedAt)
                    .ToListAsync();
                System.Diagnostics.Debug.WriteLine($"=== GetKudosAsync: Query succeeded, got {result.Count} kudos ===");
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== GetKudosAsync EXCEPTION: {ex.GetType().Name}: {ex.Message} ===");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"=== Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message} ===");
                _logger.Exception(ex, "Error retrieving kudos from database");
                return new List<Kudos>();
            }
            finally
            {
                DisposeIfFactory(context);
            }
        }

        /// <summary>
        /// Retrieves a specific kudos by ID.
        /// </summary>
        public async Task<Kudos?> GetKudosByIdAsync(Guid id)
        {
            if (_context == null) return null;

            try
            {
                return await _context.Kudos
                    .Where(k => !k.IsDeleted)
                    .Include(k => k.FromTeamMember)
                    .Include(k => k.ToTeamMember)
                    .FirstOrDefaultAsync(k => k.Id == id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving kudos with id {0}", id);
                return null;
            }
        }

        /// <summary>
        /// Adds new kudos.
        /// </summary>
        public async Task<Guid> AddKudosAsync(Kudos kudos)
        {
            if (_context == null)
            {
                _logger.Error("AddKudosAsync: _context is null");
                return Guid.Empty;
            }

            try
            {
                _context.Kudos.Add(kudos);
                await _context.SaveChangesAsync();
                return kudos.Id;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error adding kudos");
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Updates existing kudos.
        /// </summary>
        public async Task<bool> UpdateKudosAsync(Kudos kudos)
        {
            if (_context == null) return false;

            try
            {
                var existing = await _context.Kudos.FindAsync(kudos.Id);
                if (existing == null)
                {
                    _logger.Error("UpdateKudosAsync: Kudos ID {0} not found", kudos.Id);
                    return false;
                }

                _context.Entry(existing).CurrentValues.SetValues(kudos);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error updating kudos");
                return false;
            }
        }

        /// <summary>
        /// Deletes kudos by ID.
        /// </summary>
        public async Task<bool> DeleteKudosAsync(Guid id)
        {
            if (_context == null) return false;

            try
            {
                var kudos = await _context.Kudos.FindAsync(id);
                if (kudos != null)
                {
                    _context.Kudos.Remove(kudos);
                    await _context.SaveChangesAsync();
                    _logger.Info("Deleted kudos ID: {0}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting kudos ID: {0}", id);
                return false;
            }
        }

        /// <summary>
        /// Gets kudos given by a specific team member.
        /// </summary>
        public async Task<List<Kudos>> GetKudosFromAsync(Guid fromTeamMemberId)
        {
            if (_context == null) return new List<Kudos>();

            try
            {
                return await _context.Kudos
                    .Where(k => !k.IsDeleted && k.FromTeamMemberId == fromTeamMemberId)
                    .Include(k => k.FromTeamMember)
                    .Include(k => k.ToTeamMember)
                    .OrderByDescending(k => k.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving kudos from team member {0}", fromTeamMemberId);
                return new List<Kudos>();
            }
        }

        /// <summary>
        /// Gets kudos received by a specific team member.
        /// </summary>
        public async Task<List<Kudos>> GetKudosToAsync(Guid toTeamMemberId)
        {
            if (_context == null) return new List<Kudos>();

            try
            {
                return await _context.Kudos
                    .Where(k => !k.IsDeleted && k.ToTeamMemberId == toTeamMemberId)
                    .Include(k => k.FromTeamMember)
                    .Include(k => k.ToTeamMember)
                    .OrderByDescending(k => k.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving kudos to team member {0}", toTeamMemberId);
                return new List<Kudos>();
            }
        }

        /// <summary>
        /// Gets kudos between two specific team members.
        /// </summary>
        public async Task<List<Kudos>> GetKudosBetweenAsync(Guid fromTeamMemberId, Guid toTeamMemberId)
        {
            if (_context == null) return new List<Kudos>();

            try
            {
                return await _context.Kudos
                    .Where(k => !k.IsDeleted && k.FromTeamMemberId == fromTeamMemberId && k.ToTeamMemberId == toTeamMemberId)
                    .Include(k => k.FromTeamMember)
                    .Include(k => k.ToTeamMember)
                    .OrderByDescending(k => k.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving kudos between team members {0} and {1}", fromTeamMemberId, toTeamMemberId);
                return new List<Kudos>();
            }
        }

        /// <summary>
        /// Gets kudos with a specific badge type.
        /// If badgeType is null, retrieves all kudos.
        /// </summary>
        public async Task<List<Kudos>> GetKudosByBadgeTypeAsync(string? badgeType)
        {
            if (_context == null) return new List<Kudos>();

            try
            {
                var query = _context.Kudos
                    .Where(k => !k.IsDeleted)
                    .Include(k => k.FromTeamMember)
                    .Include(k => k.ToTeamMember)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(badgeType))
                {
                    query = query.Where(k => k.BadgeType == badgeType);
                }

                return await query
                    .OrderByDescending(k => k.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving kudos by badge type {0}", badgeType);
                return new List<Kudos>();
            }
        }

        /// <summary>
        /// Gets public kudos only.
        /// </summary>
        public async Task<List<Kudos>> GetPublicKudosAsync()
        {
            if (_context == null) return new List<Kudos>();

            try
            {
                return await _context.Kudos
                    .Where(k => !k.IsDeleted && k.IsPublic)
                    .Include(k => k.FromTeamMember)
                    .Include(k => k.ToTeamMember)
                    .OrderByDescending(k => k.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving public kudos");
                return new List<Kudos>();
            }
        }

        /// <summary>
        /// Gets recent kudos within a date range.
        /// </summary>
        public async Task<List<Kudos>> GetRecentKudosAsync(DateTime startDate, DateTime endDate)
        {
            if (_context == null) return new List<Kudos>();

            try
            {
                return await _context.Kudos
                    .Where(k => !k.IsDeleted && k.CreatedAt >= startDate && k.CreatedAt <= endDate)
                    .Include(k => k.FromTeamMember)
                    .Include(k => k.ToTeamMember)
                    .OrderByDescending(k => k.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error retrieving kudos in date range {0} to {1}", startDate, endDate);
                return new List<Kudos>();
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
