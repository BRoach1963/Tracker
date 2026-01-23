# Timezone and Localization Architecture

## Current State

### Timezone Handling
- **Database**: All timestamps stored as `timestamptz` (UTC with timezone info) in PostgreSQL
- **API**: Supabase Postgrest returns DateTime with `Kind=Unspecified` (we treat as UTC)
- **Display**: Using `ScheduledAtLocal` property that specifies UTC Kind before converting to local time
- **User Preference**: `timezone` field exists in `users` and `user_settings` tables (e.g., "America/New_York")

### What's Working
- UTC storage ensures consistent ordering/comparison
- Local time display using system timezone
- East Coast user sees PST meeting at correct EST time (handled by UTC conversion)

### What's Missing
1. **Scheduler's Original Timezone** - No record of what timezone the meeting was scheduled in
2. **User Timezone Override** - Not using user's stored timezone preference, only system timezone
3. **Timezone Display** - Not showing timezone context (e.g., "9:00 AM PST")

---

## Recommended Architecture

### Phase 1: Use User's Stored Timezone (Priority: High)

Instead of `DateTime.Now` and `.ToLocalTime()`, use the user's stored timezone preference:

```csharp
// Services/TimezoneService.cs
public class TimezoneService
{
    private readonly ICurrentUserService _currentUser;
    
    public TimeZoneInfo GetUserTimeZone()
    {
        var tzId = _currentUser.Timezone ?? TimeZoneInfo.Local.Id;
        return TimeZoneInfo.FindSystemTimeZoneById(tzId);
    }
    
    public DateTime ToUserLocalTime(DateTime utcTime)
    {
        var tz = GetUserTimeZone();
        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz);
    }
    
    public DateTime ToUtc(DateTime localTime)
    {
        var tz = GetUserTimeZone();
        return TimeZoneInfo.ConvertTimeToUtc(localTime, tz);
    }
}
```

### Phase 2: Store Scheduler's Timezone (Priority: Medium)

Add to meetings table:
```sql
ALTER TABLE procohere.meetings 
ADD COLUMN scheduled_timezone text;  -- e.g., "America/Los_Angeles"
```

This allows showing:
- "Scheduled at 9:00 AM PST by John"
- Recurrence calculations in original timezone (important for DST)

### Phase 3: Display Improvements (Priority: Low)

- Show timezone abbreviation: "9:00 AM PST"
- Allow users to toggle between "My timezone" and "Scheduler's timezone" views
- Calendar event export respects timezones (ICS format)

---

## Example: Cross-Timezone Meeting Flow

**Scenario**: Sarah (PST) schedules a 9 AM meeting with Tom (EST)

1. **Sarah creates meeting** at 9:00 AM (her local PST time)
2. **System stores**: `2026-01-19 17:00:00+00` (UTC) + `scheduled_timezone: "America/Los_Angeles"`
3. **Tom views meeting**: System converts UTC to EST → shows "12:00 PM"
4. **Both see correct time** in their local timezone

---

## Localization (i18n/L10n)

### Avalonia Localization Options

1. **ResX Resources** (Recommended for WPF familiarity)
   - Create `Resources/Strings.resx`, `Strings.es.resx`, `Strings.fr.resx`
   - Use `x:Static` or binding with converter
   
2. **Avalonia.Localization** package
   - Similar to WPF approach
   - `{x:Static l:Resources.WelcomeMessage}`

3. **JSON-based** (modern approach)
   - Store strings in `locales/en.json`, `locales/es.json`
   - Load based on user preference

### Implementation Plan

1. **Phase 1**: Extract all hardcoded strings to resource files
2. **Phase 2**: Add language selector to Settings
3. **Phase 3**: Implement RTL support for Arabic/Hebrew if needed

### Example ResX Setup

```xml
<!-- Resources/Strings.resx -->
<data name="CircleTab_Team" xml:space="preserve">
    <value>Team</value>
</data>
<data name="CircleTab_Goals" xml:space="preserve">
    <value>Goals</value>
</data>

<!-- Resources/Strings.es.resx -->
<data name="CircleTab_Team" xml:space="preserve">
    <value>Equipo</value>
</data>
```

```xml
<!-- XAML Usage -->
<TextBlock Text="{x:Static res:Strings.CircleTab_Team}"/>
```

---

## Priority Recommendations

| Priority | Task | Effort | Impact |
|----------|------|--------|--------|
| P0 | ✅ Fix UTC display (done) | Low | High |
| P1 | Use user's stored timezone | Medium | High |
| P2 | Add scheduled_timezone to meetings | Low | Medium |
| P3 | Extract strings to resources | High | High (for i18n) |
| P4 | Add language selector | Medium | Medium |

---

## Database Changes Needed

### For Phase 1 (User Timezone):
Already have `timezone` column in `users` table - just need to use it in code.

### For Phase 2 (Scheduler Timezone):
```sql
-- Add scheduled_timezone column to meetings
ALTER TABLE procohere.meetings 
ADD COLUMN scheduled_timezone text;

-- Comment explaining usage
COMMENT ON COLUMN procohere.meetings.scheduled_timezone IS 
'IANA timezone ID where the meeting was originally scheduled (e.g., America/Los_Angeles). Used for recurrence calculations and display context.';
```

---

## IANA Timezone IDs

Use IANA timezone IDs for cross-platform compatibility:
- `America/New_York` (Eastern)
- `America/Chicago` (Central)
- `America/Denver` (Mountain)
- `America/Los_Angeles` (Pacific)
- `Europe/London`
- `Asia/Tokyo`

.NET can map these using `TimeZoneInfo.FindSystemTimeZoneById()` (works on Windows and Unix).
