# Custom SQLite Database Location - Implementation Summary

## Overview

Added support for configurable SQLite database locations, enabling small teams (2-10 users) to share a single database file on a network drive without requiring SQL Server.

### User-Specific Settings (December 2025 Update)

Settings are now stored **per-user** based on the logged-in Supabase account:
- **Anonymous/pre-login**: `%LocalAppData%\Tracker\TrackerSettings.json`
- **Per-user settings**: `%LocalAppData%\Tracker\Users\{supabaseUserId}\TrackerSettings.json`
- **Per-user default database**: `%LocalAppData%\Tracker\Users\{supabaseUserId}\tracker.db`

This ensures that multiple Supabase users on the same Windows machine have completely isolated settings and database configurations.

## Changes Made

### 1. Database Configuration (`DatabaseSettings.cs`)

**Added Property**:
```csharp
public string CustomSqlitePath { get; set; } = string.Empty;
```

**Updated Method**: `GetConnectionString()`
- Now checks if `CustomSqlitePath` is set
- If set, uses custom path and creates directory if needed
- Otherwise, falls back to default `%LocalAppData%\Tracker\tracker.db`

### 2. Setup Wizard ViewModel (`SetupWizardViewModel.cs`)

**Added Fields**:
- `_customSqlitePath`: Stores the custom database path
- `_useCustomSqlitePath`: Toggle for enabling custom path
- `_browseSqlitePathCommand`: Command for file browser dialog

**Added Properties**:
- `UseCustomSqlitePath`: Binds to checkbox in UI
- `CustomSqlitePath`: Binds to path textbox
- `CustomSqlitePathDisplay`: Display-friendly path string
- `ShowCustomSqlitePath`: Visibility binding for custom path UI

**Added Command**:
- `BrowseSqlitePathCommand`: Opens SaveFileDialog to select database location

**Updated Method**: `BuildDatabaseSettings()`
- Now includes `CustomSqlitePath` when building settings for initialization

**Added Method**: `MigrateExistingDatabaseIfNeeded()`
- **Automatic migration detection** when changing database locations
- Compares old vs new database paths
- Prompts user: "Copy existing database to new location?"
- Copies both `tracker.db` AND `vector_store.db` if they exist
- Handles errors gracefully with fallback instructions
- Prevents data loss when reconfiguring database location

### 3. Setup Wizard UI (`SetupWizard.xaml`)

**Added Section** (after database type selection):
- Checkbox: "Use custom database location"
- Help text explaining network share scenario
- Path textbox (monospace font for readability)
- Browse button to open file picker
- Warning panel with network share considerations:
  - Concurrent write limitations (2-4 users)
  - Permission requirements
  - SQL Server recommendation for larger teams
  - Network latency notes

### 4. Settings ViewModel (`SettingsViewModel.cs`)

**Updated Property**: `CurrentDatabaseLocation`
- Now shows custom SQLite path if set
- Falls back to default path if no custom path
- Maintains existing SQL Server display logic

### 5. Documentation

**Created**: [SHARED_DATABASE_SETUP.md](../Docs/SHARED_DATABASE_SETUP.md)
- Complete setup guide for network share configuration
- Concurrent access limitations explained
- Performance considerations and recommendations
- Backup strategies
- Troubleshooting common issues
- Valid/invalid path examples
- Migration procedures
- Security considerations
- FAQ section

## User Experience Flow

### First-Time Setup

1. User launches Tracker
2. Setup wizard Step 1: Choose database type
3. Selects "Local Database"
4. **(NEW)** Optionally checks "Use custom database location"
5. **(NEW)** Clicks "Browse..." to select network path
6. Path example: `\\server\share\TrackerData\tracker.db`
7. Completes setup - database created at custom location

### Additional Team Members

1. Launch Tracker
2. Select "Local Database"
3. Check "Use custom database location"
4. Enter **same path** as first user: `\\server\share\TrackerData\tracker.db`
5. Complete setup - connects to existing shared database

### Existing Users (Migration)

1. Open Settings → Database
2. Click "Change Database"
3. Setup wizard opens
4. Select "Local Database" + "Use custom database location"
5. Browse to new location (e.g., `\\server\share\TrackerData\tracker.db`)
6. **Automatic migration prompt appears**: "Copy existing database to new location?"
7. Click **YES** - Database and vector store automatically copied ✅
8. Restart Tracker - now using shared database with all existing data intact

