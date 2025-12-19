-- ============================================================================
-- Migration: 002_add_square_billing_fields.sql
-- Description: Updates subscriptions table for Square integration and 
--              monthly/annual billing support
-- Date: 2024-12-17
-- 
-- Run this in Supabase SQL Editor: https://supabase.com/dashboard/project/YOUR_PROJECT/sql
-- ============================================================================

-- ============================================================================
-- STEP 1: Add new columns for Square and billing cadence
-- ============================================================================

-- Billing cadence (monthly or annual)
ALTER TABLE subscriptions 
ADD COLUMN IF NOT EXISTS billing_cadence TEXT DEFAULT 'monthly' 
CHECK (billing_cadence IN ('monthly', 'annual'));

COMMENT ON COLUMN subscriptions.billing_cadence IS 'Billing frequency: monthly or annual';

-- Square-specific fields (keeping Stripe fields for backward compatibility during migration)
ALTER TABLE subscriptions 
ADD COLUMN IF NOT EXISTS square_customer_id TEXT;

ALTER TABLE subscriptions 
ADD COLUMN IF NOT EXISTS square_subscription_id TEXT;

ALTER TABLE subscriptions 
ADD COLUMN IF NOT EXISTS square_plan_variation_id TEXT;

ALTER TABLE subscriptions 
ADD COLUMN IF NOT EXISTS square_invoice_id TEXT;

COMMENT ON COLUMN subscriptions.square_customer_id IS 'Square customer ID for payment processing';
COMMENT ON COLUMN subscriptions.square_subscription_id IS 'Square subscription ID for recurring billing';
COMMENT ON COLUMN subscriptions.square_plan_variation_id IS 'Square plan variation ID (ties to specific price/cadence)';
COMMENT ON COLUMN subscriptions.square_invoice_id IS 'Most recent Square invoice ID';

-- ============================================================================
-- STEP 2: Add price tracking columns
-- ============================================================================

-- Store the actual price the customer is paying (in cents)
ALTER TABLE subscriptions 
ADD COLUMN IF NOT EXISTS price_cents INTEGER DEFAULT 0;

COMMENT ON COLUMN subscriptions.price_cents IS 'Current subscription price in cents (e.g., 999 = $9.99)';

-- Original price before any discounts (for display purposes)
ALTER TABLE subscriptions 
ADD COLUMN IF NOT EXISTS original_price_cents INTEGER DEFAULT 0;

COMMENT ON COLUMN subscriptions.original_price_cents IS 'Original price before discounts, in cents';

-- Discount percentage applied (e.g., 17 for annual discount)
ALTER TABLE subscriptions 
ADD COLUMN IF NOT EXISTS discount_percent INTEGER DEFAULT 0;

COMMENT ON COLUMN subscriptions.discount_percent IS 'Discount percentage applied (e.g., 17 for annual savings)';

-- ============================================================================
-- STEP 3: Add subscription lifecycle fields
-- ============================================================================

-- When the subscription was activated (first successful payment)
ALTER TABLE subscriptions 
ADD COLUMN IF NOT EXISTS activated_at TIMESTAMPTZ;

COMMENT ON COLUMN subscriptions.activated_at IS 'When subscription was first activated';

-- Payment failure tracking
ALTER TABLE subscriptions 
ADD COLUMN IF NOT EXISTS payment_failed_at TIMESTAMPTZ;

ALTER TABLE subscriptions 
ADD COLUMN IF NOT EXISTS payment_failure_count INTEGER DEFAULT 0;

COMMENT ON COLUMN subscriptions.payment_failed_at IS 'Last payment failure timestamp';
COMMENT ON COLUMN subscriptions.payment_failure_count IS 'Consecutive payment failure count';

-- Grace period end (how long after failed payment before downgrade)
ALTER TABLE subscriptions 
ADD COLUMN IF NOT EXISTS grace_period_end TIMESTAMPTZ;

COMMENT ON COLUMN subscriptions.grace_period_end IS 'End of grace period after payment failure';

-- ============================================================================
-- STEP 4: Add indexes for common queries
-- ============================================================================

-- Index for looking up by Square customer ID
CREATE INDEX IF NOT EXISTS idx_subscriptions_square_customer 
ON subscriptions(square_customer_id) 
WHERE square_customer_id IS NOT NULL;

