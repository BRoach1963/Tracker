# Tracker Shared Database - Quick Reference Card

**Print this page and keep at your desk!**

---

## 🚀 Quick Setup (5 Minutes)

### First User (Database Creator)

1. Install Tracker → Launch
2. Select **Local Database**
3. ✅ Check **"Use custom database location"**
4. Browse to: `\\YOUR_SERVER\TrackerData\tracker.db`
5. Complete setup
6. **Share this path with your team** ⬇️

```
Database Path: \\YOUR_SERVER\TrackerData\tracker.db
```

### Additional Users (Team Members)

1. Install Tracker → Launch  
2. Select **Local Database**  
3. ✅ Check **"Use custom database location"**  
4. Enter: `\\YOUR_SERVER\TrackerData\tracker.db` (EXACT path!)  
5. ⚠️ **UNCHECK "Include sample data"**  
6. Complete setup  

---

## ✅ Quick Test (Both Users)

**User 1**: Add test team member "John Doe"  
**User 2**: Refresh → See "John Doe"? ✅ Success!

---

## ⚠️ Common Issues

| Problem | Solution |
|---------|----------|
| "Cannot access path" | Check network permissions & spelling |
| "Database locked" | Wait 5 seconds, try again |
| Don't see others' data | Verify paths MATCH exactly |
| Slow performance | Check network speed (ping < 10ms) |

---

## 📋 Team Size Limits

| Users | Performance | Recommendation |
|-------|-------------|----------------|
| 2-4 | ✅ Excellent | Perfect for SQLite |
| 5-8 | ⚠️ Good | May see occasional locks |
| 10+ | ❌ Poor | **Use SQL Server** |

---

## 🔒 Important Rules

❌ **DON'T** use OneDrive, Dropbox, Google Drive (corruption!)  
❌ **DON'T** use WiFi for server (use wired connection)  
❌ **DON'T** exceed 10 users on SQLite  
✅ **DO** use UNC paths: `\\server\share`  
✅ **DO** back up `tracker.db` regularly  
✅ **DO** test with 2 users before full rollout  

---

## 💾 Quick Backup

```powershell
# From File Explorer, copy:
\\YOUR_SERVER\TrackerData\tracker.db
TO
\\YOUR_SERVER\Backups\tracker_2025-12-24.db
```

---

## 🆘 Getting Help

**Full Guide**: `C:\...\Tracker\SHARED_DATABASE_QUICK_START.md`  
**In-App Help**: Settings → Help → Shared Database Setup  
**Support**: support@trackerapp.com  

---

**Current Path**: _______________________________________

**Setup Date**: _____________  **By**: __________________

**Team Members Using This Database**:
1. ________________  4. ________________  7. ________________
2. ________________  5. ________________  8. ________________
3. ________________  6. ________________  9. ________________

**Notes**:
________________________________________________________________
________________________________________________________________
________________________________________________________________

---

*Tracker v1.0+ | Shared Database Feature | December 2025*
