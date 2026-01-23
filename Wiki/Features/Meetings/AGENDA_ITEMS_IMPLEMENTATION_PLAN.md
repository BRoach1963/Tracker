# Agenda Items & Outcomes Implementation Plan

**Document Created:** January 20, 2026  
**Last Updated:** January 20, 2026  
**Status:** ✅ Phase 1-3 Complete, Phase 4-5 Pending  
**Estimated Effort:** 4-5 development days

---

## Implementation Progress

| Phase | Description | Status |
|-------|-------------|--------|
| Phase 1 | Schema & Model Foundation | ✅ Complete |
| Phase 2 | Services Layer | ✅ Complete |
| Phase 3 | UI - AgendaItemCard | ✅ Complete |
| Phase 4 | UI - Dialogs | ⏳ Pending |
| Phase 5 | Meeting Templates | ⏳ Pending |

### Completed Files

**Phase 1 - Models:**
- `New Docs/SupaBase SQL Scrips/20260120_agenda_items_outcomes.sql` - Migration script
- `Models/AgendaItemOutcome.cs` - OutcomeType, CarryForwardState, OutcomeVisibility constants
- `Models/AgendaItemOutcomeDetail.cs` - Supabase model for outcomes table
- `Models/MeetingDetail.cs` - Updated MeetingAgendaItem with carry-forward properties

**Phase 2 - Services:**
- `Services/AgendaItemOutcomeService.cs` - CRUD for outcomes (decisions, feedback, notes)
- `Services/CarryForwardService.cs` - Deferral lifecycle, expiration, surfacing
- `Services/MeetingAgendaItemService.cs` - Added LinkToEntityAsync, UnlinkEntityAsync

**Phase 3 - UI:**
- `Views/Controls/AgendaItemCard.axaml` - Progressive disclosure card with tabs
- `Views/Controls/AgendaItemCard.axaml.cs` - Card code-behind with events
- `Views/Controls/MeetingDetailFlyout.axaml` - Updated to use AgendaItemCard
- `Views/Controls/MeetingDetailFlyout.axaml.cs` - Added event handlers

---

## Overview

This plan addresses all gaps identified in [AGENDA_ITEMS_VALIDATION.md](AGENDA_ITEMS_VALIDATION.md) and provides a phased approach to implement the complete agenda items feature set as specified.

---

## Phase 1: Schema & Model Foundation (Day 1)

### 1.1 Database Schema Changes

**File:** `New Docs/SupaBase SQL Scrips/20260120_agenda_items_outcomes.sql`

