namespace Tracker.DataModels
{
    /// <summary>
    /// Comment or check-in on a development goal.
    /// Maps to Supabase 'development_goal_comments' table.
    /// </summary>
    public class DevelopmentGoalComment : AuditableEntity
    {
        /// <summary>
        /// Primary key (UUID).
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Parent development goal.
        /// </summary>
        public Guid GoalId { get; set; }
        public DevelopmentGoal? Goal { get; set; }

        /// <summary>
        /// Team member who authored the comment.
        /// </summary>
        public Guid AuthorTeamMemberId { get; set; }
        public TeamMember? Author { get; set; }

        /// <summary>
        /// Comment content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Type of comment (comment, check_in, encouragement).
        /// </summary>
        public string CommentType { get; set; } = "comment";
    }
}
