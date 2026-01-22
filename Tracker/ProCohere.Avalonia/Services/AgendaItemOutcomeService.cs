using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using static Supabase.Postgrest.Constants;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for managing agenda item outcomes in Supabase.
/// Handles recording decisions, feedback, notes, and linking to created entities.
/// </summary>
public class AgendaItemOutcomeService
{
    #region Singleton

    private static readonly Lazy<AgendaItemOutcomeService> _instance =
        new(() => new AgendaItemOutcomeService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static AgendaItemOutcomeService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "outcome_service.log");

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

    /// <summary>
    /// Last error message from operations.
    /// </summary>
    public string? LastError { get; private set; }

    private AgendaItemOutcomeService() { }

    #region Query Operations

    /// <summary>
    /// Gets all outcomes for a specific agenda item.
    /// </summary>
    public async Task<List<AgendaItemOutcomeDetail>> GetOutcomesForAgendaItemAsync(Guid agendaItemId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<AgendaItemOutcomeDetail>();
        }

        try
        {
            Log($"Getting outcomes for agenda item: {agendaItemId}");
            var result = await client.From<AgendaItemOutcomeDetail>()
                .Filter("agenda_item_id", Operator.Equals, agendaItemId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Order("created_at", Ordering.Ascending)
                .Get();

            var outcomes = result.Models ?? new List<AgendaItemOutcomeDetail>();
            Log($"Found {outcomes.Count} outcomes");
            return outcomes;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetOutcomesForAgendaItem ERROR: {ex.Message}");
            return new List<AgendaItemOutcomeDetail>();
        }
    }

