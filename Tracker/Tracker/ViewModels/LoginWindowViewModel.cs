using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Tracker.Command;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services.Backend;

namespace Tracker.ViewModels;

/// <summary>
/// ViewModel for the simplified login window.
/// </summary>
public partial class LoginWindowViewModel : INotifyPropertyChanged
{
    private readonly Action _onComplete;
    private readonly ILogger _logger;
    
    private string _email = string.Empty;
    private string _password = string.Empty;
    private bool _rememberMe;
    private bool _keepMeSignedIn;
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private bool _hasError;

    public LoginWindowViewModel(Action onComplete)
    {
        _onComplete = onComplete;
        _logger = LoggingManager.GetComponentLogger("LoginWindow");
        
        SignInCommand = new TrackerCommand(async _ => await SignInAsync(), _ => CanSignIn());
        ForgotPasswordCommand = new TrackerCommand(_ => OpenForgotPassword());
        
        // Load remembered email and preferences
        LoadRememberedEmail();
        LoadKeepMeSignedInPreference();
    }

    #region Properties

    public string Email
    {
        get => _email;
        set
        {
            if (_email != value)
            {
                _email = value;
                OnPropertyChanged();
                ClearError();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (_password != value)
            {
                _password = value;
                OnPropertyChanged();
                ClearError();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool RememberMe
    {
        get => _rememberMe;
        set
        {
            if (_rememberMe != value)
            {
                _rememberMe = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// When true, saves the session tokens encrypted on this Windows account.
    /// Next time the user launches the app, they'll be automatically signed in.
    /// </summary>
    public bool KeepMeSignedIn
    {
        get => _keepMeSignedIn;
        set
        {
            if (_keepMeSignedIn != value)
            {
                _keepMeSignedIn = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage != value)
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public bool HasError
    {
        get => _hasError;
        set
        {
            if (_hasError != value)
            {
                _hasError = value;
                OnPropertyChanged();
            }
        }
    }

    public bool LoginSuccessful { get; private set; }

    #endregion

    #region Commands

    public ICommand SignInCommand { get; }
    public ICommand ForgotPasswordCommand { get; }

    #endregion

    #region Methods

    private bool CanSignIn()
    {
        return !IsLoading && IsValidEmail(Email) && !string.IsNullOrWhiteSpace(Password);
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    private async Task SignInAsync()
    {
        if (IsLoading) return;
        
        IsLoading = true;
        ClearError();
        
        try
        {
            _logger.Info("Attempting sign in for: {0}", Email);
            _logger.Info("Keep me signed in: {0}", KeepMeSignedIn);
            
            // Initialize Supabase if not already
            if (!SupabaseService.Instance.IsInitialized)
            {
                await SupabaseService.Instance.InitializeAsync();
            }
            
            // Pass the KeepMeSignedIn flag to control whether tokens are saved
            var (success, error) = await SupabaseService.Instance.SignInAsync(Email, Password, KeepMeSignedIn);
            
            if (success)
            {
                _logger.Info("Sign in successful for: {0}", Email);
                
                // Always save email for convenience (it's not sensitive)
                SaveRememberedEmail();
                
                // Save the "keep me signed in" preference
                SaveKeepMeSignedInPreference();
                
                LoginSuccessful = true;
                _onComplete();
            }
            else
            {
                SetError(error ?? "Sign in failed. Please check your credentials.");
            }
        }
        catch (Exception ex)
        {
            _logger.Exception(ex, "Sign in failed");
            
            // Parse the error message for user-friendly display
            var errorMessage = ex.Message.ToLowerInvariant();
            
            if (errorMessage.Contains("invalid login") || errorMessage.Contains("invalid_credentials"))
            {
                SetError("Invalid email or password.");
            }
            else if (errorMessage.Contains("email not confirmed"))
            {
                SetError("Please verify your email address before signing in.");
            }
            else if (errorMessage.Contains("too many requests") || errorMessage.Contains("rate limit"))
            {
                SetError("Too many attempts. Please wait a moment and try again.");
            }
            else if (errorMessage.Contains("network") || errorMessage.Contains("connection"))
            {
                SetError("Unable to connect. Please check your internet connection.");
            }
            else
            {
                SetError("Sign in failed. Please try again.");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OpenForgotPassword()
    {
        try
        {
            // Open Supabase password reset page or custom page
            var resetUrl = "https://tracker-app.com/reset-password";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = resetUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.Warn("Failed to open password reset page: {0}", ex.Message);
        }
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    private void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }

    #endregion

    #region Remember Me

    private void LoadRememberedEmail()
    {
        try
        {
            var settingsPath = GetRememberMeFilePath();
            if (System.IO.File.Exists(settingsPath))
            {
                var content = System.IO.File.ReadAllText(settingsPath);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    Email = content.Trim();
                    RememberMe = true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warn("Failed to load remembered email: {0}", ex.Message);
        }
    }

    private void SaveRememberedEmail()
    {
        try
        {
            var settingsPath = GetRememberMeFilePath();
            var directory = System.IO.Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            System.IO.File.WriteAllText(settingsPath, Email);
        }
        catch (Exception ex)
        {
            _logger.Warn("Failed to save remembered email: {0}", ex.Message);
        }
    }

    private void ClearRememberedEmail()
    {
        try
        {
            var settingsPath = GetRememberMeFilePath();
            if (System.IO.File.Exists(settingsPath))
            {
                System.IO.File.Delete(settingsPath);
            }
        }
        catch (Exception ex)
        {
            _logger.Warn("Failed to clear remembered email: {0}", ex.Message);
        }
    }

    private string GetRememberMeFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return System.IO.Path.Combine(appData, "Tracker", "remember_me.txt");
    }

    private void LoadKeepMeSignedInPreference()
    {
        try
        {
            var settingsPath = GetKeepMeSignedInFilePath();
            if (System.IO.File.Exists(settingsPath))
            {
                var content = System.IO.File.ReadAllText(settingsPath);
                KeepMeSignedIn = content.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch (Exception ex)
        {
            _logger.Warn("Failed to load keep-me-signed-in preference: {0}", ex.Message);
        }
    }

    private void SaveKeepMeSignedInPreference()
    {
        try
        {
            var settingsPath = GetKeepMeSignedInFilePath();
            var directory = System.IO.Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            System.IO.File.WriteAllText(settingsPath, KeepMeSignedIn.ToString().ToLowerInvariant());
        }
        catch (Exception ex)
        {
            _logger.Warn("Failed to save keep-me-signed-in preference: {0}", ex.Message);
        }
    }

    private string GetKeepMeSignedInFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return System.IO.Path.Combine(appData, "Tracker", "keep_signed_in.txt");
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}
