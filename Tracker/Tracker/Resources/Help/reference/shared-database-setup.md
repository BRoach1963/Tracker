# Shared Database Setup

Learn how to set up Tracker for team collaboration using a shared network folder.

## Overview

Tracker supports three database configurations:

| Configuration | Users | Setup | Best For |
|---------------|-------|-------|----------|
| **Local** | 1 | None | Individual users |
| **Shared (Custom)** | 2-10 | Simple | Small teams, no SQL Server |
| **SQL Server** | 10+ | Advanced | Enterprise teams |

This guide focuses on **Shared Database** using a custom location on a network share.

## When to Use Shared Database

### ✅ Good Fit
- Team size: 2-10 people
- Have a shared network folder/drive
- All users on same network
- Don't have SQL Server
- Want simple setup

### ❌ Not a Good Fit
- Team size: 10+ concurrent users
- Remote team over VPN (high latency)
- Need advanced security/permissions
- Require offline access
- Already have SQL Server

**For larger teams**, see [SQL Server Setup](sql-server-setup.md)

## Prerequisites

Before starting, ensure:

1. ✅ **Network share exists** and all users can access it
2. ✅ **Read/write permissions** for all team members
3. ✅ **Tracker installed** on each user's computer
4. ✅ **Network connectivity** between all computers

### Testing Network Access

Each user should test:

```
1. Open File Explorer
2. Type in address bar: \\fileserver\TrackerData
3. Can you open the folder? ✅
4. Right-click → New → Text Document
5. Can you create a file? ✅
6. Can you delete it? ✅
```

If any step fails, check permissions with your IT team.

## Setup Process

### For the First User (Database Creator)

#### 1. Prepare Network Location

Create a dedicated folder for Tracker:
- Example: `\\fileserver\TrackerData`
- Or: `Z:\SharedData\TrackerData` (mapped drive)

#### 2. Launch Tracker Setup

1. Install and open Tracker
2. Setup Wizard appears automatically

#### 3. Configure Custom Location

On the "Choose Database" screen:

1. Click **Local Database** card
2. ✅ Check **"Use custom database location"**
3. New options appear below
4. Click **Browse...**
5. Navigate to your network share
6. Enter filename: `tracker.db`
7. Click **Save**
8. Verify path shows: `\\fileserver\TrackerData\tracker.db`

#### 4. Review Warning

Read the yellow warning panel:
- SQLite works best with 2-4 concurrent users
- May see occasional locks with 5-10 users
- For 10+ users, use SQL Server instead
- Network latency affects performance

#### 5. Complete Setup

1. Click **Next**
2. Complete Account Setup (or skip)
3. Review Summary
4. ✅ Check "Include sample data" (optional, for testing)
5. Click **Finish**
6. Tracker creates the database and opens

#### 6. Verify Files Created

Check the network share:
- `tracker.db` ✅ (main database)
- `vector_store.db` ✅ (AI search)

#### 7. Share Path with Team

Send this to your team members:

```
Network path: \\fileserver\TrackerData\tracker.db

Instructions:
1. Install Tracker
2. During setup, choose "Local Database"
3. Check "Use custom database location"
4. Enter the path above
5. Complete setup

Do NOT check "Include sample data" - database already has data!
```

### For Additional Users (Joining Shared Database)

#### 1. Install Tracker

Download and install Tracker on your computer.

#### 2. Launch Setup Wizard

Open Tracker - Setup Wizard appears.

#### 3. Enter Shared Path

1. Select **Local Database**
2. ✅ Check **"Use custom database location"**
3. **Type or paste** the exact path:
   ```
   \\fileserver\TrackerData\tracker.db
   ```
4. Or click **Browse...** and navigate to the file

**Critical**: Path must match EXACTLY what first user shared!

#### 4. Complete Setup

1. Click **Next**
2. Complete Account Setup (or skip)
3. On Summary screen:
   - ⚠️ **UNCHECK** "Include sample data"
   - Database already contains data!
4. Click **Finish**

#### 5. Verify Shared Access

When Tracker opens:
- Do you see team members added by others? ✅
- Can you add a test team member? ✅
- Do others see your changes? ✅

## Testing Shared Database

### Quick 2-Person Test

**User 1**:
1. Open Tracker
2. Go to Circle → Team
3. Add team member: "Test User"
4. Note the time

**User 2**:
1. Open Tracker (or refresh if already open)
2. Go to Circle → Team
3. Do you see "Test User"? ✅

If both users see the same data → Success! 🎉

## Changing Database Location Later

### Migrating Existing Data

If you want to move your database to a different location:

1. **Settings** → **Database** → **Change Database Connection**
2. Select new location
3. Tracker prompts: **"Copy existing database to new location?"**
4. Click **YES** → Automatic migration! ✅
5. No manual file copying needed

### From Local to Network

Already using local database and want to share with team:

1. Settings → Change Database
2. Select "Local Database"
3. ✅ Check "Use custom database location"
4. Browse to: `\\fileserver\TrackerData\tracker.db`
5. Click **YES** when prompted to copy
6. Your data automatically moves to network share ✅

