-- ============================================================================
-- TRACKER DATABASE - SEED DATA: PULSE SURVEYS
-- ============================================================================
-- Sample surveys and responses for Prickly Cactus Software
--
-- References:
--   Organization: '11111111-1111-1111-1111-111111111111'
--   Team Members: Emily, David, Jessica, Alex, Rachel, Michael
-- ============================================================================

-- ============================================================================
-- CLEAN UP FOR RE-RUNS
-- ============================================================================
DELETE FROM survey_answers WHERE response_id IN (
    SELECT id FROM survey_responses WHERE survey_id IN (
        SELECT id FROM surveys WHERE organization_id = '11111111-1111-1111-1111-111111111111'
    )
);
DELETE FROM survey_responses WHERE survey_id IN (
    SELECT id FROM surveys WHERE organization_id = '11111111-1111-1111-1111-111111111111'
);
DELETE FROM survey_instances WHERE survey_id IN (
    SELECT id FROM surveys WHERE organization_id = '11111111-1111-1111-1111-111111111111'
);
DELETE FROM survey_questions WHERE survey_id IN (
    SELECT id FROM surveys WHERE organization_id = '11111111-1111-1111-1111-111111111111'
);
DELETE FROM surveys WHERE organization_id = '11111111-1111-1111-1111-111111111111';

-- ============================================================================
-- SURVEYS
-- ============================================================================
INSERT INTO surveys (id, organization_id, title, description, survey_type, status,
    frequency, start_date, end_date, is_anonymous, allow_comments,
    welcome_message, thank_you_message, created_by)
VALUES
    -- Weekly pulse survey
    ('00000000-0000-b000-0000-000000000001',
     '11111111-1111-1111-1111-111111111111',
     'Weekly Team Pulse',
     'Quick weekly check-in on team morale and workload',
     'pulse', 'active', 'weekly',
     '2025-01-06', '2025-12-31',
     true, true,
     'How are things going this week? Your feedback helps us improve.',
     'Thanks for sharing! Your voice matters.',
     'a0000000-0000-0000-0000-000000000001'),
    
    -- Quarterly engagement survey
    ('00000000-0000-b000-0000-000000000002',
     '11111111-1111-1111-1111-111111111111',
     'Q1 2025 Engagement Survey',
     'Comprehensive quarterly engagement and satisfaction survey',
     'engagement', 'active', 'once',
     '2025-01-15', '2025-01-31',
     true, true,
     'Help us understand how we can make this a better place to work.',
     'Thank you for your thoughtful responses!',
     'a0000000-0000-0000-0000-000000000001');

-- ============================================================================
-- SURVEY QUESTIONS - Weekly Pulse
-- ============================================================================
INSERT INTO survey_questions (id, survey_id, question_text, question_type, 
    is_required, sort_order, min_value, max_value, min_label, max_label, category)
VALUES
    ('00000000-0000-b100-0000-000000000001',
     '00000000-0000-b000-0000-000000000001',
     'How would you rate your overall mood this week?',
     'emoji', true, 1, 1, 5, 'Very unhappy', 'Very happy', 'Wellbeing'),
    ('00000000-0000-b100-0000-000000000002',
     '00000000-0000-b000-0000-000000000001',
     'How manageable was your workload?',
     'rating', true, 2, 1, 5, 'Overwhelming', 'Very manageable', 'Workload'),
    ('00000000-0000-b100-0000-000000000003',
     '00000000-0000-b000-0000-000000000001',
     'Any blockers or concerns to share?',
     'text', false, 3, NULL, NULL, NULL, NULL, 'Feedback');

-- ============================================================================
-- SURVEY QUESTIONS - Engagement Survey
-- ============================================================================
INSERT INTO survey_questions (id, survey_id, question_text, help_text, question_type, 
    is_required, sort_order, min_value, max_value, min_label, max_label, category)
VALUES
    ('00000000-0000-b100-0000-000000000010',
     '00000000-0000-b000-0000-000000000002',
     'I would recommend this company as a great place to work',
     NULL, 'nps', true, 1, 0, 10, 'Not at all likely', 'Extremely likely', 'eNPS'),
    ('00000000-0000-b100-0000-000000000011',
     '00000000-0000-b000-0000-000000000002',
     'I feel valued for my contributions',
     NULL, 'rating', true, 2, 1, 5, 'Strongly disagree', 'Strongly agree', 'Recognition'),
    ('00000000-0000-b100-0000-000000000012',
     '00000000-0000-b000-0000-000000000002',
     'My manager supports my growth and development',
     NULL, 'rating', true, 3, 1, 5, 'Strongly disagree', 'Strongly agree', 'Manager'),
    ('00000000-0000-b100-0000-000000000013',
     '00000000-0000-b000-0000-000000000002',
     'I have the tools and resources I need to do my job well',
     NULL, 'rating', true, 4, 1, 5, 'Strongly disagree', 'Strongly agree', 'Resources'),
    ('00000000-0000-b100-0000-000000000014',
     '00000000-0000-b000-0000-000000000002',
     'I understand how my work contributes to company goals',
     NULL, 'rating', true, 5, 1, 5, 'Strongly disagree', 'Strongly agree', 'Alignment'),
    ('00000000-0000-b100-0000-000000000015',
     '00000000-0000-b000-0000-000000000002',
     'What could we do to make this a better place to work?',
     NULL, 'text', false, 6, NULL, NULL, NULL, NULL, 'Open');

