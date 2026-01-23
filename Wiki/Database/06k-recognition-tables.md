# 06k – Recognition Tables

This document covers the **Recognition** domain tables in the `procohere` schema.

---

## procohere.kudos

**Purpose**  
Peer or manager recognition messages. Allows team members to publicly or privately recognize each other.

**Columns**

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| id | uuid | NO | gen_random_uuid() | Primary key |
| organization_id | uuid | NO | - | FK → public.organizations.id |
| from_member_id | uuid | NO | - | FK → procohere.team_members.id (sender) |
| to_member_id | uuid | NO | - | FK → procohere.team_members.id (recipient) |
| message | text | NO | - | Recognition message |
| category | text | YES | - | Category: 'teamwork', 'innovation', 'leadership', etc. |
| is_public | boolean | NO | false | Whether visible to entire org |
| is_deleted | boolean | NO | false | Soft delete flag |
| created_at | timestamptz | NO | now() | Creation timestamp |
| updated_at | timestamptz | NO | now() | Last update timestamp |
| deleted_at | timestamptz | YES | - | Deletion timestamp |
| deleted_by | uuid | YES | - | FK → public.users.id |

**RLS**  
Visible to sender, recipient, and management chain. Public kudos visible to entire org.

**Model**: `ProCohere.Avalonia.Models.Kudos`

---

## Related Models

Model in `ProCohere.Avalonia/Models/Kudos.cs`:
- `Kudos` - Recognition message between team members
