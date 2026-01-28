# Projects Implementation Notes

This document describes the actual implementation of Projects in ProCohere, including any deviations from the original spec in [17-projects.md](17-projects.md).

---

## Implementation Summary

Projects was implemented 2026-01-XX as a sub-tab under **Pulse** (alongside Goals, Metrics, Tasks).

### Files Created/Modified

| File | Type | Description |
|------|------|-------------|
| `Models/Project.cs` | Model | Project, ProjectMember, ProjectLink classes |
| `Services/ProjectService.cs` | Service | CRUD operations via Supabase RPCs |
| `ViewModels/ProjectsViewModel.cs` | ViewModel | UI state, filtering, commands |
| `Views/ProjectsView.axaml` | View | Project list, detail panel, editor flyout |
| `Views/ProjectsView.axaml.cs` | Code-behind | Event handlers |
| `ViewModels/PulseViewModel.cs` | Modified | Added ProjectsViewModel integration |
| `Views/PulseView.axaml` | Modified | Added Projects tab |
| `Views/PulseView.axaml.cs` | Modified | Wired up ProjectsTab visibility |

---

## Schema Deviations from Original Spec

The actual database schema differs slightly from the original spec:

### `procohere.projects` Table

| Spec Column | Actual Column | Notes |
|-------------|---------------|-------|
| `title` | `name` | Renamed for consistency with other entities |
| `target_date` | `due_date` | More intuitive naming |
| `status` values: `active\|on_hold\|completed` | `active\|paused\|completed` | `on_hold` → `paused` |
| `is_archived`, `archived_at` | ❌ Not implemented | Archive feature deferred |
| `start_date` | ❌ Not implemented | Not needed for MVP |

### `procohere.project_members` Table

| Spec Column | Actual Column | Notes |
|-------------|---------------|-------|
| `role` values: `member\|viewer` | `member\|lead\|viewer` | Added `lead` role |

### `procohere.project_links` Table

| Spec Field | Actual Field | Notes |
|------------|--------------|-------|
| `entity_type` values | Extended list | Now includes: `goal`, `metric`, `target`, `task`, `meeting`, `feedback`, `note`, `chronicle_entry` |

---

## C# Model Classes

### Project.cs

```csharp
public class Project : BaseModel
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid OwnerTeamMemberId { get; set; }
    
    public string Name { get; set; }           // Was 'title' in spec
    public string? Description { get; set; }
    public string Status { get; set; }         // active|paused|completed
    public DateTime? DueDate { get; set; }     // Was 'target_date' in spec
    
    // Soft delete fields
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    
    // Navigation (from RPCs)
    public List<ProjectMember>? Members { get; set; }
    public List<ProjectLink>? Links { get; set; }
    public TeamMemberDetail? Owner { get; set; }
}
```

### Status Constants

```csharp
public static class ProjectStatus
{
    public const string Active = "active";
    public const string Paused = "paused";       // Was 'on_hold' in spec
    public const string Completed = "completed";
}
```

### Member Role Constants

```csharp
public static class ProjectMemberRole
{
    public const string Member = "member";
    public const string Lead = "lead";           // Not in original spec
    public const string Viewer = "viewer";
}
```

### Link Entity Type Constants

```csharp
public static class ProjectLinkEntityType
{
    public const string Goal = "goal";
    public const string Metric = "metric";
    public const string Target = "target";
    public const string Task = "task";
    public const string Meeting = "meeting";
    public const string Feedback = "feedback";
    public const string Note = "note";
    public const string ChronicleEntry = "chronicle_entry";
}
```

---

## RPCs Implemented

All CRUD operations use RPCs with `SECURITY DEFINER` for proper RLS context.

| RPC | Description | Parameters |
|-----|-------------|------------|
| `rpc_create_project` | Create new project | `p_name`, `p_description`, `p_status`, `p_due_date` |
| `rpc_update_project` | Update project | `p_id`, `p_name`, `p_description`, `p_status`, `p_due_date` |
| `rpc_delete_project` | Soft-delete project | `p_id` |
| `rpc_add_project_member` | Add member | `p_project_id`, `p_team_member_id`, `p_role` |
| `rpc_remove_project_member` | Soft-delete member | `p_project_member_id` |
| `rpc_add_project_link` | Add entity link | `p_project_id`, `p_entity_type`, `p_entity_id`, `p_entity_title_snapshot` |
| `rpc_remove_project_link` | Soft-delete link | `p_project_link_id` |

### Query Pattern

Projects are queried directly from `procohere.projects` with RLS enforcing visibility:
- User sees projects they own OR are a member of

---

## UI Features

### ProjectsView Layout

1. **Header**: Title + stats (total, active, completed counts)
2. **Filter Tabs**: All | Active | Paused | Completed
3. **Project Cards**: Clickable cards showing name, status badge, description, member count, due date
4. **Detail Panel**: Slide-in panel showing full project details
5. **Editor Flyout**: Modal dialog for create/edit with name, description, status, due date fields

### Status Badge Colors

| Status | Color | Icon |
|--------|-------|------|
| Active | Blue (#3B82F6) | Play icon |
| Paused | Amber (#F59E0B) | Pause icon |
| Completed | Green (#22C55E) | Check icon |

### Navigation

Projects is accessed via **Pulse** → **Projects tab** (Ctrl+3 → Projects)

---

## Future Enhancements

1. **Member Management UI**: Add/remove members from detail panel
2. **Link Management UI**: Link entities from detail panel
3. **Archive Support**: If needed, add archive/unarchive functionality
4. **Project Status Meetings**: Pre-populate meeting attendees from project members
5. **Project Activity Feed**: Show recent changes/updates

---

## See Also

- [17-projects.md](17-projects.md) - Original specification
- [07-models-reference.md](../../ProCohere.Avalonia/07-models-reference.md) - Models documentation
- [06-services-reference.md](../../ProCohere.Avalonia/06-services-reference.md) - Services documentation
