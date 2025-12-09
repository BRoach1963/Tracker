using System.IO;

namespace Tracker.Classes
{
    /// <summary>
    /// Database connection settings supporting both local SQLite and remote SQL Server.
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

        /// <summary>
        /// Builds the connection string based on current settings.
        /// </summary>
        public string GetConnectionString()
        {
            if (Type == DatabaseType.SQLite)
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var trackerFolder = Path.Combine(appDataPath, "Tracker");
                return $"Data Source={Path.Combine(trackerFolder, "tracker.db")}";
            }

            if (UseOdbc)
            {
                return $"DSN={OdbcDsn}";
            }

            var builder = new System.Text.StringBuilder();
            builder.Append($"Server={Server};");
            builder.Append($"Database={Database};");

            if (UseWindowsAuth)
            {
                builder.Append("Trusted_Connection=True;");
            }
            else
            {
                builder.Append($"User Id={Username};");
                builder.Append($"Password={Password};");
            }

            builder.Append($"Connect Timeout={ConnectionTimeout};");

            if (TrustServerCertificate)
            {
                builder.Append("TrustServerCertificate=True;");
            }

            return builder.ToString();
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
        SqlServer = 1
    }
}

