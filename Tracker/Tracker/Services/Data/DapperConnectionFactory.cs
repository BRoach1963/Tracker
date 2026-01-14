using System;
using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Tracker.Classes;
using Tracker.Managers;

namespace Tracker.Services.Data
{
    /// <summary>
    /// Creates and manages Npgsql connections to Supabase PostgreSQL database.
    /// 
    /// This is the ONLY place where database connections are created.
    /// All repositories use this factory - never create connections directly.
    /// 
    /// Supabase connection string format:
    /// "Server=db.xxxxx.supabase.co;Port=5432;Database=postgres;User Id=postgres;Password=xxxxx;SSL Mode=Require;"
    /// </summary>
    public interface IDapperConnectionFactory
    {
        /// <summary>
        /// Create a new open connection to Supabase PostgreSQL database.
        /// </summary>
        IDbConnection CreateConnection();
    }

    /// <summary>
    /// Implementation of IDapperConnectionFactory.
    /// Uses DatabaseSettings from UserSettingsManager to get PostgreSQL connection parameters.
    /// Supports Supabase connection strings from appsettings.json as fallback.
    /// </summary>
    public class DapperConnectionFactory : IDapperConnectionFactory
    {
        private readonly string _connectionString;

        /// <summary>
        /// Create factory with connection string from configuration or settings.
        /// Priority:
        /// 1. appsettings.json ConnectionStrings:Supabase (if present)
        /// 2. UserSettingsManager.CurrentSettings.GetConnectionString() (user-configured)
        /// 3. Throws error if neither is available
        /// </summary>
        public DapperConnectionFactory(IConfiguration? configuration = null)
        {
            // Try configuration first (from appsettings.json)
            _connectionString = configuration?.GetConnectionString("Supabase") 
                ?? GetConnectionStringFromSettings();

            if (string.IsNullOrEmpty(_connectionString))
                throw new InvalidOperationException(
                    "No database connection string found. Configure in appsettings.json (ConnectionStrings:Supabase) or UserSettings.");
        }

        /// <summary>
        /// Get connection string from user settings (DatabaseSettings).
        /// Falls back to environment variable TRACKER_SUPABASE_CONNECTION_STRING if configured.
        /// </summary>
        private static string GetConnectionStringFromSettings()
        {
            try
            {
                // First try environment variable (for container/cloud deployments)
                var envConnection = Environment.GetEnvironmentVariable("TRACKER_SUPABASE_CONNECTION_STRING");
                if (!string.IsNullOrEmpty(envConnection))
                    return envConnection;

                // Fall back to user settings
                var settings = UserSettingsManager.Instance?.Settings?.Database;
                if (settings?.Type == DatabaseType.PostgreSQL)
                    return settings.GetConnectionString();

                return string.Empty;
            }
            catch
            {
                // If settings aren't initialized yet, return empty
                return string.Empty;
            }
        }

        public IDbConnection CreateConnection()
        {
            var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}
