using System.Text.Json;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.Services.AI
{
    /// <summary>
    /// Executes AI-requested functions like creating meetings, tasks, KPIs, etc.
    /// </summary>
    public class AIFunctionService
    {
        #region Singleton

        private static readonly Lazy<AIFunctionService> _instance =
            new(() => new AIFunctionService(), LazyThreadSafetyMode.ExecutionAndPublication);

        public static AIFunctionService Instance => _instance.Value;

        #endregion

        #region Fields

        private readonly ILogger _logger;

        #endregion

        #region Constructor

        private AIFunctionService()
        {
            _logger = LoggingManager.GetComponentLogger("AIFunctions");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Executes a function call from the AI.
        /// </summary>
        public async Task<string> ExecuteFunctionAsync(string functionName, JsonElement arguments)
        {
            _logger.Info("Executing function: {0}", functionName);

            try
            {
                return functionName switch
                {
                    "create_meeting" => await CreateMeetingAsync(arguments),
                    "create_task" => await CreateTaskAsync(arguments),
                    "create_kpi" => await CreateKPIAsync(arguments),
                    "create_okr" => await CreateOKRAsync(arguments),
                    "create_feedback" => await CreateFeedbackAsync(arguments),
                    "create_project" => await CreateProjectAsync(arguments),
                    "create_goal" => await CreateGoalAsync(arguments),
                    "create_note" => await CreateNoteAsync(arguments),
                    "search_team_members" => await SearchTeamMembersAsync(arguments),
                    "get_upcoming_meetings" => await GetUpcomingMeetingsAsync(arguments),
                    "get_projects" => await GetProjectsAsync(arguments),
                    "get_notes" => await GetNotesAsync(arguments),
                    "get_insights" => await GetInsightsAsync(arguments),
                    "dismiss_insight" => await DismissInsightAsync(arguments),
                    _ => $"Unknown function: {functionName}"
                };
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error executing function {0}", functionName);
                return $"Error: {ex.Message}";
            }
        }

        #endregion

        #region Function Implementations

        private async Task<string> CreateMeetingAsync(JsonElement args)
        {
            var teamMemberName = args.GetProperty("team_member_name").GetString();
            var dateStr = args.GetProperty("date").GetString();
            var notes = args.TryGetProperty("notes", out var n) ? n.GetString() : null;

            if (string.IsNullOrEmpty(teamMemberName) || string.IsNullOrEmpty(dateStr))
                return "Error: team_member_name and date are required";

            // Find team member
            var members = await TrackerDataManager.Instance.GetTeamData();
            var member = members.FirstOrDefault(m =>
                m.FullName.Contains(teamMemberName, StringComparison.OrdinalIgnoreCase) ||
                m.FirstName.Contains(teamMemberName, StringComparison.OrdinalIgnoreCase));

            if (member == null)
            {
                var memberNames = string.Join(", ", members.Select(m => m.FullName));
                _logger.Warn("Could not find team member '{0}'. Available members: {1}", teamMemberName, memberNames);
                return $"Error: Could not find team member '{teamMemberName}'. Available: {memberNames}";
            }
            
            _logger.Info("Found team member: {0} (ID={1})", member.FullName, member.Id);

            // Parse date
            if (!DateTime.TryParse(dateStr, out var meetingDate))
                return $"Error: Invalid date format '{dateStr}'";

            // Create meeting - DO NOT set TeamMember navigation property directly
            // It causes EF Core tracking conflicts since the member is already tracked
            // We'll set TeamMemberId via shadow property in AddOneOnOneAsync
            var meeting = new OneOnOne
            {
                Date = meetingDate.Date,
                StartTime = meetingDate.TimeOfDay,
                EndTime = meetingDate.AddHours(1).TimeOfDay,
                Duration = TimeSpan.FromHours(1),
                Status = MeetingStatusEnum.Scheduled,
                Description = notes ?? "1:1 Meeting",
                Agenda = notes ?? "",
                Notes = "",
                Feedback = "",
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow
            };
            
            // Store the member ID for setting the shadow property
            var memberId = member.Id;
            var memberName = member.FullName;

            var id = await TrackerDataManager.Instance.AddOneOnOne(meeting, memberId);

            if (id > 0)
            {
                _logger.Info("Created 1:1 meeting with {0} on {1}", memberName, meetingDate.ToString("g"));
                return $"✓ Created 1:1 meeting with {memberName} on {meetingDate:dddd, MMMM d 'at' h:mm tt}";
            }
            else
            {
                _logger.Error("Failed to create 1:1 meeting - AddOneOnOne returned 0");
                return $"Error: Failed to create meeting. Please check the logs for details.";
            }
        }

        private async Task<string> CreateTaskAsync(JsonElement args)
        {
            var description = args.GetProperty("description").GetString();
            var ownerName = args.TryGetProperty("owner_name", out var o) ? o.GetString() : null;
            var dueDateStr = args.TryGetProperty("due_date", out var d) ? d.GetString() : null;

            if (string.IsNullOrEmpty(description))
                return "Error: description is required";

            // Find owner if specified
            TeamMember? owner = null;
            if (!string.IsNullOrEmpty(ownerName))
            {
                var members = await TrackerDataManager.Instance.GetTeamData();
                owner = members.FirstOrDefault(m =>
                    m.FullName.Contains(ownerName, StringComparison.OrdinalIgnoreCase) ||
                    m.FirstName.Contains(ownerName, StringComparison.OrdinalIgnoreCase));
            }

            // Parse due date if provided
            DateTime? dueDate = null;
            if (!string.IsNullOrEmpty(dueDateStr) && DateTime.TryParse(dueDateStr, out var parsedDate))
                dueDate = parsedDate;

            // Create task - DO NOT set Owner navigation property directly
            // It causes EF Core tracking conflicts since the owner is already tracked
            // We'll set OwnerId via shadow property in AddTaskAsync
            var task = new IndividualTask
            {
                Description = description,
                DueDate = dueDate ?? DateTime.Now.AddDays(7),
                IsCompleted = false,
                Notes = string.Empty,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow
            };
            
            // Ensure navigation property is null to prevent EF from tracking an empty entity
            task.Owner = null!;

            await TrackerDataManager.Instance.AddTask(task);

            var ownerText = owner != null ? $" for {owner.FullName}" : "";
            var dueDateText = dueDate.HasValue ? $" (due {dueDate.Value:MMM d})" : "";
            _logger.Info("Created task: {0}{1}{2}", description, ownerText, dueDateText);
            return $"✓ Created task: {description}{ownerText}{dueDateText}";
        }

        private async Task<string> CreateKPIAsync(JsonElement args)
        {
            var name = args.GetProperty("name").GetString();
            var targetValue = args.GetProperty("target_value").GetDouble();
            var unit = args.TryGetProperty("unit", out var u) ? u.GetString() : "";
            var currentValue = args.TryGetProperty("current_value", out var c) ? c.GetDouble() : 0;

            if (string.IsNullOrEmpty(name))
                return "Error: name is required";

            // Create KPI - DO NOT set Owner navigation property directly
            // It causes EF Core tracking conflicts since the owner is already tracked
            // We'll set OwnerId via shadow property in AddKPIAsync
            var kpi = new KeyPerformanceIndicator
            {
                Name = name,
                TargetValue = targetValue,
                Unit = unit ?? "",
                Value = currentValue,
                Description = "",
                TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                Frequency = KpiFrequencyEnum.Monthly,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow
            };
            
            // Ensure navigation property is null to prevent EF from tracking an empty entity
            kpi.Owner = null!;

            await TrackerDataManager.Instance.AddKPI(kpi);

            _logger.Info("Created KPI: {0} (target: {1} {2})", name, targetValue, unit);
            return $"✓ Created KPI: {name} with target of {targetValue:N0} {unit}";
        }

        private async Task<string> CreateOKRAsync(JsonElement args)
        {
            var title = args.GetProperty("title").GetString();
            var description = args.TryGetProperty("description", out var d) ? d.GetString() : null;

            if (string.IsNullOrEmpty(title))
                return "Error: title is required";

            // Create OKR - DO NOT set Owner navigation property directly
            // It causes EF Core tracking conflicts since the owner is already tracked
            // We'll set OwnerId via shadow property in AddOKRAsync
            var okr = new ObjectiveKeyResult
            {
                Title = title,
                Description = description ?? "",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(3),
                TimePeriod = TimePeriodEnum.Q1,
                Year = DateTime.Now.Year,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow
            };
            
            // Ensure navigation property is null to prevent EF from tracking an empty entity
            okr.Owner = null!;

            await TrackerDataManager.Instance.AddOKR(okr);

            _logger.Info("Created OKR: {0}", title);
            return $"✓ Created OKR: {title}";
        }

        private async Task<string> SearchTeamMembersAsync(JsonElement args)
        {
            var query = args.TryGetProperty("query", out var q) ? q.GetString() : "";

            var members = await TrackerDataManager.Instance.GetTeamData();
            var activeMembers = members.Where(m => m.IsActive && !m.IsDeleted).ToList();

            if (!string.IsNullOrEmpty(query))
            {
                activeMembers = activeMembers.Where(m =>
                    m.FullName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    m.JobTitle.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    (m.Email?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();
            }

            if (!activeMembers.Any())
                return "No team members found";

            var results = activeMembers.Take(10).Select(m =>
                $"• {m.FullName} - {m.JobTitle}" +
                (m.HireDate != default ? $" (hired {m.HireDate:MMM yyyy})" : ""));

            return string.Join("\n", results);
        }

        private async Task<string> GetUpcomingMeetingsAsync(JsonElement args)
        {
            var daysAhead = args.TryGetProperty("days_ahead", out var d) ? d.GetInt32() : 7;

            var meetings = await TrackerDataManager.Instance.GetOneOnOnes();
            var upcoming = meetings.Where(m =>
                m.Status == MeetingStatusEnum.Scheduled &&
                m.Date >= DateTime.Now.Date &&
                m.Date <= DateTime.Now.AddDays(daysAhead).Date)
                .OrderBy(m => m.Date)
                .ToList();

            if (!upcoming.Any())
                return $"No scheduled meetings in the next {daysAhead} days";

            var results = upcoming.Take(10).Select(m =>
            {
                var memberName = m.TeamMember?.FullName ?? "Unknown";
                var meetingTime = m.Date.Add(m.StartTime);
                return $"• {meetingTime:ddd, MMM d 'at' h:mm tt} - {memberName}";
            });

            return string.Join("\n", results);
        }

        private async Task<string> CreateFeedbackAsync(JsonElement args)
        {
            var teamMemberName = args.GetProperty("team_member_name").GetString();
            var title = args.GetProperty("title").GetString();
            var content = args.GetProperty("content").GetString();
            var typeStr = args.TryGetProperty("type", out var t) ? t.GetString() : "Positive";

            if (string.IsNullOrEmpty(teamMemberName) || string.IsNullOrEmpty(title) || string.IsNullOrEmpty(content))
                return "Error: team_member_name, title, and content are required";

            // Find team member
            var members = await TrackerDataManager.Instance.GetTeamData();
            var member = members.FirstOrDefault(m =>
                m.FullName.Contains(teamMemberName, StringComparison.OrdinalIgnoreCase) ||
                m.FirstName.Contains(teamMemberName, StringComparison.OrdinalIgnoreCase));

            if (member == null)
            {
                var memberNames = string.Join(", ", members.Select(m => m.FullName));
                return $"Error: Could not find team member '{teamMemberName}'. Available: {memberNames}";
            }

            // Parse feedback type
            if (!Enum.TryParse<FeedbackType>(typeStr, true, out var feedbackType))
                feedbackType = FeedbackType.Positive;

            // Create feedback - DO NOT set TeamMember navigation property
            var feedback = new Feedback
            {
                TeamMemberId = member.Id,
                Title = title,
                Content = content,
                Type = feedbackType,
                Date = DateTime.Now,
                Context = "",
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow
            };

            // Ensure navigation property is null to prevent EF from tracking an empty entity
            feedback.TeamMember = null!;

            var id = await TrackerDataManager.Instance.AddFeedback(feedback);

            if (id > 0)
            {
                _logger.Info("Created feedback for {0}: {1}", member.FullName, title);
                return $"✓ Created {feedbackType} feedback for {member.FullName}: {title}";
            }
            else
            {
                _logger.Error("Failed to create feedback");
                return "Error: Failed to create feedback. Please check the logs.";
            }
        }

        private async Task<string> CreateProjectAsync(JsonElement args)
        {
            var name = args.GetProperty("name").GetString();
            var description = args.TryGetProperty("description", out var d) ? d.GetString() : "";
            var startDateStr = args.TryGetProperty("start_date", out var s) ? s.GetString() : null;
            var endDateStr = args.TryGetProperty("end_date", out var e) ? e.GetString() : null;

            if (string.IsNullOrEmpty(name))
                return "Error: name is required";

            // Parse dates
            var startDate = DateTime.Now;
            if (!string.IsNullOrEmpty(startDateStr) && DateTime.TryParse(startDateStr, out var parsedStart))
                startDate = parsedStart;

            DateTime? endDate = null;
            if (!string.IsNullOrEmpty(endDateStr) && DateTime.TryParse(endDateStr, out var parsedEnd))
                endDate = parsedEnd;

            // Create project - DO NOT set Owner navigation property
            var project = new Project
            {
                Name = name,
                Description = description ?? "",
                StartDate = startDate,
                EndDate = endDate,
                Status = "NotStarted",
                Budget = decimal.MinValue,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow
            };

            // Ensure navigation property is null to prevent EF from tracking an empty entity
            project.Owner = null!;

            var id = await TrackerDataManager.Instance.AddProject(project);

            if (id > 0)
            {
                var dateInfo = endDate.HasValue ? $" (due {endDate.Value:MMM d, yyyy})" : "";
                _logger.Info("Created project: {0}", name);
                return $"✓ Created project: {name}{dateInfo}";
            }
            else
            {
                _logger.Error("Failed to create project");
                return "Error: Failed to create project. Please check the logs.";
            }
        }

        private async Task<string> CreateGoalAsync(JsonElement args)
        {
            var teamMemberName = args.GetProperty("team_member_name").GetString();
            var title = args.GetProperty("title").GetString();
            var description = args.TryGetProperty("description", out var d) ? d.GetString() : "";
            var targetDateStr = args.TryGetProperty("target_date", out var t) ? t.GetString() : null;
            var categoryStr = args.TryGetProperty("category", out var c) ? c.GetString() : "SkillDevelopment";

            if (string.IsNullOrEmpty(teamMemberName) || string.IsNullOrEmpty(title))
                return "Error: team_member_name and title are required";

            // Find team member
            var members = await TrackerDataManager.Instance.GetTeamData();
            var member = members.FirstOrDefault(m =>
                m.FullName.Contains(teamMemberName, StringComparison.OrdinalIgnoreCase) ||
                m.FirstName.Contains(teamMemberName, StringComparison.OrdinalIgnoreCase));

            if (member == null)
            {
                var memberNames = string.Join(", ", members.Select(m => m.FullName));
                return $"Error: Could not find team member '{teamMemberName}'. Available: {memberNames}";
            }

            // Parse target date
            DateTime? targetDate = null;
            if (!string.IsNullOrEmpty(targetDateStr) && DateTime.TryParse(targetDateStr, out var parsed))
                targetDate = parsed;

            // Parse category
            if (!Enum.TryParse<GoalCategory>(categoryStr, true, out var category))
                category = GoalCategory.SkillDevelopment;

            // Create goal - DO NOT set TeamMember navigation property
            var goal = new IndividualGoal
            {
                TeamMemberId = member.Id,
                Title = title,
                Description = description ?? "",
                Category = category,
                Status = GoalStatus.NotStarted,
                TargetDate = targetDate,
                ProgressPercent = 0,
                Notes = "",
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow
            };

            // Ensure navigation property is null to prevent EF from tracking an empty entity
            goal.TeamMember = null!;

            var id = await TrackerDataManager.Instance.AddGoal(goal);

            if (id > 0)
            {
                var dateInfo = targetDate.HasValue ? $" (target: {targetDate.Value:MMM d, yyyy})" : "";
                _logger.Info("Created goal for {0}: {1}", member.FullName, title);
                return $"✓ Created {category} goal for {member.FullName}: {title}{dateInfo}";
            }
            else
            {
                _logger.Error("Failed to create goal");
                return "Error: Failed to create goal. Please check the logs.";
            }
        }

        private async Task<string> CreateNoteAsync(JsonElement args)
        {
            var content = args.GetProperty("content").GetString();
            var title = args.TryGetProperty("title", out var t) ? t.GetString() : "";
            var categoryStr = args.TryGetProperty("category", out var c) ? c.GetString() : "General";

            if (string.IsNullOrEmpty(content))
                return "Error: content is required";

            // Parse category
            if (!Enum.TryParse<NoteCategory>(categoryStr, true, out var category))
                category = NoteCategory.General;

            var note = new QuickNote
            {
                Title = title ?? "",
                Content = content,
                Category = category,
                LinkedEntityType = NoteLinkedEntityType.None,
                IsPinned = false,
                IsArchived = false,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow
            };

            var id = await TrackerDataManager.Instance.AddQuickNote(note);

            if (id > 0)
            {
                var displayTitle = string.IsNullOrEmpty(title) ? content.Substring(0, Math.Min(50, content.Length)) : title;
                _logger.Info("Created note: {0}", displayTitle);
                return $"✓ Created note: {displayTitle}";
            }
            else
            {
                _logger.Error("Failed to create note");
                return "Error: Failed to create note. Please check the logs.";
            }
        }

        private async Task<string> GetProjectsAsync(JsonElement args)
        {
            var query = args.TryGetProperty("query", out var q) ? q.GetString() : "";

            var projects = await TrackerDataManager.Instance.GetProjects();

            if (!string.IsNullOrEmpty(query))
            {
                projects = projects.Where(p =>
                    p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    (p.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();
            }

            if (!projects.Any())
                return string.IsNullOrEmpty(query) ? "No projects found" : $"No projects matching '{query}'";

            var results = projects.Take(10).Select(p =>
            {
                var dateInfo = p.EndDate.HasValue ? $" (due {p.EndDate.Value:MMM d})" : "";
                var taskInfo = p.Tasks?.Any() == true ? $" - {p.Tasks.Count} tasks" : "";
                return $"• {p.Name} ({p.Status}){dateInfo}{taskInfo}";
            });

            return string.Join("\n", results);
        }

        private async Task<string> GetNotesAsync(JsonElement args)
        {
            var limit = args.TryGetProperty("limit", out var l) ? l.GetInt32() : 10;

            var notes = await TrackerDataManager.Instance.GetQuickNotes();
            var recent = notes.OrderByDescending(n => n.CreatedAt).Take(limit).ToList();

            if (!recent.Any())
                return "No notes found";

            var results = recent.Select(n =>
            {
                var title = string.IsNullOrEmpty(n.Title) ? n.Content.Substring(0, Math.Min(40, n.Content.Length)) + "..." : n.Title;
                var date = n.CreatedAt.ToString("MMM d");
                var category = n.Category != NoteCategory.General ? $" [{n.Category}]" : "";
                return $"• {date}{category}: {title}";
            });

            return string.Join("\n", results);
        }

        private async Task<string> GetInsightsAsync(JsonElement args)
        {
            try
            {
                var severityFilter = args.TryGetProperty("severity", out var s) ? s.GetString()?.ToLower() : "all";
                var typeFilter = args.TryGetProperty("type", out var t) ? t.GetString()?.ToLower() : null;

                var engine = Insights.InsightEngine.Instance;
                var insights = await engine.GetActiveInsightsAsync();

                // Apply severity filter
                if (severityFilter != "all" && !string.IsNullOrEmpty(severityFilter))
                {
                    insights = severityFilter switch
                    {
                        "critical" => insights.Where(i => i.Severity == InsightSeverity.Critical).ToList(),
                        "warning" => insights.Where(i => i.Severity == InsightSeverity.Warning).ToList(),
                        "info" => insights.Where(i => i.Severity == InsightSeverity.Info).ToList(),
                        _ => insights
                    };
                }

                // Apply type filter
                if (!string.IsNullOrEmpty(typeFilter))
                {
                    insights = typeFilter switch
                    {
                        "meeting_gap" => insights.Where(i => i.Type == InsightType.MeetingGap).ToList(),
                        "birthday" => insights.Where(i => i.Type == InsightType.UpcomingBirthday).ToList(),
                        "anniversary" => insights.Where(i => i.Type == InsightType.UpcomingAnniversary).ToList(),
                        "stale_task" => insights.Where(i => i.Type == InsightType.StaleActionItem).ToList(),
                        "okr_at_risk" => insights.Where(i => i.Type == InsightType.OkrAtRisk).ToList(),
                        "okr_ending" => insights.Where(i => i.Type == InsightType.OkrEndingSoon).ToList(),
                        "kpi_off_target" => insights.Where(i => i.Type == InsightType.KpiOffTarget).ToList(),
                        "survey_alert" => insights.Where(i => i.Type == InsightType.SurveyAlert).ToList(),
                        _ => insights
                    };
                }

                if (!insights.Any())
                {
                    return severityFilter == "all" && typeFilter == null
                        ? "🎉 All clear! No active insights - you're on top of everything."
                        : "No insights matching that filter.";
                }

                // Group by severity for organized output
                var critical = insights.Where(i => i.Severity == InsightSeverity.Critical).ToList();
                var warnings = insights.Where(i => i.Severity == InsightSeverity.Warning).ToList();
                var info = insights.Where(i => i.Severity == InsightSeverity.Info).ToList();

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"📊 You have {insights.Count} active insight(s):");
                sb.AppendLine();

                if (critical.Any())
                {
                    sb.AppendLine("🔴 **CRITICAL** (needs immediate attention):");
                    foreach (var insight in critical)
                    {
                        sb.AppendLine($"  [ID:{insight.Id}] {insight.Title}");
                        sb.AppendLine($"      {insight.Description}");
                    }
                    sb.AppendLine();
                }

                if (warnings.Any())
                {
                    sb.AppendLine("🟠 **WARNINGS** (should address soon):");
                    foreach (var insight in warnings)
                    {
                        sb.AppendLine($"  [ID:{insight.Id}] {insight.Title}");
                        sb.AppendLine($"      {insight.Description}");
                    }
                    sb.AppendLine();
                }

                if (info.Any())
                {
                    sb.AppendLine("🔵 **INFO** (good to know):");
                    foreach (var insight in info)
                    {
                        sb.AppendLine($"  [ID:{insight.Id}] {insight.Title}");
                        sb.AppendLine($"      {insight.Description}");
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error getting insights");
                return "Unable to retrieve insights at this time.";
            }
        }

        private async Task<string> DismissInsightAsync(JsonElement args)
        {
            try
            {
                if (!args.TryGetProperty("insight_id", out var idProp))
                    return "Error: insight_id is required";

                var insightId = idProp.GetInt32();

                var engine = Insights.InsightEngine.Instance;
                await engine.DismissInsightAsync(insightId);

                return $"✓ Insight #{insightId} has been dismissed.";
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error dismissing insight");
                return $"Error dismissing insight: {ex.Message}";
            }
        }

        #endregion
    }
}