**No manual file copying required!** Tracker handles the migration automatically.

## Technical Details

### SQLite Network Sharing

**How it works**:
- SQLite uses file-level locking via the operating system
- Multiple processes can open the same database file
- Reads are concurrent (unlimited)
- Writes acquire exclusive lock (serialized)

**Limitations**:
- **2-4 users**: Good performance with occasional writes
- **5-10 users**: Possible lock contention during writes
- **10+ users**: Frequent "database locked" errors - use SQL Server

**Network Requirements**:
- SMB/CIFS file sharing protocol
- Low latency (< 10ms ping preferred)
- Reliable network connection
- Read/write permissions for all users

### Path Validation

**Valid Paths**:
- UNC: `\\server\share\folder\tracker.db`
- Mapped drive: `Z:\TrackerData\tracker.db`
- IP-based UNC: `\\192.168.1.100\share\tracker.db`
- DFS path: `\\domain\dfs\TeamData\tracker.db`

**Invalid/Not Recommended**:
- ❌ Cloud sync folders (OneDrive, Dropbox) - corruption risk
- ❌ HTTP/web URLs - not supported by SQLite
- ❌ Relative paths - must be absolute

### Security

**File System Level**:
- Access control via Windows/network share permissions
- No SQLite-level authentication (file-based)
- Encryption via BitLocker or network share encryption

**Recommendations**:
- Restrict share access to authorized team members only
- Use VPN for remote access
- Enable file server backups
- For sensitive data with row-level security needs, use SQL Server

## Testing Checklist

- [ ] Build succeeds with no errors ✅ (verified)
- [ ] Setup wizard displays custom path option for SQLite
- [ ] Browse button opens file dialog
- [ ] Path textbox accepts UNC and local paths
- [ ] Warning panel shows when custom path is enabled
- [ ] Database created at custom location on first run
- [ ] Second user can connect to same network database
- [ ] Settings displays custom path correctly
- [ ] Data syncs between users via shared file
- [ ] Performance acceptable with 2-4 concurrent users

## Performance Expectations

| Scenario | Performance | Recommendation |
|----------|-------------|----------------|
| 2 users, low writes | ✅ Excellent | Network share works great |
| 4 users, moderate writes | ✅ Good | Monitor for occasional locks |
| 6-8 users, moderate writes | ⚠️ Fair | Consider SQL Server |
| 10+ users | ❌ Poor | **Use SQL Server** |

## Migration Path

**Small team outgrows SQLite** → Upgrade to SQL Server:

1. Export data from SQLite
2. Deploy SQL Server scripts ([Database/SqlServer](../Tracker/Database/SqlServer))
3. Import data to SQL Server
4. Update Tracker settings to SQL Server connection
5. Remove network share (or keep as backup)

See [DATABASE_OPTIMIZATION_REPORT.md](DATABASE_OPTIMIZATION_REPORT.md) for SQL Server performance benefits.

## Related Files

### Modified Files
- `Tracker/Classes/DatabaseSettings.cs` - Added CustomSqlitePath property
- `Tracker/ViewModels/DialogViewModels/SetupWizardViewModel.cs` - Custom path UI logic
- `Tracker/Views/Dialogs/SetupWizard.xaml` - Custom path UI elements
- `Tracker/ViewModels/DialogViewModels/SettingsViewModel.cs` - Display custom path

### New Files
- `Docs/SHARED_DATABASE_SETUP.md` - Complete user guide

### Related Documentation
- `Docs/DATABASE_OPTIMIZATION_REPORT.md` - SQL Server performance analysis
- `Database/SqlServer/README.md` - SQL Server deployment guide

## Support Scenarios

**Customer asks**: "Can 4 people share the same database?"

✅ **Yes** - Use custom SQLite path on network share (good for 2-10 users)

**Customer asks**: "We have 20 team members, should we use SQLite on network?"

❌ **No** - Use SQL Server deployment scripts for 10+ users

**Customer asks**: "Can I use Dropbox to share the database?"

❌ **No** - Cloud sync causes corruption. Use network share or SQL Server.

---

**Implementation Date**: December 24, 2025  
**Feature**: Custom SQLite Database Location  
**Target Release**: Tracker v1.1
