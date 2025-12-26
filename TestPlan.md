# Tracker Application - Functional QA Test Plan

## Test Environment Setup
- **OS**: Windows 10/11
- **Build Configuration**: Debug and Release
- **Database**: SQLite and SQL Server (ODBC)
- **Network**: Online (Supabase) and Offline scenarios

---

## 1. Authentication & User Management

### 1.1 Login
- [ ] Valid credentials login successfully
- [ ] Invalid credentials show appropriate error
- [ ] Email validation works correctly
- [ ] Password visibility toggle functions
- [ ] "Remember me" persists login state
- [ ] Exit button closes application
- [ ] MainWindow icon displays correctly after login

### 1.2 Signup
- [ ] New user registration completes successfully
- [ ] Email format validation works
- [ ] Password strength requirements enforced
- [ ] Duplicate email prevented
- [ ] Activation code flow works
- [ ] User redirected to main app after successful signup

### 1.3 Session Management
- [ ] Session persists across app restarts (if remember me checked)
- [ ] Logout clears session properly
- [ ] Token refresh works correctly
- [ ] Session timeout handled gracefully

---

## 2. Team Member Management

### 2.1 Team Member CRUD
- [ ] Add new team member with all required fields
- [ ] Edit existing team member
- [ ] Delete team member (with confirmation)
- [ ] Team member list displays correctly
- [ ] Search/filter team members works
- [ ] Team member details view shows all information
- [ ] Profile picture upload/display (if applicable)

### 2.2 Team Member Details View
- [ ] Three tabs display correctly (Meetings, Feedback, Goals)
- [ ] Switch between tabs without data loss
- [ ] Back button returns to team list
- [ ] Edit/Delete icons visible and functional

---

## 3. One-on-One Meetings

### 3.1 Meeting Creation
- [ ] Create new meeting from team member view
- [ ] Create new meeting from main menu/toolbar
- [ ] Date picker displays and selects dates correctly
- [ ] Time picker shows correct time format (12/24 hour)
- [ ] Start time validation (before end time)
- [ ] End time validation (after start time)
- [ ] Team member dropdown populates correctly
- [ ] Required fields validation works
- [ ] Save creates meeting in database
- [ ] Cancel discards changes

### 3.2 Meeting Editing
- [ ] Double-click meeting in history opens edit dialog
- [ ] Meeting data populates correctly (date, start time, end time, team member)
- [ ] Edit and save updates database
- [ ] Changes reflect immediately in history list
- [ ] Validation still works in edit mode

### 3.3 Meeting History
- [ ] Meeting history list displays all meetings
- [ ] List shows date, time, duration, team member
- [ ] Text wraps properly (no horizontal scrolling)
- [ ] Meetings sorted correctly (by date/time)
- [ ] Delete meeting removes from list
- [ ] Filter/search meetings works

### 3.4 Time Picker Control
- [ ] Time picker displays current time correctly
- [ ] Up/down buttons increment/decrement time
- [ ] Direct text input works
- [ ] Dropdown shows time options
- [ ] Dropdown closes after selection
- [ ] AM/PM toggle works (12-hour mode)
- [ ] 24-hour format displays correctly
- [ ] No visual artifacts (hidden "Now" button)
- [ ] Keyboard navigation works

---

## 4. Feedback Management

### 4.1 Feedback CRUD
- [ ] Add new feedback for team member
- [ ] Edit existing feedback
- [ ] Delete feedback (with confirmation)
- [ ] Feedback list displays all items
- [ ] Date recorded correctly
- [ ] Feedback type/category selection works

### 4.2 Feedback Display
- [ ] Feedback list shows title and content
- [ ] Text wraps properly (no horizontal scrolling)
- [ ] Long feedback displays correctly
- [ ] Formatting preserved
- [ ] Timestamps display correctly

---

## 5. Goals Management

### 5.1 Goal CRUD
- [ ] Create new goal for team member
- [ ] Edit existing goal
- [ ] Delete goal (with confirmation)
- [ ] Goal status updates (Not Started, In Progress, Completed)
- [ ] Due date setting works
- [ ] Priority setting works

### 5.2 Goal Display
- [ ] Goal list shows all goals
- [ ] Title and description wrap properly (no horizontal scrolling)
- [ ] Status indicator visible
- [ ] Progress tracking displays correctly
- [ ] Overdue goals highlighted
- [ ] Goals sorted by priority/due date

---

## 6. OKRs (Objectives & Key Results)

### 6.1 OKR Management
- [ ] Create new OKR
- [ ] Edit existing OKR
- [ ] Delete OKR
- [ ] Add key results to objective
- [ ] Edit key results
- [ ] Progress calculation correct
- [ ] Quarter/period selection works

### 6.2 OKR Display
- [ ] OKR list shows all objectives
- [ ] Key results display under objectives
- [ ] Progress bars accurate
- [ ] Status colors correct
- [ ] Filtering by quarter/period works

---

## 7. Dashboard

### 7.1 Dashboard Views
- [ ] Dashboard loads without errors
- [ ] All widgets display correctly
- [ ] Data refresh works
- [ ] Navigation between sections smooth
- [ ] Charts/graphs render correctly
- [ ] Metrics calculations accurate

