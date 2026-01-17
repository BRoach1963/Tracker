-- =================================================================================================
-- PUBLIC SCHEMA SETUP (PRODUCTION) - v4
-- Last Updated: 2026-01-17 (v4 fixes)
-- =================================================================================================
-- Fixes from v1:
--   1) Seat limit enforcement counts active seats scoped to the user's organization (not global)
--      + excludes deleted/inactive users from the count.
--   2) User org immutability trigger uses IS DISTINCT FROM for NULL-safe comparison.
-- Documentation companion:
--   - PUBLIC_SCHEMA_TABLES_PRODUCTION_v2.md
-- =================================================================================================

BEGIN;

CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS citext;

-- ---------------------------------------------
-- Utility: updated_at trigger
-- ---------------------------------------------
CREATE OR REPLACE FUNCTION public.set_updated_at()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
  NEW.updated_at := now();
  RETURN NEW;
END;
$$;

-- ---------------------------------------------
-- Table: organizations
-- ---------------------------------------------
CREATE TABLE IF NOT EXISTS public.organizations
(
  id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  name              text NOT NULL,
  slug              text NOT NULL,

  email             text,
  phone             text,
  website           text,

  timezone          text NOT NULL DEFAULT 'America/New_York',
  logo_url          text,

  billing_email       text,
  billing_name        text,
  billing_phone       text,
  billing_address     jsonb,
  billing_provider    text,
  billing_customer_id text,
  tax_id              text,
  tax_exempt          boolean NOT NULL DEFAULT false,
  default_currency    text NOT NULL DEFAULT 'USD',

  created_at        timestamptz NOT NULL DEFAULT now(),
  updated_at        timestamptz NOT NULL DEFAULT now(),
  is_deleted        boolean NOT NULL DEFAULT false,
  deleted_at        timestamptz,
  deleted_by        uuid
);

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_organizations_slug_not_blank') THEN
    ALTER TABLE public.organizations
      ADD CONSTRAINT ck_organizations_slug_not_blank CHECK (length(trim(slug)) > 0);
  END IF;
END;
$$;

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'uq_organizations_slug') THEN
    ALTER TABLE public.organizations
      ADD CONSTRAINT uq_organizations_slug UNIQUE (slug);
  END IF;
END;
$$;

CREATE INDEX IF NOT EXISTS idx_organizations_name_not_deleted
  ON public.organizations (name)
  WHERE NOT is_deleted;

CREATE INDEX IF NOT EXISTS idx_organizations_billing_customer
  ON public.organizations (billing_customer_id)
  WHERE billing_customer_id IS NOT NULL AND NOT is_deleted;

CREATE TRIGGER tr_organizations_set_updated_at
BEFORE UPDATE ON public.organizations
FOR EACH ROW
EXECUTE FUNCTION public.set_updated_at();

-- ---------------------------------------------
-- Table: products
-- ---------------------------------------------
CREATE TABLE IF NOT EXISTS public.products
(
  id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  code        text NOT NULL,
  name        text NOT NULL,
  description text,
  icon_url    text,
  color_hex   text,
  is_active   boolean NOT NULL DEFAULT true,
  created_at  timestamptz NOT NULL DEFAULT now(),
  updated_at  timestamptz NOT NULL DEFAULT now()
);

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'uq_products_code') THEN
    ALTER TABLE public.products
      ADD CONSTRAINT uq_products_code UNIQUE (code);
  END IF;
END;
$$;

CREATE INDEX IF NOT EXISTS idx_products_code ON public.products (code);

CREATE TRIGGER tr_products_set_updated_at
BEFORE UPDATE ON public.products
FOR EACH ROW
EXECUTE FUNCTION public.set_updated_at();

