-- ============================================================================
-- TRACKER SAMPLE DATA SEED SCRIPT FOR POSTGRESQL
-- ============================================================================
-- Purpose: Seeds realistic sample data for development and testing
-- Target: PostgreSQL 18+ with tracker database (public schema)
-- Usage:  psql -h localhost -U tracker_app -d tracker -f seed_sample_data.sql
--         OR run from pgAdmin/DataGrip
--
-- Story: "Q1 2025 Engineering Team"
--   - A manager (you) overseeing 6 engineers with diverse specialties
--   - Tasks with various statuses and priorities
--   - Meetings with notes
--   - Kudos for recognition
--
-- Note: This seeds the simplified spike schema (public.users, public.team_members, etc.)
--       For full schema deployment, run 00_MasterDeploy.sql first
-- ============================================================================

-- Run as tracker_app user
SET ROLE tracker_app;

-- ============================================================================
-- CLEAR EXISTING DATA (respecting RLS)
-- ============================================================================
-- Clear data for each existing user by setting their context
DO $$
DECLARE
    r RECORD;
BEGIN
    -- Loop through all users and clear their data
    FOR r IN SELECT id FROM users LOOP
        PERFORM set_config('app.current_user_id', r.id::text, true);
        DELETE FROM kudos WHERE owner_id = r.id;
        DELETE FROM tasks WHERE owner_id = r.id;
        DELETE FROM meetings WHERE owner_id = r.id;
        DELETE FROM team_members WHERE owner_id = r.id;
    END LOOP;
    -- Now we can clear users
    DELETE FROM users;
END $$;

-- ============================================================================
-- SEED DATA
-- ============================================================================
DO $$
DECLARE
    v_user_id UUID;
    v_user_email TEXT := 'brian@pricklycactussoftware.com';
    
    -- Team member IDs (for cross-references)
    v_tm_sarah UUID;
    v_tm_mike UUID;
    v_tm_emily UUID;
    v_tm_james UUID;
    v_tm_lisa UUID;
    v_tm_david UUID;
