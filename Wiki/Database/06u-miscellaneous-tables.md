# Miscellaneous Tables (procohere schema)

This document describes miscellaneous tables that provide cross-cutting functionality.

## Tables

- [activity_feed](#activity_feed) - User-facing activity stream
- [comments](#comments) - Polymorphic comments on entities
- [entity_tags](#entity_tags) - Join table for tagging entities
- [goal_categories](#goal_categories) - Goal categorization
- [goal_metrics](#goal_metrics) - Goal-to-metric associations
- [goal_templates](#goal_templates) - Reusable goal templates

---

## activity_feed

User-facing activity stream showing recent actions. Different from `audit_log` which is system-level.

### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `id` | uuid | NO | Primary key |
| `organization_id` | uuid | NO | FK to organizations |
| `actor_id` | uuid | NO | FK to team_members (who acted) |
| `action` | text | NO | Action: 'created', 'updated', 'completed', etc. |
| `entity_type` | text | NO | Type of entity affected |
| `entity_id` | uuid | NO | ID of affected entity |
| `entity_title` | text | YES | Display title at time of activity |
| `metadata` | jsonb | YES | Additional context as JSON |
| `is_deleted` | boolean | NO | Soft delete flag |
| `created_at` | timestamptz | NO | When action occurred |

**File**: `ProCohere.Avalonia/Models/Miscellaneous.cs`

---

## comments

Polymorphic comments on any entity. Supports threaded replies.

### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `id` | uuid | NO | Primary key |
| `organization_id` | uuid | NO | FK to organizations |
| `author_id` | uuid | NO | FK to team_members (author) |
| `entity_type` | text | NO | Type: 'goal', 'task', 'meeting', etc. |
| `entity_id` | uuid | NO | ID of commented entity |
| `parent_comment_id` | uuid | YES | FK to comments (for replies) |
| `content` | text | NO | Comment text |
| `is_deleted` | boolean | NO | Soft delete flag |
| `created_at` | timestamptz | NO | Record creation time |
| `updated_at` | timestamptz | NO | Last modification time |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | Who soft deleted |

**File**: `ProCohere.Avalonia/Models/Miscellaneous.cs`

---

## entity_tags

Join table linking tags to any entity type (polymorphic many-to-many).

### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `id` | uuid | NO | Primary key |
| `organization_id` | uuid | NO | FK to organizations |
| `tag_id` | uuid | NO | FK to tags |
| `entity_type` | text | NO | Type: 'goal', 'task', 'meeting', etc. |
| `entity_id` | uuid | NO | ID of tagged entity |
| `is_deleted` | boolean | NO | Soft delete flag |
| `created_at` | timestamptz | NO | Record creation time |
| `updated_at` | timestamptz | NO | Last modification time |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | Who soft deleted |

**File**: `ProCohere.Avalonia/Models/Miscellaneous.cs`

---

## goal_categories

Organization-defined categories for grouping goals.

### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `id` | uuid | NO | Primary key |
| `organization_id` | uuid | NO | FK to organizations |
| `name` | text | NO | Category name |
| `description` | text | YES | Category description |
| `color` | text | YES | Color hex code |
| `sort_order` | integer | NO | Display order |
| `is_deleted` | boolean | NO | Soft delete flag |
| `created_at` | timestamptz | NO | Record creation time |
| `updated_at` | timestamptz | NO | Last modification time |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | Who soft deleted |

**File**: `ProCohere.Avalonia/Models/Miscellaneous.cs`

---

## goal_metrics

Join table linking goals to metrics. A goal can track multiple metrics.

### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `id` | uuid | NO | Primary key |
| `organization_id` | uuid | NO | FK to organizations |
| `goal_id` | uuid | NO | FK to goals |
| `metric_id` | uuid | NO | FK to metrics |
| `is_primary` | boolean | NO | Whether this is the primary metric |
| `sort_order` | integer | NO | Display order |
| `is_deleted` | boolean | NO | Soft delete flag |
| `created_at` | timestamptz | NO | Record creation time |
| `updated_at` | timestamptz | NO | Last modification time |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | Who soft deleted |

**File**: `ProCohere.Avalonia/Models/GoalMetricAssociation.cs`

---

## goal_templates

Reusable templates for creating goals with predefined structure.

### Columns

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `id` | uuid | NO | Primary key |
| `organization_id` | uuid | NO | FK to organizations |
| `created_by` | uuid | NO | FK to team_members |
| `category_id` | uuid | YES | FK to goal_categories |
| `name` | text | NO | Template name |
| `description` | text | YES | Template description |
| `goal_type` | text | NO | Type: 'individual', 'team', 'company' |
| `default_targets` | jsonb | YES | Default targets as JSON |
| `is_system_template` | boolean | NO | Built-in template flag |
| `is_deleted` | boolean | NO | Soft delete flag |
| `created_at` | timestamptz | NO | Record creation time |
| `updated_at` | timestamptz | NO | Last modification time |
| `deleted_at` | timestamptz | YES | When soft deleted |
| `deleted_by` | uuid | YES | Who soft deleted |

**File**: `ProCohere.Avalonia/Models/Miscellaneous.cs`

---

## Relationships

```
tags
    └── entity_tags (1:N)
          └── goals, tasks, meetings, etc. (polymorphic)

goals
    └── goal_categories (N:1)
    └── goal_metrics (1:N)
          └── metrics (N:1)

goal_templates
    └── goal_categories (N:1)
```
