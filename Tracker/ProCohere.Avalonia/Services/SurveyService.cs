using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProCohere.Avalonia.Models;
using static Supabase.Postgrest.Constants;

namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for managing pulse surveys in Supabase.
/// Handles survey creation, distribution, and response collection.
/// </summary>
public class SurveyService
{
    #region Singleton

    private static readonly Lazy<SurveyService> _instance =
        new(() => new SurveyService(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static SurveyService Instance => _instance.Value;

    #endregion

    #region Logging

    private static readonly string _logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ProCohere", "survey_service.log");

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

    #region Properties

    public string? LastError { get; private set; }

    #endregion

    private SurveyService() { }

    #region Create/Update

    /// <summary>
    /// Creates a new survey with questions.
    /// </summary>
    public async Task<Survey?> CreateSurveyAsync(Survey survey, CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var teamMember = AuthService.Instance.CurrentTeamMember;

        if (client == null || teamMember == null)
        {
            LastError = "Not authenticated";
            Log("CreateSurvey failed: Not authenticated");
            return null;
        }

        try
        {
            Log($"Creating survey: {survey.Title}");

            // Set required fields
            survey.Id = Guid.NewGuid();
            survey.OrganizationId = teamMember.OrganizationId;
            survey.CreatedBy = teamMember.Id;
            survey.Status = "draft";
            survey.CreatedAt = DateTime.UtcNow;
            survey.UpdatedAt = DateTime.UtcNow;

            // Store questions separately
            var questions = survey.Questions.ToList();
            survey.Questions = new List<SurveyQuestion>(); // Clear for insert

            // Insert survey
            var result = await client.From<Survey>()
                .Insert(survey);

            var created = result.Models?.FirstOrDefault();

            if (created == null)
            {
                LastError = "Survey creation returned null";
                Log($"CreateSurvey ERROR: {LastError}");
                return null;
            }

            // Insert questions
            if (questions.Any())
            {
                foreach (var question in questions)
                {
                    question.Id = Guid.NewGuid();
                    question.SurveyId = created.Id;
                    question.OrganizationId = teamMember.OrganizationId;
                    question.CreatedAt = DateTime.UtcNow;
                    question.UpdatedAt = DateTime.UtcNow;
                }

                var questionResult = await client.From<SurveyQuestion>()
                    .Insert(questions);

                created.Questions = questionResult.Models?.ToList() ?? new List<SurveyQuestion>();
            }

            Log($"Survey created: {created.Id} with {created.Questions.Count} questions");
            return created;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"CreateSurvey ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Updates an existing survey.
    /// </summary>
    public async Task<Survey?> UpdateSurveyAsync(Survey survey, CancellationToken ct = default)
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
            Log($"Updating survey: {survey.Id}");

            survey.UpdatedAt = DateTime.UtcNow;
            survey.Questions = new List<SurveyQuestion>(); // Don't update questions through survey update

            var result = await client.From<Survey>()
                .Filter("id", Operator.Equals, survey.Id.ToString())
                .Update(survey);

            var updated = result.Models?.FirstOrDefault();
            Log($"Survey updated: {survey.Id}");
            return updated;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"UpdateSurvey ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Distributes a survey to target team members (creates response records and activates).
    /// </summary>
    public async Task<bool> DistributeSurveyAsync(Guid surveyId, CancellationToken ct = default)
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
            Log($"Distributing survey: {surveyId}");

            // Get survey with targeting info
            var surveyResult = await client.From<Survey>()
                .Filter("id", Operator.Equals, surveyId.ToString())
                .Single();

            if (surveyResult == null)
            {
                LastError = "Survey not found";
                return false;
            }

            // Get target team members
            var targetMembers = new List<Guid>();

            if (surveyResult.TargetAllEmployees)
            {
                // Get all active team members in organization
                var membersResult = await client.From<TeamMember>()
                    .Filter("organization_id", Operator.Equals, teamMember.OrganizationId.ToString())
                    .Filter("is_deleted", Operator.Equals, "false")
                    .Filter("is_active", Operator.Equals, "true")
                    .Get();

                targetMembers = membersResult.Models?.Select(m => m.Id).ToList() ?? new List<Guid>();
            }
            else if (surveyResult.TargetTeamMemberIds != null && surveyResult.TargetTeamMemberIds.Length > 0)
            {
                targetMembers = surveyResult.TargetTeamMemberIds.ToList();
            }

            if (!targetMembers.Any())
            {
                LastError = "No target team members found";
                Log("DistributeSurvey ERROR: No targets");
                return false;
            }

            // Create response records for each target
            var responses = targetMembers.Select(memberId => new SurveyResponse
            {
                Id = Guid.NewGuid(),
                OrganizationId = teamMember.OrganizationId,
                SurveyId = surveyId,
                RespondentId = surveyResult.IsAnonymous ? null : memberId,
                IsComplete = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            // Insert responses
            await client.From<SurveyResponse>()
                .Insert(responses);

            // Activate survey
            var success = await ActivateSurveyAsync(surveyId, ct);

            if (success)
            {
                Log($"Survey distributed to {targetMembers.Count} team members");
            }

            return success;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"DistributeSurvey ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Activates a survey (opens it for responses).
    /// </summary>
    public async Task<bool> ActivateSurveyAsync(Guid surveyId, CancellationToken ct = default)
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
            Log($"Activating survey: {surveyId}");

            var update = new Survey
            {
                Id = surveyId,
                Status = "active",
                UpdatedAt = DateTime.UtcNow
            };

            var result = await client.From<Survey>()
                .Filter("id", Operator.Equals, surveyId.ToString())
                .Update(update);

            Log($"Survey activated: {surveyId}");
            return result.Models?.Any() == true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"ActivateSurvey ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Closes a survey (stops accepting responses).
    /// </summary>
    public async Task<bool> CloseSurveyAsync(Guid surveyId, CancellationToken ct = default)
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
            Log($"Closing survey: {surveyId}");

            var update = new Survey
            {
                Id = surveyId,
                Status = "closed",
                UpdatedAt = DateTime.UtcNow
            };

            var result = await client.From<Survey>()
                .Filter("id", Operator.Equals, surveyId.ToString())
                .Update(update);

            Log($"Survey closed: {surveyId}");
            return result.Models?.Any() == true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"CloseSurvey ERROR: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Queries

    /// <summary>
    /// Gets all surveys for the current organization.
    /// </summary>
    public async Task<List<Survey>> GetOrganizationSurveysAsync(CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var teamMember = AuthService.Instance.CurrentTeamMember;

        if (client == null || teamMember == null)
        {
            LastError = "Not authenticated";
            return new List<Survey>();
        }

        try
        {
            Log("Loading organization surveys");

            var result = await client.From<Survey>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("organization_id", Operator.Equals, teamMember.OrganizationId.ToString())
                .Order("created_at", Ordering.Descending)
                .Get();

            var surveys = result.Models ?? new List<Survey>();
            Log($"Surveys loaded: {surveys.Count}");
            return surveys;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetOrganizationSurveys ERROR: {ex.Message}");
            return new List<Survey>();
        }
    }

    /// <summary>
    /// Gets a survey with its questions.
    /// </summary>
    public async Task<Survey?> GetSurveyWithQuestionsAsync(Guid surveyId, CancellationToken ct = default)
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
            Log($"Loading survey with questions: {surveyId}");

            // Get survey
            var surveyResult = await client.From<Survey>()
                .Filter("id", Operator.Equals, surveyId.ToString())
                .Single();

            if (surveyResult == null)
            {
                LastError = "Survey not found";
                return null;
            }

            // Get questions
            var questionsResult = await client.From<SurveyQuestion>()
                .Filter("survey_id", Operator.Equals, surveyId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Order("sort_order", Ordering.Ascending)
                .Get();

            surveyResult.Questions = questionsResult.Models?.ToList() ?? new List<SurveyQuestion>();
            Log($"Survey loaded with {surveyResult.Questions.Count} questions");
            return surveyResult;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetSurveyWithQuestions ERROR: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets active surveys for the current organization.
    /// </summary>
    public async Task<List<Survey>> GetActiveSurveysAsync(CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();
        var teamMember = AuthService.Instance.CurrentTeamMember;

        if (client == null || teamMember == null)
        {
            LastError = "Not authenticated";
            return new List<Survey>();
        }

        try
        {
            Log("Loading active surveys");

            var result = await client.From<Survey>()
                .Filter("is_deleted", Operator.Equals, "false")
                .Filter("organization_id", Operator.Equals, teamMember.OrganizationId.ToString())
                .Filter("status", Operator.Equals, "active")
                .Order("created_at", Ordering.Descending)
                .Get();

            var surveys = result.Models ?? new List<Survey>();
            Log($"Active surveys loaded: {surveys.Count}");
            return surveys;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetActiveSurveys ERROR: {ex.Message}");
            return new List<Survey>();
        }
    }

    /// <summary>
    /// Gets response statistics for a survey.
    /// </summary>
    public async Task<(int TotalResponses, int CompletedResponses)> GetSurveyStatsAsync(Guid surveyId, CancellationToken ct = default)
    {
        LastError = null;
        var client = AuthService.Instance.GetProCohereClient();

        if (client == null)
        {
            LastError = "Not authenticated";
            return (0, 0);
        }

        try
        {
            var result = await client.From<SurveyResponse>()
                .Filter("survey_id", Operator.Equals, surveyId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Get();

            var responses = result.Models ?? new List<SurveyResponse>();
            var total = responses.Count;
            var completed = responses.Count(r => r.IsComplete);

            return (total, completed);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetSurveyStats ERROR: {ex.Message}");
            return (0, 0);
        }
    }

    #endregion

    #region Soft Delete

    /// <summary>
    /// Soft deletes a survey.
    /// </summary>
    public async Task<bool> DeleteSurveyAsync(Guid surveyId, CancellationToken ct = default)
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
            Log($"Soft deleting survey: {surveyId}");

            var update = new Survey
            {
                Id = surveyId,
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow,
                DeletedBy = teamMember.Id,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await client.From<Survey>()
                .Filter("id", Operator.Equals, surveyId.ToString())
                .Update(update);

            Log($"Survey deleted: {surveyId}");
            return result.Models?.Any() == true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"DeleteSurvey ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets comprehensive analytics data for a survey.
    /// Includes survey, questions, responses, and answers.
    /// </summary>
    public async Task<SurveyAnalyticsData?> GetSurveyAnalyticsAsync(Guid surveyId, CancellationToken ct = default)
    {
        var client = AuthService.Instance.GetProCohereClient();
        if (client == null)
        {
            LastError = "Not authenticated";
            Log("GetSurveyAnalytics ERROR: No client");
            return null;
        }

        var teamMember = AuthService.Instance.CurrentTeamMember;
        if (teamMember == null)
        {
            LastError = "No team member context";
            Log("GetSurveyAnalytics ERROR: No team member");
            return null;
        }

        try
        {
            Log($"Getting survey analytics: {surveyId}");

            // Get survey
            var surveyResult = await client.From<Survey>()
                .Filter("id", Operator.Equals, surveyId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Single();

            if (surveyResult == null)
            {
                LastError = "Survey not found";
                Log($"Survey not found: {surveyId}");
                return null;
            }

            // Get all questions for this survey
            var questionsResult = await client.From<SurveyQuestion>()
                .Filter("survey_id", Operator.Equals, surveyId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Order("sort_order", Ordering.Ascending)
                .Get();

            var questions = questionsResult.Models ?? new List<SurveyQuestion>();

            // Get all responses for this survey
            var responsesResult = await client.From<SurveyResponse>()
                .Filter("survey_id", Operator.Equals, surveyId.ToString())
                .Filter("is_deleted", Operator.Equals, "false")
                .Get();

            var responses = responsesResult.Models ?? new List<SurveyResponse>();

            // Get all answers for this survey's responses
            var responseIds = responses.Select(r => r.Id.ToString()).ToList();
            var answers = new List<SurveyAnswer>();

            if (responseIds.Any())
            {
                // Supabase Postgrest IN filter
                var answersResult = await client.From<SurveyAnswer>()
                    .Filter("response_id", Operator.In, $"({string.Join(",", responseIds)})")
                    .Filter("is_deleted", Operator.Equals, "false")
                    .Get();

                answers = answersResult.Models ?? new List<SurveyAnswer>();
            }

            // Calculate stats
            var totalResponses = responses.Count;
            var completedResponses = responses.Count(r => r.IsComplete);

            var analyticsData = new SurveyAnalyticsData
            {
                Survey = surveyResult,
                Questions = questions,
                Responses = responses,
                Answers = answers,
                TotalResponses = totalResponses,
                CompletedResponses = completedResponses
            };

            Log($"Survey analytics loaded: {questions.Count} questions, {totalResponses} responses, {answers.Count} answers");
            return analyticsData;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            Log($"GetSurveyAnalytics ERROR: {ex.Message}");
            return null;
        }
    }

    #endregion
}
