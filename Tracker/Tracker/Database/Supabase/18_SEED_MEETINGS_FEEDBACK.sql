-- ============================================================================
-- TRACKER DATABASE - SEED DATA: SAMPLE MEETINGS AND FEEDBACK
-- ============================================================================
-- Creates test meetings, feedback, and recognition for Prickly Cactus Software
--
-- References from 15_SEED_TEST_USERS.sql:
--   Organization: '11111111-1111-1111-1111-111111111111'
--   Teams:
--     Engineering: '00000000-0000-2000-0000-000000000001'
--     Product:     '00000000-0000-2000-0000-000000000002'
--   Users:
--     Sarah:   'a0000000-0000-0000-0000-000000000001'
--     Emily:   'a0000000-0000-0000-0000-000000000003'
--     David:   'a0000000-0000-0000-0000-000000000004'
--     Jessica: 'a0000000-0000-0000-0000-000000000005'
--     Alex:    'a0000000-0000-0000-0000-000000000006'
--     Rachel:  'a0000000-0000-0000-0000-000000000007'
--     Michael: 'a0000000-0000-0000-0000-000000000008'
--   Team Members:
--     Emily:   '00000000-0000-1000-0000-000000000003'
--     David:   '00000000-0000-1000-0000-000000000004'
--     Jessica: '00000000-0000-1000-0000-000000000005'
--     Alex:    '00000000-0000-1000-0000-000000000006'
--     Rachel:  '00000000-0000-1000-0000-000000000007'
--     Michael: '00000000-0000-1000-0000-000000000008'
-- ============================================================================

-- ============================================================================
-- CLEAN UP FOR RE-RUNS
-- ============================================================================
DELETE FROM action_items WHERE meeting_id IN (
    SELECT id FROM meetings WHERE organization_id = '11111111-1111-1111-1111-111111111111'
);
DELETE FROM meeting_notes WHERE meeting_id IN (
    SELECT id FROM meetings WHERE organization_id = '11111111-1111-1111-1111-111111111111'
);
DELETE FROM talking_points WHERE manager_team_member_id IN (
    SELECT id FROM team_members WHERE organization_id = '11111111-1111-1111-1111-111111111111'
);
DELETE FROM recognition WHERE organization_id = '11111111-1111-1111-1111-111111111111';
DELETE FROM feedback WHERE organization_id = '11111111-1111-1111-1111-111111111111';
DELETE FROM meetings WHERE organization_id = '11111111-1111-1111-1111-111111111111';

-- ============================================================================
-- CREATE SAMPLE MEETINGS
-- ============================================================================

INSERT INTO meetings (id, organization_id, created_by_user_id, meeting_type,
    manager_team_member_id, report_team_member_id, team_id,
    title, description, scheduled_at, duration_minutes, location, status)
VALUES
    -- 1:1 Meeting: Emily <-> David
    ('00000000-0000-8000-0000-000000000001',
     '11111111-1111-1111-1111-111111111111',
     'a0000000-0000-0000-0000-000000000003',  -- Emily
     'one_on_one',
     '00000000-0000-1000-0000-000000000003',  -- Emily
     '00000000-0000-1000-0000-000000000004',  -- David
     NULL,
     'Weekly 1:1',
     'Regular weekly sync to discuss progress and blockers',
     '2025-02-05 10:00:00+00',
     30,
     'Zoom',
     'completed'),
    
    -- 1:1 Meeting: David <-> Jessica
    ('00000000-0000-8000-0000-000000000002',
     '11111111-1111-1111-1111-111111111111',
     'a0000000-0000-0000-0000-000000000004',  -- David
     'one_on_one',
     '00000000-0000-1000-0000-000000000004',  -- David
     '00000000-0000-1000-0000-000000000005',  -- Jessica
     NULL,
     'Weekly 1:1',
     'Regular sync with Jessica',
     '2025-02-06 14:00:00+00',
     30,
     'Room 301',
     'completed'),
    
    -- 1:1 Meeting: David <-> Alex
    ('00000000-0000-8000-0000-000000000003',
     '11111111-1111-1111-1111-111111111111',
     'a0000000-0000-0000-0000-000000000004',  -- David
     'one_on_one',
     '00000000-0000-1000-0000-000000000004',  -- David
     '00000000-0000-1000-0000-000000000006',  -- Alex
     NULL,
     'Weekly 1:1',
     'Regular sync with Alex',
     '2025-02-06 15:00:00+00',
     30,
     'Room 301',
     'scheduled'),
    
    -- Team Meeting: Engineering
    ('00000000-0000-8000-0000-000000000004',
     '11111111-1111-1111-1111-111111111111',
     'a0000000-0000-0000-0000-000000000003',  -- Emily
     'team_meeting',
     NULL,
     NULL,
     '00000000-0000-2000-0000-000000000001',  -- Engineering team
     'Engineering Weekly Standup',
     'Weekly team standup to sync on sprint progress',
     '2025-02-05 09:00:00+00',
     45,
     'Engineering Room',
     'completed'),
    
    -- All Hands
    ('00000000-0000-8000-0000-000000000005',
     '11111111-1111-1111-1111-111111111111',
     'a0000000-0000-0000-0000-000000000001',  -- Sarah
     'all_hands',
     NULL,
     NULL,
     NULL,
     'Monthly All Hands',
     'Company-wide update and Q&A',
     '2025-02-01 16:00:00+00',
     60,
     'Main Conference Room + Zoom',
     'completed');

