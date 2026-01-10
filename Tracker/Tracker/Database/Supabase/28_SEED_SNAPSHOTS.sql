-- ============================================================================
-- TRACKER DATABASE - SEED DATA: PROGRESS SNAPSHOTS
-- ============================================================================
-- Historical snapshot data for analytics at Prickly Cactus Software
--
-- References:
--   Organization: '11111111-1111-1111-1111-111111111111'
--   Teams:
--     Engineering: '00000000-0000-2000-0000-000000000001'
--     Product:     '00000000-0000-2000-0000-000000000002'
--   Team Members: Emily, David, Jessica, Alex, Rachel, Michael
-- ============================================================================

-- ============================================================================
-- CLEAN UP FOR RE-RUNS
-- ============================================================================
DELETE FROM organization_snapshots WHERE organization_id = '11111111-1111-1111-1111-111111111111';
DELETE FROM team_snapshots WHERE organization_id = '11111111-1111-1111-1111-111111111111';
DELETE FROM team_member_snapshots WHERE organization_id = '11111111-1111-1111-1111-111111111111';
DELETE FROM progress_snapshots WHERE organization_id = '11111111-1111-1111-1111-111111111111';

-- ============================================================================
-- TEAM MEMBER SNAPSHOTS (Last 4 weeks for David)
-- ============================================================================
INSERT INTO team_member_snapshots (organization_id, team_member_id, snapshot_date,
    period_type, period_start, period_end, goals_total, goals_on_track, goals_at_risk,
    goals_completed, goal_progress_avg, tasks_total, tasks_completed, tasks_overdue,
    task_completion_rate, one_on_ones_held, meetings_attended, feedback_given, 
    feedback_received, recognition_given, recognition_received)
VALUES
    -- David - Week 1 (Jan 13-19)
    ('11111111-1111-1111-1111-111111111111', '00000000-0000-1000-0000-000000000004',
     '2025-01-19', 'weekly', '2025-01-13', '2025-01-19',
     3, 2, 1, 0, 45.00,
     8, 5, 1, 62.50,
     2, 4, 1, 0, 0, 1),
    
    -- David - Week 2 (Jan 20-26)
    ('11111111-1111-1111-1111-111111111111', '00000000-0000-1000-0000-000000000004',
     '2025-01-26', 'weekly', '2025-01-20', '2025-01-26',
     3, 2, 1, 0, 52.00,
     10, 7, 0, 70.00,
     2, 5, 2, 1, 1, 0),
    
    -- David - Week 3 (Jan 27 - Feb 2)
    ('11111111-1111-1111-1111-111111111111', '00000000-0000-1000-0000-000000000004',
     '2025-02-02', 'weekly', '2025-01-27', '2025-02-02',
     3, 3, 0, 0, 65.00,
     9, 8, 0, 88.89,
     2, 4, 1, 1, 1, 1),
    
    -- David - Week 4 (Feb 3-9)
    ('11111111-1111-1111-1111-111111111111', '00000000-0000-1000-0000-000000000004',
     '2025-02-09', 'weekly', '2025-02-03', '2025-02-09',
     3, 3, 0, 1, 75.00,
     7, 6, 0, 85.71,
     2, 5, 2, 0, 1, 0),
    
    -- Jessica - Week 4 (Feb 3-9)
    ('11111111-1111-1111-1111-111111111111', '00000000-0000-1000-0000-000000000005',
     '2025-02-09', 'weekly', '2025-02-03', '2025-02-09',
     2, 2, 0, 0, 60.00,
     5, 4, 0, 80.00,
     1, 4, 0, 2, 0, 1),
    
    -- Alex - Week 4 (Feb 3-9)
    ('11111111-1111-1111-1111-111111111111', '00000000-0000-1000-0000-000000000006',
     '2025-02-09', 'weekly', '2025-02-03', '2025-02-09',
     1, 1, 0, 0, 60.00,
     6, 4, 1, 66.67,
     1, 3, 0, 1, 0, 1);

-- ============================================================================
-- TEAM SNAPSHOTS (Engineering team)
-- ============================================================================
INSERT INTO team_snapshots (organization_id, team_id, snapshot_date, period_type,
    period_start, period_end, member_count, active_member_count, goals_total,
    goals_on_track, goals_completed, goal_completion_rate, tasks_total,
    tasks_completed, task_completion_rate, one_on_ones_completion_rate,
    team_meetings_held, feedback_exchanges, recognition_count)
