ProCohere Onboarding Flow – Organization & Users

This document defines the canonical onboarding flow for ProCohere. It covers organization creation, licensing, user provisioning, seat assignment, and team hierarchy setup. All steps align with the current Supabase schema, triggers, and enforcement rules.

## 1. Organization Creation

Organizations should be created explicitly during admin onboarding. Implicit org creation via auth trigger should be avoided for internal users.

insert into public.organizations (
  name,
  slug,
  email,
  timezone
)
values (
  'Prickly Cactus Software',
  'prickly-cactus-software',
  'brian@pricklycactussoftware.com',
  'America/New_York'
);

## 2. Product License Provisioning

A product license must exist before any user seats are assigned.

insert into public.organization_products (
  organization_id,
  product_id,
  seat_count,
  status
)
values (
  :org_id,
  (select id from public.products where code = 'procohere'),
  10,
  'active'
);

## 3. Auth User Creation

Users are created in Supabase Auth. Metadata must include organization_id to prevent automatic organization creation.

{
  "email": "troy@pricklycactussoftware.com",
  "password": "temporary-password",
  "email_confirm": true,
  "user_metadata": {
    "organization_id": "ORG_UUID",
    "display_name": "Troy Polamalu",
    "first_name": "Troy",
    "last_name": "Polamalu"
  }
}

## 4. public.users Record

The public.users row is created automatically by the handle_new_user() trigger on auth.users insert. Manual inserts are not part of the normal flow.

## 5. Seat Assignment

Each active user consumes one seat per product.

insert into public.user_product_seats (
  user_id,
  product_id,
  role
)
values (
  :user_id,
  (select id from public.products where code = 'procohere'),
  'user'
);

## 6. Team Member Creation

Team members define in-app permissions and reporting relationships.

insert into procohere.team_members (
  organization_id,
  linked_user_id,
  role_id,
  manager_team_member_id,
  first_name,
  last_name,
  email,
  job_title
)
values (
  :org_id,
  :user_id,
  (select id from procohere.roles where name = 'Manager' and organization_id = :org_id),
  :manager_team_member_id,
  'Troy',
  'Polamalu',
  'troy@pricklycactussoftware.com',
  'Product Development Manager'
);

## 7. Verification Queries

select u.email, p.code, ups.is_active
from public.user_product_seats ups
join public.products p on p.id = ups.product_id
join public.users u on u.id = ups.user_id;

select
  tm.first_name,
  tm.last_name,
  mgr.first_name as manager_first_name,
  mgr.last_name as manager_last_name
from procohere.team_members tm
left join procohere.team_members mgr
  on mgr.id = tm.manager_team_member_id;

Summary

This onboarding flow guarantees consistent org ownership, license enforcement, seat tracking, and hierarchical access control.