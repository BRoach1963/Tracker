-- =============================================================================
-- Tracker Development Seed Data for PostgreSQL
-- =============================================================================
-- This script seeds sample data for development and testing.
-- Run with: psql -h localhost -U tracker_app -d tracker -f seed_development_data.sql
-- =============================================================================
-- IMPORTANT: All EF Core tables require these audit columns:
--   CreatedBy (NOT NULL), LastModifiedBy (NOT NULL), IsDeleted (NOT NULL)
-- Many tables also have additional NOT NULL columns with empty string defaults.
-- =============================================================================

-- Generate a consistent organization ID for referential integrity
DO $$
DECLARE
    org_id UUID := gen_random_uuid();
    brian_user_id INT;
    brian_tm_id INT;
    mike_tm_id INT;
    matt_tm_id INT;
    teryl_tm_id INT;
    pat_tm_id INT;
    karl_tm_id INT;
    grady_tm_id INT;
    seed_user TEXT := 'seed-script';
    empty_guid UUID := '00000000-0000-0000-0000-000000000000';
BEGIN

-- =============================================================================
-- Organization
-- =============================================================================
INSERT INTO "Organization" (
    "Id", "Name", "Slug", "IsActive", "SubscriptionTier", "MaxUsers", "MaxTeamMembers", 
    "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy", "IsDeleted"
)
VALUES (
    org_id, 'Prickly Cactus Software', 'prickly-cactus-software', true, 'Enterprise', 100, 100, 
    NOW(), seed_user, NOW(), seed_user, false
);

RAISE NOTICE 'Created Organization with ID: %', org_id;

-- =============================================================================
-- Users (EF Core Users table)
-- =============================================================================
INSERT INTO "Users" (
    "OrganizationId", "Username", "Email", "DisplayName", "IsAdmin", "IsActive", "Role",
    "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy", "IsDeleted"
)
VALUES (
    org_id, 'brian', 'brian@pricklycactussoftware.com', 'Brian Flores', true, true, 'Admin',
    NOW(), seed_user, NOW(), seed_user, false
)
RETURNING "Id" INTO brian_user_id;

RAISE NOTICE 'Created User with ID: %', brian_user_id;

-- =============================================================================
-- Team Members (Steelers coaching staff theme)
-- All the NOT NULL columns need values or empty strings
-- Specialty, SkillLevel, Role are integers (enums)
-- ProfileImage is bytea (use '\x' for empty)
-- =============================================================================
-- Brian is the manager
INSERT INTO "TeamMembers" (
    "FirstName", "LastName", "NickName", "Email", "CellPhone", "JobTitle", 
    "BirthDay", "HireDate", "TerminationDate", "IsActive", "ManagerId", 
    "ProfileImage", "LinkedInProfile", "FacebookProfile", "InstagramProfile", "XProfile",
    "Specialty", "SkillLevel", "Role", "OpenTaskCount", "ActiveGoalCount", "UpcomingMeetingCount",
    "OrganizationId", "UserId",
    "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy", "IsDeleted"
)
VALUES (
    'Brian', 'Flores', '', 'brian@pricklycactussoftware.com', '', 'Engineering Manager',
    '1985-06-15', '2020-01-15', '0001-01-01', true, 0,
    '\x', '', '', '', '',
    0, 0, 0, 0, 0, 0,
    org_id, brian_user_id,
    NOW(), seed_user, NOW(), seed_user, false
)
RETURNING "Id" INTO brian_tm_id;

-- Other team members with Brian as manager
INSERT INTO "TeamMembers" (
    "FirstName", "LastName", "NickName", "Email", "CellPhone", "JobTitle", 
    "BirthDay", "HireDate", "TerminationDate", "IsActive", "ManagerId", 
    "ProfileImage", "LinkedInProfile", "FacebookProfile", "InstagramProfile", "XProfile",
    "Specialty", "SkillLevel", "Role", "OpenTaskCount", "ActiveGoalCount", "UpcomingMeetingCount",
    "OrganizationId", "UserId",
    "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy", "IsDeleted"
)
VALUES (
    'Mike', 'Tomlin', '', 'mike.tomlin@pricklycactussoftware.com', '', 'Senior Software Engineer',
    '1990-03-15', '2021-03-01', '0001-01-01', true, brian_tm_id,
    '\x', '', '', '', '',
    0, 2, 0, 0, 0, 0,
    org_id, brian_user_id,
    NOW(), seed_user, NOW(), seed_user, false
)
RETURNING "Id" INTO mike_tm_id;

