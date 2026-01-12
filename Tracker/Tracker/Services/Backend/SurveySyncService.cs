using System.Security.Cryptography;
using System.Text;
using Supabase;
using Tracker.Classes;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.Database.Repositories;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Services;
using Tracker.Services.Backend.Models;

namespace Tracker.Services.Backend
{
    /// <summary>
    /// Service for syncing survey data between Tracker and Supabase.
    /// Handles uploading surveys, generating tokens, and pulling responses.
    /// </summary>
    public class SurveySyncService
    {
        #region Singleton

        private static readonly Lazy<SurveySyncService> _instance =
            new(() => new SurveySyncService(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static SurveySyncService Instance => _instance.Value;

        #endregion

        #region Fields

        private readonly ILogger _logger;
        private Supabase.Client? _client;
        private bool _isInitialized;

        #endregion

        #region Constants

        /// <summary>
        /// Base URL for external survey form.
        /// </summary>
        public const string ExternalSurveyBaseUrl = "https://polished-wood-b404.brian-6df.workers.dev";

        /// <summary>
        /// Default token expiry days.
        /// </summary>
        public const int DefaultTokenExpiryDays = 30;

        #endregion

        #region Constructor

        private SurveySyncService()
        {
            _logger = LoggingManager.GetComponentLogger("SurveySync");
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the Supabase client for survey sync.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                _logger.Info("Initializing Survey Sync Service...");

                var options = new SupabaseOptions
                {
                    AutoRefreshToken = true,
                    AutoConnectRealtime = false
                };

                _client = new Supabase.Client(
                    SupabaseConfig.ProjectUrl,
                    SupabaseConfig.AnonKey,
                    options);

                await _client.InitializeAsync();

                _isInitialized = true;
                _logger.Info("Survey Sync Service initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to initialize Survey Sync Service");
                throw;
            }
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Survey Sync Service not initialized. Call InitializeAsync first.");
            }
        }

        #endregion

        #region Survey Upload

