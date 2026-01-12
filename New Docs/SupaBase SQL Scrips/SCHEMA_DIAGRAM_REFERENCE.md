# Supabase Schema Diagram Reference

**Received:** January 12, 2026  
**Source:** pgAdmin diagrammed schema export  
**Content:** Full Primary Key - Foreign Key relationship mapping for all 60 Supabase tables

## What This Diagram Shows

- All 60 tables with complete column listings
- Primary Key designations (PK)
- Foreign Key relationships (FK) with cardinality
- Table groupings by domain (Users, Meetings, Tasks, Goals, etc.)
- Complete data model relationships

## Key Tables Visible

**Core Domain:**
- users (UUID primary key)
- team_members (references users, organizations, teams)
- organizations
- teams
- team_memberships

**Meetings & 1:1s:**
- meetings (UUID, soft-delete pattern)
- meeting_attendees
- meeting_notes
- meeting_agenda_items

**Work Management:**
- tasks (references goals, projects, team_members)
- goals (references organizations, team_members)
- targets
- metrics
- projects

**Relationships & Recognition:**
- feedback (references team_members)
- kudos
- development_goals
- performance_reviews

**Infrastructure (Do Not Model in Dapper):**
- activity_log
- user_sessions
- notification_preferences
- vector_embeddings
- announcement_reads

**Surveys & Reviews:**
- pulse_surveys
- survey_questions
- survey_responses
- survey_answers
- review_templates

## Usage for Dapper Migration

This diagram is the authoritative source for understanding:
1. Which tables to create repositories for (35-40 core tables)
2. Foreign key relationships (for JOIN queries in repositories)
3. Soft delete patterns (is_deleted, deleted_at, deleted_by columns)
4. Sync infrastructure (sync_id, sync_version columns for future offline support)
5. UUID vs int ID strategy (all are UUIDs)

## Reference During Repository Creation

When creating repository methods:
- Check this diagram for FK relationships
- Use it to understand query requirements
- Reference for complex aggregations/joins

**This is the single source of truth for data structure.**
