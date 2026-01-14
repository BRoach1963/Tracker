# Authentication Flow

**Document Version:** 1.0  
**Last Updated:** January 14, 2026  
**Prerequisites:** Read [04_SUPABASE_AND_RLS.md](04_SUPABASE_AND_RLS.md) first

---

## Overview

Tracker uses **Supabase Authentication** for user management. This document explains:

1. How users sign up and sign in
2. How tokens are managed
3. How authenticated state flows through the app
4. How the database connection gets the current user context

---

## Authentication Architecture

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                              AUTHENTICATION FLOW                              │
└──────────────────────────────────────────────────────────────────────────────┘

         ┌─────────────────┐
         │  LoginDialog    │
         │  (UI)           │
         └────────┬────────┘
                  │ email/password
                  ▼
         ┌─────────────────┐         ┌─────────────────┐
         │ LoginDialogVM   │────────▶│SupabaseService  │
         │ (ViewModel)     │         │  SignInAsync()  │
         └─────────────────┘         └────────┬────────┘
                                              │ HTTP to Supabase
                                              ▼
                                     ┌─────────────────┐
                                     │ Supabase Auth   │
                                     │ (Cloud)         │
                                     └────────┬────────┘
                                              │ JWT Token + User
                                              ▼
                                     ┌─────────────────┐
                                     │ SupabaseService │
                                     │ - Store token   │
                                     │ - Fire events   │
                                     └────────┬────────┘
                                              │
                  ┌───────────────────────────┼───────────────────────────┐
                  ▼                           ▼                           ▼
         ┌─────────────────┐         ┌─────────────────┐         ┌─────────────────┐
         │UserSettingsManager        │OrganizationContext        │ UI Updates      │
         │- Store UserId (Guid)      │- Set current org          │- Show username  │
         │- Save to disk    │         │- Available to DI │        │- Enable features│
         └─────────────────┘         └─────────────────┘         └─────────────────┘
```

---

## Key Components

### 1. SupabaseService

**Location:** `Services/Backend/SupabaseService.cs`

**Purpose:** Communicates with Supabase Auth API.

```csharp
public class SupabaseService
{
    private Supabase.Client? _client;

    // Properties
    public bool IsSignedIn => _client?.Auth.CurrentUser != null;
    public User? CurrentUser => _client?.Auth.CurrentUser;
    public string? AccessToken => _client?.Auth.CurrentSession?.AccessToken;

    // Events
    public event EventHandler<AuthState>? AuthStateChanged;

    // Methods
    public async Task<(bool Success, string? Error)> SignInAsync(email, password);
    public async Task<(bool Success, string? Error)> SignUpAsync(email, password, displayName);
    public async Task SignOutAsync();
}
```

### 2. AuthService

**Location:** `Services/Auth/AuthService.cs`

**Purpose:** Local authentication (JWT generation, password hashing).

```csharp
public class AuthService
{
    // Singleton
    public static AuthService Instance { get; }

    // Properties
    public bool IsSignedIn { get; }
    public AuthenticatedUser? CurrentUser { get; }
    public Guid? CurrentUserId { get; }

    // Password methods
    public string HashPassword(string password);
    public bool VerifyPassword(string password, string hash);

    // JWT methods
    public ClaimsPrincipal? ValidateToken(string token, string jwtSecret);
}
```

### 3. AuthenticationManager

**Location:** `Managers/AuthenticationManager.cs`

**Purpose:** Coordinates between Supabase auth and local auth.

```csharp
public class AuthenticationManager
{
    public static AuthenticationManager Instance { get; }

    // Combined check: Supabase OR local auth
    public bool IsSignedIn => _authService.IsSignedIn || 
        SupabaseService.Instance.IsSignedIn;

    public Guid? CurrentUserId { get; }
}
```

### 4. UserSettingsManager

**Location:** `Managers/UserSettingsManager.cs`

**Purpose:** Stores user-specific settings, including auth state.

Stores in: `%LocalAppData%\Tracker\Users\{userId}\TrackerSettings.json`

```csharp
// After login, store the Supabase user ID (Guid, not string)
var authSettings = UserSettingsManager.Instance.Settings.Authentication;
authSettings.UserId = user.Id;  // Guid - direct assignment
authSettings.UserEmail = user.Email;
authSettings.AccountSetupCompleted = true;
```

---

## Sign Up Flow

### Step-by-Step

```
User clicks "Sign Up"
        │
        ▼