-- ============================================================================
-- CREATE MEETING NOTES
-- ============================================================================

INSERT INTO meeting_notes (meeting_id, author_team_member_id, content, is_private) VALUES
    -- Emily-David 1:1 notes
    ('00000000-0000-8000-0000-000000000001', '00000000-0000-1000-0000-000000000003',
     E'# 1:1 Notes - Feb 5, 2025\n\n## Topics Discussed\n- Offline sync progress: on track, 75% complete\n- David raised concerns about testing complexity\n- Discussed potential promotion timeline\n\n## Action Items\n- [ ] David to document sync architecture\n- [ ] Emily to review testing resources\n\n## Career Development\n- David interested in leading platform team expansion\n- Will discuss at next review cycle',
     false),
    
    -- David-Jessica 1:1 notes
    ('00000000-0000-8000-0000-000000000002', '00000000-0000-1000-0000-000000000004',
     E'# 1:1 Notes - Feb 6, 2025\n\n## Topics Discussed\n- Auth work completed ahead of schedule\n- Jessica wants to take on more complex features\n- Discussed GraphQL migration involvement\n\n## Action Items\n- [ ] Jessica to shadow David on sync work\n- [ ] Review GraphQL schema together',
     false),
    
    -- Team meeting notes
    ('00000000-0000-8000-0000-000000000004', '00000000-0000-1000-0000-000000000003',
     E'# Engineering Weekly - Feb 5, 2025\n\n## Sprint Progress\n- Velocity: 48 points (target: 50)\n- Completed: Auth flow, data model design\n- In Progress: Conflict resolution, dashboard\n\n## Blockers\n- Waiting on design for notification preferences\n\n## Announcements\n- New CI/CD pipeline goes live next week\n- Code freeze for beta: Feb 25',
     false);

-- ============================================================================
-- CREATE TALKING POINTS (for 1:1s)
-- ============================================================================

INSERT INTO talking_points (manager_team_member_id, report_team_member_id, added_by_team_member_id,
    title, notes, category, is_recurring, is_active) VALUES
    -- David's talking points with Jessica
    ('00000000-0000-1000-0000-000000000004', '00000000-0000-1000-0000-000000000005', '00000000-0000-1000-0000-000000000004',
     'Career goals check-in', 'Discuss progress on senior engineer path', 'career', true, true),
    ('00000000-0000-1000-0000-000000000004', '00000000-0000-1000-0000-000000000005', '00000000-0000-1000-0000-000000000005',
     'GraphQL learning', 'Want to discuss opportunities to work on GraphQL migration', 'project', false, true),
    
    -- David's talking points with Alex
    ('00000000-0000-1000-0000-000000000004', '00000000-0000-1000-0000-000000000006', '00000000-0000-1000-0000-000000000004',
     'Onboarding progress', 'How is the ramp-up going?', 'feedback', true, true),
    ('00000000-0000-1000-0000-000000000004', '00000000-0000-1000-0000-000000000006', '00000000-0000-1000-0000-000000000006',
     'Testing best practices', 'Would like guidance on testing strategies', 'project', false, true);

-- ============================================================================
-- CREATE ACTION ITEMS
-- ============================================================================

