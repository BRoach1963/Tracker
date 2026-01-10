-- ============================================================================
-- TRACKER DATABASE - SEED DATA: TEST USERS AND TEAMS
-- ============================================================================
-- Creates test users, teams, and team memberships
--
-- IMPORTANT: Auth users must be created FIRST via one of these methods:
--   1. Supabase Dashboard: Authentication > Users > Add User
--   2. Supabase Admin API: supabase.auth.admin.createUser()
--   3. Supabase CLI (local dev): Can insert directly into auth.users
--
-- After creating auth users, note their UUIDs and update this script.
-- The UUIDs below are placeholders for development.
-- ============================================================================

-- ============================================================================
-- CLEAN UP FOR RE-RUNS
-- ============================================================================
-- Delete in reverse dependency order
DELETE FROM team_memberships WHERE team_id IN (
    SELECT id FROM teams WHERE organization_id = '11111111-1111-1111-1111-111111111111'
);
DELETE FROM user_roles WHERE organization_id = '11111111-1111-1111-1111-111111111111';
DELETE FROM team_members WHERE organization_id = '11111111-1111-1111-1111-111111111111';
DELETE FROM teams WHERE organization_id = '11111111-1111-1111-1111-111111111111';
DELETE FROM users WHERE email LIKE '%@pricklycactussoftware.com';