INSERT INTO public.products (code, name, description, color_hex)
VALUES
  ('procohere', 'ProCohere', 'Team relationship management for managers', '#6366F1'),
  ('procausa', 'ProCausa', 'Case management for legal professionals', '#0EA5E9'),
  ('threadline', 'Threadline', 'Therapy practice management', '#10B981'),
  ('procliente', 'ProCliente', 'Non-profit client management', '#F59E0B')
ON CONFLICT (code) DO UPDATE
SET
  name = EXCLUDED.name,
  description = EXCLUDED.description,
  color_hex = EXCLUDED.color_hex,
  updated_at = now();

-- ---------------------------------------------
-- Table: organization_products
-- ---------------------------------------------
CREATE TABLE IF NOT EXISTS public.organization_products
(
  id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id       uuid NOT NULL REFERENCES public.organizations(id),
  product_id            uuid NOT NULL REFERENCES public.products(id),

  seat_count            integer NOT NULL DEFAULT 1,

  status                text NOT NULL DEFAULT 'active',
  current_period_start  timestamptz,
  current_period_end    timestamptz,

  cancel_at_period_end  boolean NOT NULL DEFAULT false,
  canceled_at           timestamptz,

  trial_start           timestamptz,
  trial_end             timestamptz,

  billing_interval      text,
  unit_price_cents      integer,
  currency              text NOT NULL DEFAULT 'USD',

  stripe_subscription_id text,
  stripe_customer_id     text,
  stripe_price_id        text,
  stripe_product_id      text,

  metadata              jsonb NOT NULL DEFAULT '{}'::jsonb,

  is_active             boolean NOT NULL DEFAULT true,
  created_at            timestamptz NOT NULL DEFAULT now(),
  updated_at            timestamptz NOT NULL DEFAULT now(),
  is_deleted            boolean NOT NULL DEFAULT false,
  deleted_at            timestamptz,
  deleted_by            uuid
);

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'uq_org_products_org_product') THEN
    ALTER TABLE public.organization_products
      ADD CONSTRAINT uq_org_products_org_product UNIQUE (organization_id, product_id);
  END IF;
END;
$$;

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_org_products_status') THEN
    ALTER TABLE public.organization_products
      ADD CONSTRAINT ck_org_products_status CHECK (
        status IN ('trialing','active','past_due','canceled','incomplete','incomplete_expired','unpaid','paused')
      );
  END IF;
END;
$$;

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_org_products_interval') THEN
    ALTER TABLE public.organization_products
      ADD CONSTRAINT ck_org_products_interval CHECK (
        billing_interval IS NULL OR billing_interval IN ('month','year')
      );
  END IF;
END;
$$;

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_org_products_seat_count') THEN
    ALTER TABLE public.organization_products
      ADD CONSTRAINT ck_org_products_seat_count CHECK (seat_count >= 0);
  END IF;
END;
$$;

CREATE INDEX IF NOT EXISTS idx_org_products_org_active
  ON public.organization_products (organization_id, product_id)
  WHERE is_active AND NOT is_deleted;

CREATE INDEX IF NOT EXISTS idx_org_products_product
  ON public.organization_products (product_id)
  WHERE NOT is_deleted;

CREATE UNIQUE INDEX IF NOT EXISTS uq_org_products_stripe_subscription
  ON public.organization_products (stripe_subscription_id)
  WHERE stripe_subscription_id IS NOT NULL AND NOT is_deleted;

CREATE INDEX IF NOT EXISTS idx_org_products_stripe_customer
  ON public.organization_products (stripe_customer_id)
  WHERE stripe_customer_id IS NOT NULL AND NOT is_deleted;

CREATE TRIGGER tr_org_products_set_updated_at
BEFORE UPDATE ON public.organization_products
FOR EACH ROW
EXECUTE FUNCTION public.set_updated_at();

