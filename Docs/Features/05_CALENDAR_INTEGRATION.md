# Feature 05: Calendar Integration
## Technical Specification

**Feature ID:** F-005  
**Priority:** P1  
**Estimated Effort:** 3-4 sprints  
**Status:** Planning

---

## Executive Summary

Integrate Tracker with external calendar systems (Microsoft Outlook/Exchange and Google Calendar) to enable bidirectional synchronization of 1:1 meetings. This allows managers to see their meetings in their primary calendar tool and have meetings created in calendars automatically appear in Tracker.

---

## User Stories

| ID | Story | Priority |
|----|-------|----------|
| US-001 | As a manager, I want 1:1s to appear in my Outlook/Google calendar so I don't double-book | P0 |
| US-002 | As a manager, I want to create meetings from Tracker that automatically add to my calendar | P0 |
| US-003 | As a manager, I want meeting changes synced bidirectionally | P1 |
| US-004 | As a manager, I want to see free/busy when scheduling new 1:1s | P1 |
| US-005 | As a manager, I want meeting reminders from my calendar system | P1 |
| US-006 | As a manager, I want to link existing calendar events to Tracker meetings | P2 |

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        CALENDAR INTEGRATION SYSTEM                           │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                    CalendarSyncService                               │    │
│  │                                                                       │    │
│  │   ┌─────────────────┐                    ┌─────────────────┐         │    │
│  │   │ ICalendarProvider│◄──────────────────│ CalendarSync    │         │    │
│  │   │                  │                    │ Coordinator     │         │    │
│  │   └────────┬─────────┘                    └────────┬────────┘         │    │
│  │            │                                       │                  │    │
│  │   ┌────────┴────────┬──────────────────┐          │                  │    │
│  │   │                 │                   │          │                  │    │
│  │   ▼                 ▼                   ▼          ▼                  │    │
│  │ ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────────┐       │    │
│  │ │ Outlook  │  │ Google   │  │ Local    │  │ Conflict         │       │    │
│  │ │ Provider │  │ Calendar │  │ ICS File │  │ Resolution       │       │    │
│  │ │(Graph API)│  │ Provider │  │ Provider │  │ Engine           │       │    │
│  │ └──────────┘  └──────────┘  └──────────┘  └──────────────────┘       │    │
│  │                                                                       │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                    Sync Data Model                                   │    │
│  │                                                                       │    │
│  │   calendar_links table                                                │    │
│  │   ┌──────────────────────────────────────────────────────────────┐   │    │
│  │   │ tracker_meeting_id │ provider │ external_id │ last_sync      │   │    │
│  │   └──────────────────────────────────────────────────────────────┘   │    │
│  │                                                                       │    │
│  │   Tracks: OneOnOne ↔ Calendar Event mapping                          │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐    │
│  │                    Free/Busy Service                                 │    │
│  │                                                                       │    │
│  │   GetAvailabilityAsync(DateTime start, DateTime end)                 │    │
│  │   → Returns busy slots, suggesting free times                        │    │
│  └─────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Component Specifications

### 1. Calendar Integration Models

