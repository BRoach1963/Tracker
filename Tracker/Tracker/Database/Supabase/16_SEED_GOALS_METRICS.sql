-- ============================================================================
-- TRACKER DATABASE - SEED DATA: SAMPLE GOALS AND METRICS
-- ============================================================================
-- Creates test goals and metrics for Prickly Cactus Software
--
-- References from 15_SEED_TEST_USERS.sql:
--   Organization: '11111111-1111-1111-1111-111111111111'
--   Users (created_by_user_id):
--     Brian:   'b0000000-0000-0000-0000-000000000000'
--     Sarah:   'a0000000-0000-0000-0000-000000000001'
--     Marcus:  'a0000000-0000-0000-0000-000000000002'
--     Emily:   'a0000000-0000-0000-0000-000000000003'
--     David:   'a0000000-0000-0000-0000-000000000004'
--     Jessica: 'a0000000-0000-0000-0000-000000000005'
--     Rachel:  'a0000000-0000-0000-0000-000000000007'
--   Team Members (owner_team_member_id):
--     Brian:   '00000000-0000-1000-0000-000000000000'
--     Sarah:   '00000000-0000-1000-0000-000000000001'
--     Marcus:  '00000000-0000-1000-0000-000000000002'
--     Emily:   '00000000-0000-1000-0000-000000000003'
--     David:   '00000000-0000-1000-0000-000000000004'
--     Jessica: '00000000-0000-1000-0000-000000000005'
--     Rachel:  '00000000-0000-1000-0000-000000000007'
-- ============================================================================

-- ============================================================================
-- CLEAN UP FOR RE-RUNS
-- ============================================================================
DELETE FROM metric_history WHERE metric_id IN (
    SELECT id FROM metrics WHERE organization_id = '11111111-1111-1111-1111-111111111111'
);
DELETE FROM targets WHERE goal_id IN (
    SELECT id FROM goals WHERE organization_id = '11111111-1111-1111-1111-111111111111'
);
DELETE FROM goals WHERE organization_id = '11111111-1111-1111-1111-111111111111';
DELETE FROM metrics WHERE organization_id = '11111111-1111-1111-1111-111111111111';

-- ============================================================================
-- CREATE SAMPLE GOALS
-- ============================================================================

-- Q1 2025 Company Goal - from Sarah (CEO)
INSERT INTO goals (
    id, organization_id, owner_team_member_id, created_by_user_id,
    title, description, time_period, year, start_date, end_date,
    status, progress_percent, is_team_visible, is_org_visible
) VALUES (
    '00000000-0000-3000-0000-000000000001',
    '11111111-1111-1111-1111-111111111111',
    '00000000-0000-1000-0000-000000000001',  -- Sarah (team member)
    'a0000000-0000-0000-0000-000000000001',  -- Sarah (user)
    'Launch Mobile App MVP',
    'Successfully launch the mobile application minimum viable product to iOS and Android app stores',
    'q1',
    2025,
    '2025-01-01',
    '2025-03-31',
    'on_track',
    45.0,
    true,
    true
);

-- Engineering Team Goal - from Marcus (VP Engineering)
INSERT INTO goals (
    id, organization_id, owner_team_member_id, created_by_user_id,
    title, description, time_period, year, start_date, end_date,
    status, progress_percent, is_team_visible
) VALUES (
    '00000000-0000-3000-0000-000000000002',
    '11111111-1111-1111-1111-111111111111',
    '00000000-0000-1000-0000-000000000002',  -- Marcus (team member)
    'a0000000-0000-0000-0000-000000000002',  -- Marcus (user)
    'Complete Core Mobile Features',
    'Build and test all core features for the mobile app MVP including auth, dashboard, and sync',
    'q1',
    2025,
    '2025-01-01',
    '2025-03-31',
    'on_track',
    60.0,
    true
);

-- Individual Goal - David (Team Lead)
INSERT INTO goals (
    id, organization_id, owner_team_member_id, created_by_user_id,
    title, description, time_period, year, start_date, end_date,
    status, progress_percent, is_team_visible
) VALUES (
    '00000000-0000-3000-0000-000000000003',
    '11111111-1111-1111-1111-111111111111',
    '00000000-0000-1000-0000-000000000004',  -- David (team member)
    'a0000000-0000-0000-0000-000000000004',  -- David (user)
    'Implement Offline Sync',
    'Design and implement the offline-first data sync architecture for mobile',
    'q1',
    2025,
    '2025-01-01',
    '2025-03-31',
    'on_track',
    75.0,
    true
);

