-- ============================================================================
-- TRACKER DATABASE - SEED DATA: SAMPLE TASKS AND PROJECTS
-- ============================================================================
-- Creates test projects and tasks for Prickly Cactus Software
--
-- References from 15_SEED_TEST_USERS.sql:
--   Organization: '11111111-1111-1111-1111-111111111111'
--   Teams:
--     Engineering: '00000000-0000-2000-0000-000000000001'
--     Product:     '00000000-0000-2000-0000-000000000002'
--   Users:
--     Marcus:  'a0000000-0000-0000-0000-000000000002'
--     Emily:   'a0000000-0000-0000-0000-000000000003'
--     David:   'a0000000-0000-0000-0000-000000000004'
--     Jessica: 'a0000000-0000-0000-0000-000000000005'
--     Alex:    'a0000000-0000-0000-0000-000000000006'
--     Rachel:  'a0000000-0000-0000-0000-000000000007'
--     Michael: 'a0000000-0000-0000-0000-000000000008'
--   Team Members:
--     Marcus:  '00000000-0000-1000-0000-000000000002'
--     Emily:   '00000000-0000-1000-0000-000000000003'
--     David:   '00000000-0000-1000-0000-000000000004'
--     Jessica: '00000000-0000-1000-0000-000000000005'
--     Alex:    '00000000-0000-1000-0000-000000000006'
--     Rachel:  '00000000-0000-1000-0000-000000000007'
--     Michael: '00000000-0000-1000-0000-000000000008'
--   Goals from 16_SEED_GOALS_METRICS.sql:
--     Sarah MVP Goal:    '00000000-0000-3000-0000-000000000001'
--     Marcus Eng Goal:   '00000000-0000-3000-0000-000000000002'
--     David Sync Goal:   '00000000-0000-3000-0000-000000000003'
--     Jessica Auth Goal: '00000000-0000-3000-0000-000000000004'
-- ============================================================================

-- ============================================================================
-- CLEAN UP FOR RE-RUNS
-- ============================================================================
DELETE FROM milestones WHERE project_id IN (
    SELECT id FROM projects WHERE organization_id = '11111111-1111-1111-1111-111111111111'
);
DELETE FROM project_members WHERE project_id IN (
    SELECT id FROM projects WHERE organization_id = '11111111-1111-1111-1111-111111111111'
);
DELETE FROM tasks WHERE organization_id = '11111111-1111-1111-1111-111111111111';
DELETE FROM projects WHERE organization_id = '11111111-1111-1111-1111-111111111111';

-- ============================================================================
-- CREATE SAMPLE PROJECTS
-- ============================================================================

INSERT INTO projects (id, organization_id, owner_team_member_id, created_by_user_id,
    name, description, color, start_date, target_end_date, status, progress_percent, priority)
VALUES
    -- Mobile App Project (owned by Marcus)
    ('00000000-0000-6000-0000-000000000001',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000002',
     'a0000000-0000-0000-0000-000000000002',
     'Mobile App MVP',
     'Build and launch the mobile application for iOS and Android',
     '#3B82F6',
     '2025-01-01',
     '2025-03-31',
     'in_progress',
     55.0,
     'high'),
    
    -- API Modernization Project (owned by Emily)
    ('00000000-0000-6000-0000-000000000002',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000003',
     'a0000000-0000-0000-0000-000000000003',
     'API Modernization',
     'Upgrade REST APIs to GraphQL and improve performance',
     '#10B981',
     '2025-02-01',
     '2025-04-30',
     'in_progress',
     25.0,
     'medium'),
    
    -- Design System Project (owned by Michael)
    ('00000000-0000-6000-0000-000000000003',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000008',
     'a0000000-0000-0000-0000-000000000008',
     'Design System 2.0',
     'Comprehensive design system refresh with dark mode support',
     '#8B5CF6',
     '2025-01-15',
     '2025-03-15',
     'in_progress',
     70.0,
     'medium');

-- ============================================================================
-- CREATE PROJECT MEMBERS
-- ============================================================================

INSERT INTO project_members (project_id, team_member_id, role) VALUES
    -- Mobile App team
    ('00000000-0000-6000-0000-000000000001', '00000000-0000-1000-0000-000000000002', 'owner'),       -- Marcus
    ('00000000-0000-6000-0000-000000000001', '00000000-0000-1000-0000-000000000004', 'contributor'), -- David
    ('00000000-0000-6000-0000-000000000001', '00000000-0000-1000-0000-000000000005', 'contributor'), -- Jessica
    ('00000000-0000-6000-0000-000000000001', '00000000-0000-1000-0000-000000000006', 'contributor'), -- Alex
    ('00000000-0000-6000-0000-000000000001', '00000000-0000-1000-0000-000000000007', 'reviewer'),    -- Rachel
    
    -- API Modernization team
    ('00000000-0000-6000-0000-000000000002', '00000000-0000-1000-0000-000000000003', 'owner'),       -- Emily
    ('00000000-0000-6000-0000-000000000002', '00000000-0000-1000-0000-000000000004', 'contributor'), -- David
    ('00000000-0000-6000-0000-000000000002', '00000000-0000-1000-0000-000000000005', 'contributor'), -- Jessica
    
    -- Design System team
    ('00000000-0000-6000-0000-000000000003', '00000000-0000-1000-0000-000000000008', 'owner'),       -- Michael
    ('00000000-0000-6000-0000-000000000003', '00000000-0000-1000-0000-000000000007', 'reviewer');    -- Rachel

-- ============================================================================
-- CREATE PROJECT MILESTONES
-- ============================================================================

