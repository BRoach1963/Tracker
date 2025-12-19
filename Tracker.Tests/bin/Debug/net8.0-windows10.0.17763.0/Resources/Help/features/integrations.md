# Integrations

Connect Tracker with your existing tools for seamless workflow management.

## Microsoft 365 Integration (Standard+)

*Requires Standard or Pro subscription.*

### What's Included

| Feature | Standard | Pro |
|---------|----------|-----|
| Calendar Sync (Outlook) | ✓ | ✓ |
| Teams Messaging | ✓ | ✓ |
| Slack Messaging | ✓ | ✓ |
| Email Sending | ✓ | ✓ |
| Teams Meeting Links | ✓ | ✓ |
| Profile Photos from Azure AD | ✓ | ✓ |
| Presence/Availability Status | ✓ | ✓ |

### Connecting to Microsoft 365

1. Go to **Settings** → **Integrations**
2. Click **Connect Microsoft 365**
3. Sign in with your Microsoft account
4. Grant the requested permissions
5. Connection status shows "Connected"

### Permissions Explained

When you connect, Tracker requests these permissions:

| Permission | Purpose |
|------------|---------|
| Read your profile | Display your name and email |
| Read/write calendar | Sync 1:1 meetings with Outlook |
| Send mail | Send meeting summaries and messages |
| Create online meetings | Generate Teams meeting links |
| Read user photos | Show team member photos from directory |
| Read presence | Show availability status (🟢 Available, etc.) |

**Your data is secure**: Tracker only accesses what's needed and doesn't store your Microsoft password.

---

## Calendar Sync

### How It Works

When you create or edit a 1:1 meeting in Tracker:

1. **Create**: Meeting appears in your Outlook calendar
2. **Update**: Changes sync automatically
3. **Delete**: Calendar event is removed
4. **Conflict**: Tracker notifies you of external changes

### Sync Settings

- **Auto-sync**: Enabled by default
- **Sync interval**: Every 5 minutes + on focus
- **Sync range**: 3 months back, 6 months forward

### Troubleshooting Sync

| Issue | Solution |
|-------|----------|
| Events not appearing | Check connection, try manual sync |
| Duplicate events | Delete duplicate, re-sync |
| Wrong time | Verify timezone settings |
| Missing updates | Refresh with F5, check sync status |

---

## Teams Meeting Links

### Automatic Meeting Links

When creating a 1:1, Tracker can automatically generate a Teams meeting link.

**To enable**:
1. Ensure Microsoft 365 is connected
2. When creating a 1:1, check "Create Teams Meeting"
3. A join link is automatically generated

### Using Meeting Links

The Teams link appears:
- In the 1:1 details
- In the Outlook calendar invite
- Can be copied to share manually

### Meeting Link Format

```
📅 1:1 with Sarah Chen - Friday 2pm
🔗 Join: https://teams.microsoft.com/l/meetup-join/...
```

---

## Quick Messaging

### Send Messages from Tracker

Quickly send Teams, Slack, or email messages to team members without leaving Tracker.

**To send a message**:
1. Open a Team Member or 1:1 dialog
2. Click the **💬 Message** button
3. Choose **Teams**, **Slack**, or **Email** (only connected services appear)
4. Select a template or write custom message
5. Click **Send**

### Message Templates

| Template | Best For |
|----------|----------|
| Pre-meeting Reminder | "Our 1:1 is tomorrow..." |
| Action Item Check-in | "How's [task] going?" |
| Kudos | Quick recognition |
| Meeting Rescheduled | "I've moved our 1:1..." |
| 1:1 Summary | Post-meeting email recap |
| Prep Request | "What would you like to discuss?" |

### Send Meeting Summary

After completing a 1:1:
1. Click **📧 Send Summary** in the 1:1 dialog
2. Review the auto-generated summary
3. Edit if needed
4. Send via email

---

## Profile Photos

### Automatic Photo Sync

When Microsoft 365 is connected, Tracker automatically fetches profile photos from your organization's Azure Active Directory.

**Benefits**:
- No manual photo uploads needed
- Photos stay current with directory
- Consistent with other Microsoft apps

**How it works**:
1. Team member's email matches Azure AD user
2. Photo is fetched and cached
3. Falls back to local photo if unavailable

### Cache Behavior

- Photos cached for 24 hours
- Refresh by reconnecting M365
- Local photos override if set manually

---

## Presence / Availability Status

### What is Presence?

Presence shows your team members' real-time availability status from Microsoft Teams.

### Status Indicators

| Indicator | Meaning |
|-----------|---------|
| 🟢 Available | Free and online |
| 🔴 Busy | In a meeting or on a call |
| 📞 In a call | On a phone/video call |
| 📅 In a meeting | In a scheduled meeting |
| 📺 Presenting | Sharing screen |
| ⛔ Do not disturb | Notifications blocked |
| 🟡 Away | Stepped away |
| ⚫ Offline | Not signed in |
| 🏖️ Out of office | OOO auto-reply enabled |

