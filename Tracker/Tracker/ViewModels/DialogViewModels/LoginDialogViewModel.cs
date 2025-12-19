using System.Windows.Input;
using Tracker.Classes;
using Tracker.Command;
using Tracker.Database;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services.Backend;
using Tracker.Services.Subscription;

namespace Tracker.ViewModels.DialogViewModels
{
    /// <summary>
    /// ViewModel for the login dialog.
    /// Handles user authentication via Supabase cloud backend.
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

        private bool CanExecuteSignIn(object? parameter)
        {
            if (IsProcessing) return false;
            if (string.IsNullOrWhiteSpace(Email)) return false;
            if (string.IsNullOrWhiteSpace(Password)) return false;
            return true;
        }

        private async void ExecuteSignIn(object? parameter)
        {
            IsProcessing = true;
            ClearStatus();

            try
            {
                _logger.Info("Attempting sign in for: {0}", Email);

                // Initialize Supabase if needed
                if (!SupabaseService.Instance.IsInitialized)
                {
                    SetStatus("Connecting to server...", false);
                    await SupabaseService.Instance.InitializeAsync();
                }

                SetStatus("Signing in...", false);

                var (success, error) = await SupabaseService.Instance.SignInAsync(Email, Password);

                if (success)
                {
                    _logger.Info("Sign in successful");

                    // Update auth settings
                    var authSettings = UserSettingsManager.Instance.Settings.Authentication;
                    authSettings.CloudAccountLinked = true;
                    authSettings.CloudUserId = SupabaseService.Instance.CurrentUser?.Id;
                    authSettings.CloudUserEmail = Email;
                    authSettings.RememberMe = RememberMe;

                    // Save or clear credentials based on Remember Me
                    if (RememberMe)
                    {
                        authSettings.SavedEmail = Email;
                        SecureTokenStorage.SavePassword(Password);
                    }
                    else
                    {
                        authSettings.SavedEmail = null;
                        SecureTokenStorage.ClearPassword();
                    }

                    // Update subscription from cloud
                    if (SupabaseService.Instance.CurrentSubscription != null)
                    {
                        SubscriptionService.Instance.SetTier(
                            SupabaseService.Instance.CurrentSubscription.Tier);
                    }

                    // Create local user record
                    await CreateLocalUserAsync();

                    UserSettingsManager.Instance.SaveSettings();

                    SetStatus("Welcome back!", false);
                    await Task.Delay(500);

                    Result.Cancelled = false;
                    Callback?.Invoke();
                }
                else
                {
                    SetStatus(error ?? "Sign in failed", true);
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
            if (string.IsNullOrWhiteSpace(Email)) return false;
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
                // Validate
                if (Password != ConfirmPassword)
                {
                    SetStatus("Passwords do not match", true);
                    return;
                }

                if (Password.Length < 6)
                {
                    SetStatus("Password must be at least 6 characters", true);
                    return;
                }

                _logger.Info("Creating account for: {0}", Email);

                // Initialize Supabase if needed
                if (!SupabaseService.Instance.IsInitialized)
                {
                    SetStatus("Connecting to server...", false);
                    await SupabaseService.Instance.InitializeAsync();
                }

                SetStatus("Creating your account...", false);

                var displayName = !string.IsNullOrWhiteSpace(DisplayName)
                    ? DisplayName
                    : Email.Split('@')[0];

                var (success, error) = await SupabaseService.Instance.SignUpAsync(
                    Email, Password, displayName);

                if (success)
                {
                    _logger.Info("Account created successfully");

                    // Update auth settings
                    var authSettings = UserSettingsManager.Instance.Settings.Authentication;
                    authSettings.CloudAccountLinked = true;
                    authSettings.CloudUserId = SupabaseService.Instance.CurrentUser?.Id;
                    authSettings.CloudUserEmail = Email;
                    authSettings.RememberMe = RememberMe;

                    // Save credentials if Remember Me is checked
                    if (RememberMe)
                    {
                        authSettings.SavedEmail = Email;
                        SecureTokenStorage.SavePassword(Password);
                    }

                    // Create local user record
                    await CreateLocalUserAsync();

                    UserSettingsManager.Instance.SaveSettings();

                    SetStatus("Account created! Check your email to confirm.", false);
                    await Task.Delay(1500);

                    Result.Cancelled = false;
                    Callback?.Invoke();
                }
                else
                {
                    SetStatus(error ?? "Account creation failed", true);
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

        private async void ExecuteForgotPassword(object? parameter)
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                SetStatus("Enter your email address first", true);
                return;
            }

            IsProcessing = true;

            try
            {
                if (!SupabaseService.Instance.IsInitialized)
                {
                    await SupabaseService.Instance.InitializeAsync();
                }

                var (success, error) = await SupabaseService.Instance.ResetPasswordAsync(Email);

                if (success)
                {
                    SetStatus("Password reset email sent! Check your inbox.", false);
                }
                else
                {
                    SetStatus(error ?? "Failed to send reset email", true);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}", true);
            }
            finally
            {
                IsProcessing = false;
            }
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

        private async Task CreateLocalUserAsync()
        {
            try
            {
                var displayName = SupabaseService.Instance.CurrentProfile?.DisplayName
                    ?? DisplayName
                    ?? Email.Split('@')[0];

                UserSettingsManager.Instance.CurrentUser = displayName;

                if (TrackerDbManager.Instance != null)
                {
                    var user = await TrackerDbManager.Instance.GetOrCreateUserAsync(displayName);
                    if (user != null)
                    {
                        var authSettings = UserSettingsManager.Instance.Settings.Authentication;
                        authSettings.StoredUserId = user.Id;
                        authSettings.AccountSetupCompleted = true;
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