-- ============================================================================
-- OPTION 1: FOR LOCAL DEVELOPMENT WITH SUPABASE CLI
-- Uncomment this section if running locally with Supabase CLI
-- ============================================================================
/*
INSERT INTO auth.users (
    id,
    instance_id,
    email,
    encrypted_password,
    email_confirmed_at,
    created_at,
    updated_at,
    raw_app_meta_data,
    raw_user_meta_data,
    aud,
    role
) VALUES
    ('b0000000-0000-0000-0000-000000000000', '00000000-0000-0000-0000-000000000000', 'brian@pricklycactussoftware.com', crypt('$teelers4Ever', gen_salt('bf')), NOW(), NOW(), NOW(), '{"provider": "email", "providers": ["email"]}', '{"display_name": "Brian E Roach"}', 'authenticated', 'authenticated'),
    ('a0000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000000', 'sarah.chen@pricklycactussoftware.com', crypt('$teelers4Ever', gen_salt('bf')), NOW(), NOW(), NOW(), '{"provider": "email", "providers": ["email"]}', '{"display_name": "Sarah Chen"}', 'authenticated', 'authenticated'),
    ('a0000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000000', 'marcus.johnson@pricklycactussoftware.com', crypt('$teelers4Ever', gen_salt('bf')), NOW(), NOW(), NOW(), '{"provider": "email", "providers": ["email"]}', '{"display_name": "Marcus Johnson"}', 'authenticated', 'authenticated'),
    ('a0000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000000', 'emily.rodriguez@pricklycactussoftware.com', crypt('$teelers4Ever', gen_salt('bf')), NOW(), NOW(), NOW(), '{"provider": "email", "providers": ["email"]}', '{"display_name": "Emily Rodriguez"}', 'authenticated', 'authenticated'),
    ('a0000000-0000-0000-0000-000000000004', '00000000-0000-0000-0000-000000000000', 'david.kim@pricklycactussoftware.com', crypt('$teelers4Ever', gen_salt('bf')), NOW(), NOW(), NOW(), '{"provider": "email", "providers": ["email"]}', '{"display_name": "David Kim"}', 'authenticated', 'authenticated'),
    ('a0000000-0000-0000-0000-000000000005', '00000000-0000-0000-0000-000000000000', 'jessica.thompson@pricklycactussoftware.com', crypt('$teelers4Ever', gen_salt('bf')), NOW(), NOW(), NOW(), '{"provider": "email", "providers": ["email"]}', '{"display_name": "Jessica Thompson"}', 'authenticated', 'authenticated'),
    ('a0000000-0000-0000-0000-000000000006', '00000000-0000-0000-0000-000000000000', 'alex.martinez@pricklycactussoftware.com', crypt('$teelers4Ever', gen_salt('bf')), NOW(), NOW(), NOW(), '{"provider": "email", "providers": ["email"]}', '{"display_name": "Alex Martinez"}', 'authenticated', 'authenticated'),
    ('a0000000-0000-0000-0000-000000000007', '00000000-0000-0000-0000-000000000000', 'rachel.green@pricklycactussoftware.com', crypt('$teelers4Ever', gen_salt('bf')), NOW(), NOW(), NOW(), '{"provider": "email", "providers": ["email"]}', '{"display_name": "Rachel Green"}', 'authenticated', 'authenticated'),
    ('a0000000-0000-0000-0000-000000000008', '00000000-0000-0000-0000-000000000000', 'michael.brown@pricklycactussoftware.com', crypt('$teelers4Ever', gen_salt('bf')), NOW(), NOW(), NOW(), '{"provider": "email", "providers": ["email"]}', '{"display_name": "Michael Brown"}', 'authenticated', 'authenticated');

INSERT INTO auth.identities (id, user_id, provider_id, provider, identity_data, last_sign_in_at, created_at, updated_at) VALUES
    ('b0000000-0000-0000-0000-000000000000', 'b0000000-0000-0000-0000-000000000000', 'brian@pricklycactussoftware.com', 'email', '{"sub": "b0000000-0000-0000-0000-000000000000", "email": "brian@pricklycactussoftware.com"}', NOW(), NOW(), NOW()),
    ('a0000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000001', 'sarah.chen@pricklycactussoftware.com', 'email', '{"sub": "a0000000-0000-0000-0000-000000000001", "email": "sarah.chen@pricklycactussoftware.com"}', NOW(), NOW(), NOW()),
    ('a0000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000002', 'marcus.johnson@pricklycactussoftware.com', 'email', '{"sub": "a0000000-0000-0000-0000-000000000002", "email": "marcus.johnson@pricklycactussoftware.com"}', NOW(), NOW(), NOW()),
    ('a0000000-0000-0000-0000-000000000003', 'a0000000-0000-0000-0000-000000000003', 'emily.rodriguez@pricklycactussoftware.com', 'email', '{"sub": "a0000000-0000-0000-0000-000000000003", "email": "emily.rodriguez@pricklycactussoftware.com"}', NOW(), NOW(), NOW()),
    ('a0000000-0000-0000-0000-000000000004', 'a0000000-0000-0000-0000-000000000004', 'david.kim@pricklycactussoftware.com', 'email', '{"sub": "a0000000-0000-0000-0000-000000000004", "email": "david.kim@pricklycactussoftware.com"}', NOW(), NOW(), NOW()),
    ('a0000000-0000-0000-0000-000000000005', 'a0000000-0000-0000-0000-000000000005', 'jessica.thompson@pricklycactussoftware.com', 'email', '{"sub": "a0000000-0000-0000-0000-000000000005", "email": "jessica.thompson@pricklycactussoftware.com"}', NOW(), NOW(), NOW()),
    ('a0000000-0000-0000-0000-000000000006', 'a0000000-0000-0000-0000-000000000006', 'alex.martinez@pricklycactussoftware.com', 'email', '{"sub": "a0000000-0000-0000-0000-000000000006", "email": "alex.martinez@pricklycactussoftware.com"}', NOW(), NOW(), NOW()),
    ('a0000000-0000-0000-0000-000000000007', 'a0000000-0000-0000-0000-000000000007', 'rachel.green@pricklycactussoftware.com', 'email', '{"sub": "a0000000-0000-0000-0000-000000000007", "email": "rachel.green@pricklycactussoftware.com"}', NOW(), NOW(), NOW()),
    ('a0000000-0000-0000-0000-000000000008', 'a0000000-0000-0000-0000-000000000008', 'michael.brown@pricklycactussoftware.com', 'email', '{"sub": "a0000000-0000-0000-0000-000000000008", "email": "michael.brown@pricklycactussoftware.com"}', NOW(), NOW(), NOW());
*/

-- ============================================================================
-- OPTION 2: FOR HOSTED SUPABASE (Production/Staging)
-- Create users via Dashboard or API first, then update these UUIDs
-- ============================================================================

