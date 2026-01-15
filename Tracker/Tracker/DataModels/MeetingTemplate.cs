using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// A reusable template for meetings with pre-defined agenda items.
    /// Maps to Supabase meeting_templates table.
    /// </summary>
    [Table("meeting_templates")]
    public class MeetingTemplate : AuditableEntity
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Organization this template belongs to.
        /// Maps to: organization_id UUID NOT NULL
        /// </summary>
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// User who created this template.
        /// Maps to: created_by_user_id UUID NOT NULL
        /// </summary>
        [Column("created_by_user_id")]
        public Guid CreatedByUserId { get; set; }

        /// <summary>
        /// Name of the template (e.g., "Weekly Check-in", "Performance Review").
        /// Maps to: name VARCHAR(200) NOT NULL
        /// </summary>
        [Column("name")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of when to use this template.
        /// Maps to: description TEXT NULL
        /// </summary>
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Type of meeting this template is for (stored as string).
        /// Maps to: meeting_type VARCHAR(50) NOT NULL DEFAULT 'one_on_one'
        /// </summary>
        [Column("meeting_type")]
        [MaxLength(50)]
        public string MeetingTypeString { get; set; } = "one_on_one";

        /// <summary>
        /// Meeting type as enum.
        /// </summary>
        [NotMapped]
        public MeetingType MeetingType
        {
            get => Enum.TryParse<MeetingType>(MeetingTypeString, true, out var result) ? result : MeetingType.OneOnOne;
            set => MeetingTypeString = value.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// Suggested duration in minutes.
        /// Maps to: suggested_duration_minutes INT NOT NULL DEFAULT 30
        /// </summary>
        [Column("suggested_duration_minutes")]
        public int SuggestedDurationMinutes { get; set; } = 30;

        /// <summary>
        /// Sort order for display.
        /// Maps to: sort_order INT NOT NULL DEFAULT 0
        /// </summary>
        [Column("sort_order")]
        public int SortOrder { get; set; }

        /// <summary>
        /// Whether this template is available for use.
        /// Maps to: is_active BOOLEAN NOT NULL DEFAULT true
        /// </summary>
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        #region Navigation Properties

        /// <summary>
        /// Pre-defined agenda items for this template.
        /// </summary>
        [NotMapped]
        public List<MeetingTemplateItem> Items { get; set; } = new();

        #endregion
    }

    /// <summary>
    /// An agenda item within a meeting template.
    /// Maps to Supabase meeting_template_items table.
    /// Note: This table does NOT have soft delete columns.
    /// </summary>
    [Table("meeting_template_items")]
    public class MeetingTemplateItem
    {
        /// <summary>
        /// Primary key (UUID).
        /// Maps to: id UUID NOT NULL DEFAULT gen_random_uuid()
        /// </summary>
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The template this item belongs to.
        /// Maps to: template_id UUID NOT NULL REFERENCES meeting_templates(id)
        /// </summary>
        [Column("template_id")]
        public Guid TemplateId { get; set; }

        /// <summary>
        /// The title/topic of the agenda item.
        /// Maps to: title VARCHAR(300) NOT NULL
        /// </summary>
        [Column("title")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Notes or details for this agenda item.
        /// Maps to: notes TEXT NULL
        /// </summary>
        [Column("notes")]
        public string? Notes { get; set; }

        /// <summary>
        /// Estimated time for this item in minutes.
        /// Maps to: time_estimate_minutes INT NULL
        /// </summary>
        [Column("time_estimate_minutes")]
        public int? TimeEstimateMinutes { get; set; }

        /// <summary>
        /// Sort order within the template.
        /// Maps to: sort_order INT NOT NULL DEFAULT 0
        /// </summary>
        [Column("sort_order")]
        public int SortOrder { get; set; }

        /// <summary>
        /// When this record was created.
        /// Maps to: created_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this record was last updated.
        /// Maps to: updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
        /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        #region Navigation Properties

        /// <summary>
        /// Parent template.
        /// </summary>
        [NotMapped]
        public MeetingTemplate? Template { get; set; }

        #endregion
    }
}