```csharp
public interface ICalendarProvider
{
    string ProviderId { get; }  // "outlook", "google", "ics"
    string DisplayName { get; }
    
    // Authentication
    Task<bool> AuthenticateAsync();
    Task<bool> IsAuthenticatedAsync();
    Task RevokeAuthAsync();
    
    // Events
    Task<string> CreateEventAsync(CalendarEvent calEvent);
    Task UpdateEventAsync(string externalId, CalendarEvent calEvent);
    Task DeleteEventAsync(string externalId);
    Task<CalendarEvent?> GetEventAsync(string externalId);
    
    // Free/Busy
    Task<List<BusySlot>> GetFreeBusyAsync(DateTime start, DateTime end);
    
    // Sync
    Task<List<CalendarEvent>> GetEventsAsync(DateTime start, DateTime end);
    Task<SyncToken> GetSyncTokenAsync();
    Task<List<CalendarChange>> GetChangesAsync(SyncToken token);
}

public class CalendarEvent
{
    public string? ExternalId { get; set; }
    public string Subject { get; set; }
    public string? Body { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string? Location { get; set; }
    public bool IsAllDay { get; set; }
    public EventStatus Status { get; set; }
    public List<string> Attendees { get; set; } = new();
    public int? ReminderMinutes { get; set; }
    
    // Tracker-specific
    public int? TrackerMeetingId { get; set; }
    public string? TrackerLink { get; set; }  // Deep link back to Tracker
}

public enum EventStatus
{
    Tentative,
    Confirmed,
    Cancelled
}

public class BusySlot
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string? Subject { get; set; }  // May be hidden for privacy
    public BusyStatus Status { get; set; }
}

public enum BusyStatus
{
    Free,
    Tentative,
    Busy,
    OutOfOffice,
    WorkingElsewhere
}

public class CalendarLink
{
    public int Id { get; set; }
    public int TrackerMeetingId { get; set; }
    public string ProviderId { get; set; }
    public string ExternalEventId { get; set; }
    public DateTime LastSyncedAt { get; set; }
    public SyncDirection LastSyncDirection { get; set; }
    public string? ETag { get; set; }  // For change detection
}

public enum SyncDirection
{
    TrackerToCalendar,
    CalendarToTracker,
    Bidirectional
}

public class CalendarChange
{
    public ChangeType Type { get; set; }
    public CalendarEvent Event { get; set; }
    public string ExternalId { get; set; }
}

public enum ChangeType
{
    Created,
    Updated,
    Deleted
}
```

### 2. Microsoft Graph (Outlook) Provider

```csharp
public class OutlookCalendarProvider : ICalendarProvider
{
    private readonly string _clientId;
    private readonly string[] _scopes = new[] 
    { 
        "Calendars.ReadWrite", 
        "User.Read" 
    };
    
    private IPublicClientApplication _msalClient;
    private string? _accessToken;
    
    public string ProviderId => "outlook";
    public string DisplayName => "Microsoft Outlook";
    
    public async Task<bool> AuthenticateAsync()
    {
        try
        {
            // Use MSAL for authentication with device code flow
            // or interactive browser auth
            var result = await _msalClient
                .AcquireTokenInteractive(_scopes)
                .WithUseEmbeddedWebView(false)
                .ExecuteAsync();
            
            _accessToken = result.AccessToken;
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error("Outlook auth failed: {0}", ex.Message);
            return false;
        }
    }
    
    public async Task<string> CreateEventAsync(CalendarEvent calEvent)
    {
        var graphClient = GetGraphClient();
        
        var msEvent = new Event
        {
            Subject = calEvent.Subject,
            Body = new ItemBody
            {
                ContentType = BodyType.Html,
                Content = calEvent.Body ?? ""
            },
            Start = new DateTimeTimeZone
            {
                DateTime = calEvent.Start.ToString("o"),
                TimeZone = TimeZoneInfo.Local.Id
            },
            End = new DateTimeTimeZone
            {
                DateTime = calEvent.End.ToString("o"),
                TimeZone = TimeZoneInfo.Local.Id
            },
            Location = new Location { DisplayName = calEvent.Location },
            IsReminderOn = calEvent.ReminderMinutes.HasValue,
            ReminderMinutesBforeStart = calEvent.ReminderMinutes ?? 15
        };
        
        // Add Tracker link to body
        if (!string.IsNullOrEmpty(calEvent.TrackerLink))
        {
            msEvent.Body.Content += $"\n\n---\nOpen in Tracker: {calEvent.TrackerLink}";
        }
        
        var created = await graphClient.Me.Events
            .Request()
            .AddAsync(msEvent);
        
        return created.Id;
    }
    
    public async Task<List<BusySlot>> GetFreeBusyAsync(DateTime start, DateTime end)
    {
        var graphClient = GetGraphClient();
        
        var schedules = new List<string> { "me" };
        var request = new CalendarGetScheduleRequestBody
        {
            Schedules = schedules,
            StartTime = new DateTimeTimeZone 
            { 
                DateTime = start.ToString("o"), 
                TimeZone = "UTC" 
            },
            EndTime = new DateTimeTimeZone 
            { 
                DateTime = end.ToString("o"), 
                TimeZone = "UTC" 
            },
            AvailabilityViewInterval = 30  // 30-minute slots
        };
        
        var result = await graphClient.Me.Calendar
            .GetSchedule(request)
            .Request()
            .PostAsync();
        
        var busySlots = new List<BusySlot>();
        foreach (var schedule in result.Value)
        {
            foreach (var item in schedule.ScheduleItems)
            {
                busySlots.Add(new BusySlot
                {
                    Start = DateTime.Parse(item.Start.DateTime),
                    End = DateTime.Parse(item.End.DateTime),
                    Subject = item.Subject,
                    Status = MapStatus(item.Status.Value)
                });
            }
        }
        
        return busySlots;
    }
}
```