### 7.2 Quick Notes
- [ ] Add new quick note
- [ ] Edit quick note
- [ ] Delete quick note
- [ ] Notes persist across sessions
- [ ] Timestamps correct

---

## 8. System Tray Integration

### 8.1 Tray Icon
- [ ] Application minimizes to tray
- [ ] Tray icon displays correctly
- [ ] Tray icon tooltip shows app name
- [ ] Right-click shows context menu
- [ ] Double-click restores window

### 8.2 Tray Menu
- [ ] Menu displays with dark theme styling
- [ ] "Show" option restores window
- [ ] "Exit" option closes application
- [ ] Custom menu items functional
- [ ] Checkboxes render correctly with blue background
- [ ] Balloon notifications show tray icon (not generic 'i')

---

## 9. Settings & Configuration

### 9.1 General Settings
- [ ] Settings dialog opens correctly
- [ ] All settings tabs accessible
- [ ] Settings save successfully
- [ ] Settings persist across sessions
- [ ] Cancel discards changes

### 9.2 Theme Settings
- [ ] Default theme (Black/Gold) applies correctly
- [ ] Light theme applies correctly
- [ ] Modern theme applies correctly
- [ ] Spicy theme applies correctly
- [ ] Theme changes apply without restart
- [ ] All controls styled correctly in each theme
- [ ] DynamicResource bindings update on theme change

### 9.3 Database Settings
- [ ] SQLite connection works
- [ ] SQL Server connection works (via ODBC)
- [ ] Connection string validation
- [ ] Database switch works
- [ ] Data migration prompts appropriately
- [ ] Clear data function works (with confirmation)
- [ ] Seed sample data function works

### 9.4 Calendar Integration
- [ ] Google Calendar authentication works
- [ ] Calendar sync settings save
- [ ] Calendar events display correctly
- [ ] Two-way sync functions

---

## 10. Window Management & UI

### 10.1 Main Window
- [ ] Window opens at correct size/position
- [ ] Window icon displays correctly (not generic)
- [ ] Minimize/Maximize/Close buttons work
- [ ] Window resizing works smoothly
- [ ] Window state persists across sessions

### 10.2 Dialog Windows
- [ ] All dialogs show in taskbar
- [ ] Dialog icons display correctly
- [ ] Dialogs have correct owner (MainWindow)
- [ ] Dialogs don't disappear behind main window
- [ ] Modal dialogs block interaction correctly
- [ ] Close buttons work on all dialogs
- [ ] ESC key closes dialogs

### 10.3 Loading Window
- [ ] Loading window displays during startup
- [ ] Loading animation plays smoothly
- [ ] Loading window closes before MainWindow shows
- [ ] No taskbar icon confusion
- [ ] Fade animation works correctly

### 10.4 Controls & UI Elements
- [ ] All TextBoxes accept input correctly
- [ ] ComboBoxes populate and select correctly
- [ ] DatePickers display and select dates
- [ ] ListViews scroll smoothly
- [ ] No horizontal scrolling in lists
- [ ] Text wrapping works in all text areas
- [ ] Tooltips display correctly
- [ ] Icons render properly (not missing/broken)

---

## 11. Help System

### 11.1 Help Window
- [ ] Help window opens from menu
- [ ] Help topics display correctly
- [ ] Navigation between topics works
- [ ] Search functionality works
- [ ] Code examples render properly
- [ ] Images/screenshots display

### 11.2 Context Help
- [ ] F1 key opens context-sensitive help
- [ ] Help tooltips display on hover
- [ ] User manual accessible
- [ ] Version/about info displays correctly

---

## 12. Data Integrity & Persistence

### 12.1 Database Operations
- [ ] All CRUD operations persist correctly
- [ ] Foreign key relationships maintained
- [ ] Cascade deletes work correctly
- [ ] Transactions rollback on error
- [ ] No orphaned records
- [ ] Database locking handled correctly

### 12.2 Data Validation
- [ ] Required fields enforced
- [ ] Data type validation works
- [ ] Range validation works
- [ ] Duplicate prevention works
- [ ] Error messages clear and helpful

### 12.3 Audit Trail
- [ ] Created/Modified timestamps accurate
- [ ] Created/Modified by user tracked
- [ ] Audit log entries created
- [ ] Change history viewable

---

## 13. Performance & Stability

### 13.1 Performance
- [ ] Application starts in < 5 seconds
- [ ] UI remains responsive under load
- [ ] Large datasets display smoothly
- [ ] Memory usage reasonable (no leaks)
- [ ] Database queries optimized

### 13.2 Error Handling
- [ ] Unhandled exceptions caught
- [ ] Error messages user-friendly
- [ ] Errors logged to file
- [ ] Application recovers gracefully from errors
- [ ] No crashes during normal operation

### 13.3 Stability
- [ ] Application runs for extended periods without issues
- [ ] No memory leaks over time
- [ ] No UI freezing
- [ ] Concurrent operations handled correctly

---

## 14. Notifications & Reminders

