# Notifications Tables (procohere schema)

This document describes the notification-related tables in the `procohere` schema.

## Tables

- [notifications](#notifications) - In-app notifications for team members

---

## notifications

In-app notifications for team members. Supports linking to related entities for navigation.

### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `id` | uuid | NO | Primary key |
| `organization_id` | uuid | NO | FK to organizations |
| `team_member_id` | uuid | NO | FK to team_members (recipient) |
| `notification_type` | text | NO | Type of notification |
| `title` | text | NO | Notification title |
| `message` | text | YES | Detailed message text |
| `entity_type` | text | YES | Related entity type for navigation |
| `entity_id` | uuid | YES | Related entity ID for navigation |
| `is_read` | boolean | NO | Whether user has read it |
| `read_at` | timestamptz | YES | When it was read |
| `is_deleted` | boolean | NO | Soft delete flag |
| `created_at` | timestamptz | NO | Record creation time |
| `updated_at` | timestamptz | NO | Last modification time |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | Who soft deleted |

### C# Model

```csharp
[Table("notifications")]
public class Notification : BaseModel
```

**File**: `ProCohere.Avalonia/Models/Notification.cs`

### Notification Types

| Type | Description |
|------|-------------|
| `meeting_reminder` | Upcoming meeting reminder |
| `meeting_invite` | Invited to a meeting |
| `task_due` | Task deadline approaching |
| `task_assigned` | Task assigned to user |
| `feedback_received` | Received feedback |
| `goal_update` | Goal status changed |
| `review_pending` | Performance review pending |
| `kudos_received` | Received recognition |

### Entity Types for Navigation

| Entity Type | Navigates To |
|-------------|--------------|
| `meeting` | Meeting details |
| `task` | Task details |
| `goal` | Goal details |
| `feedback` | Feedback details |
| `review` | Performance review |

### Computed Properties

- `HasEntity` - Whether notification links to an entity
- `TimeAgo` - Relative time display (e.g., "5m ago", "2d ago")

---

## Relationships

```
organizations
    └── notifications (1:N)

team_members
    └── notifications (1:N via team_member_id)

notifications
    └── meetings (via entity_type='meeting', entity_id)
    └── tasks (via entity_type='task', entity_id)
    └── ... (polymorphic)
```

## Notes

- Notification preferences are stored in `user_settings` (email_notifications, push_notifications, etc.)
- No separate `notification_settings` table exists
- Notifications should be cleaned up periodically (e.g., delete read notifications older than 30 days)
