using System.IO;

namespace Tracker.Classes
{
    /// <summary>
    /// Configuration settings for database connections.
    /// 
    /// Tracker supports two database providers:
    /// 1. SQLite - Local database stored in %LocalAppData%\Tracker\tracker.db
    ///    - Best for single-user, standalone deployments
    ///    - Zero configuration required
    ///    - Data stays on user's machine
    /// 
    /// 2. SQL Server - Networked database for enterprise deployments
    ///    - Supports Windows Authentication and SQL Authentication
    ///    - Can also connect via ODBC DSN
    ///    - Enables team-wide data sharing
    /// 
    /// Usage:
    /// <code>
    /// // Local SQLite (default)
    /// var settings = new DatabaseSettings { Type = DatabaseType.SQLite };
    /// 
    /// // SQL Server with Windows Auth
    /// var settings = new DatabaseSettings 
    /// {
    ///     Type = DatabaseType.SqlServer,
    ///     Server = "server\\instance",
    ///     Database = "TrackerDB",
    ///     UseWindowsAuth = true
    /// };
    /// 
    /// // Get connection string
    /// string connectionString = settings.GetConnectionString();
    /// </code>
    /// </summary>
    public class DatabaseSettings
    {
        /// <summary>
        /// The type of database to connect to.
        /// </summary>
        public DatabaseType Type { get; set; } = DatabaseType.SQLite;

        /// <summary>
        /// SQL Server hostname or IP address.
        /// </summary>
        public string Server { get; set; } = string.Empty;

        /// <summary>
        /// SQL Server database name.
        /// </summary>
        public string Database { get; set; } = "TrackerDB";

        /// <summary>
        /// Use Windows Authentication for SQL Server.
        /// </summary>
        public bool UseWindowsAuth { get; set; } = true;

        /// <summary>
        /// SQL Server username (if not using Windows Auth).
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// SQL Server password (if not using Windows Auth).
        /// Stored encrypted.
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Use ODBC DSN instead of direct connection.
        /// </summary>
        public bool UseOdbc { get; set; } = false;

        /// <summary>
        /// ODBC Data Source Name.
        /// </summary>
        public string OdbcDsn { get; set; } = string.Empty;

        /// <summary>
        /// Connection timeout in seconds.
        /// </summary>
        public int ConnectionTimeout { get; set; } = 30;

        /// <summary>
        /// Trust server certificate (useful for dev/self-signed certs).
        /// </summary>
        public bool TrustServerCertificate { get; set; } = true;

        /// <summary>
        /// Whether the initial setup has been completed.
        /// </summary>
        public bool SetupCompleted { get; set; } = false;

        /// <summary>
        /// Last successful sync timestamp (for offline mode).
        /// </summary>
        public DateTime? LastSyncTimestamp { get; set; }

        /// <summary>
        /// Whether we're currently in offline mode (SQL Server configured but unavailable).
        /// </summary>
        public bool IsOfflineMode { get; set; } = false;

        #region PostgreSQL Settings

        /// <summary>
        /// PostgreSQL host address.
        /// Default is localhost for local development.
        /// </summary>
        public string PostgresHost { get; set; } = "localhost";

        /// <summary>
        /// PostgreSQL port number.
        /// Default is 5432.
        /// </summary>
        public int PostgresPort { get; set; } = 5432;

        /// <summary>
        /// PostgreSQL database name.
        /// </summary>
        public string PostgresDatabase { get; set; } = "tracker";

        /// <summary>
        /// PostgreSQL username for authentication.
        /// Uses a dedicated app user with limited privileges.
        /// </summary>
        public string PostgresUsername { get; set; } = "tracker_app";

        /// <summary>
        /// PostgreSQL password for authentication.
        /// </summary>
        public string PostgresPassword { get; set; } = string.Empty;

        /// <summary>
        /// Whether to use SSL for PostgreSQL connections.
        /// Should be true for production, can be false for localhost.
        /// </summary>
        public bool PostgresUseSsl { get; set; } = false;

        /// <summary>
        /// PostgreSQL connection pool minimum size.
        /// </summary>
        public int PostgresPoolMinSize { get; set; } = 1;

        /// <summary>
        /// PostgreSQL connection pool maximum size.
        /// </summary>
        public int PostgresPoolMaxSize { get; set; } = 20;

        #endregion

