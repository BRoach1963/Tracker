# Shared SQLite Database Setup Guide

## Overview

Tracker now supports configurable database locations, allowing small teams to share a single SQLite database file on a network drive without requiring SQL Server. This is ideal for teams of 2-10 users who want to collaborate but don't have access to a SQL Server instance.

### User-Specific Settings (December 2025)

**Important**: Each Tracker account has isolated settings stored per-user:
- Settings path: `%LocalAppData%\Tracker\Users\{accountId}\TrackerSettings.json`
- Default database: `%LocalAppData%\Tracker\Users\{accountId}\tracker.db`

This means:
- Multiple Tracker accounts on the same Windows machine are fully isolated
- Each user's custom database path preference is stored separately
- Signing out and signing in with a different account loads that account's settings

## When to Use This Feature

### ✅ Good Use Cases
- **Small teams** (2-10 users) without SQL Server
- **Department-level** collaboration within an organization
- **Shared network drive** available to all team members
- **Light to moderate** concurrent usage (mostly reads, occasional writes)
- **Network share** with good connectivity and low latency

### ❌ When to Use SQL Server Instead
- **Large teams** (10+ concurrent users)
- **Heavy concurrent writes** (multiple users editing simultaneously)
- **Enterprise deployment** requiring robust scalability
- **Remote users** over VPN (high network latency)
- **Critical data** requiring advanced backup/recovery features

## Setup Instructions

### Step 1: Prepare the Network Share

1. **Create a shared folder** on a network server or file share
   - Example: `\\server\share\TrackerData`
   - Or: `Z:\TrackerData` (mapped network drive)

2. **Set permissions** - All users need:
   - ✅ Read access
   - ✅ Write access
   - ✅ Modify access
   - ✅ Create files/folders

3. **Test access** from each user's computer:
   ```
   # From File Explorer or Command Prompt
   dir \\server\share\TrackerData
   ```

### Step 2: Configure Tracker (First User)

1. **Launch Tracker** for the first time (or from Settings → Change Database)

2. **Select "Local Database"** on Step 1

3. **Check "Use custom database location"**

4. **Click "Browse..."** and navigate to your network share
   - Example path: `\\server\share\TrackerData\tracker.db`
   - Or use UNC path: `\\192.168.1.100\TrackerData\tracker.db`

5. **Complete setup** and create initial data

### Step 3: Configure Additional Users

Each additional user follows the same steps:

1. Launch Tracker
2. Select Local Database
3. Check "Use custom database location"
4. **Enter the EXACT same path**: `\\server\share\TrackerData\tracker.db`
5. Complete setup (database already exists, so no sample data needed)

## Important Notes

### Concurrent Access Limitations

SQLite uses **file-level locking**, which means:

| Scenario | Supported | Notes |
|----------|-----------|-------|
| Multiple users **reading** | ✅ Yes | Unlimited concurrent reads |
| 2-4 users with **occasional writes** | ✅ Yes | Good performance |
| 5-10 users with **moderate writes** | ⚠️ Maybe | May experience occasional locks |
| 10+ users or **frequent writes** | ❌ No | Use SQL Server |

### Performance Considerations

- **Network latency** affects performance - keep < 10ms for best results
- **Gigabit network** recommended for teams of 5+
- **Avoid WiFi** for network share if possible (use wired connection)
- **Database file grows** - plan for ~100MB per 10,000 records

### Backup Strategy

Since all data is in a single file, backup is simple:

1. **Automated network backups** - Most file servers back up nightly
2. **Manual backup**: Copy `tracker.db` to another location
3. **Scheduled script**:
   ```powershell
   # PowerShell backup script (run daily)
   $source = "\\server\share\TrackerData\tracker.db"
   $backup = "\\server\backups\tracker_backup_$(Get-Date -Format 'yyyyMMdd').db"
   Copy-Item $source $backup
   ```

### Troubleshooting

#### "Database is locked" errors

**Cause**: Another user is writing to the database

**Solutions**:
- Wait a few seconds and retry
- Check if another user is performing bulk operations (import, seed data)
- Consider upgrading to SQL Server for better concurrency

#### Cannot access network path

**Cause**: Permissions or network connectivity issue

**Solutions**:
1. Test network path in File Explorer: `\\server\share\TrackerData`
2. Verify read/write permissions
3. Try UNC path instead of mapped drive: `\\server\share` vs `Z:\`
4. Check firewall settings allow SMB/CIFS traffic (port 445)

#### Slow performance

**Cause**: Network latency or large database

**Solutions**:
- Test network speed: `ping server` (should be < 10ms)
- Use wired connection instead of WiFi
- Archive old data periodically
- Consider moving to SQL Server for larger datasets

## Path Examples

### Valid Paths

```
# UNC path (recommended)
\\fileserver\TrackerData\tracker.db