```sql
-- ============================================================
-- AGENDA ITEM ENHANCEMENTS
-- ============================================================

-- 1. Add carry-forward tracking columns to meeting_agenda_items
ALTER TABLE procohere.meeting_agenda_items
ADD COLUMN IF NOT EXISTS anchor_team_member_id uuid REFERENCES procohere.team_members(id),
ADD COLUMN IF NOT EXISTS carry_forward_state text DEFAULT NULL,
ADD COLUMN IF NOT EXISTS carry_forward_expires_at timestamptz,
ADD COLUMN IF NOT EXISTS carry_forward_meeting_count int DEFAULT 0,
ADD COLUMN IF NOT EXISTS source_agenda_item_id uuid REFERENCES procohere.meeting_agenda_items(id);

-- Carry forward states: 'pending', 'surfaced', 'resolved', 'converted', 'expired'
ALTER TABLE procohere.meeting_agenda_items
ADD CONSTRAINT chk_carry_forward_state 
CHECK (carry_forward_state IS NULL OR carry_forward_state IN (
    'pending', 'surfaced', 'resolved', 'converted', 'expired'
));

COMMENT ON COLUMN procohere.meeting_agenda_items.anchor_team_member_id IS 
    'Person this carry-forward is anchored to. Required when status=deferred.';
COMMENT ON COLUMN procohere.meeting_agenda_items.carry_forward_state IS 
    'Lifecycle state for carried-forward items: pending, surfaced, resolved, converted, expired.';
COMMENT ON COLUMN procohere.meeting_agenda_items.carry_forward_expires_at IS 
    'When this carry-forward expires (30 days from deferral or 2 meetings).';
COMMENT ON COLUMN procohere.meeting_agenda_items.carry_forward_meeting_count IS 
    'Number of meeting opportunities since deferral. Expires at 2.';
COMMENT ON COLUMN procohere.meeting_agenda_items.source_agenda_item_id IS 
    'If this item was carried forward, points to the original agenda item.';

-- Index for carry-forward queries
CREATE INDEX IF NOT EXISTS idx_agenda_items_carry_forward
ON procohere.meeting_agenda_items(organization_id, anchor_team_member_id, carry_forward_state)
WHERE is_deleted = false AND carry_forward_state IS NOT NULL;

-- ============================================================
-- AGENDA ITEM OUTCOMES TABLE
-- ============================================================

CREATE TABLE IF NOT EXISTS procohere.agenda_item_outcomes (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id     uuid NOT NULL REFERENCES public.organizations(id),
    agenda_item_id      uuid NOT NULL REFERENCES procohere.meeting_agenda_items(id),
    outcome_type        text NOT NULL,
    
    -- For task/goal/meeting outcomes, link to the created entity
    linked_entity_type  text,
    linked_entity_id    uuid,
    
    -- For decision/feedback/notes outcomes, store content inline
    content             text,
    visibility          text NOT NULL DEFAULT 'attendees', -- 'private', 'attendees', 'team', 'organization'
    
    -- Metadata
    created_by          uuid NOT NULL REFERENCES procohere.team_members(id),
    is_deleted          boolean NOT NULL DEFAULT false,
    created_at          timestamptz NOT NULL DEFAULT now(),
    updated_at          timestamptz NOT NULL DEFAULT now(),
    deleted_at          timestamptz,
    deleted_by          uuid REFERENCES public.users(id)
);

-- Outcome types: 'task_created', 'goal_created', 'goal_updated', 'follow_up_scheduled', 
--                'decision_recorded', 'feedback_captured', 'notes_added'
ALTER TABLE procohere.agenda_item_outcomes
ADD CONSTRAINT chk_outcome_type 
CHECK (outcome_type IN (
    'task_created', 
    'goal_created', 
    'goal_updated', 
    'follow_up_scheduled',
    'decision_recorded', 
    'feedback_captured', 
    'notes_added'
));

-- Indexes
CREATE INDEX IF NOT EXISTS idx_outcomes_agenda_item
ON procohere.agenda_item_outcomes(agenda_item_id) WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_outcomes_org
ON procohere.agenda_item_outcomes(organization_id) WHERE is_deleted = false;

-- Trigger for updated_at
DROP TRIGGER IF EXISTS tr_agenda_item_outcomes_set_updated_at ON procohere.agenda_item_outcomes;
CREATE TRIGGER tr_agenda_item_outcomes_set_updated_at
    BEFORE UPDATE ON procohere.agenda_item_outcomes
    FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();

-- RLS
ALTER TABLE procohere.agenda_item_outcomes ENABLE ROW LEVEL SECURITY;

CREATE POLICY org_isolation ON procohere.agenda_item_outcomes
    FOR ALL
    USING (organization_id = procohere.get_user_organization_id());

-- Grants
GRANT SELECT, INSERT, UPDATE, DELETE ON procohere.agenda_item_outcomes TO authenticated;
```

### 1.2 C# Models

**File:** `ProCohere.Avalonia/Models/AgendaItemOutcome.cs` (NEW)

```csharp
namespace ProCohere.Avalonia.Models;

/// <summary>
/// Outcome types for agenda items.
/// </summary>
public static class OutcomeType
{
    public const string TaskCreated = "task_created";
    public const string GoalCreated = "goal_created";
    public const string GoalUpdated = "goal_updated";
    public const string FollowUpScheduled = "follow_up_scheduled";
    public const string DecisionRecorded = "decision_recorded";
    public const string FeedbackCaptured = "feedback_captured";
    public const string NotesAdded = "notes_added";
    
    public static readonly string[] All = {
        TaskCreated, GoalCreated, GoalUpdated, FollowUpScheduled,
        DecisionRecorded, FeedbackCaptured, NotesAdded
    };
    
    public static string GetDisplayName(string? type) => type switch
    {
        TaskCreated => "Task Created",
        GoalCreated => "Goal Created",
        GoalUpdated => "Goal Updated",
        FollowUpScheduled => "Follow-Up Scheduled",
        DecisionRecorded => "Decision Recorded",
        FeedbackCaptured => "Feedback Captured",
        NotesAdded => "Notes Added",
        _ => type ?? "Unknown"
    };
    
    public static string GetIcon(string? type) => type switch
    {
        TaskCreated => "M19,3H5A2,2 0 0,0 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5A2,2 0 0,0 19,3M10,17L5,12L6.41,10.58L10,14.17L17.59,6.58L19,8L10,17Z",
        GoalCreated or GoalUpdated => "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4M12,6A6,6 0 0,0 6,12A6,6 0 0,0 12,18A6,6 0 0,0 18,12A6,6 0 0,0 12,6M12,8A4,4 0 0,1 16,12A4,4 0 0,1 12,16A4,4 0 0,1 8,12A4,4 0 0,1 12,8Z",
        FollowUpScheduled => "M19,19H5V8H19M16,1V3H8V1H6V3H5C3.89,3 3,3.89 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V5C21,3.89 20.1,3 19,3H18V1",
        DecisionRecorded => "M9,22A1,1 0 0,1 8,21V18H4A2,2 0 0,1 2,16V4C2,2.89 2.9,2 4,2H20A2,2 0 0,1 22,4V16A2,2 0 0,1 20,18H13.9L10.2,21.71C10,21.9 9.75,22 9.5,22V22H9Z",
        FeedbackCaptured => "M20,2H4A2,2 0 0,0 2,4V22L6,18H20A2,2 0 0,0 22,16V4A2,2 0 0,0 20,2M6,9H18V11H6M14,14H6V12H14M18,8H6V6H18",
        NotesAdded => "M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M18,20H6V4H13V9H18V20Z",
        _ => string.Empty
    };
}

/// <summary>
/// Carry-forward states for deferred agenda items.
/// </summary>
public static class CarryForwardState
{
    public const string Pending = "pending";
    public const string Surfaced = "surfaced";
    public const string Resolved = "resolved";
    public const string Converted = "converted";
    public const string Expired = "expired";
}

/// <summary>
/// Visibility levels for outcomes and notes.
/// </summary>
public static class OutcomeVisibility
{
    public const string Private = "private";        // Only creator
    public const string Attendees = "attendees";    // Meeting attendees only
    public const string Team = "team";              // Creator's team
    public const string Organization = "organization"; // Entire org
}
```