-- ---------------------------------------------
-- Table: users
-- ---------------------------------------------
CREATE TABLE IF NOT EXISTS public.users
(
  id              uuid PRIMARY KEY,
  organization_id uuid NOT NULL REFERENCES public.organizations(id),

  -- Identity
  email           citext NOT NULL,
  display_name    text,
  first_name      text,
  last_name       text,

  -- Profile
  job_title       text,
  company         text,
  avatar_url      text,
  phone           text,
  timezone        text NOT NULL DEFAULT 'America/New_York',

  -- Settings (JSONB for flexibility)
  preferences           jsonb NOT NULL DEFAULT '{}'::jsonb,
  notification_settings jsonb NOT NULL DEFAULT '{}'::jsonb,

  -- Status
  is_active         boolean NOT NULL DEFAULT true,
  is_email_verified boolean NOT NULL DEFAULT false,
  last_login_at     timestamptz,

  -- Audit
  created_at      timestamptz NOT NULL DEFAULT now(),
  updated_at      timestamptz NOT NULL DEFAULT now(),
  is_deleted      boolean NOT NULL DEFAULT false,
  deleted_at      timestamptz,
  deleted_by      uuid
);


CREATE UNIQUE INDEX IF NOT EXISTS uq_users_email_active
  ON public.users (lower(email))
  WHERE NOT is_deleted;

CREATE INDEX IF NOT EXISTS idx_users_org
  ON public.users (organization_id)
  WHERE NOT is_deleted;


-- Additive migration: ensure profile and settings columns exist (idempotent)
ALTER TABLE public.users
  ADD COLUMN IF NOT EXISTS first_name text,
  ADD COLUMN IF NOT EXISTS last_name text,
  ADD COLUMN IF NOT EXISTS job_title text,
  ADD COLUMN IF NOT EXISTS company text,
  ADD COLUMN IF NOT EXISTS preferences jsonb NOT NULL DEFAULT '{}'::jsonb,
  ADD COLUMN IF NOT EXISTS notification_settings jsonb NOT NULL DEFAULT '{}'::jsonb,
  ADD COLUMN IF NOT EXISTS is_email_verified boolean NOT NULL DEFAULT false,
  ADD COLUMN IF NOT EXISTS last_login_at timestamptz;

CREATE TRIGGER tr_users_set_updated_at
BEFORE UPDATE ON public.users
FOR EACH ROW
EXECUTE FUNCTION public.set_updated_at();

CREATE OR REPLACE FUNCTION public.block_user_org_change()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
  IF NEW.organization_id IS DISTINCT FROM OLD.organization_id THEN
    RAISE EXCEPTION 'organization_id cannot be modified';
  END IF;
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS tr_users_block_org_change ON public.users;

CREATE TRIGGER tr_users_block_org_change
BEFORE UPDATE ON public.users
FOR EACH ROW
EXECUTE FUNCTION public.block_user_org_change();

-- ---------------------------------------------
-- Table: user_product_seats
-- ---------------------------------------------
CREATE TABLE IF NOT EXISTS public.user_product_seats
(
  id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id     uuid NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
  product_id  uuid NOT NULL REFERENCES public.products(id),

  role        text NOT NULL DEFAULT 'user',
  is_active   boolean NOT NULL DEFAULT true,

  granted_at  timestamptz NOT NULL DEFAULT now(),
  granted_by  uuid REFERENCES public.users(id),

  revoked_at  timestamptz,
  revoked_by  uuid REFERENCES public.users(id),

  created_at  timestamptz NOT NULL DEFAULT now(),
  updated_at  timestamptz NOT NULL DEFAULT now(),

  CONSTRAINT uq_user_product UNIQUE (user_id, product_id),
  CONSTRAINT ck_seat_role CHECK (role IN ('admin','user','viewer'))
);

CREATE INDEX IF NOT EXISTS idx_seats_user_active
  ON public.user_product_seats (user_id, product_id)
  WHERE is_active = true;

CREATE INDEX IF NOT EXISTS idx_seats_product_active
  ON public.user_product_seats (product_id)
  WHERE is_active = true;

