using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Tracker.Classes;
using Tracker.Command;
using Tracker.Database;
using Tracker.Eventing;
using Tracker.Eventing.Messages;
using Tracker.Helpers;
using Tracker.Managers;
using Tracker.Services.Backend;

namespace Tracker.ViewModels.DialogViewModels
{
    /// <summary>
    /// ViewModel for the first-run Setup Wizard dialog.
    /// 
    /// This wizard guides users through initial database and account configuration:
    /// 
    /// Step 1 - Choose Database Type:
    ///   - Local (SQLite): Stores data on user's machine, no setup required
    ///   - SQL Server: Connect to networked server for team-wide access
    /// 
    /// Step 2 - SQL Server Configuration (if SQL Server selected):
    ///   - Server name and database
    ///   - Authentication method (Windows Auth, SQL Auth, or ODBC)
    ///   - Connection testing
    /// 
    /// Step 3 - Account Setup:
    ///   - Create new Tracker account (email/password)
    ///   - Or sign in to existing account
    ///   - Links app to cloud services and subscription
    /// 
    /// Step 4 - Summary:
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
    /// - User account linked to cloud backend
    /// - App continues to main interface
    /// </summary>
    public class SetupWizardViewModel : BaseDialogViewModel
    {
        #region Fields

        private int _currentStep = 1;
        private DatabaseType _selectedDatabaseType = DatabaseType.SQLite;
        
        // SQLite fields
        private string _customSqlitePath = string.Empty;
        private bool _useCustomSqlitePath = false;
        
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

        // Account Setup (Supabase)
        private bool _isCreatingAccount = true;
        private string _accountEmail = string.Empty;
        private string _accountPassword = string.Empty;
        private string _accountPasswordConfirm = string.Empty;
        private string _accountDisplayName = string.Empty;
        private string _accountStatus = string.Empty;
        private bool _isAccountProcessing = false;
        private bool _accountSetupComplete = false;
        private bool _skipAccountSetup = false;

        // Commands
        private ICommand? _selectLocalCommand;
        private ICommand? _selectSqlServerCommand;
        private ICommand? _testConnectionCommand;
        private ICommand? _nextCommand;
        private ICommand? _backCommand;
        private ICommand? _finishCommand;
        private ICommand? _createAccountCommand;
        private ICommand? _signInCommand;
        private ICommand? _skipAccountCommand;
        private ICommand? _toggleAccountModeCommand;
        private ICommand? _browseSqlitePathCommand;

        #endregion

        #region Constructor

        public SetupWizardViewModel(Action? callback) : base(callback)
        {
            // Pre-populate from existing settings if this is a "Change Database" operation
            // (i.e., user already has settings but SetupCompleted was reset)
            var existingSettings = UserSettingsManager.Instance.Settings.Database;
            if (existingSettings != null)
            {
                SelectedDatabaseType = existingSettings.Type;
                
                // Pre-populate custom SQLite path if previously set
                if (!string.IsNullOrEmpty(existingSettings.CustomSqlitePath))
                {
                    _useCustomSqlitePath = true;
                    _customSqlitePath = existingSettings.CustomSqlitePath;
                }
                
                // Pre-populate SQL Server settings
                if (existingSettings.Type == DatabaseType.SqlServer)
                {
                    Server = existingSettings.Server;
                    Database = existingSettings.Database;
                    UseWindowsAuth = existingSettings.UseWindowsAuth;
                    Username = existingSettings.Username;
                    Password = existingSettings.Password;
                    UseOdbc = existingSettings.UseOdbc;
                    OdbcDsn = existingSettings.OdbcDsn;
                    TrustServerCertificate = existingSettings.TrustServerCertificate;
                }
            }
        }

        #endregion

        #region Commands

