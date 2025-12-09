# Calendar & Email Integration Plan

## Overview
This document outlines the plan for integrating Tracker with Google Calendar/Gmail and Microsoft Outlook/Office 365 for calendar synchronization and email notifications.

---

## Phase 1: Google Calendar Integration

### 1.1 Prerequisites & Dependencies

**NuGet Packages Required:**
- `Google.Apis.Calendar.v3` (Version 1.68.0.3473) - ✅ Already installed
- `Google.Apis.Auth` (Latest) - For OAuth2 authentication
- `Google.Apis.Core` (Latest) - Core Google API functionality

**Google Cloud Console Setup:**
1. Create/configure Google Cloud Project
2. Enable Google Calendar API
3. Create OAuth 2.0 credentials (Client ID & Secret)
4. Configure authorized redirect URIs
5. Store credentials securely (encrypted in user settings or secure storage)

### 1.2 Architecture Components

**New Classes/Files:**
```
Tracker/
├── Services/
│   ├── GoogleCalendarService.cs          # Google Calendar API wrapper
│   └── GoogleAuthService.cs              # OAuth2 authentication handler
├── Managers/
│   └── CalendarSyncManager.cs            # Unified calendar sync manager
├── ViewModels/
│   └── DialogViewModels/
│       └── CalendarSettingsViewModel.cs  # Settings for calendar integration
├── Views/
│   └── Dialogs/
│       └── CalendarSettingsDialog.xaml   # UI for calendar configuration
└── Classes/
    ├── CalendarSettings.cs               # Settings model
    └── CalendarEvent.cs                  # Calendar event DTO
```

### 1.3 Authentication Flow

**OAuth 2.0 Flow:**
1. User clicks "Connect Google Calendar" in Settings
2. Application opens browser/embedded web view
3. User authenticates with Google
4. Google redirects with authorization code
5. Application exchanges code for access token & refresh token
6. Store tokens securely (encrypted)
7. Use refresh token to get new access tokens when expired

**Token Storage:**
- Store in `UserSettings` (encrypted)
- Use Windows Data Protection API (DPAPI) for encryption
- Never store tokens in plain text

### 1.4 Data Synchronization

**Sync Direction Options:**
- **One-Way (Tracker → Google)**: Create/update 1:1 meetings in Google Calendar
- **Two-Way Sync**: Sync both directions (more complex, requires conflict resolution)

**Initial Implementation: One-Way (Tracker → Google)**

**Sync Triggers:**
- On 1:1 creation/update
- Manual sync button
- Scheduled background sync (optional)

**Data Mapping:**
```
OneOnOne → Google Calendar Event
├── Date → Start DateTime
├── StartTime → Start Time
├── EndTime → End Time
├── Description → Description
├── TeamMember → Attendee (email)
├── Agenda → Notes/Description
└── Notes → Extended Properties
```

**Google Calendar Event ID Storage:**
- Add `GoogleCalendarEventId` field to `OneOnOne` model
- Store for future updates/deletions

### 1.5 Implementation Steps

1. **Create GoogleAuthService**
   - Handle OAuth2 flow
   - Token management (refresh, storage)
   - User credential management

2. **Create GoogleCalendarService**
   - Create calendar events
   - Update calendar events
   - Delete calendar events
   - List calendar events (for sync)

3. **Update OneOnOne Model**
   - Add `GoogleCalendarEventId` property
   - Add `IsSyncedToGoogle` flag

4. **Create CalendarSyncManager**
   - Coordinate sync operations
   - Handle errors and retries
   - Provide sync status

5. **Update OneOnOneViewModel**
   - Add "Sync to Google Calendar" option
   - Show sync status
   - Handle sync on save

6. **Create Calendar Settings UI**
   - Connect/disconnect Google Calendar
   - Show sync status
   - Configure sync options

---

## Phase 2: Outlook/Office 365 Integration

### 2.1 Prerequisites & Dependencies

**NuGet Packages Required:**
- `Microsoft.Graph` (Latest) - Microsoft Graph API SDK
- `Microsoft.Graph.Auth` (Latest) - Authentication helpers
- `Microsoft.Identity.Client` (MSAL) - OAuth2 authentication

**Azure AD Setup:**
1. Register application in Azure AD
2. Configure API permissions:
   - `Calendars.ReadWrite`
   - `Mail.Send` (for email integration)
   - `User.Read` (basic profile)
3. Create client secret or configure certificate
4. Configure redirect URIs
5. Store credentials securely

### 2.2 Architecture Components