CREATE TRIGGER tr_user_product_seats_set_updated_at
BEFORE UPDATE ON public.user_product_seats
FOR EACH ROW
EXECUTE FUNCTION public.set_updated_at();

-- ---------------------------------------------
-- Table: organization_billing_events
-- ---------------------------------------------
CREATE TABLE IF NOT EXISTS public.organization_billing_events
(
  id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id   uuid NOT NULL REFERENCES public.organizations(id),
  product_id        uuid REFERENCES public.products(id),

  provider          text NOT NULL DEFAULT 'stripe',
  provider_event_id text NOT NULL,
  event_type        text NOT NULL,

  occurred_at       timestamptz NOT NULL DEFAULT now(),
  payload           jsonb NOT NULL DEFAULT '{}'::jsonb,

  processed_at      timestamptz,
  processing_error  text,

  created_at        timestamptz NOT NULL DEFAULT now()
);

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'uq_billing_events_provider_event') THEN
    ALTER TABLE public.organization_billing_events
      ADD CONSTRAINT uq_billing_events_provider_event UNIQUE (provider, provider_event_id);
  END IF;
END;
$$;

CREATE INDEX IF NOT EXISTS idx_billing_events_org_time
  ON public.organization_billing_events (organization_id, occurred_at DESC);

CREATE INDEX IF NOT EXISTS idx_billing_events_event_type
  ON public.organization_billing_events (event_type, occurred_at DESC);

-- ---------------------------------------------
-- Helper Functions
-- ---------------------------------------------
CREATE OR REPLACE FUNCTION public.get_user_organization_id()
RETURNS uuid
LANGUAGE sql
SECURITY DEFINER
STABLE
SET search_path = public
AS $$
  SELECT u.organization_id
  FROM public.users u
  WHERE u.id = auth.uid()
    AND NOT u.is_deleted
    AND u.is_active = true;
$$;

CREATE OR REPLACE FUNCTION public.get_user_product_role(product_code text)
RETURNS text
LANGUAGE sql
SECURITY DEFINER
STABLE
SET search_path = public
AS $$
  SELECT ups.role
  FROM public.user_product_seats ups
  JOIN public.products p ON p.id = ups.product_id
  WHERE ups.user_id = auth.uid()
    AND p.code = product_code
    AND ups.is_active = true;
$$;

CREATE OR REPLACE FUNCTION public.user_has_product_seat(product_code text)
RETURNS boolean
LANGUAGE sql
SECURITY DEFINER
STABLE
SET search_path = public
AS $$
  SELECT EXISTS (
    SELECT 1
    FROM public.user_product_seats ups
    JOIN public.products p ON p.id = ups.product_id
    WHERE ups.user_id = auth.uid()
      AND p.code = product_code
      AND ups.is_active = true
  );
$$;

CREATE OR REPLACE FUNCTION public.org_has_active_product_license(product_code text)
RETURNS boolean
LANGUAGE sql
SECURITY DEFINER
STABLE
SET search_path = public
AS $$
  SELECT EXISTS (
    SELECT 1
    FROM public.organization_products op
    JOIN public.products p ON p.id = op.product_id
    WHERE op.organization_id = public.get_user_organization_id()
      AND p.code = product_code
      AND op.is_active = true
      AND op.is_deleted = false
      AND op.status IN ('trialing','active')
      AND (op.current_period_end IS NULL OR op.current_period_end > now())
  );
$$;

CREATE OR REPLACE FUNCTION public.user_has_active_product_access(product_code text)
RETURNS boolean
LANGUAGE sql
SECURITY DEFINER
STABLE
SET search_path = public
AS $$
  SELECT public.user_has_product_seat(product_code)
     AND public.org_has_active_product_license(product_code);
$$;

-- ---------------------------------------------
-- Seat Limit Enforcement (org-scoped)
-- ---------------------------------------------
CREATE OR REPLACE FUNCTION public.enforce_seat_limit()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_org_id uuid;
  v_seat_limit integer;
  v_active_count integer;
  v_license_id uuid;
  v_is_activation boolean;
