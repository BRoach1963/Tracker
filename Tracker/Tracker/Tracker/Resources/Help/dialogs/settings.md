# Settings

The Settings page allows you to configure Tracker to work the way you want.

## Accessing Settings

Click **Settings** in the sidebar to open the settings page.

## General Settings

Configure general application behavior and appearance.

### Theme Selection

Choose from available themes:
- **Default** - Dark theme with gold accents
- **Light** - Clean light theme
- **Modern** - Contemporary styling
- **Spicy** - Bold colors

### Display Options

- Grid density settings
- Date/time format preferences
- Language selection (if available)

### Startup Options

- Launch on Windows startup
- Remember window position
- Start minimized to tray

## Database Settings

Configure your database connection and manage data.

### Local Database (SQLite)

- Default for single-user installations
- Data stored on your local machine
- No network required
- File location displayed

### Remote Database (SQL Server)

- For team/enterprise installations
- Shared data across multiple users
- Requires network access

### Database Management

| Action | Description |
|--------|-------------|
| **Clear All Data** | Removes all data (use with caution!) |
| **Add Sample Data** | Populates with example data for testing |
| **Export Data** | Backup your data to a file |
| **Import Data** | Restore from a previous backup |

> ⚠️ **Warning**: Clear All Data cannot be undone. Export your data first if you need to keep it.

### Backup & Restore

- **Export** - Save all data to a JSON file
- **Import** - Restore from a backup file
- Recommended: Regular backups to external storage

## Calendar Settings

Connect to external calendars for meeting synchronization.

### Google Calendar

1. Click **Connect Google Calendar**
2. Sign in with your Google account
3. Authorize Tracker to access your calendar
4. 1:1s will sync automatically

### Outlook Calendar

(Coming soon)

### Calendar Sync Options

- Sync direction (one-way or two-way)
- Sync frequency
- Which calendars to include

## Reminders Settings

Configure the reminder and notification system.

### Enable Reminders

Toggle the entire reminder system on or off.

### Meeting Reminders

| Setting | Description |
|---------|-------------|
| **Enable** | Turn meeting reminders on/off |
| **Lead Time** | How far in advance (15, 30, 60 min) |
| **Sound** | Play notification sound |

### Task Reminders

| Setting | Description |
|---------|-------------|
| **Enable** | Turn task reminders on/off |
| **Due Date Alert** | Days before due date to remind |
| **Overdue Alert** | Remind about overdue tasks |

### Engagement Alerts

Get notified when team members need attention:

| Setting | Description |
|---------|-------------|
| **Enable** | Turn engagement alerts on/off |
| **Threshold** | Days without 1:1 before alerting |
| **Include Goals** | Alert for stalled goals |

### Notification Options

| Setting | Description |
|---------|-------------|
| **Toast Notifications** | Show Windows toast notifications |
| **In-App Alerts** | Show notifications within Tracker |
| **System Tray** | Show balloon tips from tray |
| **Sound** | Play sounds for alerts |

### Minimize to Tray

- Enable to keep Tracker running in system tray
- Click tray icon to restore window
- Right-click for quick actions

## Logs Viewer

View application logs for troubleshooting and auditing.

### Log Display

- View log entries in real-time
- Entries show timestamp, level, source, and message
- Color-coded by severity (Info, Warning, Error)

### Filtering

| Filter | Description |
|--------|-------------|
| **Time Range** | Show logs from specific period |
| **Search** | Find text in log messages |
| **Level** | Filter by severity level |

### Log Management

| Action | Description |
|--------|-------------|
| **Refresh** | Reload log entries |
| **Clear** | Remove old log entries |
| **Export** | Save logs to file for support |

### Log Retention

- Logs automatically roll over after 1 week
- Old log files are deleted automatically
- Export important logs before they expire

### Troubleshooting

If you encounter issues:
1. Check the logs for errors
2. Look for red (Error) or yellow (Warning) entries
3. Export logs and contact support if needed

---

*See also:*
- [Overview](../getting-started/overview.md)
- [Troubleshooting](../reference/troubleshooting.md)

