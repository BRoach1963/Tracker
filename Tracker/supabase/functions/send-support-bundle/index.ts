import { serve } from "https://deno.land/std@0.168.0/http/server.ts";

const RESEND_API_KEY = Deno.env.get("RESEND_API_KEY");
const SUPPORT_EMAIL = "support@procohere.com"; // Change to your actual support email

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
};

interface SupportRequest {
  user_email: string;
  user_name: string;
  organization_name?: string;
  subject: string;
  description: string;
  bundle_url?: string;
  app_version: string;
  os_info: string;
}

serve(async (req) => {
  // Handle CORS preflight
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  try {
    const payload: SupportRequest = await req.json();

    const emailHtml = `
      <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 600px; margin: 0 auto;">
        <div style="background: #22C55E; color: white; padding: 20px; border-radius: 8px 8px 0 0;">
          <h1 style="margin: 0; font-size: 20px;">🐛 Support Request</h1>
        </div>
        
        <div style="background: #f9fafb; padding: 20px; border: 1px solid #e5e7eb; border-top: none;">
          <h2 style="margin-top: 0; color: #374151;">User Information</h2>
          <table style="width: 100%; border-collapse: collapse;">
            <tr><td style="padding: 8px 0; color: #6b7280;">Name:</td><td style="padding: 8px 0;"><strong>${payload.user_name}</strong></td></tr>
            <tr><td style="padding: 8px 0; color: #6b7280;">Email:</td><td style="padding: 8px 0;"><a href="mailto:${payload.user_email}">${payload.user_email}</a></td></tr>
            <tr><td style="padding: 8px 0; color: #6b7280;">Organization:</td><td style="padding: 8px 0;">${payload.organization_name || "N/A"}</td></tr>
            <tr><td style="padding: 8px 0; color: #6b7280;">App Version:</td><td style="padding: 8px 0;">${payload.app_version}</td></tr>
            <tr><td style="padding: 8px 0; color: #6b7280;">OS:</td><td style="padding: 8px 0;">${payload.os_info}</td></tr>
          </table>
          
          <h2 style="color: #374151; margin-top: 24px;">Issue Description</h2>
          <div style="background: white; padding: 16px; border-radius: 8px; border: 1px solid #e5e7eb;">
            <p style="margin: 0; white-space: pre-wrap;">${payload.description}</p>
          </div>
          
          ${payload.bundle_url ? `
          <h2 style="color: #374151; margin-top: 24px;">Diagnostic Bundle</h2>
          <p><a href="${payload.bundle_url}" style="display: inline-block; background: #3b82f6; color: white; padding: 10px 20px; border-radius: 6px; text-decoration: none;">Download Log Bundle</a></p>
          <p style="color: #6b7280; font-size: 12px;">Link expires in 7 days.</p>
          ` : ""}
        </div>
        
        <div style="padding: 16px; text-align: center; color: #9ca3af; font-size: 12px;">
          Sent from ProCohere Desktop App
        </div>
      </div>
    `;

    const res = await fetch("https://api.resend.com/emails", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${RESEND_API_KEY}`,
      },
      body: JSON.stringify({
        from: "ProCohere Support <noreply@procohere.com>",
        to: [SUPPORT_EMAIL],
        reply_to: payload.user_email,
        subject: `[Support] ${payload.subject}`,
        html: emailHtml,
      }),
    });

    const data = await res.json();

    if (!res.ok) {
      console.error("Resend error:", data);
      return new Response(JSON.stringify({ error: data }), {
        status: 400,
        headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    return new Response(JSON.stringify({ success: true, id: data.id }), {
      status: 200,
      headers: { ...corsHeaders, "Content-Type": "application/json" },
    });
  } catch (error) {
    console.error("Error:", error);
    return new Response(JSON.stringify({ error: error.message }), {
      status: 500,
      headers: { ...corsHeaders, "Content-Type": "application/json" },
    });
  }
});