BEGIN
  SELECT u.organization_id INTO v_org_id
  FROM public.users u
  WHERE u.id = NEW.user_id
    AND NOT u.is_deleted
    AND u.is_active = true;

  IF v_org_id IS NULL THEN
    RAISE EXCEPTION 'Seat user is invalid or inactive';
  END IF;

  v_is_activation := NEW.is_active = true AND (TG_OP = 'INSERT' OR OLD.is_active = false);

  IF NOT v_is_activation THEN
    RETURN NEW;
  END IF;

  SELECT op.id, op.seat_count INTO v_license_id, v_seat_limit
  FROM public.organization_products op
  WHERE op.organization_id = v_org_id
    AND op.product_id = NEW.product_id
    AND op.is_active = true
    AND op.is_deleted = false
    AND op.status IN ('trialing','active')
    AND (op.current_period_end IS NULL OR op.current_period_end > now())
  FOR UPDATE;

  IF v_license_id IS NULL THEN
    RAISE EXCEPTION 'Organization does not have an active license for this product';
  END IF;

  SELECT COUNT(*) INTO v_active_count
  FROM public.user_product_seats ups
  JOIN public.users u ON u.id = ups.user_id
  WHERE u.organization_id = v_org_id
    AND u.is_active = true
    AND u.is_deleted = false
    AND ups.product_id = NEW.product_id
    AND ups.is_active = true
    AND (TG_OP <> 'UPDATE' OR ups.id <> NEW.id);

  IF v_active_count >= v_seat_limit THEN
    RAISE EXCEPTION 'Seat limit exceeded for this product';
  END IF;

  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS tr_user_product_seats_enforce_seat_limit ON public.user_product_seats;

CREATE TRIGGER tr_user_product_seats_enforce_seat_limit
BEFORE INSERT OR UPDATE OF is_active, product_id, user_id
ON public.user_product_seats
FOR EACH ROW
EXECUTE FUNCTION public.enforce_seat_limit();

-- ---------------------------------------------
-- Signup Flow: Create user row on auth signup
-- ---------------------------------------------
CREATE OR REPLACE FUNCTION public.create_default_organization(user_email text)
RETURNS uuid
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_new_org_id uuid;
  v_domain text;
  v_slug text;
BEGIN
  v_domain := split_part(user_email, '@', 2);
  v_slug := regexp_replace(lower(v_domain), '[^a-z0-9\-]+', '-', 'g') || '-' || substr(gen_random_uuid()::text, 1, 8);

  INSERT INTO public.organizations (name, slug, email)
  VALUES (v_domain, v_slug, user_email)
  RETURNING id INTO v_new_org_id;

  RETURN v_new_org_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_org_id uuid;
  v_display text;
BEGIN
  v_display := COALESCE(NEW.raw_user_meta_data->>'display_name', split_part(NEW.email, '@', 1));

  v_org_id := NULLIF(NEW.raw_user_meta_data->>'organization_id', '')::uuid;

  IF v_org_id IS NULL THEN
    v_org_id := public.create_default_organization(NEW.email);
  END IF;

  INSERT INTO public.users
  (
    id,
    email,
    display_name,
    organization_id,
    first_name,
    last_name,
    job_title,
    company,
    preferences,
    notification_settings
  )
  VALUES
  (
    NEW.id,
    NEW.email,
    v_display,
    v_org_id,
    NULLIF(NEW.raw_user_meta_data->>'first_name', ''),
    NULLIF(NEW.raw_user_meta_data->>'last_name', ''),
    NULLIF(NEW.raw_user_meta_data->>'job_title', ''),
    NULLIF(NEW.raw_user_meta_data->>'company', ''),
    COALESCE((NEW.raw_user_meta_data->'preferences')::jsonb, '{}'::jsonb),
    COALESCE((NEW.raw_user_meta_data->'notification_settings')::jsonb, '{}'::jsonb)
  )
  ON CONFLICT (id) DO NOTHING;

  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS on_auth_user_created ON auth.users;

