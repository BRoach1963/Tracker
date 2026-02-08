using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Models.Dtos;
using static Supabase.Postgrest.Constants;

namespace ProCohere.Avalonia.Services.Insights;

/// <summary>
/// Repository for insight data operations using Supabase.
/// Maps to existing ai_insights table schema (procohere.ai_insights).
/// </summary>
public class InsightRepository : IInsightRepository
{
    private Supabase.Client Client => AuthService.Instance.GetProCohereClient()!;

    #region Logging
    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "insight_engine.log");

    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        Debug.WriteLine(line);
        try
        {
            var dir = Path.GetDirectoryName(_logPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }
        catch { }
    }
    #endregion

    public async Task<List<Insight>> GetActiveInsightsAsync(Guid teamMemberId)
    {
        try
        {
            var response = await Client
                .From<InsightDto>()
                .Where(x => x.GeneratedFor == teamMemberId)
                .Where(x => x.IsDeleted == false)
                .Where(x => x.IsDismissed == false)
                .Order("created_at", Ordering.Descending)
                .Get();

            return response.Models.Select(MapToModel).ToList();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Insight?> GetInsightByIdAsync(Guid id)
    {
        try
        {
            var response = await Client
                .From<InsightDto>()
                .Where(x => x.Id == id)
                .Single();

            return response == null ? null : MapToModel(response);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Obsolete("Use IInsightRpcService.CreateInsightAsync instead. Direct INSERT blocked by RLS.")]
    public async Task<Guid> CreateInsightAsync(Insight insight)
    {
        try
        {
            var dto = MapToDto(insight);
            dto.Id = Guid.NewGuid();
            dto.CreatedAt = DateTime.UtcNow;
            dto.UpdatedAt = DateTime.UtcNow;
            dto.GeneratedAt = DateTime.UtcNow;
            
            // Debug logging
            Log($"[InsightRepository] LEGACY INSERT (will fail with RLS): org={dto.OrganizationId}, generatedFor={dto.GeneratedFor}, type={dto.InsightType}");
            
            var response = await Client
                .From<InsightDto>()
                .Insert(dto);

            var created = response.Models.FirstOrDefault();
            if (created == null)
            {
                throw new InvalidOperationException("Failed to create insight - use IInsightRpcService instead");
            }

            return created.Id;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task UpdateInsightAsync(Insight insight)
    {
        try
        {
            var dto = MapToDto(insight);
            dto.UpdatedAt = DateTime.UtcNow;
            
            await Client
                .From<InsightDto>()
                .Where(x => x.Id == insight.Id)
                .Update(dto);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Obsolete("Use IInsightActionRepository.DismissAsync instead")]
    public async Task DismissInsightAsync(Guid id, Guid userId)
    {
        try
        {
            var update = new InsightDto
            {
                Id = id,
                IsDismissed = true,
                DismissedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            await Client
                .From<InsightDto>()
                .Where(x => x.Id == id)
                .Update(update);
        }
        catch (Exception)
        {
            throw;
        }
    }

    [Obsolete("Use IInsightActionRepository.SnoozeAsync instead")]
    public async Task SnoozeInsightAsync(Guid id, DateTime until)
    {
        // Snooze not supported in existing schema - just dismiss for now
#pragma warning disable CS0618
        await DismissInsightAsync(id, Guid.Empty);
#pragma warning restore CS0618
    }

    [Obsolete("Use IInsightActionRepository.MarkActedAsync instead")]
    public async Task MarkInsightActionedAsync(Guid id)
    {
        // ActedOn not in schema - just dismiss
#pragma warning disable CS0618
        await DismissInsightAsync(id, Guid.Empty);
#pragma warning restore CS0618
    }

    public async Task<int> GetActiveCountAsync(Guid teamMemberId)
    {
        try
        {
            var response = await Client
                .From<InsightDto>()
                .Where(x => x.GeneratedFor == teamMemberId)
                .Where(x => x.IsDeleted == false)
                .Where(x => x.IsDismissed == false)
                .Get();

            return response.Models.Count;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task DeleteInsightAsync(Guid id)
    {
        try
        {
            var update = new InsightDto
            {
                Id = id,
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            await Client
                .From<InsightDto>()
                .Where(x => x.Id == id)
                .Update(update);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<Insight>> GetInsightsByTypeAsync(Guid teamMemberId, InsightType type)
    {
        try
        {
            var typeString = type.ToString().ToLowerInvariant();
            
            var response = await Client
                .From<InsightDto>()
                .Where(x => x.GeneratedFor == teamMemberId)
                .Where(x => x.InsightType == typeString)
                .Where(x => x.IsDeleted == false)
                .Order("created_at", Ordering.Descending)
                .Get();

            return response.Models.Select(MapToModel).ToList();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Insight?> GetInsightBySignatureAsync(Guid organizationId, string signatureHash)
    {
        try
        {
            var response = await Client
                .From<InsightDto>()
                .Where(x => x.OrganizationId == organizationId)
                .Where(x => x.SignatureHash == signatureHash)
                .Where(x => x.IsDeleted == false)
                .Order("generated_at", Ordering.Descending)
                .Limit(1)
                .Get();

            var dto = response.Models.FirstOrDefault();
            return dto == null ? null : MapToModel(dto);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<bool> SignatureExistsAsync(Guid organizationId, Guid teamMemberId, string signatureHash)
    {
        try
        {
            var response = await Client
                .From<InsightDto>()
                .Where(x => x.OrganizationId == organizationId)
                .Where(x => x.GeneratedFor == teamMemberId)
                .Where(x => x.SignatureHash == signatureHash)
                .Where(x => x.IsDeleted == false)
                .Limit(1)
                .Get();

            return response.Models.Count > 0;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<Insight>> GetTopInsightsAsync(Guid teamMemberId, int count, int minSeverity = 4)
    {
        try
        {
            var response = await Client
                .From<InsightDto>()
                .Where(x => x.GeneratedFor == teamMemberId)
                .Where(x => x.IsDeleted == false)
                .Where(x => x.IsDismissed == false)
                .Where(x => x.Severity >= minSeverity)
                .Order("severity", Ordering.Descending)
                .Order("generated_at", Ordering.Descending)
                .Limit(count)
                .Get();

            return response.Models.Select(MapToModel).ToList();
        }
        catch (Exception)
        {
            throw;
        }
    }

    #region Legacy Methods

    [Obsolete("Use SignatureExistsAsync instead")]
    public async Task<bool> InsightExistsAsync(Guid organizationId, Guid teamMemberId, InsightType type, Guid? entityId)
    {
        try
        {
            var typeString = type.ToString().ToLowerInvariant();
            
            var query = Client
                .From<InsightDto>()
                .Where(x => x.OrganizationId == organizationId)
                .Where(x => x.GeneratedFor == teamMemberId)
                .Where(x => x.InsightType == typeString)
                .Where(x => x.IsDeleted == false);

            if (entityId.HasValue)
            {
                query = query.Where(x => x.SourceId == entityId.Value);
            }

            var response = await query.Get();
            return response.Models.Count > 0;
        }
        catch (Exception)
        {
            throw;
        }
    }

    #endregion

    /// <summary>
    /// Maps DTO to domain model.
    /// </summary>
    private static Insight MapToModel(InsightDto dto)
    {
        return new Insight
        {
            Id = dto.Id,
            OrganizationId = dto.OrganizationId,
            GeneratedFor = dto.GeneratedFor,
#pragma warning disable CS0618 // Type or member is obsolete
            TeamMemberId = dto.TeamMemberId,
#pragma warning restore CS0618
            SubjectType = dto.SubjectType,
            SubjectId = dto.SubjectId,
            Type = ParseInsightType(dto.InsightType),
            Title = dto.Title,
            Content = dto.Content,
            RuleKey = dto.RuleKey,
            SignatureHash = dto.SignatureHash,
            SourceType = dto.SourceType,
            SourceId = dto.SourceId,
            SeverityLevel = dto.Severity,
            RelevanceScore = dto.RelevanceScore,
            GeneratedAt = dto.GeneratedAt,
            ExpiresAt = dto.ExpiresAt,
            IsDismissed = dto.IsDismissed,
            DismissedAt = dto.DismissedAt,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            IsDeleted = dto.IsDeleted,
            DeletedAt = dto.DeletedAt,
            DeletedBy = dto.DeletedBy
        };
    }

    /// <summary>
    /// Maps domain model to DTO.
    /// </summary>
    private static InsightDto MapToDto(Insight model)
    {
        return new InsightDto
        {
            Id = model.Id,
            OrganizationId = model.OrganizationId,
            GeneratedFor = model.GeneratedFor,
#pragma warning disable CS0618 // Type or member is obsolete
            TeamMemberId = model.TeamMemberId,
#pragma warning restore CS0618
            SubjectType = model.SubjectType,
            SubjectId = model.SubjectId,
            InsightType = model.Type.ToString().ToLowerInvariant(),
            Title = model.Title,
            Content = model.Content,
            RuleKey = model.RuleKey,
            SignatureHash = model.SignatureHash,
            SourceType = model.SourceType,
            SourceId = model.SourceId,
            Severity = model.SeverityLevel,
            RelevanceScore = model.RelevanceScore,
            GeneratedAt = model.GeneratedAt,
            ExpiresAt = model.ExpiresAt,
            IsDismissed = model.IsDismissed,
            DismissedAt = model.DismissedAt,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            IsDeleted = model.IsDeleted,
            DeletedAt = model.DeletedAt,
            DeletedBy = model.DeletedBy
        };
    }
    
    /// <summary>
    /// Parses insight type from snake_case database value to enum.
    /// </summary>
    private static InsightType ParseInsightType(string dbValue)
    {
        if (string.IsNullOrEmpty(dbValue))
            return InsightType.TaskOverdue; // Default fallback
            
        // Convert snake_case to PascalCase: "metric_declining" -> "MetricDeclining"
        var parts = dbValue.Split('_');
        var pascalCase = string.Concat(parts.Select(p => 
            string.IsNullOrEmpty(p) ? p : char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant()));
        
        if (Enum.TryParse<InsightType>(pascalCase, true, out var result))
            return result;
            
        // Log warning and return default
        System.Diagnostics.Debug.WriteLine($"[InsightRepository] Unknown insight type: {dbValue}");
        return InsightType.TaskOverdue;
    }
}
