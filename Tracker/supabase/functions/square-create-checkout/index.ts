// Supabase Edge Function: Create Square Checkout Session
// This function creates a checkout link for subscription purchases

import { serve } from "https://deno.land/std@0.168.0/http/server.ts"
import { createClient } from "https://esm.sh/@supabase/supabase-js@2"

const corsHeaders = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Headers': 'authorization, x-client-info, apikey, content-type',
}

interface CheckoutRequest {
  plan_id: string      // 'standard_monthly', 'standard_annual', 'pro_monthly', 'pro_annual'
  user_id: string      // Supabase user ID
  return_url?: string  // URL to redirect after checkout
}

serve(async (req) => {
  // Handle CORS preflight
  if (req.method === 'OPTIONS') {
    return new Response('ok', { headers: corsHeaders })
  }

  try {
    // Get secrets from environment
    const environment = Deno.env.get('SQUARE_ENVIRONMENT') || 'sandbox'
    const accessToken = environment === 'production' 
      ? Deno.env.get('SQUARE_PRODUCTION_ACCESS_TOKEN')
      : Deno.env.get('SQUARE_SANDBOX_ACCESS_TOKEN')
    
    const planIdPro = Deno.env.get('SQUARE_PLAN_ID_PRO')
    const planIdStandard = Deno.env.get('SQUARE_PLAN_ID_STANDARD')

    if (!accessToken) {
      throw new Error('Square access token not configured')
    }

    // Parse request body
    const { plan_id, user_id, return_url } = await req.json() as CheckoutRequest

    if (!plan_id || !user_id) {
      return new Response(
        JSON.stringify({ error: 'Missing required fields: plan_id, user_id' }),
        { status: 400, headers: { ...corsHeaders, 'Content-Type': 'application/json' } }
      )
    }

    // Map plan_id to Square plan
    const planMapping: Record<string, { squarePlanId: string, cadence: string }> = {
      'standard_monthly': { squarePlanId: planIdStandard!, cadence: 'MONTHLY' },
      'standard_annual': { squarePlanId: planIdStandard!, cadence: 'ANNUAL' },
      'pro_monthly': { squarePlanId: planIdPro!, cadence: 'MONTHLY' },
      'pro_annual': { squarePlanId: planIdPro!, cadence: 'ANNUAL' },
    }

    const planConfig = planMapping[plan_id]
    if (!planConfig) {
      return new Response(
        JSON.stringify({ error: 'Invalid plan_id' }),
        { status: 400, headers: { ...corsHeaders, 'Content-Type': 'application/json' } }
      )
    }

    // Square API base URL
    const squareBaseUrl = environment === 'production'
      ? 'https://connect.squareup.com'
      : 'https://connect.squareupsandbox.com'

    // First, create or get customer in Square
    const supabaseUrl = Deno.env.get('SUPABASE_URL')!
    const supabaseKey = Deno.env.get('SUPABASE_SERVICE_ROLE_KEY')!
    const supabase = createClient(supabaseUrl, supabaseKey)

    // Get user profile from Supabase
    const { data: profile } = await supabase
      .from('profiles')
      .select('email, display_name, first_name, last_name')
      .eq('id', user_id)
      .single()

    // Create/retrieve Square customer
    let squareCustomerId: string | null = null
    
    // Check if user already has a Square customer ID
    const { data: subscription } = await supabase
      .from('subscriptions')
      .select('square_customer_id')
      .eq('user_id', user_id)
      .single()

    if (subscription?.square_customer_id) {
      squareCustomerId = subscription.square_customer_id
    } else {
      // Create new Square customer
      const customerResponse = await fetch(`${squareBaseUrl}/v2/customers`, {
        method: 'POST',
        headers: {
          'Square-Version': '2024-01-18',
          'Authorization': `Bearer ${accessToken}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          idempotency_key: `customer-${user_id}-${Date.now()}`,
          email_address: profile?.email,
          given_name: profile?.first_name || profile?.display_name?.split(' ')[0],
          family_name: profile?.last_name || profile?.display_name?.split(' ').slice(1).join(' '),
          reference_id: user_id, // Link to Supabase user
        }),
      })

      const customerData = await customerResponse.json()
      
      if (customerData.customer?.id) {
        squareCustomerId = customerData.customer.id
        
        // Save Square customer ID to subscription
        await supabase
          .from('subscriptions')
          .update({ square_customer_id: squareCustomerId })
          .eq('user_id', user_id)
      }
    }

    // Create subscription checkout
    const checkoutResponse = await fetch(`${squareBaseUrl}/v2/subscriptions`, {
      method: 'POST',
      headers: {
        'Square-Version': '2024-01-18',
        'Authorization': `Bearer ${accessToken}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        idempotency_key: `sub-${user_id}-${plan_id}-${Date.now()}`,
        location_id: Deno.env.get('SQUARE_LOCATION_ID'), // You may need to add this secret
        plan_variation_id: planConfig.squarePlanId,
        customer_id: squareCustomerId,
        start_date: new Date().toISOString().split('T')[0], // Today
        source: {
          name: 'Tracker Desktop App',
        },
      }),
    })

    const checkoutData = await checkoutResponse.json()

    if (checkoutData.errors) {
      console.error('Square API error:', checkoutData.errors)
      return new Response(
        JSON.stringify({ error: 'Failed to create subscription', details: checkoutData.errors }),
        { status: 500, headers: { ...corsHeaders, 'Content-Type': 'application/json' } }
      )
    }

    // Return checkout URL or subscription confirmation
    return new Response(
      JSON.stringify({
        success: true,
        subscription_id: checkoutData.subscription?.id,
        customer_id: squareCustomerId,
        // For hosted checkout, you might need to use payment links instead
        // This is a direct subscription creation
      }),
      { headers: { ...corsHeaders, 'Content-Type': 'application/json' } }
    )

  } catch (error) {
    console.error('Error:', error)
    return new Response(
      JSON.stringify({ error: error.message }),
      { status: 500, headers: { ...corsHeaders, 'Content-Type': 'application/json' } }
    )
  }
})