-- Test User Reference:
-- Email: brian@pricklycactussoftware.com       | Password: $teelers4Ever | Role: Admin
-- Email: sarah.chen@pricklycactussoftware.com  | Password: $teelers4Ever | Role: Admin
-- Email: marcus.johnson@pricklycactussoftware.com | Password: $teelers4Ever | Role: Manager
-- Email: emily.rodriguez@pricklycactussoftware.com | Password: $teelers4Ever | Role: Manager
-- Email: david.kim@pricklycactussoftware.com   | Password: $teelers4Ever | Role: Team Lead
-- Email: jessica.thompson@pricklycactussoftware.com | Password: $teelers4Ever | Role: Member
-- Email: alex.martinez@pricklycactussoftware.com | Password: $teelers4Ever | Role: Member
-- Email: rachel.green@pricklycactussoftware.com | Password: $teelers4Ever | Role: Manager
-- Email: michael.brown@pricklycactussoftware.com | Password: $teelers4Ever | Role: Member

-- ============================================================================
-- CREATE PUBLIC USERS (our application users table)
-- ============================================================================
-- supabase_auth_id should match the UUID from auth.users after creation

INSERT INTO users (id, supabase_auth_id, email, display_name, avatar_url) VALUES
    -- Brian E Roach - Founder/Admin (PRIMARY TEST USER)
    ('b0000000-0000-0000-0000-000000000000', 
     'b0000000-0000-0000-0000-000000000000',
     'brian@pricklycactussoftware.com', 
     'Brian E Roach', 
     NULL),
    
    -- Sarah Chen - CEO/Admin
    ('a0000000-0000-0000-0000-000000000001', 
     'a0000000-0000-0000-0000-000000000001',
     'sarah.chen@pricklycactussoftware.com', 
     'Sarah Chen', 
     NULL),
    
    -- Marcus Johnson - VP Engineering (Manager)
    ('a0000000-0000-0000-0000-000000000002',
     'a0000000-0000-0000-0000-000000000002',
     'marcus.johnson@pricklycactussoftware.com',
     'Marcus Johnson',
     NULL),
    
    -- Emily Rodriguez - Engineering Manager
    ('a0000000-0000-0000-0000-000000000003',
     'a0000000-0000-0000-0000-000000000003',
     'emily.rodriguez@pricklycactussoftware.com',
     'Emily Rodriguez',
     NULL),
    
    -- David Kim - Team Lead
    ('a0000000-0000-0000-0000-000000000004',
     'a0000000-0000-0000-0000-000000000004',
     'david.kim@pricklycactussoftware.com',
     'David Kim',
     NULL),
    
    -- Jessica Thompson - Senior Developer
    ('a0000000-0000-0000-0000-000000000005',
     'a0000000-0000-0000-0000-000000000005',
     'jessica.thompson@pricklycactussoftware.com',
     'Jessica Thompson',
     NULL),
    
    -- Alex Martinez - Developer
    ('a0000000-0000-0000-0000-000000000006',
     'a0000000-0000-0000-0000-000000000006',
     'alex.martinez@pricklycactussoftware.com',
     'Alex Martinez',
     NULL),
    
    -- Rachel Green - Product Manager
    ('a0000000-0000-0000-0000-000000000007',
     'a0000000-0000-0000-0000-000000000007',
     'rachel.green@pricklycactussoftware.com',
     'Rachel Green',
     NULL),
    
    -- Michael Brown - Designer
    ('a0000000-0000-0000-0000-000000000008',
     'a0000000-0000-0000-0000-000000000008',
     'michael.brown@pricklycactussoftware.com',
     'Michael Brown',
     NULL);

-- ============================================================================
-- CREATE TEAMS
-- ============================================================================
INSERT INTO teams (id, organization_id, name, description) VALUES
    ('00000000-0000-2000-0000-000000000001',
     '11111111-1111-1111-1111-111111111111',
     'Engineering',
     'Product engineering team'),
    
    ('00000000-0000-2000-0000-000000000002',
     '11111111-1111-1111-1111-111111111111',
     'Product',
     'Product management and design'),
    
    ('00000000-0000-2000-0000-000000000003',
     '11111111-1111-1111-1111-111111111111',
     'Platform',
     'Platform and infrastructure team');