### From Network to Local

Want to stop sharing and go back to local:

1. Settings → Change Database
2. Select "Local Database"
3. ⬜ **Uncheck** "Use custom database location"
4. Click **YES** when prompted to copy
5. Database moves back to local machine ✅

## Troubleshooting

### "Cannot access network path"

**Symptoms**: Error connecting to `\\fileserver\TrackerData\tracker.db`

**Solutions**:
1. Open File Explorer and verify path exists
2. Check read/write permissions
3. Use UNC path (`\\server\share`), not mapped drive (`Z:\`)
4. Test: `dir \\fileserver\TrackerData` from Command Prompt
5. Contact IT if network share is inaccessible

### "Database is locked"

**Symptoms**: Error when saving data: "Database is locked"

**Cause**: Another user is writing at the same time (SQLite limitation)

**Solutions**:
1. Wait 5 seconds and try again
2. Check if another user is doing bulk operations
3. If happens frequently: too many users for SQLite
   - **Upgrade to SQL Server** for better concurrency

### Data not syncing

**Symptoms**: User adds data, others don't see it

**Solutions**:
1. **Refresh**: Close and reopen Tracker
2. **Verify paths match**:
   - User 1: Settings → Database → Check path
   - User 2: Settings → Database → Check path
   - Must be IDENTICAL!
3. **Check file modification**:
   - Right-click `tracker.db` → Properties
   - Does "Modified" timestamp change when data is added?

### Slow performance

**Symptoms**: Tracker is sluggish, actions take long

**Causes & Fixes**:

1. **Network latency**:
   - Test: `ping fileserver` (should be < 10ms)
   - Use wired connection, not WiFi
   
2. **Large database**:
   - Check size: Right-click tracker.db → Properties
   - If > 500MB, archive old data
   
3. **Too many users**:
   - SQLite: 2-4 users = good, 5-10 = okay, 10+ = migrate to SQL Server

## Best Practices

### ✅ DO

- Test network path before rolling out
- Use UNC paths (`\\server\share`)
- Keep team size under 10 for SQLite
- Set up automatic backups
- Use wired networks when possible
- Document the path for new team members

### ❌ DON'T

- Use OneDrive, Dropbox, or Google Drive folders (corruption risk!)
- Mix local and network - all users must use same path
- Exceed 10 concurrent users on SQLite
- Use WiFi for file server if avoidable
- Store on removable/disconnectable drives

## Backup Strategy

### Automatic Network Backups

Most file servers automatically back up:
- Check with IT team about backup schedule
- Verify backups include your TrackerData folder

### Manual Backup

Quick backup anytime:

```
1. Close Tracker on ALL computers
2. Copy these files to backup location:
   - tracker.db
   - vector_store.db
3. Label backup with date: tracker_backup_2025-12-24.db
```

### Scheduled PowerShell Backup

Create a daily backup script:

```powershell
# Save as: Backup-Tracker.ps1
$source = "\\fileserver\TrackerData\tracker.db"
$backup = "\\fileserver\Backups\tracker_$(Get-Date -Format 'yyyyMMdd').db"
Copy-Item $source $backup
```

Run via Task Scheduler daily.

## Upgrading to SQL Server

When your team outgrows SQLite:

### Signs You Need SQL Server

- ⚠️ More than 10 concurrent users
- ⚠️ Frequent "database locked" errors
- ⚠️ Remote users over VPN
- ⚠️ Performance degradation
- ⚠️ Need advanced permissions

### Migration Process

1. Deploy SQL Server database (see [SQL Server Setup](sql-server-setup.md))
2. Export data from SQLite
3. Import to SQL Server
4. Update all users: Settings → Change Database → SQL Server
5. Enter server details
6. Old SQLite file remains as backup

See [Database Migration Guide](../../Docs/DATABASE_OPTIMIZATION_REPORT.md) for details.

## FAQ

**Q: How many users can share a SQLite database?**

A: 2-4 users comfortably, up to 10 with light usage. Beyond that, use SQL Server.

**Q: Can I use OneDrive or Dropbox for the shared database?**

A: **No!** Cloud sync folders cause database corruption when multiple users write simultaneously. Use a traditional network share only.

**Q: What if the file server goes offline?**

A: Tracker cannot access data until the server is back. No offline mode for shared SQLite. For resilience, use SQL Server with offline support.

**Q: Can Mac users connect too?**

A: Tracker is currently Windows-only. When Mac version launches (MAUI), it will support SMB shares.

**Q: How do I remove a user's access?**

A: Remove their network share permissions via Windows file sharing settings. Tracker has no built-in user authentication for SQLite.

**Q: Can I have some users on local and some on network?**

A: Technically yes, but they won't see each other's data. All users must use the SAME path to share data.

## Getting Help

Need assistance?

- 📖 [Troubleshooting Guide](troubleshooting.md)
- 📖 [Detailed Setup Guide](../../SHARED_DATABASE_QUICK_START.md)
- 📧 Email: support@trackerapp.com
- 💬 Forum: https://community.trackerapp.com

---

*Last updated: December 2025*
