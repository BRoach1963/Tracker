using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using Supabase.Postgrest;
using static Supabase.Postgrest.Constants;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Interface for feedback operations.
/// </summary>
public interface IFeedbackService
{
    /// <summary>
    /// Gets feedback received by the current user.
    /// </summary>
    Task<List<FeedbackDetail>> GetReceivedFeedbackAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets feedback given by the current user.
    /// </summary>
    Task<List<FeedbackDetail>> GetGivenFeedbackAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets feedback for a specific team member (received).
    /// </summary>
    Task<List<FeedbackDetail>> GetFeedbackForMemberAsync(Guid teamMemberId, CancellationToken ct = default);

    /// <summary>
    /// Gets a single feedback by ID.
    /// </summary>
    Task<FeedbackDetail?> GetByIdAsync(Guid feedbackId, CancellationToken ct = default);

    /// <summary>
    /// Creates new feedback.
    /// </summary>
    Task<FeedbackDetail?> CreateFeedbackAsync(
        Guid recipientId,
        string content,
        string feedbackType = "general",
        string? title = null,
        string visibility = "private",
        bool isAnonymous = false,
        int? rating = null,
        Guid? meetingId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Updates existing feedback.
    /// </summary>
    Task<FeedbackDetail?> UpdateFeedbackAsync(
        Guid feedbackId,
        string content,
        string feedbackType,
        string? title = null,
        string visibility = "private",
        int? rating = null,
        CancellationToken ct = default);

    /// <summary>
    /// Soft deletes feedback.
    /// </summary>
    Task<bool> DeleteFeedbackAsync(Guid feedbackId, CancellationToken ct = default);

    /// <summary>
    /// Gets the last error message.
    /// </summary>
    string? LastError { get; }
}

/// <summary>
/// Service for managing feedback CRUD operations.
/// </summary>
public class FeedbackService : IFeedbackService
{
    private static FeedbackService? _instance;
    private static readonly object _lock = new();