    /// <summary>
    /// Gets all outcomes of a specific type for an agenda item.
    /// </summary>
    public async Task<List<AgendaItemOutcomeDetail>> GetOutcomesByTypeAsync(Guid agendaItemId, string outcomeType)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<AgendaItemOutcomeDetail>();
        }

        try
        {
            Log($"Getting {outcomeType} outcomes for agenda item: {agendaItemId}");
            var result = await client.From<AgendaItemOutcomeDetail>()
                .Filter("agenda_item_id", Operator.Equals, agendaItemId.ToString())
                .Filter("outcome_type", Operator.Equals, outcomeType)
                .Filter("is_deleted", Operator.Equals, "false")
                .Order("created_at", Ordering.Ascending)
                .Get();

            var outcomes = result.Models ?? new List<AgendaItemOutcomeDetail>();
            Log($"Found {outcomes.Count} {outcomeType} outcomes");
            return outcomes;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetOutcomesByType ERROR: {ex.Message}");
            return new List<AgendaItemOutcomeDetail>();
        }
    }

    /// <summary>
    /// Gets all outcomes for a meeting (by fetching all agenda items first).
    /// </summary>
    public async Task<List<AgendaItemOutcomeDetail>> GetOutcomesForMeetingAsync(Guid meetingId)
    {
        LastError = null;
        var agendaItems = await MeetingAgendaItemService.Instance.GetAgendaItemsForMeetingAsync(meetingId);
        if (agendaItems.Count == 0)
            return new List<AgendaItemOutcomeDetail>();

        var allOutcomes = new List<AgendaItemOutcomeDetail>();
        foreach (var item in agendaItems)
        {
            var outcomes = await GetOutcomesForAgendaItemAsync(item.Id);
            allOutcomes.AddRange(outcomes);
        }

        return allOutcomes.OrderBy(o => o.CreatedAt).ToList();
    }

    /// <summary>
    /// Gets a single outcome by ID.
    /// </summary>
    public async Task<AgendaItemOutcomeDetail?> GetOutcomeAsync(Guid outcomeId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            Log($"Getting outcome: {outcomeId}");
            var result = await client.From<AgendaItemOutcomeDetail>()
                .Filter("id", Operator.Equals, outcomeId.ToString())
                .Single();

            return result;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetOutcome ERROR: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region Create Operations

    /// <summary>
    /// Records a decision outcome for an agenda item.
    /// </summary>
    public async Task<AgendaItemOutcomeDetail?> RecordDecisionAsync(
        Guid agendaItemId,
        string content,
        string visibility = "attendees")
    {
        return await CreateContentOutcomeAsync(
            agendaItemId, 
            OutcomeType.DecisionRecorded, 
            content, 
            visibility);
    }

    /// <summary>
    /// Captures feedback for an agenda item.
    /// </summary>
    public async Task<AgendaItemOutcomeDetail?> CaptureFeedbackAsync(
        Guid agendaItemId,
        string content,
        string visibility = "attendees")
    {
        return await CreateContentOutcomeAsync(
            agendaItemId, 
            OutcomeType.FeedbackCaptured, 
            content, 
            visibility);
    }

    /// <summary>
    /// Adds notes to an agenda item.
    /// </summary>
    public async Task<AgendaItemOutcomeDetail?> AddNotesAsync(
        Guid agendaItemId,
        string content,
        string visibility = "attendees")
    {
        return await CreateContentOutcomeAsync(
            agendaItemId, 
            OutcomeType.NotesAdded, 
            content, 
            visibility);
    }

    /// <summary>
    /// Records that a task was created from this agenda item.
    /// </summary>
    public async Task<AgendaItemOutcomeDetail?> RecordTaskCreatedAsync(
        Guid agendaItemId,
        Guid taskId)
    {
        return await CreateEntityOutcomeAsync(
            agendaItemId, 
            OutcomeType.TaskCreated, 
            "task", 
            taskId);
    }

    /// <summary>
    /// Records that a goal was created from this agenda item.
    /// </summary>
    public async Task<AgendaItemOutcomeDetail?> RecordGoalCreatedAsync(
        Guid agendaItemId,
        Guid goalId)
    {
        return await CreateEntityOutcomeAsync(
            agendaItemId, 
            OutcomeType.GoalCreated, 
            "goal", 
            goalId);
    }

    /// <summary>
    /// Records that a goal was updated based on agenda item discussion.
    /// </summary>
    public async Task<AgendaItemOutcomeDetail?> RecordGoalUpdatedAsync(
        Guid agendaItemId,
        Guid goalId,
        string? updateNotes = null)
    {
        return await CreateEntityOutcomeAsync(
            agendaItemId, 
            OutcomeType.GoalUpdated, 
            "goal", 
            goalId,
            updateNotes);
    }

    /// <summary>
    /// Records that a follow-up meeting was scheduled.
    /// </summary>
    public async Task<AgendaItemOutcomeDetail?> RecordFollowUpScheduledAsync(
        Guid agendaItemId,
        Guid meetingId)
    {
        return await CreateEntityOutcomeAsync(
            agendaItemId, 
            OutcomeType.FollowUpScheduled, 
            "meeting", 
            meetingId);
    }

    /// <summary>
    /// Creates a content-based outcome (decision, feedback, notes).
    /// </summary>
    private async Task<AgendaItemOutcomeDetail?> CreateContentOutcomeAsync(
        Guid agendaItemId,
        string outcomeType,
        string content,
        string visibility)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        var profile = AuthService.Instance.CurrentProfile;
        if (profile == null)
        {
            LastError = "Not authenticated - no profile";
            return null;
        }

        var orgId = profile.OrganizationId;
        if (!orgId.HasValue)
        {
            LastError = "No organization context";
            return null;
        }

        try
        {
            Log($"Creating {outcomeType} outcome for agenda item: {agendaItemId}");

            var outcome = new AgendaItemOutcomeDetail
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId.Value,
                AgendaItemId = agendaItemId,
                OutcomeType = outcomeType,
                Content = content,
                Visibility = visibility,
                CreatedBy = profile.Id,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await client.From<AgendaItemOutcomeDetail>()
                .Insert(outcome);

            var created = result.Models?.FirstOrDefault();
            if (created != null)
            {
                Log($"Created {outcomeType} outcome: {created.Id}");
            }
            return created;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"CreateContentOutcome ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Creates an entity-linking outcome (task, goal, meeting).
    /// </summary>
    private async Task<AgendaItemOutcomeDetail?> CreateEntityOutcomeAsync(
        Guid agendaItemId,
        string outcomeType,
        string linkedEntityType,
        Guid linkedEntityId,
        string? content = null)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        var profile = AuthService.Instance.CurrentProfile;
        if (profile == null)
        {
            LastError = "Not authenticated - no profile";
            return null;
        }

        var orgId = profile.OrganizationId;
        if (!orgId.HasValue)
        {
            LastError = "No organization context";
            return null;
        }

        try
        {
            Log($"Creating {outcomeType} outcome for agenda item: {agendaItemId}, linked to {linkedEntityType}:{linkedEntityId}");

            var outcome = new AgendaItemOutcomeDetail
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId.Value,
                AgendaItemId = agendaItemId,
                OutcomeType = outcomeType,
                LinkedEntityType = linkedEntityType,
                LinkedEntityId = linkedEntityId,
                Content = content,
                Visibility = OutcomeVisibility.Attendees,
                CreatedBy = profile.Id,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await client.From<AgendaItemOutcomeDetail>()
                .Insert(outcome);

            var created = result.Models?.FirstOrDefault();
            if (created != null)
            {
                Log($"Created {outcomeType} outcome: {created.Id}");
            }
            return created;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"CreateEntityOutcome ERROR: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region Update Operations

    /// <summary>
    /// Updates the content of an outcome.
    /// </summary>
    public async Task<bool> UpdateOutcomeContentAsync(Guid outcomeId, string newContent)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Updating outcome content: {outcomeId}");
            await client.From<AgendaItemOutcomeDetail>()
                .Filter("id", Operator.Equals, outcomeId.ToString())
                .Set(x => x.Content!, newContent)
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .Update();

            Log("Outcome content updated successfully");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdateOutcomeContent ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Updates the visibility of an outcome.
    /// </summary>
    public async Task<bool> UpdateOutcomeVisibilityAsync(Guid outcomeId, string visibility)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Updating outcome visibility: {outcomeId} -> {visibility}");
            await client.From<AgendaItemOutcomeDetail>()
                .Filter("id", Operator.Equals, outcomeId.ToString())
                .Set(x => x.Visibility, visibility)
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .Update();

            Log("Outcome visibility updated successfully");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdateOutcomeVisibility ERROR: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Delete Operations

    /// <summary>
    /// Soft-deletes an outcome.
    /// </summary>
    public async Task<bool> DeleteOutcomeAsync(Guid outcomeId)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Deleting outcome: {outcomeId}");
            await client.From<AgendaItemOutcomeDetail>()
                .Filter("id", Operator.Equals, outcomeId.ToString())
                .Set(x => x.IsDeleted, true)
                .Set(x => x.UpdatedAt, DateTime.UtcNow)
                .Update();

            Log("Outcome deleted successfully");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"DeleteOutcome ERROR: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Gets a summary of outcomes for an agenda item.
    /// </summary>
    public async Task<OutcomeSummary> GetOutcomeSummaryAsync(Guid agendaItemId)
    {
        var outcomes = await GetOutcomesForAgendaItemAsync(agendaItemId);
        return new OutcomeSummary
        {
            TotalCount = outcomes.Count,
            TasksCreated = outcomes.Count(o => o.OutcomeType == OutcomeType.TaskCreated),
            GoalsCreated = outcomes.Count(o => o.OutcomeType == OutcomeType.GoalCreated),
            GoalsUpdated = outcomes.Count(o => o.OutcomeType == OutcomeType.GoalUpdated),
            FollowUpsScheduled = outcomes.Count(o => o.OutcomeType == OutcomeType.FollowUpScheduled),
            DecisionsRecorded = outcomes.Count(o => o.OutcomeType == OutcomeType.DecisionRecorded),
            FeedbackCaptured = outcomes.Count(o => o.OutcomeType == OutcomeType.FeedbackCaptured),
            NotesAdded = outcomes.Count(o => o.OutcomeType == OutcomeType.NotesAdded)
        };
    }

    #endregion
}

/// <summary>
/// Summary of outcomes for an agenda item.
/// </summary>
public class OutcomeSummary
{
    public int TotalCount { get; set; }
    public int TasksCreated { get; set; }
    public int GoalsCreated { get; set; }
    public int GoalsUpdated { get; set; }
    public int FollowUpsScheduled { get; set; }
    public int DecisionsRecorded { get; set; }
    public int FeedbackCaptured { get; set; }
    public int NotesAdded { get; set; }

    public bool HasAnyOutcome => TotalCount > 0;
    public bool HasEntityOutcomes => TasksCreated + GoalsCreated + GoalsUpdated + FollowUpsScheduled > 0;
    public bool HasContentOutcomes => DecisionsRecorded + FeedbackCaptured + NotesAdded > 0;
}
