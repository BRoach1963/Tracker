using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ProCohere.Avalonia.Interfaces.AI;

namespace ProCohere.Avalonia.Services.AI;

/// <summary>
/// Executes AI-requested functions like creating tasks, meetings, goals, etc.
/// Provides clean interface between AI and ProCohere data operations.
/// </summary>
public sealed class AIFunctionService
{
    #region Singleton

    private static readonly Lazy<AIFunctionService> _instance = 
        new(() => new AIFunctionService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

    public static AIFunctionService Instance => _instance.Value;

    #endregion

    #region Data Services

    private readonly ITaskDataService _taskService;
    private readonly IMeetingDataService _meetingService;
    private readonly IGoalDataService _goalService;
    private readonly IProjectDataService _projectService;
    private readonly INoteDataService _noteService;
    private readonly ITeamDataService _teamService;

    #endregion

    #region Constructor

    private AIFunctionService() 
    {
        _taskService = new TaskDataService();
        _meetingService = new MeetingDataService();
        _goalService = new GoalDataService();
        _projectService = new ProjectDataService();
        _noteService = new NoteDataService();
        _teamService = new TeamDataService();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Executes a function call from the AI.
    /// </summary>
    /// <param name="functionName">Name of the function to execute</param>
    /// <param name="arguments">JSON arguments for the function</param>
    /// <returns>Function execution result as text</returns>
    public async Task<string> ExecuteFunctionAsync(string functionName, JsonElement arguments)
    {
        try
        {
            return functionName.ToLowerInvariant() switch
            {
                // Core creation functions
                "create_task" => await CreateTaskAsync(arguments),
                "create_meeting" => await CreateMeetingAsync(arguments),
                "create_goal" => await CreateGoalAsync(arguments),
                "create_project" => await CreateProjectAsync(arguments),
                "create_note" => await CreateNoteAsync(arguments),
                
                // Information retrieval functions
                "search_team_members" => await SearchTeamMembersAsync(arguments),
                "get_upcoming_meetings" => await GetUpcomingMeetingsAsync(arguments),
                "get_projects" => await GetProjectsAsync(arguments),
                "get_notes" => await GetNotesAsync(arguments),
                "get_tasks" => await GetTasksAsync(arguments),
                
                // Helper functions
                "get_current_time" => GetCurrentTime(),
                "help" => GetAvailableFunctions(),
                
                _ => $"Unknown function: {functionName}. Use 'help' to see available functions."
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AIFunctionService] Error executing function {functionName}: {ex.Message}");
            return $"Error executing {functionName}: {ex.Message}";
        }
    }

    #endregion

    #region Function Implementations

    private async Task<string> CreateTaskAsync(JsonElement args)
    {
        try
        {
            var description = GetStringProperty(args, "description", required: true);
            var priority = GetStringProperty(args, "priority", defaultValue: "Medium");
            var dueDate = args.TryGetProperty("due_date", out var dd) ? dd.GetString() : null;
            var assignedTo = args.TryGetProperty("assigned_to", out var at) ? at.GetString() : null;

            return await _taskService.CreateTaskAsync(description, priority, dueDate, assignedTo);
        }
        catch (Exception ex)
        {
            return $"❌ Error creating task: {ex.Message}";
        }
    }

    private async Task<string> CreateMeetingAsync(JsonElement args)
    {
        try
        {
            var title = GetStringProperty(args, "title", required: true);
            var attendees = args.TryGetProperty("attendees", out var att) ? att.GetString() : null;
            var dateTime = args.TryGetProperty("date_time", out var dt) ? dt.GetString() : null;
            var agenda = args.TryGetProperty("agenda", out var ag) ? ag.GetString() : null;

            return await _meetingService.CreateMeetingAsync(title, attendees, dateTime, agenda);
        }
        catch (Exception ex)
        {
            return $"❌ Error creating meeting: {ex.Message}";
        }
    }

    private async Task<string> CreateGoalAsync(JsonElement args)
    {
        try
        {
            var title = GetStringProperty(args, "title", required: true);
            var description = args.TryGetProperty("description", out var desc) ? desc.GetString() : null;
            var targetDate = args.TryGetProperty("target_date", out var td) ? td.GetString() : null;
            var category = GetStringProperty(args, "category", defaultValue: "Professional");

            return await _goalService.CreateGoalAsync(title, description, targetDate, category);
        }
        catch (Exception ex)
        {
            return $"❌ Error creating goal: {ex.Message}";
        }
    }

    private async Task<string> CreateProjectAsync(JsonElement args)
    {
        try
        {
            var name = GetStringProperty(args, "name", required: true);
            var description = args.TryGetProperty("description", out var desc) ? desc.GetString() : null;
            var startDate = args.TryGetProperty("start_date", out var sd) ? sd.GetString() : null;
            var endDate = args.TryGetProperty("end_date", out var ed) ? ed.GetString() : null;

            return await _projectService.CreateProjectAsync(name, description, startDate, endDate);
        }
        catch (Exception ex)
        {
            return $"❌ Error creating project: {ex.Message}";
        }
    }

    private async Task<string> CreateNoteAsync(JsonElement args)
    {
        try
        {
            var title = GetStringProperty(args, "title", required: true);
            var content = GetStringProperty(args, "content", required: true);
            var tags = args.TryGetProperty("tags", out var t) ? t.GetString() : null;

            return await _noteService.CreateNoteAsync(title, content, tags);
        }
        catch (Exception ex)
        {
            return $"❌ Error creating note: {ex.Message}";
        }
    }

    private async Task<string> SearchTeamMembersAsync(JsonElement args)
    {
        try
        {
            var query = GetStringProperty(args, "query");

            var members = await _teamService.SearchTeamMembersAsync(query);
            
            if (members == null || members.Count == 0)
            {
                return string.IsNullOrEmpty(query) 
                    ? "❌ No team members found" 
                    : $"❌ No team members found matching '{query}'";
            }

            var memberList = members.Take(10).Select(m => 
                $"👤 {m.FirstName} {m.LastName}" +
                (string.IsNullOrEmpty(m.JobTitle) ? "" : $" - {m.JobTitle}") +
                (string.IsNullOrEmpty(m.Email) ? "" : $" ({m.Email})")
            );

            return $"👥 Found {members.Count} team member(s):\n\n" + string.Join("\n", memberList);
        }
        catch (Exception ex)
        {
            return $"❌ Error searching team members: {ex.Message}";
        }
    }

    private async Task<string> GetUpcomingMeetingsAsync(JsonElement args)
    {
        try
        {
            var days = args.TryGetProperty("days_ahead", out var daysProp) ? daysProp.GetInt32() : 7;

            var meetings = await _meetingService.GetUpcomingMeetingsAsync(days);
            
            if (meetings == null || meetings.Count == 0)
            {
                return $"📅 No meetings scheduled for the next {days} days.";
            }

            var meetingList = meetings.Take(10).Select(m =>
                $"📅 **{m.Title}** - {m.ScheduledAt:MMM dd, yyyy h:mm tt}"
            );

            return $"📅 Upcoming meetings (next {days} days):\n\n" + string.Join("\n", meetingList);
        }
        catch (Exception ex)
        {
            return $"❌ Error getting meetings: {ex.Message}";
        }
    }

    private async Task<string> GetProjectsAsync(JsonElement args)
    {
        try
        {
            var query = GetStringProperty(args, "query");

            var projects = await _projectService.GetProjectsAsync(query);
            
            if (projects == null || projects.Count == 0)
            {
                return string.IsNullOrEmpty(query) 
                    ? "📊 No active projects found." 
                    : $"📊 No projects found matching '{query}'.";
            }

            var projectList = projects.Take(10).Select(p =>
                $"📊 **{p.Name}** - {p.Status}" +
                (p.DueDate.HasValue ? $" (due {p.DueDate:MMM dd, yyyy})" : "")
            );

            return $"📊 Found {projects.Count} project(s):\n\n" + string.Join("\n", projectList);
        }
        catch (Exception ex)
        {
            return $"❌ Error getting projects: {ex.Message}";
        }
    }

    private async Task<string> GetNotesAsync(JsonElement args)
    {
        try
        {
            var limit = args.TryGetProperty("limit", out var limitProp) ? limitProp.GetInt32() : 10;

            var notes = await _noteService.GetNotesAsync(limit);
            
            if (notes == null || notes.Count == 0)
            {
                return "📝 No notes found.";
            }

            var noteList = notes.Take(limit).Select(n =>
            {
                var title = string.IsNullOrEmpty(n.Title) 
                    ? (n.Content.Length > 40 ? n.Content.Substring(0, 40) + "..." : n.Content)
                    : n.Title;
                return $"📝 **{title}** - {n.UpdatedAt:MMM dd, yyyy}";
            });

            return $"📝 Recent notes (last {Math.Min(limit, notes.Count)}):\n\n" + string.Join("\n", noteList);
        }
        catch (Exception ex)
        {
            return $"❌ Error getting notes: {ex.Message}";
        }
    }

    private async Task<string> GetTasksAsync(JsonElement args)
    {
        try
        {
            var priority = args.TryGetProperty("priority", out var p) ? p.GetString() : null;
            var status = GetStringProperty(args, "status", defaultValue: "open");

            var tasks = await _taskService.GetTasksAsync(priority, status);
            
            if (tasks == null || tasks.Count == 0)
            {
                return $"📋 No {status} tasks found" + 
                       (string.IsNullOrEmpty(priority) ? "." : $" with {priority} priority.");
            }

            var taskList = tasks.Take(10).Select(t =>
                $"📋 **{t.Description}** - {t.Priority} Priority" +
                (t.DueDate.HasValue ? $" - Due {t.DueDate:MMM dd, yyyy}" : "")
            );

            return $"📋 Found {tasks.Count} {status} task(s):\n\n" + string.Join("\n", taskList);
        }
        catch (Exception ex)
        {
            return $"❌ Error getting tasks: {ex.Message}";
        }
    }

    private string GetCurrentTime()
    {
        var now = DateTime.Now;
        return $"🕐 Current time: {now:yyyy-MM-dd HH:mm:ss}\n" +
               $"📅 Today is {now:dddd, MMMM dd, yyyy}";
    }

    private string GetAvailableFunctions()
    {
        return @"🤖 **Available AI Functions:**

📝 **Creation Functions:**
• `create_task` - Create a new task with description, priority, due date
• `create_meeting` - Schedule a meeting with attendees and agenda
• `create_goal` - Set up a new goal with targets and timeline
• `create_project` - Start a new project with scope and milestones
• `create_note` - Document insights and meeting notes

🔍 **Information Functions:**
• `search_team_members` - Find team members by name or role
• `get_upcoming_meetings` - Show scheduled meetings
• `get_projects` - List your active projects
• `get_notes` - Browse recent notes and documentation
• `get_tasks` - View your task list with filters

⚙️ **Utility Functions:**
• `get_current_time` - Get current date and time
• `help` - Show this function reference

💡 **Usage:** Simply ask me to do something like ""Create a task to review the quarterly reports"" or ""Schedule a 1:1 with Sarah for next Tuesday""";
    }

    #endregion

    #region Helper Methods

    private string GetStringProperty(JsonElement args, string propertyName, bool required = false, string defaultValue = "")
    {
        if (args.TryGetProperty(propertyName, out var prop))
        {
            return prop.GetString() ?? defaultValue;
        }

        if (required)
        {
            throw new ArgumentException($"Required parameter '{propertyName}' is missing");
        }

        return defaultValue;
    }

    #endregion
}