**File:** `ProCohere.Avalonia/Models/AgendaItemOutcomeDetail.cs` (NEW)

```csharp
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ProCohere.Avalonia.Models;

/// <summary>
/// Outcome record for an agenda item discussion.
/// Maps to procohere.agenda_item_outcomes table.
/// </summary>
[Table("agenda_item_outcomes")]
public class AgendaItemOutcomeDetail : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("agenda_item_id")]
    public Guid AgendaItemId { get; set; }

    [Column("outcome_type")]
    public string OutcomeType { get; set; } = string.Empty;

    [Column("linked_entity_type")]
    public string? LinkedEntityType { get; set; }

    [Column("linked_entity_id")]
    public Guid? LinkedEntityId { get; set; }

    [Column("content")]
    public string? Content { get; set; }

    [Column("visibility")]
    public string Visibility { get; set; } = OutcomeVisibility.Attendees;

    [Column("created_by")]
    public Guid CreatedBy { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #region Computed Properties

    public string OutcomeTypeDisplay => Models.OutcomeType.GetDisplayName(OutcomeType);
    public string OutcomeTypeIcon => Models.OutcomeType.GetIcon(OutcomeType);
    public bool HasLinkedEntity => !string.IsNullOrEmpty(LinkedEntityType) && LinkedEntityId.HasValue;
    public bool HasContent => !string.IsNullOrEmpty(Content);

    public string VisibilityDisplay => Visibility switch
    {
        OutcomeVisibility.Private => "Private",
        OutcomeVisibility.Attendees => "Meeting Attendees",
        OutcomeVisibility.Team => "My Team",
        OutcomeVisibility.Organization => "Organization",
        _ => "Unknown"
    };

    #endregion
}
```

### 1.3 Update MeetingAgendaItem Model

**File:** `ProCohere.Avalonia/Models/MeetingDetail.cs` - Add to `MeetingAgendaItem` class:

```csharp
// Add these properties to MeetingAgendaItem class:

[Column("anchor_team_member_id")]
public Guid? AnchorTeamMemberId { get; set; }

[Column("carry_forward_state")]
public string? CarryForwardState { get; set; }

[Column("carry_forward_expires_at")]
public DateTime? CarryForwardExpiresAt { get; set; }

[Column("carry_forward_meeting_count")]
public int CarryForwardMeetingCount { get; set; }

[Column("source_agenda_item_id")]
public Guid? SourceAgendaItemId { get; set; }

// Navigation property for outcomes (loaded separately)
[Supabase.Postgrest.Attributes.Reference(typeof(AgendaItemOutcomeDetail), 
    ReferenceAttribute.JoinType.Left, false, "agenda_item_id")]
public List<AgendaItemOutcomeDetail> Outcomes { get; set; } = new();

#region Carry Forward Computed Properties

public bool IsCarriedForward => SourceAgendaItemId.HasValue;
public bool HasCarryForwardState => !string.IsNullOrEmpty(CarryForwardState);

public string CarryForwardStateDisplay => CarryForwardState switch
{
    Models.CarryForwardState.Pending => "Pending",
    Models.CarryForwardState.Surfaced => "Surfaced",
    Models.CarryForwardState.Resolved => "Resolved",
    Models.CarryForwardState.Converted => "Converted",
    Models.CarryForwardState.Expired => "Expired",
    _ => string.Empty
};

public bool IsCarryForwardExpired => 
    CarryForwardExpiresAt.HasValue && DateTime.UtcNow > CarryForwardExpiresAt.Value;

public int DaysUntilExpiration => CarryForwardExpiresAt.HasValue 
    ? Math.Max(0, (int)(CarryForwardExpiresAt.Value - DateTime.UtcNow).TotalDays)
    : 0;

#endregion
```