### 3. Google Calendar Provider

```csharp
public class GoogleCalendarProvider : ICalendarProvider
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private CalendarService? _calendarService;
    
    public string ProviderId => "google";
    public string DisplayName => "Google Calendar";
    
    public async Task<bool> AuthenticateAsync()
    {
        try
        {
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = _clientId,
                    ClientSecret = _clientSecret
                },
                new[] { CalendarService.Scope.Calendar },
                "user",
                CancellationToken.None,
                new FileDataStore("TrackerGoogleCalendar")
            );
            
            _calendarService = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Tracker"
            });
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error("Google auth failed: {0}", ex.Message);
            return false;
        }
    }
    
    public async Task<string> CreateEventAsync(CalendarEvent calEvent)
    {
        var googleEvent = new Google.Apis.Calendar.v3.Data.Event
        {
            Summary = calEvent.Subject,
            Description = calEvent.Body,
            Start = new EventDateTime
            {
                DateTime = calEvent.Start,
                TimeZone = TimeZoneInfo.Local.Id
            },
            End = new EventDateTime
            {
                DateTime = calEvent.End,
                TimeZone = TimeZoneInfo.Local.Id
            },
            Location = calEvent.Location,
            Reminders = new Event.RemindersData
            {
                UseDefault = false,
                Overrides = new List<EventReminder>
                {
                    new EventReminder 
                    { 
                        Method = "popup", 
                        Minutes = calEvent.ReminderMinutes ?? 15 
                    }
                }
            }
        };
        
        // Add Tracker link
        if (!string.IsNullOrEmpty(calEvent.TrackerLink))
        {
            googleEvent.Description += $"\n\n---\nOpen in Tracker: {calEvent.TrackerLink}";
        }
        
        var created = await _calendarService.Events
            .Insert(googleEvent, "primary")
            .ExecuteAsync();
        
        return created.Id;
    }
}
```

### 4. Calendar Sync Service