-- ============================================================================
-- CREATE TEAM MEMBERS
-- ============================================================================
-- Note: manager_user_id references users table for hierarchy
-- linked_user_id links to users table if they have a login

INSERT INTO team_members (id, organization_id, linked_user_id, manager_user_id, first_name, last_name, email, job_title, department, hire_date) VALUES
    -- Brian E Roach - Founder (no manager, top of hierarchy)
    ('00000000-0000-1000-0000-000000000000',
     '11111111-1111-1111-1111-111111111111',
     'b0000000-0000-0000-0000-000000000000',
     NULL,
     'Brian', 'Roach',
     'brian@pricklycactussoftware.com',
     'Founder',
     'Engineering',
     '2020-01-01'),
    
    -- Sarah Chen - CEO (reports to Brian)
    ('00000000-0000-1000-0000-000000000001',
     '11111111-1111-1111-1111-111111111111',
     'a0000000-0000-0000-0000-000000000001',
     'b0000000-0000-0000-0000-000000000000',
     'Sarah', 'Chen',
     'sarah.chen@pricklycactussoftware.com',
     'Chief Executive Officer',
     'Executive',
     '2020-01-15'),
    
    -- Marcus Johnson - VP Engineering (reports to Sarah)
    ('00000000-0000-1000-0000-000000000002',
     '11111111-1111-1111-1111-111111111111',
     'a0000000-0000-0000-0000-000000000002',
     'a0000000-0000-0000-0000-000000000001',
     'Marcus', 'Johnson',
     'marcus.johnson@pricklycactussoftware.com',
     'VP of Engineering',
     'Engineering',
     '2020-03-01'),
    
    -- Emily Rodriguez - Engineering Manager (reports to Marcus)
    ('00000000-0000-1000-0000-000000000003',
     '11111111-1111-1111-1111-111111111111',
     'a0000000-0000-0000-0000-000000000003',
     'a0000000-0000-0000-0000-000000000002',
     'Emily', 'Rodriguez',
     'emily.rodriguez@pricklycactussoftware.com',
     'Engineering Manager',
     'Engineering',
     '2021-02-15'),
    
    -- David Kim - Team Lead (reports to Emily)
    ('00000000-0000-1000-0000-000000000004',
     '11111111-1111-1111-1111-111111111111',
     'a0000000-0000-0000-0000-000000000004',
     'a0000000-0000-0000-0000-000000000003',
     'David', 'Kim',
     'david.kim@pricklycactussoftware.com',
     'Team Lead',
     'Engineering',
     '2021-06-01'),
    
    -- Jessica Thompson - Senior Developer (reports to David)
    ('00000000-0000-1000-0000-000000000005',
     '11111111-1111-1111-1111-111111111111',
     'a0000000-0000-0000-0000-000000000005',
     'a0000000-0000-0000-0000-000000000004',
     'Jessica', 'Thompson',
     'jessica.thompson@pricklycactussoftware.com',
     'Senior Software Engineer',
     'Engineering',
     '2022-01-10'),
    
    -- Alex Martinez - Developer (reports to David)
    ('00000000-0000-1000-0000-000000000006',
     '11111111-1111-1111-1111-111111111111',
     'a0000000-0000-0000-0000-000000000006',
     'a0000000-0000-0000-0000-000000000004',
     'Alex', 'Martinez',
     'alex.martinez@pricklycactussoftware.com',
     'Software Engineer',
     'Engineering',
     '2023-03-15'),
    
    -- Rachel Green - Product Manager (reports to Sarah)
    ('00000000-0000-1000-0000-000000000007',
     '11111111-1111-1111-1111-111111111111',
     'a0000000-0000-0000-0000-000000000007',
     'a0000000-0000-0000-0000-000000000001',
     'Rachel', 'Green',
     'rachel.green@pricklycactussoftware.com',
     'Senior Product Manager',
     'Product',
     '2021-09-01'),
    
    -- Michael Brown - Designer (reports to Rachel)
    ('00000000-0000-1000-0000-000000000008',
     '11111111-1111-1111-1111-111111111111',
     'a0000000-0000-0000-0000-000000000008',
     'a0000000-0000-0000-0000-000000000007',
     'Michael', 'Brown',
     'michael.brown@pricklycactussoftware.com',
     'Senior Product Designer',
     'Product',
     '2022-04-01');