INSERT INTO "TeamMembers" (
    "FirstName", "LastName", "NickName", "Email", "CellPhone", "JobTitle", 
    "BirthDay", "HireDate", "TerminationDate", "IsActive", "ManagerId", 
    "ProfileImage", "LinkedInProfile", "FacebookProfile", "InstagramProfile", "XProfile",
    "Specialty", "SkillLevel", "Role", "OpenTaskCount", "ActiveGoalCount", "UpcomingMeetingCount",
    "OrganizationId", "UserId",
    "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy", "IsDeleted"
)
VALUES (
    'Matt', 'Canada', '', 'matt.canada@pricklycactussoftware.com', '', 'Software Engineer',
    '1988-11-20', '2022-06-15', '0001-01-01', true, brian_tm_id,
    '\x', '', '', '', '',
    0, 1, 0, 0, 0, 0,
    org_id, brian_user_id,
    NOW(), seed_user, NOW(), seed_user, false
)
RETURNING "Id" INTO matt_tm_id;

INSERT INTO "TeamMembers" (
    "FirstName", "LastName", "NickName", "Email", "CellPhone", "JobTitle", 
    "BirthDay", "HireDate", "TerminationDate", "IsActive", "ManagerId", 
    "ProfileImage", "LinkedInProfile", "FacebookProfile", "InstagramProfile", "XProfile",
    "Specialty", "SkillLevel", "Role", "OpenTaskCount", "ActiveGoalCount", "UpcomingMeetingCount",
    "OrganizationId", "UserId",
    "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy", "IsDeleted"
)
VALUES (
    'Teryl', 'Austin', '', 'teryl.austin@pricklycactussoftware.com', '', 'DevOps Engineer',
    '1982-04-10', '2021-09-01', '0001-01-01', true, brian_tm_id,
    '\x', '', '', '', '',
    0, 2, 0, 0, 0, 0,
    org_id, brian_user_id,
    NOW(), seed_user, NOW(), seed_user, false
)
RETURNING "Id" INTO teryl_tm_id;

INSERT INTO "TeamMembers" (
    "FirstName", "LastName", "NickName", "Email", "CellPhone", "JobTitle", 
    "BirthDay", "HireDate", "TerminationDate", "IsActive", "ManagerId", 
    "ProfileImage", "LinkedInProfile", "FacebookProfile", "InstagramProfile", "XProfile",
    "Specialty", "SkillLevel", "Role", "OpenTaskCount", "ActiveGoalCount", "UpcomingMeetingCount",
    "OrganizationId", "UserId",
    "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy", "IsDeleted"
)
VALUES (
    'Pat', 'Meyer', '', 'pat.meyer@pricklycactussoftware.com', '', 'QA Engineer',
    '1995-08-25', '2023-01-10', '0001-01-01', true, brian_tm_id,
    '\x', '', '', '', '',
    0, 1, 0, 0, 0, 0,
    org_id, brian_user_id,
    NOW(), seed_user, NOW(), seed_user, false
)
RETURNING "Id" INTO pat_tm_id;

INSERT INTO "TeamMembers" (
    "FirstName", "LastName", "NickName", "Email", "CellPhone", "JobTitle", 
    "BirthDay", "HireDate", "TerminationDate", "IsActive", "ManagerId", 
    "ProfileImage", "LinkedInProfile", "FacebookProfile", "InstagramProfile", "XProfile",
    "Specialty", "SkillLevel", "Role", "OpenTaskCount", "ActiveGoalCount", "UpcomingMeetingCount",
    "OrganizationId", "UserId",
    "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy", "IsDeleted"
)
VALUES (
    'Karl', 'Dunbar', '', 'karl.dunbar@pricklycactussoftware.com', '', 'Frontend Developer',
    '1992-12-05', '2022-11-01', '0001-01-01', true, brian_tm_id,
    '\x', '', '', '', '',
    0, 1, 0, 0, 0, 0,
    org_id, brian_user_id,
    NOW(), seed_user, NOW(), seed_user, false
)
RETURNING "Id" INTO karl_tm_id;