---

## Phase 2: Services Layer (Day 2)

### 2.1 Agenda Item Outcomes Service

**File:** `ProCohere.Avalonia/Services/AgendaItemOutcomeService.cs` (NEW)

```csharp
namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for managing agenda item outcomes.
/// </summary>
public class AgendaItemOutcomeService
{
    // Singleton pattern (similar to other services)
    
    /// <summary>
    /// Records a decision outcome for an agenda item.
    /// </summary>
    public async Task<AgendaItemOutcomeDetail?> RecordDecisionAsync(
        Guid agendaItemId,
        string decisionContent,
        string visibility = OutcomeVisibility.Attendees);

    /// <summary>
    /// Records feedback captured during discussion.
    /// </summary>
    public async Task<AgendaItemOutcomeDetail?> RecordFeedbackAsync(
        Guid agendaItemId,
        string feedbackContent,
        string visibility = OutcomeVisibility.Attendees);

    /// <summary>
    /// Records notes from the discussion.
    /// </summary>
    public async Task<AgendaItemOutcomeDetail?> RecordNotesAsync(
        Guid agendaItemId,
        string notesContent,
        string visibility = OutcomeVisibility.Attendees);

    /// <summary>
    /// Records that a task was created from this agenda item.
    /// Called automatically when CreateTaskFromAgendaItemAsync succeeds.
    /// </summary>
    public async Task<AgendaItemOutcomeDetail?> RecordTaskCreatedAsync(
        Guid agendaItemId,
        Guid taskId);

    /// <summary>
    /// Records that a follow-up meeting was scheduled.
    /// </summary>
    public async Task<AgendaItemOutcomeDetail?> RecordFollowUpScheduledAsync(
        Guid agendaItemId,
        Guid meetingId);

    /// <summary>
    /// Gets all outcomes for an agenda item.
    /// </summary>
    public async Task<List<AgendaItemOutcomeDetail>> GetOutcomesAsync(Guid agendaItemId);
}
```

### 2.2 Carry Forward Service

**File:** `ProCohere.Avalonia/Services/CarryForwardService.cs` (NEW)

```csharp
namespace ProCohere.Avalonia.Services;

/// <summary>
/// Service for managing agenda item carry-forward lifecycle.
/// </summary>
public class CarryForwardService
{
    // Singleton pattern
    
    /// <summary>
    /// Defers an agenda item to a future meeting.
    /// Creates a new agenda item in the target meeting linked to the original.
    /// </summary>
    /// <param name="agendaItemId">The item being deferred</param>
    /// <param name="targetMeetingId">The meeting to carry forward to</param>
    /// <param name="anchorTeamMemberId">Person this is anchored to</param>
    public async Task<MeetingAgendaItem?> DeferToMeetingAsync(
        Guid agendaItemId,
        Guid targetMeetingId,
        Guid anchorTeamMemberId);

    /// <summary>
    /// Gets pending carry-forward items for a person.
    /// Used to suggest items when preparing for a meeting.
    /// </summary>
    public async Task<List<MeetingAgendaItem>> GetPendingCarryForwardsAsync(
        Guid teamMemberId);

    /// <summary>
    /// Marks a carry-forward as surfaced (shown in meeting prep).
    /// </summary>
    public async Task<bool> MarkSurfacedAsync(Guid agendaItemId);

    /// <summary>
    /// Marks a carry-forward as resolved (discussed successfully).
    /// </summary>
    public async Task<bool> MarkResolvedAsync(Guid agendaItemId);

    /// <summary>
    /// Marks a carry-forward as converted (turned into task/action).
    /// </summary>
    public async Task<bool> MarkConvertedAsync(Guid agendaItemId);

    /// <summary>
    /// Processes expiration for carry-forwards.
    /// Called on app startup and periodically.
    /// </summary>
    public async Task ProcessExpirationsAsync();

    /// <summary>
    /// Increments meeting count for carry-forwards when a meeting with the anchor occurs.
    /// </summary>
    public async Task IncrementMeetingCountAsync(Guid meetingId);
}
```

### 2.3 Update MeetingAgendaItemService

Add methods to existing service:

```csharp
// Add to MeetingAgendaItemService:

/// <summary>
/// Links an agenda item to a goal, metric, or other entity for discussion context.
/// </summary>
public async Task<bool> LinkToEntityAsync(
    Guid agendaItemId,
    string entityType,  // 'goal', 'metric', 'task', 'milestone'
    Guid entityId);

/// <summary>
/// Removes the linked entity from an agenda item.
/// </summary>
public async Task<bool> UnlinkEntityAsync(Guid agendaItemId);

/// <summary>
/// Gets agenda items with their outcomes loaded.
/// </summary>
public async Task<List<MeetingAgendaItem>> GetAgendaItemsWithOutcomesAsync(Guid meetingId);
```

---

## Phase 3: UI Components (Days 3-4)

### 3.1 New UI Components

#### 3.1.1 Expandable Agenda Item Control

**File:** `ProCohere.Avalonia/Views/Controls/AgendaItemCard.axaml` (NEW)

Features:
- Collapsed state: Checkbox, Title, Status badge, indicators
- Expanded state: Description, Linked entity, Notes tab, Outcomes tab
- Click to expand/collapse
- Status change buttons
- Action buttons (Create Task, Record Decision, etc.)

```xml
<!-- Conceptual structure -->
<UserControl x:Class="ProCohere.Avalonia.Views.Controls.AgendaItemCard">
    <Border Classes="agenda-item-card" Classes.expanded="{Binding IsExpanded}">
        <!-- Collapsed Header (always visible) -->
        <Grid>
            <CheckBox IsChecked="{Binding IsCompleted}"/>
            <TextBlock Text="{Binding Title}"/>
            <Border Classes="status-badge"><!-- Status --></Border>
            <!-- Indicators: has notes, has outcomes, is carried forward -->
            <Button Classes="expand-toggle"/>
        </Grid>
        
        <!-- Expanded Content (collapsible) -->
        <StackPanel IsVisible="{Binding IsExpanded}">
            <!-- Linked Entity Summary -->
            <Border Classes="linked-entity" IsVisible="{Binding HasLinkedEntity}">
                <StackPanel Orientation="Horizontal">
                    <PathIcon Data="{Binding LinkedEntityIcon}"/>
                    <TextBlock Text="{Binding LinkedEntitySummary}"/>
                </StackPanel>
            </Border>
            
            <!-- Tab Control: Notes | Outcomes -->
            <TabControl>
                <TabItem Header="Notes">
                    <!-- Agenda item notes -->
                    <TextBox Text="{Binding Notes}" AcceptsReturn="True"/>
                </TabItem>
                <TabItem Header="Outcomes">
                    <!-- List of outcomes -->
                    <ItemsControl ItemsSource="{Binding Outcomes}">
                        <!-- Outcome cards -->
                    </ItemsControl>
                    <!-- Add outcome buttons -->
                    <StackPanel Orientation="Horizontal">
                        <Button Content="+ Decision"/>
                        <Button Content="+ Feedback"/>
                        <Button Content="+ Note"/>
                    </StackPanel>
                </TabItem>
            </TabControl>
            
            <!-- Actions Row -->
            <StackPanel Orientation="Horizontal">
                <Button Content="Create Task"/>
                <Button Content="Schedule Follow-up"/>
                <Button Content="Defer"/>
            </StackPanel>
        </StackPanel>
    </Border>
</UserControl>
```

#### 3.1.2 Record Outcome Dialog

**File:** `ProCohere.Avalonia/Views/Dialogs/RecordOutcomeDialog.axaml` (NEW)

```xml
<!-- Dialog for recording decisions, feedback, or notes -->
<Window x:Class="ProCohere.Avalonia.Views.Dialogs.RecordOutcomeDialog">
    <StackPanel>
        <TextBlock Text="Record Outcome"/>
        
        <!-- Outcome Type Selection -->
        <ComboBox SelectedItem="{Binding SelectedOutcomeType}">
            <ComboBoxItem Content="Decision"/>
            <ComboBoxItem Content="Feedback"/>
            <ComboBoxItem Content="Notes"/>
        </ComboBox>
        
        <!-- Content -->
        <TextBox Text="{Binding Content}" 
                 AcceptsReturn="True" 
                 Height="150"
                 Watermark="Enter your decision, feedback, or notes..."/>
        
        <!-- Visibility -->
        <ComboBox SelectedItem="{Binding Visibility}">
            <ComboBoxItem Content="Private (only me)"/>
            <ComboBoxItem Content="Meeting Attendees"/>
            <ComboBoxItem Content="My Team"/>
            <ComboBoxItem Content="Organization"/>
        </ComboBox>
        
        <!-- Actions -->
        <StackPanel Orientation="Horizontal">
            <Button Content="Save" Command="{Binding SaveCommand}"/>
            <Button Content="Cancel" Command="{Binding CancelCommand}"/>
        </StackPanel>
    </StackPanel>
</Window>
```