INSERT INTO milestones (project_id, title, description, target_date, is_completed, completed_date, sort_order) VALUES
    -- Mobile App milestones
    ('00000000-0000-6000-0000-000000000001', 'Design Complete', 'All UI/UX designs finalized', '2025-01-31', true, '2025-01-28', 1),
    ('00000000-0000-6000-0000-000000000001', 'Alpha Release', 'Internal alpha version ready', '2025-02-15', true, '2025-02-14', 2),
    ('00000000-0000-6000-0000-000000000001', 'Beta Release', 'Public beta launch', '2025-03-01', false, NULL, 3),
    ('00000000-0000-6000-0000-000000000001', 'App Store Submission', 'Submit to iOS and Android stores', '2025-03-20', false, NULL, 4),
    
    -- API Modernization milestones
    ('00000000-0000-6000-0000-000000000002', 'Schema Design', 'GraphQL schema designed and approved', '2025-02-15', true, '2025-02-12', 1),
    ('00000000-0000-6000-0000-000000000002', 'Core Queries', 'All read operations migrated', '2025-03-15', false, NULL, 2),
    ('00000000-0000-6000-0000-000000000002', 'Mutations Complete', 'All write operations migrated', '2025-04-15', false, NULL, 3);

-- ============================================================================
-- CREATE SAMPLE TASKS
-- ============================================================================

INSERT INTO tasks (id, organization_id, owner_team_member_id, created_by_user_id,
    project_id, goal_id, title, description, status, priority, due_date, sort_order)
VALUES
    -- Mobile App tasks (linked to auth goal)
    ('00000000-0000-7000-0000-000000000001',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000005',  -- Jessica
     'a0000000-0000-0000-0000-000000000004',  -- David created
     '00000000-0000-6000-0000-000000000001',  -- Mobile project
     '00000000-0000-3000-0000-000000000004',  -- Jessica Auth Goal
     'Implement OAuth2 login flow',
     'Set up OAuth2 authentication with Google and Apple sign-in',
     'completed',
     'high',
     '2025-02-01',
     1),
    
    ('00000000-0000-7000-0000-000000000002',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000005',  -- Jessica
     'a0000000-0000-0000-0000-000000000004',  -- David created
     '00000000-0000-6000-0000-000000000001',
     '00000000-0000-3000-0000-000000000004',  -- Jessica Auth Goal
     'Add biometric authentication',
     'Implement Face ID and fingerprint login',
     'completed',
     'high',
     '2025-02-10',
     2),
    
    -- Sync tasks (linked to David's sync goal)
    ('00000000-0000-7000-0000-000000000003',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000004',  -- David
     'a0000000-0000-0000-0000-000000000004',  -- David created
     '00000000-0000-6000-0000-000000000001',
     '00000000-0000-3000-0000-000000000003',  -- David Sync Goal
     'Design offline data model',
     'Define SQLite schema for offline-first storage',
     'completed',
     'high',
     '2025-01-20',
     3),
    
    ('00000000-0000-7000-0000-000000000004',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000004',  -- David
     'a0000000-0000-0000-0000-000000000004',
     '00000000-0000-6000-0000-000000000001',
     '00000000-0000-3000-0000-000000000003',  -- David Sync Goal
     'Implement conflict resolution',
     'Build sync conflict detection and resolution logic',
     'in_progress',
     'high',
     '2025-02-28',
     4),
    
    -- General mobile tasks (Alex)
    ('00000000-0000-7000-0000-000000000005',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000006',  -- Alex
     'a0000000-0000-0000-0000-000000000004',  -- David created
     '00000000-0000-6000-0000-000000000001',
     NULL,
     'Build dashboard screen',
     'Implement the main dashboard with widgets',
     'in_progress',
     'medium',
     '2025-02-25',
     5),
    
    ('00000000-0000-7000-0000-000000000006',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000006',  -- Alex
     'a0000000-0000-0000-0000-000000000004',
     '00000000-0000-6000-0000-000000000001',
     NULL,
     'Add push notifications',
     'Integrate Firebase for push notifications',
     'not_started',
     'medium',
     '2025-03-10',
     6),
    
    -- API Modernization tasks
    ('00000000-0000-7000-0000-000000000007',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000005',  -- Jessica
     'a0000000-0000-0000-0000-000000000003',  -- Emily created
     '00000000-0000-6000-0000-000000000002',
     NULL,
     'Set up Apollo Server',
     'Configure Apollo Server with Express',
     'completed',
     'high',
     '2025-02-10',
     1),
    
    ('00000000-0000-7000-0000-000000000008',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000004',  -- David
     'a0000000-0000-0000-0000-000000000003',  -- Emily created
     '00000000-0000-6000-0000-000000000002',
     NULL,
     'Migrate user queries',
     'Convert user REST endpoints to GraphQL queries',
     'in_progress',
     'medium',
     '2025-03-01',
     2);

SELECT 'Sample projects and tasks created successfully' AS status;

-- Show projects summary
SELECT 
    p.name,
    p.status,
    p.progress_percent || '%' as progress,
    tm.first_name || ' ' || tm.last_name as owner
FROM projects p
JOIN team_members tm ON tm.id = p.owner_team_member_id
WHERE p.organization_id = '11111111-1111-1111-1111-111111111111'
ORDER BY p.start_date;

-- Show tasks summary
SELECT 
    t.title,
    t.status,
    t.priority,
    tm.first_name || ' ' || tm.last_name as assigned_to
FROM tasks t
JOIN team_members tm ON tm.id = t.owner_team_member_id
WHERE t.organization_id = '11111111-1111-1111-1111-111111111111'
ORDER BY t.sort_order;
