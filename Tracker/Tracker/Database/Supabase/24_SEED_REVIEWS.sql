-- ============================================================================
-- TRACKER DATABASE - SEED DATA: PERFORMANCE REVIEWS
-- ============================================================================
-- Sample review templates, cycles, and reviews for Prickly Cactus Software
--
-- References:
--   Organization: '11111111-1111-1111-1111-111111111111'
--   Team Members:
--     Emily:   '00000000-0000-1000-0000-000000000003'
--     David:   '00000000-0000-1000-0000-000000000004'
--     Jessica: '00000000-0000-1000-0000-000000000005'
--     Alex:    '00000000-0000-1000-0000-000000000006'
--   Users:
--     Sarah:   'a0000000-0000-0000-0000-000000000001'
-- ============================================================================

-- ============================================================================
-- CLEAN UP FOR RE-RUNS
-- ============================================================================
DELETE FROM review_responses WHERE review_id IN (
    SELECT id FROM reviews WHERE organization_id = '11111111-1111-1111-1111-111111111111'
);
DELETE FROM reviews WHERE organization_id = '11111111-1111-1111-1111-111111111111';
DELETE FROM review_cycles WHERE organization_id = '11111111-1111-1111-1111-111111111111';
DELETE FROM review_template_questions WHERE section_id IN (
    SELECT s.id FROM review_template_sections s
    JOIN review_templates t ON t.id = s.template_id
    WHERE t.organization_id = '11111111-1111-1111-1111-111111111111'
);
DELETE FROM review_template_sections WHERE template_id IN (
    SELECT id FROM review_templates WHERE organization_id = '11111111-1111-1111-1111-111111111111'
);
DELETE FROM review_templates WHERE organization_id = '11111111-1111-1111-1111-111111111111';

-- ============================================================================
-- REVIEW TEMPLATE
-- ============================================================================
INSERT INTO review_templates (id, organization_id, name, description, review_type, 
    is_default, include_self_review, include_peer_review, created_by)
VALUES
    ('00000000-0000-a000-0000-000000000001',
     '11111111-1111-1111-1111-111111111111',
     'Annual Performance Review',
     'Comprehensive annual review covering performance, growth, and goals',
     'annual',
     true, true, false,
     'a0000000-0000-0000-0000-000000000001');

-- ============================================================================
-- TEMPLATE SECTIONS
-- ============================================================================
INSERT INTO review_template_sections (id, template_id, title, description, sort_order, weight)
VALUES
    ('00000000-0000-a100-0000-000000000001',
     '00000000-0000-a000-0000-000000000001',
     'Core Competencies',
     'Assessment of key job competencies',
     1, 1.0),
    ('00000000-0000-a100-0000-000000000002',
     '00000000-0000-a000-0000-000000000001',
     'Goal Achievement',
     'Review of goals set for the review period',
     2, 1.0),
    ('00000000-0000-a100-0000-000000000003',
     '00000000-0000-a000-0000-000000000001',
     'Growth & Development',
     'Career growth and learning',
     3, 1.0);

-- ============================================================================
-- TEMPLATE QUESTIONS
-- ============================================================================
INSERT INTO review_template_questions (id, section_id, question_text, help_text, 
    question_type, is_required, sort_order, min_rating, max_rating, rating_labels)
VALUES
    -- Core Competencies
    ('00000000-0000-a200-0000-000000000001',
     '00000000-0000-a100-0000-000000000001',
     'Technical Skills',
     'Demonstrates expertise in their technical domain',
     'rating', true, 1, 1, 5,
     '{"1": "Needs Development", "3": "Meets Expectations", "5": "Exceptional"}'),
    ('00000000-0000-a200-0000-000000000002',
     '00000000-0000-a100-0000-000000000001',
     'Communication',
     'Communicates clearly and effectively with team and stakeholders',
     'rating', true, 2, 1, 5,
     '{"1": "Needs Development", "3": "Meets Expectations", "5": "Exceptional"}'),
    ('00000000-0000-a200-0000-000000000003',
     '00000000-0000-a100-0000-000000000001',
     'Collaboration',
     'Works effectively with others, contributes to team success',
     'rating', true, 3, 1, 5,
     '{"1": "Needs Development", "3": "Meets Expectations", "5": "Exceptional"}'),
    
    -- Goal Achievement
    ('00000000-0000-a200-0000-000000000004',
     '00000000-0000-a100-0000-000000000002',
     'Goal Completion Rate',
     'How well did they achieve their stated goals?',
     'rating', true, 1, 1, 5,
     '{"1": "Did not meet goals", "3": "Met most goals", "5": "Exceeded all goals"}'),
    ('00000000-0000-a200-0000-000000000005',
     '00000000-0000-a100-0000-000000000002',
     'Goal Summary',
     'Summarize key accomplishments and any goals not met',
     'text', true, 2, NULL, NULL, NULL),
    
    -- Growth & Development
    ('00000000-0000-a200-0000-000000000006',
     '00000000-0000-a100-0000-000000000003',
     'Learning & Growth',
     'Actively pursues learning and skill development',
     'rating', true, 1, 1, 5,
     '{"1": "Needs Development", "3": "Meets Expectations", "5": "Exceptional"}'),
    ('00000000-0000-a200-0000-000000000007',
     '00000000-0000-a100-0000-000000000003',
     'Development Goals for Next Period',
     'What skills or areas should be focused on?',
     'text', true, 2, NULL, NULL, NULL);

