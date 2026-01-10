# Tracker Database - Supabase Setup

This folder contains SQL scripts to set up the Tracker database in Supabase from scratch.

## Quick Start

1. Open your Supabase project dashboard: https://app.supabase.com
2. Go to **SQL Editor**
3. Run each script in numerical order (00 → 18)

## Scripts Overview

### Schema Setup (Run First)

| Script | Purpose |
|--------|---------|
| `00_DROP_ALL.sql` | Drops all existing tables for a clean slate |
| `01_EXTENSIONS_TYPES.sql` | Enables uuid-ossp, pgcrypto, vector extensions; creates enums |
| `02_CORE_TABLES.sql` | Creates organizations, roles, users, user_roles tables |
| `03_TEAMS.sql` | Creates teams, team_members, team_memberships, manager_history |
| `04_GOALS.sql` | Creates goals, targets, target_measurables, goal_milestones |
| `05_METRICS.sql` | Creates metrics, metric_data_sources, metric_history |
| `06_PROJECTS_TASKS.sql` | Creates projects, tasks, milestones, task_collections |
| `07_MEETINGS.sql` | Creates meetings, agenda items, notes, action items, talking points |
| `08_FEEDBACK.sql` | Creates feedback, feedback_requests, recognition, performance_reviews |
| `09_NOTES.sql` | Creates notes, note_templates, journal_entries |
| `10_AI_VECTORS.sql` | Creates vector_embeddings, ai_conversations, ai_messages, ai_insights |
| `11_ACTIVITY_NOTIFICATIONS.sql` | Creates activity_log, notifications, announcements |
| `12_RLS_POLICIES.sql` | Enables Row Level Security on all tables with policies |

### Seed Data (Run After Schema)

| Script | Purpose |
|--------|---------|
| `13_SEED_ROLES.sql` | Creates function to seed default roles for new organizations |
| `14_SEED_TEST_ORG.sql` | Creates "Acme Corporation" test organization |
| `15_SEED_TEST_USERS.sql` | Creates 8 test users with org hierarchy |
| `16_SEED_GOALS_METRICS.sql` | Creates sample goals, targets, and metrics |
| `17_SEED_TASKS_PROJECTS.sql` | Creates sample projects and tasks |
| `18_SEED_MEETINGS_FEEDBACK.sql` | Creates sample meetings and feedback |

### Verification

| Script | Purpose |
|--------|---------|
| `99_RUN_VERIFICATION.sql` | Queries to verify setup was successful |

## Test Organization Structure

After running all scripts, you'll have:

```
Acme Corporation
├── Sarah Chen (CEO) - Admin
│   ├── Marcus Johnson (VP Engineering) - Manager
│   │   └── Emily Rodriguez (Engineering Manager) - Manager
│   │       └── David Kim (Team Lead) - Team Lead
│   │           ├── Jessica Thompson (Senior Dev) - Member
│   │           └── Alex Martinez (Developer) - Member
│   └── Rachel Green (Product Manager) - Manager
│       └── Michael Brown (Designer) - Member
```

### Teams
- **Engineering**: Marcus (lead), Emily, David, Jessica, Alex
- **Product**: Rachel (lead), Michael
- **Platform**: David (lead), Jessica

### Sample Data
- 4 Goals (company, team, individual levels)
- 7 Targets (key results)
- 5 Metrics with historical data
- 3 Projects with milestones
- 13 Tasks including subtasks
- 5 Meetings (1:1s, team, all-hands)
- 4 Feedback entries
- 2 Recognition posts

## Role System

5 default roles with 30+ granular permissions:

| Role | Hierarchy | Key Permissions |
|------|-----------|-----------------|
| Admin | 100 | Full access to everything |
| Manager | 75 | Manage team members, goals, metrics, feedback |
| Team Lead | 50 | Lead team activities, limited admin |
| Member | 25 | Standard access, own tasks/goals |
| Viewer | 10 | Read-only access |

## Terminology

This database uses universal business terminology:

| Old Term | New Term | Description |
|----------|----------|-------------|
| OKR | Goal | Objective (what you want to achieve) |
| Key Result | Target | Measurable outcome |
| KPI | Metric | Key performance indicator |

## Important Notes

### Vector Extension
The `10_AI_VECTORS.sql` script requires the `pgvector` extension. In Supabase:
1. Go to **Database** → **Extensions**
2. Search for "vector"
3. Enable the extension

### Row Level Security
After running `12_RLS_POLICIES.sql`, tables are protected by RLS. The policies use `auth.uid()` to identify users. For testing without authentication, you may need to:
```sql
-- Temporarily disable RLS for testing (don't do in production!)
ALTER TABLE table_name DISABLE ROW LEVEL SECURITY;
```

### Resetting the Database
To start over, just run `00_DROP_ALL.sql` first, then run the remaining scripts in order.

## Connection Info

After setup, connect your app using:
- **Host**: Your Supabase project URL
- **API Key**: From Project Settings → API
- **Database**: postgres (direct connection) or via Supabase client

## Troubleshooting

### "relation already exists"
Run `00_DROP_ALL.sql` first to clean up existing tables.

### "extension does not exist"
Enable the required extension in Supabase dashboard under Database → Extensions.

### "permission denied"
Make sure you're running as the database owner or have sufficient privileges.