-- Individual Goal - Jessica (Senior Developer)
INSERT INTO goals (
    id, organization_id, owner_team_member_id, created_by_user_id,
    title, description, time_period, year, start_date, end_date,
    status, progress_percent, is_team_visible
) VALUES (
    '00000000-0000-3000-0000-000000000004',
    '11111111-1111-1111-1111-111111111111',
    '00000000-0000-1000-0000-000000000005',  -- Jessica (team member)
    'a0000000-0000-0000-0000-000000000005',  -- Jessica (user)
    'Build Authentication Flow',
    'Implement secure authentication including biometric login for mobile',
    'q1',
    2025,
    '2025-01-01',
    '2025-03-31',
    'completed',
    100.0,
    true
);

-- ============================================================================
-- CREATE TARGETS (Key Results) FOR GOALS
-- ============================================================================

-- Targets for Company Goal (Mobile App MVP)
INSERT INTO targets (id, goal_id, title, description, target_value, current_value, starting_value, unit, weight, status, sort_order) VALUES
    ('00000000-0000-4000-0000-000000000001', 
     '00000000-0000-3000-0000-000000000001', 
     'App Store Submission', 
     'Submit to both iOS App Store and Google Play Store',
     2, 0, 0, 'submissions', 1.0, 'on_track', 1),
    
    ('00000000-0000-4000-0000-000000000002', 
     '00000000-0000-3000-0000-000000000001',
     'Beta User Signups', 
     'Get 500 beta users signed up before launch',
     500, 325, 0, 'users', 1.0, 'on_track', 2),
    
    ('00000000-0000-4000-0000-000000000003', 
     '00000000-0000-3000-0000-000000000001',
     'Crash-Free Rate', 
     'Maintain crash-free rate above 99%',
     99, 99.5, 95, '%', 1.0, 'on_track', 3),
    
    ('00000000-0000-4000-0000-000000000004', 
     '00000000-0000-3000-0000-000000000001',
     'Feature Completion', 
     'Complete all MVP features',
     100, 45, 0, '%', 1.0, 'on_track', 4);

-- Targets for Engineering Goal
INSERT INTO targets (id, goal_id, title, description, target_value, current_value, starting_value, unit, weight, status, sort_order) VALUES
    ('00000000-0000-4000-0000-000000000005', 
     '00000000-0000-3000-0000-000000000002',
     'API Endpoints', 
     'Implement all required API endpoints',
     25, 20, 0, 'endpoints', 1.0, 'on_track', 1),
    
    ('00000000-0000-4000-0000-000000000006', 
     '00000000-0000-3000-0000-000000000002',
     'Test Coverage', 
     'Achieve 80% test coverage on mobile codebase',
     80, 65, 30, '%', 1.0, 'on_track', 2),
    
    ('00000000-0000-4000-0000-000000000007', 
     '00000000-0000-3000-0000-000000000002',
     'Performance Target', 
     'App launch time under 2 seconds',
     2, 1.8, 4.5, 'seconds', 1.0, 'on_track', 3);

-- Targets for David's Goal (Offline Sync)
INSERT INTO targets (id, goal_id, title, description, target_value, current_value, starting_value, unit, weight, status, sort_order) VALUES
    ('00000000-0000-4000-0000-000000000008', 
     '00000000-0000-3000-0000-000000000003',
     'Sync Architecture Design', 
     'Complete and approve architecture document',
     1, 1, 0, 'document', 1.0, 'completed', 1),
    
    ('00000000-0000-4000-0000-000000000009', 
     '00000000-0000-3000-0000-000000000003',
     'Conflict Resolution', 
     'Implement conflict resolution for all entity types',
     8, 6, 0, 'entities', 1.0, 'on_track', 2);

-- Targets for Jessica's Goal (Auth Flow)
INSERT INTO targets (id, goal_id, title, description, target_value, current_value, starting_value, unit, weight, status, sort_order) VALUES
    ('00000000-0000-4000-0000-000000000010', 
     '00000000-0000-3000-0000-000000000004',
     'OAuth Providers', 
     'Integrate Microsoft and Google OAuth',
     2, 2, 0, 'providers', 1.0, 'completed', 1),
    
    ('00000000-0000-4000-0000-000000000011', 
     '00000000-0000-3000-0000-000000000004',
     'Biometric Login', 
     'Implement Face ID and fingerprint authentication',
     2, 2, 0, 'methods', 1.0, 'completed', 2);

-- ============================================================================
-- CREATE SAMPLE METRICS
-- ============================================================================

