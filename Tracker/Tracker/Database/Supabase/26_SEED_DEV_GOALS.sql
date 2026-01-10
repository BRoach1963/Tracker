-- ============================================================================
-- TRACKER DATABASE - SEED DATA: DEVELOPMENT GOALS
-- ============================================================================
-- Personal development goals for team members at Prickly Cactus Software
--
-- References:
--   Organization: '11111111-1111-1111-1111-111111111111'
--   Team Members:
--     David:   '00000000-0000-1000-0000-000000000004'
--     Jessica: '00000000-0000-1000-0000-000000000005'
--     Alex:    '00000000-0000-1000-0000-000000000006'
-- ============================================================================

-- ============================================================================
-- CLEAN UP FOR RE-RUNS
-- ============================================================================
DELETE FROM development_goal_comments WHERE goal_id IN (
    SELECT id FROM development_goals WHERE organization_id = '11111111-1111-1111-1111-111111111111'
);
DELETE FROM development_goal_milestones WHERE goal_id IN (
    SELECT id FROM development_goals WHERE organization_id = '11111111-1111-1111-1111-111111111111'
);
DELETE FROM development_goals WHERE organization_id = '11111111-1111-1111-1111-111111111111';

-- ============================================================================
-- DEVELOPMENT GOALS
-- ============================================================================
INSERT INTO development_goals (id, organization_id, team_member_id, title, description,
    category, target_date, status, progress_percent, why_important, success_criteria,
    support_needed, is_private, shared_with_manager, started_at)
VALUES
    -- David: Leadership development
    ('00000000-0000-c000-0000-000000000001',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000004',
     'Develop Platform Team Leadership Skills',
     'Prepare for leading the expanded platform team by Q3 2025',
     'leadership',
     '2025-09-01',
     'active', 35,
     'Company is growing and I want to take on more leadership responsibility. This aligns with my long-term career goal of becoming a Staff Engineer.',
     'Successfully mentor 2 junior engineers. Lead at least one major technical initiative. Complete leadership training.',
     'Time allocation for mentoring. Access to leadership training resources.',
     false, true, '2025-01-15 00:00:00+00'),
    
    -- David: Technical certification
    ('00000000-0000-c000-0000-000000000002',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000004',
     'AWS Solutions Architect Certification',
     'Obtain AWS Solutions Architect Professional certification',
     'certification',
     '2025-06-30',
     'active', 50,
     'Our infrastructure is moving more to AWS. This certification will help me make better architectural decisions.',
     'Pass the AWS Solutions Architect Professional exam',
     'Study time, exam fee coverage',
     false, true, '2025-01-01 00:00:00+00'),
    
    -- Jessica: Skill development
    ('00000000-0000-c000-0000-000000000003',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000005',
     'Master GraphQL Development',
     'Become proficient in GraphQL for the upcoming API migration',
     'skill_development',
     '2025-04-30',
     'active', 25,
     'The team is migrating to GraphQL and I want to be able to contribute significantly to this effort.',
     'Complete GraphQL course. Implement at least 3 GraphQL resolvers in production. Present learnings to team.',
     'Access to GraphQL course, pairing time with David',
     false, true, '2025-01-20 00:00:00+00'),
    
    -- Alex: Onboarding goal
    ('00000000-0000-c000-0000-000000000004',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-1000-0000-000000000006',
     'Complete Onboarding & Ramp Up',
     'Get fully productive in the first 90 days',
     'career_growth',
     '2025-04-15',
     'active', 60,
     'Want to contribute meaningfully to the team as quickly as possible',
     'Ship first feature independently. Understand full codebase architecture. Build relationships with all team members.',
     'Onboarding buddy, documentation access',
     false, true, '2025-01-15 00:00:00+00');

-- ============================================================================
-- MILESTONES
-- ============================================================================
INSERT INTO development_goal_milestones (id, goal_id, title, description,
    target_date, status, sort_order, completed_at)
