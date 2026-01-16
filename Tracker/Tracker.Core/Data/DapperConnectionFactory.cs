using System;
using System.Data;
using Npgsql;
using Tracker.Core.Services.Backend;

namespace Tracker.Core.Data
{
    /// <summary>
    /// Creates and manages Npgsql connections to Supabase PostgreSQL database.
    /// 
    /// This is the ONLY place where database connections are created.
    /// All repositories use this factory - never create connections directly.
    /// 
    /// Connection string is configured in SupabaseConfig.DatabaseConnectionString.
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
    /// Uses SupabaseConfig.DatabaseConnectionString for all connections.
    /// </summary>
    public class DapperConnectionFactory : IDapperConnectionFactory
    {
        private readonly string _connectionString;
        
        private static DapperConnectionFactory? _instance;
        private static readonly object _lock = new();
        
        /// <summary>
        /// Singleton instance for services that need a shared connection factory.
        /// </summary>
        public static DapperConnectionFactory Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new DapperConnectionFactory();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Create factory using SupabaseConfig.DatabaseConnectionString.
        /// Falls back to environment variable TRACKER_SUPABASE_CONNECTION_STRING if set.
        /// </summary>
        public DapperConnectionFactory()
        {
            // Environment variable takes precedence (for container/cloud deployments)
            var envConnection = Environment.GetEnvironmentVariable("TRACKER_SUPABASE_CONNECTION_STRING");
            _connectionString = !string.IsNullOrEmpty(envConnection) 
                ? envConnection 
                : SupabaseConfig.DatabaseConnectionString;
        }

        public IDbConnection CreateConnection()
        {
            var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}
