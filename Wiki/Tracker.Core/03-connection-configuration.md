# 03 – Connection & Configuration

This document describes **database connection setup and configuration** in Tracker.Core.

---

## Configuration Location

All Supabase configuration lives in:
```
Tracker.Core/Services/Backend/SupabaseConfig.cs
```

This is an `internal static class` - values are compile-time constants.

---

## Configuration Values

### Project URL
```csharp
internal const string ProjectUrl = "https://cftzoxucrzqljadyiijd.supabase.co";
```

### Anon Key (Public)
```csharp
internal const string AnonKey = "eyJhbGci...";  // JWT format
```

This key is safe for client-side use because:
- It only allows operations permitted by Row Level Security (RLS)
- All sensitive operations require user authentication
- RLS policies enforce organization-level isolation

### Database Connection String
```csharp
internal const string DatabaseConnectionString = 
    "Host=db.cftzoxucrzqljadyiijd.supabase.co;" +
    "Port=5432;" +
    "Database=postgres;" +
    "Username=postgres;" +
    "Password=***;" +
    "SSL Mode=Require;" +
    "Trust Server Certificate=true;" +
    "Pooling=true;" +
    "Minimum Pool Size=1;" +
    "Maximum Pool Size=20;";
```

### Storage Configuration
```csharp
internal const string AvatarBucket = "avatars";
internal const int MaxAvatarSizeBytes = 512000;  // 500KB
internal const int AvatarSizePx = 256;
```

---

## DapperConnectionFactory

### Purpose
Single point of PostgreSQL connection creation.

### Implementation
```csharp
public class DapperConnectionFactory : IDapperConnectionFactory
{
    private readonly string _connectionString;
    private static DapperConnectionFactory? _instance;
    
    public static DapperConnectionFactory Instance { get; }
    
    public DapperConnectionFactory()
    {
        // Environment variable overrides compiled config
        var envConnection = Environment.GetEnvironmentVariable(
            "TRACKER_SUPABASE_CONNECTION_STRING");
        _connectionString = !string.IsNullOrEmpty(envConnection) 
            ? envConnection 
            : SupabaseConfig.DatabaseConnectionString;
    }
    
    public IDbConnection CreateConnection()
    {
        var connection = new NpgsqlConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
```

### Singleton Pattern
Factory is available as singleton via `DapperConnectionFactory.Instance`.
Used when dependency injection is not available.

### Environment Override
Set `TRACKER_SUPABASE_CONNECTION_STRING` environment variable to override the compiled connection string (useful for container/cloud deployments).

---

## Connection Pooling

### Settings
| Setting | Value | Purpose |
|---------|-------|---------|
| Pooling | true | Enable connection reuse |
| Minimum Pool Size | 1 | Keep at least 1 connection ready |
| Maximum Pool Size | 20 | Cap concurrent connections |

### Behavior
- First `CreateConnection()` opens a new connection
- Connection is returned to pool when disposed
- Next `CreateConnection()` reuses pooled connection
- Pool managed by Npgsql (not our code)

---

## SSL Configuration

| Setting | Value | Purpose |
|---------|-------|---------|
| SSL Mode | Require | Encrypt all traffic |
| Trust Server Certificate | true | Accept Supabase's certificate |

Supabase requires SSL for all database connections.

---

## Usage Patterns

### In Repositories (Recommended)
```csharp
public class MeetingRepository : BaseRepository<Meeting>
{
    public MeetingRepository(
        IDapperConnectionFactory connectionFactory,  // Injected
        ILogger<MeetingRepository> logger)
        : base(connectionFactory, logger)
    {
        TableName = "meetings";
    }
}
```

### Direct Usage (When DI Not Available)
```csharp
var factory = DapperConnectionFactory.Instance;
using var connection = factory.CreateConnection();
var result = await connection.QueryAsync<Meeting>(sql);
```

---

## Security Considerations

### What's Safe in Client Code
- Project URL (public)
- Anon Key (public, RLS-enforced)

### What's Protected
- User credentials (handled by Supabase Auth)
- Database password (compiled, not user-facing)
- User data (protected by RLS policies)

### RLS Enforcement
Even though we connect as `postgres` user:
- Supabase RLS policies are enforced
- JWT token passed in requests sets the security context
- Cross-organization data access is impossible

---

## Deployment Configuration

### Local Development
Uses compiled `SupabaseConfig` values.

### Container/Cloud
Set environment variable:
```bash
TRACKER_SUPABASE_CONNECTION_STRING="Host=...;Password=...;..."
```

This overrides compiled config without code changes.

---

## Troubleshooting

### Connection Timeout
- Check internet connectivity
- Verify Supabase project is active
- Check connection pool exhaustion

### Authentication Failed
- Verify password in connection string
- Check if Supabase project credentials changed

### SSL Errors
- Ensure `SSL Mode=Require` is set
- Add `Trust Server Certificate=true`

### Pool Exhaustion
- Ensure all connections are disposed (`using` statement)
- Consider increasing `Maximum Pool Size`
- Check for connection leaks

---

## Invariants

1. All connections created via `IDapperConnectionFactory`
2. All connections disposed via `using` statement
3. Connection string never logged or displayed
4. Environment variable takes precedence over compiled config
5. SSL is always required