### 14.1 Meeting Reminders
- [ ] Reminders trigger at correct time
- [ ] Reminder notifications display
- [ ] Snooze functionality works
- [ ] Dismiss removes reminder
- [ ] Reminder settings configurable

### 14.2 Toast Notifications
- [ ] Toast notifications display correctly
- [ ] Correct icon shows in notifications
- [ ] Click action works
- [ ] Notifications queue properly
- [ ] Notification sounds work (if enabled)

---

## 15. Integration Features

### 15.1 Calendar Integration
- [ ] Google Calendar sync works
- [ ] Events import correctly
- [ ] Events export correctly
- [ ] Conflict detection works
- [ ] Sync errors handled gracefully

### 15.2 Slack Integration (if applicable)
- [ ] Slack authentication works
- [ ] Messages post correctly
- [ ] Notifications from Slack received
- [ ] Slack workspace selection works

---

## 16. Accessibility & Usability

### 16.1 Keyboard Navigation
- [ ] Tab order logical
- [ ] All controls keyboard accessible
- [ ] Shortcuts work correctly
- [ ] Enter key submits forms
- [ ] ESC key cancels dialogs

### 16.2 Screen Reader Support
- [ ] Automation peers implemented
- [ ] Controls have appropriate labels
- [ ] Focus indicators visible
- [ ] ARIA properties set correctly

### 16.3 Usability
- [ ] UI intuitive and self-explanatory
- [ ] Common tasks easily discoverable
- [ ] Consistent design patterns
- [ ] Clear visual hierarchy
- [ ] Appropriate use of color and icons

---

## 17. Edge Cases & Boundary Testing

### 17.1 Data Limits
- [ ] Empty fields handled correctly
- [ ] Maximum length fields enforced
- [ ] Very long text displays correctly
- [ ] Special characters handled
- [ ] Unicode/emoji support

### 17.2 Network Scenarios
- [ ] Offline mode works
- [ ] Connection loss handled gracefully
- [ ] Reconnection automatic
- [ ] Sync conflicts resolved
- [ ] Timeout errors handled

### 17.3 Concurrent Operations
- [ ] Multiple windows/dialogs handled
- [ ] Simultaneous edits managed
- [ ] Race conditions prevented
- [ ] Database locks handled

---

## 18. Installation & Deployment

### 18.1 Installation
- [ ] Installer runs without errors
- [ ] All files copied correctly
- [ ] Registry entries created
- [ ] Desktop shortcut created
- [ ] Start menu entry created
- [ ] Uninstaller works correctly

### 18.2 Updates
- [ ] Update check works
- [ ] Update download succeeds
- [ ] Update installation smooth
- [ ] Data preserved during update
- [ ] Rollback possible if needed

### 18.3 First Run Experience
- [ ] Welcome screen displays
- [ ] Initial configuration wizard works
- [ ] Sample data option works
- [ ] License agreement shown
- [ ] Default settings appropriate

---

## 19. Regression Testing Checklist

After each build/release, verify:
- [ ] Login/Authentication still works
- [ ] Main workflows (create meeting, feedback, goal) work
- [ ] No new UI rendering issues
- [ ] Theme switching still works
- [ ] Database operations successful
- [ ] No console errors or warnings
- [ ] Performance not degraded
- [ ] All critical bugs fixed and verified

---

## 20. Known Issues & Workarounds

Document any known issues discovered during testing:

### Current Known Issues:
1. ~~MainWindow icon shows generic icon on first load~~ (Accepted - low priority)
2. ~~TimePicker dropdown stays open after selection~~ (DeepEndControls limitation)
3. ~~TimePicker dropdown text hard to read~~ (No ComboBoxForeground property)

---

## Test Execution Notes

### Priority Levels:
- **P0 (Critical)**: Must pass - blocks release
- **P1 (High)**: Should pass - important functionality
- **P2 (Medium)**: Nice to have - minor issues acceptable
- **P3 (Low)**: Future improvement - cosmetic issues

### Test Status:
- ✅ **Pass**: Test executed successfully
- ❌ **Fail**: Test failed, bug filed
- ⚠️ **Blocked**: Cannot test, dependency missing
- ⏭️ **Skipped**: Test not applicable for this build

### Bug Severity:
- **Critical**: App crashes, data loss, security issue
- **Major**: Feature broken, major functionality impaired
- **Minor**: UI issue, workaround exists
- **Trivial**: Cosmetic, typo, very minor

---

## Test Sign-off

| Role | Name | Date | Status |
|------|------|------|--------|
| QA Lead | | | |
| Developer | | | |
| Product Owner | | | |
| Release Manager | | | |

---

## Appendix: Test Data

### Sample Users:
- Valid: brian@pricklycactussoftware.com
- Invalid: invalid@email.com

### Sample Team Members:
- John Doe (Software Engineer)
- Jane Smith (Product Manager)
- Bob Johnson (Designer)

### Sample Dates:
- Today
- Past dates (last week, last month)
- Future dates (next week, next month)
- Edge dates (Feb 29, Dec 31)

---

**Document Version**: 1.0  
**Last Updated**: December 21, 2025  
**Next Review**: Before each major release
