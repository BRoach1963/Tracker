// Supabase Edge Function: Square Webhook Handler
// Receives payment events from Square and updates subscriptions

import { serve } from "https://deno.land/std@0.168.0/http/server.ts"
import { createClient } from "https://esm.sh/@supabase/supabase-js@2"
import { createHmac } from "https://deno.land/std@0.168.0/crypto/mod.ts"

const corsHeaders = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Headers': 'authorization, x-client-info, apikey, content-type, x-square-hmacsha256-signature',
}

interface SquareWebhookEvent {
  merchant_id: string
  type: string
  event_id: string
  created_at: string
  data: {
    type: string
    id: string
    object: Record<string, any>
  }
}

serve(async (req) => {
  // Handle CORS preflight
  if (req.method === 'OPTIONS') {
    return new Response('ok', { headers: corsHeaders })
  }

  try {
    const supabaseUrl = Deno.env.get('SUPABASE_URL')!
    const supabaseKey = Deno.env.get('SUPABASE_SERVICE_ROLE_KEY')!
    const supabase = createClient(supabaseUrl, supabaseKey)

    // Get the raw body for signature verification
    const body = await req.text()
    const event: SquareWebhookEvent = JSON.parse(body)

    // Optional: Verify webhook signature
    const signature = req.headers.get('x-square-hmacsha256-signature')
    const webhookSignatureKey = Deno.env.get('SQUARE_WEBHOOK_SIGNATURE_KEY')
    
    if (webhookSignatureKey && signature) {
      // Verify signature (implement if needed for production)
      // const expectedSignature = createHmac('sha256', webhookSignatureKey).update(body).digest('base64')
      // if (signature !== expectedSignature) {
      //   return new Response('Invalid signature', { status: 401 })
      // }
    }

    console.log(`Received Square webhook: ${event.type}`, event.event_id)

    // Handle different event types
    switch (event.type) {
      case 'subscription.created':
        await handleSubscriptionCreated(supabase, event.data.object)
        break

      case 'subscription.updated':
        await handleSubscriptionUpdated(supabase, event.data.object)
        break

      case 'subscription.canceled':
        await handleSubscriptionCanceled(supabase, event.data.object)
        break

      case 'invoice.payment_made':
        await handlePaymentSuccess(supabase, event.data.object)
        break

      case 'invoice.payment_failed':
        await handlePaymentFailed(supabase, event.data.object)
        break

      default:
        console.log(`Unhandled event type: ${event.type}`)
    }

    // Log the event
    await supabase.from('subscription_events').insert({
      subscription_id: event.data.object.subscription_id || event.data.id,
      user_id: null, // Will be filled by trigger if needed
      event_type: event.type,
      event_data: event.data,
      square_event_id: event.event_id,
    })

    return new Response(
      JSON.stringify({ received: true }),
      { headers: { ...corsHeaders, 'Content-Type': 'application/json' } }
    )

  } catch (error) {
    console.error('Webhook error:', error)
    return new Response(
      JSON.stringify({ error: error.message }),
      { status: 500, headers: { ...corsHeaders, 'Content-Type': 'application/json' } }
    )
  }
})

async function handleSubscriptionCreated(supabase: any, subscription: any) {
  console.log('Subscription created:', subscription.id)

  // Find user by Square customer ID
  const { data: existingSub } = await supabase
    .from('subscriptions')
    .select('id, user_id')
    .eq('square_customer_id', subscription.customer_id)
    .single()

  if (existingSub) {
    // Determine tier from plan
    const tier = determineTierFromPlan(subscription.plan_variation_id)
    const cadence = determineCadenceFromPlan(subscription)

    await supabase
      .from('subscriptions')
      .update({
        tier: tier,
        status: 'active',
        billing_cadence: cadence,
        square_subscription_id: subscription.id,
        current_period_start: subscription.start_date,
        current_period_end: subscription.charged_through_date,
        activated_at: new Date().toISOString(),
        updated_at: new Date().toISOString(),
      })
      .eq('id', existingSub.id)

    console.log(`Updated subscription for user ${existingSub.user_id} to ${tier}`)
  }
}

async function handleSubscriptionUpdated(supabase: any, subscription: any) {
  console.log('Subscription updated:', subscription.id)

  const { data: existingSub } = await supabase
    .from('subscriptions')
    .select('id')
    .eq('square_subscription_id', subscription.id)
    .single()

  if (existingSub) {
    await supabase
      .from('subscriptions')
      .update({
        status: subscription.status?.toLowerCase() || 'active',
        current_period_end: subscription.charged_through_date,
        updated_at: new Date().toISOString(),
      })
      .eq('id', existingSub.id)
  }
}

async function handleSubscriptionCanceled(supabase: any, subscription: any) {
  console.log('Subscription canceled:', subscription.id)

  const { data: existingSub } = await supabase
    .from('subscriptions')
    .select('id')
    .eq('square_subscription_id', subscription.id)
    .single()

  if (existingSub) {
    await supabase
      .from('subscriptions')
      .update({
        status: 'cancelled',
        canceled_at: new Date().toISOString(),
        updated_at: new Date().toISOString(),
      })
      .eq('id', existingSub.id)
  }
}

async function handlePaymentSuccess(supabase: any, invoice: any) {
  console.log('Payment successful for invoice:', invoice.id)

  if (invoice.subscription_id) {
    const { data: existingSub } = await supabase
      .from('subscriptions')
      .select('id')
      .eq('square_subscription_id', invoice.subscription_id)
      .single()

    if (existingSub) {
      await supabase
        .from('subscriptions')
        .update({
          status: 'active',
          payment_failure_count: 0,
          payment_failed_at: null,
          grace_period_end: null,
          square_invoice_id: invoice.id,
          updated_at: new Date().toISOString(),
        })
        .eq('id', existingSub.id)
    }
  }
}

async function handlePaymentFailed(supabase: any, invoice: any) {
  console.log('Payment failed for invoice:', invoice.id)

  if (invoice.subscription_id) {
    const { data: existingSub } = await supabase
      .from('subscriptions')
      .select('id, payment_failure_count')
      .eq('square_subscription_id', invoice.subscription_id)
      .single()

    if (existingSub) {
      const failureCount = (existingSub.payment_failure_count || 0) + 1
      const gracePeriodDays = 7 // Give 7 days to fix payment

      await supabase
        .from('subscriptions')
        .update({
          status: 'past_due',
          payment_failure_count: failureCount,
          payment_failed_at: new Date().toISOString(),
          grace_period_end: new Date(Date.now() + gracePeriodDays * 24 * 60 * 60 * 1000).toISOString(),
          updated_at: new Date().toISOString(),
        })
        .eq('id', existingSub.id)
    }
  }
}

function determineTierFromPlan(planVariationId: string): string {
  const proPlanId = Deno.env.get('SQUARE_PLAN_ID_PRO')
  const standardPlanId = Deno.env.get('SQUARE_PLAN_ID_STANDARD')

  if (planVariationId === proPlanId) return 'pro'
  if (planVariationId === standardPlanId) return 'standard'
  return 'free'
}

function determineCadenceFromPlan(subscription: any): string {
  // Check the billing cadence from subscription phases or intervals
  const phases = subscription.phases || []
  if (phases.length > 0) {
    const period = phases[0].cadence
    if (period === 'ANNUAL' || period === 'YEARLY') return 'annual'
  }
  return 'monthly'
}


