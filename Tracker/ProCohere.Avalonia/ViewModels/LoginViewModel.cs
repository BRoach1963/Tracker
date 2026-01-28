using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProCohere.Avalonia.Services;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProCohere.Avalonia.ViewModels;

/// <summary>
/// ViewModel for the Pro Cohere login window.
/// </summary>
public partial class LoginViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand))]
    private string _email = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand))]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _keepMeSignedIn;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SignInCommand))]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _isPasswordVisible;

    /// <summary>
    /// Event raised when login is successful.
    /// </summary>
    public event Action? LoginSuccessful;

    public LoginViewModel()
    {
        LoadRememberedSettings();
    }

    [RelayCommand(CanExecute = nameof(CanSignIn))]
    private async Task SignInAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        ClearError();

        try
        {
            var (success, error) = await AuthService.Instance.SignInAsync(Email, Password, KeepMeSignedIn);

            if (success)
            {
                // Get the full user session (includes access check, team member, and role)
                var session = await AuthService.Instance.GetUserSessionAsync("procohere");
                
                if (!session.HasAccess)
                {
                    // Sign them out - they authenticated but don't have product access
                    await AuthService.Instance.SignOutAsync();
                    SetError(session.Error ?? "You don't have access to ProCohere. Please contact your administrator.");
                    return;
                }
                
                SaveRememberedSettings();
                LoginSuccessful?.Invoke();
            }
            else
            {
                SetError(error ?? "Sign in failed.");
            }
        }
        catch (Exception ex)
        {
            // Log full exception to file for debugging
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var logDir = System.IO.Path.Combine(appData, "ProCohere");
                if (!System.IO.Directory.Exists(logDir))
                    System.IO.Directory.CreateDirectory(logDir);
                
                var logPath = System.IO.Path.Combine(logDir, "login_errors.log");
                var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Login Exception\n" +
                            $"  Type: {ex.GetType().FullName}\n" +
                            $"  Message: {ex.Message}\n" +
                            $"  Stack: {ex.StackTrace}\n" +
                            $"  Inner: {ex.InnerException?.GetType().Name}: {ex.InnerException?.Message}\n\n";
                System.IO.File.AppendAllText(logPath, entry);
            }
            catch { /* ignore */ }
            
            SetError($"Connection error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanSignIn()
    {
        return !IsLoading && IsValidEmail(Email) && !string.IsNullOrWhiteSpace(Password);
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }

    [RelayCommand]
    private void ForgotPassword()
    {
        try
        {
            var resetUrl = "https://procohere.com/reset-password";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = resetUrl,
                UseShellExecute = true
            });
        }
        catch
        {
            // Silently fail
        }
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
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

    private void LoadRememberedSettings()
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var emailPath = System.IO.Path.Combine(appData, "ProCohere", "remembered_email.txt");

            if (System.IO.File.Exists(emailPath))
            {
                Email = System.IO.File.ReadAllText(emailPath).Trim();
                // If we have a remembered email, default to keeping signed in
                KeepMeSignedIn = true;
            }
        }
        catch
        {
            // Silently fail
        }
    }

    private void SaveRememberedSettings()
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var directory = System.IO.Path.Combine(appData, "ProCohere");
            var emailPath = System.IO.Path.Combine(directory, "remembered_email.txt");

            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            if (KeepMeSignedIn)
            {
                // Remember email for next time
                System.IO.File.WriteAllText(emailPath, Email);
            }
            else
            {
                // User doesn't want to stay signed in - clear remembered email
                if (System.IO.File.Exists(emailPath))
                {
                    System.IO.File.Delete(emailPath);
                }
            }
        }
        catch
        {
            // Silently fail
        }
    }
}