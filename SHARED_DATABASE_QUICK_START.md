# Setting Up a Shared Database - Quick Start Guide

**Scenario**: You have a small team (2-10 people) who want to share Tracker data without setting up SQL Server.

**Solution**: Use a custom database location on a network share that all team members can access.

---

## Prerequisites

✅ **Network share** that all team members can access (e.g., `\\fileserver\TrackerData`)  
✅ **Read/write permissions** for all team members on that share  
✅ **Windows network** (corporate network or workgroup)  
✅ **Tracker installed** on each team member's computer  

---

## Step-by-Step Instructions

### 👤 For the FIRST USER (Database Creator)

#### Step 1: Prepare the Network Share

1. Open File Explorer
2. Navigate to your shared network location (e.g., `\\fileserver\TeamData`)
3. Create a new folder called `TrackerData` (or any name you prefer)
4. **Test access**: Make sure you can create a file in this folder and delete it

#### Step 2: Launch Tracker Setup Wizard

1. **Install and launch Tracker** for the first time
2. The Setup Wizard will appear automatically

#### Step 3: Choose Database Type

1. On the "Choose Database Type" screen, click **Local Database** (the left card)
2. ✅ Check the box that says: **"Use custom database location"**
3. A new section appears below

#### Step 4: Select Network Share Location

1. Click the **Browse...** button
2. In the file picker:
   - Type the network path in the location bar: `\\fileserver\TrackerData`
   - Or navigate through Network → Server → Folder
3. In the "File name" field, keep the default: `tracker.db`
4. Click **Save**
5. You should see the full path displayed: `\\fileserver\TrackerData\tracker.db`

#### Step 5: Review the Warning

Read the yellow warning box that appears:
- ⚠️ SQLite supports 2-4 concurrent users comfortably
- ⚠️ All users need read/write permissions
- ⚠️ For teams of 10+, consider SQL Server instead
- ⚠️ Network latency may affect performance

#### Step 6: Complete Setup

1. Click **Next** to continue
2. Complete the Account Setup (create account or skip)
3. On the Summary screen:
   - ✅ Review your database path
   - ✅ **Include sample data** is helpful for testing
4. Click **Finish**

#### Step 7: Verify Database Created

1. Open File Explorer
2. Navigate to `\\fileserver\TrackerData`
3. You should see TWO files:
   - ✅ `tracker.db` (your main database)
   - ✅ `vector_store.db` (AI search index)

#### Step 8: Share the Path with Your Team

**Send this exact path to your team members**:
```
\\fileserver\TrackerData\tracker.db
```

📧 Example message to team:
```
Hi team,

I've set up our shared Tracker database. To connect:

1. Install Tracker from [link]
2. During setup, choose "Local Database"
3. Check "Use custom database location"
4. Enter this exact path: \\fileserver\TrackerData\tracker.db
5. Complete setup - all our data will be shared!

Let me know if you have any issues.
```

---

### 👥 For ADDITIONAL TEAM MEMBERS (Connecting to Existing Database)

#### Step 1: Install Tracker

1. Install Tracker on your computer
2. Launch the application
3. The Setup Wizard appears

#### Step 2: Choose Database Type

1. Select **Local Database** (left card)
2. ✅ Check: **"Use custom database location"**

#### Step 3: Enter the EXACT Path

1. In the path textbox, type or paste:
   ```
   \\fileserver\TrackerData\tracker.db
   ```
2. **Important**: Use the EXACT path your first user shared
3. Alternatively, click **Browse...** and navigate to the location

#### Step 4: Verify Path

1. Make sure the path shown matches exactly what your team lead provided
2. Click **Next**

#### Step 5: Complete Setup

1. Complete Account Setup (or skip)
2. On Summary screen:
   - ⚠️ **UNCHECK "Include sample data"** (database already has data!)
3. Click **Finish**

#### Step 6: Test Access

1. Once Tracker opens, you should see:
   - ✅ Team members added by the first user
   - ✅ Any 1:1s, tasks, or other data already created
2. Try creating a test task
3. Have the first user check if they can see your task

---

## Testing the Shared Database

### Quick Test (2-3 minutes)

**User 1**:
1. Open Tracker
2. Go to Circle → Team
3. Add a test team member: "Test Person"
4. Note the time you added it

**User 2**:
1. Open Tracker
2. Go to Circle → Team
3. **Refresh** the view (or close/reopen Tracker)
4. ✅ You should see "Test Person" in your list!

**Both users**:
- If both see the same data → ✅ Success!
- If not → Check path spelling and network permissions

---

