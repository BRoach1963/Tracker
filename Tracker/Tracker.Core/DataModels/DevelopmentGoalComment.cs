using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tracker.Core.DataModels
{
    /// <summary>
    /// Comment or check-in on a development goal.
    /// Used for progress updates, manager feedback, and notes.
    /// Maps to: development_goal_comments (7 columns)
    /// NOTE: This table does NOT have soft delete columns - just timestamps.
    /// </summary>
    [Table("development_goal_comments")]
    public class DevelopmentGoalComment
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The goal this comment belongs to.
        /// Maps to: goal_id UUID NOT NULL
        /// </summary>
        [Column("goal_id")]
        public Guid GoalId { get; set; }

        /// <summary>
        /// Author of the comment.
        /// Maps to: author_team_member_id UUID NOT NULL
        /// </summary>
        [Column("author_team_member_id")]
        public Guid AuthorTeamMemberId { get; set; }

        /// <summary>
        /// Comment content.
        /// Maps to: content TEXT NOT NULL
        /// </summary>
        [Column("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Type of comment (stored as string for PostgreSQL enum).
        /// Maps to: comment_type dev_goal_comment_type (enum) NOT NULL DEFAULT 'check_in'
        /// </summary>
        [Column("comment_type")]
        [MaxLength(50)]
        public string CommentTypeString { get; set; } = "check_in";

        /// <summary>
        /// Comment type as enum.
        /// </summary>
        [NotMapped]
        public DevelopmentGoalCommentType CommentType
        {
            get => CommentTypeString switch
            {
                "check_in" => DevelopmentGoalCommentType.CheckIn,
                "progress_update" => DevelopmentGoalCommentType.ProgressUpdate,
                "manager_feedback" => DevelopmentGoalCommentType.ManagerFeedback,
                "blocker" => DevelopmentGoalCommentType.Blocker,
                "milestone_completed" => DevelopmentGoalCommentType.MilestoneCompleted,
                "resource_added" => DevelopmentGoalCommentType.ResourceAdded,
                "note" => DevelopmentGoalCommentType.Note,
                _ => DevelopmentGoalCommentType.CheckIn
            };
            set => CommentTypeString = value switch
            {
                DevelopmentGoalCommentType.CheckIn => "check_in",
                DevelopmentGoalCommentType.ProgressUpdate => "progress_update",
                DevelopmentGoalCommentType.ManagerFeedback => "manager_feedback",
                DevelopmentGoalCommentType.Blocker => "blocker",
                DevelopmentGoalCommentType.MilestoneCompleted => "milestone_completed",
                DevelopmentGoalCommentType.ResourceAdded => "resource_added",
                DevelopmentGoalCommentType.Note => "note",
                _ => "check_in"
            };
        }

        /// <summary>
        /// When the comment was created.
        /// Maps to: created_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the comment was last updated.
        /// Maps to: updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        #region Navigation Properties

        /// <summary>
        /// Navigation to the development goal.
        /// </summary>
        [NotMapped]
        public DevelopmentGoal? Goal { get; set; }

        /// <summary>
        /// Navigation to the author.
        /// </summary>
        [NotMapped]
        public TeamMember? Author { get; set; }

        #endregion
    }

    /// <summary>
    /// Types of comments on development goals.
    /// </summary>
    public enum DevelopmentGoalCommentType
    {
        CheckIn,
        ProgressUpdate,
        ManagerFeedback,
        Blocker,
        MilestoneCompleted,
        ResourceAdded,
        Note
    }
}
