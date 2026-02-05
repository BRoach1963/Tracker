using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;

namespace ProCohere.Avalonia.Interfaces.AI;

/// <summary>
/// AI-facing interface for project data operations.
/// Provides simplified, AI-friendly methods for project management.
/// </summary>
public interface IProjectDataService
{
    /// <summary>
    /// Creates a new project with the specified details.
    /// </summary>
    /// <param name="name">Project name</param>
    /// <param name="description">Project description</param>
    /// <param name="startDate">Project start date</param>
    /// <param name="endDate">Project target end date</param>
    /// <returns>Created project details or error message</returns>
    Task<string> CreateProjectAsync(string name, string? description = null, string? startDate = null, string? endDate = null);

    /// <summary>
    /// Gets projects with optional filtering.
    /// </summary>
    /// <param name="query">Search query for project name</param>
    /// <param name="status">Filter by status (active, completed, all)</param>
    /// <returns>List of matching projects</returns>
    Task<List<Project>> GetProjectsAsync(string? query = null, string status = "active");

    /// <summary>
    /// Updates an existing project.
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <param name="name">New name (optional)</param>
    /// <param name="description">New description (optional)</param>
    /// <param name="status">New status (optional)</param>
    /// <returns>Success message or error</returns>
    Task<string> UpdateProjectAsync(Guid projectId, string? name = null, string? description = null, string? status = null);

    /// <summary>
    /// Archives a project.
    /// </summary>
    /// <param name="projectId">Project ID</param>
    /// <returns>Success message or error</returns>
    Task<string> ArchiveProjectAsync(Guid projectId);
}