-- ============================================================================
-- TRACKER DATABASE - SEED DATA: CALENDAR & REMINDERS
-- ============================================================================
-- Sample calendar links and reminders for Prickly Cactus Software
--
-- References:
--   Organization: '11111111-1111-1111-1111-111111111111'
--   Users:
--     Sarah:   'a0000000-0000-0000-0000-000000000001'
--     Emily:   'a0000000-0000-0000-0000-000000000003'
--     David:   'a0000000-0000-0000-0000-000000000004'
--   Team Members:
--     Emily:   '00000000-0000-1000-0000-000000000003'
--     David:   '00000000-0000-1000-0000-000000000004'
--   Meetings: '00000000-0000-8000-0000-00000000000X'
-- ============================================================================

-- ============================================================================
-- CLEAN UP FOR RE-RUNS
-- ============================================================================
DELETE FROM reminder_preferences WHERE user_id IN (
    SELECT id FROM users WHERE organization_id = '11111111-1111-1111-1111-111111111111'
);
DELETE FROM reminders WHERE organization_id = '11111111-1111-1111-1111-111111111111';
DELETE FROM calendar_links WHERE user_id IN (
    SELECT id FROM users WHERE organization_id = '11111111-1111-1111-1111-111111111111'
);

-- ============================================================================
-- CALENDAR LINKS
-- ============================================================================
INSERT INTO calendar_links (id, user_id, provider, account_email, account_name,
    is_active, sync_enabled, sync_meetings_to_calendar, sync_tasks_to_calendar,
    default_calendar_name, last_sync_at, last_sync_status)
VALUES
    -- Emily's Google Calendar
    ('00000000-0000-d000-0000-000000000001',
     'a0000000-0000-0000-0000-000000000003',
     'google',
     'emily.chen@pricklycactus.io',
     'Emily Chen',
     true, true, true, false,
     'Work Calendar',
     '2025-02-05 08:00:00+00', 'synced'),
    
    -- David's Microsoft Calendar
    ('00000000-0000-d000-0000-000000000002',
     'a0000000-0000-0000-0000-000000000004',
     'microsoft',
     'david.kim@pricklycactus.io',
     'David Kim',
     true, true, true, true,
     'Calendar',
     '2025-02-05 08:30:00+00', 'synced'),
    
    -- Sarah's Google Calendar (CEO)
    ('00000000-0000-d000-0000-000000000003',
     'a0000000-0000-0000-0000-000000000001',
     'google',
     'sarah.johnson@pricklycactus.io',
     'Sarah Johnson',
     true, true, true, false,
     'Prickly Cactus',
     '2025-02-05 07:00:00+00', 'synced');

-- ============================================================================
-- UPDATE MEETINGS WITH CALENDAR INFO
-- ============================================================================
UPDATE meetings SET
    calendar_event_id = 'evt_google_' || id::text,
    calendar_provider = 'google',
    calendar_link_id = '00000000-0000-d000-0000-000000000001',
    video_conference_url = 'https://meet.google.com/abc-defg-hij',
    video_conference_provider = 'google_meet',
    calendar_sync_status = 'synced',
    last_synced_at = '2025-02-05 08:00:00+00'
WHERE id = '00000000-0000-8000-0000-000000000001';  -- Emily-David 1:1

UPDATE meetings SET
    calendar_event_id = 'evt_ms_' || id::text,
    calendar_provider = 'microsoft',
    calendar_link_id = '00000000-0000-d000-0000-000000000002',
    video_conference_url = 'https://teams.microsoft.com/l/meetup-join/xxx',
    video_conference_provider = 'teams',
    calendar_sync_status = 'synced',
    last_synced_at = '2025-02-05 08:30:00+00'
WHERE id IN ('00000000-0000-8000-0000-000000000002', '00000000-0000-8000-0000-000000000003');

-- ============================================================================
-- REMINDER PREFERENCES
-- ============================================================================
INSERT INTO reminder_preferences (user_id, entity_type, sub_type, enabled,
    default_minutes_before, send_push, send_email, send_in_app)
