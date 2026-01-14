# Troubleshooting

**Document Version:** 1.0  
**Last Updated:** January 14, 2026  
**Prerequisites:** Read all previous documents

---

## Overview

This document covers common issues you may encounter when working with Dapper and Supabase, along with solutions.

---

## Connection Issues

### Error: "Connection refused"

**Symptoms:**
```
Npgsql.NpgsqlException: Failed to connect to xxx.supabase.co:5432
Connection refused
```

**Causes:**
1. Supabase project is paused (free tier)
2. Firewall blocking connection
3. Wrong server address

**Solutions:**
1. Go to Supabase dashboard, check if project is active
2. Check firewall settings allow port 5432
3. Verify connection string server address

---

### Error: "SSL connection is required"

**Symptoms:**
```
Npgsql.NpgsqlException: SSL connection is required. Please enable SSL.
```

**Solution:**
Add `SSL Mode=Require` to connection string:
```
Server=db.xxx.supabase.co;Port=5432;Database=postgres;User Id=postgres;Password=xxx;SSL Mode=Require
```

---

### Error: "Password authentication failed"

**Symptoms:**
```
28P01: password authentication failed for user "postgres"
```

**Causes:**
1. Wrong password in connection string
2. Password was reset in Supabase

**Solutions:**
1. Go to Supabase Dashboard → Settings → Database
2. Reset database password if needed
3. Update connection string with correct password

---

### Error: "No database connection string found"

**Symptoms:**
```
InvalidOperationException: No database connection string found. Configure in appsettings.json or UserSettings.
```

**Solutions:**
1. Add connection string to `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "Supabase": "Server=db.xxx.supabase.co;..."
     }
   }
   ```
2. Or set environment variable:
   ```
   TRACKER_SUPABASE_CONNECTION_STRING=Server=db.xxx.supabase.co;...
   ```

---

## Query Issues

### Problem: Query returns empty but data exists

**Possible Causes:**

1. **Soft delete filtering:**
   ```sql
   -- This filters out deleted records
   WHERE is_deleted = false
   ```
   Check if records are soft-deleted in database.

2. **RLS filtering:**
   Row-Level Security might be filtering based on current user/org.

3. **Wrong organization_id:**
   Query might be filtering by wrong organization.

**Debugging:**
```csharp
// Add logging to see the actual query
_logger.LogDebug("Querying {TableName} with params: {Params}", 
    TableName, JsonSerializer.Serialize(parameters));
```

---

### Problem: Properties are null after query

**Cause:** Column names don't match property names.

**Database:** `created_at`  
**C# Property:** `CreatedAt`

**Solution:**
Dapper handles snake_case to PascalCase automatically. Verify:
1. Property name matches column (ignoring case/underscores)
2. Property is public with getter and setter
3. Property type matches column type

**For complex cases, use explicit mapping:**
```csharp
var sql = @"
    SELECT 
        id as Id,
        organization_id as OrganizationId,
        created_at as CreatedAt
    FROM table_name";
```

---

### Problem: "Invalid cast" or type mismatch

**Symptoms:**
```
System.InvalidCastException: Can't cast database type uuid to System.Int32
```

**Causes:**
1. C# property type doesn't match PostgreSQL column type
2. Using `int` for UUID columns

**Common Type Mappings:**

| PostgreSQL | C# |
|------------|------|
| `uuid` | `Guid` |
| `text`, `varchar` | `string` |
| `integer` | `int` |
| `bigint` | `long` |
| `boolean` | `bool` |
| `timestamp`, `timestamptz` | `DateTime` |
| `numeric`, `decimal` | `decimal` |
| `jsonb` | `string` (or custom class with handler) |

---

### Problem: Enum not mapping correctly

**Symptoms:**
```
Can't map database type 'meeting_type' to enum
```

**Solution:**
PostgreSQL enums need explicit handling. Options:

1. **Cast to string in SQL:**
   ```sql
   SELECT id, status::text as Status FROM meetings
   ```

2. **Register enum type with Npgsql:**
   ```csharp
   NpgsqlConnection.GlobalTypeMapper.MapEnum<MeetingType>("meeting_type");
   ```

3. **Use string property and convert in C#:**
   ```csharp
   public string StatusString { get; set; }
   public MeetingStatus Status => Enum.Parse<MeetingStatus>(StatusString);
   ```

---

## DI/Registration Issues

### Error: "No service for type 'IXxxRepository'"

**Symptoms:**
```
InvalidOperationException: No service for type 'IAnnouncementRepository' has been registered
```

**Solution:**
Add to `ServiceConfiguration.cs`:
```csharp
services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
```

---

### Error: "Cannot resolve scoped service from root provider"

**Symptoms:**
```
InvalidOperationException: Cannot resolve scoped service 'IUserRepository' from root provider
```

**Cause:**
Trying to resolve a scoped service from a singleton.

**Solution:**
Either:
1. Inject `IServiceProvider` and create a scope
2. Change the consuming service to scoped
3. Use a factory pattern

---

## Authentication Issues

### Error: "JWT expired"

