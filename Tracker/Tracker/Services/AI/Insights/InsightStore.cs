using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Tracker.DataModels;
using Tracker.Logging;

namespace Tracker.Services.AI.Insights
{
    /// <summary>
    /// Persists insights to SQLite for history, deduplication, and tracking.
    /// </summary>
    public class InsightStore : IDisposable
    {
        private static InsightStore? _instance;
        private static readonly object _lock = new();

        private readonly string _databasePath;
        private SqliteConnection? _connection;
        private bool _isInitialized;
        private bool _disposed;
        private readonly ILogger _logger;

        /// <summary>
        /// Singleton instance of the InsightStore.
        /// </summary>
        public static InsightStore Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new InsightStore();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Creates a new InsightStore instance.
        /// </summary>
        private InsightStore()
        {
            _logger = LoggingManager.GetComponentLogger("InsightStore");
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Tracker");
            Directory.CreateDirectory(appDataPath);
            _databasePath = Path.Combine(appDataPath, "insights.db");
        }

        /// <summary>
        /// Initializes the database schema.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                _connection = new SqliteConnection($"Data Source={_databasePath}");
                await _connection.OpenAsync();

                var createTableSql = @"
                    CREATE TABLE IF NOT EXISTS insights (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        unique_key TEXT NOT NULL UNIQUE,
                        type TEXT NOT NULL,
                        severity TEXT NOT NULL,
                        title TEXT NOT NULL,
                        description TEXT,
                        action_suggestion TEXT,
                        entity_type TEXT,
                        entity_id INTEGER,
                        generated_at TEXT NOT NULL,
                        dismissed_at TEXT,
                        acted_on_at TEXT,
                        is_read INTEGER DEFAULT 0,
                        created_at TEXT DEFAULT CURRENT_TIMESTAMP
                    );

                    CREATE INDEX IF NOT EXISTS idx_insights_type ON insights(type);
                    CREATE INDEX IF NOT EXISTS idx_insights_severity ON insights(severity);
                    CREATE INDEX IF NOT EXISTS idx_insights_generated ON insights(generated_at);
                    CREATE INDEX IF NOT EXISTS idx_insights_dismissed ON insights(dismissed_at);
                    CREATE INDEX IF NOT EXISTS idx_insights_unique_key ON insights(unique_key);
                ";

                using var command = new SqliteCommand(createTableSql, _connection);
                await command.ExecuteNonQueryAsync();