-- ============================================================================
-- SURVEY INSTANCES (for weekly pulse)
-- ============================================================================
INSERT INTO survey_instances (id, survey_id, period_start, period_end, status,
    sent_at, total_recipients, total_responses, response_rate)
VALUES
    ('00000000-0000-b200-0000-000000000001',
     '00000000-0000-b000-0000-000000000001',
     '2025-01-27', '2025-02-02', 'closed',
     '2025-01-27 09:00:00+00', 6, 5, 83.33),
    ('00000000-0000-b200-0000-000000000002',
     '00000000-0000-b000-0000-000000000001',
     '2025-02-03', '2025-02-09', 'active',
     '2025-02-03 09:00:00+00', 6, 3, 50.00);

-- ============================================================================
-- SURVEY RESPONSES (Anonymous)
-- ============================================================================
INSERT INTO survey_responses (id, survey_id, instance_id, team_member_id,
    started_at, completed_at, is_complete)
VALUES
    -- Week of Jan 27 responses (completed)
    ('00000000-0000-b300-0000-000000000001',
     '00000000-0000-b000-0000-000000000001',
     '00000000-0000-b200-0000-000000000001',
     NULL, '2025-01-27 10:00:00+00', '2025-01-27 10:02:00+00', true),
    ('00000000-0000-b300-0000-000000000002',
     '00000000-0000-b000-0000-000000000001',
     '00000000-0000-b200-0000-000000000001',
     NULL, '2025-01-27 11:30:00+00', '2025-01-27 11:32:00+00', true),
    ('00000000-0000-b300-0000-000000000003',
     '00000000-0000-b000-0000-000000000001',
     '00000000-0000-b200-0000-000000000001',
     NULL, '2025-01-28 09:00:00+00', '2025-01-28 09:03:00+00', true),
    
    -- Engagement survey responses
    ('00000000-0000-b300-0000-000000000010',
     '00000000-0000-b000-0000-000000000002',
     NULL, NULL, '2025-01-20 14:00:00+00', '2025-01-20 14:10:00+00', true),
    ('00000000-0000-b300-0000-000000000011',
     '00000000-0000-b000-0000-000000000002',
     NULL, NULL, '2025-01-21 10:00:00+00', '2025-01-21 10:08:00+00', true);

-- ============================================================================
-- SURVEY ANSWERS
-- ============================================================================
INSERT INTO survey_answers (response_id, question_id, rating_value, text_value)
VALUES
    -- Response 1 to pulse
    ('00000000-0000-b300-0000-000000000001', '00000000-0000-b100-0000-000000000001', 4, NULL),
    ('00000000-0000-b300-0000-000000000001', '00000000-0000-b100-0000-000000000002', 3, NULL),
    ('00000000-0000-b300-0000-000000000001', '00000000-0000-b100-0000-000000000003', NULL, 'Sprint deadline is tight but manageable'),
    
    -- Response 2 to pulse
    ('00000000-0000-b300-0000-000000000002', '00000000-0000-b100-0000-000000000001', 5, NULL),
    ('00000000-0000-b300-0000-000000000002', '00000000-0000-b100-0000-000000000002', 4, NULL),
    
    -- Response 3 to pulse
    ('00000000-0000-b300-0000-000000000003', '00000000-0000-b100-0000-000000000001', 3, NULL),
    ('00000000-0000-b300-0000-000000000003', '00000000-0000-b100-0000-000000000002', 2, NULL),
    ('00000000-0000-b300-0000-000000000003', '00000000-0000-b100-0000-000000000003', NULL, 'Too many meetings this week'),
    
    -- Engagement survey response 1
    ('00000000-0000-b300-0000-000000000010', '00000000-0000-b100-0000-000000000010', 8, NULL),
    ('00000000-0000-b300-0000-000000000010', '00000000-0000-b100-0000-000000000011', 4, NULL),
    ('00000000-0000-b300-0000-000000000010', '00000000-0000-b100-0000-000000000012', 5, NULL),
    ('00000000-0000-b300-0000-000000000010', '00000000-0000-b100-0000-000000000013', 4, NULL),
    ('00000000-0000-b300-0000-000000000010', '00000000-0000-b100-0000-000000000014', 4, NULL),
    
    -- Engagement survey response 2
    ('00000000-0000-b300-0000-000000000011', '00000000-0000-b100-0000-000000000010', 9, NULL),
    ('00000000-0000-b300-0000-000000000011', '00000000-0000-b100-0000-000000000011', 5, NULL),
    ('00000000-0000-b300-0000-000000000011', '00000000-0000-b100-0000-000000000012', 5, NULL),
    ('00000000-0000-b300-0000-000000000011', '00000000-0000-b100-0000-000000000013', 3, NULL),
    ('00000000-0000-b300-0000-000000000011', '00000000-0000-b100-0000-000000000014', 5, NULL),
    ('00000000-0000-b300-0000-000000000011', '00000000-0000-b100-0000-000000000015', NULL, 'Would love better development tools and more learning budget');

SELECT 'Sample survey data created successfully' AS status;

-- Show summary
SELECT 
    s.title,
    s.survey_type,
    s.status,
    COUNT(DISTINCT sr.id) as responses
FROM surveys s
LEFT JOIN survey_responses sr ON sr.survey_id = s.id
WHERE s.organization_id = '11111111-1111-1111-1111-111111111111'
GROUP BY s.id, s.title, s.survey_type, s.status;