CREATE TRIGGER on_auth_user_created
AFTER INSERT ON auth.users
FOR EACH ROW
EXECUTE FUNCTION public.handle_new_user();

-- ---------------------------------------------
-- RLS Policies (Phase 1)
-- ---------------------------------------------
ALTER TABLE public.organizations ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS org_select_own ON public.organizations;
CREATE POLICY org_select_own
ON public.organizations
FOR SELECT
TO authenticated
USING (id = public.get_user_organization_id());

ALTER TABLE public.products ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS products_select_all ON public.products;
CREATE POLICY products_select_all
ON public.products
FOR SELECT
TO anon, authenticated
USING (true);

ALTER TABLE public.organization_products ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS org_products_select_own_org ON public.organization_products;
CREATE POLICY org_products_select_own_org
ON public.organization_products
FOR SELECT
TO authenticated
USING (organization_id = public.get_user_organization_id() AND is_deleted = false);

ALTER TABLE public.users ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS users_select_self ON public.users;
CREATE POLICY users_select_self
ON public.users
FOR SELECT
TO authenticated
USING (id = auth.uid() AND NOT is_deleted);

DROP POLICY IF EXISTS users_select_same_org ON public.users;
CREATE POLICY users_select_same_org
ON public.users
FOR SELECT
TO authenticated
USING (organization_id = public.get_user_organization_id() AND NOT is_deleted);

DROP POLICY IF EXISTS users_update_self_safe ON public.users;
CREATE POLICY users_update_self_safe
ON public.users
FOR UPDATE
TO authenticated
USING (id = auth.uid() AND NOT is_deleted)
WITH CHECK (id = auth.uid() AND NOT is_deleted);

ALTER TABLE public.user_product_seats ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS seats_select_self ON public.user_product_seats;
CREATE POLICY seats_select_self
ON public.user_product_seats
FOR SELECT
TO authenticated
USING (user_id = auth.uid());

DROP POLICY IF EXISTS seats_select_org ON public.user_product_seats;
CREATE POLICY seats_select_org
ON public.user_product_seats
FOR SELECT
TO authenticated
USING (
  EXISTS (
    SELECT 1
    FROM public.users u
    WHERE u.id = user_product_seats.user_id
      AND u.organization_id = public.get_user_organization_id()
      AND NOT u.is_deleted
      AND u.is_active = true
  )
);

ALTER TABLE public.organization_billing_events ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS billing_events_select_own_org ON public.organization_billing_events;
CREATE POLICY billing_events_select_own_org
ON public.organization_billing_events
FOR SELECT
TO authenticated
USING (organization_id = public.get_user_organization_id());

-- ---------------------------------------------
-- GRANTS
-- ---------------------------------------------
REVOKE ALL ON public.organizations FROM anon, authenticated;
REVOKE ALL ON public.products FROM anon, authenticated;
REVOKE ALL ON public.organization_products FROM anon, authenticated;
REVOKE ALL ON public.users FROM anon, authenticated;
REVOKE ALL ON public.user_product_seats FROM anon, authenticated;
REVOKE ALL ON public.organization_billing_events FROM anon, authenticated;

GRANT SELECT ON public.products TO anon, authenticated;

GRANT SELECT ON public.organizations TO authenticated;
GRANT SELECT ON public.organization_products TO authenticated;
GRANT SELECT ON public.users TO authenticated;
GRANT SELECT ON public.user_product_seats TO authenticated;
GRANT SELECT ON public.organization_billing_events TO authenticated;

GRANT UPDATE (display_name, avatar_url, phone, timezone, first_name, last_name, job_title, company, preferences, notification_settings) ON public.users TO authenticated;

COMMIT;