**Symptoms:**
- API calls failing
- User appears logged out

**Solution:**
```csharp
// Refresh the session
await SupabaseService.Instance.RefreshSessionAsync();
```

---

### Problem: User sees other organization's data

**Causes:**
1. RLS not enabled on table
2. Wrong organization_id in context
3. Missing WHERE clause in repository

**Immediate Actions:**
1. Check OrganizationContext.Current.OrganizationId
2. Verify RLS policies in Supabase dashboard
3. Check repository WHERE clauses include organization_id

---

## Performance Issues

### Problem: Queries are slow

**Debugging Steps:**

1. **Check for missing indexes:**
   ```sql
   -- In Supabase SQL Editor
   EXPLAIN ANALYZE SELECT * FROM meetings WHERE organization_id = 'xxx';
   ```

2. **Add indexes if needed:**
   ```sql
   CREATE INDEX idx_meetings_org_id ON meetings(organization_id);
   ```

3. **Limit result sets:**
   ```csharp
   // Bad: Get all then filter
   var all = await GetAllAsync();
   var filtered = all.Where(x => x.Status == "active");

   // Good: Filter in database
   var filtered = await GetWhereSqlAsync("status = @Status", new { Status = "active" });
   ```

4. **Use pagination for large datasets:**
   ```csharp
   var (items, total) = await GetPagedAsync(page: 1, pageSize: 20);
   ```

---

### Problem: Too many database connections

**Symptoms:**
```
Npgsql.NpgsqlException: Connection pool exhausted
```

**Causes:**
1. Connections not being disposed
2. Connection pool size too small

**Solutions:**

1. **Ensure proper disposal:**
   ```csharp
   // Always use 'using'
   using var connection = _connectionFactory.CreateConnection();
   ```

2. **Increase pool size (if needed):**
   ```
   ...;Max Pool Size=100;...
   ```

3. **Check for connection leaks:**
   ```csharp
   // Bad: Connection never disposed
   var connection = _connectionFactory.CreateConnection();
   var result = await connection.QueryAsync<User>(sql);
   // connection.Dispose() never called!

   // Good: Using block disposes automatically
   using var connection = _connectionFactory.CreateConnection();
   var result = await connection.QueryAsync<User>(sql);
   ```

---

## Build Issues

### Error: "Ambiguous reference" between Dapper and EF repositories

**Symptoms:**
```
CS0104: 'IMeetingRepository' is an ambiguous reference between 
'Tracker.Services.Data.Repositories.IMeetingRepository' and 
'Tracker.Database.Repositories.IMeetingRepository'
```

**Cause:**
Both Dapper and legacy EF repositories exist during migration.

**Solution:**
Use fully qualified names or aliases:
```csharp
using DapperMeetingRepo = Tracker.Services.Data.Repositories.IMeetingRepository;
using EfMeetingRepo = Tracker.Database.Repositories.IMeetingRepository;
```

---

## Debugging Tips

### 1. Log SQL Queries

```csharp
// Before executing
_logger.LogDebug("Executing SQL: {Sql} with params: {Params}", 
    sql, JsonSerializer.Serialize(parameters));
```

### 2. Test Queries in Supabase SQL Editor

Copy the SQL from your code and run it directly in Supabase:
1. Go to SQL Editor
2. Paste query
3. Replace `@Param` with actual values
4. Run and check results

### 3. Check Supabase Logs

1. Go to Supabase Dashboard
2. Click "Logs" in sidebar
3. Check:
   - API logs (for auth issues)
   - Database logs (for query issues)

### 4. Enable Verbose Logging

In `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Tracker.Services.Data": "Debug",
      "Npgsql": "Debug"
    }
  }
}
```

---

## Quick Reference: Error → Solution

| Error | Quick Fix |
|-------|-----------|
| Connection refused | Check Supabase project is running |
| SSL required | Add `SSL Mode=Require` |
| Password failed | Reset password in Supabase |
| No connection string | Add to appsettings.json |
| Empty results | Check soft delete + RLS |
| Null properties | Verify column name mapping |
| Type mismatch | Fix C# type to match PostgreSQL |
| No service registered | Add to ServiceConfiguration.cs |
| Pool exhausted | Use `using`, increase pool size |
| JWT expired | Refresh session |

---

## Getting Help

1. **Check Supabase status:** [status.supabase.com](https://status.supabase.com)
2. **Supabase docs:** [supabase.com/docs](https://supabase.com/docs)
3. **Dapper docs:** [github.com/DapperLib/Dapper](https://github.com/DapperLib/Dapper)
4. **Npgsql docs:** [npgsql.org/doc](https://www.npgsql.org/doc/)
5. **Check existing code:** Look at how other repositories solve similar problems

---

## Summary

Most issues fall into these categories:

1. **Connection:** Wrong credentials, SSL missing, Supabase paused
2. **Query:** Soft delete filtering, RLS, type mismatches
3. **DI:** Service not registered
4. **Performance:** Missing indexes, connection leaks

When stuck:
1. Check the logs
2. Test SQL directly in Supabase
3. Compare with working repository code
4. Ask for help with specific error message
