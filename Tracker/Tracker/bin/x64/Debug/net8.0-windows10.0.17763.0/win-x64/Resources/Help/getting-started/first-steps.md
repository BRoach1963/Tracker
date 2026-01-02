# First Steps with Tracker

This guide walks you through your first day with Tracker, from account creation to running your first 1:1.

## Before You Begin

Make sure you have:
- A valid email address
- 15-20 minutes to set up
- Basic info about your team members

## Part 1: Account Setup

### Creating Your Account

1. **Launch Tracker** from your Start menu or desktop
2. On the sign-in screen, click **Create Account**
3. Fill in your information:
   - **Display Name**: How you'll appear in the app
   - **Email**: Your email (used for sign-in and notifications)
   - **Password**: Create a strong password
   - **Confirm Password**: Re-enter to verify
4. Click **Create Account**
5. **Check your email** for a verification link
6. Click the link to verify your account

### Choosing Your Plan

After verification, you're on the **Free** plan with a fresh subscription:
- 10 team members
- Unlimited 1:1s, tasks, projects, OKRs, KPIs
- Local database storage
- Your own isolated settings and data

[Compare all plans](../account/subscriptions.md) to see if Standard ($7/mo) or Pro ($12/mo) fits your needs.

## Part 2: Setup Wizard

### Step 1: Welcome
The wizard introduces you to Tracker. Click **Next** to continue.

### Step 2: Database Setup
Choose where your data lives:

| Option | Best For |
|--------|----------|
| **Local (Default)** | Most users - fast, private, on your machine |
| **Custom Location** | Teams (2-10 users) sharing a network folder |
| **Network Database** | Enterprise teams (10+) with SQL Server (Pro only) |