-- Index for looking up by Square subscription ID
CREATE INDEX IF NOT EXISTS idx_subscriptions_square_subscription 
ON subscriptions(square_subscription_id) 
WHERE square_subscription_id IS NOT NULL;

-- Index for finding subscriptions needing renewal
CREATE INDEX IF NOT EXISTS idx_subscriptions_period_end 
ON subscriptions(current_period_end) 
WHERE status = 'active';

-- Index for finding failed payments in grace period
CREATE INDEX IF NOT EXISTS idx_subscriptions_grace_period 
ON subscriptions(grace_period_end) 
WHERE payment_failure_count > 0;

-- ============================================================================
-- STEP 5: Create subscription_plans reference table
-- ============================================================================

CREATE TABLE IF NOT EXISTS subscription_plans (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    tier TEXT NOT NULL CHECK (tier IN ('free', 'standard', 'pro', 'internal')),
    billing_cadence TEXT NOT NULL CHECK (billing_cadence IN ('monthly', 'annual')),
    price_cents INTEGER NOT NULL,
    original_price_cents INTEGER NOT NULL,
    discount_percent INTEGER DEFAULT 0,
    square_plan_id TEXT,
    square_plan_variation_id TEXT,
    features JSONB DEFAULT '[]'::jsonb,
    is_active BOOLEAN DEFAULT true,
    display_order INTEGER DEFAULT 0,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

COMMENT ON TABLE subscription_plans IS 'Available subscription plans and pricing';

-- Insert default plans (adjust prices as needed)
INSERT INTO subscription_plans (id, name, tier, billing_cadence, price_cents, original_price_cents, discount_percent, display_order, features)
VALUES 
    -- Free tier (always free)
    ('free', 'Free', 'free', 'monthly', 0, 0, 0, 0, 
     '["Up to 3 team members", "Basic 1:1 tracking", "Local database only"]'::jsonb),
    
    -- Standard tier
    ('standard_monthly', 'Standard Monthly', 'standard', 'monthly', 999, 999, 0, 10,
     '["Up to 10 team members", "Unlimited 1:1s & tasks", "Basic reports", "Calendar sync", "Email support"]'::jsonb),
    
    ('standard_annual', 'Standard Annual', 'standard', 'annual', 9999, 11988, 17, 11,
     '["Up to 10 team members", "Unlimited 1:1s & tasks", "Basic reports", "Calendar sync", "Email support", "2 months free"]'::jsonb),
    
    -- Pro tier
    ('pro_monthly', 'Pro Monthly', 'pro', 'monthly', 1999, 1999, 0, 20,
     '["Unlimited team members", "All Standard features", "Advanced reports", "AI Assistant", "AI Data Analysis", "Network database", "Priority support"]'::jsonb),
    
    ('pro_annual', 'Pro Annual', 'pro', 'annual', 19999, 23988, 17, 21,
     '["Unlimited team members", "All Standard features", "Advanced reports", "AI Assistant", "AI Data Analysis", "Network database", "Priority support", "2 months free"]'::jsonb),
    
    -- Internal tier (for beta testers, employees, etc.)
    ('internal', 'Internal', 'internal', 'monthly', 0, 0, 0, 100,
     '["Full access to all features", "Beta tester access", "Direct support channel"]'::jsonb)

ON CONFLICT (id) DO UPDATE SET
    name = EXCLUDED.name,
    price_cents = EXCLUDED.price_cents,
    original_price_cents = EXCLUDED.original_price_cents,
    discount_percent = EXCLUDED.discount_percent,
    features = EXCLUDED.features,
    updated_at = NOW();

-- ============================================================================
-- STEP 6: Create subscription_events audit table
-- ============================================================================

CREATE TABLE IF NOT EXISTS subscription_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    subscription_id UUID NOT NULL REFERENCES subscriptions(id),
    user_id UUID NOT NULL REFERENCES auth.users(id),
    event_type TEXT NOT NULL,
    event_data JSONB DEFAULT '{}'::jsonb,
    square_event_id TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

COMMENT ON TABLE subscription_events IS 'Audit log of all subscription changes';

-- Index for querying events by subscription
CREATE INDEX IF NOT EXISTS idx_subscription_events_subscription 
ON subscription_events(subscription_id, created_at DESC);

-- Note: subscription_id is UUID type to match subscriptions.id

-- Index for querying events by user
CREATE INDEX IF NOT EXISTS idx_subscription_events_user 
ON subscription_events(user_id, created_at DESC);

-- Event types we'll track:
-- 'created', 'activated', 'upgraded', 'downgraded', 'cancelled', 
-- 'payment_succeeded', 'payment_failed', 'renewed', 'expired'

-- ============================================================================
-- STEP 7: Create helper function for subscription status
-- ============================================================================

CREATE OR REPLACE FUNCTION get_subscription_status(sub subscriptions)
RETURNS TEXT AS $$
BEGIN
    -- Check various states in priority order
    IF sub.status = 'cancelled' THEN
        RETURN 'cancelled';
    END IF;
    
    IF sub.payment_failure_count > 0 AND sub.grace_period_end IS NOT NULL THEN
        IF sub.grace_period_end > NOW() THEN
            RETURN 'past_due';
        ELSE
            RETURN 'expired';
        END IF;
    END IF;
    
    IF sub.trial_end IS NOT NULL AND sub.trial_end > NOW() THEN
        RETURN 'trialing';
    END IF;
    
    IF sub.current_period_end IS NOT NULL AND sub.current_period_end < NOW() THEN
        RETURN 'expired';
    END IF;
    
    IF sub.status = 'active' THEN
        RETURN 'active';
    END IF;
    
    RETURN sub.status;
END;
$$ LANGUAGE plpgsql IMMUTABLE;

-- ============================================================================
-- STEP 8: Create RLS policies for new tables
-- ============================================================================

-- subscription_plans is public read (anyone can see pricing)
ALTER TABLE subscription_plans ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Anyone can view active plans" ON subscription_plans
    FOR SELECT USING (is_active = true);

-- subscription_events restricted to own events
ALTER TABLE subscription_events ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users can view own subscription events" ON subscription_events
    FOR SELECT USING (auth.uid() = user_id);

-- Service role can insert events (from webhooks)
CREATE POLICY "Service role can insert events" ON subscription_events
    FOR INSERT WITH CHECK (true);

-- ============================================================================
-- STEP 9: Update existing subscriptions to have billing_cadence
-- ============================================================================

-- Set all existing subscriptions to monthly (they can upgrade to annual later)
UPDATE subscriptions 
SET billing_cadence = 'monthly' 
WHERE billing_cadence IS NULL;

-- ============================================================================
-- VERIFICATION QUERIES (run these to verify migration worked)
-- ============================================================================

-- Check subscriptions table structure
-- SELECT column_name, data_type, is_nullable, column_default 
-- FROM information_schema.columns 
-- WHERE table_name = 'subscriptions' 
-- ORDER BY ordinal_position;

-- Check subscription_plans data
-- SELECT * FROM subscription_plans ORDER BY display_order;

-- Check indexes
-- SELECT indexname, indexdef 
-- FROM pg_indexes 
-- WHERE tablename IN ('subscriptions', 'subscription_plans', 'subscription_events');

-- ============================================================================
-- ROLLBACK (if needed - run these to undo changes)
-- ============================================================================

-- DROP TABLE IF EXISTS subscription_events;
-- DROP TABLE IF EXISTS subscription_plans;
-- DROP FUNCTION IF EXISTS get_subscription_status;
-- ALTER TABLE subscriptions DROP COLUMN IF EXISTS billing_cadence;
-- ALTER TABLE subscriptions DROP COLUMN IF EXISTS square_customer_id;
-- ALTER TABLE subscriptions DROP COLUMN IF EXISTS square_subscription_id;
-- ALTER TABLE subscriptions DROP COLUMN IF EXISTS square_plan_variation_id;
-- ALTER TABLE subscriptions DROP COLUMN IF EXISTS square_invoice_id;
-- ALTER TABLE subscriptions DROP COLUMN IF EXISTS price_cents;
-- ALTER TABLE subscriptions DROP COLUMN IF EXISTS original_price_cents;
-- ALTER TABLE subscriptions DROP COLUMN IF EXISTS discount_percent;
-- ALTER TABLE subscriptions DROP COLUMN IF EXISTS activated_at;
-- ALTER TABLE subscriptions DROP COLUMN IF EXISTS payment_failed_at;
-- ALTER TABLE subscriptions DROP COLUMN IF EXISTS payment_failure_count;
-- ALTER TABLE subscriptions DROP COLUMN IF EXISTS grace_period_end;