    public static FeedbackService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new FeedbackService();
                }
            }
            return _instance;
        }
    }

    public string? LastError { get; private set; }

    private static void Log(string message)
    {
        Debug.WriteLine($"[FeedbackService] {message}");
    }

    /// <inheritdoc />
    public async Task<List<FeedbackDetail>> GetReceivedFeedbackAsync(CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var teamMember = AuthService.Instance.CurrentTeamMember;

        if (client == null || teamMember == null)
        {
            LastError = "Not authenticated";
            return new List<FeedbackDetail>();
        }

        try
        {
            Log($"Loading received feedback for {teamMember.Id}");

            var result = await client.From<FeedbackDetail>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("to_member_id", Operator.Equals, teamMember.Id.ToString())
                .Order("created_at", Ordering.Descending)
                .Get();

            var feedback = result.Models ?? new List<FeedbackDetail>();
            Log($"Received feedback count: {feedback.Count}");
            return feedback;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetReceivedFeedback ERROR: {ex.Message}");
            return new List<FeedbackDetail>();
        }
    }

    /// <inheritdoc />
    public async Task<List<FeedbackDetail>> GetGivenFeedbackAsync(CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var teamMember = AuthService.Instance.CurrentTeamMember;

        if (client == null || teamMember == null)
        {
            LastError = "Not authenticated";
            return new List<FeedbackDetail>();
        }

        try
        {
            Log($"Loading given feedback by {teamMember.Id}");

            var result = await client.From<FeedbackDetail>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("from_member_id", Operator.Equals, teamMember.Id.ToString())
                .Order("created_at", Ordering.Descending)
                .Get();

            var feedback = result.Models ?? new List<FeedbackDetail>();
            Log($"Given feedback count: {feedback.Count}");
            return feedback;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetGivenFeedback ERROR: {ex.Message}");
            return new List<FeedbackDetail>();
        }
    }

    /// <inheritdoc />
    public async Task<List<FeedbackDetail>> GetFeedbackForMemberAsync(Guid teamMemberId, CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return new List<FeedbackDetail>();
        }

        try
        {
            Log($"Loading feedback for member {teamMemberId}");

            var result = await client.From<FeedbackDetail>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("to_member_id", Operator.Equals, teamMemberId.ToString())
                .Order("created_at", Ordering.Descending)
                .Get();

            var feedback = result.Models ?? new List<FeedbackDetail>();
            Log($"Feedback for member count: {feedback.Count}");
            return feedback;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetFeedbackForMember ERROR: {ex.Message}");
            return new List<FeedbackDetail>();
        }
    }

    /// <inheritdoc />
    public async Task<FeedbackDetail?> GetByIdAsync(Guid feedbackId, CancellationToken ct = default)
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
            Log($"Getting feedback by ID: {feedbackId}");

            var result = await client.From<FeedbackDetail>()
                .Filter("id", Operator.Equals, feedbackId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Single();

            return result;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetById ERROR: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<FeedbackDetail?> CreateFeedbackAsync(
        Guid recipientId,
        string content,
        string feedbackType = "general",
        string? title = null,
        string visibility = "private",
        bool isAnonymous = false,
        int? rating = null,
        Guid? meetingId = null,
        CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var teamMember = AuthService.Instance.CurrentTeamMember;

        if (client == null || teamMember == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            var feedback = new FeedbackDetail
            {
                Id = Guid.NewGuid(),
                OrganizationId = teamMember.OrganizationId,
                FromMemberId = teamMember.Id,
                TeamMemberId = recipientId,
                FeedbackType = feedbackType,
                Title = title,
                Content = content,
                Visibility = visibility,
                IsAnonymous = isAnonymous,
                Rating = rating,
                MeetingId = meetingId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            Log($"Creating feedback for {recipientId}");

            var result = await client.From<FeedbackDetail>().Insert(feedback);
            var created = result.Models?.FirstOrDefault();

            if (created != null)
            {
                Log($"Feedback created: {created.Id}");
            }

            return created;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"CreateFeedback ERROR: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<FeedbackDetail?> UpdateFeedbackAsync(
        Guid feedbackId,
        string content,
        string feedbackType,
        string? title = null,
        string visibility = "private",
        int? rating = null,
        CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var teamMember = AuthService.Instance.CurrentTeamMember;

        if (client == null || teamMember == null)
        {
            LastError = "Not authenticated";
            return null;
        }

        try
        {
            Log($"Updating feedback {feedbackId}");

            // Get existing feedback first
            var existing = await GetByIdAsync(feedbackId, ct);
            if (existing == null)
            {
                LastError = "Feedback not found";
                return null;
            }

            // Only the author can edit their own feedback
            if (existing.FromMemberId != teamMember.Id)
            {
                LastError = "You can only edit your own feedback";
                return null;
            }

            // Update fields
            existing.Content = content;
            existing.FeedbackType = feedbackType;
            existing.Title = title;
            existing.Visibility = visibility;
            existing.Rating = rating;
            existing.UpdatedAt = DateTime.UtcNow;

            var result = await client.From<FeedbackDetail>()
                .Filter("id", Operator.Equals, feedbackId.ToString())
                .Update(existing);

            var updated = result.Models?.FirstOrDefault();
            if (updated != null)
            {
                Log($"Feedback updated: {updated.Id}");
            }

            return updated;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdateFeedback ERROR: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteFeedbackAsync(Guid feedbackId, CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var teamMember = AuthService.Instance.CurrentTeamMember;

        if (client == null || teamMember == null)
        {
            LastError = "Not authenticated";
            return false;
        }

        try
        {
            Log($"Soft deleting feedback {feedbackId}");

            // Get existing feedback first
            var existing = await GetByIdAsync(feedbackId, ct);
            if (existing == null)
            {
                LastError = "Feedback not found";
                return false;
            }

            // Only the author can delete their own feedback
            if (existing.FromMemberId != teamMember.Id)
            {
                LastError = "You can only delete your own feedback";
                return false;
            }

            // Soft delete
            existing.IsDeleted = true;
            existing.DeletedAt = DateTime.UtcNow;
            existing.DeletedBy = teamMember.Id;
            existing.UpdatedAt = DateTime.UtcNow;

            await client.From<FeedbackDetail>()
                .Filter("id", Operator.Equals, feedbackId.ToString())
                .Update(existing);

            Log($"Feedback soft deleted: {feedbackId}");
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"DeleteFeedback ERROR: {ex.Message}");
            return false;
        }
    }
}
