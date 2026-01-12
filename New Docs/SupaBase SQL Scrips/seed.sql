-- Tracker Seed Data
-- Run this to populate test data after schema setup
-- Usage: psql -f seed.sql or run via Supabase SQL Editor

-- ============================================
-- TEST SURVEY DATA
-- ============================================

-- Create a test survey
INSERT INTO surveys (id, title, description, is_anonymous, is_active, created_at)
VALUES (
  'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
  'Q4 2025 Team Pulse Check',
  'Quick check-in on how the team is feeling about current projects and workload.',
  false,
  true,
  NOW()
) ON CONFLICT (id) DO NOTHING;

-- Create survey questions
INSERT INTO survey_questions (id, survey_id, question_text, question_type, options, is_required, order_index)
VALUES
  -- Rating question
  (
    'q1111111-1111-1111-1111-111111111111',
    'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    'How would you rate your current workload?',
    'rating',
    '{"maxRating": 5, "lowLabel": "Too Light", "highLabel": "Overwhelming"}',
    true,
    1
  ),
  -- Multiple choice question
  (
    'q2222222-2222-2222-2222-222222222222',
    'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    'Which area needs the most improvement?',
    'multiple_choice',
    '{"choices": ["Communication", "Tools & Resources", "Work-Life Balance", "Career Growth", "Team Collaboration"]}',
    true,
    2
  ),
  -- Yes/No question
  (
    'q3333333-3333-3333-3333-333333333333',
    'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    'Do you feel supported by your manager?',
    'yes_no',
    '{}',
    true,
    3
  ),
  -- Text question
  (
    'q4444444-4444-4444-4444-444444444444',
    'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    'What''s one thing we could do to improve your work experience?',
    'text',
    '{}',
    false,
    4
  )
ON CONFLICT (id) DO NOTHING;

-- Create a test token (valid for 7 days)
INSERT INTO survey_tokens (id, survey_id, token, team_member_id, team_member_name, expires_at, created_at)
VALUES (
  't0000000-0000-0000-0000-000000000001',
  'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
  'TEST-TOKEN-12345',
  NULL,
  'Test User',
  NOW() + INTERVAL '7 days',
  NOW()
) ON CONFLICT (id) DO NOTHING;

-- Output the test URL
SELECT 'Test Survey URL: https://your-site.workers.dev?token=TEST-TOKEN-12345' AS info;
