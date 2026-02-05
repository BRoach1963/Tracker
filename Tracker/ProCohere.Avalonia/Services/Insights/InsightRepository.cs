using System;
using System.Collections.Generic;
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

    public async Task<Guid> CreateInsightAsync(Insight insight)
    {
        try
        {
            var dto = MapToDto(insight);
            dto.Id = Guid.NewGuid();
            dto.CreatedAt = DateTime.UtcNow;
            dto.UpdatedAt = DateTime.UtcNow;
            
            var response = await Client
                .From<InsightDto>()
                .Insert(dto);

            var created = response.Models.FirstOrDefault();
            if (created == null)
            {
                throw new InvalidOperationException("Failed to create insight");
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

    public async Task SnoozeInsightAsync(Guid id, DateTime until)
    {
        // Snooze not supported in existing schema - just dismiss for now
        await DismissInsightAsync(id, Guid.Empty);
    }

    public async Task MarkInsightActionedAsync(Guid id)
    {
        // ActedOn not in schema - just dismiss
        await DismissInsightAsync(id, Guid.Empty);
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
            TeamMemberId = dto.TeamMemberId,
            Type = Enum.Parse<InsightType>(dto.InsightType, true),
            Title = dto.Title,
            Content = dto.Content,
            EntityType = dto.SourceType,
            EntityId = dto.SourceId,
            RelevanceScore = dto.RelevanceScore,
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
            TeamMemberId = model.TeamMemberId,
            InsightType = model.Type.ToString().ToLowerInvariant(),
            Title = model.Title,
            Content = model.Content,
            SourceType = model.EntityType,
            SourceId = model.EntityId,
            RelevanceScore = model.RelevanceScore,
            IsDismissed = model.IsDismissed,
            DismissedAt = model.DismissedAt,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            IsDeleted = model.IsDeleted,
            DeletedAt = model.DeletedAt,
            DeletedBy = model.DeletedBy
        };
    }
}