        /// <summary>
        /// Uploads a local PulseSurvey to Supabase.
        /// </summary>
        public async Task<(bool Success, string? Error, string? SupabaseSurveyId)> UploadSurveyAsync(PulseSurvey survey)
        {
            EnsureInitialized();

            try
            {
                var userId = SupabaseService.Instance.CurrentUser?.Id;
                if (string.IsNullOrEmpty(userId))
                {
                    return (false, "Not signed in. Please sign in to upload surveys.", null);
                }

                _logger.Info("Uploading survey: {0}", survey.Title);

                // Create the survey in Supabase
                var supabaseSurvey = new SupabaseSurvey
                {
                    Id = Guid.NewGuid().ToString(),
                    TrackerId = survey.Id,
                    OwnerId = userId,
                    Title = survey.Title,
                    Description = survey.Description,
                    IsAnonymous = survey.IsAnonymous,
                    Status = survey.Status.ToString().ToLower(),
                    DueDate = survey.DueDate,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _client!.From<SupabaseSurvey>().Insert(supabaseSurvey);

                // Upload questions
                if (survey.Questions?.Any() == true)
                {
                    var supabaseQuestions = survey.Questions.Select(q => new SupabaseSurveyQuestion
                    {
                        Id = Guid.NewGuid().ToString(),
                        SurveyId = supabaseSurvey.Id,
                        TrackerId = q.Id,
                        QuestionText = q.Text,
                        QuestionType = MapQuestionType(q.QuestionType),
                        IsRequired = q.IsRequired,
                        SortOrder = q.SortOrder,
                        CreatedAt = DateTime.UtcNow
                    }).ToList();

                    await _client.From<SupabaseSurveyQuestion>().Insert(supabaseQuestions);
                }

                _logger.Info("Survey uploaded successfully: {0}", supabaseSurvey.Id);
                return (true, null, supabaseSurvey.Id);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to upload survey");
                return (false, $"Failed to upload survey: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Updates an existing survey in Supabase.
        /// </summary>
        public async Task<(bool Success, string? Error)> UpdateSurveyStatusAsync(string supabaseSurveyId, SurveyStatus status)
        {
            EnsureInitialized();

            try
            {
                _logger.Info("Updating survey status: {0} -> {1}", supabaseSurveyId, status);

                await _client!.From<SupabaseSurvey>()
                    .Where(s => s.Id == supabaseSurveyId)
                    .Set(s => s.Status!, status.ToString().ToLower())
                    .Set(s => s.UpdatedAt!, DateTime.UtcNow)
                    .Update();

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to update survey status");
                return (false, ex.Message);
            }
        }

        #endregion

        #region Token Generation

        /// <summary>
        /// Generates survey tokens for team members.
        /// Returns a list of (TeamMember, Token, URL) tuples.
        /// </summary>
        public async Task<(bool Success, string? Error, List<SurveyTokenInfo>? Tokens)> GenerateTokensAsync(
            string supabaseSurveyId,
            IEnumerable<TeamMember> teamMembers,
            int? expiryDays = null)
        {
            EnsureInitialized();

            try
            {
                _logger.Info("Generating tokens for survey: {0}", supabaseSurveyId);

                var tokens = new List<SurveyTokenInfo>();
                var expiryDate = DateTime.UtcNow.AddDays(expiryDays ?? DefaultTokenExpiryDays);

                foreach (var member in teamMembers)
                {
                    var tokenString = GenerateSecureToken();
                    var supabaseToken = new SupabaseSurveyToken
                    {
                        Id = Guid.NewGuid().ToString(),
                        SurveyId = supabaseSurveyId,
                        Token = tokenString,
                        TeamMemberName = member.FullName,
                        TeamMemberId = member.Id,
                        ExpiresAt = expiryDate,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _client!.From<SupabaseSurveyToken>().Insert(supabaseToken);

                    tokens.Add(new SurveyTokenInfo
                    {
                        TeamMember = member,
                        Token = tokenString,
                        Url = $"{ExternalSurveyBaseUrl}?token={tokenString}",
                        ExpiresAt = expiryDate
                    });
                }

                _logger.Info("Generated {0} tokens", tokens.Count);
                return (true, null, tokens);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to generate tokens");
                return (false, ex.Message, null);
            }
        }

        /// <summary>
        /// Generates a single token for a team member.
        /// </summary>
        public async Task<(bool Success, string? Error, SurveyTokenInfo? Token)> GenerateTokenAsync(
            string supabaseSurveyId,
            TeamMember teamMember,
            int? expiryDays = null)
        {
            var result = await GenerateTokensAsync(supabaseSurveyId, new[] { teamMember }, expiryDays);
            return (result.Success, result.Error, result.Tokens?.FirstOrDefault());
        }

        /// <summary>
        /// Gets existing tokens for a survey.
        /// </summary>
        public async Task<(bool Success, string? Error, List<SupabaseSurveyToken>? Tokens)> GetTokensForSurveyAsync(string supabaseSurveyId)
        {
            EnsureInitialized();

            try
            {
                var result = await _client!.From<SupabaseSurveyToken>()
                    .Where(t => t.SurveyId == supabaseSurveyId)
                    .Get();

                return (true, null, result.Models);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to get tokens");
                return (false, ex.Message, null);
            }
        }

        private static string GenerateSecureToken()
        {
            // Generate a cryptographically secure random token
            var bytes = new byte[24];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        #endregion

        #region Response Sync

        /// <summary>
        /// Syncs responses from Supabase to local database.
        /// </summary>
        public async Task<(bool Success, string? Error, int SyncedCount)> SyncResponsesAsync(string supabaseSurveyId, int localSurveyId)
        {
            EnsureInitialized();

            try
            {
                _logger.Info("Syncing responses for survey: {0}", supabaseSurveyId);

                // Get unsynced responses
                var responsesResult = await _client!.From<SupabaseSurveyResponse>()
                    .Where(r => r.SurveyId == supabaseSurveyId)
                    .Where(r => r.SyncedToTracker == false)
                    .Get();

                if (!responsesResult.Models.Any())
                {
                    _logger.Info("No new responses to sync");
                    return (true, null, 0);
                }

                var syncedCount = 0;

                foreach (var response in responsesResult.Models)
                {
                    try
                    {
                        // Get answers for this response
                        var answersResult = await _client.From<SupabaseSurveyAnswer>()
                            .Where(a => a.ResponseId == response.Id)
                            .Get();

                        // Get the questions to map IDs
                        var questionsResult = await _client.From<SupabaseSurveyQuestion>()
                            .Where(q => q.SurveyId == supabaseSurveyId)
                            .Get();

                        var questionMap = questionsResult.Models
                            .Where(q => q.TrackerId.HasValue)
                            .ToDictionary(q => q.Id, q => q.TrackerId!.Value);

                        // Create local response
                        var localResponse = new PulseSurveyResponse
                        {
                            PulseSurveyId = localSurveyId,
                            TeamMemberId = null, // Set below if we can match
                            SubmittedAt = response.SubmittedAt,
                            Answers = new List<PulseSurveyAnswer>()
                        };

                        // Try to find team member by name using repository
                        if (!string.IsNullOrEmpty(response.RespondentName))
                        {
                            var teamMemberRepository = CreateTeamMemberRepository();
                            if (teamMemberRepository != null)
                            {
                                var teamMember = await teamMemberRepository.FindTeamMemberByNameAsync(response.RespondentName);
                                if (teamMember != null)
                                {
                                    localResponse.TeamMemberId = teamMember.Id;
                                }
                            }
                        }

                        // Map answers
                        foreach (var answer in answersResult.Models)
                        {
                            if (!questionMap.TryGetValue(answer.QuestionId, out var localQuestionId))
                                continue;

                            var localAnswer = new PulseSurveyAnswer
                            {
                                PulseSurveyQuestionId = localQuestionId
                            };

                            // Map answer values
                            if (answer.AnswerRating.HasValue)
                                localAnswer.RatingValue = answer.AnswerRating.Value;
                            if (!string.IsNullOrEmpty(answer.AnswerText))
                                localAnswer.TextValue = answer.AnswerText;
                            if (answer.AnswerBoolean.HasValue)
                                localAnswer.BoolValue = answer.AnswerBoolean.Value;

                            localResponse.Answers.Add(localAnswer);
                        }

                        // Save to local database via repository
                        var pulseSurveyRepository = CreatePulseSurveyRepository();
                        var responseId = 0;
                        if (pulseSurveyRepository != null)
                        {
                            responseId = await pulseSurveyRepository.AddSurveyResponseAsync(localResponse);
                        }
                        if (responseId > 0)
                        {
                            // Mark as synced in Supabase
                            await _client.From<SupabaseSurveyResponse>()
                                .Where(r => r.Id == response.Id)
                                .Set(r => r.SyncedToTracker, true)
                                .Set(r => r.SyncedAt!, DateTime.UtcNow)
                                .Update();

                            syncedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn("Failed to sync response {0}: {1}", response.Id, ex.Message);
                    }
                }

                _logger.Info("Synced {0} responses", syncedCount);
                return (true, null, syncedCount);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to sync responses");
                return (false, ex.Message, 0);
            }
        }

        /// <summary>
        /// Gets the response count for a survey from Supabase.
        /// </summary>
        public async Task<(bool Success, int TotalResponses, int UnsyncedResponses)> GetResponseCountAsync(string supabaseSurveyId)
        {
            EnsureInitialized();

            try
            {
                var allResponses = await _client!.From<SupabaseSurveyResponse>()
                    .Where(r => r.SurveyId == supabaseSurveyId)
                    .Get();

                var total = allResponses.Models.Count;
                var unsynced = allResponses.Models.Count(r => !r.SyncedToTracker);

                return (true, total, unsynced);
            }
            catch (Exception ex)
            {
                _logger.Warn("Failed to get response count: {0}", ex.Message);
                return (false, 0, 0);
            }
        }

        #endregion

        #region Helpers

        private static TeamMemberRepository? CreateTeamMemberRepository()
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                return null;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            var context = contextFactory.CreateContext();
            return new TeamMemberRepository(context, userId.Value, () => contextFactory.CreateContext());
        }

        private static PulseSurveyRepository? CreatePulseSurveyRepository()
        {
            var userId = OrganizationContext.Current.UserIdOrNull;
            if (!userId.HasValue)
            {
                return null;
            }

            var contextFactory = TrackerDbContextFactory.Instance;
            var context = contextFactory.CreateContext();
            return new PulseSurveyRepository(context, userId.Value, () => contextFactory.CreateContext());
        }

        private static string MapQuestionType(SurveyQuestionType type)
        {
            return type switch
            {
                SurveyQuestionType.Rating => "rating",
                SurveyQuestionType.OpenEnded => "text",
                SurveyQuestionType.YesNo => "yes_no",
                _ => "text"
            };
        }

        #endregion
    }

    /// <summary>
    /// Information about a generated survey token.
    /// </summary>
    public class SurveyTokenInfo
    {
        public TeamMember TeamMember { get; set; } = null!;
        public string Token { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