INSERT INTO "TeamMembers" (
    "FirstName", "LastName", "NickName", "Email", "CellPhone", "JobTitle", 
    "BirthDay", "HireDate", "TerminationDate", "IsActive", "ManagerId", 
    "ProfileImage", "LinkedInProfile", "FacebookProfile", "InstagramProfile", "XProfile",
    "Specialty", "SkillLevel", "Role", "OpenTaskCount", "ActiveGoalCount", "UpcomingMeetingCount",
    "OrganizationId", "UserId",
    "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy", "IsDeleted"
)
VALUES (
    'Grady', 'Brown', '', 'grady.brown@pricklycactussoftware.com', '', 'Backend Developer',
    '1993-02-18', '2023-04-15', '0001-01-01', true, brian_tm_id,
    '\x', '', '', '', '',
    0, 1, 0, 0, 0, 0,
    org_id, brian_user_id,
    NOW(), seed_user, NOW(), seed_user, false
)
RETURNING "Id" INTO grady_tm_id;

RAISE NOTICE 'Created 7 Team Members';

-- =============================================================================
-- Projects
-- =============================================================================
-- First check if Projects has required NOT NULL columns
INSERT INTO "Projects" (
    "OrganizationId", "Name", "Description", "Status", "StartDate", "EndDate", "Budget", 
    "OwnerId", "UserId",
    "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy", "IsDeleted"
)
VALUES 
(org_id, 'Tracker 2.0 Release', 'Major release with PostgreSQL support and new features', 'Active', '2025-10-01', '2026-03-31', 150000.00, brian_tm_id, brian_user_id, NOW(), seed_user, NOW(), seed_user, false),
(org_id, 'Mobile App Development', 'Cross-platform mobile companion app', 'NotStarted', '2026-02-01', '2026-08-31', 200000.00, mike_tm_id, brian_user_id, NOW(), seed_user, NOW(), seed_user, false),
(org_id, 'API Gateway Migration', 'Migrate to new API gateway infrastructure', 'Active', '2025-11-15', '2026-01-31', 50000.00, teryl_tm_id, brian_user_id, NOW(), seed_user, NOW(), seed_user, false),
(org_id, 'Security Audit Remediation', 'Address findings from Q4 security audit', 'Active', '2025-12-01', '2026-02-28', 75000.00, brian_tm_id, brian_user_id, NOW(), seed_user, NOW(), seed_user, false);

RAISE NOTICE 'Created 4 Projects';

-- =============================================================================
-- Tasks  
-- =============================================================================
INSERT INTO "Tasks" (
    "OrganizationId", "Description", "Notes", "IsCompleted", "DueDate", 
    "OwnerId", "ProjectId", "UserId",
    "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy", "IsDeleted"
)
SELECT 
    org_id, t.task_desc, t.task_notes, t.completed, t.due::timestamp,
    t.owner_id, p."ID", brian_user_id,
    NOW(), seed_user, NOW(), seed_user, false
FROM (VALUES
    ('Complete PostgreSQL migration testing', 'Run full regression suite', false, '2026-01-10', mike_tm_id, 'Tracker 2.0 Release'),
    ('Update documentation for 2.0 release', 'Include new features and breaking changes', false, '2026-01-20', matt_tm_id, 'Tracker 2.0 Release'),
    ('Implement mobile app login flow', 'OAuth2 with biometric support', false, '2026-02-15', karl_tm_id, 'Mobile App Development'),
    ('Configure API rate limiting', 'Implement per-user rate limits', false, '2026-01-08', teryl_tm_id, 'API Gateway Migration'),
    ('Fix SQL injection vulnerability', 'Parameterize all dynamic queries', false, '2026-01-07', grady_tm_id, 'Security Audit Remediation'),
    ('Code review for auth module', 'Security-focused review', true, '2025-12-20', pat_tm_id, 'Security Audit Remediation')
) AS t(task_desc, task_notes, completed, due, owner_id, proj_name)
JOIN "Projects" p ON p."Name" = t.proj_name;

RAISE NOTICE 'Created Tasks';

