# 04 – Authentication Flow

This document describes the **authentication architecture** in ProCohere.Avalonia.

---

## Overview

Authentication uses:
- **Supabase Auth** (email/password, managed by `AuthService`)
- **Windows DPAPI** for secure session storage (`WindowsCredentialService`)
- **Two Supabase clients** (public schema for auth, procohere schema for data)

---

## Key Classes

| Class | File | Purpose |
|-------|------|---------|
| `AuthService` | `Services/AuthService.cs` | Singleton auth manager, Supabase client wrapper |
| `ICredentialService` | `Services/ICredentialService.cs` | Interface for session storage |
| `WindowsCredentialService` | `Services/WindowsCredentialService.cs` | DPAPI-encrypted session storage |
| `SupabaseConfig` | `Services/SupabaseConfig.cs` | Project URL and Anon Key |

---

## AuthService (Singleton)

### Access
```csharp
AuthService.Instance
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `IsInitialized` | bool | Supabase clients initialized |
| `IsSignedIn` | bool | User currently authenticated |
| `CurrentUser` | `User?` | Supabase auth user |
| `CurrentSession` | `Session?` | Supabase auth session |
| `AccessToken` | `string?` | JWT access token |
| `CurrentProfile` | `UserProfile?` | User profile from `public.users` |
| `CurrentSession_ProCohere` | `ProCohereUserSessionDto?` | Full session with team/role |
| `CurrentTeamMember` | `TeamMemberDto?` | Shortcut to session team member |
| `CurrentRole` | `RoleDto?` | Shortcut to session role |
| `HasStoredSession` | bool | Credentials exist for auto-login |

### Events

| Event | Payload | When Fired |
|-------|---------|------------|
| `AuthStateChanged` | `User?` | Sign in, sign out, session restore |
| `ProfileChanged` | `UserProfile?` | Profile loaded or updated |

---

## Two Supabase Clients

```csharp
// Public schema client - auth and licensing
private Supabase.Client? _publicClient;

// ProCohere schema client - app data
private Supabase.Client? _procohereClient;
```

### Why Two Clients?

1. **public schema** (`_publicClient`):
   - Authentication (sign in, sign up, sign out)
   - User profile (`public.users`)
   - Avatar storage
   - Licensing functions

2. **procohere schema** (`_procohereClient`):
   - All app data (meetings, goals, tasks, etc.)
   - RLS enforced per user
   - Session function (`get_user_session`)

### Initialization

```csharp
// Public client (default schema)
_publicClient = new Supabase.Client(
    SupabaseConfig.ProjectUrl,
    SupabaseConfig.AnonKey,
    new SupabaseOptions
    {
        AutoRefreshToken = true,
        AutoConnectRealtime = false
    });

// ProCohere client (explicit schema)
_procohereClient = new Supabase.Client(
    SupabaseConfig.ProjectUrl,
    SupabaseConfig.AnonKey,
    new SupabaseOptions
    {
        AutoRefreshToken = false,  // Public client manages auth
        AutoConnectRealtime = false,
        Schema = "procohere"
    });
```

### Auth Synchronization

After any authentication, the session must be synced to both clients:

```csharp
private async Task SyncAuthToProCohereClientAsync()
{
    var session = _publicClient?.Auth.CurrentSession;
    if (session != null && _procohereClient != null)
    {
        await _procohereClient.Auth.SetSession(
            session.AccessToken!, 
            session.RefreshToken!);
    }
}
```

---

## Session Storage (DPAPI)

### File Location
```
%LocalAppData%\ProCohere\session.protected
```

### Storage Format
```csharp
private class SessionData
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? UserEmail { get; set; }
    public string? UserId { get; set; }
    public DateTime StoredAt { get; set; }
}
```

### Security
- Encrypted with Windows DPAPI (`ProtectedData.Protect`)
- Scoped to `DataProtectionScope.CurrentUser`
- Only the same Windows user can decrypt
- File is binary (not readable as JSON)

### Methods

```csharp
bool StoreSession(accessToken, refreshToken, email, userId)
(string? AccessToken, string? RefreshToken) GetStoredSession()
(string? Email, string? UserId) GetStoredUserIdentity()
bool ClearSession()
bool HasStoredSession()
```

---

## Authentication Flows

### Flow 1: App Startup (Auto-Login)

```
App.OnFrameworkInitializationCompleted
├── AuthService.Instance.InitializeAsync()
│   ├── Create public client
│   └── Create procohere client
├── AuthService.Instance.TryAutoLoginAsync()
│   ├── Check HasStoredSession
│   ├── Get stored tokens (DPAPI decrypt)
│   ├── Call SetSession(accessToken, refreshToken)
│   ├── If success: SyncAuthToProCohereClientAsync()
│   └── Return true/false
├── If auto-login succeeded:
│   └── Show MainWindow
└── If auto-login failed:
    └── Show LoginWindow
