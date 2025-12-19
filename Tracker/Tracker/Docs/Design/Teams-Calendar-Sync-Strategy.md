# Microsoft Teams & Calendar Sync Strategy

## Overview

This document defines the synchronization architecture for Tracker's Microsoft 365 integration, covering Outlook Calendar sync and Teams messaging features.

**Availability:** Pro Tier Only  
**Last Updated:** December 2024

---

## Architecture Decision: Multi-Tenant Azure AD

Tracker uses a **multi-tenant Azure AD application** registration:

- Users authenticate with **their own** Microsoft work/school accounts
- Tracker does not manage user identities (Microsoft does)
- App registration defines requested permissions, but users auth against their org
- Single app registration works for all customers

**Why not own Azure AD tenant?**
- Would require users to have accounts in Tracker's tenant
- Defeats SaaS purpose - users should use their existing M365 credentials

---

## Teams App Distribution: Sideload Only

For initial release, Teams app will be sideloaded:

- No Microsoft Store approval process required
- Users install via "Upload a custom app" or admin org-wide deployment
- Full functionality without store listing
- Can migrate to Store later if desired

---

## Sync Architecture: Optimistic Push + Delta Pull

```
┌─────────────────┐                    ┌─────────────────┐
│     TRACKER     │ ◄──── SYNC ────►  │  OUTLOOK/TEAMS  │
│   (Desktop)     │                    │   (Cloud)       │
└─────────────────┘                    └─────────────────┘
```

### Outbound Sync (Tracker → Calendar)

**Strategy: IMMEDIATE PUSH**

| Event | Action |
|-------|--------|
| User creates 1:1 | Immediately create calendar event |
| User edits 1:1 | Immediately update calendar event |
| User deletes 1:1 | Immediately delete calendar event |

**Failure Handling:**
- If offline: Queue in local database, sync when online
- If API fails: Retry 3x with exponential backoff, then queue

### Inbound Sync (Calendar → Tracker)

**Strategy: SMART POLLING with DELTA QUERIES**

Microsoft Graph delta queries return only items changed since last sync:

```
GET /me/calendarView/delta
→ Returns only new/modified/deleted events
→ Includes deltaLink for next request
```

**Sync Triggers:**

| Trigger | Action | Rationale |
|---------|--------|-----------|
| App startup | Full delta sync | Catch up on missed changes |
| Every 5 minutes | Delta sync | Catch external changes |
| Window gains focus | Quick delta sync | Fresh data when user returns |
| Before 1:1 prep | Sync specific meeting | Latest data for preparation |
| Manual refresh | Full delta sync | User-initiated |
| After push fails | Retry queue + delta sync | Error recovery |

**Why NOT Webhooks:**
- Desktop apps lack publicly accessible callback URLs
- Would require local server + tunnel (fragile) or cloud relay (complexity/cost)
- Delta queries designed for exactly this scenario

### Delta Query Flow

**First Sync (Initial):**
```
1. GET /me/calendarView/delta?startDateTime=...&endDateTime=...
2. Returns: All events + deltaLink
3. Store deltaLink in local settings
```

**Subsequent Syncs:**
```
1. GET {deltaLink from last sync}
2. Returns: Only changed events
   - Added events (new)
   - Modified events (updated)  
   - Deleted events (@removed)
3. Store new deltaLink
```

**Efficiency:**
- First sync: ~500ms (all events)
- Delta sync: ~100ms (typically 0-2 changes)
- API calls: 1 per sync cycle

---

## Conflict Resolution

### Tracked Metadata

Each synced event stores:
- `CalendarEventId` - Microsoft Graph event ID
- `LastKnownEtag` - Version identifier for change detection
- `LastSyncedAt` - Timestamp of last sync
- `TrackerModifiedAt` - Local change timestamp

### Conflict Scenarios

| Scenario | Resolution |
|----------|------------|
| User edits in Tracker, event unchanged in Calendar | Push update normally |
| Event changed in Calendar, user hasn't edited in Tracker | Pull update normally |
| BOTH changed (etag differs + local changes) | **Calendar Wins + Notify** |

### Resolution Strategy: "Calendar Wins + Notify"

- Calendar is the "source of truth" for scheduling
- Pull the calendar change
- Preserve Tracker-only data (notes, agenda items, links)
- Show user notification:
  > "Your 1:1 with Sarah was moved to 3pm in Outlook. Your Tracker notes have been preserved."

**Rationale:**
- Calendar invites may include other attendees who accepted
- Moving calendar events has real-world scheduling implications
- Tracker-specific data (notes, agenda) has no calendar equivalent, so always preserved

---

## Offline Handling

### Sync Queue Table

```sql
CREATE TABLE SyncQueue (
    Id INTEGER PRIMARY KEY,
    Operation TEXT NOT NULL,      -- CREATE, UPDATE, DELETE
    EntityType TEXT NOT NULL,     -- OneOnOne, Task, etc.
    EntityId TEXT NOT NULL,
    Payload TEXT NOT NULL,        -- JSON serialized data
    CreatedAt DATETIME NOT NULL,
    RetryCount INTEGER DEFAULT 0,
    LastError TEXT
);
```

### Queue Processing

When back online:
1. Process queue in FIFO order
2. For each item:
   - Attempt operation
   - If conflict: Apply resolution strategy
   - If success: Remove from queue
   - If fail: Increment retry count, max 3 attempts
3. After queue empty: Full delta sync

### UI Indicators

| State | Indicator |
|-------|-----------|
| Queue has items | "⏳ Syncing..." in status bar |
| Offline | "📴 Offline - changes will sync later" |
| Error | "⚠️ Sync issue - [Retry]" |
| Synced | "✓ Synced" (subtle, fades) |