-- =============================================================================
-- One-on-Ones
-- Duration, StartTime, EndTime are interval types; Status is integer
-- ManagerId FK points to Users table, not TeamMembers
-- Many NOT NULL text fields need defaults
-- =============================================================================
INSERT INTO "OneOnOnes" (
    "OrganizationId", "TeamMemberId", "ManagerId", "Date", "StartTime", "EndTime", "Duration", 
    "IsRecurring", "Status", "Description", "Agenda", "Notes", "Feedback", 
    "SyncStatus", "IsSyncedToGoogle",
    "UserId",
    "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy", "IsDeleted"
)
VALUES
(org_id, mike_tm_id, brian_user_id, '2025-12-30 14:00:00', '14:00', '14:30', '30 minutes', false, 0, 'Weekly 1:1 with Mike', '', 'Discussed PostgreSQL migration progress.', 'Great progress on migration.', '', false, brian_user_id, NOW(), seed_user, NOW(), seed_user, false),
(org_id, matt_tm_id, brian_user_id, '2025-12-27 10:00:00', '10:00', '10:30', '30 minutes', false, 0, 'Weekly 1:1 with Matt', '', 'Discussed documentation needs.', 'Taking ownership of docs.', '', false, brian_user_id, NOW(), seed_user, NOW(), seed_user, false),
(org_id, teryl_tm_id, brian_user_id, '2025-12-28 11:00:00', '11:00', '11:30', '30 minutes', false, 0, 'Weekly 1:1 with Teryl', '', 'API gateway migration is going well.', 'Solid technical approach.', '', false, brian_user_id, NOW(), seed_user, NOW(), seed_user, false),
(org_id, pat_tm_id, brian_user_id, '2025-12-29 15:00:00', '15:00', '15:30', '30 minutes', false, 0, 'Weekly 1:1 with Pat', '', 'QA automation progress.', 'Excellent progress on automation.', '', false, brian_user_id, NOW(), seed_user, NOW(), seed_user, false),
(org_id, karl_tm_id, brian_user_id, '2025-12-26 09:00:00', '09:00', '09:30', '30 minutes', false, 0, 'Weekly 1:1 with Karl', '', 'Mobile app wireframes approved.', 'Good collaboration with design team.', '', false, brian_user_id, NOW(), seed_user, NOW(), seed_user, false),
(org_id, grady_tm_id, brian_user_id, '2025-12-27 16:00:00', '16:00', '16:30', '30 minutes', false, 0, 'Weekly 1:1 with Grady', '', 'Security fix prioritization.', 'Quick response on security issues.', '', false, brian_user_id, NOW(), seed_user, NOW(), seed_user, false);

RAISE NOTICE 'Created 6 One-on-Ones';

-- =============================================================================
-- Feedback
-- Type is an integer (enum: 0=Positive, 1=Constructive, etc)
-- =============================================================================
INSERT INTO "Feedbacks" (
    "OrganizationId", "TeamMemberId", "Date", "Type", "Title", "Content", "Context", 
    "UserId",
    "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy", "IsDeleted"
)
VALUES
(org_id, mike_tm_id, '2025-12-15', 0, 'PostgreSQL Migration Leadership', 'Mike has been doing an excellent job leading the PostgreSQL migration.', 'Q4 2025 Performance Review', brian_user_id, NOW(), seed_user, NOW(), seed_user, false),
(org_id, teryl_tm_id, '2025-12-20', 0, 'API Gateway Excellence', 'Teryls work on the API gateway has been outstanding.', 'Project Retrospective', brian_user_id, NOW(), seed_user, NOW(), seed_user, false),
(org_id, grady_tm_id, '2025-12-28', 0, 'Security Response', 'Grady responded quickly to the security vulnerability report.', 'Security Incident Response', brian_user_id, NOW(), seed_user, NOW(), seed_user, false),
(org_id, matt_tm_id, '2025-12-22', 1, 'Communication Improvement', 'Matt could improve his communication during standups.', 'Sprint Retrospective', brian_user_id, NOW(), seed_user, NOW(), seed_user, false),
(org_id, karl_tm_id, '2025-12-26', 0, 'Design Collaboration', 'Karl has been great at collaborating with the design team.', 'Cross-team Feedback', brian_user_id, NOW(), seed_user, NOW(), seed_user, false);

RAISE NOTICE 'Created 5 Feedback entries';

-- =============================================================================
-- Individual Goals
-- Status and Category are integers (enums)
-- ProgressPercent and Notes are required
-- =============================================================================
INSERT INTO "IndividualGoals" (
    "OrganizationId", "TeamMemberId", "Title", "Description", "Status", "TargetDate", "Category", 
    "ProgressPercent", "Notes",
    "UserId",
    "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy", "IsDeleted"
)
VALUES
(org_id, mike_tm_id, 'AWS Solutions Architect Certification', 'Obtain AWS SAA certification', 1, '2026-03-31', 0, 50, '', brian_user_id, NOW(), seed_user, NOW(), seed_user, false),
(org_id, matt_tm_id, 'Improve Technical Writing', 'Complete technical writing course', 1, '2026-06-30', 0, 30, '', brian_user_id, NOW(), seed_user, NOW(), seed_user, false),
(org_id, teryl_tm_id, 'Kubernetes Administration', 'Get CKA certified', 0, '2026-06-30', 0, 0, '', brian_user_id, NOW(), seed_user, NOW(), seed_user, false),
(org_id, pat_tm_id, 'Test Automation Leadership', 'Lead the test automation framework initiative', 1, '2026-03-31', 0, 60, '', brian_user_id, NOW(), seed_user, NOW(), seed_user, false),
(org_id, karl_tm_id, 'React Native Expertise', 'Complete React Native course', 1, '2026-04-30', 0, 40, '', brian_user_id, NOW(), seed_user, NOW(), seed_user, false),
(org_id, grady_tm_id, 'Security Champion', 'Complete OWASP training', 1, '2026-03-31', 0, 55, '', brian_user_id, NOW(), seed_user, NOW(), seed_user, false);