## Troubleshooting

### "Cannot access database file"

**Problem**: User can't connect to `\\fileserver\TrackerData\tracker.db`

**Solutions**:
1. ✅ Verify network path is accessible:
   - Open File Explorer
   - Type `\\fileserver\TrackerData` in address bar
   - Can you see the folder?
2. ✅ Check permissions:
   - Right-click the folder → Properties → Security
   - Your user account needs: Read, Write, Modify
3. ✅ Try UNC path instead of mapped drive:
   - Use `\\server\share` NOT `Z:\share`
4. ✅ Test from Command Prompt:
   ```
   dir \\fileserver\TrackerData
   ```

### "Database is locked" error

**Problem**: Error when trying to save data

**Cause**: Another user is writing to the database at the same time

**Solutions**:
1. ⏱️ Wait 5-10 seconds and try again
2. ✅ Check if another user is doing bulk operations (importing data, clearing database)
3. ⚠️ If happens frequently, your team may be too large for SQLite - consider SQL Server

### Data not syncing between users

**Problem**: User 1 adds data, User 2 doesn't see it

**Solutions**:
1. 🔄 Refresh the view or restart Tracker
2. ✅ Verify both users are pointing to the EXACT same path:
   - User 1: Settings → Database → Check path
   - User 2: Settings → Database → Check path
3. ✅ Check if both users see the same file modification time:
   - Right-click `tracker.db` → Properties → Modified date

### Slow performance

**Problem**: Tracker feels sluggish

**Causes & Solutions**:
1. **Network latency**:
   - Test ping time: `ping fileserver` (should be < 10ms)
   - Use wired connection instead of WiFi
2. **Large database**:
   - Check file size (File Explorer → Right-click tracker.db → Properties)
   - If > 500MB, consider archiving old data
3. **Too many concurrent users**:
   - SQLite works best with 2-4 users
   - For 10+ users, migrate to SQL Server

---

## Migrating to SQL Server Later

As your team grows, you may need to upgrade to SQL Server:

### When to Migrate
- ⚠️ More than 10 concurrent users
- ⚠️ Frequent "database locked" errors
- ⚠️ Need better performance
- ⚠️ Remote users over VPN (high latency)

### How to Migrate
1. See [DATABASE_OPTIMIZATION_REPORT.md](Docs/DATABASE_OPTIMIZATION_REPORT.md)
2. Use deployment scripts in `Database/SqlServer/`
3. Export data from SQLite, import to SQL Server
4. Update all users' settings to point to SQL Server

---

## Best Practices

### ✅ DO:
- Test the network path before sharing with team
- Use UNC paths (`\\server\share`) instead of mapped drives
- Keep team size under 10 users for SQLite
- Set up regular backups of `tracker.db` file
- Use wired network connections for better performance
- Test with 2 users before rolling out to whole team

### ❌ DON'T:
- Use cloud sync folders (OneDrive, Dropbox) - causes corruption!
- Mix local and network databases - pick one
- Have 10+ users on SQLite - use SQL Server instead
- Store on USB drives that may be disconnected
- Use WiFi for the file server if possible

---

## FAQ

**Q: Can I move the database to a different network share later?**

A: Yes! Settings → Change Database → Select new location. Tracker will offer to copy your existing database automatically.

**Q: What if the file server goes down?**

A: Tracker won't be able to access data until the server is back online. No local caching. For better resilience, use SQL Server.

**Q: Can remote workers access this over VPN?**

A: Technically yes, but performance may be poor due to VPN latency. SQL Server is better for remote teams.

**Q: How do I back up the shared database?**

A: Copy `tracker.db` and `vector_store.db` to a backup location. Most file servers have automatic backup - check with your IT team.

**Q: Can I use this with Mac users?**

A: Currently Tracker is Windows-only. When Mac version launches (MAUI), it will support SMB shares the same way.

**Q: What's the maximum database size?**

A: SQLite can handle databases up to 281TB, but for performance reasons, keep it under 1GB. If you exceed this with a small team, you may be storing too many attachments.

---

## Getting Help

If you run into issues:

1. 📖 Check [Troubleshooting Guide](Docs/SHARED_DATABASE_SETUP.md#troubleshooting)
2. 📧 Email support with:
   - Network path you're using
   - Error messages (screenshot)
   - Number of users trying to connect
3. 💬 Community forum: https://community.trackerapp.com

---

**Setup Time**: 10-15 minutes  
**Recommended Team Size**: 2-10 users  
**Technical Skill**: Basic (can navigate network shares)  

Good luck! 🚀
