using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Npgsql;
using Tracker.Classes;
using Tracker.Command;
using Tracker.Common;
using Tracker.DataModels;
using Tracker.Database;
using Tracker.Helpers;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.ViewModels
{
    public class AdminWindowViewModel : BaseViewModel
    {
        private readonly ILogger _logger;
        
        // Properties
        private int _totalUsers;
        public int TotalUsers
        {
            get => _totalUsers;
            set { _totalUsers = value; RaisePropertyChanged(); }
        }

        private int _totalRecords;
        public int TotalRecords
        {
            get => _totalRecords;
            set { _totalRecords = value; RaisePropertyChanged(); }
        }

        private string _databaseSize = "0 MB";
        public string DatabaseSize
        {
            get => _databaseSize;
            set { _databaseSize = value; RaisePropertyChanged(); }
        }

        private string _databasePath = "Loading...";
        public string DatabasePath
        {
            get => _databasePath;
            set { _databasePath = value; RaisePropertyChanged(); }
        }

        private string _databaseType = "Unknown";
        public string DatabaseType
        {
            get => _databaseType;
            set { _databaseType = value; RaisePropertyChanged(); }
        }

        private string _connectionStatus = "Checking...";
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set { _connectionStatus = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<User> _users = new ObservableCollection<User>();
        public ObservableCollection<User> Users
        {
            get => _users;
            set { _users = value; RaisePropertyChanged(); }
        }

        private User? _selectedUser;
        public User? SelectedUser
        {
            get => _selectedUser;
            set { _selectedUser = value; RaisePropertyChanged(); }
        }

        // SQL Query Editor Properties
        private string _sqlQuery = "-- Enter your SQL query here\nSELECT * FROM Users;";
        public string SqlQuery
        {
            get => _sqlQuery;
            set { _sqlQuery = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<string> _tables = new ObservableCollection<string>();
        public ObservableCollection<string> Tables
        {
            get => _tables;
            set { _tables = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<QueryResultSet> _queryResultSets = new ObservableCollection<QueryResultSet>();
        public ObservableCollection<QueryResultSet> QueryResultSets
        {
            get => _queryResultSets;
            set { _queryResultSets = value; RaisePropertyChanged(); }
        }

        // Autocomplete suggestions
        private ObservableCollection<string> _autocompleteSuggestions = new ObservableCollection<string>();
        public ObservableCollection<string> AutocompleteSuggestions
        {
            get => _autocompleteSuggestions;
            set { _autocompleteSuggestions = value; RaisePropertyChanged(); }
        }

        private bool _showAutoComplete;
        public bool ShowAutoComplete
        {
            get => _showAutoComplete;
            set { _showAutoComplete = value; RaisePropertyChanged(); }
        }

        // Schema cache for autocomplete
        private Dictionary<string, List<string>> _tableColumns = new Dictionary<string, List<string>>();
        private List<string> _sqlKeywords = new List<string>
        {
            "SELECT", "FROM", "WHERE", "AND", "OR", "NOT", "IN", "LIKE", "BETWEEN",
            "ORDER BY", "GROUP BY", "HAVING", "JOIN", "LEFT JOIN", "RIGHT JOIN", "INNER JOIN",
            "ON", "AS", "DISTINCT", "COUNT", "SUM", "AVG", "MAX", "MIN",
            "INSERT INTO", "VALUES", "UPDATE", "SET", "DELETE FROM",
            "CREATE TABLE", "ALTER TABLE", "DROP TABLE", "LIMIT", "OFFSET",
            "NULL", "IS NULL", "IS NOT NULL", "ASC", "DESC", "UNION", "UNION ALL"
        };

        private string _queryStatus = "Ready";
        public string QueryStatus
        {
            get => _queryStatus;
            set { _queryStatus = value; RaisePropertyChanged(); }
        }

        // Commands
        public ICommand ViewUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand BackupDatabaseCommand { get; }
        public ICommand RestoreDatabaseCommand { get; }
        public ICommand OptimizeDatabaseCommand { get; }
        public ICommand ExportDataCommand { get; }
        public ICommand ImportDataCommand { get; }
        public ICommand ClearDataCommand { get; }
        public ICommand ExecuteQueryCommand { get; }
        public ICommand RefreshTablesCommand { get; }

        public AdminWindowViewModel()
        {
            _logger = LoggingManager.GetComponentLogger("AdminWindow");

            // Initialize commands
            ViewUserCommand = new AsyncCommand(async _ => await ViewUserAsync());
            DeleteUserCommand = new AsyncCommand(async _ => await DeleteUserAsync());
            BackupDatabaseCommand = new AsyncCommand(async _ => await BackupDatabaseAsync());
            RestoreDatabaseCommand = new AsyncCommand(async _ => await RestoreDatabaseAsync());
            OptimizeDatabaseCommand = new AsyncCommand(async _ => await OptimizeDatabaseAsync());
            ExportDataCommand = new AsyncCommand(async _ => await ExportDataAsync());
            ImportDataCommand = new AsyncCommand(async _ => await ImportDataAsync());
            ClearDataCommand = new AsyncCommand(async _ => await ClearDataAsync());
            ExecuteQueryCommand = new AsyncCommand(async _ => await ExecuteQueryAsync());
            RefreshTablesCommand = new AsyncCommand(async _ => await RefreshTablesAsync());

            // Load data
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                _logger.Info("AdminWindow: Loading database information...");
                
                // Get current user
                var currentUser = UserSettingsManager.Instance.CurrentUser;
                TotalUsers = 1; // Placeholder

                // Get database settings and path
                var dbSettings = UserSettingsManager.Instance.Settings.Database;
                DatabaseType = dbSettings.Type switch
                {
                    Classes.DatabaseType.SQLite => "SQLite",
                    Classes.DatabaseType.PostgreSQL => "PostgreSQL",
                    Classes.DatabaseType.SqlServer => "SQL Server",
                    _ => "Unknown"
                };
                
                _logger.Info($"AdminWindow: Database type configured as: {DatabaseType}");
                
                if (dbSettings.Type == Classes.DatabaseType.SQLite)
                {
                    // Check if custom path is configured
                    string sqlitePath;
                    if (!string.IsNullOrWhiteSpace(dbSettings.CustomSqlitePath))
                    {
                        sqlitePath = dbSettings.CustomSqlitePath;
                        _logger.Info($"AdminWindow: Using CUSTOM SQLite path: {sqlitePath}");
                    }
                    else
                    {
                        sqlitePath = DatabaseSettings.GetSqlitePath();
                        _logger.Info($"AdminWindow: Using DEFAULT SQLite path: {sqlitePath}");
                    }
                    
                    DatabasePath = sqlitePath;
                    
                    if (File.Exists(sqlitePath))
                    {
                        var fileInfo = new FileInfo(sqlitePath);
                        DatabaseSize = $"{fileInfo.Length / 1024.0 / 1024.0:F2} MB";
                        ConnectionStatus = "✅ Connected";
                        _logger.Info($"AdminWindow: Database file found. Size: {DatabaseSize}, Last modified: {fileInfo.LastWriteTime}");
                    }
                    else
                    {
                        DatabaseSize = "N/A";
                        ConnectionStatus = "⚠️ File not found";
                        _logger.Warn($"AdminWindow: Database file NOT FOUND at: {sqlitePath}");
                    }
                }
                else if (dbSettings.Type == Classes.DatabaseType.PostgreSQL)
                {
                    // PostgreSQL
                    DatabasePath = $"Host: {dbSettings.PostgresHost}:{dbSettings.PostgresPort}\nDatabase: {dbSettings.PostgresDatabase}";
                    ConnectionStatus = "✅ PostgreSQL";
                    DatabaseSize = "N/A (PostgreSQL)";
                    _logger.Info($"AdminWindow: PostgreSQL connection - Host: {dbSettings.PostgresHost}, Database: {dbSettings.PostgresDatabase}");
                }
                else
                {
                    // SQL Server
                    DatabasePath = $"Server: {dbSettings.Server}\nDatabase: {dbSettings.Database}";
                    ConnectionStatus = "✅ SQL Server";
                    DatabaseSize = "N/A (SQL Server)";
                    _logger.Info($"AdminWindow: SQL Server connection - Server: {dbSettings.Server}, Database: {dbSettings.Database}");
                }
                
                // Load database tables
                await RefreshTablesAsync();
                // Placeholder for total records
                TotalRecords = 0;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to load admin data");
                MessageBoxHelper.Show($"Failed to load data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ViewUserAsync()
        {
            if (SelectedUser == null)
            {
                MessageBoxHelper.Show(TrackerConstants.PleaseSelectUserFirst, TrackerConstants.NoUserSelected, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Show user details
            MessageBoxHelper.Show(
                $"User Details:\n\n" +
                $"ID: {SelectedUser.Id}\n" +
                $"Username: {SelectedUser.Username}\n" +
                $"Email: {SelectedUser.Email}\n" +
                $"Admin: {SelectedUser.IsAdmin}\n" +
                $"Active: {SelectedUser.IsActive}", 
                "User Details", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }

        private async Task DeleteUserAsync()
        {
            if (SelectedUser == null)
            {
                MessageBoxHelper.Show(TrackerConstants.PleaseSelectUserFirst, TrackerConstants.NoUserSelected, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBoxHelper.Show(
                $"Delete user '{SelectedUser.Username}'?\n\nThis will delete all their data.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                MessageBoxHelper.Show(TrackerConstants.DeleteUserComingSoon, TrackerConstants.FeaturePreview, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async Task BackupDatabaseAsync()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "SQLite Database|*.db",
                    DefaultExt = ".db",
                    FileName = $"tracker_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db"
                };

                if (dialog.ShowDialog() == true)
                {
                    var sourcePath = DatabaseSettings.GetSqlitePath();
                    File.Copy(sourcePath, dialog.FileName, true);
                    MessageBoxHelper.Show($"Database backed up to:\n{dialog.FileName}", "Backup Created", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to backup database");
                MessageBoxHelper.Show($"Failed to backup database: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RestoreDatabaseAsync()
        {
            MessageBoxHelper.Show(TrackerConstants.RestoreDbComingSoon, TrackerConstants.FeaturePreview, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task OptimizeDatabaseAsync()
        {
            MessageBoxHelper.Show(TrackerConstants.OptimizeDbComingSoon, TrackerConstants.FeaturePreview, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task ExportDataAsync()
        {
            MessageBoxHelper.Show(TrackerConstants.ExportDataComingSoon, TrackerConstants.FeaturePreview, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task ImportDataAsync()
        {
            MessageBoxHelper.Show(TrackerConstants.ImportDataComingSoon, TrackerConstants.FeaturePreview, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task ClearDataAsync()
        {
            var result = MessageBoxHelper.Show(
                "This will DELETE ALL DATA!\n\nThis action CANNOT be undone.\n\nAre you sure?",
                "DANGER: Clear All Data",
                MessageBoxButton.YesNo,
                MessageBoxImage.Error);

            if (result == MessageBoxResult.Yes)
            {
                MessageBoxHelper.Show(TrackerConstants.ClearDataComingSoon, TrackerConstants.FeaturePreview, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async Task RefreshTablesAsync()
        {
            try
            {
                Tables.Clear();
                _tableColumns.Clear();
                var dbSettings = UserSettingsManager.Instance.Settings.Database;
                
                if (dbSettings.Type == Classes.DatabaseType.SQLite)
                {
                    var sqlitePath = GetCurrentDatabasePath();
                    using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={sqlitePath}");
                    await connection.OpenAsync();
                    
                    // Get all tables
                    using var command = connection.CreateCommand();
                    command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
                    
                    using var reader = await command.ExecuteReaderAsync();
                    var tableNames = new List<string>();
                    while (await reader.ReadAsync())
                    {
                        tableNames.Add(reader.GetString(0));
                    }
                    
                    foreach (var table in tableNames)
                    {
                        Tables.Add(table);
                        
                        // Get columns for each table
                        using var colCommand = connection.CreateCommand();
                        colCommand.CommandText = $"PRAGMA table_info({table});";
                        using var colReader = await colCommand.ExecuteReaderAsync();
                        
                        var columns = new List<string>();
                        while (await colReader.ReadAsync())
                        {
                            columns.Add(colReader.GetString(1)); // Column name is at index 1
                        }
                        _tableColumns[table] = columns;
                    }
                }
                else if (dbSettings.Type == Classes.DatabaseType.PostgreSQL)
                {
                    var connectionString = $"Host={dbSettings.PostgresHost};Port={dbSettings.PostgresPort};Database={dbSettings.PostgresDatabase};Username={dbSettings.PostgresUsername};Password={dbSettings.PostgresPassword}";
                    await using var connection = new NpgsqlConnection(connectionString);
                    await connection.OpenAsync();
                    
                    // Get all tables in public schema
                    await using var command = new NpgsqlCommand(
                        "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name;", 
                        connection);
                    
                    await using var reader = await command.ExecuteReaderAsync();
                    var tableNames = new List<string>();
                    while (await reader.ReadAsync())
                    {
                        tableNames.Add(reader.GetString(0));
                    }
                    await reader.CloseAsync();
                    
                    foreach (var table in tableNames)
                    {
                        Tables.Add(table);
                        
                        // Get columns for each table
                        await using var colCommand = new NpgsqlCommand(
                            "SELECT column_name FROM information_schema.columns WHERE table_schema = 'public' AND table_name = @table ORDER BY ordinal_position;",
                            connection);
                        colCommand.Parameters.AddWithValue("@table", table);
                        await using var colReader = await colCommand.ExecuteReaderAsync();
                        
                        var columns = new List<string>();
                        while (await colReader.ReadAsync())
                        {
                            columns.Add(colReader.GetString(0));
                        }
                        _tableColumns[table] = columns;
                    }
                }
                
                QueryStatus = $"Found {Tables.Count} tables";
                _logger.Info($"Schema loaded: {Tables.Count} tables, {_tableColumns.Values.Sum(c => c.Count)} columns");
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to refresh tables");
                QueryStatus = $"Error: {ex.Message}";
            }
        }

        private string GetCurrentDatabasePath()
        {
            var dbSettings = UserSettingsManager.Instance.Settings.Database;
            if (!string.IsNullOrWhiteSpace(dbSettings.CustomSqlitePath))
                return dbSettings.CustomSqlitePath;
            return DatabaseSettings.GetSqlitePath();
        }

        public void UpdateAutocompleteSuggestions(string currentWord)
        {
            AutocompleteSuggestions.Clear();
            
            if (string.IsNullOrWhiteSpace(currentWord) || currentWord.Length < 2)
            {
                ShowAutoComplete = false;
                return;
            }

            var upperWord = currentWord.ToUpperInvariant();
            var suggestions = new List<string>();

            // Add matching SQL keywords
            suggestions.AddRange(_sqlKeywords.Where(k => k.StartsWith(upperWord, StringComparison.OrdinalIgnoreCase)));
            
            // Add matching table names
            suggestions.AddRange(Tables.Where(t => t.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase)));
            
            // Add matching column names from all tables
            foreach (var table in _tableColumns)
            {
                suggestions.AddRange(table.Value.Where(c => c.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase))
                    .Select(c => $"{c} ({table.Key})"));
            }

            foreach (var suggestion in suggestions.Distinct().Take(15))
            {
                AutocompleteSuggestions.Add(suggestion);
            }

            ShowAutoComplete = AutocompleteSuggestions.Count > 0;
        }

        private async Task ExecuteQueryAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SqlQuery))
                {
                    QueryStatus = "No query to execute";
                    return;
                }

                QueryStatus = "Executing...";
                QueryResultSets.Clear();
                
                var dbSettings = UserSettingsManager.Instance.Settings.Database;
                
                if (dbSettings.Type == Classes.DatabaseType.SQLite)
                {
                    var sqlitePath = GetCurrentDatabasePath();
                    _logger.Info($"Executing query against: {sqlitePath}");
                    
                    using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={sqlitePath}");
                    await connection.OpenAsync();
                    
                    // Split queries by semicolon (like SSMS)
                    var queries = SplitQueries(SqlQuery);
                    int totalRows = 0;
                    int queryIndex = 0;
                    
                    foreach (var query in queries)
                    {
                        if (string.IsNullOrWhiteSpace(query)) continue;
                        
                        queryIndex++;
                        var trimmedQuery = query.Trim();
                        
                        // Skip comments
                        if (trimmedQuery.StartsWith("--")) continue;
                        
                        using var command = connection.CreateCommand();
                        command.CommandText = trimmedQuery;
                        
                        // Check if it's a SELECT query
                        var upperQuery = trimmedQuery.ToUpperInvariant();
                        if (upperQuery.StartsWith("SELECT") || upperQuery.StartsWith("PRAGMA"))
                        {
                            var dataTable = new System.Data.DataTable();
                            using var reader = await command.ExecuteReaderAsync();
                            dataTable.Load(reader);
                            
                            QueryResultSets.Add(new QueryResultSet
                            {
                                QueryText = trimmedQuery.Length > 50 ? trimmedQuery.Substring(0, 50) + "..." : trimmedQuery,
                                Results = dataTable,
                                RowCount = dataTable.Rows.Count,
                                Message = $"Query {queryIndex}: {dataTable.Rows.Count} row(s) returned"
                            });
                            
                            totalRows += dataTable.Rows.Count;
                        }
                        else
                        {
                            // Execute non-query (UPDATE, DELETE, INSERT, etc.)
                            var affectedRows = await command.ExecuteNonQueryAsync();
                            
                            QueryResultSets.Add(new QueryResultSet
                            {
                                QueryText = trimmedQuery.Length > 50 ? trimmedQuery.Substring(0, 50) + "..." : trimmedQuery,
                                Results = null,
                                RowCount = affectedRows,
                                Message = $"Query {queryIndex}: {affectedRows} row(s) affected"
                            });
                        }
                    }
                    
                    QueryStatus = $"Executed {queryIndex} query(ies). Total: {totalRows} row(s) returned";
                }
                else if (dbSettings.Type == Classes.DatabaseType.PostgreSQL)
                {
                    var connectionString = $"Host={dbSettings.PostgresHost};Port={dbSettings.PostgresPort};Database={dbSettings.PostgresDatabase};Username={dbSettings.PostgresUsername};Password={dbSettings.PostgresPassword}";
                    _logger.Info($"Executing query against PostgreSQL: {dbSettings.PostgresHost}/{dbSettings.PostgresDatabase}");
                    
                    await using var connection = new NpgsqlConnection(connectionString);
                    await connection.OpenAsync();
                    
                    // Split queries by semicolon
                    var queries = SplitQueries(SqlQuery);
                    int totalRows = 0;
                    int queryIndex = 0;
                    
                    foreach (var query in queries)
                    {
                        if (string.IsNullOrWhiteSpace(query)) continue;
                        
                        queryIndex++;
                        var trimmedQuery = query.Trim();
                        
                        // Skip comments
                        if (trimmedQuery.StartsWith("--")) continue;
                        
                        await using var command = new NpgsqlCommand(trimmedQuery, connection);
                        
                        // Check if it's a SELECT query
                        var upperQuery = trimmedQuery.ToUpperInvariant();
                        if (upperQuery.StartsWith("SELECT") || upperQuery.StartsWith("WITH") || upperQuery.StartsWith("SHOW"))
                        {
                            var dataTable = new System.Data.DataTable();
                            await using var reader = await command.ExecuteReaderAsync();
                            dataTable.Load(reader);
                            
                            QueryResultSets.Add(new QueryResultSet
                            {
                                QueryText = trimmedQuery.Length > 50 ? trimmedQuery.Substring(0, 50) + "..." : trimmedQuery,
                                Results = dataTable,
                                RowCount = dataTable.Rows.Count,
                                Message = $"Query {queryIndex}: {dataTable.Rows.Count} row(s) returned"
                            });
                            
                            totalRows += dataTable.Rows.Count;
                        }
                        else
                        {
                            // Execute non-query (UPDATE, DELETE, INSERT, etc.)
                            var affectedRows = await command.ExecuteNonQueryAsync();
                            
                            QueryResultSets.Add(new QueryResultSet
                            {
                                QueryText = trimmedQuery.Length > 50 ? trimmedQuery.Substring(0, 50) + "..." : trimmedQuery,
                                Results = null,
                                RowCount = affectedRows,
                                Message = $"Query {queryIndex}: {affectedRows} row(s) affected"
                            });
                        }
                    }
                    
                    QueryStatus = $"Executed {queryIndex} query(ies). Total: {totalRows} row(s) returned";
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to execute query");
                QueryStatus = $"Error: {ex.Message}";
            }
        }

        private List<string> SplitQueries(string sql)
        {
            var queries = new List<string>();
            var currentQuery = new System.Text.StringBuilder();
            bool inString = false;
            char stringChar = '\0';

            for (int i = 0; i < sql.Length; i++)
            {
                char c = sql[i];

                // Track string literals to avoid splitting on semicolons inside strings
                if ((c == '\'' || c == '"') && (i == 0 || sql[i - 1] != '\\'))
                {
                    if (!inString)
                    {
                        inString = true;
                        stringChar = c;
                    }
                    else if (c == stringChar)
                    {
                        inString = false;
                    }
                }

                if (c == ';' && !inString)
                {
                    var query = currentQuery.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(query))
                    {
                        queries.Add(query);
                    }
                    currentQuery.Clear();
                }
                else
                {
                    currentQuery.Append(c);
                }
            }

            // Don't forget the last query if it doesn't end with semicolon
            var lastQuery = currentQuery.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(lastQuery))
            {
                queries.Add(lastQuery);
            }

            return queries;
        }
    }

    /// <summary>
    /// Represents a single query result set (for multi-query support like SSMS)
    /// </summary>
    public class QueryResultSet : BaseViewModel
    {
        public string QueryText { get; set; } = string.Empty;
        public System.Data.DataTable? Results { get; set; }
        public int RowCount { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool HasResults => Results != null && Results.Rows.Count > 0;
    }
}
