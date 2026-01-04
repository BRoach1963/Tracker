using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Tracker.Logging;

namespace Tracker.Database.Interceptors
{
    /// <summary>
    /// EF Core connection interceptor that sets PostgreSQL session variable for Row-Level Security (RLS).
    /// This interceptor sets the 'app.current_user_id' session variable when a database connection is opened,
    /// enabling RLS policies to filter data based on the current user.
    /// </summary>
    /// <remarks>
    /// Usage:
    /// Each DbContext should be created with a new instance of this interceptor configured for the current user.
    /// The interceptor fires on ConnectionOpened, setting the user context before any queries execute.
    /// 
    /// RLS policies in PostgreSQL reference this variable like:
    /// <code>
    /// CREATE POLICY user_isolation ON table_name
    ///     USING (owner_id = current_setting('app.current_user_id')::uuid);
    /// </code>
    /// </remarks>
    public class RlsConnectionInterceptor : DbConnectionInterceptor
    {
        private readonly Guid _userId;
        private readonly ILogger _logger;
        private bool _contextSet = false;

        /// <summary>
        /// Creates a new RLS connection interceptor for the specified user.
        /// </summary>
        /// <param name="userId">The user ID to set as the RLS context.</param>
        public RlsConnectionInterceptor(Guid userId)
        {
            _userId = userId;
            _logger = LoggingManager.GetComponentLogger("RlsInterceptor");
        }

        /// <summary>
        /// Synchronous connection opened handler.
        /// </summary>
        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            SetUserContext(connection);
            base.ConnectionOpened(connection, eventData);
        }

        /// <summary>
        /// Async connection opened handler.
        /// </summary>
        public override async Task ConnectionOpenedAsync(
            DbConnection connection,
            ConnectionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            await SetUserContextAsync(connection, cancellationToken);
            await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
        }

        /// <summary>
        /// Sets the user context on the connection synchronously.
        /// </summary>
        private void SetUserContext(DbConnection connection)
        {
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $"SET app.current_user_id = '{_userId}'";
                cmd.ExecuteNonQuery();
                _contextSet = true;
                
                _logger.Debug("RLS context set for user: {0}", _userId);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to set RLS context for user: {0}", _userId);
                throw new InvalidOperationException($"Failed to set RLS user context: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Sets the user context on the connection asynchronously.
        /// </summary>
        private async Task SetUserContextAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            try
            {
                await using var cmd = connection.CreateCommand();
                cmd.CommandText = $"SET app.current_user_id = '{_userId}'";
                await cmd.ExecuteNonQueryAsync(cancellationToken);
                _contextSet = true;
                
                _logger.Debug("RLS context set for user: {0}", _userId);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to set RLS context for user: {0}", _userId);
                throw new InvalidOperationException($"Failed to set RLS user context: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Whether the user context has been set on this connection.
        /// </summary>
        public bool IsContextSet => _contextSet;

        /// <summary>
        /// The user ID this interceptor is configured for.
        /// </summary>
        public Guid UserId => _userId;
    }
}
