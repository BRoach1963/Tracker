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
                    "search_team_members" => await SearchTeamMembersAsync(arguments),
                    "get_upcoming_meetings" => await GetUpcomingMeetingsAsync(arguments),
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
                return $"Error: Could not find team member '{teamMemberName}'";

            // Parse date
            if (!DateTime.TryParse(dateStr, out var meetingDate))
                return $"Error: Invalid date format '{dateStr}'";

            // Create meeting
            var meeting = new OneOnOne
            {
                TeamMember = member,
                Date = meetingDate.Date,
                StartTime = meetingDate.TimeOfDay,
                EndTime = meetingDate.AddHours(1).TimeOfDay,
                Duration = TimeSpan.FromHours(1),
                Status = MeetingStatusEnum.Scheduled,
                Description = notes ?? "1:1 Meeting",
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow
            };

            await TrackerDataManager.Instance.AddOneOnOne(meeting);

            _logger.Info("Created 1:1 meeting with {0} on {1}", member.FullName, meetingDate.ToString("g"));
            return $"✓ Created 1:1 meeting with {member.FullName} on {meetingDate:dddd, MMMM d 'at' h:mm tt}";
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

            // Create task
            var task = new IndividualTask
            {
                Description = description,
                Owner = owner ?? new TeamMember(),
                DueDate = dueDate ?? DateTime.Now.AddDays(7),
                IsCompleted = false,
                Notes = string.Empty,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow
            };

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

            var kpi = new KeyPerformanceIndicator
            {
                Name = name,
                TargetValue = targetValue,
                Unit = unit ?? "",
                Value = currentValue,
                Description = "",
                Owner = new TeamMember(),
                TargetDirection = TargetDirectionEnum.GreaterOrEqual,
                Frequency = KpiFrequencyEnum.Monthly,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow
            };

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

            var okr = new ObjectiveKeyResult
            {
                Title = title,
                Description = description ?? "",
                Owner = new TeamMember(),
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(3),
                TimePeriod = TimePeriodEnum.Q1,
                Year = DateTime.Now.Year,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow
            };

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

        #endregion
    }
}
