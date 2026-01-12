using System.IO;
using System.Text;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services.AI;
using Tracker.Services.Analytics;

namespace Tracker.Services
{
    /// <summary>
    /// Builds context for the Help Bot using RAG (Retrieval Augmented Generation).
    /// Uses semantic search to find relevant documentation for each query.
    /// </summary>
    public class HelpBotContextService
    {
        #region Singleton

        private static readonly Lazy<HelpBotContextService> _instance = 
            new(() => new HelpBotContextService(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static HelpBotContextService Instance => _instance.Value;

        #endregion

        #region Fields

        private readonly ILogger _logger;
        private bool _indexInitialized;

        #endregion

        #region Constructor

        private HelpBotContextService()
        {
            _logger = LoggingManager.GetComponentLogger("HelpBotContext");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Ensures the documentation is indexed for semantic search.
        /// Call this once at startup.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_indexInitialized) return;

            try
            {
                _logger.Info("Initializing RAG index...");
                await VectorStore.Instance.InitializeAsync();
                await DocumentIndexer.Instance.EnsureIndexedAsync();
                _indexInitialized = true;
                
                var stats = await DocumentIndexer.Instance.GetStatsAsync();
                _logger.Info("RAG initialized: {0} docs, {1} chunks", stats.DocumentCount, stats.ChunkCount);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Failed to initialize RAG");
            }
        }

        /// <summary>
        /// Builds the system context (instructions + user data).
        /// This is sent with every request as the system instruction.
        /// Expanded to ~10000 chars for better data coverage.
        /// </summary>
        public async Task<string> BuildSystemContextAsync()
        {
            var sb = new StringBuilder();

            // Core instructions
            sb.AppendLine(GetCoreInstructions());

            // User data summary - expanded limit
            var userData = await GetUserDataSummaryAsync();
            if (userData.Length > 8000)
            {
                userData = userData.Substring(0, 8000) + "\n[More data available...]";
            }
            sb.AppendLine(userData);

            var result = sb.ToString();
            
            // Expanded limit to 10K
            if (result.Length > 10000)
            {
                result = result.Substring(0, 10000);
                _logger.Warn("System context truncated to 10000 chars");
            }
            
            _logger.Info("System context: {0} chars", result.Length);
            
            return result;
        }

        /// <summary>
        /// Gets relevant documentation AND data for a user question using semantic search.
        /// Returns the most relevant chunks to include in the prompt.
        /// Expanded to ~5000 chars for better context.
        /// </summary>
        public async Task<string> GetRelevantDocsAsync(string question, int topK = 5)
        {
            if (!_indexInitialized)
            {
                await InitializeAsync();
            }

            try
            {
                // Get embedding for the question
                var questionEmbedding = await EmbeddingService.Instance.GetEmbeddingAsync(question);
                if (questionEmbedding == null)
                {
                    _logger.Warn("Could not embed question");
                    return "";
                }

                // Search for relevant chunks - search BOTH docs AND data
                // This searches across documentation + team members + tasks + meetings + OKRs + everything!
                var results = await VectorStore.Instance.SearchAsync(questionEmbedding, topK, minScore: 0.4f);

                if (results.Count == 0)
                {
                    _logger.Debug("No relevant docs/data found for: {0}", question);
                    return "";
                }

                // Build context from relevant chunks with expanded limit
                var sb = new StringBuilder();
                sb.AppendLine("Relevant information:");
                
                int totalChars = 0;
                const int maxChars = 5000; // Expanded limit on doc context

                foreach (var result in results)
                {
                    var chunk = result.Content;
                    if (totalChars + chunk.Length > maxChars)
                    {
                        // Truncate this chunk to fit
                        var remaining = maxChars - totalChars;
                        if (remaining > 100)
                        {
                            chunk = chunk.Substring(0, remaining) + "...";
                            sb.AppendLine(chunk);
                        }
                        break;
                    }
                    
                    sb.AppendLine(chunk);
                    totalChars += chunk.Length;
                }

                var docs = sb.ToString();
                _logger.Info("RAG: Found {0} chunks, returning {1} chars", results.Count, docs.Length);

                return docs;
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error in semantic search");
                return "";
            }
        }

        /// <summary>
        /// Builds the complete prompt for a question, including relevant docs.
        /// </summary>
        public async Task<string> BuildPromptWithContextAsync(string question)
        {
            var relevantDocs = await GetRelevantDocsAsync(question);
            
            if (string.IsNullOrEmpty(relevantDocs))
            {
                return question;
            }

            // Include relevant docs with the question
            return $"{relevantDocs}\n\n=== USER QUESTION ===\n{question}";
        }

        /// <summary>
        /// Forces a re-index of all documentation.
        /// </summary>
        public async Task ReindexAsync()
        {
            _indexInitialized = false;
            await DocumentIndexer.Instance.ReindexAllAsync();
            _indexInitialized = true;
        }

        /// <summary>
        /// Gets a quick context without RAG (for fallback).
        /// </summary>
        public string GetQuickContext()
        {
            return GetCoreInstructions();
        }

        #endregion

        #region Private Methods - Core Instructions

        private string GetCoreInstructions()
        {
            var today = DateTime.Now;
            return $@"You are Oracle, the AI assistant for Tracker. Tracker is a team management app with these features:
- Team Members: profiles of direct reports
- 1:1 Meetings: scheduled meetings with agenda items and notes
- Tasks: work items with due dates and priorities
- Projects: multi-task initiatives  
- OKRs: Objectives & Key Results for goal tracking
- KPIs: Key Performance Indicators for metrics
- Goals: individual development goals
- Feedback: performance feedback records

PREDICTIVE ANALYTICS CAPABILITIES:
You have access to trajectory predictions for OKRs, KPIs, Goals, and Projects. Use this data to:
- Alert users about items at risk of missing deadlines
- Recommend actions when trends are declining
- Celebrate improving trends and on-track progress
- Provide data-driven coaching suggestions

When discussing predictions:
- Explain confidence levels (High/Medium/Low/Insufficient data)
- Note that predictions improve with more historical data
- Suggest checking trajectory charts in the app for visual analysis

IMPORTANT - Current Date/Time Context:
- Today is {today:dddd, MMMM d, yyyy} at {today:h:mm tt}
- When users say ""next Tuesday"", ""tomorrow"", etc., YOU must calculate the actual date
- When creating meetings/tasks, convert relative dates to absolute dates (YYYY-MM-DD h:mm tt format)
- Example: If today is Thursday Dec 19 and user says ""next Tuesday at 12 PM"", that's ""2025-12-24 12:00 PM""

Be concise and helpful. Reference the user's actual data when relevant.";
        }

        private string GetRestrictions()
        {
            return @"Only answer questions about Tracker and the user's data. Be helpful but stay on topic.";
        }

        #endregion

        #region Private Methods - User Data

        private async Task<string> GetUserDataSummaryAsync()
        {
            var sb = new StringBuilder();

            try
            {
                // Team Members Summary
                sb.AppendLine("\n## Team Overview");
                await AppendTeamMembersSummary(sb);

                // Upcoming Meetings
                sb.AppendLine("\n## Upcoming 1:1 Meetings");
                await AppendUpcomingMeetings(sb);

                // Recent Tasks
                sb.AppendLine("\n## Active Tasks");
                await AppendActiveTasks(sb);

                // OKRs Summary
                sb.AppendLine("\n## Current OKRs");
                await AppendOkrsSummary(sb);

                // OKR Trajectory Predictions
                sb.AppendLine("\n## OKR Trajectory Predictions");
                await AppendOkrPredictionsSummary(sb);

                // KPIs Summary
                sb.AppendLine("\n## KPI Status");
                await AppendKpisSummary(sb);

                // Projects Summary
                sb.AppendLine("\n## Active Projects");
                await AppendProjectsSummary(sb);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error building user data summary");
                sb.AppendLine("Error loading user data summary.");
            }

            return sb.ToString();
        }

        private async Task AppendTeamMembersSummary(StringBuilder sb)
        {
            try
            {
                var members = await TrackerDataManager.Instance.GetTeamData();
                var activeMembers = members.Where(m => !m.IsDeleted).ToList();

                sb.AppendLine($"Total team members: {activeMembers.Count}");
                
                if (activeMembers.Any())
                {
                    sb.AppendLine("Team:");
                    foreach (var member in activeMembers.Take(25))
                    {
                        var details = new List<string>();
                        details.Add(member.JobTitle ?? "No title");
                        
                        if (member.HireDate != default && member.HireDate != DateTime.MinValue)
                            details.Add($"Hired: {member.HireDate:MMM d, yyyy}");
                        
                        if (member.Birthday.HasValue && member.Birthday.Value != DateTime.MinValue)
                            details.Add($"Birthday: {member.Birthday.Value:MMM d}");
                        
                        if (!string.IsNullOrEmpty(member.Email))
                            details.Add($"Email: {member.Email}");
                        
                        sb.AppendLine($"  - {member.FullName}: {string.Join(", ", details)}");
                    }
                    if (activeMembers.Count > 25)
                    {
                        sb.AppendLine($"  ... and {activeMembers.Count - 25} more");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warn("Error loading team members: {0}", ex.Message);
            }
        }

        private async Task AppendUpcomingMeetings(StringBuilder sb)
        {
            try
            {
                var meetings = await TrackerDataManager.Instance.GetOneOnOneMeetings();
                var upcoming = meetings
                    .Where(m => m.ScheduledAt >= DateTime.Today && !m.IsDeleted)
                    .OrderBy(m => m.ScheduledAt)
                    .Take(15)
                    .ToList();

                if (upcoming.Any())
                {
                    foreach (var meeting in upcoming)
                    {
                        var details = $"{meeting.ScheduledAt:MMM dd}: 1:1 with {meeting.Report?.FullName ?? "Unknown"} ({meeting.Status})";
                        
                        // Add agenda items if present
                        if (meeting.AgendaItems?.Any() == true)
                        {
                            details += $" - {meeting.AgendaItems.Count} agenda items";
                        }
                        
                        // Add notes preview if present
                        if (!string.IsNullOrEmpty(meeting.Notes) && meeting.Notes.Length > 50)
                        {
                            details += $" [Has notes]";
                        }
                        
                        sb.AppendLine($"  - {details}");
                    }
                }
                else
                {
                    sb.AppendLine("  No upcoming meetings scheduled.");
                }
            }
            catch (Exception ex)
            {
                _logger.Warn("Error loading meetings: {0}", ex.Message);
            }
        }

        private async Task AppendActiveTasks(StringBuilder sb)
        {
            try
            {
                var tasks = await TrackerDataManager.Instance.GetTasks();
                var activeTasks = tasks
                    .Where(t => !t.IsCompleted && !t.IsDeleted)
                    .OrderBy(t => t.DueDate)
                    .Take(15)
                    .ToList();

                if (activeTasks.Any())
                {
                    foreach (var task in activeTasks)
                    {
                        var dueInfo = task.DueDate != default ? $"Due: {task.DueDate:MMM dd}" : "No due date";
                        var owner = task.Owner != null && !string.IsNullOrEmpty(task.Owner.FullName) 
                            ? $"Owner: {task.Owner.FullName}" : "";
                        
                        var details = new List<string> { task.Description };
                        if (!string.IsNullOrEmpty(owner)) details.Add(owner);
                        details.Add(dueInfo);
                        
                        sb.AppendLine($"  - {string.Join(", ", details)}");
                    }
                }
                else
                {
                    sb.AppendLine("  No active tasks.");
                }
            }
            catch (Exception ex)
            {
                _logger.Warn("Error loading tasks: {0}", ex.Message);
            }
        }

        private async Task AppendOkrsSummary(StringBuilder sb)
        {
            try
            {
                var goals = await TrackerDataManager.Instance.GetStrategicGoals();
                var activeGoals = goals.Where(o => !o.IsDeleted).Take(10).ToList();

                if (activeGoals.Any())
                {
                    foreach (var goal in activeGoals)
                    {
                        sb.AppendLine($"  - {goal.Title} ({goal.ProgressPercent:P0} complete, {goal.Status})");
                    }
                }
                else
                {
                    sb.AppendLine("  No active OKRs.");
                }
            }
            catch (Exception ex)
            {
                _logger.Warn("Error loading OKRs: {0}", ex.Message);
            }
        }

        private async Task AppendOkrPredictionsSummary(StringBuilder sb)
        {
            try
            {
                var analyticsService = PredictiveAnalyticsService.Instance;
                var goals = await TrackerDataManager.Instance.GetStrategicGoals();
                var activeGoals = goals.Where(g => !g.IsDeleted).Take(5).ToList();

                if (!activeGoals.Any())
                {
                    sb.AppendLine("  No active Goals to analyze.");
                    return;
                }

                var predictionsAdded = 0;
                foreach (var goal in activeGoals)
                {
                    try
                    {
                        var prediction = await analyticsService.AnalyzeGoalAsync(
                            goal.Id, 
                            goal.Title,
                            goal.StartDate,
                            goal.EndDate);

                        if (prediction.IsValid && prediction.Trajectory != null)
                        {
                            var risk = prediction.Trajectory.Risk.ToString();
                            var trend = prediction.Trend?.Direction.ToString() ?? "Unknown";
                            var predictedDate = prediction.Trajectory.PredictedCompletionDate?.ToString("MMM d, yyyy") ?? "N/A";
                            var confidence = prediction.DataSufficiency?.Confidence.ToString() ?? "Unknown";

                            sb.AppendLine($"  - {goal.Title}:");
                            sb.AppendLine($"      Risk: {risk}, Trend: {trend}");
                            sb.AppendLine($"      Predicted completion: {predictedDate} (Confidence: {confidence})");
                            
                            if (prediction.Trajectory.Risk == TrajectoryPredictor.RiskLevel.Critical)
                            {
                                sb.AppendLine($"      ⚠️ CRITICAL: This Goal may not meet its target deadline");
                            }
                            else if (prediction.Trajectory.Risk == TrajectoryPredictor.RiskLevel.AtRisk)
                            {
                                sb.AppendLine($"      ⚡ AT RISK: Progress is slower than expected");
                            }

                            predictionsAdded++;
                        }
                    }
                    catch
                    {
                        // Skip OKRs without sufficient data
                    }
                }

                if (predictionsAdded == 0)
                {
                    sb.AppendLine("  Not enough historical data for predictions yet. Predictions will be available after a few days of progress tracking.");
                }
            }
            catch (Exception ex)
            {
                _logger.Warn("Error loading OKR predictions: {0}", ex.Message);
            }
        }

        private async Task AppendKpisSummary(StringBuilder sb)
        {
            try
            {
                var metrics = await TrackerDataManager.Instance.GetMetrics();
                var activeMetrics = metrics.Where(k => !k.IsDeleted).Take(10).ToList();

                if (activeMetrics.Any())
                {
                    foreach (var metric in activeMetrics)
                    {
                        var status = metric.CurrentValue >= (metric.TargetValue ?? 0) ? "✓ On Target" : "⚠ Below Target";
                        sb.AppendLine($"  - {metric.Name}: {metric.CurrentValue:N0}/{metric.TargetValue:N0} {metric.Unit} ({status})");
                    }
                }
                else
                {
                    sb.AppendLine("  No KPIs configured.");
                }
            }
            catch (Exception ex)
            {
                _logger.Warn("Error loading KPIs: {0}", ex.Message);
            }
        }

        private async Task AppendProjectsSummary(StringBuilder sb)
        {
            try
            {
                var projects = await TrackerDataManager.Instance.GetProjects();
                var activeProjects = projects
                    .Where(p => !p.IsDeleted && p.Status != WorkItemStatus.Completed)
                    .Take(10)
                    .ToList();

                if (activeProjects.Any())
                {
                    foreach (var project in activeProjects)
                    {
                        sb.AppendLine($"  - {project.Name} ({project.Status}, {project.ProgressPercent:P0} complete)");
                    }
                }
                else
                {
                    sb.AppendLine("  No active projects.");
                }
            }
            catch (Exception ex)
            {
                _logger.Warn("Error loading projects: {0}", ex.Message);
            }
        }

        #endregion

    }
}