                _isInitialized = true;
                _logger.Info("InsightStore initialized at {0}", _databasePath);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to initialize InsightStore");
                throw;
            }
        }

        /// <summary>
        /// Saves or updates an insight. Uses UniqueKey for deduplication.
        /// </summary>
        /// <param name="insight">The insight to save.</param>
        /// <returns>True if inserted, false if already existed.</returns>
        public async Task<bool> SaveInsightAsync(Insight insight)
        {
            EnsureInitialized();

            try
            {
                // Check if already exists
                var existingId = await GetInsightIdByUniqueKeyAsync(insight.UniqueKey);
                if (existingId.HasValue)
                {
                    // Already exists - don't duplicate
                    _logger.Debug("Insight already exists: {0}", insight.UniqueKey);
                    return false;
                }

                var sql = @"
                    INSERT INTO insights (unique_key, type, severity, title, description, 
                        action_suggestion, entity_type, entity_id, generated_at, is_read)
                    VALUES (@unique_key, @type, @severity, @title, @description,
                        @action_suggestion, @entity_type, @entity_id, @generated_at, @is_read)
                ";

                using var command = new SqliteCommand(sql, _connection);
                command.Parameters.AddWithValue("@unique_key", insight.UniqueKey);
                command.Parameters.AddWithValue("@type", insight.Type.ToString());
                command.Parameters.AddWithValue("@severity", insight.Severity.ToString());
                command.Parameters.AddWithValue("@title", insight.Title);
                command.Parameters.AddWithValue("@description", insight.Description ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@action_suggestion", insight.ActionSuggestion ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@entity_type", insight.EntityType ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@entity_id", insight.EntityId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@generated_at", insight.GeneratedAt.ToString("O"));
                command.Parameters.AddWithValue("@is_read", insight.IsRead ? 1 : 0);

                await command.ExecuteNonQueryAsync();
                _logger.Info("Saved new insight: {0} - {1}", insight.Type, insight.Title);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to save insight: {0}", insight.UniqueKey);
                return false;
            }
        }

        /// <summary>
        /// Saves multiple insights, skipping duplicates.
        /// </summary>
        /// <param name="insights">The insights to save.</param>
        /// <returns>Number of new insights saved.</returns>
        public async Task<int> SaveInsightsAsync(IEnumerable<Insight> insights)
        {
            int savedCount = 0;
            foreach (var insight in insights)
            {
                if (await SaveInsightAsync(insight))
                {
                    savedCount++;
                }
            }
            return savedCount;
        }

        /// <summary>
        /// Gets all active (non-dismissed) insights.
        /// </summary>
        public async Task<List<Insight>> GetActiveInsightsAsync()
        {
            EnsureInitialized();

            var insights = new List<Insight>();
            var sql = @"
                SELECT * FROM insights 
                WHERE dismissed_at IS NULL 
                ORDER BY 
                    CASE severity 
                        WHEN 'Critical' THEN 0 
                        WHEN 'Warning' THEN 1 
                        ELSE 2 
                    END,
                    generated_at DESC
            ";

            using var command = new SqliteCommand(sql, _connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                insights.Add(ReadInsight(reader));
            }

            return insights;
        }

        /// <summary>
        /// Gets unread insight count.
        /// </summary>
        public async Task<int> GetUnreadCountAsync()
        {
            EnsureInitialized();

            var sql = "SELECT COUNT(*) FROM insights WHERE dismissed_at IS NULL AND is_read = 0";
            using var command = new SqliteCommand(sql, _connection);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Marks an insight as read.
        /// </summary>
        public async Task MarkAsReadAsync(int insightId)
        {
            EnsureInitialized();

            var sql = "UPDATE insights SET is_read = 1 WHERE id = @id";
            using var command = new SqliteCommand(sql, _connection);
            command.Parameters.AddWithValue("@id", insightId);
            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Marks all insights as read.
        /// </summary>
        public async Task MarkAllAsReadAsync()
        {
            EnsureInitialized();

            var sql = "UPDATE insights SET is_read = 1 WHERE is_read = 0";
            using var command = new SqliteCommand(sql, _connection);
            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Dismisses an insight.
        /// </summary>
        public async Task DismissInsightAsync(int insightId)
        {
            EnsureInitialized();

            var sql = "UPDATE insights SET dismissed_at = @dismissed_at WHERE id = @id";
            using var command = new SqliteCommand(sql, _connection);
            command.Parameters.AddWithValue("@id", insightId);
            command.Parameters.AddWithValue("@dismissed_at", DateTime.Now.ToString("O"));
            await command.ExecuteNonQueryAsync();
            _logger.Info("Dismissed insight {0}", insightId);
        }

        /// <summary>
        /// Marks an insight as acted upon.
        /// </summary>
        public async Task MarkAsActedOnAsync(int insightId)
        {
            EnsureInitialized();

            var sql = "UPDATE insights SET acted_on_at = @acted_on_at, is_read = 1 WHERE id = @id";
            using var command = new SqliteCommand(sql, _connection);
            command.Parameters.AddWithValue("@id", insightId);
            command.Parameters.AddWithValue("@acted_on_at", DateTime.Now.ToString("O"));
            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Checks if an insight with the given unique key exists and is not dismissed.
        /// </summary>
        public async Task<bool> ExistsActiveAsync(string uniqueKey)
        {
            EnsureInitialized();

            var sql = "SELECT COUNT(*) FROM insights WHERE unique_key = @unique_key AND dismissed_at IS NULL";
            using var command = new SqliteCommand(sql, _connection);
            command.Parameters.AddWithValue("@unique_key", uniqueKey);
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result) > 0;
        }

        /// <summary>
        /// Deletes old dismissed insights (cleanup).
        /// </summary>
        /// <param name="olderThanDays">Delete insights dismissed more than this many days ago.</param>
        public async Task CleanupOldInsightsAsync(int olderThanDays = 30)
        {
            EnsureInitialized();

            var cutoffDate = DateTime.Now.AddDays(-olderThanDays).ToString("O");
            var sql = "DELETE FROM insights WHERE dismissed_at IS NOT NULL AND dismissed_at < @cutoff";
            using var command = new SqliteCommand(sql, _connection);
            command.Parameters.AddWithValue("@cutoff", cutoffDate);
            var deleted = await command.ExecuteNonQueryAsync();
            
            if (deleted > 0)
            {
                _logger.Info("Cleaned up {0} old insights", deleted);
            }
        }

        /// <summary>
        /// Gets insights by type.
        /// </summary>
        public async Task<List<Insight>> GetInsightsByTypeAsync(InsightType type)
        {
            EnsureInitialized();

            var insights = new List<Insight>();
            var sql = "SELECT * FROM insights WHERE type = @type AND dismissed_at IS NULL ORDER BY generated_at DESC";

            using var command = new SqliteCommand(sql, _connection);
            command.Parameters.AddWithValue("@type", type.ToString());
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                insights.Add(ReadInsight(reader));
            }

            return insights;
        }

        /// <summary>
        /// Removes all insights for a specific entity (e.g., when entity is deleted).
        /// </summary>
        public async Task RemoveInsightsForEntityAsync(string entityType, int entityId)
        {
            EnsureInitialized();

            var sql = "DELETE FROM insights WHERE entity_type = @entity_type AND entity_id = @entity_id";
            using var command = new SqliteCommand(sql, _connection);
            command.Parameters.AddWithValue("@entity_type", entityType);
            command.Parameters.AddWithValue("@entity_id", entityId);
            await command.ExecuteNonQueryAsync();
        }

        private async Task<int?> GetInsightIdByUniqueKeyAsync(string uniqueKey)
        {
            var sql = "SELECT id FROM insights WHERE unique_key = @unique_key AND dismissed_at IS NULL";
            using var command = new SqliteCommand(sql, _connection);
            command.Parameters.AddWithValue("@unique_key", uniqueKey);
            var result = await command.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : null;
        }

        private static Insight ReadInsight(SqliteDataReader reader)
        {
            return new Insight
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                UniqueKey = reader.GetString(reader.GetOrdinal("unique_key")),
                Type = Enum.Parse<InsightType>(reader.GetString(reader.GetOrdinal("type"))),
                Severity = Enum.Parse<InsightSeverity>(reader.GetString(reader.GetOrdinal("severity"))),
                Title = reader.GetString(reader.GetOrdinal("title")),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? string.Empty : reader.GetString(reader.GetOrdinal("description")),
                ActionSuggestion = reader.IsDBNull(reader.GetOrdinal("action_suggestion")) ? string.Empty : reader.GetString(reader.GetOrdinal("action_suggestion")),
                EntityType = reader.IsDBNull(reader.GetOrdinal("entity_type")) ? null : reader.GetString(reader.GetOrdinal("entity_type")),
                EntityId = reader.IsDBNull(reader.GetOrdinal("entity_id")) ? null : reader.GetInt32(reader.GetOrdinal("entity_id")),
                GeneratedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("generated_at"))),
                DismissedAt = reader.IsDBNull(reader.GetOrdinal("dismissed_at")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("dismissed_at"))),
                ActedOnAt = reader.IsDBNull(reader.GetOrdinal("acted_on_at")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("acted_on_at"))),
                IsRead = reader.GetInt32(reader.GetOrdinal("is_read")) == 1
            };
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("InsightStore not initialized. Call InitializeAsync first.");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            _connection?.Close();
            _connection?.Dispose();
            _disposed = true;
        }
    }
}