INSERT INTO action_items (meeting_id, assignee_team_member_id, title, due_date, is_completed, completed_at) VALUES
    ('00000000-0000-8000-0000-000000000001', '00000000-0000-1000-0000-000000000004', 
     'Document sync architecture', '2025-02-12', false, NULL),
    ('00000000-0000-8000-0000-000000000001', '00000000-0000-1000-0000-000000000003',
     'Review testing resources', '2025-02-09', true, '2025-02-08 11:00:00+00'),
    ('00000000-0000-8000-0000-000000000002', '00000000-0000-1000-0000-000000000005',
     'Shadow David on sync work', '2025-02-13', false, NULL),
    ('00000000-0000-8000-0000-000000000004', '00000000-0000-1000-0000-000000000008',
     'Complete notification preferences design', '2025-02-07', true, '2025-02-06 16:00:00+00');

-- ============================================================================
-- CREATE SAMPLE FEEDBACK
-- ============================================================================

INSERT INTO feedback (id, organization_id, from_team_member_id, to_team_member_id,
    feedback_type, sentiment, content, is_private, is_acknowledged, acknowledged_at)
VALUES
    -- Positive feedback for Jessica from David
    ('00000000-0000-9000-0000-000000000001',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000004',  -- David
     '00000000-0000-1000-0000-000000000005',  -- Jessica
     'praise',
     'positive',
     'Jessica did an outstanding job on the authentication implementation. She completed it ahead of schedule and the code quality was excellent. Her documentation was thorough and made the handoff to QA seamless.',
     false,
     true,
     '2025-02-02 10:00:00+00'),
    
    -- Constructive feedback for Alex from David
    ('00000000-0000-9000-0000-000000000002',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000004',  -- David
     '00000000-0000-1000-0000-000000000006',  -- Alex
     'coaching',
     'constructive',
     'Alex is doing great work on the dashboard feature. One area for improvement: I''d encourage more thorough testing before submitting PRs. A few bugs slipped through that could have been caught with better unit tests. Happy to pair on testing strategies.',
     false,
     true,
     '2025-02-04 14:00:00+00'),
    
    -- Peer feedback from Rachel to David
    ('00000000-0000-9000-0000-000000000003',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000007',  -- Rachel
     '00000000-0000-1000-0000-000000000004',  -- David
     'collaboration',
     'positive',
     'David has been incredibly collaborative on the mobile project. He takes the time to explain technical constraints in a way that helps product make better decisions. Great partner to work with!',
     false,
     false,
     NULL),
    
    -- Private manager note from Emily about David
    ('00000000-0000-9000-0000-000000000004',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000003',  -- Emily
     '00000000-0000-1000-0000-000000000004',  -- David
     'general',
     'positive',
     'David is ready for promotion to Senior Team Lead. He consistently mentors his team members effectively and takes ownership of complex technical challenges. Will recommend in next review cycle.',
     true,  -- Private feedback
     false,
     NULL);

-- ============================================================================
-- CREATE SAMPLE RECOGNITION
-- ============================================================================

INSERT INTO recognition (organization_id, from_team_member_id, to_team_member_id,
    title, message, badge_type, project_id, is_public, reactions_count)
VALUES
    -- Kudos for Jessica
    ('11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000004',  -- From David
     '00000000-0000-1000-0000-000000000005',  -- To Jessica
     'Auth Feature Champion',
     'Huge shoutout to Jessica for delivering the authentication feature ahead of schedule! Her attention to security details and clean code made the review process smooth.',
     'innovator',
     '00000000-0000-6000-0000-000000000001',  -- Mobile project
     true,
     5),
    
    -- Kudos for Alex
    ('11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000005',  -- From Jessica
     '00000000-0000-1000-0000-000000000006',  -- To Alex
     'Great Team Player',
     'Alex jumped in to help debug a tricky issue in the dashboard even though it was not on his plate. Really appreciate the collaborative spirit!',
     'team_player',
     '00000000-0000-6000-0000-000000000001',
     true,
     3);

SELECT 'Sample meetings and feedback created successfully' AS status;

-- Show meetings summary
SELECT 
    m.title,
    m.meeting_type,
    m.status,
    m.scheduled_at::date as meeting_date
FROM meetings m
WHERE m.organization_id = '11111111-1111-1111-1111-111111111111'
ORDER BY m.scheduled_at;

-- Show feedback summary
SELECT 
    f.feedback_type,
    f.sentiment,
    from_tm.first_name || ' ' || from_tm.last_name as from_person,
    to_tm.first_name || ' ' || to_tm.last_name as to_person
FROM feedback f
JOIN team_members from_tm ON from_tm.id = f.from_team_member_id
JOIN team_members to_tm ON to_tm.id = f.to_team_member_id
WHERE f.organization_id = '11111111-1111-1111-1111-111111111111'
ORDER BY f.created_at;