---

## Rate Limiting Protection

### Microsoft Graph Limits
- 10,000 requests per 10 minutes per app per tenant
- Normal usage: ~50-100 requests/hour (well under limit)

### Conservative Throttling

```csharp
public class GraphRateLimiter
{
    private readonly SemaphoreSlim _semaphore = new(10); // Max 10 concurrent
    private readonly Queue<DateTime> _requestTimes = new();
    private const int MaxRequestsPerMinute = 100;
    
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> request)
    {
        await _semaphore.WaitAsync();
        try
        {
            // Ensure we don't exceed limit
            while (_requestTimes.Count >= MaxRequestsPerMinute && 
                   _requestTimes.Peek() > DateTime.UtcNow.AddMinutes(-1))
            {
                await Task.Delay(1000);
            }
            
            _requestTimes.Enqueue(DateTime.UtcNow);
            if (_requestTimes.Count > MaxRequestsPerMinute)
                _requestTimes.Dequeue();
                
            return await request();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

---

## Service Architecture

### CalendarSyncService

```csharp
public class CalendarSyncService : IDisposable
{
    private Timer? _syncTimer;
    private string? _deltaLink;
    private readonly ConcurrentQueue<SyncOperation> _outboundQueue;
    
    // === SYNC TRIGGERS ===
    
    public void StartPeriodicSync() 
        => _syncTimer = new Timer(SyncDelta, null, 
           TimeSpan.Zero, TimeSpan.FromMinutes(5));
    
    public void OnAppFocused() 
        => _ = SyncDeltaAsync();
    
    public void OnOneOnOneCreated(OneOnOne meeting) 
        => _ = PushCreateAsync(meeting);
    
    public void OnOneOnOneUpdated(OneOnOne meeting) 
        => _ = PushUpdateAsync(meeting);
    
    public void OnOneOnOneDeleted(OneOnOne meeting) 
        => _ = PushDeleteAsync(meeting);
    
    // === CORE SYNC LOGIC ===
    
    private async Task SyncDeltaAsync() { }
    private async Task PushCreateAsync(OneOnOne meeting) { }
    private async Task PushUpdateAsync(OneOnOne meeting) { }
    private async Task PushDeleteAsync(OneOnOne meeting) { }
    private async Task ProcessOfflineQueueAsync() { }
}
```

### Integration Points

**TrackerDataManager:**
- Calls `CalendarSyncService.OnOneOnOneCreated/Updated/Deleted`

**MainWindow:**
- Calls `CalendarSyncService.OnAppFocused` on window activation

**App.xaml.cs:**
- Calls `CalendarSyncService.StartPeriodicSync` after login (Pro tier only)

---

## Data Model Extensions

### OneOnOne Table Additions

```sql
ALTER TABLE OneOnOnes ADD COLUMN CalendarEventId TEXT;
ALTER TABLE OneOnOnes ADD COLUMN CalendarEventEtag TEXT;
ALTER TABLE OneOnOnes ADD COLUMN LastSyncedAt DATETIME;
ALTER TABLE OneOnOnes ADD COLUMN SyncStatus TEXT DEFAULT 'NotSynced';
-- SyncStatus: NotSynced, Synced, Pending, Error
```

### Calendar Sync Settings

```csharp
public class CalendarSyncSettings
{
    public bool IsEnabled { get; set; } = false;
    public string? DeltaLink { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public int SyncIntervalMinutes { get; set; } = 5;
    public bool SyncOnFocus { get; set; } = true;
    public bool ShowSyncNotifications { get; set; } = true;
}
```

---

## Required Microsoft Graph Permissions

| Permission | Type | Purpose |
|------------|------|---------|
| `Calendars.ReadWrite` | Delegated | Read/write calendar events |
| `User.Read` | Delegated | Get user profile info |
| `offline_access` | Delegated | Refresh tokens for persistent access |

**Future (Teams messaging):**
| Permission | Type | Purpose |
|------------|------|---------|
| `Chat.ReadWrite` | Delegated | Read/send Teams messages |
| `ChannelMessage.Send` | Delegated | Post to channels |

---

## Summary

| Aspect | Decision | Rationale |
|--------|----------|-----------|
| **App Model** | Multi-tenant Azure AD | Users auth with their own M365 |
| **Distribution** | Sideload only | No Store approval needed |
| **Tier** | Pro only | Premium feature |
| **Outbound** | Immediate push | User expects instant feedback |
| **Inbound** | Delta polling (5 min) | Efficient, no webhook complexity |
| **Conflict** | Calendar wins + notify | Calendar is scheduling truth |
| **Offline** | Queue + retry | Never lose user's work |
| **Rate limits** | Conservative throttle | Stay well under limits |

---

## Implementation Phases

### Phase 1: Foundation (Week 1)
- Azure App Registration
- OAuth 2.0 flow with PKCE
- Token storage & refresh
- Basic Graph client wrapper

### Phase 2: Calendar Sync (Weeks 2-3)
- Delta query implementation
- Push create/update/delete
- Offline queue
- Conflict detection & resolution
- UI integration (settings, indicators)

### Phase 3: Teams Features (Weeks 4-6)
- Teams app manifest
- Slash commands
- Message extensions
- Notifications

### Phase 4: Polish (Week 7)
- Smart features (prep reminders)
- Analytics & diagnostics
- Documentation

### Phase 5: Google Calendar (Week 8+)
- Google Cloud Console project setup
- OAuth 2.0 credentials for desktop app
- Same sync architecture (delta queries for Google use `syncToken`)
- `GoogleCalendarService` implementing same interfaces
- User chooses which provider(s) to sync with

