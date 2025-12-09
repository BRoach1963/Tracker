using System.Windows.Input;
using Tracker.Classes;
using Tracker.Command;
using Tracker.Database;
using Tracker.Managers;

namespace Tracker.ViewModels.DialogViewModels
{
    /// <summary>
    /// ViewModel for the first-run Setup Wizard dialog.
    /// 
    /// This wizard guides users through initial database configuration:
    /// 
    /// Step 1 - Choose Database Type:
    ///   - Local (SQLite): Stores data on user's machine, no setup required
    ///   - SQL Server: Connect to networked server for team-wide access
    /// 
    /// Step 2 - SQL Server Configuration (if selected):
    ///   - Server name and database
    ///   - Authentication method (Windows Auth, SQL Auth, or ODBC)
    ///   - Connection testing
    /// 
    /// Step 3 - Summary:
    ///   - Review configuration
    ///   - Option to include sample data
    ///   - Complete setup
    /// 
    /// The wizard appears automatically on first launch (when SetupCompleted = false)
    /// and can be triggered later from Settings if the user wants to change databases.
    /// 
    /// After completion:
    /// - Settings are saved to %LocalAppData%\Tracker\TrackerSettings.json
    /// - Database is created/connected
    /// - Optional sample data is seeded
    /// - App continues to login screen
    /// </summary>
    public class SetupWizardViewModel : BaseDialogViewModel
    {
        #region Fields

        private int _currentStep = 1;
        private DatabaseType _selectedDatabaseType = DatabaseType.SQLite;
        
        // SQL Server fields
        private string _server = string.Empty;
        private string _database = "TrackerDB";
        private bool _useWindowsAuth = true;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private bool _useOdbc = false;
        private string _odbcDsn = string.Empty;
        private bool _trustServerCertificate = true;

        // Status
        private bool _isTestingConnection = false;
        private bool _connectionTestSucceeded = false;
        private string _connectionStatus = string.Empty;
        private bool _createDatabase = true;
        private bool _includeSampleData = true;

        // Commands
        private ICommand? _selectLocalCommand;
        private ICommand? _selectSqlServerCommand;
        private ICommand? _testConnectionCommand;
        private ICommand? _nextCommand;
        private ICommand? _backCommand;
        private ICommand? _finishCommand;

        #endregion

        #region Constructor

        public SetupWizardViewModel(Action? callback) : base(callback)
        {
        }

        #endregion

        #region Commands

        public ICommand SelectLocalCommand => _selectLocalCommand ??= new TrackerCommand(ExecuteSelectLocal);
        public ICommand SelectSqlServerCommand => _selectSqlServerCommand ??= new TrackerCommand(ExecuteSelectSqlServer);
        public ICommand TestConnectionCommand => _testConnectionCommand ??= new TrackerCommand(ExecuteTestConnection, CanTestConnection);
        public ICommand NextCommand => _nextCommand ??= new TrackerCommand(ExecuteNext, CanExecuteNext);
        public ICommand BackCommand => _backCommand ??= new TrackerCommand(ExecuteBack, CanExecuteBack);
        public ICommand FinishCommand => _finishCommand ??= new TrackerCommand(ExecuteFinish, CanExecuteFinish);

        #endregion

        #region Properties