# UNC with IP address
\\192.168.1.100\shared\Tracker\tracker.db

# Mapped network drive
Z:\TrackerData\tracker.db

# DFS path
\\domain\dfs\TeamData\Tracker\tracker.db
```

### Invalid Paths

```
# HTTP/web paths (not supported)
http://server/tracker.db

# Cloud sync folders (not recommended - conflicts likely)
C:\Users\john\OneDrive\tracker.db
C:\Users\john\Dropbox\tracker.db

# Relative paths (must be absolute)
.\tracker.db
..\shared\tracker.db
```

## Migration Between Locations

### Automatic Migration

When you change database locations in Tracker, **the app will automatically detect your existing database** and offer to migrate it:

1. **Settings** → Change Database → Select new location
2. Tracker detects existing database at old location
3. **Prompt appears**: "An existing database was found. Copy to new location?"
   - **YES** (recommended): Copies database + vector store to new location
   - **NO**: Starts fresh with empty database at new location

### Moving from Local to Network Share

**Option 1: Use Tracker's Built-in Migration (Recommended)**

1. **Settings** → Change Database
2. Select "Use custom database location"
3. Browse to `\\server\share\TrackerData\tracker.db`
4. Click "YES" when prompted to copy existing database
5. Restart Tracker → Now using network share with all your data ✅

**Option 2: Manual Migration**

**Option 2: Manual Migration**

1. **Close Tracker** completely
2. **Copy existing database**:
   - From: `%LocalAppData%\Tracker\tracker.db`
   - To: `\\server\share\TrackerData\tracker.db`
3. **Copy vector store** (if exists):
   - From: `%LocalAppData%\Tracker\vector_store.db`
   - To: `\\server\share\TrackerData\vector_store.db`
4. **Launch Tracker** → Settings → Change Database
5. Select "Use custom database location"
6. Browse to `\\server\share\TrackerData\tracker.db`
7. Click "NO" when prompted (file already copied manually)
8. Restart Tracker

### Moving from Network Share to Local

**Option 1: Use Tracker's Built-in Migration (Recommended)**

1. **Settings** → Change Database
2. **Uncheck** "Use custom database location" (reverts to default local path)
3. Click "YES" when prompted to copy database from network share
4. Restart Tracker → Now using local database with all your data ✅

**Option 2: Manual Migration**

1. **Copy database** from network to local:
   - From: `\\server\share\TrackerData\tracker.db`
   - To: `C:\MyData\tracker.db` (or any local path)
2. **Settings** → Change Database
3. Select "Use custom database location"
4. Browse to local path: `C:\MyData\tracker.db`
5. Restart Tracker

### Migrating to SQL Server

See [Database Migration Guide](DATABASE_MIGRATION.md) for full instructions on:
- Exporting SQLite data
- Deploying SQL Server scripts
- Importing data to SQL Server
- Updating Tracker configuration

## Security Considerations

### Data Protection

- **Network share** should be on a secure internal network
- **File permissions** should restrict access to authorized team members only
- **Encryption**: Enable BitLocker on the file server for encryption at rest
- **VPN required** if accessing from outside the corporate network

### Access Control

SQLite has **no built-in user authentication** - access control is at the **file system level**:

- All users with file access can read **all data**
- No row-level security or user-specific views
- For sensitive data requiring access controls, use SQL Server with authentication

## FAQ

**Q: Can I use OneDrive, Dropbox, or Google Drive for the shared database?**

A: ❌ **Not recommended**. Cloud sync can cause database corruption when multiple users write simultaneously. SQLite is not designed for cloud-synced folders. Use a traditional network share or SQL Server instead.

**Q: How many users can realistically share a SQLite database?**

A: **2-4 users comfortably**, up to **10 users** with light usage. Beyond that, you'll experience locking conflicts and should migrate to SQL Server.

**Q: What happens if the network goes down?**

A: Tracker will be unable to access data until network connectivity is restored. Consider SQL Server with offline mode for better resilience.

**Q: Can I switch between local and network paths easily?**

A: Yes! Go to Settings → Database → Change Database, then select the new location. Tracker will reconnect to whichever database file you point it to.

**Q: Does this work on Mac or Linux?**

A: Currently Tracker is Windows-only. When the Mac version is released (via .NET MAUI), it will support SMB network shares the same way (e.g., `smb://server/share/tracker.db`).

## Support

For additional help or issues:
- Check [Troubleshooting Guide](TROUBLESHOOTING.md)
- Review [Database Optimization](DATABASE_OPTIMIZATION_REPORT.md) for performance tips
- Contact support: support@trackerapp.com
- Community forum: https://community.trackerapp.com

---

**Last Updated**: December 2025  
**Tracker Version**: 1.0+  
**Feature**: Custom SQLite Database Location