VALUES
    -- Emily's preferences
    ('a0000000-0000-0000-0000-000000000003', 'meeting', 'one_on_one', true, 15, true, false, true),
    ('a0000000-0000-0000-0000-000000000003', 'meeting', 'team_meeting', true, 10, true, false, true),
    ('a0000000-0000-0000-0000-000000000003', 'task', NULL, true, 60, false, true, true),
    
    -- David's preferences
    ('a0000000-0000-0000-0000-000000000004', 'meeting', 'one_on_one', true, 30, true, true, true),
    ('a0000000-0000-0000-0000-000000000004', 'meeting', 'team_meeting', true, 15, true, false, true),
    ('a0000000-0000-0000-0000-000000000004', 'task', NULL, true, 120, false, true, true),
    ('a0000000-0000-0000-0000-000000000004', 'goal', NULL, true, 1440, false, true, true);  -- 24 hours

-- ============================================================================
-- REMINDERS
-- ============================================================================
INSERT INTO reminders (id, organization_id, user_id, team_member_id, reminder_type,
    entity_type, entity_id, title, message, remind_at, minutes_before,
    status, send_push, send_email, send_in_app)
VALUES
    -- Upcoming 1:1 reminder for David
    ('00000000-0000-d100-0000-000000000001',
     '11111111-1111-1111-1111-111111111111',
     'a0000000-0000-0000-0000-000000000004',
     '00000000-0000-1000-0000-000000000004',
     'meeting',
     'meeting', '00000000-0000-8000-0000-000000000003',
     'Upcoming 1:1 with Alex',
     'Your 1:1 with Alex starts in 30 minutes',
     '2025-02-06 14:30:00+00', 30,
     'scheduled', true, true, true),
    
    -- Task reminder for Emily
    ('00000000-0000-d100-0000-000000000002',
     '11111111-1111-1111-1111-111111111111',
     'a0000000-0000-0000-0000-000000000003',
     '00000000-0000-1000-0000-000000000003',
     'task',
     'task', '00000000-0000-7000-0000-000000000002',
     'Task due tomorrow: Review Auth PR',
     'Your task "Review Auth PR" is due tomorrow',
     '2025-02-10 09:00:00+00', 1440,
     'scheduled', false, true, true),
    
    -- 1:1 prep reminder
    ('00000000-0000-d100-0000-000000000003',
     '11111111-1111-1111-1111-111111111111',
     'a0000000-0000-0000-0000-000000000004',
     '00000000-0000-1000-0000-000000000004',
     'one_on_one_prep',
     'meeting', '00000000-0000-8000-0000-000000000002',
     'Prepare for 1:1 with Jessica',
     'Take a few minutes to review notes and talking points before your 1:1',
     '2025-02-06 13:00:00+00', 60,
     'sent', true, false, true),
    
    -- Goal check-in reminder
    ('00000000-0000-d100-0000-000000000004',
     '11111111-1111-1111-1111-111111111111',
     'a0000000-0000-0000-0000-000000000004',
     '00000000-0000-1000-0000-000000000004',
     'goal',
     'development_goal', '00000000-0000-c000-0000-000000000002',
     'Goal check-in: AWS Certification',
     'Time to update your progress on AWS Solutions Architect Certification',
     '2025-02-15 09:00:00+00', NULL,
     'scheduled', false, true, true);

SELECT 'Sample calendar and reminder data created successfully' AS status;

-- Show calendar links
SELECT 
    u.display_name,
    cl.provider::text,
    cl.account_email,
    cl.last_sync_status::text
FROM calendar_links cl
JOIN users u ON u.id = cl.user_id
ORDER BY u.display_name;

-- Show upcoming reminders
SELECT 
    r.title,
    r.reminder_type::text,
    r.status::text,
    r.remind_at
FROM reminders r
WHERE r.organization_id = '11111111-1111-1111-1111-111111111111'
ORDER BY r.remind_at;