┌──────────────────────────────────────────────────────────────────┐
│ LoginDialogViewModel.ExecuteSignUp()                              │
│                                                                   │
│ 1. Validate inputs (email format, password strength)             │
│ 2. Call SupabaseService.SignUpAsync(email, password, name)       │
└───────────────────────────────────────────┬──────────────────────┘
                                            │
                                            ▼
┌──────────────────────────────────────────────────────────────────┐
│ SupabaseService.SignUpAsync()                                     │
│                                                                   │
│ 1. Call Supabase Auth API                                        │
│ 2. On success: User created in Supabase auth.users table         │
│ 3. JWT tokens returned (access_token, refresh_token)             │
│ 4. Fire AuthStateChanged event                                   │
│ 5. Load user profile from database                               │
└───────────────────────────────────────────┬──────────────────────┘
                                            │
                                            ▼
┌──────────────────────────────────────────────────────────────────┐
│ Post-Signup                                                       │
│                                                                   │
│ 1. Store UserId (Guid) in UserSettingsManager                    │
│ 2. Switch to user-specific settings folder                       │
│ 3. Show main application window                                  │
└──────────────────────────────────────────────────────────────────┘
```

### Code Example

```csharp
// In LoginDialogViewModel
private async Task ExecuteSignUp()
{
    // 1. Call Supabase
    var (success, error) = await SupabaseService.Instance.SignUpAsync(
        Email, Password, DisplayName);

    if (!success)
    {
        ErrorMessage = error ?? "Sign up failed";
        return;
    }

    // 2. Get the new user
    var user = SupabaseService.Instance.CurrentUser;

    // 3. Switch settings to this user
    await UserSettingsManager.Instance.SwitchToUserAsync(user.Id);

    // 4. Store auth info (UserId is Guid, no ToString() needed)
    var authSettings = UserSettingsManager.Instance.Settings.Authentication;
    authSettings.UserId = Guid.Parse(user.Id);  // Parse string to Guid
    authSettings.UserEmail = user.Email;
    authSettings.AccountSetupCompleted = true;

    // 5. Close dialog
    _closeAction?.Invoke();
}
```

---

## Sign In Flow

### Step-by-Step

```
User enters email/password, clicks "Sign In"
        │
        ▼
┌──────────────────────────────────────────────────────────────────┐
│ LoginDialogViewModel.ExecuteSignIn()                              │
│                                                                   │
│ 1. Validate inputs                                               │
│ 2. Call SupabaseService.SignInAsync(email, password)             │
└───────────────────────────────────────────┬──────────────────────┘
                                            │
                                            ▼
┌──────────────────────────────────────────────────────────────────┐
│ SupabaseService.SignInAsync()                                     │
│                                                                   │
│ 1. POST to Supabase /auth/v1/token?grant_type=password           │
│ 2. Supabase validates credentials                                │
│ 3. Returns: access_token, refresh_token, user object             │
│ 4. Stores session internally                                     │
│ 5. Fires AuthStateChanged event                                  │
└───────────────────────────────────────────┬──────────────────────┘
                                            │
                                            ▼
┌──────────────────────────────────────────────────────────────────┐
│ Application State Updates                                         │
│                                                                   │
│ 1. UserSettingsManager.SwitchToUserAsync(userId)                 │
│ 2. OrganizationContext updated with current org                  │
│ 3. UI updates (username shown, features enabled)                 │
│ 4. Repositories can now query with user context                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## Token Management

### Token Types

| Token | Purpose | Lifetime |
|-------|---------|----------|
| **Access Token** | Authorizes API requests | ~1 hour |
| **Refresh Token** | Gets new access tokens | ~7 days |

### Token Storage

Supabase SDK handles token storage automatically. Tokens are persisted to disk so users stay logged in.

### Token Refresh

```csharp
// SupabaseService handles this automatically
// When access token expires, refresh token is used to get a new one

// Manual refresh if needed:
await _client.Auth.RefreshSession();
```

### Session Restoration

On app startup, we try to restore the previous session:

```csharp
// In SupabaseService.InitializeAsync()
private async Task TryRestoreSessionAsync()
{
    try
    {
        // Supabase SDK automatically checks for stored session
        if (_client?.Auth.CurrentSession != null)
        {
            // Session restored, fire event
            AuthStateChanged?.Invoke(this, AuthState.SignedIn);
            await LoadUserDataAsync();
        }
    }
    catch (Exception ex)
    {
        _logger.Warn("Could not restore session: {0}", ex.Message);
    }
}
```

---

## OrganizationContext

**Location:** `Services/OrganizationContext.cs`

**Purpose:** Provides current user/org context throughout the app via DI.

```csharp
public class OrganizationContext
{
    // Thread-local current context
    public static OrganizationContext Current { get; set; }

    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public Guid? UserIdOrNull => UserId == Guid.Empty ? null : UserId;
}
```

### Usage in Repositories

```csharp
// Getting current user for repository operations
private static Guid GetCurrentUserId()
{
    var context = OrganizationContext.Current;
    return context.UserIdOrNull ?? Guid.Empty;
}
```

---

## Sign Out Flow

```csharp
// In SupabaseService
public async Task SignOutAsync()
{
    try
    {
        await _client!.Auth.SignOut();
        
        // Clear local state
        CurrentProfile = null;
        CurrentSubscription = null;
        
        // Fire event - UI will update
        AuthStateChanged?.Invoke(this, AuthState.SignedOut);
    }
    catch (Exception ex)
    {
        _logger.Exception(ex, "Error signing out");
    }
}
```

### What Happens on Sign Out

1. Supabase session invalidated
2. Local tokens cleared
3. `AuthStateChanged` event fired with `SignedOut`
4. UI returns to login screen
5. User-specific settings remain on disk (for next login)

---

## Password Hashing

We use **BCrypt** for password hashing (when doing local auth):

```csharp
// In AuthService
public string HashPassword(string password)
{
    return BCrypt.Net.BCrypt.HashPassword(password, BcryptWorkFactor);
}

public bool VerifyPassword(string password, string hash)
{
    return BCrypt.Net.BCrypt.Verify(password, hash);
}
```

**Note:** For Supabase auth, password hashing is handled by Supabase. BCrypt is used for any local-only authentication scenarios.

---

## Security Considerations

### DO:
- ✅ Store tokens securely (Supabase SDK handles this)
- ✅ Use HTTPS for all API calls (Supabase enforces this)
- ✅ Clear tokens on sign out
- ✅ Handle token expiration gracefully

### DON'T:
- ❌ Log tokens or passwords
- ❌ Store passwords (only hashes, and Supabase handles this)
- ❌ Share the `service_role` key in client code
- ❌ Disable Supabase's email verification in production

---

## Error Handling

### Common Auth Errors

| Error | Meaning | User Message |
|-------|---------|--------------|
| `Invalid login credentials` | Wrong email/password | "Invalid email or password" |
| `Email not confirmed` | User hasn't verified email | "Please verify your email" |
| `User already registered` | Email in use | "An account with this email exists" |
| `Token expired` | Session timeout | (Auto-refresh or re-login) |

### Handling in ViewModel

```csharp
var (success, error) = await SupabaseService.Instance.SignInAsync(Email, Password);

if (!success)
{
    // Map technical errors to user-friendly messages
    ErrorMessage = error switch
    {
        "Invalid login credentials" => "Invalid email or password. Please try again.",
        "Email not confirmed" => "Please check your email and verify your account.",
        _ => error ?? "Sign in failed. Please try again."
    };
    return;
}
```

---

## Testing Authentication

### Manual Testing Checklist

- [ ] Sign up with new email
- [ ] Verify email confirmation required (if enabled)
- [ ] Sign in with correct credentials
- [ ] Sign in with wrong password (error shown)
- [ ] Close and reopen app (session restored)
- [ ] Sign out
- [ ] Sign in again (works)

### Test Users

Create test users in Supabase dashboard:
1. Go to Authentication → Users
2. Click "Create new user"
3. Enter test email and password
4. Use for development testing

---

## Next Steps

**Next:** Read [06_ADDING_NEW_ENTITIES.md](06_ADDING_NEW_ENTITIES.md) for a step-by-step guide on adding new entity types with repositories and services.