**New Classes/Files:**
```
Tracker/
├── Services/
│   ├── OutlookCalendarService.cs        # Microsoft Graph Calendar API wrapper
│   ├── OutlookMailService.cs            # Microsoft Graph Mail API wrapper
│   └── MicrosoftAuthService.cs          # MSAL authentication handler
└── Classes/
    └── OutlookSettings.cs               # Outlook-specific settings
```

### 2.3 Authentication Flow

**MSAL OAuth 2.0 Flow:**
1. User clicks "Connect Outlook" in Settings
2. Application uses MSAL to open authentication dialog
3. User authenticates with Microsoft account
4. MSAL handles token acquisition and refresh
5. Store tokens securely (encrypted)

**Token Storage:**
- Similar to Google tokens
- Use DPAPI encryption
- Store in `UserSettings`

### 2.4 Data Synchronization

**Sync Direction:** One-Way (Tracker → Outlook) initially

**Data Mapping:**
```
OneOnOne → Outlook Calendar Event
├── Date → Start DateTime
├── StartTime → Start Time
├── EndTime → End Time
├── Description → Subject
├── TeamMember → Attendee (email)
├── Agenda → Body
└── Notes → Extended Properties
```

**Outlook Calendar Event ID Storage:**
- Add `OutlookCalendarEventId` field to `OneOnOne` model

### 2.5 Implementation Steps

1. **Create MicrosoftAuthService**
   - Handle MSAL authentication
   - Token management
   - User credential management

2. **Create OutlookCalendarService**
   - Create calendar events via Microsoft Graph
   - Update calendar events
   - Delete calendar events

3. **Update OneOnOne Model**
   - Add `OutlookCalendarEventId` property
   - Add `IsSyncedToOutlook` flag

4. **Update CalendarSyncManager**
   - Support both Google and Outlook
   - Unified sync interface

---

## Phase 3: Email Integration (Gmail & Outlook)

### 3.1 Gmail Integration

**NuGet Packages:**
- `Google.Apis.Gmail.v1` (Latest)

**Use Cases:**
- Send meeting invitations via email
- Send meeting summaries/notes
- Send action item reminders

**Implementation:**
1. **Create GmailService**
   - Compose and send emails
   - Use Gmail API for sending
   - Handle attachments if needed

2. **Email Templates**
   - Meeting invitation template
   - Meeting summary template
   - Action item reminder template

3. **Integration Points:**
   - "Send Invitation" button in 1:1 dialog
   - "Email Summary" button after meeting
   - Scheduled reminders for action items

### 3.2 Outlook Mail Integration

**Use Cases:** Same as Gmail

**Implementation:**
1. **Create OutlookMailService**
   - Use Microsoft Graph Mail API
   - Compose and send emails
   - Handle attachments

2. **Reuse Email Templates**
   - Same templates for both providers
   - Provider-specific formatting if needed

---

## Phase 4: Unified Calendar Sync Manager

### 4.1 Architecture

**CalendarSyncManager Responsibilities:**
- Manage multiple calendar providers
- Coordinate sync operations
- Handle conflicts and errors
- Provide unified status interface

**Provider Abstraction:**
```csharp
interface ICalendarProvider
{
    Task<bool> ConnectAsync();
    Task<string> CreateEventAsync(OneOnOne meeting);
    Task<bool> UpdateEventAsync(string eventId, OneOnOne meeting);
    Task<bool> DeleteEventAsync(string eventId);
    Task<bool> DisconnectAsync();
    bool IsConnected { get; }
}
```

**Implementations:**
- `GoogleCalendarProvider : ICalendarProvider`
- `OutlookCalendarProvider : ICalendarProvider`

### 4.2 Sync Strategy

**Sync Modes:**
1. **Manual Sync**: User triggers sync explicitly
2. **Auto Sync on Save**: Sync when 1:1 is created/updated
3. **Scheduled Sync**: Background sync at intervals

**Conflict Resolution:**
- **Server Wins**: Calendar event takes precedence
- **Client Wins**: Tracker data takes precedence
- **Manual Resolution**: Prompt user to choose

**Initial Implementation:** Manual sync + Auto sync on save

---

## Phase 5: Settings & Configuration

### 5.1 Calendar Settings Model

