using System;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Tracker.Documentation
{
    /// <summary>
    /// Generates a comprehensive user manual for the Tracker application.
    /// </summary>
    public class UserManualGenerator
    {
        public static void GenerateUserManual(string outputPath)
        {
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());

                // Title Page
                AddTitlePage(body);

                // Table of Contents placeholder
                AddTableOfContents(body);

                // Chapter 1: Getting Started
                AddChapter(body, "1", "Getting Started", GetGettingStartedContent());

                // Chapter 2: Initial Setup
                AddChapter(body, "2", "Initial Setup", GetInitialSetupContent());

                // Chapter 3: Team Members
                AddChapter(body, "3", "Managing Team Members", GetTeamMembersContent());

                // Chapter 4: One-on-One Meetings
                AddChapter(body, "4", "One-on-One Meetings", GetOneOnOneContent());

                // Chapter 5: Projects
                AddChapter(body, "5", "Managing Projects", GetProjectsContent());

                // Chapter 6: Tasks
                AddChapter(body, "6", "Managing Tasks", GetTasksContent());

                // Chapter 7: OKRs and KPIs
                AddChapter(body, "7", "Objectives, Key Results, and KPIs", GetOKRsKPIsContent());

                // Chapter 8: Dashboard
                AddChapter(body, "8", "Dashboard & Analytics", GetDashboardContent());

                // Chapter 9: Reports
                AddChapter(body, "9", "Reports & Export", GetReportsContent());

                // Chapter 10: Settings
                AddChapter(body, "10", "Settings & Configuration", GetSettingsContent());

                // Chapter 11: Calendar Integration
                AddChapter(body, "11", "Calendar Integration", GetCalendarIntegrationContent());

                // Appendix
                AddAppendix(body);
            }
        }

        private static void AddTitlePage(Body body)
        {
            Paragraph titlePara = new Paragraph();
            Run titleRun = new Run();
            RunProperties titleProps = new RunProperties();
            titleProps.FontSize = new FontSize { Val = "48" };
            titleProps.Bold = new Bold();
            titleRun.Append(titleProps);
            titleRun.Append(new Text("Tracker"));
            titlePara.Append(titleRun);
            titlePara.ParagraphProperties = new ParagraphProperties
            {
                Justification = new Justification { Val = JustificationValues.Center },
                SpacingBetweenLines = new SpacingBetweenLines { After = "400" }
            };
            body.Append(titlePara);

            Paragraph subtitlePara = new Paragraph();
            Run subtitleRun = new Run();
            RunProperties subtitleProps = new RunProperties();
            subtitleProps.FontSize = new FontSize { Val = "24" };
            subtitleRun.Append(subtitleProps);
            subtitleRun.Append(new Text("User Manual"));
            subtitlePara.Append(subtitleRun);
            subtitlePara.ParagraphProperties = new ParagraphProperties
            {
                Justification = new Justification { Val = JustificationValues.Center },
                SpacingBetweenLines = new SpacingBetweenLines { After = "200" }
            };
            body.Append(subtitlePara);

            Paragraph versionPara = new Paragraph();
            Run versionRun = new Run();
            versionRun.Append(new Text("Version 1.0"));
            versionPara.Append(versionRun);
            versionPara.ParagraphProperties = new ParagraphProperties
            {
                Justification = new Justification { Val = JustificationValues.Center },
                SpacingBetweenLines = new SpacingBetweenLines { After = "600" }
            };
            body.Append(versionPara);

            // Page break
            body.Append(new Paragraph(new Run(new Break { Type = BreakValues.Page })));
        }

        private static void AddTableOfContents(Body body)
        {
            AddHeading(body, "Table of Contents", 1);
            
            string[] tocItems = {
                "1. Getting Started",
                "2. Initial Setup",
                "3. Managing Team Members",
                "4. One-on-One Meetings",
                "5. Managing Projects",
                "6. Managing Tasks",
                "7. Objectives, Key Results, and KPIs",
                "8. Dashboard & Analytics",
                "9. Reports & Export",
                "10. Settings & Configuration",
                "11. Calendar Integration",
                "Appendix A: Keyboard Shortcuts",
                "Appendix B: Troubleshooting"
            };

            foreach (var item in tocItems)
            {
                Paragraph para = new Paragraph();
                Run run = new Run();
                run.Append(new Text(item));
                para.Append(run);
                para.ParagraphProperties = new ParagraphProperties
                {
                    SpacingBetweenLines = new SpacingBetweenLines { After = "100" }
                };
                body.Append(para);
            }

            body.Append(new Paragraph(new Run(new Break { Type = BreakValues.Page })));
        }

        private static void AddChapter(Body body, string chapterNumber, string title, string content)
        {
            AddHeading(body, $"Chapter {chapterNumber}: {title}", 1);
            AddParagraph(body, content);
            body.Append(new Paragraph(new Run(new Break { Type = BreakValues.Page })));
        }

        private static void AddHeading(Body body, string text, int level)
        {
            Paragraph para = new Paragraph();
            Run run = new Run();
            RunProperties props = new RunProperties();
            props.FontSize = new FontSize { Val = level == 1 ? "32" : level == 2 ? "24" : "20" };
            props.Bold = new Bold();
            run.Append(props);
            run.Append(new Text(text));
            para.Append(run);
            para.ParagraphProperties = new ParagraphProperties
            {
                SpacingBetweenLines = new SpacingBetweenLines { After = "200", Before = level == 1 ? "400" : "200" }
            };
            body.Append(para);
        }

        private static void AddSubHeading(Body body, string text)
        {
            Paragraph para = new Paragraph();
            Run run = new Run();
            RunProperties props = new RunProperties();
            props.FontSize = new FontSize { Val = "20" };
            props.Bold = new Bold();
            run.Append(props);
            run.Append(new Text(text));
            para.Append(run);
            para.ParagraphProperties = new ParagraphProperties
            {
                SpacingBetweenLines = new SpacingBetweenLines { After = "120", Before = "240" }
            };
            body.Append(para);
        }

        private static void AddParagraph(Body body, string text)
        {
            string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    body.Append(new Paragraph());
                    continue;
                }

                Paragraph para = new Paragraph();
                Run run = new Run();
                
                // Handle bold text (**text**)
                string remaining = line;
                while (remaining.Contains("**"))
                {
                    int start = remaining.IndexOf("**");
                    if (start >= 0)
                    {
                        run.Append(new Text(remaining.Substring(0, start)));
                        remaining = remaining.Substring(start + 2);
                        int end = remaining.IndexOf("**");
                        if (end >= 0)
                        {
                            Run boldRun = new Run();
                            boldRun.RunProperties = new RunProperties { Bold = new Bold() };
                            boldRun.Append(new Text(remaining.Substring(0, end)));
                            para.Append(boldRun);
                            remaining = remaining.Substring(end + 2);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                run.Append(new Text(remaining));
                para.Append(run);
                para.ParagraphProperties = new ParagraphProperties
                {
                    SpacingBetweenLines = new SpacingBetweenLines { After = "120" }
                };
                body.Append(para);
            }
        }

        private static void AddBulletList(Body body, string[] items)
        {
            foreach (var item in items)
            {
                Paragraph para = new Paragraph();
                Run run = new Run();
                run.Append(new Text("• " + item));
                para.Append(run);
                para.ParagraphProperties = new ParagraphProperties
                {
                    SpacingBetweenLines = new SpacingBetweenLines { After = "100" },
                    Indentation = new Indentation { Left = "360" }
                };
                body.Append(para);
            }
        }

        private static void AddScreenshotPlaceholder(Body body, string description)
        {
            AddParagraph(body, $"[SCREENSHOT PLACEHOLDER: {description}]");
            Paragraph para = new Paragraph();
            para.ParagraphProperties = new ParagraphProperties
            {
                SpacingBetweenLines = new SpacingBetweenLines { After = "200" }
            };
            body.Append(para);
        }

        private static string GetGettingStartedContent()
        {
            return @"Tracker is a comprehensive team management application designed to help managers track team members, conduct one-on-one meetings, manage projects, and monitor performance through OKRs and KPIs.

**What is Tracker?**
Tracker is a Windows desktop application that provides a centralized platform for:
- Managing team member information and contact details
- Scheduling and documenting one-on-one meetings
- Tracking projects and their progress
- Managing tasks and action items
- Setting and monitoring Objectives and Key Results (OKRs)
- Tracking Key Performance Indicators (KPIs)
- Generating reports and analytics
- Integrating with Google Calendar for meeting synchronization

**System Requirements**
- Windows 10 or later
- .NET 8.0 Runtime
- Internet connection (for calendar integration and updates)

**Installation**
1. Download the Tracker installer
2. Run the installer and follow the on-screen instructions
3. Launch Tracker from the Start menu or desktop shortcut

**First Launch**
When you first launch Tracker, you will be guided through an initial setup wizard to configure your database connection. See Chapter 2: Initial Setup for detailed instructions.

**Application Overview**
The Tracker interface consists of:
- **Main Window**: The primary workspace with tabs for different sections
- **Dashboard**: Overview of key metrics and charts
- **Team Members**: Manage your team roster
- **One-on-Ones**: Schedule and document meetings
- **Projects**: Track project progress
- **Tasks**: Manage individual tasks
- **OKRs**: Objectives and Key Results
- **KPIs**: Key Performance Indicators

[SCREENSHOT PLACEHOLDER: Main application window showing all tabs]";
        }

        private static string GetInitialSetupContent()
        {
            return @"When you first launch Tracker, the Setup Wizard will guide you through configuring your database connection.

**Step 1: Choose Database Type**
You have two options:
- **Local Database (SQLite)**: Stores data locally on your computer. No server required. Recommended for individual use.
- **SQL Server**: Connects to a remote SQL Server instance. Requires server access credentials. Recommended for team/organizational use.

[SCREENSHOT PLACEHOLDER: Setup Wizard Step 1 - Database Type Selection]

**Step 2: Configure SQL Server (if selected)**
If you chose SQL Server, you'll need to provide:
- **Server Name**: The name or IP address of your SQL Server
- **Database Name**: The name of the database (TrackerDB will be created if it doesn't exist)
- **Authentication**: Choose between Windows Authentication or SQL Server Authentication
- **Credentials**: If using SQL Server Authentication, enter username and password
- **ODBC Connection**: Option to use an ODBC DSN instead of direct connection
- **Trust Server Certificate**: Check this if using self-signed certificates

[SCREENSHOT PLACEHOLDER: Setup Wizard Step 2 - SQL Server Configuration]

**Step 3: Summary and Sample Data**
Review your settings and choose whether to include sample data:
- **Include Sample Data**: Adds demo data including team members, meetings, projects, and tasks to help you get started
- **Empty Database**: Creates a fresh database with no sample data

[SCREENSHOT PLACEHOLDER: Setup Wizard Step 3 - Summary and Sample Data Options]

**Completing Setup**
Click ""Finish"" to complete the setup. Tracker will:
1. Create and configure your database
2. Set up all necessary tables
3. Optionally populate with sample data
4. Launch the main application

**Changing Database Settings Later**
You can change your database connection settings at any time through Settings → Database tab. Note that changing databases will not migrate your data - you'll need to set up the new database separately.";
        }

        private static string GetTeamMembersContent()
        {
            return @"The Team Members section allows you to manage your team roster, including contact information, roles, and relationships.

**Viewing Team Members**
1. Click the **Team** tab in the main window
2. You'll see a list of all team members in a grid
3. Click on a team member to view their detailed information in the right panel

[SCREENSHOT PLACEHOLDER: Team Members tab with list and detail view]

**Adding a Team Member**
1. Click the **Add Team Member** button (plus icon) in the toolbar
2. Fill in the team member's information:
   - **First Name** and **Last Name** (required)
   - **Email Address**
   - **Phone Number**
   - **Job Title**
   - **Role** (Manager, Developer, Designer, etc.)
   - **Specialty** (Technical area of expertise)
   - **Manager**: Select their manager from existing team members
   - **Social Media Links**: LinkedIn, Facebook, Instagram, X (Twitter) profiles
3. Click **Add** to save

[SCREENSHOT PLACEHOLDER: Add Team Member dialog]

**Editing a Team Member**
1. Select a team member from the list
2. Click the **Edit** button (pencil icon) in the toolbar
3. Modify the information as needed
4. Click **Update** to save changes

**Deleting a Team Member**
1. Select a team member from the list
2. Click the **Delete** button (trash icon) in the toolbar
3. Confirm the deletion in the dialog that appears

**Team Member Details Panel**
When you select a team member, the right panel shows:
- Full contact information
- Job details (title, role, specialty)
- Manager relationship
- Social media links
- Quick actions (Add 1:1 Meeting, Edit, Delete)

**Filtering and Searching**
Use the search box in the toolbar to quickly find team members by name, email, or job title.

**Reports**
Click the **Reports** button to export team member data to Excel. See Chapter 9 for more details.";
        }

        private static string GetOneOnOneContent()
        {
            return @"One-on-One meetings are a core feature of Tracker, allowing you to schedule, document, and track meetings with your team members.

**Viewing One-on-One Meetings**
1. Click the **One-on-Ones** tab
2. You'll see a list of all meetings, sorted by date
3. Select a meeting to view its details

[SCREENSHOT PLACEHOLDER: One-on-Ones tab with meeting list]

**Creating a New One-on-One Meeting**
1. Click the **Add 1:1** button in the toolbar
   - Or select a team member and click **Add 1:1 Meeting** from their detail panel
2. Fill in the meeting details:
   - **Description**: Meeting title or topic
   - **Date**: Meeting date
   - **Start Time** and **End Time**: Meeting duration
   - **Team Member**: Select the team member (pre-filled if launched from team member view)
   - **Agenda**: Pre-meeting agenda items
3. Click **Add** to create the meeting

[SCREENSHOT PLACEHOLDER: Add One-on-One dialog - Basic Information tab]

**Previous Meeting Summary**
If the team member has previous meetings, you'll see:
- A summary of the last meeting
- Number of action items, follow-ups, and discussion points
- A **Rollover Uncompleted Items** button to automatically add uncompleted items to the current meeting

[SCREENSHOT PLACEHOLDER: Previous Meeting Summary section]

**Discussion Points Tab**
Document topics discussed during the meeting:
1. Click **Add Discussion Point** button
2. Enter the discussion topic and details
3. Optionally link to an action item
4. Edit or delete discussion points as needed

**Concerns Tab**
Record any concerns raised by the team member:
1. Click **Add Concern** button
2. Enter the concern description and details
3. Set the severity level
4. Optionally link to an action item

**Action Items Tab**
Create action items from the meeting:
1. Click **Add Task** button
2. Enter the action item description
3. Set a due date
4. Assign to the team member or yourself
5. Mark as completed when done

**Follow-Up Items Tab**
Track items that need follow-up:
1. Click **Add Follow-Up** button
2. Enter the follow-up description
3. Set a due date
4. Track completion status

**Linked Items Tab (Phase 1 Feature)**
Link existing tasks, OKRs, or KPIs that were discussed in this meeting:
1. Select an available task, OKR, or KPI from the dropdown
2. Click **Link** to associate it with the meeting
3. Add discussion notes about how it was discussed
4. View all linked items in the grid below
5. Click **Unlink** to remove an association

[SCREENSHOT PLACEHOLDER: Linked Items tab showing linked tasks and OKRs]

**Benefits of Linking Items**
- Track which meetings discussed specific tasks, OKRs, or KPIs
- See ""Discussed In X meetings"" count on task/OKR/KPI detail views
- Build a history of how items have been discussed over time

**Rollover Uncompleted Items**
Automatically add uncompleted action items and follow-ups from previous meetings:
1. Click **Rollover Uncompleted Items** button
2. All uncompleted items from previous meetings are added to the current meeting
3. Review and adjust as needed

**Editing a Meeting**
1. Select a meeting from the list
2. Click **Edit** button
3. Modify any information
4. Click **Update** to save changes

**Deleting a Meeting**
1. Select a meeting from the list
2. Click **Delete** button
3. Confirm deletion

**Meeting Status**
Meetings can have different statuses:
- **Scheduled**: Upcoming meeting
- **Completed**: Meeting has occurred
- **Cancelled**: Meeting was cancelled

**Calendar Sync**
If calendar integration is enabled (see Chapter 11), meetings are automatically synced to your Google Calendar when created or updated.";
        }

        private static string GetProjectsContent()
        {
            return @"The Projects section helps you track project progress, budgets, timelines, and team assignments.

**Viewing Projects**
1. Click the **Projects** tab
2. View all projects in a grid format
3. Select a project to see its details

[SCREENSHOT PLACEHOLDER: Projects tab]

**Creating a New Project**
1. Click the **Add Project** button in the toolbar
2. Fill in project information:
   - **Name**: Project name (required)
   - **Description**: Detailed project description
   - **Start Date** and **End Date**: Project timeline
   - **Budget**: Project budget (optional)
   - **Status**: Current project status
   - **Owner**: Project owner/manager
   - **Team Members**: Select team members assigned to the project
3. Click **Add** to create the project

[SCREENSHOT PLACEHOLDER: Add Project dialog]

**Editing a Project**
1. Select a project from the list
2. Click **Edit** button
3. Update project information
4. Click **Update** to save

**Deleting a Project**
1. Select a project
2. Click **Delete** button
3. Confirm deletion

**Project Status**
Projects can have various statuses:
- **Planning**: In planning phase
- **Active**: Currently in progress
- **On Hold**: Temporarily paused
- **Completed**: Project finished
- **Cancelled**: Project cancelled

**Linking Projects to OKRs**
Projects can be associated with Objectives and Key Results. See Chapter 7 for details.";
        }

        private static string GetTasksContent()
        {
            return @"Tasks help you track individual work items, action items, and assignments.

**Viewing Tasks**
1. Click the **Tasks** tab
2. View all tasks in a grid
3. Tasks show owner, due date, completion status, and more

[SCREENSHOT PLACEHOLDER: Tasks tab]

**Creating a New Task**
1. Click the **Add Task** button in the toolbar
2. Fill in task details:
   - **Description**: Task description (required)
   - **Due Date**: When the task is due
   - **Owner**: Team member assigned to the task
   - **Task Type**: Type of task (Action Item, Follow-Up, etc.)
   - **Notes**: Additional notes or details
   - **Completed**: Check if task is already completed
3. Click **Add** to create the task

[SCREENSHOT PLACEHOLDER: Add Task dialog]

**Editing a Task**
1. Select a task from the list
2. Click **Edit** button
3. Update task information
4. Click **Update** to save

**Deleting a Task**
1. Select a task
2. Click **Delete** button
3. Confirm deletion

**Task Status**
Tasks show their completion status:
- **Completed**: Task is finished
- **In Progress**: Task is being worked on
- **Not Started**: Task hasn't been started

**Discussed In Meetings**
The ""Discussed In"" column shows how many one-on-one meetings have discussed this task. This helps you track which tasks are frequently brought up in conversations.

**Task Types**
Tasks can be categorized as:
- **Action Item**: Action item from a meeting
- **Follow-Up**: Follow-up item
- **General Task**: General work task";
        }

        private static string GetOKRsKPIsContent()
        {
            return @"OKRs (Objectives and Key Results) and KPIs (Key Performance Indicators) help you track performance and goals.

**Objectives and Key Results (OKRs)**
OKRs consist of an Objective (what you want to achieve) and multiple Key Results (measurable outcomes).

**Viewing OKRs**
1. Click the **OKRs** tab
2. View all OKRs with their status and completion percentage
3. Select an OKR to see details

[SCREENSHOT PLACEHOLDER: OKRs tab]

**Creating a New OKR**
1. Click the **Add OKR** button
2. Fill in OKR information:
   - **Title**: Objective title (required)
   - **Description**: Detailed description
   - **Start Date** and **End Date**: OKR timeframe
   - **Owner**: Team member responsible
   - **Project**: Optional project association
   - **Key Results**: Add multiple KPIs as key results
3. Click **Add** to create

[SCREENSHOT PLACEHOLDER: Add OKR dialog]

**Adding Key Results to an OKR**
1. In the OKR dialog, scroll to the Key Results section
2. Click **Add Key Result** button
3. Enter KPI details (see KPIs section below)
4. Key Results are automatically linked to the OKR

**Key Performance Indicators (KPIs)**
KPIs are measurable metrics that track performance.

**Viewing KPIs**
1. Click the **KPIs** tab
2. View all KPIs with their current values and status
3. Select a KPI to see details

[SCREENSHOT PLACEHOLDER: KPIs tab]

**Creating a New KPI**
1. Click the **Add KPI** button
2. Fill in KPI information:
   - **Name**: KPI name (required)
   - **Description**: What this KPI measures
   - **Value**: Current value
   - **Target Value**: Target or goal value
   - **Target Direction**: Whether higher or lower is better
   - **Owner**: Team member responsible
   - **OKR**: Optional OKR association
   - **Thresholds**: On-target and off-target thresholds
3. Click **Add** to create

[SCREENSHOT PLACEHOLDER: Add KPI dialog]

**KPI Status**
KPIs automatically calculate status based on their values:
- **On Target**: Value meets or exceeds target
- **Off Target**: Value is below target
- **Close to Target**: Value is near target threshold

**Discussed In Meetings**
Both OKRs and KPIs show how many meetings have discussed them, helping you track which goals are frequently reviewed.

**Editing and Deleting**
- Click **Edit** to modify an OKR or KPI
- Click **Delete** to remove (with confirmation)";
        }

        private static string GetDashboardContent()
        {
            return @"The Dashboard provides an overview of key metrics and visualizations.

**Accessing the Dashboard**
1. Click the **Dashboard** tab (first tab in the main window)
2. View summary statistics and charts

[SCREENSHOT PLACEHOLDER: Dashboard view]

**Summary Statistics**
The dashboard displays:
- **Total Team Members**: Number of team members in your database
- **Total One-on-Ones**: Total number of meetings
- **Upcoming One-on-Ones**: Meetings scheduled in the future
- **Total Tasks**: All tasks in the system
- **Completed Tasks Percentage**: Percentage of completed tasks
- **Total Projects**: Number of active projects

**Charts and Visualizations**
The dashboard includes several charts:

**Task Completion Chart**
- Pie chart showing completed vs. in-progress vs. not started tasks
- Visual representation of task status distribution

**OKR Progress Chart**
- Column chart showing OKR status (On Track, At Risk, Off Track)
- Helps identify which objectives need attention

**KPI Status Chart**
- Pie chart showing KPI status distribution
- Visual overview of performance indicators

**Team Activity Chart**
- Line chart showing one-on-one meeting frequency over time
- Helps track meeting consistency

**Refreshing Data**
Click the **Refresh** button to update all dashboard data with the latest information from the database.

**Dashboard Benefits**
- Quick overview of team health and activity
- Identify areas needing attention
- Track trends over time
- Visual representation of data";
        }

        private static string GetReportsContent()
        {
            return @"Tracker includes comprehensive reporting and export capabilities.

**Accessing Reports**
1. Click the **Reports** button in the Team Members toolbar
2. The Reports dialog opens with export options

[SCREENSHOT PLACEHOLDER: Reports dialog]

**Export Options**
You can export the following data types to Excel:

**Team Members Export**
- Exports all team member information
- Includes contact details, roles, and relationships
- File format: Excel (.xlsx)

**One-on-Ones Export**
- Exports all meeting records
- Includes dates, attendees, agendas, notes, and action items
- File format: Excel (.xlsx)

**Tasks Export**
- Exports all tasks
- Includes descriptions, due dates, owners, and status
- File format: Excel (.xlsx)

**Projects Export**
- Exports all projects
- Includes project details, timelines, budgets, and status
- File format: Excel (.xlsx)

**OKRs Export**
- Exports all Objectives and Key Results
- Includes OKR details, key results, and progress
- File format: Excel (.xlsx)

**KPIs Export**
- Exports all Key Performance Indicators
- Includes KPI values, targets, and status
- File format: Excel (.xlsx)

**Export All Data**
- Creates a comprehensive Excel workbook
- Includes a summary sheet plus separate sheets for each data type
- Perfect for comprehensive reporting or backups

**Exporting Data**
1. Click the export button for the data type you want
2. Choose a location and filename in the Save dialog
3. Click **Save**
4. A success notification confirms the export

**Using Exported Data**
- Open in Microsoft Excel or compatible spreadsheet software
- Create custom reports and analysis
- Share with stakeholders
- Use for presentations
- Create backups of your data

**Export Format**
All exports use Excel format (.xlsx) with:
- Formatted columns and headers
- Proper data types (dates, numbers, text)
- Multiple sheets (for ""Export All Data"")
- Professional formatting";
        }

        private static string GetSettingsContent()
        {
            return @"The Settings dialog allows you to configure Tracker to your preferences.

**Accessing Settings**
1. Click the **Settings** button (gear icon) in the toolbar
2. The Settings dialog opens with multiple tabs

[SCREENSHOT PLACEHOLDER: Settings dialog]

**General Settings Tab**

**Theme Selection**
Tracker supports multiple themes:
- **Default (Black/Gold)**: Classic black background with gold accents
- **Modern**: Modern color scheme
- **Light**: Light theme for bright environments
- **Spicy**: Vibrant color scheme

To change themes:
1. Select a theme from the dropdown
2. The theme is applied immediately
3. Changes are saved automatically

[SCREENSHOT PLACEHOLDER: Theme selection dropdown]

**Database Settings Tab**

**Current Connection Information**
View your current database configuration:
- **Type**: Local (SQLite) or SQL Server
- **Location**: Database file path or server location

**Changing Database Connection**
1. Click **Change Database Connection...** button
2. Confirm that you want to change (data will not be migrated)
3. Restart Tracker to run the setup wizard again

**Data Management**

**Add Sample Data**
- Populates your database with demo data
- Includes team members, meetings, projects, tasks, OKRs, and KPIs
- Useful for testing or learning the application
- If data already exists, you'll be warned that existing data will be replaced

**Clear All Data**
- **WARNING**: Permanently deletes ALL data from your database
- Requires double confirmation
- Cannot be undone
- Use with extreme caution

[SCREENSHOT PLACEHOLDER: Database Settings tab]

**Calendar Settings Tab**

See Chapter 11: Calendar Integration for detailed calendar settings information.

**Saving Settings**
All settings are saved automatically when changed. No need to click a ""Save"" button.";
        }

        private static string GetCalendarIntegrationContent()
        {
            return @"Tracker can integrate with Google Calendar to automatically sync your one-on-one meetings.

**Setting Up Google Calendar Integration**

**Prerequisites**
- A Google account
- Internet connection
- Google Calendar API access (configured in Google Cloud Console)

**Connecting Google Calendar**
1. Go to Settings → Calendar tab
2. In the Google Calendar section, click **Connect Google Calendar**
3. Your browser will open to Google's authorization page
4. Sign in with your Google account
5. Review the permissions Tracker is requesting
6. Click **Allow** to grant access
7. The browser will redirect back to Tracker
8. If automatic callback doesn't work, you can manually enter the authorization code
9. Once connected, you'll see ""Connected"" status with your email

[SCREENSHOT PLACEHOLDER: Calendar Settings - Google Calendar connection]

**Sync Options**
Configure how meetings are synced:

**Automatically sync meetings when created or updated**
- When enabled, meetings are automatically added/updated in Google Calendar
- When disabled, you must manually sync meetings
- Recommended: Enabled

**Send meeting invitations via email**
- Sends email invitations to meeting attendees
- Requires Gmail integration (coming in Phase 3)
- Currently: Placeholder for future feature

**Send meeting summaries via email after meetings**
- Sends summary emails after meetings complete
- Requires Gmail integration (coming in Phase 3)
- Currently: Placeholder for future feature

**How Calendar Sync Works**
1. When you create or update a one-on-one meeting, Tracker checks if calendar sync is enabled
2. If enabled, the meeting is automatically synced to your Google Calendar
3. The meeting appears in Google Calendar with:
   - Title: ""1:1 with [Team Member Name]""
   - Date and time from the meeting
   - Description including agenda and linked items
   - Attendee: The team member's email address
   - Reminders: 15 minutes before (email and popup)

**Updating Synced Meetings**
- If you edit a meeting in Tracker, it's automatically updated in Google Calendar
- Changes include: date, time, description, attendees

**Deleting Synced Meetings**
- If you delete a meeting in Tracker, it's removed from Google Calendar
- The calendar event is permanently deleted

**Disconnecting Google Calendar**
1. Go to Settings → Calendar tab
2. Click **Disconnect** button
3. Confirm disconnection
4. Meetings will no longer sync to Google Calendar
5. Existing calendar events are not deleted (you can delete them manually in Google Calendar)

**Troubleshooting Calendar Sync**

**Meetings not appearing in Google Calendar**
- Check that calendar sync is enabled in Settings
- Verify Google Calendar connection status
- Check your internet connection
- Review application logs for errors

**Authorization failed**
- Ensure you granted all requested permissions
- Try disconnecting and reconnecting
- Check that Google Calendar API is enabled in Google Cloud Console

**Manual Authorization Code Entry**
If automatic callback fails:
1. After authorizing in browser, copy the code from the URL
2. The code is after ""code="" in the address bar
3. Paste it into the manual entry dialog
4. Click Continue

**Outlook Calendar Integration**
Outlook Calendar integration is planned for Phase 2. Check back for updates!";
        }

        private static void AddAppendix(Body body)
        {
            AddHeading(body, "Appendix A: Keyboard Shortcuts", 1);
            AddParagraph(body, @"While Tracker is primarily mouse-driven, here are some helpful shortcuts:

**General Shortcuts**
- **Ctrl+S**: Save (in applicable dialogs)
- **Escape**: Close dialog or cancel operation
- **Enter**: Confirm/Submit (in applicable dialogs)
- **Tab**: Navigate between fields

**Navigation**
- Click tabs to switch between sections
- Use mouse to interact with all controls
- Scroll to view more items in lists

**Note**: Tracker is designed for mouse/touch interaction. Most operations are performed through buttons and menus.");

            AddHeading(body, "Appendix B: Troubleshooting", 1);
            
            AddSubHeading(body, "Application Won't Start");
            AddParagraph(body, @"- Ensure .NET 8.0 Runtime is installed
- Check Windows Event Viewer for error details
- Verify you have administrator privileges if needed
- Try running as administrator");

            AddSubHeading(body, "Database Connection Issues");
            AddParagraph(body, @"- Verify database file exists (for SQLite) or server is accessible (for SQL Server)
- Check firewall settings for SQL Server connections
- Verify credentials are correct
- Use the Test Connection button in Setup Wizard");

            AddSubHeading(body, "Data Not Appearing");
            AddParagraph(body, @"- Click Refresh button in the toolbar
- Check that you're viewing the correct tab
- Verify data exists in the database
- Try restarting the application");

            AddSubHeading(body, "Calendar Sync Not Working");
            AddParagraph(body, @"- Verify Google Calendar is connected in Settings
- Check internet connection
- Ensure auto-sync is enabled
- Try disconnecting and reconnecting Google Calendar
- Check application logs for detailed error messages");

            AddSubHeading(body, "Performance Issues");
            AddParagraph(body, @"- Close and reopen the application
- Check available disk space
- Verify database file isn't corrupted
- Clear old data if database is very large");

            AddSubHeading(body, "Getting Help");
            AddParagraph(body, @"- Check application logs in: %AppData%\\Tracker\\Logs\\
- Review error messages in notifications
- Contact your system administrator
- Refer to this manual for feature documentation");
        }
    }
}

