using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// User settings model - maps to the user_settings table in Supabase procohere schema.
/// Per-user preferences and notification settings.
/// </summary>
[Table("user_settings")]
public class UserSettings : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("team_member_id")]
    public Guid TeamMemberId { get; set; }

    #endregion

    #region Preferences

    /// <summary>
    /// UI theme preference: 'light', 'dark', 'system'.
    /// </summary>
    [Column("theme")]
    public string? Theme { get; set; }

    /// <summary>
    /// User's timezone (IANA format, e.g., 'America/New_York').
    /// </summary>
    [Column("timezone")]
    public string? Timezone { get; set; }

    /// <summary>
    /// User's locale (e.g., 'en-US').
    /// </summary>
    [Column("locale")]
    public string? Locale { get; set; }

    /// <summary>
    /// Default meeting duration in minutes.
    /// </summary>
    [Column("default_meeting_duration")]
    public int? DefaultMeetingDuration { get; set; }

    #endregion

    #region Notification Settings

    [Column("email_notifications")]
    public bool EmailNotifications { get; set; } = true;

    [Column("push_notifications")]
    public bool PushNotifications { get; set; } = true;

    [Column("meeting_reminders")]
    public bool MeetingReminders { get; set; } = true;

    [Column("task_reminders")]
    public bool TaskReminders { get; set; } = true;

    [Column("weekly_digest")]
    public bool WeeklyDigest { get; set; } = true;

    #endregion

    #region Extended Settings

    /// <summary>
    /// Additional settings stored as JSON for extensibility.
    /// </summary>
    [Column("settings_json")]
    public string SettingsJson { get; set; } = "{}";

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
}

/// <summary>
/// Organization settings model - maps to the org_settings table in Supabase procohere schema.
/// Organization-wide configuration and defaults.
/// </summary>
[Table("org_settings")]
public class OrgSettings : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    #endregion

    #region Meeting Defaults

    /// <summary>
    /// Default meeting duration in minutes.
    /// </summary>
    [Column("default_meeting_duration")]
    public int? DefaultMeetingDuration { get; set; }

    /// <summary>
    /// Minutes before meeting to send reminders.
    /// </summary>
    [Column("meeting_reminder_minutes")]
    public int? MeetingReminderMinutes { get; set; }

    /// <summary>
    /// Whether meetings require an agenda.
    /// </summary>
    [Column("require_agenda")]
    public bool RequireAgenda { get; set; }

    /// <summary>
    /// Whether meetings require notes.
    /// </summary>
    [Column("require_notes")]
    public bool RequireNotes { get; set; }

    #endregion

    #region Feature Flags

    /// <summary>
    /// Whether AI features are enabled for the organization.
    /// </summary>
    [Column("enable_ai_features")]
    public bool EnableAiFeatures { get; set; } = true;

    /// <summary>
    /// Whether anonymous feedback is allowed.
    /// </summary>
    [Column("enable_anonymous_feedback")]
    public bool EnableAnonymousFeedback { get; set; }

    #endregion

    #region Fiscal & Goals

    /// <summary>
    /// Month when fiscal year starts (1-12).
    /// </summary>
    [Column("fiscal_year_start_month")]
    public int? FiscalYearStartMonth { get; set; }

    /// <summary>
    /// Goal cycle type: 'annual', 'quarterly', 'monthly'.
    /// </summary>
    [Column("goal_cycle_type")]
    public string? GoalCycleType { get; set; }

    #endregion

    #region Extended Settings

    /// <summary>
    /// Additional settings stored as JSON for extensibility.
    /// </summary>
    [Column("settings_json")]
    public string SettingsJson { get; set; } = "{}";

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
}

/// <summary>
/// Calendar integration model - maps to the calendar_integrations table in Supabase procohere schema.
/// Stores OAuth tokens and sync state for external calendar providers.
/// </summary>
[Table("calendar_integrations")]
public class CalendarIntegration : BaseModel
{
    #region Identity

    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("team_member_id")]
    public Guid TeamMemberId { get; set; }

    #endregion

    #region Provider Info

    /// <summary>
    /// Calendar provider: 'google', 'microsoft', 'apple'.
    /// </summary>
    [Column("provider")]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// External account identifier from the provider.
    /// </summary>
    [Column("external_account_id")]
    public string? ExternalAccountId { get; set; }

    #endregion

    #region OAuth Tokens

    /// <summary>
    /// OAuth access token (encrypted at rest).
    /// </summary>
    [Column("access_token")]
    public string? AccessToken { get; set; }

    /// <summary>
    /// OAuth refresh token (encrypted at rest).
    /// </summary>
    [Column("refresh_token")]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// When the access token expires.
    /// </summary>
    [Column("token_expires_at")]
    public DateTime? TokenExpiresAt { get; set; }

    #endregion

    #region Sync State

    /// <summary>
    /// Whether calendar sync is enabled.
    /// </summary>
    [Column("sync_enabled")]
    public bool SyncEnabled { get; set; } = true;

    /// <summary>
    /// When the calendar was last synced.
    /// </summary>
    [Column("last_synced_at")]
    public DateTime? LastSyncedAt { get; set; }

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

    public bool IsTokenExpired => TokenExpiresAt.HasValue && TokenExpiresAt.Value < DateTime.UtcNow;

    public bool NeedsRefresh => IsTokenExpired || string.IsNullOrEmpty(AccessToken);

    public string ProviderDisplay => Provider switch
    {
        "google" => "Google Calendar",
        "microsoft" => "Microsoft Outlook",
        "apple" => "Apple Calendar",
        _ => Provider
    };

    #endregion
}
