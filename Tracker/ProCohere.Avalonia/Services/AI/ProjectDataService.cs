using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Interfaces.AI;
using ProCohere.Avalonia.Models;
using ProCohere.Avalonia.Services;

namespace ProCohere.Avalonia.Services.AI;

/// <summary>
/// AI data service implementation for project operations.
/// Wraps ProjectService with AI-friendly interface.
/// </summary>
public class ProjectDataService : IProjectDataService
{
    private readonly ProjectService _projectService;

    public ProjectDataService()
    {
        _projectService = ProjectService.Instance;
    }

    public async Task<string> CreateProjectAsync(string name, string? description = null, string? startDate = null, string? endDate = null)
    {
        try
        {
            // Parse end date if provided (no StartDate in ProCohere Project model)
            DateTime? parsedDueDate = null;
            if (!string.IsNullOrEmpty(endDate))
            {
                if (!DateTime.TryParse(endDate, out var date))
                {
                    return $"Invalid end date format '{endDate}'. Please use a standard date format like YYYY-MM-DD";
                }
                parsedDueDate = date;
            }

            // Create project using service method
            var project = await _projectService.CreateProjectAsync(
                name: name,
                description: description,
                status: ProjectStatus.Active,
                dueDate: parsedDueDate
            );
            
            if (project != null)
            {
                var dateText = parsedDueDate.HasValue 
                    ? $" (due: {parsedDueDate:MM/dd/yyyy})" 
                    : "";
                
                return $"✅ Created project '{name}'{dateText}";
            }
            else
            {
                return $"❌ Failed to create project: {_projectService.LastError ?? "Unknown error"}";
            }
        }
        catch (Exception ex)
        {
            return $"❌ Error creating project: {ex.Message}";
        }
    }

    public async Task<List<Project>> GetProjectsAsync(string? query = null, string status = "active")
    {
        try
        {
            var projects = await _projectService.GetAllProjectsAsync();
            
            if (projects == null)
                return new List<Project>();

            // Apply filters
            var filtered = projects.AsEnumerable();

            if (status.ToLower() != "all")
            {
                var statusFilter = status.ToLower() switch
                {
                    "completed" => ProjectStatus.Completed,
                    "paused" => ProjectStatus.Paused,
                    _ => ProjectStatus.Active
                };
                
                filtered = filtered.Where(p => string.Equals(p.Status, statusFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(query))
            {
                var searchTerm = query.ToLower();
                filtered = filtered.Where(p =>
                    (p.Name?.ToLower().Contains(searchTerm) ?? false) ||
                    (p.Description?.ToLower().Contains(searchTerm) ?? false)
                );
            }

            return filtered.ToList();
        }
        catch (Exception)
        {
            return new List<Project>();
        }
    }

    public async Task<string> UpdateProjectAsync(Guid projectId, string? name = null, string? description = null, string? status = null)
    {
        try
        {
            // Build status for update
            string? statusValue = null;
            if (!string.IsNullOrEmpty(status))
            {
                if (!IsValidStatus(status))
                {
                    return $"Invalid status '{status}'. Valid options are: active, paused, completed";
                }
                
                statusValue = status.ToLower() switch
                {
                    "completed" => ProjectStatus.Completed,
                    "paused" => ProjectStatus.Paused,
                    _ => ProjectStatus.Active
                };
            }

            var updated = await _projectService.UpdateProjectAsync(
                projectId: projectId,
                name: name,
                description: description,
                status: statusValue
            );
            
            if (updated != null)
            {
                return "✅ Project updated successfully";
            }
            else
            {
                return $"❌ Failed to update project: {_projectService.LastError ?? "Unknown error"}";
            }
        }
        catch (Exception ex)
        {
            return $"❌ Error updating project: {ex.Message}";
        }
    }

    public async Task<string> ArchiveProjectAsync(Guid projectId)
    {
        try
        {
            // ProCohere uses soft delete instead of archive
            var success = await _projectService.DeleteProjectAsync(projectId);
            
            if (success)
            {
                return "✅ Project archived successfully";
            }
            else
            {
                return $"❌ Failed to archive project: {_projectService.LastError ?? "Unknown error"}";
            }
        }
        catch (Exception ex)
        {
            return $"❌ Error archiving project: {ex.Message}";
        }
    }

    private static bool IsValidStatus(string status)
    {
        var validStatuses = new[] { "active", "paused", "completed" };
        return validStatuses.Any(s => string.Equals(s, status, StringComparison.OrdinalIgnoreCase));
    }
}