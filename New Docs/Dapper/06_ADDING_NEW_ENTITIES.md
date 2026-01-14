# Adding New Entities

**Document Version:** 1.0  
**Last Updated:** January 14, 2026  
**Prerequisites:** Read all previous documents

---

## Overview

This document provides a step-by-step guide for adding a new entity to Tracker with full Dapper support. By the end, you'll have:

1. A data model (POCO)
2. A repository interface and implementation
3. A business service (optional but recommended)
4. DI registration

---

## Prerequisites

Before adding a new entity:

1. ✅ Table exists in Supabase database
2. ✅ Table has standard columns (id, is_deleted, created_at, etc.)
3. ✅ You know the table structure (columns, types, relationships)

---

## Step 1: Create the Data Model

**Location:** `Tracker/DataModels/{EntityName}.cs`

### Template

```csharp
using Tracker.Common.Enums;  // If using enums

namespace Tracker.DataModels
{
    /// <summary>
    /// {Description of what this entity represents}.
    /// Maps to Supabase '{table_name}' table.
    /// </summary>
    public class {EntityName} : AuditableEntity
    {
        #region Core Identity

        /// <summary>
        /// Primary key (UUID).
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Organization this entity belongs to.
        /// </summary>
        public Guid OrganizationId { get; set; }

        #endregion

        #region Entity-Specific Properties

        /// <summary>
        /// {Property description}.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// {Property description}.
        /// </summary>
        public string? Description { get; set; }

        // Add all columns from the database table

        #endregion

        #region Relationships (Optional)

        /// <summary>
        /// Related team member.
        /// </summary>
        public Guid? TeamMemberId { get; set; }
        public TeamMember? TeamMember { get; set; }

        #endregion
    }
}
```

### Example: Announcement Entity

```csharp
namespace Tracker.DataModels
{
    /// <summary>
    /// Organization-wide announcement.
    /// Maps to Supabase 'announcements' table.
    /// </summary>
    public class Announcement : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid CreatedByUserId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Priority { get; set; }  // 'low', 'medium', 'high'
        
        public DateTime? ExpiresAt { get; set; }
        public bool IsPinned { get; set; }

        // Navigation
        public User? CreatedByUser { get; set; }
    }
}
```

### Property Naming Convention

| Database Column | C# Property | Notes |
|-----------------|-------------|-------|
| `id` | `Id` | PascalCase |
| `organization_id` | `OrganizationId` | Dapper handles snake_case → PascalCase |
| `created_at` | `CreatedAt` | In `AuditableEntity` base class |
| `is_deleted` | `IsDeleted` | In `AuditableEntity` base class |

**Note:** Dapper automatically maps `snake_case` columns to `PascalCase` properties. No additional configuration needed.

---

## Step 2: Create the Repository Interface

**Location:** `Tracker/Services/Data/Repositories/{EntityName}Repository.cs`

### Template

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tracker.DataModels;

namespace Tracker.Services.Data.Repositories
{
    /// <summary>
    /// Repository for {EntityName} entity.
    /// Provides data access for all {entity}-related operations.
    /// 
    /// This is the ONLY place that queries the '{table_name}' table.
    /// </summary>
    public interface I{EntityName}Repository : IRepository<{EntityName}>
    {
        /// <summary>
        /// Get all {entities} for an organization.
        /// </summary>
        Task<IEnumerable<{EntityName}>> GetByOrganizationAsync(Guid organizationId);

        /// <summary>
        /// Get active (not deleted, not expired) {entities}.
        /// </summary>
        Task<IEnumerable<{EntityName}>> GetActiveByOrganizationAsync(Guid organizationId);

