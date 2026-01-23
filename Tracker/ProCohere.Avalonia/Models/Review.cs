using System;
using System.Collections.Generic;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Review cycle model - maps to the review_cycles table in Supabase procohere schema.
/// Defines performance review cycles (annual, quarterly, etc.).
/// </summary>
[Table("review_cycles")]
public class ReviewCycle : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    #endregion

    #region Content

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Cycle type: 'annual', 'semi_annual', 'quarterly'.
    /// </summary>
    [Column("cycle_type")]
    public string CycleType { get; set; } = "annual";

    #endregion

    #region Status & Dates

    /// <summary>
    /// Status: 'draft', 'active', 'completed', 'cancelled'.
    /// </summary>
    [Column("status")]
    public string Status { get; set; } = "draft";

    /// <summary>
    /// Performance period start date.
    /// </summary>
    [Column("start_date")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Performance period end date.
    /// </summary>
    [Column("end_date")]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// When reviews can begin.
    /// </summary>
    [Column("review_start_date")]
    public DateTime? ReviewStartDate { get; set; }

    /// <summary>
    /// Deadline for completing reviews.
    /// </summary>
    [Column("review_end_date")]
    public DateTime? ReviewEndDate { get; set; }

    #endregion

    #region Soft Delete

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }

    #endregion

    #region Timestamps

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #endregion

    #region Navigation (not mapped)

    /// <summary>
    /// Reviews in this cycle (populated by service).
    /// </summary>
    public List<PerformanceReview> Reviews { get; set; } = new();

    #endregion

    #region Computed Properties

    public bool IsDraft => Status == "draft";
    public bool IsActive => Status == "active";
    public bool IsCompleted => Status == "completed";
    public bool IsCancelled => Status == "cancelled";

    public bool IsReviewPeriodOpen
    {
        get
        {
            if (!ReviewStartDate.HasValue || !ReviewEndDate.HasValue) return false;
            var now = DateTime.UtcNow;
            return now >= ReviewStartDate.Value && now <= ReviewEndDate.Value;
        }
    }

    public string StatusDisplay => Status switch
    {
        "draft" => "Draft",
        "active" => "Active",
        "completed" => "Completed",
        "cancelled" => "Cancelled",
        _ => Status
    };

    public string CycleTypeDisplay => CycleType switch
    {
        "annual" => "Annual",
        "semi_annual" => "Semi-Annual",
        "quarterly" => "Quarterly",
        _ => CycleType
    };

    public int ReviewCount => Reviews.Count;

    #endregion
}

/// <summary>
/// Performance review model - maps to performance_reviews table.
/// Individual performance reviews within a cycle.
/// </summary>
[Table("performance_reviews")]
public class PerformanceReview : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("review_cycle_id")]
    public Guid ReviewCycleId { get; set; }

    #endregion

    #region Participants

    /// <summary>
    /// Team member being reviewed.
    /// </summary>
    [Column("reviewee_id")]
    public Guid RevieweeId { get; set; }

    /// <summary>
    /// Team member giving the review.
    /// </summary>
    [Column("reviewer_id")]
    public Guid ReviewerId { get; set; }

    #endregion

    #region Review Type & Status

    /// <summary>
    /// Review type: 'manager', 'self', 'peer', '360'.
    /// </summary>
    [Column("review_type")]
    public string ReviewType { get; set; } = "manager";

    /// <summary>
    /// Status: 'pending', 'in_progress', 'submitted', 'acknowledged'.
    /// </summary>
    [Column("status")]
    public string Status { get; set; } = "pending";

    #endregion

    #region Review Content

    /// <summary>
    /// Overall rating (e.g., 1-5).
    /// </summary>
    [Column("overall_rating")]
    public int? OverallRating { get; set; }

    [Column("strengths")]
    public string? Strengths { get; set; }

    [Column("areas_for_improvement")]
    public string? AreasForImprovement { get; set; }

    [Column("goals_for_next_period")]
    public string? GoalsForNextPeriod { get; set; }

    [Column("additional_comments")]
    public string? AdditionalComments { get; set; }

    #endregion

    #region Workflow Dates

    [Column("submitted_at")]
    public DateTime? SubmittedAt { get; set; }

    [Column("acknowledged_at")]
    public DateTime? AcknowledgedAt { get; set; }

    #endregion

    #region Soft Delete

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }

    #endregion

    #region Timestamps

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #endregion

    #region Computed Properties

    public bool IsPending => Status == "pending";
    public bool IsInProgress => Status == "in_progress";
    public bool IsSubmitted => Status == "submitted";
    public bool IsAcknowledged => Status == "acknowledged";

    public bool IsSelfReview => ReviewType == "self";
    public bool IsManagerReview => ReviewType == "manager";
    public bool IsPeerReview => ReviewType == "peer";
    public bool Is360Review => ReviewType == "360";

    public string StatusDisplay => Status switch
    {
        "pending" => "Pending",
        "in_progress" => "In Progress",
        "submitted" => "Submitted",
        "acknowledged" => "Acknowledged",
        _ => Status
    };

    public string ReviewTypeDisplay => ReviewType switch
    {
        "manager" => "Manager Review",
        "self" => "Self Review",
        "peer" => "Peer Review",
        "360" => "360° Review",
        _ => ReviewType
    };

    public string RatingDisplay => OverallRating switch
    {
        1 => "Needs Improvement",
        2 => "Developing",
        3 => "Meets Expectations",
        4 => "Exceeds Expectations",
        5 => "Outstanding",
        _ => "Not Rated"
    };

    #endregion
}