-- ============================================================================
-- REVIEW CYCLE
-- ============================================================================
INSERT INTO review_cycles (id, organization_id, template_id, name, description,
    start_date, end_date, self_review_due, manager_review_due, status,
    include_all_employees, created_by, launched_at)
VALUES
    ('00000000-0000-a300-0000-000000000001',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-a000-0000-000000000001',
     '2024 Annual Reviews',
     'Annual performance reviews for calendar year 2024',
     '2025-01-15', '2025-02-28',
     '2025-02-07', '2025-02-21',
     'active',
     true,
     'a0000000-0000-0000-0000-000000000001',
     '2025-01-15 09:00:00+00');

-- ============================================================================
-- REVIEWS (Individual reviews for team members)
-- ============================================================================
INSERT INTO reviews (id, organization_id, cycle_id, reviewee_team_member_id, 
    reviewer_team_member_id, status, self_review_status, manager_review_status,
    overall_rating, strengths, areas_for_improvement)
VALUES
    -- David's review (by Emily)
    ('00000000-0000-a400-0000-000000000001',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-a300-0000-000000000001',
     '00000000-0000-1000-0000-000000000004',  -- David
     '00000000-0000-1000-0000-000000000003',  -- Emily
     'in_progress',
     'submitted', 'in_progress',
     NULL,
     'Strong technical leadership, excellent mentoring',
     'Could delegate more to develop team'),
    
    -- Jessica's review (by David)
    ('00000000-0000-a400-0000-000000000002',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-a300-0000-000000000001',
     '00000000-0000-1000-0000-000000000005',  -- Jessica
     '00000000-0000-1000-0000-000000000004',  -- David
     'in_progress',
     'submitted', 'not_started',
     NULL, NULL, NULL),
    
    -- Alex's review (by David)
    ('00000000-0000-a400-0000-000000000003',
     '11111111-1111-1111-1111-111111111111',
     '00000000-0000-a300-0000-000000000001',
     '00000000-0000-1000-0000-000000000006',  -- Alex
     '00000000-0000-1000-0000-000000000004',  -- David
     'not_started',
     'not_started', 'not_started',
     NULL, NULL, NULL);

-- ============================================================================
-- SAMPLE REVIEW RESPONSES (David's self-review)
-- ============================================================================
INSERT INTO review_responses (review_id, question_id, responder_type, 
    responder_team_member_id, rating_value, text_value)
VALUES
    ('00000000-0000-a400-0000-000000000001', '00000000-0000-a200-0000-000000000001',
     'self', '00000000-0000-1000-0000-000000000004', 4, NULL),
    ('00000000-0000-a400-0000-000000000001', '00000000-0000-a200-0000-000000000002',
     'self', '00000000-0000-1000-0000-000000000004', 4, NULL),
    ('00000000-0000-a400-0000-000000000001', '00000000-0000-a200-0000-000000000003',
     'self', '00000000-0000-1000-0000-000000000004', 5, NULL),
    ('00000000-0000-a400-0000-000000000001', '00000000-0000-a200-0000-000000000004',
     'self', '00000000-0000-1000-0000-000000000004', 4, NULL),
    ('00000000-0000-a400-0000-000000000001', '00000000-0000-a200-0000-000000000005',
     'self', '00000000-0000-1000-0000-000000000004', NULL, 
     'Completed offline sync architecture ahead of schedule. Led successful migration to new auth system. Mentored 2 junior engineers.'),
    ('00000000-0000-a400-0000-000000000001', '00000000-0000-a200-0000-000000000006',
     'self', '00000000-0000-1000-0000-000000000004', 4, NULL),
    ('00000000-0000-a400-0000-000000000001', '00000000-0000-a200-0000-000000000007',
     'self', '00000000-0000-1000-0000-000000000004', NULL,
     'Want to develop leadership skills for platform team expansion. Interested in learning more about system design at scale.');

SELECT 'Sample review data created successfully' AS status;

-- Show summary
SELECT 
    rt.name as template,
    rc.name as cycle,
    rc.status as cycle_status,
    COUNT(r.id) as review_count
FROM review_templates rt
JOIN review_cycles rc ON rc.template_id = rt.id
LEFT JOIN reviews r ON r.cycle_id = rc.id
WHERE rt.organization_id = '11111111-1111-1111-1111-111111111111'
GROUP BY rt.name, rc.name, rc.status;
