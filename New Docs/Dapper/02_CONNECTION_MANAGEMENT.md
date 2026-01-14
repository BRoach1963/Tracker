# Connection Management

**Document Version:** 1.0  
**Last Updated:** January 14, 2026  
**Prerequisites:** Read [01_ARCHITECTURE_OVERVIEW.md](01_ARCHITECTURE_OVERVIEW.md) first

---

## Overview

This document explains how Tracker creates and manages database connections to Supabase PostgreSQL.

**Key Points:**
- All connections go through `DapperConnectionFactory`
- Connections are created per-operation (no long-lived connections)
- Connection strings come from configuration or user settings
- Npgsql is the PostgreSQL driver for .NET

---

## The Connection Factory

### File Location
`Tracker/Services/Data/DapperConnectionFactory.cs`

### Purpose
Creates database connections. This is the **ONLY** place in the codebase that creates `NpgsqlConnection` objects.

### Interface
```csharp
public interface IDapperConnectionFactory
{
    /// <summary>
    /// Create a new open connection to Supabase PostgreSQL database.
    /// </summary>
    IDbConnection CreateConnection();
}
```

### Implementation
```csharp
public class DapperConnectionFactory : IDapperConnectionFactory
{
    private readonly string _connectionString;

    public DapperConnectionFactory(IConfiguration? configuration = null)
    {
        // Priority order:
        // 1. appsettings.json ConnectionStrings:Supabase
        // 2. Environment variable TRACKER_SUPABASE_CONNECTION_STRING
        // 3. UserSettingsManager database settings
        _connectionString = configuration?.GetConnectionString("Supabase") 
            ?? GetConnectionStringFromSettings();

        if (string.IsNullOrEmpty(_connectionString))
            throw new InvalidOperationException(
                "No database connection string found.");
    }

    public IDbConnection CreateConnection()
    {
        var connection = new NpgsqlConnection(_connectionString);
        connection.Open();  // Connection is opened immediately
        return connection;
    }
}
```

---

## Connection String Sources

The factory looks for connection strings in this priority order:

### 1. appsettings.json (Highest Priority)
```json
{
  "ConnectionStrings": {
    "Supabase": "Server=db.xxxx.supabase.co;Port=5432;Database=postgres;User Id=postgres;Password=xxxxx;SSL Mode=Require;"
  }
}
```

**When to use:** Local development, testing, CI/CD pipelines.

### 2. Environment Variable
```
TRACKER_SUPABASE_CONNECTION_STRING=Server=db.xxxx.supabase.co;...
```

**When to use:** Container deployments, cloud environments, secure production setups.

### 3. UserSettingsManager (Lowest Priority)
Configured through the app's Settings UI and stored in user-specific settings file:
`%LocalAppData%\Tracker\Users\{userId}\TrackerSettings.json`

**When to use:** Per-user database selection in multi-tenant scenarios.

---

## Supabase Connection String Format

A typical Supabase PostgreSQL connection string:

```
Server=db.projectid.supabase.co;Port=5432;Database=postgres;User Id=postgres;Password=your-database-password;SSL Mode=Require;Trust Server Certificate=true
```

### Components:

| Component | Value | Notes |
|-----------|-------|-------|
| `Server` | `db.{project-id}.supabase.co` | From Supabase dashboard |
| `Port` | `5432` | PostgreSQL default |
| `Database` | `postgres` | Supabase uses `postgres` as main db |
| `User Id` | `postgres` | Supabase admin user |
| `Password` | Your database password | From Supabase dashboard |
| `SSL Mode` | `Require` | **Required** for Supabase |
| `Trust Server Certificate` | `true` | Needed for some environments |

### Finding Your Connection String in Supabase:

