using System.Windows.Input;
using Tracker.Classes;
using Tracker.Command;
using Tracker.Database;
using Tracker.Managers;

namespace Tracker.ViewModels.DialogViewModels
{
    /// <summary>
    /// ViewModel for the login dialog.
    /// Handles user authentication and database connection verification.
    /// </summary>
    public class LoginDialogViewModel : BaseDialogViewModel
    {
        #region Fields

        private ICommand? _loginCommand;
        private ICommand? _cancelCommand;

        private string _userName = string.Empty;
        private string _password = string.Empty;
        private string _errorMessage = string.Empty;

        private bool _useWindows = true;
        private bool _isLoggingIn;

        #endregion

        #region Ctor

        public LoginDialogViewModel(Action? callback) : base(callback)
        {
            // Default to Windows auth if available
            _useWindows = true;
            
            // Pre-fill with current Windows user
            _userName = Environment.UserName;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the dialog result.
        /// </summary>
        public DialogResult Result { get; set; } = new DialogResult();

        /// <summary>
        /// Gets or sets the username for SQL Server authentication.
        /// </summary>
        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                RaisePropertyChanged();
                ClearError();
            }
        }

        /// <summary>
        /// Gets or sets the password for SQL Server authentication.
        /// </summary>
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                RaisePropertyChanged();
                ClearError();
            }
        }

        /// <summary>
        /// Gets or sets whether to use Windows authentication.
        /// </summary>
        public bool UseWindowsAuthentication
        {
            get => _useWindows;
            set
            {
                _useWindows = value;
                RaisePropertyChanged();
                ClearError();
            }
        }

        /// <summary>
        /// Gets or sets the error message to display.
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasError));
            }
        }

        /// <summary>
        /// Gets whether there's an error to display.
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(_errorMessage);

        /// <summary>
        /// Gets whether a login is in progress.
        /// </summary>
        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            set
            {
                _isLoggingIn = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to attempt login.
        /// </summary>
        public ICommand LoginCommand =>
            _loginCommand ??= new TrackerCommand(LoginCommandExecuted, CanExecuteLoginCommand);

        /// <summary>
        /// Command to cancel and close the dialog.
        /// </summary>
        public ICommand CancelCommand => _cancelCommand ??= new TrackerCommand(CancelCommandExecuted);

        #endregion

        #region Private Methods

        private void CancelCommandExecuted(object? obj)
        {
            Result = new DialogResult
            {
                Cancelled = true
            };
            Callback?.Invoke();
        }

        private bool CanExecuteLoginCommand(object? obj)
        {
            if (_isLoggingIn) return false;
            
            if (UseWindowsAuthentication)
            {
                return true;
            }

            return !string.IsNullOrEmpty(_password) && !string.IsNullOrEmpty(_userName);
        }

        private async void LoginCommandExecuted(object? obj)
        {
            IsLoggingIn = true;
            ClearError();

            try
            {
                var settings = UserSettingsManager.Instance.Settings.Database;
                
                // For SQL Server auth, update credentials in settings temporarily for connection test
                if (!UseWindowsAuthentication && settings.Type == DatabaseType.SqlServer)
                {
                    settings.UseWindowsAuth = false;
                    settings.Username = _userName;
                    settings.Password = _password;
                }
                else if (settings.Type == DatabaseType.SqlServer)
                {
                    settings.UseWindowsAuth = true;
                }

                // Test the database connection
                var connectionResult = await TrackerDbManager.Instance!.TestConnectionAsync(settings);

                if (connectionResult.Success)
                {
                    // Store the current user for audit tracking
                    UserSettingsManager.Instance.CurrentUser = UseWindowsAuthentication 
                        ? Environment.UserName 
                        : _userName;

                    Result = new DialogResult
                    {
                        Cancelled = false
                    };
                    Callback?.Invoke();
                }
                else
                {
                    ErrorMessage = "Unable to connect to database. Please check your credentials.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Login failed: {ex.Message}";
            }
            finally
            {
                IsLoggingIn = false;
            }
        }

        private void ClearError()
        {
            ErrorMessage = string.Empty;
        }

        #endregion
    }
}
