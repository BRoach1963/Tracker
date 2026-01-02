# Troubleshooting

Common issues and their solutions.

## Application Issues

### App won't start

**Symptoms:** Application fails to launch or crashes immediately.

**Solutions:**
1. Check Windows Event Viewer for error details
2. Delete the settings file:
   - Anonymous/pre-login: `%LOCALAPPDATA%\Tracker\TrackerSettings.json`
   - Per-user settings: `%LOCALAPPDATA%\Tracker\Users\{user-id}\TrackerSettings.json`
3. Ensure .NET 8 runtime is installed
4. Try running as Administrator

### Slow performance

**Symptoms:** Application is sluggish or unresponsive.

**Solutions:**
1. Check if many items are loaded (consider archiving old data)
2. Close other memory-intensive applications
3. Check available disk space
4. View logs in Settings > Log Viewer for errors

## Database Issues

### "Database is locked" error

**Symptoms:** Operations fail with database locked message.

**Solutions:**
1. Close any other instances of Tracker
2. Check for Tracker processes in Task Manager
3. Restart the application
4. If persists, restart your computer

### Data not saving

**Symptoms:** Changes don't persist after closing.

**Solutions:**
1. Check disk space on the drive where database is stored
2. Verify write permissions to `%LOCALAPPDATA%\Tracker`
3. Check if antivirus is blocking writes
4. View log files for error details

### Missing data after update

**Symptoms:** Data seems to have disappeared.

**Solutions:**
1. Check if you're logged in with the correct Tracker account
2. Data and settings are account-specific - each Tracker account has its own isolated data
3. Settings are stored per-account in `%LOCALAPPDATA%\Tracker\Users\{user-id}\`
4. Check for backup files in the Tracker folder

## Calendar Integration

### Google Calendar not syncing

**Symptoms:** 1:1s don't appear in Google Calendar.

**Solutions:**
1. Go to Settings > Calendar
2. Disconnect and reconnect Google Calendar
3. Verify internet connection
4. Check Google account permissions

### Authorization errors

**Symptoms:** "Authorization failed" when connecting calendar.

**Solutions:**
1. Clear browser cache and cookies
2. Try a different browser for authorization
3. Check if Google account has 2FA - may need app password
4. Verify popup blockers aren't blocking the auth window

## Display Issues

### Text is too small/large

**Solutions:**
1. Adjust Windows display scaling
2. Check Settings > Appearance for display options
3. Try a different theme

### Theme not applying

**Solutions:**
1. Go to Settings > Appearance
2. Select a different theme
3. Restart the application

### Missing icons or images

**Solutions:**
1. Reinstall the application
2. Clear Windows icon cache
3. Check if font files are installed

## Viewing Logs

To diagnose issues:

1. Go to **Settings** in the sidebar
2. Scroll to **Log Viewer**
3. Use filters to find relevant entries
4. Look for ERROR or WARN level messages

### Log file location

Logs are stored in:
```
%LOCALAPPDATA%\Tracker\Logs\
```

## Getting Help

If these solutions don't resolve your issue:

1. **Check for updates** - Make sure you're on the latest version
2. **Export logs** - Use Settings > Log Viewer > Export
3. **Contact support** - Include log files and steps to reproduce

---

*See also:*
- [Settings](../dialogs/settings.md)
- [Overview](../getting-started/overview.md)