-- ============================================================================
-- ASSIGN ROLES TO USERS
-- ============================================================================
-- Roles are GLOBAL (not per-organization), so we just reference role by name
INSERT INTO user_roles (user_id, role_id, organization_id, assigned_by) 
SELECT 
    u.id,
    r.id,
    '11111111-1111-1111-1111-111111111111',
    'b0000000-0000-0000-0000-000000000000'  -- Brian assigns all roles
FROM users u
CROSS JOIN roles r
WHERE (
    -- Brian is Admin (PRIMARY TEST USER)
    (u.email = 'brian@pricklycactussoftware.com' AND r.name = 'admin')
    -- Sarah is Admin
    OR (u.email = 'sarah.chen@pricklycactussoftware.com' AND r.name = 'admin')
    -- Marcus is Manager
    OR (u.email = 'marcus.johnson@pricklycactussoftware.com' AND r.name = 'manager')
    -- Emily is Manager
    OR (u.email = 'emily.rodriguez@pricklycactussoftware.com' AND r.name = 'manager')
    -- David is Team Lead
    OR (u.email = 'david.kim@pricklycactussoftware.com' AND r.name = 'team_lead')
    -- Jessica is Member
    OR (u.email = 'jessica.thompson@pricklycactussoftware.com' AND r.name = 'member')
    -- Alex is Member
    OR (u.email = 'alex.martinez@pricklycactussoftware.com' AND r.name = 'member')
    -- Rachel is Manager
    OR (u.email = 'rachel.green@pricklycactussoftware.com' AND r.name = 'manager')
    -- Michael is Member
    OR (u.email = 'michael.brown@pricklycactussoftware.com' AND r.name = 'member')
);

-- ============================================================================
-- TEAM MEMBERSHIPS
-- ============================================================================
INSERT INTO team_memberships (team_id, team_member_id, is_lead) VALUES
    -- Engineering team
    ('00000000-0000-2000-0000-000000000001', '00000000-0000-1000-0000-000000000002', true),     -- Marcus (lead)
    ('00000000-0000-2000-0000-000000000001', '00000000-0000-1000-0000-000000000003', false),    -- Emily
    ('00000000-0000-2000-0000-000000000001', '00000000-0000-1000-0000-000000000004', false),    -- David
    ('00000000-0000-2000-0000-000000000001', '00000000-0000-1000-0000-000000000005', false),    -- Jessica
    ('00000000-0000-2000-0000-000000000001', '00000000-0000-1000-0000-000000000006', false),    -- Alex
    
    -- Product team
    ('00000000-0000-2000-0000-000000000002', '00000000-0000-1000-0000-000000000007', true),     -- Rachel (lead)
    ('00000000-0000-2000-0000-000000000002', '00000000-0000-1000-0000-000000000008', false),    -- Michael
    
    -- Platform team (cross-functional)
    ('00000000-0000-2000-0000-000000000003', '00000000-0000-1000-0000-000000000004', true),     -- David (lead)
    ('00000000-0000-2000-0000-000000000003', '00000000-0000-1000-0000-000000000005', false);    -- Jessica

SELECT 'Test users and teams created successfully' AS status;

-- Show the org structure
SELECT 
    tm.job_title,
    tm.first_name || ' ' || tm.last_name AS display_name,
    tm.email,
    r.name as role,
    mgr.first_name || ' ' || mgr.last_name as reports_to
FROM team_members tm
LEFT JOIN team_members mgr ON mgr.linked_user_id = tm.manager_user_id
LEFT JOIN user_roles ur ON ur.user_id = tm.linked_user_id
LEFT JOIN roles r ON r.id = ur.role_id
WHERE tm.organization_id = '11111111-1111-1111-1111-111111111111'
ORDER BY tm.hire_date;