        // Add entity-specific methods here
    }
}
```

---

## Step 3: Implement the Repository

**Location:** Same file as interface (or separate file if preferred)

### Template

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Tracker.DataModels;

namespace Tracker.Services.Data.Repositories
{
    public class {EntityName}Repository : BaseRepository<{EntityName}>, I{EntityName}Repository
    {
        public {EntityName}Repository(
            IDapperConnectionFactory connectionFactory,
            ILogger<{EntityName}Repository> logger)
            : base(connectionFactory, logger)
        {
            TableName = "{table_name}";  // e.g., "announcements"
        }

        public async Task<IEnumerable<{EntityName}>> GetByOrganizationAsync(Guid organizationId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM {table_name}
                    WHERE organization_id = @OrgId AND is_deleted = false
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<{EntityName}>(sql, new { OrgId = organizationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting {entities} by organization {OrgId}", organizationId);
                throw;
            }
        }

        public async Task<IEnumerable<{EntityName}>> GetActiveByOrganizationAsync(Guid organizationId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                const string sql = @"
                    SELECT * FROM {table_name}
                    WHERE organization_id = @OrgId 
                      AND is_deleted = false
                      AND (expires_at IS NULL OR expires_at > NOW())
                    ORDER BY created_at DESC";

                return await connection.QueryAsync<{EntityName}>(sql, new { OrgId = organizationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active {entities} by organization {OrgId}", organizationId);
                throw;
            }
        }
    }
}
```

### Example: AnnouncementRepository

```csharp
namespace Tracker.Services.Data.Repositories
{
    public interface IAnnouncementRepository : IRepository<Announcement>
    {
        Task<IEnumerable<Announcement>> GetByOrganizationAsync(Guid organizationId);
        Task<IEnumerable<Announcement>> GetActiveAsync(Guid organizationId);
        Task<IEnumerable<Announcement>> GetPinnedAsync(Guid organizationId);
    }

    public class AnnouncementRepository : BaseRepository<Announcement>, IAnnouncementRepository
    {
        public AnnouncementRepository(
            IDapperConnectionFactory connectionFactory,
            ILogger<AnnouncementRepository> logger)
            : base(connectionFactory, logger)
        {
            TableName = "announcements";
        }

        public async Task<IEnumerable<Announcement>> GetByOrganizationAsync(Guid organizationId)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT * FROM announcements
                WHERE organization_id = @OrgId AND is_deleted = false
                ORDER BY is_pinned DESC, created_at DESC";

            return await connection.QueryAsync<Announcement>(sql, new { OrgId = organizationId });
        }

        public async Task<IEnumerable<Announcement>> GetActiveAsync(Guid organizationId)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT * FROM announcements
                WHERE organization_id = @OrgId 
                  AND is_deleted = false
                  AND (expires_at IS NULL OR expires_at > NOW())
                ORDER BY is_pinned DESC, created_at DESC";

            return await connection.QueryAsync<Announcement>(sql, new { OrgId = organizationId });
        }

        public async Task<IEnumerable<Announcement>> GetPinnedAsync(Guid organizationId)
        {
            using var connection = _connectionFactory.CreateConnection();
            const string sql = @"
                SELECT * FROM announcements
                WHERE organization_id = @OrgId 
                  AND is_deleted = false
                  AND is_pinned = true
                ORDER BY created_at DESC";

            return await connection.QueryAsync<Announcement>(sql, new { OrgId = organizationId });
        }
    }
}
```

---

## Step 4: Register in Dependency Injection

**Location:** `Tracker/Infrastructure/ServiceConfiguration.cs`

Add to the `ConfigureServices` method:

```csharp
// In ServiceConfiguration.cs, inside ConfigureServices()

// Add with other repository registrations
services.AddScoped<I{EntityName}Repository, {EntityName}Repository>();

// Example:
services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
```

---

## Step 5: Create Business Service (Recommended)

**Location:** `Tracker/Services/{EntityName}Service.cs`

### Template

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tracker.DataModels;
using Tracker.Services.Data.Repositories;

namespace Tracker.Services
{
    public interface I{EntityName}Service
    {
        Task<IEnumerable<{EntityName}>> GetOrganization{Entities}Async(Guid organizationId);
        Task<{EntityName}> Create{EntityName}Async({EntityName} entity);
        Task Update{EntityName}Async({EntityName} entity);
        Task Delete{EntityName}Async(Guid id, Guid deletedByUserId);
        Task<{EntityName}?> Get{EntityName}Async(Guid id);
    }

    public class {EntityName}Service : I{EntityName}Service
    {
        private readonly I{EntityName}Repository _repository;
        private readonly ILogger<{EntityName}Service> _logger;