#### Local Database (Recommended for Beginners)
- Data stored in `%LocalAppData%\Tracker\Users\{your-account-id}\`
- Each account has isolated settings and database preferences
- No setup required
- Perfect for individual users
- Click **Next** to continue

#### Custom Location (Team Sharing)
For small teams sharing a network folder:

1. Select **Local Database**
2. ✅ Check **"Use custom database location"**
3. Click **Browse...** and navigate to your network share
4. Example path: `\\fileserver\TrackerData\tracker.db`
5. All team members use the SAME path to share data

**When to use**:
- Small team (2-10 people) without SQL Server
- Shared network drive available to all
- Everyone needs read/write permissions to the folder

**See**: [Shared Database Setup Guide](../../SHARED_DATABASE_QUICK_START.md) for detailed instructions

#### Network Database (SQL Server)
For enterprise teams requiring advanced features - see [SQL Server Setup](../reference/sql-server-setup.md)

**Recommendation**: 
- **New users**: Start with Local
- **Small teams**: Use Custom Location with network share
- **Large teams (10+)**: Use SQL Server
- You can migrate later if needs change

### Step 3: Sample Data
- **Include sample data**: Great for learning how things work
- **Start fresh**: Better if you want to add real data immediately

**Recommendation**: Include sample data for your first time.

### Step 4: Finish
Click **Finish** to complete setup and enter Tracker!

## Part 3: Exploring the Interface

### The Dashboard (Home)
Your command center showing:
- Today's meetings
- Upcoming 1:1s
- Overdue tasks
- OKR progress
- Quick stats

### Navigation Pillars
| Pillar | What's Inside |
|--------|---------------|
| 🏠 **Home** | Dashboard |
| 👥 **Circle** | Team Members, 1:1s, Feedback, Goals |
| 📊 **Pulse** | Projects, Tasks, OKRs, KPIs |
| 📝 **Chronicle** | Notes, Reports |
| ⚙️ **Settings** | Preferences, Account |

### Your Profile
Click your **initials** (top-right) to:
- View account settings
- See your subscription
- Sign out

## Part 4: Adding Team Members

Your team is at the heart of Tracker. Let's add your first team member.

### Add a Team Member

1. Go to **Circle** → **Team** (or press `Alt+2`)
2. Click **+ Add Team Member**
3. Fill in the basics:
   - **Name** (required)
   - **Job Title** (e.g., "Software Engineer")
   - **Email** (for contact reference)
   - **Start Date** (when they joined your team)
4. Add optional details:
   - **Skills**: What are they good at?
   - **Notes**: Anything else important
5. Click **Save**

### Tips for Team Setup
- Add at least 2-3 team members to see the full experience
- If you loaded sample data, you already have example team members
- You can always edit profiles later

## Part 5: Your First 1:1

1:1 meetings are where management happens. Let's schedule one.

### Schedule a 1:1

1. Go to **Circle** → **1:1s**
2. Click **+ New 1:1**
3. Select a **Team Member**
4. Set the **Date** and **Time**
5. Add **Agenda Items**:
   - Career development
   - Current projects
   - Any blockers?
   - Feedback
6. Click **Save**

### Prepare Your Agenda
Good agenda items are specific:
- ❌ "Check in"
- ✅ "Review progress on Q4 project"
- ✅ "Discuss promotion timeline"
- ✅ "Address concern about workload"

### Take Notes
During the 1:1:
1. Open the meeting from the 1:1s list
2. Use the **Notes** section for key points
3. Mark agenda items complete as you discuss them
4. Create follow-up tasks directly from the meeting

## Part 6: Creating Tasks

Tasks are action items for you or your team.

### Create a Task

1. Go to **Pulse** → **Tasks**
2. Click **+ New Task**
3. Fill in:
   - **Title**: What needs to be done
   - **Description**: Details and context
   - **Assigned To**: Who's responsible
   - **Due Date**: When it's due
   - **Priority**: Critical, High, Medium, Low
4. Click **Save**

### Link to 1:1s
You can create tasks directly from 1:1 meetings:
1. During a 1:1, click **Create Task**
2. The task is automatically linked to that meeting
3. Great for follow-up commitments!

## Part 7: Setting Up OKRs

OKRs (Objectives and Key Results) align your team to goals.

### Create an OKR

1. Go to **Pulse** → **OKRs**
2. Click **+ New OKR**
3. Define the **Objective**:
   - Qualitative and inspiring
   - Example: "Improve team productivity"
4. Add **Key Results**:
   - Measurable outcomes
   - Example: "Reduce bug count by 50%"
   - Example: "Ship 3 major features"
5. Set the **Time Period** (Quarter/Year)
6. Click **Save**

### Track Progress
- Update Key Results as work progresses
- Link KPIs for automatic tracking
- Review weekly with your team

## Part 8: Daily Workflow

### Morning Routine
1. Check **Dashboard** for today's meetings
2. Review **overdue tasks**
3. Glance at **OKR progress**

### Throughout the Day
- Capture thoughts in **Quick Notes** (`Ctrl+Shift+N`)
- Update task statuses
- Record feedback when it happens

### Weekly Routine
1. Review all **upcoming 1:1s**
2. Update **OKR progress**
3. Check **KPI health**
4. Plan next week's priorities

## Getting Help

### Oracle - AI Assistant (Standard+ Plans)
Press **F1** anytime to ask:
- "How do I create an OKR?"
- "What's the best way to structure 1:1s?"
- "How do I export my data?"

### Documentation
- Click the **ⓘ** (info) icon on any screen
- Browse the help center
- Search for specific topics

### Support
- **Free**: Community forums
- **Standard**: Email support (24hr response)
- **Pro**: Priority support (4hr response)

## Next Steps

Now that you're set up:
1. ✅ Account created
2. ✅ Team members added
3. ✅ First 1:1 scheduled
4. ✅ Tasks created
5. ✅ OKRs defined

### Continue Learning
- [Master the Dashboard](../features/dashboard.md)
- [Deep dive into 1:1s](../features/one-on-ones.md)
- [Understand OKRs](../features/okrs.md)
- [Track KPIs effectively](../features/kpis.md)
- [Keyboard shortcuts](../reference/keyboard-shortcuts.md)

### Need More Features?
Compare [subscription plans](../account/subscriptions.md) to unlock:
- Oracle (AI assistant)
- Reports & Export
- Calendar Sync
- And more!

---

**Questions?** Press F1 for help or email support@pricklycactus.com