    /// <summary>
    /// Custom path for SQLite database file.
    /// If empty, uses default %LocalAppData%\Tracker\tracker.db
    /// Can be set to a network share (e.g., \\server\share\TrackerData\tracker.db) for team sharing.
    /// </summary>
    public string CustomSqlitePath { get; set; } = string.Empty;
        /// <returns>
        /// A connection string suitable for the configured database provider:
        /// - SQLite: "Data Source=path\to\tracker.db"
        /// - SQL Server: Full ADO.NET connection string with server, database, and auth
        /// - ODBC: "DSN=datasourcename"
        /// </returns>
        public string GetConnectionString()
        {
            // SQLite uses a simple file-based connection string
            if (Type == DatabaseType.SQLite)
            {
                // Use custom path if specified, otherwise use default
                if (!string.IsNullOrWhiteSpace(CustomSqlitePath))
                {
                    // Ensure the directory exists
                    var directory = Path.GetDirectoryName(CustomSqlitePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    return $"Data Source={CustomSqlitePath}";
                }
                
                // Default path: %LocalAppData%\Tracker\tracker.db
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var trackerFolder = Path.Combine(appDataPath, "Tracker");
                return $"Data Source={Path.Combine(trackerFolder, "tracker.db")}";
            }

            // PostgreSQL connection string with connection pooling
            if (Type == DatabaseType.PostgreSQL)
            {
                return $"Host={PostgresHost};" +
                       $"Port={PostgresPort};" +
                       $"Database={PostgresDatabase};" +
                       $"Username={PostgresUsername};" +
                       $"Password={PostgresPassword};" +
                       $"Timeout={ConnectionTimeout};" +
                       $"Minimum Pool Size={PostgresPoolMinSize};" +
                       $"Maximum Pool Size={PostgresPoolMaxSize};" +
                       (PostgresUseSsl ? "SSL Mode=Require;" : "SSL Mode=Prefer;");
            }

            // ODBC connection - uses a pre-configured Data Source Name
            if (UseOdbc)
            {
                return $"DSN={OdbcDsn}";
            }

            // Build SQL Server connection string
            var builder = new System.Text.StringBuilder();
            
            // Server can include instance name (e.g., "server\instance")
            builder.Append($"Server={Server};");
            builder.Append($"Database={Database};");

            // Authentication: Windows (integrated) or SQL Server credentials
            if (UseWindowsAuth)
            {
                // Uses the current Windows user's credentials
                builder.Append("Trusted_Connection=True;");
            }
            else
            {
                // SQL Server authentication with username/password
                builder.Append($"User Id={Username};");
                builder.Append($"Password={Password};");
            }

            builder.Append($"Connect Timeout={ConnectionTimeout};");

            // TrustServerCertificate is useful for development/self-signed certs
            // In production, you may want to set this to false for security
            if (TrustServerCertificate)
            {
                builder.Append("TrustServerCertificate=True;");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Gets the PostgreSQL connection string for authentication (before user is known).
        /// </summary>
        public string GetPostgresAuthConnectionString()
        {
            return $"Host={PostgresHost};" +
                   $"Port={PostgresPort};" +
                   $"Database={PostgresDatabase};" +
                   $"Username={PostgresUsername};" +
                   $"Password={PostgresPassword};" +
                   $"Timeout={ConnectionTimeout};" +
                   (PostgresUseSsl ? "SSL Mode=Require;" : "SSL Mode=Prefer;");
        }

        /// <summary>
        /// Gets the SQLite database path.
        /// </summary>
        public static string GetSqlitePath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var trackerFolder = Path.Combine(appDataPath, "Tracker");
            
            if (!Directory.Exists(trackerFolder))
            {
                Directory.CreateDirectory(trackerFolder);
            }
            
            return Path.Combine(trackerFolder, "tracker.db");
        }

        #region Vector Storage Strategy

        /// <summary>
        /// Gets the vector storage provider type based on the database configuration.
        /// PostgreSQL uses pgvector, SQL Server uses VARBINARY with app-side similarity,
        /// SQLite uses the legacy local vector store.
        /// </summary>
        public VectorStorageProvider GetVectorStorageProvider()
        {
            return Type switch
            {
                DatabaseType.PostgreSQL => VectorStorageProvider.PostgreSQL,
                DatabaseType.SqlServer => VectorStorageProvider.SqlServer,
                _ => VectorStorageProvider.Legacy
            };
        }

        /// <summary>
        /// Whether vector embeddings should be stored in the main database (PostgreSQL/SQL Server)
        /// or in a separate local SQLite database (legacy mode).
        /// </summary>
        public bool UseUnifiedVectorStorage => Type == DatabaseType.PostgreSQL || Type == DatabaseType.SqlServer;

        #endregion
    }

    /// <summary>
    /// Supported vector storage providers.
    /// </summary>
    public enum VectorStorageProvider
    {
        /// <summary>
        /// Legacy SQLite vector store at %LocalAppData%\Tracker\vectors.db.
        /// Used for backwards compatibility when no PostgreSQL/SQL Server is configured.
        /// </summary>
        Legacy = 0,

        /// <summary>
        /// PostgreSQL with pgvector extension.
        /// Provides native vector similarity operations.
        /// </summary>
        PostgreSQL = 1,

        /// <summary>
        /// SQL Server with VARBINARY storage.
        /// Similarity calculated in application layer.
        /// </summary>
        SqlServer = 2
    }

    /// <summary>
    /// Supported database types.
    /// </summary>
    public enum DatabaseType
    {
        /// <summary>
        /// Local SQLite database stored on this machine.
        /// </summary>
        SQLite = 0,

        /// <summary>
        /// Remote SQL Server instance.
        /// </summary>
        SqlServer = 1,

        /// <summary>
        /// PostgreSQL database with Row-Level Security.
        /// This is the preferred option for new deployments.
        /// </summary>
        PostgreSQL = 2
    }
}

