using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Tracker.Classes;
using Tracker.Command;
using Tracker.Database;
using Tracker.Helpers;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services.Auth;
using Tracker.Services.Backend;
using Tracker.Services.Subscription;

namespace Tracker.ViewModels.DialogViewModels
{
    /// <summary>
    /// ViewModel for the login dialog.
    /// Handles user authentication via PostgreSQL with Row-Level Security.
    /// Supports both Sign In and Create Account modes.
    /// </summary>
    public class LoginDialogViewModel : BaseDialogViewModel
    {
        #region Fields

        private readonly ILogger _logger;

        // Mode
        private bool _isCreateAccountMode;

        // Sign In fields
        private string _email = string.Empty;
        private string _password = string.Empty;
        private bool _rememberMe;
        private bool _isAdminLogin;

        // Create Account fields
        private string _displayName = string.Empty;
        private string _confirmPassword = string.Empty;

        // Status
        private string _statusMessage = string.Empty;
        private bool _isProcessing;
        private bool _hasError;
        
        // Password visibility
        private bool _showPassword;

        // Commands
        private ICommand? _signInCommand;
        private ICommand? _createAccountCommand;
        private ICommand? _toggleModeCommand;
        private ICommand? _cancelCommand;
        private ICommand? _forgotPasswordCommand;
        private ICommand? _openHelpCommand;
        private ICommand? _contactSupportCommand;
        private ICommand? _togglePasswordVisibilityCommand;

        #endregion

        #region Constructor

        public LoginDialogViewModel(Action? callback) : base(callback)
        {
            _logger = LoggingManager.GetComponentLogger("Login");

            // Load saved credentials if "Remember Me" was checked
            var authSettings = UserSettingsManager.Instance.Settings.Authentication;
            if (authSettings.RememberMe && !string.IsNullOrEmpty(authSettings.SavedEmail))
            {
                _email = authSettings.SavedEmail;
                _rememberMe = true;
                // Password will be loaded from secure storage if available
                _password = SecureTokenStorage.GetSavedPassword() ?? string.Empty;
            }

            // Pre-fill display name for new accounts
            _displayName = Environment.UserName;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the dialog result.
        /// </summary>
        public DialogResult Result { get; set; } = new DialogResult();

        /// <summary>
        /// Whether in Create Account mode (vs Sign In mode).
        /// </summary>
        public bool IsCreateAccountMode
        {
            get => _isCreateAccountMode;
            set
            {
                _isCreateAccountMode = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsSignInMode));
                RaisePropertyChanged(nameof(ModeTitle));
                RaisePropertyChanged(nameof(ActionButtonText));
                RaisePropertyChanged(nameof(ToggleModeText));
                ClearStatus();
            }
        }

        public bool IsSignInMode => !_isCreateAccountMode;

        public string ModeTitle => IsCreateAccountMode ? "Create Account" : "Sign In";
        public string ActionButtonText => IsCreateAccountMode ? "Create Account" : "Sign In";
        public string ToggleModeText => IsCreateAccountMode
            ? "Already have an account? Sign in"
            : "Don't have an account? Create one";