BEGIN
    -- ========================================================================
    -- STEP 1: Create User (the manager/owner)
    -- ========================================================================
    RAISE NOTICE 'Creating user...';
    
    v_user_id := gen_random_uuid();
    
    -- Password: $teelers4Ever (BCrypt hash, 12 rounds)
    INSERT INTO users (id, email, display_name, password_hash)
    VALUES (v_user_id, v_user_email, 'Brian', '$2a$12$RiU0iEugjA8FTb1sowNEqeSgB9W05j09DvwuJg764e3T8Sy5QydtK');
    
    -- Set session context for RLS
    PERFORM set_config('app.current_user_id', v_user_id::text, true);
    
    -- ========================================================================
    -- STEP 2: Create Team Members
    -- ========================================================================
    RAISE NOTICE 'Creating team members...';
    
    -- Sarah Chen - Senior Frontend Developer
    v_tm_sarah := gen_random_uuid();
    INSERT INTO team_members (id, owner_id, name, email, role)
    VALUES (v_tm_sarah, v_user_id, 'Sarah Chen', 'sarah.chen@techcorp.com', 'Senior Frontend Developer');

    -- Mike Rodriguez - Backend Developer
    v_tm_mike := gen_random_uuid();
    INSERT INTO team_members (id, owner_id, name, email, role)
    VALUES (v_tm_mike, v_user_id, 'Mike Rodriguez', 'mike.rodriguez@techcorp.com', 'Backend Developer');

    -- Emily Watson - Full Stack Developer
    v_tm_emily := gen_random_uuid();
    INSERT INTO team_members (id, owner_id, name, email, role)
    VALUES (v_tm_emily, v_user_id, 'Emily Watson', 'emily.watson@techcorp.com', 'Full Stack Developer');

    -- James Park - DevOps Engineer
    v_tm_james := gen_random_uuid();
    INSERT INTO team_members (id, owner_id, name, email, role)
    VALUES (v_tm_james, v_user_id, 'James Park', 'james.park@techcorp.com', 'DevOps Engineer');

    -- Lisa Thompson - Junior Developer
    v_tm_lisa := gen_random_uuid();
    INSERT INTO team_members (id, owner_id, name, email, role)
    VALUES (v_tm_lisa, v_user_id, 'Lisa Thompson', 'lisa.thompson@techcorp.com', 'Junior Developer');

    -- David Kim - QA Engineer
    v_tm_david := gen_random_uuid();
    INSERT INTO team_members (id, owner_id, name, email, role)
    VALUES (v_tm_david, v_user_id, 'David Kim', 'david.kim@techcorp.com', 'QA Engineer');

    -- ========================================================================
    -- STEP 3: Create Tasks
    -- ========================================================================
    RAISE NOTICE 'Creating tasks...';
    
    -- Mobile App Tasks
    INSERT INTO tasks (owner_id, team_member_id, title, description, status, priority, due_date) VALUES
        (v_user_id, v_tm_sarah, 'Implement new navigation component', 'Create bottom navigation with animations for mobile app redesign', 'completed', 'high', '2025-01-15'),
        (v_user_id, v_tm_sarah, 'Design system tokens migration', 'Update all components to use new design tokens', 'in_progress', 'high', '2025-01-25'),
        (v_user_id, v_tm_lisa, 'User profile screen redesign', 'Implement new profile layout with settings panel', 'in_progress', 'medium', '2025-01-30'),
        (v_user_id, v_tm_david, 'Mobile UI test automation', 'Set up Detox tests for critical user flows', 'pending', 'medium', '2025-02-10'),
        (v_user_id, v_tm_sarah, 'Performance optimization', 'Reduce app startup time by 40%', 'pending', 'high', '2025-02-28');
    
    -- API v2 Tasks
    INSERT INTO tasks (owner_id, team_member_id, title, description, status, priority, due_date) VALUES
        (v_user_id, v_tm_mike, 'Design new auth flow', 'Implement OAuth 2.0 with PKCE for API v2', 'completed', 'high', '2025-01-20'),
        (v_user_id, v_tm_mike, 'Users endpoint migration', 'Migrate /users to v2 with breaking changes documented', 'in_progress', 'high', '2025-01-28'),
        (v_user_id, v_tm_emily, 'GraphQL schema design', 'Design schema with federation support for microservices', 'in_progress', 'high', '2025-02-05'),
        (v_user_id, v_tm_james, 'API gateway setup', 'Configure Kong gateway for v2 routing and rate limiting', 'pending', 'medium', '2025-02-15'),
        (v_user_id, v_tm_david, 'API contract tests', 'Create Pact tests for all v2 endpoints', 'pending', 'medium', '2025-02-20');
    
    -- Analytics Dashboard Tasks
    INSERT INTO tasks (owner_id, team_member_id, title, description, status, priority, due_date) VALUES
        (v_user_id, v_tm_emily, 'Requirements gathering', 'Interview stakeholders for analytics dashboard needs', 'completed', 'high', '2025-01-10'),
        (v_user_id, v_tm_emily, 'Data model design', 'Design star schema for analytics warehouse', 'in_progress', 'high', '2025-02-01'),
        (v_user_id, v_tm_lisa, 'Widget library research', 'Evaluate charting libraries (Victory, Recharts, D3)', 'pending', 'medium', '2025-02-10');
    
    -- DevOps Tasks
    INSERT INTO tasks (owner_id, team_member_id, title, description, status, priority, due_date) VALUES
        (v_user_id, v_tm_james, 'Upgrade CI pipeline', 'Migrate from Jenkins to GitHub Actions', 'in_progress', 'medium', '2025-01-31'),
        (v_user_id, v_tm_james, 'Terraform modules cleanup', 'Refactor infrastructure modules for reusability', 'pending', 'low', '2025-02-15'),
        (v_user_id, v_tm_david, 'Test coverage report', 'Generate quarterly test coverage metrics', 'completed', 'medium', '2025-01-05');

    -- ========================================================================
    -- STEP 4: Create Meetings (1:1s)
    -- ========================================================================
    RAISE NOTICE 'Creating meetings...';
    
    -- Sarah's 1:1s
    INSERT INTO meetings (owner_id, team_member_id, title, meeting_date, duration_minutes, status, notes) VALUES
        (v_user_id, v_tm_sarah, 'Weekly 1:1 - Sarah', '2025-01-06 10:00', 30, 'completed', 
         'Great progress on the design system migration. Sarah identified potential performance issues with the new animation library - need to investigate alternatives. She''s interested in presenting at the next tech talk.'),
        (v_user_id, v_tm_sarah, 'Weekly 1:1 - Sarah', '2025-01-13 10:00', 30, 'scheduled', NULL);
    
    -- Mike's 1:1s
    INSERT INTO meetings (owner_id, team_member_id, title, meeting_date, duration_minutes, status, notes) VALUES
        (v_user_id, v_tm_mike, 'Weekly 1:1 - Mike', '2025-01-07 14:00', 30, 'completed',
         'API v2 schema is looking solid. Discussed authentication edge cases. Mike wants to explore GraphQL subscriptions for real-time features. Action: Schedule architecture review with team.'),
        (v_user_id, v_tm_mike, 'Weekly 1:1 - Mike', '2025-01-14 14:00', 30, 'scheduled', NULL);
    
    -- Emily's 1:1s
    INSERT INTO meetings (owner_id, team_member_id, title, meeting_date, duration_minutes, status, notes) VALUES
        (v_user_id, v_tm_emily, 'Career Check-in - Emily', '2025-01-08 11:00', 45, 'completed',
         'Career conversation - Emily is interested in tech lead track. Discussed what that looks like and set up shadow opportunities with Sarah on architecture decisions.'),
        (v_user_id, v_tm_emily, 'Weekly 1:1 - Emily', '2025-01-15 11:00', 30, 'scheduled', NULL);
    
    -- Lisa's 1:1s (more frequent for junior)
    INSERT INTO meetings (owner_id, team_member_id, title, meeting_date, duration_minutes, status, notes) VALUES
        (v_user_id, v_tm_lisa, 'Weekly 1:1 - Lisa', '2025-01-03 15:00', 45, 'completed',
         'Good progress for first month! Lisa is picking up React patterns quickly. Paired on debugging session which was helpful. Next: assign more independent tickets to build confidence.'),
        (v_user_id, v_tm_lisa, 'Weekly 1:1 - Lisa', '2025-01-10 15:00', 30, 'completed',
         'Reviewed her first production PR - clean code and good test coverage. Discussed testing strategies and when to ask for help vs. trying to figure it out.'),
        (v_user_id, v_tm_lisa, 'Weekly 1:1 - Lisa', '2025-01-17 15:00', 30, 'scheduled', NULL);
    
    -- James's 1:1s
    INSERT INTO meetings (owner_id, team_member_id, title, meeting_date, duration_minutes, status, notes) VALUES
        (v_user_id, v_tm_james, 'Weekly 1:1 - James', '2025-01-02 09:00', 30, 'completed',
         'CI migration going well - 60% faster build times already. Discussed on-call rotation improvements and potential Kubernetes upgrade path.'),
        (v_user_id, v_tm_james, 'Weekly 1:1 - James', '2025-01-09 09:00', 30, 'scheduled', NULL);
    
    -- David's 1:1s
    INSERT INTO meetings (owner_id, team_member_id, title, meeting_date, duration_minutes, status, notes) VALUES
        (v_user_id, v_tm_david, 'Weekly 1:1 - David', '2025-01-03 13:00', 30, 'completed',
         'Quarterly test coverage report looks good - up to 68% from 55%. Discussed E2E testing strategy and Playwright evaluation.'),
        (v_user_id, v_tm_david, 'Weekly 1:1 - David', '2025-01-10 13:00', 30, 'scheduled', NULL);

    -- ========================================================================
    -- STEP 5: Create Kudos
    -- ========================================================================
    RAISE NOTICE 'Creating kudos...';
    
    INSERT INTO kudos (owner_id, team_member_id, message, category, created_at) VALUES
        (v_user_id, v_tm_sarah, 'Excellent work leading the design system initiative! Your documentation made onboarding new team members much easier.', 'leadership', '2025-01-03 14:30'),
        (v_user_id, v_tm_mike, 'Great job on the OAuth implementation - clean code, well-tested, and delivered ahead of schedule!', 'technical', '2025-01-05 10:15'),
        (v_user_id, v_tm_lisa, 'Congratulations on your first production PR! Clean code and good test coverage. Keep it up!', 'growth', '2025-01-02 16:00'),
        (v_user_id, v_tm_james, 'The new CI pipeline reduced build times by 60%. This has been a huge productivity boost for the whole team!', 'impact', '2025-01-04 11:00'),
        (v_user_id, v_tm_emily, 'Thanks for stepping up to help with the API documentation. Great collaboration with the product team!', 'teamwork', '2025-01-06 09:30'),
        (v_user_id, v_tm_david, 'Your thorough testing caught 3 critical bugs before release. Great attention to detail!', 'quality', '2025-01-05 15:45');

    -- ========================================================================
    -- COMPLETE
    -- ========================================================================
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Seed complete! Created:';
    RAISE NOTICE '  - 1 user: %', v_user_email;
    RAISE NOTICE '  - 6 team members';
    RAISE NOTICE '  - 16 tasks (various statuses)';
    RAISE NOTICE '  - 12 meetings (1:1s with notes)';
    RAISE NOTICE '  - 6 kudos';
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Login with: %', v_user_email;
    
END $$;

-- ============================================================================
-- VERIFICATION QUERIES
-- ============================================================================
-- Set context for seeded user to see data through RLS
SELECT set_config('app.current_user_id', 
    (SELECT id::text FROM users WHERE email = 'brian@pricklycactussoftware.com'), 
    false);

SELECT 'Summary' as info, '' as detail
UNION ALL
SELECT 'Users', COUNT(*)::text FROM users
UNION ALL SELECT 'Team Members', COUNT(*)::text FROM team_members
UNION ALL SELECT 'Tasks', COUNT(*)::text FROM tasks
UNION ALL SELECT 'Meetings', COUNT(*)::text FROM meetings
UNION ALL SELECT 'Kudos', COUNT(*)::text FROM kudos;