1. Go to [supabase.com](https://supabase.com) → Your Project
2. Click **Settings** (gear icon)
3. Click **Database**
4. Scroll to **Connection string**
5. Select **URI** tab and copy

---

## How Connections Are Used

### Pattern: One Connection Per Operation

Repositories create a connection, use it, then dispose it:

```csharp
public async Task<User?> GetByIdAsync(Guid id)
{
    // 1. Create new connection (opened automatically)
    using var connection = _connectionFactory.CreateConnection();
    
    // 2. Execute query
    var sql = "SELECT * FROM users WHERE id = @Id";
    var user = await connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
    
    // 3. Connection automatically disposed and returned to pool
    return user;
}
```

### Why This Pattern?

- **Connection Pooling:** Npgsql handles pooling automatically. Creating connections is cheap.
- **No Leaks:** `using` ensures connections are always disposed.
- **Thread Safety:** Each operation gets its own connection.
- **Simplicity:** No connection state management needed.

---

## Connection Pooling

Npgsql (the PostgreSQL driver) handles connection pooling automatically.

### Default Pool Settings:
- **Min Pool Size:** 0
- **Max Pool Size:** 100
- **Connection Idle Lifetime:** 300 seconds

### Customizing Pool Settings:
Add to connection string:
```
...;Min Pool Size=5;Max Pool Size=50;Connection Idle Lifetime=60;
```

### When to Customize:
- **High-traffic applications:** Increase max pool size
- **Limited server resources:** Decrease max pool size
- **Long-running desktop apps:** Decrease idle lifetime

---

## Dependency Injection Registration

The factory is registered as **Scoped** in `ServiceConfiguration.cs`:

```csharp
services.AddScoped<IDapperConnectionFactory, DapperConnectionFactory>();
```

### Why Scoped?

- **Request isolation:** Each DI scope gets its own factory instance
- **Configuration flexibility:** Different scopes can have different settings
- **WPF compatibility:** Works with the application's DI container

---

## Error Handling

### Common Connection Errors

| Error | Cause | Solution |
|-------|-------|----------|
| `Connection refused` | Database not accessible | Check Supabase is running, check firewall |
| `SSL connection required` | Missing SSL Mode | Add `SSL Mode=Require` to connection string |
| `Password authentication failed` | Wrong credentials | Verify password in Supabase dashboard |
| `Connection pool exhausted` | Too many open connections | Check for connection leaks, increase pool size |
| `Name resolution failed` | DNS issue | Check server hostname, check internet connection |

### Debugging Connection Issues

1. **Check the connection string:**
   ```csharp
   _logger.Info("Connection string: {0}", 
       _connectionString.Replace("Password=.*?;", "Password=***;"));
   ```

2. **Test connection manually:**
   ```csharp
   using var connection = new NpgsqlConnection(connectionString);
   connection.Open();  // Will throw if connection fails
   Console.WriteLine("Connected!");
   ```

3. **Check Supabase dashboard:**
   - Is the project paused? (free tier projects pause after inactivity)
   - Is the IP allowlisted? (if IP restrictions are enabled)

---

## Security Best Practices

### DO:
- ✅ Store connection strings in environment variables or secure config
- ✅ Use SSL Mode=Require for all connections
- ✅ Use connection pooling (enabled by default)
- ✅ Dispose connections properly (use `using`)

### DON'T:
- ❌ Hardcode connection strings in source code
- ❌ Log connection strings with passwords
- ❌ Share the Supabase database password
- ❌ Disable SSL for production

---

## Code Reference

### Full DapperConnectionFactory.cs
```csharp
using System;
using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Tracker.Classes;
using Tracker.Managers;

namespace Tracker.Services.Data
{
    public interface IDapperConnectionFactory
    {
        IDbConnection CreateConnection();
    }

    public class DapperConnectionFactory : IDapperConnectionFactory
    {
        private readonly string _connectionString;

        public DapperConnectionFactory(IConfiguration? configuration = null)
        {
            _connectionString = configuration?.GetConnectionString("Supabase") 
                ?? GetConnectionStringFromSettings();

            if (string.IsNullOrEmpty(_connectionString))
                throw new InvalidOperationException(
                    "No database connection string found.");
        }

        private static string GetConnectionStringFromSettings()
        {
            try
            {
                // Try environment variable first
                var envConnection = Environment.GetEnvironmentVariable(
                    "TRACKER_SUPABASE_CONNECTION_STRING");
                if (!string.IsNullOrEmpty(envConnection))
                    return envConnection;

                // Fall back to user settings
                var settings = UserSettingsManager.Instance?.Settings?.Database;
                if (settings?.Type == DatabaseType.PostgreSQL)
                    return settings.GetConnectionString();

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public IDbConnection CreateConnection()
        {
            var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}
```

---

## Next Steps

**Next:** Read [03_REPOSITORY_PATTERN.md](03_REPOSITORY_PATTERN.md) to understand how repositories use connections to execute queries.