#### 3.1.3 Link Entity Dialog

**File:** `ProCohere.Avalonia/Views/Dialogs/LinkEntityDialog.axaml` (NEW)

```xml
<!-- Dialog for linking agenda item to goal/metric/task -->
<Window x:Class="ProCohere.Avalonia.Views.Dialogs.LinkEntityDialog">
    <StackPanel>
        <TextBlock Text="Link to Entity"/>
        
        <!-- Entity Type Tabs -->
        <TabControl SelectedIndex="{Binding SelectedEntityTypeIndex}">
            <TabItem Header="Goals">
                <ListBox ItemsSource="{Binding Goals}" SelectedItem="{Binding SelectedGoal}"/>
            </TabItem>
            <TabItem Header="Metrics">
                <ListBox ItemsSource="{Binding Metrics}" SelectedItem="{Binding SelectedMetric}"/>
            </TabItem>
            <TabItem Header="Tasks">
                <ListBox ItemsSource="{Binding Tasks}" SelectedItem="{Binding SelectedTask}"/>
            </TabItem>
        </TabControl>
        
        <!-- Actions -->
        <StackPanel Orientation="Horizontal">
            <Button Content="Link" Command="{Binding LinkCommand}"/>
            <Button Content="Cancel" Command="{Binding CancelCommand}"/>
        </StackPanel>
    </StackPanel>
</Window>
```

#### 3.1.4 Defer Agenda Item Dialog

**File:** `ProCohere.Avalonia/Views/Dialogs/DeferAgendaItemDialog.axaml` (NEW)

```xml
<!-- Dialog for deferring/carrying forward an agenda item -->
<Window x:Class="ProCohere.Avalonia.Views.Dialogs.DeferAgendaItemDialog">
    <StackPanel>
        <TextBlock Text="Defer to Future Meeting"/>
        
        <!-- Target Person (Anchor) -->
        <ComboBox ItemsSource="{Binding TeamMembers}" 
                  SelectedItem="{Binding AnchorPerson}"
                  DisplayMemberBinding="{Binding FullName}">
            <ComboBox.Header>
                <TextBlock Text="Anchor to Person"/>
            </ComboBox.Header>
        </ComboBox>
        
        <!-- Target Meeting -->
        <ListBox ItemsSource="{Binding UpcomingMeetings}" 
                 SelectedItem="{Binding TargetMeeting}">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <StackPanel>
                        <TextBlock Text="{Binding Title}"/>
                        <TextBlock Text="{Binding ScheduledDate}" FontSize="10"/>
                    </StackPanel>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>
        
        <!-- Actions -->
        <StackPanel Orientation="Horizontal">
            <Button Content="Defer" Command="{Binding DeferCommand}"/>
            <Button Content="Cancel" Command="{Binding CancelCommand}"/>
        </StackPanel>
    </StackPanel>
</Window>
```

#### 3.1.5 Carry Forward Suggestions Panel

**File:** `ProCohere.Avalonia/Views/Controls/CarryForwardSuggestionsPanel.axaml` (NEW)

```xml
<!-- Shows pending carry-forwards when preparing for a meeting -->
<UserControl x:Class="ProCohere.Avalonia.Views.Controls.CarryForwardSuggestionsPanel">
    <Border Classes="suggestions-panel" IsVisible="{Binding HasPendingCarryForwards}">
        <StackPanel>
            <TextBlock Text="Suggested Topics" Classes="panel-header"/>
            <TextBlock Text="These topics were deferred from previous meetings:" 
                       FontSize="11" Opacity="0.7"/>
            
            <ItemsControl ItemsSource="{Binding PendingCarryForwards}">
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <Border Classes="suggestion-card">
                            <Grid ColumnDefinitions="*,Auto">
                                <StackPanel>
                                    <TextBlock Text="{Binding Title}"/>
                                    <TextBlock Text="{Binding SourceMeetingInfo}" 
                                               FontSize="10" Opacity="0.6"/>
                                </StackPanel>
                                <StackPanel Grid.Column="1" Orientation="Horizontal">
                                    <Button Content="Add" Command="{Binding AddToAgendaCommand}"/>
                                    <Button Content="Dismiss" Command="{Binding DismissCommand}"/>
                                </StackPanel>
                            </Grid>
                        </Border>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </StackPanel>
    </Border>
</UserControl>
```

### 3.2 Update Existing UI

#### 3.2.1 Update MeetingDetailFlyout.axaml

Replace current agenda items section with new expandable cards:

```xml
<!-- Replace current ItemsControl with: -->
<ItemsControl ItemsSource="{Binding AgendaItems}">
    <ItemsControl.ItemTemplate>
        <DataTemplate x:DataType="models:MeetingAgendaItem">
            <controls:AgendaItemCard DataContext="{Binding}"/>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>

<!-- Add carry-forward suggestions before agenda items -->
<controls:CarryForwardSuggestionsPanel 
    DataContext="{Binding CarryForwardSuggestions}"
    IsVisible="{Binding HasCarryForwardSuggestions}"/>
```

### 3.3 ViewModels

#### 3.3.1 AgendaItemCardViewModel

**File:** `ProCohere.Avalonia/ViewModels/AgendaItemCardViewModel.cs` (NEW)

```csharp
public class AgendaItemCardViewModel : ViewModelBase
{
    private MeetingAgendaItem _item;
    private bool _isExpanded;
    private int _selectedTabIndex;
    
    // Properties
    public bool IsExpanded { get; set; }
    public int SelectedTabIndex { get; set; }  // 0 = Notes, 1 = Outcomes
    public ObservableCollection<AgendaItemOutcomeDetail> Outcomes { get; }
    
    // Commands
    public ICommand ToggleExpandedCommand { get; }
    public ICommand CreateTaskCommand { get; }
    public ICommand RecordDecisionCommand { get; }
    public ICommand RecordFeedbackCommand { get; }
    public ICommand AddNotesCommand { get; }
    public ICommand LinkEntityCommand { get; }
    public ICommand DeferCommand { get; }
    public ICommand SetStatusCommand { get; }
    public ICommand ScheduleFollowUpCommand { get; }
}
```

#### 3.3.2 Update CircleViewModel

Add support for new agenda functionality:

```csharp
// Add to CircleViewModel:

// Carry-forward suggestions for selected meeting
public ObservableCollection<MeetingAgendaItem> CarryForwardSuggestions { get; }
public bool HasCarryForwardSuggestions => CarryForwardSuggestions.Count > 0;

// Commands for new features
public ICommand RecordDecisionCommand { get; }
public ICommand RecordFeedbackCommand { get; }
public ICommand LinkAgendaItemCommand { get; }
public ICommand DeferAgendaItemCommand { get; }
public ICommand ScheduleFollowUpCommand { get; }
public ICommand AddCarryForwardToAgendaCommand { get; }
public ICommand DismissCarryForwardCommand { get; }

// Methods
private async Task LoadCarryForwardSuggestionsAsync(Guid meetingId);
private async Task RecordDecisionAsync(MeetingAgendaItem item);
private async Task DeferAgendaItemAsync(MeetingAgendaItem item);
```

---

## Phase 4: Meeting Templates (Day 5)

### 4.1 Template Model for ProCohere.Avalonia

**File:** `ProCohere.Avalonia/Models/MeetingTemplateDetail.cs` (NEW)

```csharp
[Table("meeting_templates")]
public class MeetingTemplateDetail : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    
    [Column("description")]
    public string? Description { get; set; }
    
    [Column("meeting_type")]
    public string MeetingType { get; set; } = "one_on_one";
    
    [Column("suggested_duration_minutes")]
    public int SuggestedDurationMinutes { get; set; } = 30;
    
    // Template items loaded separately
    public List<MeetingTemplateItemDetail> Items { get; set; } = new();
}

[Table("meeting_template_items")]
public class MeetingTemplateItemDetail : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }
    
    [Column("template_id")]
    public Guid TemplateId { get; set; }
    
    [Column("title")]
    public string Title { get; set; } = string.Empty;
    
    [Column("notes")]
    public string? Notes { get; set; }
    
    [Column("sort_order")]
    public int SortOrder { get; set; }
}
```

### 4.2 Template Service

**File:** `ProCohere.Avalonia/Services/MeetingTemplateService.cs` (NEW)

```csharp
public class MeetingTemplateService
{
    // Get all templates for org
    public async Task<List<MeetingTemplateDetail>> GetTemplatesAsync();
    
    // Get template with items
    public async Task<MeetingTemplateDetail?> GetTemplateWithItemsAsync(Guid templateId);
    
    // Apply template to meeting (creates agenda items)
    public async Task<bool> ApplyTemplateToMeetingAsync(Guid meetingId, Guid templateId);
    
    // Create default templates for org (one-time setup)
    public async Task SeedDefaultTemplatesAsync(Guid organizationId);
}
```

### 4.3 Default Templates