VALUES
    -- Engineering - Week 1
    ('11111111-1111-1111-1111-111111111111', '00000000-0000-2000-0000-000000000001',
     '2025-01-19', 'weekly', '2025-01-13', '2025-01-19',
     4, 4, 8, 5, 0, 0.00,
     25, 15, 60.00, 75.00, 1, 3, 1),
    
    -- Engineering - Week 2
    ('11111111-1111-1111-1111-111111111111', '00000000-0000-2000-0000-000000000001',
     '2025-01-26', 'weekly', '2025-01-20', '2025-01-26',
     4, 4, 8, 6, 1, 12.50,
     28, 20, 71.43, 100.00, 1, 5, 2),
    
    -- Engineering - Week 3
    ('11111111-1111-1111-1111-111111111111', '00000000-0000-2000-0000-000000000001',
     '2025-02-02', 'weekly', '2025-01-27', '2025-02-02',
     4, 4, 7, 6, 1, 14.29,
     30, 25, 83.33, 100.00, 1, 4, 3),
    
    -- Engineering - Week 4
    ('11111111-1111-1111-1111-111111111111', '00000000-0000-2000-0000-000000000001',
     '2025-02-09', 'weekly', '2025-02-03', '2025-02-09',
     4, 4, 6, 6, 2, 33.33,
     24, 22, 91.67, 100.00, 1, 5, 2);

-- ============================================================================
-- ORGANIZATION SNAPSHOTS
-- ============================================================================
INSERT INTO organization_snapshots (organization_id, snapshot_date, period_type,
    period_start, period_end, total_users, active_users, total_team_members,
    users_logged_in, login_rate, goals_total, goals_on_track_rate,
    goals_completed_this_period, one_on_ones_held, one_on_one_completion_rate,
    avg_engagement_score, enps_score, feedback_count, recognition_count)
VALUES
    -- Week 1
    ('11111111-1111-1111-1111-111111111111',
     '2025-01-19', 'weekly', '2025-01-13', '2025-01-19',
     8, 8, 6, 7, 87.50,
     12, 66.67, 0, 6, 75.00,
     NULL, NULL, 4, 2),
    
    -- Week 2
    ('11111111-1111-1111-1111-111111111111',
     '2025-01-26', 'weekly', '2025-01-20', '2025-01-26',
     8, 8, 6, 8, 100.00,
     12, 75.00, 1, 8, 100.00,
     4.2, 40, 6, 3),
    
    -- Week 3
    ('11111111-1111-1111-1111-111111111111',
     '2025-02-02', 'weekly', '2025-01-27', '2025-02-02',
     8, 8, 6, 8, 100.00,
     11, 81.82, 1, 8, 100.00,
     4.0, 35, 5, 4),
    
    -- Week 4
    ('11111111-1111-1111-1111-111111111111',
     '2025-02-09', 'weekly', '2025-02-03', '2025-02-09',
     8, 8, 6, 8, 100.00,
     9, 88.89, 2, 8, 100.00,
     4.3, 45, 4, 3),
    
    -- January Monthly
    ('11111111-1111-1111-1111-111111111111',
     '2025-01-31', 'monthly', '2025-01-01', '2025-01-31',
     8, 8, 6, 8, 100.00,
     12, 75.00, 2, 30, 93.75,
     4.1, 38, 15, 8);

SELECT 'Sample snapshot data created successfully' AS status;

-- Show team trends
SELECT 
    snapshot_date,
    goals_on_track || '/' || goals_total as goals,
    task_completion_rate || '%' as task_rate,
    one_on_ones_completion_rate || '%' as one_on_one_rate
FROM team_snapshots
WHERE team_id = '00000000-0000-2000-0000-000000000001'
ORDER BY snapshot_date;

-- Show org trends
SELECT 
    snapshot_date,
    period_type::text,
    goals_on_track_rate || '%' as goals_on_track,
    login_rate || '%' as engagement,
    enps_score as enps
FROM organization_snapshots
WHERE organization_id = '11111111-1111-1111-1111-111111111111'
ORDER BY snapshot_date;