INSERT INTO metrics (
    id, organization_id, owner_team_member_id, created_by_user_id,
    name, description, category, current_value, target_value, baseline_value,
    unit, target_direction, frequency, is_team_visible
) VALUES
    -- Engineering Metrics (owned by Emily - Engineering Manager)
    ('00000000-0000-5000-0000-000000000001',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000003',  -- Emily
     'a0000000-0000-0000-0000-000000000003',
     'Sprint Velocity',
     'Story points completed per sprint',
     'Engineering',
     48, 50, 35,
     'points',
     'higher_is_better',
     'weekly',
     true),
    
    ('00000000-0000-5000-0000-000000000002',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000003',  -- Emily
     'a0000000-0000-0000-0000-000000000003',
     'Open Bugs',
     'Number of unresolved bugs in backlog',
     'Engineering',
     15, 10, 25,
     'count',
     'lower_is_better',
     'weekly',
     true),
    
    ('00000000-0000-5000-0000-000000000003',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000004',  -- David
     'a0000000-0000-0000-0000-000000000004',
     'Deployment Frequency',
     'Number of production deployments per week',
     'Engineering',
     4, 5, 2,
     'deployments',
     'higher_is_better',
     'weekly',
     true),
    
    -- Product Metrics (owned by Rachel - Product Manager)
    ('00000000-0000-5000-0000-000000000004',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000007',  -- Rachel
     'a0000000-0000-0000-0000-000000000007',
     'Net Promoter Score',
     'Customer satisfaction and loyalty score',
     'Product',
     42, 50, 30,
     'score',
     'higher_is_better',
     'monthly',
     true),
    
    ('00000000-0000-5000-0000-000000000005',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000007',  -- Rachel
     'a0000000-0000-0000-0000-000000000007',
     'Feature Adoption Rate',
     'Percentage of users using new features within 7 days',
     'Product',
     55, 60, 40,
     '%',
     'higher_is_better',
     'weekly',
     true);

-- ============================================================================
-- ADD METRIC HISTORY
-- ============================================================================

INSERT INTO metric_history (metric_id, recorded_at, value, notes, recorded_by_user_id) VALUES
    -- Sprint Velocity history
    ('00000000-0000-5000-0000-000000000001', '2025-01-07', 42, 'Sprint 1', 'a0000000-0000-0000-0000-000000000003'),
    ('00000000-0000-5000-0000-000000000001', '2025-01-14', 45, 'Sprint 2', 'a0000000-0000-0000-0000-000000000003'),
    ('00000000-0000-5000-0000-000000000001', '2025-01-21', 44, 'Sprint 3 - holidays', 'a0000000-0000-0000-0000-000000000003'),
    ('00000000-0000-5000-0000-000000000001', '2025-01-28', 48, 'Sprint 4', 'a0000000-0000-0000-0000-000000000003'),
    
    -- Open Bugs history
    ('00000000-0000-5000-0000-000000000002', '2025-01-07', 22, NULL, 'a0000000-0000-0000-0000-000000000003'),
    ('00000000-0000-5000-0000-000000000002', '2025-01-14', 20, NULL, 'a0000000-0000-0000-0000-000000000003'),
    ('00000000-0000-5000-0000-000000000002', '2025-01-21', 18, NULL, 'a0000000-0000-0000-0000-000000000003'),
    ('00000000-0000-5000-0000-000000000002', '2025-01-28', 15, 'Bug bash completed', 'a0000000-0000-0000-0000-000000000003'),
    
    -- NPS history
    ('00000000-0000-5000-0000-000000000004', '2024-12-01', 35, 'Q4 2024 baseline', 'a0000000-0000-0000-0000-000000000007'),
    ('00000000-0000-5000-0000-000000000004', '2025-01-01', 38, 'December survey', 'a0000000-0000-0000-0000-000000000007'),
    ('00000000-0000-5000-0000-000000000004', '2025-02-01', 42, 'January survey - improvement after updates', 'a0000000-0000-0000-0000-000000000007');

SELECT 'Sample goals and metrics created successfully' AS status;

-- Show goals summary
SELECT 
    g.title,
    g.status,
    g.progress_percent || '%' as progress,
    tm.first_name || ' ' || tm.last_name as owner
FROM goals g
JOIN team_members tm ON tm.id = g.owner_team_member_id
WHERE g.organization_id = '11111111-1111-1111-1111-111111111111'
ORDER BY g.created_at;

-- Show metrics summary
SELECT 
    m.name,
    m.current_value || ' ' || COALESCE(m.unit, '') as current,
    m.target_value || ' ' || COALESCE(m.unit, '') as target,
    tm.first_name || ' ' || tm.last_name as owner
FROM metrics m
JOIN team_members tm ON tm.id = m.owner_team_member_id
WHERE m.organization_id = '11111111-1111-1111-1111-111111111111'
ORDER BY m.category, m.name;
