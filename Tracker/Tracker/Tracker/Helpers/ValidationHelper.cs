using System.ComponentModel.DataAnnotations;
using Tracker.DataModels;

namespace Tracker.Helpers
{
    /// <summary>
    /// Provides validation helpers for data models.
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Validates an entity and throws ValidationException if invalid.
        /// </summary>
        public static void ValidateAndThrow<T>(T entity) where T : class
        {
            var results = Validate(entity);
            if (results.Any())
            {
                throw new ValidationException(
                    $"Validation failed: {string.Join(", ", results.Select(r => r.ErrorMessage))}");
            }
        }

        /// <summary>
        /// Validates an entity and returns validation results.
        /// </summary>
        public static List<ValidationResult> Validate<T>(T entity) where T : class
        {
            ArgumentNullException.ThrowIfNull(entity);

            var context = new ValidationContext(entity);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(entity, context, results, validateAllProperties: true);

            // Additional custom validations based on type
            ValidateCustomRules(entity, results);

            return results;
        }

        /// <summary>
        /// Checks if an entity is valid.
        /// </summary>
        public static bool IsValid<T>(T entity) where T : class
        {
            return !Validate(entity).Any();
        }

        private static void ValidateCustomRules<T>(T entity, List<ValidationResult> results) where T : class
        {
            switch (entity)
            {
                case TeamMember tm:
                    ValidateTeamMember(tm, results);
                    break;
                case Meeting oneOnOne:
                    ValidateMeeting(oneOnOne, results);
                    break;
                case TrackerTask task:
                    ValidateTask(task, results);
                    break;
                case Project project:
                    ValidateProject(project, results);
                    break;
                case Feedback feedback:
                    ValidateFeedback(feedback, results);
                    break;
                case DevelopmentGoal goal:
                    ValidateGoal(goal, results);
                    break;
            }
        }

        private static void ValidateTeamMember(TeamMember tm, List<ValidationResult> results)
        {
            if (string.IsNullOrWhiteSpace(tm.FirstName))
                results.Add(new ValidationResult("First name is required", new[] { nameof(tm.FirstName) }));

            if (string.IsNullOrWhiteSpace(tm.LastName))
                results.Add(new ValidationResult("Last name is required", new[] { nameof(tm.LastName) }));

            if (string.IsNullOrWhiteSpace(tm.Email))
                results.Add(new ValidationResult("Email is required", new[] { nameof(tm.Email) }));
            else if (!IsValidEmail(tm.Email))
                results.Add(new ValidationResult("Invalid email format", new[] { nameof(tm.Email) }));

            if (tm.HireDate > DateTime.Today)
                results.Add(new ValidationResult("Hire date cannot be in the future", new[] { nameof(tm.HireDate) }));
        }

        private static void ValidateMeeting(Meeting meeting, List<ValidationResult> results)
        {
            if (!meeting.ReportTeamMemberId.HasValue || meeting.ReportTeamMemberId == Guid.Empty)
                results.Add(new ValidationResult("Team member is required", new[] { nameof(meeting.ReportTeamMemberId) }));

            if (meeting.DurationMinutes <= 0)
                results.Add(new ValidationResult("Duration must be positive", new[] { nameof(meeting.DurationMinutes) }));

            if (meeting.DurationMinutes > 240)
                results.Add(new ValidationResult("Duration seems unreasonably long (over 4 hours)", new[] { nameof(meeting.DurationMinutes) }));
        }

        private static void ValidateTask(TrackerTask task, List<ValidationResult> results)
        {
            if (string.IsNullOrWhiteSpace(task.Description))
                results.Add(new ValidationResult("Description is required", new[] { nameof(task.Description) }));

            if (task.DueDate < task.CreatedAt.Date && !task.IsCompleted)
                results.Add(new ValidationResult("Due date is in the past", new[] { nameof(task.DueDate) }));
        }

        private static void ValidateProject(Project project, List<ValidationResult> results)
        {
            if (string.IsNullOrWhiteSpace(project.Name))
                results.Add(new ValidationResult("Project name is required", new[] { nameof(project.Name) }));

            if (project.TargetEndDate.HasValue && project.StartDate > project.TargetEndDate)
                results.Add(new ValidationResult("End date must be after start date", new[] { nameof(project.TargetEndDate) }));
        }

        private static void ValidateFeedback(Feedback feedback, List<ValidationResult> results)
        {
            if (string.IsNullOrWhiteSpace(feedback.Content))
                results.Add(new ValidationResult("Content is required", new[] { nameof(feedback.Content) }));

            if (feedback.ToTeamMemberId == Guid.Empty)
                results.Add(new ValidationResult("Recipient team member is required", new[] { nameof(feedback.ToTeamMemberId) }));

            if (feedback.FromTeamMemberId == Guid.Empty)
                results.Add(new ValidationResult("Sender team member is required", new[] { nameof(feedback.FromTeamMemberId) }));
        }

        private static void ValidateGoal(DevelopmentGoal goal, List<ValidationResult> results)
        {
            if (string.IsNullOrWhiteSpace(goal.Title))
                results.Add(new ValidationResult("Title is required", new[] { nameof(goal.Title) }));

            if (goal.TeamMemberId == Guid.Empty)
                results.Add(new ValidationResult("Team member is required", new[] { nameof(goal.TeamMemberId) }));

            if (goal.ProgressPercent < 0 || goal.ProgressPercent > 100)
                results.Add(new ValidationResult("Progress must be between 0 and 100", new[] { nameof(goal.ProgressPercent) }));
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}