### Where It Appears

- Team member cards
- 1:1 dialog (next to team member)
- Dashboard quick view

### Use Cases

- **Best time to message**: See 🟢 before sending
- **Don't interrupt**: Respect 🔴 or ⛔ status
- **Schedule appropriately**: Avoid 🏖️ dates

---

## Slack Integration (Standard+)

*Requires Standard or Pro subscription.*

### What's Included

| Feature | Standard | Pro |
|---------|----------|-----|
| Slack DMs | ✓ | ✓ |
| Presence/Status Sync | ✓ | ✓ |
| Profile Photos | ✓ | ✓ |
| Rich Message Templates | ✓ | ✓ |

### Connecting to Slack

1. Go to **Settings** → **Integrations**
2. Click **Connect Slack**
3. Tracker validates the pre-configured bot token
4. Connection status shows "Connected" with your workspace name

### Permissions Explained

| Permission | Purpose |
|------------|---------|
| chat:write | Send direct messages |
| users:read | Read user profiles and presence |
| users:read.email | Match team members by email |
| im:write | Open DM conversations |

### Sending Slack Messages

When Slack is connected, it appears as an option in the Quick Message dialog:

1. Open a Team Member or 1:1 dialog
2. Click the **💬 Message** button
3. Choose **# Slack** (alongside Teams and Email)
4. Write your message or use a template
5. Click **Send**

The message will be sent as a DM to the team member's Slack account (matched by email address).

### Slack Presence

When connected, Tracker shows team members' Slack presence:

| Indicator | Meaning |
|-----------|---------|
| 🟢 Active | User is online and active |
| 🔘 Away | User has been inactive |
| ⚪ Unknown | Presence unavailable |

**Combined Presence**: Tracker shows the best available status from Microsoft 365 or Slack. If the team member has both, Microsoft 365 takes priority.

### Profile Photos

Slack profile photos are automatically fetched and used if:
- No local photo is set
- No Azure AD photo is available
- Team member's email matches their Slack account

---

## Google Workspace Integration (Standard+)

*Requires Standard or Pro subscription.*

### What's Included

| Feature | Standard | Pro |
|---------|----------|-----|
| Google Calendar Sync | ✓ | ✓ |
| Gmail Integration | ✓ | ✓ |
| Google Meet Links | ✓ | ✓ |
| Contact Photos | ✓ | ✓ |

### Connecting to Google

1. Go to **Settings** → **Integrations**
2. Click **Connect Google**
3. Sign in with your Google account
4. Grant the requested permissions
5. Connection status shows "Connected"

### Permissions Explained

| Permission | Purpose |
|------------|---------|
| View/edit calendar | Sync 1:1 meetings |
| Send email (Gmail) | Send meeting summaries |
| View contacts | Show profile photos |
| Create calendar events | Auto-create Google Meet |

### Google Meet Links

When creating a 1:1:
1. Check "Create Google Meet Link"
2. A join link is automatically generated
3. Link appears in Google Calendar event

### Gmail Integration

Send emails directly from Tracker using your Gmail account:
- Meeting summaries
- Reminders
- Custom messages

### Calendar Sync

Works identically to Outlook sync:
- Create meeting → appears in Google Calendar
- Update meeting → Calendar updated
- Delete meeting → Event removed

---

## Security & Privacy

### Authentication

- **OAuth 2.0**: Industry-standard secure auth
- **No passwords stored**: Only encrypted tokens
- **Revoke anytime**: Disconnect removes access

### Data Handling

- Only necessary data is transmitted
- No data stored on third-party servers
- All processing happens locally

### Revoking Access

**To disconnect Microsoft 365**:
1. Go to **Settings** → **Integrations**
2. Click **Disconnect** next to Microsoft 365
3. Confirm disconnection

**To remove from Microsoft side**:
1. Go to [account.microsoft.com/permissions](https://account.microsoft.com/permissions)
2. Find "Tracker"
3. Click "Remove access"

**To disconnect Slack**:
1. Go to **Settings** → **Integrations**
2. Click **Disconnect** next to Slack
3. Confirm disconnection

**To remove from Slack side**:
1. Go to your Slack workspace settings
2. Navigate to **Apps & integrations**
3. Find "Tracker" and revoke access

---

## Troubleshooting

### Connection Issues

| Problem | Solution |
|---------|----------|
| "Sign-in failed" | Try again, check network |
| "Permission denied" | Ensure admin consent if required |
| "Token expired" | Reconnect integration |

### Sync Problems

| Problem | Solution |
|---------|----------|
| Changes not syncing | Check connection status |
| Duplicates appearing | Clear cache, re-sync |
| Old data showing | Force refresh (F5) |

### Permission Errors

Some organizations require admin approval for app permissions. Contact your IT administrator if you see "Admin approval required."

---

## Related Topics

- [Settings](settings.md)
- [1:1 Meetings](one-on-ones.md)
- [Team Members](team-members.md)
- [AI Help Bot](ai-help-bot.md)