RAISE NOTICE 'Created 6 Individual Goals';

-- =============================================================================
-- Quick Notes
-- Category and LinkedEntityType are integers (enums)
-- =============================================================================
INSERT INTO "QuickNotes" (
    "OrganizationId", "Title", "Content", "Category", "LinkedEntityType", "Tags", "IsPinned", "IsArchived",
    "UserId",
    "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy", "IsDeleted"
)
VALUES
(org_id, 'Q1 2026 Priorities', E'Focus areas:\n1. Tracker 2.0 release\n2. Security remediation\n3. Team growth', 0, 0, 'planning,q1,priorities', true, false, brian_user_id, NOW(), seed_user, NOW(), seed_user, false),
(org_id, 'Interview Questions', E'Key questions for senior engineer candidates', 0, 0, 'hiring,interviews', false, false, brian_user_id, NOW(), seed_user, NOW(), seed_user, false),
(org_id, 'Team Retro Ideas', E'Topics for next retrospective', 0, 0, 'retro,team', false, false, brian_user_id, NOW(), seed_user, NOW(), seed_user, false);

RAISE NOTICE 'Created 3 Quick Notes';

-- =============================================================================
-- Kudos
-- TeamMemberId = recipient, UserId = sender/owner
-- Category and DeliveryChannel/Status are strings, NOT integers
-- =============================================================================
INSERT INTO "Kudos" (
    "OrganizationId", "TeamMemberId", "Message", "Category", 
    "DeliveryChannel", "DeliveryStatus", "IsPublic", "MentionInMeetingPrep",
    "UserId",
    "CreatedAt", "CreatedBy", "LastModifiedAt", "LastModifiedBy", "IsDeleted"
)
VALUES
(org_id, mike_tm_id, 'Amazing work leading the PostgreSQL migration!', 'Leadership', '', '', true, false, brian_user_id, NOW(), seed_user, NOW(), seed_user, false),
(org_id, grady_tm_id, 'Quick turnaround on the security fix.', 'Problem Solving', '', '', true, false, brian_user_id, NOW(), seed_user, NOW(), seed_user, false),
(org_id, teryl_tm_id, 'The API gateway documentation is excellent.', 'Collaboration', '', '', true, false, brian_user_id, NOW(), seed_user, NOW(), seed_user, false),
(org_id, pat_tm_id, 'Great catch on that regression bug!', 'Quality', '', '', true, false, brian_user_id, NOW(), seed_user, NOW(), seed_user, false),
(org_id, karl_tm_id, 'The mobile wireframes look fantastic.', 'Innovation', '', '', true, false, brian_user_id, NOW(), seed_user, NOW(), seed_user, false);

RAISE NOTICE 'Created 5 Kudos';

-- =============================================================================
-- Summary
-- =============================================================================
RAISE NOTICE 'Seed complete!';
RAISE NOTICE 'Organization ID: %', org_id;

END $$;

-- Output summary counts
SELECT 'Seed Summary' as info;
SELECT 'Organizations: ' || COUNT(*) FROM "Organization";
SELECT 'Users: ' || COUNT(*) FROM "Users";
SELECT 'Team Members: ' || COUNT(*) FROM "TeamMembers";
SELECT 'Projects: ' || COUNT(*) FROM "Projects";
SELECT 'Tasks: ' || COUNT(*) FROM "Tasks";
SELECT 'One-on-Ones: ' || COUNT(*) FROM "OneOnOnes";
SELECT 'Feedback: ' || COUNT(*) FROM "Feedbacks";
SELECT 'Goals: ' || COUNT(*) FROM "IndividualGoals";
SELECT 'Quick Notes: ' || COUNT(*) FROM "QuickNotes";
SELECT 'Kudos: ' || COUNT(*) FROM "Kudos";
