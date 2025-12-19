using Tracker.Common.Enums;
using Tracker.DataModels;

namespace Tracker.Tests.Infrastructure
{
    /// <summary>
    /// Builder class for creating test data with sensible defaults.
    /// Uses fluent API for easy customization.
    /// </summary>
    public static class TestDataBuilder
    {
        private static int _idCounter = 1000;

        public static TeamMember CreateTeamMember(
            string? firstName = null,
            string? lastName = null,
            string? email = null,
            string? role = null,
            DateTime? hireDate = null)
        {
            var id = Interlocked.Increment(ref _idCounter);
            return new TeamMember
            {
                FirstName = firstName ?? $"Test{id}",
                LastName = lastName ?? $"User{id}",
                Email = email ?? $"test{id}@example.com",
                Role = role ?? "Developer",
                JobTitle = "Software Developer",
                HireDate = hireDate ?? DateTime.Today.AddYears(-1),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static OneOnOne CreateOneOnOne(
            TeamMember teamMember,
            DateTime? date = null,
            string? description = null,
            MeetingStatusEnum status = MeetingStatusEnum.Scheduled)
        {
            return new OneOnOne
            {
                TeamMember = teamMember,
                Date = date ?? DateTime.Today.AddDays(7),
                Time = TimeSpan.FromHours(10),
                Duration = 30,
                Description = description ?? "Weekly sync",
                Status = status,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static IndividualTask CreateTask(
            TeamMember owner,
            string? description = null,
            DateTime? dueDate = null,
            bool isCompleted = false)
        {
            return new IndividualTask
            {
                Description = description ?? "Test task",
                Owner = owner,
                DueDate = dueDate ?? DateTime.Today.AddDays(7),
                IsCompleted = isCompleted,
                Priority = TaskPriorityEnum.Medium,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static Project CreateProject(
            TeamMember owner,
            string? name = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var id = Interlocked.Increment(ref _idCounter);
            return new Project
            {
                Name = name ?? $"Test Project {id}",
                Description = "Test project description",
                Owner = owner,
                StartDate = startDate ?? DateTime.Today,
                EndDate = endDate ?? DateTime.Today.AddMonths(3),
                Status = ProjectStatusEnum.InProgress,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static ObjectiveKeyResult CreateOkr(
            TeamMember owner,
            string? title = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var id = Interlocked.Increment(ref _idCounter);
            return new ObjectiveKeyResult
            {
                Title = title ?? $"Test OKR {id}",
                Description = "Test OKR description",
                Owner = owner,
                StartDate = startDate ?? DateTime.Today,
                EndDate = endDate ?? DateTime.Today.AddMonths(3),
                Status = OkrStatusEnum.OnTrack,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static KeyPerformanceIndicator CreateKpi(
            TeamMember owner,
            string? name = null,
            double targetValue = 100,
            double currentValue = 50)
        {
            var id = Interlocked.Increment(ref _idCounter);
            return new KeyPerformanceIndicator
            {
                Name = name ?? $"Test KPI {id}",
                Description = "Test KPI description",
                Owner = owner,
                TargetValue = targetValue,
                Value = currentValue,
                Unit = "units",
                CreatedAt = DateTime.UtcNow
            };
        }

        public static Feedback CreateFeedback(
            TeamMember teamMember,
            FeedbackType type = FeedbackType.Positive,
            string? title = null,
            string? content = null)
        {
            var id = Interlocked.Increment(ref _idCounter);
            return new Feedback
            {
                TeamMember = teamMember,
                Type = type,
                Title = title ?? $"Test Feedback {id}",
                Content = content ?? "Test feedback content",
                Date = DateTime.Today,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static IndividualGoal CreateGoal(
            TeamMember teamMember,
            string? title = null,
            GoalCategory category = GoalCategory.SkillDevelopment,
            GoalStatus status = GoalStatus.InProgress)
        {
            var id = Interlocked.Increment(ref _idCounter);
            return new IndividualGoal
            {
                TeamMember = teamMember,
                Title = title ?? $"Test Goal {id}",
                Description = "Test goal description",
                Category = category,
                Status = status,
                ProgressPercent = 50,
                TargetDate = DateTime.Today.AddMonths(3),
                CreatedAt = DateTime.UtcNow
            };
        }

        public static Reminder CreateReminder(
            string? title = null,
            DateTime? dueDateTime = null,
            ReminderType type = ReminderType.Custom)
        {
            var id = Interlocked.Increment(ref _idCounter);
            return new Reminder
            {
                Title = title ?? $"Test Reminder {id}",
                Message = "Test reminder message",
                Type = type,
                Status = ReminderStatus.Pending,
                DueDateTime = dueDateTime ?? DateTime.Now.AddHours(1),
                CreatedAt = DateTime.UtcNow
            };
        }

        public static QuickNote CreateQuickNote(
            string? content = null,
            NoteCategory category = NoteCategory.General)
        {
            var id = Interlocked.Increment(ref _idCounter);
            return new QuickNote
            {
                Content = content ?? $"Test note content {id}",
                Category = category,
                IsPinned = false,
                IsArchived = false,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static MeetingTemplate CreateMeetingTemplate(
            string? name = null,
            int duration = 30)
        {
            var id = Interlocked.Increment(ref _idCounter);
            return new MeetingTemplate
            {
                Name = name ?? $"Test Template {id}",
                Description = "Test template description",
                Duration = duration,
                IsDefault = false,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}