```csharp
public class CalendarSyncService
{
    private readonly ICalendarProvider? _activeProvider;
    private readonly TrackerDbManager _db;
    
    public bool IsEnabled => _activeProvider != null;
    public string? ActiveProviderName => _activeProvider?.DisplayName;
    
    /// <summary>
    /// Push a Tracker meeting to the connected calendar.
    /// </summary>
    public async Task<bool> SyncMeetingToCalendarAsync(OneOnOne meeting)
    {
        if (_activeProvider == null)
            return false;
        
        // Check if already linked
        var link = await _db.GetCalendarLinkAsync(meeting.Id);
        
        var calEvent = MapToCalendarEvent(meeting);
        
        if (link != null)
        {
            // Update existing event
            await _activeProvider.UpdateEventAsync(link.ExternalEventId, calEvent);
            link.LastSyncedAt = DateTime.UtcNow;
            link.LastSyncDirection = SyncDirection.TrackerToCalendar;
            await _db.UpdateCalendarLinkAsync(link);
        }
        else
        {
            // Create new event
            var externalId = await _activeProvider.CreateEventAsync(calEvent);
            
            // Save link
            await _db.SaveCalendarLinkAsync(new CalendarLink
            {
                TrackerMeetingId = meeting.Id,
                ProviderId = _activeProvider.ProviderId,
                ExternalEventId = externalId,
                LastSyncedAt = DateTime.UtcNow,
                LastSyncDirection = SyncDirection.TrackerToCalendar
            });
        }
        
        return true;
    }
    
    /// <summary>
    /// Pull calendar changes and apply to Tracker.
    /// </summary>
    public async Task<int> SyncFromCalendarAsync()
    {
        if (_activeProvider == null)
            return 0;
        
        var token = await GetStoredSyncTokenAsync();
        var changes = await _activeProvider.GetChangesAsync(token);
        
        var applied = 0;
        foreach (var change in changes)
        {
            var link = await _db.GetCalendarLinkByExternalIdAsync(
                _activeProvider.ProviderId, 
                change.ExternalId
            );
            
            if (link == null)
                continue;  // Not a Tracker meeting
            
            var meeting = await _db.GetOneOnOneAsync(link.TrackerMeetingId);
            if (meeting == null)
                continue;
            
            switch (change.Type)
            {
                case ChangeType.Updated:
                    // Apply changes to Tracker meeting
                    ApplyCalendarChanges(meeting, change.Event);
                    await _db.UpdateOneOnOneAsync(meeting);
                    applied++;
                    break;
                    
                case ChangeType.Deleted:
                    // Mark as cancelled in Tracker
                    meeting.Status = MeetingStatus.Cancelled;
                    await _db.UpdateOneOnOneAsync(meeting);
                    await _db.DeleteCalendarLinkAsync(link.Id);
                    applied++;
                    break;
            }
        }
        
        // Store new sync token
        await SaveSyncTokenAsync(await _activeProvider.GetSyncTokenAsync());
        
        return applied;
    }
    
    /// <summary>
    /// Get available time slots for scheduling.
    /// </summary>
    public async Task<List<TimeSlot>> GetAvailableTimesAsync(
        DateTime start, 
        DateTime end, 
        int durationMinutes)
    {
        if (_activeProvider == null)
            return new List<TimeSlot>();
        
        var busySlots = await _activeProvider.GetFreeBusyAsync(start, end);
        
        // Find gaps between busy slots
        var available = new List<TimeSlot>();
        var current = start;
        
        foreach (var busy in busySlots.OrderBy(b => b.Start))
        {
            if (current.AddMinutes(durationMinutes) <= busy.Start)
            {
                // There's a gap - add available slots
                while (current.AddMinutes(durationMinutes) <= busy.Start)
                {
                    available.Add(new TimeSlot
                    {
                        Start = current,
                        End = current.AddMinutes(durationMinutes)
                    });
                    current = current.AddMinutes(30);  // 30-min increments
                }
            }
            current = busy.End > current ? busy.End : current;
        }
        
        // Add remaining time until end
        while (current.AddMinutes(durationMinutes) <= end)
        {
            available.Add(new TimeSlot
            {
                Start = current,
                End = current.AddMinutes(durationMinutes)
            });
            current = current.AddMinutes(30);
        }
        
        return available;
    }
    
    private CalendarEvent MapToCalendarEvent(OneOnOne meeting)
    {
        return new CalendarEvent
        {
            TrackerMeetingId = meeting.Id,
            Subject = $"1:1 with {meeting.TeamMember.FullName}",
            Body = BuildMeetingBody(meeting),
            Start = meeting.MeetingDate,
            End = meeting.MeetingDate.AddMinutes(meeting.Duration),
            Location = meeting.Location,
            ReminderMinutes = 15,
            TrackerLink = $"tracker://meeting/{meeting.Id}"
        };
    }
    
    private string BuildMeetingBody(OneOnOne meeting)
    {
        var sb = new StringBuilder();
        
        if (!string.IsNullOrEmpty(meeting.Agenda))
        {
            sb.AppendLine("<h3>Agenda</h3>");
            sb.AppendLine($"<p>{meeting.Agenda.Replace("\n", "<br/>")}</p>");
        }
        
        sb.AppendLine("<p><em>Managed by Tracker</em></p>");
        
        return sb.ToString();
    }
}
```

### 5. Database Schema for Calendar Links

