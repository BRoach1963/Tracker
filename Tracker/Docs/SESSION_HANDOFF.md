# Session Handoff Document
**Date:** December 18, 2025

## Project Overview
**Tracker** - A WPF team management application for managers to track 1:1 meetings, OKRs, KPIs, tasks, and team member performance.

- **Tech Stack:** WPF, .NET 8, Entity Framework Core, SQLite/SQL Server, Supabase (auth/storage), Square (payments)
- **Architecture:** MVVM pattern with ViewModels, custom controls, and a service layer
- **Custom Controls Library:** DeepEndControls (separate repo at `C:\Users\vbpro\source\repos\DeepEndControls`)

## Recent Session Changes

### 1. Build Order Fix (Installer)
- **File:** `Tracker.sln` - Added project dependency so Tracker builds before Tracker.Installer
- **File:** `Tracker.Installer/Tracker.Installer.wixproj` - Added `PublishTrackerFirst` target
- **File:** `Tracker.Installer/Package.wxs` - Removed unused WebView2 references (not used in codebase)

### 2. Test Fixes
- **Files:** `Tracker.Tests/Database/DatabaseSeederTests.cs`, `EntityCrudTests.cs`
- Removed references to `KeyPerformanceIndicator.OkrId` (property was removed from model)

### 3. DatePicker Calendar Icon Centering
- **File:** `Tracker/Resources/Styles.xaml` - Fixed `DatePickerStyle` PART_Button to use inline template with centered calendar icon

### 4. Team Member Card Double-Click
- **File:** `Tracker/Controls/TeamMembersControl.xaml` - Added `MouseBinding` with `LeftDoubleClick` for reliable double-click detection
- **File:** `Tracker/Controls/TeamMembersControl.xaml.cs` - Simplified to `MemberCard_Click` for selection only

### 5. Team Member Save Functionality
- **File:** `Tracker/ViewModels/DialogViewModels/TeamMemberViewModel.cs`
  - Added toast notifications for Add/Update success/error
  - Added `DataMessenger.SendRefresh(DataChangeType.TeamMembers)` for UI refresh
  - Added proper error handling with try/catch

- **File:** `Tracker/Database/TrackerDbManager.cs`
  - Fixed `UpdateTeamMemberAsync` to handle EF tracking properly:
    - Now uses `FindAsync` to get tracked entity
    - Uses `SetValues()` to copy properties
    - This fixes the "changes not saving" bug

## Key Architecture Notes

### Data Managers
- `TrackerDbManager` (Database folder) - Direct EF Core database operations
- `TrackerDataManager` (Managers folder) - Wrapper with caching and messaging

### Dialog Pattern
- Dialogs use `DialogFactory.TryGetWindowFromType(DialogType, callback, out window, dataObject)`
- ViewModels inherit from `BaseDialogViewModel`
- Commands use `TrackerCommand` class

### Messaging
- `DataMessenger.SendRefresh(DataChangeType.X)` - Triggers UI refresh
- `Messenger.Publish(PropertyChangedMessage)` - Legacy messaging

### Styling
- Theme brushes: `AccentBrush` (gold), `ForegroundBrush`, `BackgroundBrush`, `SurfaceBrush`, `HintTextBrush`
- Custom controls: `TextBoxWithHint`, `TimePicker`, `RichTextEditor` (in DeepEndControls)
- Dialog styles in `Tracker/Resources/Styles/DialogStyles.xaml`
- Global styles in `Tracker/Resources/Styles.xaml`

## Known Issues / TODO

1. **Remaining Dialogs:** AddKeyResult, EditKeyResult, AddMeasurable dialogs exist but may need testing
2. **Calendar Sync:** Google works, Microsoft 365 implemented, Outlook/Apple not started
3. **Profile Fields:** FirstName, LastName, JobTitle, Company, Phone added to UserProfile but UI may need verification

## Files to Know

| Purpose | Path |
|---------|------|
| Main ViewModel | `Tracker/ViewModels/TrackerMainViewModel.cs` |
| Dialog Factory | `Tracker/Factories/DialogFactory.cs` |
| DB Manager | `Tracker/Database/TrackerDbManager.cs` |
| Data Manager | `Tracker/Managers/TrackerDataManager.cs` |
| Global Styles | `Tracker/Resources/Styles.xaml` |
| Dialog Styles | `Tracker/Resources/Styles/DialogStyles.xaml` |
| Team Members UI | `Tracker/Controls/TeamMembersControl.xaml` |
| Team Member Dialog | `Tracker/Views/Dialogs/TeamMemberDialog.xaml` |

## Cursor Rules
See `Tracker/.cursorrules` for project-specific guidelines including:
- Never use `MessageBox.Show()` or `Interaction.InputBox()` - use styled dialogs
- Test maintenance requirements
- Styling conventions

---

## Prompt for New Chat

```
I'm continuing work on Tracker, a WPF team management app. 

Key context:
- .NET 8 WPF with MVVM, Entity Framework Core, Supabase backend
- Custom control library: DeepEndControls (separate repo)
- Recent fixes: Team member edit/save now works (EF tracking fix), double-click on cards works
- See Docs/SESSION_HANDOFF.md for detailed session notes

What would you like me to work on?
```