```

### Flow 2: Manual Sign In

```
LoginWindow.SignInButton_Click
├── AuthService.Instance.SignInAsync(email, password, rememberMe)
│   ├── PublicClient.Auth.SignIn(email, password)
│   ├── SyncAuthToProCohereClientAsync()
│   ├── If rememberMe: StoreSession(tokens, email, userId)
│   └── Fire AuthStateChanged
├── AuthService.Instance.GetUserSessionAsync("procohere")
│   ├── Call RPC: get_user_session
│   └── Store CurrentSession_ProCohere
├── If session.HasAccess == false:
│   └── Show "No license" error
└── Navigate to MainWindow
```

### Flow 3: Sign Up

```
SignUpWindow.SignUpButton_Click
├── AuthService.Instance.SignUpAsync(email, password, displayName)
│   ├── PublicClient.Auth.SignUp(email, password, {display_name})
│   ├── SyncAuthToProCohereClientAsync()
│   ├── Always store session (new users get remembered)
│   └── Fire AuthStateChanged
└── Redirect to onboarding or main app
```

### Flow 4: Sign Out

```
MainWindowViewModel.SignOutAsync
├── AuthService.Instance.SignOutAsync()
│   ├── PublicClient.Auth.SignOut()
│   ├── ClearSession() (delete DPAPI file)
│   ├── ClearSessionData() (null out properties)
│   └── Fire AuthStateChanged(null)
├── Fire SignOutRequested event
└── MainWindow closes, App shows LoginWindow
```

---

## User Session DTO

After login, call `GetUserSessionAsync` to get full context:

```csharp
public class ProCohereUserSessionDto
{
    public bool HasAccess { get; set; }
    public string? Error { get; set; }
    public TeamMemberDto? TeamMember { get; set; }
    public RoleDto? Role { get; set; }
}

public class TeamMemberDto
{
    public Guid Id { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    // ... more fields
}

public class RoleDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }  // "admin", "manager", "user"
    // ... more fields
}
```

---

## Profile Management

### Load Profile
```csharp
var profile = await AuthService.Instance.LoadUserProfileAsync();
```

Queries `public.users` where `id = auth.uid()`.

### Update Profile
```csharp
var (success, error) = await AuthService.Instance.UpdateUserProfileAsync(
    firstName: "John",
    lastName: "Doe",
    jobTitle: "Manager",
    company: "Acme Corp",
    phone: "555-1234",
    timezone: "America/New_York"
);
```

### Upload Avatar
```csharp
var (success, avatarUrl, error) = await AuthService.Instance.UploadAvatarAsync(filePath);
```

Uploads to `avatars` bucket at path `{userId}/avatar.{ext}`.

---

## Product Access Check

Before accessing the app, check license:

```csharp
bool hasAccess = await AuthService.Instance.HasProductAccessAsync("procohere");
```

Calls RPC: `user_has_active_product_access(product_code)`.

Returns `true` if:
- User has a product seat assignment
- Organization has active license

---

## Error Handling

### Friendly Error Messages

```csharp
private static string GetFriendlyAuthError(GotrueException ex)
{
    var message = ex.Message.ToLowerInvariant();

    if (message.Contains("invalid login"))
        return "Invalid email or password.";
    if (message.Contains("email not confirmed"))
        return "Please verify your email address before signing in.";
    if (message.Contains("too many requests"))
        return "Too many attempts. Please wait a moment and try again.";
    if (message.Contains("user already registered"))
        return "An account with this email already exists.";
    // ...
}
```

### Token Expiry Handling

In avatar upload (and should be elsewhere):
```csharp
var expiresAt = session.ExpiresAt();
if (expiresAt < DateTime.UtcNow)
{
    var refreshedSession = await _publicClient!.Auth.RefreshSession();
    // Use refreshed session
}
```

---

## Data Counts (Debug)

For testing/debugging, shows data accessible to user:

```csharp
var counts = await AuthService.Instance.GetDataCountsAsync();
// counts.TeamMembers, counts.Meetings, counts.Goals, etc.
```

Queries procohere schema tables filtered by `created_by_user_id`.

---

## SupabaseConfig

```csharp
public static class SupabaseConfig
{
    public const string ProjectUrl = "https://xxxxx.supabase.co";
    public const string AnonKey = "eyJ...";  // Public anon key (safe to expose)
}
```

**Note**: Anon key is public. All security is via RLS policies.

---

## Key Files

| File | Lines | Purpose |
|------|-------|---------|
| `Services/AuthService.cs` | ~1068 | Main auth service |
| `Services/ICredentialService.cs` | ~40 | Session storage interface |
| `Services/WindowsCredentialService.cs` | ~160 | DPAPI implementation |
| `Services/SupabaseConfig.cs` | ~10 | Config constants |
| `Models/UserProfile.cs` | - | User model for public.users |
| `Models/ProCohereUserSessionDto.cs` | - | Session response model |

---

## Invariants

1. **Two clients always exist** after InitializeAsync
2. **Auth sync required** after any authentication
3. **Session stored in DPAPI** (not plaintext, not Windows Credential Manager)
4. **Remember Me** stores both tokens AND user identity
5. **Profile loaded separately** from auth (different table)
6. **GetUserSessionAsync** required for team/role info
7. **RLS enforced** - user only sees their own data

---

## Common Patterns

### Check If Signed In
```csharp
if (AuthService.Instance.IsSignedIn)
{
    var user = AuthService.Instance.CurrentUser;
}
```

### Get Current User's Team Member
```csharp
var teamMember = AuthService.Instance.CurrentTeamMember;
if (teamMember != null)
{
    var role = AuthService.Instance.CurrentRole?.Name;
}
```

### Get Supabase Client for Queries
```csharp
// For auth/licensing
var publicClient = AuthService.Instance.GetSupabaseClient();

// For app data
var procohereClient = AuthService.Instance.GetProCohereClient();
```

