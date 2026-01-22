# 02 – Application Lifecycle

This document describes the **application startup, authentication, and shutdown flow**.

---

## Entry Point

### Program.cs
Standard Avalonia bootstrap:
```csharp
public static void Main(string[] args)
    => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
```

### App.axaml.cs
Application initialization and lifecycle management.

---

## Startup Flow

```
Program.Main()
    │
    ▼
App.Initialize()
    │ Load XAML resources
    ▼
App.OnFrameworkInitializationCompleted()
    │
    ├── DisableAvaloniaDataAnnotationValidation()
    │   (Avoid duplicate validation with CommunityToolkit)
    │
    ├── ThemeService.Instance.Initialize()
    │   (Apply saved theme preference)
    │
    ├── Show SplashWindow
    │
    └── InitializeAndNavigateAsync()
            │
            ├── AuthService.Instance.TryAutoLoginAsync()
            │       │
            │       ├── Success + HasAccess → MainWindow
            │       │
            │       ├── Success + NoAccess → SignOut → LoginWindow
            │       │
            │       └── Failure → LoginWindow
            │
            └── Close SplashWindow
```

---

## Windows

### SplashWindow
- Shows ProCohere logo during initialization
- Displayed while checking stored credentials
- Closed once destination window is determined

### LoginWindow
- Email/password authentication
- "Remember Me" checkbox for credential storage
- On success: Creates MainWindow, closes LoginWindow

### MainWindow
- Primary application window
- Contains navigation sidebar
- Hosts all main views (Briefing, Me, Circle, Pulse, Settings)
- Handles sign-out → recreates LoginWindow

---

## Authentication Check Flow

```csharp
private static async Task InitializeAndNavigateAsync(
    IClassicDesktopStyleApplicationLifetime desktop, 
    SplashWindow splashWindow)
{
    var autoLoginSuccess = await AuthService.Instance.TryAutoLoginAsync();

    if (autoLoginSuccess)
    {
        var session = await AuthService.Instance.GetUserSessionAsync("procohere");
        
        if (!session.HasAccess)
        {
            // User lost product access - sign out
            await AuthService.Instance.SignOutAsync();
            ShowLoginWindow(desktop, splashWindow);
            return;
        }
        
        // Go to main window
        var mainWindow = new MainWindow
        {
            DataContext = new MainWindowViewModel()
        };
        desktop.MainWindow = mainWindow;
        mainWindow.Show();
        splashWindow.Close();
    }
    else
    {
        ShowLoginWindow(desktop, splashWindow);
    }
}
```

---

## Auto-Login (Remember Me)

### Storage
Credentials stored in Windows Credential Manager:
- Key: `ProCohere_Session`
- Contains: Refresh token (encrypted by Windows)

### TryAutoLoginAsync Flow
1. Check if stored session exists
2. Retrieve encrypted refresh token
3. Call Supabase to refresh session
4. If success, populate CurrentUser, CurrentSession
5. Return true/false

### Session Persistence
```
Login with "Remember Me"
    │
    ▼
AuthService stores refresh token
    │
    ▼
WindowsCredentialService.SaveSession()
    │
    ▼
Windows Credential Manager (encrypted)
```

---

## Sign-Out Flow

```
User clicks Sign Out (Settings or MainWindow menu)
    │
    ▼
SettingsViewModel.LogoutCommand / MainWindowViewModel.SignOutCommand
    │
    ▼
AuthService.Instance.SignOutAsync()
    │
    ├── Clear Supabase session
    ├── Clear stored credentials
    └── Reset CurrentUser, CurrentSession
    │
    ▼
MainWindow.OnLogoutRequested()
    │
    ├── Create new LoginWindow
    ├── Set as MainWindow
    └── Close current MainWindow
```

---

## Theme Initialization

Happens during `OnFrameworkInitializationCompleted`:

```csharp
ThemeService.Instance.Initialize();
```

ThemeService:
1. Reads saved theme preference from LocalSettingsService
2. Applies Light or Dark theme
3. Sets `IsDarkTheme` property for UI binding

---

## Error Handling

### Auto-Login Errors
Caught and logged, fall back to LoginWindow:
```csharp
catch (Exception ex)
{
    System.Diagnostics.Debug.WriteLine($"Auto-login failed: {ex.Message}");
    ShowLoginWindow(desktop, splashWindow);
}
```

### Session Errors
If auto-login succeeds but session check fails, sign out and show login.

---

## Key Files

| File | Purpose |
|------|---------|
| `Program.cs` | Entry point |
| `App.axaml` | Application resources |
| `App.axaml.cs` | Lifecycle management |
| `Views/SplashWindow.axaml` | Loading screen |
| `Views/LoginWindow.axaml` | Authentication UI |
| `Views/MainWindow.axaml` | Primary app window |
| `Services/AuthService.cs` | Authentication logic |
| `Services/ThemeService.cs` | Theme management |

---

## Invariants

1. SplashWindow is always shown first
2. Auto-login is always attempted before showing LoginWindow
3. Product access is always verified after successful auth
4. Theme is initialized before any window is shown
5. MainWindow is never shown without valid session