VALUES
    -- David Leadership milestones
    ('00000000-0000-c100-0000-000000000001',
     '00000000-0000-c000-0000-000000000001',
     'Complete leadership fundamentals course',
     'Take the internal leadership training program',
     '2025-03-31', 'completed', 1, '2025-02-01 00:00:00+00'),
    ('00000000-0000-c100-0000-000000000002',
     '00000000-0000-c000-0000-000000000001',
     'Start mentoring Alex',
     'Begin regular mentoring sessions with Alex',
     '2025-02-15', 'completed', 2, '2025-01-20 00:00:00+00'),
    ('00000000-0000-c100-0000-000000000003',
     '00000000-0000-c000-0000-000000000001',
     'Lead offline sync initiative',
     'Take ownership of the offline sync architecture project',
     '2025-06-30', 'in_progress', 3, NULL),
    ('00000000-0000-c100-0000-000000000004',
     '00000000-0000-c000-0000-000000000001',
     'Conduct first team retrospective',
     'Facilitate a sprint retrospective for the platform team',
     '2025-04-30', 'not_started', 4, NULL),
    
    -- David AWS milestones
    ('00000000-0000-c100-0000-000000000010',
     '00000000-0000-c000-0000-000000000002',
     'Complete AWS course modules',
     'Finish all modules in the A Cloud Guru course',
     '2025-03-31', 'in_progress', 1, NULL),
    ('00000000-0000-c100-0000-000000000011',
     '00000000-0000-c000-0000-000000000002',
     'Pass practice exams',
     'Score 80%+ on 3 practice exams',
     '2025-05-31', 'not_started', 2, NULL),
    ('00000000-0000-c100-0000-000000000012',
     '00000000-0000-c000-0000-000000000002',
     'Schedule and take exam',
     'Book and pass the certification exam',
     '2025-06-30', 'not_started', 3, NULL),
    
    -- Jessica GraphQL milestones
    ('00000000-0000-c100-0000-000000000020',
     '00000000-0000-c000-0000-000000000003',
     'Complete GraphQL fundamentals course',
     'Finish Frontend Masters GraphQL course',
     '2025-02-28', 'in_progress', 1, NULL),
    ('00000000-0000-c100-0000-000000000021',
     '00000000-0000-c000-0000-000000000003',
     'Build practice project',
     'Create a small GraphQL API for the internal tool',
     '2025-03-31', 'not_started', 2, NULL),
    ('00000000-0000-c100-0000-000000000022',
     '00000000-0000-c000-0000-000000000003',
     'Contribute to migration project',
     'Implement resolvers for user profile API',
     '2025-04-30', 'not_started', 3, NULL);

-- ============================================================================
-- COMMENTS / CHECK-INS
-- ============================================================================
INSERT INTO development_goal_comments (goal_id, author_team_member_id, content, comment_type)
VALUES
    -- Emily checking in on David's leadership goal
    ('00000000-0000-c000-0000-000000000001',
     '00000000-0000-1000-0000-000000000003',
     'Great progress on the mentoring! Alex has mentioned how helpful the sessions have been. Keep it up!',
     'encouragement'),
    
    -- David's self-check-in
    ('00000000-0000-c000-0000-000000000001',
     '00000000-0000-1000-0000-000000000004',
     'Feeling good about the leadership course. Starting to apply learnings in daily standups.',
     'check_in'),
    
    -- David commenting on Jessica's goal
    ('00000000-0000-c000-0000-000000000003',
     '00000000-0000-1000-0000-000000000004',
     'Happy to pair with you on GraphQL next week. Let me know what time works.',
     'comment');

SELECT 'Sample development goal data created successfully' AS status;

-- Show summary
SELECT 
    tm.first_name || ' ' || tm.last_name as person,
    dg.title,
    dg.category::text,
    dg.status::text,
    dg.progress_percent || '%' as progress,
    COUNT(dgm.id) as milestones
FROM development_goals dg
JOIN team_members tm ON tm.id = dg.team_member_id
LEFT JOIN development_goal_milestones dgm ON dgm.goal_id = dg.id
WHERE dg.organization_id = '11111111-1111-1111-1111-111111111111'
GROUP BY dg.id, tm.first_name, tm.last_name, dg.title, dg.category, dg.status, dg.progress_percent
ORDER BY person, dg.title;