        public {EntityName}Service(
            I{EntityName}Repository repository,
            ILogger<{EntityName}Service> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<{EntityName}>> GetOrganization{Entities}Async(Guid organizationId)
        {
            return await _repository.GetByOrganizationAsync(organizationId);
        }

        public async Task<{EntityName}> Create{EntityName}Async({EntityName} entity)
        {
            // Add business validation here if needed
            return await _repository.CreateAsync(entity);
        }

        public async Task Update{EntityName}Async({EntityName} entity)
        {
            await _repository.UpdateAsync(entity);
        }

        public async Task Delete{EntityName}Async(Guid id, Guid deletedByUserId)
        {
            await _repository.DeleteAsync(id, deletedByUserId);
        }

        public async Task<{EntityName}?> Get{EntityName}Async(Guid id)
        {
            return await _repository.GetByIdAsync(id);
        }
    }
}
```

### Register Service in DI

```csharp
// In ServiceConfiguration.cs
services.AddScoped<I{EntityName}Service, {EntityName}Service>();

// Example:
services.AddScoped<IAnnouncementService, AnnouncementService>();
```

---

## Step 6: Use in ViewModel

```csharp
public class AnnouncementsViewModel : BaseViewModel
{
    private readonly IAnnouncementService _announcementService;
    private readonly Guid _organizationId;

    public AnnouncementsViewModel(IAnnouncementService announcementService)
    {
        _announcementService = announcementService;
        _organizationId = OrganizationContext.Current.OrganizationId;
    }

    public ObservableCollection<Announcement> Announcements { get; } = new();

    public async Task LoadAnnouncementsAsync()
    {
        var announcements = await _announcementService.GetOrganizationAnnouncementsAsync(_organizationId);
        
        Announcements.Clear();
        foreach (var a in announcements)
        {
            Announcements.Add(a);
        }
    }
}
```

---

## Complete Checklist

When adding a new entity, complete all these steps:

- [ ] **Step 1:** Create data model in `DataModels/{EntityName}.cs`
- [ ] **Step 2:** Create repository interface `I{EntityName}Repository`
- [ ] **Step 3:** Implement repository `{EntityName}Repository`
- [ ] **Step 4:** Register repository in `ServiceConfiguration.cs`
- [ ] **Step 5:** Create service `{EntityName}Service` (optional)
- [ ] **Step 6:** Register service in `ServiceConfiguration.cs`
- [ ] **Step 7:** Build project - verify no errors
- [ ] **Step 8:** Write unit tests (optional but recommended)

---

## Common Patterns

### Pagination

```csharp
public async Task<(IEnumerable<Announcement> items, int total)> GetPagedAsync(
    Guid organizationId, 
    int page, 
    int pageSize)
{
    return await base.GetPagedAsync(
        page, 
        pageSize, 
        orderBySql: "created_at DESC",
        whereSql: "organization_id = @OrgId",
        parameters: new { OrgId = organizationId });
}
```

### Search

```csharp
public async Task<IEnumerable<Announcement>> SearchAsync(Guid organizationId, string query)
{
    using var connection = _connectionFactory.CreateConnection();
    const string sql = @"
        SELECT * FROM announcements
        WHERE organization_id = @OrgId 
          AND is_deleted = false
          AND (title ILIKE @Query OR content ILIKE @Query)
        ORDER BY created_at DESC";

    return await connection.QueryAsync<Announcement>(sql, 
        new { OrgId = organizationId, Query = $"%{query}%" });
}
```

### Joins

```csharp
public async Task<IEnumerable<Announcement>> GetWithCreatorAsync(Guid organizationId)
{
    using var connection = _connectionFactory.CreateConnection();
    const string sql = @"
        SELECT a.*, u.display_name as CreatorName
        FROM announcements a
        INNER JOIN users u ON a.created_by_user_id = u.id
        WHERE a.organization_id = @OrgId AND a.is_deleted = false
        ORDER BY a.created_at DESC";

    return await connection.QueryAsync<Announcement>(sql, new { OrgId = organizationId });
}
```

---

## Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| Properties not mapping | Column name mismatch | Verify column names match (snake_case → PascalCase) |
| Repository not found | Not registered in DI | Add to `ServiceConfiguration.cs` |
| `null` returned unexpectedly | Soft delete filtering | Check `is_deleted = false` in query |
| `Object reference` error | Missing null check | Add `?` to nullable properties |

---

## Next Steps

**Next:** Read [07_TROUBLESHOOTING.md](07_TROUBLESHOOTING.md) for common issues and their solutions.