        public int CurrentStep
        {
            get => _currentStep;
            set
            {
                _currentStep = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsStep1));
                RaisePropertyChanged(nameof(IsStep2));
                RaisePropertyChanged(nameof(IsStep3));
            }
        }

        public bool IsStep1 => CurrentStep == 1;
        public bool IsStep2 => CurrentStep == 2;
        public bool IsStep3 => CurrentStep == 3;

        public DatabaseType SelectedDatabaseType
        {
            get => _selectedDatabaseType;
            set
            {
                _selectedDatabaseType = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsLocalSelected));
                RaisePropertyChanged(nameof(IsSqlServerSelected));
            }
        }

        public bool IsLocalSelected => SelectedDatabaseType == DatabaseType.SQLite;
        public bool IsSqlServerSelected => SelectedDatabaseType == DatabaseType.SqlServer;

        public string Server
        {
            get => _server;
            set
            {
                _server = value;
                RaisePropertyChanged();
                ConnectionTestSucceeded = false;
            }
        }

        public string Database
        {
            get => _database;
            set
            {
                _database = value;
                RaisePropertyChanged();
                ConnectionTestSucceeded = false;
            }
        }

        public bool UseWindowsAuth
        {
            get => _useWindowsAuth;
            set
            {
                _useWindowsAuth = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ShowCredentials));
                ConnectionTestSucceeded = false;
            }
        }

        public bool ShowCredentials => !UseWindowsAuth && !UseOdbc;

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                RaisePropertyChanged();
                ConnectionTestSucceeded = false;
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                RaisePropertyChanged();
                ConnectionTestSucceeded = false;
            }
        }

        public bool UseOdbc
        {
            get => _useOdbc;
            set
            {
                _useOdbc = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ShowCredentials));
                RaisePropertyChanged(nameof(ShowDirectConnection));
                ConnectionTestSucceeded = false;
            }
        }

        public bool ShowDirectConnection => !UseOdbc;

        public string OdbcDsn
        {
            get => _odbcDsn;
            set
            {
                _odbcDsn = value;
                RaisePropertyChanged();
                ConnectionTestSucceeded = false;
            }
        }

        public bool TrustServerCertificate
        {
            get => _trustServerCertificate;
            set
            {
                _trustServerCertificate = value;
                RaisePropertyChanged();
            }
        }

        public bool IsTestingConnection
        {
            get => _isTestingConnection;
            set
            {
                _isTestingConnection = value;
                RaisePropertyChanged();
            }
        }

        public bool ConnectionTestSucceeded
        {
            get => _connectionTestSucceeded;
            set
            {
                _connectionTestSucceeded = value;
                RaisePropertyChanged();
            }
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set
            {
                _connectionStatus = value;
                RaisePropertyChanged();
            }
        }

        public bool CreateDatabase
        {
            get => _createDatabase;
            set
            {
                _createDatabase = value;
                RaisePropertyChanged();
            }
        }

        public bool IncludeSampleData
        {
            get => _includeSampleData;
            set
            {
                _includeSampleData = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Command Implementations

        private void ExecuteSelectLocal(object? parameter)
        {
            SelectedDatabaseType = DatabaseType.SQLite;
            CurrentStep = 3; // Skip SQL config, go to summary
        }

        private void ExecuteSelectSqlServer(object? parameter)
        {
            SelectedDatabaseType = DatabaseType.SqlServer;
            CurrentStep = 2; // Go to SQL configuration
        }

        private bool CanTestConnection(object? parameter)
        {
            if (UseOdbc)
                return !string.IsNullOrWhiteSpace(OdbcDsn);
            
            return !string.IsNullOrWhiteSpace(Server);
        }

        private async void ExecuteTestConnection(object? parameter)
        {
            IsTestingConnection = true;
            ConnectionStatus = "Testing connection...";
            ConnectionTestSucceeded = false;

            try
            {
                var settings = BuildDatabaseSettings();
                var result = await TrackerDbManager.Instance!.TestConnectionAsync(settings);
                
                if (result.Success)
                {
                    ConnectionTestSucceeded = true;
                    ConnectionStatus = result.DatabaseExists 
                        ? "✓ Connected successfully! Database exists."
                        : "✓ Connected successfully! Database will be created.";
                    CreateDatabase = !result.DatabaseExists;
                }
                else
                {
                    ConnectionTestSucceeded = false;
                    ConnectionStatus = $"✗ Connection failed: {result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                ConnectionTestSucceeded = false;
                ConnectionStatus = $"✗ Error: {ex.Message}";
            }
            finally
            {
                IsTestingConnection = false;
            }
        }

        private bool CanExecuteNext(object? parameter)
        {
            return CurrentStep < 3;
        }

        private void ExecuteNext(object? parameter)
        {
            if (CurrentStep == 2 && IsSqlServerSelected && !ConnectionTestSucceeded)
            {
                ConnectionStatus = "Please test the connection before proceeding.";
                return;
            }
            
            CurrentStep++;
        }

        private bool CanExecuteBack(object? parameter)
        {
            return CurrentStep > 1;
        }

        private void ExecuteBack(object? parameter)
        {
            if (CurrentStep == 3 && IsLocalSelected)
            {
                CurrentStep = 1; // Go back to selection
            }
            else
            {
                CurrentStep--;
            }
        }

        private bool CanExecuteFinish(object? parameter)
        {
            if (IsLocalSelected)
                return true;
            
            return ConnectionTestSucceeded;
        }

        private async void ExecuteFinish(object? parameter)
        {
            try
            {
                var settings = BuildDatabaseSettings();
                settings.SetupCompleted = true;

                // Save settings
                UserSettingsManager.Instance.Settings.Database = settings;
                UserSettingsManager.Instance.SaveSettings();

                // Initialize database
                await TrackerDbManager.Instance!.InitializeAsync(settings, CreateDatabase, IncludeSampleData);

                // Signal completion
                DialogResult.Cancelled = false;
                Callback?.Invoke();
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"Setup failed: {ex.Message}";
                NotificationManager.Instance.ShowError("Setup Failed", ex.Message);
            }
        }

        #endregion

        #region Private Methods

        private DatabaseSettings BuildDatabaseSettings()
        {
            return new DatabaseSettings
            {
                Type = SelectedDatabaseType,
                Server = Server,
                Database = Database,
                UseWindowsAuth = UseWindowsAuth,
                Username = Username,
                Password = Password,
                UseOdbc = UseOdbc,
                OdbcDsn = OdbcDsn,
                TrustServerCertificate = TrustServerCertificate
            };
        }

        #endregion
    }
}