        public ICommand SelectLocalCommand => _selectLocalCommand ??= new TrackerCommand(ExecuteSelectLocal);
        public ICommand SelectSqlServerCommand => _selectSqlServerCommand ??= new TrackerCommand(ExecuteSelectSqlServer);
        public ICommand TestConnectionCommand => _testConnectionCommand ??= new TrackerCommand(ExecuteTestConnection, CanTestConnection);
        public ICommand NextCommand => _nextCommand ??= new TrackerCommand(ExecuteNext, CanExecuteNext);
        public ICommand BackCommand => _backCommand ??= new TrackerCommand(ExecuteBack, CanExecuteBack);
        public ICommand FinishCommand => _finishCommand ??= new TrackerCommand(ExecuteFinish, CanExecuteFinish);
        public ICommand CreateAccountCommand => _createAccountCommand ??= new TrackerCommand(ExecuteCreateAccount, CanExecuteAccountAction);
        public ICommand SignInCommand => _signInCommand ??= new TrackerCommand(ExecuteSignIn, CanExecuteAccountAction);
        public ICommand SkipAccountCommand => _skipAccountCommand ??= new TrackerCommand(ExecuteSkipAccount);
        public ICommand ToggleAccountModeCommand => _toggleAccountModeCommand ??= new TrackerCommand(ExecuteToggleAccountMode);
        public ICommand BrowseSqlitePathCommand => _browseSqlitePathCommand ??= new TrackerCommand(ExecuteBrowseSqlitePath);

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
                RaisePropertyChanged(nameof(IsStep4));
                RaisePropertyChanged(nameof(CurrentStepNumber));
                RaisePropertyChanged(nameof(ShowNextButton));
            }
        }

        public bool IsStep1 => CurrentStep == 1;
        public bool IsStep2 => CurrentStep == 2;
        public bool IsStep3 => CurrentStep == 3;
        public bool IsStep4 => CurrentStep == 4;

        /// <summary>
        /// Gets the current step number for display.
        /// SQLite: 1-Database → 2-Account → 3-Summary (skips SQL config)
        /// SQL Server: 1-Database → 2-SQL Config → 3-Account → 4-Summary
        /// </summary>
        public int CurrentStepNumber
        {
            get
            {
                if (IsLocalSelected)
                {
                    // Skip step 2 (SQL config) for local
                    return CurrentStep switch
                    {
                        1 => 1,
                        3 => 2, // Account step shows as 2
                        4 => 3, // Summary shows as 3
                        _ => CurrentStep
                    };
                }
                return CurrentStep;
            }
        }

        /// <summary>
        /// Gets the total number of steps (3 for SQLite, 4 for SQL Server).
        /// </summary>
        public int TotalSteps => IsLocalSelected ? 3 : 4;

        /// <summary>
        /// Gets whether to show the Next button.
        /// </summary>
        public bool ShowNextButton => IsStep1 || (IsStep2 && IsSqlServerSelected) || IsStep3;

        public DatabaseType SelectedDatabaseType
        {
            get => _selectedDatabaseType;
            set
            {
                _selectedDatabaseType = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsLocalSelected));
                RaisePropertyChanged(nameof(IsSqlServerSelected));
                RaisePropertyChanged(nameof(TotalSteps));
                RaisePropertyChanged(nameof(CurrentStepNumber));
                RaisePropertyChanged(nameof(ShowNextButton));
            }
        }

        public bool IsLocalSelected => SelectedDatabaseType == DatabaseType.SQLite;
        public bool IsSqlServerSelected => SelectedDatabaseType == DatabaseType.SqlServer;

        public bool UseCustomSqlitePath
        {
            get => _useCustomSqlitePath;
            set
            {
                _useCustomSqlitePath = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ShowCustomSqlitePath));
                RaisePropertyChanged(nameof(SummaryDatabaseLocation));
                // Re-evaluate Next button when custom path option changes
                (_nextCommand as TrackerCommand)?.RaiseCanExecuteChanged();
            }
        }

        public bool ShowCustomSqlitePath => UseCustomSqlitePath;

        public string CustomSqlitePath
        {
            get => _customSqlitePath;
            set
            {
                _customSqlitePath = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(CustomSqlitePathDisplay));
                RaisePropertyChanged(nameof(SummaryDatabaseLocation));
                // Re-evaluate Next button when path changes
                (_nextCommand as TrackerCommand)?.RaiseCanExecuteChanged();
            }
        }

        public string CustomSqlitePathDisplay => 
            string.IsNullOrWhiteSpace(CustomSqlitePath) 
                ? "No custom path set" 
                : CustomSqlitePath;

        /// <summary>
        /// Gets the database location to display in the summary step.
        /// Shows custom path if selected, otherwise the default location.
        /// </summary>
        public string SummaryDatabaseLocation =>
            UseCustomSqlitePath && !string.IsNullOrWhiteSpace(CustomSqlitePath)
                ? CustomSqlitePath
                : "%LocalAppData%\\Tracker\\tracker.db";

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

        /// <summary>
        /// Gets or sets whether to use Windows Authentication for automatic login.
        #region Account Setup Properties

        /// <summary>
        /// Whether user is in "create account" mode (vs sign in).
        /// </summary>
        public bool IsCreatingAccount
        {
            get => _isCreatingAccount;
            set
            {
                _isCreatingAccount = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsSigningIn));
                RaisePropertyChanged(nameof(AccountActionButtonText));
                RaisePropertyChanged(nameof(AccountToggleLinkText));
                AccountStatus = string.Empty;
            }
        }

        public bool IsSigningIn => !_isCreatingAccount;

        public string AccountEmail
        {
            get => _accountEmail;
            set
            {
                _accountEmail = value;
                RaisePropertyChanged();
                AccountStatus = string.Empty;
                RaiseAccountCommandsCanExecuteChanged();
            }
        }

        public string AccountPassword
        {
            get => _accountPassword;
            set
            {
                _accountPassword = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(PasswordValidationMessage));
                RaisePropertyChanged(nameof(ConfirmPasswordValidationMessage));
                AccountStatus = string.Empty;
                RaiseAccountCommandsCanExecuteChanged();
            }
        }

        public string AccountPasswordConfirm
        {
            get => _accountPasswordConfirm;
            set
            {
                _accountPasswordConfirm = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ConfirmPasswordValidationMessage));
                AccountStatus = string.Empty;
                RaiseAccountCommandsCanExecuteChanged();
            }
        }

        /// <summary>
        /// Gets the password validation message (if any).
        /// </summary>
        public string? PasswordValidationMessage
        {
            get
            {
                if (!IsCreatingAccount) return null;
                if (string.IsNullOrEmpty(AccountPassword)) return null;
                
                var errors = new List<string>();
                
                if (AccountPassword.Length < 8)
                    errors.Add($"at least 8 characters (currently {AccountPassword.Length})");
                
                if (!AccountPassword.Any(char.IsUpper))
                    errors.Add("an uppercase letter (A-Z)");
                
                if (!AccountPassword.Any(char.IsLower))
                    errors.Add("a lowercase letter (a-z)");
                
                if (!AccountPassword.Any(char.IsDigit))
                    errors.Add("a number (0-9)");
                
                // Safe special characters (exclude: \ ' " ` < > & | ; and SQL injection chars)
                const string safeSpecialChars = "!@#$%^*()_+-=[]{}:,.?/~";
                if (!AccountPassword.Any(c => safeSpecialChars.Contains(c)))
                    errors.Add($"a special character ({safeSpecialChars})");
                
                // Check for problematic characters
                const string problematicChars = "\\'\"`<>&|;";
                var foundProblematic = AccountPassword.Where(c => problematicChars.Contains(c)).Distinct().ToList();
                if (foundProblematic.Any())
                    errors.Add($"remove unsafe characters: {string.Join(" ", foundProblematic)}");
                
                if (errors.Count == 0) return null;
                
                return "Password needs: " + string.Join(", ", errors);
            }
        }

        /// <summary>
        /// Gets the confirm password validation message (if any).
        /// </summary>
        public string? ConfirmPasswordValidationMessage
        {
            get
            {
                if (!IsCreatingAccount) return null;
                if (string.IsNullOrEmpty(AccountPasswordConfirm)) return null;
                if (AccountPassword != AccountPasswordConfirm)
                    return "Passwords do not match";
                return null;
            }
        }

        public string AccountDisplayName
        {
            get => _accountDisplayName;
            set
            {
                _accountDisplayName = value;
                RaisePropertyChanged();
            }
        }

        public string AccountStatus
        {
            get => _accountStatus;
            set
            {
                _accountStatus = value;
                RaisePropertyChanged();
            }
        }

        public bool IsAccountProcessing
        {
            get => _isAccountProcessing;
            set
            {
                _isAccountProcessing = value;
                RaisePropertyChanged();
                RaiseAccountCommandsCanExecuteChanged();
            }
        }

        public bool AccountSetupComplete
        {
            get => _accountSetupComplete;
            set
            {
                _accountSetupComplete = value;
                RaisePropertyChanged();
            }
        }

        public bool SkipAccountSetup
        {
            get => _skipAccountSetup;
            set
            {
                _skipAccountSetup = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets the text for the main account action button.
        /// </summary>
        public string AccountActionButtonText => IsCreatingAccount ? "Create Account" : "Sign In";

        /// <summary>
        /// Gets the text for toggling between create/sign in.
        /// </summary>
        public string AccountToggleLinkText => IsCreatingAccount
            ? "Already have an account? Sign in"
            : "Don't have an account? Create one";

        /// <summary>
        /// Gets the signed-in user's email for display.
        /// </summary>
        public string? SignedInUserEmail => SupabaseService.Instance.CurrentUser?.Email;

        /// <summary>
        /// Gets the signed-in user's display name.
        /// </summary>
        public string? SignedInUserName => SupabaseService.Instance.CurrentProfile?.DisplayName
            ?? SupabaseService.Instance.CurrentUser?.Email?.Split('@')[0];

        /// <summary>
        /// Gets the account name for the summary page.
        /// Uses locally entered values as fallback if Supabase hasn't populated yet.
        /// </summary>
        public string AccountSummaryName => 
            SignedInUserName 
            ?? (string.IsNullOrWhiteSpace(AccountDisplayName) ? AccountEmail?.Split('@')[0] : AccountDisplayName) 
            ?? "User";

        /// <summary>
        /// Gets the account email for the summary page.
        /// Uses locally entered value as fallback.
        /// </summary>
        public string AccountSummaryEmail => SignedInUserEmail ?? AccountEmail ?? "Not set";

        #endregion

        #endregion

        #region Command Implementations

        private void ExecuteSelectLocal(object? parameter)
        {
            SelectedDatabaseType = DatabaseType.SQLite;
            CurrentStep = 3; // Skip SQL config, go to account setup
        }

        private void ExecuteSelectSqlServer(object? parameter)
        {
            SelectedDatabaseType = DatabaseType.SqlServer;
            CurrentStep = 2; // Go to SQL configuration
        }

        #region Account Commands

        private bool CanExecuteAccountAction(object? parameter)
        {
            if (IsAccountProcessing) return false;

            if (string.IsNullOrWhiteSpace(AccountEmail)) return false;
            if (string.IsNullOrWhiteSpace(AccountPassword)) return false;

            if (IsCreatingAccount)
            {
                if (AccountPassword != AccountPasswordConfirm) return false;
                if (!IsPasswordValid(AccountPassword)) return false;
            }

            return true;
        }

        /// <summary>
        /// Validates password meets all security requirements.
        /// </summary>
        private static bool IsPasswordValid(string password)
        {
            if (string.IsNullOrEmpty(password)) return false;
            if (password.Length < 8) return false;
            if (!password.Any(char.IsUpper)) return false;
            if (!password.Any(char.IsLower)) return false;
            if (!password.Any(char.IsDigit)) return false;
            
            const string safeSpecialChars = "!@#$%^*()_+-=[]{}:,.?/~";
            if (!password.Any(c => safeSpecialChars.Contains(c))) return false;
            
            // Reject passwords with problematic characters
            const string problematicChars = "\\'\"`<>&|;";
            if (password.Any(c => problematicChars.Contains(c))) return false;
            
            return true;
        }

        private async void ExecuteCreateAccount(object? parameter)
        {
            if (!CanExecuteAccountAction(null)) return;

            IsAccountProcessing = true;
            AccountStatus = "Creating your account...";

            try
            {
                // Initialize Supabase if not already
                if (!SupabaseService.Instance.IsInitialized)
                {
                    await SupabaseService.Instance.InitializeAsync();
                }

                var displayName = !string.IsNullOrWhiteSpace(AccountDisplayName)
                    ? AccountDisplayName
                    : AccountEmail.Split('@')[0];

                var (success, error) = await SupabaseService.Instance.SignUpAsync(
                    AccountEmail, AccountPassword, displayName);

                if (success)
                {
                    AccountSetupComplete = true;
                    AccountStatus = "✓ Account created successfully! Check your email to confirm.";
                    RaisePropertyChanged(nameof(SignedInUserEmail));
                    RaisePropertyChanged(nameof(SignedInUserName));
                    RaisePropertyChanged(nameof(AccountSummaryName));
                    RaisePropertyChanged(nameof(AccountSummaryEmail));
                }
                else
                {
                    AccountStatus = $"✗ {error}";
                }
            }
            catch (Exception ex)
            {
                AccountStatus = $"✗ Error: {ex.Message}";
            }
            finally
            {
                IsAccountProcessing = false;
            }
        }

        private async void ExecuteSignIn(object? parameter)
        {
            if (!CanExecuteAccountAction(null)) return;

            IsAccountProcessing = true;
            AccountStatus = "Signing in...";

            try
            {
                // Initialize Supabase if not already
                if (!SupabaseService.Instance.IsInitialized)
                {
                    await SupabaseService.Instance.InitializeAsync();
                }

                var (success, error) = await SupabaseService.Instance.SignInAsync(
                    AccountEmail, AccountPassword);

                if (success)
                {
                    AccountSetupComplete = true;
                    AccountStatus = $"✓ Welcome back, {AccountSummaryName}!";
                    RaisePropertyChanged(nameof(SignedInUserEmail));
                    RaisePropertyChanged(nameof(SignedInUserName));
                    RaisePropertyChanged(nameof(AccountSummaryName));
                    RaisePropertyChanged(nameof(AccountSummaryEmail));
                }
                else
                {
                    AccountStatus = $"✗ {error}";
                }
            }
            catch (Exception ex)
            {
                AccountStatus = $"✗ Error: {ex.Message}";
            }
            finally
            {
                IsAccountProcessing = false;
            }
        }

        private void ExecuteSkipAccount(object? parameter)
        {
            SkipAccountSetup = true;
            AccountSetupComplete = true;
            AccountStatus = "Account setup skipped. You can create an account later in Settings.";
        }

        private void ExecuteToggleAccountMode(object? parameter)
        {
            IsCreatingAccount = !IsCreatingAccount;
            RaiseAccountCommandsCanExecuteChanged();
        }

        private void ExecuteBrowseSqlitePath(object? parameter)
        {            
            // Use folder browser - we're picking WHERE to store the database, not an existing file
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select folder for Tracker database",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            // Set initial directory if we have an existing path
            if (!string.IsNullOrWhiteSpace(CustomSqlitePath))
            {
                var existingDir = Path.GetDirectoryName(CustomSqlitePath);
                if (!string.IsNullOrEmpty(existingDir) && Directory.Exists(existingDir))
                {
                    dialog.InitialDirectory = existingDir;
                }
            }

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var selectedFolder = dialog.SelectedPath;
                var dbFilePath = Path.Combine(selectedFolder, "tracker.db");
                
                // Check if a database already exists in this folder
                if (File.Exists(dbFilePath))
                {
                    var result = MessageBoxHelper.Show(
                        $"A database already exists at:\n{dbFilePath}\n\nDo you want to use this existing database?\n\n" +
                        "• Yes - Connect to the existing database\n" +
                        "• No - Create a new database (existing will be backed up)",
                        "Existing Database Found",
                        System.Windows.MessageBoxButton.YesNoCancel,
                        System.Windows.MessageBoxImage.Question);
                    
                    if (result == System.Windows.MessageBoxResult.Cancel)
                    {
                        return; // User cancelled, don't change anything
                    }
                    
                    if (result == System.Windows.MessageBoxResult.No)
                    {
                        // User wants a fresh database - we'll back up the existing one during initialization
                        // The backup will be handled by TrackerDbManager when CreateDatabase is true
                        CreateDatabase = true;
                    }
                    else
                    {
                        // User wants to use existing database
                        CreateDatabase = false;
                    }
                }
                
                CustomSqlitePath = dbFilePath;
            }
        }

        /// <summary>
        /// Notifies account-related commands to re-evaluate their CanExecute state.
        /// </summary>
        private void RaiseAccountCommandsCanExecuteChanged()
        {
            (_createAccountCommand as TrackerCommand)?.RaiseCanExecuteChanged();
            (_signInCommand as TrackerCommand)?.RaiseCanExecuteChanged();
        }

        #endregion

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
                
                // With Dapper/Supabase migration, connection testing uses TrackerDataManager
                // The connection is validated when the user authenticates with Supabase
                await Task.Delay(500); // Brief delay to show testing UI
                TrackerDataManager.Instance.Initialize();
                
                ConnectionTestSucceeded = true;
                ConnectionStatus = "✓ Connected successfully!";
                CreateDatabase = false;
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
            // Step 1: Database type selection
            if (CurrentStep == 1)
            {
                // If custom SQLite path is checked, must have a valid path
                if (IsLocalSelected && UseCustomSqlitePath)
                {
                    return !string.IsNullOrWhiteSpace(CustomSqlitePath);
                }
                return true;
            }
            
            // Step 2: SQL Server config - must test connection
            if (CurrentStep == 2 && IsSqlServerSelected)
                return ConnectionTestSucceeded;
            
            // Step 3: Account setup
            if (CurrentStep == 3)
                return AccountSetupComplete || SkipAccountSetup;

            return CurrentStep < 4;
        }

        private void ExecuteNext(object? parameter)
        {
            if (CurrentStep == 2 && IsSqlServerSelected && !ConnectionTestSucceeded)
            {
                ConnectionStatus = "Please test the connection before proceeding.";
                return;
            }

            if (CurrentStep == 3 && !AccountSetupComplete && !SkipAccountSetup)
            {
                AccountStatus = "Please create an account, sign in, or skip to continue.";
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
                CurrentStep = 1; // Go back to database selection (skip SQL config)
            }
            else if (CurrentStep == 4 && IsLocalSelected)
            {
                CurrentStep = 3; // Go back to account setup
            }
            else
            {
                CurrentStep--;
            }
        }

        private bool CanExecuteFinish(object? parameter)
        {
            if (IsSqlServerSelected && !ConnectionTestSucceeded)
                return false;
            
            return AccountSetupComplete || SkipAccountSetup;
        }

        private async void ExecuteFinish(object? parameter)
        {
            var logger = Logging.LoggingManager.GetComponentLogger("SetupWizard");
            
            try
            {
                var settings = BuildDatabaseSettings();
                settings.SetupCompleted = true;

                logger.Info("ExecuteFinish - UseCustomSqlitePath: {0}, CustomSqlitePath: '{1}'", 
                    UseCustomSqlitePath, CustomSqlitePath);
                logger.Info("ExecuteFinish - BuildDatabaseSettings returned CustomSqlitePath: '{0}'", 
                    settings.CustomSqlitePath);

                // Check if we need to migrate existing database to new location
                await MigrateExistingDatabaseIfNeeded(settings);

                // Save database settings
                UserSettingsManager.Instance.Settings.Database = settings;
                
                logger.Info("ExecuteFinish - Saving to user settings. Current Supabase user ID: '{0}'", 
                    SupabaseService.Instance.CurrentUser?.Id ?? "(none)");
                
                // Save authentication settings
                var authSettings = UserSettingsManager.Instance.Settings.Authentication;
                
                // Save user info from Supabase
                if (SupabaseService.Instance.CurrentUser != null)
                {
                    authSettings.UserId = Guid.Parse(SupabaseService.Instance.CurrentUser.Id);
                    authSettings.UserEmail = SupabaseService.Instance.CurrentUser.Email;
                }
                
                UserSettingsManager.Instance.SaveSettings();
                logger.Info("ExecuteFinish - Settings saved successfully");

                // Initialize data manager (Dapper connection factory)
                TrackerDataManager.Instance.Initialize();

                // Create the local user account using Supabase UUID
                var supabaseUser = SupabaseService.Instance.CurrentUser;
                if (supabaseUser == null || string.IsNullOrEmpty(supabaseUser.Id))
                {
                    logger.Error("No Supabase user available during setup");
                    return;
                }
                var supabaseUserId = Guid.Parse(supabaseUser.Id);
                var displayName = SupabaseService.Instance.CurrentProfile?.DisplayName
                    ?? AccountDisplayName
                    ?? supabaseUser.Email?.Split('@')[0]
                    ?? Environment.UserName;
                
                // Get or create user via UserRepository (Dapper)
                var connectionFactory = new Services.Data.DapperConnectionFactory();
                var userRepository = new Services.Data.Repositories.UserRepository(connectionFactory, null!);
                var user = await userRepository.GetBySupabaseIdAsync(supabaseUserId);
                if (user == null)
                {
                    user = await userRepository.CreateAsync(new DataModels.User
                    {
                        Id = supabaseUserId,
                        Email = supabaseUser.Email ?? "",
                        Username = displayName,
                        DisplayName = displayName,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                
                if (user != null)
                {
                    // Update auth settings - user.Id is already a Guid
                    authSettings.UserId = user.Id;
                    authSettings.AccountSetupCompleted = true;
                    UserSettingsManager.Instance.CurrentUser = displayName ?? user.Username;
                    UserSettingsManager.Instance.SaveSettings();
                }

                // Update subscription service with cloud subscription
                if (SupabaseService.Instance.CurrentSubscription != null)
                {
                    Services.Subscription.SubscriptionService.Instance.SetTier(
                        SupabaseService.Instance.CurrentSubscription.Tier);
                }

                // Notify all views to refresh their data
                DataMessenger.SendRefreshAll();

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
                CustomSqlitePath = UseCustomSqlitePath ? CustomSqlitePath : string.Empty,
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

        /// <summary>
        /// Migrates existing database to new location if needed.
        /// </summary>
        private async Task MigrateExistingDatabaseIfNeeded(DatabaseSettings newSettings)
        {
            // Only handle SQLite migrations (SQL Server doesn't need file copying)
            if (newSettings.Type != DatabaseType.SQLite)
                return;

            var oldSettings = UserSettingsManager.Instance.Settings.Database;
            
            // Skip if this is first-time setup (no old settings)
            if (oldSettings == null || !oldSettings.SetupCompleted)
                return;

            // Get old and new database paths
            string oldPath = string.IsNullOrWhiteSpace(oldSettings.CustomSqlitePath)
                ? DatabaseSettings.GetSqlitePath()
                : oldSettings.CustomSqlitePath;

            string newPath = string.IsNullOrWhiteSpace(newSettings.CustomSqlitePath)
                ? DatabaseSettings.GetSqlitePath()
                : newSettings.CustomSqlitePath;

            // If paths are the same, no migration needed
            if (string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
                return;

            // Check if old database exists
            if (!File.Exists(oldPath))
                return;

            // Ask user if they want to copy existing database
            var result = await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var messageResult = MessageBoxHelper.Show(
                    $"An existing database was found at:\n{oldPath}\n\n" +
                    $"Would you like to copy it to the new location?\n{newPath}\n\n" +
                    "YES - Copy existing database (recommended)\n" +
                    "NO - Start with empty database at new location",
                    "Migrate Existing Database?",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);
                return messageResult;
            });

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    // Ensure target directory exists
                    var targetDir = Path.GetDirectoryName(newPath);
                    if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    // Copy database file
                    File.Copy(oldPath, newPath, overwrite: true);

                    // Also copy the vector store if it exists
                    var oldVectorPath = Path.Combine(
                        Path.GetDirectoryName(oldPath) ?? string.Empty,
                        "vector_store.db");
                    var newVectorPath = Path.Combine(
                        Path.GetDirectoryName(newPath) ?? string.Empty,
                        "vector_store.db");

                    if (File.Exists(oldVectorPath))
                    {
                        File.Copy(oldVectorPath, newVectorPath, overwrite: true);
                    }

                    ConnectionStatus = $"✓ Database migrated successfully to {newPath}";
                }
                catch (Exception ex)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBoxHelper.Show(
                            $"Failed to copy database:\n{ex.Message}\n\n" +
                            $"You may need to manually copy:\n{oldPath}\nto\n{newPath}",
                            "Migration Failed",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning);
                    });
                    throw;
                }
            }
            else
            {
                // User chose to start fresh - set CreateDatabase to true
                CreateDatabase = true;
                IncludeSampleData = false; // Don't auto-add sample data when migrating locations
            }
        }

        #endregion
    }
}