```csharp
public class CalendarSettings
{
    // Google Calendar
    public bool GoogleCalendarEnabled { get; set; }
    public string? GoogleAccessToken { get; set; } // Encrypted
    public string? GoogleRefreshToken { get; set; } // Encrypted
    public DateTime? GoogleTokenExpiry { get; set; }
    
    // Outlook Calendar
    public bool OutlookCalendarEnabled { get; set; }
    public string? OutlookAccessToken { get; set; } // Encrypted
    public string? OutlookRefreshToken { get; set; } // Encrypted
    public DateTime? OutlookTokenExpiry { get; set; }
    
    // Sync Options
    public bool AutoSyncOnSave { get; set; } = true;
    public bool SyncMeetingInvitations { get; set; } = true;
    public bool SyncMeetingSummaries { get; set; } = false;
}
```

### 5.2 Settings UI

**Calendar Settings Dialog:**
- Toggle for Google Calendar (Connect/Disconnect)
- Toggle for Outlook Calendar (Connect/Disconnect)
- Sync options checkboxes
- Sync status indicators
- Manual sync button
- Test connection buttons

---

## Phase 6: Future Outlook Integration (Full)

### 6.1 Advanced Features

**Potential Integrations:**
- **Outlook Add-in**: Create Outlook add-in for Tracker
- **Exchange Web Services (EWS)**: Direct Exchange server integration
- **Microsoft Teams Integration**: Sync with Teams meetings
- **Outlook Tasks**: Sync action items with Outlook Tasks
- **Outlook Notes**: Sync meeting notes

### 6.2 Considerations

**When to Implement:**
- After basic calendar sync is stable
- Based on user feedback
- If enterprise customers request it

**Technical Approach:**
- Microsoft Graph API (recommended)
- EWS API (legacy, but still supported)
- Outlook REST API (deprecated)

---

## Security Considerations

### Token Storage
- **Encryption**: Use Windows DPAPI for token encryption
- **Storage Location**: `%LocalAppData%\Tracker\` (encrypted)
- **Never**: Store tokens in plain text or in source code

### Credential Management
- **Client Secrets**: Store encrypted in user settings
- **Certificates**: Use certificate-based auth for production
- **Refresh Tokens**: Handle token refresh automatically

### Permissions
- **Minimal Permissions**: Request only necessary scopes
- **User Consent**: Clear consent dialogs
- **Permission Revocation**: Handle gracefully

---

## Error Handling & Resilience

### Error Scenarios
1. **Network Failures**: Retry with exponential backoff
2. **Token Expiry**: Automatic token refresh
3. **API Rate Limits**: Respect rate limits, queue requests
4. **Invalid Credentials**: Prompt user to re-authenticate
5. **Calendar Not Found**: Create calendar if needed

### Logging
- Log all sync operations
- Log authentication events
- Log errors with context
- User-friendly error messages

---

## Testing Strategy

### Unit Tests
- Mock calendar providers
- Test sync logic
- Test error handling

### Integration Tests
- Test with test Google/Microsoft accounts
- Test authentication flows
- Test sync operations

### User Acceptance Testing
- Test with real calendars
- Verify data accuracy
- Test error scenarios

---

## Implementation Priority

### Phase 1 (High Priority)
1. ✅ Google Calendar integration (one-way sync)
2. ✅ Calendar settings UI
3. ✅ Basic authentication flow

### Phase 2 (High Priority)
1. ✅ Outlook Calendar integration (one-way sync)
2. ✅ Unified calendar sync manager
3. ✅ Error handling and retry logic

### Phase 3 (Medium Priority)
1. Gmail email integration
2. Outlook Mail integration
3. Email templates

### Phase 4 (Low Priority)
1. Two-way sync
2. Conflict resolution
3. Scheduled background sync

### Phase 5 (Future)
1. Full Outlook integration
2. Teams integration
3. Advanced features

---

## Estimated Effort

- **Google Calendar Integration**: 2-3 days
- **Outlook Calendar Integration**: 2-3 days
- **Email Integration (Both)**: 2-3 days
- **Settings UI**: 1 day
- **Testing & Polish**: 2-3 days

**Total Estimated Time**: 9-13 days

---

## Dependencies & Prerequisites

### External Accounts Required
- Google Cloud Console account
- Azure AD account (for Outlook)

### API Access
- Google Calendar API enabled
- Microsoft Graph API access

### User Requirements
- Google account (for Google Calendar)
- Microsoft/Office 365 account (for Outlook)

---

## Notes

- Start with one-way sync (Tracker → Calendar) for simplicity
- Add two-way sync later if needed
- Focus on reliability over features initially
- Consider user feedback before adding advanced features
- Keep authentication flows simple and secure
- Test thoroughly with real accounts before release