```sql
CREATE TABLE calendar_links (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    tracker_meeting_id INTEGER NOT NULL,
    provider_id TEXT NOT NULL,
    external_event_id TEXT NOT NULL,
    last_synced_at TEXT NOT NULL,
    last_sync_direction TEXT NOT NULL,
    etag TEXT,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (tracker_meeting_id) REFERENCES OneOnOnes(Id),
    UNIQUE(provider_id, external_event_id)
);

CREATE INDEX idx_calendar_links_meeting ON calendar_links(tracker_meeting_id);
CREATE INDEX idx_calendar_links_external ON calendar_links(provider_id, external_event_id);

CREATE TABLE calendar_sync_tokens (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    provider_id TEXT NOT NULL UNIQUE,
    sync_token TEXT NOT NULL,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP
);
```

### 6. UI Components

#### CalendarSetupDialog
- Select provider (Outlook/Google)
- Authenticate with OAuth
- Test connection
- Set sync preferences

#### SchedulingAssistant
- Shows free/busy when creating meetings
- Suggests available times
- One-click scheduling

#### MeetingCalendarStatus
- Icon showing sync status on meeting cards
- "Synced to Outlook" / "Not synced" indicator

---

## Data Flow

### Sync to Calendar Flow
```
User creates/updates meeting in Tracker
              │
              ▼
    MeetingViewModel.SaveAsync()
              │
              ├──▶ Save to database
              │
              └──▶ CalendarSyncService.SyncMeetingToCalendarAsync()
                              │
                              ├── Check for existing CalendarLink
                              │
                              ├── Map OneOnOne → CalendarEvent
                              │
                              ├── Provider.CreateEventAsync() or UpdateEventAsync()
                              │
                              └── Save/Update CalendarLink
```

### Sync from Calendar Flow
```
App Startup (or periodic sync)
              │
              ▼
    CalendarSyncService.SyncFromCalendarAsync()
              │
              ├── Get stored sync token
              │
              ├── Provider.GetChangesAsync(token)
              │
              ├── For each change:
              │       ├── Find CalendarLink by external ID
              │       ├── Get Tracker meeting
              │       └── Apply changes / mark cancelled
              │
              └── Store new sync token
```

### Free/Busy Flow
```
User opens "New Meeting" dialog
              │
              ▼
    Select team member and date range
              │
              ▼
    CalendarSyncService.GetAvailableTimesAsync()
              │
              ├── Provider.GetFreeBusyAsync()
              │
              └── Calculate available slots
                        │
                        ▼
              Display in SchedulingAssistant UI
                        │
                        ▼
              User clicks suggested time → Auto-fill
```

---

## Authentication Flows

### Microsoft (Azure AD / MSAL)

**Requirements:**
- Azure AD App Registration
- Client ID configured
- Redirect URI: `http://localhost:XXXX` for desktop
- Delegated permissions: `Calendars.ReadWrite`, `User.Read`

**Flow:**
1. User clicks "Connect Outlook"
2. Browser opens Azure AD login
3. User consents to permissions
4. Redirect back with auth code
5. Exchange for access + refresh tokens
6. Store tokens securely

### Google (OAuth 2.0)

**Requirements:**
- Google Cloud Console project
- OAuth 2.0 Client ID (Desktop app type)
- Client ID and Secret configured
- Scopes: `https://www.googleapis.com/auth/calendar`

**Flow:**
1. User clicks "Connect Google Calendar"
2. Browser opens Google login
3. User consents to Calendar access
4. Redirect back with auth code
5. Exchange for access + refresh tokens
6. Store tokens via FileDataStore

---

## Configuration

### User Settings
```json
{
    "CalendarIntegration": {
        "IsEnabled": true,
        "Provider": "outlook",
        "SyncDirection": "Bidirectional",
        "AutoSyncOnStartup": true,
        "SyncIntervalMinutes": 15,
        "DefaultMeetingDuration": 30,
        "DefaultReminderMinutes": 15,
        "IncludeAgendaInCalendar": true
    }
}
```

### App Registration (stored separately, encrypted)
```json
{
    "MicrosoftGraph": {
        "ClientId": "YOUR_CLIENT_ID",
        "TenantId": "common"
    },
    "GoogleCalendar": {
        "ClientId": "YOUR_CLIENT_ID",
        "ClientSecret": "YOUR_CLIENT_SECRET"
    }
}
```