        /// <summary>
        /// User's email address.
        /// </summary>
        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                RaisePropertyChanged();
                ClearStatus();
            }
        }

        /// <summary>
        /// User's password.
        /// </summary>
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                RaisePropertyChanged();
                ClearStatus();
            }
        }

        /// <summary>
        /// Display name (Create Account mode).
        /// </summary>
        public string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                RaisePropertyChanged();
                ClearStatus();
            }
        }

        /// <summary>
        /// Password confirmation (Create Account mode).
        /// </summary>
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                _confirmPassword = value;
                RaisePropertyChanged();
                ClearStatus();
            }
        }

        /// <summary>
        /// Whether to remember login credentials.
        /// </summary>
        public bool RememberMe
        {
            get => _rememberMe;
            set
            {
                _rememberMe = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Whether to log in as admin (launches admin window instead of main app).
        /// Only functional if the user account has IsAdmin = true.
        /// </summary>
        public bool IsAdminLogin
        {
            get => _isAdminLogin;
            set
            {
                _isAdminLogin = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Whether the admin checkbox can be selected.
        /// Checks if the current user has admin privileges.
        /// </summary>
        public bool CanSelectAdmin
        {
            get
            {
                // For now, admin is determined by local check
                // TODO: Add admin flag to PostgreSQL users table
                return false;
            }
        }

        /// <summary>
        /// Whether to show password in plain text.
        /// </summary>
        public bool ShowPassword
        {
            get => _showPassword;
            set
            {
                _showPassword = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Status message to display.
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasStatus));
            }
        }

        public bool HasStatus => !string.IsNullOrEmpty(_statusMessage);

        /// <summary>
        /// Whether there's an error (vs success message).
        /// </summary>
        public bool HasError
        {
            get => _hasError;
            set
            {
                _hasError = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Whether an operation is in progress.
        /// </summary>
        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                _isProcessing = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsNotProcessing));
            }
        }

        public bool IsNotProcessing => !_isProcessing;

        #endregion

        #region Commands

        public ICommand SignInCommand =>
            _signInCommand ??= new TrackerCommand(ExecuteSignIn, CanExecuteSignIn);

        public ICommand CreateAccountCommand =>
            _createAccountCommand ??= new TrackerCommand(ExecuteCreateAccount, CanExecuteCreateAccount);

        public ICommand ToggleModeCommand =>
            _toggleModeCommand ??= new TrackerCommand(_ => IsCreateAccountMode = !IsCreateAccountMode);

        public ICommand CancelCommand =>
            _cancelCommand ??= new TrackerCommand(ExecuteCancel);

        public ICommand ForgotPasswordCommand =>
            _forgotPasswordCommand ??= new TrackerCommand(ExecuteForgotPassword, _ => !IsProcessing);

        public ICommand OpenHelpCommand =>
            _openHelpCommand ??= new TrackerCommand(ExecuteOpenHelp);

        public ICommand ContactSupportCommand =>
            _contactSupportCommand ??= new TrackerCommand(ExecuteContactSupport);

        public ICommand TogglePasswordVisibilityCommand =>
            _togglePasswordVisibilityCommand ??= new TrackerCommand(_ => ShowPassword = !ShowPassword);

        #endregion

        #region Command Implementations

        /// <summary>
        /// Simple email format validation regex.
        /// </summary>
        private static readonly Regex EmailRegex = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$", 
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static bool IsValidEmail(string email) => 
            !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email);

        private bool CanExecuteSignIn(object? parameter)
        {
            if (IsProcessing) return false;
            if (!IsValidEmail(Email)) return false;
            if (string.IsNullOrWhiteSpace(Password)) return false;
            return true;
        }

        private async void ExecuteSignIn(object? parameter)
        {
            IsProcessing = true;
            ClearStatus();

            try
            {
                // Validate email format
                if (!IsValidEmail(Email))
                {
                    SetStatus("Please enter a valid email address", true);
                    return;
                }

                _logger.Info("Attempting sign in for: {0}", Email);

                // Ensure PostgreSQL auth is initialized
                EnsureAuthenticationInitialized();

                SetStatus("Signing in...", false);

                var result = await AuthenticationManager.Instance.SignInAsync(Email, Password);

                if (result.Success && result.User != null)
                {
                    _logger.Info("Sign in successful");

                    // Switch to user-specific settings
                    var userId = result.User.Id.ToString();
                    UserSettingsManager.Instance.SwitchToUser(userId, isNewAccount: false);

                    // Create local user record FIRST - this sets CurrentUserId
                    await CreateLocalUserAsync(result.User);

                    // Set PostgreSQL RLS context AFTER CurrentUserId is set
                    await TrackerDbManager.Instance!.SetPostgresUserAsync(result.User.Id);

                    // Save auth settings
                    SaveAuthenticationSettings(isNewAccount: false, result.User, result.AccessToken);

                    UserSettingsManager.Instance.SaveSettings();

                    SetStatus("Welcome back!", false);
                    await Task.Delay(500);

                    Result.Cancelled = false;
                    Result.IsAdminLogin = IsAdminLogin;
                    Callback?.Invoke();
                }
                else
                {
                    SetStatus(result.ErrorMessage ?? "Sign in failed", true);
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Sign in error");
                SetStatus($"Error: {ex.Message}", true);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private bool CanExecuteCreateAccount(object? parameter)
        {
            if (IsProcessing) return false;
            if (!IsValidEmail(Email)) return false;
            if (string.IsNullOrWhiteSpace(Password)) return false;
            if (Password.Length < 6) return false;
            if (Password != ConfirmPassword) return false;
            return true;
        }

        private async void ExecuteCreateAccount(object? parameter)
        {
            IsProcessing = true;
            ClearStatus();

            try
            {
                // Validate email format
                if (!IsValidEmail(Email))
                {
                    SetStatus("Please enter a valid email address", true);
                    return;
                }

                // Validate passwords
                if (Password != ConfirmPassword)
                {
                    SetStatus("Passwords do not match", true);
                    return;
                }

                if (Password.Length < 8)
                {
                    SetStatus("Password must be at least 8 characters", true);
                    return;
                }

                _logger.Info("Creating account for: {0}", Email);

                // Ensure PostgreSQL auth is initialized
                EnsureAuthenticationInitialized();

                SetStatus("Creating your account...", false);

                var displayName = !string.IsNullOrWhiteSpace(DisplayName)
                    ? DisplayName
                    : Email.Split('@')[0];

                var result = await AuthenticationManager.Instance.SignUpAsync(Email, Password, displayName);

                if (result.Success && result.User != null)
                {
                    _logger.Info("Account created successfully");

                    // Switch to user-specific settings (new account = fresh defaults)
                    var userId = result.User.Id.ToString();
                    UserSettingsManager.Instance.SwitchToUser(userId, isNewAccount: true);

                    // Create local user record FIRST - this sets CurrentUserId
                    await CreateLocalUserAsync(result.User);

                    // Set PostgreSQL RLS context AFTER CurrentUserId is set
                    await TrackerDbManager.Instance!.SetPostgresUserAsync(result.User.Id);

                    // Save auth settings
                    SaveAuthenticationSettings(isNewAccount: true, result.User, result.AccessToken);

                    UserSettingsManager.Instance.SaveSettings();

                    SetStatus("Account created successfully!", false);
                    await Task.Delay(1000);

                    Result.Cancelled = false;
                    Result.IsAdminLogin = IsAdminLogin;
                    Callback?.Invoke();
                }
                else
                {
                    SetStatus(result.ErrorMessage ?? "Account creation failed", true);
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Account creation error");
                SetStatus($"Error: {ex.Message}", true);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void ExecuteForgotPassword(object? parameter)
        {
            // Password reset not yet implemented for PostgreSQL
            // TODO: Implement email-based password reset
            SetStatus("Password reset: Contact support@pricklycactus.com", false);
        }

        private void ExecuteCancel(object? parameter)
        {
            Result.Cancelled = true;
            Callback?.Invoke();
        }

        private void ExecuteOpenHelp(object? parameter)
        {
            try
            {
                // Open help center in default browser
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://help.pricklycactus.com/account",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.Warn("Failed to open help: {0}", ex.Message);
                SetStatus("Could not open help center. Visit help.pricklycactus.com", true);
            }
        }

        private void ExecuteContactSupport(object? parameter)
        {
            try
            {
                // Open email client with support address
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "mailto:support@pricklycactus.com?subject=Tracker%20Support%20Request",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.Warn("Failed to open email: {0}", ex.Message);
                SetStatus("Email: support@pricklycactus.com", false);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Ensures PostgreSQL authentication is initialized with current database settings.
        /// </summary>
        private void EnsureAuthenticationInitialized()
        {
            if (!AuthenticationManager.Instance.IsPostgresConfigured)
            {
                // Get PostgreSQL settings from user settings
                var dbSettings = UserSettingsManager.Instance.Settings.Database;
                
                // If not configured for PostgreSQL, set up defaults for local development
                if (dbSettings.Type != DatabaseType.PostgreSQL)
                {
                    dbSettings.Type = DatabaseType.PostgreSQL;
                    dbSettings.PostgresHost = "localhost";
                    dbSettings.PostgresPort = 5432;
                    dbSettings.PostgresDatabase = "tracker";
                    dbSettings.PostgresUsername = "tracker_app";
                    dbSettings.PostgresPassword = "tracker123";
                }

                // JWT secret - in production this should come from secure config
                var jwtSecret = "TrackerProductionSecret_AtLeast32Characters!";
                
                AuthenticationManager.Instance.Initialize(dbSettings, jwtSecret);
            }
        }

        /// <summary>
        /// Saves authentication settings after successful sign-in or account creation.
        /// </summary>
        /// <param name="isNewAccount">True if this is a new account (don't clear credentials on non-remember)</param>
        /// <param name="user">The authenticated user</param>
        /// <param name="accessToken">The JWT access token (optional, for session restore)</param>
        private void SaveAuthenticationSettings(bool isNewAccount, AuthenticatedUser user, string? accessToken)
        {
            // Save to user-specific settings (after SwitchToUser has been called)
            var authSettings = UserSettingsManager.Instance.Settings.Authentication;
            authSettings.CloudAccountLinked = true;
            authSettings.CloudUserId = user.Id.ToString();
            authSettings.CloudUserEmail = Email;
            authSettings.RememberMe = RememberMe;

            if (RememberMe)
            {
                authSettings.SavedEmail = Email;
                SecureTokenStorage.SavePassword(Password);
                // Also save access token for session restore
                if (!string.IsNullOrEmpty(accessToken))
                {
                    SecureTokenStorage.SaveAccessToken(accessToken);
                }
            }
            else if (!isNewAccount)
            {
                // Only clear on sign-in, not on new account creation
                authSettings.SavedEmail = null;
                SecureTokenStorage.ClearPassword();
                SecureTokenStorage.ClearAccessToken();
            }

            // CRITICAL: Also save RememberMe to anonymous settings so it's available
            // before login on next app startup
            UserSettingsManager.Instance.SaveRememberMeToAnonymousSettings(RememberMe, RememberMe ? Email : null);
        }

        private async Task CreateLocalUserAsync(AuthenticatedUser user)
        {
            try
            {
                var displayName = user.DisplayName ?? DisplayName ?? Email.Split('@')[0];

                UserSettingsManager.Instance.CurrentUser = displayName;

                if (TrackerDbManager.Instance != null)
                {
                    // Look up by SupabaseUserId (UUID), not by display name string
                    var localUser = await TrackerDbManager.Instance.GetOrCreateUserAsync(user.Id, user.Email, displayName);
                    if (localUser != null)
                    {
                        var authSettings = UserSettingsManager.Instance.Settings.Authentication;
                        authSettings.StoredUserId = localUser.Id;
                        authSettings.AccountSetupCompleted = true;
                        
                        _logger.Info("Local user created/retrieved: {0} (Id: {1})", displayName, localUser.Id);
                        
                        // Refresh CanSelectAdmin binding to enable/disable checkbox
                        RaisePropertyChanged(nameof(CanSelectAdmin));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warn("Failed to create local user: {0}", ex.Message);
            }
        }

        private void SetStatus(string message, bool isError)
        {
            StatusMessage = message;
            HasError = isError;
        }

        private void ClearStatus()
        {
            StatusMessage = string.Empty;
            HasError = false;
        }

        #endregion
    }
}
