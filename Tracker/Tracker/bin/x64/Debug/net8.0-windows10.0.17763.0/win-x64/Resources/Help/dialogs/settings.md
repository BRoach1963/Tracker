# Settings

The Settings page allows you to configure Tracker to work the way you want.

## Accessing Settings

Click **Settings** in the sidebar to open the settings page.

## General Settings {#general-settings}

### Theme Selection

Choose from available themes:
- **Default** - Dark theme with gold accents
- **Light** - Clean light theme
- **Modern** - Contemporary styling
- **Spicy** - Bold colors

Theme changes are applied immediately and saved automatically.

## Account Settings {#account-settings}

Manage your user account and authentication preferences.

### Current Account

Displays your current logged-in user and Windows account information.

### Windows Authentication

Windows Authentication allows Tracker to automatically log you in using your Windows user account. This means:

- **No login screen** - You go straight to the app on startup
- **Automatic authentication** - Your Windows credentials identify you
- **Your data stays yours** - All data is still filtered by your user account

#### Enable Windows Authentication

1. Go to **Settings** → **Account**
2. Click **Enable Windows Authentication**
3. Confirm the action

Once enabled:
- The app will automatically log you in on startup
- Your Windows username is used to identify your account
- All your data (team members, 1:1s, tasks, etc.) is associated with YOUR account

#### Disable Windows Authentication

1. Go to **Settings** → **Account**
2. Click **Disable Windows Authentication**
3. Confirm the action

Once disabled, you'll see the login screen on each startup.

> **Note:** Windows Authentication doesn't share your data with anyone. It simply uses your Windows username to automatically identify you instead of requiring you to log in manually each time.

## Database Settings {#database-settings}

Configure your database connection and location.

### Database Types

#### Local Database (SQLite)
- **Default** for single-user installations
- Data stored on your local machine (`%LocalAppData%\Tracker\`)
- No network required
- Fast and simple

#### Custom Location (Shared SQLite)
- **For small teams** (2-10 users)
- SQLite database stored on a network share
- All team members point to the same file
- Example: `\\fileserver\TrackerData\tracker.db`
- Requires network share with read/write permissions

**When to use Custom Location**:
- Small team wants to share data
- No SQL Server available
- Simple setup preferred
- 2-10 concurrent users

**Limitations**:
- 2-4 users work great, 5-10 may see occasional "database locked" errors
- Network latency affects performance
- All users need network access to the share

#### Remote Database (SQL Server)
- **For enterprise teams** (10+ users)
- Shared data across many users
- Better concurrency and performance
- Requires SQL Server installation
- Pro plan required

### Changing Database Location

#### Moving to Network Share (Team Sharing)

1. Click **Change Database Connection**
2. Select **Local Database**
3. ✅ Check **"Use custom database location"**
4. Click **Browse...** 
5. Navigate to network share: `\\fileserver\TrackerData\`
6. Enter filename: `tracker.db`
7. Click **Save**
8. When prompted: **"Copy existing database to new location?"**
   - Click **YES** to migrate your data automatically ✅
   - Click **NO** to start fresh at new location
9. Click **Finish**
10. Restart Tracker

**Automatic Migration**: Tracker will detect your existing database and offer to copy it to the new location - no manual file copying needed!

#### Reverting to Local Database

1. Click **Change Database Connection**
2. Select **Local Database**
3. ⬜ **Uncheck** "Use custom database location"
4. When prompted, click **YES** to copy network database back to local
5. Click **Finish**
6. Restart Tracker

### Database Path Display

The current database location is shown in Settings:

- **Local**: `C:\Users\YourName\AppData\Local\Tracker\tracker.db`
- **Custom**: `\\fileserver\TrackerData\tracker.db`
- **SQL Server**: `ServerName/DatabaseName`

### Database Management

#### Clear All Data
Removes **all data permanently** from the current database:
- All team members
- All 1:1 meetings
- All tasks, projects, OKRs, KPIs
- All notes and reminders

⚠️ **Warning**: This cannot be undone! Back up first.

#### Add Sample Data
Populates the database with example data for testing:
- Sample team members
- Example 1:1 meetings
- Test tasks and projects
- Demo OKRs and KPIs

Useful for trying out features or training.

### Shared Database Quick Start

For detailed step-by-step instructions on setting up a shared database for your team, see:
- [Shared Database Setup Guide](../../SHARED_DATABASE_QUICK_START.md)

**Quick summary**:
1. First user: Set custom location to `\\server\share\tracker.db`
2. Other users: Enter the EXACT same path
3. All users share the same data automatically

## Calendar Settings {#calendar-settings}

Connect to external calendars for 1:1 synchronization.

### Google Calendar

1. Click **Connect Google Calendar**
2. Sign in with your Google account
3. Authorize Tracker to access your calendar
4. 1:1s will sync automatically

### Outlook Calendar

1. Click **Connect Outlook Calendar**
2. Sign in with your Microsoft account
3. Authorize Tracker
4. 1:1s will sync to your Outlook calendar

### Sync Options

- **Auto-sync on save** - Automatically sync when creating/updating meetings
- **Send invitations** - Send calendar invitations to team members
- **Send summaries** - Email meeting summaries after 1:1s

## Reminders Settings {#reminders-settings}

Configure the reminder and notification system.

### General

- **Enable reminders** - Toggle all reminders on/off

### System Tray

- **Minimize to tray** - Keep app running in system tray when closed
- **Start with Windows** - Launch Tracker when Windows starts

### Meeting Reminders

- **Show meeting reminders** - Get notified before 1:1s
- **Reminder timing** - Choose 5, 10, 15, 30 minutes, or 1 hour before
- **Day-before reminder** - Optional reminder the day before

### Engagement Alerts

- **Enable alerts** - Notify when team members haven't had a 1:1
- **Alert threshold** - 1, 2, 3, or 4 weeks without a 1:1

### Task & Goal Reminders

- **Task reminders** - Notify before task due dates
- **Goal reminders** - Notify before goal target dates

### Other Settings

- **Play sound** - Audio notification with alerts
- **Default snooze** - How long to snooze dismissed reminders

## Log Viewer {#logs-viewer}

View application logs for troubleshooting:

- **Search** - Filter logs by text content
- **Time filter** - Show logs from specific date ranges
- **Clear logs** - Remove old log entries
- **Auto-cleanup** - Logs older than 1 week are automatically removed

## Understanding User Data

All data in Tracker is associated with YOUR user account:

- **Team members** you add belong to you
- **1:1 meetings** you create are yours
- **Tasks, projects, OKRs, KPIs** - all filtered by your user
- **Notes, reminders, feedback** - private to your account

When you log in (manually or via Windows Authentication), the app loads only YOUR data. Other users on the same database see only their own data.

---

*See also:*
- [Overview](../getting-started/overview.md)
- [Troubleshooting](../reference/troubleshooting.md)