---

## Implementation Plan

### Phase 1: Infrastructure (Sprint 1)
| Task | Estimate | Dependencies |
|------|----------|--------------|
| Design ICalendarProvider interface | 2h | None |
| Create CalendarEvent/Link models | 2h | None |
| Create calendar_links table | 1h | None |
| Create CalendarSyncService skeleton | 3h | Models |
| Implement link storage/retrieval | 2h | Table, Service |

### Phase 2: Microsoft Outlook Provider (Sprint 2)
| Task | Estimate | Dependencies |
|------|----------|--------------|
| Register Azure AD App | 2h | None (Azure access) |
| Implement MSAL authentication | 6h | Azure App |
| Implement CreateEventAsync | 4h | Auth |
| Implement UpdateEventAsync | 2h | Create |
| Implement DeleteEventAsync | 1h | Create |
| Implement GetFreeBusyAsync | 4h | Auth |
| Implement sync token / GetChangesAsync | 4h | Auth |

### Phase 3: Google Calendar Provider (Sprint 3)
| Task | Estimate | Dependencies |
|------|----------|--------------|
| Register Google Cloud project | 2h | None (Google access) |
| Implement Google OAuth | 4h | Google project |
| Implement CreateEventAsync | 3h | Auth |
| Implement UpdateEventAsync | 2h | Create |
| Implement DeleteEventAsync | 1h | Create |
| Implement GetFreeBusyAsync | 3h | Auth |
| Implement sync token / GetChangesAsync | 3h | Auth |

### Phase 4: UI Integration (Sprint 4)
| Task | Estimate | Dependencies |
|------|----------|--------------|
| Create CalendarSetupDialog | 4h | Providers |
| Create SchedulingAssistant control | 6h | GetFreeBusyAsync |
| Add calendar status to meeting cards | 2h | CalendarLink |
| Integrate sync into meeting save | 2h | CalendarSyncService |
| Settings page for calendar options | 3h | Settings model |
| Background sync service | 4h | CalendarSyncService |

---

## Roadblocks & Risks

### Technical Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| OAuth token expiration | High | Implement refresh token flow |
| API rate limiting | Medium | Batch requests, respect limits |
| Network connectivity issues | Medium | Graceful degradation, offline queue |
| Different calendar schemas | Medium | Abstract via ICalendarProvider |

### Security Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Token storage | Critical | Use Windows Credential Manager or DPAPI |
| Client secret exposure | High | Use auth code flow, not implicit |
| Calendar data exposure | Medium | Minimal permissions, user consent |

### UX Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Complex setup flow | High | Step-by-step wizard, clear instructions |
| Sync conflicts confuse users | Medium | Clear conflict resolution UI |
| Calendar permission scary | Medium | Explain why permissions needed |

### Organizational Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| IT blocks OAuth | High | Provide IT admin documentation |
| Azure AD app approval | Medium | Start approval process early |
| Google workspace restrictions | Medium | Document workspace admin setup |

---

## Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Calendar connection rate | >60% of users | Track provider setup |
| Sync success rate | >98% | Log sync operations |
| Meeting no-shows reduced | 20% | Compare before/after |
| Setup completion rate | >80% | Funnel analytics |

---

## Dependencies

- **NuGet Packages:**
  - `Microsoft.Identity.Client` (MSAL)
  - `Microsoft.Graph`
  - `Google.Apis.Calendar.v3`
- **External:**
  - Azure AD App Registration
  - Google Cloud Console project
- **Database:**
  - calendar_links table
  - calendar_sync_tokens table

---

## Future Enhancements

1. **Team Member Calendar** - If team member shares calendar, show their free/busy
2. **Recurring Meetings** - Support recurring 1:1 sync
3. **Meeting Room Booking** - Integrate with room calendars
4. **iCal/ICS Export** - Manual export for unsupported calendars
5. **Multiple Calendars** - Sync to specific calendar (not just primary)
6. **Calendar Reminders** - Custom reminder rules

---

**Document End**