```csharp
// Default templates to create:
public static class DefaultTemplates
{
    public static readonly (string Name, string Type, string[] Items)[] Templates = new[]
    {
        ("1:1 Check-In", "one_on_one", new[] {
            "Personal check-in",
            "Workload / capacity",
            "Progress on priorities",
            "Feedback (two-way)",
            "Follow-ups from last meeting"
        }),
        
        ("Sprint / Team Status", "team_sync", new[] {
            "Sprint status",
            "Risks and dependencies",
            "Blockers",
            "Upcoming priorities"
        }),
        
        ("Planning Session", "planning", new[] {
            "Goals and success criteria",
            "Scope and constraints",
            "Ownership and sequencing",
            "Risks and assumptions"
        }),
        
        ("Retrospective", "retrospective", new[] {
            "What went well",
            "What didn't go well",
            "What to change",
            "Action items"
        }),
        
        ("Ad-Hoc / Issue Review", "ad_hoc", new[] {
            "Context",
            "Impact",
            "Options",
            "Decision / next steps"
        })
    };
}
```

### 4.4 Template Picker UI

**File:** `ProCohere.Avalonia/Views/Dialogs/ApplyTemplateDialog.axaml` (NEW)

---

## Implementation Order Summary

| Day | Phase | Deliverables |
|-----|-------|--------------|
| 1 | Schema & Models | SQL migration, C# models, updated MeetingAgendaItem |
| 2 | Services | OutcomeService, CarryForwardService, MeetingAgendaItemService updates |
| 3 | UI - Cards | AgendaItemCard control with expand/collapse, tabs |
| 4 | UI - Dialogs | RecordOutcome, LinkEntity, DeferAgendaItem, CarryForwardSuggestions |
| 5 | Templates | MeetingTemplate model/service, default templates, template picker |

---

## Testing Checklist

### Outcomes
- [ ] Record decision from agenda item
- [ ] Record feedback from agenda item  
- [ ] Add notes to agenda item
- [ ] Create task from agenda item (existing, verify outcome recorded)
- [ ] View outcomes in expanded agenda item
- [ ] Verify visibility scoping works

### Carry Forward
- [ ] Defer agenda item to future meeting
- [ ] Verify original marked as 'deferred'
- [ ] Verify new item created with link
- [ ] Verify anchor person set
- [ ] See pending carry-forwards in meeting prep
- [ ] Add carry-forward to agenda
- [ ] Dismiss carry-forward suggestion
- [ ] Verify expiration after 30 days
- [ ] Verify expiration after 2 meetings

### Linked Entities
- [ ] Link agenda item to Goal
- [ ] Link agenda item to Metric
- [ ] Link agenda item to Task
- [ ] Unlink entity
- [ ] View linked entity summary

### Templates
- [ ] View available templates
- [ ] Apply template to meeting
- [ ] Verify agenda items created
- [ ] Default templates seeded for new org

### UI
- [ ] Agenda item collapsed state shows indicators
- [ ] Click to expand/collapse
- [ ] Notes tab editable
- [ ] Outcomes tab shows list
- [ ] Status change buttons work
- [ ] Progressive disclosure feels natural

---

## Files to Create/Modify

### New Files (13)
1. `Models/AgendaItemOutcome.cs`
2. `Models/AgendaItemOutcomeDetail.cs`
3. `Models/MeetingTemplateDetail.cs`
4. `Services/AgendaItemOutcomeService.cs`
5. `Services/CarryForwardService.cs`
6. `Services/MeetingTemplateService.cs`
7. `Views/Controls/AgendaItemCard.axaml` + `.cs`
8. `Views/Controls/CarryForwardSuggestionsPanel.axaml` + `.cs`
9. `Views/Dialogs/RecordOutcomeDialog.axaml` + `.cs`
10. `Views/Dialogs/LinkEntityDialog.axaml` + `.cs`
11. `Views/Dialogs/DeferAgendaItemDialog.axaml` + `.cs`
12. `Views/Dialogs/ApplyTemplateDialog.axaml` + `.cs`
13. `ViewModels/AgendaItemCardViewModel.cs`

### Modified Files (4)
1. `Models/MeetingDetail.cs` - Add carry-forward properties to MeetingAgendaItem
2. `Services/MeetingAgendaItemService.cs` - Add linking, outcomes loading
3. `Views/Controls/MeetingDetailFlyout.axaml` - Replace agenda section
4. `ViewModels/CircleViewModel.cs` - Add new commands and methods

### SQL Scripts (1)
1. `New Docs/SupaBase SQL Scrips/20260120_agenda_items_outcomes.sql`

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Complex UI state management | Use dedicated ViewModel per agenda item card |
| Performance with many outcomes | Lazy-load outcomes only when expanded |
| Carry-forward expiration timing | Run expiration check on app startup + hourly |
| Migration complexity | Additive schema changes only, no breaking changes |
| Template seeding race condition | Check existence before seeding, idempotent operation |
