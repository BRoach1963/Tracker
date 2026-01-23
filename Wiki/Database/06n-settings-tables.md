# Settings Tables (procohere schema)

This document describes the settings-related tables in the `procohere` schema.

## Tables

- [user_settings](#user_settings) - Per-user preferences and notifications
- [org_settings](#org_settings) - Organization-wide configuration
- [calendar_integrations](#calendar_integrations) - External calendar OAuth tokens

---

## user_settings

Per-user preferences and notification settings.

### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `id` | uuid | NO | Primary key |
| `organization_id` | uuid | NO | FK to organizations |
| `team_member_id` | uuid | NO | FK to team_members |
| `theme` | text | YES | UI theme: 'light', 'dark', 'system' |
| `email_notifications` | boolean | NO | Enable email notifications |
| `push_notifications` | boolean | NO | Enable push notifications |
| `meeting_reminders` | boolean | NO | Enable meeting reminders |
| `task_reminders` | boolean | NO | Enable task reminders |
| `weekly_digest` | boolean | NO | Enable weekly digest emails |
| `default_meeting_duration` | integer | YES | Default meeting length (minutes) |
| `timezone` | text | YES | IANA timezone (e.g., 'America/New_York') |
| `locale` | text | YES | Locale code (e.g., 'en-US') |
| `settings_json` | jsonb | NO | Extended settings as JSON |
| `is_deleted` | boolean | NO | Soft delete flag |
| `created_at` | timestamptz | NO | Record creation time |
| `updated_at` | timestamptz | NO | Last modification time |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | Who soft deleted |

### C# Model

```csharp
[Table("user_settings")]
public class UserSettings : BaseModel
```

**File**: `ProCohere.Avalonia/Models/Settings.cs`

---

## org_settings

Organization-wide configuration and defaults.

### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `id` | uuid | NO | Primary key |
| `organization_id` | uuid | NO | FK to organizations (unique) |
| `default_meeting_duration` | integer | YES | Org default meeting length (minutes) |
| `meeting_reminder_minutes` | integer | YES | Minutes before meeting for reminders |
| `require_agenda` | boolean | NO | Require agenda for meetings |
| `require_notes` | boolean | NO | Require notes for meetings |
| `enable_ai_features` | boolean | NO | Enable AI features for org |
| `enable_anonymous_feedback` | boolean | NO | Allow anonymous feedback |
| `fiscal_year_start_month` | integer | YES | Fiscal year start (1-12) |
| `goal_cycle_type` | text | YES | Goal cycle: 'annual', 'quarterly', 'monthly' |
| `settings_json` | jsonb | NO | Extended settings as JSON |
| `is_deleted` | boolean | NO | Soft delete flag |
| `created_at` | timestamptz | NO | Record creation time |
| `updated_at` | timestamptz | NO | Last modification time |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | Who soft deleted |

### C# Model

```csharp
[Table("org_settings")]
public class OrgSettings : BaseModel
```

**File**: `ProCohere.Avalonia/Models/Settings.cs`

---

## calendar_integrations

OAuth tokens and sync state for external calendar providers (Google, Microsoft, Apple).

### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `id` | uuid | NO | Primary key |
| `organization_id` | uuid | NO | FK to organizations |
| `team_member_id` | uuid | NO | FK to team_members |
| `provider` | text | NO | Provider: 'google', 'microsoft', 'apple' |
| `external_account_id` | text | YES | Account ID from provider |
| `access_token` | text | YES | OAuth access token (encrypted) |
| `refresh_token` | text | YES | OAuth refresh token (encrypted) |
| `token_expires_at` | timestamptz | YES | Access token expiration |
| `sync_enabled` | boolean | NO | Whether sync is active |
| `last_synced_at` | timestamptz | YES | Last successful sync time |
| `is_deleted` | boolean | NO | Soft delete flag |
| `created_at` | timestamptz | NO | Record creation time |
| `updated_at` | timestamptz | NO | Last modification time |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | Who soft deleted |

### C# Model

```csharp
[Table("calendar_integrations")]
public class CalendarIntegration : BaseModel
```

**File**: `ProCohere.Avalonia/Models/Settings.cs`

### Computed Properties

- `IsTokenExpired` - Whether the access token has expired
- `NeedsRefresh` - Whether token refresh is needed
- `ProviderDisplay` - Human-readable provider name

---

## Relationships

```
organizations
    └── org_settings (1:1)
    └── user_settings (1:N via team_members)
    └── calendar_integrations (1:N via team_members)

team_members
    └── user_settings (1:1)
    └── calendar_integrations (1:N)
```

## Security Notes

- **OAuth tokens** (access_token, refresh_token) should be encrypted at rest
- RLS policies restrict access to own settings only
- Organization admins can view/modify org_settings
